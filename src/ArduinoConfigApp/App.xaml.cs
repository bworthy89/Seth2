using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Xaml.Media;
using ArduinoConfigApp.Views;
using ArduinoConfigApp.Services;
using WinRT.Interop;

namespace ArduinoConfigApp;

public partial class App : Application
{
    private Window? _window;

    public static Window? MainWindow { get; private set; }

    public App()
    {
        this.InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs e)
    {
        _window = new Window();
        MainWindow = _window;

        // Set window title
        _window.Title = "Arduino Configuration App";

        // Try to apply Mica backdrop
        TrySetMicaBackdrop(_window);

        // Create the root frame
        var rootFrame = new Frame();
        rootFrame.NavigationFailed += OnNavigationFailed;

        // Apply saved theme
        var theme = SettingsService.Instance.GetCurrentTheme();
        if (rootFrame.Content is FrameworkElement element)
        {
            element.RequestedTheme = theme;
        }

        // Apply theme to Frame before setting as content
        rootFrame.RequestedTheme = theme;

        _window.Content = rootFrame;

        // Navigate to shell
        rootFrame.Navigate(typeof(ShellPage), e.Arguments);

        // Set minimum window size
        var hwnd = WindowNative.GetWindowHandle(_window);
        var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
        var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);
        appWindow.Resize(new Windows.Graphics.SizeInt32(1200, 800));

        _window.Activate();
    }

    private void TrySetMicaBackdrop(Window window)
    {
        if (MicaController.IsSupported())
        {
            // Use Mica backdrop
            window.SystemBackdrop = new MicaBackdrop { Kind = MicaKind.Base };
        }
        else if (DesktopAcrylicController.IsSupported())
        {
            // Fallback to Acrylic if Mica is not supported
            window.SystemBackdrop = new DesktopAcrylicBackdrop();
        }
        // If neither is supported, the default solid background will be used
    }

    private void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
    {
        throw new Exception($"Failed to load Page {e.SourcePageType.FullName}");
    }
}
