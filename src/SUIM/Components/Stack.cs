using static System.Net.Mime.MediaTypeNames;

namespace SUIM;

public class Stack : LayoutElement
{
    public Orientation Orientation { get; set; } = Orientation.Vertical;
    public bool Clip { get; set; }

    public override void SetAttribute(string name, object? value)
    {
        switch (name)
        {
            case "orientation":
                Orientation = Enum.Parse<Orientation>(Convert.ToString(value), true);
                break;
            case "clip":
                Clip = Convert.ToBoolean(value);
                break;
            default:
                base.SetAttribute(name, value);
                break;
        }
    }
}

public enum Orientation
{
    Horizontal,
    Vertical
}