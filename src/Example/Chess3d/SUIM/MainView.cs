namespace Chess3d;

using System;
using System.Threading.Tasks;
using Stride.Engine;
using Stride.Graphics;
using Stride.UI;
using Stride.UI.Controls;
using Stride.UI.Panels;
using SUIM.StrideIntegration;

public class MainView
{
    private readonly SpriteFont Font;
    private readonly UIPage Page;
    private bool IsWorkInProgress;
    private readonly dynamic Model;
    private readonly UIElement MainUI;
    private const string MainUIMarkup = @"<grid>
<model>
{
    ""blockerMessage"": """",
    ""popupTitle"": """",
    ""popupMessage"": """"
}
</model>
<style>
* {
    HorizontalAlignment: Left;
    VerticalAlignment: Top;
}
text {
    Color: Green;
    FontSize: 16;
    HorizontalAlignment: Center;
    VerticalAlignment: Center;
}
.mybutton {
    Background: Black;
    Width: 200;
    Height: 50;
    Margin: 5;
}
.overlay {
    visibility: collapse;
}
.container {
    HorizontalAlignment: Center;
    VerticalAlignment: Center;
    Background: rgba(0, 0, 0, 0.5);
    Padding: 10;
    Border: 5;
}
</style>
    <vstack id=""buttonsUI"" class=""container"" halign=""left"" valign=""top"">
        <button class=""mybutton"" id=""quitButton"">Quit</button>
        <button class=""mybutton"" id=""restartButton"">Restart</button>
        <button class=""mybutton"" id=""loadButton"">Load</button>
        <button class=""mybutton"" id=""saveButton"">Save</button>
    </vstack>

    <overlay id=""popup"" class=""overlay"">
        <grid width=""360"" height=""180"" halign=""center"" valign=""center"">
            <vstack>
                <hstack>
                    <label value=""@popupTitle"" />
                </hstack>
                <vstack margin=""6"">
                    <label value=""@popupMessage"" />
                </vstack>
                <hstack>
                    <button id=""yesButton"" class=""mybutton"">YES</button>
                    <button id=""noButton"" class=""mybutton"">NO</button>
                </hstack>
            </vstack>
        </grid>
    </overlay>

    <overlay id=""screenOverlay"" class=""overlay"">
        <grid halign=""center"" valign=""center"">
            <label value=""@blockerMessage"" />
        </grid>
    </overlay>
</grid>";
    private readonly List<EventHandler<Stride.UI.Events.RoutedEventArgs>> popupYesHandlers;

    public MainView(Game game, UIComponent component)
    {
        IsWorkInProgress = false;
        Font = game.Content.Load<SpriteFont>("StrideDefaultFont");

        var mapper = new SUIMStride
        {
            ContentManager = game.Content
        };
        (MainUI, Model) = mapper.Parse(MainUIMarkup, game);
        Page = new UIPage
        {
            RootElement = MainUI
        };
        component.Page = Page;

        var quitButton = FindStrideElementByName(Page.RootElement, "quitButton") as Button;
        var restartButton = FindStrideElementByName(Page.RootElement, "restartButton") as Button;
        var loadButton = FindStrideElementByName(Page.RootElement, "loadButton") as Button;
        var saveButton = FindStrideElementByName(Page.RootElement, "saveButton") as Button;
        quitButton!.Click += (s, e) => OpenPopup("Quit", "Are you sure you want to quit?", QuitHandler);
        restartButton!.Click += (s, e) => OpenPopup("Restart", "Are you sure you want to start over?", RestartHandler);
        loadButton!.Click += (s, e) => OpenPopup("Load", "Load() Not implemented yet!", LoadHandler);
        saveButton!.Click += (s, e) => OpenPopup("Save", "Save() Not implemented yet!", SaveHandler);
        
        void QuitHandler(object? sender, EventArgs args)
        {
            game.Exit();
        }
        
        void RestartHandler(object? sender, EventArgs args)
        {
            if (IsWorkInProgress) return;
        
            IsWorkInProgress = true;
            ShowBlocker("Working...");
            BoardManager.GetInstance().InitBoard();
            UnshowBlocker();
            UnshowPopup();
            IsWorkInProgress = false;
        }
        
        void LoadHandler(object? sender, EventArgs args)
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
        
        void SaveHandler(object? sender, EventArgs args)
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
        popupYesHandlers =
        [
            QuitHandler,
            RestartHandler,
            LoadHandler,
            SaveHandler
        ];
    }

    private void OpenPopup(string title, string message, EventHandler<Stride.UI.Events.RoutedEventArgs> yesClickHandler)
    {
        Model.popupTitle = title;
        Model.popupMessage = message;
        var popup = FindStrideElementByName(MainUI, "popup");
        popup!.Visibility = Visibility.Visible;
        var yesButton = FindStrideElementByName(MainUI, "yesButton") as Button;
        yesButton!.Click += yesClickHandler;
        var noButton = FindStrideElementByName(MainUI, "noButton") as Button;
        noButton!.Click += (_, _) => UnshowPopup();
    }

    private void UnshowPopup()
    {
        var blocker = FindStrideElementByName(MainUI, "popup");
        blocker!.Visibility = Visibility.Collapsed;
        var yesButton = FindStrideElementByName(MainUI, "yesButton") as Button;
        popupYesHandlers.ForEach(x => yesButton!.Click -= x);
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

        // Panels with Children
        if (root is Panel panel)
        {
            foreach (var child in panel.Children)
            {
                var found = FindStrideElementByName(child, name);
                if (found != null) return found;
            }
        }

        // Grid (inherits Panel) already handled above

        // Content Controls
        if (root is ContentControl cc && cc.Content is UIElement contentElem)
        {
            var found = FindStrideElementByName(contentElem, name);
            if (found != null) return found;
        }

        // Border has Content
        if (root is Border br && br.Content is UIElement borderContent)
        {
            var found = FindStrideElementByName(borderContent, name);
            if (found != null) return found;
        }

        return null;
    }
}
