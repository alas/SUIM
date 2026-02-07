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
            var valueStr = value as string ?? throw new ArgumentException($"Value for attribute '{name}' must be a non-null string.");
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
            Value = value as string ?? throw new ArgumentException($"Value for attribute '{name}' must be a non-null string.");
        }
        else if (name.Equals("min", StringComparison.OrdinalIgnoreCase))
        {
            Min = value is int i ? i : Convert.ToInt32(value);
        }
        else if (name.Equals("max", StringComparison.OrdinalIgnoreCase))
        {
            Max = value is int i ? i : Convert.ToInt32(value);
        }
        else if (name.Equals("step", StringComparison.OrdinalIgnoreCase))
        {
            Step = value is int i ? i : Convert.ToInt32(value);
        }
        else if (name.Equals("mask", StringComparison.OrdinalIgnoreCase))
        {
            Mask = value as string ?? throw new ArgumentException($"Value for attribute '{name}' must be a non-null string.");
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
