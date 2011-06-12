using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;
using Microsoft.VisualStudio.Text.Classification;

namespace HighlightLine
{
	// Use a custom background color for the line the caret is within.
	internal class HighlightLine
	{
		public HighlightLine(IWpfTextView view, IClassificationFormatMap formatMap, IClassificationType formatType)
		{
			m_view = view;
			m_layer = view.GetAdornmentLayer("CurrentLine");
			m_formatMap = formatMap;
			m_formatType = formatType;

			// Listen to any event that changes the layout (text changes, scrolling, etc)
			m_view.LayoutChanged += DoLayoutChanged;
			m_view.ViewportWidthChanged += (s, e) => DoDraw();
			m_view.ViewportLeftChanged += (s, e) => DoDraw();
			m_view.Selection.SelectionChanged += (s, e) => DoDraw();
			formatMap.ClassificationFormatMappingChanged += (s, e) => { m_fillBrush = null; DoReset(); };
		}

		#region Private Methods
		private void DoLayoutChanged(object sender, TextViewLayoutChangedEventArgs e)
		{
			SnapshotPoint caret = m_view.Caret.Position.BufferPosition;
			foreach (var line in e.NewOrReformattedLines)
			{
				if (line.ContainsBufferPosition(caret))
				{
					DoReset();
					break;
				}
			}
		}

		private void DoReset()
		{
			m_layer.RemoveAdornmentsByTag(AdornmentName);
			m_adornment = null;
			DoDraw();
		}

		private void DoDraw()
		{
			ITextViewLine startLine = DoGetLineByPos(m_view.Selection.Start.Position);
			ITextViewLine endLine = DoGetLineByPos(m_view.Selection.End.Position);

			// If the selection extends over more than one line then it is not helpful to highlight the
			// current line (and it looks dorky to do so as can be seen with the LineAdornment extension).
			if (startLine == endLine && m_view.TextViewLines != null)
			{
				SnapshotSpan span = startLine.Extent;
				Rect area = new Rect(
				   new Point(startLine.Left, startLine.Top),
				   new Point(Math.Max(m_view.ViewportRight - 2, startLine.Right), startLine.Bottom)	// TODO: height isn't always right for documents that use different font sizes
				);

				if (DoNeedsNewImage(area) || m_fillBrush == null)
					m_adornment = DoCreateAdornment(area);

				m_layer.RemoveAdornmentsByTag(AdornmentName);
				DoAddAdornment(startLine, area);
			}
			else if (m_adornment != null)
			{
				m_layer.RemoveAdornmentsByTag(AdornmentName);
				m_adornment = null;
			}
		}

		private void DoAddAdornment(ITextViewLine line, Rect area)
		{
			IWpfTextViewLineCollection textViewLines = m_view.TextViewLines;

			// Align the image with the top of the bounds of the text geometry
			Canvas.SetLeft(m_adornment, area.Left);
			Canvas.SetTop(m_adornment, area.Top);

			m_layer.AddAdornment(
				AdornmentPositioningBehavior.TextRelative, line.Extent,
				AdornmentName, m_adornment, null);
		}

		// Unlike the LineAdornment extension we do not add a border (a border adds a bit too much
		// visual noise and makes it hard to see the insertion point when it is in the first column).
		private Image DoCreateAdornment(Rect area)
		{
			// Create the brush we'll used to highlight the current line. The color will be
			// the CurrentLine property from the Fonts and Colors panel in the Options dialog.
			if (m_fillBrush == null)
			{
				TextFormattingRunProperties format = m_formatMap.GetTextProperties(m_formatType);
				m_fillBrush = format.BackgroundBrush;
			}

			var drawing = new GeometryDrawing();
			drawing.Brush = m_fillBrush;
			drawing.Geometry = new RectangleGeometry(area, 1.0, 1.0);
			drawing.Freeze();

			var drawingImage = new DrawingImage(drawing);
			drawingImage.Freeze();

			var image = new Image();
			image.UseLayoutRounding = false;		// work around WPF rounding bug
			image.Source = drawingImage;
			return image;
		}

		private bool DoNeedsNewImage(Rect area)
		{
			if (m_adornment == null)
				return true;

			if (DoAreClose(m_adornment.Width, area.Width))
				return true;

			return DoAreClose(m_adornment.Height, area.Height);
		}

		private bool DoAreClose(double d1, double d2)
		{
			double diff = d1 - d2;
			return Math.Abs(diff) < 0.1;
		}

		private ITextViewLine DoGetLineByPos(SnapshotPoint pos)
		{
			return m_view.GetTextViewLineContainingBufferPosition(pos);
		}
		#endregion

		#region Fields
		private const string AdornmentName = "HCurrentLine";

		private IAdornmentLayer m_layer;
		private IWpfTextView m_view;
		private IClassificationFormatMap m_formatMap;
		private IClassificationType m_formatType;
		private Brush m_fillBrush;
		private Image m_adornment;
		#endregion
	}
}
