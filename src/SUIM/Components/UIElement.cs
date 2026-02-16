namespace SUIM.Components;

using System.Xml.Linq;
using SUIM.Layout;

public abstract class UIElement
{
    public string? Id { get; set; }
    public string? Class { get; set; }
    public UIElement? Parent { get; set; }
    public HorizontalAlignment HorizontalAlignment { get; set; } = HorizontalAlignment.Left;
    public VerticalAlignment VerticalAlignment { get; set; } = VerticalAlignment.Top;
    public UnitValue X { get; set; }
    public UnitValue Y { get; set; }
    public UnitValue Width { get; set; } = UnitValue.Auto;
    public UnitValue Height { get; set; } = UnitValue.Auto;
    public Thickness Margin { get; set; } = Thickness.None;
    public Thickness Padding { get; set; } = Thickness.None;
    public float ActualX { get; set; } = float.NaN;
    public float ActualY { get; set; } = float.NaN;
    public float ActualWidth { get; set; } = float.NaN;
    public float ActualHeight { get; set; } = float.NaN;
    public string? Font { get; set; }
    public float FontSize { get; set; }
    public string? RootFont { get; set; }
    public float RootFontSize { get; set; }
    public Anchor? Anchor { get; set; }
    public string? Color { get; set; }
    public string? BackgroundColor { get; set; }
    public float Opacity { get; set; } = 1.0f;
    public int ZIndex { get; set; }
    public string? Visibility { get; set; }
    public bool ReadOnly { get; set; }
    public List<BindingDefinition> Bindings { get; } = [];
    public string? Sprite { get; set; }
    public string? HoverSprite { get; set; }
    public string? PressedSprite { get; set; }
    public bool StopClicks { get; set; }
    public Dictionary<string, string> Events { get; set; } = [];
    public List<UIElement> Children { get; } = [];

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
            Id = value as string ?? throw new ArgumentException($"Value for attribute '{name}' must be a non-null string.");
        }
        else if (name.Equals("x", StringComparison.OrdinalIgnoreCase))
        {
            X = UnitValue.FromObject(value);
        }
        else if (name.Equals("y", StringComparison.OrdinalIgnoreCase))
        {
            Y = UnitValue.FromObject(value);
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
            Margin = Thickness.FromObject(value);
        }
        else if (name.Equals("padding", StringComparison.OrdinalIgnoreCase))
        {
            Padding = Thickness.FromObject(value);
        }
        else if (name.Equals("width", StringComparison.OrdinalIgnoreCase))
        {
            Width = UnitValue.FromObject(value);
        }
        else if (name.Equals("height", StringComparison.OrdinalIgnoreCase))
        {
            Height = UnitValue.FromObject(value);
        }
        else if (name.Equals("anchor", StringComparison.OrdinalIgnoreCase))
        {
            var s = value as string ?? throw new ArgumentException($"Value for attribute '{name}' must be a non-null string.");
            Anchor = ParseAnchor(s);
        }
        else if (name.Equals("class", StringComparison.OrdinalIgnoreCase))
        {
            Class = value as string ?? throw new ArgumentException($"Value for attribute '{name}' must be a non-null string.");
        }
        else if (name.StartsWith("on", StringComparison.OrdinalIgnoreCase))
        {
            var handlerName = value as string ?? throw new ArgumentException($"Value for attribute '{name}' must be a non-null string.");
            Events[name.Substring(2)] = handlerName;
        }
        else if (name.Equals("placeholder", StringComparison.OrdinalIgnoreCase))
        {
            (this as IPlaceholder)?.Placeholder = value as string ?? throw new ArgumentException($"Value for attribute '{name}' must be a non-null string.");
        }
        else if (name.Equals("font", StringComparison.OrdinalIgnoreCase))
        {
            Font = value as string ?? throw new ArgumentException($"Value for attribute '{name}' must be a non-null string.");
        }
        else if (name.Equals("fontsize", StringComparison.OrdinalIgnoreCase) || name.Equals("font-size", StringComparison.OrdinalIgnoreCase))
        {
            FontSize = value is float f ? f : Convert.ToSingle(value);
        }
        else if (name.Equals("color", StringComparison.OrdinalIgnoreCase))
        {
            Color = value as string ?? throw new ArgumentException($"Value for attribute '{name}' must be a non-null string.");
        }
        else if (name.Equals("bg", StringComparison.OrdinalIgnoreCase) || name.Equals("background", StringComparison.OrdinalIgnoreCase) || name.Equals("backgroundcolor", StringComparison.OrdinalIgnoreCase))
        {
            BackgroundColor = value as string ?? value?.ToString() ?? throw new ArgumentException($"Value for attribute '{name}' must be a non-null string.");
        }
        else if (name.Equals("readonly", StringComparison.OrdinalIgnoreCase))
        {
            ReadOnly = value is bool b ? b : Convert.ToBoolean(value);
        }
        else if (name.Equals("stopclicks", StringComparison.OrdinalIgnoreCase))
        {
            StopClicks = value is bool b ? b : Convert.ToBoolean(value);
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
        if (name.Equals("bg", StringComparison.OrdinalIgnoreCase) || name.Equals("background", StringComparison.OrdinalIgnoreCase) || name.Equals("backgroundcolor", StringComparison.OrdinalIgnoreCase)) return BackgroundColor;
        if (name.Equals("width", StringComparison.OrdinalIgnoreCase)) return Width;
        if (name.Equals("height", StringComparison.OrdinalIgnoreCase)) return Height;
        if (name.Equals("anchor", StringComparison.OrdinalIgnoreCase)) return Anchor;
        return null;
    }

    private static Anchor ParseAnchor(string value)
    {
        var parts = value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        Anchor result = Components.Anchor.None;
        
        foreach (var part in parts)
        {
            if (Enum.TryParse<Anchor>(part, true, out var anchor))
            {
                result |= anchor;
            }
        }
        
        return result;
    }

    public float ToPixels(UnitValue unitValue) => unitValue.Type switch
    {
        UnitType.Pixels => unitValue.Value,
        UnitType.Rem => unitValue.Value * RootFontSize,
        UnitType.Em => unitValue.Value * (Parent?.FontSize ?? RootFontSize),
        UnitType.Auto => 0f, // Will be calculated during layout
        UnitType.Fr => 0f, // Will be calculated during fr distribution
        _ => 0f
    };
}

public class LayoutElement : UIElement
{
    public int Spacing { get; set; }
    public bool Clip { get; set; }
    public Thickness SliceWidth { get; set; } = Thickness.None;

    public LayoutElement() : base()
    {
        Width = UnitValue.OneFR;
        Height = UnitValue.OneFR;
    }

    public override void SetAttribute(string name, object? value)
    {
        if (name.Equals("spacing", StringComparison.OrdinalIgnoreCase))
        {
            Spacing = value is int i ? i : Convert.ToInt32(value);
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

public enum HorizontalAlignment
{
    Left,
    Center,
    Right
}

public enum VerticalAlignment
{
    Top,
    Center,
    Bottom
}

[Flags]
public enum Anchor
{
    None = 0,
    Top = 1,
    Bottom = 2,
    Left = 4,
    Right = 8
}
