namespace SUIM.Layout;

using SUIM.Components;
using SUIM.Components.Attributes;
using System.Xml.Linq;

public static class LayoutEngine
{
    public static void Layout(UIElement root, float rootFontSize, float availableWidth, float availableHeight)
    {
        ResetPositions(root);
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
            if (width.Type == UnitType.None) element.Width = (width = UnitValue.OneFR).ToString();
            if (height.Type == UnitType.None) element.Height = (height = UnitValue.OneFR).ToString();
        }
        else if (element is Stack or Grid or Dock or Overlay or Text)
        {
            if (width.Type == UnitType.None) element.Width = UnitValue.Auto.ToString();
            if (height.Type == UnitType.None) element.Height = UnitValue.Auto.ToString();
        }

        // Convert explicit sizes to pixels
        var widthInPixels = (width.Type == UnitType.Auto || width.Type == UnitType.None) ? 0 : element.ToPixels(element.Width);
        var heightInPixels = (height.Type == UnitType.Auto || height.Type == UnitType.None) ? 0 : element.ToPixels(element.Height);

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
        else
        {
            // Measure children for generic elements
            foreach (var child in element.Children)
            {
                child.CurrentFontSize = element.CurrentFontSize;
                MeasureElement(child, availableContentWidth, availableContentHeight);
            }
        }

        // Handle root element sizing per-axis (only fill available space for axes without any explicit sizes in the tree)
        if (element.Parent == null)
        {
            if (!TreeHasAnyExplicitWidth(element) || width.Type == UnitType.Fr)
                element.MeasuredContentWidth = availableContentWidth;
            else if (widthInPixels == 0)
                element.MeasuredContentWidth = Math.Min(element.MeasuredContentWidth, availableContentWidth);

            if (!TreeHasAnyExplicitHeight(element) || height.Type == UnitType.Fr)
                element.MeasuredContentHeight = availableContentHeight;
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

    private static bool TreeHasAnyExplicitWidth(UIElement element)
    {
        var width = UnitValue.Parse(element.Width);
        bool widthExplicit = width.Type != UnitType.None && 
                            width.Type != UnitType.Rem && 
                            width.Type != UnitType.Em && 
                            width.Type != UnitType.Fr;
        if (widthExplicit) return true;
        foreach (var child in element.Children)
        {
            if (TreeHasAnyExplicitWidth(child)) return true;
        }
        return false;
    }

    private static bool TreeHasAnyExplicitHeight(UIElement element)
    {
        var height = UnitValue.Parse(element.Height);
        // Treat any non-None/non-Fr height (including rem/em/auto) as explicit for height axis
        bool heightExplicit = height.Type != UnitType.None && height.Type != UnitType.Fr;
        if (heightExplicit) return true;
        foreach (var child in element.Children)
        {
            if (TreeHasAnyExplicitHeight(child)) return true;
        }
        return false;
    }

    private static void PositionElement(UIElement element, float parentX, float parentY)
    {
        // Position the element itself
        if (element is Stack or Grid or Dock or Div or Overlay)
        {
            // Container positioning handled by specific methods
        }
        else if (float.IsNaN(element.ActualX))
        {
            // Default positioning for leaf elements if not already positioned by parent container
            element.ActualX = parentX;
            element.ActualY = parentY;
        }

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

    private static void MeasureStack(Stack stack, float availableWidth, float availableHeight)
    {
        var totalSpacing = Math.Max(0, stack.Spacing * (stack.Children.Count - 1));

        if (stack.Orientation == Orientation.Horizontal)
        {
            MeasureHorizontalStack(stack, availableWidth, availableHeight, totalSpacing);
        }
        else
        {
            MeasureVerticalStack(stack, availableWidth, availableHeight, totalSpacing);
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
                totalWidth += child.ActualWidth;
                maxHeight = Math.Max(maxHeight, child.ActualHeight);
            }
        }

        // Resolve FractionalUnit widths
        if (frElements.Count > 0)
        {
            var remainingWidth = availableWidth - totalWidth - totalSpacing;
            ResolveFractionalUnitWidths(frElements, Math.Max(0, remainingWidth), availableHeight);

            foreach (var frElement in frElements)
            {
                totalWidth += frElement.ActualWidth;
                maxHeight = Math.Max(maxHeight, frElement.ActualHeight);
            }
        }

        stack.MeasuredContentWidth = totalWidth + totalSpacing;
        stack.MeasuredContentHeight = maxHeight;
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
                totalHeight += child.ActualHeight;
                maxWidth = Math.Max(maxWidth, child.ActualWidth);
            }
        }

        // Resolve FractionalUnit heights
        if (elements.Count > 0)
        {
            var remainingHeight = availableHeight - totalHeight - totalSpacing;
            ResolveFractionalUnitHeights(elements, Math.Max(0, remainingHeight), availableWidth);

            foreach (var element in elements)
            {
                totalHeight += element.ActualHeight;
                maxWidth = Math.Max(maxWidth, element.ActualWidth);
            }
        }

        stack.MeasuredContentWidth = maxWidth;
        stack.MeasuredContentHeight = totalHeight + totalSpacing;
    }

    private static void ResolveFractionalUnitWidths(List<UIElement> elements, float remainingSpace, float availableHeight)
    {
        var values = elements.Select(e => UnitValue.Parse(e.Width)).Select(w => w.Type == UnitType.Fr ? w.Value : 1f).ToArray();
        var resolvedValues = FractionalUnitResolver.ResolveFractionalUnits(values, remainingSpace);

        for (int i = 0; i < elements.Count; i++)
        {
            elements[i].CurrentFontSize = (elements[i].Parent as Stack)?.CurrentFontSize ?? 16f;
            var width = UnitValue.Parse(elements[i].Width);
            if (width.Type == UnitType.None) width = new UnitValue(resolvedValues[i], UnitType.Pixels);
            MeasureElement(elements[i], resolvedValues[i], availableHeight);
        }
    }

    private static void ResolveFractionalUnitHeights(List<UIElement> elements, float remainingSpace, float availableWidth)
    {
        var values = elements.Select(e => UnitValue.Parse(e.Height)).Select(h => h.Type == UnitType.Fr ? h.Value : 1f).ToArray();
        var resolvedValues = FractionalUnitResolver.ResolveFractionalUnits(values, remainingSpace);

        for (int i = 0; i < elements.Count; i++)
        {
            elements[i].CurrentFontSize = (elements[i].Parent as Stack)?.CurrentFontSize ?? 16f;
            var height = UnitValue.Parse(elements[i].Height);
            if (height.Type == UnitType.None) height = new UnitValue(resolvedValues[i], UnitType.Pixels);
            MeasureElement(elements[i], availableWidth, resolvedValues[i]);
        }
    }

    private static void MeasureGrid(Grid grid, float availableWidth, float availableHeight)
    {
        // When grid has no explicit columns/rows and is auto-sized, 
        // measure children with auto space and let content drive sizing
        if (string.IsNullOrWhiteSpace(grid.Columns) && string.IsNullOrWhiteSpace(grid.Rows) 
            && UnitValue.Parse(grid.Width).Type == UnitType.Auto && UnitValue.Parse(grid.Height).Type == UnitType.Auto)
        {
            float maxWidth = 0;
            float maxHeight = 0;

            foreach (var gridChild in grid.GridChildren)
            {
                gridChild.Element.CurrentFontSize = grid.CurrentFontSize;
                // Measure with effectively unlimited space to get content-driven size
                MeasureElement(gridChild.Element, float.MaxValue, float.MaxValue);
                maxWidth = Math.Max(maxWidth, gridChild.Element.ActualWidth);
                maxHeight = Math.Max(maxHeight, gridChild.Element.ActualHeight);
            }

            grid.MeasuredContentWidth = maxWidth;
            grid.MeasuredContentHeight = maxHeight;
        }
        else
        {
            var columnWidths = ParseGridUnits(grid.Columns, availableWidth, grid);
            var rowHeights = ParseGridUnits(grid.Rows, availableHeight, grid);

            foreach (var gridChild in grid.GridChildren)
            {
                var childWidth = GetGridSpanWidth(columnWidths, gridChild.Column, gridChild.ColumnSpan);
                var childHeight = GetGridSpanHeight(rowHeights, gridChild.Row, gridChild.RowSpan);

                gridChild.Element.CurrentFontSize = grid.CurrentFontSize;
                MeasureElement(gridChild.Element, childWidth, childHeight);
            }

            grid.MeasuredContentWidth = columnWidths.Sum();
            grid.MeasuredContentHeight = rowHeights.Sum();
        }
    }

    private static void MeasureDock(Dock dock, float availableWidth, float availableHeight)
    {
        foreach (var dockChild in dock.DockChildren)
        {
            dockChild.Element.CurrentFontSize = dock.CurrentFontSize;
            MeasureElement(dockChild.Element, availableWidth, availableHeight);
        }

        dock.MeasuredContentWidth = availableWidth;
        dock.MeasuredContentHeight = availableHeight;
    }

    private static void MeasureOverlay(Overlay overlay, float availableWidth, float availableHeight)
    {
        // Overlays should fill available space, BUT:
        // When measured with float.MaxValue (auto-sized parent), measure content instead
        // When measured with actual size (explicit parent dimensions), use that space
        
        float contentWidth;
        float contentHeight;

        // measure to content size
        if (availableWidth == float.MaxValue)
        {
            float maxWidth = 0;
            float maxHeight = 0;
            foreach (var child in overlay.Children)
            {
                child.CurrentFontSize = overlay.CurrentFontSize;
                MeasureElement(child, float.MaxValue, float.MaxValue);
                maxWidth = Math.Max(maxWidth, child.ActualWidth);
                maxHeight = Math.Max(maxHeight, child.ActualHeight);
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
                MeasureElement(child, contentWidth, contentHeight);
            }
        }

        // Guarantee overlays always get valid size for mapping
        if (float.IsNaN(contentWidth) || contentWidth == 0)
            contentWidth = availableWidth;
        if (float.IsNaN(contentHeight) || contentHeight == 0)
            contentHeight = availableHeight;
        overlay.MeasuredContentWidth = contentWidth;
        overlay.MeasuredContentHeight = contentHeight;
    }

    private static void MeasureDiv(Div div, float availableWidth, float availableHeight)
    {
        bool hasExplicitlyPositionedChildren = div.Children.Any(c => UnitValue.Parse(c.X).Type != UnitType.None || UnitValue.Parse(c.Y).Type != UnitType.None);

        if (!hasExplicitlyPositionedChildren && div.Children.Count > 0)
        {
            MeasureVerticalDiv(div, availableWidth, availableHeight);
        }
        else
        {
            MeasureStandardDiv(div, availableWidth, availableHeight);
        }
    }

    private static void MeasureVerticalDiv(Div div, float availableWidth, float availableHeight)
    {
        float maxWidth = 0;
        float totalHeight = 0;
        var elements = new List<UIElement>();
        float spacing = div.Spacing;
        var totalSpacing = Math.Max(0, spacing * (div.Children.Count - 1));

        // Resolve unspecified width for Div (default to 1fr)
        var width = UnitValue.Parse(div.Width);
        if (width.Type == UnitType.None)
            div.Width = (width = UnitValue.OneFR).ToString();
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
                totalHeight += child.ActualHeight;
                maxWidth = Math.Max(maxWidth, child.ActualWidth);
            }
        }

        // Resolve FractionalUnit heights
        if (elements.Count > 0)
        {
            var remainingHeight = availableHeight - totalHeight - totalSpacing;
            ResolveFractionalUnitHeights(elements, Math.Max(0, remainingHeight), availableWidth);

            foreach (var element in elements)
            {
                totalHeight += element.ActualHeight;
                maxWidth = Math.Max(maxWidth, element.ActualWidth);
            }
        }

        // Preserve explicit dimensions
        if (width.Type != UnitType.None && width.Type != UnitType.Auto)
        {
            // Keep explicit width
        }
        else
        {
            div.MeasuredContentWidth = Math.Max(maxWidth, availableWidth <= 0 || availableWidth == float.MaxValue ? 0 : availableWidth);
        }

        if (height.Type != UnitType.None && height.Type != UnitType.Auto)
        {
            // Keep explicit height
        }
        else
        {
            div.MeasuredContentHeight = totalHeight + totalSpacing;
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
            maxWidth = Math.Max(maxWidth, child.ActualWidth);
            maxHeight = Math.Max(maxHeight, child.ActualHeight);
        }

        var width = UnitValue.Parse(div.Width);
        var height = UnitValue.Parse(div.Height);
        if (width.Type == UnitType.Auto && height.Type == UnitType.Auto)
        {
            div.MeasuredContentWidth = maxWidth > 0 ? maxWidth : availableWidth;
            div.MeasuredContentHeight = maxHeight > 0 ? maxHeight : availableHeight;
        }
    }

    private static void PositionStack(Stack stack)
    {
        stack.ActualX = 0;
        stack.ActualY = 0;

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
            totalWidth += child.ActualWidth;

        if (stack.Children.Count > 1)
            totalWidth += stack.Spacing * (stack.Children.Count - 1);

        float currentX = stack.ActualX + stack.ComputedPaddingLeft + 
            CalculateHorizontalAlignmentOffset(stack.MeasuredContentWidth, totalWidth, stack.ContentHorizontalAlignment);
        float baseY = stack.ActualY + stack.ComputedPaddingTop;

        foreach (var child in stack.Children)
        {
            child.ActualX = currentX;
            child.ActualY = baseY;
            ApplyVerticalAlignment(child, baseY, stack.MeasuredContentHeight);
            currentX += child.ActualWidth + stack.Spacing;
        }
    }

    private static void PositionVerticalStack(Stack stack)
    {
        float baseX = stack.ActualX + stack.ComputedPaddingLeft;
        
        float totalHeight = 0;
        foreach (var child in stack.Children)
            totalHeight += child.ActualHeight;

        if (stack.Children.Count > 1)
            totalHeight += stack.Spacing * (stack.Children.Count - 1);

        float currentY = stack.ActualY + stack.ComputedPaddingTop + 
            CalculateVerticalAlignmentOffset(stack.MeasuredContentHeight, totalHeight, stack.ContentVerticalAlignment);

        foreach (var child in stack.Children)
        {
            child.ActualX = baseX;
            child.ActualY = currentY;
            ApplyHorizontalAlignment(child, baseX, stack.MeasuredContentWidth);
            currentY += child.ActualHeight + stack.Spacing;
        }
    }

    private static void PositionGrid(Grid grid)
    {
        grid.ActualX = 0;
        grid.ActualY = 0;

        var columnWidths = ParseGridUnits(grid.Columns, grid.MeasuredContentWidth, grid);
        var rowHeights = ParseGridUnits(grid.Rows, grid.MeasuredContentHeight, grid);

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

            var cellWidth = GetGridSpanWidth(columnWidths, gridChild.Column, gridChild.ColumnSpan);
            var cellHeight = GetGridSpanHeight(rowHeights, gridChild.Row, gridChild.RowSpan);

            gridChild.Element.ActualX = x;
            gridChild.Element.ActualY = y;

            ApplyHorizontalAlignment(gridChild.Element, x, cellWidth);
            ApplyVerticalAlignment(gridChild.Element, y, cellHeight);
        }
    }

    private static void PositionDock(Dock dock)
    {
        dock.ActualX = 0;
        dock.ActualY = 0;

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
        else if (UnitValue.Parse(div.X).Type != UnitType.None || UnitValue.Parse(div.Y).Type != UnitType.None)
        {
            div.ActualX = div.ToPixels(div.X);
            div.ActualY = div.ToPixels(div.Y);
        }
        else
        {
            div.ActualX = 0;
            div.ActualY = 0;
        }

        bool hasExplicitlyPositionedChildren = div.Children.Any(c => UnitValue.Parse(c.X).Type != UnitType.None || UnitValue.Parse(c.Y).Type != UnitType.None);

        if (!hasExplicitlyPositionedChildren && div.Children.Count > 0)
        {
            PositionVerticalDiv(div);
        }
    }

    private static void PositionOverlay(Overlay overlay)
    {
        overlay.ActualX = 0;
        overlay.ActualY = 0;

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
            totalHeight += child.ActualHeight;

        if (div.Children.Count > 1)
            totalHeight += div.Spacing * (div.Children.Count - 1);

        float currentY = div.ActualY + div.ComputedPaddingTop + 
            CalculateVerticalAlignmentOffset(div.MeasuredContentWidth, totalHeight, div.ContentVerticalAlignment);

        foreach (var child in div.Children)
        {
            child.ActualX = baseX;
            child.ActualY = currentY;
            ApplyHorizontalAlignment(child, baseX, div.MeasuredContentWidth);
            currentY += child.ActualHeight + div.Spacing;
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

    private static void ApplyHorizontalAlignment(UIElement element, float baseX, float containerWidth)
    {
        var alignment = HorizontalAlignment.Parse(element.HorizontalAlignment);
        if (alignment == HorizontalAlignment.Unspecified && element.Parent != null)
        {
            alignment = HorizontalAlignment.Parse(element.Parent.ContentHorizontalAlignment);
        }
        
        if (alignment == HorizontalAlignment.Unspecified)
            alignment = HorizontalAlignment.Left;

        switch (alignment)
        {
            case HorizontalAlignment.Left:
                element.ActualX = baseX;
                break;
            case HorizontalAlignment.Center:
                element.ActualX = baseX + (containerWidth - element.ActualWidth) / 2;
                break;
            case HorizontalAlignment.Right:
                element.ActualX = baseX + containerWidth - element.ActualWidth;
                break;
        }
    }

    private static void ApplyVerticalAlignment(UIElement element, float baseY, float containerHeight)
    {
        var alignment = VerticalAlignment.Parse(element.VerticalAlignment);
        if (alignment == VerticalAlignment.Unspecified && element.Parent != null)
        {
            alignment = VerticalAlignment.Parse(element.Parent.ContentVerticalAlignment);
        }

        if (alignment == VerticalAlignment.Unspecified)
            alignment = VerticalAlignment.Top;

        switch (alignment)
        {
            case VerticalAlignment.Top:
                element.ActualY = baseY;
                break;
            case VerticalAlignment.Center:
                element.ActualY = baseY + (containerHeight - element.ActualHeight) / 2;
                break;
            case VerticalAlignment.Bottom:
                element.ActualY = baseY + containerHeight - element.ActualHeight;
                break;
        }
    }

    private static float CalculateHorizontalAlignmentOffset(float containerSize, float contentSize, string? alignmentString)
    {
        var alignment = HorizontalAlignment.Parse(alignmentString);
        if (alignment == HorizontalAlignment.Center) return Math.Max(0, (containerSize - contentSize) / 2);
        if (alignment == HorizontalAlignment.Right) return Math.Max(0, containerSize - contentSize);
        return 0; // Default or Left or Unspecified
    }

    private static float CalculateVerticalAlignmentOffset(float containerSize, float contentSize, string? alignmentString)
    {
        var alignment = VerticalAlignment.Parse(alignmentString);
        if (alignment == VerticalAlignment.Center) return Math.Max(0, (containerSize - contentSize) / 2);
        if (alignment == VerticalAlignment.Bottom) return Math.Max(0, containerSize - contentSize);
        return 0; // Default or Top or Unspecified
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

    private static float[] ParseGridUnits(string? unitsString, float totalSize, Grid elem)
    {
        if (string.IsNullOrWhiteSpace(unitsString))
            return [totalSize];

        var parts = unitsString.Split([','], StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim())
            .ToArray();

        if (parts.Length == 0)
            return [totalSize];

        var result = new float[parts.Length];
        var frUnits = new List<UnitValue>();
        float fixedSize = 0;

        for (int i = 0; i < parts.Length; i++)
        {
            var unit = UnitValue.Parse(parts[i]);
            if (unit.Type == UnitType.Fr)
            {
                frUnits.Add(unit);
            }
            else
            {
                result[i] = elem.ToPixels(unit);
                fixedSize += result[i];
            }
        }

        float remainingSpace = Math.Max(0, totalSize - fixedSize);
        if (frUnits.Count > 0)
        {
            var values = frUnits.Select(u => u.Value).ToArray();
            var resolvedValues = FractionalUnitResolver.ResolveFractionalUnits(values, remainingSpace);

            int index = 0;
            for (int i = 0; i < parts.Length; i++)
            {
                var unit = UnitValue.Parse(parts[i]);
                if (unit.Type == UnitType.Fr)
                {
                    result[i] = resolvedValues[index++];
                }
            }
        }

        return result;
    }

    private static float GetGridSpanWidth(float[] columnWidths, int column, int columnSpan)
    {
        float width = 0;
        for (int i = column; i < column + columnSpan && i < columnWidths.Length; i++)
        {
            width += columnWidths[i];
        }
        return width;
    }

    private static float GetGridSpanHeight(float[] rowHeights, int row, int rowSpan)
    {
        float height = 0;
        for (int i = row; i < row + rowSpan && i < rowHeights.Length; i++)
        {
            height += rowHeights[i];
        }
        return height;
    }
}
