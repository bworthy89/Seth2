using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using ArduinoConfigApp.Services;

namespace ArduinoConfigApp.Views;

public sealed partial class ShellPage : Page
{
    private readonly Dictionary<string, Type> _pages = new()
    {
        { "Dashboard", typeof(DashboardPage) },
        { "Inputs", typeof(InputsPage) },
        { "Display", typeof(DisplayPage) },
        { "OutputMapping", typeof(OutputMappingPage) },
        { "Testing", typeof(TestingPage) },
        { "Wiring", typeof(WiringPage) }
    };

    public ShellPage()
    {
        this.InitializeComponent();
        this.Loaded += ShellPage_Loaded;
    }

    private void ShellPage_Loaded(object sender, RoutedEventArgs e)
    {
        LoadThemeSetting();
    }

    private void LoadThemeSetting()
    {
        var theme = SettingsService.Instance.Settings.Theme;

        // Set combo box selection without triggering change event
        ThemeComboBox.SelectionChanged -= ThemeComboBox_SelectionChanged;
        foreach (ComboBoxItem item in ThemeComboBox.Items)
        {
            if (item.Tag?.ToString() == theme)
            {
                ThemeComboBox.SelectedItem = item;
                break;
            }
        }
        ThemeComboBox.SelectionChanged += ThemeComboBox_SelectionChanged;

        // Apply the theme
        ApplyTheme(theme);
    }

    private void NavView_Loaded(object sender, RoutedEventArgs e)
    {
        // Select Dashboard by default
        NavView.SelectedItem = NavView.MenuItems[0];
    }

    private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItemContainer is NavigationViewItem selectedItem)
        {
            var tag = selectedItem.Tag?.ToString();

            if (tag == "ThemeToggle")
            {
                // Toggle theme when theme button clicked
                CycleTheme();
                return;
            }

            if (tag != null && _pages.TryGetValue(tag, out var pageType))
            {
                ContentFrame.Navigate(pageType);
            }
        }
    }

    private void ThemeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ThemeComboBox.SelectedItem is ComboBoxItem selectedItem)
        {
            var theme = selectedItem.Tag?.ToString() ?? "Default";
            SettingsService.Instance.SetTheme(theme);
            ApplyTheme(theme);
        }
    }

    private void CycleTheme()
    {
        var currentTheme = SettingsService.Instance.Settings.Theme;
        var newTheme = currentTheme switch
        {
            "Default" => "Light",
            "Light" => "Dark",
            "Dark" => "Default",
            _ => "Default"
        };

        SettingsService.Instance.SetTheme(newTheme);
        ApplyTheme(newTheme);

        // Update combo box
        foreach (ComboBoxItem item in ThemeComboBox.Items)
        {
            if (item.Tag?.ToString() == newTheme)
            {
                ThemeComboBox.SelectedItem = item;
                break;
            }
        }
    }

    private void ApplyTheme(string theme)
    {
        var elementTheme = theme switch
        {
            "Light" => ElementTheme.Light,
            "Dark" => ElementTheme.Dark,
            _ => ElementTheme.Default
        };

        // Apply theme to the page itself
        this.RequestedTheme = elementTheme;

        // Also apply to the root Frame if available
        if (this.XamlRoot?.Content is FrameworkElement rootElement)
        {
            rootElement.RequestedTheme = elementTheme;
        }

        // Update theme icon
        ThemeIcon.Glyph = theme switch
        {
            "Light" => "\uE706",  // Sun
            "Dark" => "\uE708",   // Moon
            _ => "\uE793"         // Settings
        };
    }
}
