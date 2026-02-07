namespace SUIM.Components;

public class Stack : LayoutElement
{
    public Orientation Orientation { get; set; } = Orientation.Vertical;
    public bool Clip { get; set; }

    public override void SetAttribute(string name, object? value)
    {
        if (name.Equals("orientation", StringComparison.OrdinalIgnoreCase))
        {
            var str = Convert.ToString(value) ?? throw new ArgumentException($"Value for attribute '{name}' cannot be null.");
            Orientation = Enum.Parse<Orientation>(str, true);
        }
        else if (name.Equals("clip", StringComparison.OrdinalIgnoreCase))
        {
            Clip = Convert.ToBoolean(value);
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