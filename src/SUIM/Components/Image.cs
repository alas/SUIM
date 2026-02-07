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
                Source = Convert.ToString(value);
                break;
            case "stretch":
                Stretch = Enum.Parse<ImageStretch>(Convert.ToString(value), true);
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
    Uniform,
    UniformToFill
}
