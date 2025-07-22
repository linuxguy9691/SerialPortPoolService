// SerialPortPool.Core/Models/DeviceGroup.cs - ÉTAPE 5 NEW
namespace SerialPortPool.Core.Models;

/// <summary>
/// Represents a group of serial ports that belong to the same physical device
/// For example, an FT4232H chip appears as 4 separate COM ports but is one physical device
/// </summary>
public class DeviceGroup
{
    /// <summary>
    /// Unique identifier for this physical device
    /// </summary>
    public string DeviceId { get; set; } = string.Empty;

    /// <summary>
    /// Serial number of the physical device (shared across all ports)
    /// </summary>
    public string SerialNumber { get; set; } = string.Empty;

    /// <summary>
    /// FTDI device information (null for non-FTDI devices)
    /// </summary>
    public FtdiDeviceInfo? DeviceInfo { get; set; }

    /// <summary>
    /// All serial ports that belong to this physical device
    /// </summary>
    public List<SerialPortInfo> Ports { get; set; } = new();

    /// <summary>
    /// Whether this device has multiple ports (like FT4232H, FT2232H)
    /// </summary>
    public bool IsMultiPortDevice { get; set; }

    /// <summary>
    /// Shared system information for this device (read once, applies to all ports)
    /// </summary>
    public SystemInfo? SharedSystemInfo { get; set; }

    /// <summary>
    /// When this device group was created/last updated
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    /// <summary>
    /// Number of ports in this device
    /// </summary>
    public int PortCount => Ports?.Count ?? 0;

    /// <summary>
    /// Number of available (unallocated) ports in this device
    /// </summary>
    public int AvailablePortCount => Ports?.Count(p => p.Status == PortStatus.Available) ?? 0;

    /// <summary>
    /// Number of allocated ports in this device
    /// </summary>
    public int AllocatedPortCount => Ports?.Count(p => p.Status == PortStatus.Allocated) ?? 0;

    /// <summary>
    /// Utilization percentage of this device (allocated ports / total ports * 100)
    /// </summary>
    public double UtilizationPercentage => PortCount > 0 ? (AllocatedPortCount * 100.0 / PortCount) : 0;

    /// <summary>
    /// Whether this device has any available ports
    /// </summary>
    public bool HasAvailablePorts => AvailablePortCount > 0;

    /// <summary>
    /// Whether this device is fully allocated (all ports in use)
    /// </summary>
    public bool IsFullyAllocated => PortCount > 0 && AllocatedPortCount == PortCount;

    /// <summary>
    /// Whether this is an FTDI device
    /// </summary>
    public bool IsFtdiDevice => DeviceInfo != null;

    /// <summary>
    /// Whether this is a client-valid device (FTDI 4232H)
    /// </summary>
    public bool IsClientValidDevice => DeviceInfo?.Is4232H ?? false;

    /// <summary>
    /// Human-readable device type description
    /// </summary>
    public string DeviceTypeDescription
    {
        get
        {
            if (DeviceInfo != null)
            {
                var portCount = IsMultiPortDevice ? $" ({PortCount} ports)" : "";
                return $"FTDI {DeviceInfo.ChipType}{portCount}";
            }
            
            return $"Non-FTDI Device ({PortCount} port{(PortCount != 1 ? "s" : "")})";
        }
    }

    /// <summary>
    /// Get all port names in this device group
    /// </summary>
    /// <returns>List of port names (e.g., ["COM3", "COM4", "COM5", "COM6"])</returns>
    public List<string> GetPortNames()
    {
        return Ports?.Select(p => p.PortName).OrderBy(name => name).ToList() ?? new List<string>();
    }

    /// <summary>
    /// Get all available ports in this device group
    /// </summary>
    /// <returns>Ports that are available for allocation</returns>
    public List<SerialPortInfo> GetAvailablePorts()
    {
        return Ports?.Where(p => p.Status == PortStatus.Available).OrderBy(p => p.PortName).ToList() ?? new List<SerialPortInfo>();
    }

    /// <summary>
    /// Get all allocated ports in this device group
    /// </summary>
    /// <returns>Ports that are currently allocated</returns>
    public List<SerialPortInfo> GetAllocatedPorts()
    {
        return Ports?.Where(p => p.Status == PortStatus.Allocated).OrderBy(p => p.PortName).ToList() ?? new List<SerialPortInfo>();
    }

    /// <summary>
    /// Find a port by name in this device group
    /// </summary>
    /// <param name="portName">Port name to find</param>
    /// <returns>Port info if found, null otherwise</returns>
    public SerialPortInfo? FindPort(string portName)
    {
        return Ports?.FirstOrDefault(p => p.PortName.Equals(portName, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Update the status of a port in this device group
    /// </summary>
    /// <param name="portName">Port name to update</param>
    /// <param name="newStatus">New status</param>
    /// <returns>True if port was found and updated, false otherwise</returns>
    public bool UpdatePortStatus(string portName, PortStatus newStatus)
    {
        var port = FindPort(portName);
        if (port != null)
        {
            port.Status = newStatus;
            return true;
        }
        return false;
    }

    /// <summary>
    /// Get a summary of this device group for logging/display
    /// </summary>
    /// <returns>Formatted summary string</returns>
    public string GetSummary()
    {
        var deviceType = IsFtdiDevice ? DeviceInfo!.ChipType : "Non-FTDI";
        var clientValid = IsClientValidDevice ? "✅" : "❌";
        var utilization = $"{UtilizationPercentage:F0}%";
        var portRange = GetPortNames().Any() ? 
            $"{GetPortNames().First()}-{GetPortNames().Last()}" : 
            "No ports";

        return $"{clientValid} {deviceType} ({portRange}) - {AllocatedPortCount}/{PortCount} used ({utilization})";
    }

    /// <summary>
    /// Create a device group for a single port (non-multi-port device)
    /// </summary>
    /// <param name="port">Single port</param>
    /// <returns>Device group containing only this port</returns>
    public static DeviceGroup CreateSinglePortGroup(SerialPortInfo port)
    {
        var deviceGroup = new DeviceGroup
        {
            DeviceId = $"SINGLE_{port.PortName}",
            SerialNumber = port.FtdiInfo?.SerialNumber ?? port.PortName,
            DeviceInfo = port.FtdiInfo,
            Ports = new List<SerialPortInfo> { port },
            IsMultiPortDevice = false
        };

        return deviceGroup;
    }

    /// <summary>
    /// Create a device group for multiple ports from the same device
    /// </summary>
    /// <param name="deviceId">Device identifier</param>
    /// <param name="ports">Ports belonging to this device</param>
    /// <returns>Device group containing all ports</returns>
    public static DeviceGroup CreateMultiPortGroup(string deviceId, List<SerialPortInfo> ports)
    {
        if (!ports.Any())
            throw new ArgumentException("Cannot create device group with no ports", nameof(ports));

        var representativePort = ports.First();
        var deviceGroup = new DeviceGroup
        {
            DeviceId = deviceId,
            SerialNumber = representativePort.FtdiInfo?.SerialNumber ?? deviceId,
            DeviceInfo = representativePort.FtdiInfo,
            Ports = ports.OrderBy(p => p.PortName).ToList(),
            IsMultiPortDevice = ports.Count > 1 || (representativePort.FtdiInfo?.IsMultiPortDevice ?? false)
        };

        return deviceGroup;
    }

    public override string ToString()
    {
        return GetSummary();
    }

    public override bool Equals(object? obj)
    {
        return obj is DeviceGroup other && DeviceId == other.DeviceId;
    }

    public override int GetHashCode()
    {
        return DeviceId.GetHashCode();
    }
}