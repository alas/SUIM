using SUIM.Flexbox;

namespace SUIM.Parse.Components;

public class Image() : UIElement(nameof(Image))
{
    public string? Source { get; set; }
    public string? Stretch { get; set; }

    public override void ApplyLayout(float parentWidth, float parentHeight, Direction parentDirection)
    {
        throw new NotImplementedException();
    }

    public override void SetAttribute(string name, object? value)
    {
        if (name.Equals("source", StringComparison.OrdinalIgnoreCase))
        {
            Source = value as string;
        }
        else if (name.Equals("stretch", StringComparison.OrdinalIgnoreCase))
        {
            Stretch = value as string;
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

public static class ImageStretchExtensions
{
    extension(ImageStretch)
    {
        public static ImageStretch FromString(string? value)
        {
            if (value == null) return default;

            return Enum.TryParse<ImageStretch>(value, true, out var r) ? r : default;
        }
    }
}
