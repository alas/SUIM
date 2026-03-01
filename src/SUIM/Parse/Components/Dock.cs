namespace SUIM.Parse.Components;

using SUIM.Flexbox;

public class Dock() : UIElement(nameof(Dock))
{
    public string? LastChildFill { get; set; } = "true";
    public List<DockChild> DockChildren { get; } = [];

    public Node Root { get; } = new Node();

    public override void SetAttribute(string name, object? value)
    {
        if (name.Equals("lastchildfill", StringComparison.OrdinalIgnoreCase))
        {
            LastChildFill = value as string;
        }
        else
        {
            base.SetAttribute(name, value);
        }
    }

    public override void ApplyLayout(float parentWidth, float parentHeight, Direction parentDirection)
    {
        var width = Flex.ParseValueFromString(Width ?? "0", out var v) ? v : new Value(0, Unit.Point);
        var height = Flex.ParseValueFromString(Height ?? "0", out var h) ? h : new Value(0, Unit.Point);
        Root.Helper_SetDimensions(width, Dimension.Width);
        Root.Helper_SetDimensions(height, Dimension.Height);
        Root.StyleSetFlexDirection(FlexDirection.Row);

        foreach (var child in Children)
        {
            child.ApplyLayout(parentWidth, parentHeight, parentDirection);
        }
    }

    public void AddDockChild(DockChild item)
    {
        DockChildren.Add(item);
    }

    public void CalculateLayout(float parentWidth, float parentHeight, Direction parentDirection)
    {
        Root.Children.Clear();
        var current = Root;

        for (int i = 0; i < DockChildren.Count; i++)
        {
            var item = DockChildren[i];
            bool isLast = i == DockChildren.Count - 1;

            if (item.Edge == DockEdge.Fill || (isLast && "true".Equals(LastChildFill, StringComparison.OrdinalIgnoreCase)))
            {
                item.Node.StyleSetFlexGrow(1);
                current.AddChild(item.Node);
                break;
            }

            switch (item.Edge)
            {
                case DockEdge.Left:
                case DockEdge.Right:
                    current.StyleSetFlexDirection(FlexDirection.Row);

                    var mainCol = CreateFillColumn();
                    ApplyDockSize(item, isWidth: true);

                    if (item.Edge == DockEdge.Left)
                    {
                        current.AddChild(item.Node);
                        current.AddChild(mainCol);
                    }
                    else
                    {
                        current.AddChild(mainCol);
                        current.AddChild(item.Node);
                    }

                    current = mainCol;
                    break;

                case DockEdge.Top:
                case DockEdge.Bottom:
                    current.StyleSetFlexDirection(FlexDirection.Column);

                    var mainRow = CreateFillRow();
                    ApplyDockSize(item, isWidth: false);

                    if (item.Edge == DockEdge.Top)
                    {
                        current.AddChild(item.Node);
                        current.AddChild(mainRow);
                    }
                    else
                    {
                        current.AddChild(mainRow);
                        current.AddChild(item.Node);
                    }

                    current = mainRow;
                    break;
            }
        }

        Root.CalculateLayout(parentWidth, parentHeight, parentDirection);
    }

    //public void CalculateLayout2(float parentWidth, float parentHeight, Direction parentDirection)
    //{
        //var width = Flex.resolveValue(Root.StyleGetWidth(), parentWidth);
        //var minWidth = Flex.resolveValue(Root.StyleGetMinWidth(), parentWidth);
        //var maxWidth = Flex.resolveValue(Root.StyleGetMaxWidth(), parentWidth);
        //var height = Flex.resolveValue(Root.StyleGetHeight(), parentHeight);
        //var minHeight = Flex.resolveValue(Root.StyleGetMinHeight(), parentHeight);
        //var maxHeight = Flex.resolveValue(Root.StyleGetMaxHeight(), parentHeight);
        //var marginTop = Flex.resolveValue(Root.StyleGetMargin(Edge.Top), parentHeight);
        //var marginBottom = Root.StyleGetMargin(Edge.Bottom);
        //var marginLeft = Root.StyleGetMargin(Edge.Left);
        //var marginRight = Root.StyleGetMargin(Edge.Right);
        //var paddingTop = Root.StyleGetPadding(Edge.Top);
        //var paddingBottom = Root.StyleGetPadding(Edge.Bottom);
        //var paddingLeft = Root.StyleGetPadding(Edge.Left);
        //var paddingRight = Root.StyleGetPadding(Edge.Right);
        //var visible = Root.StyleGetVisibility();
        //var disabled = Root.;

        //// Set min/max height and width
        //el.style.minHeight = minHeight || '';
        //el.style.maxHeight = maxHeight || '';
        //el.style.minWidth = minWidth || '';
        //el.style.maxWidth = maxWidth || '';
        //el.style.padding = this.thicknessToCss(padding) || '';

        //var alignV = Root.StyleGetAlignSelf?();
        //
        //switch (alignV)
        //{
        //    case Align.FlexStart:
        //        Root.StyleSetMargin(Edge.Top, marginTop);
        //        el.style.marginBottom = 'auto';
        //        el.style.height = height;
        //        break;
        //    case 'bottom':
        //        el.style.marginTop = 'auto';
        //        el.style.marginBottom = margin.bottom || '0';
        //        el.style.height = height;
        //        break;
        //    case 'center':
        //        el.style.marginTop = 'auto';
        //        el.style.marginBottom = 'auto';
        //        el.style.height = height;
        //        break;
        //    case 'stretch':
        //        el.style.marginTop = margin.top || '0';
        //        el.style.marginBottom = margin.bottom || '0';
        //
        //        if (!height)
        //        {
        //            const top = margin.top || '0px';
        //            const bottom = margin.bottom || '0px';
        //            el.style.height = `calc(100 % -(${ top}
        //            + ${ bottom}))`;
        //        }
        //        else
        //        {
        //            el.style.height = height;
        //        }
        //        break;
        //}
        //
        //var alignH = this.alignH || 'stretch';
        //switch (alignH)
        //{
        //    case 'left':
        //        el.style.marginLeft = margin.left || '0';
        //        el.style.marginRight = 'auto';
        //        el.style.width = width;
        //        break;
        //    case 'right':
        //        el.style.marginLeft = 'auto';
        //        el.style.marginRight = margin.right || '0';
        //        el.style.width = width;
        //        break;
        //    case 'center':
        //        el.style.marginLeft = 'auto';
        //        el.style.marginRight = 'auto';
        //        el.style.width = width;
        //        break;
        //    case 'stretch':
        //        el.style.marginLeft = margin.left || '0';
        //        el.style.marginRight = margin.right || '0';
        //
        //        if (!width)
        //        {
        //            const left = margin.left || '0px';
        //            const right = margin.right || '0px';
        //            el.style.width = `calc(100 % -(${ left}
        //            + ${ right}))`;
        //
        //        }
        //        else
        //        {
        //            el.style.width = width;
        //        }
        //        break;
        //}
        //
        //var overflowX = this.overflowX || 'hidden';
        //var overflowY = this.overflowY || 'hidden';
        //el.style.overflowX = overflowX;
        //el.style.overflowY = overflowY;
        //
        //// Apply visibility (only if explicitly set)
        //if (visible != undefined)
        //{
        //    el.style.display = visible == 'false' || visible == false ? 'none' : '';
        //}
        //
        //var opacity = this.opacity !== undefined ? this.opacity / 100 : 1;
        //el.style.opacity = opacity;
        //
        //// Apply enabled/disabled state (only if explicitly set)
        //if (enabled != undefined)
        //{
        //    if (enabled == false)
        //    {
        //        el.style.pointerEvents = 'none';
        //        el.style.opacity = '0.6';
        //    }
        //    else
        //    {
        //        el.style.pointerEvents = '';
        //        el.style.opacity = '';
        //    }
        //}
        //
        //Root.CalculateLayout(parentWidth, parentHeight, parentDirection);
    //}

    private static Node CreateFillColumn()
    {
        var node = new Node();
        node.StyleSetFlexGrow(1);
        node.StyleSetFlexDirection(FlexDirection.Column);
        return node;
    }

    private static Node CreateFillRow()
    {
        var node = new Node();
        node.StyleSetFlexGrow(1);
        node.StyleSetFlexDirection(FlexDirection.Row);
        return node;
    }

    private static void ApplyDockSize(DockChild item, bool isWidth)
    {
        if (item.Size.HasValue)
        {
            if (isWidth)
                item.Node.StyleSetWidthPercent(item.Size.Value * 100f);
            else
                item.Node.StyleSetHeightPercent(item.Size.Value * 100f);
        }
    }
}

public class DockChild
{
    public DockEdge Edge { get; init; }
    public float? Size { get; init; } // % (0..1) or absolute px
    public required Node Node { get; init; }
}

public enum DockEdge
{
    Left,
    Right,
    Top,
    Bottom,
    Fill
}
