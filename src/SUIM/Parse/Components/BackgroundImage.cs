namespace SUIM.Parse.Components;

public class BackgroundImage : UIElement
{
    public string? Source { get; set; }

    public BackgroundImage() : base(nameof(BackgroundImage))
    {
        FillParent(Node);
    }

    public override void SetAttribute(string name, object? value)
    {
        if (name.Equals("source", StringComparison.OrdinalIgnoreCase) || 
            name.Equals("backgroundimage", StringComparison.OrdinalIgnoreCase) || 
            name.Equals("background-image", StringComparison.OrdinalIgnoreCase))
        {
            Source = value as string;
        }
        else
        {
            base.SetAttribute(name, value);
        }
    }
}
