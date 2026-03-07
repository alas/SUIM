namespace SUIM.Parse.Components;

using SUIM.Flexbox;

/// <summary>
/// Forces itself to parent size and intercepts all input.
/// </summary>
public class Overlay : LayoutElement
{
    public Overlay() : base(nameof(Overlay))
    {
        Node.nodeStyle["width"] = "100%";
        Node.nodeStyle["height"] = "100%";

        StopClicks = "true";
        BackgroundColor = "#80000000";

        Node.StyleSetPositionType(PositionType.Absolute);
        //Node.StyleSetPosition(Edge.Left, 0);
        //Node.StyleSetPosition(Edge.Right, 0);
        //Node.StyleSetPosition(Edge.Top, 0);
        //Node.StyleSetPosition(Edge.Bottom, 0);
        Node.nodeStyle["display"] = "flex";
        Node.nodeStyle["justify-content"] = "center";
        Node.nodeStyle["align-items"] = "center";
    }
}