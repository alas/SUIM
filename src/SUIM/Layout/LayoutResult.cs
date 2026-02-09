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
    
    public float ContentX => X + MarginLeft + PaddingLeft;
    public float ContentY => Y + MarginTop + PaddingTop;
    public float ContentRight => ContentX + ContentWidth;
    public float ContentBottom => ContentY + ContentHeight;
}