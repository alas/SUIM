namespace SUIM.Parse.Components;

using SUIM.Flexbox;

public class Stack : LayoutElement
{
    public Node Node { get; } = new Node();

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

    public override void ApplyLayout(float parentWidth, float parentHeight, Direction parentDirection)
    {
        // Gap
        // Yoga added gap support later; if your version doesn't support Gap,
        // emulate spacing using margins on children:
        for (int i = 0; i < Node.Children.Count; i++)
        {
            var node = Node.Children[i];
            var succes = Flex.ParseValueFromString(Gap ?? "0", out var gap);

            if (Orientation == Orientation.Vertical)
            {
                var resolved = succes ? Flex.ResolveValue(gap, Parent?.MeasuredContentHeight ?? 0) : 0; ;
                node.StyleSetMargin(Edge.Top, i == 0 ? 0 : resolved);
                node.StyleSetMargin(Edge.Left, 0);
            }
            else
            {
                var resolved = succes ? Flex.ResolveValue(gap, Parent?.MeasuredContentWidth ?? 0) : 0; ;
                node.StyleSetMargin(Edge.Left, i == 0 ? 0 : resolved);
                node.StyleSetMargin(Edge.Top, 0);
            }
        }

        foreach (var child in Children)
        {
            child.ApplyLayout(parentWidth, parentHeight, parentDirection);
        }
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