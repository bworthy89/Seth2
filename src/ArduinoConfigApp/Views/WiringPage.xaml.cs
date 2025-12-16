using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI;
using Windows.ApplicationModel.DataTransfer;
using ArduinoConfigApp.Models;
using ArduinoConfigApp.Services;
using System.Text;

namespace ArduinoConfigApp.Views;

public sealed partial class WiringPage : Page
{
    private ProjectConfiguration _config = null!;

    public WiringPage()
    {
        this.InitializeComponent();
    }

    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
        RefreshWiringGuide();
    }

    private void Refresh_Click(object sender, RoutedEventArgs e)
    {
        RefreshWiringGuide();
    }

    private void RefreshWiringGuide()
    {
        _config = ConfigurationService.Instance.CurrentConfiguration;

        var inputCount = _config.Inputs.Count;
        var displayCount = _config.Displays.Count;

        SummaryText.Text = $"{inputCount} input(s), {displayCount} display(s) configured";

        // Show/hide empty state
        bool hasComponents = inputCount > 0 || displayCount > 0;
        EmptyDiagramPanel.Visibility = hasComponents ? Visibility.Collapsed : Visibility.Visible;
        DiagramScrollViewer.Visibility = hasComponents ? Visibility.Visible : Visibility.Collapsed;

        if (hasComponents)
        {
            GenerateBoardDiagram();
            GenerateComponentsVisual();
            GenerateStepByStepGuide();
        }
        else
        {
            StepsPanel.Children.Clear();
            StepsPanel.Children.Add(CreateInfoStep("No components configured", "Add inputs or displays to generate wiring guide."));
        }
    }

    private void GenerateBoardDiagram()
    {
        var boardType = _config.Board.BoardType;
        BoardNameText.Text = boardType switch
        {
            BoardType.ProMicro => "Arduino Pro Micro",
            BoardType.Mega2560 => "Arduino Mega 2560",
            _ => "Arduino Board"
        };

        // Get used pins
        var usedPins = GetUsedPins();

        // Generate pin headers
        LeftPinsPanel.Children.Clear();
        RightPinsPanel.Children.Clear();

        var pins = boardType == BoardType.ProMicro
            ? new[] { 2, 3, 4, 5, 6, 7, 8, 9, 10, 14, 15, 16 }
            : new[] { 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13 };

        var pins2 = boardType == BoardType.ProMicro
            ? new[] { 18, 19, 20, 21 }
            : new[] { 22, 23, 24, 25, 26, 27, 28, 29 };

        foreach (var pin in pins)
        {
            LeftPinsPanel.Children.Add(CreatePinVisual(pin, usedPins.ContainsKey(pin), usedPins.GetValueOrDefault(pin)));
        }

        foreach (var pin in pins2)
        {
            RightPinsPanel.Children.Add(CreatePinVisual(pin, usedPins.ContainsKey(pin), usedPins.GetValueOrDefault(pin)));
        }
    }

    private Border CreatePinVisual(int pin, bool isUsed, string? componentName)
    {
        var spiPins = GetSpiPins();
        var isSpi = spiPins.Contains(pin);

        var color = isUsed ? (isSpi ? "#F39C12" : "#3498DB") : "#4A6785";

        var border = new Border
        {
            Width = 32,
            Height = 24,
            Background = new SolidColorBrush(GetColorFromHex(color)),
            CornerRadius = new CornerRadius(3)
        };

        ToolTipService.SetToolTip(border, isUsed ? $"Pin {pin}: {componentName}" : $"Pin {pin}: Available");

        var text = new TextBlock
        {
            Text = pin.ToString(),
            Foreground = new SolidColorBrush(Colors.White),
            FontSize = 10,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

        border.Child = text;
        return border;
    }

    private Dictionary<int, string> GetUsedPins()
    {
        var usedPins = new Dictionary<int, string>();

        foreach (var input in _config.Inputs)
        {
            usedPins[input.Pin] = input.Name;
            if (input.Pin2.HasValue)
                usedPins[input.Pin2.Value] = $"{input.Name} (CLK)";
            if (input.ButtonPin.HasValue)
                usedPins[input.ButtonPin.Value] = $"{input.Name} (SW)";
        }

        foreach (var display in _config.Displays)
        {
            usedPins[display.CsPin] = $"{display.Name} (CS)";
        }

        return usedPins;
    }

    private HashSet<int> GetSpiPins()
    {
        return _config.Board.BoardType switch
        {
            BoardType.ProMicro => new HashSet<int> { 14, 15, 16 },
            BoardType.Mega2560 => new HashSet<int> { 50, 51, 52, 53 },
            _ => new HashSet<int>()
        };
    }

    private void GenerateComponentsVisual()
    {
        ComponentsVisual.Items.Clear();

        foreach (var input in _config.Inputs)
        {
            ComponentsVisual.Items.Add(CreateComponentCard(input));
        }

        foreach (var display in _config.Displays)
        {
            ComponentsVisual.Items.Add(CreateDisplayCard(display));
        }
    }

    private Border CreateComponentCard(InputConfiguration input)
    {
        var typeIcon = input.Type switch
        {
            InputType.MomentaryButton => "\uE73A",
            InputType.LatchingButton => "\uE73A",
            InputType.ToggleSwitch => "\uE8AB",
            InputType.RotaryEncoder => "\uE8AB",
            _ => "\uE964"
        };

        var typeName = input.Type switch
        {
            InputType.MomentaryButton => "Momentary Button",
            InputType.LatchingButton => "Latching Button (KD2-22)",
            InputType.ToggleSwitch => "Toggle Switch",
            InputType.RotaryEncoder => "Rotary Encoder (EC11)",
            _ => "Input"
        };

        var pinInfo = input.Type == InputType.RotaryEncoder
            ? $"CLK: Pin {input.Pin}, DT: Pin {input.Pin2}, SW: Pin {input.ButtonPin}"
            : $"Signal: Pin {input.Pin}";

        var border = new Border
        {
            Background = new SolidColorBrush(GetColorFromHex("#2D4A5E")),
            BorderBrush = new SolidColorBrush(GetColorFromHex("#3D6A8E")),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(12),
            Margin = new Thickness(0, 0, 0, 0)
        };

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var icon = new FontIcon
        {
            Glyph = typeIcon,
            FontSize = 20,
            Foreground = new SolidColorBrush(Colors.White),
            Margin = new Thickness(0, 0, 12, 0)
        };
        Grid.SetColumn(icon, 0);

        var stack = new StackPanel { Spacing = 4 };
        stack.Children.Add(new TextBlock
        {
            Text = input.Name,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Foreground = new SolidColorBrush(Colors.White)
        });
        stack.Children.Add(new TextBlock
        {
            Text = typeName,
            FontSize = 12,
            Foreground = new SolidColorBrush(GetColorFromHex("#A0B0C0"))
        });
        stack.Children.Add(new TextBlock
        {
            Text = pinInfo,
            FontSize = 11,
            Foreground = new SolidColorBrush(GetColorFromHex("#80A0B0"))
        });
        Grid.SetColumn(stack, 1);

        grid.Children.Add(icon);
        grid.Children.Add(stack);
        border.Child = grid;

        return border;
    }

    private Border CreateDisplayCard(DisplayConfiguration display)
    {
        var spiPins = _config.Board.BoardType == BoardType.ProMicro
            ? "MOSI: 16, SCK: 15, CS: " + display.CsPin
            : "MOSI: 51, SCK: 52, CS: " + display.CsPin;

        var border = new Border
        {
            Background = new SolidColorBrush(GetColorFromHex("#4A2D5E")),
            BorderBrush = new SolidColorBrush(GetColorFromHex("#6A3D8E")),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(12)
        };

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var icon = new FontIcon
        {
            Glyph = "\uE7F4",
            FontSize = 20,
            Foreground = new SolidColorBrush(Colors.White),
            Margin = new Thickness(0, 0, 12, 0)
        };
        Grid.SetColumn(icon, 0);

        var stack = new StackPanel { Spacing = 4 };
        stack.Children.Add(new TextBlock
        {
            Text = display.Name,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Foreground = new SolidColorBrush(Colors.White)
        });
        stack.Children.Add(new TextBlock
        {
            Text = $"MAX7219 {display.NumDigits}-Digit Display",
            FontSize = 12,
            Foreground = new SolidColorBrush(GetColorFromHex("#C0A0D0"))
        });
        stack.Children.Add(new TextBlock
        {
            Text = spiPins,
            FontSize = 11,
            Foreground = new SolidColorBrush(GetColorFromHex("#A080C0"))
        });
        Grid.SetColumn(stack, 1);

        grid.Children.Add(icon);
        grid.Children.Add(stack);
        border.Child = grid;

        return border;
    }

    private void GenerateStepByStepGuide()
    {
        StepsPanel.Children.Clear();
        int stepNumber = 1;

        // Power connections
        StepsPanel.Children.Add(CreateStepHeader("Power Connections"));
        StepsPanel.Children.Add(CreateStep(stepNumber++, "Connect Arduino GND to breadboard ground rail", "black"));
        StepsPanel.Children.Add(CreateStep(stepNumber++, "Connect Arduino VCC/5V to breadboard power rail", "red"));

        // SPI setup for displays
        if (_config.Displays.Count > 0)
        {
            StepsPanel.Children.Add(CreateStepHeader("SPI Bus (for MAX7219 Displays)"));

            var (miso, mosi, sck, ss) = _config.Board.BoardType == BoardType.ProMicro
                ? (14, 16, 15, 10)
                : (50, 51, 52, 53);

            StepsPanel.Children.Add(CreateStep(stepNumber++, $"Connect Arduino Pin {mosi} (MOSI) to all display DIN pins", "orange"));
            StepsPanel.Children.Add(CreateStep(stepNumber++, $"Connect Arduino Pin {sck} (SCK) to all display CLK pins", "orange"));
        }

        // Individual displays
        foreach (var display in _config.Displays)
        {
            StepsPanel.Children.Add(CreateStepHeader($"Display: {display.Name}"));
            StepsPanel.Children.Add(CreateStep(stepNumber++, $"Connect display VCC to 5V power rail", "red"));
            StepsPanel.Children.Add(CreateStep(stepNumber++, $"Connect display GND to ground rail", "black"));
            StepsPanel.Children.Add(CreateStep(stepNumber++, $"Connect Arduino Pin {display.CsPin} to display CS pin", "orange"));
        }

        // Individual inputs
        foreach (var input in _config.Inputs)
        {
            StepsPanel.Children.Add(CreateStepHeader($"Input: {input.Name}"));

            switch (input.Type)
            {
                case InputType.MomentaryButton:
                case InputType.LatchingButton:
                    StepsPanel.Children.Add(CreateStep(stepNumber++, $"Connect one terminal of button to Arduino Pin {input.Pin}", "blue"));
                    StepsPanel.Children.Add(CreateStep(stepNumber++, "Connect other terminal of button to GND", "black"));
                    if (input.PullupEnabled)
                        StepsPanel.Children.Add(CreateInfoStep("Note", "Internal pull-up resistor enabled - no external resistor needed"));
                    break;

                case InputType.ToggleSwitch:
                    StepsPanel.Children.Add(CreateStep(stepNumber++, $"Connect common (middle) terminal to Arduino Pin {input.Pin}", "blue"));
                    StepsPanel.Children.Add(CreateStep(stepNumber++, "Connect one outer terminal to GND", "black"));
                    StepsPanel.Children.Add(CreateStep(stepNumber++, "Leave other outer terminal unconnected (or connect to VCC for explicit HIGH)", "gray"));
                    break;

                case InputType.RotaryEncoder:
                    StepsPanel.Children.Add(CreateStep(stepNumber++, $"Connect encoder CLK to Arduino Pin {input.Pin}", "blue"));
                    StepsPanel.Children.Add(CreateStep(stepNumber++, $"Connect encoder DT to Arduino Pin {input.Pin2}", "blue"));
                    if (input.ButtonPin.HasValue)
                        StepsPanel.Children.Add(CreateStep(stepNumber++, $"Connect encoder SW (button) to Arduino Pin {input.ButtonPin}", "blue"));
                    StepsPanel.Children.Add(CreateStep(stepNumber++, "Connect encoder GND to ground rail", "black"));
                    StepsPanel.Children.Add(CreateStep(stepNumber++, "Connect encoder + (VCC) to 5V power rail", "red"));
                    break;
            }
        }

        // Final checks
        StepsPanel.Children.Add(CreateStepHeader("Final Checks"));
        StepsPanel.Children.Add(CreateStep(stepNumber++, "Verify all connections are secure", "gray"));
        StepsPanel.Children.Add(CreateStep(stepNumber++, "Double-check no short circuits between VCC and GND", "gray"));
        StepsPanel.Children.Add(CreateStep(stepNumber, "Connect Arduino to computer via USB", "gray"));
    }

    private Border CreateStepHeader(string title)
    {
        return new Border
        {
            Margin = new Thickness(0, 8, 0, 4),
            Child = new TextBlock
            {
                Text = title,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                Style = (Style)Application.Current.Resources["BodyStrongTextBlockStyle"]
            }
        };
    }

    private Border CreateStep(int stepNumber, string instruction, string wireColor)
    {
        var colorBrush = wireColor switch
        {
            "red" => GetColorFromHex("#E74C3C"),
            "black" => GetColorFromHex("#2C3E50"),
            "blue" => GetColorFromHex("#3498DB"),
            "orange" => GetColorFromHex("#F39C12"),
            "gray" => GetColorFromHex("#7F8C8D"),
            _ => GetColorFromHex("#95A5A6")
        };

        var border = new Border
        {
            Background = Application.Current.Resources["SubtleFillColorSecondaryBrush"] as Brush,
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(12, 8, 12, 8)
        };

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var numberBorder = new Border
        {
            Width = 24,
            Height = 24,
            CornerRadius = new CornerRadius(12),
            Background = Application.Current.Resources["PrimaryBrush"] as Brush,
            Margin = new Thickness(0, 0, 8, 0)
        };
        numberBorder.Child = new TextBlock
        {
            Text = stepNumber.ToString(),
            Foreground = new SolidColorBrush(Colors.White),
            FontSize = 12,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(numberBorder, 0);

        var colorIndicator = new Border
        {
            Width = 12,
            Height = 12,
            CornerRadius = new CornerRadius(2),
            Background = new SolidColorBrush(colorBrush),
            Margin = new Thickness(0, 0, 8, 0),
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(colorIndicator, 1);

        var text = new TextBlock
        {
            Text = instruction,
            TextWrapping = TextWrapping.Wrap,
            VerticalAlignment = VerticalAlignment.Center,
            Style = (Style)Application.Current.Resources["BodyTextBlockStyle"]
        };
        Grid.SetColumn(text, 2);

        grid.Children.Add(numberBorder);
        grid.Children.Add(colorIndicator);
        grid.Children.Add(text);
        border.Child = grid;

        return border;
    }

    private Border CreateInfoStep(string title, string content)
    {
        var border = new Border
        {
            Background = Application.Current.Resources["SubtleFillColorTertiaryBrush"] as Brush,
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(12, 8, 12, 8)
        };

        var stack = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        stack.Children.Add(new FontIcon
        {
            Glyph = "\uE946",
            FontSize = 14,
            Foreground = Application.Current.Resources["TextFillColorSecondaryBrush"] as Brush
        });
        stack.Children.Add(new TextBlock
        {
            Text = $"{title}: {content}",
            TextWrapping = TextWrapping.Wrap,
            Style = (Style)Application.Current.Resources["CaptionTextBlockStyle"],
            Foreground = Application.Current.Resources["TextFillColorSecondaryBrush"] as Brush
        });

        border.Child = stack;
        return border;
    }

    private async void CopyToClipboard_Click(object sender, RoutedEventArgs e)
    {
        var guide = GenerateTextGuide();

        var dataPackage = new DataPackage();
        dataPackage.SetText(guide);
        Clipboard.SetContent(dataPackage);

        // Show confirmation
        var button = sender as Button;
        if (button != null)
        {
            var originalContent = button.Content;
            button.Content = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 8,
                Children =
                {
                    new FontIcon { Glyph = "\uE73E", FontSize = 16 },
                    new TextBlock { Text = "Copied!" }
                }
            };
            await Task.Delay(2000);
            button.Content = originalContent;
        }
    }

    private string GenerateTextGuide()
    {
        var sb = new StringBuilder();
        var boardName = _config.Board.BoardType switch
        {
            BoardType.ProMicro => "Arduino Pro Micro",
            BoardType.Mega2560 => "Arduino Mega 2560",
            _ => "Arduino Board"
        };

        sb.AppendLine($"WIRING GUIDE - {_config.Name}");
        sb.AppendLine($"Board: {boardName}");
        sb.AppendLine(new string('=', 50));
        sb.AppendLine();

        // Pin Summary
        sb.AppendLine("PIN ASSIGNMENTS");
        sb.AppendLine(new string('-', 30));

        foreach (var input in _config.Inputs)
        {
            if (input.Type == InputType.RotaryEncoder)
            {
                sb.AppendLine($"Pin {input.Pin}: {input.Name} (CLK)");
                sb.AppendLine($"Pin {input.Pin2}: {input.Name} (DT)");
                if (input.ButtonPin.HasValue)
                    sb.AppendLine($"Pin {input.ButtonPin}: {input.Name} (SW)");
            }
            else
            {
                sb.AppendLine($"Pin {input.Pin}: {input.Name}");
            }
        }

        foreach (var display in _config.Displays)
        {
            sb.AppendLine($"Pin {display.CsPin}: {display.Name} (CS)");
        }

        sb.AppendLine();
        sb.AppendLine("STEP-BY-STEP INSTRUCTIONS");
        sb.AppendLine(new string('-', 30));

        int step = 1;

        sb.AppendLine($"\n[Power Connections]");
        sb.AppendLine($"{step++}. Connect Arduino GND to breadboard ground rail");
        sb.AppendLine($"{step++}. Connect Arduino VCC/5V to breadboard power rail");

        if (_config.Displays.Count > 0)
        {
            var (_, mosi, sck, _) = _config.Board.BoardType == BoardType.ProMicro
                ? (14, 16, 15, 10)
                : (50, 51, 52, 53);

            sb.AppendLine($"\n[SPI Bus for MAX7219]");
            sb.AppendLine($"{step++}. Connect Pin {mosi} (MOSI) to all display DIN pins");
            sb.AppendLine($"{step++}. Connect Pin {sck} (SCK) to all display CLK pins");
        }

        foreach (var display in _config.Displays)
        {
            sb.AppendLine($"\n[{display.Name}]");
            sb.AppendLine($"{step++}. Connect display VCC to 5V");
            sb.AppendLine($"{step++}. Connect display GND to ground");
            sb.AppendLine($"{step++}. Connect Pin {display.CsPin} to display CS");
        }

        foreach (var input in _config.Inputs)
        {
            sb.AppendLine($"\n[{input.Name}]");

            switch (input.Type)
            {
                case InputType.MomentaryButton:
                case InputType.LatchingButton:
                    sb.AppendLine($"{step++}. Connect button terminal to Pin {input.Pin}");
                    sb.AppendLine($"{step++}. Connect other terminal to GND");
                    break;

                case InputType.ToggleSwitch:
                    sb.AppendLine($"{step++}. Connect switch common to Pin {input.Pin}");
                    sb.AppendLine($"{step++}. Connect one terminal to GND");
                    break;

                case InputType.RotaryEncoder:
                    sb.AppendLine($"{step++}. Connect CLK to Pin {input.Pin}");
                    sb.AppendLine($"{step++}. Connect DT to Pin {input.Pin2}");
                    if (input.ButtonPin.HasValue)
                        sb.AppendLine($"{step++}. Connect SW to Pin {input.ButtonPin}");
                    sb.AppendLine($"{step++}. Connect GND to ground");
                    sb.AppendLine($"{step++}. Connect VCC to 5V");
                    break;
            }
        }

        sb.AppendLine($"\n[Final Checks]");
        sb.AppendLine($"{step++}. Verify all connections are secure");
        sb.AppendLine($"{step++}. Check for short circuits");
        sb.AppendLine($"{step}. Connect Arduino via USB");

        return sb.ToString();
    }

    private static Windows.UI.Color GetColorFromHex(string hex)
    {
        hex = hex.TrimStart('#');
        var r = Convert.ToByte(hex.Substring(0, 2), 16);
        var g = Convert.ToByte(hex.Substring(2, 2), 16);
        var b = Convert.ToByte(hex.Substring(4, 2), 16);
        return Windows.UI.Color.FromArgb(255, r, g, b);
    }
}
