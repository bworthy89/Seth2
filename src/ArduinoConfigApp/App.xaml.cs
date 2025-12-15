using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using ArduinoConfigApp.Views;
using ArduinoConfigApp.Services;

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

        // Create the root frame
        var rootFrame = new Frame();
        rootFrame.NavigationFailed += OnNavigationFailed;

        // Apply saved theme
        var theme = SettingsService.Instance.GetCurrentTheme();
        if (rootFrame.Content is FrameworkElement element)
        {
            element.RequestedTheme = theme;
        }

        _window.Content = rootFrame;

        // Navigate to shell
        rootFrame.Navigate(typeof(ShellPage), e.Arguments);

        // Apply theme to root
        if (_window.Content is FrameworkElement rootElement)
        {
            rootElement.RequestedTheme = theme;
        }

        _window.Activate();
    }

    private void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
    {
        throw new Exception($"Failed to load Page {e.SourcePageType.FullName}");
    }
}
