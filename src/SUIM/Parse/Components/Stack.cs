namespace SUIM.Parse.Components;

using SUIM.Flexbox;

public class Stack : LayoutElement
{
    public Orientation Orientation
    {
        get;
        set
        {
            field = value;
            Node.StyleSetFlexDirection(value == Orientation.Horizontal ? FlexDirection.Row : FlexDirection.Column);
        }
    } = Orientation.Vertical;

    public Stack() : base(nameof(Stack))
    {
        Node.StyleSetDisplay(Display.Flex);
        Node.StyleSetAlignItems(Align.FlexStart);
        Node.StyleSetJustifyContent(Justify.FlexStart);
    }

    public override void SetAttribute(string name, object? value)
    {
        if (name.Equals("orientation", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                Orientation = Orientation.FromObject(value);
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Value for attribute '{name}' must be a valid Orientation (either an Orientation enum value or a string).", ex);
            }
        }
        else
        {
            base.SetAttribute(name, value);
        }
    }

    public override string? GetAttribute(string name)
    {
        if (name.Equals("orientation", StringComparison.OrdinalIgnoreCase)) return Orientation.ToString();

        return base.GetAttribute(name);
    }

    internal override void ApplySUIMLayout()
    {
        if (Gap != null && Flex.ParseValueFromString(Gap, out var gap))
        {
            // Yoga added gap support later; if your version doesn't support Gap,
            // emulate spacing using margins on children:
            for (int i = 0; i < Node.Children.Count; i++)
            {
                var node = Node.Children[i];

                if (Orientation == Orientation.Vertical)
                {
                    node.StyleSetMargin(Edge.Top, i == 0 ? Value.Zero : gap);
                    node.StyleSetMargin(Edge.Left, 0);
                }
                else
                {
                    node.StyleSetMargin(Edge.Left, i == 0 ? Value.Zero : gap);
                    node.StyleSetMargin(Edge.Top, 0);
                }
            }
        }

        base.ApplySUIMLayout();
    }
}

public enum Orientation
{
    Horizontal,
    Vertical
}

public static class OrientationExtensions
{
    extension(Orientation)
    {
        public static Orientation FromObject(object? obj)
        {
            if (obj is Orientation o)
            {
                return o;
            }
            else
            {
                var str = obj as string ?? throw new ArgumentException($"Value must be a non-null string.");
                return Enum.Parse<Orientation>(str, true);
            }
        }
    }
}