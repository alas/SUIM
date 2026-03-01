using SUIM.Flexbox;

namespace SUIM.Parse.Components;

public class Div() : LayoutElement(nameof(Div))
{
    public string? Display { get; set; }
    public string? FlexDirection { get; set; }

    public override void ApplyLayout(float parentWidth, float parentHeight, Direction parentDirection)
    {
        throw new NotImplementedException();
    }

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
        else
        {
            base.SetAttribute(name, value);
        }
    }

    public override string? GetAttribute(string name)
    {
        if (name.Equals("display", StringComparison.OrdinalIgnoreCase)) return Display;
        if (name.Equals("flex-direction", StringComparison.OrdinalIgnoreCase) || name.Equals("flexdirection", StringComparison.OrdinalIgnoreCase)) return FlexDirection;
        return base.GetAttribute(name);
    }
}
