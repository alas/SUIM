namespace Chess3d.SUIM;

using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Graphics;
using Stride.UI;
using Stride.UI.Controls;
using Stride.UI.Panels;
using System;
using System.Threading.Tasks;

public static class MainView
{
    public static void GetMainView(Game game, UIComponent component)
    {
        var IsWorkInProgress = false;
        // Build UI
        var root = new StackPanel { Margin = new Thickness(6, 6, 6, 6), Orientation = Orientation.Vertical, HorizontalAlignment = HorizontalAlignment.Left };
        var rootPanel = new Grid();
        rootPanel.Children.Add(root);
        var page = new UIPage
        {
            RootElement = rootPanel
        };
        component.Page = page;

        // Main buttons
        var quitButton    = new Button { Content = new TextBlock { Text = "Quit"   , TextColor = Color.Green }, BackgroundColor = Color.Black, Color = Color.Green, Width = 200, Height = 50, Margin = new Thickness(5, 5, 5, 5) };
        var restartButton = new Button { Content = new TextBlock { Text = "Restart", TextColor = Color.Green }, BackgroundColor = Color.Black, Color = Color.Green, Width = 200, Height = 50, Margin = new Thickness(5, 5, 5, 5) };
        var loadButton    = new Button { Content = new TextBlock { Text = "Load"   , TextColor = Color.Green }, BackgroundColor = Color.Black, Color = Color.Green, Width = 200, Height = 50, Margin = new Thickness(5, 5, 5, 5) };
        var saveButton    = new Button { Content = new TextBlock { Text = "Save"   , TextColor = Color.Green }, BackgroundColor = Color.Black, Color = Color.Green, Width = 200, Height = 50, Margin = new Thickness(5, 5, 5, 5) };

        root.Children.Add(quitButton);
        root.Children.Add(restartButton);
        root.Children.Add(loadButton);
        root.Children.Add(saveButton);

        var blockMessage = new TextBlock { Text = string.Empty };
        var modalMessage = new TextBlock { Text = string.Empty };
        var yesButton = new Button { Content = new TextBlock { Text = "Yes" } };
        var noButton = new Button { Content = new TextBlock { Text = "No" } };

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
        
        var blocker = new Canvas
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            BackgroundColor = new Stride.Core.Mathematics.Color(0, 0, 0, 0.6f),
        };
        blocker.Children.Add(blockMessage);

        // Place modal content inside a movable Window. Create a custom header with an X button.
        var modalWindow = new ModalElement
        {
            Width = 360,
            Height = 180,
            IsModal = true,
        };
        // Ensure CloseModal is called when the window is closed (e.g. via the window X)
        noButton.Click += (_, _) => CloseModal();
        
        var windowContent = new StackPanel
        {
            Orientation = Orientation.Vertical,
        };
        
        var header = new StackPanel
        {
            Margin = new Thickness(6, 6, 6, 6),
            Orientation = Orientation.Horizontal,
        };
        var titleLabel = new TextBlock { Text = "" };
        header.Children.Add(titleLabel);
        
        windowContent.Children.Add(header);
        windowContent.Children.Add(modalContent);
        
        modalWindow.Content = windowContent;
        
        quitButton.Click += (s, e) => OpenModal("Are you sure you want to quit?", QuitHandler);
        restartButton.Click += (s, e) => OpenModal("Are you sure you want to start over?", RestartHandler);
        loadButton.Click += (s, e) => OpenModal("Load() Not implemented yet!", LoadHandler);
        saveButton.Click += (s, e) => OpenModal("Save() Not implemented yet!", SaveHandler);
        
        void QuitHandler(object? sender, EventArgs args)
        {
            game.Exit();
        }
        
        void RestartHandler(object? sender, EventArgs args)
        {
            if (IsWorkInProgress) return;
        
            IsWorkInProgress = true;
            page.RootElement = blocker;
            BoardManager.GetInstance().InitBoard();
            page.RootElement = root;
            IsWorkInProgress = false;
            CloseModal();
        }
        
        void LoadHandler(object? sender, EventArgs args)
        {
            if (IsWorkInProgress) return;
        
            IsWorkInProgress = true;
            page.RootElement = blocker;
            //BoardManager.GetInstance().LoadFromFile(GetFileName());
            blockMessage.Text = "Not implemented yet!";
            Task.Delay(5000).ContinueWith(t =>
            {
                page.RootElement = root;
                IsWorkInProgress = false;
                CloseModal();
            });
        }
        
        void SaveHandler(object? sender, EventArgs args)
        {
            if (IsWorkInProgress) return;
        
            IsWorkInProgress = true;
            page.RootElement = blocker;
            //BoardManager.GetInstance().SaveBoardStateToFile(GetFileName());
            blockMessage.Text = "Not implemented yet!";
            Task.Delay(1000).ContinueWith(t =>
            {
                page.RootElement = root;
                IsWorkInProgress = false;
                CloseModal();
            });
        }
        
        void OpenModal(string message, EventHandler<Stride.UI.Events.RoutedEventArgs> clickHandler)
        {
            modalMessage.Text = message;
            page.RootElement = modalWindow;
            yesButton.Click += clickHandler;
        }
        
        void CloseModal()
        {
            if (IsWorkInProgress == true)
            {
                return;
            }
        
            page.RootElement = root;
            yesButton.Click -= QuitHandler;
            yesButton.Click -= RestartHandler;
            yesButton.Click -= LoadHandler;
            yesButton.Click -= SaveHandler;
        }
    }
}
