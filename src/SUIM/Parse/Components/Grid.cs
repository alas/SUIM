namespace SUIM.Parse.Components;

using System;
using System.Xml.Linq;
using SUIM.Flexbox;

public class Grid() : LayoutElement(nameof(Grid))
{
    public string? Columns { get; set; }
    public string? Rows { get; set; }
    public List<GridChild> GridChildren { get; } = [];
    private bool _isStructureBuilt = false;
    private readonly List<Node> _rowNodes = [];
    private readonly List<Node> _cellNodes = [];

    #region ApplySUIMLayout

    internal override void ApplySUIMLayout()
    {
        ValidateGridPlacement();

        if (!_isStructureBuilt)
        {
            BuildGridStructure();
            _isStructureBuilt = true;
        }

        base.ApplySUIMLayout();
    }

    private void BuildGridStructure()
    {
        Node.StyleSetDisplay(Display.Flex);
        Node.StyleSetFlexDirection(FlexDirection.Column);

        var columnDefs = ParseUnits(Columns, Value.UndefinedValue);
        var rowDefs = ParseUnits(Rows, Value.UndefinedValue);

        int columnCount = columnDefs.Length > 0 ? columnDefs.Length : 1;
        int rowCount = rowDefs.Length > 0 ? rowDefs.Length : 1;

        // Auto-expand grid based on children placement
        foreach (var gridChild in GridChildren)
        {
            int maxRow = gridChild.Row + gridChild.RowSpan;
            int maxCol = gridChild.Column + gridChild.ColumnSpan;
            if (maxRow > rowCount) rowCount = maxRow;
            if (maxCol > columnCount) columnCount = maxCol;
        }

        // Create row containers
        for (int r = 0; r < rowCount; r++)
        {
            var rowNode = new Node();
            rowNode.StyleSetFlexDirection(FlexDirection.Row);

            var rowDef = r < rowDefs.Length ? rowDefs[r] : Value.UndefinedValue;
            ApplySizeDefinition(rowNode, rowDef, isRow: true);

            Node.AddChild(rowNode);
            _rowNodes.Add(rowNode);
        }

        // Create cell grid
        var cells = new Node[rowCount, columnCount];
        for (int r = 0; r < rowCount; r++)
        {
            for (int c = 0; c < columnCount; c++)
            {
                var cellNode = new Node();
                cellNode.StyleSetFlexDirection(FlexDirection.Column);

                var colDef = c < columnDefs.Length ? columnDefs[c] : Value.UndefinedValue;
                ApplySizeDefinition(cellNode, colDef, isRow: false);

                _rowNodes[r].AddChild(cellNode);
                cells[r, c] = cellNode;
                _cellNodes.Add(cellNode);
            }
        }

        // Place children into cells with proper spanning
        foreach (var gridChild in GridChildren)
        {
            int row = Math.Clamp(gridChild.Row, 0, rowCount - 1);
            int col = Math.Clamp(gridChild.Column, 0, columnCount - 1);

            var targetCell = cells[row, col];
            
            if (gridChild.RowSpan > 1 || gridChild.ColumnSpan > 1)
            {
                // Spanning: use absolute positioning
                gridChild.Element.Node.StyleSetPositionType(PositionType.Absolute);
                gridChild.Element.Node.StyleSetPosition(Edge.Left, 0);
                gridChild.Element.Node.StyleSetPosition(Edge.Top, 0);
                
                // Hide spanned cells
                for (int r = row; r < Math.Min(row + gridChild.RowSpan, rowCount); r++)
                {
                    for (int c = col; c < Math.Min(col + gridChild.ColumnSpan, columnCount); c++)
                    {
                        if (r == row && c == col) continue;
                        cells[r, c].StyleSetFlexGrow(0);
                        cells[r, c].StyleSetFlexBasis(0);
                    }
                }
            }

            targetCell.AddChild(gridChild.Element.Node);
        }
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
                node.StyleSetFlexGrow(1);
                node.StyleSetFlexBasis(0);
                break;
        }
    }

    public static Value[] ParseUnits(string? unitsString, Value totalSize)
    {
        if (string.IsNullOrWhiteSpace(unitsString))
            return [];

        var parts = unitsString.Split([',', ' '], StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim())
            .ToArray();

        if (parts.Length == 0)
            return [];

        return [.. parts.Select(x => Flex.ParseValueFromString(x, out var v) ? v : Value.UndefinedValue)];
    }

    private void ValidateGridPlacement()
    {
        var cellOccupancy = new HashSet<(int row, int col)>();

        foreach (var gridChild in GridChildren)
        {
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
        child.Parent = this;
        Children.Add(child);

        var gridChild = new GridChild { Element = child };

        if (element != null)
        {
            gridChild.Row = ParseIntAttribute(element, "grid.row");
            gridChild.Column = ParseIntAttribute(element, "grid.column");
            gridChild.RowSpan = ParseIntAttribute(element, "grid.rowspan", 1);
            gridChild.ColumnSpan = ParseIntAttribute(element, "grid.columnspan", 1);
        }

        GridChildren.Add(gridChild);
        _isStructureBuilt = false;
    }

    private static int ParseIntAttribute(XElement element, string attributeName, int defaultValue = 0)
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

        return defaultValue;
    }

    public override void RemoveChild(UIElement child)
    {
        child.Parent = null;
        Children.Remove(child);

        var gridChild = GridChildren.FirstOrDefault(gc => gc.Element == child);
        if (gridChild != null)
            GridChildren.Remove(gridChild);
        
        _isStructureBuilt = false;
    }

    public override void ClearChildren()
    {
        foreach (var child in Children)
            child.Parent = null;
        Children.Clear();
        GridChildren.Clear();
        _isStructureBuilt = false;
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
