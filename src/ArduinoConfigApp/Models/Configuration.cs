using System.Text.Json.Serialization;

namespace ArduinoConfigApp.Models;

/// <summary>
/// Root configuration object that contains all project settings
/// </summary>
public class ProjectConfiguration
{
    public string Version { get; set; } = "1.0";
    public string Name { get; set; } = "Untitled Configuration";
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime ModifiedAt { get; set; } = DateTime.Now;

    public BoardConfiguration Board { get; set; } = new();
    public List<InputConfiguration> Inputs { get; set; } = new();
    public List<DisplayConfiguration> Displays { get; set; } = new();
    public List<OutputMapping> OutputMappings { get; set; } = new();
}

/// <summary>
/// Board-specific configuration
/// </summary>
public class BoardConfiguration
{
    public BoardType BoardType { get; set; } = BoardType.ProMicro;
    public string? PreferredPort { get; set; }
    public int BaudRate { get; set; } = 115200;
}

/// <summary>
/// Input device configuration (buttons, encoders, switches)
/// </summary>
public class InputConfiguration
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = "Input";
    public InputType Type { get; set; }
    public int Pin { get; set; }
    public int? Pin2 { get; set; } // For encoders (CLK, DT)
    public int? ButtonPin { get; set; } // For encoder button (SW)
    public bool PullupEnabled { get; set; } = true;
    public int DebounceMs { get; set; } = 50;
}

public enum InputType
{
    MomentaryButton,
    LatchingButton,  // KD2-22
    ToggleSwitch,
    RotaryEncoder    // EC11
}

/// <summary>
/// MAX7219 display configuration
/// </summary>
public class DisplayConfiguration
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = "Display";
    public int CsPin { get; set; } // Chip Select pin
    public int NumDigits { get; set; } = 8;
    public int Brightness { get; set; } = 8; // 0-15
    public bool LeadingZeros { get; set; } = false;
    public int? DecimalPosition { get; set; } // Position from right (0-7)
    public int InitialValue { get; set; } = 0;
    public int MinValue { get; set; } = 0;
    public int MaxValue { get; set; } = 99999999;

    // Encoder mappings for this display
    public List<EncoderDisplayMapping> EncoderMappings { get; set; } = new();
}

/// <summary>
/// Maps an encoder to a display with increment settings
/// </summary>
public class EncoderDisplayMapping
{
    public string EncoderId { get; set; } = string.Empty;
    public int Increment { get; set; } = 1; // 1, 10, 100, 1000
    public bool ClockwiseIncreases { get; set; } = true;
}

/// <summary>
/// Maps an input to a keyboard output
/// </summary>
public class OutputMapping
{
    public string InputId { get; set; } = string.Empty;
    public KeyboardAction Action { get; set; } = new();

    // For encoders - separate actions for CW/CCW
    public KeyboardAction? ClockwiseAction { get; set; }
    public KeyboardAction? CounterClockwiseAction { get; set; }
}

/// <summary>
/// Keyboard action (key press, combo, or sequence)
/// </summary>
public class KeyboardAction
{
    public ActionType Type { get; set; } = ActionType.SingleKey;
    public string Key { get; set; } = string.Empty; // e.g., "A", "F1", "Space"
    public bool Ctrl { get; set; }
    public bool Alt { get; set; }
    public bool Shift { get; set; }
    public bool Win { get; set; }
    public List<string>? Sequence { get; set; } // For key sequences

    public string DisplayText
    {
        get
        {
            var modifiers = new List<string>();
            if (Ctrl) modifiers.Add("Ctrl");
            if (Alt) modifiers.Add("Alt");
            if (Shift) modifiers.Add("Shift");
            if (Win) modifiers.Add("Win");
            modifiers.Add(Key);
            return string.Join("+", modifiers);
        }
    }
}

public enum ActionType
{
    SingleKey,
    KeyCombo,
    KeySequence,
    MediaKey
}
