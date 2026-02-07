namespace SUIM.Components;

using System.Xml.Linq;

public class Dock : LayoutElement
{
    public bool LastChildFill { get; set; } = true;
    public List<DockChild> DockChildren { get; } = [];

    public override void AddChild(UIElement child, XElement? element)
    {
        base.AddChild(child, element);

        var edgeAttr = element?.Attribute("dock.edge");
        var dockChild_Edge = edgeAttr != null ? Enum.Parse<DockEdge>(edgeAttr.Value, true) : DockEdge.Left;
        var dockChild = new DockChild(dockChild_Edge, child);
        DockChildren.Add(dockChild);
    }

    public override void RemoveChild(UIElement child)
    {
        base.RemoveChild(child);

        var dockChild = DockChildren.Find(dc => dc.Element == child);
        if (dockChild != null)
            DockChildren.Remove(dockChild);
    }

    public override void SetAttribute(string name, object? value)
    {
        switch (name)
        {
            case "lastchildfill":
                LastChildFill = Convert.ToBoolean(value);
                break;
            default:
                base.SetAttribute(name, value);
                break;
        }
    }
}

public record class DockChild(DockEdge Edge, UIElement Element);

public enum DockEdge
{
    Left,
    Right,
    Top,
    Bottom
}