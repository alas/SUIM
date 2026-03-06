namespace SUIM.Tests;

using SUIM.Parse.Components;
using System.Text;
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

        var (suimRoot, model) = Parse.MarkupParser.Parse(markup, model: null, basePath: rootPath);

        suimRoot.CalculateLayout(1280, 720);

        // Show popup
        model!.PopupTitle = "Test Title";
        model.PopupMessage = "Test Message";
        model.PopupVisibility = "visible";

        // Find overlay component
        var overlay = FindOverlay(suimRoot);
        Assert.NotNull(overlay);
        var sb = new StringBuilder();
        GetLayout(overlay, sb);
        Console.WriteLine(sb.ToString());

        // Overlay should fill entire screen
        Assert.Equal(1280, overlay.GetWidth());
        Assert.Equal(720, overlay.GetHeight());
        Assert.Equal(0, overlay.GetLeft());
        Assert.Equal(0, overlay.GetTop());
        
        // Find popup content (vstack with wood-panel class)
        var popupContent = FindPopupContent(overlay);
        Assert.NotNull(popupContent);
        
        // Content should be centered horizontally
        var contentLeft = popupContent.GetLeft();
        var contentWidth = popupContent.GetWidth();
        var expectedLeft = (1280 - contentWidth) / 2;
        Assert.True(Math.Abs(contentLeft - expectedLeft) < 5, 
            $"Content should be horizontally centered. Expected left ~{expectedLeft}, got {contentLeft}");
        
        // Content should be centered vertically
        var contentTop = popupContent.GetTop();
        var contentHeight = popupContent.GetHeight();
        var expectedTop = (720 - contentHeight) / 2;
        Assert.True(Math.Abs(contentTop - expectedTop) < 5,
            $"Content should be vertically centered. Expected top ~{expectedTop}, got {contentTop}");
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

    private static void GetLayout(UIElement element, StringBuilder sb, int indent = 0)
    {
        element.AppendDebugString(sb, indent);

        foreach (var child in element.Children)
        {
            GetLayout(child, sb, indent + 1);
        }
    }
}
