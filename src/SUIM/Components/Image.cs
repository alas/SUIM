using static System.Net.Mime.MediaTypeNames;

namespace SUIM;

public class Image : UIElement
{
    public string? Source { get; set; }
    public ImageStretch Stretch { get; set; }

    public override void SetAttribute(string name, string value)
    {
        switch (name)
        {
            case "source":
                Source = value;
                break;
            case "stretch":
                Stretch = Enum.Parse<ImageStretch>(value, true);
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
