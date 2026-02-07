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
            Id = value as string ?? throw new ArgumentException($"Value for attribute '{name}' must be a non-null string.");
        }
        else if (name.Equals("x", StringComparison.OrdinalIgnoreCase))
        {
            X = value is int i ? i : Convert.ToInt32(value);
        }
        else if (name.Equals("y", StringComparison.OrdinalIgnoreCase))
        {
            Y = value is int i ? i : Convert.ToInt32(value);
        }
        else if (name.Equals("opacity", StringComparison.OrdinalIgnoreCase))
        {
            Opacity = value is float f ? f : Convert.ToSingle(value);
        }
        else if (name.Equals("z-index", StringComparison.OrdinalIgnoreCase))
        {
            ZIndex = value is int i ? i : Convert.ToInt32(value);
        }
        else if (name.Equals("visibility", StringComparison.OrdinalIgnoreCase))
        {
            Visibility = value as string ?? throw new ArgumentException($"Value for attribute '{name}' must be a non-null string.");
        }
        else if (name.Equals("halign", StringComparison.OrdinalIgnoreCase) || name.Equals("horizontalalignment", StringComparison.OrdinalIgnoreCase))
        {
            var s = value as string ?? throw new ArgumentException($"Value for attribute '{name}' must be a non-null string.");
            HorizontalAlignment = Enum.Parse<HorizontalAlignment>(s, true);
        }
        else if (name.Equals("valign", StringComparison.OrdinalIgnoreCase) || name.Equals("verticalalignment", StringComparison.OrdinalIgnoreCase))
        {
            var s = value as string ?? throw new ArgumentException($"Value for attribute '{name}' must be a non-null string.");
            VerticalAlignment = Enum.Parse<VerticalAlignment>(s, true);
        }
        else if (name.Equals("margin", StringComparison.OrdinalIgnoreCase))
        {
            Margin = value as string ?? value?.ToString() ?? throw new ArgumentException($"Value for attribute '{name}' must be a non-null string.");
        }
        else if (name.Equals("padding", StringComparison.OrdinalIgnoreCase))
        {
            Padding = value as string ?? value?.ToString() ?? throw new ArgumentException($"Value for attribute '{name}' must be a non-null string.");
        }
        else if (name.Equals("bg", StringComparison.OrdinalIgnoreCase) || name.Equals("background", StringComparison.OrdinalIgnoreCase))
        {
            Background = value as string ?? value?.ToString() ?? throw new ArgumentException($"Value for attribute '{name}' must be a non-null string.");
        }
        else if (name.Equals("width", StringComparison.OrdinalIgnoreCase))
        {
            Width = value as string ?? value?.ToString() ?? throw new ArgumentException($"Value for attribute '{name}' must be a non-null string.");
        }
        else if (name.Equals("height", StringComparison.OrdinalIgnoreCase))
        {
            Height = value as string ?? value?.ToString() ?? throw new ArgumentException($"Value for attribute '{name}' must be a non-null string.");
        }
        else if (name.Equals("anchor", StringComparison.OrdinalIgnoreCase))
        {
            var s = value as string ?? throw new ArgumentException($"Value for attribute '{name}' must be a non-null string.");
            Anchor = Enum.Parse<Anchor>(s, true);
        }
        else if (name.Equals("class", StringComparison.OrdinalIgnoreCase))
        {
            Class = value as string ?? throw new ArgumentException($"Value for attribute '{name}' must be a non-null string.");
        }
        else if (name.StartsWith("on", StringComparison.OrdinalIgnoreCase))
        {
            var handlerName = value as string ?? throw new ArgumentException($"Value for attribute '{name}' must be a non-null string.");
            On(name.Substring(2), GetHandler(handlerName));
        }
        else if (name.Equals("placeholder", StringComparison.OrdinalIgnoreCase))
        {
            (this as IPlaceholder)?.Placeholder = value as string ?? throw new ArgumentException($"Value for attribute '{name}' must be a non-null string.");
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
            Spacing = value is int i ? i : Convert.ToInt32(value);
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
