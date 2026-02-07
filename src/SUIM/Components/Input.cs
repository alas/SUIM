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
        switch (name)
        {
            case "type":
                var valueStr = Convert.ToString(value);
                if (valueStr == "datetime-local")
                {
                    Type = InputType.DatetimeLocal;
                }
                else
                {
                    Type = Enum.Parse<InputType>(valueStr, true);
                }
                break;
            case "value":
                Value = Convert.ToString(value);
                break;
            case "min":
                Min = Convert.ToInt32(value);
                break;
            case "max":
                Max = Convert.ToInt32(value);
                break;
            case "step":
                Step = Convert.ToInt32(value);
                break;
            case "mask":
                Mask = Convert.ToString(value);
                break;
            default:
                base.SetAttribute(name, value);
                break;
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
