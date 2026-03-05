namespace SUIM.Parse.Components;

using SUIM.Flexbox;

public class Overlay : LayoutElement
{
    public Overlay() : base(nameof(Overlay))
    {
        StopClicks = "true";
        BackgroundColor = "#80000000";

        Node.StyleSetPositionType(PositionType.Absolute);
        Node.StyleSetPosition(Edge.Left, 0);
        Node.StyleSetPosition(Edge.Right, 0);
        Node.StyleSetPosition(Edge.Top, 0);
        Node.StyleSetPosition(Edge.Bottom, 0);
        Node.StyleSetJustifyContent(Justify.Center);
        Node.StyleSetAlignItems(Align.Center);
    }
}