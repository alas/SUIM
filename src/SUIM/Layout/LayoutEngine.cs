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
        
        // Phase 1: Measure all elements
        MeasureElement(root, context);
        
        // Phase 2: Position all elements
        PositionElement(root, context);
        
        // Phase 3: Finalize layout
        FinalizeElement(root, context);
        
        return _layoutResults[root];
    }
    
    private void MeasureElement(UIElement element, LayoutContext context)
    {
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
        else
        {
            // Default measurement for content elements
            result.ContentWidth = widthInPixels;
            result.ContentHeight = heightInPixels;
        }
        
        // Apply constraints
        result.ContentWidth = Math.Min(result.ContentWidth, availableWidth);
        result.ContentHeight = Math.Min(result.ContentHeight, availableHeight);
        
        // Calculate total size including padding
        result.Width = result.ContentWidth + paddingLeft + paddingRight;
        result.Height = result.ContentHeight + paddingTop + paddingBottom;
        
        // Store result
        _layoutResults[element] = result;
        _layoutContexts[element] = context;
    }
    
    private void PositionElement(UIElement element, LayoutContext context)
    {
        if (!_layoutResults.TryGetValue(element, out var result))
            return;
            
        // Position based on element type and alignment
        if (element is Stack stack)
        {
            PositionStack(stack, result, context);
        }
        else if (element is Grid grid)
        {
            PositionGrid(grid, result, context);
        }
        else if (element is Dock dock)
        {
            PositionDock(dock, result, context);
        }
        else if (element is Div div)
        {
            PositionDiv(div, result, context);
        }
        else if (element is Window window)
        {
            PositionWindow(window, result, context);
        }
        else
        {
            // Default positioning
            result.X = 0;
            result.Y = 0;
        }
        
        // Update stored result
        _layoutResults[element] = result;
    }
    
    private void FinalizeElement(UIElement element, LayoutContext context)
    {
        // Apply final adjustments and propagate to children
        foreach (var child in element.Children)
        {
            var childContext = CreateChildContext(element, child, context);
            Layout(child, childContext);
        }
    }
    
    private LayoutContext CreateChildContext(UIElement parent, UIElement child, LayoutContext parentContext)
    {
        if (!_layoutResults.TryGetValue(parent, out var parentResult))
            return parentContext;
            
        var childContext = new LayoutContext(parentContext.RootFontSize, 
            parentResult.ContentWidth, parentResult.ContentHeight)
        {
            CurrentFontSize = parentContext.CurrentFontSize
        };
        
        return childContext;
    }
    
    private void MeasureStack(Stack stack, float availableWidth, float availableHeight, LayoutContext context)
    {
        var totalSpacing = Math.Max(0, stack.Spacing * (stack.Children.Count - 1));
        var remainingWidth = availableWidth;
        var remainingHeight = availableHeight - totalSpacing;
        
        if (stack.Orientation == Orientation.Horizontal)
        {
            // Horizontal stack: sum of widths
            float totalWidth = 0;
            var starElements = new List<UIElement>();
            
            foreach (var child in stack.Children)
            {
                if (child.Width.Type == UnitType.Star)
                {
                    starElements.Add(child);
                }
                else
                {
                    var childContext = new LayoutContext(context.RootFontSize, availableWidth, availableHeight);
                    MeasureElement(child, childContext);
                    var childResult = _layoutResults[child];
                    totalWidth += childResult.Width;
                }
            }
            
            // Handle star units for proportional width
            if (starElements.Count > 0)
            {
                ResolveStarWidths(starElements, Math.Max(0, remainingWidth - totalWidth), context);
            }
        }
        else
        {
            // Vertical stack: sum of heights
            float totalHeight = 0;
            var starElements = new List<UIElement>();
            
            foreach (var child in stack.Children)
            {
                if (child.Height.Type == UnitType.Star)
                {
                    starElements.Add(child);
                }
                else
                {
                    var childContext = new LayoutContext(context.RootFontSize, availableWidth, availableHeight);
                    MeasureElement(child, childContext);
                    var childResult = _layoutResults[child];
                    totalHeight += childResult.Height;
                }
            }
            
            // Handle star units for proportional height
            if (starElements.Count > 0)
            {
                ResolveStarHeights(starElements, Math.Max(0, remainingHeight - totalHeight), context);
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
        // This would be handled in the finalization phase
    }
    
    private void MeasureDiv(Div div, float availableWidth, float availableHeight, LayoutContext context)
    {
        if (div.Anchor.HasValue)
        {
            // Absolute positioning based on anchor - measurement handled in positioning phase
        }
        else if (div.X != 0 || div.Y != 0)
        {
            // Explicit positioning - measurement handled in positioning phase
        }
        else
        {
            // Default block layout - use available space
        }
    }
    
    private void MeasureWindow(Window window, float availableWidth, float availableHeight, LayoutContext context)
    {
        // Window takes full available space
        // No special measurement needed, uses available space
    }
    
    private void PositionStack(Stack stack, LayoutResult result, LayoutContext context)
    {
        if (stack.Orientation == Orientation.Horizontal)
        {
            PositionHorizontalStack(stack, result, context);
        }
        else
        {
            PositionVerticalStack(stack, result, context);
        }
    }
    
    private void PositionHorizontalStack(Stack stack, LayoutResult result, LayoutContext context)
    {
        float currentX = result.ContentX;
        
        foreach (var child in stack.Children)
        {
            if (_layoutResults.TryGetValue(child, out var childResult))
            {
                childResult.X = currentX;
                childResult.Y = result.ContentY;
                
                // Apply vertical alignment
                ApplyVerticalAlignment(child, childResult, result.ContentHeight);
                
                currentX += childResult.Width + stack.Spacing;
                _layoutResults[child] = childResult;
            }
        }
    }
    
    private void PositionVerticalStack(Stack stack, LayoutResult result, LayoutContext context)
    {
        float currentY = result.ContentY;
        
        foreach (var child in stack.Children)
        {
            if (_layoutResults.TryGetValue(child, out var childResult))
            {
                childResult.X = result.ContentX;
                childResult.Y = currentY;
                
                // Apply horizontal alignment
                ApplyHorizontalAlignment(child, childResult, result.ContentWidth);
                
                currentY += childResult.Height + stack.Spacing;
                _layoutResults[child] = childResult;
            }
        }
    }
    
    private void PositionGrid(Grid grid, LayoutResult result, LayoutContext context)
    {
        var columnWidths = ParseGridUnits(grid.Columns, result.ContentWidth);
        var rowHeights = ParseGridUnits(grid.Rows, result.ContentHeight);
        
        foreach (var gridChild in grid.GridChildren)
        {
            if (_layoutResults.TryGetValue(gridChild.Element, out var childResult))
            {
                float x = result.ContentX;
                float y = result.ContentY;
                
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
    
    private void PositionDock(Dock dock, LayoutResult result, LayoutContext context)
    {
        float left = result.ContentX;
        float top = result.ContentY;
        float right = result.ContentRight;
        float bottom = result.ContentBottom;
        
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
    
    private void PositionDiv(Div div, LayoutResult result, LayoutContext context)
    {
        if (div.Anchor.HasValue)
        {
            PositionWithAnchor(div, result, context);
        }
        else if (div.X != 0 || div.Y != 0)
        {
            result.X = div.X;
            result.Y = div.Y;
        }
        else
        {
            // Default positioning
            result.X = 0;
            result.Y = 0;
        }
    }
    
    private void PositionWindow(Window window, LayoutResult result, LayoutContext context)
    {
        // Window positioned at origin by default
        result.X = 0;
        result.Y = 0;
    }
    
    private void PositionWithAnchor(Div div, LayoutResult result, LayoutContext context)
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
    
    private void ResolveStarWidths(List<UIElement> elements, float remainingSpace, LayoutContext context)
    {
        var starElements = elements.Where(e => e.Width.Type == UnitType.Star).ToList();
        if (starElements.Count == 0) return;
        
        var starValues = starElements.Select(e => e.Width.Value).ToArray();
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
    
    private void ResolveStarHeights(List<UIElement> elements, float remainingSpace, LayoutContext context)
    {
        var starElements = elements.Where(e => e.Height.Type == UnitType.Star).ToList();
        if (starElements.Count == 0) return;
        
        var starValues = starElements.Select(e => e.Height.Value).ToArray();
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
    
    private float[] ParseGridUnits(string? unitsString, float totalSize)
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
    
    private float GetGridSpanWidth(float[] columnWidths, int startColumn, int columnSpan)
    {
        float width = 0;
        for (int i = startColumn; i < startColumn + columnSpan && i < columnWidths.Length; i++)
        {
            width += columnWidths[i];
        }
        return width;
    }
    
    private float GetGridSpanHeight(float[] rowHeights, int startRow, int rowSpan)
    {
        float height = 0;
        for (int i = startRow; i < startRow + rowSpan && i < rowHeights.Length; i++)
        {
            height += rowHeights[i];
        }
        return height;
    }
}