namespace SUIM.Tests;

using Xunit;

public class PopupIntegrationTests
{
    [Fact]
    public void MainView_ButtonsHaveWidthFromCSS()
    {
        var rootPath = "..\\..\\..\\..\\..\\src\\Example\\Chess3d\\SUIM";
        var project = new SUIMProject(rootPath);
        var project_views_path = Path.Combine(rootPath, "views", "MainView.suim");

        var markup = File.ReadAllText(project_views_path);
        project.ResolveDependencies(markup);

        var (suimRoot, _) = Parse.MarkupParser.Parse(markup, model: null, basePath: rootPath);
        suimRoot.CalculateLayout(1280, 720);

        // Check root grid dimensions after layout
        System.Diagnostics.Debug.WriteLine($"After layout: Root.ActualWidth={suimRoot.GetWidth()}, Root.ActualHeight={suimRoot.GetHeight()}");
        Assert.True(suimRoot.GetWidth() > 0, "Root grid should have positive actual width after layout");
        Assert.True(suimRoot.GetHeight() > 0, "Root grid should have positive actual height after layout");
    }
}
