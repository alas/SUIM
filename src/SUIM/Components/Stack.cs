namespace SUIM.Components;

public class Stack : LayoutElement
{
    public Orientation Orientation { get; set; } = Orientation.Vertical;

    public Stack() : base() { }

    public override void SetAttribute(string name, object? value)
    {
        if (name.Equals("orientation", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                Orientation = Orientation.FromObject(value);
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Value for attribute '{name}' must be a valid Orientation (either an Orientation enum value or a string).", ex);
            }
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

public static class OrientationExtensions
{
    extension(Orientation)
    {
        public static Orientation FromObject(object? obj)
        {
            if (obj is Orientation o)
            {
                return o;
            }
            else
            {
                var str = obj as string ?? throw new ArgumentException($"Value must be a non-null string.");
                return Enum.Parse<Orientation>(str, true);
            }
        }
    }
}