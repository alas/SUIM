namespace SUIM.Tests;

using Xunit;
using SUIM.Components;

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
        var element = new BaseText();
        var binding = new PropertyBinding(model, "Text", element, "text");

        binding.Apply();

        Assert.Equal("Hello", element.Text);
    }

    [Fact]
    public void PropertyBinding_Should_Update_Target_When_Model_Changes()
    {
        var model = Create(new { Text = "Initial" });
        var element = new BaseText();
        using var binding = new PropertyBinding(model, "Text", element, "text");
        binding.Apply();

        Assert.Equal("Initial", element.Text);

        model.Text = "Updated";

        Assert.Equal("Updated", element.Text);
    }

    [Fact]
    public void PropertyBinding_Should_Stop_Updating_After_Dispose()
    {
        var model = Create(new { FontSize = 10 });
        var element = new BaseText();
        var binding = new PropertyBinding(model, "FontSize", element, "fontsize");
        binding.Apply();

        Assert.Equal(10, element.FontSize);

        binding.Dispose();

        model.FontSize = 20;

        Assert.Equal(10, element.FontSize);
    }

    [Fact]
    public void PropertyBinding_Should_Work_With_SUIM_Create_And_AnonymousTypes()
    {
        var model = Create(new { Text = "Dynamic", FontSize = 42 });
        var element = new BaseText();

        var binder1 = new PropertyBinding(model, "Text", element, "text");
        binder1.Apply();

        var binder2 = new PropertyBinding(model, "FontSize", element, "fontsize");
        binder2.Apply();

        Assert.Equal("Dynamic", element.Text);
        Assert.Equal(42, element.FontSize);
    }

    [Fact]
    public void PropertyBinding_Should_Update_When_Dynamic_Property_Set()
    {
        var model = Create(new { Text = "Initial" });
        var element = new BaseText();
        var binding = new PropertyBinding(model, "Text", element, "text");
        binding.Apply();

        Assert.Equal("Initial", element.Text);

        model.Text = "Updated";

        Assert.Equal("Updated", element.Text);
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
        Assert.NotNull(div.Width);
    }

    [Fact]
    public void Parse_DataBinding_Text()
    {
        var markup = "<label text=\"@stringValue\" />";
        var (element, _) = MarkupParser.Parse(markup, _model);

        Assert.IsType<Label>(element);
        var label = (Label)element;
        // Property binding should be created for text
        Assert.NotNull(label.Text);
    }

    private static dynamic Create(object model)
    {
        var observable = new ObservableObject();
        observable.Initialize(model);
        return observable;
    }
}
