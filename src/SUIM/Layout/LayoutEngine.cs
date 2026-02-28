namespace SUIM.Layout;

using SUIM.Parse.Components;
using SUIM.Parse.Components.Attributes;

public static class LayoutEngine
{
    public static void Layout(UIElement root, float rootFontSize, float availableWidth, float availableHeight)
    {
        ResetPositions(root);
        root.ActualX = 0;
        root.ActualY = 0;
        root.CurrentFontSize = root.RootFontSize = rootFontSize;
        MeasureElement(root, availableWidth, availableHeight);
        PositionElement(root, 0, 0);
        DetectOverflow(root);
    }

    private static void ResetPositions(UIElement element)
    {
        element.ActualX = float.NaN;
        element.ActualY = float.NaN;
        foreach (var child in element.Children)
            ResetPositions(child);
    }

    private static void MeasureElement(UIElement element, float availableWidth, float availableHeight)
    {
        // Propagate root and current font sizes from parent when present
        if (element.Parent != null)
        {
            element.RootFontSize = element.Parent.RootFontSize;
            if (element.CurrentFontSize == 0)
                element.CurrentFontSize = element.Parent.CurrentFontSize;
        }

        // Calculate margins and padding in pixels
        var margin = Thickness.Parse(element.Margin);
        element.ComputedMarginLeft = element.ToPixels(margin.Left);
        element.ComputedMarginTop = element.ToPixels(margin.Top);
        element.ComputedMarginRight = element.ToPixels(margin.Right);
        element.ComputedMarginBottom = element.ToPixels(margin.Bottom);

        var padding = Thickness.Parse(element.Padding);
        element.ComputedPaddingLeft = element.ToPixels(padding.Left);
        element.ComputedPaddingTop = element.ToPixels(padding.Top);
        element.ComputedPaddingRight = element.ToPixels(padding.Right);
        element.ComputedPaddingBottom = element.ToPixels(padding.Bottom);

        // Calculate available space for content
        var availableContentWidth = Math.Max(0, availableWidth - element.ComputedMarginLeft - element.ComputedMarginRight - element.ComputedPaddingLeft - element.ComputedPaddingRight);
        var availableContentHeight = Math.Max(0, availableHeight - element.ComputedMarginTop - element.ComputedMarginBottom - element.ComputedPaddingTop - element.ComputedPaddingBottom);

        // For most containers and Text elements, default to Auto if unspecified
        // For LayoutElement derivatives (like Stack, Grid, Div), default to 1fr if unspecified
        var width = UnitValue.Parse(element.Width);
        var height = UnitValue.Parse(element.Height);
        if (element is LayoutElement)
        {
            if (width.Type == UnitType.None) 
            {
                element.Width = "fr";
                width = UnitValue.OneFR;
            }

            if (height.Type == UnitType.None)
            {
                element.Height = "fr";
                height = UnitValue.OneFR;
            }
        }
        else
        {
            if (width.Type == UnitType.None)
            {
                element.Width = "auto";
                width = UnitValue.Auto;
            }

            if (height.Type == UnitType.None)
            {
                element.Height = "auto";
                height = UnitValue.Auto;
            }
        }

        // Convert explicit sizes to pixels
        var widthInPixels = (width.Type == UnitType.Auto || width.Type == UnitType.None || width.Type == UnitType.Fr) ? 0 : element.ToPixels(element.Width);
        var heightInPixels = (height.Type == UnitType.Auto || height.Type == UnitType.None || height.Type == UnitType.Fr) ? 0 : element.ToPixels(element.Height);

        // If we have a fixed available space from parent (e.g. resolved 1fr), use it as fixed size
        if (width.Type == UnitType.Fr && availableWidth != float.MaxValue && availableWidth > 0)
            widthInPixels = Math.Max(0, availableWidth - element.ComputedPaddingLeft - element.ComputedPaddingRight);
        if (height.Type == UnitType.Fr && availableHeight != float.MaxValue && availableHeight > 0)
            heightInPixels = Math.Max(0, availableHeight - element.ComputedPaddingTop - element.ComputedPaddingBottom);

        // Initialize content size
        if (element is Text baseText)
        {
            // Width: if explicit pixels provided use that, if auto use MetricTable, otherwise constrain to available width
            if (width.Type == UnitType.Auto)
            {
                var fontName = element.Font ?? element.RootFont ?? "__default__";
                element.MeasuredContentWidth = MetricTable.MeasureText(baseText.Value ?? string.Empty, fontName, element.CurrentFontSize);
            }
            else
            {
                element.MeasuredContentWidth = widthInPixels == 0 ? availableContentWidth : widthInPixels;
            }

            // Height: auto resolves to single-line height from font metrics. Explicit heights are honored.
            if (height.Type == UnitType.Auto)
            {
                var fontName = element.Font ?? element.RootFont ?? "__default__";
                element.MeasuredContentHeight = MetricTable.GetLineHeight(fontName, element.CurrentFontSize);
            }
            else
            {
                element.MeasuredContentHeight = heightInPixels == 0 ? element.CurrentFontSize : heightInPixels;
            }
        }
        else
        {
            element.MeasuredContentWidth = widthInPixels;
            element.MeasuredContentHeight = heightInPixels;
        }

        // Measure children based on element type
        if (element is Stack stack)
        {
            MeasureStack(stack, availableContentWidth, availableContentHeight);
        }
        else if (element is Grid grid)
        {
            MeasureGrid(grid, availableContentWidth, availableContentHeight);
        }
        else if (element is Dock dock)
        {
            MeasureDock(dock, availableContentWidth, availableContentHeight);
        }
        else if (element is Overlay overlay)
        {
            MeasureOverlay(overlay, availableWidth, availableHeight);
        }
        else if (element is Div div)
        {
            MeasureDiv(div, availableContentWidth, availableContentHeight);
        }
        else if (element.Children.Count > 0)
        {
            MeasureGeneric(element, availableContentWidth, availableContentHeight);
        }

        // Handle root element sizing per-axis:
        // - Fr or None (unset/defaulted to layout-fill): fill available space
        // - Auto (explicit shrink-wrap): keep measured content size, capped to available
        // - Pixels/Rem/Em: already resolved via widthInPixels, just cap to available
        if (element.Parent == null)
        {
            if (width.Type == UnitType.Fr || width.Type == UnitType.None)
                element.MeasuredContentWidth = availableContentWidth;
            else if (width.Type == UnitType.Auto)
                element.MeasuredContentWidth = Math.Min(element.MeasuredContentWidth, availableContentWidth);
            else if (widthInPixels == 0)
                element.MeasuredContentWidth = Math.Min(element.MeasuredContentWidth, availableContentWidth);

            if (height.Type == UnitType.Fr || height.Type == UnitType.None)
                element.MeasuredContentHeight = availableContentHeight;
            else if (height.Type == UnitType.Auto)
                element.MeasuredContentHeight = Math.Min(element.MeasuredContentHeight, availableContentHeight);
            else if (heightInPixels == 0)
                element.MeasuredContentHeight = Math.Min(element.MeasuredContentHeight, availableContentHeight);
        }
        else
        {
            // Apply constraints for non-root elements
            if (widthInPixels == 0)
                element.MeasuredContentWidth = Math.Min(element.MeasuredContentWidth, availableContentWidth);
            if (heightInPixels == 0)
                element.MeasuredContentHeight = Math.Min(element.MeasuredContentHeight, availableContentHeight);
        }

        // Calculate total size including padding
        element.ActualWidth = element.MeasuredContentWidth + element.ComputedPaddingLeft + element.ComputedPaddingRight;
        element.ActualHeight = element.MeasuredContentHeight + element.ComputedPaddingTop + element.ComputedPaddingBottom;
    }

    private static void MeasureStack(Stack stack, float availableWidth, float availableHeight)
    {
        var rowGap = UnitValue.Parse(stack.RowGap ?? stack.Gap);
        var colGap = UnitValue.Parse(stack.ColumnGap ?? stack.Gap);
        var totalHorizontalSpacing = Math.Max(0, stack.ToPixels(colGap) * (stack.Children.Count - 1));
        var totalVerticalSpacing = Math.Max(0, stack.ToPixels(rowGap) * (stack.Children.Count - 1));
        if (stack.Orientation == Orientation.Horizontal)
        {
            MeasureHorizontalStack(stack, availableWidth, availableHeight, totalHorizontalSpacing);
        }
        else
        {
            MeasureVerticalStack(stack, availableWidth, availableHeight, totalVerticalSpacing);
        }
    }

    private static void MeasureHorizontalStack(Stack stack, float availableWidth, float availableHeight, float totalSpacing)
    {
        float totalWidth = 0;
        float maxHeight = 0;
        var frElements = new List<UIElement>();

        // Measure fixed-size children
        foreach (var child in stack.Children)
        {
            var width = UnitValue.Parse(child.Width);
            if (width.Type == UnitType.Fr || (width.Type == UnitType.None && child is LayoutElement))
            {
                frElements.Add(child);
            }
            else
            {
                child.CurrentFontSize = stack.CurrentFontSize;
                MeasureElement(child, availableWidth, availableHeight);
                totalWidth += child.ActualWidth + child.ComputedMarginLeft + child.ComputedMarginRight;
                maxHeight = Math.Max(maxHeight, child.ActualHeight + child.ComputedMarginTop + child.ComputedMarginBottom);
            }
        }

        // Resolve FractionalUnit widths
        if (frElements.Count > 0)
        {
            var remainingWidth = availableWidth - totalWidth - totalSpacing;
            ResolveFractionalUnitWidths(frElements, Math.Max(0, remainingWidth), availableHeight);

            foreach (var frElement in frElements)
            {
                totalWidth += frElement.ActualWidth + frElement.ComputedMarginLeft + frElement.ComputedMarginRight;
                maxHeight = Math.Max(maxHeight, frElement.ActualHeight + frElement.ComputedMarginTop + frElement.ComputedMarginBottom);
            }
        }

        stack.MeasuredContentWidth = Math.Max(stack.MeasuredContentWidth, totalWidth + totalSpacing);
        stack.MeasuredContentHeight = Math.Max(stack.MeasuredContentHeight, maxHeight);
    }

    private static void MeasureVerticalStack(Stack stack, float availableWidth, float availableHeight, float totalSpacing)
    {
        float totalHeight = 0;
        float maxWidth = 0;
        var elements = new List<UIElement>();

        // Measure fixed-size children
        foreach (var child in stack.Children)
        {
            var height = UnitValue.Parse(child.Height);
            if (height.Type == UnitType.Fr || (height.Type == UnitType.None && child is LayoutElement))
            {
                elements.Add(child);
            }
            else
            {
                child.CurrentFontSize = stack.CurrentFontSize;
                MeasureElement(child, availableWidth, availableHeight);
                totalHeight += child.ActualHeight + child.ComputedMarginTop + child.ComputedMarginBottom;
                maxWidth = Math.Max(maxWidth, child.ActualWidth + child.ComputedMarginLeft + child.ComputedMarginRight);
            }
        }

        // Resolve FractionalUnit heights
        if (elements.Count > 0)
        {
            var remainingHeight = availableHeight - totalHeight - totalSpacing;
            ResolveFractionalUnitHeights(elements, Math.Max(0, remainingHeight), availableWidth);

            foreach (var element in elements)
            {
                totalHeight += element.ActualHeight + element.ComputedMarginTop + element.ComputedMarginBottom;
                maxWidth = Math.Max(maxWidth, element.ActualWidth + element.ComputedMarginLeft + element.ComputedMarginRight);
            }
        }

        stack.MeasuredContentWidth = Math.Max(stack.MeasuredContentWidth, maxWidth);
        stack.MeasuredContentHeight = Math.Max(stack.MeasuredContentHeight, totalHeight + totalSpacing);
    }

    private static void MeasureGrid(Grid grid, float availableWidth, float availableHeight)
    {
        // When grid has no explicit columns/rows and at least one dimension is auto-sized, 
        // measure children with auto space and let content drive sizing
        bool isWidthAuto = UnitValue.Parse(grid.Width).Type == UnitType.Auto;
        bool isHeightAuto = UnitValue.Parse(grid.Height).Type == UnitType.Auto;

        if (string.IsNullOrWhiteSpace(grid.Columns) && string.IsNullOrWhiteSpace(grid.Rows) 
            && (isWidthAuto || isHeightAuto))
        {
            float maxWidth = 0;
            float maxHeight = 0;

            foreach (var gridChild in grid.GridChildren)
            {
                gridChild.Element.CurrentFontSize = grid.CurrentFontSize;
                // Measure with effectively unlimited space only for the auto dimension
                float childAvailW = isWidthAuto ? float.MaxValue : availableWidth;
                float childAvailH = isHeightAuto ? float.MaxValue : availableHeight;
                
                MeasureElement(gridChild.Element, childAvailW, childAvailH);
                maxWidth = Math.Max(maxWidth, gridChild.Element.ActualWidth + gridChild.Element.ComputedMarginLeft + gridChild.Element.ComputedMarginRight);
                maxHeight = Math.Max(maxHeight, gridChild.Element.ActualHeight + gridChild.Element.ComputedMarginTop + gridChild.Element.ComputedMarginBottom);
            }

            if (isWidthAuto) grid.MeasuredContentWidth = maxWidth;
            if (isHeightAuto) grid.MeasuredContentHeight = maxHeight;
        }
        else
        {
            float gridContentAvailW = availableWidth == float.MaxValue ? 0 : availableWidth;
            float gridContentAvailH = availableHeight == float.MaxValue ? 0 : availableHeight;

            var columnWidths = grid.ParseUnits(grid.Columns, availableWidth == float.MaxValue ? 0 : availableWidth);
            var rowHeights = grid.ParseUnits(grid.Rows, availableHeight == float.MaxValue ? 0 : availableHeight);

            foreach (var gridChild in grid.GridChildren)
            {
                var childWidth = Grid.GetSpanSize(columnWidths, gridChild.Column, gridChild.ColumnSpan);
                var childHeight = Grid.GetSpanSize(rowHeights, gridChild.Row, gridChild.RowSpan);

                gridChild.Element.CurrentFontSize = grid.CurrentFontSize;
                MeasureElement(gridChild.Element, childWidth, childHeight);
            }

            grid.MeasuredContentWidth = columnWidths.Sum();
            grid.MeasuredContentHeight = rowHeights.Sum();
        }
    }

    private static void MeasureDock(Dock dock, float availableWidth, float availableHeight)
    {
        float maxWidth = 0;
        float maxHeight = 0;
        foreach (var dockChild in dock.DockChildren)
        {
            dockChild.Element.CurrentFontSize = dock.CurrentFontSize;
            MeasureElement(dockChild.Element, availableWidth, availableHeight);
            maxWidth = Math.Max(maxWidth, dockChild.Element.ActualWidth + dockChild.Element.ComputedMarginLeft + dockChild.Element.ComputedMarginRight);
            maxHeight = Math.Max(maxHeight, dockChild.Element.ActualHeight + dockChild.Element.ComputedMarginTop + dockChild.Element.ComputedMarginBottom);
        }

        var width = UnitValue.Parse(dock.Width);
        var height = UnitValue.Parse(dock.Height);

        if (width.Type == UnitType.Auto)
            dock.MeasuredContentWidth = maxWidth;
        else
            dock.MeasuredContentWidth = availableWidth == float.MaxValue ? maxWidth : availableWidth;

        if (height.Type == UnitType.Auto)
            dock.MeasuredContentHeight = maxHeight;
        else
            dock.MeasuredContentHeight = availableHeight == float.MaxValue ? maxHeight : availableHeight;
    }

    private static void MeasureOverlay(Overlay overlay, float availableWidth, float availableHeight)
    {
        // Overlays should fill available space, BUT:
        // When measured with float.MaxValue (auto-sized parent), measure content instead
        // When measured with actual size (explicit parent dimensions), use that space
        
        float contentWidth;
        float contentHeight;

        // measure to content size
        if (availableWidth == float.MaxValue && availableHeight == float.MaxValue)
        {
            float maxWidth = 0;
            float maxHeight = 0;
            foreach (var child in overlay.Children)
            {
                child.CurrentFontSize = overlay.CurrentFontSize;
                MeasureElement(child, float.MaxValue, float.MaxValue);
                maxWidth = Math.Max(maxWidth, child.ActualWidth + child.ComputedMarginLeft + child.ComputedMarginRight);
                maxHeight = Math.Max(maxHeight, child.ActualHeight + child.ComputedMarginTop + child.ComputedMarginBottom);
            }
            contentWidth = maxWidth;
            contentHeight = maxHeight;
        }
        else
        {
            // Always fill available space unless explicit pixel size is set
            var width = UnitValue.Parse(overlay.Width);
            if (width.Type == UnitType.Auto || width.Type == UnitType.None)
                contentWidth = availableWidth;
            else
                contentWidth = overlay.ToPixels(width);

            var height = UnitValue.Parse(overlay.Height);
            if (height.Type == UnitType.Auto || height.Type == UnitType.None)
                contentHeight = availableHeight;
            else
                contentHeight = overlay.ToPixels(height);

            foreach (var child in overlay.Children)
            {
                child.CurrentFontSize = overlay.CurrentFontSize;
                // If dimensions are unconstrained (MaxValue), measure children with that MaxValue
                MeasureElement(child, contentWidth, contentHeight);
            }
        }

        // Guarantee overlays always get valid size for mapping
        // If unconstrained, we might need to fallback to a reasonable default or keep it MaxValue
        if (contentWidth == 0 && availableWidth != float.MaxValue)
            contentWidth = availableWidth;
        if (contentHeight == 0 && availableHeight != float.MaxValue)
            contentHeight = availableHeight;
            
        overlay.MeasuredContentWidth = contentWidth;
        overlay.MeasuredContentHeight = contentHeight;
    }

    private static void MeasureDiv(Div div, float availableWidth, float availableHeight)
    {
        if (div.Display?.Equals("flex", StringComparison.OrdinalIgnoreCase) == true)
        {
            MeasureFlexDiv(div, availableWidth, availableHeight);
        }
        else
        {
            bool hasExplicitlyPositionedChildren = div.Children
                .Any(c => UnitValue.Parse(c.Top).Type != UnitType.None
                || UnitValue.Parse(c.Left).Type != UnitType.None
                || UnitValue.Parse(c.Bottom).Type != UnitType.None
                || UnitValue.Parse(c.Right).Type != UnitType.None);

            if (!hasExplicitlyPositionedChildren && div.Children.Count > 0)
            {
                MeasureVerticalDiv(div, availableWidth, availableHeight);
            }
            else
            {
                MeasureStandardDiv(div, availableWidth, availableHeight);
            }
        }
    }

    private static void MeasureFlexDiv(Div div, float availableWidth, float availableHeight)
    {
        bool isRow = !string.Equals(div.FlexDirection, "column", StringComparison.OrdinalIgnoreCase);
        var rowGap = UnitValue.Parse(div.RowGap ?? div.Gap);
        var colGap = UnitValue.Parse(div.ColumnGap ?? div.Gap);
        float totalSpacing = Math.Max(0, (isRow ? div.ToPixels(colGap) : div.ToPixels(rowGap)) * (div.Children.Count - 1));

        float mainAxisSize = 0;
        float crossAxisSize = 0;
        var flexElements = new List<UIElement>();

        foreach (var child in div.Children)
        {
            var mainUnit = UnitValue.Parse(isRow ? child.Width : child.Height);
            if (mainUnit.Type == UnitType.Fr || (mainUnit.Type == UnitType.None && child is LayoutElement))
            {
                flexElements.Add(child);
            }
            else
            {
                child.CurrentFontSize = div.CurrentFontSize;
                MeasureElement(child, availableWidth, availableHeight);
                float childWidth = child.ActualWidth + child.ComputedMarginLeft + child.ComputedMarginRight;
                float childHeight = child.ActualHeight + child.ComputedMarginTop + child.ComputedMarginBottom;
                mainAxisSize += isRow ? childWidth : childHeight;
                crossAxisSize = Math.Max(crossAxisSize, isRow ? childHeight : childWidth);
            }
        }

        if (flexElements.Count > 0)
        {
            float remainingSpace = (isRow ? availableWidth : availableHeight) - mainAxisSize - totalSpacing;
            if (isRow)
                ResolveFractionalUnitWidths(flexElements, Math.Max(0, remainingSpace), availableHeight);
            else
                ResolveFractionalUnitHeights(flexElements, Math.Max(0, remainingSpace), availableWidth);

            foreach (var flexElement in flexElements)
            {
                float childWidth = flexElement.ActualWidth + flexElement.ComputedMarginLeft + flexElement.ComputedMarginRight;
                float childHeight = flexElement.ActualHeight + flexElement.ComputedMarginTop + flexElement.ComputedMarginBottom;
                mainAxisSize += isRow ? childWidth : childHeight;
                crossAxisSize = Math.Max(crossAxisSize, isRow ? childHeight : childWidth);
            }
        }

        div.MeasuredContentWidth = isRow ? Math.Max(div.MeasuredContentWidth, mainAxisSize + totalSpacing) : Math.Max(div.MeasuredContentWidth, crossAxisSize);
        div.MeasuredContentHeight = isRow ? Math.Max(div.MeasuredContentHeight, crossAxisSize) : Math.Max(div.MeasuredContentHeight, mainAxisSize + totalSpacing);

        // Handle stretch alignment
        if (string.Equals(div.AlignItems, "stretch", StringComparison.OrdinalIgnoreCase))
        {
            float crossStretchSize = isRow ? div.MeasuredContentHeight : div.MeasuredContentWidth;
            foreach (var child in div.Children)
            {
                if (isRow)
                {
                    if (UnitValue.Parse(child.Height).Type == UnitType.None || UnitValue.Parse(child.Height).Type == UnitType.Auto)
                        child.ActualHeight = crossStretchSize;
                }
                else
                {
                    if (UnitValue.Parse(child.Width).Type == UnitType.None || UnitValue.Parse(child.Width).Type == UnitType.Auto)
                        child.ActualWidth = crossStretchSize;
                }
            }
        }
    }

    private static void MeasureVerticalDiv(Div div, float availableWidth, float availableHeight)
    {
        float maxWidth = 0;
        float totalHeight = 0;
        var elements = new List<UIElement>();
        var rowGap = UnitValue.Parse(div.RowGap ?? div.Gap);
        var colGap = UnitValue.Parse(div.ColumnGap ?? div.Gap);
        var totalHorizontalSpacing = Math.Max(0, div.ToPixels(colGap) * (div.Children.Count - 1));
        var totalVerticalSpacing = Math.Max(0, div.ToPixels(rowGap) * (div.Children.Count - 1));

        // Resolve unspecified width for Div (default to 1fr)
        var width = UnitValue.Parse(div.Width);
        if (width.Type == UnitType.None)
        {
            div.Width = "fr";
            width = UnitValue.OneFR;
        }
        // Resolve unspecified height for Div (default to Auto)
        var height = UnitValue.Parse(div.Height);
        if (height.Type == UnitType.None)
            div.Height = (height = UnitValue.Auto).ToString();

        // Measure fixed-size children
        foreach (var child in div.Children)
        {
            var cheight = UnitValue.Parse(child.Height);
            if (cheight.Type == UnitType.Fr || (cheight.Type == UnitType.None && child is LayoutElement))
            {
                elements.Add(child);
            }
            else
            {
                child.CurrentFontSize = div.CurrentFontSize;
                MeasureElement(child, availableWidth, availableHeight);
                totalHeight += child.ActualHeight + child.ComputedMarginTop + child.ComputedMarginBottom;
                maxWidth = Math.Max(maxWidth, child.ActualWidth + child.ComputedMarginLeft + child.ComputedMarginRight);
            }
        }

        // Resolve FractionalUnit heights
        if (elements.Count > 0)
        {
            var remainingHeight = availableHeight - totalHeight - totalVerticalSpacing;
            ResolveFractionalUnitHeights(elements, Math.Max(0, remainingHeight), availableWidth);

            foreach (var element in elements)
            {
                totalHeight += element.ActualHeight + element.ComputedMarginTop + element.ComputedMarginBottom;
                maxWidth = Math.Max(maxWidth, element.ActualWidth + element.ComputedMarginLeft + element.ComputedMarginRight);
            }
        }

        // Preserve explicit dimensions
        if (width.Type != UnitType.None && width.Type != UnitType.Auto)
        {
            // Keep explicit width
        }
        else
        {
            availableWidth = FractionalUnit.Sanitize(availableWidth);
            div.MeasuredContentWidth = Math.Max(maxWidth, availableWidth);
        }

        if (height.Type != UnitType.None && height.Type != UnitType.Auto)
        {
            // Keep explicit height
        }
        else
        {
            div.MeasuredContentHeight = totalHeight + totalVerticalSpacing;
        }
    }

    private static void MeasureStandardDiv(Div div, float availableWidth, float availableHeight)
    {
        float maxWidth = 0;
        float maxHeight = 0;

        foreach (var child in div.Children)
        {
            child.CurrentFontSize = div.CurrentFontSize;
            MeasureElement(child, availableWidth, availableHeight);
            maxWidth = Math.Max(maxWidth, child.ActualWidth + child.ComputedMarginLeft + child.ComputedMarginRight);
            maxHeight = Math.Max(maxHeight, child.ActualHeight + child.ComputedMarginTop + child.ComputedMarginBottom);
        }

        var width = UnitValue.Parse(div.Width);
        var height = UnitValue.Parse(div.Height);
        
        if (width.Type == UnitType.Auto)
            div.MeasuredContentWidth = maxWidth;
        else if (width.Type == UnitType.Fr && availableWidth != float.MaxValue)
            div.MeasuredContentWidth = availableWidth;

        if (height.Type == UnitType.Auto)
            div.MeasuredContentHeight = maxHeight;
        else if (height.Type == UnitType.Fr && availableHeight != float.MaxValue)
            div.MeasuredContentHeight = availableHeight;
    }

    private static void MeasureGeneric(UIElement element, float availableWidth, float availableHeight)
    {
        float maxWidth = 0;
        float maxHeight = 0;

        // If element has explicit sizing, constrain children to that size
        var width = UnitValue.Parse(element.Width);
        var height = UnitValue.Parse(element.Height);

        var childAvailableWidth = availableWidth;
        var childAvailableHeight = availableHeight;

        // If element has explicit pixel width, constrain children to that width
        if (width.Type != UnitType.Auto && width.Type != UnitType.None && width.Type != UnitType.Fr)
        {
            childAvailableWidth = element.ToPixels(element.Width ?? "0");
        }

        // If element has explicit pixel height, constrain children to that height
        if (height.Type != UnitType.Auto && height.Type != UnitType.None && height.Type != UnitType.Fr)
        {
            childAvailableHeight = element.ToPixels(element.Height ?? "0");
        }

        foreach (var child in element.Children)
        {
            child.CurrentFontSize = element.CurrentFontSize;
            MeasureElement(child, childAvailableWidth, childAvailableHeight);
            maxWidth = Math.Max(maxWidth, child.ActualWidth + child.ComputedMarginLeft + child.ComputedMarginRight);
            maxHeight = Math.Max(maxHeight, child.ActualHeight + child.ComputedMarginTop + child.ComputedMarginBottom);
        }

        element.MeasuredContentWidth = Math.Max(element.MeasuredContentWidth, maxWidth);
        element.MeasuredContentHeight = Math.Max(element.MeasuredContentHeight, maxHeight);
    }

    private static void ResolveFractionalUnitWidths(List<UIElement> elements, float remainingSpace, float availableHeight)
    {
        var values = elements.Select(e => UnitValue.Parse(e.Width)).Select(w => w.Type == UnitType.Fr ? w.Value : 1f).ToArray();
        var resolvedValues = FractionalUnit.Resolve(values, remainingSpace);

        for (int i = 0; i < elements.Count; i++)
        {
            elements[i].CurrentFontSize = elements[i].Parent?.CurrentFontSize ?? 16f;
            MeasureElement(elements[i], resolvedValues[i], availableHeight);
        }
    }

    private static void ResolveFractionalUnitHeights(List<UIElement> elements, float remainingSpace, float availableWidth)
    {
        var values = elements.Select(e => UnitValue.Parse(e.Height)).Select(h => h.Type == UnitType.Fr ? h.Value : 1f).ToArray();
        var resolvedValues = FractionalUnit.Resolve(values, remainingSpace);

        for (int i = 0; i < elements.Count; i++)
        {
            elements[i].CurrentFontSize = elements[i].Parent?.CurrentFontSize ?? 16f;
            MeasureElement(elements[i], availableWidth, resolvedValues[i]);
        }
    }

    private static void PositionElement(UIElement element, float parentX, float parentY)
    {
        // Default positioning: only set coordinates that haven't been
        // positioned by the parent container/parent-specific layout logic.
        if (FractionalUnit.IsInvalid(element.ActualX))
            element.ActualX = parentX;
        if (FractionalUnit.IsInvalid(element.ActualY))
            element.ActualY = parentY;

        // Position based on element type
        switch (element)
        {
            case Stack stack:
                PositionStack(stack);
                break;
            case Grid grid:
                PositionGrid(grid);
                break;
            case Dock dock:
                PositionDock(dock);
                break;
            case Overlay overlay:
                PositionOverlay(overlay);
                break;
            case Div div:
                PositionDiv(div);
                break;
            default:
                if (element.Children.Count > 0)
                {
                    PositionGeneric(element);
                }
                break;
        }

        // Position children
        var contentX = element.ActualX + element.ComputedMarginLeft + element.ComputedPaddingLeft;
        var contentY = element.ActualY + element.ComputedMarginTop + element.ComputedPaddingTop;
        
        foreach (var child in element.Children)
        {
            child.CurrentFontSize = element.CurrentFontSize;
            PositionElement(child, contentX, contentY);
        }
    }

    private static void PositionStack(Stack stack)
    {
        if (stack.Orientation == Orientation.Horizontal)
        {
            PositionHorizontalStack(stack);
        }
        else
        {
            PositionVerticalStack(stack);
        }
    }

    private static void PositionHorizontalStack(Stack stack)
    {
        float totalWidth = 0;
        foreach (var child in stack.Children)
            totalWidth += child.ActualWidth + child.ComputedMarginLeft + child.ComputedMarginRight;

        var colGap = UnitValue.Parse(stack.ColumnGap ?? stack.Gap);
        var gapPixels = stack.ToPixels(colGap);
        if (stack.Children.Count > 1)
        {
            var totalHorizontalSpacing = gapPixels * (stack.Children.Count - 1);
            totalWidth += totalHorizontalSpacing;
        }

        float currentX = stack.ActualX + stack.ComputedPaddingLeft + 
            CalculateHorizontalAlignmentOffset(stack.MeasuredContentWidth, totalWidth, stack.JustifyContent);
        float baseY = stack.ActualY + stack.ComputedPaddingTop;

        foreach (var child in stack.Children)
        {
            child.ActualX = currentX + child.ComputedMarginLeft;
            child.ActualY = baseY + child.ComputedMarginTop;
            ApplyVerticalAlignment(child, baseY, stack.MeasuredContentHeight);
            currentX += child.ActualWidth + child.ComputedMarginLeft + child.ComputedMarginRight + gapPixels;
        }
    }

    private static void PositionVerticalStack(Stack stack)
    {
        float baseX = stack.ActualX + stack.ComputedPaddingLeft;
        
        float totalHeight = 0;
        foreach (var child in stack.Children)
            totalHeight += child.ActualHeight + child.ComputedMarginTop + child.ComputedMarginBottom;

        var rowGap = UnitValue.Parse(stack.RowGap ?? stack.Gap);
        var gapPixels = stack.ToPixels(rowGap);
        if (stack.Children.Count > 1)
        {
            var totalVerticalSpacing = gapPixels * (stack.Children.Count - 1);
            totalHeight += totalVerticalSpacing;
        }

        float currentY = stack.ActualY + stack.ComputedPaddingTop + 
            CalculateVerticalAlignmentOffset(stack.MeasuredContentHeight, totalHeight, stack.AlignContent);

        foreach (var child in stack.Children)
        {
            child.ActualX = baseX + child.ComputedMarginLeft;
            child.ActualY = currentY + child.ComputedMarginTop;
            ApplyHorizontalAlignment(child, baseX, stack.MeasuredContentWidth);
            currentY += child.ActualHeight + child.ComputedMarginTop + child.ComputedMarginBottom + gapPixels;
        }
    }

    private static void PositionGrid(Grid grid)
    {
        var columnWidths = grid.ParseUnits(grid.Columns, grid.MeasuredContentWidth);
        var rowHeights = grid.ParseUnits(grid.Rows, grid.MeasuredContentHeight);

        float baseX = grid.ActualX + grid.ComputedPaddingLeft;
        float baseY = grid.ActualY + grid.ComputedPaddingTop;

        foreach (var gridChild in grid.GridChildren)
        {
            float x = baseX;
            float y = baseY;

            for (int i = 0; i < gridChild.Column; i++)
                x += columnWidths[i];

            for (int i = 0; i < gridChild.Row; i++)
                y += rowHeights[i];

            var cellWidth = Grid.GetSpanSize(columnWidths, gridChild.Column, gridChild.ColumnSpan);
            var cellHeight = Grid.GetSpanSize(rowHeights, gridChild.Row, gridChild.RowSpan);

            gridChild.Element.ActualX = x;
            gridChild.Element.ActualY = y;

            ApplyHorizontalAlignment(gridChild.Element, x, cellWidth);
            ApplyVerticalAlignment(gridChild.Element, y, cellHeight);
        }
    }

    private static void PositionDock(Dock dock)
    {
        float left = dock.ActualX + dock.ComputedPaddingLeft;
        float top = dock.ActualY + dock.ComputedPaddingTop;
        float right = left + dock.MeasuredContentWidth;
        float bottom = top + dock.MeasuredContentHeight;

        foreach (var dockChild in dock.DockChildren)
        {
            var child = dockChild.Element;
            switch (dockChild.Edge)
            {
                case DockEdge.Left:
                    child.ActualX = left;
                    child.ActualY = top;
                    child.ActualHeight = bottom - top;
                    left += child.ActualWidth;
                    break;
                case DockEdge.Right:
                    child.ActualX = right - child.ActualWidth;
                    child.ActualY = top;
                    child.ActualHeight = bottom - top;
                    right = child.ActualX;
                    break;
                case DockEdge.Top:
                    child.ActualX = left;
                    child.ActualY = top;
                    child.ActualWidth = right - left;
                    top += child.ActualHeight;
                    break;
                case DockEdge.Bottom:
                    child.ActualX = left;
                    child.ActualY = bottom - child.ActualHeight;
                    child.ActualWidth = right - left;
                    bottom = child.ActualY;
                    break;
            }
        }
    }

    private static void PositionDiv(Div div)
    {
        var anchor = Anchor.Parse(div.Anchor);
        if (anchor != Anchor.None)
        {
            PositionWithAnchor(div);
        }
        else if (UnitValue.Parse(div.Top).Type != UnitType.None
            || UnitValue.Parse(div.Left).Type != UnitType.None
            || UnitValue.Parse(div.Bottom).Type != UnitType.None
            || UnitValue.Parse(div.Right).Type != UnitType.None)
        {
            div.ActualX = div.ToPixels(div.Left);
            div.ActualY = div.ToPixels(div.Top);
            if (div.Parent != null)
            {
                div.ActualWidth = div.Parent.ActualWidth - div.ActualX - div.ToPixels(div.Right);
                div.ActualHeight = div.Parent.ActualHeight - div.ActualY - div.ToPixels(div.Bottom);
            }
        }

        if (div.Display?.Equals("flex", StringComparison.OrdinalIgnoreCase) == true)
        {
            PositionFlexDiv(div);
        }
        else
        {
            bool hasExplicitlyPositionedChildren = div.Children
                .Any(c => UnitValue.Parse(c.Top).Type != UnitType.None
                || UnitValue.Parse(c.Left).Type != UnitType.None
                || UnitValue.Parse(c.Bottom).Type != UnitType.None
                || UnitValue.Parse(c.Right).Type != UnitType.None);

            if (!hasExplicitlyPositionedChildren && div.Children.Count > 0)
            {
                PositionVerticalDiv(div);
            }
        }
    }

    private static void PositionFlexDiv(Div div)
    {
        bool isRow = !string.Equals(div.FlexDirection, "column", StringComparison.OrdinalIgnoreCase);
        var rowGap = UnitValue.Parse(div.RowGap ?? div.Gap);
        var colGap = UnitValue.Parse(div.ColumnGap ?? div.Gap);
        float itemSpacing = isRow ? div.ToPixels(colGap) : div.ToPixels(rowGap);

        float totalChildrenSize = 0;
        foreach (var child in div.Children)
            totalChildrenSize += isRow ? child.ActualWidth + child.ComputedMarginLeft + child.ComputedMarginRight : child.ActualHeight + child.ComputedMarginTop + child.ComputedMarginBottom;
        
        float totalSpacing = itemSpacing * (div.Children.Count - 1);
        float contentSize = totalChildrenSize + totalSpacing;

        float startOffset = 0;
        float actualGap = itemSpacing;

        // Justify Content
        var justify = div.JustifyContent?.ToLowerInvariant();
        float availableSpace = (isRow ? div.MeasuredContentWidth : div.MeasuredContentHeight) - contentSize;
        availableSpace = FractionalUnit.Sanitize(availableSpace);

        switch (justify)
        {
            case "flex-end":
            case "end":
                startOffset = availableSpace;
                break;
            case "center":
                startOffset = availableSpace / 2;
                break;
            case "space-between":
                if (div.Children.Count > 1)
                {
                    actualGap = itemSpacing + (availableSpace / (div.Children.Count - 1));
                    startOffset = 0;
                }
                break;
            case "space-around":
                if (div.Children.Count > 0)
                {
                    float gap = availableSpace / div.Children.Count;
                    actualGap = itemSpacing + gap;
                    startOffset = gap / 2;
                }
                break;
        }

        float currentPos = (isRow ? div.ActualX + div.ComputedPaddingLeft : div.ActualY + div.ComputedPaddingTop) + startOffset;
        float crossStart = isRow ? div.ActualY + div.ComputedPaddingTop : div.ActualX + div.ComputedPaddingLeft;
        float crossSize = isRow ? div.MeasuredContentHeight : div.MeasuredContentWidth;

        foreach (var child in div.Children)
        {
            if (isRow)
            {
                child.ActualX = currentPos + child.ComputedMarginLeft;
                ApplyVerticalAlignment(child, crossStart, crossSize);
                
                // Align items override
                var align = div.AlignItems?.ToLowerInvariant();
                if (align != null)
                {
                    switch (align)
                    {
                        case "flex-start": case "start": child.ActualY = crossStart; break;
                        case "flex-end": case "end": child.ActualY = crossStart + crossSize - child.ActualHeight; break;
                        case "center": child.ActualY = crossStart + (crossSize - child.ActualHeight) / 2; break;
                    }
                }
                
                currentPos += child.ActualWidth + child.ComputedMarginLeft + child.ComputedMarginRight + actualGap;
            }
            else
            {
                child.ActualY = currentPos + child.ComputedMarginTop;
                ApplyHorizontalAlignment(child, crossStart, crossSize);

                // Align items override
                var align = div.AlignItems?.ToLowerInvariant();
                if (align != null)
                {
                    switch (align)
                    {
                        case "flex-start": case "start": child.ActualX = crossStart; break;
                        case "flex-end": case "end": child.ActualX = crossStart + crossSize - child.ActualWidth; break;
                        case "center": child.ActualX = crossStart + (crossSize - child.ActualWidth) / 2; break;
                    }
                }

                currentPos += child.ActualHeight + child.ComputedMarginTop + child.ComputedMarginBottom + actualGap;
            }
        }
    }

    private static void PositionOverlay(Overlay overlay)
    {
        float baseX = overlay.ActualX + overlay.ComputedPaddingLeft;
        float baseY = overlay.ActualY + overlay.ComputedPaddingTop;

        foreach (var child in overlay.Children)
        {
            child.ActualX = baseX;
            child.ActualY = baseY;
            ApplyHorizontalAlignment(child, baseX, overlay.MeasuredContentWidth);
            ApplyVerticalAlignment(child, baseY, overlay.MeasuredContentHeight);
        }
    }

    private static void PositionVerticalDiv(Div div)
    {
        float baseX = div.ActualX + div.ComputedPaddingLeft;

        float totalHeight = 0;
        foreach (var child in div.Children)
            totalHeight += child.ActualHeight + child.ComputedMarginTop + child.ComputedMarginBottom;

        var rowGap = UnitValue.Parse(div.RowGap ?? div.Gap);
        var gapPixels = div.ToPixels(rowGap);
        if (div.Children.Count > 1)
        {
            var totalVerticalSpacing = gapPixels * (div.Children.Count - 1);
            totalHeight += totalVerticalSpacing;
        }

        float currentY = div.ActualY + div.ComputedPaddingTop + 
            CalculateVerticalAlignmentOffset(div.MeasuredContentHeight, totalHeight, div.AlignItems);

        foreach (var child in div.Children)
        {
            child.ActualX = baseX + child.ComputedMarginLeft;
            child.ActualY = currentY + child.ComputedMarginTop;
            ApplyHorizontalAlignment(child, baseX, div.MeasuredContentWidth, div.JustifyContent);
            currentY += child.ActualHeight + child.ComputedMarginTop + child.ComputedMarginBottom + gapPixels;
        }
    }

    private static void PositionWithAnchor(UIElement element)
    {
        // Get parent dimensions
        float parentWidth = element.Parent?.ActualWidth ?? 0;
        float parentHeight = element.Parent?.ActualHeight ?? 0;
        
        if (parentWidth == 0 || parentHeight == 0)
        {
            // Fallback if parent not measured
            element.ActualX = 0;
            element.ActualY = 0;
            return;
        }

        var anchor = Anchor.Parse(element.Anchor);
        
        // WinForms-style anchoring: element pins to the specified edges
        // If both opposite edges are anchored, element stretches between them
        
        bool left = anchor.HasFlag(Anchor.Left);
        bool right = anchor.HasFlag(Anchor.Right);
        bool top = anchor.HasFlag(Anchor.Top);
        bool bottom = anchor.HasFlag(Anchor.Bottom);
        
        // Horizontal positioning
        if (left && right)
        {
            // Anchored to both left and right - stretch
            element.ActualX = 0;
            element.ActualWidth = parentWidth;
        }
        else if (right)
        {
            // Anchored to right only
            element.ActualX = parentWidth - element.ActualWidth;
        }
        else
        {
            // Anchored to left or no horizontal anchor (default left)
            element.ActualX = 0;
        }
        
        // Vertical positioning
        if (top && bottom)
        {
            // Anchored to both top and bottom - stretch
            element.ActualY = 0;
            element.ActualHeight = parentHeight;
        }
        else if (bottom)
        {
            // Anchored to bottom only
            element.ActualY = parentHeight - element.ActualHeight;
        }
        else
        {
            // Anchored to top or no vertical anchor (default top)
            element.ActualY = 0;
        }
    }

    private static void PositionGeneric(UIElement element)
    {
        float baseX = element.ActualX + element.ComputedPaddingLeft;
        float baseY = element.ActualY + element.ComputedPaddingTop;

        foreach (var child in element.Children)
        {
            child.ActualX = baseX;
            child.ActualY = baseY;
            ApplyHorizontalAlignment(child, baseX, element.MeasuredContentWidth);
            ApplyVerticalAlignment(child, baseY, element.MeasuredContentHeight);
        }
    }

    private static void ApplyHorizontalAlignment(UIElement element, float baseX, float containerWidth, string? parentJustifyContent = null)
    {
        var align = element.JustifySelf ?? element.Parent?.JustifyItems ?? parentJustifyContent;
        if (string.IsNullOrEmpty(align)) return;

        switch (align.ToLowerInvariant())
        {
            case "center":
                element.ActualX = baseX + (containerWidth - element.ActualWidth) / 2;
                break;
            case "end":
            case "flex-end":
            case "right":
                element.ActualX = baseX + containerWidth - element.ActualWidth;
                break;
            case "stretch":
                element.ActualX = baseX;
                if (UnitValue.Parse(element.Width).Type == UnitType.Auto)
                    element.ActualWidth = containerWidth;
                break;
            case "start":
            case "flex-start":
            case "left":
                element.ActualX = baseX;
                break;
        }
    }

    private static void ApplyVerticalAlignment(UIElement element, float baseY, float containerHeight, string? parentAlignItems = null)
    {
        var align = element.AlignSelf ?? element.Parent?.AlignItems ?? parentAlignItems;
        if (string.IsNullOrEmpty(align)) return;

        switch (align.ToLowerInvariant())
        {
            case "center":
                element.ActualY = baseY + (containerHeight - element.ActualHeight) / 2;
                break;
            case "end":
            case "flex-end":
            case "bottom":
                element.ActualY = baseY + containerHeight - element.ActualHeight;
                break;
            case "stretch":
                element.ActualY = baseY;
                if (UnitValue.Parse(element.Height).Type == UnitType.Auto)
                    element.ActualHeight = containerHeight;
                break;
            case "start":
            case "flex-start":
            case "top":
                element.ActualY = baseY;
                break;
        }
    }
    private static float CalculateHorizontalAlignmentOffset(float containerWidth, float contentWidth, string? justify)
    {
        if (string.IsNullOrEmpty(justify)) return 0;
        return justify.ToLowerInvariant() switch
        {
            "end" or "flex-end" or "right" => containerWidth - contentWidth,
            "center" => (containerWidth - contentWidth) / 2,
            _ => 0
        };
    }

    private static float CalculateVerticalAlignmentOffset(float containerHeight, float contentHeight, string? align)
    {
        if (string.IsNullOrEmpty(align)) return 0;
        return align.ToLowerInvariant() switch
        {
            "end" or "flex-end" or "bottom" => containerHeight - contentHeight,
            "center" => (containerHeight - contentHeight) / 2,
            _ => 0
        };
    }

    private static void DetectOverflow(UIElement element)
    {
        var width = UnitValue.Parse(element.Width);
        var height = UnitValue.Parse(element.Height);
        if (width.IsExplicit() && height.IsExplicit())
        {
            var explicitWidth = element.ToPixels(width);
            var explicitHeight = element.ToPixels(height);

            element.NeedsHorizontalScroll = element.MeasuredContentWidth > explicitWidth;
            element.NeedsVerticalScroll = element.MeasuredContentHeight > explicitHeight;
        }

        foreach (var child in element.Children)
        {
            DetectOverflow(child);
        }
    }
}
