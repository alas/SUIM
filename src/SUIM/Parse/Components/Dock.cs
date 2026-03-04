namespace SUIM.Parse.Components;

using SUIM.Flexbox;
using System.Xml.Linq;

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


    #region Children

    public override void AddChild(UIElement child, XElement? element)
    {
        base.AddChild(child, element);

        var edge = Enum.TryParse<DockEdge>(element?.Attribute("dock.edge")?.Value, true, out var parsedEdge) ? parsedEdge : DockEdge.Top;
        var dockChild = new DockChild(child, edge);

        DockChildren.Add(dockChild);
    }

    public override void RemoveChild(UIElement child)
    {
        base.RemoveChild(child);

        var gridChild = DockChildren.FirstOrDefault(gc => gc.Element == child);
        if (gridChild != null)
            DockChildren.Remove(gridChild);
    }

    public override void ClearChildren()
    {
        base.ClearChildren();
        DockChildren.Clear();
    }

    #endregion

    #region ApplySUIMLayout

    internal override void ApplySUIMLayout()
    {
        Node.StyleSetFlexDirection(FlexDirection.Row);
        Node.Children.Clear();
        var current = Node;

        for (int i = 0; i < DockChildren.Count; i++)
        {
            var item = DockChildren[i];
            bool isLast = i == DockChildren.Count - 1;

            if (item.Edge == DockEdge.Fill || (isLast && "true".Equals(LastChildFill, StringComparison.OrdinalIgnoreCase)))
            {
                item.Element.Node.StyleSetFlexGrow(1);
                current.AddChild(item.Element.Node);
                break;
            }

            switch (item.Edge)
            {
                case DockEdge.Left:
                case DockEdge.Right:
                    current.StyleSetFlexDirection(FlexDirection.Row);

                    var mainCol = CreateFillColumn();

                    if (item.Edge == DockEdge.Left)
                    {
                        current.AddChild(item.Element.Node);
                        current.AddChild(mainCol);
                    }
                    else
                    {
                        current.AddChild(mainCol);
                        current.AddChild(item.Element.Node);
                    }

                    current = mainCol;
                    break;

                case DockEdge.Top:
                case DockEdge.Bottom:
                    current.StyleSetFlexDirection(FlexDirection.Column);

                    var mainRow = CreateFillRow();

                    if (item.Edge == DockEdge.Top)
                    {
                        current.AddChild(item.Element.Node);
                        current.AddChild(mainRow);
                    }
                    else
                    {
                        current.AddChild(mainRow);
                        current.AddChild(item.Element.Node);
                    }

                    current = mainRow;
                    break;
            }
        }

        base.ApplySUIMLayout();
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

    #endregion
}

public record class DockChild(UIElement Element, DockEdge Edge);

public enum DockEdge
{
    Left,
    Right,
    Top,
    Bottom,
    Fill
}
