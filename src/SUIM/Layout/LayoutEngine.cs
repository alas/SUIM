namespace SUIM.Layout;

using SUIM.Components;

public static class LayoutEngine
{
    public static void Layout(UIElement root, float rootFontSize, float availableWidth, float availableHeight)
    {
        root.CurrentFontSize = root.RootFontSize = rootFontSize;
        MeasureElement(root, availableWidth, availableHeight);
        PositionElement(root, 0, 0);
        DetectOverflow(root);
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
        element.ComputedMarginLeft = element.ToPixels(element.Margin.Left);
        element.ComputedMarginTop = element.ToPixels(element.Margin.Top);
        element.ComputedMarginRight = element.ToPixels(element.Margin.Right);
        element.ComputedMarginBottom = element.ToPixels(element.Margin.Bottom);

        element.ComputedPaddingLeft = element.ToPixels(element.Padding.Left);
        element.ComputedPaddingTop = element.ToPixels(element.Padding.Top);
        element.ComputedPaddingRight = element.ToPixels(element.Padding.Right);
        element.ComputedPaddingBottom = element.ToPixels(element.Padding.Bottom);

        // Calculate available space for content
        var availableContentWidth = Math.Max(0, availableWidth - element.ComputedMarginLeft - element.ComputedMarginRight - element.ComputedPaddingLeft - element.ComputedPaddingRight);
        var availableContentHeight = Math.Max(0, availableHeight - element.ComputedMarginTop - element.ComputedMarginBottom - element.ComputedPaddingTop - element.ComputedPaddingBottom);

        // Convert explicit sizes to pixels
        var widthInPixels = element.Width.Type == UnitType.Auto ? 0 : element.ToPixels(element.Width);
        var heightInPixels = element.Height.Type == UnitType.Auto ? 0 : element.ToPixels(element.Height);

        // Initialize content size
        if (element is Text baseText)
        {
            // Width: if explicit pixels provided use that, if auto use MetricTable, otherwise constrain to available width
            if (element.Width.Type == UnitType.Auto)
            {
                var fontName = element.Font ?? element.RootFont ?? "__default__";
                element.MeasuredContentWidth = MetricTable.MeasureText(baseText.Value ?? string.Empty, fontName, element.CurrentFontSize);
            }
            else
            {
                element.MeasuredContentWidth = widthInPixels == 0 ? availableContentWidth : widthInPixels;
            }

            // Height: auto resolves to single-line height from font metrics. Explicit heights are honored.
            if (element.Height.Type == UnitType.Auto)
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
        else if (element is Div div)
        {
            MeasureDiv(div, availableContentWidth, availableContentHeight);
        }
        else if (element is Window window)
        {
            MeasureWindow(window, availableContentWidth, availableContentHeight);
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
            if (!TreeHasAnyExplicitWidth(element) || element.Width.Type == UnitType.Fr)
                element.MeasuredContentWidth = availableContentWidth;
            else if (widthInPixels == 0)
                element.MeasuredContentWidth = Math.Min(element.MeasuredContentWidth, availableContentWidth);

            if (!TreeHasAnyExplicitHeight(element) || element.Height.Type == UnitType.Fr)
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
        bool widthExplicit = element.Width.Type != UnitType.None && 
                            element.Width.Type != UnitType.Rem && 
                            element.Width.Type != UnitType.Em && 
                            element.Width.Type != UnitType.Fr;
        if (widthExplicit) return true;
        foreach (var child in element.Children)
        {
            if (TreeHasAnyExplicitWidth(child)) return true;
        }
        return false;
    }

    private static bool TreeHasAnyExplicitHeight(UIElement element)
    {
        // Treat any non-None/non-Fr height (including rem/em/auto) as explicit for height axis
        bool heightExplicit = element.Height.Type != UnitType.None && element.Height.Type != UnitType.Fr;
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
                PositionStack(stack);
                break;
            case Grid grid:
                PositionGrid(grid);
                break;
            case Dock dock:
                PositionDock(dock);
                break;
            case Div div:
                PositionDiv(div);
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
            if (child.Width.Type == UnitType.Fr || (child.Width.Type == UnitType.None && child is LayoutElement))
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
            if (child.Height.Type == UnitType.Fr || (child.Height.Type == UnitType.None && child is LayoutElement))
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
        var values = elements.Select(e => e.Width.Type == UnitType.Fr ? e.Width.Value : 1f).ToArray();
        var resolvedValues = FractionalUnitResolver.ResolveFractionalUnits(values, remainingSpace);

        for (int i = 0; i < elements.Count; i++)
        {
            elements[i].CurrentFontSize = (elements[i].Parent as Stack)?.CurrentFontSize ?? 16f;
            MeasureElement(elements[i], resolvedValues[i], availableHeight);
        }
    }

    private static void ResolveFractionalUnitHeights(List<UIElement> elements, float remainingSpace, float availableWidth)
    {
        var values = elements.Select(e => e.Height.Type == UnitType.Fr ? e.Height.Value : 1f).ToArray();
        var resolvedValues = FractionalUnitResolver.ResolveFractionalUnits(values, remainingSpace);

        for (int i = 0; i < elements.Count; i++)
        {
            elements[i].CurrentFontSize = (elements[i].Parent as Stack)?.CurrentFontSize ?? 16f;
            MeasureElement(elements[i], availableWidth, resolvedValues[i]);
        }
    }

    private static void MeasureGrid(Grid grid, float availableWidth, float availableHeight)
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

    private static void MeasureDiv(Div div, float availableWidth, float availableHeight)
    {
        bool hasExplicitlyPositionedChildren = div.Children.Any(c => c.X.Type != UnitType.None || c.Y.Type != UnitType.None);

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
        int spacing = 0;
        var totalSpacing = Math.Max(0, spacing * (div.Children.Count - 1));

        // Measure fixed-size children
        foreach (var child in div.Children)
        {
            if (child.Height.Type == UnitType.Fr || (child.Height.Type == UnitType.None && child is LayoutElement))
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

        if (div.Width.Type == UnitType.Auto && div.Height.Type == UnitType.Auto)
        {
            div.MeasuredContentWidth = maxWidth > 0 ? maxWidth : availableWidth;
            div.MeasuredContentHeight = maxHeight > 0 ? maxHeight : availableHeight;
        }
    }

    private static void MeasureWindow(Window window, float availableWidth, float availableHeight)
    {
        float maxWidth = 0;
        float maxHeight = 0;

        foreach (var child in window.Children)
        {
            child.CurrentFontSize = window.CurrentFontSize;
            MeasureElement(child, availableWidth, availableHeight);
            maxWidth = Math.Max(maxWidth, child.ActualWidth);
            maxHeight = Math.Max(maxHeight, child.ActualHeight);
        }

        window.MeasuredContentWidth = maxWidth > 0 ? maxWidth : availableWidth;
        window.MeasuredContentHeight = maxHeight > 0 ? maxHeight : availableHeight;
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

    private static void PositionVerticalStack(Stack stack)
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

            gridChild.Element.ActualX = x;
            gridChild.Element.ActualY = y;
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
        if (div.Anchor.HasValue && div.Anchor != Anchor.None)
        {
            PositionWithAnchor(div);
        }
        else if (div.X.Type != UnitType.None || div.Y.Type != UnitType.None)
        {
            div.ActualX = div.ToPixels(div.X);
            div.ActualY = div.ToPixels(div.Y);
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

    private static void PositionVerticalDiv(Div div)
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

    private static void PositionWindow(Window window)
    {
        window.ActualX = 0;
        window.ActualY = 0;
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

        var anchor = element.Anchor ?? Anchor.None;
        
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

    private static void ApplyVerticalAlignment(UIElement element, float baseY, float containerHeight)
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

    private static void DetectOverflow(UIElement element)
    {
        if (element.Width.Type != UnitType.None && element.Width.Type != UnitType.Auto &&
            element.Height.Type != UnitType.None && element.Height.Type != UnitType.Auto)
        {
            var explicitWidth = element.ToPixels(element.Width);
            var explicitHeight = element.ToPixels(element.Height);

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
