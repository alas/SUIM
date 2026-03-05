namespace SUIM.Tests.Parsing;

using System.IO;
using Xunit;
using SUIM.Parse;
using SUIM.Parse.Components;

public class CustomComponentTests
{
    [Fact]
    public void Parse_CustomTag_ExpandsFromFile()
    {
        // 1. Create a dummy .suim file
        var tempFile = Path.GetTempFileName() + ".suim";
        File.WriteAllText(tempFile, "<stack><h1>Inside Custom</h1></stack>");

        try
        {
            // 2. Register the custom tag
            ComponentRegistry.Register("MyCustomTag", tempFile);

            // 3. Parse markup using the custom tag
            var markup = "<grid><MyCustomTag /></grid>";
            var (element, _) = MarkupParser.Parse(markup);

            // 4. Verify expansion
            Assert.IsType<Grid>(element);
            Assert.Single(element.Children);
            
            var custom = Assert.IsType<CustomComponent>(element.Children[0]);
            Assert.Single(custom.Children);
            
            var stack = Assert.IsType<Stack>(custom.Children[0]);
            Assert.Single(stack.Children);
            
            var label = Assert.IsType<Text>(stack.Children[0].Children[0]);
            Assert.Equal("Inside Custom", label.Value);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public void Programmatic_Instantiation_Works()
    {
        // 1. Register a factory
        ComponentRegistry.Register("FactoryTag", () => new Stack { Id = "programmatic" });

        // 2. Instantiate via registry
        var element = ComponentRegistry.Create("FactoryTag");

        // 3. Verify
        Assert.IsType<Stack>(element);
        Assert.Equal("programmatic", element.Id);
    }

    [Fact]
    public void Parse_CustomTag_AppliesStylesByTagName()
    {
        // 1. Create a dummy .suim file
        var tempFile = Path.GetTempFileName() + ".suim";
        File.WriteAllText(tempFile, "<div id=\"inner\" />");

        try
        {
            // 2. Register the custom tag
            ComponentRegistry.Register("StyledTag", tempFile);

            // 3. Parse markup with a style for StyledTag
            var markup = @"<grid>
<style>
StyledTag { width: 450; background: red; }
</style>
<StyledTag />
</grid>";
            var (element, _) = MarkupParser.Parse(markup);

            // 4. Verify style application
            var custom = Assert.IsType<CustomComponent>(element.Children[0]);
            Assert.Equal("450", custom.GetAttribute("width"));
            Assert.Equal("red", custom.BackgroundColor);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }
}
