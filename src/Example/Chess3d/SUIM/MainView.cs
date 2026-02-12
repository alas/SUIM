namespace Chess3d;

using System;
using System.Threading.Tasks;
using Stride.Core.Mathematics;
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
    private readonly UIElement rootUI;
    private bool IsWorkInProgress;

    public MainView(Game game, UIComponent component)
    {
        //var adapterOutput = game.GraphicsDevice.Adapter.Outputs[0];
        //var currentMonitorResolution = adapterOutput.CurrentDisplayMode;
        //game.Window.PreferredWindowedSize = new Int2(currentMonitorResolution.Width, currentMonitorResolution.Height);
        //game.Window.FullscreenIsBorderlessWindow = true;
        //game.Window.IsFullscreen = true;

        IsWorkInProgress = false;
        Font = game.Content.Load<SpriteFont>("StrideDefaultFont");

        var markup = @"<grid halign=""left"">
<style>
* {
        Font: StrideDefaultFont;
        Color: Green;
        FontSize: 16;
        HorizontalAlignment: Center;
        VerticalAlignment: Center;
}
.mybutton {
        Background: Black;
        Color: Green;
        Width: 200;
        Height: 50;
        Margin: 5;
}
</style>
    <stack orientation=""vertical"" margin=""6"" halign=""left"">
        <button class=""mybutton"" id=""quitButton"">Quit</button>
        <button class=""mybutton"" id=""restartButton"">Restart</button>
        <button class=""mybutton"" id=""loadButton"">Load</button>
        <button class=""mybutton"" id=""saveButton"">Save</button>
    </stack>
</grid>";
        var mapper = new SUIMStride
        {
            ContentManager = game.Content
        };
        rootUI = mapper.Parse(markup);
        Page = new UIPage
        {
            RootElement = rootUI
        };
        component.Page = Page;

        var quitButton = FindStrideElementByName(Page.RootElement, "quitButton") as Button;
        var restartButton = FindStrideElementByName(Page.RootElement, "restartButton") as Button;
        var loadButton = FindStrideElementByName(Page.RootElement, "loadButton") as Button;
        var saveButton = FindStrideElementByName(Page.RootElement, "saveButton") as Button;
        quitButton!.Click += (s, e) => OpenModal("Are you sure you want to quit?", QuitHandler);
        restartButton!.Click += (s, e) => OpenModal("Are you sure you want to start over?", RestartHandler);
        loadButton!.Click += (s, e) => OpenModal("Load() Not implemented yet!", LoadHandler);
        saveButton!.Click += (s, e) => OpenModal("Save() Not implemented yet!", SaveHandler);
        
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
            Page.RootElement = rootUI;
            IsWorkInProgress = false;
            ShowUI();
        }
        
        void LoadHandler(object? sender, EventArgs args)
        {
            if (IsWorkInProgress) return;
        
            IsWorkInProgress = true;
            ShowBlocker("Not implemented yet!");
            //BoardManager.GetInstance().LoadFromFile(GetFileName());
            Task.Delay(5000).ContinueWith(t =>
            {
                Page.RootElement = rootUI;
                IsWorkInProgress = false;
                ShowUI();
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
                Page.RootElement = rootUI;
                IsWorkInProgress = false;
                ShowUI();
            });
        }
    }

    private void ShowUI()
    {
        if (IsWorkInProgress == true)
        {
            return;
        }

        Page.RootElement = rootUI;
    }

    private void OpenModal(string message, EventHandler<Stride.UI.Events.RoutedEventArgs> yesClickHandler)
    {
        var modalMessage = GetText(string.Empty);
        var yesButton = GetButton("Yes");
        var noButton = GetButton("No");

        var modalContent = new StackPanel
        {
            Margin = new Thickness(6, 6, 6, 6),
            Orientation = Orientation.Vertical,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };
        modalContent.Children.Add(modalMessage);

        var modalButtons = new StackPanel
        {
            Margin = new Thickness(6, 6, 6, 6),
            Orientation = Orientation.Horizontal,
        };
        modalButtons.Children.Add(yesButton);
        modalButtons.Children.Add(noButton);
        modalContent.Children.Add(modalButtons);
        var header = new StackPanel
        {
            Margin = new Thickness(6, 6, 6, 6),
            Orientation = Orientation.Horizontal,
        };
        var titleLabel = GetText(string.Empty);
        header.Children.Add(titleLabel);
        var windowContent = new StackPanel
        {
            Orientation = Orientation.Vertical,
        };
        windowContent.Children.Add(header);
        windowContent.Children.Add(modalContent);
        var modalWindow = new ModalElement
        {
            Width = 360,
            Height = 180,
            IsModal = true,
            Content = windowContent
        };

        modalMessage.Text = message;
        Page.RootElement = modalWindow;
        yesButton.Click += yesClickHandler;
        noButton.Click += (_, _) => ShowUI();
    }

    private void ShowBlocker(string message)
    {
        var blockMessage = GetText(message);
        var blocker = new Canvas
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            CanBeHitByUser = true,
            Width = 1280,
            Height = 720,
            BackgroundColor = new Color(0, 0, 0, 0.6f),
        };
        blocker.Children.Add(blockMessage);
        Page.RootElement = blocker;
    }

    private TextBlock GetText(string text) => new()
    {
        Text = text,
        Font = Font,
        TextColor = Color.Green,
        TextSize = 16,
        HorizontalAlignment = HorizontalAlignment.Center,
        VerticalAlignment = VerticalAlignment.Center,
    };

    private Button GetButton(string text) => new()
    {
        Content = GetText(text),
        BackgroundColor = Color.Black,
        Color = Color.Green,
        Width = 200,
        Height = 50,
        Margin = new Thickness(5, 5, 5, 5),
    };

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
