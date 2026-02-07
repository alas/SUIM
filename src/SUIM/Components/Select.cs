namespace SUIM;

public class Select : UIElement
{
    public string? SelectedValue { get; set; }
    public int SelectedIndex { get; set; }
    public bool Multiple { get; set; }
    public List<Option> Options { get; set; } = [];

    public override void SetAttribute(string name, string value)
    {
        switch (name)
        {
            case "multiple":
                Multiple = bool.Parse(value);
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

    public override void SetAttribute(string name, string value)
    {
        switch (name)
        {
            case "value":
                Value = value;
                break;
            default:
                base.SetAttribute(name, value);
                break;
        }
    }
}
