namespace SUIM.Flexbox;

public partial class Node
{
    internal void Helper_SetDimensions(Value value, Dimension dimension)
    {
        if (dimension == Dimension.Width)
        {
            if (value.Unit == Unit.Auto)
                StyleSetWidthAuto();
            else if (value.Unit == Unit.Percent)
                StyleSetWidthPercent(value.ValueUnit);
            else if (value.Unit == Unit.Point)
                StyleSetWidth(value.ValueUnit);
        }
        else
        {
            if (value.Unit == Unit.Auto)
                StyleSetHeightAuto();
            else if (value.Unit == Unit.Percent)
                StyleSetHeightPercent(value.ValueUnit);
            else if (value.Unit == Unit.Point)
                StyleSetHeight(value.ValueUnit);
        }
    }

    internal void Helper_SetMinDimensions(Value value, Dimension dimension)
    {
        if (dimension == Dimension.Width)
        {
            if (value.Unit == Unit.Percent)
                StyleSetMinWidthPercent(value.ValueUnit);
            else if (value.Unit == Unit.Point)
                StyleSetMinWidth(value.ValueUnit);
            else StyleSetMinWidth(float.NaN);
        }
        else
        {
            if (value.Unit == Unit.Percent)
                StyleSetMinHeightPercent(value.ValueUnit);
            else if (value.Unit == Unit.Point)
                StyleSetMinHeight(value.ValueUnit);
            else StyleSetMinHeight(float.NaN);
        }
    }

    internal void Helper_SetMaxDimensions(Value value, Dimension dimension)
    {
        if (dimension == Dimension.Width)
        {
            if (value.Unit == Unit.Percent)
                StyleSetMaxWidthPercent(value.ValueUnit);
            else if (value.Unit == Unit.Point)
                StyleSetMaxWidth(value.ValueUnit);
            else StyleSetMaxWidth(float.NaN);
        }
        else
        {
            if (value.Unit == Unit.Percent)
                StyleSetMaxHeightPercent(value.ValueUnit);
            else if (value.Unit == Unit.Point)
                StyleSetMaxHeight(value.ValueUnit);
            else StyleSetMaxHeight(float.NaN);
        }
    }

    internal void Helper_SetMarginPaddingBorder(string tag, Edge edge, Value value)
    {
        if (tag == "margin")
        {
            if (value.Unit == Unit.Auto)
                StyleSetMarginAuto(edge);
            else if (value.Unit == Unit.Percent)
                StyleSetMarginPercent(edge, value.ValueUnit);
            else if (value.Unit == Unit.Point)
                StyleSetMargin(edge, value.ValueUnit);
            else // if (value.unit == Unit.Undefined)
                StyleSetMargin(edge, float.NaN);
        }
        else if (tag == "padding")
        {
            if (value.Unit == Unit.Percent)
                StyleSetPaddingPercent(edge, value.ValueUnit);
            else if (value.Unit == Unit.Point)
                StyleSetPadding(edge, value.ValueUnit);
            else StyleSetPadding(edge, float.NaN);
        }
        else if (tag == "border")
        {
            if (value.Unit == Unit.Point)
                StyleSetBorder(edge, value.ValueUnit);
            else StyleSetBorder(edge, float.NaN);
        }
    }
    // StyleSetWidth sets width
    public void StyleSetWidth(float width)
    {
        var dim = this.nodeStyle.Dimensions[(int)Dimension.Width];
        if (dim.ValueUnit != width || dim.Unit != Unit.Point)
        {
            var unit = Unit.Point;
            if (Flex.FloatIsUndefined(width))
            {
                unit = Unit.Auto;
            }
            this.nodeStyle.Dimensions[(int)Dimension.Width] = new(width, unit);
            Flex.NodeMarkDirtyInternal(this);
        }
    }

    // StyleSetWidthPercent sets width percent
    public void StyleSetWidthPercent(float width)
    {
        var dim = this.nodeStyle.Dimensions[(int)Dimension.Width];
        if (dim.ValueUnit != width || dim.Unit != Unit.Percent)
        {
            var unit = Unit.Percent;
            if (Flex.FloatIsUndefined(width))
            {
                unit = Unit.Auto;
            }
            this.nodeStyle.Dimensions[(int)Dimension.Width] = new(width, unit);
            Flex.NodeMarkDirtyInternal(this);
        }
    }

    // StyleSetWidthAuto sets width auto
    public void StyleSetWidthAuto()
    {
        var dim = this.nodeStyle.Dimensions[(int)Dimension.Width];
        if (dim.Unit != Unit.Auto)
        {
            this.nodeStyle.Dimensions[(int)Dimension.Width] = new(float.NaN, Unit.Auto);
            Flex.NodeMarkDirtyInternal(this);
        }
    }

    // StyleGetWidth gets width
    public Value StyleGetWidth() => nodeStyle.Dimensions[(int)Dimension.Width];

    // StyleSetHeight sets height
    public void StyleSetHeight(float height)
    {
        var dim = this.nodeStyle.Dimensions[(int)Dimension.Height];
        if (dim.ValueUnit != height || dim.Unit != Unit.Point)
        {
            var unit = Unit.Point;
            if (Flex.FloatIsUndefined(height))
            {
                unit = Unit.Auto;
            }
            this.nodeStyle.Dimensions[(int)Dimension.Height] = new(height, unit);
            Flex.NodeMarkDirtyInternal(this);
        }
    }

    // StyleSetHeightPercent sets height percent
    public void StyleSetHeightPercent(float height)
    {
        var dim = this.nodeStyle.Dimensions[(int)Dimension.Height];
        if (dim.ValueUnit != height || dim.Unit != Unit.Percent)
        {
            var unit = Unit.Percent;
            if (Flex.FloatIsUndefined(height))
            {
                unit = Unit.Auto;
            }
            this.nodeStyle.Dimensions[(int)Dimension.Height] = new(height, unit);
            Flex.NodeMarkDirtyInternal(this);
        }
    }

    // StyleSetHeightAuto sets height auto
    public void StyleSetHeightAuto()
    {
        var dim = this.nodeStyle.Dimensions[(int)Dimension.Height];
        if (dim.Unit != Unit.Auto)
        {
            this.nodeStyle.Dimensions[(int)Dimension.Height] = new(float.NaN, Unit.Auto);
            Flex.NodeMarkDirtyInternal(this);
        }
    }

    // StyleGetHeight gets height
    public Value StyleGetHeight() => this.nodeStyle.Dimensions[(int)Dimension.Height];

    // StyleSetPositionType sets position type
    public void StyleSetPositionType(PositionType positionType)
    {
        if (this.nodeStyle.PositionType != positionType)
        {
            this.nodeStyle.PositionType = positionType;
            Flex.NodeMarkDirtyInternal(this);
        }
    }

    public PositionType StyleGetPositionType() => this.nodeStyle.PositionType;

    // StyleSetPosition sets position
    public void StyleSetPosition(Edge edge, float position)
    {
        var pos = this.nodeStyle.Position[(int)edge];
        if (pos.ValueUnit != position || pos.Unit != Unit.Point)
        {
            var unit = Unit.Point;
            if (Flex.FloatIsUndefined(position))
            {
                unit = Unit.Undefined;
            }
            this.nodeStyle.Position[(int)edge] = new(position, unit);
            Flex.NodeMarkDirtyInternal(this);
        }
    }

    // StyleSetPositionPercent sets position percent
    public void StyleSetPositionPercent(Edge edge, float position)
    {
        var pos = this.nodeStyle.Position[(int)edge];
        if (pos.ValueUnit != position || pos.Unit != Unit.Percent)
        {
            var unit = Unit.Percent;
            if (Flex.FloatIsUndefined(position))
            {
                unit = Unit.Undefined;
            }
            this.nodeStyle.Position[(int)edge] = new(position, unit);
            Flex.NodeMarkDirtyInternal(this);
        }
    }

    // StyleGetPosition gets position
    public Value StyleGetPosition(Edge edge) => this.nodeStyle.Position[(int)edge];

    // StyleSetDirection sets direction
    public void StyleSetDirection(Direction direction)
    {
        if (this.nodeStyle.Direction != direction)
        {
            this.nodeStyle.Direction = direction;
            Flex.NodeMarkDirtyInternal(this);
        }
    }

    public Direction StyleGetDirection() => this.nodeStyle.Direction;

    // StyleSetFlexDirection sets flex directions
    public void StyleSetFlexDirection(FlexDirection flexDirection)
    {
        if (this.nodeStyle.FlexDirection != flexDirection)
        {
            this.nodeStyle.FlexDirection = flexDirection;
            Flex.NodeMarkDirtyInternal(this);
        }
    }

    public FlexDirection StyleGetFlexDirection() => this.nodeStyle.FlexDirection;

    // StyleSetJustifyContent sets justify content
    public void StyleSetJustifyContent(Justify justifyContent)
    {
        if (this.nodeStyle.JustifyContent != justifyContent)
        {
            this.nodeStyle.JustifyContent = justifyContent;
            Flex.NodeMarkDirtyInternal(this);
        }
    }

    public Justify StyleGetJustifyContent() => this.nodeStyle.JustifyContent;

    // StyleSetAlignContent sets align content
    public void StyleSetAlignContent(Align alignContent)
    {
        if (this.nodeStyle.AlignContent != alignContent)
        {
            this.nodeStyle.AlignContent = alignContent;
            Flex.NodeMarkDirtyInternal(this);
        }
    }

    public Align StyleGetAlignContent() => this.nodeStyle.AlignContent;

    // StyleSetAlignItems sets align content
    public void StyleSetAlignItems(Align alignItems)
    {
        if (this.nodeStyle.AlignItems != alignItems)
        {
            this.nodeStyle.AlignItems = alignItems;
            Flex.NodeMarkDirtyInternal(this);
        }
    }

    public Align StyleGetAlignItems() => this.nodeStyle.AlignItems;

    // StyleSetAlignSelf sets align self
    public void StyleSetAlignSelf(Align alignSelf)
    {
        if (this.nodeStyle.AlignSelf != alignSelf)
        {
            this.nodeStyle.AlignSelf = alignSelf;
            Flex.NodeMarkDirtyInternal(this);
        }
    }

    public Align StyleGetAlignSelf() => this.nodeStyle.AlignSelf;

    // StyleSetFlexWrap sets flex wrap
    public void StyleSetFlexWrap(Wrap flexWrap)
    {
        if (this.nodeStyle.FlexWrap != flexWrap)
        {
            this.nodeStyle.FlexWrap = flexWrap;
            Flex.NodeMarkDirtyInternal(this);
        }
    }

    public Wrap StyleGetFlexWrap() => this.nodeStyle.FlexWrap;

    // StyleSetOverflow sets overflow
    public void StyleSetOverflow(Overflow overflow)
    {
        if (this.nodeStyle.Overflow != overflow)
        {
            this.nodeStyle.Overflow = overflow;
            Flex.NodeMarkDirtyInternal(this);
        }
    }

    public Overflow StyleGetOverflow() => this.nodeStyle.Overflow;

    // StyleSetDisplay sets display
    public void StyleSetDisplay(Display display)
    {
        if (this.nodeStyle.Display != display)
        {
            this.nodeStyle.Display = display;
            Flex.NodeMarkDirtyInternal(this);
        }
    }

    public Display StyleGetDisplay() => this.nodeStyle.Display;


    // StyleSetFlexGrow sets flex grow
    public void StyleSetFlexGrow(float flexGrow)
    {
        if (this.nodeStyle.FlexGrow != flexGrow)
        {
            this.nodeStyle.FlexGrow = flexGrow;
            Flex.NodeMarkDirtyInternal(this);
        }
    }

    // StyleGetFlexGrow gets flex grow
    public float StyleGetFlexGrow()
    {
        if (float.IsNaN(this.nodeStyle.FlexGrow))
        {
            return Constant.defaultFlexGrow;
        }
        return this.nodeStyle.FlexGrow;
    }

    // StyleGetFlexShrink gets flex shrink
    public float StyleGetFlexShrink()
    {
        if (float.IsNaN(this.nodeStyle.FlexShrink))
        {
            if (this.config.UseWebDefaults)
            {
                return Constant.webDefaultFlexShrink;
            }
            return Constant.defaultFlexShrink;
        }
        return this.nodeStyle.FlexShrink;
    }

    // StyleSetFlexShrink sets flex shrink
    public void StyleSetFlexShrink(float flexShrink)
    {
        if (this.nodeStyle.FlexShrink != flexShrink)
        {
            this.nodeStyle.FlexShrink = flexShrink;
            Flex.NodeMarkDirtyInternal(this);
        }
    }

    // StyleSetFlexBasis sets flex basis
    public void StyleSetFlexBasis(float flexBasis)
    {
        if (this.nodeStyle.FlexBasis.ValueUnit != flexBasis ||
            this.nodeStyle.FlexBasis.Unit != Unit.Point)
        {
            var unit = Unit.Point;
            if (Flex.FloatIsUndefined(flexBasis))
            {
                unit = Unit.Auto;
            }
            this.nodeStyle.FlexBasis = new(flexBasis, unit);
            Flex.NodeMarkDirtyInternal(this);
        }
    }

    // StyleSetFlexBasisPercent sets flex basis percent
    public void StyleSetFlexBasisPercent(float flexBasis)
    {
        if (this.nodeStyle.FlexBasis.ValueUnit != flexBasis ||
            this.nodeStyle.FlexBasis.Unit != Unit.Percent)
        {
            var unit = Unit.Percent;
            if (Flex.FloatIsUndefined(flexBasis))
            {
                unit = Unit.Auto;
            }
            this.nodeStyle.FlexBasis = new(flexBasis, unit);
            Flex.NodeMarkDirtyInternal(this);
        }
    }

    // NodeStyleSetFlexBasisAuto sets flex basis auto
    public void NodeStyleSetFlexBasisAuto()
    {
        if (this.nodeStyle.FlexBasis.Unit != Unit.Auto)
        {
            this.nodeStyle.FlexBasis = new(float.NaN, Unit.Auto);
            Flex.NodeMarkDirtyInternal(this);
        }
    }

    public Value NodeStyleGetFlexBasis() => this.nodeStyle.FlexBasis;

    // StyleSetMargin sets margin
    public void StyleSetMargin(Edge edge, float margin)
    {
        if (this.nodeStyle.Margin[(int)edge].ValueUnit != margin ||
            this.nodeStyle.Margin[(int)edge].Unit != Unit.Point)
        {
            var unit = Unit.Point;
            if (Flex.FloatIsUndefined(margin))
            {
                unit = Unit.Undefined;
            }
            this.nodeStyle.Margin[(int)edge] = new(margin, unit);
            Flex.NodeMarkDirtyInternal(this);
        }
    }

    // StyleSetMarginPercent sets margin percent
    public void StyleSetMarginPercent(Edge edge, float margin)
    {
        if (this.nodeStyle.Margin[(int)edge].ValueUnit != margin ||
            this.nodeStyle.Margin[(int)edge].Unit != Unit.Percent)
        {
            var unit = Unit.Percent;
            if (Flex.FloatIsUndefined(margin))
            {
                unit = Unit.Undefined;
            }
            this.nodeStyle.Margin[(int)edge] = new(margin, unit);
            Flex.NodeMarkDirtyInternal(this);
        }
    }

    // StyleGetMargin gets margin
    public Value StyleGetMargin(Edge edge) => this.nodeStyle.Margin[(int)edge];

    // StyleSetMarginAuto sets margin auto
    public void StyleSetMarginAuto(Edge edge)
    {
        if (this.nodeStyle.Margin[(int)edge].Unit != Unit.Auto)
        {
            this.nodeStyle.Margin[(int)edge] = new(float.NaN, Unit.Auto);
            Flex.NodeMarkDirtyInternal(this);
        }
    }

    // StyleSetPadding sets padding
    public void StyleSetPadding(Edge edge, float padding)
    {
        if (this.nodeStyle.Padding[(int)edge].ValueUnit != padding ||
            this.nodeStyle.Padding[(int)edge].Unit != Unit.Point)
        {
            var unit = Unit.Point;
            if (Flex.FloatIsUndefined(padding))
            {
                unit = Unit.Undefined;
            }
            this.nodeStyle.Padding[(int)edge] = new(padding, unit);
            Flex.NodeMarkDirtyInternal(this);
        }
    }

    // StyleSetPaddingPercent sets padding percent
    public void StyleSetPaddingPercent(Edge edge, float padding)
    {
        if (this.nodeStyle.Padding[(int)edge].ValueUnit != padding ||
            this.nodeStyle.Padding[(int)edge].Unit != Unit.Percent)
        {
            var unit = Unit.Percent;
            if (Flex.FloatIsUndefined(padding))
            {
                unit = Unit.Undefined;
            }
            this.nodeStyle.Padding[(int)edge] = new(padding, unit);
            Flex.NodeMarkDirtyInternal(this);
        }
    }

    // StyleGetPadding gets padding
    public Value StyleGetPadding(Edge edge) => this.nodeStyle.Padding[(int)edge];

    // StyleSetBorder sets border
    public void StyleSetBorder(Edge edge, float border)
    {
        if (this.nodeStyle.Border[(int)edge].ValueUnit != border ||
            this.nodeStyle.Border[(int)edge].Unit != Unit.Point)
        {
            var unit = Unit.Point;
            if (Flex.FloatIsUndefined(border))
            {
                unit = Unit.Undefined;
            }
            this.nodeStyle.Border[(int)edge] = new(border, unit);
            Flex.NodeMarkDirtyInternal(this);
        }
    }

    // StyleGetBorder gets border
    public float StyleGetBorder(Edge edge) => this.nodeStyle.Border[(int)edge].ValueUnit;

    // StyleSetMinWidth sets min width
    public void StyleSetMinWidth(float minWidth)
    {
        if (this.nodeStyle.MinDimensions[(int)Dimension.Width].ValueUnit != minWidth ||
            this.nodeStyle.MinDimensions[(int)Dimension.Width].Unit != Unit.Point)
        {
            var unit = Unit.Point;
            if (Flex.FloatIsUndefined(minWidth))
            {
                unit = Unit.Auto;
            }
            this.nodeStyle.MinDimensions[(int)Dimension.Width] = new(minWidth, unit);
            Flex.NodeMarkDirtyInternal(this);
        }
    }

    // StyleSetMinWidthPercent sets width percent
    public void StyleSetMinWidthPercent(float minWidth)
    {
        if (this.nodeStyle.MinDimensions[(int)Dimension.Width].ValueUnit != minWidth ||
            this.nodeStyle.MinDimensions[(int)Dimension.Width].Unit != Unit.Percent)
        {
            var unit = Unit.Percent;
            if (Flex.FloatIsUndefined(minWidth))
            {
                unit = Unit.Auto;
            }
            this.nodeStyle.MinDimensions[(int)Dimension.Width] = new(minWidth, unit);
            Flex.NodeMarkDirtyInternal(this);
        }
    }

    // StyleGetMinWidth gets min width
    public Value StyleGetMinWidth() => this.nodeStyle.MinDimensions[(int)Dimension.Width];

    // StyleSetMinHeight sets min width
    public void StyleSetMinHeight(float minHeight)
    {
        if (this.nodeStyle.MinDimensions[(int)Dimension.Height].ValueUnit != minHeight ||
            this.nodeStyle.MinDimensions[(int)Dimension.Height].Unit != Unit.Point)
        {
            var unit = Unit.Point;
            if (Flex.FloatIsUndefined(minHeight))
            {
                unit = Unit.Auto;
            }
            this.nodeStyle.MinDimensions[(int)Dimension.Height] = new(minHeight, unit);
            Flex.NodeMarkDirtyInternal(this);
        }
    }

    // StyleSetMinHeightPercent sets min height percent
    public void StyleSetMinHeightPercent(float minHeight)
    {
        if (this.nodeStyle.MinDimensions[(int)Dimension.Height].ValueUnit != minHeight ||
            this.nodeStyle.MinDimensions[(int)Dimension.Height].Unit != Unit.Percent)
        {
            var unit = Unit.Percent;
            if (Flex.FloatIsUndefined(minHeight))
            {
                unit = Unit.Auto;
            }
            this.nodeStyle.MinDimensions[(int)Dimension.Height] = new(minHeight, unit);
            Flex.NodeMarkDirtyInternal(this);
        }
    }

    // StyleGetMinHeight gets min height
    public Value StyleGetMinHeight() => this.nodeStyle.MinDimensions[(int)Dimension.Height];

    // StyleSetMaxWidth sets max width
    public void StyleSetMaxWidth(float maxWidth)
    {
        if (this.nodeStyle.MaxDimensions[(int)Dimension.Width].ValueUnit != maxWidth ||
            this.nodeStyle.MaxDimensions[(int)Dimension.Width].Unit != Unit.Point)
        {
            var unit = Unit.Point;
            if (Flex.FloatIsUndefined(maxWidth))
            {
                unit = Unit.Auto;
            }
            this.nodeStyle.MaxDimensions[(int)Dimension.Width] = new(maxWidth, unit);
            Flex.NodeMarkDirtyInternal(this);
        }
    }

    // StyleSetMaxWidthPercent sets max width percent
    public void StyleSetMaxWidthPercent(float maxWidth)
    {
        if (this.nodeStyle.MaxDimensions[(int)Dimension.Width].ValueUnit != maxWidth ||
            this.nodeStyle.MaxDimensions[(int)Dimension.Width].Unit != Unit.Percent)
        {
            var unit = Unit.Percent;
            if (Flex.FloatIsUndefined(maxWidth))
            {
                unit = Unit.Auto;
            }
            this.nodeStyle.MaxDimensions[(int)Dimension.Width] = new(maxWidth, unit);
            Flex.NodeMarkDirtyInternal(this);
        }
    }

    // StyleGetMaxWidth gets max width
    public Value StyleGetMaxWidth() => this.nodeStyle.MaxDimensions[(int)Dimension.Width];

    // StyleSetMaxHeight sets max width
    public void StyleSetMaxHeight(float maxHeight)
    {
        if (this.nodeStyle.MaxDimensions[(int)Dimension.Height].ValueUnit != maxHeight ||
            this.nodeStyle.MaxDimensions[(int)Dimension.Height].Unit != Unit.Point)
        {
            var unit = Unit.Point;
            if (Flex.FloatIsUndefined(maxHeight))
            {
                unit = Unit.Auto;
            }
            this.nodeStyle.MaxDimensions[(int)Dimension.Height] = new(maxHeight, unit);
            Flex.NodeMarkDirtyInternal(this);
        }
    }

    // StyleSetMaxHeightPercent sets max height percent
    public void StyleSetMaxHeightPercent(float maxHeight)
    {
        if (this.nodeStyle.MaxDimensions[(int)Dimension.Height].ValueUnit != maxHeight ||
            this.nodeStyle.MaxDimensions[(int)Dimension.Height].Unit != Unit.Percent)
        {
            var unit = Unit.Percent;
            if (Flex.FloatIsUndefined(maxHeight))
            {
                unit = Unit.Auto;
            }
            this.nodeStyle.MaxDimensions[(int)Dimension.Height] = new(maxHeight, unit);
            Flex.NodeMarkDirtyInternal(this);
        }
    }

    // StyleGetMaxHeight gets max height
    public Value StyleGetMaxHeight() => this.nodeStyle.MaxDimensions[(int)Dimension.Height];

    // StyleSetAspectRatio sets axpect ratio
    public void StyleSetAspectRatio(float aspectRatio)
    {
        if (this.nodeStyle.AspectRatio != aspectRatio)
        {
            this.nodeStyle.AspectRatio = aspectRatio;
            Flex.NodeMarkDirtyInternal(this);
        }
    }
}
