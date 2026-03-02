namespace SUIM.Parse.Components;

using SUIM.Flexbox;

public class Dock() : UIElement(nameof(Dock))
{
    public string? LastChildFill { get; set; } = "true";
    public List<DockChild> DockChildren { get; } = [];

    public override void SetAttribute(string name, object? value)
    {
        if (name.Equals("lastchildfill", StringComparison.OrdinalIgnoreCase))
        {
            LastChildFill = value as string;
        }
        else
        {
            base.SetAttribute(name, value);
        }
    }

    public override void ApplySUIMLayout()
    {
        Node.StyleSetFlexDirection(FlexDirection.Row);

        base.ApplySUIMLayout();
    }

    public void CalculateLayout(float parentWidth, float parentHeight, Direction parentDirection)
    {
        Node.Children.Clear();
        var current = Node;

        for (int i = 0; i < DockChildren.Count; i++)
        {
            var item = DockChildren[i];
            bool isLast = i == DockChildren.Count - 1;

            if (item.Edge == DockEdge.Fill || (isLast && "true".Equals(LastChildFill, StringComparison.OrdinalIgnoreCase)))
            {
                item.Node.StyleSetFlexGrow(1);
                current.AddChild(item.Node);
                break;
            }

            switch (item.Edge)
            {
                case DockEdge.Left:
                case DockEdge.Right:
                    current.StyleSetFlexDirection(FlexDirection.Row);

                    var mainCol = CreateFillColumn();
                    ApplyDockSize(item, isWidth: true);

                    if (item.Edge == DockEdge.Left)
                    {
                        current.AddChild(item.Node);
                        current.AddChild(mainCol);
                    }
                    else
                    {
                        current.AddChild(mainCol);
                        current.AddChild(item.Node);
                    }

                    current = mainCol;
                    break;

                case DockEdge.Top:
                case DockEdge.Bottom:
                    current.StyleSetFlexDirection(FlexDirection.Column);

                    var mainRow = CreateFillRow();
                    ApplyDockSize(item, isWidth: false);

                    if (item.Edge == DockEdge.Top)
                    {
                        current.AddChild(item.Node);
                        current.AddChild(mainRow);
                    }
                    else
                    {
                        current.AddChild(mainRow);
                        current.AddChild(item.Node);
                    }

                    current = mainRow;
                    break;
            }
        }

        Node.CalculateLayout(parentWidth, parentHeight, parentDirection);
    }

    private static Node CreateFillColumn()
    {
        var node = new Node();
        node.StyleSetFlexGrow(1);
        node.StyleSetFlexDirection(FlexDirection.Column);
        return node;
    }

    private static Node CreateFillRow()
    {
        var node = new Node();
        node.StyleSetFlexGrow(1);
        node.StyleSetFlexDirection(FlexDirection.Row);
        return node;
    }

    private static void ApplyDockSize(DockChild item, bool isWidth)
    {
        if (item.Size.HasValue)
        {
            if (isWidth)
                item.Node.StyleSetWidthPercent(item.Size.Value * 100f);
            else
                item.Node.StyleSetHeightPercent(item.Size.Value * 100f);
        }
    }
}

public class DockChild
{
    public DockEdge Edge { get; init; }
    public float? Size { get; init; } // % (0..1) or absolute px
    public required Node Node { get; init; }
}

public enum DockEdge
{
    Left,
    Right,
    Top,
    Bottom,
    Fill
}
