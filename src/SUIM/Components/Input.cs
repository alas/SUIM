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
