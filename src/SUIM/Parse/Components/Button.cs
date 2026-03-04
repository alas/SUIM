namespace SUIM.Parse.Components;

public class Button() : UIElement(nameof(Button))
{
    public string? HoverImage { get; set; }
    public string? PressedImage { get; set; }

    public override void SetAttribute(string name, object? value)
    {
        if (name.Equals("notpressedimage", StringComparison.OrdinalIgnoreCase) || name.Equals("normal", StringComparison.OrdinalIgnoreCase) || name.Equals("normal-image", StringComparison.OrdinalIgnoreCase))
        {
            BackgroundImage = value as string;
        }
        else if (name.Equals("mouseoverimage", StringComparison.OrdinalIgnoreCase) || name.Equals("hover", StringComparison.OrdinalIgnoreCase) || name.Equals("hover-image", StringComparison.OrdinalIgnoreCase))
        {
            HoverImage = value as string;
        }
        else if (name.Equals("pressedimage", StringComparison.OrdinalIgnoreCase) || name.Equals("pressed", StringComparison.OrdinalIgnoreCase) || name.Equals("pressed-image", StringComparison.OrdinalIgnoreCase))
        {
            PressedImage = value as string;
        }
        else
        {
            base.SetAttribute(name, value);
        }
    }
}