namespace Chess3d.SUIM.Views;

using System.Threading.Tasks;
using Stride.Engine;
using SUIMComponent = global::SUIM.Parse.Components.UIComponent;

public class MainView() : SUIMComponent(nameof(MainView))
{
    public Game? Game { get; set; }
    private bool IsWorkInProgress = false;

    private void YesHandler()
    {
        switch (Model!.PopupTitle)
        {
            case "Quit":
                Game?.Exit();
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
        if (IsWorkInProgress) return;

        IsWorkInProgress = true;
        ShowBlocker("Working...");
        BoardManager.GetInstance().InitBoard();
        UnshowBlocker();
        ClosePopup();
        IsWorkInProgress = false;
    }
    
    private void LoadHandler()
    {
        if (IsWorkInProgress) return;

        IsWorkInProgress = true;
        ShowBlocker("Not implemented yet!");
        //BoardManager.GetInstance().LoadFromFile(GetFileName());
        Task.Delay(5000).ContinueWith(t =>
        {
            UnshowBlocker();
            ClosePopup();
            IsWorkInProgress = false;
        });
    }
    
    private void SaveHandler()
    {
        if (IsWorkInProgress) return;

        IsWorkInProgress = true;
        //BoardManager.GetInstance().SaveBoardStateToFile(GetFileName());
        ShowBlocker("Not implemented yet!");
        Task.Delay(1000).ContinueWith(t =>
        {
            UnshowBlocker();
            ClosePopup();
            IsWorkInProgress = false;
        });
    }

    private void OpenPopup(string title, string message)
    {
        Model!.PopupTitle = title;
        Model.PopupMessage = message;
        Model.PopupVisibility = "visible";
    }

    private void ClosePopup()
    {
        Model!.PopupVisibility = "collapsed";
    }

    private void ShowBlocker(string message)
    {
        Model!.OverlayMessage = message;
        Model.OverlayVisibility = "visible";
    }

    private void UnshowBlocker()
    {
        Model!.OverlayVisibility = "collapsed";
    }
}
