namespace SUIM;

using System.Xml.Linq;

public class Dock : LayoutElement
{
    public bool LastChildFill { get; set; } = true;
    public List<DockChild> DockChildren { get; } = [];

    public override void AddChild(UIElement child, XElement element)
    {
        base.AddChild(child, element);

        var dockChild = new DockChild { Element = child };
        var edgeAttr = element.Attribute("dock.edge");
        if (edgeAttr != null) dockChild.Edge = Enum.Parse<DockEdge>(edgeAttr.Value, true);
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

public class DockChild
{
    public UIElement Element { get; set; } = null!;
    public DockEdge Edge { get; set; }
}

public enum DockEdge
{
    Left,
    Right,
    Top,
    Bottom
}