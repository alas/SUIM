namespace SUIM.Layout;

using SUIM.Components;

public class LayoutEngine
{
    public LayoutResult Layout(UIElement root, LayoutContext context)
    {
        root.CurrentFontSize = context.RootFontSize;
        LayoutInternal(root, context.AvailableWidth, context.AvailableHeight, context.RootFontSize);
        
        // Return a LayoutResult based on root's calculated size
        return new LayoutResult
        {
            X = root.ActualX,
            Y = root.ActualY,
            Width = root.ActualWidth,
            Height = root.ActualHeight,
            ContentWidth = root.MeasuredContentWidth,
            ContentHeight = root.MeasuredContentHeight
        };
    }

    private void LayoutInternal(UIElement root, float availableWidth, float availableHeight, float rootFontSize)
    {
        var context = new LayoutContext(rootFontSize);
        root.CurrentFontSize = rootFontSize;
        MeasureElement(root, availableWidth, availableHeight, context);
        PositionElement(root, 0, 0, availableWidth, availableHeight, context);
        DetectOverflow(root);
    }

    private static bool TreeHasAnyExplicitSize(UIElement element)
    {
        if (element.Width.Type != UnitType.None || element.Height.Type != UnitType.None)
            return true;

        foreach (var child in element.Children)
        {
            if (TreeHasAnyExplicitSize(child)) return true;
        }

        return false;
    }

    private void MeasureElement(UIElement element, float availableWidth, float availableHeight, LayoutContext context)
    {
        // Calculate margins and padding in pixels
        element.ComputedMarginLeft = element.Margin.Left.ToPixels(context);
        element.ComputedMarginTop = element.Margin.Top.ToPixels(context);
        element.ComputedMarginRight = element.Margin.Right.ToPixels(context);
        element.ComputedMarginBottom = element.Margin.Bottom.ToPixels(context);

        element.ComputedPaddingLeft = element.Padding.Left.ToPixels(context);
        element.ComputedPaddingTop = element.Padding.Top.ToPixels(context);
        element.ComputedPaddingRight = element.Padding.Right.ToPixels(context);
        element.ComputedPaddingBottom = element.Padding.Bottom.ToPixels(context);

        // Calculate available space for content
        var availableContentWidth = Math.Max(0, availableWidth - element.ComputedMarginLeft - element.ComputedMarginRight - element.ComputedPaddingLeft - element.ComputedPaddingRight);
        var availableContentHeight = Math.Max(0, availableHeight - element.ComputedMarginTop - element.ComputedMarginBottom - element.ComputedPaddingTop - element.ComputedPaddingBottom);

        // Convert explicit sizes to pixels
        var widthInPixels = element.Width.Type == UnitType.Auto ? 0 : element.Width.ToPixels(context);
        var heightInPixels = element.Height.Type == UnitType.Auto ? 0 : element.Height.ToPixels(context);

        // Initialize content size
        if (element is BaseText)
        {
            element.MeasuredContentWidth = widthInPixels == 0 ? availableContentWidth : widthInPixels;
            element.MeasuredContentHeight = heightInPixels == 0 ? element.CurrentFontSize : heightInPixels;
        }
        else
        {
            element.MeasuredContentWidth = widthInPixels;
            element.MeasuredContentHeight = heightInPixels;
        }

        // Measure children based on element type
        if (element is Stack stack)
        {
            MeasureStack(stack, availableContentWidth, availableContentHeight, context);
        }
        else if (element is Grid grid)
        {
            MeasureGrid(grid, availableContentWidth, availableContentHeight, context);
        }
        else if (element is Dock dock)
        {
            MeasureDock(dock, availableContentWidth, availableContentHeight, context);
        }
        else if (element is Div div)
        {
            MeasureDiv(div, availableContentWidth, availableContentHeight, context);
        }
        else if (element is Window window)
        {
            MeasureWindow(window, availableContentWidth, availableContentHeight, context);
        }
        else
        {
            // Measure children for generic elements
            foreach (var child in element.Children)
            {
                child.CurrentFontSize = element.CurrentFontSize;
                MeasureElement(child, availableContentWidth, availableContentHeight, context);
            }
        }

        // Handle root element sizing
        if (element.Parent == null && !TreeHasAnyExplicitSize(element))
        {
            element.MeasuredContentWidth = availableContentWidth;
            element.MeasuredContentHeight = availableContentHeight;
        }
        else
        {
            // Apply constraints
            if (widthInPixels == 0)
            {
                element.MeasuredContentWidth = Math.Min(element.MeasuredContentWidth, availableContentWidth);
            }
            if (heightInPixels == 0)
            {
                element.MeasuredContentHeight = Math.Min(element.MeasuredContentHeight, availableContentHeight);
            }
        }

        // Calculate total size including padding
        element.ActualWidth = element.MeasuredContentWidth + element.ComputedPaddingLeft + element.ComputedPaddingRight;
        element.ActualHeight = element.MeasuredContentHeight + element.ComputedPaddingTop + element.ComputedPaddingBottom;
    }

    private void PositionElement(UIElement element, float parentX, float parentY, float availableWidth, float availableHeight, LayoutContext context)
    {
        // Position the element itself
        if (element is Stack or Grid or Dock or Div or Window)
        {
            // Container positioning handled by specific methods
        }
        else
        {
            // Default positioning for leaf elements
            element.ActualX = parentX;
            element.ActualY = parentY;
        }

        // Position based on element type
        switch (element)
        {
            case Stack stack:
                PositionStack(stack, context);
                break;
            case Grid grid:
                PositionGrid(grid, context);
                break;
            case Dock dock:
                PositionDock(dock, context);
                break;
            case Div div:
                PositionDiv(div, context);
                break;
            case Window window:
                PositionWindow(window);
                break;
        }

        // Position children
        var contentX = element.ActualX + element.ComputedMarginLeft + element.ComputedPaddingLeft;
        var contentY = element.ActualY + element.ComputedMarginTop + element.ComputedPaddingTop;
        
        foreach (var child in element.Children)
        {
            child.CurrentFontSize = element.CurrentFontSize;
            PositionElement(child, contentX, contentY, element.MeasuredContentWidth, element.MeasuredContentHeight, context);
        }
    }

    private void MeasureStack(Stack stack, float availableWidth, float availableHeight, LayoutContext context)
    {
        var totalSpacing = Math.Max(0, stack.Spacing * (stack.Children.Count - 1));

        if (stack.Orientation == Orientation.Horizontal)
        {
            MeasureHorizontalStack(stack, availableWidth, availableHeight, totalSpacing, context);
        }
        else
        {
            MeasureVerticalStack(stack, availableWidth, availableHeight, totalSpacing, context);
        }
    }

    private void MeasureHorizontalStack(Stack stack, float availableWidth, float availableHeight, float totalSpacing, LayoutContext context)
    {
        float totalWidth = 0;
        float maxHeight = 0;
        var starElements = new List<UIElement>();

        // Measure fixed-size children
        foreach (var child in stack.Children)
        {
            if (child.Width.Type == UnitType.Star || (child.Width.Type == UnitType.None && child is LayoutElement))
            {
                starElements.Add(child);
            }
            else
            {
                child.CurrentFontSize = stack.CurrentFontSize;
                MeasureElement(child, availableWidth, availableHeight, context);
                totalWidth += child.ActualWidth;
                maxHeight = Math.Max(maxHeight, child.ActualHeight);
            }
        }

        // Resolve star widths
        if (starElements.Count > 0)
        {
            var remainingWidth = availableWidth - totalWidth - totalSpacing;
            ResolveStarWidths(starElements, Math.Max(0, remainingWidth), availableHeight, context);

            foreach (var starElement in starElements)
            {
                totalWidth += starElement.ActualWidth;
                maxHeight = Math.Max(maxHeight, starElement.ActualHeight);
            }
        }

        stack.MeasuredContentWidth = totalWidth + totalSpacing;
        stack.MeasuredContentHeight = maxHeight;
    }

    private void MeasureVerticalStack(Stack stack, float availableWidth, float availableHeight, float totalSpacing, LayoutContext context)
    {
        float totalHeight = 0;
        float maxWidth = 0;
        var starElements = new List<UIElement>();

        // Measure fixed-size children
        foreach (var child in stack.Children)
        {
            if (child.Height.Type == UnitType.Star || (child.Height.Type == UnitType.None && child is LayoutElement))
            {
                starElements.Add(child);
            }
            else
            {
                child.CurrentFontSize = stack.CurrentFontSize;
                MeasureElement(child, availableWidth, availableHeight, context);
                totalHeight += child.ActualHeight;
                maxWidth = Math.Max(maxWidth, child.ActualWidth);
            }
        }

        // Resolve star heights
        if (starElements.Count > 0)
        {
            var remainingHeight = availableHeight - totalHeight - totalSpacing;
            ResolveStarHeights(starElements, Math.Max(0, remainingHeight), availableWidth, context);

            foreach (var starElement in starElements)
            {
                totalHeight += starElement.ActualHeight;
                maxWidth = Math.Max(maxWidth, starElement.ActualWidth);
            }
        }

        stack.MeasuredContentWidth = maxWidth;
        stack.MeasuredContentHeight = totalHeight + totalSpacing;
    }

    private void ResolveStarWidths(List<UIElement> starElements, float remainingSpace, float availableHeight, LayoutContext context)
    {
        var starValues = starElements.Select(e => e.Width.Type == UnitType.Star ? e.Width.Value : 1f).ToArray();
        var resolvedValues = StarUnitResolver.ResolveStarUnits(starValues, remainingSpace);

        for (int i = 0; i < starElements.Count; i++)
        {
            starElements[i].CurrentFontSize = (starElements[i].Parent as Stack)?.CurrentFontSize ?? 16f;
            MeasureElement(starElements[i], resolvedValues[i], availableHeight, context);
        }
    }

    private void ResolveStarHeights(List<UIElement> starElements, float remainingSpace, float availableWidth, LayoutContext context)
    {
        var starValues = starElements.Select(e => e.Height.Type == UnitType.Star ? e.Height.Value : 1f).ToArray();
        var resolvedValues = StarUnitResolver.ResolveStarUnits(starValues, remainingSpace);

        for (int i = 0; i < starElements.Count; i++)
        {
            starElements[i].CurrentFontSize = (starElements[i].Parent as Stack)?.CurrentFontSize ?? 16f;
            MeasureElement(starElements[i], availableWidth, resolvedValues[i], context);
        }
    }

    private void MeasureGrid(Grid grid, float availableWidth, float availableHeight, LayoutContext context)
    {
        var columnWidths = ParseGridUnits(grid.Columns, availableWidth);
        var rowHeights = ParseGridUnits(grid.Rows, availableHeight);

        foreach (var gridChild in grid.GridChildren)
        {
            var childWidth = GetGridSpanWidth(columnWidths, gridChild.Column, gridChild.ColumnSpan);
            var childHeight = GetGridSpanHeight(rowHeights, gridChild.Row, gridChild.RowSpan);

            gridChild.Element.CurrentFontSize = grid.CurrentFontSize;
            MeasureElement(gridChild.Element, childWidth, childHeight, context);
        }

        grid.MeasuredContentWidth = columnWidths.Sum();
        grid.MeasuredContentHeight = rowHeights.Sum();
    }

    private void MeasureDock(Dock dock, float availableWidth, float availableHeight, LayoutContext context)
    {
        foreach (var dockChild in dock.DockChildren)
        {
            dockChild.Element.CurrentFontSize = dock.CurrentFontSize;
            MeasureElement(dockChild.Element, availableWidth, availableHeight, context);
        }

        dock.MeasuredContentWidth = availableWidth;
        dock.MeasuredContentHeight = availableHeight;
    }

    private void MeasureDiv(Div div, float availableWidth, float availableHeight, LayoutContext context)
    {
        bool hasExplicitlyPositionedChildren = div.Children.Any(c => c.X.Type != UnitType.None || c.Y.Type != UnitType.None);

        if (!hasExplicitlyPositionedChildren && div.Children.Count > 0)
        {
            MeasureVerticalDiv(div, availableWidth, availableHeight, context);
        }
        else
        {
            MeasureStandardDiv(div, availableWidth, availableHeight, context);
        }
    }

    private void MeasureVerticalDiv(Div div, float availableWidth, float availableHeight, LayoutContext context)
    {
        float maxWidth = 0;
        float totalHeight = 0;
        var starElements = new List<UIElement>();
        int spacing = 0;
        var totalSpacing = Math.Max(0, spacing * (div.Children.Count - 1));

        // Measure fixed-size children
        foreach (var child in div.Children)
        {
            if (child.Height.Type == UnitType.Star || (child.Height.Type == UnitType.None && child is LayoutElement))
            {
                starElements.Add(child);
            }
            else
            {
                child.CurrentFontSize = div.CurrentFontSize;
                MeasureElement(child, availableWidth, availableHeight, context);
                totalHeight += child.ActualHeight;
                maxWidth = Math.Max(maxWidth, child.ActualWidth);
            }
        }

        // Resolve star heights
        if (starElements.Count > 0)
        {
            var remainingHeight = availableHeight - totalHeight - totalSpacing;
            ResolveStarHeights(starElements, Math.Max(0, remainingHeight), availableWidth, context);

            foreach (var starElement in starElements)
            {
                totalHeight += starElement.ActualHeight;
                maxWidth = Math.Max(maxWidth, starElement.ActualWidth);
            }
        }

        // Preserve explicit dimensions
        if (div.Width.Type != UnitType.None && div.Width.Type != UnitType.Auto)
        {
            // Keep explicit width
        }
        else
        {
            div.MeasuredContentWidth = maxWidth;
        }

        if (div.Height.Type != UnitType.None && div.Height.Type != UnitType.Auto)
        {
            // Keep explicit height
        }
        else
        {
            div.MeasuredContentHeight = totalHeight + totalSpacing;
        }
    }

    private void MeasureStandardDiv(Div div, float availableWidth, float availableHeight, LayoutContext context)
    {
        float maxWidth = 0;
        float maxHeight = 0;

        foreach (var child in div.Children)
        {
            child.CurrentFontSize = div.CurrentFontSize;
            MeasureElement(child, availableWidth, availableHeight, context);
            maxWidth = Math.Max(maxWidth, child.ActualWidth);
            maxHeight = Math.Max(maxHeight, child.ActualHeight);
        }

        if (div.Width.Type == UnitType.Auto && div.Height.Type == UnitType.Auto)
        {
            div.MeasuredContentWidth = maxWidth > 0 ? maxWidth : availableWidth;
            div.MeasuredContentHeight = maxHeight > 0 ? maxHeight : availableHeight;
        }
    }

    private void MeasureWindow(Window window, float availableWidth, float availableHeight, LayoutContext context)
    {
        float maxWidth = 0;
        float maxHeight = 0;

        foreach (var child in window.Children)
        {
            child.CurrentFontSize = window.CurrentFontSize;
            MeasureElement(child, availableWidth, availableHeight, context);
            maxWidth = Math.Max(maxWidth, child.ActualWidth);
            maxHeight = Math.Max(maxHeight, child.ActualHeight);
        }

        window.MeasuredContentWidth = maxWidth > 0 ? maxWidth : availableWidth;
        window.MeasuredContentHeight = maxHeight > 0 ? maxHeight : availableHeight;
    }

    private void PositionStack(Stack stack, LayoutContext context)
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

    private void PositionHorizontalStack(Stack stack)
    {
        float currentX = stack.ActualX + stack.ComputedPaddingLeft;
        float baseY = stack.ActualY + stack.ComputedPaddingTop;

        foreach (var child in stack.Children)
        {
            child.ActualX = currentX;
            child.ActualY = baseY;
            ApplyVerticalAlignment(child, baseY, stack.MeasuredContentHeight);
            currentX += child.ActualWidth + stack.Spacing;
        }
    }

    private void PositionVerticalStack(Stack stack)
    {
        float baseX = stack.ActualX + stack.ComputedPaddingLeft;
        float currentY = stack.ActualY + stack.ComputedPaddingTop;

        foreach (var child in stack.Children)
        {
            child.ActualX = baseX;
            child.ActualY = currentY;
            ApplyHorizontalAlignment(child, baseX, stack.MeasuredContentWidth);
            currentY += child.ActualHeight + stack.Spacing;
        }
    }

    private void PositionGrid(Grid grid, LayoutContext context)
    {
        grid.ActualX = 0;
        grid.ActualY = 0;

        var columnWidths = ParseGridUnits(grid.Columns, grid.MeasuredContentWidth);
        var rowHeights = ParseGridUnits(grid.Rows, grid.MeasuredContentHeight);

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

            gridChild.Element.ActualX = x;
            gridChild.Element.ActualY = y;
        }
    }

    private void PositionDock(Dock dock, LayoutContext context)
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

    private void PositionDiv(Div div, LayoutContext context)
    {
        if (div.Anchor.HasValue)
        {
            PositionWithAnchor(div, context);
        }
        else if (div.X.Type != UnitType.None || div.Y.Type != UnitType.None)
        {
            div.ActualX = div.X.ToPixels(context);
            div.ActualY = div.Y.ToPixels(context);
        }
        else
        {
            div.ActualX = 0;
            div.ActualY = 0;
        }

        bool hasExplicitlyPositionedChildren = div.Children.Any(c => c.X.Type != UnitType.None || c.Y.Type != UnitType.None);

        if (!hasExplicitlyPositionedChildren && div.Children.Count > 0)
        {
            PositionVerticalDiv(div);
        }
    }

    private void PositionVerticalDiv(Div div)
    {
        float baseX = div.ActualX + div.ComputedPaddingLeft;
        float currentY = div.ActualY + div.ComputedPaddingTop;

        foreach (var child in div.Children)
        {
            child.ActualX = baseX;
            child.ActualY = currentY;
            ApplyHorizontalAlignment(child, baseX, div.MeasuredContentWidth);
            currentY += child.ActualHeight;
        }
    }

    private void PositionWindow(Window window)
    {
        window.ActualX = 0;
        window.ActualY = 0;
    }

    private void PositionWithAnchor(Div div, LayoutContext context)
    {
        // This needs parent context info - for now, assume parent is 640x480
        var parentWidth = 640f;
        var parentHeight = 480f;

        switch (div.Anchor)
        {
            case Anchor.TopLeft:
                div.ActualX = 0;
                div.ActualY = 0;
                break;
            case Anchor.TopRight:
                div.ActualX = parentWidth - div.ActualWidth;
                div.ActualY = 0;
                break;
            case Anchor.BottomLeft:
                div.ActualX = 0;
                div.ActualY = parentHeight - div.ActualHeight;
                break;
            case Anchor.BottomRight:
                div.ActualX = parentWidth - div.ActualWidth;
                div.ActualY = parentHeight - div.ActualHeight;
                break;
            case Anchor.Center:
                div.ActualX = (parentWidth - div.ActualWidth) / 2;
                div.ActualY = (parentHeight - div.ActualHeight) / 2;
                break;
        }
    }

    private void ApplyHorizontalAlignment(UIElement element, float baseX, float containerWidth)
    {
        switch (element.HorizontalAlignment)
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
            case HorizontalAlignment.Stretch:
                element.ActualWidth = containerWidth;
                element.ActualX = baseX;
                break;
        }
    }

    private void ApplyVerticalAlignment(UIElement element, float baseY, float containerHeight)
    {
        switch (element.VerticalAlignment)
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
            case VerticalAlignment.Stretch:
                element.ActualHeight = containerHeight;
                element.ActualY = baseY;
                break;
        }
    }

    private void DetectOverflow(UIElement element)
    {
        if (element.Width.Type != UnitType.None && element.Width.Type != UnitType.Auto &&
            element.Height.Type != UnitType.None && element.Height.Type != UnitType.Auto)
        {
            var context = new LayoutContext(element.CurrentFontSize);
            var explicitWidth = element.Width.ToPixels(context);
            var explicitHeight = element.Height.ToPixels(context);

            element.NeedsHorizontalScroll = element.MeasuredContentWidth > explicitWidth;
            element.NeedsVerticalScroll = element.MeasuredContentHeight > explicitHeight;
        }

        foreach (var child in element.Children)
        {
            DetectOverflow(child);
        }
    }

    private static float[] ParseGridUnits(string? unitsString, float totalSize)
    {
        if (string.IsNullOrWhiteSpace(unitsString))
            return [totalSize];

        var parts = unitsString.Split([','], StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim())
            .ToArray();

        if (parts.Length == 0)
            return [totalSize];

        var result = new float[parts.Length];
        var starUnits = new List<UnitValue>();
        float fixedSize = 0;

        for (int i = 0; i < parts.Length; i++)
        {
            var unit = UnitValue.Parse(parts[i]);
            if (unit.Type == UnitType.Star)
            {
                starUnits.Add(unit);
            }
            else
            {
                result[i] = unit.ToPixels(new LayoutContext(16));
                fixedSize += result[i];
            }
        }

        float remainingSpace = Math.Max(0, totalSize - fixedSize);
        if (starUnits.Count > 0)
        {
            var starValues = starUnits.Select(u => u.Value).ToArray();
            var resolvedValues = StarUnitResolver.ResolveStarUnits(starValues, remainingSpace);

            int starIndex = 0;
            for (int i = 0; i < parts.Length; i++)
            {
                var unit = UnitValue.Parse(parts[i]);
                if (unit.Type == UnitType.Star)
                {
                    result[i] = resolvedValues[starIndex++];
                }
            }
        }

        return result;
    }

    private static float GetGridSpanWidth(float[] columnWidths, int startColumn, int columnSpan)
    {
        float width = 0;
        for (int i = startColumn; i < startColumn + columnSpan && i < columnWidths.Length; i++)
        {
            width += columnWidths[i];
        }
        return width;
    }

    private static float GetGridSpanHeight(float[] rowHeights, int startRow, int rowSpan)
    {
        float height = 0;
        for (int i = startRow; i < startRow + rowSpan && i < rowHeights.Length; i++)
        {
            height += rowHeights[i];
        }
        return height;
    }
}
