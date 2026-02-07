namespace SUIM.Components;

public class Select : UIElement
{
    public string? SelectedValue { get; set; }
    public int SelectedIndex { get; set; }
    public bool Multiple { get; set; }
    public List<Option> Options { get; set; } = [];

    public override void SetAttribute(string name, object? value)
    {
        if (name.Equals("multiple", StringComparison.OrdinalIgnoreCase))
        {
            Multiple = Convert.ToBoolean(value);
        }
        else
        {
            base.SetAttribute(name, value);
        }
    }
}

public class Option : UIElement
{
    public string? Value { get; set; }

    public override void SetAttribute(string name, object? value)
    {
        if (name.Equals("value", StringComparison.OrdinalIgnoreCase))
        {
            Value = Convert.ToString(value);
        }
        else
        {
            base.SetAttribute(name, value);
        }
    }
}
