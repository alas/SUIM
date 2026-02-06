namespace SUIM;

internal class Scroll : LayoutElement
{
    public ScrollDirection Direction { get; set; } = ScrollDirection.Vertical;
    public float ScrollX { get; set; }
    public float ScrollY { get; set; }
}

public enum ScrollDirection { None, Vertical, Horizontal, Both }
