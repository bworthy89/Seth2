namespace ArduinoConfigApp.Models;

public enum BoardType
{
    Unknown,
    ProMicro,
    Mega2560
}

public enum ConnectionStatus
{
    Disconnected,
    Connected,
    PortBusy,
    Error
}

public class ArduinoBoard
{
    public string PortName { get; set; } = string.Empty;
    public BoardType BoardType { get; set; } = BoardType.Unknown;
    public ConnectionStatus Status { get; set; } = ConnectionStatus.Disconnected;
    public string? VendorId { get; set; }
    public string? ProductId { get; set; }
    public string? Description { get; set; }
    public string? ErrorMessage { get; set; }

    public string BoardTypeName => BoardType switch
    {
        BoardType.ProMicro => "Arduino Pro Micro",
        BoardType.Mega2560 => "Arduino Mega 2560",
        _ => "Unknown Board"
    };

    public string StatusText => Status switch
    {
        ConnectionStatus.Connected => "Connected",
        ConnectionStatus.Disconnected => "Disconnected",
        ConnectionStatus.PortBusy => "Port Busy",
        ConnectionStatus.Error => ErrorMessage ?? "Error",
        _ => "Unknown"
    };

    // Pin information based on board type
    public int[] AvailableDigitalPins => BoardType switch
    {
        BoardType.ProMicro => new[] { 2, 3, 4, 5, 6, 7, 8, 9, 10, 14, 15, 16, 18, 19, 20, 21 },
        BoardType.Mega2560 => Enumerable.Range(2, 52).ToArray(), // 2-53
        _ => Array.Empty<int>()
    };

    public (int MISO, int MOSI, int SCK, int SS) SpiPins => BoardType switch
    {
        BoardType.ProMicro => (14, 16, 15, 10),
        BoardType.Mega2560 => (50, 51, 52, 53),
        _ => (0, 0, 0, 0)
    };
}
