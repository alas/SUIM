namespace SUIM.Tests.Parsing;

using System.IO;
using Xunit;
using SUIM.Parse;
using SUIM.Parse.Components;

public class SUIMProjectTests
{
    [Fact]
    public void GetView_ResolvesDependenciesRecursively()
    {
        // 1. Setup a dummy project structure
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(Path.Combine(tempDir, "views"));
        Directory.CreateDirectory(Path.Combine(tempDir, "components"));
        Directory.CreateDirectory(Path.Combine(tempDir, "styles"));

        var viewMarkup = @"<grid>
    <style src=""styles/main.suim"" />
    <CompA />
</grid>";
        var compAMarkup = @"<stack>
    <CompB />
</stack>";
        var compBMarkup = @"<h1>Hello from B</h1>";
        var styleContent = "grid { background: blue; }";

        File.WriteAllText(Path.Combine(tempDir, "views", "Main.suim"), viewMarkup);
        File.WriteAllText(Path.Combine(tempDir, "components", "CompA.suim"), compAMarkup);
        File.WriteAllText(Path.Combine(tempDir, "components", "CompB.suim"), compBMarkup);
        File.WriteAllText(Path.Combine(tempDir, "styles", "main.suim"), styleContent);

        try
        {
            // 2. Initialize project and get view
            var project = new SUIMProject(tempDir);
            var (element, _) = project.GetView("Main");

            // 3. Verify
            Assert.IsType<Grid>(element);
            Assert.Equal("blue", element.BackgroundColor);
            
            var stack = Assert.IsType<Stack>(element.Children[0]);
            var label = Assert.IsType<Text>(stack.Children[0].Children[0]);
            
            Assert.Equal("Hello from B", label.Value);
            
            // Verify registration
            Assert.True(ComponentRegistry.IsRegistered("CompA"));
            Assert.True(ComponentRegistry.IsRegistered("CompB"));
        }
        finally
        {
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
        }
    }
}
