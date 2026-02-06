namespace SUIM;

public class Image : UIElement
{
    public string? Source { get; set; }
    public ImageStretch Stretch { get; set; }
}

public enum ImageStretch
{
    None,
    Fill,
    Uniform,
    UniformToFill
}
