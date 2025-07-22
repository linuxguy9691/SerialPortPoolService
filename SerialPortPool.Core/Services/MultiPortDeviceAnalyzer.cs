// SerialPortPool.Core/Services/MultiPortDeviceAnalyzer.cs - ÉTAPE 5 NEW
using Microsoft.Extensions.Logging;
using SerialPortPool.Core.Interfaces;
using SerialPortPool.Core.Models;

namespace SerialPortPool.Core.Services;

/// <summary>
/// Service for analyzing and grouping multi-port FTDI devices (ÉTAPE 5)
/// Detects when multiple serial ports belong to the same physical device (e.g., FT4232H = 4 ports)
/// </summary>
public class MultiPortDeviceAnalyzer : IMultiPortDeviceAnalyzer
{
    private readonly ILogger<MultiPortDeviceAnalyzer> _logger;
    private readonly IFtdiDeviceReader _ftdiReader;

    public MultiPortDeviceAnalyzer(
        ILogger<MultiPortDeviceAnalyzer> logger,
        IFtdiDeviceReader ftdiReader)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _ftdiReader = ftdiReader ?? throw new ArgumentNullException(nameof(ftdiReader));
    }

    /// <summary>
    /// Analyze discovered ports to identify multi-port devices (FT4232H = 4 ports)
    /// </summary>
    public async Task<IEnumerable<DeviceGroup>> AnalyzeDeviceGroupsAsync(IEnumerable<SerialPortInfo> ports)
    {
        if (ports == null || !ports.Any())
        {
            _logger.LogDebug("No ports provided for device grouping analysis");
            return Enumerable.Empty<DeviceGroup>();
        }

        try
        {
            _logger.LogDebug($"Analyzing {ports.Count()} ports for multi-port device grouping");

            // Group ports by physical device using serial numbers and device properties
            var portGroups = GroupPortsByDevice(ports);
            var deviceGroups = new List<DeviceGroup>();

            foreach (var group in portGroups)
            {
                var deviceGroup = await CreateDeviceGroupAsync(group.Key, group.Value);
                if (deviceGroup != null)
                {
                    deviceGroups.Add(deviceGroup);
                    
                    var deviceType = deviceGroup.IsMultiPortDevice ? "Multi-port" : "Single-port";
                    _logger.LogInformation($"Device group created: {deviceGroup.DeviceId} - {deviceType} device with {deviceGroup.Ports.Count} port(s)");
                }
            }

            _logger.LogInformation($"Device grouping complete: {deviceGroups.Count} physical devices, {deviceGroups.Sum(g => g.Ports.Count)} total ports");
            return deviceGroups;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during device grouping analysis");
            return Enumerable.Empty<DeviceGroup>();
        }
    }

    /// <summary>
    /// Group ports by physical device using serial numbers and device IDs
    /// </summary>
    public Dictionary<string, List<SerialPortInfo>> GroupPortsByDevice(IEnumerable<SerialPortInfo> ports)
    {
        var groups = new Dictionary<string, List<SerialPortInfo>>();

        foreach (var port in ports)
        {
            var deviceKey = GetDeviceGroupingKey(port);
            
            if (!groups.ContainsKey(deviceKey))
            {
                groups[deviceKey] = new List<SerialPortInfo>();
            }
            
            groups[deviceKey].Add(port);
        }

        _logger.LogDebug($"Port grouping result: {groups.Count} device groups from {ports.Count()} ports");
        return groups;
    }

    /// <summary>
    /// Get device grouping key for identifying ports that belong to the same physical device
    /// </summary>
    private string GetDeviceGroupingKey(SerialPortInfo port)
    {
        // For FTDI devices, use serial number as primary grouping key
        if (port.IsFtdiDevice && port.FtdiInfo != null && !string.IsNullOrEmpty(port.FtdiInfo.SerialNumber))
        {
            return $"FTDI_{port.FtdiInfo.SerialNumber}";
        }

        // For non-FTDI or devices without serial number, use device ID or port name
        if (!string.IsNullOrEmpty(port.DeviceId))
        {
            // Extract base device ID (remove port-specific suffixes)
            var baseDeviceId = ExtractBaseDeviceId(port.DeviceId);
            return $"DEVICE_{baseDeviceId}";
        }

        // Fallback: treat each port as separate device
        return $"SINGLE_{port.PortName}";
    }

    /// <summary>
    /// Extract base device identifier from device ID string
    /// </summary>
    private string ExtractBaseDeviceId(string deviceId)
    {
        if (string.IsNullOrEmpty(deviceId))
            return "UNKNOWN";

        // For FTDI devices: FTDIBUS\VID_0403+PID_6011+SERIAL\0000 -> VID_0403+PID_6011+SERIAL
        if (deviceId.Contains("FTDIBUS"))
        {
            var match = System.Text.RegularExpressions.Regex.Match(deviceId, @"VID_[0-9A-Fa-f]{4}\+PID_[0-9A-Fa-f]{4}\+[^\\]+");
            if (match.Success)
            {
                return match.Value;
            }
        }

        // For other devices, use first part before instance identifier
        var parts = deviceId.Split('\\');
        return parts.Length > 1 ? parts[1] : deviceId;
    }

    /// <summary>
    /// Create a DeviceGroup from grouped ports
    /// </summary>
    private async Task<DeviceGroup?> CreateDeviceGroupAsync(string deviceKey, List<SerialPortInfo> ports)
    {
        if (!ports.Any())
            return null;

        try
        {
            // Use first port to get device information (all ports should have same device info)
            var representativePort = ports.OrderBy(p => p.PortName).First();
            var ftdiInfo = representativePort.FtdiInfo;

            var deviceGroup = new DeviceGroup
            {
                DeviceId = deviceKey,
                Ports = ports.OrderBy(p => p.PortName).ToList(), // Sort ports for consistency
                IsMultiPortDevice = DetermineIfMultiPortDevice(ports, ftdiInfo)
            };

            // Set device-level information
            if (ftdiInfo != null)
            {
                deviceGroup.SerialNumber = ftdiInfo.SerialNumber;
                deviceGroup.DeviceInfo = ftdiInfo;
                
                // Read shared EEPROM data for the device (using existing interface method)
                try
                {
                    var eepromData = await _ftdiReader.ReadEepromDataAsync(representativePort.PortName);
                    if (eepromData.Any())
                    {
                        // Create SystemInfo from EEPROM data
                        var systemInfo = new SystemInfo
                        {
                            SerialNumber = ftdiInfo.SerialNumber,
                            Manufacturer = ftdiInfo.Manufacturer,
                            ProductDescription = ftdiInfo.ProductDescription,
                            EepromData = eepromData,
                            LastRead = DateTime.Now,
                            IsDataValid = true
                        };
                        
                        deviceGroup.SharedSystemInfo = systemInfo;
                        _logger.LogDebug($"System info created for device group {deviceKey} with {eepromData.Count} EEPROM entries");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, $"Could not read EEPROM data for device group {deviceKey}");
                }
            }
            else
            {
                deviceGroup.SerialNumber = $"NonFTDI_{representativePort.PortName}";
            }

            return deviceGroup;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error creating device group for {deviceKey}");
            return null;
        }
    }

    /// <summary>
    /// Determine if this is a multi-port device based on chip type and port count
    /// </summary>
    private bool DetermineIfMultiPortDevice(List<SerialPortInfo> ports, FtdiDeviceInfo? ftdiInfo)
    {
        // Multiple ports with same device ID = multi-port device
        if (ports.Count > 1)
            return true;

        // Single port but known multi-port chip type
        if (ftdiInfo != null)
        {
            return ftdiInfo.IsMultiPortDevice; // Uses existing property from FtdiDeviceInfo
        }

        return false;
    }

    /// <summary>
    /// Get statistics about device grouping
    /// </summary>
    public DeviceGroupingStatistics GetGroupingStatistics(IEnumerable<DeviceGroup> deviceGroups)
    {
        var groups = deviceGroups.ToList();
        
        return new DeviceGroupingStatistics
        {
            TotalDevices = groups.Count,
            TotalPorts = groups.Sum(g => g.Ports.Count),
            MultiPortDevices = groups.Count(g => g.IsMultiPortDevice),
            SinglePortDevices = groups.Count(g => !g.IsMultiPortDevice),
            AveragePortsPerDevice = groups.Any() ? groups.Average(g => g.Ports.Count) : 0,
            LargestDevicePortCount = groups.Any() ? groups.Max(g => g.Ports.Count) : 0,
            FtdiDevices = groups.Count(g => g.DeviceInfo != null),
            NonFtdiDevices = groups.Count(g => g.DeviceInfo == null)
        };
    }
}

/// <summary>
/// Statistics about device grouping results
/// </summary>
public class DeviceGroupingStatistics
{
    public int TotalDevices { get; set; }
    public int TotalPorts { get; set; }
    public int MultiPortDevices { get; set; }
    public int SinglePortDevices { get; set; }
    public double AveragePortsPerDevice { get; set; }
    public int LargestDevicePortCount { get; set; }
    public int FtdiDevices { get; set; }
    public int NonFtdiDevices { get; set; }

    public override string ToString()
    {
        return $"Device Grouping: {TotalDevices} devices, {TotalPorts} ports, {MultiPortDevices} multi-port, {FtdiDevices} FTDI";
    }
}