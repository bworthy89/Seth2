using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Dispatching;
using ArduinoConfigApp.Models;
using ArduinoConfigApp.Services;

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

        // Start monitoring
        SerialPortService.Instance.StartMonitoring();

        // Initial update
        UpdateBoardsList(SerialPortService.Instance.DetectedBoards.ToList());
    }

    private void Page_Unloaded(object sender, RoutedEventArgs e)
    {
        // Unsubscribe from events
        SerialPortService.Instance.BoardsChanged -= OnBoardsChanged;
        SerialPortService.Instance.BoardConnected -= OnBoardConnected;
        SerialPortService.Instance.BoardDisconnected -= OnBoardDisconnected;
    }

    private void OnBoardsChanged(object? sender, List<ArduinoBoard> boards)
    {
        _dispatcherQueue.TryEnqueue(() => UpdateBoardsList(boards));
    }

    private void OnBoardConnected(object? sender, ArduinoBoard board)
    {
        _dispatcherQueue.TryEnqueue(() =>
        {
            // Could show a notification here
        });
    }

    private void OnBoardDisconnected(object? sender, ArduinoBoard board)
    {
        _dispatcherQueue.TryEnqueue(() =>
        {
            // Could show a notification here
        });
    }

    private void UpdateBoardsList(List<ArduinoBoard> boards)
    {
        BoardsListView.ItemsSource = boards;

        if (boards.Count == 0)
        {
            NoBoardsText.Visibility = Visibility.Visible;
            BoardsListView.Visibility = Visibility.Collapsed;

            ConnectionStatusText.Text = "No boards detected";
            StatusIcon.Glyph = "\uE839"; // Warning
            StatusIcon.Foreground = (Brush)Application.Current.Resources["SystemFillColorCautionBrush"];
            BoardTypeText.Text = "--";
            ComPortText.Text = "--";
        }
        else
        {
            NoBoardsText.Visibility = Visibility.Collapsed;
            BoardsListView.Visibility = Visibility.Visible;

            // Update status based on first available board
            var firstBoard = boards.FirstOrDefault();
            if (firstBoard != null)
            {
                UpdateSelectedBoard(firstBoard);

                // Auto-select if only one board
                if (boards.Count == 1)
                {
                    BoardsListView.SelectedIndex = 0;
                }
            }

            ConnectionStatusText.Text = $"{boards.Count} board(s) detected";
            StatusIcon.Glyph = "\uE73E"; // Checkmark
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
    }
}
