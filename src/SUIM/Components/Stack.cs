namespace SUIM;

public class Stack : LayoutElement
{
    public Orientation Orientation { get; set; } = Orientation.Vertical;
    public bool Clip { get; set; }
}

public enum Orientation
{
    Horizontal,
    Vertical
}