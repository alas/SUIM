namespace SUIM.Components;

public class Image : UIElement
{
    public string? Source { get; set; }
    public ImageStretch Stretch { get; set; }

    public override void SetAttribute(string name, object? value)
    {
        if (name.Equals("source", StringComparison.OrdinalIgnoreCase))
        {
            Source = value as string;
        }
        else if (name.Equals("stretch", StringComparison.OrdinalIgnoreCase))
        {
            Stretch = Enum.Parse<ImageStretch>((value as string)!, true);
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
