namespace SUIM;

public class Input : UIElement
{
    public string? Value { get; set; }
    public string? Placeholder { get; set; }

    public string? Type { get; set; } = "text";
}
