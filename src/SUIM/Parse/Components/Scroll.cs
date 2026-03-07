namespace SUIM.Parse.Components;

public class Scroll() : LayoutElement(nameof(Scroll))
{
    public ScrollDirection Direction { get; set; } = ScrollDirection.Vertical;
    public string? ScrollX { get; set; } = "auto";
    public string? ScrollY { get; set; } = "auto";

    public override void SetAttribute(string name, object? value)
    {
        if (name.Equals("scrollx", StringComparison.OrdinalIgnoreCase) || name.Equals("scroll-x", StringComparison.OrdinalIgnoreCase))
        {
            ScrollX = value as string;
        }
        else if (name.Equals("scrolly", StringComparison.OrdinalIgnoreCase) || name.Equals("scroll-y", StringComparison.OrdinalIgnoreCase))
        {
            ScrollY= value as string;
        }
        else
        {
            base.SetAttribute(name, value);
        }
    }
}

public enum ScrollDirection { None, Vertical, Horizontal, Both }
