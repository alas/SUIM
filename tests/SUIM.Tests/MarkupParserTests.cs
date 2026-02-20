namespace SUIM.Tests;

using Xunit;
using SUIM.Components;
using SUIM.Layout;

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
        var (element, _) = MarkupParser.Parse(markup, _model);

        Assert.IsType<Div>(element);
        var div = (Div)element;
        Assert.Equal("main", div.Id);
        Assert.Equal(new UnitValue(100), div.Width);
        Assert.Equal(new UnitValue(200), div.Height);
        Assert.Equal(HorizontalAlignment.Center, div.HorizontalAlignment);
        Assert.Equal(VerticalAlignment.Top, div.VerticalAlignment);
        Assert.Equal(new Thickness(10), div.Margin);
        Assert.Equal(new Thickness(5), div.Padding);
        Assert.Equal("blue", div.BackgroundColor);
    }

    [Fact]
    public void Parse_StackVertical()
    {
        var markup = "<stack orientation=\"vertical\" spacing=\"10\"><div /><div /></stack>";
        var (element, _) = MarkupParser.Parse(markup, _model);

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
        var markup = "<stack orientation=\"horizontal\"><label value=\"Hello\" /><button /></stack>";
        var (element, _) = MarkupParser.Parse(markup, _model);

        Assert.IsType<Stack>(element);
        var stack = (Stack)element;
        Assert.Equal(Orientation.Horizontal, stack.Orientation);
        Assert.Equal(2, stack.Children.Count);
        Assert.IsType<Label>(stack.Children[0]);
        Assert.IsType<Button>(stack.Children[1]);
        var label = (Label)stack.Children[0];
        Assert.Equal("Hello", label.Value);
    }

    [Fact]
    public void Parse_GridWithChildren()
    {
        var markup = @"<grid columns=""100, *"" rows=""50, *"">
<div grid.row=""0"" grid.column=""0"" bg=""gray"" />
<div grid.row=""0"" grid.column=""1"" bg=""silver"" />
<div grid.row=""1"" grid.column=""0"" grid.columnspan=""2"" bg=""white"" />
</grid>";
        var (element, _) = MarkupParser.Parse(markup, _model);

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
        var (element, _) = MarkupParser.Parse(markup, _model);

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
        var (element, _) = MarkupParser.Parse(markup, _model);

        Assert.IsType<Overlay>(element);
        var overlay = (Overlay)element;
        Assert.Equal(2, overlay.Children.Count);
    }

    [Fact]
    public void Parse_Label()
    {
        var markup = "<label value=\"Test Label\" />";
        var (element, _) = MarkupParser.Parse(markup, _model);

        Assert.IsType<Label>(element);
        var label = (Label)element;
        Assert.Equal("Test Label", label.Value);
    }

    [Fact]
    public void Parse_Button()
    {
        var markup = "<button><label value=\"Click me\" /></button>";
        var (element, _) = MarkupParser.Parse(markup, _model);

        Assert.IsType<Button>(element);
        var button = (Button)element;
        Assert.Single(button.Children);
        Assert.IsType<Label>(button.Children[0]);
    }

    [Fact]
    public void Parse_UnknownTag_Throws()
    {
        var markup = "<unknown />";
        Assert.Throws<NotSupportedException>(() => MarkupParser.Parse(markup, _model));
    }

    [Fact]
    public void Parse_InvalidXml_Throws()
    {
        var markup = "<div><unclosed>";
        Assert.Throws<System.Xml.XmlException>(() => MarkupParser.Parse(markup, _model));
    }

    [Fact]
    public void Parse_EmptyMarkup_Throws()
    {
        var markup = "";
        Assert.Throws<System.Xml.XmlException>(() => MarkupParser.Parse(markup, _model));
    }

    [Fact]
    public void Parse_NestedElements()
    {
        var markup = "<stack><div><label value=\"Nested\" /></div></stack>";
        var (element, _) = MarkupParser.Parse(markup, _model);

        Assert.IsType<Stack>(element);
        var stack = (Stack)element;
        Assert.Single(stack.Children);
        var div = (Div)stack.Children[0];
        Assert.Single(div.Children);
        var label = (Label)div.Children[0];
        Assert.Equal("Nested", label.Value);
    }

    [Fact]
    public void Parse_AnchorAttribute_Top()
    {
        var markup = "<div anchor=\"Top\" />";
        var (element, _) = MarkupParser.Parse(markup, _model);

        Assert.IsType<Div>(element);
        var div = (Div)element;
        Assert.Equal(Anchor.Top, div.Anchor);
    }

    [Fact]
    public void Parse_AnchorAttribute_Left()
    {
        var markup = "<div anchor=\"Left\" />";
        var (element, _) = MarkupParser.Parse(markup, _model);

        Assert.IsType<Div>(element);
        var div = (Div)element;
        Assert.Equal(Anchor.Left, div.Anchor);
    }

    [Fact]
    public void Parse_SynonymAttributes()
    {
        var markup = "<div halign=\"right\" valign=\"bottom\" />";
        var (element, _) = MarkupParser.Parse(markup, _model);

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
        var (element, _) = MarkupParser.Parse(markup, _model);

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
        var (element, _) = MarkupParser.Parse(markup, _model);

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
        var (element, _) = MarkupParser.Parse(markup, _model);

        Assert.IsType<Button>(element);
        var button = (Button)element;
        var child = button.Children[0] as Text;
        Assert.Equal("idle_sprite", child?.Value);
    }

    [Fact]
    public void Parse_Input_TextType()
    {
        var markup = "<input type=\"text\" placeholder=\"Enter name\" />";
        var (element, _) = MarkupParser.Parse(markup, _model);

        Assert.IsType<Input>(element);
        var input = (Input)element;
        Assert.Equal(InputType.Text, input.Type);
        Assert.Equal("Enter name", input.Placeholder);
    }

    [Fact]
    public void Parse_Input_NumberType_WithMinMax()
    {
        var markup = "<input type=\"number\" min=\"0\" max=\"100\" step=\"5\" />";
        var (element, _) = MarkupParser.Parse(markup, _model);

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
        var (element, _) = MarkupParser.Parse(markup, _model);

        Assert.IsType<Input>(element);
        var input = (Input)element;
        Assert.Equal("[0-9]{3}-[0-9]{3}-[0-9]{4}", input.Mask);
    }

    [Fact]
    public void Parse_Label_WithAllAttributes()
    {
        var markup = "<label value=\"Hello\" font=\"Arial\" fontsize=\"16\" color=\"#FF0000\" wrap=\"true\" />";
        var (element, _) = MarkupParser.Parse(markup, _model);

        Assert.IsType<Label>(element);
        var label = (Label)element;
        Assert.Equal("Hello", label.Value);
        Assert.Equal("Arial", label.Font);
        Assert.Equal(16f, label.FontSize);
        Assert.Equal("#FF0000", label.Color);
        Assert.True(label.Wrap);
    }

    [Fact]
    public void Parse_Image_WithStretch()
    {
        var markup = "<image source=\"mysprite\" stretch=\"uniform\" />";
        var (element, _) = MarkupParser.Parse(markup, _model);

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
        var (element, _) = MarkupParser.Parse(markup, _model);

        Assert.IsType<Select>(element);
        var select = (Select)element;
        Assert.Equal("dropdown", select.Id);
        Assert.Equal(2, select.Children.Count);
    }

    [Fact]
    public void Parse_Select_WithMultiple()
    {
        var markup = "<select multiple=\"true\"><option>Opt1</option><option>Opt2</option></select>";
        var (element, _) = MarkupParser.Parse(markup, _model);

        Assert.IsType<Select>(element);
        var select = (Select)element;
        Assert.True(select.Multiple);
    }

    [Fact]
    public void Parse_Textarea()
    {
        var markup = "<textarea id=\"notes\" width=\"300\" height=\"200\" />";
        var (element, _) = MarkupParser.Parse(markup, _model);

        Assert.IsType<TextArea>(element);
        var textarea = (TextArea)element;
        Assert.Equal("notes", textarea.Id);
        Assert.Equal(new UnitValue(300), textarea.Width);
        Assert.Equal(new UnitValue(200), textarea.Height);
    }

    // ============== STACK SYNONYMS TESTS ==============

    [Fact]
    public void Parse_VStack_Synonym()
    {
        var markup = "<vstack><div /><div /></vstack>";
        var (element, _) = MarkupParser.Parse(markup, _model);

        Assert.IsType<Stack>(element);
        var stack = (Stack)element;
        Assert.Equal(Orientation.Vertical, stack.Orientation);
        Assert.Equal(2, stack.Children.Count);
    }

    [Fact]
    public void Parse_VBox_Synonym()
    {
        var markup = "<vbox><label value=\"A\" /><label value=\"B\" /></vbox>";
        var (element, _) = MarkupParser.Parse(markup, _model);

        Assert.IsType<Stack>(element);
        var stack = (Stack)element;
        Assert.Equal(Orientation.Vertical, stack.Orientation);
    }

    [Fact]
    public void Parse_HStack_Synonym()
    {
        var markup = "<hstack><div /><div /></hstack>";
        var (element, _) = MarkupParser.Parse(markup, _model);

        Assert.IsType<Stack>(element);
        var stack = (Stack)element;
        Assert.Equal(Orientation.Horizontal, stack.Orientation);
        Assert.Equal(2, stack.Children.Count);
    }

    [Fact]
    public void Parse_HBox_Synonym()
    {
        var markup = "<hbox><label value=\"X\" /><label value=\"Y\" /></hbox>";
        var (element, _) = MarkupParser.Parse(markup, _model);

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
        var (element, _) = MarkupParser.Parse(markup, _model);

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
        var (element, _) = MarkupParser.Parse(markup, _model);

        Assert.IsType<Grid>(element);
        var grid = (Grid)element;
        Assert.NotEmpty(grid.Children);
    }

    // ============== COMMON ATTRIBUTES TESTS ==============

    [Fact]
    public void Parse_Visibility_Attribute()
    {
        var markup = "<div visibility=\"hidden\" />";
        var (element, _) = MarkupParser.Parse(markup, _model);

        Assert.IsType<Div>(element);
        var div = (Div)element;
        Assert.Equal(Visibility.Hidden, div.Visibility);
    }

    [Fact]
    public void Parse_Opacity_Attribute()
    {
        var markup = "<div opacity=\"0.5\" />";
        var (element, _) = MarkupParser.Parse(markup, _model);

        Assert.IsType<Div>(element);
        var div = (Div)element;
        Assert.Equal(0.5, div.Opacity);
    }

    [Fact]
    public void Parse_ZIndex_Attribute()
    {
        var markup = "<div z-index=\"10\" />";
        var (element, _) = MarkupParser.Parse(markup, _model);

        Assert.IsType<Div>(element);
        var div = (Div)element;
        Assert.Equal(10, div.ZIndex);
    }

    [Fact]
    public void Parse_XY_Positioning()
    {
        var markup = "<div x=\"50\" y=\"100\" width=\"200\" height=\"150\" />";
        var (element, _) = MarkupParser.Parse(markup, _model);

        Assert.IsType<Div>(element);
        var div = (Div)element;
        Assert.Equal(50f, div.X.Value);
        Assert.Equal(100f, div.Y.Value);
    }

    [Fact]
    public void Parse_Clip_Attribute()
    {
        var markup = "<stack clip=\"true\"><div /></stack>";
        var (element, _) = MarkupParser.Parse(markup, _model);

        Assert.IsType<Stack>(element);
        var stack = (Stack)element;
        Assert.True(stack.Clip);
    }

    [Fact]
    public void Parse_Spacing_SingleValue()
    {
        var markup = "<stack spacing=\"10\"><div /><div /></stack>";
        var (element, _) = MarkupParser.Parse(markup, _model);

        Assert.IsType<Stack>(element);
        var stack = (Stack)element;
        Assert.Equal(10, stack.Spacing);
    }

    [Fact]
    public void Parse_Class_Attribute()
    {
        var markup = "<div class=\"primary secondary\" />";
        var (element, _) = MarkupParser.Parse(markup, _model);

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
    <label value=""Top Left"" />
    <label value=""Top"" />
</stack>
<div grid.row=""1"" grid.column=""0"" grid.columnspan=""2"" bg=""lightgray"" />
</grid>";
        var (element, _) = MarkupParser.Parse(markup, _model);

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
        var (element, _) = MarkupParser.Parse(markup, _model);

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
    <label value=""Click Me"" />
}
else
{
    <label value=""Disabled"" />
}
</button>";
        var (element, _) = MarkupParser.Parse(markup, _model);

        Assert.IsType<Button>(element);
        var button = (Button)element;
        Assert.Single(button.Children);
        var label = (Label)button.Children[0];
        Assert.Equal("Click Me", label.Value);
    }

    // ============== COLOR FORMATTING TESTS ==============

    [Fact]
    public void Parse_Color_Hex()
    {
        var markup = "<div bg=\"#FF0000\" />";
        var (element, _) = MarkupParser.Parse(markup, _model);

        Assert.IsType<Div>(element);
        var div = (Div)element;
        Assert.Equal("#FF0000", div.BackgroundColor);
    }

    [Fact]
    public void Parse_Color_RGBA()
    {
        var markup = "<div bg=\"255,0,0,255\" />";
        var (element, _) = MarkupParser.Parse(markup, _model);

        Assert.IsType<Div>(element);
        var div = (Div)element;
        Assert.Equal("255,0,0,255", div.BackgroundColor);
    }

    [Fact]
    public void Parse_Color_Named()
    {
        var markup = "<div bg=\"Red\" />";
        var (element, _) = MarkupParser.Parse(markup, _model);

        Assert.IsType<Div>(element);
        var div = (Div)element;
        Assert.Equal("Red", div.BackgroundColor);
    }

    // ============== SIZING UNITS TESTS ==============

    [Fact]
    public void Parse_Size_Pixels()
    {
        var markup = "<div width=\"100\" height=\"200\" />";
        var (element, _) = MarkupParser.Parse(markup, _model);

        Assert.IsType<Div>(element);
        var div = (Div)element;
        Assert.Equal(new UnitValue(100), div.Width);
        Assert.Equal(new UnitValue(200), div.Height);
    }

    [Fact]
    public void Parse_Size_FractionalUnits()
    {
        var markup = "<div width=\"fr\" height=\"2fr\" />";
        var (element, _) = MarkupParser.Parse(markup, _model);

        Assert.IsType<Div>(element);
        var div = (Div)element;
        Assert.Equal(new UnitValue(1, UnitType.Fr), div.Width);
        Assert.Equal(new UnitValue(2, UnitType.Fr), div.Height);
    }

    [Fact]
    public void Parse_Size_Auto()
    {
        var markup = "<label value=\"Auto\" width=\"auto\" />";
        var (element, _) = MarkupParser.Parse(markup, _model);

        Assert.IsType<Label>(element);
        var label = (Label)element;
        Assert.Equal(new UnitValue(0, UnitType.Auto), label.Width);
    }

    // ============== ADDITIONAL INPUT TYPES TESTS ==============

    [Fact]
    public void Parse_Input_EmailType()
    {
        var markup = "<input type=\"email\" placeholder=\"your@email.com\" />";
        var (element, _) = MarkupParser.Parse(markup, _model);

        Assert.IsType<Input>(element);
        var input = (Input)element;
        Assert.Equal(InputType.Email, input.Type);
    }

    [Fact]
    public void Parse_Input_UrlType()
    {
        var markup = "<input type=\"url\" placeholder=\"https://example.com\" />";
        var (element, _) = MarkupParser.Parse(markup, _model);

        Assert.IsType<Input>(element);
        var input = (Input)element;
        Assert.Equal(InputType.Url, input.Type);
    }

    [Fact]
    public void Parse_Input_PasswordType()
    {
        var markup = "<input type=\"password\" />";
        var (element, _) = MarkupParser.Parse(markup, _model);

        Assert.IsType<Input>(element);
        var input = (Input)element;
        Assert.Equal(InputType.Password, input.Type);
    }

    [Fact]
    public void Parse_Input_DateType()
    {
        var markup = "<input type=\"date\" />";
        var (element, _) = MarkupParser.Parse(markup, _model);

        Assert.IsType<Input>(element);
        var input = (Input)element;
        Assert.Equal(InputType.Date, input.Type);
    }

    [Fact]
    public void Parse_Input_TimeType()
    {
        var markup = "<input type=\"time\" />";
        var (element, _) = MarkupParser.Parse(markup, _model);

        Assert.IsType<Input>(element);
        var input = (Input)element;
        Assert.Equal(InputType.Time, input.Type);
    }

    [Fact]
    public void Parse_Input_DatetimeLocalType()
    {
        var markup = "<input type=\"datetime-local\" />";
        var (element, _) = MarkupParser.Parse(markup, _model);

        Assert.IsType<Input>(element);
        var input = (Input)element;
        Assert.Equal(InputType.DatetimeLocal, input.Type);
    }

    [Fact]
    public void Parse_Input_RangeType()
    {
        var markup = "<input type=\"range\" min=\"0\" max=\"100\" />";
        var (element, _) = MarkupParser.Parse(markup, _model);

        Assert.IsType<Input>(element);
        var input = (Input)element;
        Assert.Equal(InputType.Range, input.Type);
    }

    [Fact]
    public void Parse_Input_CheckboxType()
    {
        var markup = "<input type=\"checkbox\" />";
        var (element, _) = MarkupParser.Parse(markup, _model);

        Assert.IsType<Input>(element);
        var input = (Input)element;
        Assert.Equal(InputType.Checkbox, input.Type);
    }

    [Fact]
    public void Parse_Input_RadioType()
    {
        var markup = "<input type=\"radio\" />";
        var (element, _) = MarkupParser.Parse(markup, _model);

        Assert.IsType<Input>(element);
        var input = (Input)element;
        Assert.Equal(InputType.Radio, input.Type);
    }

    // ============== TEXTAREA TESTS ==============

    [Fact]
    public void Parse_Textarea_WithPlaceholder()
    {
        var markup = "<textarea placeholder=\"Enter description\" rows=\"5\" columns=\"40\" />";
        var (element, _) = MarkupParser.Parse(markup, _model);

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
        var (element, _) = MarkupParser.Parse(markup, _model);

        Assert.IsType<Option>(element);
        var option = (Option)element;
        Assert.Equal("test-value", option.Value);
        Assert.Single(option.Children);
        var textNode = option.Children[0] as Text;
        Assert.Equal("Test Label", textNode?.Value);
    }

    // ============== IMAGE STRETCH VARIANTS ==============

    [Fact]
    public void Parse_Image_Stretch_None()
    {
        var markup = "<image source=\"sprite\" stretch=\"none\" />";
        var (element, _) = MarkupParser.Parse(markup, _model);

        Assert.IsType<Image>(element);
        var image = (Image)element;
        Assert.Equal(ImageStretch.None, image.Stretch);
    }

    [Fact]
    public void Parse_Image_Stretch_Fill()
    {
        var markup = "<image source=\"sprite\" stretch=\"fill\" />";
        var (element, _) = MarkupParser.Parse(markup, _model);

        Assert.IsType<Image>(element);
        var image = (Image)element;
        Assert.Equal(ImageStretch.Fill, image.Stretch);
    }

    [Fact]
    public void Parse_Image_Stretch_UniformToFill()
    {
        var markup = "<image source=\"sprite\" stretch=\"uniformtofill\" />";
        var (element, _) = MarkupParser.Parse(markup, _model);

        Assert.IsType<Image>(element);
        var image = (Image)element;
        Assert.Equal(ImageStretch.UniformToFill, image.Stretch);
    }

    // ============== ANCHOR VARIANTS ==============

    [Fact]
    public void Parse_Anchor_Top()
    {
        var markup = "<div anchor=\"Top\" />";
        var (element, _) = MarkupParser.Parse(markup, _model);

        Assert.IsType<Div>(element);
        var div = (Div)element;
        Assert.Equal(Anchor.Top, div.Anchor);
    }

    [Fact]
    public void Parse_Anchor_Bottom()
    {
        var markup = "<div anchor=\"Bottom\" />";
        var (element, _) = MarkupParser.Parse(markup, _model);

        Assert.IsType<Div>(element);
        var div = (Div)element;
        Assert.Equal(Anchor.Bottom, div.Anchor);
    }

    [Fact]
    public void Parse_Anchor_Right()
    {
        var markup = "<div anchor=\"Right\" />";
        var (element, _) = MarkupParser.Parse(markup, _model);

        Assert.IsType<Div>(element);
        var div = (Div)element;
        Assert.Equal(Anchor.Right, div.Anchor);
    }

    [Fact]
    public void Parse_Anchor_Left()
    {
        var markup = "<div anchor=\"Left\" />";
        var (element, _) = MarkupParser.Parse(markup, _model);

        Assert.IsType<Div>(element);
        var div = (Div)element;
        Assert.Equal(Anchor.Left, div.Anchor);
    }

    [Fact]
    public void Parse_Anchor_None()
    {
        var markup = "<div anchor=\"None\" />";
        var (element, _) = MarkupParser.Parse(markup, _model);

        Assert.IsType<Div>(element);
        var div = (Div)element;
        Assert.Equal(Anchor.None, div.Anchor);
    }

    // ============== DOCK EDGE VARIANTS ==============

    [Fact]
    public void Parse_Dock_EdgeBottom()
    {
        var markup = @"<dock>
<div dock.edge=""bottom"" height=""50"" />
<div />
</dock>";
        var (element, _) = MarkupParser.Parse(markup, _model);

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
        var (element, _) = MarkupParser.Parse(markup, _model);

        Assert.IsType<Dock>(element);
        var dock = (Dock)element;
        Assert.False(dock.LastChildFill);
    }

    // ============== MORE COMMON ATTRIBUTES ==============

    [Fact]
    public void Parse_Opacity_FullyOpaque()
    {
        var markup = "<div opacity=\"1.0\" />";
        var (element, _) = MarkupParser.Parse(markup, _model);

        Assert.IsType<Div>(element);
        var div = (Div)element;
        Assert.Equal(1.0f, div.Opacity);
    }

    [Fact]
    public void Parse_Opacity_FullyTransparent()
    {
        var markup = "<div opacity=\"0.0\" />";
        var (element, _) = MarkupParser.Parse(markup, _model);

        Assert.IsType<Div>(element);
        var div = (Div)element;
        Assert.Equal(0.0f, div.Opacity);
    }

    [Fact]
    public void Parse_Opacity_PartialTransparency()
    {
        var markup = "<div opacity=\"0.75\" />";
        var (element, _) = MarkupParser.Parse(markup, _model);

        Assert.IsType<Div>(element);
        var div = (Div)element;
        Assert.Equal(0.75f, div.Opacity);
    }

    [Fact]
    public void Parse_ZIndex_Negative()
    {
        var markup = "<div z-index=\"-5\" />";
        var (element, _) = MarkupParser.Parse(markup, _model);

        Assert.IsType<Div>(element);
        var div = (Div)element;
        Assert.Equal(-5, div.ZIndex);
    }

    [Fact]
    public void Parse_ZIndex_Large()
    {
        var markup = "<div z-index=\"1000\" />";
        var (element, _) = MarkupParser.Parse(markup, _model);

        Assert.IsType<Div>(element);
        var div = (Div)element;
        Assert.Equal(1000, div.ZIndex);
    }

    [Fact]
    public void Parse_MultipleClasses()
    {
        var markup = "<div class=\"primary secondary large\" />";
        var (element, _) = MarkupParser.Parse(markup, _model);

        Assert.IsType<Div>(element);
        var div = (Div)element;
        Assert.Equal("primary secondary large", div.Class);
    }

    // ============== LABEL WITH OPTIONAL TEXT ==============

    [Fact]
    public void Parse_Label_WithoutWrap()
    {
        var markup = "<label value=\"Test\" wrap=\"false\" />";
        var (element, _) = MarkupParser.Parse(markup, _model);

        Assert.IsType<Label>(element);
        var label = (Label)element;
        Assert.False(label.Wrap);
    }

    [Fact]
    public void Parse_Label_WithColor()
    {
        var markup = "<label value=\"Colored\" color=\"blue\" />";
        var (element, _) = MarkupParser.Parse(markup, _model);

        Assert.IsType<Label>(element);
        var label = (Label)element;
        Assert.Equal("blue", label.Color);
    }

    // ============== VISIBILITY VARIANTS ==============

    [Fact]
    public void Parse_Visibility_Visible()
    {
        var markup = "<div visibility=\"visible\" />";
        var (element, _) = MarkupParser.Parse(markup, _model);

        Assert.IsType<Div>(element);
        var div = (Div)element;
        Assert.Equal(Visibility.Visible, div.Visibility);
    }

    [Fact]
    public void Parse_Visibility_Collapsed()
    {
        var markup = "<div visibility=\"collapsed\" />";
        var (element, _) = MarkupParser.Parse(markup, _model);

        Assert.IsType<Div>(element);
        var div = (Div)element;
        Assert.Equal(Visibility.Collapsed, div.Visibility);
    }

    // ============== GRID SPAN EDGE CASES ==============

    [Fact]
    public void Parse_Grid_DefaultSpans()
    {
        var markup = @"<grid>
<div grid.row=""0"" grid.column=""0"" />
</grid>";
        var (element, _) = MarkupParser.Parse(markup, _model);

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
<label value=""Deep"" />
</stack>
</div>
</stack>
</div>";
        var (element, _) = MarkupParser.Parse(markup, _model);

        Assert.IsType<Div>(element);
        var div = (Div)element;
        var stack = (Stack)div.Children[0];
        var innerDiv = (Div)stack.Children[0];
        var innerStack = (Stack)innerDiv.Children[0];
        var label = (Label)innerStack.Children[0];
        Assert.Equal("Deep", label.Value);
    }

    // ============== BUTTON WITH NESTED CONTENT ==============

    [Fact]
    public void Parse_Button_WithNestedElements()
    {
        var markup = @"<button>
<stack>
<label value=""Icon"" />
<label value=""Label"" />
</stack>
</button>";
        var (element, _) = MarkupParser.Parse(markup, _model);

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
        var (element, _) = MarkupParser.Parse(markup, _model);

        Assert.IsType<Input>(element);
        var input = (Input)element;
        Assert.Equal("default-value", input.Value);
    }

    // ============== TEXT NODES AS BASETEXT ==============

    [Fact]
    public void Parse_PlainText_CreatesLabel()
    {
        var markup = "<div>Simple text</div>";
        var (element, _) = MarkupParser.Parse(markup, _model);

        Assert.IsType<Div>(element);
        var div = (Div)element;
        Assert.Single(div.Children);
        Assert.IsType<Text>(div.Children[0]);
        var label = (Text)div.Children[0];
        Assert.Equal("Simple text", label.Value);
    }

    [Fact]
    public void Parse_MixedTextAndElements()
    {
        var markup = @"<stack>
Text before
<label value=""Label"" />
Text after
</stack>";
        var (element, _) = MarkupParser.Parse(markup, _model);

        Assert.IsType<Stack>(element);
        var stack = (Stack)element;
        Assert.Equal(3, stack.Children.Count);
        
        // First child: text "Text before"
        Assert.IsType<Text>(stack.Children[0]);
        Assert.Equal("Text before", ((Text)stack.Children[0]).Value);
        
        // Second child: label element
        Assert.IsType<Label>(stack.Children[1]);
        Assert.Equal("Label", ((Label)stack.Children[1]).Value);
        
        // Third child: text "Text after"
        Assert.IsType<Text>(stack.Children[2]);
        Assert.Equal("Text after", ((Text)stack.Children[2]).Value);
    }

    [Fact]
    public void Parse_MultilineText_TrimsWhitespace()
    {
        var markup = @"<div>
            Multi-line
            text content
        </div>";
        var (element, _) = MarkupParser.Parse(markup, _model);

        Assert.IsType<Div>(element);
        var div = (Div)element;
        Assert.Single(div.Children);
        var label = (Text)div.Children[0];
        // Should be trimmed and preserved as single text
        Assert.Contains("Multi-line", label.Value);
    }

    [Fact]
    public void Parse_EmptyText_Ignored()
    {
        var markup = @"<div>
            
            <label value=""Only label"" />
            
        </div>";
        var (element, _) = MarkupParser.Parse(markup, _model);

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
<label value=""Item 1"" />
<label value=""Item 2"" />
<label value=""Item 3"" />
</stack>";
        var (element, _) = MarkupParser.Parse(markup, _model);

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
<label value=""Item 1"" />
<label value=""Item 2"" />
</stack>";
        var (element, _) = MarkupParser.Parse(markup, _model);

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
        var (element, _) = MarkupParser.Parse(markup, _model);

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
<label value=""Scrollable Item 1"" />
<label value=""Scrollable Item 2"" />
<label value=""Scrollable Item 3"" />
</stack>";
        var (element, _) = MarkupParser.Parse(markup, _model);

        Assert.IsType<Scroll>(element);
        var scroll = (Scroll)element;
        Assert.Equal(ScrollDirection.Vertical, scroll.Direction);
        Assert.Equal(new UnitValue(400), scroll.Width);
        Assert.Equal(new UnitValue(300), scroll.Height);
        
        Assert.Single(scroll.Children);
        var stack = (Stack)scroll.Children[0];
        Assert.IsType<Stack>(stack);
        Assert.Equal(Orientation.Vertical, stack.Orientation);
        // Inner element should default to `auto` sizing when wrapped by a scroll-viewport
        Assert.Equal(UnitValue.Auto, stack.Width);
        Assert.Equal(UnitValue.Auto, stack.Height);
        // Spacing is component specific, goes to stack
        Assert.Equal(5, stack.Spacing);
        Assert.Equal(3, stack.Children.Count);
    }

    [Fact]
    public void Parse_Border_WithThicknessAndColor()
    {
        var markup = @"<border thickness=""2"" color=""#FF0000"">
<label value=""Bordered Content"" />
</border>";
        var (element, _) = MarkupParser.Parse(markup, _model);

        Assert.IsType<Border>(element);
        var border = (Border)element;
        Assert.Single(border.Children);
        Assert.Equal("#FF0000", border.Color);
    }

    [Fact]
    public void Parse_Border_WithMultipleSideThickness()
    {
        var markup = @"<border thickness=""5,10,5,10"" color=""blue"">
<div width=""200"" height=""100"" />
</border>";
        var (element, _) = MarkupParser.Parse(markup, _model);

        Assert.IsType<Border>(element);
        var border = (Border)element;
        Assert.Equal(new Thickness(5, 10, 5, 10),  border.Thickness);
        Assert.Equal("blue", border.Color);
        Assert.Single(border.Children);
    }

    [Fact]
    public void Parse_Border_WithTwoValueThickness()
    {
        var markup = @"<border thickness=""3,6"" color=""green"">
<label value=""Border Test"" />
</border>";
        var (element, _) = MarkupParser.Parse(markup, _model);

        Assert.IsType<Border>(element);
        var border = (Border)element;
        Assert.Equal(new Thickness(3, 6), border.Thickness);
        Assert.Equal("green", border.Color);
    }

    [Fact]
    public void Parse_Div_WithBorder()
    {
        var markup = @"<div width=""300"" height=""200"" bg=""lightgray"">
<border thickness=""2"" color=""red"">
<label value=""Bordered Inner Content"" />
</border>
</div>";
        var (element, _) = MarkupParser.Parse(markup, _model);

        Assert.IsType<Div>(element);
        var div = (Div)element;
        Assert.Equal(new UnitValue(300), div.Width);
        Assert.Equal(new UnitValue(200), div.Height);
        Assert.Equal("lightgray", div.BackgroundColor);
        Assert.Single(div.Children);
        
        var border = (Border)div.Children[0];
        Assert.Equal("red", border.Color);
        Assert.Single(border.Children);
        var label = (Label)border.Children[0];
        Assert.Equal("Bordered Inner Content", label.Value);
    }

    [Fact]
    public void Parse_Border_WithoutColor()
    {
        var markup = @"<border thickness=""1"">
<div width=""100"" />
</border>";
        var (element, _) = MarkupParser.Parse(markup, _model);

        Assert.IsType<Border>(element);
        var border = (Border)element;
        Assert.Null(border.Color);
        Assert.Single(border.Children);
    }

    [Fact]
    public void Parse_Div_WithBorderAttribute()
    {
        var markup = @"<div width=""300"" height=""200"" bg=""lightgray"" border=""2 red"">
<label value=""Bordered Div"" />
</div>";
        var (element, _) = MarkupParser.Parse(markup, _model);

        Assert.IsType<Border>(element);
        var border = (Border)element;
        
        // Border props
        // "2 red" -> Thickness 2, Color red
        Assert.Equal(2, border.Thickness.Left.Value); 
        Assert.Equal("red", border.Color);
        
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
        
        Assert.Equal(new UnitValue(300), border.Width);
        Assert.Equal(new UnitValue(200), border.Height);
        Assert.Equal("lightgray", border.BackgroundColor);
        
        // Inner Div should NOT have them? Or Parser doesn't set them on inner.
        // Let's check Inner Div.
        Assert.Equal(UnitValue.Auto, div.Width);
        Assert.Equal(UnitValue.Auto, div.Height);
        Assert.Null(div.BackgroundColor); // If it was null before.
        
        Assert.Single(div.Children);
    }

    [Fact]
    public void Parse_BorderAttribute_WithThicknessAndColor()
    {
        // Note: Styles are not applied in MarkupParser, so we test inline attribute to ensure wrapper creation logic works.
        // Fixed XML hierarchy and tag matching.
        var markup = @"<div width=""500"" height=""400"" border=""5 #FF0000"">
<label value=""Bordered Content"" />
</div>";
        var (element, _) = MarkupParser.Parse(markup, _model);

        Assert.IsType<Border>(element);
        var border = (Border)element;
        
        Assert.Equal(5f, border.Thickness.Left.Value); // Assuming uniform
        Assert.Equal("#FF0000", border.Color);
        Assert.Equal(new UnitValue(500), border.Width);
        Assert.Equal(new UnitValue(400), border.Height);
        
        Assert.Single(border.Children);
        Assert.IsType<Div>(border.Children[0]);
    }

    [Fact]
    public void Parse_BorderAttribute_WithThicknessAndColorInStyle()
    {
        var markup = @"<grid>
                <style>
                .myclass {
	                width: 500;
	                height: 400;
	                border: 5 #FF0000;
                }
                </style>
                <div class=""myclass"">
                    <label value=""Bordered Content"" />
                </div>
            </grid>";
        var (element, _) = MarkupParser.Parse(markup, _model);

        var border = element?.Children.Single() as Border;
        Assert.NotNull(border);
        Assert.Equal("#FF0000", border.Color);
        // Style sizes should be applied to the wrapper (Border)
        Assert.Equal(new UnitValue(500), border.Width);
        Assert.Equal(new UnitValue(400), border.Height);
        Assert.Single(border.Children);

        var div = border.Children[0] as Div;
        Assert.NotNull(div);
        Assert.Equal("myclass", div.Class);
        // Wrapped inner element should default to auto sizing (style moved to wrapper)
        Assert.Equal(UnitValue.Auto, div.Width);
        Assert.Equal(UnitValue.Auto, div.Height);
        Assert.Single(div.Children);
        Assert.IsType<Label>(div.Children[0]);
    }

    [Fact]
    public void ParseStyles_ClassWithBorderAndSizes_AssignsProperties()
    {
        var styleContent = ".myclass { width: 500; height: 400; border: 5 #FF0000; }";
        Dictionary<string, Dictionary<string, string>> styleDictionary = [];
        var mi = typeof(MarkupParser).GetMethod("ParseStyles", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
            ?? throw new Exception();
        var styles = mi.Invoke(null, [styleContent, styleDictionary])!;

        Assert.True(styleDictionary.ContainsKey(".myclass"));
        var props = styleDictionary[".myclass"];
        Assert.Equal("500", props["width"]);
        Assert.Equal("400", props["height"]);
        Assert.Equal("5 #FF0000", props["border"]);
    }

    // ============== Grid TESTS ==============

    [Fact]
    public void Parse_Grid_WithDivElement()
    {
        var markup = @"<grid><div /></grid>";
        var (element, _) = MarkupParser.Parse(markup, _model);

        Assert.IsType<Div>(element!.Children.Single());
    }

    [Fact]
    public void Parse_Grid_WithStyleAndDiv()
    {
        var markup = @"<grid>
    <style>.class { width: 100; }</style>
    <div width=""200"" />
</grid>";
        var (element, _) = MarkupParser.Parse(markup, _model);

        var div = element?.Children.Single() as Div;
        Assert.Equal(new UnitValue(200), div?.Width);
    }

    [Fact]
    public void Parse_Grid_WithModelAndDiv()
    {
        var markup = @"<grid>
    <model></model>
    <div width=""150"" />
</grid>";
        var (element, _) = MarkupParser.Parse(markup, _model);

        var div = element?.Children.Single() as Div;
        Assert.Equal(new UnitValue(150), div?.Width);
    }

    [Fact]
    public void Parse_Grid_WithModelStyleAndStack()
    {
        var markup = @"<grid>
    <model></model>
    <style>.btn { }</style>
    <stack orientation=""vertical"">
        <label value=""Item 1"" />
        <label value=""Item 2"" />
    </stack>
</grid>";
        var (element, _) = MarkupParser.Parse(markup, _model);

        var stack = element?.Children.Single() as Stack;
        Assert.Equal(Orientation.Vertical, stack!.Orientation);
        Assert.Equal(2, stack.Children.Count);
    }

    [Fact]
    public void Parse_Grid_IgnoresModelAndStyle()
    {
        var markup = @"<button>
<model>{ ""value"": ""ignored"" }</model>
<style>.button { color: red; }</style>
    <label value=""Click"" />
</button>";
        var (element, _) = MarkupParser.Parse(markup, _model);

        Assert.IsType<Button>(element);
        var button = (Button)element;
        Assert.Single(button.Children);
        Assert.IsType<Label>(button.Children[0]);
    }

    [Fact]
    public void Parse_ComplexRoot()
    {
        var markup = @"<grid columns=""*,*"" rows=""auto,*"">
<style></style>
    <div grid.row=""0"" grid.column=""0"" bg=""blue"" />
    <div grid.row=""0"" grid.column=""1"" bg=""red"" />
</grid>";
        var (element, _) = MarkupParser.Parse(markup, _model);

        Assert.IsType<Grid>(element);
        var grid = (Grid)element;
        Assert.Equal("*,*", grid.Columns);
        Assert.Equal("auto,*", grid.Rows);
        Assert.Equal(2, grid.GridChildren.Count);
    }

    // ============== MODEL PARSING TESTS ==============

    [Fact]
    public void Parse_Grid_WithJsonModel()
    {
        var markup = @"<grid>
    <model>{ ""name"": ""John"", ""age"": 30 }</model>
    <div />
</grid>";
        var (_, model) = MarkupParser.Parse(markup);

        Assert.NotNull(model);
        Assert.Equal("John", model!.name);
        Assert.Equal(30, model.age);
    }

    [Fact]
    public void Parse_Grid_WithJsonModelAndProvidedModel()
    {
        var providedModel = new { firstName = "Jane", age = 25 };
        var markup = @"<grid>
    <model>{ ""lastName"": ""Doe"", ""age"": 30 }</model>
    <div />
</grid>";
        var (element, model) = MarkupParser.Parse(markup, providedModel);

        Assert.NotNull(model);
        // From provided model
        Assert.Equal("Jane", model!.firstName);
        // From JSON (overrides provided)
        Assert.Equal("Doe", model.lastName);
        Assert.Equal(30, model.age); // JSON value overrides provided value
    }

    [Fact]
    public void Parse_Grid_WithJsonModelStringProperty()
    {
        var markup = @"<grid>
    <model>{ ""title"": ""Hello World"", ""description"": ""Test"" }</model>
    <label value=""@title"" />
</grid>";
        var (element, model) = MarkupParser.Parse(markup);

        Assert.NotNull(model);
        Assert.Equal("Hello World", model!.title);
        Assert.Equal("Test", model.description);
        
        var label = element?.Children.Single() as Label;
        Assert.NotNull(label);
        Assert.Single(label!.Bindings);
        Assert.Equal("value", label.Bindings[0].TargetPropertyName);
        Assert.Equal("title", label.Bindings[0].ModelPropertyName);
    }

    [Fact]
    public void Parse_Grid_WithJsonModelNumberProperty()
    {
        var markup = @"<grid>
    <model>{ ""width"": 500, ""height"": 300 }</model>
    <div width=""@width"" height=""@height"" />
</grid>";
        var (_, model) = MarkupParser.Parse(markup);

        Assert.NotNull(model);
        Assert.Equal(500, model!.width);
        Assert.Equal(300, model.height);
    }

    [Fact]
    public void Parse_Grid_WithJsonModelBooleanProperty()
    {
        var markup = @"<grid>
    <model>{ ""isVisible"": true, ""isEnabled"": false }</model>
    <div />
</grid>";
        var (_, model) = MarkupParser.Parse(markup);

        Assert.NotNull(model);
        Assert.True(model!.isVisible);
        Assert.False(model.isEnabled);
    }

    [Fact]
    public void Parse_Grid_WithJsonModelArrayProperty()
    {
        var markup = @"<grid>
    <model>{ ""items"": [1, 2, 3], ""names"": [""Alice"", ""Bob""] }</model>
    <div />
</grid>";
        var (_, model) = MarkupParser.Parse(markup);

        Assert.NotNull(model);
        
        var items = model!.items as object[];
        Assert.NotNull(items);
        Assert.Equal(3, items.Length);
        
        var names = model.names as object[];
        Assert.NotNull(names);
        Assert.Equal(2, names.Length);
    }

    [Fact]
    public void Parse_Grid_WithJsonModelNullProperty()
    {
        var markup = @"<grid>
    <model>{ ""value"": null }</model>
    <div />
</grid>";
        var (_, model) = MarkupParser.Parse(markup);

        Assert.NotNull(model);
        Assert.Null(model!.value);
    }

    [Fact]
    public void Parse_Grid_WithEmptyJsonModel()
    {
        var markup = @"<grid>
    <model>{ }</model>
    <div />
</grid>";
        var (_, model) = MarkupParser.Parse(markup);

        // Should create an observable object even if empty
        Assert.NotNull(model);
    }

    [Fact]
    public void Parse_Grid_WithNoModel()
    {
        var providedModel = new { value = "test" };
        var markup = @"<grid>
    <div />
</grid>";
        var (_, model) = MarkupParser.Parse(markup, providedModel);

        // Should only have provided model properties
        Assert.NotNull(model);
        Assert.Equal("test", model!.value);
    }

    [Fact]
    public void Parse_Grid_WithInvalidJsonModel()
    {
        var markup = @"<grid>
    <model>{ invalid json }</model>
    <div />
</grid>";
        
        Assert.Throws<InvalidOperationException>(() => MarkupParser.Parse(markup));
    }

    [Fact]
    public void Parse_Grid_ModelWithComplexObject()
    {
        var markup = @"<grid>
    <model>{ ""user"": { ""name"": ""John"", ""age"": 30 }, ""settings"": { ""theme"": ""dark"" } }</model>
    <div />
</grid>";
        var (_, model) = MarkupParser.Parse(markup);

        Assert.NotNull(model);
        var user = model!.user;
        Assert.NotNull(user);
        
        var settings = model.settings;
        Assert.NotNull(settings);
    }

    [Fact]
    public void Parse_Grid_ModelPropertiesAccessible()
    {
        var markup = @"<grid>
    <model>{ ""buttonText"": ""Click Me"", ""count"": 42 }</model>
    <button><label value=""@buttonText"" /></button>
</grid>";
        var (element, model) = MarkupParser.Parse(markup);

        var button = element?.Children.Single() as Button;
        Assert.NotNull(button);
        Assert.Single(button.Children);
        var label = (Label)button.Children[0];
        Assert.NotNull(label);
        Assert.Single(label.Bindings);
        Assert.Equal("value", label.Bindings[0].TargetPropertyName);
        Assert.Equal("buttonText", label.Bindings[0].ModelPropertyName);

        Assert.NotNull(model);
        Assert.Equal("Click Me", model!.buttonText);
        Assert.Equal(42, model.count);
    }

    [Fact]
    public void Parse_Grid_MergesProvidedAndJsonModel()
    {
        var providedModel = new { existing = "value" };
        var markup = @"<grid>
    <model>{ ""newProp"": ""new"" }</model>
    <div />
</grid>";
        var (_, model) = MarkupParser.Parse(markup, providedModel);

        Assert.NotNull(model);
        Assert.Equal("value", model!.existing);
        Assert.Equal("new", model.newProp);
    }

    [Fact]
    public void Parse_Grid_ModelWithMixedTypes()
    {
        var markup = @"<grid>
    <model>{ ""str"": ""text"", ""num"": 123, ""bool"": true, ""arr"": [1, 2], ""obj"": { ""key"": ""val"" }, ""nil"": null }</model>
    <div />
</grid>";
        var (element, model) = MarkupParser.Parse(markup);

        Assert.NotNull(model);
        Assert.Equal("text", model!.str);
        Assert.Equal(123, model.num);
        Assert.True(model.@bool);
        Assert.Null(model.nil);
        
        var arr = model.arr as object[];
        Assert.NotNull(arr);
        Assert.Equal(2, arr.Length);
        
        var obj = model.obj;
        Assert.NotNull(obj);
    }

    [Fact]
    public void Parse_Style_ClassSelector()
    {
        var markup = @"<grid>
    <style>
        .header { padding: 10; margin: 5; }
    </style>
    <div class=""header"" />
</grid>";
        var (element, _) = MarkupParser.Parse(markup);

        var div = element.Children.Single() as Div;
        Assert.NotNull(div);
        Assert.Equal(new Thickness(10), div.Padding);
        Assert.Equal(new Thickness(5), div.Margin);
    }

    [Fact]
    public void Parse_Style_IdSelector()
    {
        var markup = @"<grid>
    <style>
        #main { width: 500; height: 300; }
    </style>
    <div id=""main"" />
</grid>";
        var (element, _) = MarkupParser.Parse(markup);

        var div = element.Children.Single() as Div;
        Assert.NotNull(div);
        Assert.Equal(new UnitValue(500), div.Width);
        Assert.Equal(new UnitValue(300), div.Height);
    }

    [Fact]
    public void Parse_Style_TagSelector()
    {
        var markup = @"<grid>
    <style>
        div { padding: 8; background: blue; }
    </style>
    <div />
</grid>";
        var (element, _) = MarkupParser.Parse(markup);

        var div = element.Children.Single() as Div;
        Assert.NotNull(div);
        Assert.Equal(new Thickness(8), div.Padding);
        Assert.Equal("blue", div.BackgroundColor);
    }

    [Fact]
    public void Parse_Style_UniversalSelector()
    {
        var markup = @"<grid>
    <style>
        * { margin: 5; padding: 3; }
    </style>
    <stack><div /><label /></stack>
</grid>";
        var (element, _) = MarkupParser.Parse(markup);

        var stack = element.Children.Single() as Stack;
        Assert.NotNull(stack);
        Assert.Equal(new Thickness(5), stack.Margin);
        Assert.Equal(new Thickness(3), stack.Padding);

        var div = (Div)stack.Children[0];
        Assert.Equal(new Thickness(5), div.Margin);
        Assert.Equal(new Thickness(3), div.Padding);

        var label = (Label)stack.Children[1];
        Assert.Equal(new Thickness(5), label.Margin);
        Assert.Equal(new Thickness(3), label.Padding);
    }

    [Fact]
    public void Parse_Style_MergeMultipleSelectors()
    {
        var markup = @"<grid>
    <style>
        * { padding: 5; }
        .container { margin: 10; }
        div { background: gray; }
    </style>
    <div class=""container"" />
</grid>";
        var (element, _) = MarkupParser.Parse(markup);

        var div = element.Children.Single() as Div;
        Assert.NotNull(div);
        Assert.Equal(new Thickness(5), div.Padding);      // from universal
        Assert.Equal(new Thickness(10), div.Margin);      // from class
        Assert.Equal("gray", div.BackgroundColor); // from tag
    }

    [Fact]
    public void Parse_Style_PrecedenceOverride()
    {
        var markup = @"<grid>
    <style>
        * { padding: 5; margin: 1; }
        div { padding: 8; background: blue; }
        .special { padding: 12; }
        #unique { padding: 20; }
    </style>
    <div id=""unique"" class=""special"" />
</grid>";
        var (element, _) = MarkupParser.Parse(markup);

        var div = element.Children.Single() as Div;
        Assert.NotNull(div);
        Assert.Equal(new Thickness(20), div.Padding);      // ID selector overrides all
        Assert.Equal(new Thickness(1), div.Margin);        // from universal
        Assert.Equal("blue", div.BackgroundColor);  // from tag selector
    }

    [Fact]
    public void Parse_Style_ClassSelectorOverridesTagAndUniversal()
    {
        var markup = @"<grid>
    <style>
        * { padding: 5; }
        div { padding: 8; }
        .highlight { padding: 12; }
    </style>
    <div class=""highlight"" />
</grid>";
        var (element, _) = MarkupParser.Parse(markup);

        var div = element.Children.Single() as Div;
        Assert.NotNull(div);
        Assert.Equal(new Thickness(12), div.Padding); // class overrides tag and universal
    }

    [Fact]
    public void Parse_Style_TagSelectorAppliedToChildren()
    {
        var markup = @"<grid>
    <style>
        label { padding: 6; }
    </style>
    <stack><label /><button /></stack>
</grid>";
        var (element, _) = MarkupParser.Parse(markup);

        var stack = element.Children.Single() as Stack;
        Assert.NotNull(stack);
        var label = (Label)stack.Children[0];
        Assert.Equal(new Thickness(6), label.Padding);

        var button = (Button)stack.Children[1];
        Assert.Equal(new Thickness(new UnitValue(0, UnitType.None)), button.Padding); // button not styled
    }

    [Fact]
    public void Parse_Style_MultipleClassesNotSupported()
    {
        // Currently only single class is supported, test verifies behavior
        var markup = @"<grid>
    <style>
        .header { padding: 10; }
    </style>
    <div class=""header_other"" />
</grid>";
        var (element, _) = MarkupParser.Parse(markup);

        var div = element.Children.Single() as Div;
        Assert.NotNull(div);
        // Class selector should not match "header other" exactly
        Assert.Equal(new Thickness(new UnitValue(0, UnitType.None)), div.Padding); // div not styled
    }

    [Fact]
    public void Parse_Style_DirectRootWithoutWrapper()
    {
        var markup = @"<div padding=""5"" margin=""10"" />";
        var (element, _) = MarkupParser.Parse(markup);

        var div = (Div)element;
        Assert.Equal(new Thickness(5), div.Padding);
        Assert.Equal(new Thickness(10), div.Margin);
    }

    [Fact]
    public void Parse_Style_WrapperWithOnlyVisualRoot()
    {
        var markup = @"<grid><div padding=""5"" /></grid>";
        var (element, _) = MarkupParser.Parse(markup);

        var div = element.Children.Single() as Div;
        Assert.NotNull(div);
        Assert.Equal(new Thickness(5), div.Padding);
    }

    [Fact]
    public void Parse_Style_WrapperWithModelAndStyle()
    {
        var markup = @"<grid>
    <model>{ ""name"": ""test"" }</model>
    <style>
        div { padding: 8; }
    </style>
    <div />
</grid>";
        var (element, model) = MarkupParser.Parse(markup);

        var div = element.Children.Single() as Div;
        Assert.NotNull(div);
        Assert.Equal(new Thickness(8), div.Padding);
        Assert.NotNull(model);
        Assert.Equal("test", model!.name);
    }

    [Fact]
    public void Parse_Style_IdPrecedenceOverClassAndTag()
    {
        var markup = @"<grid>
    <style>
        div { padding: 5; }
        .btn { padding: 8; }
        #submit { padding: 15; }
    </style>
    <div id=""submit"" class=""btn"" />
</grid>";
        var (element, _) = MarkupParser.Parse(markup);

        var div = element.Children.Single() as Div;
        Assert.NotNull(div);
        Assert.Equal(new Thickness(15), div.Padding); // ID takes highest precedence
    }

    [Fact]
    public void Parse_Style_MergeWithNoOverlap()
    {
        var markup = @"<grid>
    <style>
        * { margin: 5; }
        .card { padding: 10; }
        div { background: white; }
    </style>
    <div class=""card"" />
</grid>";
        var (element, _) = MarkupParser.Parse(markup);

        var div = element.Children.Single() as Div;
        Assert.NotNull(div);
        Assert.Equal(new Thickness(5), div.Margin);        // from universal
        Assert.Equal(new Thickness(10), div.Padding);      // from class
        Assert.Equal("white", div.BackgroundColor); // from tag
    }
}

