using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;

namespace HighlightLine
{
	// HighlightLine places red boxes behind all the "A"s in the editor window
	internal class HighlightLine
	{
		public HighlightLine(IWpfTextView view)
		{
			m_view = view;
			m_layer = view.GetAdornmentLayer("HighlightLine");

			// Listen to any event that changes the layout (text changes, scrolling, etc)
			m_view.LayoutChanged += OnLayoutChanged;

			//Create the pen and brush to color the box behind the a's
			var brush = new SolidColorBrush(Color.FromArgb(0x20, 0x00, 0x00, 0xff));
			brush.Freeze();
			var penBrush = new SolidColorBrush(Colors.Red);
			penBrush.Freeze();
			var pen = new Pen(penBrush, 0.5);
			pen.Freeze();

			m_brush = brush;
			m_pen = pen;
		}

		#region Private Methods
		// On layout change add the adornment to any reformatted lines
		private void OnLayoutChanged(object sender, TextViewLayoutChangedEventArgs e)
		{
			foreach (ITextViewLine line in e.NewOrReformattedLines)
			{
				this.CreateVisuals(line);
			}
		}

		// Within the given line add the scarlet box behind the a
		private void CreateVisuals(ITextViewLine line)
		{
			// grab a reference to the lines in the current TextView 
			IWpfTextViewLineCollection textViewLines = m_view.TextViewLines;
			int start = line.Start;
			int end = line.End;

			// Loop through each character, and place a box around any a 
			for (int i = start; (i < end); ++i)
			{
				if (m_view.TextSnapshot[i] == 'a')
				{
					var span = new SnapshotSpan(m_view.TextSnapshot, Span.FromBounds(i, i + 1));
					Geometry g = textViewLines.GetMarkerGeometry(span);
					if (g != null)
					{
						var drawing = new GeometryDrawing(m_brush, m_pen, g);
						drawing.Freeze();

						var drawingImage = new DrawingImage(drawing);
						drawingImage.Freeze();

						var image = new Image();
						image.Source = drawingImage;

						// Align the image with the top of the bounds of the text geometry
						Canvas.SetLeft(image, g.Bounds.Left);
						Canvas.SetTop(image, g.Bounds.Top);

						m_layer.AddAdornment(AdornmentPositioningBehavior.TextRelative, span, null, image, null);
					}
				}
			}
		}
		#endregion

		#region Fields
		private IAdornmentLayer m_layer;
		private IWpfTextView m_view;
		private Brush m_brush;
		private Pen m_pen;
		#endregion
	}
}
