namespace SUIM.Parse.Components;

using SUIM.Flexbox;

/// <summary>
/// A simple container where children are arranged in a vertical stack by default.
/// Equivalent to a CSS block-level element (display: flex; flex-direction: column; align-items:stretch;).
/// </summary>
public class Div : LayoutElement
{
    public Div() : base(nameof(Div))
    {
        Node.StyleSetDisplay(Display.Flex);
        Node.StyleSetFlexDirection(FlexDirection.Column);
        Node.StyleSetAlignItems(Align.Stretch);
    }
}
