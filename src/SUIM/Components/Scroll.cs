namespace SUIM.Components;

public class Scroll() : LayoutElement(nameof(Scroll))
{
    public ScrollDirection Direction { get; set; } = ScrollDirection.Vertical;
    public float ScrollX { get; set; }
    public float ScrollY { get; set; }
}

public enum ScrollDirection { None, Vertical, Horizontal, Both }
