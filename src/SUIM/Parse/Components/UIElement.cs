namespace SUIM.Parse.Components;

using System.Xml.Linq;
using SUIM.Flexbox;
using SUIM.Parse.Components.Attributes;

public abstract class UIElement(string tagName)
{
    public string? Id { get; set; }
    public string? Class { get; set; }
    public string? Anchor { get; set; }
    public string? Color { get; set; }
    public string? BackgroundColor { get; set; }
    public string? Opacity { get; set; }
    public string? ReadOnly { get; set; }
    public string? StopClicks { get; set; }
    public string? BackgroundImage { get; set; }

    // Internal properties to the engine - not directly settable via markup attributes
    public string TagName { get; } = tagName.ToLowerInvariant();
    public bool IsComponentRoot { get; set; }
    public UIElement? Parent { get; set; }
    public List<UIElement> Children { get; } = [];
    public List<BindingDefinition> Bindings { get; } = [];
    public dynamic? Model { get; set; }
    public Dictionary<string, string> Events { get; set; } = [];
    internal Node Node { get; } = new();

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

    public virtual void ClearChildren()
    {
        foreach (var child in Children)
        {
            child.Parent = null;
        }
        Children.Clear();
    }

    public virtual void SetAttribute(string name, object? value)
    {
        if (name.Equals("id", StringComparison.OrdinalIgnoreCase))
        {
            Id = value as string;
        }
        else if (name.Equals("anchor", StringComparison.OrdinalIgnoreCase))
        {
            Anchor = value as string;
        }
        else if (name.Equals("bg", StringComparison.OrdinalIgnoreCase) || name.Equals("background", StringComparison.OrdinalIgnoreCase) || name.Equals("backgroundcolor", StringComparison.OrdinalIgnoreCase))
        {
            BackgroundColor = value as string ?? value?.ToString();
        }
        else if (name.Equals("backgroundimage", StringComparison.OrdinalIgnoreCase))
        {
            BackgroundImage = value as string;
        }
        else if (name.Equals("class", StringComparison.OrdinalIgnoreCase))
        {
            Class = value as string;
        }
        else if (name.Equals("color", StringComparison.OrdinalIgnoreCase))
        {
            Color = value as string;
        }
        else if (name.Equals("opacity", StringComparison.OrdinalIgnoreCase))
        {
            Opacity = value as string;
        }
        else if (name.StartsWith("on", StringComparison.OrdinalIgnoreCase))
        {
            var handlerName = value as string;
            var eventName = name[2..];
            Events[eventName] = handlerName ?? throw new ArgumentException($"Value for attribute '{name}' must be a non-null string.");
        }
        else if (name.Equals("placeholder", StringComparison.OrdinalIgnoreCase))
        {
            (this as IPlaceholder)?.Placeholder = value as string;
        }
        else if (name.Equals("readonly", StringComparison.OrdinalIgnoreCase))
        {
            ReadOnly = value as string;
        }
        else if (value is string s && !s.StartsWith('@'))
        {
            Node.nodeStyle[name] = s;
        }
    }

    public virtual string? GetAttribute(string name)
    {
        if (name.Equals("id", StringComparison.OrdinalIgnoreCase)) return Id;
        if (name.Equals("anchor", StringComparison.OrdinalIgnoreCase)) return Anchor;
        if (name.Equals("bg", StringComparison.OrdinalIgnoreCase) || name.Equals("background", StringComparison.OrdinalIgnoreCase) || name.Equals("backgroundcolor", StringComparison.OrdinalIgnoreCase) || name.Equals("background-color", StringComparison.OrdinalIgnoreCase)) return BackgroundColor;
        if (name.Equals("backgroundimage", StringComparison.OrdinalIgnoreCase)) return BackgroundImage;
        if (name.Equals("class", StringComparison.OrdinalIgnoreCase)) return Class;
        if (name.Equals("color", StringComparison.OrdinalIgnoreCase)) return Color;
        if (name.Equals("opacity", StringComparison.OrdinalIgnoreCase)) return Opacity;
        if (name.Equals("placeholder", StringComparison.OrdinalIgnoreCase)) return (this as IPlaceholder)?.Placeholder;
        if (name.Equals("readonly", StringComparison.OrdinalIgnoreCase)) return ReadOnly;

        return Node.nodeStyle[name];
    }

    public float GetLeft()
    {
        return Node.LayoutGetLeft();
    }

    public float GetTop()
    {
        return Node.LayoutGetTop();
    }

    public float GetWidth()
    {
        return Node.LayoutGetWidth();
    }

    public float GetHeight()
    {
        return Node.LayoutGetHeight();
    }

    public static float ToPixels(string? value)
    {
        return ToPixels(UnitValue.Parse(value));
    }

    public static float ToPixels(UnitValue unitValue)
    {
        float val = unitValue.Type switch
        {
            UnitType.Pixels => unitValue.Value,
            UnitType.Auto => 0f, // Will be calculated during layout
            UnitType.Fr => 0f, // Will be calculated during fr distribution
            _ => 0f
        };
        return float.IsNaN(val) || float.IsInfinity(val) ? 0f : val;
    }

    public dynamic? GetEffectiveModel()
    {
        if (Model != null) return Model;
        if (IsComponentRoot) return null;
        return Parent?.GetEffectiveModel();
    }

    internal virtual void ApplySUIMLayout()
    {
        foreach (var child in Children)
        {
            child.ApplySUIMLayout();
        }
    }

    public void CalculateLayout(float parentWidth, float parentHeight, Direction parentDirection = Direction.LTR)
    {
        ApplySUIMLayout();
        Node.CalculateLayout(parentWidth, parentHeight, parentDirection);
    }
}

public record BindingDefinition(string TargetPropertyName, string ModelPropertyName);

public abstract class LayoutElement(string tagName) : UIElement(tagName)
{
    public string? Gap { get; set; }
    public string? RowGap { get; set; }
    public string? ColumnGap { get; set; }
    public bool Clip { get; set; }

    public override void SetAttribute(string name, object? value)
    {
        if (name.Equals("gap", StringComparison.OrdinalIgnoreCase))
        {
            Gap = value as string;
        }
        else if (name.Equals("row-gap", StringComparison.OrdinalIgnoreCase) || name.Equals("rowgap", StringComparison.OrdinalIgnoreCase))
        {
            RowGap = value as string;
        }
        else if (name.Equals("column-gap", StringComparison.OrdinalIgnoreCase) || name.Equals("columngap", StringComparison.OrdinalIgnoreCase))
        {
            ColumnGap = value as string;
        }
        else if (name.Equals("clip", StringComparison.OrdinalIgnoreCase))
        {
            Clip = value is bool b ? b : Convert.ToBoolean(value);
        }
        else
        {
            base.SetAttribute(name, value);
        }
    }

    public override string? GetAttribute(string name)
    {
        if (name.Equals("gap", StringComparison.OrdinalIgnoreCase)) return Gap;
        if (name.Equals("row-gap", StringComparison.OrdinalIgnoreCase) || name.Equals("rowgap", StringComparison.OrdinalIgnoreCase)) return RowGap;
        if (name.Equals("column-gap", StringComparison.OrdinalIgnoreCase) || name.Equals("columngap", StringComparison.OrdinalIgnoreCase)) return ColumnGap;
        if (name.Equals("clip", StringComparison.OrdinalIgnoreCase)) return Clip.ToString();

        return base.GetAttribute(name);
    }
}

public interface IPlaceholder
{
    string? Placeholder { get; set; }
}
