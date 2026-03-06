namespace Chess3d.SUIM;

using System;
using System.Threading.Tasks;
using Stride.Engine;
using Stride.UI;
using SUIMStride;

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
                OpenPopup = new Action<string, string>(OpenPopupHandler),
                YesHandler = new Action(YesHandlerAction),
                NoHandler = new Action(UnshowPopup),
            };

        var parser = new Parser { RootPath = "SUIM" };
        var (strideRoot, modelResult) = parser.GetView("MainView", game, model: model);
        RootElement = strideRoot ?? throw new Exception("Failed to load MainView view.");
        Model = modelResult ?? throw new Exception("Failed to map model.");
        component.Page = new() { RootElement = RootElement };
    }

    private void YesHandlerAction()
    {
        switch (Model.PopupTitle)
        {
            case "Quit":
                Game.Exit();
                break;
            case "Restart":
                RestartHandler();
                break;
            case "Load":
                LoadHandler();
                break;
            case "Save":
                SaveHandler();
                break;
        }
    }

    private void RestartHandler()
    {
        if (Model.IsWorkInProgress) return;

        Model.IsWorkInProgress = true;
        ShowBlocker("Working...");
        BoardManager.GetInstance().InitBoard();
        UnshowBlocker();
        UnshowPopup();
        Model.IsWorkInProgress = false;
    }
    
    private void LoadHandler()
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
    
    private void SaveHandler()
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

    private void OpenPopupHandler(string title, string message)
    {
        Model.PopupTitle = title;
        Model.PopupMessage = message;
        Model.PopupVisibility = "visible";
    }

    private void UnshowPopup()
    {
        Model.PopupVisibility = "collapsed";
    }

    private void ShowBlocker(string message)
    {
        Model.OverlayMessage = message;
        Model.OverlayVisibility = "visible";
    }

    private void UnshowBlocker()
    {
        Model.OverlayVisibility = "collapsed";
    }
}
