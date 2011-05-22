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
	// HighlightLine places red boxes behind all the "A"s in the editor window
	internal class HighlightLine
	{
		public HighlightLine(IWpfTextView view, IClassificationFormatMap formatMap, IClassificationType formatType)
		{
			m_view = view;
			m_layer = view.GetAdornmentLayer("HighlightLine");
			m_formatMap = formatMap;
			m_formatType = formatType;

			// Listen to any event that changes the layout (text changes, scrolling, etc)
			m_view.Caret.PositionChanged += DoCaretPositionChanged;
			m_view.LayoutChanged += DoLayoutChanged;
			m_view.ViewportWidthChanged += DoViewportWidthChanged;
			m_view.ViewportLeftChanged += DoViewportLeftChanged;
			m_view.Selection.SelectionChanged += DoSelectionChanged;

			// Create the brush we'll used to highlight the current line. The color will be
			// the CurrentLine property from the Fonts and Colors panel in the Options dialog.
			TextFormattingRunProperties format = formatMap.GetTextProperties(formatType);
			m_fillBrush = format.BackgroundBrush;
			DoRedraw();
		}

		#region Private Methods
		private void DoLayoutChanged(object sender, TextViewLayoutChangedEventArgs e)
		{
			SnapshotPoint caret = m_view.Caret.Position.BufferPosition;
			foreach (var line in e.NewOrReformattedLines)
			{
				if (line.ContainsBufferPosition(caret))
				{
					m_adornment = null; // force recalculation
					DoCreateVisuals(line);
					break;
				}
			}
		}

		void DoViewportLeftChanged(object sender, EventArgs e)
		{
			DoRedraw();
		}

		void DoViewportWidthChanged(object sender, EventArgs e)
		{
			DoRedraw();
		}
		
		private void DoCaretPositionChanged(object sender, CaretPositionChangedEventArgs e)
		{
			ITextViewLine newLine = DoGetLineByPos(e.NewPosition.BufferPosition);
			ITextViewLine oldLine = DoGetLineByPos(e.OldPosition.BufferPosition);
			if (newLine != oldLine)
			{
				m_layer.RemoveAdornmentsByTag(AdornmentName);
				DoCreateVisuals(newLine);
			}
		}

		// If the selection extends over more than one line then it is not helpful to highlight the
		// current line (and it looks dorky to do so as can be seen with the LineAdornment extension).
		private void DoSelectionChanged(object sender, EventArgs e)
		{
			ITextViewLine startLine = DoGetLineByPos(m_view.Selection.Start.Position);
			ITextViewLine endLine = DoGetLineByPos(m_view.Selection.End.Position);
			if (startLine == endLine)
			{
				m_layer.RemoveAdornmentsByTag(AdornmentName);
				DoCreateVisuals(startLine);
			}
			else
			{
				m_layer.RemoveAdornmentsByTag(AdornmentName);
				m_adornment = null;
			}
		}

		private void DoRedraw()
		{
			if (m_view.TextViewLines != null)
			{
				if (m_adornment != null)
				{
					m_layer.RemoveAdornment(m_adornment);
					m_adornment = null; // force redraw
				}

				var caret = m_view.Caret.Position;
				ITextViewLine line = DoGetLineByPos(caret.BufferPosition);
				DoCreateVisuals(line);
			}
		}
		
		// Within the given line add the scarlet box behind the a
		private void DoCreateVisuals(ITextViewLine line)
		{
			IWpfTextViewLineCollection textViewLines = m_view.TextViewLines;
			if (textViewLines != null)
			{
				SnapshotSpan span = line.Extent;
				Rect area = new Rect(
				   new Point(line.Left, line.Top),
				   new Point(Math.Max(m_view.ViewportRight - 2, line.Right), line.Bottom)
				);

				if (DoNeedsNewImage(area))
					m_adornment = DoCreateAdornment(area);

				// Align the image with the top of the bounds of the text geometry
				Canvas.SetLeft(m_adornment, area.Left);
				Canvas.SetTop(m_adornment, area.Top);

				m_layer.AddAdornment(
				   AdornmentPositioningBehavior.TextRelative, span,
				   AdornmentName, m_adornment, null);
			}			
		}

		// Unlike the LineAdornment extension we do not add a border (a border adds a bit too much
		// visual noise and makes it hard to see the insertion point when it is in the first column).
		private Image DoCreateAdornment(Rect area)
		{
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
