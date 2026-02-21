namespace SUIM.Components;

public class Button() : UIElement(nameof(Button))
{
    public string? MouseOverImage { get; set; }
    public string? NotPressedImage { get; set; }
    public string? PressedImage { get; set; }

    public override void SetAttribute(string name, object? value)
    {
        if (name.Equals("mouseoverimage", StringComparison.OrdinalIgnoreCase) || name.Equals("hover", StringComparison.OrdinalIgnoreCase))
        {
            MouseOverImage = value as string;
        }
        else if (name.Equals("notpressedimage", StringComparison.OrdinalIgnoreCase) || name.Equals("normal", StringComparison.OrdinalIgnoreCase))
        {
            NotPressedImage = value as string;
        }
        else if (name.Equals("pressedimage", StringComparison.OrdinalIgnoreCase) || name.Equals("pressed", StringComparison.OrdinalIgnoreCase))
        {
            PressedImage = value as string;
        }
        else
        {
            base.SetAttribute(name, value);
        }
    }
}