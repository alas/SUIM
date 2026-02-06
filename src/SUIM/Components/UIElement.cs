namespace SUIM;

using System.Xml.Linq;

public abstract class UIElement
{
    public string? Id { get; set; }
    public string? Class { get; set; }
    public UIElement? Parent { get; set; }
    public HorizontalAlignment HorizontalAlignment { get; set; } = HorizontalAlignment.Left;
    public VerticalAlignment VerticalAlignment { get; set; } = VerticalAlignment.Top;
    public int X { get; set; }
    public int Y { get; set; }
    public string? Width { get; set; }
    public string? Height { get; set; }
    public string? Margin { get; set; }
    public string? Padding { get; set; }
    public Anchor? Anchor { get; set; }
    public string? Background { get; set; }
    public string? Color { get; set; }
    public float Opacity { get; set; } = 1.0f;
    public int ZIndex { get; set; }
    public string? Visibility { get; set; }
    public bool ReadOnly { get; set; }
    public List<PropertyBinding> Bindings { get; } = [];
    public string? Sprite { get; set; }
    public string? HoverSprite { get; set; }
    public string? PressedSprite { get; set; }
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

    public virtual void On(string eventName, Action<UIElement>? handler)
    {
        if (!EventHandlers.TryGetValue(eventName, out List<Action<UIElement>>? value))
        {
            value = [];
            EventHandlers[eventName] = value;
        }

        if (handler != null)
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
}

public class LayoutElement : UIElement
{
    public int Spacing { get; set; }
}

public interface IPlaceholder
{
    string? Placeholder { get; set; }
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
