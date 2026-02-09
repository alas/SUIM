namespace SUIM.Layout;

public record struct LayoutResult
{
    public float X { get; set; }
    public float Y { get; set; }
    public float Width { get; set; }
    public float Height { get; set; }
    public float ContentWidth { get; set; }
    public float ContentHeight { get; set; }
    public float MarginLeft { get; set; }
    public float MarginTop { get; set; }
    public float MarginRight { get; set; }
    public float MarginBottom { get; set; }
    public float PaddingLeft { get; set; }
    public float PaddingTop { get; set; }
    public float PaddingRight { get; set; }
    public float PaddingBottom { get; set; }

    public readonly float GetContentX() => X + MarginLeft + PaddingLeft;

    public readonly float GetContentY() => Y + MarginTop + PaddingTop;

    public readonly float GetContentRight() => GetContentX() + ContentWidth;

    public readonly float GetContentBottom() => GetContentY() + ContentHeight;
}