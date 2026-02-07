namespace SUIM.Components;

public class Select : UIElement
{
    public string? SelectedValue { get; set; }
    public int SelectedIndex { get; set; }
    public bool Multiple { get; set; }
    public List<Option> Options { get; set; } = [];

    public override void SetAttribute(string name, object? value)
    {
        switch (name)
        {
            case "multiple":
                Multiple = Convert.ToBoolean(value);
                break;
            default:
                base.SetAttribute(name, value);
                break;
        }
    }
}

public class Option : UIElement
{
    public string? Value { get; set; }

    public override void SetAttribute(string name, object? value)
    {
        switch (name)
        {
            case "value":
                Value = Convert.ToString(value);
                break;
            default:
                base.SetAttribute(name, value);
                break;
        }
    }
}
