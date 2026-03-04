namespace SUIM.Parse.Components;

public class Label() : LayoutElement(nameof(Label))
{
    public string? For { get; set; }

    public override void SetAttribute(string name, object? value)
    {
        if (name.Equals("for", StringComparison.OrdinalIgnoreCase))
        {
            For = value is string s ? s : value?.ToString();
        }
        else
        {
            base.SetAttribute(name, value);
        }
    }

    public override string? GetAttribute(string name)
    {
        if (name.Equals("for", StringComparison.OrdinalIgnoreCase)) return For;
        return base.GetAttribute(name);
    }
}
