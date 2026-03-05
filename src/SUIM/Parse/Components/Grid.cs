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
        foreach (var child in Node.Children.ToList())         {
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

    #endregion

    #region Children

    public override void AddChild(UIElement child, XElement? element)
    {
        base.AddChild(child, element);

        var gridChild = new GridChild { Element = child };

        if (element != null)
        {
            var rowAttr = element.Attribute("grid.row");
            if (rowAttr != null) gridChild.Row = int.Parse(rowAttr.Value);
            var colAttr = element.Attribute("grid.column");
            if (colAttr != null) gridChild.Column = int.Parse(colAttr.Value);
            var rowspanAttr = element.Attribute("grid.rowspan");
            if (rowspanAttr != null) gridChild.RowSpan = int.Parse(rowspanAttr.Value);
            var colspanAttr = element.Attribute("grid.columnspan");
            if (colspanAttr != null) gridChild.ColumnSpan = int.Parse(colspanAttr.Value);
        }

        GridChildren.Add(gridChild);
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
