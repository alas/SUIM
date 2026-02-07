namespace SUIM.Components;

public class Image : UIElement
{
    public string? Source { get; set; }
    public ImageStretch Stretch { get; set; }

    public override void SetAttribute(string name, object? value)
    {
        if (name.Equals("source", StringComparison.OrdinalIgnoreCase))
        {
            Source = Convert.ToString(value) ?? throw new ArgumentException($"Value for attribute '{name}' cannot be null.");
        }
        else if (name.Equals("stretch", StringComparison.OrdinalIgnoreCase))
        {
            var str = Convert.ToString(value) ?? throw new ArgumentException($"Value for attribute '{name}' cannot be null.");
            Stretch = Enum.Parse<ImageStretch>(str, true);
        }
        else
        {
            base.SetAttribute(name, value);
        }
    }
}

public enum ImageStretch
{
    None,
    Fill,
    FillOnStretch,
    Uniform,
    UniformToFill
}
