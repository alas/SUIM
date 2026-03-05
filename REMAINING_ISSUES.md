## Success Metrics

### Quantitative
- ✅ All spec examples work without modification
- ✅ 90%+ test coverage for layout components
- ✅ Zero node hierarchy rebuilds during layout
- ✅ <16ms layout time for typical UI (60fps)

### Qualitative
- ✅ Web developers can build UIs without reading Flexbox docs
- ✅ WinForms developers recognize familiar patterns
- ✅ Error messages guide users to solutions
- ✅ Spec and implementation match 100%




# SUIM Remaining Issues

## Status: Major Work Required 🔴
1. **Grid Component** - Fundamentally broken
   - ❌ Uses FlexGrow for spanning (doesn't actually span cells)
   - ❌ No auto-placement (children without grid.row/column default to 0,0)
   - ❌ Rebuilds structure on every layout (like old Dock)
   - ❌ Not a real grid - fakes it with nested flex containers
**File:** `Grid.cs`, method `ApplySUIMLayout()`

**Problems:**
- Destroys and rebuilds node tree on every layout pass
- Uses FlexGrow for spanning (incorrect - doesn't actually span cells)
- No auto-placement (children without grid.row/column default to 0,0)
- Not a real grid - fakes it with nested flex containers

**Example of broken spanning:**
```csharp
if (gridChild.ColumnSpan > 1) {
    cellNode.StyleSetFlexGrow(gridChild.ColumnSpan); // WRONG
}
```
FlexGrow doesn't make an element span multiple grid cells, it just makes it grow relative to siblings.


Rewrite Grid Component
**Two options:**

**Option A: Fix Flex-Based Grid (Pragmatic)**
- Build structure once (not on every layout)
- Implement proper spanning (calculate actual cell sizes)
- Add auto-placement logic
- Estimated: 3-4 days

**Option B: True Grid Layout (Ideal)**
- Implement proper CSS Grid algorithm
- Support fr units, auto-placement, grid-template-areas
- Estimated: 2-3 weeks

**Recommendation:** Start with Option A for v1.0, plan Option B for v2.0


---

### 3. **Grid Component - Fundamentally Broken**

**Spec Says:** "Divides space into a matrix" with explicit column/row definitions and child positioning via `grid.row`, `grid.column`, etc.

**Implementation Issues:**

```csharp
internal override void ApplySUIMLayout()
{
    // REMOVES all children from Node
    foreach (var child in Node.Children.ToList())
        Flex.RemoveChild(Node, child);
    
    // Creates wrapper row/column nodes
    // Places children in cells
    // Uses FlexGrow for spans (WRONG)
}
```

**Critical Problems:**
- ❌ **Destroys original node hierarchy** - removes all children and rebuilds
- ❌ **Span implementation is incorrect** - uses FlexGrow instead of proper grid spanning
- ❌ **No true grid layout** - fakes it with nested flex containers
- ❌ **Auto-placement missing** - children without explicit row/column fall to (0,0)
- ❌ **No `<row>` or `<column>` helper support** as shown in spec examples

**Mental Model Friction:**
- **Web developers** expect CSS Grid behavior (auto-placement, fr units, grid-template-areas)
- **WinForms developers** expect TableLayoutPanel (explicit cell assignment, spanning)
- **Current implementation:** Neither - it's a broken flex-based approximation



#### 1.3 Rewrite Grid Component (Major Refactor)

**Option A: True Grid Layout (Requires Custom Layout Engine)**
- Implement proper grid algorithm (not flex-based)
- Support auto-placement, fr units, spanning
- High effort, high value

**Option B: Improve Flex-Based Grid (Pragmatic)**
- Fix spanning logic (don't use FlexGrow)
- Add auto-placement for children without explicit row/column
- Support `<row>` and `<column>` helper tags
- Medium effort, medium value

**Recommendation:** Start with Option B, plan Option A for v2.0

```csharp
// Improved flex-based grid
internal override void ApplySUIMLayout()
{
    // Don't destroy children - build structure once during parsing
    if (!_isStructureBuilt)
    {
        BuildGridStructure();
        _isStructureBuilt = true;
    }
    
    // Update layout properties only
    UpdateGridLayout();
    
    base.ApplySUIMLayout();
}
```







## Status: Nice to Have 🟡

1. Update Specification
- Clarify Div default behavior
- Document Grid limitations (flex-based vs true grid)
- Add Overlay layer rendering details
- Expand Yoga gotchas section

2. Stack Gap Uses Margin Emulation

3. **Validation Warnings** - No Yoga gotcha detection
   - Missing root size warnings
   - Deep percentage height warnings
   - Grid auto-placement conflict warnings

---

Add Validation Warnings
- Warn about missing root size
- Warn about deep percentage heights
- Warn about Grid auto-placement conflicts


## Testing Priorities

1. Complex layouts (grid + dock + overlay)
2. Grid spanning (currently broken)
3. Grid auto-placement (currently broken)
4. Stack gap with native implementation
5. Dock structure stability across layouts
6. Border thickness rendering
7. Scroll overflow behavior
8. All stack synonym variants
9. Nested grids


## Testing Strategy

### Unit Tests Needed

1. **Div default behavior**
   - Children stack vertically
   - Children stretch to fill width
   - Respects explicit flex properties

2. **Stack with gap**
   - Native gap support
   - Correct spacing between children
   - Works with both orientations

3. **Grid auto-placement**
   - Children without row/column get placed automatically
   - No overlapping cells
   - Respects explicit placements

4. **Grid spanning**
   - ColumnSpan/RowSpan work correctly
   - Spanning doesn't break layout
   - Mixed span and non-span children

5. **Dock edge cases**
   - All edge combinations
   - LastChildFill behavior
   - Nested docks

6. **Overlay positioning**
   - Fills parent correctly
   - Centers content by default
   - Respects custom alignment

7. **Border/Scroll wrapping**
   - Attributes trigger wrapping
   - Size/styling transferred correctly
   - Nested wrapping works
