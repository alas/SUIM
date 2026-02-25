namespace SUIM.Components;

public class Scroll() : LayoutElement(nameof(Scroll))
{
    public ScrollDirection Direction { get; set; } = ScrollDirection.Vertical;
    public string? ScrollX { get; set; } = "auto";
    public string? ScrollY { get; set; } = "auto";
}

public enum ScrollDirection { None, Vertical, Horizontal, Both }
