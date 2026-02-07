namespace SUIM.Tests;

using Xunit;

public class PropertyBindingTests
{
    [Fact]
    public void PropertyBinding_Should_Update_Target_On_Initialize()
    {
        var model = SUIM.Create(new { Text = "Hello" });
        var element = new BaseText();
        var binding = new PropertyBinding(model, "Text", element, "text");

        binding.Apply();

        Assert.Equal("Hello", element.Text);
    }

    [Fact]
    public void PropertyBinding_Should_Update_Target_When_Model_Changes()
    {
        var model = SUIM.Create(new { Text = "Initial" });
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
        var model = SUIM.Create(new { FontSize = 10 });
        var element = new BaseText();
        var binding = new PropertyBinding(model, "FontSize", element, "fontsize");
        binding.Apply();

        Assert.Equal(10, element.FontSize);

        binding.Dispose();

        model.Count = 20;

        Assert.Equal(10, element.FontSize);
    }

    [Fact]
    public void PropertyBinding_Should_Work_With_SUIM_Create_And_AnonymousTypes()
    {
        var model = SUIM.Create(new { Text = "Dynamic", FontSize = 42 });
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
        var model = SUIM.Create(new { Text = "Initial" });
        var element = new BaseText();
        var binding = new PropertyBinding(model, "Text", element, "text");
        binding.Apply();

        Assert.Equal("Initial", element.Text);

        model.Text = "Updated";

        Assert.Equal("Updated", element.Text);
    }
}
