namespace SUIM.Components;

public class BackgroundImage() : UIElement(nameof(BackgroundImage))
{
    public string? Source { get; set; }

    public override void SetAttribute(string name, object? value)
    {
        if (name.Equals("source", StringComparison.OrdinalIgnoreCase) || 
            name.Equals("backgroundimage", StringComparison.OrdinalIgnoreCase))
        {
            Source = value as string;
        }
        else
        {
            base.SetAttribute(name, value);
        }
    }
}
