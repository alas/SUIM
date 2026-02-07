namespace SUIM.Components;

public class Image : UIElement
{
    public string? Source { get; set; }
    public ImageStretch Stretch { get; set; }

    public override void SetAttribute(string name, object? value)
    {
        switch (name)
        {
            case "source":
                Source = Convert.ToString(value) ?? throw new ArgumentException($"Value for attribute '{name}' cannot be null.");
                break;
            case "stretch":
                var str = Convert.ToString(value) ?? throw new ArgumentException($"Value for attribute '{name}' cannot be null.");
                Stretch = Enum.Parse<ImageStretch>(str, true);
                break;
            default:
                base.SetAttribute(name, value);
                break;
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
