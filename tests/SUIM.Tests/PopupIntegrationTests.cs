namespace SUIM.Tests;

using System.Collections.Generic;
using System.Linq;
using Xunit;
using Stride.UI.Controls;
using Stride.UI.Panels;
using Stride.Engine;
using StrideUIElement = Stride.UI.UIElement;
using StrideButton = Stride.UI.Controls.Button;
using SUIM.Layout;
using SUIMStride;
using SUIMElement = Parse.Components.UIElement;

public class PopupIntegrationTests
{
    private static Game CreateTestGame()
    {
        var game = new Game();
        game.GraphicsDeviceManager.PreferredBackBufferWidth = 1280;
        game.GraphicsDeviceManager.PreferredBackBufferHeight = 720;
        return game;
    }

    private static List<StrideButton> CollectButtons(StrideUIElement element)
    {
        var buttons = new List<StrideButton>();

        if (element is StrideButton button)
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

    private static List<Parse.Components.Button> FindSuimButtons(SUIMElement elem)
    {
        var buttons = new List<Parse.Components.Button>();
        if (elem is Parse.Components.Button btn)
            buttons.Add(btn);
        foreach (var child in elem.Children)
            buttons.AddRange(FindSuimButtons(child));
        return buttons;
    }

    private static SUIMElement? FindElementByClass(SUIMElement elem, string className)
    {
        if (!string.IsNullOrEmpty(elem.Class) && elem.Class.Contains(className))
            return elem;
        foreach (var child in elem.Children)
        {
            var result = FindElementByClass(child, className);
            if (result != null)
                return result;
        }
        return null;
    }

    private static SUIMElement? FindElementByType<T>(SUIMElement elem) where T : SUIMElement
    {
        if (elem is T)
            return elem;

        foreach (var child in elem.Children)
        {
            var result = FindElementByType<T>(child);
            if (result != null)
                return result;
        }
        return null;
    }

    private static StrideUIElement? FindElementInStrideTree(StrideUIElement root, Func<StrideUIElement, bool> predicate)
    {
        if (predicate(root))
            return root;

        if (root is Panel panel)
        {
            foreach (var child in panel.Children)
            {
                var result = FindElementInStrideTree(child, predicate);
                if (result != null)
                    return result;
            }
        }

        return null;
    }

    private static void PrintStrideWidgetTree(StrideUIElement element, int depth = 0)
    {
        var indent = new string(' ', depth * 2);
        var elementType = element.GetType().Name;
        var name = !string.IsNullOrEmpty(element.Name) ? $" Name='{element.Name}'" : "";

        Console.WriteLine($"{indent}{elementType}{name} | " +
            $"Width={element.Width}, Height={element.Height}, " +
            $"ActualWidth={element.ActualWidth:F2}, ActualHeight={element.ActualHeight:F2}, " +
            $"Margin=({element.Margin.Left:F2},{element.Margin.Top:F2},{element.Margin.Right:F2},{element.Margin.Bottom:F2})");

        depth++;

        if (element is Panel panel)
        {
            foreach (var child in panel.Children)
            {
                PrintStrideWidgetTree(child, depth);
            }
        }

        if (element is ContentControl cc)
        {
            PrintStrideWidgetTree(cc.Content, depth);
        }
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

        var (suimRoot, _) = Parse.MarkupParser.Parse(markup, model: null, basePath: rootPath);

        var buttons = FindSuimButtons(suimRoot);
        Assert.NotEmpty(buttons);

        // Check vstack container
        var containerVStack = FindElementByClass(suimRoot, "container");
        Assert.NotNull(containerVStack);
        System.Diagnostics.Debug.WriteLine($"Before layout: Container.Width={containerVStack.Width ?? "null"}, Container.Height={containerVStack.Height ?? "null"}");

        // Check root grid
        Assert.NotNull(suimRoot);
        System.Diagnostics.Debug.WriteLine($"Before layout: Root.Width={suimRoot.Width ?? "null"}, Root.Height={suimRoot.Height ?? "null"}");

        foreach (var btn in buttons)
        {
            System.Diagnostics.Debug.WriteLine($"Before layout: Button.Width={btn.Width ?? "null"}, Button.Height={btn.Height ?? "null"}");
            // CSS should have been applied during parsing
            Assert.NotNull(btn.Width);
            Assert.NotNull(btn.Height);
        }

        // Now layout
        LayoutEngine.Layout(suimRoot, 16, 1280, 720);

        foreach (var btn in buttons)
        {
            System.Diagnostics.Debug.WriteLine($"After layout: Button.ActualWidth={btn.ActualWidth}, Button.ActualHeight={btn.ActualHeight}");
        }

        // Check container dimensions after layout
        System.Diagnostics.Debug.WriteLine($"After layout: Container.ActualWidth={containerVStack.ActualWidth}, Container.ActualHeight={containerVStack.ActualHeight}");
        Assert.True(containerVStack.ActualWidth > 0, "Container should have positive actual width after layout");
        Assert.True(containerVStack.ActualHeight > 0, "Container should have positive actual height after layout");

        // Check root grid dimensions after layout
        System.Diagnostics.Debug.WriteLine($"After layout: Root.ActualWidth={suimRoot.ActualWidth}, Root.ActualHeight={suimRoot.ActualHeight}");
        Assert.True(suimRoot.ActualWidth > 0, "Root grid should have positive actual width after layout");
        Assert.True(suimRoot.ActualHeight > 0, "Root grid should have positive actual height after layout");
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
            var text = button.Content as TextBlock;
            Assert.NotNull(text);
            Assert.True(200 > text.Width);
            Assert.Equal(16, text.Height);
            Assert.Equal(0, text.Margin.Left);
            Assert.Equal(0, text.Margin.Top);
            Assert.Equal(0, text.Margin.Right);
            Assert.Equal(0, text.Margin.Bottom);
        }

        // Check vstack container (converted to Stride StackPanel)
        var containerPanel = FindElementInStrideTree(strideRoot, element =>
        {
            if (element is StackPanel panel && panel.Name == "container")
                return true;
            return false;
        });

        if (containerPanel is StackPanel containerStackPanel)
        {
            Assert.True(containerStackPanel.Width > 0 || containerStackPanel.ActualWidth > 0,
                "Container should have width set or actual width > 0");
            Assert.True(containerStackPanel.Height > 0 || containerStackPanel.ActualHeight > 0,
                "Container should have height set or actual height > 0");
        }

        // Check root grid
        Assert.True(strideRoot.Width > 0 || strideRoot.ActualWidth > 0,
            "Root grid should have width set or actual width > 0");
        Assert.True(strideRoot.Height > 0 || strideRoot.ActualHeight > 0,
            "Root grid should have height set or actual height > 0");
        Console.WriteLine($"Root Grid: Width={strideRoot.Width}, Height={strideRoot.Height}, " +
            $"ActualWidth={strideRoot.ActualWidth}, ActualHeight={strideRoot.ActualHeight}");

        // Print the entire Stride widget tree with dimensions
        Console.WriteLine("\n=== Stride Widget Tree (After Conversion) ===");
        Console.WriteLine("NOTE: Check if ActualX and ActualY from SUIM are being mapped to Stride widgets");
        PrintStrideWidgetTree(strideRoot);
    }

    [Fact]
    public void Popup_VisibilityToggle_DoesNotProduceNaN()
    {
        var rootPath = "..\\..\\..\\..\\..\\src\\Example\\Chess3d\\SUIM";
        var project = new SUIMProject(rootPath);
        var project_views_path = Path.Combine(rootPath, "Views", "MainView.suim");

        Assert.True(File.Exists(project_views_path), $"MainView.suim not found at {project_views_path}");

        var markup = File.ReadAllText(project_views_path);
        project.ResolveDependencies(markup);

        var (suimRoot, _) = Parse.MarkupParser.Parse(markup, model: null, basePath: rootPath);

        // First layout with generic values
        LayoutEngine.Layout(suimRoot, 16, 1280, 720);

        // Find the Popup (it's a component, probably resolved to something else, or we find it by searching child elements)
        // In the parsed tree, it should have the class or id that matches the popup, let's just find the first thing with visibility="collapsed"
        var popup = suimRoot.Children.FirstOrDefault(c => string.Equals(c.Visibility, "collapsed", StringComparison.OrdinalIgnoreCase)) 
                    ?? suimRoot.Children.LastOrDefault(); // Assuming it's at the end if not collapsed by default in parsed tree
        
        Assert.NotNull(popup);
        
        // Change visibility to Visible
        popup.Visibility = "visible";
        
        // Set some dummy values
        if (popup is SUIMElement popupElement)
        {
            // Try to set title and message if those IDs exist, or just layout again
        }

        LayoutEngine.Layout(suimRoot, 16, 1280, 720);

        // Verify no NaNs in the tree
        VerifyNoNaNs(suimRoot);
    }

    private void VerifyNoNaNs(SUIMElement element)
    {
        Assert.False(float.IsNaN(element.ActualWidth), $"{element.GetType().Name} ActualWidth is NaN");
        Assert.False(float.IsNaN(element.ActualHeight), $"{element.GetType().Name} ActualHeight is NaN");
        Assert.False(float.IsNaN(element.ActualX), $"{element.GetType().Name} ActualX is NaN");
        Assert.False(float.IsNaN(element.ActualY), $"{element.GetType().Name} ActualY is NaN");
        Assert.False(float.IsNaN(element.ComputedMarginTop), $"{element.GetType().Name} ComputedMarginTop is NaN");
        Assert.False(float.IsNaN(element.ComputedMarginBottom), $"{element.GetType().Name} ComputedMarginBottom is NaN");
        Assert.False(float.IsNaN(element.ComputedMarginLeft), $"{element.GetType().Name} ComputedMarginLeft is NaN");
        Assert.False(float.IsNaN(element.ComputedMarginRight), $"{element.GetType().Name} ComputedMarginRight is NaN");

        foreach (var child in element.Children)
        {
            VerifyNoNaNs(child);
        }
    }
}
