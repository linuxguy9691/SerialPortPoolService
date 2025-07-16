namespace SerialPortPool.Core.Models;

/// <summary>
/// Information about a serial port in the system
/// </summary>
public class SerialPortInfo
{
    /// <summary>
    /// The port name (e.g., "COM3", "COM10")
    /// </summary>
    public string PortName { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable friendly name from device manager
    /// </summary>
    public string FriendlyName { get; set; } = string.Empty;

    /// <summary>
    /// Current status of the port
    /// </summary>
    public PortStatus Status { get; set; } = PortStatus.Unknown;

    /// <summary>
    /// Last time this port was seen/updated
    /// </summary>
    public DateTime LastSeen { get; set; } = DateTime.Now;

    /// <summary>
    /// Hardware device identifier
    /// </summary>
    public string DeviceId { get; set; } = string.Empty;

    /// <summary>
    /// Whether this port is suitable for the pool (will be set by validation)
    /// </summary>
    public bool IsValidForPool { get; set; } = false;

    public override string ToString()
    {
        return $"{PortName} - {FriendlyName} ({Status})";
    }

    public override bool Equals(object? obj)
    {
        return obj is SerialPortInfo other && PortName == other.PortName;
    }

    public override int GetHashCode()
    {
        return PortName.GetHashCode();
    }
}
