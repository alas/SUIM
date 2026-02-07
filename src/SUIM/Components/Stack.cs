namespace SUIM.Components;

public class Stack : LayoutElement
{
    public Orientation Orientation { get; set; } = Orientation.Vertical;
    public bool Clip { get; set; }

    public override void SetAttribute(string name, object? value)
    {
        if (name.Equals("orientation", StringComparison.OrdinalIgnoreCase))
        {
            var str = value as string ?? throw new ArgumentException($"Value for attribute '{name}' must be a non-null string.");
            Orientation = Enum.Parse<Orientation>(str, true);
        }
        else if (name.Equals("clip", StringComparison.OrdinalIgnoreCase))
        {
            Clip = value is bool b ? b : Convert.ToBoolean(value);
        }
        else
        {
            base.SetAttribute(name, value);
        }
    }
}

public enum Orientation
{
    Horizontal,
    Vertical
}