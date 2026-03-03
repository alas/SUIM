namespace SUIM.Parse.Components;

public class Overlay : LayoutElement
{
    public Overlay() : base(nameof(Overlay))
    {
        StopClicks = "true";
        BackgroundColor = "#80000000";
    }

    internal override void ApplySUIMLayout()
    {
        Node.StyleSetWidthPercent(100f);
        Node.StyleSetHeightPercent(100f);
        Node.StyleSetJustifyContent(Flexbox.Justify.Center);
        Node.StyleSetAlignItems(Flexbox.Align.Center);
    }
}