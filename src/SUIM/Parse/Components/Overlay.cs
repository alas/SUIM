namespace SUIM.Parse.Components;

using SUIM.Flexbox;

/// <summary>
/// Forces itself to parent size and intercepts all input.
/// </summary>
public class Overlay : LayoutElement
{
    public Overlay() : base(nameof(Overlay))
    {
        StopClicks = "true";
        BackgroundColor = "#80000000";

        Node.StyleSetPositionType(PositionType.Absolute);
        Node.nodeStyle["left"] = "0";
        Node.nodeStyle["right"] = "0";
        Node.nodeStyle["top"] = "0";
        Node.nodeStyle["bottom"] = "0";
        Node.nodeStyle["width"] = "100%";
        Node.nodeStyle["height"] = "100%";
        Node.nodeStyle["display"] = "flex";
        Node.nodeStyle["flex-grow"] = "1";
        Node.nodeStyle["justify-content"] = "center";
        Node.nodeStyle["align-items"] = "center";
    }
}