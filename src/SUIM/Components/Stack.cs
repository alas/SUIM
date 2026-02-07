namespace SUIM.Components;

public class Stack : LayoutElement
{
    public Orientation Orientation { get; set; } = Orientation.Vertical;
    public bool Clip { get; set; }

    public override void SetAttribute(string name, object? value)
    {
        switch (name)
        {
            case "orientation":
                var str = Convert.ToString(value) ?? throw new ArgumentException($"Value for attribute '{name}' cannot be null.");
                Orientation = Enum.Parse<Orientation>(str, true);
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