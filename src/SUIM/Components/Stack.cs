namespace SUIM;

public class Stack : UIElement
{
    public Orientation Orientation { get; set; } = Orientation.Vertical;
    public int Spacing { get; set; } = 0;
}

public enum Orientation
{
    Horizontal,
    Vertical
}