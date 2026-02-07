namespace SUIM.Components;

using System.Globalization;
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

    public virtual void AddChild(UIElement child, XElement? element)
    {
        child.Parent = this;
        Children.Add(child);
    }

    public virtual void RemoveChild(UIElement child)
    {
        child.Parent = null;
        Children.Remove(child);
    }

    public virtual void SetAttribute(string name, object? value)
    {
        if (name.Equals("id", StringComparison.OrdinalIgnoreCase))
        {
            Id = Convert.ToString(value);
        }
        else if (name.Equals("x", StringComparison.OrdinalIgnoreCase))
        {
            X = Convert.ToInt32(value);
        }
        else if (name.Equals("y", StringComparison.OrdinalIgnoreCase))
        {
            Y = Convert.ToInt32(value);
        }
        else if (name.Equals("opacity", StringComparison.OrdinalIgnoreCase))
        {
            Opacity = Convert.ToSingle(value);
        }
        else if (name.Equals("z-index", StringComparison.OrdinalIgnoreCase))
        {
            ZIndex = Convert.ToInt32(value);
        }
        else if (name.Equals("visibility", StringComparison.OrdinalIgnoreCase))
        {
            Visibility = Convert.ToString(value);
        }
        else if (name.Equals("halign", StringComparison.OrdinalIgnoreCase) || name.Equals("horizontalalignment", StringComparison.OrdinalIgnoreCase))
        {
            HorizontalAlignment = Enum.Parse<HorizontalAlignment>(Convert.ToString(value) ?? throw new ArgumentException($"Attribute '{name}' cannot be '{value}'"), true);
        }
        else if (name.Equals("valign", StringComparison.OrdinalIgnoreCase) || name.Equals("verticalalignment", StringComparison.OrdinalIgnoreCase))
        {
            VerticalAlignment = Enum.Parse<VerticalAlignment>(Convert.ToString(value) ?? throw new ArgumentException($"Attribute '{name}' cannot be '{value}'"), true);
        }
        else if (name.Equals("margin", StringComparison.OrdinalIgnoreCase))
        {
            Margin = Convert.ToString(value);
        }
        else if (name.Equals("padding", StringComparison.OrdinalIgnoreCase))
        {
            Padding = Convert.ToString(value);
        }
        else if (name.Equals("bg", StringComparison.OrdinalIgnoreCase) || name.Equals("background", StringComparison.OrdinalIgnoreCase))
        {
            Background = Convert.ToString(value);
        }
        else if (name.Equals("width", StringComparison.OrdinalIgnoreCase))
        {
            Width = Convert.ToString(value);
        }
        else if (name.Equals("height", StringComparison.OrdinalIgnoreCase))
        {
            Height = Convert.ToString(value);
        }
        else if (name.Equals("anchor", StringComparison.OrdinalIgnoreCase))
        {
            Anchor = Enum.Parse<Anchor>(Convert.ToString(value), true);
        }
        else if (name.Equals("class", StringComparison.OrdinalIgnoreCase))
        {
            Class = Convert.ToString(value);
        }
        else if (name.StartsWith("on", StringComparison.OrdinalIgnoreCase))
        {
            On(name.Substring(2), GetHandler(Convert.ToString(value)));
        }
        else if (name.Equals("placeholder", StringComparison.OrdinalIgnoreCase))
        {
            (this as IPlaceholder)?.Placeholder = Convert.ToString(value);
        }
        else if (name.Contains('.'))
        {
            // ignore parent properties
        }
        else
        {
            throw new NotSupportedException($"Attribute '{name}' is not supported on {GetType().Name}");
        }
    }

    private static Action<UIElement>? GetHandler(string value)
    {
        //throw new NotImplementedException();
        return null;
    }

    public virtual object? GetAttribute(string name)
    {
        if (name.Equals("id", StringComparison.OrdinalIgnoreCase)) return Id;
        if (name.Equals("class", StringComparison.OrdinalIgnoreCase)) return Class;
        if (name.Equals("x", StringComparison.OrdinalIgnoreCase)) return X;
        if (name.Equals("y", StringComparison.OrdinalIgnoreCase)) return Y;
        if (name.Equals("opacity", StringComparison.OrdinalIgnoreCase)) return Opacity;
        if (name.Equals("z-index", StringComparison.OrdinalIgnoreCase)) return ZIndex;
        if (name.Equals("visibility", StringComparison.OrdinalIgnoreCase)) return Visibility;
        if (name.Equals("halign", StringComparison.OrdinalIgnoreCase) || name.Equals("horizontalalignment", StringComparison.OrdinalIgnoreCase)) return HorizontalAlignment;
        if (name.Equals("valign", StringComparison.OrdinalIgnoreCase) || name.Equals("verticalalignment", StringComparison.OrdinalIgnoreCase)) return VerticalAlignment;
        if (name.Equals("margin", StringComparison.OrdinalIgnoreCase)) return Margin;
        if (name.Equals("padding", StringComparison.OrdinalIgnoreCase)) return Padding;
        if (name.Equals("bg", StringComparison.OrdinalIgnoreCase) || name.Equals("background", StringComparison.OrdinalIgnoreCase)) return Background;
        if (name.Equals("width", StringComparison.OrdinalIgnoreCase)) return Width;
        if (name.Equals("height", StringComparison.OrdinalIgnoreCase)) return Height;
        if (name.Equals("anchor", StringComparison.OrdinalIgnoreCase)) return Anchor;
        return null;
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

    public override void SetAttribute(string name, object? value)
    {
        if (name.Equals("spacing", StringComparison.OrdinalIgnoreCase))
        {
            Spacing = Convert.ToInt32(value);
        }
        else
        {
            base.SetAttribute(name, value);
        }
    }
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
