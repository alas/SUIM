namespace SUIM;

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

    public virtual void SetAttribute(string name, string value)
    {
        switch (name)
        {
            case "id":
                Id = value;
                break;
            case "x":
                X = int.Parse(value);
                break;
            case "y":
                Y = int.Parse(value);
                break;
            case "opacity":
                Opacity = float.Parse(value, CultureInfo.InvariantCulture);
                break;
            case "z-index":
                ZIndex = int.Parse(value);
                break;
            case "visibility":
                Visibility = value;
                break;
            case "halign" or "horizontalalignment":
                HorizontalAlignment = Enum.Parse<HorizontalAlignment>(value, true);
                break;
            case "valign" or "verticalalignment":
                VerticalAlignment = Enum.Parse<VerticalAlignment>(value, true);
                break;
            case "margin":
                Margin = value;
                break;
            case "padding":
                Padding = value;
                break;
            case "bg" or "background":
                Background = value;
                break;
            case "width":
                Width = value;
                break;
            case "height":
                Height = value;
                break;
            case "anchor":
                Anchor = Enum.Parse<Anchor>(value, true);
                break;
            case "class":
                Class = value;
                break;
            case string x when x.StartsWith("on"):
                On(x.Substring(2), GetHandler(value));
                break;
            case "placeholder":
                (this as IPlaceholder)?.Placeholder = value;
                break;
            case string x when x.Contains('.'):
                //todo: maybe move parent properties handling here
                break;
            default:
                throw new NotSupportedException($"Attribute '{name}' is not supported on {GetType().Name}");
        }
    }

    private static Action<UIElement>? GetHandler(string value)
    {
        //throw new NotImplementedException();
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

    public override void SetAttribute(string name, string value)
    {
        switch (name)
        {
            case "spacing":
                Spacing = int.Parse(value);
                break;
            default:
                base.SetAttribute(name, value);
                break;
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
