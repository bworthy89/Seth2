using System.IO.Ports;
using System.Management;
using ArduinoConfigApp.Models;

namespace ArduinoConfigApp.Services;

public class SerialPortService : IDisposable
{
    private static SerialPortService? _instance;
    public static SerialPortService Instance => _instance ??= new SerialPortService();

    private readonly System.Timers.Timer _pollTimer;
    private readonly List<ArduinoBoard> _detectedBoards = new();
    private SerialPort? _activeConnection;
    private bool _isDisposed;

    // Known Arduino VID/PID combinations
    private static readonly (string Vid, string Pid, BoardType Type)[] KnownBoards = new[]
    {
        // Arduino Pro Micro (SparkFun)
        ("1B4F", "9205", BoardType.ProMicro),
        ("1B4F", "9206", BoardType.ProMicro),
        // Arduino Pro Micro (Arduino/Genuino)
        ("2341", "8036", BoardType.ProMicro),
        ("2341", "8037", BoardType.ProMicro),
        // Arduino Leonardo (similar to Pro Micro)
        ("2341", "0036", BoardType.ProMicro),
        ("2341", "8036", BoardType.ProMicro),
        // Arduino Mega 2560
        ("2341", "0042", BoardType.Mega2560),
        ("2341", "0010", BoardType.Mega2560),
        ("2341", "0242", BoardType.Mega2560),
        // Chinese clones (CH340)
        ("1A86", "7523", BoardType.Unknown), // CH340 - could be any board
    };

    public event EventHandler<List<ArduinoBoard>>? BoardsChanged;
    public event EventHandler<ArduinoBoard>? BoardConnected;
    public event EventHandler<ArduinoBoard>? BoardDisconnected;

    public IReadOnlyList<ArduinoBoard> DetectedBoards => _detectedBoards.AsReadOnly();
    public ArduinoBoard? ActiveBoard { get; private set; }
    public bool IsConnected => _activeConnection?.IsOpen ?? false;

    private SerialPortService()
    {
        _pollTimer = new System.Timers.Timer(2000); // Poll every 2 seconds
        _pollTimer.Elapsed += (s, e) => RefreshPorts();
        _pollTimer.AutoReset = true;
    }

    public void StartMonitoring()
    {
        RefreshPorts();
        _pollTimer.Start();
    }

    public void StopMonitoring()
    {
        _pollTimer.Stop();
    }

    public void RefreshPorts()
    {
        try
        {
            var newBoards = DetectArduinoBoards();
            var changed = false;

            // Check for disconnected boards
            var disconnected = _detectedBoards
                .Where(b => !newBoards.Any(nb => nb.PortName == b.PortName))
                .ToList();

            foreach (var board in disconnected)
            {
                _detectedBoards.Remove(board);
                changed = true;
                BoardDisconnected?.Invoke(this, board);
            }

            // Check for new boards
            foreach (var board in newBoards)
            {
                var existing = _detectedBoards.FirstOrDefault(b => b.PortName == board.PortName);
                if (existing == null)
                {
                    _detectedBoards.Add(board);
                    changed = true;
                    BoardConnected?.Invoke(this, board);
                }
                else if (existing.Status != board.Status)
                {
                    existing.Status = board.Status;
                    changed = true;
                }
            }

            if (changed)
            {
                BoardsChanged?.Invoke(this, _detectedBoards.ToList());
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error refreshing ports: {ex.Message}");
        }
    }

    private List<ArduinoBoard> DetectArduinoBoards()
    {
        var boards = new List<ArduinoBoard>();

        try
        {
            // Use WMI to get detailed port information
            using var searcher = new ManagementObjectSearcher(
                "SELECT * FROM Win32_PnPEntity WHERE Caption LIKE '%(COM%'");

            foreach (var device in searcher.Get())
            {
                var caption = device["Caption"]?.ToString() ?? "";
                var deviceId = device["DeviceID"]?.ToString() ?? "";
                var pnpDeviceId = device["PNPDeviceID"]?.ToString() ?? "";

                // Extract COM port from caption
                var portMatch = System.Text.RegularExpressions.Regex.Match(caption, @"\(COM(\d+)\)");
                if (!portMatch.Success) continue;

                var portName = $"COM{portMatch.Groups[1].Value}";

                // Extract VID and PID
                var vidPidMatch = System.Text.RegularExpressions.Regex.Match(
                    pnpDeviceId, @"VID_([0-9A-Fa-f]{4})&PID_([0-9A-Fa-f]{4})");

                string? vid = null;
                string? pid = null;
                var boardType = BoardType.Unknown;

                if (vidPidMatch.Success)
                {
                    vid = vidPidMatch.Groups[1].Value.ToUpper();
                    pid = vidPidMatch.Groups[2].Value.ToUpper();

                    // Match against known boards
                    var match = KnownBoards.FirstOrDefault(kb =>
                        kb.Vid.Equals(vid, StringComparison.OrdinalIgnoreCase) &&
                        kb.Pid.Equals(pid, StringComparison.OrdinalIgnoreCase));

                    if (match != default)
                    {
                        boardType = match.Type;
                    }
                    else
                    {
                        // Check if it's at least an Arduino VID
                        if (vid == "2341" || vid == "1B4F" || vid == "1A86")
                        {
                            boardType = BoardType.Unknown; // Arduino but unknown type
                        }
                        else
                        {
                            continue; // Not an Arduino, skip
                        }
                    }
                }
                else
                {
                    continue; // Can't identify, skip
                }

                // Check port status
                var status = ConnectionStatus.Disconnected;
                try
                {
                    using var testPort = new SerialPort(portName);
                    testPort.Open();
                    testPort.Close();
                    status = ConnectionStatus.Disconnected; // Available
                }
                catch (UnauthorizedAccessException)
                {
                    status = ConnectionStatus.PortBusy;
                }
                catch (Exception)
                {
                    status = ConnectionStatus.Error;
                }

                boards.Add(new ArduinoBoard
                {
                    PortName = portName,
                    BoardType = boardType,
                    Status = status,
                    VendorId = vid,
                    ProductId = pid,
                    Description = caption
                });
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error detecting boards: {ex.Message}");
        }

        return boards;
    }

    public async Task<bool> ConnectAsync(ArduinoBoard board, int baudRate = 115200)
    {
        try
        {
            if (_activeConnection?.IsOpen == true)
            {
                _activeConnection.Close();
            }

            _activeConnection = new SerialPort(board.PortName)
            {
                BaudRate = baudRate,
                DataBits = 8,
                Parity = Parity.None,
                StopBits = StopBits.One,
                ReadTimeout = 1000,
                WriteTimeout = 1000
            };

            _activeConnection.Open();

            // Small delay to let Arduino reset
            await Task.Delay(2000);

            ActiveBoard = board;
            board.Status = ConnectionStatus.Connected;
            BoardsChanged?.Invoke(this, _detectedBoards.ToList());

            return true;
        }
        catch (UnauthorizedAccessException)
        {
            board.Status = ConnectionStatus.PortBusy;
            board.ErrorMessage = "Port is in use by another application";
            return false;
        }
        catch (Exception ex)
        {
            board.Status = ConnectionStatus.Error;
            board.ErrorMessage = ex.Message;
            return false;
        }
    }

    public void Disconnect()
    {
        if (_activeConnection?.IsOpen == true)
        {
            _activeConnection.Close();
        }
        _activeConnection?.Dispose();
        _activeConnection = null;

        if (ActiveBoard != null)
        {
            ActiveBoard.Status = ConnectionStatus.Disconnected;
            ActiveBoard = null;
            BoardsChanged?.Invoke(this, _detectedBoards.ToList());
        }
    }

    public void Dispose()
    {
        if (_isDisposed) return;

        _isDisposed = true;
        _pollTimer.Stop();
        _pollTimer.Dispose();
        Disconnect();
    }
}
