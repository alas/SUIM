/*namespace Chess3d.SUIM;

using System;
using System.Threading.Tasks;

public class MainView
{
    private bool IsWorkInProgress;

    public MainView()
    {
        // Build UI
        var root = new VerticalStackPanel { Spacing = 6 };

        // Main buttons
        var quitButton = new Button { Content = new Label { Text = "Quit" } };
        var restartButton = new Button { Content = new Label { Text = "Restart" } };
        var loadButton = new Button { Content = new Label { Text = "Load" } };
        var saveButton = new Button { Content = new Label { Text = "Save" } };

        root.Widgets.Add(quitButton);
        root.Widgets.Add(restartButton);
        root.Widgets.Add(loadButton);
        root.Widgets.Add(saveButton);

        var modalMessage = new Label { Text = string.Empty };
        var yesButton = new Button { Content = new Label { Text = "Yes" } };
        var noButton = new Button { Content = new Label { Text = "No" } };

        var modalContent = new VerticalStackPanel
        {
            Spacing = 6,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };
        modalContent.Widgets.Add(modalMessage);

        var modalButtons = new HorizontalStackPanel { Spacing = 6 };
        modalButtons.Widgets.Add(yesButton);
        modalButtons.Widgets.Add(noButton);
        modalContent.Widgets.Add(modalButtons);

        var _backgroundBlocker = new Panel
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };
        // Place modal content inside a movable Window. Create a custom header with an X button.
        var modalWindow = new Window
        {
            Title = "",
            Width = 360,
            Height = 180,
        };
        // Ensure CloseModal is called when the window is closed (e.g. via the window X)
        modalWindow.Closing += (s, e) =>
        {
            if (IsWorkInProgress == true)
            {
                e.Cancel = true;
                return;
            }

            Desktop.Widgets.Remove(_backgroundBlocker);
            yesButton.Click -= QuitHandler;
            yesButton.Click -= RestartHandler;
            yesButton.Click -= LoadHandler;
            yesButton.Click -= SaveHandler;
        };
        modalWindow.Closed += (_, _) => CloseModal();
        noButton.Click += (_, _) => CloseModal();

        var windowContent = new VerticalStackPanel { Spacing = 0 };

        var header = new HorizontalStackPanel { Spacing = 6 };
        var titleLabel = new Label { Text = "" };
        header.Widgets.Add(titleLabel);

        windowContent.Widgets.Add(header);
        windowContent.Widgets.Add(modalContent);

        modalWindow.Content = windowContent;

        var rootPanel = new Grid();
        rootPanel.Widgets.Add(root);

        // Set root
        Widgets.Add(rootPanel);

        quitButton.Click += (s, e) => OpenModal("Are you sure you want to quit?", QuitHandler);
        restartButton.Click += (s, e) => OpenModal("Are you sure you want to start over?", RestartHandler);
        loadButton.Click += (s, e) => OpenModal("Load() Not implemented yet!", LoadHandler);
        saveButton.Click += (s, e) => OpenModal("Save() Not implemented yet!", SaveHandler);

        void QuitHandler(object sender, EventArgs args)
        {
            if (MyraEnvironment.Game is Stride.Engine.Game g)
            {
                g.Exit();
            }
        }

        void RestartHandler(object sender, EventArgs args)
        {
            if (IsWorkInProgress) return;

            IsWorkInProgress = true;
            BoardManager.GetInstance().InitBoard();
            IsWorkInProgress = false;
            CloseModal();
        }

        void LoadHandler(object sender, EventArgs args)
        {
            if (IsWorkInProgress) return;

            IsWorkInProgress = true;
            //BoardManager.GetInstance().LoadFromFile(GetFileName());
            modalMessage.Text = "Not implemented yet!";
            Task.Delay(5000).ContinueWith(t =>
            {
                IsWorkInProgress = false;
                CloseModal();
            });
        }

        void SaveHandler(object sender, EventArgs args)
        {
            if (IsWorkInProgress) return;

            IsWorkInProgress = true;
            //BoardManager.GetInstance().SaveBoardStateToFile(GetFileName());
            modalMessage.Text = "Not implemented yet!";
            Task.Delay(1000).ContinueWith(t =>
            {
                IsWorkInProgress = false;
                CloseModal();
            });
        }

        void OpenModal(string message, EventHandler clickHandler)
        {
            modalMessage.Text = message;
            Desktop.Widgets.Add(_backgroundBlocker);
            modalWindow.ShowModal(Desktop);
            yesButton.Click += clickHandler;
        }

        void CloseModal() => modalWindow.Close();
    }
}*/
