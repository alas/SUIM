namespace SUIM.Components;

using System.Xml.Linq;
using SUIM.Components.Attributes;

public abstract class UIElement(string tagName)
{
    public string? Id { get; set; }
    public string? Class { get; set; }
    public string? HorizontalAlignment { get; set; }
    public string? VerticalAlignment { get; set; }
    public string? ContentHorizontalAlignment { get; set; }
    public string? ContentVerticalAlignment { get; set; }
    public string? X { get; set; }
    public string? Y { get; set; }
    public string? Width { get; set; }
    public string? Height { get; set; }
    public string? Margin { get; set; }
    public string? Padding { get; set; }
    public string? Font { get; set; }
    public string? FontSize { get; set; }
    public string? Anchor { get; set; }
    public string? Color { get; set; }
    public string? BackgroundColor { get; set; }
    public string? Opacity { get; set; }
    public string? ZIndex { get; set; }
    public string? Visibility { get; set; }
    public string? ReadOnly { get; set; }
    public string? Sprite { get; set; }
    public string? HoverSprite { get; set; }
    public string? PressedSprite { get; set; }
    public string? StopClicks { get; set; }
    public string? BackgroundImage { get; set; }

    // Internal properties calculated during parsing
    public string TagName { get; } = tagName.ToLowerInvariant();
    public UIElement? Parent { get; set; }
    public string? RootFont { get; set; }
    public float RootFontSize { get; set; }

    // Internal properties to the engine - not directly settable via markup attributes
    public List<BindingDefinition> Bindings { get; } = [];
    public dynamic? Model { get; set; }
    public bool IsComponentRoot { get; set; }
    public Dictionary<string, string> Events { get; set; } = [];
    public List<UIElement> Children { get; } = [];
    public Dictionary<string, object?> Attributes { get; } = [];

    // Actual layout properties calculated during measurement/arrangement
    public float ActualX { get; set; } = float.NaN;
    public float ActualY { get; set; } = float.NaN;
    public float ActualWidth { get; set; } = float.NaN;
    public float ActualHeight { get; set; } = float.NaN;

    // Layout calculation properties (transient, used during measurement/positioning)
    internal float MeasuredContentWidth { get; set; }
    internal float MeasuredContentHeight { get; set; }
    internal float ComputedMarginLeft { get; set; }
    internal float ComputedMarginTop { get; set; }
    internal float ComputedMarginRight { get; set; }
    internal float ComputedMarginBottom { get; set; }
    internal float ComputedPaddingLeft { get; set; }
    internal float ComputedPaddingTop { get; set; }
    internal float ComputedPaddingRight { get; set; }
    internal float ComputedPaddingBottom { get; set; }
    internal float CurrentFontSize { get; set; }
    internal bool NeedsVerticalScroll { get; set; }
    internal bool NeedsHorizontalScroll { get; set; }

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
        else if (name.Equals("x", StringComparison.OrdinalIgnoreCase))
        {
            X = value as string;
        }
        else if (name.Equals("y", StringComparison.OrdinalIgnoreCase))
        {
            Y = value as string;
        }
        else if (name.Equals("opacity", StringComparison.OrdinalIgnoreCase))
        {
            Opacity = value as string;
        }
        else if (name.Equals("z-index", StringComparison.OrdinalIgnoreCase))
        {
            ZIndex = value as string;
        }
        else if (name.Equals("visibility", StringComparison.OrdinalIgnoreCase))
        {
            Visibility = value as string;
        }
        else if (name.Equals("halign", StringComparison.OrdinalIgnoreCase) || name.Equals("horizontalalignment", StringComparison.OrdinalIgnoreCase))
        {
            HorizontalAlignment = value as string;
        }
        else if (name.Equals("valign", StringComparison.OrdinalIgnoreCase) || name.Equals("verticalalignment", StringComparison.OrdinalIgnoreCase))
        {
            VerticalAlignment = value as string;
        }
        else if (name.Equals("chalign", StringComparison.OrdinalIgnoreCase) || name.Equals("contenthorizontalalignment", StringComparison.OrdinalIgnoreCase))
        {
            ContentHorizontalAlignment = value as string;
        }
        else if (name.Equals("cvalign", StringComparison.OrdinalIgnoreCase) || name.Equals("contentverticalalignment", StringComparison.OrdinalIgnoreCase))
        {
            ContentVerticalAlignment = value as string;
        }
        else if (name.Equals("margin", StringComparison.OrdinalIgnoreCase))
        {
            Margin = value as string;
        }
        else if (name.Equals("padding", StringComparison.OrdinalIgnoreCase))
        {
            Padding = value as string;
        }
        else if (name.Equals("width", StringComparison.OrdinalIgnoreCase))
        {
            Width = value as string;
        }
        else if (name.Equals("height", StringComparison.OrdinalIgnoreCase))
        {
            Height = value as string;
        }
        else if (name.Equals("anchor", StringComparison.OrdinalIgnoreCase))
        {
            var s = value as string;
            Anchor = value as string;
        }
        else if (name.Equals("class", StringComparison.OrdinalIgnoreCase))
        {
            Class = value as string;
        }
        else if (name.StartsWith("on", StringComparison.OrdinalIgnoreCase))
        {
            var handlerName = value as string;
            // Treat only known event attributes (e.g., onclick) as events. Attributes that simply start with "on" but
            // are intended as component properties (like "onbuttonclick") should be preserved as regular attributes.
            var eventName = name[2..];
            if (string.Equals(eventName, "click", StringComparison.OrdinalIgnoreCase))
            {
                Events[eventName] = handlerName ?? throw new ArgumentException($"Value for attribute '{name}' must be a non-null string.");
            }
            else
            {
                // Preserve as regular attribute for components or custom usage
                Attributes[name] = value;
            }
        }
        else if (name.Equals("placeholder", StringComparison.OrdinalIgnoreCase))
        {
            (this as IPlaceholder)?.Placeholder = value as string;
        }
        else if (name.Equals("font", StringComparison.OrdinalIgnoreCase))
        {
            Font = value as string;
        }
        else if (name.Equals("fontsize", StringComparison.OrdinalIgnoreCase) || name.Equals("font-size", StringComparison.OrdinalIgnoreCase))
        {
            FontSize = value as string;
        }
        else if (name.Equals("color", StringComparison.OrdinalIgnoreCase))
        {
            Color = value as string;
        }
        else if (name.Equals("bg", StringComparison.OrdinalIgnoreCase) || name.Equals("background", StringComparison.OrdinalIgnoreCase) || name.Equals("backgroundcolor", StringComparison.OrdinalIgnoreCase))
        {
            BackgroundColor = value as string ?? value?.ToString();
        }
        else if (name.Equals("readonly", StringComparison.OrdinalIgnoreCase))
        {
            ReadOnly = value as string;
        }
        else if (name.Equals("stopclicks", StringComparison.OrdinalIgnoreCase))
        {
            StopClicks = value as string;
        }
        else if (name.Equals("backgroundimage", StringComparison.OrdinalIgnoreCase))
        {
            BackgroundImage = value as string;
        }
        else if (name.Contains('.'))
        {
            // ignore parent properties
        }
        else
        {
            Attributes[name] = value;
        }
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
        if (name.Equals("chalign", StringComparison.OrdinalIgnoreCase) || name.Equals("contenthorizontalalignment", StringComparison.OrdinalIgnoreCase)) return ContentHorizontalAlignment;
        if (name.Equals("cvalign", StringComparison.OrdinalIgnoreCase) || name.Equals("contentverticalalignment", StringComparison.OrdinalIgnoreCase)) return ContentVerticalAlignment;
        if (name.Equals("margin", StringComparison.OrdinalIgnoreCase)) return Margin;
        if (name.Equals("padding", StringComparison.OrdinalIgnoreCase)) return Padding;
        if (name.Equals("bg", StringComparison.OrdinalIgnoreCase) || name.Equals("background", StringComparison.OrdinalIgnoreCase) || name.Equals("backgroundcolor", StringComparison.OrdinalIgnoreCase)) return BackgroundColor;
        if (name.Equals("width", StringComparison.OrdinalIgnoreCase)) return Width;
        if (name.Equals("height", StringComparison.OrdinalIgnoreCase)) return Height;
        if (name.Equals("anchor", StringComparison.OrdinalIgnoreCase)) return Anchor;
        if (name.Equals("backgroundimage", StringComparison.OrdinalIgnoreCase)) return BackgroundImage;
        return null;
    }

    public float ToPixels(string? value) => ToPixels(UnitValue.Parse(value));

    public float ToPixels(UnitValue unitValue) => unitValue.Type switch
    {
        UnitType.Pixels => unitValue.Value,
        UnitType.Rem => unitValue.Value * RootFontSize,
        UnitType.Em => unitValue.Value * (Parent?.FontSize is string s ? Convert.ToSingle(s) : RootFontSize),
        UnitType.Auto => 0f, // Will be calculated during layout
        UnitType.Fr => 0f, // Will be calculated during fr distribution
        _ => 0f
    };

    public dynamic? GetEffectiveModel()
    {
        if (Model != null) return Model;
        if (IsComponentRoot) return null;
        return Parent?.GetEffectiveModel();
    }
}

public class LayoutElement(string tagName) : UIElement(tagName)
{
    public string? Spacing { get; set; }
    public bool Clip { get; set; }
    public Thickness SliceWidth { get; set; } = Thickness.None;

    public override void SetAttribute(string name, object? value)
    {
        if (name.Equals("spacing", StringComparison.OrdinalIgnoreCase))
        {
            Spacing = value as string;
        }
        else if (name.Equals("clip", StringComparison.OrdinalIgnoreCase))
        {
            Clip = value is bool b ? b : Convert.ToBoolean(value);
        }
        else if (name.Equals("slicewidth", StringComparison.OrdinalIgnoreCase))
        {
            SliceWidth = Thickness.FromObject(value);
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
