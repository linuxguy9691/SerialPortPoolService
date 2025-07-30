// SerialPortPool.Core/Models/ProtocolSession.cs - Sprint 5 Week 1 COMPLETION
namespace SerialPortPool.Core.Models;

/// <summary>
/// Represents an active protocol communication session
/// Sprint 5: RS232 support with NativeHandle for SerialPort
/// Sprint 6: Extended for RS485, USB, CAN, I2C, SPI protocols
/// </summary>
public class ProtocolSession : IDisposable
{
    /// <summary>
    /// Unique session identifier
    /// </summary>
    public string SessionId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Protocol name (e.g., "RS232", "RS485", "USB", "CAN", "I2C", "SPI")
    /// </summary>
    public string ProtocolName { get; set; } = string.Empty;

    /// <summary>
    /// Physical port name (e.g., "COM11", "COM6")
    /// </summary>
    public string PortName { get; set; } = string.Empty;

    /// <summary>
    /// Protocol-specific native handle
    /// RS232: System.IO.Ports.SerialPort
    /// RS485: System.IO.Ports.SerialPort (with RS485 settings)
    /// USB: USB device handle
    /// CAN: CAN interface handle
    /// I2C: I2C device handle
    /// SPI: SPI device handle
    /// </summary>
    public object? NativeHandle { get; set; }

    /// <summary>
    /// Original configuration used to create this session
    /// </summary>
    public ProtocolConfiguration? Configuration { get; set; }

    /// <summary>
    /// When this session was opened
    /// </summary>
    public DateTime OpenedAt { get; set; } = DateTime.Now;

    /// <summary>
    /// When this session was last used for communication
    /// </summary>
    public DateTime LastUsedAt { get; set; } = DateTime.Now;

    /// <summary>
    /// Whether this session is currently active and available for communication
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Total number of commands executed in this session
    /// </summary>
    public int CommandCount { get; set; } = 0;

    /// <summary>
    /// Number of successful commands in this session
    /// </summary>
    public int SuccessfulCommands { get; set; } = 0;

    /// <summary>
    /// Number of failed commands in this session
    /// </summary>
    public int FailedCommands { get; set; } = 0;

    /// <summary>
    /// Optional client identifier who owns this session
    /// </summary>
    public string? ClientId { get; set; }

    /// <summary>
    /// Session metadata for tracking and debugging
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// Error message if session is in error state
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Whether this session has been disposed
    /// </summary>
    public bool IsDisposed { get; private set; }

    #region Computed Properties

    /// <summary>
    /// How long this session has been active
    /// </summary>
    public TimeSpan SessionDuration => DateTime.Now - OpenedAt;

    /// <summary>
    /// How long since this session was last used
    /// </summary>
    public TimeSpan IdleTime => DateTime.Now - LastUsedAt;

    /// <summary>
    /// Success rate percentage for commands in this session
    /// </summary>
    public double SuccessRate => CommandCount > 0 ? (double)SuccessfulCommands / CommandCount * 100.0 : 0.0;

    /// <summary>
    /// Whether this session is idle (not used for more than 5 minutes)
    /// </summary>
    public bool IsIdle => IdleTime.TotalMinutes > 5;

    /// <summary>
    /// Whether this session is healthy (active and no errors)
    /// </summary>
    public bool IsHealthy => IsActive && string.IsNullOrEmpty(ErrorMessage) && !IsDisposed;

    #endregion

    #region Command Tracking

    /// <summary>
    /// Record a successful command execution
    /// </summary>
    public void RecordSuccessfulCommand()
    {
        CommandCount++;
        SuccessfulCommands++;
        LastUsedAt = DateTime.Now;
    }

    /// <summary>
    /// Record a failed command execution
    /// </summary>
    /// <param name="errorMessage">Optional error message</param>
    public void RecordFailedCommand(string? errorMessage = null)
    {
        CommandCount++;
        FailedCommands++;
        LastUsedAt = DateTime.Now;
        
        if (!string.IsNullOrEmpty(errorMessage))
        {
            ErrorMessage = errorMessage;
        }
    }

    /// <summary>
    /// Mark session as having an error
    /// </summary>
    /// <param name="errorMessage">Error description</param>
    public void SetError(string errorMessage)
    {
        IsActive = false;
        ErrorMessage = errorMessage;
        Metadata["ErrorAt"] = DateTime.Now.ToString("O");
    }

    /// <summary>
    /// Clear error state and reactivate session
    /// </summary>
    public void ClearError()
    {
        IsActive = true;
        ErrorMessage = null;
        Metadata.Remove("ErrorAt");
    }

    #endregion

    #region Factory Methods

    /// <summary>
    /// Create a new protocol session for RS232
    /// </summary>
    /// <param name="portName">COM port name</param>
    /// <param name="serialPort">Opened SerialPort instance</param>
    /// <param name="config">Protocol configuration</param>
    /// <param name="clientId">Optional client identifier</param>
    /// <returns>New RS232 protocol session</returns>
    public static ProtocolSession CreateRS232Session(
        string portName, 
        System.IO.Ports.SerialPort serialPort, 
        ProtocolConfiguration config,
        string? clientId = null)
    {
        return new ProtocolSession
        {
            SessionId = Guid.NewGuid().ToString(),
            ProtocolName = "RS232",
            PortName = portName,
            NativeHandle = serialPort,
            Configuration = config,
            ClientId = clientId,
            OpenedAt = DateTime.Now,
            LastUsedAt = DateTime.Now,
            IsActive = true
        };
    }

    /// <summary>
    /// Create a protocol session for any protocol type
    /// </summary>
    /// <param name="protocolName">Protocol name</param>
    /// <param name="portName">Port name</param>
    /// <param name="nativeHandle">Protocol-specific handle</param>
    /// <param name="config">Protocol configuration</param>
    /// <param name="clientId">Optional client identifier</param>
    /// <returns>New protocol session</returns>
    public static ProtocolSession Create(
        string protocolName,
        string portName,
        object? nativeHandle,
        ProtocolConfiguration? config = null,
        string? clientId = null)
    {
        return new ProtocolSession
        {
            SessionId = Guid.NewGuid().ToString(),
            ProtocolName = protocolName,
            PortName = portName,
            NativeHandle = nativeHandle,
            Configuration = config,
            ClientId = clientId,
            OpenedAt = DateTime.Now,
            LastUsedAt = DateTime.Now,
            IsActive = true
        };
    }

    #endregion

    #region Display and Debug

    /// <summary>
    /// Get a summary of this protocol session for logging/display
    /// </summary>
    /// <returns>Formatted summary string</returns>
    public string GetSummary()
    {
        var status = IsHealthy ? "✅ HEALTHY" : !IsActive ? "❌ INACTIVE" : "⚠️ ERROR";
        var commands = CommandCount > 0 ? $"{SuccessfulCommands}/{CommandCount} cmds ({SuccessRate:F1}%)" : "No commands";
        var duration = SessionDuration.TotalMinutes > 1 ? $"{SessionDuration.TotalMinutes:F1}min" : $"{SessionDuration.TotalSeconds:F0}s";
        
        return $"{status}: {ProtocolName} on {PortName} - {commands}, {duration} active";
    }

    /// <summary>
    /// Get detailed session information for debugging
    /// </summary>
    /// <returns>Detailed session information</returns>
    public string GetDetailedInfo()
    {
        var lines = new List<string>
        {
            $"Protocol Session: {SessionId}",
            $"  Protocol: {ProtocolName}",
            $"  Port: {PortName}",
            $"  Status: {(IsHealthy ? "Healthy" : "Unhealthy")}",
            $"  Active: {IsActive}",
            $"  Client: {ClientId ?? "None"}",
            $"  Opened: {OpenedAt:yyyy-MM-dd HH:mm:ss}",
            $"  Duration: {SessionDuration}",
            $"  Last Used: {LastUsedAt:yyyy-MM-dd HH:mm:ss} ({IdleTime} ago)",
            $"  Commands: {CommandCount} total, {SuccessfulCommands} success, {FailedCommands} failed",
            $"  Success Rate: {SuccessRate:F1}%"
        };

        if (!string.IsNullOrEmpty(ErrorMessage))
        {
            lines.Add($"  Error: {ErrorMessage}");
        }

        if (Metadata.Any())
        {
            lines.Add($"  Metadata: {string.Join(", ", Metadata.Select(kvp => $"{kvp.Key}={kvp.Value}"))}");
        }

        return string.Join(Environment.NewLine, lines);
    }

    public override string ToString()
    {
        return GetSummary();
    }

    #endregion

    #region IDisposable Implementation

    /// <summary>
    /// Dispose the protocol session and clean up native resources
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Protected dispose method
    /// </summary>
    /// <param name="disposing">Whether disposing from Dispose() call</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!IsDisposed && disposing)
        {
            try
            {
                // Close protocol-specific native handle
                if (NativeHandle != null)
                {
                    switch (NativeHandle)
                    {
                        case System.IO.Ports.SerialPort serialPort when serialPort.IsOpen:
                            serialPort.Close();
                            serialPort.Dispose();
                            break;
                        case IDisposable disposable:
                            disposable.Dispose();
                            break;
                    }
                }

                IsActive = false;
                NativeHandle = null;
                Metadata["DisposedAt"] = DateTime.Now.ToString("O");
            }
            catch (Exception)
            {
                // Ignore disposal errors - session is being closed anyway
            }
            finally
            {
                IsDisposed = true;
            }
        }
    }

    /// <summary>
    /// Finalizer for protocol session
    /// </summary>
    ~ProtocolSession()
    {
        Dispose(false);
    }

    #endregion

    #region Equality and Comparison

    public override bool Equals(object? obj)
    {
        return obj is ProtocolSession other && SessionId == other.SessionId;
    }

    public override int GetHashCode()
    {
        return SessionId.GetHashCode();
    }

    #endregion
}