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
    public void PropertyBinding_Should_Update_Target_On_Initialize()
    {
        var model = Create(new { Text = "Hello" });
        var element = new Text();
        var binding = new PropertyBinding(model, "Text", element, "value");

        binding.Apply();

        Assert.Equal("Hello", element.Value);
    }

    [Fact]
    public void PropertyBinding_Should_Update_Target_When_Model_Changes()
    {
        var model = Create(new { Text = "Initial" });
        var element = new Text();
        using var binding = new PropertyBinding(model, "Text", element, "value");
        binding.Apply();

        Assert.Equal("Initial", element.Value);

        model.Text = "Updated";

        Assert.Equal("Updated", element.Value);
    }

    [Fact]
    public void PropertyBinding_Should_Stop_Updating_After_Dispose()
    {
        var model = Create(new { FontSize = 10 });
        var element = new Text();
        var binding = new PropertyBinding(model, "FontSize", element, "fontsize");
        binding.Apply();

        Assert.Equal(10f, element.FontSize);

        binding.Dispose();

        model.FontSize = 20;

        Assert.Equal(10f, element.FontSize);
    }

    [Fact]
    public void PropertyBinding_Should_Work_With_SUIM_Create_And_AnonymousTypes()
    {
        var model = Create(new { Text = "Dynamic", FontSize = 42 });
        var element = new Text();

        var binder1 = new PropertyBinding(model, "Text", element, "value");
        binder1.Apply();

        var binder2 = new PropertyBinding(model, "FontSize", element, "fontsize");
        binder2.Apply();

        Assert.Equal("Dynamic", element.Value);
        Assert.Equal(42f, element.FontSize);
    }

    [Fact]
    public void PropertyBinding_Should_Update_When_Dynamic_Property_Set()
    {
        var model = Create(new { Text = "Initial" });
        var element = new Text();
        var binding = new PropertyBinding(model, "Text", element, "value");
        binding.Apply();

        Assert.Equal("Initial", element.Value);

        model.Text = "Updated";

        Assert.Equal("Updated", element.Value);
    }

    // ============== DATA BINDING TESTS ==============

    [Fact]
    public void Parse_DataBinding_Width()
    {
        var markup = "<div width=\"@currentWidth\" height=\"100\" />";
        var (element, _) = MarkupParser.Parse(markup, _model);

        Assert.IsType<Div>(element);
        var div = (Div)element;
        // Property binding should be created for width
        Assert.Equal(new UnitValue(250), div.Width);
    }

    [Fact]
    public void Parse_DataBinding_Text()
    {
        var markup = "<label value=\"@stringValue\" />";
        var (element, _) = MarkupParser.Parse(markup, _model);

        Assert.IsType<Label>(element);
        var label = (Label)element;
        // Property binding should be created for text
        Assert.NotNull(label.Value);
        Assert.Equal("test", label.Value);
    }

    [Fact]
    public void Parse_Suim_ModelPropertiesAccessibleAndBindingWorks()
    {
        var markup = @"<grid>
    <model>{ ""buttonText"": ""Click Me"", ""count"": 42 }</model>
    <button><label value=""@buttonText"" /></button>
</grid>";
        var (element, model) = MarkupParser.Parse(markup);

        var label = element?.Children?.Single().Children?.Single() as Label;
        Assert.Equal("Click Me", label?.Value);
        model!.buttonText = "Updated Text";
        Assert.Equal("Updated Text", label!.Value);
    }

    private static dynamic Create(object model)
    {
        var observable = new ObservableObject();
        observable.Initialize(model);
        return observable;
    }
}
