namespace SUIM;

using System.Xml.Linq;

public abstract class UIElement
{
    public string? Id { get; set; }
    public UIElement? Parent { get; set; }
    public HorizontalAlignment HorizontalAlignment { get; set; } = HorizontalAlignment.Left;
    public VerticalAlignment VerticalAlignment { get; set; } = VerticalAlignment.Top;
    public int? Width { get; set; }
    public int? Height { get; set; }
    public int Margin { get; set; } = 0;
    public int Padding { get; set; } = 0;
    public Anchor? Anchor { get; set; }
    public string? Background { get; set; }
    public int ZIndex { get; set; } = 0;
    public bool IsVisible { get; set; } = true;
    public bool IsEnabled { get; set; } = true;
    public List<PropertyBinding> Bindings { get; } = [];
    public Dictionary<string, List<Action<UIElement>>> EventHandlers { get; set; } = [];

    public List<UIElement> Children { get; } = [];

    public virtual void AddChild(UIElement child, XElement element)
    {
        child.Parent = this;
        Children.Add(child);
    }

    public virtual void RemoveChild(UIElement child)
    {
        child.Parent = null;
        Children.Remove(child);
    }

    public virtual void On(string eventName, Action<UIElement> handler)
    {
        if (!EventHandlers.TryGetValue(eventName, out List<Action<UIElement>>? value))
        {
            value = [];
            EventHandlers[eventName] = value;
        }

        value.Add(handler);
    }

    public virtual void Trigger(string eventName)
    {
        if (EventHandlers.TryGetValue(eventName, out var handlers))
        {
            foreach (var handler in handlers)
            {
                handler?.Invoke(this);
            }
        }
    }

    public UIElement? FindById(string id)
    {
        if (Id == id)
            return this;

        foreach (var child in Children)
        {
            var found = child.FindById(id);
            if (found != null)
                return found;
        }

        return null;
    }
}

public enum HorizontalAlignment
{
    Left,
    Center,
    Right,
    Stretch
}

public enum VerticalAlignment
{
    Top,
    Center,
    Bottom,
    Stretch
}

public enum Anchor
{
    TopLeft,
    TopRight,
    BottomLeft,
    BottomRight,
    Center
}
