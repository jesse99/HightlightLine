using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace HighlightLine
{
	// Establishes an IAdornmentLayer to place the adornment on and exports the <see IWpfTextViewCreationListener
	// that instantiates the adornment on the event of a IWpfTextView's creation
	[Export(typeof(IWpfTextViewCreationListener))]
	[ContentType("text")]
	[TextViewRole(PredefinedTextViewRoles.Document)]
	internal sealed class HighlightLineFactory : IWpfTextViewCreationListener
	{
		// Defines the adornment layer for the adornment. This layer is ordered 
		// after the selection layer in the Z-order
		[Name("HighlightLine")]
		[Export(typeof(AdornmentLayerDefinition))]
		[Order(After = PredefinedAdornmentLayers.Selection, Before = PredefinedAdornmentLayers.Text)]
		[TextViewRole(PredefinedTextViewRoles.Document)]
		public AdornmentLayerDefinition editorAdornmentLayer = null;

		// Instantiates a HighlightLine manager when a textView is created.
		public void TextViewCreated(IWpfTextView textView)
		{
			new HighlightLine(textView);
		}
	}
}
