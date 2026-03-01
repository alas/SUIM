namespace SUIM.Parse.Components;

using SUIM.Flexbox;
using SUIM.Parse.Components.Attributes;
using System.Xml.Linq;

public abstract class UIElement(string tagName)
{
    public string? Id { get; set; }
    public string? Class { get; set; }
    public string? JustifySelf { get; set; }
    public string? JustifyItems { get; set; }
    public string? JustifyContent { get; set; }
    public string? AlignSelf { get; set; }
    public string? AlignItems { get; set; }
    public string? AlignContent { get; set; }
    public string? Top { get; set; }
    public string? Left { get; set; }
    public string? Bottom { get; set; }
    public string? Right { get; set; }
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
    public string? Visibility { get; set; } = "visible";
    public string? ReadOnly { get; set; }
    public string? StopClicks { get; set; }
    public string? BackgroundImage { get; set; }

    // Internal properties to the engine - not directly settable via markup attributes
    public string TagName { get; } = tagName.ToLowerInvariant();
    public UIElement? Parent { get; set; }
    public string? RootFont { get; set; }
    public float RootFontSize { get; set; } = float.NaN;
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
    public float MeasuredContentWidth { get; set; }
    public float MeasuredContentHeight { get; set; }
    public float ComputedMarginLeft { get; set; }
    public float ComputedMarginTop { get; set; }
    public float ComputedMarginRight { get; set; }
    public float ComputedMarginBottom { get; set; }
    public float ComputedPaddingLeft { get; set; }
    public float ComputedPaddingTop { get; set; }
    public float ComputedPaddingRight { get; set; }
    public float ComputedPaddingBottom { get; set; }
    public float CurrentFontSize { get; set; }
    public bool NeedsVerticalScroll { get; set; }
    public bool NeedsHorizontalScroll { get; set; }

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
        else if (name.Equals("top", StringComparison.OrdinalIgnoreCase))
        {
            Top = value as string;
        }
        else if (name.Equals("left", StringComparison.OrdinalIgnoreCase))
        {
            Left = value as string;
        }
        else if (name.Equals("bottom", StringComparison.OrdinalIgnoreCase))
        {
            Bottom = value as string;
        }
        else if (name.Equals("right", StringComparison.OrdinalIgnoreCase))
        {
            Right = value as string;
        }
        else if (name.Equals("opacity", StringComparison.OrdinalIgnoreCase))
        {
            Opacity = value as string;
        }
        else if (name.Equals("z-index", StringComparison.OrdinalIgnoreCase) || name.Equals("zindex", StringComparison.OrdinalIgnoreCase))
        {
            ZIndex = value as string;
        }
        else if (name.Equals("visibility", StringComparison.OrdinalIgnoreCase))
        {
            Visibility = value as string;
        }
        else if (name.Equals("justify-self", StringComparison.OrdinalIgnoreCase) || name.Equals("justifyself", StringComparison.OrdinalIgnoreCase))
        {
            JustifySelf = value as string;
        }
        else if (name.Equals("justify-items", StringComparison.OrdinalIgnoreCase) || name.Equals("justifyitems", StringComparison.OrdinalIgnoreCase))
        {
            JustifyItems = value as string;
        }
        else if (name.Equals("justify-content", StringComparison.OrdinalIgnoreCase) || name.Equals("justifycontent", StringComparison.OrdinalIgnoreCase))
        {
            JustifyContent = value as string;
        }
        else if (name.Equals("align-self", StringComparison.OrdinalIgnoreCase) || name.Equals("alignself", StringComparison.OrdinalIgnoreCase))
        {
            AlignSelf = value as string;
        }
        else if (name.Equals("align-items", StringComparison.OrdinalIgnoreCase) || name.Equals("alignitems", StringComparison.OrdinalIgnoreCase))
        {
            AlignItems = value as string;
        }
        else if (name.Equals("align-content", StringComparison.OrdinalIgnoreCase) || name.Equals("aligncontent", StringComparison.OrdinalIgnoreCase))
        {
            AlignContent = value as string;
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
        else if (name.Equals("font-size", StringComparison.OrdinalIgnoreCase) || name.Equals("fontsize", StringComparison.OrdinalIgnoreCase))
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
        else if (name.Equals("backgroundimage", StringComparison.OrdinalIgnoreCase))
        {
            BackgroundImage = value as string;
        }
        else if (name.Equals("readonly", StringComparison.OrdinalIgnoreCase))
        {
            ReadOnly = value as string;
        }
        else if (name.Equals("stopclicks", StringComparison.OrdinalIgnoreCase))
        {
            StopClicks = value as string;
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

    public virtual string? GetAttribute(string name)
    {
        if (name.Equals("id", StringComparison.OrdinalIgnoreCase)) return Id;
        if (name.Equals("class", StringComparison.OrdinalIgnoreCase)) return Class;
        if (name.Equals("top", StringComparison.OrdinalIgnoreCase)) return Top;
        if (name.Equals("left", StringComparison.OrdinalIgnoreCase)) return Left;
        if (name.Equals("bottom", StringComparison.OrdinalIgnoreCase)) return Bottom;
        if (name.Equals("right", StringComparison.OrdinalIgnoreCase)) return Right;
        if (name.Equals("opacity", StringComparison.OrdinalIgnoreCase)) return Opacity;
        if (name.Equals("z-index", StringComparison.OrdinalIgnoreCase) || name.Equals("zindex", StringComparison.OrdinalIgnoreCase)) return ZIndex;
        if (name.Equals("visibility", StringComparison.OrdinalIgnoreCase)) return Visibility;
        if (name.Equals("justify-self", StringComparison.OrdinalIgnoreCase) || name.Equals("justifyself", StringComparison.OrdinalIgnoreCase)) return JustifySelf;
        if (name.Equals("justify-items", StringComparison.OrdinalIgnoreCase) || name.Equals("justifyitems", StringComparison.OrdinalIgnoreCase)) return JustifyItems;
        if (name.Equals("justify-content", StringComparison.OrdinalIgnoreCase) || name.Equals("justifycontent", StringComparison.OrdinalIgnoreCase)) return JustifyContent;
        if (name.Equals("align-self", StringComparison.OrdinalIgnoreCase) || name.Equals("alignself", StringComparison.OrdinalIgnoreCase)) return AlignSelf;
        if (name.Equals("align-items", StringComparison.OrdinalIgnoreCase) || name.Equals("alignitems", StringComparison.OrdinalIgnoreCase)) return AlignItems;
        if (name.Equals("align-content", StringComparison.OrdinalIgnoreCase) || name.Equals("aligncontent", StringComparison.OrdinalIgnoreCase)) return AlignContent;
        if (name.Equals("margin", StringComparison.OrdinalIgnoreCase)) return Margin;
        if (name.Equals("padding", StringComparison.OrdinalIgnoreCase)) return Padding;
        if (name.Equals("bg", StringComparison.OrdinalIgnoreCase) || name.Equals("background", StringComparison.OrdinalIgnoreCase) || name.Equals("backgroundcolor", StringComparison.OrdinalIgnoreCase)) return BackgroundColor;
        if (name.Equals("backgroundimage", StringComparison.OrdinalIgnoreCase)) return BackgroundImage;
        if (name.Equals("width", StringComparison.OrdinalIgnoreCase)) return Width;
        if (name.Equals("height", StringComparison.OrdinalIgnoreCase)) return Height;
        if (name.Equals("anchor", StringComparison.OrdinalIgnoreCase)) return Anchor;
        return null;
    }

    public static float ToPixels(string? value) => ToPixels(UnitValue.Parse(value));

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

    public abstract void ApplyLayout(float parentWidth, float parentHeight, Direction parentDirection);
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
