namespace SUIM.Tests;

using Xunit;

public class SUIMControlFlowParserTests
{
    private readonly Dictionary<string, object> _model = new()
    {
        { "identifierbool", true },
        { "identifierbool2", true },
        { "identifierbool3", false },
        { "identifierany", 500 },
        { "identifier2", 500 },
        { "Collection", new List<string> { "item1", "item2" } }
    };

    private readonly SUIMMarkupParser _processor;

    public SUIMControlFlowParserTests()
    {
        _processor = new SUIMMarkupParser(_model);
    }

    [Fact]
    public void Parse_DivWithAttributes()
    {
        var markup = "<div id=\"main\" width=\"100\" height=\"200\" halign=\"center\" valign=\"top\" margin=\"10\" padding=\"5\" bg=\"blue\" />";
        var element = _processor.Parse(markup);

        Assert.IsType<Div>(element);
        var div = (Div)element;
        Assert.Equal("main", div.Id);
        Assert.Equal(100, div.Width);
        Assert.Equal(200, div.Height);
        Assert.Equal(HorizontalAlignment.Center, div.HorizontalAlignment);
        Assert.Equal(VerticalAlignment.Top, div.VerticalAlignment);
        Assert.Equal(10, div.Margin);
        Assert.Equal(5, div.Padding);
        Assert.Equal("blue", div.Background);
    }

    [Fact]
    public void Parse_StackVertical()
    {
        var markup = "<stack orientation=\"vertical\" spacing=\"10\"><div /><div /></stack>";
        var element = _processor.Parse(markup);

        Assert.IsType<Stack>(element);
        var stack = (Stack)element;
        Assert.Equal(Orientation.Vertical, stack.Orientation);
        Assert.Equal(10, stack.Spacing);
        Assert.Equal(2, stack.Children.Count);
        Assert.IsType<Div>(stack.Children[0]);
        Assert.IsType<Div>(stack.Children[1]);
    }

    [Fact]
    public void Parse_StackHorizontal()
    {
        var markup = "<stack orientation=\"horizontal\"><label text=\"Hello\" /><button /></stack>";
        var element = _processor.Parse(markup);

        Assert.IsType<Stack>(element);
        var stack = (Stack)element;
        Assert.Equal(Orientation.Horizontal, stack.Orientation);
        Assert.Equal(2, stack.Children.Count);
        Assert.IsType<Label>(stack.Children[0]);
        Assert.IsType<Button>(stack.Children[1]);
        var label = (Label)stack.Children[0];
        Assert.Equal("Hello", label.Text);
    }

    [Fact]
    public void Parse_GridWithChildren()
    {
        var markup = @"<grid columns=""100, *"" rows=""50, *"">
<div grid.row=""0"" grid.column=""0"" bg=""gray"" />
<div grid.row=""0"" grid.column=""1"" bg=""silver"" />
<div grid.row=""1"" grid.column=""0"" grid.columnspan=""2"" bg=""white"" />
</grid>";
        var element = _processor.Parse(markup);

        Assert.IsType<Grid>(element);
        var grid = (Grid)element;
        Assert.Equal("100, *", grid.Columns);
        Assert.Equal("50, *", grid.Rows);
        Assert.Equal(3, grid.GridChildren.Count);
        Assert.Equal(0, grid.GridChildren[0].Row);
        Assert.Equal(0, grid.GridChildren[0].Column);
        Assert.Equal(0, grid.GridChildren[1].Row);
        Assert.Equal(1, grid.GridChildren[1].Column);
        Assert.Equal(1, grid.GridChildren[2].Row);
        Assert.Equal(0, grid.GridChildren[2].Column);
        Assert.Equal(2, grid.GridChildren[2].ColumnSpan);
    }

    [Fact]
    public void Parse_DockWithChildren()
    {
        var markup = @"<dock lastchildfill=""true"">
<div dock.edge=""left"" />
<div dock.edge=""right"" />
<div dock.edge=""top"" />
<div />
</dock>";
        var element = _processor.Parse(markup);

        Assert.IsType<Dock>(element);
        var dock = (Dock)element;
        Assert.True(dock.LastChildFill);
        Assert.Equal(4, dock.DockChildren.Count);
        Assert.Equal(DockEdge.Left, dock.DockChildren[0].Edge);
        Assert.Equal(DockEdge.Right, dock.DockChildren[1].Edge);
        Assert.Equal(DockEdge.Top, dock.DockChildren[2].Edge);
        // Last one has no edge, but still added
    }

    [Fact]
    public void Parse_Overlay()
    {
        var markup = "<overlay><div /><div /></overlay>";
        var element = _processor.Parse(markup);

        Assert.IsType<Overlay>(element);
        var overlay = (Overlay)element;
        Assert.Equal(2, overlay.Children.Count);
    }

    [Fact]
    public void Parse_Label()
    {
        var markup = "<label text=\"Test Label\" />";
        var element = _processor.Parse(markup);

        Assert.IsType<Label>(element);
        var label = (Label)element;
        Assert.Equal("Test Label", label.Text);
    }

    [Fact]
    public void Parse_Button()
    {
        var markup = "<button><label text=\"Click me\" /></button>";
        var element = _processor.Parse(markup);

        Assert.IsType<Button>(element);
        var button = (Button)element;
        Assert.Single(button.Children);
        Assert.IsType<Label>(button.Children[0]);
    }

    [Fact]
    public void Parse_UnknownTag_Throws()
    {
        var markup = "<unknown />";
        Assert.Throws<NotSupportedException>(() => _processor.Parse(markup));
    }

    [Fact]
    public void Parse_InvalidXml_Throws()
    {
        var markup = "<div><unclosed>";
        Assert.Throws<System.Xml.XmlException>(() => _processor.Parse(markup));
    }

    [Fact]
    public void Parse_EmptyMarkup_Throws()
    {
        var markup = "";
        Assert.Throws<System.Xml.XmlException>(() => _processor.Parse(markup));
    }

    [Fact]
    public void Parse_NestedElements()
    {
        var markup = "<stack><div><label text=\"Nested\" /></div></stack>";
        var element = _processor.Parse(markup);

        Assert.IsType<Stack>(element);
        var stack = (Stack)element;
        Assert.Single(stack.Children);
        var div = (Div)stack.Children[0];
        Assert.Single(div.Children);
        var label = (Label)div.Children[0];
        Assert.Equal("Nested", label.Text);
    }

    [Fact]
    public void Parse_AnchorAttribute()
    {
        var markup = "<div anchor=\"TopLeft\" />";
        var element = _processor.Parse(markup);

        Assert.IsType<Div>(element);
        var div = (Div)element;
        Assert.Equal(Anchor.TopLeft, div.Anchor);
    }

    [Fact]
    public void Parse_SynonymAttributes()
    {
        var markup = "<div halign=\"right\" valign=\"bottom\" />";
        var element = _processor.Parse(markup);

        Assert.IsType<Div>(element);
        var div = (Div)element;
        Assert.Equal(HorizontalAlignment.Right, div.HorizontalAlignment);
        Assert.Equal(VerticalAlignment.Bottom, div.VerticalAlignment);
    }

    [Fact]
    public void Parse_GridRowSpanColumnSpan()
    {
        var markup = @"<grid>
<div grid.row=""0"" grid.column=""0"" grid.rowspan=""2"" grid.columnspan=""2"" />
</grid>";
        var element = _processor.Parse(markup);

        Assert.IsType<Grid>(element);
        var grid = (Grid)element;
        Assert.Single(grid.GridChildren);
        var child = grid.GridChildren[0];
        Assert.Equal(0, child.Row);
        Assert.Equal(0, child.Column);
        Assert.Equal(2, child.RowSpan);
        Assert.Equal(2, child.ColumnSpan);
    }

    [Fact]
    public void Parse_DockEdgeCaseInsensitive()
    {
        var markup = @"<dock>
<div dock.edge=""LEFT"" />
</dock>";
        var element = _processor.Parse(markup);

        Assert.IsType<Dock>(element);
        var dock = (Dock)element;
        Assert.Single(dock.DockChildren);
        Assert.Equal(DockEdge.Left, dock.DockChildren[0].Edge);
    }

    [Fact]
    public void Parse_IfDirective_True()
    {
        var markup = @"<div>
@if identifierbool
{
    <label text=""True"" />
}
</div>";
        var element = _processor.Parse(markup);

        Assert.IsType<Div>(element);
        var div = (Div)element;
        Assert.Single(div.Children);
        Assert.IsType<Label>(div.Children[0]);
        var label = (Label)div.Children[0];
        Assert.Equal("True", label.Text);
    }

    [Fact]
    public void Parse_IfElseDirective_True()
    {
        var markup = @"<div>
@if identifierbool2
{
    <label text=""True"" />
}
else
{
    <label text=""False"" />
}
</div>";
        var element = _processor.Parse(markup);

        Assert.IsType<Div>(element);
        var div = (Div)element;
        Assert.Single(div.Children);
        Assert.IsType<Label>(div.Children[0]);
        var label = (Label)div.Children[0];
        Assert.Equal("True", label.Text);
    }

    [Fact]
    public void Parse_IfElseDirective_False()
    {
        var markup = @"<div>
@if identifierbool3
{
    <label text=""True"" />
}
else
{
    <label text=""False"" />
}
</div>";
        var element = _processor.Parse(markup);

        Assert.IsType<Div>(element);
        var div = (Div)element;
        Assert.Single(div.Children);
        Assert.IsType<Label>(div.Children[0]);
        var label = (Label)div.Children[0];
        Assert.Equal("False", label.Text);
    }

    [Fact]
    public void Parse_IfElseIfElseDirective_True()
    {
        var markup = @"<div>
@if identifierbool3
{
    <label text=""False"" />
}
else if identifierbool3
{
    <label text=""False"" />
}
else
{
    <label text=""True"" />
}
</div>";
        var element = _processor.Parse(markup);

        Assert.IsType<Div>(element);
        var div = (Div)element;
        Assert.Single(div.Children);
        Assert.IsType<Label>(div.Children[0]);
        var label = (Label)div.Children[0];
        Assert.Equal("True", label.Text);
    }

    [Fact]
    public void Parse_IfElseIfElseIfElseDirective_FinalElse()
    {
        var markup = @"<div>
@if identifierbool3
{
    <label text=""False"" />
}
else if identifierbool3
{
    <label text=""False"" />
}
else if identifierbool3
{
    <label text=""False"" />
}
else
{
    <label text=""FinalElse"" />
}
</div>";
        var element = _processor.Parse(markup);

        Assert.IsType<Div>(element);
        var div = (Div)element;
        Assert.Single(div.Children);
        Assert.IsType<Label>(div.Children[0]);
        var label = (Label)div.Children[0];
        Assert.Equal("FinalElse", label.Text);
    }

    [Fact]
    public void Parse_IfElseIfElseIfElseDirective_True()
    {
        var markup = @"<div>
@if identifierbool3
{
    <label text=""False"" />
}
else if identifierbool3
{
    <label text=""False"" />
}
else if identifierbool2
{
    <label text=""True"" />
}
else
{
    <label text=""False"" />
}
</div>";
        var element = _processor.Parse(markup);

        Assert.IsType<Div>(element);
        var div = (Div)element;
        Assert.Single(div.Children);
        Assert.IsType<Label>(div.Children[0]);
        var label = (Label)div.Children[0];
        Assert.Equal("True", label.Text);
    }

    [Fact]
    public void Parse_ForDirective()
    {
        var markup = @"<stack>
@for i=0 count=3
{
    <label text=""@i"" />
}
</stack>";
        var element = _processor.Parse(markup);

        Assert.IsType<Stack>(element);
        var stack = (Stack)element;
        Assert.Equal(3, stack.Children.Count);
        for (int i = 0; i < 3; i++)
        {
            Assert.IsType<Label>(stack.Children[i]);
            var label = (Label)stack.Children[i];
            Assert.Equal(i.ToString(), label.Text);
        }
    }

    [Fact]
    public void Parse_SwitchDirective()
    {
        var markup = @"<div>
@switch identifierany
{
    case 500
    {
        <label text=""Matched"" />
    }
    default
    {
        <label text=""Default"" />
    }
}
</div>";
        var element = _processor.Parse(markup);

        Assert.IsType<Div>(element);
        var div = (Div)element;
        Assert.Single(div.Children);
        Assert.IsType<Label>(div.Children[0]);
        var label = (Label)div.Children[0];
        Assert.Equal("Matched", label.Text);
    }

    [Fact]
    public void ControlFlow_SwitchDirective()
    {
        var parser = new SUIMControlFlowParser(_model);
        var markup = @"@switch identifierany
{
    case 500
    {
        <label text=""Matched"" />
    }
    default
    {
        <label text=""Default"" />
    }
}";
        var expanded = parser.ExpandDirectives(markup);
        Assert.Equal("<label text=\"Matched\" />", expanded.Trim());
    }
}
