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
        BackgroundColor = "#80808000";
        FillParent(Node);
        Node.nodeStyle["justify-content"] = "center";
        Node.nodeStyle["align-items"] = "center";
    }
}