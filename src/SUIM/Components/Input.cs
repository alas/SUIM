namespace SUIM.Components;

public class Input : UIElement, IPlaceholder
{
    public string? Value { get; set; }
    public string? Placeholder { get; set; }
    public string? Mask { get; set; }
    public InputType Type { get; set; } = InputType.Text;
    public int? Min { get; set; }
    public int? Max { get; set; }
    public int? Step { get; set; }

    public override void SetAttribute(string name, object? value)
    {
        if (name.Equals("type", StringComparison.OrdinalIgnoreCase))
        {
            var valueStr = Convert.ToString(value) ?? throw new ArgumentException($"Value for attribute '{name}' cannot be null.");
            if (valueStr.Equals("datetime-local", StringComparison.OrdinalIgnoreCase))
            {
                Type = InputType.DatetimeLocal;
            }
            else
            {
                Type = Enum.Parse<InputType>(valueStr, true);
            }
        }
        else if (name.Equals("value", StringComparison.OrdinalIgnoreCase))
        {
            Value = Convert.ToString(value) ?? throw new ArgumentException($"Value for attribute '{name}' cannot be null.");
        }
        else if (name.Equals("min", StringComparison.OrdinalIgnoreCase))
        {
            Min = Convert.ToInt32(value);
        }
        else if (name.Equals("max", StringComparison.OrdinalIgnoreCase))
        {
            Max = Convert.ToInt32(value);
        }
        else if (name.Equals("step", StringComparison.OrdinalIgnoreCase))
        {
            Step = Convert.ToInt32(value);
        }
        else if (name.Equals("mask", StringComparison.OrdinalIgnoreCase))
        {
            Mask = Convert.ToString(value) ?? throw new ArgumentException($"Value for attribute '{name}' cannot be null.");
        }
        else
        {
            base.SetAttribute(name, value);
        }
    }
}

public enum InputType
{
    Text,
    Password,
    Number,
    Range,
    Date,
    Time,
    Datetime,
    DatetimeLocal,
    Checkbox,
    Radio,
    Button,
    Email,
    Url,
}
