namespace Chess3d.SUIM;

using System;
using System.Threading.Tasks;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Graphics;
using Stride.UI;
using Stride.UI.Controls;
using Stride.UI.Panels;

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

        var font = game.Content.Load<SpriteFont>("StrideDefaultFont");

        // Main buttons
        var quitButton = GetButton("Quit", font);
        var restartButton = GetButton("Restart", font);
        var loadButton = GetButton("Load", font);
        var saveButton = GetButton("Save", font);

        root.Children.Add(quitButton);
        root.Children.Add(restartButton);
        root.Children.Add(loadButton);
        root.Children.Add(saveButton);        
        
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
            ShowBlocker("Working...");
            BoardManager.GetInstance().InitBoard();
            page.RootElement = rootPanel;
            IsWorkInProgress = false;
            CloseModal();
        }
        
        void LoadHandler(object? sender, EventArgs args)
        {
            if (IsWorkInProgress) return;
        
            IsWorkInProgress = true;
            ShowBlocker("Not implemented yet!");
            //BoardManager.GetInstance().LoadFromFile(GetFileName());
            Task.Delay(5000).ContinueWith(t =>
            {
                page.RootElement = rootPanel;
                IsWorkInProgress = false;
                CloseModal();
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
                page.RootElement = rootPanel;
                IsWorkInProgress = false;
                CloseModal();
            });
        }
        
        void OpenModal(string message, EventHandler<Stride.UI.Events.RoutedEventArgs> clickHandler)
        {
            var modalMessage = GetText(string.Empty, font);
            var yesButton = GetButton("Yes", font);
            var noButton = GetButton("No", font);

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
            var titleLabel = GetText(string.Empty, font);
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
            page.RootElement = modalWindow;
            yesButton.Click += clickHandler;
            noButton.Click += (_, _) => CloseModal();
        }

        void ShowBlocker(string message)
        {
            var blockMessage = GetText(message, font);
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
            page.RootElement = blocker;
        }

        void CloseModal()
        {
            if (IsWorkInProgress == true)
            {
                return;
            }

            page.RootElement = rootPanel;
        }
    }

    private static TextBlock GetText(string text, SpriteFont font) => new()
    {
        Text = text,
        Font = font,
        TextColor = Color.Green,
        TextSize = 16,
        HorizontalAlignment = HorizontalAlignment.Center,
        VerticalAlignment = VerticalAlignment.Center,
    };

    private static Button GetButton(string text, SpriteFont font) => new()
    {
        Content = GetText(text, font),
        BackgroundColor = Color.Black,
        Color = Color.Green,
        Width = 200,
        Height = 50,
        Margin = new Thickness(5, 5, 5, 5),
    };
}
