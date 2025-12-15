using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using ArduinoConfigApp.Models;
using ArduinoConfigApp.Services;

namespace ArduinoConfigApp.Views;

public sealed partial class InputsPage : Page
{
    public InputsPage()
    {
        this.InitializeComponent();
    }

    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
        // Set board type from configuration
        var boardType = ConfigurationService.Instance.CurrentConfiguration.Board.BoardType;
        BoardTypeComboBox.SelectedIndex = boardType == BoardType.Mega2560 ? 1 : 0;

        // Subscribe to configuration changes
        ConfigurationService.Instance.ConfigurationChanged += OnConfigurationChanged;

        RefreshInputsList();
    }

    private void OnConfigurationChanged(object? sender, EventArgs e)
    {
        RefreshInputsList();
    }

    #region Board Type

    private void BoardTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (BoardTypeComboBox.SelectedItem is ComboBoxItem item && item.Tag is string tag)
        {
            var newBoardType = tag == "Mega2560" ? BoardType.Mega2560 : BoardType.ProMicro;
            var currentConfig = ConfigurationService.Instance.CurrentConfiguration;

            if (currentConfig.Board.BoardType != newBoardType)
            {
                // Check if any pins are out of range for the new board type
                if (newBoardType == BoardType.ProMicro && currentConfig.Inputs.Count > 0)
                {
                    var proMicroPins = PinHelper.GetAvailablePins(BoardType.ProMicro);
                    var invalidInputs = currentConfig.Inputs.Where(i =>
                        !proMicroPins.Contains(i.Pin) ||
                        (i.Pin2.HasValue && !proMicroPins.Contains(i.Pin2.Value)) ||
                        (i.ButtonPin.HasValue && !proMicroPins.Contains(i.ButtonPin.Value))).ToList();

                    if (invalidInputs.Count > 0)
                    {
                        ShowBoardChangeWarning(invalidInputs);
                        return;
                    }
                }

                currentConfig.Board.BoardType = newBoardType;
                ConfigurationService.Instance.MarkAsModified();
            }
        }
    }

    private async void ShowBoardChangeWarning(List<InputConfiguration> invalidInputs)
    {
        var inputNames = string.Join(", ", invalidInputs.Select(i => i.Name));
        var dialog = new ContentDialog
        {
            Title = "Invalid Pin Assignments",
            Content = $"The following inputs have pins that are not available on Pro Micro and will be removed:\n\n{inputNames}\n\nDo you want to continue?",
            PrimaryButtonText = "Continue",
            CloseButtonText = "Cancel",
            XamlRoot = this.XamlRoot
        };

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            // Remove invalid inputs
            foreach (var input in invalidInputs)
            {
                ConfigurationService.Instance.RemoveInput(input.Id);
            }

            ConfigurationService.Instance.CurrentConfiguration.Board.BoardType = BoardType.ProMicro;
            ConfigurationService.Instance.MarkAsModified();
            RefreshInputsList();
        }
        else
        {
            // Revert selection
            BoardTypeComboBox.SelectedIndex = 1; // Mega2560
        }
    }

    #endregion

    #region Input Management

    private void AddInputButton_Click(object sender, RoutedEventArgs e)
    {
        // Flyout opens automatically
    }

    private void AddMomentaryButton_Click(object sender, RoutedEventArgs e)
    {
        AddInput(InputType.MomentaryButton);
    }

    private void AddLatchingButton_Click(object sender, RoutedEventArgs e)
    {
        AddInput(InputType.LatchingButton);
    }

    private void AddToggleSwitch_Click(object sender, RoutedEventArgs e)
    {
        AddInput(InputType.ToggleSwitch);
    }

    private void AddRotaryEncoder_Click(object sender, RoutedEventArgs e)
    {
        AddInput(InputType.RotaryEncoder);
    }

    private async void AddInput(InputType type)
    {
        var boardType = ConfigurationService.Instance.CurrentConfiguration.Board.BoardType;
        var usedPins = GetUsedPins();
        var availablePins = PinHelper.GetAvailablePins(boardType).Where(p => !usedPins.Contains(p)).ToList();

        if (availablePins.Count == 0)
        {
            await ShowErrorDialog("No Available Pins", "All pins are already in use. Delete an input to free up pins.");
            return;
        }

        var input = new InputConfiguration
        {
            Name = GetDefaultInputName(type),
            Type = type,
            Pin = availablePins.First()
        };

        // For encoders, assign second pin
        if (type == InputType.RotaryEncoder && availablePins.Count >= 2)
        {
            input.Pin2 = availablePins[1];
        }

        var result = await ShowInputDialog(input, "Add Input", availablePins, true);
        if (result != null)
        {
            ConfigurationService.Instance.AddInput(result);
            RefreshInputsList();
        }
    }

    private async void EditInput_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string inputId)
        {
            var input = ConfigurationService.Instance.CurrentConfiguration.Inputs.FirstOrDefault(i => i.Id == inputId);
            if (input == null) return;

            var boardType = ConfigurationService.Instance.CurrentConfiguration.Board.BoardType;
            var usedPins = GetUsedPins(inputId); // Exclude current input's pins
            var availablePins = PinHelper.GetAvailablePins(boardType).Where(p => !usedPins.Contains(p)).ToList();

            // Add current pins back to available list
            if (!availablePins.Contains(input.Pin)) availablePins.Add(input.Pin);
            if (input.Pin2.HasValue && !availablePins.Contains(input.Pin2.Value)) availablePins.Add(input.Pin2.Value);
            if (input.ButtonPin.HasValue && !availablePins.Contains(input.ButtonPin.Value)) availablePins.Add(input.ButtonPin.Value);
            availablePins.Sort();

            var result = await ShowInputDialog(input, "Edit Input", availablePins, false);
            if (result != null)
            {
                ConfigurationService.Instance.UpdateInput(result);
                RefreshInputsList();
            }
        }
    }

    private async void DeleteInput_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string inputId)
        {
            var input = ConfigurationService.Instance.CurrentConfiguration.Inputs.FirstOrDefault(i => i.Id == inputId);
            if (input == null) return;

            var dialog = new ContentDialog
            {
                Title = "Delete Input",
                Content = $"Are you sure you want to delete '{input.Name}'?",
                PrimaryButtonText = "Delete",
                CloseButtonText = "Cancel",
                XamlRoot = this.XamlRoot
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                ConfigurationService.Instance.RemoveInput(inputId);
                RefreshInputsList();
            }
        }
    }

    private async Task<InputConfiguration?> ShowInputDialog(InputConfiguration input, string title, List<int> availablePins, bool isNew)
    {
        var boardType = ConfigurationService.Instance.CurrentConfiguration.Board.BoardType;
        var spiPins = PinHelper.GetSpiPins(boardType);

        // Create dialog content
        var panel = new StackPanel { Spacing = 16, MinWidth = 350 };

        // Name field
        var nameBox = new TextBox
        {
            Header = "Name",
            Text = input.Name,
            PlaceholderText = "Enter input name"
        };
        panel.Children.Add(nameBox);

        // Type display (read-only for edit)
        var typeText = new TextBlock
        {
            Text = $"Type: {GetInputTypeName(input.Type)}",
            Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["TextFillColorSecondaryBrush"]
        };
        panel.Children.Add(typeText);

        // Pin selection
        var pinItems = availablePins.Select(p => new PinItem(p, spiPins.Contains(p))).ToList();
        var pinCombo = new ComboBox
        {
            Header = "Pin",
            MinWidth = 150,
            ItemsSource = pinItems,
            DisplayMemberPath = "Display"
        };
        pinCombo.SelectedItem = pinItems.FirstOrDefault(p => p.Pin == input.Pin);
        panel.Children.Add(pinCombo);

        // Second pin for encoders
        ComboBox? pin2Combo = null;
        ComboBox? buttonPinCombo = null;
        List<PinItem>? pin2Items = null;
        List<PinItem>? buttonPinItems = null;

        if (input.Type == InputType.RotaryEncoder)
        {
            pin2Items = availablePins.Select(p => new PinItem(p, spiPins.Contains(p))).ToList();
            pin2Combo = new ComboBox
            {
                Header = "Pin 2 (DT)",
                MinWidth = 150,
                ItemsSource = pin2Items,
                DisplayMemberPath = "Display"
            };
            pin2Combo.SelectedItem = pin2Items.FirstOrDefault(p => p.Pin == input.Pin2);
            panel.Children.Add(pin2Combo);

            // Optional button pin
            buttonPinItems = new List<PinItem> { new PinItem(-1, false) { Display = "None" } };
            buttonPinItems.AddRange(availablePins.Select(p => new PinItem(p, spiPins.Contains(p))));

            buttonPinCombo = new ComboBox
            {
                Header = "Button Pin (SW) - Optional",
                MinWidth = 150,
                ItemsSource = buttonPinItems,
                DisplayMemberPath = "Display"
            };
            buttonPinCombo.SelectedItem = input.ButtonPin.HasValue
                ? buttonPinItems.FirstOrDefault(p => p.Pin == input.ButtonPin)
                : buttonPinItems[0];
            panel.Children.Add(buttonPinCombo);
        }

        // Debounce setting
        var debounceBox = new NumberBox
        {
            Header = "Debounce (ms)",
            Value = input.DebounceMs,
            Minimum = 0,
            Maximum = 500,
            SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Compact
        };
        panel.Children.Add(debounceBox);

        // Pullup toggle
        var pullupToggle = new ToggleSwitch
        {
            Header = "Internal Pull-up Resistor",
            IsOn = input.PullupEnabled
        };
        panel.Children.Add(pullupToggle);

        // Test button (placeholder)
        var testButton = new Button
        {
            Content = "Test Input",
            Margin = new Thickness(0, 8, 0, 0)
        };
        testButton.Click += (s, e) => TestInput_Click(input);
        panel.Children.Add(testButton);

        var dialog = new ContentDialog
        {
            Title = title,
            Content = panel,
            PrimaryButtonText = isNew ? "Add" : "Save",
            CloseButtonText = "Cancel",
            XamlRoot = this.XamlRoot
        };

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            // Validate
            if (string.IsNullOrWhiteSpace(nameBox.Text))
            {
                await ShowErrorDialog("Validation Error", "Please enter a name for the input.");
                return await ShowInputDialog(input, title, availablePins, isNew);
            }

            var selectedPin = (pinCombo.SelectedItem as PinItem)?.Pin ?? availablePins.First();
            int? selectedPin2 = null;
            int? selectedButtonPin = null;

            if (input.Type == InputType.RotaryEncoder)
            {
                selectedPin2 = (pin2Combo?.SelectedItem as PinItem)?.Pin;
                var buttonPinItem = buttonPinCombo?.SelectedItem as PinItem;
                selectedButtonPin = buttonPinItem?.Pin == -1 ? null : buttonPinItem?.Pin;

                if (selectedPin == selectedPin2)
                {
                    await ShowErrorDialog("Validation Error", "Pin 1 and Pin 2 must be different.");
                    return await ShowInputDialog(input, title, availablePins, isNew);
                }
            }

            return new InputConfiguration
            {
                Id = input.Id,
                Name = nameBox.Text.Trim(),
                Type = input.Type,
                Pin = selectedPin,
                Pin2 = selectedPin2,
                ButtonPin = selectedButtonPin,
                DebounceMs = (int)debounceBox.Value,
                PullupEnabled = pullupToggle.IsOn
            };
        }

        return null;
    }

    private async void TestInput_Click(InputConfiguration input)
    {
        await ShowErrorDialog("Test Input", $"Testing '{input.Name}' on pin {input.Pin}.\n\nThis feature will be implemented when serial communication is added.");
    }

    #endregion

    #region Helpers

    private void RefreshInputsList()
    {
        var inputs = ConfigurationService.Instance.CurrentConfiguration.Inputs;
        var boardType = ConfigurationService.Instance.CurrentConfiguration.Board.BoardType;
        var spiPins = PinHelper.GetSpiPins(boardType);

        if (inputs.Count == 0)
        {
            EmptyStatePanel.Visibility = Visibility.Visible;
            InputsListView.Visibility = Visibility.Collapsed;
            InputCountText.Text = "No inputs configured";
        }
        else
        {
            EmptyStatePanel.Visibility = Visibility.Collapsed;
            InputsListView.Visibility = Visibility.Visible;
            InputCountText.Text = $"{inputs.Count} input(s) configured";

            InputsListView.ItemsSource = inputs.Select(i => new InputListItem
            {
                Id = i.Id,
                Name = i.Name,
                TypeName = GetInputTypeName(i.Type),
                TypeIcon = GetInputTypeIcon(i.Type),
                PinDisplay = GetPinDisplay(i),
                SpiWarningVisibility = IsSpiPin(i, spiPins) ? Visibility.Visible : Visibility.Collapsed
            }).ToList();
        }
    }

    private HashSet<int> GetUsedPins(string? excludeInputId = null)
    {
        var usedPins = new HashSet<int>();
        foreach (var input in ConfigurationService.Instance.CurrentConfiguration.Inputs)
        {
            if (input.Id == excludeInputId) continue;

            usedPins.Add(input.Pin);
            if (input.Pin2.HasValue) usedPins.Add(input.Pin2.Value);
            if (input.ButtonPin.HasValue) usedPins.Add(input.ButtonPin.Value);
        }

        // Also check display CS pins
        foreach (var display in ConfigurationService.Instance.CurrentConfiguration.Displays)
        {
            usedPins.Add(display.CsPin);
        }

        return usedPins;
    }

    private string GetDefaultInputName(InputType type)
    {
        var existingCount = ConfigurationService.Instance.CurrentConfiguration.Inputs.Count(i => i.Type == type);
        return type switch
        {
            InputType.MomentaryButton => $"Momentary Button {existingCount + 1}",
            InputType.LatchingButton => $"Latching Button {existingCount + 1}",
            InputType.ToggleSwitch => $"Toggle Switch {existingCount + 1}",
            InputType.RotaryEncoder => $"Encoder {existingCount + 1}",
            _ => $"Input {existingCount + 1}"
        };
    }

    private static string GetInputTypeName(InputType type) => type switch
    {
        InputType.MomentaryButton => "Momentary Button",
        InputType.LatchingButton => "Latching Button (KD2-22)",
        InputType.ToggleSwitch => "Toggle Switch",
        InputType.RotaryEncoder => "Rotary Encoder (EC11)",
        _ => "Unknown"
    };

    private static string GetInputTypeIcon(InputType type) => type switch
    {
        InputType.MomentaryButton => "\uE73A",
        InputType.LatchingButton => "\uE73A",
        InputType.ToggleSwitch => "\uE8AB",
        InputType.RotaryEncoder => "\uE8AB",
        _ => "\uE946"
    };

    private static string GetPinDisplay(InputConfiguration input)
    {
        if (input.Type == InputType.RotaryEncoder)
        {
            var pins = $"CLK: {input.Pin}, DT: {input.Pin2}";
            if (input.ButtonPin.HasValue) pins += $", SW: {input.ButtonPin}";
            return pins;
        }
        return $"Pin {input.Pin}";
    }

    private static bool IsSpiPin(InputConfiguration input, HashSet<int> spiPins)
    {
        if (spiPins.Contains(input.Pin)) return true;
        if (input.Pin2.HasValue && spiPins.Contains(input.Pin2.Value)) return true;
        if (input.ButtonPin.HasValue && spiPins.Contains(input.ButtonPin.Value)) return true;
        return false;
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

// Helper class for pin combo items
public class PinItem
{
    public int Pin { get; set; }
    public bool IsSpi { get; set; }
    public string Display { get; set; }

    public PinItem(int pin, bool isSpi)
    {
        Pin = pin;
        IsSpi = isSpi;
        Display = pin == -1 ? "None" : (isSpi ? $"Pin {pin} (SPI)" : $"Pin {pin}");
    }
}

// Helper class for list view items
public class InputListItem
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string TypeName { get; set; } = string.Empty;
    public string TypeIcon { get; set; } = string.Empty;
    public string PinDisplay { get; set; } = string.Empty;
    public Visibility SpiWarningVisibility { get; set; }
}

// Static helper for pin information
public static class PinHelper
{
    public static List<int> GetAvailablePins(BoardType boardType) => boardType switch
    {
        BoardType.ProMicro => new List<int> { 2, 3, 4, 5, 6, 7, 8, 9, 10, 14, 15, 16, 18, 19, 20, 21 },
        BoardType.Mega2560 => Enumerable.Range(2, 52).ToList(), // 2-53
        _ => new List<int>()
    };

    public static HashSet<int> GetSpiPins(BoardType boardType) => boardType switch
    {
        BoardType.ProMicro => new HashSet<int> { 14, 15, 16 }, // MISO, SCK, MOSI
        BoardType.Mega2560 => new HashSet<int> { 50, 51, 52, 53 }, // MISO, MOSI, SCK, SS
        _ => new HashSet<int>()
    };
}
