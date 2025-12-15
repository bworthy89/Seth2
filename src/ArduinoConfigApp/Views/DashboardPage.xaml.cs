using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Dispatching;
using Windows.Storage.Pickers;
using Windows.Storage;
using ArduinoConfigApp.Models;
using ArduinoConfigApp.Services;
using WinRT.Interop;

namespace ArduinoConfigApp.Views;

public sealed partial class DashboardPage : Page
{
    private readonly DispatcherQueue _dispatcherQueue;

    public DashboardPage()
    {
        this.InitializeComponent();
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
    }

    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
        // Subscribe to board changes
        SerialPortService.Instance.BoardsChanged += OnBoardsChanged;
        SerialPortService.Instance.BoardConnected += OnBoardConnected;
        SerialPortService.Instance.BoardDisconnected += OnBoardDisconnected;

        // Subscribe to configuration changes
        ConfigurationService.Instance.ConfigurationChanged += OnConfigurationChanged;
        ConfigurationService.Instance.ConfigurationSaved += OnConfigurationSaved;

        // Start board monitoring
        SerialPortService.Instance.StartMonitoring();

        // Initial updates
        UpdateBoardsList(SerialPortService.Instance.DetectedBoards.ToList());
        UpdateConfigurationUI();
        UpdateRecentConfigurations();

        // Load auto-save setting
        AutoSaveToggle.IsOn = SettingsService.Instance.Settings.AutoSaveEnabled;
    }

    private void Page_Unloaded(object sender, RoutedEventArgs e)
    {
        // Unsubscribe from events
        SerialPortService.Instance.BoardsChanged -= OnBoardsChanged;
        SerialPortService.Instance.BoardConnected -= OnBoardConnected;
        SerialPortService.Instance.BoardDisconnected -= OnBoardDisconnected;
        ConfigurationService.Instance.ConfigurationChanged -= OnConfigurationChanged;
        ConfigurationService.Instance.ConfigurationSaved -= OnConfigurationSaved;
    }

    #region Board Detection

    private void OnBoardsChanged(object? sender, List<ArduinoBoard> boards)
    {
        _dispatcherQueue.TryEnqueue(() => UpdateBoardsList(boards));
    }

    private void OnBoardConnected(object? sender, ArduinoBoard board)
    {
        _dispatcherQueue.TryEnqueue(() => { });
    }

    private void OnBoardDisconnected(object? sender, ArduinoBoard board)
    {
        _dispatcherQueue.TryEnqueue(() => { });
    }

    private void UpdateBoardsList(List<ArduinoBoard> boards)
    {
        BoardsListView.ItemsSource = boards;

        if (boards.Count == 0)
        {
            NoBoardsText.Visibility = Visibility.Visible;
            BoardsListView.Visibility = Visibility.Collapsed;

            ConnectionStatusText.Text = "No boards detected";
            StatusIcon.Glyph = "\uE839";
            StatusIcon.Foreground = (Brush)Application.Current.Resources["SystemFillColorCautionBrush"];
            BoardTypeText.Text = "--";
            ComPortText.Text = "--";
        }
        else
        {
            NoBoardsText.Visibility = Visibility.Collapsed;
            BoardsListView.Visibility = Visibility.Visible;

            var firstBoard = boards.FirstOrDefault();
            if (firstBoard != null)
            {
                UpdateSelectedBoard(firstBoard);
                if (boards.Count == 1)
                {
                    BoardsListView.SelectedIndex = 0;
                }
            }

            ConnectionStatusText.Text = $"{boards.Count} board(s) detected";
            StatusIcon.Glyph = "\uE73E";
            StatusIcon.Foreground = (Brush)Application.Current.Resources["SystemFillColorSuccessBrush"];
        }
    }

    private void UpdateSelectedBoard(ArduinoBoard board)
    {
        BoardTypeText.Text = board.BoardTypeName;
        ComPortText.Text = board.PortName;
    }

    private void BoardsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (BoardsListView.SelectedItem is ArduinoBoard board)
        {
            UpdateSelectedBoard(board);
        }
    }

    private void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        ConnectionStatusText.Text = "Scanning for boards...";
        SerialPortService.Instance.RefreshPorts();
        UpdateBoardsList(SerialPortService.Instance.DetectedBoards.ToList());
    }

    #endregion

    #region Configuration Management

    private void OnConfigurationChanged(object? sender, EventArgs e)
    {
        _dispatcherQueue.TryEnqueue(UpdateConfigurationUI);
    }

    private void OnConfigurationSaved(object? sender, EventArgs e)
    {
        _dispatcherQueue.TryEnqueue(() =>
        {
            UpdateConfigurationUI();
            UpdateRecentConfigurations();
        });
    }

    private void UpdateConfigurationUI()
    {
        var config = ConfigurationService.Instance.CurrentConfiguration;
        var hasFile = !ConfigurationService.Instance.IsNewConfiguration;
        var hasChanges = ConfigurationService.Instance.HasUnsavedChanges;

        var name = config.Name;
        if (hasChanges) name += " *";
        if (hasFile)
        {
            var fileName = Path.GetFileName(ConfigurationService.Instance.CurrentFilePath);
            ConfigNameText.Text = $"{name} - {fileName}";
        }
        else
        {
            ConfigNameText.Text = name;
        }

        SaveConfigButton.IsEnabled = hasFile && hasChanges;
    }

    private void UpdateRecentConfigurations()
    {
        var recent = ConfigurationService.Instance.GetRecentConfigurations();

        if (recent.Count == 0)
        {
            RecentConfigsListView.Visibility = Visibility.Collapsed;
            NoRecentConfigsText.Visibility = Visibility.Visible;
        }
        else
        {
            RecentConfigsListView.Visibility = Visibility.Visible;
            NoRecentConfigsText.Visibility = Visibility.Collapsed;
            RecentConfigsListView.ItemsSource = recent.Select(r => new { Name = r.Name, Path = r.Path }).ToList();
        }
    }

    private async void NewConfigButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Check for unsaved changes
            if (ConfigurationService.Instance.HasUnsavedChanges)
            {
                var dialog = new ContentDialog
                {
                    Title = "Unsaved Changes",
                    Content = "Do you want to save changes to the current configuration?",
                    PrimaryButtonText = "Save",
                    SecondaryButtonText = "Don't Save",
                    CloseButtonText = "Cancel",
                    XamlRoot = this.XamlRoot
                };

                var result = await dialog.ShowAsync();
                if (result == ContentDialogResult.Primary)
                {
                    await SaveConfigurationAsync();
                }
                else if (result == ContentDialogResult.None)
                {
                    return; // Cancel
                }
            }

            ConfigurationService.Instance.NewConfiguration();
            UpdateConfigurationUI();

            // Brief visual feedback
            ConfigNameText.Text = "New configuration created";
            await Task.Delay(1000);
            UpdateConfigurationUI();
        }
        catch (Exception ex)
        {
            await ShowErrorDialog("Error", ex.Message);
        }
    }

    private async void OpenConfigButton_Click(object sender, RoutedEventArgs e)
    {
        // Check for unsaved changes
        if (ConfigurationService.Instance.HasUnsavedChanges)
        {
            var dialog = new ContentDialog
            {
                Title = "Unsaved Changes",
                Content = "Do you want to save changes to the current configuration?",
                PrimaryButtonText = "Save",
                SecondaryButtonText = "Don't Save",
                CloseButtonText = "Cancel",
                XamlRoot = this.XamlRoot
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                await SaveConfigurationAsync();
            }
            else if (result == ContentDialogResult.None)
            {
                return;
            }
        }

        var picker = new FileOpenPicker();
        picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
        picker.FileTypeFilter.Add(".arduinoconfig");
        picker.FileTypeFilter.Add(".json");

        // Initialize picker with window handle
        var hwnd = WindowNative.GetWindowHandle(App.MainWindow);
        InitializeWithWindow.Initialize(picker, hwnd);

        var file = await picker.PickSingleFileAsync();
        if (file != null)
        {
            var (success, error) = await ConfigurationService.Instance.LoadConfigurationAsync(file.Path);
            if (!success)
            {
                await ShowErrorDialog("Error Loading Configuration", error ?? "Unknown error");
            }
            else
            {
                UpdateConfigurationUI();
                UpdateRecentConfigurations();
            }
        }
    }

    private async void SaveConfigButton_Click(object sender, RoutedEventArgs e)
    {
        await SaveConfigurationAsync();
    }

    private async void SaveAsConfigButton_Click(object sender, RoutedEventArgs e)
    {
        await SaveConfigurationAsAsync();
    }

    private async Task SaveConfigurationAsync()
    {
        if (ConfigurationService.Instance.IsNewConfiguration)
        {
            await SaveConfigurationAsAsync();
        }
        else
        {
            var (success, error) = await ConfigurationService.Instance.SaveConfigurationAsync();
            if (!success)
            {
                await ShowErrorDialog("Error Saving Configuration", error ?? "Unknown error");
            }
        }
    }

    private async Task SaveConfigurationAsAsync()
    {
        var picker = new FileSavePicker();
        picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
        picker.FileTypeChoices.Add("Arduino Configuration", new List<string> { ".arduinoconfig" });
        picker.FileTypeChoices.Add("JSON File", new List<string> { ".json" });
        picker.SuggestedFileName = ConfigurationService.Instance.CurrentConfiguration.Name;

        var hwnd = WindowNative.GetWindowHandle(App.MainWindow);
        InitializeWithWindow.Initialize(picker, hwnd);

        var file = await picker.PickSaveFileAsync();
        if (file != null)
        {
            var (success, error) = await ConfigurationService.Instance.SaveConfigurationAsync(file.Path);
            if (!success)
            {
                await ShowErrorDialog("Error Saving Configuration", error ?? "Unknown error");
            }
            else
            {
                UpdateConfigurationUI();
                UpdateRecentConfigurations();
            }
        }
    }

    private async void RecentConfigsListView_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is { } item)
        {
            var pathProp = item.GetType().GetProperty("Path");
            var path = pathProp?.GetValue(item)?.ToString();

            if (!string.IsNullOrEmpty(path))
            {
                // Check for unsaved changes
                if (ConfigurationService.Instance.HasUnsavedChanges)
                {
                    var dialog = new ContentDialog
                    {
                        Title = "Unsaved Changes",
                        Content = "Do you want to save changes to the current configuration?",
                        PrimaryButtonText = "Save",
                        SecondaryButtonText = "Don't Save",
                        CloseButtonText = "Cancel",
                        XamlRoot = this.XamlRoot
                    };

                    var result = await dialog.ShowAsync();
                    if (result == ContentDialogResult.Primary)
                    {
                        await SaveConfigurationAsync();
                    }
                    else if (result == ContentDialogResult.None)
                    {
                        return;
                    }
                }

                var (success, error) = await ConfigurationService.Instance.LoadConfigurationAsync(path);
                if (!success)
                {
                    await ShowErrorDialog("Error Loading Configuration", error ?? "Unknown error");
                }
                else
                {
                    UpdateConfigurationUI();
                    UpdateRecentConfigurations();
                }
            }
        }
    }

    private void AutoSaveToggle_Toggled(object sender, RoutedEventArgs e)
    {
        SettingsService.Instance.Settings.AutoSaveEnabled = AutoSaveToggle.IsOn;
        SettingsService.Instance.SaveSettings();
    }

    private async Task ShowErrorDialog(string title, string message)
    {
        var dialog = new ContentDialog
        {
            Title = title,
            Content = message,
            CloseButtonText = "OK",
            XamlRoot = this.XamlRoot
        };
        await dialog.ShowAsync();
    }

    #endregion
}
