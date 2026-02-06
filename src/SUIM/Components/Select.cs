namespace SUIM;

public class Select : UIElement
{
    public string? SelectedValue { get; set; }
    public int SelectedIndex { get; set; }
    public List<Option> Options { get; set; } = [];
}

public class Option : UIElement
{
    public string? Value { get; set; }
}
