namespace SUIM.Tests;

using System.Text;
using Xunit;
using SUIM.Parse.Components;

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

        var (suimRoot, model) = Parse.MarkupParser.Parse(markup, model: null, basePath: rootPath);

        // Show popup
        model!.PopupTitle = "Test Title";
        model.PopupMessage = "Test Message";
        model.PopupVisibility = "visible";

        suimRoot.CalculateLayout(1280, 720);

        // Find overlay component
        var overlay = FindOverlay(suimRoot);
        Assert.NotNull(overlay);

        var sb = new StringBuilder();
        overlay.AppendDebugString(sb);
        Console.WriteLine(sb.ToString());

        // Overlay should fill entire screen
        Assert.Equal(1280, overlay.GetWidth());
        //Assert.Equal(720, overlay.GetHeight());
        Assert.Equal(0, overlay.GetLeft());
        Assert.Equal(0, overlay.GetTop());
        
        // Find popup content (vstack with wood-panel class)
        var popupContent = FindPopupContent(overlay);
        Assert.NotNull(popupContent);
        
        // Content should be centered horizontally
        var contentX = popupContent.GetX();
        var contentWidth = popupContent.GetWidth();
        var expectedX = (1280 - contentWidth) / 2;
        Assert.True(Math.Abs(contentX - expectedX) < 5, 
            $"Content should be horizontally centered. Expected X ~{expectedX}, got {contentX}");
        
        // Content should be centered vertically
        var contentY = popupContent.GetY();
        var contentHeight = popupContent.GetHeight();
        var expectedY = (720 - contentHeight) / 2;
        Assert.True(Math.Abs(contentY - expectedY) < 5,
            $"Content should be vertically centered. Expected Y ~{expectedY}, got {contentY}");
    }
    
    private static Overlay? FindOverlay(UIElement root)
    {
        if (root is Overlay overlay) return overlay;
        foreach (var child in root.Children)
        {
            var found = FindOverlay(child);
            if (found != null) return found;
        }
        return null;
    }
    
    private static UIElement? FindPopupContent(UIElement root)
    {
        if (root.Class?.Contains("wood-panel") == true) return root;
        foreach (var child in root.Children)
        {
            var found = FindPopupContent(child);
            if (found != null) return found;
        }
        return null;
    }
}
