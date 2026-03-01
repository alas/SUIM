namespace SUIM.Flexbox;

public class Style
{
    internal Direction Direction = Direction.Inherit;
    internal FlexDirection FlexDirection = FlexDirection.Row;
    internal Justify JustifyContent = Justify.FlexStart;
    internal Align AlignContent = Align.Stretch;
    internal Align AlignItems = Align.Stretch;
    internal Align AlignSelf = Align.Auto;
    internal PositionType PositionType = PositionType.Relative;
    internal Wrap FlexWrap = Wrap.NoWrap;
    internal Overflow Overflow = Overflow.Visible;
    internal Display Display = Display.Flex;
    internal float FlexGrow = 0f;
    internal float FlexShrink = 1f;
    internal Value FlexBasis = CreateAutoValue();
    internal Value[] Margin = CreateDefaultEdgeValuesUnit();
    internal Value[] Position = CreateDefaultEdgeValuesUnit();
    internal Value[] Padding = CreateDefaultEdgeValuesUnit();
    internal Value[] Border = CreateDefaultEdgeValuesUnit();
    internal Value[] Dimensions = [CreateAutoValue(), CreateAutoValue()];
    internal Value[] MinDimensions = [Value.UndefinedValue, Value.UndefinedValue];
    internal Value[] MaxDimensions = [Value.UndefinedValue, Value.UndefinedValue];
    // Yoga specific properties, not compatible with flexbox specification
    internal float AspectRatio = float.NaN;

    // default values of supported attrs
    protected static readonly Dictionary<string, string> layoutAttributeDefault = new()
    {
        {"display", "flex"},
        {"overflow", "visible"},
        {"position", "relative"},
        {"align-content", "stretch"},
        {"align-items", "stretch"},
        {"align-self", "auto"},
        {"flex-direction", "row"},
        {"flex-wrap", "no-wrap"},
        {"flex-basis", "auto"},
        {"flex-shrink", "1"},
        {"flex-grow", "0"},
        {"justify-content", "flex-start"},
        {"direction", "inherit"},
        {"left", "auto"},
        {"top", "auto"},
        {"right", "auto"},
        {"bottom", "auto"},
        {"width", "auto"},
        {"height", "auto"},
        {"min-width", "auto"},
        {"min-height", "auto"},
        {"max-width", "auto"},
        {"max-height", "auto"},
        {"margin", "skip"},
        {"margin-left", "0"},
        {"margin-right", "0"},
        {"margin-top", "0"},
        {"margin-bottom", "0"},
        {"padding", "skip"},
        {"padding-left", "0"},
        {"padding-right", "0"},
        {"padding-top", "0"},
        {"padding-bottom", "0"},
        {"border-width", "skip"},
        {"border-left-width", "0"},
        {"border-right-width", "0"},
        {"border-top-width", "0"},
        {"border-bottom-width", "0"},
    };

    // default values for inherit attrs
    protected static readonly Dictionary<string, string> layoutAttributeInherit = new()
    {
        {"direction", "ltr"}
    };

    // Reports that styles have changed values that affect to layout    
    public bool LayoutDirty
    {
        get
        {
            return field || layoutAttributeChanged.Count > 0;
        }
        set
        {
            field = value;
            if (!value)
                layoutAttributeChanged.Clear();
        }
    } = false;

    // change logic for track changes when calls Set() 
    protected bool setMode = false;

    // use to store affected attrs
    protected readonly Dictionary<string, string> layoutAttribute = [];
    // use to store previous values for changed attributes (relative Apply(), Set() and this[] )
    protected readonly Dictionary<string, string> layoutAttributeChanged = [];
    // use to store attrs values before Set() was called. Thus attributeChanged represents changed values relative values before Set() was called.
    protected readonly Dictionary<string, string> layoutAttributeWas = [];
    // use to store attrs values which changed by animation(see ApplyAnimation())
    protected readonly Dictionary<string, string> layoutAttributeAnimated = [];

    private static readonly Dictionary<string, int> edgeNameToId = new()
        {
            {"left", (int) Edge.Left},
            {"top", (int) Edge.Top},
            {"right", (int) Edge.Right},
            {"bottom", (int) Edge.Bottom}
        };

    // get/set attribute by text name used string value
    public virtual string this[string attr]
    {
        get
        {
            if (layoutAttributeAnimated.TryGetValue(attr, out string? value1)) return value1;
            if (!layoutAttributeDefault.ContainsKey(attr)) throw new Exception("Try to get unknown layout style attribute [" + attr + "]");
            string? value = layoutAttribute.TryGetValue(attr, out value) ? value : layoutAttributeDefault[attr];
            if (layoutAttributeInherit.TryGetValue(attr, out string? value2) && value == "inherit")
            {
                //! Dirty-dirty hack (for good solution need to knew style of parent node, placed in TODO)
                value = value2;
            }
            return value;
        }
        set
        {
            value = value.Trim();

            // if attr is margin, padding, border-width - ignore it in change tracking and expands it to edges(top, right, bottom, and left)
            if (attr == "margin" || attr == "padding" || attr == "border-width")
            {
                var tail = attr == "border-width" ? "-width" : "";
                var name = attr == "border-width" ? "border" : attr;
                if (Flex.ParseFourValueFromString(value, out var vals))
                    foreach (var kv in edgeNameToId)
                        this[name + "-" + kv.Key + tail] = $"{vals![kv.Value].ValueUnit.ToString("F", System.Globalization.CultureInfo.InvariantCulture)}{(vals[kv.Value].Unit == Unit.Percent ? "%" : "")}";
                else
                    throw new Exception("Failed to parse attribute [" + attr + ":" + value + "]");

                return;
            }

            if (!layoutAttributeDefault.ContainsKey(attr)) throw new Exception("Try to set unknown layout style attribute [" + attr + "]");
            if (setMode)
            {
                var old_value = layoutAttributeWas.TryGetValue(attr, out string? value1) ? value1 : layoutAttributeDefault[attr];
                if (value != old_value)
                    layoutAttributeChanged[attr] = old_value;
                else
                    layoutAttributeChanged.Remove(attr);
            }
            else
            {
                layoutAttributeChanged[attr] = this[attr];
            }
            layoutAttribute[attr] = value;
            if (!Flex.ParseStyleAttr(this, attr, value))
                throw new Exception("Failed to parse attribute [" + attr + ":" + value + "]");

        }
    }

    // set new values for animated attributes 
    public virtual void ApplyAnimation(Dictionary<string, string>? animated_attrs = null)
    {
        var prev_animation = new Dictionary<string, string>(layoutAttributeAnimated);
        layoutAttributeAnimated.Clear();
        if (animated_attrs != null)
        {
            foreach (var kv in animated_attrs)
            {
                var attr = kv.Key;
                var prev_value = prev_animation.TryGetValue(attr, out string? value) ? value : this[attr];
                if (prev_value != kv.Value)
                    layoutAttributeChanged[attr] = prev_value;
                layoutAttributeAnimated[attr] = kv.Value;
            }
        }
        Sync();
    }

    // Sync text values to class properties. Used for animation.
    protected virtual void Sync()
    {
        SetDefault(false);
        foreach (var kv in layoutAttribute)
            if (!Flex.ParseStyleAttr(this, kv.Key, kv.Value))
                throw new Exception("Failed to parse attribute [" + kv.Key + ":" + kv.Value + "]");

        foreach (var kv in layoutAttributeAnimated)
            if (!Flex.ParseStyleAttr(this, kv.Key, kv.Value))
                throw new Exception("Failed to parse attribute [" + kv.Key + ":" + kv.Value + "]");
    }

    //fix problem like : changes (v0 -> v1 -> v0)
    protected virtual void CleanupLayoutAttributeChanged()
    {
        var lac = new Dictionary<string, string>(layoutAttributeChanged);
        foreach (var kv in lac)
        {
            var new_val = (layoutAttribute.TryGetValue(kv.Key, out string? value) ? value : layoutAttributeDefault[kv.Key]);
            if (kv.Value == new_val)
                layoutAttributeChanged.Remove(kv.Key);
        }
    }

    //returns dictionatry of changed attributes with previous value after Set() or Apply() was called
    //used for transition animation
    public virtual Dictionary<string, string> GetChangedAttributes() => new(layoutAttributeChanged);

    // Reset to default state and apply styles
    public virtual void Set(string style)
    {
        layoutAttributeWas.Clear();
        foreach (var kv in layoutAttributeChanged)
            layoutAttributeWas[kv.Key] = kv.Value;
        foreach (var kv in layoutAttribute)
            layoutAttributeWas[kv.Key] = kv.Value;


        setMode = true;
        SetDefault();
        Apply(style);
        foreach (var kv in layoutAttributeWas)
            if (!layoutAttribute.ContainsKey(kv.Key))
                layoutAttributeChanged[kv.Key] = layoutAttributeWas.TryGetValue(kv.Key, out string? value) ? value : layoutAttributeDefault[kv.Key];

        setMode = false;
        CleanupLayoutAttributeChanged();
    }

    // Apply styles to current state
    public virtual void Apply(string style)
    {
        if (style.Trim() != "") Parse(style);
        CleanupLayoutAttributeChanged();
    }

    // simple parser for css style text. comments must be removed. 
    public virtual void Parse(string style)
    {
        var items = style.Split(';');
        foreach (var item in items)
        {
            if (item.Trim() == "") continue;
            var part = item.Trim().Split(':');
            if (part.Length == 2)
                this[part[0].Trim()] = part[1].Trim();
            else
                throw new Exception("Failed to parse style [" + item + "] in  [" + style + "]");
        }
    }

    // Set defaults values for attributes
    // clear_text_values = false used for animation(see Sync())
    public virtual void SetDefault(bool clear_text_values = true)
    {
        if (clear_text_values)
        {
            layoutAttributeChanged.Clear();
            layoutAttribute.Clear();
        }
        Direction = Direction.Inherit;
        FlexDirection = FlexDirection.Row;
        JustifyContent = Justify.FlexStart;
        AlignContent = Align.Stretch;
        AlignItems = Align.Stretch;
        AlignSelf = Align.Auto;
        PositionType = PositionType.Relative;
        FlexWrap = Wrap.NoWrap;
        Overflow = Overflow.Visible;
        Display = Display.Flex;
        FlexGrow = 0f;
        FlexShrink = 1f;
        FlexBasis = CreateAutoValue();
        Margin = CreateDefaultEdgeValuesUnit();
        Position = CreateDefaultEdgeValuesUnit();
        Padding = CreateDefaultEdgeValuesUnit();
        Border = CreateDefaultEdgeValuesUnit();
        Dimensions = [CreateAutoValue(), CreateAutoValue()];
        MinDimensions = [Value.UndefinedValue, Value.UndefinedValue];
        MaxDimensions = [Value.UndefinedValue, Value.UndefinedValue];
        // Yoga specific properties, not compatible with flexbox specification
        AspectRatio = float.NaN;
    }


    public static void Copy(Style dest, Style src)
    {
        dest.Direction = src.Direction;
        dest.FlexDirection = src.FlexDirection;
        dest.JustifyContent = src.JustifyContent;
        dest.AlignContent = src.AlignContent;
        dest.AlignItems = src.AlignItems;
        dest.AlignSelf = src.AlignSelf;
        dest.PositionType = src.PositionType;
        dest.FlexWrap = src.FlexWrap;
        dest.Overflow = src.Overflow;
        dest.Display = src.Display;
        dest.FlexGrow = src.FlexGrow;
        dest.FlexShrink = src.FlexShrink;
        dest.FlexBasis = src.FlexBasis;

        Value.CopyValue(dest.Margin, src.Margin);
        Value.CopyValue(dest.Position, src.Position);
        Value.CopyValue(dest.Padding, src.Padding);
        Value.CopyValue(dest.Border, src.Border);

        Value.CopyValue(dest.Dimensions, src.Dimensions);
        Value.CopyValue(dest.MinDimensions, src.MinDimensions);
        Value.CopyValue(dest.MaxDimensions, src.MaxDimensions);

        dest.AspectRatio = src.AspectRatio;
    }
    internal static Value CreateAutoValue() => new(float.NaN, Unit.Auto);

    internal static Value[] CreateDefaultEdgeValuesUnit() => [
            Value.UndefinedValue,
            Value.UndefinedValue,
            Value.UndefinedValue,
            Value.UndefinedValue,
            Value.UndefinedValue,
            Value.UndefinedValue,
            Value.UndefinedValue,
            Value.UndefinedValue,
            Value.UndefinedValue,
        ];

    public virtual Style Clone()
    {
        var clone = new Style();
        Copy(this, clone);
        return clone;
    }
}
