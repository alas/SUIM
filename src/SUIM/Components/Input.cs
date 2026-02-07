namespace SUIM;

public class Input : UIElement, IPlaceholder
{
    public string? Value { get; set; }
    public string? Placeholder { get; set; }
    public string? Mask { get; set; }
    public InputType Type { get; set; } = InputType.Text;
    public int? Min { get; set; }
    public int? Max { get; set; }
    public int? Step { get; set; }

    public override void SetAttribute(string name, string value)
    {
        switch (name)
        {
            case "type":
                if (value == "datetime-local")
                {
                    Type = InputType.DatetimeLocal;
                }
                else
                {
                    Type = Enum.Parse<InputType>(value, true);
                }
                break;
            case "value":
                Value = value;
                break;
            case "min":
                Min = int.Parse(value);
                break;
            case "max":
                Max = int.Parse(value);
                break;
            case "step":
                Step = int.Parse(value);
                break;
            case "mask":
                Mask = value;
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
