namespace SUIM.Parse.Components;

public class Div() : LayoutElement(nameof(Div))
{
    public string? Display { get; set; }
    public string? FlexDirection { get; set; }
    public string? JustifyContent { get; set; }
    public string? AlignItems { get; set; }

    public override void SetAttribute(string name, object? value)
    {
        if (name.Equals("display", StringComparison.OrdinalIgnoreCase))
        {
            Display = value as string;
        }
        else if (name.Equals("flex-direction", StringComparison.OrdinalIgnoreCase) || name.Equals("flexdirection", StringComparison.OrdinalIgnoreCase))
        {
            FlexDirection = value as string;
        }
        else if (name.Equals("justify-content", StringComparison.OrdinalIgnoreCase) || name.Equals("justifycontent", StringComparison.OrdinalIgnoreCase))
        {
            JustifyContent = value as string;
        }
        else if (name.Equals("align-items", StringComparison.OrdinalIgnoreCase) || name.Equals("alignitems", StringComparison.OrdinalIgnoreCase))
        {
            AlignItems = value as string;
        }
        else
        {
            base.SetAttribute(name, value);
        }
    }

    public override string? GetAttribute(string name)
    {
        if (name.Equals("display", StringComparison.OrdinalIgnoreCase)) return Display;
        if (name.Equals("flex-direction", StringComparison.OrdinalIgnoreCase) || name.Equals("flexdirection", StringComparison.OrdinalIgnoreCase)) return FlexDirection;
        if (name.Equals("justify-content", StringComparison.OrdinalIgnoreCase) || name.Equals("justifycontent", StringComparison.OrdinalIgnoreCase)) return JustifyContent;
        if (name.Equals("align-items", StringComparison.OrdinalIgnoreCase) || name.Equals("alignitems", StringComparison.OrdinalIgnoreCase)) return AlignItems;
        return base.GetAttribute(name);
    }
}
