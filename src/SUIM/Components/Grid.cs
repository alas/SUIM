namespace SUIM.Components;

using System;
using System.Xml.Linq;

public class Grid : LayoutElement
{
    public string? Columns { get; set; }
    public string? Rows { get; set; }
    public List<GridChild> GridChildren { get; } = [];

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

    public override void SetAttribute(string name, object? value)
    {
        if (name.Equals("columns", StringComparison.OrdinalIgnoreCase))
        {
            Columns = Convert.ToString(value);
        }
        else if (name.Equals("rows", StringComparison.OrdinalIgnoreCase))
        {
            Rows = Convert.ToString(value);
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
