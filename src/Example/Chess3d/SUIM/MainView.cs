namespace Chess3d;

using System;
using System.Threading.Tasks;
using Stride.Engine;
using Stride.UI;
using Stride.UI.Controls;
using Stride.UI.Panels;
using SUIM.StrideIntegration;

public class MainView
{
    private readonly dynamic Model;
    private readonly UIElement RootElement;
    private readonly Game Game;

    public MainView(Game game, UIComponent component)
    {
        Game = game;

        var model =
            new
            {
                IsWorkInProgress = false,
                BlockerMessage = "",
                PopupTitle = "",
                PopupMessage = "",
                QuitHandler = new Action(() => QuitHandler()),
                RestartHandler = new Action(() => RestartHandler()),
                LoadHandler = new Action(() => LoadHandler()),
                SaveHandler = new Action(() => SaveHandler()),
                NoHandler = new Action(() => NoHandler()),
            };

        var mapper = new SUIMStride
        {
            RootPath = "SUIM"
        };
        var (rootElement, modelResult) = mapper.GetView("MainUI", game, model: model);
        RootElement = rootElement ?? throw new Exception("Failed to load MainUI view.");
        Model = modelResult ?? throw new Exception("Failed to map model.");
        component.Page = new()
        {
            RootElement = RootElement
        };
    }

    private void QuitHandler(object? sender, EventArgs args)
    {
        Game.Exit();
    }
    
    private void RestartHandler(object? sender, EventArgs args)
    {
        if (Model.IsWorkInProgress) return;

        Model.IsWorkInProgress = true;
        ShowBlocker("Working...");
        BoardManager.GetInstance().InitBoard();
        UnshowBlocker();
        UnshowPopup();
        Model.IsWorkInProgress = false;
    }
    
    private void LoadHandler(object? sender, EventArgs args)
    {
        if (Model.IsWorkInProgress) return;

        Model.IsWorkInProgress = true;
        ShowBlocker("Not implemented yet!");
        //BoardManager.GetInstance().LoadFromFile(GetFileName());
        Task.Delay(5000).ContinueWith(t =>
        {
            UnshowBlocker();
            UnshowPopup();
            Model.IsWorkInProgress = false;
        });
    }
    
    private void SaveHandler(object? sender, EventArgs args)
    {
        if (Model.IsWorkInProgress) return;

        Model.IsWorkInProgress = true;
        //BoardManager.GetInstance().SaveBoardStateToFile(GetFileName());
        ShowBlocker("Not implemented yet!");
        Task.Delay(1000).ContinueWith(t =>
        {
            UnshowBlocker();
            UnshowPopup();
            Model.IsWorkInProgress = false;
        });
    }

    // Methods bound from markup
    private void QuitHandler() => OpenPopup("Quit", "Are you sure you want to quit?", QuitHandler);
    private void RestartHandler() => OpenPopup("Restart", "Are you sure you want to start over?", RestartHandler);
    private void LoadHandler() => OpenPopup("Load", "Load() Not implemented yet!", LoadHandler);
    private void SaveHandler() => OpenPopup("Save", "Save() Not implemented yet!", SaveHandler);
    private void NoHandler() => UnshowPopup();

    private void OpenPopup(string title, string message, EventHandler<Stride.UI.Events.RoutedEventArgs> yesClickHandler)
    {
        Model.PopupTitle = title;
        Model.PopupMessage = message;
        var popup = FindStrideElementByName(RootElement, "popup");
        popup!.Visibility = Visibility.Visible;
        var yesButton = FindStrideElementByName(RootElement, "yesButton") as Button;
        yesButton!.Click += yesClickHandler;
    }

    private void UnshowPopup()
    {
        var blocker = FindStrideElementByName(RootElement, "popup");
        blocker!.Visibility = Visibility.Collapsed;
        var yesButton = FindStrideElementByName(RootElement, "yesButton") as Button;
        yesButton!.Click -= QuitHandler;
        yesButton!.Click -= RestartHandler;
        yesButton!.Click -= LoadHandler;
        yesButton!.Click -= SaveHandler;
    }

    private void ShowBlocker(string message)
    {
        Model.BlockerMessage = message;
        var blocker = FindStrideElementByName(RootElement, "screenOverlay");
        blocker!.Visibility = Visibility.Visible;
    }

    private void UnshowBlocker()
    {
        var blocker = FindStrideElementByName(RootElement, "screenOverlay");
        blocker!.Visibility = Visibility.Collapsed;
    }

    private static UIElement? FindStrideElementByName(UIElement element, string name)
    {
        if (element == null) return null;
        if (string.Equals(element.Name, name, StringComparison.OrdinalIgnoreCase)) return element;

        if (element is Panel panel)
        {
            foreach (var child in panel.Children)
            {
                var found = FindStrideElementByName(child, name);
                if (found != null) return found;
            }
        }

        // Content Controls
        if (element is ContentControl cc && cc.Content is UIElement contentElem)
        {
            var found = FindStrideElementByName(contentElem, name);
            if (found != null) return found;
        }

        return null;
    }
}
