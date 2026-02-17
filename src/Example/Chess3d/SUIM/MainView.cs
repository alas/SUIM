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
    private readonly UIPage Page;
    private bool IsWorkInProgress;
    private readonly dynamic Model;
    private readonly UIElement MainUI;
    private readonly Game Game;

    public MainView(Game game, UIComponent component)
    {
        Game = game;
        IsWorkInProgress = false;

        var model =
            new
            {
                blockerMessage = "",
                popupTitle = "",
                popupMessage = "",
                QuitHandler = new Action(() => QuitHandler()),
                RestartHandler = new Action(() => RestartHandler()),
                LoadHandler = new Action(() => LoadHandler()),
                SaveHandler = new Action(() => SaveHandler()),
                NoHandler = new Action(() => NoHandler()),
            };

        SUIM.ComponentRegistry.Register("MyPopup", "SUIM/Components/Popup.suim");
        SUIM.ComponentRegistry.Register("MyScreenOverlay", "SUIM/Components/ScreenOverlay.suim");
        var markup = File.ReadAllText("SUIM/Views/MainUI.suim");
        var mapper = new SUIMStride();
        (MainUI, Model) = mapper.Parse(markup, game, model: model);
        Page = new UIPage
        {
            RootElement = MainUI
        };
        component.Page = Page;
    }

    private void QuitHandler(object? sender, EventArgs args)
    {
        Game.Exit();
    }
    
    private void RestartHandler(object? sender, EventArgs args)
    {
        if (IsWorkInProgress) return;
    
        IsWorkInProgress = true;
        ShowBlocker("Working...");
        BoardManager.GetInstance().InitBoard();
        UnshowBlocker();
        UnshowPopup();
        IsWorkInProgress = false;
    }
    
    private void LoadHandler(object? sender, EventArgs args)
    {
        if (IsWorkInProgress) return;
    
        IsWorkInProgress = true;
        ShowBlocker("Not implemented yet!");
        //BoardManager.GetInstance().LoadFromFile(GetFileName());
        Task.Delay(5000).ContinueWith(t =>
        {
            UnshowBlocker();
            UnshowPopup();
            IsWorkInProgress = false;
        });
    }
    
    private void SaveHandler(object? sender, EventArgs args)
    {
        if (IsWorkInProgress) return;
    
        IsWorkInProgress = true;
        //BoardManager.GetInstance().SaveBoardStateToFile(GetFileName());
        ShowBlocker("Not implemented yet!");
        Task.Delay(1000).ContinueWith(t =>
        {
            UnshowBlocker();
            UnshowPopup();
            IsWorkInProgress = false;
        });
    }

    // Methods bound from markup
    public void QuitHandler() => OpenPopup("Quit", "Are you sure you want to quit?", QuitHandler);
    public void RestartHandler() => OpenPopup("Restart", "Are you sure you want to start over?", RestartHandler);
    public void LoadHandler() => OpenPopup("Load", "Load() Not implemented yet!", LoadHandler);
    public void SaveHandler() => OpenPopup("Save", "Save() Not implemented yet!", SaveHandler);
    public void NoHandler() => UnshowPopup();

    private void OpenPopup(string title, string message, EventHandler<Stride.UI.Events.RoutedEventArgs> yesClickHandler)
    {
        Model.popupTitle = title;
        Model.popupMessage = message;
        var popup = FindStrideElementByName(MainUI, "popup");
        popup!.Visibility = Visibility.Visible;
        var yesButton = FindStrideElementByName(MainUI, "yesButton") as Button;
        yesButton!.Click += yesClickHandler;
    }

    private void UnshowPopup()
    {
        var blocker = FindStrideElementByName(MainUI, "popup");
        blocker!.Visibility = Visibility.Collapsed;
        var yesButton = FindStrideElementByName(MainUI, "yesButton") as Button;
        yesButton!.Click -= QuitHandler;
        yesButton!.Click -= RestartHandler;
        yesButton!.Click -= LoadHandler;
        yesButton!.Click -= SaveHandler;
    }

    private void ShowBlocker(string message)
    {
        Model.blockerMessage = message;
        var blocker = FindStrideElementByName(MainUI, "screenOverlay");
        blocker!.Visibility = Visibility.Visible;
    }

    private void UnshowBlocker()
    {
        var blocker = FindStrideElementByName(MainUI, "screenOverlay");
        blocker!.Visibility = Visibility.Collapsed;
    }

    private static UIElement? FindStrideElementByName(UIElement root, string name)
    {
        if (root == null) return null;
        if (string.Equals(root.Name, name, StringComparison.OrdinalIgnoreCase)) return root;

        if (root is Panel panel)
        {
            foreach (var child in panel.Children)
            {
                var found = FindStrideElementByName(child, name);
                if (found != null) return found;
            }
        }

        // Content Controls
        if (root is ContentControl cc && cc.Content is UIElement contentElem)
        {
            var found = FindStrideElementByName(contentElem, name);
            if (found != null) return found;
        }

        return null;
    }
}
