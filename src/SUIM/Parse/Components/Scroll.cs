using SUIM.Flexbox;

namespace SUIM.Parse.Components;

public class Scroll() : LayoutElement(nameof(Scroll))
{
    public ScrollDirection Direction { get; set; } = ScrollDirection.Vertical;
    public string? ScrollX { get; set; } = "auto";
    public string? ScrollY { get; set; } = "auto";

    public override void ApplyLayout(float parentWidth, float parentHeight, Direction parentDirection)
    {
        throw new NotImplementedException();
    }
}

public enum ScrollDirection { None, Vertical, Horizontal, Both }
