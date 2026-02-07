namespace SUIM.Tests;

using Xunit;
using SUIM.Components;

public class MarkupParserTests
{
    private readonly object _model =
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
        };

    [Fact]
    public void Parse_DivWithAttributes()
    {
        var markup = "<div id=\"main\" width=\"100\" height=\"200\" halign=\"center\" valign=\"top\" margin=\"10\" padding=\"5\" bg=\"blue\" />";
        var (element, _) = new MarkupParser(_model).Parse(markup);

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
        var (element, _) = new MarkupParser(_model).Parse(markup);

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
        var (element, _) = new MarkupParser(_model).Parse(markup);

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
        var (element, _) = new MarkupParser(_model).Parse(markup);

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
        var (element, _) = new MarkupParser(_model).Parse(markup);

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
        var (element, _) = new MarkupParser(_model).Parse(markup);

        Assert.IsType<Overlay>(element);
        var overlay = (Overlay)element;
        Assert.Equal(2, overlay.Children.Count);
    }

    [Fact]
    public void Parse_Label()
    {
        var markup = "<label text=\"Test Label\" />";
        var (element, _) = new MarkupParser(_model).Parse(markup);

        Assert.IsType<Label>(element);
        var label = (Label)element;
        Assert.Equal("Test Label", label.Text);
    }

    [Fact]
    public void Parse_Button()
    {
        var markup = "<button><label text=\"Click me\" /></button>";
        var (element, _) = new MarkupParser(_model).Parse(markup);

        Assert.IsType<Button>(element);
        var button = (Button)element;
        Assert.Single(button.Children);
        Assert.IsType<Label>(button.Children[0]);
    }

    [Fact]
    public void Parse_UnknownTag_Throws()
    {
        var markup = "<unknown />";
        Assert.Throws<NotSupportedException>(() => new MarkupParser(_model).Parse(markup));
    }

    [Fact]
    public void Parse_InvalidXml_Throws()
    {
        var markup = "<div><unclosed>";
        Assert.Throws<System.Xml.XmlException>(() => new MarkupParser(_model).Parse(markup));
    }

    [Fact]
    public void Parse_EmptyMarkup_Throws()
    {
        var markup = "";
        Assert.Throws<System.Xml.XmlException>(() => new MarkupParser(_model).Parse(markup));
    }

    [Fact]
    public void Parse_NestedElements()
    {
        var markup = "<stack><div><label text=\"Nested\" /></div></stack>";
        var (element, _) = new MarkupParser(_model).Parse(markup);

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
        var (element, _) = new MarkupParser(_model).Parse(markup);

        Assert.IsType<Div>(element);
        var div = (Div)element;
        Assert.Equal(Anchor.TopLeft, div.Anchor);
    }

    [Fact]
    public void Parse_SynonymAttributes()
    {
        var markup = "<div halign=\"right\" valign=\"bottom\" />";
        var (element, _) = new MarkupParser(_model).Parse(markup);

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
        var (element, _) = new MarkupParser(_model).Parse(markup);

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
        var (element, _) = new MarkupParser(_model).Parse(markup);

        Assert.IsType<Dock>(element);
        var dock = (Dock)element;
        Assert.Single(dock.DockChildren);
        Assert.Equal(DockEdge.Left, dock.DockChildren[0].Edge);
    }

    // ============== CONTENT TAGS TESTS ==============

    [Fact]
    public void Parse_Button_WithSprites()
    {
        var markup = "<button>idle_sprite</button>";
        var (element, _) = new MarkupParser(_model).Parse(markup);

        Assert.IsType<Button>(element);
        var button = (Button)element;
        var child = button.Children[0] as BaseText;
        Assert.Equal("idle_sprite", child?.Text);
    }

    [Fact]
    public void Parse_Button_WithOnClick()
    {
        var markup = "<button onclick=\"HandleSubmit\">Submit</button>";
        var (element, _) = new MarkupParser(_model).Parse(markup);

        Assert.IsType<Button>(element);
        var button = (Button)element;
        Assert.True(button.EventHandlers.ContainsKey("click"));
    }

    [Fact]
    public void Parse_Input_TextType()
    {
        var markup = "<input type=\"text\" placeholder=\"Enter name\" />";
        var (element, _) = new MarkupParser(_model).Parse(markup);

        Assert.IsType<Input>(element);
        var input = (Input)element;
        Assert.Equal(InputType.Text, input.Type);
        Assert.Equal("Enter name", input.Placeholder);
    }

    [Fact]
    public void Parse_Input_NumberType_WithMinMax()
    {
        var markup = "<input type=\"number\" min=\"0\" max=\"100\" step=\"5\" />";
        var (element, _) = new MarkupParser(_model).Parse(markup);

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
        var (element, _) = new MarkupParser(_model).Parse(markup);

        Assert.IsType<Input>(element);
        var input = (Input)element;
        Assert.Equal("[0-9]{3}-[0-9]{3}-[0-9]{4}", input.Mask);
    }

    [Fact]
    public void Parse_Label_WithAllAttributes()
    {
        var markup = "<label text=\"Hello\" font=\"Arial\" fontsize=\"16\" color=\"#FF0000\" wrap=\"true\" />";
        var (element, _) = new MarkupParser(_model).Parse(markup);

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
        var (element, _) = new MarkupParser(_model).Parse(markup);

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
        var (element, _) = new MarkupParser(_model).Parse(markup);

        Assert.IsType<Select>(element);
        var select = (Select)element;
        Assert.Equal("dropdown", select.Id);
        Assert.Equal(2, select.Children.Count);
    }

    [Fact]
    public void Parse_Select_WithMultiple()
    {
        var markup = "<select multiple=\"true\"><option>Opt1</option><option>Opt2</option></select>";
        var (element, _) = new MarkupParser(_model).Parse(markup);

        Assert.IsType<Select>(element);
        var select = (Select)element;
        Assert.True(select.Multiple);
    }

    [Fact]
    public void Parse_Textarea()
    {
        var markup = "<textarea id=\"notes\" width=\"300\" height=\"200\" />";
        var (element, _) = new MarkupParser(_model).Parse(markup);

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
        var (element, _) = new MarkupParser(_model).Parse(markup);

        Assert.IsType<Stack>(element);
        var stack = (Stack)element;
        Assert.Equal(Orientation.Vertical, stack.Orientation);
        Assert.Equal(2, stack.Children.Count);
    }

    [Fact]
    public void Parse_VBox_Synonym()
    {
        var markup = "<vbox><label text=\"A\" /><label text=\"B\" /></vbox>";
        var (element, _) = new MarkupParser(_model).Parse(markup);

        Assert.IsType<Stack>(element);
        var stack = (Stack)element;
        Assert.Equal(Orientation.Vertical, stack.Orientation);
    }

    [Fact]
    public void Parse_HStack_Synonym()
    {
        var markup = "<hstack><div /><div /></hstack>";
        var (element, _) = new MarkupParser(_model).Parse(markup);

        Assert.IsType<Stack>(element);
        var stack = (Stack)element;
        Assert.Equal(Orientation.Horizontal, stack.Orientation);
        Assert.Equal(2, stack.Children.Count);
    }

    [Fact]
    public void Parse_HBox_Synonym()
    {
        var markup = "<hbox><label text=\"X\" /><label text=\"Y\" /></hbox>";
        var (element, _) = new MarkupParser(_model).Parse(markup);

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
        var (element, _) = new MarkupParser(_model).Parse(markup);

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
        var (element, _) = new MarkupParser(_model).Parse(markup);

        Assert.IsType<Grid>(element);
        var grid = (Grid)element;
        Assert.NotEmpty(grid.Children);
    }

    // ============== COMMON ATTRIBUTES TESTS ==============

    [Fact]
    public void Parse_Visibility_Attribute()
    {
        var markup = "<div visibility=\"hidden\" />";
        var (element, _) = new MarkupParser(_model).Parse(markup);

        Assert.IsType<Div>(element);
        var div = (Div)element;
        Assert.Equal("hidden", div.Visibility);
    }

    [Fact]
    public void Parse_Opacity_Attribute()
    {
        var markup = "<div opacity=\"0.5\" />";
        var (element, _) = new MarkupParser(_model).Parse(markup);

        Assert.IsType<Div>(element);
        var div = (Div)element;
        Assert.Equal(0.5, div.Opacity);
    }

    [Fact]
    public void Parse_ZIndex_Attribute()
    {
        var markup = "<div z-index=\"10\" />";
        var (element, _) = new MarkupParser(_model).Parse(markup);

        Assert.IsType<Div>(element);
        var div = (Div)element;
        Assert.Equal(10, div.ZIndex);
    }

    [Fact]
    public void Parse_XY_Positioning()
    {
        var markup = "<div x=\"50\" y=\"100\" width=\"200\" height=\"150\" />";
        var (element, _) = new MarkupParser(_model).Parse(markup);

        Assert.IsType<Div>(element);
        var div = (Div)element;
        Assert.Equal(50, div.X);
        Assert.Equal(100, div.Y);
    }

    [Fact]
    public void Parse_Clip_Attribute()
    {
        var markup = "<stack clip=\"true\"><div /></stack>";
        var (element, _) = new MarkupParser(_model).Parse(markup);

        Assert.IsType<Stack>(element);
        var stack = (Stack)element;
        Assert.True(stack.Clip);
    }

    [Fact]
    public void Parse_Spacing_SingleValue()
    {
        var markup = "<stack spacing=\"10\"><div /><div /></stack>";
        var (element, _) = new MarkupParser(_model).Parse(markup);

        Assert.IsType<Stack>(element);
        var stack = (Stack)element;
        Assert.Equal(10, stack.Spacing);
    }

    [Fact]
    public void Parse_Class_Attribute()
    {
        var markup = "<div class=\"primary secondary\" />";
        var (element, _) = new MarkupParser(_model).Parse(markup);

        Assert.IsType<Div>(element);
        var div = (Div)element;
        Assert.Equal("primary secondary", div.Class);
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
        var (element, _) = new MarkupParser(_model).Parse(markup);

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
        var (element, _) = new MarkupParser(_model).Parse(markup);

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
        var (element, _) = new MarkupParser(_model).Parse(markup);

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
        var (element, _) = new MarkupParser(_model).Parse(markup);

        Assert.IsType<Div>(element);
        var div = (Div)element;
        Assert.Equal("#FF0000", div.Background);
    }

    [Fact]
    public void Parse_Color_RGBA()
    {
        var markup = "<div bg=\"255,0,0,255\" />";
        var (element, _) = new MarkupParser(_model).Parse(markup);

        Assert.IsType<Div>(element);
        var div = (Div)element;
        Assert.Equal("255,0,0,255", div.Background);
    }

    [Fact]
    public void Parse_Color_Named()
    {
        var markup = "<div bg=\"Red\" />";
        var (element, _) = new MarkupParser(_model).Parse(markup);

        Assert.IsType<Div>(element);
        var div = (Div)element;
        Assert.Equal("Red", div.Background);
    }

    // ============== SIZING UNITS TESTS ==============

    [Fact]
    public void Parse_Size_Pixels()
    {
        var markup = "<div width=\"100\" height=\"200\" />";
        var (element, _) = new MarkupParser(_model).Parse(markup);

        Assert.IsType<Div>(element);
        var div = (Div)element;
        Assert.Equal("100", div.Width);
        Assert.Equal("200", div.Height);
    }

    [Fact]
    public void Parse_Size_Star()
    {
        var markup = "<div width=\"*\" height=\"2*\" />";
        var (element, _) = new MarkupParser(_model).Parse(markup);

        Assert.IsType<Div>(element);
        var div = (Div)element;
        Assert.Equal("*", div.Width);
        Assert.Equal("2*", div.Height);
    }

    [Fact]
    public void Parse_Size_Auto()
    {
        var markup = "<label text=\"Auto\" width=\"auto\" />";
        var (element, _) = new MarkupParser(_model).Parse(markup);

        Assert.IsType<Label>(element);
        var label = (Label)element;
        Assert.Equal("auto", label.Width);
    }

    // ============== ADDITIONAL INPUT TYPES TESTS ==============

    [Fact]
    public void Parse_Input_EmailType()
    {
        var markup = "<input type=\"email\" placeholder=\"your@email.com\" />";
        var (element, _) = new MarkupParser(_model).Parse(markup);

        Assert.IsType<Input>(element);
        var input = (Input)element;
        Assert.Equal(InputType.Email, input.Type);
    }

    [Fact]
    public void Parse_Input_UrlType()
    {
        var markup = "<input type=\"url\" placeholder=\"https://example.com\" />";
        var (element, _) = new MarkupParser(_model).Parse(markup);

        Assert.IsType<Input>(element);
        var input = (Input)element;
        Assert.Equal(InputType.Url, input.Type);
    }

    [Fact]
    public void Parse_Input_PasswordType()
    {
        var markup = "<input type=\"password\" />";
        var (element, _) = new MarkupParser(_model).Parse(markup);

        Assert.IsType<Input>(element);
        var input = (Input)element;
        Assert.Equal(InputType.Password, input.Type);
    }

    [Fact]
    public void Parse_Input_DateType()
    {
        var markup = "<input type=\"date\" />";
        var (element, _) = new MarkupParser(_model).Parse(markup);

        Assert.IsType<Input>(element);
        var input = (Input)element;
        Assert.Equal(InputType.Date, input.Type);
    }

    [Fact]
    public void Parse_Input_TimeType()
    {
        var markup = "<input type=\"time\" />";
        var (element, _) = new MarkupParser(_model).Parse(markup);

        Assert.IsType<Input>(element);
        var input = (Input)element;
        Assert.Equal(InputType.Time, input.Type);
    }

    [Fact]
    public void Parse_Input_DatetimeLocalType()
    {
        var markup = "<input type=\"datetime-local\" />";
        var (element, _) = new MarkupParser(_model).Parse(markup);

        Assert.IsType<Input>(element);
        var input = (Input)element;
        Assert.Equal(InputType.DatetimeLocal, input.Type);
    }

    [Fact]
    public void Parse_Input_RangeType()
    {
        var markup = "<input type=\"range\" min=\"0\" max=\"100\" />";
        var (element, _) = new MarkupParser(_model).Parse(markup);

        Assert.IsType<Input>(element);
        var input = (Input)element;
        Assert.Equal(InputType.Range, input.Type);
    }

    [Fact]
    public void Parse_Input_CheckboxType()
    {
        var markup = "<input type=\"checkbox\" />";
        var (element, _) = new MarkupParser(_model).Parse(markup);

        Assert.IsType<Input>(element);
        var input = (Input)element;
        Assert.Equal(InputType.Checkbox, input.Type);
    }

    [Fact]
    public void Parse_Input_RadioType()
    {
        var markup = "<input type=\"radio\" />";
        var (element, _) = new MarkupParser(_model).Parse(markup);

        Assert.IsType<Input>(element);
        var input = (Input)element;
        Assert.Equal(InputType.Radio, input.Type);
    }

    // ============== TEXTAREA TESTS ==============

    [Fact]
    public void Parse_Textarea_WithPlaceholder()
    {
        var markup = "<textarea placeholder=\"Enter description\" rows=\"5\" columns=\"40\" />";
        var (element, _) = new MarkupParser(_model).Parse(markup);

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
        var (element, _) = new MarkupParser(_model).Parse(markup);

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
        var (element, _) = new MarkupParser(_model).Parse(markup);

        Assert.IsType<Image>(element);
        var image = (Image)element;
        Assert.Equal(ImageStretch.None, image.Stretch);
    }

    [Fact]
    public void Parse_Image_Stretch_Fill()
    {
        var markup = "<image source=\"sprite\" stretch=\"fill\" />";
        var (element, _) = new MarkupParser(_model).Parse(markup);

        Assert.IsType<Image>(element);
        var image = (Image)element;
        Assert.Equal(ImageStretch.Fill, image.Stretch);
    }

    [Fact]
    public void Parse_Image_Stretch_UniformToFill()
    {
        var markup = "<image source=\"sprite\" stretch=\"uniformtofill\" />";
        var (element, _) = new MarkupParser(_model).Parse(markup);

        Assert.IsType<Image>(element);
        var image = (Image)element;
        Assert.Equal(ImageStretch.UniformToFill, image.Stretch);
    }

    // ============== ANCHOR VARIANTS ==============

    [Fact]
    public void Parse_Anchor_TopRight()
    {
        var markup = "<div anchor=\"TopRight\" />";
        var (element, _) = new MarkupParser(_model).Parse(markup);

        Assert.IsType<Div>(element);
        var div = (Div)element;
        Assert.Equal(Anchor.TopRight, div.Anchor);
    }

    [Fact]
    public void Parse_Anchor_BottomLeft()
    {
        var markup = "<div anchor=\"BottomLeft\" />";
        var (element, _) = new MarkupParser(_model).Parse(markup);

        Assert.IsType<Div>(element);
        var div = (Div)element;
        Assert.Equal(Anchor.BottomLeft, div.Anchor);
    }

    [Fact]
    public void Parse_Anchor_BottomRight()
    {
        var markup = "<div anchor=\"BottomRight\" />";
        var (element, _) = new MarkupParser(_model).Parse(markup);

        Assert.IsType<Div>(element);
        var div = (Div)element;
        Assert.Equal(Anchor.BottomRight, div.Anchor);
    }

    [Fact]
    public void Parse_Anchor_Center()
    {
        var markup = "<div anchor=\"Center\" />";
        var (element, _) = new MarkupParser(_model).Parse(markup);

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
        var (element, _) = new MarkupParser(_model).Parse(markup);

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
        var (element, _) = new MarkupParser(_model).Parse(markup);

        Assert.IsType<Dock>(element);
        var dock = (Dock)element;
        Assert.False(dock.LastChildFill);
    }

    // ============== MORE COMMON ATTRIBUTES ==============

    [Fact]
    public void Parse_Opacity_FullyOpaque()
    {
        var markup = "<div opacity=\"1.0\" />";
        var (element, _) = new MarkupParser(_model).Parse(markup);

        Assert.IsType<Div>(element);
        var div = (Div)element;
        Assert.Equal(1.0f, div.Opacity);
    }

    [Fact]
    public void Parse_Opacity_FullyTransparent()
    {
        var markup = "<div opacity=\"0.0\" />";
        var (element, _) = new MarkupParser(_model).Parse(markup);

        Assert.IsType<Div>(element);
        var div = (Div)element;
        Assert.Equal(0.0f, div.Opacity);
    }

    [Fact]
    public void Parse_Opacity_PartialTransparency()
    {
        var markup = "<div opacity=\"0.75\" />";
        var (element, _) = new MarkupParser(_model).Parse(markup);

        Assert.IsType<Div>(element);
        var div = (Div)element;
        Assert.Equal(0.75f, div.Opacity);
    }

    [Fact]
    public void Parse_ZIndex_Negative()
    {
        var markup = "<div z-index=\"-5\" />";
        var (element, _) = new MarkupParser(_model).Parse(markup);

        Assert.IsType<Div>(element);
        var div = (Div)element;
        Assert.Equal(-5, div.ZIndex);
    }

    [Fact]
    public void Parse_ZIndex_Large()
    {
        var markup = "<div z-index=\"1000\" />";
        var (element, _) = new MarkupParser(_model).Parse(markup);

        Assert.IsType<Div>(element);
        var div = (Div)element;
        Assert.Equal(1000, div.ZIndex);
    }

    [Fact]
    public void Parse_MultipleClasses()
    {
        var markup = "<div class=\"primary secondary large\" />";
        var (element, _) = new MarkupParser(_model).Parse(markup);

        Assert.IsType<Div>(element);
        var div = (Div)element;
        Assert.Equal("primary secondary large", div.Class);
    }

    // ============== LABEL WITH OPTIONAL TEXT ==============

    [Fact]
    public void Parse_Label_WithoutWrap()
    {
        var markup = "<label text=\"Test\" wrap=\"false\" />";
        var (element, _) = new MarkupParser(_model).Parse(markup);

        Assert.IsType<Label>(element);
        var label = (Label)element;
        Assert.False(label.Wrap);
    }

    [Fact]
    public void Parse_Label_WithColor()
    {
        var markup = "<label text=\"Colored\" color=\"blue\" />";
        var (element, _) = new MarkupParser(_model).Parse(markup);

        Assert.IsType<Label>(element);
        var label = (Label)element;
        Assert.Equal("blue", label.Color);
    }

    // ============== VISIBILITY VARIANTS ==============

    [Fact]
    public void Parse_Visibility_Visible()
    {
        var markup = "<div visibility=\"visible\" />";
        var (element, _) = new MarkupParser(_model).Parse(markup);

        Assert.IsType<Div>(element);
        var div = (Div)element;
        Assert.Equal("visible", div.Visibility);
    }

    [Fact]
    public void Parse_Visibility_Collapsed()
    {
        var markup = "<div visibility=\"collapsed\" />";
        var (element, _) = new MarkupParser(_model).Parse(markup);

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
        var (element, _) = new MarkupParser(_model).Parse(markup);

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
        var (element, _) = new MarkupParser(_model).Parse(markup);

        Assert.IsType<Div>(element);
        var div = (Div)element;
        var stack = (Stack)div.Children[0];
        var innerDiv = (Div)stack.Children[0];
        var innerStack = (Stack)innerDiv.Children[0];
        var label = (Label)innerStack.Children[0];
        Assert.Equal("Deep", label.Text);
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
        var (element, _) = new MarkupParser(_model).Parse(markup);

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
        var (element, _) = new MarkupParser(_model).Parse(markup);

        Assert.IsType<Input>(element);
        var input = (Input)element;
        Assert.Equal("default-value", input.Value);
    }

    // ============== TEXT NODES AS BASETEXT ==============

    [Fact]
    public void Parse_PlainText_CreatesLabel()
    {
        var markup = "<div>Simple text</div>";
        var (element, _) = new MarkupParser(_model).Parse(markup);

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
        var (element, _) = new MarkupParser(_model).Parse(markup);

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
        var (element, _) = new MarkupParser(_model).Parse(markup);

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
        var (element, _) = new MarkupParser(_model).Parse(markup);

        Assert.IsType<Div>(element);
        var div = (Div)element;
        // Only whitespace before and after should be ignored
        Assert.Single(div.Children);
        Assert.IsType<Label>(div.Children[0]);
    }

    // ============== SCROLL & BORDER TESTS ==============

    [Fact]
    public void Parse_Stack_WithScroll_Vertical()
    {
        var markup = @"<stack orientation=""vertical"" scroll=""vertical"">
<label text=""Item 1"" />
<label text=""Item 2"" />
<label text=""Item 3"" />
</stack>";
        var (element, _) = new MarkupParser(_model).Parse(markup);

        Assert.IsType<Scroll>(element);
        var scroll = (Scroll)element;
        Assert.Equal(ScrollDirection.Vertical, scroll.Direction);
        
        Assert.Single(scroll.Children);
        Assert.IsType<Stack>(scroll.Children[0]);
        var stack = (Stack)scroll.Children[0];
        Assert.Equal(Orientation.Vertical, stack.Orientation);
        Assert.Equal(3, stack.Children.Count);
    }

    [Fact]
    public void Parse_Stack_WithScroll_Horizontal()
    {
        var markup = @"<stack orientation=""horizontal"" scroll=""horizontal"">
<label text=""Item 1"" />
<label text=""Item 2"" />
</stack>";
        var (element, _) = new MarkupParser(_model).Parse(markup);

        Assert.IsType<Scroll>(element);
        var scroll = (Scroll)element;
        Assert.Equal(ScrollDirection.Horizontal, scroll.Direction);
        
        Assert.Single(scroll.Children);
        Assert.IsType<Stack>(scroll.Children[0]);
        var stack = (Stack)scroll.Children[0];
        Assert.Equal(Orientation.Horizontal, stack.Orientation);
        Assert.Equal(2, stack.Children.Count);
    }

    [Fact]
    public void Parse_Stack_WithScroll_Both()
    {
        var markup = @"<stack scroll=""both"">
<div width=""1000"" height=""800"" />
</stack>";
        var (element, _) = new MarkupParser(_model).Parse(markup);

        Assert.IsType<Scroll>(element);
        var scroll = (Scroll)element;
        Assert.Equal(ScrollDirection.Both, scroll.Direction);
        
        Assert.Single(scroll.Children);
        Assert.IsType<Stack>(scroll.Children[0]);
    }

    [Fact]
    public void Parse_Stack_WithScroll_WithAllAttributes()
    {
        var markup = @"<stack orientation=""vertical"" scroll=""vertical"" width=""400"" height=""300"" spacing=""5"">
<label text=""Scrollable Item 1"" />
<label text=""Scrollable Item 2"" />
<label text=""Scrollable Item 3"" />
</stack>";
        var (element, _) = new MarkupParser(_model).Parse(markup);

        Assert.IsType<Scroll>(element);
        var scroll = (Scroll)element;
        Assert.Equal(ScrollDirection.Vertical, scroll.Direction);
        Assert.Equal("400", scroll.Width);
        Assert.Equal("300", scroll.Height);
        
        Assert.Single(scroll.Children);
        var stack = (Stack)scroll.Children[0];
        Assert.IsType<Stack>(stack);
        Assert.Equal(Orientation.Vertical, stack.Orientation);
        // Spacing is component specific, goes to stack
        Assert.Equal(5, stack.Spacing);
        Assert.Equal(3, stack.Children.Count);
    }

    [Fact]
    public void Parse_Border_WithThicknessAndColor()
    {
        var markup = @"<border thickness=""2"" color=""#FF0000"">
<label text=""Bordered Content"" />
</border>";
        var (element, _) = new MarkupParser(_model).Parse(markup);

        Assert.IsType<Border>(element);
        var border = (Border)element;
        Assert.Single(border.Children);
        Assert.Equal("#FF0000", border.BorderColor);
    }

    [Fact]
    public void Parse_Border_WithMultipleSideThickness()
    {
        var markup = @"<border thickness=""5,10,5,10"" color=""blue"">
<div width=""200"" height=""100"" />
</border>";
        var (element, _) = new MarkupParser(_model).Parse(markup);

        Assert.IsType<Border>(element);
        var border = (Border)element;
        Assert.NotNull(border.BorderThickness);
        Assert.Equal("blue", border.BorderColor);
        Assert.Single(border.Children);
    }

    [Fact]
    public void Parse_Border_WithTwoValueThickness()
    {
        var markup = @"<border thickness=""3,6"" color=""green"">
<label text=""Border Test"" />
</border>";
        var (element, _) = new MarkupParser(_model).Parse(markup);

        Assert.IsType<Border>(element);
        var border = (Border)element;
        Assert.NotNull(border.BorderThickness);
        Assert.Equal("green", border.BorderColor);
    }

    [Fact]
    public void Parse_Div_WithBorder()
    {
        var markup = @"<div width=""300"" height=""200"" bg=""lightgray"">
<border thickness=""2"" color=""red"">
<label text=""Bordered Inner Content"" />
</border>
</div>";
        var (element, _) = new MarkupParser(_model).Parse(markup);

        Assert.IsType<Div>(element);
        var div = (Div)element;
        Assert.Equal("300", div.Width);
        Assert.Equal("200", div.Height);
        Assert.Equal("lightgray", div.Background);
        Assert.Single(div.Children);
        
        var border = (Border)div.Children[0];
        Assert.Equal("red", border.BorderColor);
        Assert.Single(border.Children);
        var label = (Label)border.Children[0];
        Assert.Equal("Bordered Inner Content", label.Text);
    }

    [Fact]
    public void Parse_Border_WithoutColor()
    {
        var markup = @"<border thickness=""1"">
<div width=""100"" />
</border>";
        var (element, _) = new MarkupParser(_model).Parse(markup);

        Assert.IsType<Border>(element);
        var border = (Border)element;
        Assert.Null(border.BorderColor);
        Assert.Single(border.Children);
    }

    [Fact]
    public void Parse_Div_WithBorderAttribute()
    {
        var markup = @"<div width=""300"" height=""200"" bg=""lightgray"" border=""2 red"">
<label text=""Bordered Div"" />
</div>";
        var (element, _) = new MarkupParser(_model).Parse(markup);

        Assert.IsType<Border>(element);
        var border = (Border)element;
        
        // Border props
        // "2 red" -> Thickness 2, Color red
        Assert.Equal(2, border.BorderThickness.Left); 
        Assert.Equal("red", border.BorderColor);
        
        Assert.Single(border.Children);
        Assert.IsType<Div>(border.Children[0]);
        var div = (Div)border.Children[0];
        
        // Layout props transfer to Wrapper (Border)
        // Spec: "inherits all of the tag's styling"
        // My implementation adds Layout props to rootElement (Border).
        // So Width, Height, Bg should be on Border?
        // Wait, Spec says: "including size, background, borders, and padding".
        // So Border should have Width=300, Height=200, Bg=lightgray.
        // And Div? Div becomes just a container? 
        // Logic in Parser: `target = IsLayoutAttribute(name) ? rootElement : innerElement;`
        // Width, Height, Bg ARE LayoutAttributes. So they go to Border.
        
        Assert.Equal("300", border.Width);
        Assert.Equal("200", border.Height);
        Assert.Equal("lightgray", border.Background);
        
        // Inner Div should NOT have them? Or Parser doesn't set them on inner.
        // Let's check Inner Div.
        Assert.Null(div.Width);
        Assert.Null(div.Height);
        Assert.Null(div.Background); // If it was null before.
        
        Assert.Single(div.Children);
    }

    [Fact]
    public void Parse_BorderAttribute_WithThicknessAndColor()
    {
        // Note: Styles are not applied in MarkupParser, so we test inline attribute to ensure wrapper creation logic works.
        // Fixed XML hierarchy and tag matching.
        var markup = @"<div width=""500"" height=""400"" border=""5 #FF0000"">
<label text=""Bordered Content"" />
</div>";
        var (element, _) = new MarkupParser(_model).Parse(markup);

        Assert.IsType<Border>(element);
        var border = (Border)element;
        
        Assert.Equal(5f, border.BorderThickness.Left); // Assuming uniform
        Assert.Equal("#FF0000", border.BorderColor);
        Assert.Equal("500", border.Width);
        Assert.Equal("400", border.Height);
        
        Assert.Single(border.Children);
        Assert.IsType<Div>(border.Children[0]);
    }

    [Fact]
    public void Parse_BorderAttribute_WithThicknessAndColorInStyle()
    {
        var markup =
@"<suim>
    <style>
    .myclass {
	    width: 500,
	    height: 400,
	    border: 5 #FF0000
    }
    </style>
    <div class=""myclass"">
        <label text=""Bordered Content"" />
    </div>
</suim>";
        var (element, _) = new MarkupParser(_model).Parse(markup);

        // The parser extracts the div from the suim wrapper
        Assert.IsType<Div>(element);
        var div = (Div)element;
        Assert.Equal("myclass", div.Class);
        Assert.Single(div.Children);

        var border = div.Children[0] as Border;
        Assert.NotNull(border);
        Assert.Single(border.Children);
        Assert.Equal("#FF0000", border.BorderColor);
    }

    // ============== SUIM WRAPPER TESTS ==============

    [Fact]
    public void Parse_Suim_WithRootElement()
    {
        var markup = @"<suim><div /></suim>";
        var (element, _) = new MarkupParser(_model).Parse(markup);

        Assert.IsType<Div>(element);
    }

    [Fact]
    public void Parse_Suim_WithStyleAndRoot()
    {
        var markup = @"<suim>
    <style>.class { width: 100; }</style>
    <div width=""200"" />
</suim>";
        var (element, _) = new MarkupParser(_model).Parse(markup);

        Assert.IsType<Div>(element);
        var div = (Div)element;
        Assert.Equal("200", div.Width);
    }

    [Fact]
    public void Parse_Suim_WithModelAndRoot()
    {
        var markup = @"<suim>
    <model></model>
    <div width=""150"" />
</suim>";
        var (element, _) = new MarkupParser(_model).Parse(markup);

        Assert.IsType<Div>(element);
        var div = (Div)element;
        Assert.Equal("150", div.Width);
    }

    [Fact]
    public void Parse_Suim_WithModelStyleAndRoot()
    {
        var markup = @"<suim>
    <model></model>
    <style>.btn { }</style>
    <stack orientation=""vertical"">
        <label text=""Item 1"" />
        <label text=""Item 2"" />
    </stack>
</suim>";
        var (element, _) = new MarkupParser(_model).Parse(markup);

        Assert.IsType<Stack>(element);
        var stack = (Stack)element;
        Assert.Equal(Orientation.Vertical, stack.Orientation);
        Assert.Equal(2, stack.Children.Count);
    }

    [Fact]
    public void Parse_Suim_IgnoresModelAndStyle()
    {
        var markup = @"<suim>
    <model>.class { value: ignored }</model>
    <style>.button { color: red; }</style>
    <button><label text=""Click"" /></button>
</suim>";
        var (element, _) = new MarkupParser(_model).Parse(markup);

        Assert.IsType<Button>(element);
        var button = (Button)element;
        Assert.Single(button.Children);
        Assert.IsType<Label>(button.Children[0]);
    }

    [Fact]
    public void Parse_Suim_EmptyThrows()
    {
        var markup = @"<suim></suim>";
        Assert.Throws<InvalidOperationException>(() => new MarkupParser(_model).Parse(markup));
    }

    [Fact]
    public void Parse_Suim_OnlyModelAndStyleThrows()
    {
        var markup = @"<suim>
    <model></model>
    <style></style>
</suim>";
        Assert.Throws<InvalidOperationException>(() => new MarkupParser(_model).Parse(markup));
    }

    [Fact]
    public void Parse_Suim_WithComplexRoot()
    {
        var markup = @"<suim>
    <style></style>
    <grid columns=""*,*"" rows=""auto,*"">
        <div grid.row=""0"" grid.column=""0"" bg=""blue"" />
        <div grid.row=""0"" grid.column=""1"" bg=""red"" />
    </grid>
</suim>";
        var (element, _) = new MarkupParser(_model).Parse(markup);

        Assert.IsType<Grid>(element);
        var grid = (Grid)element;
        Assert.Equal("*,*", grid.Columns);
        Assert.Equal("auto,*", grid.Rows);
        Assert.Equal(2, grid.GridChildren.Count);
    }

    [Fact]
    public void Parse_Suim_LastElementIsRoot()
    {
        var markup = @"<suim>
    <model></model>
    <div bg=""gray"" />
    <stack orientation=""vertical"">
        <label text=""This is root"" />
    </stack>
</suim>";
        var (element, _) = new MarkupParser(_model).Parse(markup);

        // The last element (stack) should be the root, not the div
        Assert.IsType<Stack>(element);
        var stack = (Stack)element;
        Assert.Equal(Orientation.Vertical, stack.Orientation);
        Assert.Single(stack.Children);
        var label = (Label)stack.Children[0];
        Assert.Equal("This is root", label.Text);
    }
}
