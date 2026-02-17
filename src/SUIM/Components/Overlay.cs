namespace SUIM.Components;

public class Overlay : LayoutElement
{
    public Overlay() : base(nameof(Overlay))
    {
        StopClicks = true;
        BackgroundColor = "#80000000";
    }
}