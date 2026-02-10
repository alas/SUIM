namespace SUIM.Layout;

using SUIM.Components;

public class LayoutEngine
{
    private readonly Dictionary<UIElement, LayoutResult> _layoutResults = [];
    private readonly Dictionary<UIElement, LayoutContext> _layoutContexts = [];
    
    public LayoutResult Layout(UIElement root, LayoutContext context)
    {
        _layoutResults.Clear();
        _layoutContexts.Clear();
        MeasureElement(root, context);
        PositionElement(root, context);

        return FinalizeElement(root, context);
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

    private void MeasureElement(UIElement element, LayoutContext context)
    {
        // First, recursively measure all children
        foreach (var child in element.Children)
        {
            var childContext = CreateChildContext(context);
            MeasureElement(child, childContext);
        }
        
        var result = new LayoutResult();
        
        // Convert units to pixels
        var widthInPixels = element.Width.Type == UnitType.Auto ? 0 : element.Width.ToPixels(context);
        var heightInPixels = element.Height.Type == UnitType.Auto ? 0 : element.Height.ToPixels(context);
        
        // Convert margin and padding to pixels
        var marginLeft = element.Margin.Left.ToPixels(context);
        var marginTop = element.Margin.Top.ToPixels(context);
        var marginRight = element.Margin.Right.ToPixels(context);
        var marginBottom = element.Margin.Bottom.ToPixels(context);
        
        var paddingLeft = element.Padding.Left.ToPixels(context);
        var paddingTop = element.Padding.Top.ToPixels(context);
        var paddingRight = element.Padding.Right.ToPixels(context);
        var paddingBottom = element.Padding.Bottom.ToPixels(context);
        
        // Calculate content area
        var availableWidth = Math.Max(0, context.AvailableWidth - marginLeft - marginRight - paddingLeft - paddingRight);
        var availableHeight = Math.Max(0, context.AvailableHeight - marginTop - marginBottom - paddingTop - paddingBottom);
        
        // Initialize content size with explicit sizes if provided
        // Default measurement for content elements
        if (element is BaseText)
        {
            // If no explicit width, use available width so text fills its container
            result.ContentWidth = widthInPixels == 0 ? availableWidth : widthInPixels;
            // If no explicit height, use current font size as a reasonable default
            result.ContentHeight = heightInPixels == 0 ? context.CurrentFontSize : heightInPixels;
        }
        else
        {
            result.ContentWidth = widthInPixels;
            result.ContentHeight = heightInPixels;
        }
        
        // Store result early so measurement methods can update it
        _layoutResults[element] = result;
        _layoutContexts[element] = context;
        
        // Measure children based on element type
        if (element is Stack stack)
        {
            MeasureStack(stack, availableWidth, availableHeight, context);
        }
        else if (element is Grid grid)
        {
            MeasureGrid(grid, availableWidth, availableHeight, context);
        }
        else if (element is Dock dock)
        {
            MeasureDock(dock, availableWidth, availableHeight, context);
        }
        else if (element is Div div)
        {
            MeasureDiv(div, availableWidth, availableHeight, context);
        }
        else if (element is Window window)
        {
            MeasureWindow(window, availableWidth, availableHeight, context);
        }
        
        // Retrieve the result (which may have been updated by measurement methods)
        if (_layoutResults.TryGetValue(element, out var updatedResult))
        {
            result = updatedResult;
        }

        // If this is the root element and no explicit sizes are present in the tree,
        // the root should take the full available space (treats undefined sizing as "*").
        if (element.Parent == null && !TreeHasAnyExplicitSize(element))
        {
            result.ContentWidth = availableWidth;
            result.ContentHeight = availableHeight;
        }
        else
        {
            // Apply constraints (but don't override explicit sizes)
            if (widthInPixels == 0)
            {
                result.ContentWidth = Math.Min(result.ContentWidth, availableWidth);
            }
            if (heightInPixels == 0)
            {
                result.ContentHeight = Math.Min(result.ContentHeight, availableHeight);
            }
        }
        
        // Calculate total size including padding
        result.Width = result.ContentWidth + paddingLeft + paddingRight;
        result.Height = result.ContentHeight + paddingTop + paddingBottom;
        
        // Store final result
        _layoutResults[element] = result;
    }
    
    private void PositionElement(UIElement element, LayoutContext context)
    {
        if (!_layoutResults.TryGetValue(element, out var result))
            return;
            
        // Position based on element type and alignment
        if (element is Stack stack)
        {
            PositionStack(stack, result);
        }
        else if (element is Grid grid)
        {
            PositionGrid(grid, result);
        }
        else if (element is Dock dock)
        {
            PositionDock(dock, result);
        }
        else if (element is Div div)
        {
            PositionDiv(div, result);
        }
        else if (element is Window)
        {
            PositionWindow(result);
        }
        else
        {
            // Default positioning
            result.X = 0;
            result.Y = 0;
        }
        
        // Update stored result
        _layoutResults[element] = result;
        
        // Recursively position children
        foreach (var child in element.Children)
        {
            var childContext = CreateChildContext(context);
            PositionElement(child, childContext);
        }
    }

    private LayoutResult FinalizeElement(UIElement root, LayoutContext context)
    {
        // Ensure the root element has been measured and positioned
        if (!_layoutResults.TryGetValue(root, out var rootResult))
        {
            // If root wasn't measured (shouldn't happen with recursive measurement), measure it directly
            var result = new LayoutResult();

            // Convert units to pixels
            var widthInPixels = root.Width.Type == UnitType.Auto ? 0 : root.Width.ToPixels(context);
            var heightInPixels = root.Height.Type == UnitType.Auto ? 0 : root.Height.ToPixels(context);

            // Convert margin and padding to pixels
            var marginLeft = root.Margin.Left.ToPixels(context);
            var marginTop = root.Margin.Top.ToPixels(context);
            var marginRight = root.Margin.Right.ToPixels(context);
            var marginBottom = root.Margin.Bottom.ToPixels(context);

            var paddingLeft = root.Padding.Left.ToPixels(context);
            var paddingTop = root.Padding.Top.ToPixels(context);
            var paddingRight = root.Padding.Right.ToPixels(context);
            var paddingBottom = root.Padding.Bottom.ToPixels(context);

            // Calculate content area
            var availableWidth = Math.Max(0, context.AvailableWidth - marginLeft - marginRight - paddingLeft - paddingRight);
            var availableHeight = Math.Max(0, context.AvailableHeight - marginTop - marginBottom - paddingTop - paddingBottom);

            // Default measurement for content elements
            result.ContentWidth = widthInPixels;
            result.ContentHeight = heightInPixels;

            // Apply constraints
            result.ContentWidth = Math.Min(result.ContentWidth, availableWidth);
            result.ContentHeight = Math.Min(result.ContentHeight, availableHeight);

            // Calculate total size including padding
            result.Width = result.ContentWidth + paddingLeft + paddingRight;
            result.Height = result.ContentHeight + paddingTop + paddingBottom;

            // Store result
            _layoutResults[root] = result;
            _layoutContexts[root] = context;

            rootResult = result;
        }

        ApplyResultsToElements();

        return rootResult;
    }

    private void ApplyResultsToElements()
    {
        foreach (var kv in _layoutResults)
        {
            var element = kv.Key;
            var res = kv.Value;

            element.ActualX = res.X;
            element.ActualY = res.Y;
            element.ActualWidth = res.Width;
            element.ActualHeight = res.Height;
        }
    }

    private static LayoutContext CreateChildContext(LayoutContext parentContext)
    {
        // For child context, use the available space from parent context
        // The parent's content area is the available space for children
        var childContext = new LayoutContext(
            parentContext.RootFontSize,
            parentContext.AvailableWidth,
            parentContext.AvailableHeight)
        {
            CurrentFontSize = parentContext.CurrentFontSize
        };
        
        return childContext;
    }
    
    private void MeasureStack(Stack stack, float availableWidth, float availableHeight, LayoutContext context)
    {
        var totalSpacing = Math.Max(0, stack.Spacing * (stack.Children.Count - 1));
        
        if (stack.Orientation == Orientation.Horizontal)
        {
            // Horizontal stack: sum of widths, max of heights
            float totalWidth = 0;
            float maxHeight = 0;
            var starElements = new List<UIElement>();
            
            foreach (var child in stack.Children)
            {
                if (child.Width.Type == UnitType.Star || (child.Width.Type == UnitType.None && child is LayoutElement))
                {
                    starElements.Add(child);
                }
                else
                {
                    var childContext = new LayoutContext(context.RootFontSize, availableWidth, availableHeight);
                    MeasureElement(child, childContext);
                    if (_layoutResults.TryGetValue(child, out var childResult))
                    {
                        totalWidth += childResult.Width;
                        maxHeight = Math.Max(maxHeight, childResult.Height);
                    }
                }
            }
            
            // Handle star units for proportional width
            if (starElements.Count > 0)
            {
                var remainingWidth = availableWidth - totalWidth - totalSpacing;
                ResolveStarWidths(starElements, Math.Max(0, remainingWidth));
                
                // Re-measure star elements using their resolved widths so their children/layout adapt
                foreach (var starElement in starElements)
                {
                    if (_layoutResults.TryGetValue(starElement, out var starResult))
                    {
                        var childContext = new LayoutContext(context.RootFontSize, starResult.Width, availableHeight);
                        MeasureElement(starElement, childContext);
                    }
                }

                // Update maxHeight for star elements after re-measure
                foreach (var starElement in starElements)
                {
                    if (_layoutResults.TryGetValue(starElement, out var starResult))
                    {
                        maxHeight = Math.Max(maxHeight, starResult.Height);
                    }
                }
            }
            
            // Calculate final width including spacing and star elements
            float finalWidth = totalWidth + totalSpacing;
            foreach (var starElement in starElements)
            {
                if (_layoutResults.TryGetValue(starElement, out var starResult))
                {
                    finalWidth += starResult.Width;
                }
            }
            
            // Set the stack's content size
            if (_layoutResults.TryGetValue(stack, out var stackResult))
            {
                stackResult.ContentWidth = finalWidth;
                stackResult.ContentHeight = maxHeight;
                _layoutResults[stack] = stackResult;
            }
        }
        else
        {
            // Vertical stack: max of widths, sum of heights
            float maxWidth = 0;
            float totalHeight = 0;
            var starElements = new List<UIElement>();
            
            foreach (var child in stack.Children)
            {
                if (child.Height.Type == UnitType.Star || (child.Height.Type == UnitType.None && child is LayoutElement))
                {
                    starElements.Add(child);
                }
                else
                {
                    var childContext = new LayoutContext(context.RootFontSize, availableWidth, availableHeight);
                    MeasureElement(child, childContext);
                    if (_layoutResults.TryGetValue(child, out var childResult))
                    {
                        totalHeight += childResult.Height;
                        maxWidth = Math.Max(maxWidth, childResult.Width);
                    }
                }
            }
            
            // Handle star units for proportional height
            if (starElements.Count > 0)
            {
                var remainingHeight = availableHeight - totalHeight - totalSpacing;
                ResolveStarHeights(starElements, Math.Max(0, remainingHeight));
                
                // Re-measure star elements using their resolved heights so their children/layout adapt
                foreach (var starElement in starElements)
                {
                    if (_layoutResults.TryGetValue(starElement, out var starResult))
                    {
                        var childContext = new LayoutContext(context.RootFontSize, availableWidth, starResult.Height);
                        MeasureElement(starElement, childContext);
                    }
                }

                // Update maxWidth for star elements after re-measure
                foreach (var starElement in starElements)
                {
                    if (_layoutResults.TryGetValue(starElement, out var starResult))
                    {
                        maxWidth = Math.Max(maxWidth, starResult.Width);
                    }
                }
            }
            
            // Calculate final height including spacing and star elements
            float finalHeight = totalHeight + totalSpacing;
            foreach (var starElement in starElements)
            {
                if (_layoutResults.TryGetValue(starElement, out var starResult))
                {
                    finalHeight += starResult.Height;
                }
            }
            
            // Set the stack's content size
            if (_layoutResults.TryGetValue(stack, out var stackResult))
            {
                stackResult.ContentWidth = maxWidth;
                stackResult.ContentHeight = finalHeight;
                _layoutResults[stack] = stackResult;
            }
        }
    }
    
    private void MeasureGrid(Grid grid, float availableWidth, float availableHeight, LayoutContext context)
    {
        // Parse grid columns and rows
        var columnWidths = ParseGridUnits(grid.Columns, availableWidth);
        var rowHeights = ParseGridUnits(grid.Rows, availableHeight);
        
        // Measure each grid child
        foreach (var gridChild in grid.GridChildren)
        {
            var childWidth = GetGridSpanWidth(columnWidths, gridChild.Column, gridChild.ColumnSpan);
            var childHeight = GetGridSpanHeight(rowHeights, gridChild.Row, gridChild.RowSpan);
            
            var childContext = new LayoutContext(context.RootFontSize, childWidth, childHeight);
            MeasureElement(gridChild.Element, childContext);
        }
        
        // Calculate total grid size
        float totalWidth = columnWidths.Sum();
        float totalHeight = rowHeights.Sum();
        
        // Set the grid's content size
        if (_layoutResults.TryGetValue(grid, out var gridResult))
        {
            gridResult.ContentWidth = totalWidth;
            gridResult.ContentHeight = totalHeight;
            _layoutResults[grid] = gridResult;
        }
    }
    
    private void MeasureDock(Dock dock, float availableWidth, float availableHeight, LayoutContext context)
    {
        // Measure docked elements first
        foreach (var dockChild in dock.DockChildren)
        {
            var childContext = new LayoutContext(context.RootFontSize, availableWidth, availableHeight);
            MeasureElement(dockChild.Element, childContext);
        }
        
        // Calculate remaining space for last child
        var remainingWidth = availableWidth;
        var remainingHeight = availableHeight;
        
        // Subtract docked elements space
        foreach (var dockChild in dock.DockChildren)
        {
            if (dockChild.Edge == DockEdge.Left || dockChild.Edge == DockEdge.Right)
            {
                remainingWidth -= _layoutResults[dockChild.Element].Width;
            }
            else if (dockChild.Edge == DockEdge.Top || dockChild.Edge == DockEdge.Bottom)
            {
                remainingHeight -= _layoutResults[dockChild.Element].Height;
            }
        }
        
        // Measure remaining space for last child if needed
        // Set the dock's content size to the available space
        if (_layoutResults.TryGetValue(dock, out var dockResult))
        {
            dockResult.ContentWidth = availableWidth;
            dockResult.ContentHeight = availableHeight;
            _layoutResults[dock] = dockResult;
        }
    }
    
    private void MeasureDiv(Div div, float availableWidth, float availableHeight, LayoutContext context)
    {
        // Measure children
        foreach (var child in div.Children)
        {
            var childContext = new LayoutContext(context.RootFontSize, availableWidth, availableHeight);
            MeasureElement(child, childContext);
        }
        
        // Only update content size if div doesn't have explicit dimensions
        if (_layoutResults.TryGetValue(div, out var divResult))
        {
            if (div.Width.Type == UnitType.Auto && div.Height.Type == UnitType.Auto)
            {
                // Size to children
                float maxWidth = 0;
                float maxHeight = 0;
                
                foreach (var child in div.Children)
                {
                    if (_layoutResults.TryGetValue(child, out var childResult))
                    {
                        maxWidth = Math.Max(maxWidth, childResult.Width);
                        maxHeight = Math.Max(maxHeight, childResult.Height);
                    }
                }
                
                divResult.ContentWidth = maxWidth > 0 ? maxWidth : availableWidth;
                divResult.ContentHeight = maxHeight > 0 ? maxHeight : availableHeight;
            }
            // If has explicit dimensions, they're already set in MeasureElement
            _layoutResults[div] = divResult;
        }
    }
    
    private void MeasureWindow(Window window, float availableWidth, float availableHeight, LayoutContext context)
    {
        // Measure children
        foreach (var child in window.Children)
        {
            var childContext = new LayoutContext(context.RootFontSize, availableWidth, availableHeight);
            MeasureElement(child, childContext);
        }
        
        // Window measures its children to determine its own size
        float maxWidth = 0;
        float maxHeight = 0;
        
        foreach (var child in window.Children)
        {
            if (_layoutResults.TryGetValue(child, out var childResult))
            {
                maxWidth = Math.Max(maxWidth, childResult.Width);
                maxHeight = Math.Max(maxHeight, childResult.Height);
            }
        }
        
        if (_layoutResults.TryGetValue(window, out var windowResult))
        {
            windowResult.ContentWidth = maxWidth > 0 ? maxWidth : availableWidth;
            windowResult.ContentHeight = maxHeight > 0 ? maxHeight : availableHeight;
            _layoutResults[window] = windowResult;
        }
    }
    
    private void PositionStack(Stack stack, LayoutResult result)
    {
        if (stack.Orientation == Orientation.Horizontal)
        {
            PositionHorizontalStack(stack, result);
        }
        else
        {
            PositionVerticalStack(stack, result);
        }
    }
    
    private void PositionHorizontalStack(Stack stack, LayoutResult result)
    {
        float currentX = result.GetContentX();
        
        foreach (var child in stack.Children)
        {
            if (_layoutResults.TryGetValue(child, out var childResult))
            {
                childResult.X = currentX;
                childResult.Y = result.GetContentY();
                
                // Apply vertical alignment
                ApplyVerticalAlignment(child, childResult, result.ContentHeight);
                
                currentX += childResult.Width + stack.Spacing;
                _layoutResults[child] = childResult;
            }
        }
    }
    
    private void PositionVerticalStack(Stack stack, LayoutResult result)
    {
        float currentY = result.GetContentY();
        
        foreach (var child in stack.Children)
        {
            if (_layoutResults.TryGetValue(child, out var childResult))
            {
                childResult.X = result.GetContentX();
                childResult.Y = currentY;
                
                // Apply horizontal alignment
                ApplyHorizontalAlignment(child, childResult, result.ContentWidth);
                
                currentY += childResult.Height + stack.Spacing;
                _layoutResults[child] = childResult;
            }
        }
    }
    
    private void PositionGrid(Grid grid, LayoutResult result)
    {
        var columnWidths = ParseGridUnits(grid.Columns, result.ContentWidth);
        var rowHeights = ParseGridUnits(grid.Rows, result.ContentHeight);
        
        foreach (var gridChild in grid.GridChildren)
        {
            if (_layoutResults.TryGetValue(gridChild.Element, out var childResult))
            {
                float x = result.GetContentX();
                float y = result.GetContentY();
                
                // Calculate position based on grid coordinates
                for (int i = 0; i < gridChild.Column; i++)
                {
                    x += columnWidths[i];
                }
                
                for (int i = 0; i < gridChild.Row; i++)
                {
                    y += rowHeights[i];
                }
                
                childResult.X = x;
                childResult.Y = y;
                
                _layoutResults[gridChild.Element] = childResult;
            }
        }
    }
    
    private void PositionDock(Dock dock, LayoutResult result)
    {
        float left = result.GetContentX();
        float top = result.GetContentY();
        float right = result.GetContentRight();
        float bottom = result.GetContentBottom();
        
        foreach (var dockChild in dock.DockChildren)
        {
            if (_layoutResults.TryGetValue(dockChild.Element, out var childResult))
            {
                switch (dockChild.Edge)
                {
                    case DockEdge.Left:
                        childResult.X = left;
                        childResult.Y = top;
                        childResult.Width = _layoutResults[dockChild.Element].Width;
                        childResult.Height = bottom - top;
                        left += childResult.Width;
                        break;
                    case DockEdge.Right:
                        childResult.X = right - _layoutResults[dockChild.Element].Width;
                        childResult.Y = top;
                        childResult.Width = _layoutResults[dockChild.Element].Width;
                        childResult.Height = bottom - top;
                        right = childResult.X;
                        break;
                    case DockEdge.Top:
                        childResult.X = left;
                        childResult.Y = top;
                        childResult.Width = right - left;
                        childResult.Height = _layoutResults[dockChild.Element].Height;
                        top += childResult.Height;
                        break;
                    case DockEdge.Bottom:
                        childResult.X = left;
                        childResult.Y = bottom - _layoutResults[dockChild.Element].Height;
                        childResult.Width = right - left;
                        childResult.Height = _layoutResults[dockChild.Element].Height;
                        bottom = childResult.Y;
                        break;
                }
                
                _layoutResults[dockChild.Element] = childResult;
            }
        }
    }
    
    private void PositionDiv(Div div, LayoutResult result)
    {
        if (div.Anchor.HasValue)
        {
            PositionWithAnchor(div, result);
        }
        else if (div.X != UnitValue.None || div.Y != UnitValue.None)
        {
            result.X = div.X.Value;
            result.Y = div.Y.Value;
        }
        else
        {
            // Default positioning
            result.X = 0;
            result.Y = 0;
        }
    }
    
    private static void PositionWindow(LayoutResult result)
    {
        // Window positioned at origin by default
        result.X = 0;
        result.Y = 0;
    }
    
    private void PositionWithAnchor(Div div, LayoutResult result)
    {
        if (!_layoutContexts.TryGetValue(div, out var divContext))
            return;
            
        var parentWidth = divContext.AvailableWidth;
        var parentHeight = divContext.AvailableHeight;
        
        switch (div.Anchor)
        {
            case Anchor.TopLeft:
                result.X = 0;
                result.Y = 0;
                break;
            case Anchor.TopRight:
                result.X = parentWidth - result.Width;
                result.Y = 0;
                break;
            case Anchor.BottomLeft:
                result.X = 0;
                result.Y = parentHeight - result.Height;
                break;
            case Anchor.BottomRight:
                result.X = parentWidth - result.Width;
                result.Y = parentHeight - result.Height;
                break;
            case Anchor.Center:
                result.X = (parentWidth - result.Width) / 2;
                result.Y = (parentHeight - result.Height) / 2;
                break;
        }
    }
    
    private void ApplyHorizontalAlignment(UIElement element, LayoutResult result, float containerWidth)
    {
        if (!_layoutResults.TryGetValue(element, out var elementResult))
            return;
            
        var elementWidth = elementResult.Width;
        
        switch (element.HorizontalAlignment)
        {
            case HorizontalAlignment.Left:
                elementResult.X = result.X;
                break;
            case HorizontalAlignment.Center:
                elementResult.X = result.X + (containerWidth - elementWidth) / 2;
                break;
            case HorizontalAlignment.Right:
                elementResult.X = result.X + containerWidth - elementWidth;
                break;
            case HorizontalAlignment.Stretch:
                elementResult.Width = containerWidth;
                elementResult.X = result.X;
                break;
        }
        
        _layoutResults[element] = elementResult;
    }
    
    private void ApplyVerticalAlignment(UIElement element, LayoutResult result, float containerHeight)
    {
        if (!_layoutResults.TryGetValue(element, out var elementResult))
            return;
            
        var elementHeight = elementResult.Height;
        
        switch (element.VerticalAlignment)
        {
            case VerticalAlignment.Top:
                elementResult.Y = result.Y;
                break;
            case VerticalAlignment.Center:
                elementResult.Y = result.Y + (containerHeight - elementHeight) / 2;
                break;
            case VerticalAlignment.Bottom:
                elementResult.Y = result.Y + containerHeight - elementHeight;
                break;
            case VerticalAlignment.Stretch:
                elementResult.Height = containerHeight;
                elementResult.Y = result.Y;
                break;
        }
        
        _layoutResults[element] = elementResult;
    }
    
    private void ResolveStarWidths(List<UIElement> elements, float remainingSpace)
    {
        // Treat explicit star units as well as implicit star-like layout elements
        var starElements = elements.Where(e => e.Width.Type == UnitType.Star || (e.Width.Type == UnitType.None && e is LayoutElement)).ToList();
        if (starElements.Count == 0) return;

        var starValues = starElements.Select(e => e.Width.Type == UnitType.Star ? e.Width.Value : 1f).ToArray();
        var resolvedValues = StarUnitResolver.ResolveStarUnits(starValues, remainingSpace);

        for (int i = 0; i < starElements.Count; i++)
        {
            var element = starElements[i];
            if (_layoutResults.TryGetValue(element, out var result))
            {
                result.Width = resolvedValues[i];
                _layoutResults[element] = result;
            }
        }
    }
    
    private void ResolveStarHeights(List<UIElement> elements, float remainingSpace)
    {
        // Treat explicit star units as well as implicit star-like layout elements
        var starElements = elements.Where(e => e.Height.Type == UnitType.Star || (e.Height.Type == UnitType.None && e is LayoutElement)).ToList();
        if (starElements.Count == 0) return;

        var starValues = starElements.Select(e => e.Height.Type == UnitType.Star ? e.Height.Value : 1f).ToArray();
        var resolvedValues = StarUnitResolver.ResolveStarUnits(starValues, remainingSpace);

        for (int i = 0; i < starElements.Count; i++)
        {
            var element = starElements[i];
            if (_layoutResults.TryGetValue(element, out var result))
            {
                result.Height = resolvedValues[i];
                _layoutResults[element] = result;
            }
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
        
        // First pass: calculate fixed sizes and collect star units
        for (int i = 0; i < parts.Length; i++)
        {
            var unit = UnitValue.Parse(parts[i]);
            if (unit.Type == UnitType.Star)
            {
                starUnits.Add(unit);
            }
            else
            {
                result[i] = unit.ToPixels(new LayoutContext(16, totalSize, totalSize));
                fixedSize += result[i];
            }
        }
        
        // Second pass: distribute remaining space to star units
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
