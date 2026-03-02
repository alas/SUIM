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
        throw new NotImplementedException();
    }
}