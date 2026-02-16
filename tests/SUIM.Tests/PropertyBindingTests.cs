namespace SUIM.Tests;

using Xunit;
using SUIM.Components;
using SUIM.Layout;

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
        var (element, _) = MarkupParser.Parse(markup, _model);

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
        var markup = "<label value=\"@stringValue\" />";
        var (element, _) = MarkupParser.Parse(markup, _model);

        Assert.IsType<Label>(element);
        var label = (Label)element;
        
        Assert.Single(label.Bindings);
        var binding = label.Bindings[0];
        Assert.Equal("value", binding.TargetPropertyName);
        Assert.Equal("stringValue", binding.ModelPropertyName);
    }

    [Fact]
    public void Parse_Suim_ModelProperties_CreatesBindings()
    {
        var markup = @"<grid>
    <model>{ ""buttonText"": ""Click Me"", ""count"": 42 }</model>
    <button><label value=""@buttonText"" /></button>
</grid>";
        var (element, model) = MarkupParser.Parse(markup);

        var button = element.Children.Single() as Button;
        var label = button?.Children.Single() as Label;
        
        Assert.NotNull(label);
        Assert.Single(label!.Bindings);
        Assert.Equal("value", label.Bindings[0].TargetPropertyName);
        Assert.Equal("buttonText", label.Bindings[0].ModelPropertyName);
    }
}
