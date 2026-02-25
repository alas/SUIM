namespace SUIM.Tests;

using System.Collections.Generic;
using System.Linq;
using Xunit;
using Stride.UI;
using Stride.UI.Controls;
using Stride.UI.Panels;
using Stride.Engine;
using SUIMStride;

public class PopupIntegrationTests
{
    private static Game CreateTestGame()
    {
        var game = new Game();
        game.GraphicsDeviceManager.PreferredBackBufferWidth = 1280;
        game.GraphicsDeviceManager.PreferredBackBufferHeight = 720;
        return game;
    }

    private static List<Button> CollectButtons(UIElement element)
    {
        var buttons = new List<Button>();
        
        if (element is Button button)
        {
            buttons.Add(button);
        }

        if (element is Panel panel)
        {
            foreach (var child in panel.Children)
            {
                buttons.AddRange(CollectButtons(child));
            }
        }

        return buttons;
    }

    [Fact]
    public void MainViewWithPopup_ChecksButtonActualProperties()
    {
        var game = CreateTestGame();
        
        var model = new
        {
            OpenPopup = (Action<string, string>)((title, message) => { }),
            YesHandler = (Action)(() => { }),
            NoHandler = (Action)(() => { }),
            PopupTitle = "Test Title",
            PopupMessage = "Test Message",
            PopupVisibility = "visible",
        };

        var parser = new Parser { RootPath = "..\\..\\..\\..\\..\\src\\Example\\Chess3d\\SUIM" };
        var (strideRoot, _) = parser.GetView("MainView", game, model: model);

        Assert.NotNull(strideRoot);

        // Collect all buttons from the view
        var buttons = CollectButtons(strideRoot);
        Assert.NotEmpty(buttons);

        // Check MainView buttons (Quit, Restart, Load, Save)
        var mainViewButtons = buttons.Where(b => b.Content is TextBlock).ToList();
        Assert.NotEmpty(mainViewButtons);

        foreach (var button in mainViewButtons)
        {
            Assert.True(button.ActualWidth == 200, "Button should have ActualWidth > 0");
            Assert.True(button.ActualHeight == 50, "Button should have ActualHeight > 0");
            Assert.True(button.RenderOffsets.X == 5, "Button should have valid RenderOffsets.X");
            Assert.True(button.RenderOffsets.Y > 0, "Button should have valid RenderOffsets.Y");
        }
    }
}
