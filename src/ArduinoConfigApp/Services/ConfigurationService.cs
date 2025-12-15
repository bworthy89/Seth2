using System.Text.Json;
using System.Text.Json.Serialization;
using ArduinoConfigApp.Models;

namespace ArduinoConfigApp.Services;

public class ConfigurationService
{
    private static ConfigurationService? _instance;
    public static ConfigurationService Instance => _instance ??= new ConfigurationService();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    public const string FileExtension = ".arduinoconfig";
    public const string FileFilter = "Arduino Config|*.arduinoconfig|JSON Files|*.json|All Files|*.*";

    public ProjectConfiguration CurrentConfiguration { get; private set; } = new();
    public string? CurrentFilePath { get; private set; }
    public bool HasUnsavedChanges { get; private set; }
    public bool IsNewConfiguration => CurrentFilePath == null;

    public event EventHandler? ConfigurationChanged;
    public event EventHandler? ConfigurationSaved;
    public event EventHandler? ConfigurationLoaded;

    private ConfigurationService() { }

    /// <summary>
    /// Creates a new empty configuration
    /// </summary>
    public void NewConfiguration()
    {
        CurrentConfiguration = new ProjectConfiguration
        {
            Name = "Untitled Configuration",
            CreatedAt = DateTime.Now,
            ModifiedAt = DateTime.Now
        };
        CurrentFilePath = null;
        HasUnsavedChanges = false;
        ConfigurationChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Loads a configuration from file
    /// </summary>
    public async Task<(bool Success, string? Error)> LoadConfigurationAsync(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                return (false, "File not found");
            }

            var json = await File.ReadAllTextAsync(filePath);
            var config = JsonSerializer.Deserialize<ProjectConfiguration>(json, JsonOptions);

            if (config == null)
            {
                return (false, "Failed to parse configuration file");
            }

            // Validate configuration
            var validationResult = ValidateConfiguration(config);
            if (!validationResult.IsValid)
            {
                return (false, validationResult.Error);
            }

            CurrentConfiguration = config;
            CurrentFilePath = filePath;
            HasUnsavedChanges = false;

            // Add to recent configurations
            AddToRecentConfigurations(filePath);

            ConfigurationLoaded?.Invoke(this, EventArgs.Empty);
            ConfigurationChanged?.Invoke(this, EventArgs.Empty);

            return (true, null);
        }
        catch (JsonException ex)
        {
            return (false, $"Invalid JSON format: {ex.Message}");
        }
        catch (Exception ex)
        {
            return (false, $"Error loading file: {ex.Message}");
        }
    }

    /// <summary>
    /// Saves the current configuration to file
    /// </summary>
    public async Task<(bool Success, string? Error)> SaveConfigurationAsync(string? filePath = null)
    {
        try
        {
            var path = filePath ?? CurrentFilePath;
            if (string.IsNullOrEmpty(path))
            {
                return (false, "No file path specified");
            }

            CurrentConfiguration.ModifiedAt = DateTime.Now;

            var json = JsonSerializer.Serialize(CurrentConfiguration, JsonOptions);
            await File.WriteAllTextAsync(path, json);

            CurrentFilePath = path;
            HasUnsavedChanges = false;

            // Add to recent configurations
            AddToRecentConfigurations(path);

            ConfigurationSaved?.Invoke(this, EventArgs.Empty);

            return (true, null);
        }
        catch (Exception ex)
        {
            return (false, $"Error saving file: {ex.Message}");
        }
    }

    /// <summary>
    /// Marks the configuration as modified
    /// </summary>
    public void MarkAsModified()
    {
        HasUnsavedChanges = true;
        CurrentConfiguration.ModifiedAt = DateTime.Now;
        ConfigurationChanged?.Invoke(this, EventArgs.Empty);

        // Auto-save if enabled
        if (SettingsService.Instance.Settings.AutoSaveEnabled && CurrentFilePath != null)
        {
            _ = SaveConfigurationAsync();
        }
    }

    /// <summary>
    /// Validates a configuration
    /// </summary>
    public (bool IsValid, string? Error) ValidateConfiguration(ProjectConfiguration config)
    {
        // Check version compatibility
        if (string.IsNullOrEmpty(config.Version))
        {
            return (false, "Configuration version is missing");
        }

        // Check for duplicate pin assignments
        var usedPins = new HashSet<int>();

        foreach (var input in config.Inputs)
        {
            if (!usedPins.Add(input.Pin))
            {
                return (false, $"Duplicate pin assignment: Pin {input.Pin} is used by multiple inputs");
            }

            if (input.Pin2.HasValue && !usedPins.Add(input.Pin2.Value))
            {
                return (false, $"Duplicate pin assignment: Pin {input.Pin2} is used by multiple inputs");
            }

            if (input.ButtonPin.HasValue && !usedPins.Add(input.ButtonPin.Value))
            {
                return (false, $"Duplicate pin assignment: Pin {input.ButtonPin} is used by multiple inputs");
            }
        }

        foreach (var display in config.Displays)
        {
            if (!usedPins.Add(display.CsPin))
            {
                return (false, $"Duplicate pin assignment: Pin {display.CsPin} is used by display CS and another component");
            }
        }

        return (true, null);
    }

    /// <summary>
    /// Adds a file to the recent configurations list
    /// </summary>
    private void AddToRecentConfigurations(string filePath)
    {
        var settings = SettingsService.Instance.Settings;

        // Remove if already exists
        settings.RecentConfigurations.Remove(filePath);

        // Add to front
        settings.RecentConfigurations.Insert(0, filePath);

        // Keep only last 10
        while (settings.RecentConfigurations.Count > 10)
        {
            settings.RecentConfigurations.RemoveAt(settings.RecentConfigurations.Count - 1);
        }

        settings.LastOpenedConfiguration = filePath;
        SettingsService.Instance.SaveSettings();
    }

    /// <summary>
    /// Gets the list of recent configurations that still exist
    /// </summary>
    public List<(string Path, string Name)> GetRecentConfigurations()
    {
        var recent = new List<(string, string)>();
        var settings = SettingsService.Instance.Settings;
        var toRemove = new List<string>();

        foreach (var path in settings.RecentConfigurations)
        {
            if (File.Exists(path))
            {
                var name = Path.GetFileNameWithoutExtension(path);
                recent.Add((path, name));
            }
            else
            {
                toRemove.Add(path);
            }
        }

        // Clean up non-existent files
        if (toRemove.Count > 0)
        {
            foreach (var path in toRemove)
            {
                settings.RecentConfigurations.Remove(path);
            }
            SettingsService.Instance.SaveSettings();
        }

        return recent;
    }

    #region Input Management

    public void AddInput(InputConfiguration input)
    {
        CurrentConfiguration.Inputs.Add(input);
        MarkAsModified();
    }

    public void UpdateInput(InputConfiguration input)
    {
        var index = CurrentConfiguration.Inputs.FindIndex(i => i.Id == input.Id);
        if (index >= 0)
        {
            CurrentConfiguration.Inputs[index] = input;
            MarkAsModified();
        }
    }

    public void RemoveInput(string inputId)
    {
        CurrentConfiguration.Inputs.RemoveAll(i => i.Id == inputId);
        // Also remove related output mappings
        CurrentConfiguration.OutputMappings.RemoveAll(m => m.InputId == inputId);
        MarkAsModified();
    }

    #endregion

    #region Display Management

    public void AddDisplay(DisplayConfiguration display)
    {
        CurrentConfiguration.Displays.Add(display);
        MarkAsModified();
    }

    public void UpdateDisplay(DisplayConfiguration display)
    {
        var index = CurrentConfiguration.Displays.FindIndex(d => d.Id == display.Id);
        if (index >= 0)
        {
            CurrentConfiguration.Displays[index] = display;
            MarkAsModified();
        }
    }

    public void RemoveDisplay(string displayId)
    {
        CurrentConfiguration.Displays.RemoveAll(d => d.Id == displayId);
        MarkAsModified();
    }

    #endregion

    #region Output Mapping Management

    public void SetOutputMapping(OutputMapping mapping)
    {
        var index = CurrentConfiguration.OutputMappings.FindIndex(m => m.InputId == mapping.InputId);
        if (index >= 0)
        {
            CurrentConfiguration.OutputMappings[index] = mapping;
        }
        else
        {
            CurrentConfiguration.OutputMappings.Add(mapping);
        }
        MarkAsModified();
    }

    public void RemoveOutputMapping(string inputId)
    {
        CurrentConfiguration.OutputMappings.RemoveAll(m => m.InputId == inputId);
        MarkAsModified();
    }

    #endregion
}
