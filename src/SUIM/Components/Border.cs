namespace SUIM.Components;

public class Border : UIElement
{
    public Thickness BorderThickness { get; set; } = new Thickness(0);
    public string? BorderColor { get; set; }

    public override void SetAttribute(string name, object? value)
    {
        switch (name)
        {
            case "thickness":
                BorderThickness = Thickness.Parse(Convert.ToString(value));
                break;
            case "color":
                BorderColor = Convert.ToString(value);
                break;
            default:
                base.SetAttribute(name, value);
                break;
        }
    }
}
