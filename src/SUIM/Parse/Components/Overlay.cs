namespace SUIM.Parse.Components;

using SUIM.Flexbox;

public class Overlay : LayoutElement
{
    public Overlay() : base(nameof(Overlay))
    {
        StopClicks = "true";
        BackgroundColor = "#80000000";
    }

    public override void ApplyLayout(float parentWidth, float parentHeight, Direction parentDirection)
    {
        throw new NotImplementedException();
    }
}