namespace SUIM.Parse.Components;

public class Progress() : UIElement(nameof(Progress))
{
    public float Value { get; set; } = 0f;
    public float Maximum { get; set; } = 100f;

    public override void SetAttribute(string name, object? value)
    {
        if (name.Equals("value", StringComparison.OrdinalIgnoreCase))
        {
            Value = value is float f ? f : Convert.ToSingle(value);
        }
        else if (name.Equals("max", StringComparison.OrdinalIgnoreCase) || name.Equals("maximum", StringComparison.OrdinalIgnoreCase))
        {
            Maximum = value is float f ? f : Convert.ToSingle(value);
        }
        else
        {
            base.SetAttribute(name, value);
        }
    }
}
