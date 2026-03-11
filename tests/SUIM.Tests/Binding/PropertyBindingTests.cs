namespace SUIM.Tests.Binding;

using Xunit;
using SUIM.Parse;
using SUIM.Parse.Components;

public class PropertyBindingTests
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
    public void Parse_DataBinding_Width_CreatesBindingDefinition()
    {
        var markup = "<div width=\"@currentWidth\" height=\"100\" />";
        var element = MarkupParser.Parse(markup, _model);

        Assert.IsType<Div>(element);
        var div = (Div)element;
        
        // Should have a binding for "width" -> "currentWidth"
        Assert.Single(div.Bindings);
        var binding = div.Bindings[0];
        Assert.Equal("width", binding.TargetPropertyName);
        Assert.Equal("currentWidth", binding.ModelPropertyName);
        
        // Initial value is NOT applied during parse anymore, binding is deferred to mapper
        // Assert.Equal(new UnitValue(250), div.Width); // This would fail now
    }

    [Fact]
    public void Parse_DataBinding_Text_CreatesBindingDefinition()
    {
        var markup = "<input id=\"username\" type=\"text\" value=\"@stringValue\" />";
        var element = MarkupParser.Parse(markup, _model);

        Assert.IsType<Input>(element);
        var input = (Input)element;
        
        Assert.Single(input.Bindings);
        var binding = input.Bindings[0];
        Assert.Equal("value", binding.TargetPropertyName);
        Assert.Equal("stringValue", binding.ModelPropertyName);
    }

    [Fact]
    public void Parse_Suim_ModelProperties_CreatesBindings()
    {
        var markup = @"<grid>
    <model>{ ""buttonText"": ""Click Me"", ""count"": 42 }</model>
    <button><input type=""text"" value=""@buttonText"" /></button>
</grid>";
        var element = MarkupParser.Parse(markup);

        var button = element.Children.Single() as Button;
        var input = button?.Children.Single() as Input;
        
        Assert.NotNull(input);
        Assert.Single(input!.Bindings);
        Assert.Equal("value", input.Bindings[0].TargetPropertyName);
        Assert.Equal("buttonText", input.Bindings[0].ModelPropertyName);
    }

    [Fact]
    public void Parse_Suim_ModelProperties_CreatesBindingsH1()
    {
        var markup = @"<grid>
    <model>{ ""buttonText"": ""Click Me"", ""count"": 42 }</model>
    <button><h1>@buttonText</h1></button>
</grid>";
        var element = MarkupParser.Parse(markup);

        var t = element.Children.Single()?.Children.Single()?.Children.Single() as Text;
        
        Assert.NotNull(t);
        Assert.Single(t!.Bindings);
        Assert.Equal("value", t.Bindings[0].TargetPropertyName);
        Assert.Equal("buttonText", t.Bindings[0].ModelPropertyName);
    }

    [Fact]
    public void Parse_Suim_ModelProperties_CreatesBindingsText()
    {
        var markup = @"<div>@buttonText</div>";
        var element = MarkupParser.Parse(markup);

        var t = element.Children.Single() as Text;

        Assert.NotNull(t);
        Assert.Single(t!.Bindings);
        Assert.Equal("value", t.Bindings[0].TargetPropertyName);
        Assert.Equal("buttonText", t.Bindings[0].ModelPropertyName);
    }

    [Fact]
    public void Parse_Suim_ModelProperties_CreatesBindingsText2()
    {
        var markup = @"<div>@buttonText1 @buttonText2</div>";
        var element = MarkupParser.Parse(markup);

        Assert.Equal(3, element.Children.Count);

        var child = element.Children[0];
        Assert.IsType<Text>(child);
        var t = child as Text;
        Assert.NotNull(t);
        Assert.Single(t!.Bindings);
        Assert.Equal("value", t.Bindings[0].TargetPropertyName);
        Assert.Equal("buttonText1", t.Bindings[0].ModelPropertyName);

        child = element.Children[1];
        Assert.IsType<Text>(child);
        t = child as Text;
        Assert.NotNull(t);
        Assert.Empty(t!.Bindings);
        Assert.Equal(" ", t.Value);

        child = element.Children[2];
        Assert.IsType<Text>(child);
        t = child as Text;
        Assert.NotNull(t);
        Assert.Single(t!.Bindings);
        Assert.Equal("value", t.Bindings[0].TargetPropertyName);
        Assert.Equal("buttonText2", t.Bindings[0].ModelPropertyName);
    }

    [Fact]
    public void Parse_Suim_ModelProperties_CreatesBindingsText3()
    {
        var markup = @"<div>@buttonText1 @@text @buttonText2</div>";
        var element = MarkupParser.Parse(markup);

        Assert.Equal(3, element.Children.Count);

        var child = element.Children[0];
        Assert.IsType<Text>(child);
        var t = child as Text;
        Assert.NotNull(t);
        Assert.Single(t!.Bindings);
        Assert.Equal("value", t.Bindings[0].TargetPropertyName);
        Assert.Equal("buttonText1", t.Bindings[0].ModelPropertyName);

        child = element.Children[1];
        Assert.IsType<Text>(child);
        t = child as Text;
        Assert.NotNull(t);
        Assert.Empty(t!.Bindings);
        Assert.Equal(" @@text ", t.Value);

        child = element.Children[2];
        Assert.IsType<Text>(child);
        t = child as Text;
        Assert.NotNull(t);
        Assert.Single(t!.Bindings);
        Assert.Equal("value", t.Bindings[0].TargetPropertyName);
        Assert.Equal("buttonText2", t.Bindings[0].ModelPropertyName);
    }

    [Fact]
    public void Parse_Suim_ModelProperties_CreatesBindingsText4()
    {
        var markup = @"<h1>@@lol @buttonText1 @@text @buttonText2 @ ytytyt @@ytytyt @@</h1>";
        var element = MarkupParser.Parse(markup);

        Assert.Equal(5, element.Children.Count);

        var child = element.Children[0];
        Assert.IsType<Text>(child);
        var t = child as Text;
        Assert.NotNull(t);
        Assert.Empty(t!.Bindings);
        Assert.Equal("@@lol ", t.Value);

        child = element.Children[1];
        Assert.IsType<Text>(child);
        t = child as Text;
        Assert.NotNull(t);
        Assert.Single(t!.Bindings);
        Assert.Equal("value", t.Bindings[0].TargetPropertyName);
        Assert.Equal("buttonText1", t.Bindings[0].ModelPropertyName);

        child = element.Children[2];
        Assert.IsType<Text>(child);
        t = child as Text;
        Assert.NotNull(t);
        Assert.Empty(t!.Bindings);
        Assert.Equal(" @@text ", t.Value);

        child = element.Children[3];
        Assert.IsType<Text>(child);
        t = child as Text;
        Assert.NotNull(t);
        Assert.Single(t!.Bindings);
        Assert.Equal("value", t.Bindings[0].TargetPropertyName);
        Assert.Equal("buttonText2", t.Bindings[0].ModelPropertyName);

        child = element.Children[4];
        Assert.IsType<Text>(child);
        t = child as Text;
        Assert.NotNull(t);
        Assert.Empty(t!.Bindings);
        Assert.Equal(" @ ytytyt @@ytytyt @@", t.Value);
    }
}
