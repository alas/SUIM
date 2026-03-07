namespace SUIM.Parse.Components;

public class Button() : UIElement(nameof(Button))
{
    public string? NormalImage { get; set; }
    public string? HoverImage { get; set; }
    public string? PressedImage { get; set; }

    public override void SetAttribute(string name, object? value)
    {
        if (IsNormal.Contains(name))
        {
            NormalImage = value as string;
        }
        else if (IsHover.Contains(name))
        {
            HoverImage = value as string;
        }
        else if (IsPressed.Contains(name))
        {
            PressedImage = value as string;
        }
        else
        {
            base.SetAttribute(name, value);
        }
    }

    private static readonly HashSet<string> IsNormal = new(StringComparer.OrdinalIgnoreCase) { "notpressedimage", "normal", "normal-image" };
    private static readonly HashSet<string> IsHover = new(StringComparer.OrdinalIgnoreCase) { "mouseoverimage", "hover", "hover-image" };
    private static readonly HashSet<string> IsPressed = new(StringComparer.OrdinalIgnoreCase) { "pressedimage", "pressed", "pressed-image" };
}