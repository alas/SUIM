namespace SUIM.Tests;

using System.Collections.Generic;
using System.Linq;
using Xunit;
using Stride.UI;
using Stride.UI.Controls;
using Stride.UI.Panels;
using Stride.Engine;
using SUIM;
using SUIM.Layout;
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

    private static List<Components.Button> FindSuimButtons(Components.UIElement elem)
    {
        var buttons = new List<Components.Button>();
        if (elem is Components.Button btn)
            buttons.Add(btn);
        foreach (var child in elem.Children)
            buttons.AddRange(FindSuimButtons(child));
        return buttons;
    }

    [Fact]
    public void MainView_ButtonsHaveWidthFromCSS()
    {
        var rootPath = "..\\..\\..\\..\\..\\src\\Example\\Chess3d\\SUIM";
        var project = new SUIMProject(rootPath);
        var project_views_path = Path.Combine(rootPath, "views", "MainView.suim");
        if (!File.Exists(project_views_path))
            project_views_path = Path.Combine(rootPath, "Views", "MainView.suim");

        Assert.True(File.Exists(project_views_path), $"MainView.suim not found at {project_views_path}");

        var markup = File.ReadAllText(project_views_path);
        project.ResolveDependencies(markup);

        var (suimRoot, _) = MarkupParser.Parse(markup, model: null, basePath: rootPath);

        var buttons = FindSuimButtons(suimRoot);
        Assert.NotEmpty(buttons);

        foreach (var btn in buttons)
        {
            System.Diagnostics.Debug.WriteLine($"Before layout: Button.Width={btn.Width ?? "null"}, Button.Height={btn.Height ?? "null"}");
            // CSS should have been applied during parsing
            Assert.NotNull(btn.Width);
            Assert.NotNull(btn.Height);
        }

        // Now layout
        LayoutEngine.Layout(suimRoot, 16, 1920, 1080);

        foreach (var btn in buttons)
        {
            System.Diagnostics.Debug.WriteLine($"After layout: Button.ActualWidth={btn.ActualWidth}, Button.ActualHeight={btn.ActualHeight}");
        }
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

        var rootPath = "..\\..\\..\\..\\..\\src\\Example\\Chess3d\\SUIM";
        var parser = new Parser { RootPath = rootPath };
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
            Assert.Equal(200, button.Width);
            Assert.Equal(50, button.Height);
            Assert.Equal(5, button.Margin.Left);
            Assert.Equal(5, button.Margin.Top);
            Assert.Equal(5, button.Margin.Right);
            Assert.Equal(5, button.Margin.Bottom);
        }
    }
}
