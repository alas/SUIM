namespace SUIM.Parse.Components;

using System.Xml.Linq;
using SUIM.Flexbox;

public class Dock : UIElement
{
    public string? LastChildFill { get; set; } = "true";
    public List<DockChild> DockChildren { get; } = [];

    private bool _isStructureBuilt = false;
    private readonly List<Node> _wrapperNodes = [];

    public Dock() : base(nameof(Dock))
    {
        Node.nodeStyle["width"] = "100%";
        Node.nodeStyle["height"] = "100%";
    }

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
        child.Parent = this;
        Children.Add(child);

        var edge = Enum.TryParse<DockEdge>(element?.Attribute("dock.edge")?.Value, true, out var parsedEdge) ? parsedEdge : DockEdge.Top;
        var dockChild = new DockChild(child, edge);

        DockChildren.Add(dockChild);
        _isStructureBuilt = false;
    }

    public override void RemoveChild(UIElement child)
    {
        child.Parent = null;
        Children.Remove(child);

        var dockChild = DockChildren.FirstOrDefault(gc => gc.Element == child);
        if (dockChild != null)
            DockChildren.Remove(dockChild);
        
        _isStructureBuilt = false;
    }

    public override void ClearChildren()
    {
        foreach (var child in Children)
            child.Parent = null;
        Children.Clear();
        DockChildren.Clear();
        _isStructureBuilt = false;
    }

    #endregion

    #region ApplySUIMLayout

    internal override void ApplySUIMLayout()
    {
        if (!_isStructureBuilt)
        {
            BuildDockStructure();
            _isStructureBuilt = true;
        }

        base.ApplySUIMLayout();
    }

    private void BuildDockStructure()
    {
        Node.Children.Clear();
        _wrapperNodes.Clear();
        
        Node.StyleSetFlexDirection(FlexDirection.Row);
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
                    _wrapperNodes.Add(mainCol);

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
                    _wrapperNodes.Add(mainRow);

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
