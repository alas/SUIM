namespace SUIM.Parse.Components;

public class Border() : UIElement(nameof(Border))
{
    public string? Value { get; set; }
    public string? Thickness { get; set; }
    public string? Style { get; set; }

    public override void SetAttribute(string name, object? value)
    {
        if (name.Equals("border", StringComparison.OrdinalIgnoreCase) || name.Equals("value", StringComparison.OrdinalIgnoreCase))
        {
            Value = value as string;
            if (!string.IsNullOrWhiteSpace(Value))
            {
                var borderAttributes = Value.Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries);
                if (borderAttributes.Length > 0 && !string.IsNullOrEmpty(borderAttributes[0]))
                {
                    Thickness = borderAttributes[0];
                }

                if (borderAttributes.Length > 1 && !string.IsNullOrEmpty(borderAttributes[1]))
                {
                    Style = borderAttributes[1];
                }

                if (borderAttributes.Length > 2 && !string.IsNullOrEmpty(borderAttributes[2]))
                {
                    Color = borderAttributes[2];
                }
            }
        }
        else if (name.Equals("thickness", StringComparison.OrdinalIgnoreCase))
        {
            Thickness = value as string;
        }
        else if (name.Equals("style", StringComparison.OrdinalIgnoreCase))
        {
            Style = value as string;
        }
        else
        {
            base.SetAttribute(name, value);
        }
    }
}
