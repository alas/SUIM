namespace SUIM.Parse.Components;

using System;
using System.Xml.Linq;
using SUIM.Flexbox;

public class Grid() : LayoutElement(nameof(Grid))
{
    public string? Columns { get; set; }
    public string? Rows { get; set; }
    public List<GridChild> GridChildren { get; } = [];

    internal override void ApplySUIMLayout()
    {
        throw new NotImplementedException();
    }

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

    public static float GetSpanSize(float[] sizes, int start, int length)
    {
        float result = 0;
        for (int i = start; i < start + length && i < sizes.Length; i++)
        {
            result += sizes[i];
        }
        return result;
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
