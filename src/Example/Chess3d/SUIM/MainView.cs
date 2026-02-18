namespace Chess3d;

using System;
using System.Threading.Tasks;
using Stride.Engine;
using Stride.UI;
using Stride.UI.Controls;
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
                QuitHandler = new Action(ShowQuitPopup),
                RestartHandler = new Action(ShowRestartPopup),
                LoadHandler = new Action(ShowLoadPopup),
                SaveHandler = new Action(ShowSavePopup),
                NoHandler = new Action(() => UnshowPopup()),
            };

        var mapper = new SUIMStride
        {
            RootPath = "SUIM"
        };
        var (rootElement, modelResult) = mapper.GetView("MainView", game, model: model);
        RootElement = rootElement ?? throw new Exception("Failed to load MainView view.");
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
    private void ShowQuitPopup() => OpenPopup("Quit", "Are you sure you want to quit?", QuitHandler);
    private void ShowRestartPopup() => OpenPopup("Restart", "Are you sure you want to start over?", RestartHandler);
    private void ShowLoadPopup() => OpenPopup("Load", "Load() Not implemented yet!", LoadHandler);
    private void ShowSavePopup() => OpenPopup("Save", "Save() Not implemented yet!", SaveHandler);

    private void OpenPopup(string title, string message, EventHandler<Stride.UI.Events.RoutedEventArgs> yesClickHandler)
    {
        Model.PopupTitle = title;
        Model.PopupMessage = message;
        var popup = XPath.Find(RootElement, "popup");
        popup!.Visibility = Visibility.Visible;
        var yesButton = XPath.Find(RootElement, "yesButton") as Button;
        yesButton!.Click += yesClickHandler;
    }

    private void UnshowPopup()
    {
        var blocker = XPath.Find(RootElement, "popup") as UIElement;
        blocker!.Visibility = Visibility.Collapsed;
        var yesButton = XPath.Find(RootElement, "yesButton") as Button;
        yesButton!.Click -= QuitHandler;
        yesButton!.Click -= RestartHandler;
        yesButton!.Click -= LoadHandler;
        yesButton!.Click -= SaveHandler;
    }

    private void ShowBlocker(string message)
    {
        Model.BlockerMessage = message;
        var blocker = XPath.Find(RootElement, "screenOverlay") as UIElement;
        blocker!.Visibility = Visibility.Visible;
    }

    private void UnshowBlocker()
    {
        var blocker = XPath.Find(RootElement, "screenOverlay") as UIElement;
        blocker!.Visibility = Visibility.Collapsed;
    }
}
