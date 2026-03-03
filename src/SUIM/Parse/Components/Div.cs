namespace SUIM.Parse.Components;

public class Div() : LayoutElement(nameof(Div))
{
    public string? Visibility { get; set; }

    public override void SetAttribute(string name, object? value)
    {
        if (name.Equals("visibility", StringComparison.OrdinalIgnoreCase))
        {
            Visibility = value as string;
        }
        else
        {
            base.SetAttribute(name, value);
        }
    }

    public override string? GetAttribute(string name)
    {
        if (name.Equals("visibility", StringComparison.OrdinalIgnoreCase)) return Visibility;
        return base.GetAttribute(name);
    }
}
