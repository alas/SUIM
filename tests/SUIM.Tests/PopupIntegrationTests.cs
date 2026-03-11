namespace SUIM.Tests;

using System.Text;
using Xunit;
using Stride.Engine;
using SUIM.Parse;
using SUIM.Parse.Components;
using SUIMStride;
using Chess3d.SUIM.Components;
using Chess3d.SUIM.Views;

public class PopupIntegrationTests
{
    [Fact]
    public void MainView_ButtonsHaveWidthFromCSS()
    {
        const int TOTAL_WIDTH = 1280;
        const int TOTAL_HEIGHT = 720;

        var game = new Game();
        var rootPath = "..\\..\\..\\..\\..\\src\\Example\\Chess3d\\SUIM";
        ComponentRegistry.Register("ScreenOverlay", rootPath: rootPath);
        ComponentRegistry.Register<Popup>();
        ComponentRegistry.Register(nameof(MainView), () => new MainView() { Game = game });
        var parser = new Parser { RootPath = rootPath };
        var (strideRoot, model) = parser.GetView("MainView", game);

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
        Assert.Equal("collapsed", model!.GetValue("PopupVisibility"));

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
