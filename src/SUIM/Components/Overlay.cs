namespace SUIM.Components;

public class Overlay : LayoutElement
{
    public Overlay() : base()
    {
        HorizontalAlignment = HorizontalAlignment.Center;
        VerticalAlignment = VerticalAlignment.Center;
        StopClicks = true;
        BackgroundColor = "#80000000";
    }
}