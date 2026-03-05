namespace SUIM.Parse.Components;

using System;
using System.Xml.Linq;
using SUIM.Flexbox;

public class Grid() : LayoutElement(nameof(Grid))
{
    public string? Columns { get; set; }
    public string? Rows { get; set; }
    public List<GridChild> GridChildren { get; } = [];

    #region ApplySUIMLayout

    internal override void ApplySUIMLayout()
    {
        ValidateGridPlacement();

        foreach (var child in Node.Children.ToList())
        {
            Flex.RemoveChild(Node, child);
        }

        // Grid behaves as vertical flex (rows stacked)
        Node.StyleSetDisplay(Display.Flex);
        Node.StyleSetFlexDirection(FlexDirection.Column);

        // Parse column/row definitions
        var columnDefs = ParseUnits(Columns, Value.UndefinedValue);
        var rowDefs = ParseUnits(Rows, Value.UndefinedValue);

        int columnCount = columnDefs.Length;
        int rowCount = rowDefs.Length;

        if (columnCount == 0) columnCount = 1;
        if (rowCount == 0) rowCount = 1;

        // Create row containers
        var rowNodes = new Node[rowCount];

        for (int r = 0; r < rowCount; r++)
        {
            var rowNode = new Node();
            rowNode.StyleSetFlexDirection(FlexDirection.Row);

            // Row height behavior
            var rowDef = r < rowDefs.Length ? rowDefs[r] : Value.UndefinedValue;
            ApplySizeDefinition(rowNode, rowDef, isRow: true);

            Node.AddChild(rowNode);
            rowNodes[r] = rowNode;
        }

        // Place children into grid
        foreach (var gridChild in GridChildren)
        {
            int row = Math.Clamp(gridChild.Row, 0, rowCount - 1);
            int col = Math.Clamp(gridChild.Column, 0, columnCount - 1);

            var rowNode = rowNodes[row];

            // Wrap child in cell container
            var cellNode = new Node();
            cellNode.StyleSetFlexDirection(FlexDirection.Column);

            // Column width behavior
            var colDef = col < columnDefs.Length ? columnDefs[col] : Value.UndefinedValue;
            ApplySizeDefinition(cellNode, colDef, isRow: false);

            // Handle spans using flex-grow
            if (gridChild.ColumnSpan > 1)
            {
                cellNode.StyleSetFlexGrow(gridChild.ColumnSpan);
            }

            if (gridChild.RowSpan > 1)
            {
                cellNode.StyleSetFlexGrow(gridChild.RowSpan);
            }

            // Add actual element to cell
            cellNode.AddChild(gridChild.Element.Node);

            rowNode.AddChild(cellNode);
        }

        base.ApplySUIMLayout();
    }

    private static void ApplySizeDefinition(Node node, Value definition, bool isRow)
    {
        switch (definition.Unit)
        {
            case Unit.Point:
                if (isRow)
                    node.StyleSetHeight(definition.ValueUnit);
                else
                    node.StyleSetWidth(definition.ValueUnit);
                break;

            case Unit.Percent:
                if (isRow)
                    node.StyleSetHeightPercent(definition.ValueUnit);
                else
                    node.StyleSetWidthPercent(definition.ValueUnit);
                break;

            case Unit.Undefined:
            default:
                // Treat undefined or "fr" like flex-grow = 1
                node.StyleSetFlexGrow(1);
                node.StyleSetFlexBasis(0);
                break;
        }
    }

    public static Value[] ParseUnits(string? unitsString, Value totalSize)
    {
        if (string.IsNullOrWhiteSpace(unitsString))
            return [totalSize];

        var parts = unitsString.Split([',', ' '], StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim())
            .ToArray();

        if (parts.Length == 0)
            return [totalSize];

        return [.. parts.Select(x => Flex.ParseValueFromString(x, out var v) ? v : Value.UndefinedValue)];
    }

    private void ValidateGridPlacement()
    {
        var cellOccupancy = new HashSet<(int row, int col)>();

        foreach (var gridChild in GridChildren)
        {
            // Check for overlaps
            for (int r = gridChild.Row; r < gridChild.Row + gridChild.RowSpan; r++)
            {
                for (int c = gridChild.Column; c < gridChild.Column + gridChild.ColumnSpan; c++)
                {
                    if (!cellOccupancy.Add((r, c)))
                    {
                        Console.WriteLine($"WARNING: Grid cell ({r},{c}) is occupied by multiple children. This will cause overlap.");
                    }
                }
            }
        }
    }

    #endregion

    #region Children

    public override void AddChild(UIElement child, XElement? element)
    {
        base.AddChild(child, element);

        var gridChild = new GridChild { Element = child };

        if (element != null)
        {
            gridChild.Row = ParseIntAttribute(element, "grid.row", child);
            gridChild.Column = ParseIntAttribute(element, "grid.column", child);
            gridChild.RowSpan = ParseIntAttribute(element, "grid.rowspan", child, 1);
            gridChild.ColumnSpan = ParseIntAttribute(element, "grid.columnspan", child, 1);
        }

        GridChildren.Add(gridChild);
    }

    private static int ParseIntAttribute(XElement element, string attributeName, UIElement child, int defaultValue = 0)
    {
        var attr = element.Attribute(attributeName);
        if (attr != null)
        {
            if (!int.TryParse(attr.Value, out var row))
            {
                throw new ArgumentException($"{attributeName} must be an integer, got '{attr.Value}' on element {element.Name}");
            }

            return row;
        }

        // todo: get next available row/column[span] index if not specified, for now just default to 0
        Console.WriteLine($"WARNING: Grid child <{child.TagName}> has no grid.row/grid.column. Defaulting to (0,0) may cause overlap.");
        return defaultValue;
    }

    public override void RemoveChild(UIElement child)
    {
        base.RemoveChild(child);

        var gridChild = GridChildren.FirstOrDefault(gc => gc.Element == child);
        if (gridChild != null)
            GridChildren.Remove(gridChild);
    }

    public override void ClearChildren()
    {
        base.ClearChildren();
        GridChildren.Clear();
    }

    #endregion

    public override void SetAttribute(string name, object? value)
    {
        if (name.Equals("columns", StringComparison.OrdinalIgnoreCase))
        {
            Columns = value as string;
        }
        else if (name.Equals("rows", StringComparison.OrdinalIgnoreCase))
        {
            Rows = value as string;
        }
        else
        {
            base.SetAttribute(name, value);
        }
    }
}

public class GridChild
{
    public UIElement Element { get; set; } = null!;
    public int Row { get; set; }
    public int Column { get; set; }
    public int RowSpan { get; set; } = 1;
    public int ColumnSpan { get; set; } = 1;
}
