using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using ArduinoConfigApp.Models;
using ArduinoConfigApp.Services;
using Windows.System;

namespace ArduinoConfigApp.Views;

public sealed partial class OutputMappingPage : Page
{
    public OutputMappingPage()
    {
        this.InitializeComponent();
    }

    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
        ConfigurationService.Instance.ConfigurationChanged += OnConfigurationChanged;
        RefreshMappingsList();
    }

    private void OnConfigurationChanged(object? sender, EventArgs e)
    {
        RefreshMappingsList();
    }

    private void RefreshMappingsList()
    {
        var inputs = ConfigurationService.Instance.CurrentConfiguration.Inputs;
        var mappings = ConfigurationService.Instance.CurrentConfiguration.OutputMappings;

        if (inputs.Count == 0)
        {
            EmptyStatePanel.Visibility = Visibility.Visible;
            MappingsListView.Visibility = Visibility.Collapsed;
            MappingCountText.Text = "No inputs configured";
            return;
        }

        EmptyStatePanel.Visibility = Visibility.Collapsed;
        MappingsListView.Visibility = Visibility.Visible;

        var mappedCount = mappings.Count(m => !string.IsNullOrEmpty(m.Action.Key) ||
                                               m.ClockwiseAction != null ||
                                               m.CounterClockwiseAction != null);
        MappingCountText.Text = $"{mappedCount} of {inputs.Count} inputs mapped";

        var listItems = inputs.Select(input =>
        {
            var mapping = mappings.FirstOrDefault(m => m.InputId == input.Id);
            return new MappingListItem
            {
                InputId = input.Id,
                InputName = input.Name,
                InputType = GetInputTypeName(input.Type),
                TypeIcon = GetInputTypeIcon(input.Type),
                IsEncoder = input.Type == InputType.RotaryEncoder,
                MappingDisplay = GetMappingDisplay(input, mapping),
                MappingBackground = GetMappingBackground(mapping)
            };
        }).ToList();

        MappingsListView.ItemsSource = listItems;
        CheckForDuplicates();
    }

    private void MappingsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (MappingsListView.SelectedItem is MappingListItem item)
        {
            SelectedInputPanel.Visibility = Visibility.Visible;
            SelectedInputName.Text = item.InputName;
            SelectedInputMapping.Text = item.MappingDisplay;
        }
        else
        {
            SelectedInputPanel.Visibility = Visibility.Collapsed;
        }
    }

    private async void EditMapping_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string inputId)
        {
            var input = ConfigurationService.Instance.CurrentConfiguration.Inputs.FirstOrDefault(i => i.Id == inputId);
            if (input == null) return;

            var mapping = ConfigurationService.Instance.CurrentConfiguration.OutputMappings
                .FirstOrDefault(m => m.InputId == inputId) ?? new OutputMapping { InputId = inputId };

            var result = await ShowMappingDialog(input, mapping);
            if (result != null)
            {
                ConfigurationService.Instance.SetOutputMapping(result);
                RefreshMappingsList();
            }
        }
    }

    private async Task<OutputMapping?> ShowMappingDialog(InputConfiguration input, OutputMapping mapping)
    {
        var panel = new StackPanel { Spacing = 16, MinWidth = 400 };

        // Input info
        var infoText = new TextBlock
        {
            Text = $"Configure keyboard output for: {input.Name}",
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 8)
        };
        panel.Children.Add(infoText);

        KeyboardAction mainAction;

        if (input.Type == InputType.RotaryEncoder)
        {
            // Encoder has CW/CCW actions
            var cwPanel = CreateKeyActionPanel("Clockwise (CW)", mapping.ClockwiseAction ?? new KeyboardAction());
            var ccwPanel = CreateKeyActionPanel("Counter-Clockwise (CCW)", mapping.CounterClockwiseAction ?? new KeyboardAction());

            panel.Children.Add(cwPanel.Panel);
            panel.Children.Add(ccwPanel.Panel);

            // Optional button action
            if (input.ButtonPin.HasValue)
            {
                var btnPanel = CreateKeyActionPanel("Encoder Button Press", mapping.Action);
                panel.Children.Add(btnPanel.Panel);
                mainAction = btnPanel.GetAction;
            }
            else
            {
                mainAction = new KeyboardAction();
            }

            var dialog = new ContentDialog
            {
                Title = "Edit Encoder Mapping",
                Content = panel,
                PrimaryButtonText = "Save",
                CloseButtonText = "Cancel",
                XamlRoot = this.XamlRoot
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                return new OutputMapping
                {
                    InputId = input.Id,
                    Action = mainAction,
                    ClockwiseAction = cwPanel.GetAction,
                    CounterClockwiseAction = ccwPanel.GetAction
                };
            }
        }
        else
        {
            // Regular button/switch
            var actionPanel = CreateKeyActionPanel("Key Action", mapping.Action);
            panel.Children.Add(actionPanel.Panel);

            var dialog = new ContentDialog
            {
                Title = "Edit Key Mapping",
                Content = panel,
                PrimaryButtonText = "Save",
                CloseButtonText = "Cancel",
                XamlRoot = this.XamlRoot
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                return new OutputMapping
                {
                    InputId = input.Id,
                    Action = actionPanel.GetAction
                };
            }
        }

        return null;
    }

    private (StackPanel Panel, KeyboardAction GetAction) CreateKeyActionPanel(string header, KeyboardAction action)
    {
        var panel = new StackPanel { Spacing = 8 };

        // Header
        var headerText = new TextBlock
        {
            Text = header,
            Style = (Style)Application.Current.Resources["BodyStrongTextBlockStyle"]
        };
        panel.Children.Add(headerText);

        // Key selection
        var keyCombo = new ComboBox
        {
            Header = "Key",
            MinWidth = 200,
            ItemsSource = GetAllKeys(),
            SelectedItem = string.IsNullOrEmpty(action.Key) ? null : action.Key
        };
        panel.Children.Add(keyCombo);

        // Modifiers
        var modifiersPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 16, Margin = new Thickness(0, 8, 0, 0) };

        var ctrlCheck = new CheckBox { Content = "Ctrl", IsChecked = action.Ctrl };
        var altCheck = new CheckBox { Content = "Alt", IsChecked = action.Alt };
        var shiftCheck = new CheckBox { Content = "Shift", IsChecked = action.Shift };
        var winCheck = new CheckBox { Content = "Win", IsChecked = action.Win };

        modifiersPanel.Children.Add(ctrlCheck);
        modifiersPanel.Children.Add(altCheck);
        modifiersPanel.Children.Add(shiftCheck);
        modifiersPanel.Children.Add(winCheck);
        panel.Children.Add(modifiersPanel);

        // Key capture button
        var capturePanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Margin = new Thickness(0, 8, 0, 0) };
        var captureButton = new Button { Content = "Capture Key..." };
        var captureStatus = new TextBlock
        {
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"]
        };

        captureButton.Click += async (s, e) =>
        {
            captureButton.Content = "Press any key...";
            captureButton.IsEnabled = false;
            captureStatus.Text = "Listening...";

            var captured = await CaptureKeyPress();
            if (captured != null)
            {
                keyCombo.SelectedItem = captured.Key;
                ctrlCheck.IsChecked = captured.Ctrl;
                altCheck.IsChecked = captured.Alt;
                shiftCheck.IsChecked = captured.Shift;
                winCheck.IsChecked = captured.Win;
                captureStatus.Text = $"Captured: {captured.DisplayText}";
            }
            else
            {
                captureStatus.Text = "Cancelled";
            }

            captureButton.Content = "Capture Key...";
            captureButton.IsEnabled = true;
        };

        capturePanel.Children.Add(captureButton);
        capturePanel.Children.Add(captureStatus);
        panel.Children.Add(capturePanel);

        // Preview
        var previewText = new TextBlock
        {
            Margin = new Thickness(0, 8, 0, 0),
            Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"]
        };

        void UpdatePreview()
        {
            var key = keyCombo.SelectedItem?.ToString() ?? "";
            var parts = new List<string>();
            if (ctrlCheck.IsChecked == true) parts.Add("Ctrl");
            if (altCheck.IsChecked == true) parts.Add("Alt");
            if (shiftCheck.IsChecked == true) parts.Add("Shift");
            if (winCheck.IsChecked == true) parts.Add("Win");
            if (!string.IsNullOrEmpty(key)) parts.Add(key);
            previewText.Text = parts.Count > 0 ? $"Preview: {string.Join("+", parts)}" : "Preview: (none)";
        }

        keyCombo.SelectionChanged += (s, e) => UpdatePreview();
        ctrlCheck.Checked += (s, e) => UpdatePreview();
        ctrlCheck.Unchecked += (s, e) => UpdatePreview();
        altCheck.Checked += (s, e) => UpdatePreview();
        altCheck.Unchecked += (s, e) => UpdatePreview();
        shiftCheck.Checked += (s, e) => UpdatePreview();
        shiftCheck.Unchecked += (s, e) => UpdatePreview();
        winCheck.Checked += (s, e) => UpdatePreview();
        winCheck.Unchecked += (s, e) => UpdatePreview();

        UpdatePreview();
        panel.Children.Add(previewText);

        // Return panel and getter function
        KeyboardAction GetAction() => new KeyboardAction
        {
            Key = keyCombo.SelectedItem?.ToString() ?? "",
            Ctrl = ctrlCheck.IsChecked == true,
            Alt = altCheck.IsChecked == true,
            Shift = shiftCheck.IsChecked == true,
            Win = winCheck.IsChecked == true,
            Type = (ctrlCheck.IsChecked == true || altCheck.IsChecked == true ||
                    shiftCheck.IsChecked == true || winCheck.IsChecked == true)
                ? ActionType.KeyCombo : ActionType.SingleKey
        };

        return (panel, GetAction());
    }

    private async Task<KeyboardAction?> CaptureKeyPress()
    {
        var tcs = new TaskCompletionSource<KeyboardAction?>();
        var capturedAction = new KeyboardAction();
        var timeout = Task.Delay(5000); // 5 second timeout

        // Create a temporary invisible TextBox to capture key events
        var captureBox = new TextBox
        {
            Width = 0,
            Height = 0,
            Opacity = 0
        };

        // We need to add it to the visual tree to receive focus
        if (this.Content is Grid grid)
        {
            grid.Children.Add(captureBox);
            captureBox.Focus(FocusState.Programmatic);

            captureBox.KeyDown += (s, e) =>
            {
                var key = e.Key;

                // Check for modifiers
                capturedAction.Ctrl = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control).HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);
                capturedAction.Alt = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Menu).HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);
                capturedAction.Shift = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift).HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);
                capturedAction.Win = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.LeftWindows).HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down) ||
                                     Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.RightWindows).HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);

                // Skip if only modifier pressed
                if (key == VirtualKey.Control || key == VirtualKey.Menu || key == VirtualKey.Shift ||
                    key == VirtualKey.LeftWindows || key == VirtualKey.RightWindows ||
                    key == VirtualKey.LeftControl || key == VirtualKey.RightControl ||
                    key == VirtualKey.LeftMenu || key == VirtualKey.RightMenu ||
                    key == VirtualKey.LeftShift || key == VirtualKey.RightShift)
                {
                    return;
                }

                capturedAction.Key = ConvertVirtualKeyToString(key);
                e.Handled = true;
                tcs.TrySetResult(capturedAction);
            };

            // Handle escape to cancel
            captureBox.KeyDown += (s, e) =>
            {
                if (e.Key == VirtualKey.Escape)
                {
                    tcs.TrySetResult(null);
                    e.Handled = true;
                }
            };

            // Wait for either key press or timeout
            var completedTask = await Task.WhenAny(tcs.Task, timeout);

            grid.Children.Remove(captureBox);

            if (completedTask == timeout)
            {
                return null;
            }

            return await tcs.Task;
        }

        return null;
    }

    private string ConvertVirtualKeyToString(VirtualKey key)
    {
        return key switch
        {
            >= VirtualKey.A and <= VirtualKey.Z => key.ToString(),
            >= VirtualKey.Number0 and <= VirtualKey.Number9 => key.ToString().Replace("Number", ""),
            >= VirtualKey.NumberPad0 and <= VirtualKey.NumberPad9 => "Num" + key.ToString().Replace("NumberPad", ""),
            >= VirtualKey.F1 and <= VirtualKey.F24 => key.ToString(),
            VirtualKey.Space => "Space",
            VirtualKey.Enter => "Enter",
            VirtualKey.Tab => "Tab",
            VirtualKey.Escape => "Esc",
            VirtualKey.Back => "Backspace",
            VirtualKey.Delete => "Delete",
            VirtualKey.Insert => "Insert",
            VirtualKey.Home => "Home",
            VirtualKey.End => "End",
            VirtualKey.PageUp => "PgUp",
            VirtualKey.PageDown => "PgDn",
            VirtualKey.Up => "Up",
            VirtualKey.Down => "Down",
            VirtualKey.Left => "Left",
            VirtualKey.Right => "Right",
            VirtualKey.Add => "Num+",
            VirtualKey.Subtract => "Num-",
            VirtualKey.Multiply => "Num*",
            VirtualKey.Divide => "Num/",
            VirtualKey.Decimal => "Num.",
            (VirtualKey)186 => ";",
            (VirtualKey)187 => "=",
            (VirtualKey)188 => ",",
            (VirtualKey)189 => "-",
            (VirtualKey)190 => ".",
            (VirtualKey)191 => "/",
            (VirtualKey)192 => "`",
            (VirtualKey)219 => "[",
            (VirtualKey)220 => "\\",
            (VirtualKey)221 => "]",
            (VirtualKey)222 => "'",
            _ => key.ToString()
        };
    }

    private void CheckForDuplicates()
    {
        var mappings = ConfigurationService.Instance.CurrentConfiguration.OutputMappings;
        var duplicates = new List<string>();

        var keyUsage = new Dictionary<string, List<string>>();

        foreach (var mapping in mappings)
        {
            var input = ConfigurationService.Instance.CurrentConfiguration.Inputs.FirstOrDefault(i => i.Id == mapping.InputId);
            if (input == null) continue;

            void CheckKey(KeyboardAction? action, string context)
            {
                if (action == null || string.IsNullOrEmpty(action.Key)) return;
                var keyStr = action.DisplayText;
                if (!keyUsage.ContainsKey(keyStr))
                    keyUsage[keyStr] = new List<string>();
                keyUsage[keyStr].Add($"{input.Name} ({context})");
            }

            if (input.Type == InputType.RotaryEncoder)
            {
                CheckKey(mapping.ClockwiseAction, "CW");
                CheckKey(mapping.CounterClockwiseAction, "CCW");
                if (input.ButtonPin.HasValue)
                    CheckKey(mapping.Action, "Button");
            }
            else
            {
                CheckKey(mapping.Action, "Press");
            }
        }

        foreach (var kvp in keyUsage.Where(k => k.Value.Count > 1))
        {
            duplicates.Add($"{kvp.Key}: {string.Join(", ", kvp.Value)}");
        }

        if (duplicates.Count > 0)
        {
            DuplicateWarningPanel.Visibility = Visibility.Visible;
            DuplicateWarningText.Text = $"Duplicate mappings detected:\n{string.Join("\n", duplicates)}";
        }
        else
        {
            DuplicateWarningPanel.Visibility = Visibility.Collapsed;
        }
    }

    #region Helpers

    private static List<string> GetAllKeys()
    {
        var keys = new List<string>();

        // Letters
        for (char c = 'A'; c <= 'Z'; c++)
            keys.Add(c.ToString());

        // Numbers
        for (int i = 0; i <= 9; i++)
            keys.Add(i.ToString());

        // Function keys
        for (int i = 1; i <= 24; i++)
            keys.Add($"F{i}");

        // Navigation
        keys.AddRange(new[] { "Up", "Down", "Left", "Right", "Home", "End", "PgUp", "PgDn" });

        // Special
        keys.AddRange(new[] { "Enter", "Tab", "Esc", "Space", "Backspace", "Delete", "Insert" });

        // Media
        keys.AddRange(new[] { "Play", "Pause", "Stop", "Next", "Prev", "Vol+", "Vol-", "Mute" });

        // Punctuation
        keys.AddRange(new[] { ";", "=", ",", "-", ".", "/", "`", "[", "\\", "]", "'" });

        // Numpad
        for (int i = 0; i <= 9; i++)
            keys.Add($"Num{i}");
        keys.AddRange(new[] { "Num+", "Num-", "Num*", "Num/", "Num." });

        return keys;
    }

    private static string GetInputTypeName(InputType type) => type switch
    {
        InputType.MomentaryButton => "Momentary Button",
        InputType.LatchingButton => "Latching Button",
        InputType.ToggleSwitch => "Toggle Switch",
        InputType.RotaryEncoder => "Rotary Encoder",
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

    private static string GetMappingDisplay(InputConfiguration input, OutputMapping? mapping)
    {
        if (mapping == null)
            return "(not mapped)";

        if (input.Type == InputType.RotaryEncoder)
        {
            var cw = mapping.ClockwiseAction?.DisplayText ?? "-";
            var ccw = mapping.CounterClockwiseAction?.DisplayText ?? "-";
            if (string.IsNullOrEmpty(mapping.ClockwiseAction?.Key)) cw = "-";
            if (string.IsNullOrEmpty(mapping.CounterClockwiseAction?.Key)) ccw = "-";

            if (cw == "-" && ccw == "-")
                return "(not mapped)";

            return $"CW: {cw} | CCW: {ccw}";
        }

        if (string.IsNullOrEmpty(mapping.Action.Key))
            return "(not mapped)";

        return mapping.Action.DisplayText;
    }

    private static Brush GetMappingBackground(OutputMapping? mapping)
    {
        var hasMappng = mapping != null &&
            (!string.IsNullOrEmpty(mapping.Action.Key) ||
             (mapping.ClockwiseAction != null && !string.IsNullOrEmpty(mapping.ClockwiseAction.Key)) ||
             (mapping.CounterClockwiseAction != null && !string.IsNullOrEmpty(mapping.CounterClockwiseAction.Key)));

        return hasMappng
            ? (Brush)Application.Current.Resources["SystemFillColorSuccessBackgroundBrush"]
            : (Brush)Application.Current.Resources["SubtleFillColorSecondaryBrush"];
    }

    #endregion
}

public class MappingListItem
{
    public string InputId { get; set; } = string.Empty;
    public string InputName { get; set; } = string.Empty;
    public string InputType { get; set; } = string.Empty;
    public string TypeIcon { get; set; } = string.Empty;
    public bool IsEncoder { get; set; }
    public string MappingDisplay { get; set; } = string.Empty;
    public Brush MappingBackground { get; set; } = null!;
}
