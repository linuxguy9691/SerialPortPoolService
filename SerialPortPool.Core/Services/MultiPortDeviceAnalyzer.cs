// SerialPortPool.Core/Services/MultiPortDeviceAnalyzer.cs - FIXED grouping logic
using Microsoft.Extensions.Logging;
using SerialPortPool.Core.Interfaces;
using SerialPortPool.Core.Models;

namespace SerialPortPool.Core.Services;

/// <summary>
/// Service for analyzing and grouping multi-port FTDI devices (FIXED for FT4232HL)
/// Detects when multiple serial ports belong to the same physical device (e.g., FT4232HL = 4 ports)
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
    /// Analyze discovered ports to identify multi-port devices (FT4232HL = 4 ports)
    /// FIXED: Enhanced logging and improved grouping logic
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
            var portList = ports.ToList();
            _logger.LogDebug($"🔍 Analyzing {portList.Count} ports for multi-port device grouping");

            // Group ports by physical device using improved algorithm
            var portGroups = GroupPortsByDevice(portList);
            var deviceGroups = new List<DeviceGroup>();

            _logger.LogDebug($"📦 Found {portGroups.Count} potential device groups");

            foreach (var group in portGroups)
            {
                try
                {
                    var deviceGroup = await CreateDeviceGroupAsync(group.Key, group.Value);
                    if (deviceGroup != null)
                    {
                        deviceGroups.Add(deviceGroup);
                        
                        var deviceType = deviceGroup.IsMultiPortDevice ? "Multi-port" : "Single-port";
                        _logger.LogInformation($"✅ Device group created: {deviceGroup.DeviceId} - {deviceType} device with {deviceGroup.Ports.Count} port(s)");
                        
                        // Special logging for FT4232HL
                        if (deviceGroup.DeviceInfo?.ChipType == "FT4232HL")
                        {
                            _logger.LogInformation($"🎉 FT4232HL device detected with {deviceGroup.PortCount} ports: {string.Join(", ", deviceGroup.GetPortNames())}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"❌ Error creating device group for {group.Key}");
                }
            }

            _logger.LogInformation($"🎯 Device grouping complete: {deviceGroups.Count} physical devices, {deviceGroups.Sum(g => g.Ports.Count)} total ports");
            
            // Log summary statistics
            var multiPortCount = deviceGroups.Count(g => g.IsMultiPortDevice);
            var ftdiCount = deviceGroups.Count(g => g.IsFtdiDevice);
            var ft4232hlCount = deviceGroups.Count(g => g.DeviceInfo?.ChipType == "FT4232HL");
            
            _logger.LogInformation($"📊 Summary: {ftdiCount} FTDI devices, {multiPortCount} multi-port, {ft4232hlCount} FT4232HL");
            
            return deviceGroups;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "💥 Error during device grouping analysis");
            return Enumerable.Empty<DeviceGroup>();
        }
    }

    /// <summary>
    /// Group ports by physical device using serial numbers and device IDs
    /// ENHANCED: Improved algorithm with better logging and FT4232HL support
    /// </summary>
    public Dictionary<string, List<SerialPortInfo>> GroupPortsByDevice(IEnumerable<SerialPortInfo> ports)
    {
        var groups = new Dictionary<string, List<SerialPortInfo>>();
        var portList = ports.ToList();

        _logger.LogDebug($"🔍 Grouping {portList.Count} ports by device...");

        foreach (var port in portList)
        {
            var deviceKey = GetDeviceGroupingKey(port);
            
            if (!groups.ContainsKey(deviceKey))
            {
                groups[deviceKey] = new List<SerialPortInfo>();
                _logger.LogDebug($"📦 New group created: {deviceKey}");
            }
            
            groups[deviceKey].Add(port);
            _logger.LogDebug($"   ➕ Added {port.PortName} to group {deviceKey}");
        }

        // Log grouping results
        _logger.LogDebug($"📋 Port grouping result: {groups.Count} device groups");
        foreach (var group in groups)
        {
            var ports_str = string.Join(", ", group.Value.Select(p => p.PortName));
            _logger.LogDebug($"   📦 {group.Key}: {group.Value.Count} ports ({ports_str})");
            
            // Check for potential FT4232HL
            var ftdiPorts = group.Value.Where(p => p.IsFtdiDevice).ToList();
            if (ftdiPorts.Any(p => p.FtdiInfo?.ChipType == "FT4232HL"))
            {
                _logger.LogInformation($"🎯 FT4232HL detected in group {group.Key}: {ftdiPorts.Count} FTDI ports");
                
                if (ftdiPorts.Count != 4)
                {
                    _logger.LogWarning($"⚠️  FT4232HL group {group.Key} has {ftdiPorts.Count} ports, expected 4");
                }
            }
        }

        return groups;
    }

    /// <summary>
    /// Get device grouping key for identifying ports that belong to the same physical device
    /// ENHANCED: Better algorithm for FT4232HL and improved fallback logic
    /// </summary>
    private string GetDeviceGroupingKey(SerialPortInfo port)
    {
        // Priority 1: FTDI devices with serial number (most reliable)
        if (port.IsFtdiDevice && port.FtdiInfo != null && !string.IsNullOrEmpty(port.FtdiInfo.SerialNumber))
        {
            var key = $"FTDI_{port.FtdiInfo.SerialNumber}";
            _logger.LogDebug($"🔑 FTDI grouping key for {port.PortName}: {key}");
            return key;
        }

        // Priority 2: Use device ID for grouping (remove port-specific suffixes)
        if (!string.IsNullOrEmpty(port.DeviceId))
        {
            var baseDeviceId = ExtractBaseDeviceId(port.DeviceId);
            var key = $"DEVICE_{baseDeviceId}";
            _logger.LogDebug($"🔑 Device ID grouping key for {port.PortName}: {key}");
            return key;
        }

        // Fallback: treat each port as separate device
        var fallbackKey = $"SINGLE_{port.PortName}";
        _logger.LogDebug($"🔑 Fallback grouping key for {port.PortName}: {fallbackKey}");
        return fallbackKey;
    }

    /// <summary>
    /// Extract base device identifier from device ID string
    /// ENHANCED: Better parsing for FTDI device IDs
    /// </summary>
    private string ExtractBaseDeviceId(string deviceId)
    {
        if (string.IsNullOrEmpty(deviceId))
        {
            return "UNKNOWN";
        }

        // For FTDI devices: FTDIBUS\VID_0403+PID_6048+SERIAL\0000 -> VID_0403+PID_6048+SERIAL
        if (deviceId.Contains("FTDIBUS"))
        {
            // Enhanced regex to capture the full device identifier including serial
            var match = System.Text.RegularExpressions.Regex.Match(deviceId, @"VID_[0-9A-Fa-f]{4}\+PID_[0-9A-Fa-f]{4}\+[^\\]+");
            if (match.Success)
            {
                var result = match.Value;
                _logger.LogDebug($"🔍 Extracted FTDI base ID: {result} from {deviceId}");
                return result;
            }
        }

        // For other devices, use first part before instance identifier
        var parts = deviceId.Split('\\');
        var result_fallback = parts.Length > 1 ? parts[1] : deviceId;
        _logger.LogDebug($"🔍 Extracted generic base ID: {result_fallback} from {deviceId}");
        return result_fallback;
    }

    /// <summary>
    /// Create a DeviceGroup from grouped ports
    /// ENHANCED: Better SystemInfo handling and FT4232HL detection
    /// </summary>
    private async Task<DeviceGroup?> CreateDeviceGroupAsync(string deviceKey, List<SerialPortInfo> ports)
    {
        if (!ports.Any())
        {
            _logger.LogWarning($"⚠️  Cannot create device group {deviceKey} with no ports");
            return null;
        }

        try
        {
            _logger.LogDebug($"🏗️  Creating device group {deviceKey} with {ports.Count} ports");

            // Use first port to get device information (all ports should have same device info)
            var representativePort = ports.OrderBy(p => p.PortName).First();
            var ftdiInfo = representativePort.FtdiInfo;

            var deviceGroup = new DeviceGroup
            {
                DeviceId = deviceKey,
                Ports = ports.OrderBy(p => p.PortName).ToList(), // Sort ports for consistency
                IsMultiPortDevice = DetermineIfMultiPortDevice(ports, ftdiInfo),
                CreatedAt = DateTime.Now
            };

            // Set device-level information
            if (ftdiInfo != null)
            {
                deviceGroup.SerialNumber = ftdiInfo.SerialNumber;
                deviceGroup.DeviceInfo = ftdiInfo;
                
                _logger.LogDebug($"📱 FTDI device group: {ftdiInfo.ChipType}, Serial: {ftdiInfo.SerialNumber}");
                
                // Special handling for FT4232HL
                if (ftdiInfo.ChipType == "FT4232HL")
                {
                    _logger.LogInformation($"🎯 FT4232HL device group created: {ports.Count} ports");
                    
                    // Validate expected port count
                    if (ports.Count != 4)
                    {
                        _logger.LogWarning($"⚠️  FT4232HL device has {ports.Count} ports, expected 4. Possible driver/hardware issue.");
                    }
                }
                
                // Read shared EEPROM data for the device
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
                        _logger.LogDebug($"💾 System info created for device group {deviceKey} with {eepromData.Count} EEPROM entries");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, $"⚠️  Could not read EEPROM data for device group {deviceKey}");
                }
            }
            else
            {
                deviceGroup.SerialNumber = $"NonFTDI_{representativePort.PortName}";
                _logger.LogDebug($"📱 Non-FTDI device group: {deviceKey}");
            }

            _logger.LogInformation($"✅ Successfully created device group {deviceKey}: {deviceGroup.DeviceTypeDescription}");
            return deviceGroup;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"💥 Error creating device group for {deviceKey}");
            return null;
        }
    }

    /// <summary>
    /// Determine if this is a multi-port device based on chip type and port count
    /// ENHANCED: Better detection logic for FT4232HL and other multi-port devices
    /// </summary>
    private bool DetermineIfMultiPortDevice(List<SerialPortInfo> ports, FtdiDeviceInfo? ftdiInfo)
    {
        // Rule 1: Multiple ports with same device info = multi-port device
        if (ports.Count > 1)
        {
            _logger.LogDebug($"🔀 Multi-port device detected: {ports.Count} ports found");
            return true;
        }

        // Rule 2: Single port but known multi-port chip type
        if (ftdiInfo != null && ftdiInfo.IsMultiPortDevice)
        {
            _logger.LogDebug($"🔀 Multi-port chip detected: {ftdiInfo.ChipType} (even though only 1 port found)");
            
            // Special warning for FT4232HL with only 1 port found
            if (ftdiInfo.ChipType == "FT4232HL")
            {
                _logger.LogWarning($"⚠️  FT4232HL detected but only 1 port found - possible driver issue or other ports not connected");
            }
            
            return true;
        }

        _logger.LogDebug($"📌 Single-port device: {ports.Count} port(s), chip: {ftdiInfo?.ChipType ?? "Unknown"}");
        return false;
    }

    /// <summary>
    /// Get statistics about device grouping
    /// </summary>
    public DeviceGroupingStatistics GetGroupingStatistics(IEnumerable<DeviceGroup> deviceGroups)
    {
        var groups = deviceGroups.ToList();
        
        var stats = new DeviceGroupingStatistics
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
        
        _logger.LogInformation($"📊 Device grouping statistics: {stats}");
        return stats;
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