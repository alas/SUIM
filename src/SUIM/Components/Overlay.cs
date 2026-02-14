namespace SUIM.Components;

public class Overlay : LayoutElement
{
    public Overlay() : base()
    {
        HorizontalAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment = VerticalAlignment.Stretch;
        StopClicks = true;
        BackgroundColor = "#80000000";
    }
}