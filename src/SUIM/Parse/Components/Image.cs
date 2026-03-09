namespace SUIM.Parse.Components;

using SUIM.Flexbox;

public class Image() : UIElement(nameof(Image)), IMeasureFunc
{
    public static MeasureFunc? MeasureFunc { get; set; } = null;

    public string? Source { get; set; }
    public string? Stretch { get; set; }

    public override void SetAttribute(string name, object? value)
    {
        if (name.Equals("source", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(Node.nodeStyle["width"])
                && string.IsNullOrWhiteSpace(Node.nodeStyle["height"]) 
                && (string.IsNullOrWhiteSpace(Stretch) || "none".Equals(Stretch, StringComparison.OrdinalIgnoreCase)))
            {
                Node.SetMeasureFunc(MeasureFunc);
            }
            Source = value as string;
        }
        else if (name.Equals("stretch", StringComparison.OrdinalIgnoreCase))
        {
            if (value is string s && !"none".Equals(s, StringComparison.OrdinalIgnoreCase))
            {
                Node.SetMeasureFunc(null);
            }
            Stretch = value as string;
        }
        else
        {
            if (name.Equals("width", StringComparison.OrdinalIgnoreCase) || name.Equals("height", StringComparison.OrdinalIgnoreCase))
            {
                Node.SetMeasureFunc(null);
            }
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
