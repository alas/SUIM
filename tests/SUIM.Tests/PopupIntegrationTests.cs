namespace SUIM.Tests;

using System.Text;
using Xunit;
using Stride.Engine;
using SUIM.Model;
using SUIM.Parse.Components;
using SUIMStride;

public class PopupIntegrationTests
{
    [Fact]
    public void MainView_ButtonsHaveWidthFromCSS()
    {
        const int TOTAL_WIDTH = 1280;
        const int TOTAL_HEIGHT = 720;
        var rootPath = "..\\..\\..\\..\\..\\src\\Example\\Chess3d\\SUIM";
        var project = new SUIMProject(rootPath);
        var projectViewsPath = Path.Combine(rootPath, "views", "MainView.suim");

        var markup = File.ReadAllText(projectViewsPath);
        project.ResolveDependencies(markup);

        var model = new ObservableObject();
        model.SetValue("PopupTitle", "");
        model.SetValue("PopupMessage", "");
        model.SetValue("PopupVisibility", "collapsed");
        model.SetValue("OverlayMessage", "");
        model.SetValue("OverlayVisibility", "collapsed");

        model.SetValue("OpenPopup", new Action<string, string>((title, message) =>
        {
            model.SetValue("PopupTitle", title);
            model.SetValue("PopupMessage", message);
            model.SetValue("PopupVisibility", "visible");
        }));
        model.SetValue("ClosePopup", new Action(() =>
        {
            model.SetValue("PopupVisibility", "collapsed");
        }));

        var game = new Game();
        var parser = new Parser { RootPath = rootPath };
        var (strideRoot, _) = parser.GetView("MainView", game, model: model);
        var suimRoot = parser.GetSuimRootFor(strideRoot);
        Assert.NotNull(suimRoot);

        suimRoot!.CalculateLayout(TOTAL_WIDTH, TOTAL_HEIGHT);

        // Find overlay component
        var overlay = FindOverlay(suimRoot);
        Assert.NotNull(overlay);

        var sb = new StringBuilder();
        overlay.AppendDebugString(sb);
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
        var expectedLeft = (TOTAL_WIDTH - contentWidth) / 2;
        Assert.True(Math.Abs(contentLeft - expectedLeft) < 5, 
            $"Content should be horizontally centered. Expected X ~{expectedLeft}, got {contentLeft}");
        
        // Content should be centered vertically
        var contentTop = popupContent.GetTop();
        var contentHeight = popupContent.GetHeight();
        var expectedTop = (TOTAL_HEIGHT - contentHeight) / 2;
        Assert.True(Math.Abs(contentTop - expectedTop) < 5,
            $"Content should be vertically centered. Expected Y ~{expectedTop}, got {contentTop}");

        // Overlay should be hidden at start
        Assert.Equal("collapsed", model.GetValue("PopupVisibility"));

        // Click "Restart" -> open popup
        var restartButton = FindButtonByText(suimRoot, "Restart");
        Assert.NotNull(restartButton);
        restartButton!.TriggerEvent("click");
        Assert.Equal("visible", model.GetValue("PopupVisibility"));

        // Click "NO" -> close popup
        var noButton = FindButtonByText(suimRoot, "NO");
        Assert.NotNull(noButton);
        noButton!.TriggerEvent("click");
        Assert.Equal("collapsed", model.GetValue("PopupVisibility"));
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
        if (root.Class?.Contains("window") == true) return root;
        foreach (var child in root.Children)
        {
            var found = FindPopupContent(child);
            if (found != null) return found;
        }
        return null;
    }

    private static Button? FindButtonByText(UIElement root, string text)
    {
        if (root is Button btn)
        {
            foreach (var child in btn.Children)
            {
                if (child is Text t && string.Equals(t.Value, text, StringComparison.OrdinalIgnoreCase))
                {
                    return btn;
                }
            }
        }

        foreach (var child in root.Children)
        {
            var found = FindButtonByText(child, text);
            if (found != null) return found;
        }
        return null;
    }
}
