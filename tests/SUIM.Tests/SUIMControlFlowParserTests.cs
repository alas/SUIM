namespace SUIM.Tests;

using Xunit;

public class SUIMControlFlowParserTests
{
    private readonly dynamic _model;

    private readonly SUIMMarkupParser _processor;

    public SUIMControlFlowParserTests()
    {
        _model = SUIM.Create(
            new
            {
                identifierbool = true,
                identifierbool2 = true,
                identifierbool3 = false,
                identifierany = 500,
                identifier2 = 500,
                Collection = new[] { "item1", "item2" },
                stringValue = "test",
                numericValue = 42,
                currentWidth = 250,
                invWidth = 500,
                items = new[] { new { Name = "Apple" }, new { Name = "Banana" } }
            });
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
        Assert.Equal("100", div.Width);
        Assert.Equal("200", div.Height);
        Assert.Equal(HorizontalAlignment.Center, div.HorizontalAlignment);
        Assert.Equal(VerticalAlignment.Top, div.VerticalAlignment);
        Assert.Equal("10", div.Margin);
        Assert.Equal("5", div.Padding);
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

    // ============== CONTENT TAGS TESTS ==============

    [Fact]
    public void Parse_Button_WithSprites()
    {
        var markup = "<button text=\"idle_sprite\" />";
        var element = _processor.Parse(markup);

        Assert.IsType<Button>(element);
        var button = (Button)element;
        Assert.Equal("idle_sprite", button.Text);
    }

    [Fact]
    public void Parse_Button_WithOnClick()
    {
        var markup = "<button text=\"Submit\" onclick=\"HandleSubmit\" />";
        var element = _processor.Parse(markup);

        Assert.IsType<Button>(element);
        var button = (Button)element;
        Assert.True(button.EventHandlers.ContainsKey("click"));
    }

    [Fact]
    public void Parse_Input_TextType()
    {
        var markup = "<input type=\"text\" placeholder=\"Enter name\" />";
        var element = _processor.Parse(markup);

        Assert.IsType<Input>(element);
        var input = (Input)element;
        Assert.Equal(InputType.Text, input.Type);
        Assert.Equal("Enter name", input.Placeholder);
    }

    [Fact]
    public void Parse_Input_NumberType_WithMinMax()
    {
        var markup = "<input type=\"number\" min=\"0\" max=\"100\" step=\"5\" />";
        var element = _processor.Parse(markup);

        Assert.IsType<Input>(element);
        var input = (Input)element;
        Assert.Equal(InputType.Number, input.Type);
        Assert.Equal(0, input.Min);
        Assert.Equal(100, input.Max);
        Assert.Equal(5, input.Step);
    }

    [Fact]
    public void Parse_Input_WithMask()
    {
        var markup = "<input type=\"text\" mask=\"[0-9]{3}-[0-9]{3}-[0-9]{4}\" />";
        var element = _processor.Parse(markup);

        Assert.IsType<Input>(element);
        var input = (Input)element;
        Assert.Equal("[0-9]{3}-[0-9]{3}-[0-9]{4}", input.Mask);
    }

    [Fact]
    public void Parse_Label_WithAllAttributes()
    {
        var markup = "<label text=\"Hello\" font=\"Arial\" fontsize=\"16\" color=\"#FF0000\" wrap=\"true\" />";
        var element = _processor.Parse(markup);

        Assert.IsType<Label>(element);
        var label = (Label)element;
        Assert.Equal("Hello", label.Text);
        Assert.Equal("Arial", label.Font);
        Assert.Equal(16, label.FontSize);
        Assert.Equal("#FF0000", label.Color);
        Assert.True(label.Wrap);
    }

    [Fact]
    public void Parse_Image_WithStretch()
    {
        var markup = "<image source=\"mysprite\" stretch=\"uniform\" />";
        var element = _processor.Parse(markup);

        Assert.IsType<Image>(element);
        var image = (Image)element;
        Assert.Equal("mysprite", image.Source);
        Assert.Equal(ImageStretch.Uniform, image.Stretch);
    }

    [Fact]
    public void Parse_Select_WithOptions()
    {
        var markup = @"<select id=""dropdown"">
<option value=""val1"">Option 1</option>
<option value=""val2"">Option 2</option>
</select>";
        var element = _processor.Parse(markup);

        Assert.IsType<Select>(element);
        var select = (Select)element;
        Assert.Equal("dropdown", select.Id);
        Assert.Equal(2, select.Children.Count);
    }

    [Fact]
    public void Parse_Select_WithMultiple()
    {
        var markup = "<select multiple=\"true\"><option>Opt1</option><option>Opt2</option></select>";
        var element = _processor.Parse(markup);

        Assert.IsType<Select>(element);
        var select = (Select)element;
        Assert.True(select.Multiple);
    }

    [Fact]
    public void Parse_Textarea()
    {
        var markup = "<textarea id=\"notes\" width=\"300\" height=\"200\" />";
        var element = _processor.Parse(markup);

        Assert.IsType<TextArea>(element);
        var textarea = (TextArea)element;
        Assert.Equal("notes", textarea.Id);
        Assert.Equal("300", textarea.Width);
        Assert.Equal("200", textarea.Height);
    }

    // ============== STACK SYNONYMS TESTS ==============

    [Fact]
    public void Parse_VStack_Synonym()
    {
        var markup = "<vstack><div /><div /></vstack>";
        var element = _processor.Parse(markup);

        Assert.IsType<Stack>(element);
        var stack = (Stack)element;
        Assert.Equal(Orientation.Vertical, stack.Orientation);
        Assert.Equal(2, stack.Children.Count);
    }

    [Fact]
    public void Parse_VBox_Synonym()
    {
        var markup = "<vbox><label text=\"A\" /><label text=\"B\" /></vbox>";
        var element = _processor.Parse(markup);

        Assert.IsType<Stack>(element);
        var stack = (Stack)element;
        Assert.Equal(Orientation.Vertical, stack.Orientation);
    }

    [Fact]
    public void Parse_HStack_Synonym()
    {
        var markup = "<hstack><div /><div /></hstack>";
        var element = _processor.Parse(markup);

        Assert.IsType<Stack>(element);
        var stack = (Stack)element;
        Assert.Equal(Orientation.Horizontal, stack.Orientation);
        Assert.Equal(2, stack.Children.Count);
    }

    [Fact]
    public void Parse_HBox_Synonym()
    {
        var markup = "<hbox><label text=\"X\" /><label text=\"Y\" /></hbox>";
        var element = _processor.Parse(markup);

        Assert.IsType<Stack>(element);
        var stack = (Stack)element;
        Assert.Equal(Orientation.Horizontal, stack.Orientation);
    }

    // ============== GRID WITH ROW/COLUMN TESTS ==============

    [Fact]
    public void Parse_Grid_WithRow()
    {
        var markup = @"<grid>
<row height=""2rem"">
    <div width=""100"" bg=""blue"" />
    <div width=""*"" bg=""green"" />
</row>
</grid>";
        var element = _processor.Parse(markup);

        Assert.IsType<Grid>(element);
        var grid = (Grid)element;
        Assert.NotEmpty(grid.Children);
    }

    [Fact]
    public void Parse_Grid_WithColumn()
    {
        var markup = @"<grid columns=""200, *"">
<column>
    <div height=""100"" bg=""blue"" />
    <div height=""*"" bg=""green"" />
</column>
</grid>";
        var element = _processor.Parse(markup);

        Assert.IsType<Grid>(element);
        var grid = (Grid)element;
        Assert.NotEmpty(grid.Children);
    }

    // ============== COMMON ATTRIBUTES TESTS ==============

    [Fact]
    public void Parse_Visibility_Attribute()
    {
        var markup = "<div visibility=\"hidden\" />";
        var element = _processor.Parse(markup);

        Assert.IsType<Div>(element);
        var div = (Div)element;
        Assert.Equal("hidden", div.Visibility);
    }

    [Fact]
    public void Parse_Opacity_Attribute()
    {
        var markup = "<div opacity=\"0.5\" />";
        var element = _processor.Parse(markup);

        Assert.IsType<Div>(element);
        var div = (Div)element;
        Assert.Equal(0.5, div.Opacity);
    }

    [Fact]
    public void Parse_ZIndex_Attribute()
    {
        var markup = "<div z-index=\"10\" />";
        var element = _processor.Parse(markup);

        Assert.IsType<Div>(element);
        var div = (Div)element;
        Assert.Equal(10, div.ZIndex);
    }

    [Fact]
    public void Parse_XY_Positioning()
    {
        var markup = "<div x=\"50\" y=\"100\" width=\"200\" height=\"150\" />";
        var element = _processor.Parse(markup);

        Assert.IsType<Div>(element);
        var div = (Div)element;
        Assert.Equal(50, div.X);
        Assert.Equal(100, div.Y);
    }

    [Fact]
    public void Parse_Clip_Attribute()
    {
        var markup = "<stack clip=\"true\"><div /></stack>";
        var element = _processor.Parse(markup);

        Assert.IsType<Stack>(element);
        var stack = (Stack)element;
        Assert.True(stack.Clip);
    }

    [Fact]
    public void Parse_Spacing_SingleValue()
    {
        var markup = "<stack spacing=\"10\"><div /><div /></stack>";
        var element = _processor.Parse(markup);

        Assert.IsType<Stack>(element);
        var stack = (Stack)element;
        Assert.Equal(10, stack.Spacing);
    }

    [Fact]
    public void Parse_Class_Attribute()
    {
        var markup = "<div class=\"primary secondary\" />";
        var element = _processor.Parse(markup);

        Assert.IsType<Div>(element);
        var div = (Div)element;
        Assert.Equal("primary secondary", div.Class);
    }

    // ============== DATA BINDING TESTS ==============

    [Fact]
    public void Parse_DataBinding_Width()
    {
        var markup = "<div width=\"@currentWidth\" height=\"100\" />";
        var element = _processor.Parse(markup);

        Assert.IsType<Div>(element);
        var div = (Div)element;
        // Property binding should be created for width
        Assert.NotNull(div.Width);
    }

    [Fact]
    public void Parse_DataBinding_Text()
    {
        var markup = "<label text=\"@stringValue\" />";
        var element = _processor.Parse(markup);

        Assert.IsType<Label>(element);
        var label = (Label)element;
        // Property binding should be created for text
        Assert.NotNull(label.Text);
    }

    // ============== CONTROL FLOW - FOR WITH STEP ==============

    [Fact]
    public void Parse_ForDirective_WithNegativeStep()
    {
        var markup = @"<stack>
@for i=2 count=3 step=-1
{
    <label text=""@i"" />
}
</stack>";
        var element = _processor.Parse(markup);

        Assert.IsType<Stack>(element);
        var stack = (Stack)element;
        Assert.Equal(3, stack.Children.Count);
        // Should contain: 2, 1, 0
        var labels = stack.Children.Cast<Label>();
        Assert.Equal("2", labels.ElementAt(0).Text);
        Assert.Equal("1", labels.ElementAt(1).Text);
        Assert.Equal("0", labels.ElementAt(2).Text);
    }

    [Fact]
    public void Parse_ForDirective_WithCustomStep()
    {
        var markup = @"<stack>
@for i=0 count=3 step=2
{
    <label text=""@i"" />
}
</stack>";
        var element = _processor.Parse(markup);

        Assert.IsType<Stack>(element);
        var stack = (Stack)element;
        Assert.Equal(3, stack.Children.Count);
        var labels = stack.Children.Cast<Label>();
        Assert.Equal("0", labels.ElementAt(0).Text);
        Assert.Equal("2", labels.ElementAt(1).Text);
        Assert.Equal("4", labels.ElementAt(2).Text);
    }

    // ============== CONTROL FLOW - SWITCH WITH STRING ==============

    [Fact]
    public void Parse_SwitchDirective_WithStringCase()
    {
        var markup = @"<div>
@switch stringValue
{
    case ""test""
    {
        <label text=""Matched String"" />
    }
    default
    {
        <label text=""No Match"" />
    }
}
</div>";
        var element = _processor.Parse(markup);

        Assert.IsType<Div>(element);
        var div = (Div)element;
        Assert.Single(div.Children);
        var label = (Label)div.Children[0];
        Assert.Equal("Matched String", label.Text);
    }

    [Fact]
    public void Parse_SwitchDirective_WithMultipleCases()
    {
        var markup = @"<div>
@switch identifierany
{
    case 100
    {
        <label text=""Hundred"" />
    }
    case 500
    {
        <label text=""FiveHundred"" />
    }
    default
    {
        <label text=""Other"" />
    }
}
</div>";
        var element = _processor.Parse(markup);

        Assert.IsType<Div>(element);
        var div = (Div)element;
        Assert.Single(div.Children);
        var label = (Label)div.Children[0];
        Assert.Equal("FiveHundred", label.Text);
    }

    [Fact]
    public void ControlFlow_SwitchDirective_WithVariableCase()
    {
        var parser = new SUIMControlFlowParser(_model);
        var markup = @"@switch identifierany
{
    case @identifier2
    {
        <label text=""Variable Match"" />
    }
    default
    {
        <label text=""No Match"" />
    }
}";
        var expanded = parser.ExpandDirectives(markup);
        Assert.Contains("Variable Match", expanded);
    }

    // ============== CONTROL FLOW - FOREACH TESTS ==============

    [Fact]
    public void Parse_ForEach_WithCollection()
    {
        var markup = @"<stack>
@foreach item in Collection
{
    <label text=""@item"" />
}
</stack>";
        var element = _processor.Parse(markup);

        Assert.IsType<Stack>(element);
        var stack = (Stack)element;
        Assert.Equal(2, stack.Children.Count);
        var labels = stack.Children.Cast<Label>();
        Assert.Equal("item1", labels.ElementAt(0).Text);
        Assert.Equal("item2", labels.ElementAt(1).Text);
    }

    [Fact]
    public void Parse_ForEach_WithCollectionProperty()
    {
        var markup = @"<stack>
@foreach item in items
{
    <label text=""@item.Name"" />
}
</stack>";
        var element = _processor.Parse(markup);

        Assert.IsType<Stack>(element);
        var stack = (Stack)element;
        Assert.Equal(2, stack.Children.Count);
        var labels = stack.Children.Cast<Label>();
        Assert.Equal("Apple", labels.ElementAt(0).Text);
        Assert.Equal("Banana", labels.ElementAt(1).Text);
    }

    [Fact]
    public void Parse_ForEach_WithRange()
    {
        var markup = @"<stack>
@foreach i in 0..3
{
    <label text=""@i"" />
}
</stack>";
        var element = _processor.Parse(markup);

        Assert.IsType<Stack>(element);
        var stack = (Stack)element;
        Assert.Equal(3, stack.Children.Count);
        var labels = stack.Children.Cast<Label>();
        Assert.Equal("0", labels.ElementAt(0).Text);
        Assert.Equal("1", labels.ElementAt(1).Text);
        Assert.Equal("2", labels.ElementAt(2).Text);
    }

    // ============== COMPLEX NESTING & COMBINATIONS ==============

    [Fact]
    public void Parse_Nested_GridAndStack_Complex()
    {
        var markup = @"<grid columns=""*,*"" rows=""auto,*"">
<stack grid.row=""0"" grid.column=""0"" orientation=""horizontal"" spacing=""10"">
    <label text=""Top Left"" />
    <label text=""Top"" />
</stack>
<div grid.row=""1"" grid.column=""0"" grid.columnspan=""2"" bg=""lightgray"" />
</grid>";
        var element = _processor.Parse(markup);

        Assert.IsType<Grid>(element);
        var grid = (Grid)element;
        Assert.NotEmpty(grid.GridChildren);
    }

    [Fact]
    public void Parse_Dock_WithAllEdges()
    {
        var markup = @"<dock lastchildfill=""true"">
<div dock.edge=""left"" width=""50"" />
<div dock.edge=""right"" width=""50"" />
<div dock.edge=""top"" height=""30"" />
<div dock.edge=""bottom"" height=""30"" />
<div bg=""white"" />
</dock>";
        var element = _processor.Parse(markup);

        Assert.IsType<Dock>(element);
        var dock = (Dock)element;
        Assert.Equal(5, dock.DockChildren.Count);
        Assert.True(dock.LastChildFill);
    }

    [Fact]
    public void Parse_ControlFlow_IfWithin_Button()
    {
        var markup = @"<button>
@if identifierbool
{
    <label text=""Click Me"" />
}
else
{
    <label text=""Disabled"" />
}
</button>";
        var element = _processor.Parse(markup);

        Assert.IsType<Button>(element);
        var button = (Button)element;
        Assert.Single(button.Children);
        var label = (Label)button.Children[0];
        Assert.Equal("Click Me", label.Text);
    }

    // ============== COLOR FORMATTING TESTS ==============

    [Fact]
    public void Parse_Color_Hex()
    {
        var markup = "<div bg=\"#FF0000\" />";
        var element = _processor.Parse(markup);

        Assert.IsType<Div>(element);
        var div = (Div)element;
        Assert.Equal("#FF0000", div.Background);
    }

    [Fact]
    public void Parse_Color_RGBA()
    {
        var markup = "<div bg=\"255,0,0,255\" />";
        var element = _processor.Parse(markup);

        Assert.IsType<Div>(element);
        var div = (Div)element;
        Assert.Equal("255,0,0,255", div.Background);
    }

    [Fact]
    public void Parse_Color_Named()
    {
        var markup = "<div bg=\"Red\" />";
        var element = _processor.Parse(markup);

        Assert.IsType<Div>(element);
        var div = (Div)element;
        Assert.Equal("Red", div.Background);
    }

    // ============== SIZING UNITS TESTS ==============

    [Fact]
    public void Parse_Size_Pixels()
    {
        var markup = "<div width=\"100\" height=\"200\" />";
        var element = _processor.Parse(markup);

        Assert.IsType<Div>(element);
        var div = (Div)element;
        Assert.Equal("100", div.Width);
        Assert.Equal("200", div.Height);
    }

    [Fact]
    public void Parse_Size_Star()
    {
        var markup = "<div width=\"*\" height=\"2*\" />";
        var element = _processor.Parse(markup);

        Assert.IsType<Div>(element);
        var div = (Div)element;
        Assert.Equal("*", div.Width);
        Assert.Equal("2*", div.Height);
    }

    [Fact]
    public void Parse_Size_Auto()
    {
        var markup = "<label text=\"Auto\" width=\"auto\" />";
        var element = _processor.Parse(markup);

        Assert.IsType<Label>(element);
        var label = (Label)element;
        Assert.Equal("auto", label.Width);
    }

    // ============== ADDITIONAL INPUT TYPES TESTS ==============

    [Fact]
    public void Parse_Input_EmailType()
    {
        var markup = "<input type=\"email\" placeholder=\"your@email.com\" />";
        var element = _processor.Parse(markup);

        Assert.IsType<Input>(element);
        var input = (Input)element;
        Assert.Equal(InputType.Email, input.Type);
    }

    [Fact]
    public void Parse_Input_UrlType()
    {
        var markup = "<input type=\"url\" placeholder=\"https://example.com\" />";
        var element = _processor.Parse(markup);

        Assert.IsType<Input>(element);
        var input = (Input)element;
        Assert.Equal(InputType.Url, input.Type);
    }

    [Fact]
    public void Parse_Input_PasswordType()
    {
        var markup = "<input type=\"password\" />";
        var element = _processor.Parse(markup);

        Assert.IsType<Input>(element);
        var input = (Input)element;
        Assert.Equal(InputType.Password, input.Type);
    }

    [Fact]
    public void Parse_Input_DateType()
    {
        var markup = "<input type=\"date\" />";
        var element = _processor.Parse(markup);

        Assert.IsType<Input>(element);
        var input = (Input)element;
        Assert.Equal(InputType.Date, input.Type);
    }

    [Fact]
    public void Parse_Input_TimeType()
    {
        var markup = "<input type=\"time\" />";
        var element = _processor.Parse(markup);

        Assert.IsType<Input>(element);
        var input = (Input)element;
        Assert.Equal(InputType.Time, input.Type);
    }

    [Fact]
    public void Parse_Input_DatetimeLocalType()
    {
        var markup = "<input type=\"datetime-local\" />";
        var element = _processor.Parse(markup);

        Assert.IsType<Input>(element);
        var input = (Input)element;
        Assert.Equal(InputType.DatetimeLocal, input.Type);
    }

    [Fact]
    public void Parse_Input_RangeType()
    {
        var markup = "<input type=\"range\" min=\"0\" max=\"100\" />";
        var element = _processor.Parse(markup);

        Assert.IsType<Input>(element);
        var input = (Input)element;
        Assert.Equal(InputType.Range, input.Type);
    }

    [Fact]
    public void Parse_Input_CheckboxType()
    {
        var markup = "<input type=\"checkbox\" />";
        var element = _processor.Parse(markup);

        Assert.IsType<Input>(element);
        var input = (Input)element;
        Assert.Equal(InputType.Checkbox, input.Type);
    }

    [Fact]
    public void Parse_Input_RadioType()
    {
        var markup = "<input type=\"radio\" />";
        var element = _processor.Parse(markup);

        Assert.IsType<Input>(element);
        var input = (Input)element;
        Assert.Equal(InputType.Radio, input.Type);
    }

    // ============== TEXTAREA TESTS ==============

    [Fact]
    public void Parse_Textarea_WithPlaceholder()
    {
        var markup = "<textarea placeholder=\"Enter description\" rows=\"5\" columns=\"40\" />";
        var element = _processor.Parse(markup);

        Assert.IsType<TextArea>(element);
        var textarea = (TextArea)element;
        Assert.Equal("Enter description", textarea.Placeholder);
        Assert.Equal(5, textarea.Rows);
        Assert.Equal(40, textarea.Columns);
    }

    // ============== SELECT TESTS ==============

    [Fact]
    public void Parse_Option_Element()
    {
        var markup = "<option value=\"test-value\">Test Label</option>";
        var element = _processor.Parse(markup);

        Assert.IsType<Option>(element);
        var option = (Option)element;
        Assert.Equal("test-value", option.Value);
        Assert.Single(option.Children);
        var textNode = option.Children[0] as BaseText;
        Assert.Equal("Test Label", textNode?.Text);
    }

    // ============== IMAGE STRETCH VARIANTS ==============

    [Fact]
    public void Parse_Image_Stretch_None()
    {
        var markup = "<image source=\"sprite\" stretch=\"none\" />";
        var element = _processor.Parse(markup);

        Assert.IsType<Image>(element);
        var image = (Image)element;
        Assert.Equal(ImageStretch.None, image.Stretch);
    }

    [Fact]
    public void Parse_Image_Stretch_Fill()
    {
        var markup = "<image source=\"sprite\" stretch=\"fill\" />";
        var element = _processor.Parse(markup);

        Assert.IsType<Image>(element);
        var image = (Image)element;
        Assert.Equal(ImageStretch.Fill, image.Stretch);
    }

    [Fact]
    public void Parse_Image_Stretch_UniformToFill()
    {
        var markup = "<image source=\"sprite\" stretch=\"uniformtofill\" />";
        var element = _processor.Parse(markup);

        Assert.IsType<Image>(element);
        var image = (Image)element;
        Assert.Equal(ImageStretch.UniformToFill, image.Stretch);
    }

    // ============== ANCHOR VARIANTS ==============

    [Fact]
    public void Parse_Anchor_TopRight()
    {
        var markup = "<div anchor=\"TopRight\" />";
        var element = _processor.Parse(markup);

        Assert.IsType<Div>(element);
        var div = (Div)element;
        Assert.Equal(Anchor.TopRight, div.Anchor);
    }

    [Fact]
    public void Parse_Anchor_BottomLeft()
    {
        var markup = "<div anchor=\"BottomLeft\" />";
        var element = _processor.Parse(markup);

        Assert.IsType<Div>(element);
        var div = (Div)element;
        Assert.Equal(Anchor.BottomLeft, div.Anchor);
    }

    [Fact]
    public void Parse_Anchor_BottomRight()
    {
        var markup = "<div anchor=\"BottomRight\" />";
        var element = _processor.Parse(markup);

        Assert.IsType<Div>(element);
        var div = (Div)element;
        Assert.Equal(Anchor.BottomRight, div.Anchor);
    }

    [Fact]
    public void Parse_Anchor_Center()
    {
        var markup = "<div anchor=\"Center\" />";
        var element = _processor.Parse(markup);

        Assert.IsType<Div>(element);
        var div = (Div)element;
        Assert.Equal(Anchor.Center, div.Anchor);
    }

    // ============== DOCK EDGE VARIANTS ==============

    [Fact]
    public void Parse_Dock_EdgeBottom()
    {
        var markup = @"<dock>
<div dock.edge=""bottom"" height=""50"" />
<div />
</dock>";
        var element = _processor.Parse(markup);

        Assert.IsType<Dock>(element);
        var dock = (Dock)element;
        Assert.Equal(DockEdge.Bottom, dock.DockChildren[0].Edge);
    }

    [Fact]
    public void Parse_Dock_WithoutLastChildFill()
    {
        var markup = @"<dock lastchildfill=""false"">
<div dock.edge=""left"" width=""50"" />
<div bg=""white"" />
</dock>";
        var element = _processor.Parse(markup);

        Assert.IsType<Dock>(element);
        var dock = (Dock)element;
        Assert.False(dock.LastChildFill);
    }

    // ============== MORE COMMON ATTRIBUTES ==============

    [Fact]
    public void Parse_Opacity_FullyOpaque()
    {
        var markup = "<div opacity=\"1.0\" />";
        var element = _processor.Parse(markup);

        Assert.IsType<Div>(element);
        var div = (Div)element;
        Assert.Equal(1.0f, div.Opacity);
    }

    [Fact]
    public void Parse_Opacity_FullyTransparent()
    {
        var markup = "<div opacity=\"0.0\" />";
        var element = _processor.Parse(markup);

        Assert.IsType<Div>(element);
        var div = (Div)element;
        Assert.Equal(0.0f, div.Opacity);
    }

    [Fact]
    public void Parse_Opacity_PartialTransparency()
    {
        var markup = "<div opacity=\"0.75\" />";
        var element = _processor.Parse(markup);

        Assert.IsType<Div>(element);
        var div = (Div)element;
        Assert.Equal(0.75f, div.Opacity);
    }

    [Fact]
    public void Parse_ZIndex_Negative()
    {
        var markup = "<div z-index=\"-5\" />";
        var element = _processor.Parse(markup);

        Assert.IsType<Div>(element);
        var div = (Div)element;
        Assert.Equal(-5, div.ZIndex);
    }

    [Fact]
    public void Parse_ZIndex_Large()
    {
        var markup = "<div z-index=\"1000\" />";
        var element = _processor.Parse(markup);

        Assert.IsType<Div>(element);
        var div = (Div)element;
        Assert.Equal(1000, div.ZIndex);
    }

    [Fact]
    public void Parse_MultipleClasses()
    {
        var markup = "<div class=\"primary secondary large\" />";
        var element = _processor.Parse(markup);

        Assert.IsType<Div>(element);
        var div = (Div)element;
        Assert.Equal("primary secondary large", div.Class);
    }

    // ============== LABEL WITH OPTIONAL TEXT ==============

    [Fact]
    public void Parse_Label_WithoutWrap()
    {
        var markup = "<label text=\"Test\" wrap=\"false\" />";
        var element = _processor.Parse(markup);

        Assert.IsType<Label>(element);
        var label = (Label)element;
        Assert.False(label.Wrap);
    }

    [Fact]
    public void Parse_Label_WithColor()
    {
        var markup = "<label text=\"Colored\" color=\"blue\" />";
        var element = _processor.Parse(markup);

        Assert.IsType<Label>(element);
        var label = (Label)element;
        Assert.Equal("blue", label.Color);
    }

    // ============== VISIBILITY VARIANTS ==============

    [Fact]
    public void Parse_Visibility_Visible()
    {
        var markup = "<div visibility=\"visible\" />";
        var element = _processor.Parse(markup);

        Assert.IsType<Div>(element);
        var div = (Div)element;
        Assert.Equal("visible", div.Visibility);
    }

    [Fact]
    public void Parse_Visibility_Collapsed()
    {
        var markup = "<div visibility=\"collapsed\" />";
        var element = _processor.Parse(markup);

        Assert.IsType<Div>(element);
        var div = (Div)element;
        Assert.Equal("collapsed", div.Visibility);
    }

    // ============== GRID SPAN EDGE CASES ==============

    [Fact]
    public void Parse_Grid_DefaultSpans()
    {
        var markup = @"<grid>
<div grid.row=""0"" grid.column=""0"" />
</grid>";
        var element = _processor.Parse(markup);

        Assert.IsType<Grid>(element);
        var grid = (Grid)element;
        var child = grid.GridChildren[0];
        Assert.Equal(1, child.RowSpan);
        Assert.Equal(1, child.ColumnSpan);
    }

    // ============== STRESS TEST: DEEPLY NESTED ==============

    [Fact]
    public void Parse_DeeplyNested()
    {
        var markup = @"<div>
<stack>
<div>
<stack>
<label text=""Deep"" />
</stack>
</div>
</stack>
</div>";
        var element = _processor.Parse(markup);

        Assert.IsType<Div>(element);
        var div = (Div)element;
        var stack = (Stack)div.Children[0];
        var innerDiv = (Div)stack.Children[0];
        var innerStack = (Stack)innerDiv.Children[0];
        var label = (Label)innerStack.Children[0];
        Assert.Equal("Deep", label.Text);
    }

    // ============== CONTROL FLOW - IF WITHOUT ELSE ==============

    [Fact]
    public void Parse_IfDirective_False_NoElement()
    {
        var markup = @"<div>
@if identifierbool3
{
    <label text=""Should not appear"" />
}
</div>";
        var element = _processor.Parse(markup);

        Assert.IsType<Div>(element);
        var div = (Div)element;
        Assert.Empty(div.Children);
    }

    // ============== BUTTON WITH NESTED CONTENT ==============

    [Fact]
    public void Parse_Button_WithNestedElements()
    {
        var markup = @"<button>
<stack>
<label text=""Icon"" />
<label text=""Label"" />
</stack>
</button>";
        var element = _processor.Parse(markup);

        Assert.IsType<Button>(element);
        var button = (Button)element;
        Assert.Single(button.Children);
        var stack = (Stack)button.Children[0];
        Assert.Equal(2, stack.Children.Count);
    }

    // ============== INPUT WITH VALUE ==============

    [Fact]
    public void Parse_Input_WithValue()
    {
        var markup = "<input type=\"text\" value=\"default-value\" />";
        var element = _processor.Parse(markup);

        Assert.IsType<Input>(element);
        var input = (Input)element;
        Assert.Equal("default-value", input.Value);
    }

    // ============== TEXT NODES AS BASETEXT ==============

    [Fact]
    public void Parse_PlainText_CreatesLabel()
    {
        var markup = "<div>Simple text</div>";
        var element = _processor.Parse(markup);

        Assert.IsType<Div>(element);
        var div = (Div)element;
        Assert.Single(div.Children);
        Assert.IsType<Label>(div.Children[0]);
        var label = (Label)div.Children[0];
        Assert.Equal("Simple text", label.Text);
    }

    [Fact]
    public void Parse_MixedTextAndElements()
    {
        var markup = @"<stack>
Text before
<label text=""Label"" />
Text after
</stack>";
        var element = _processor.Parse(markup);

        Assert.IsType<Stack>(element);
        var stack = (Stack)element;
        Assert.Equal(3, stack.Children.Count);
        
        // First child: text "Text before"
        Assert.IsType<Label>(stack.Children[0]);
        Assert.Equal("Text before", ((Label)stack.Children[0]).Text);
        
        // Second child: label element
        Assert.IsType<Label>(stack.Children[1]);
        Assert.Equal("Label", ((Label)stack.Children[1]).Text);
        
        // Third child: text "Text after"
        Assert.IsType<Label>(stack.Children[2]);
        Assert.Equal("Text after", ((Label)stack.Children[2]).Text);
    }

    [Fact]
    public void Parse_MultilineText_TrimsWhitespace()
    {
        var markup = @"<div>
            Multi-line
            text content
        </div>";
        var element = _processor.Parse(markup);

        Assert.IsType<Div>(element);
        var div = (Div)element;
        Assert.Single(div.Children);
        var label = (Label)div.Children[0];
        // Should be trimmed and preserved as single text
        Assert.Contains("Multi-line", label.Text);
    }

    [Fact]
    public void Parse_EmptyText_Ignored()
    {
        var markup = @"<div>
            
            <label text=""Only label"" />
            
        </div>";
        var element = _processor.Parse(markup);

        Assert.IsType<Div>(element);
        var div = (Div)element;
        // Only whitespace before and after should be ignored
        Assert.Single(div.Children);
        Assert.IsType<Label>(div.Children[0]);
    }
}

public record struct Item(string Name);

