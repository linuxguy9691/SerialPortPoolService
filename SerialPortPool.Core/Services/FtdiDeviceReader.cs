using System.Management;
using Microsoft.Extensions.Logging;
using SerialPortPool.Core.Interfaces;
using SerialPortPool.Core.Models;

namespace SerialPortPool.Core.Services;

/// <summary>
/// Service for reading and analyzing FTDI device information with enhanced system info capabilities
/// </summary>
public class FtdiDeviceReader : IFtdiDeviceReader
{
    private readonly ILogger<FtdiDeviceReader> _logger;

    public FtdiDeviceReader(ILogger<FtdiDeviceReader> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region Original Sprint 2 Methods

    /// <summary>
    /// Read detailed FTDI device information from a port
    /// </summary>
    public async Task<FtdiDeviceInfo?> ReadDeviceInfoAsync(string portName)
    {
        if (string.IsNullOrWhiteSpace(portName))
        {
            _logger.LogWarning("Port name is null or empty");
            return null;
        }

        try
        {
            _logger.LogDebug($"Reading FTDI device info for port {portName}");

            // First, get the device ID from WMI
            string? deviceId = await GetDeviceIdFromPortAsync(portName);
            if (string.IsNullOrEmpty(deviceId))
            {
                _logger.LogDebug($"No device ID found for port {portName}");
                return null;
            }

            // Parse FTDI information from device ID
            var ftdiInfo = await ReadDeviceInfoFromIdAsync(deviceId);
            if (ftdiInfo != null)
            {
                // Enrich with additional WMI data
                await EnrichWithWmiDataAsync(ftdiInfo, portName);
                _logger.LogInformation($"FTDI device analyzed: {portName} → {ftdiInfo.ChipType} (VID: {ftdiInfo.VendorId}, PID: {ftdiInfo.ProductId})");
            }

            return ftdiInfo;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error reading FTDI device info for port {portName}");
            return null;
        }
    }

    /// <summary>
    /// Read FTDI device information from Windows Device ID
    /// </summary>
    public async Task<FtdiDeviceInfo?> ReadDeviceInfoFromIdAsync(string deviceId)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
        {
            return null;
        }

        try
        {
            _logger.LogDebug($"Parsing device ID: {deviceId}");

            // Use the parsing logic from FtdiDeviceInfo
            var ftdiInfo = await Task.Run(() => FtdiDeviceInfo.ParseFromDeviceId(deviceId));
            
            if (ftdiInfo != null)
            {
                _logger.LogDebug($"Successfully parsed FTDI device: {ftdiInfo.ChipType} (Serial: {ftdiInfo.SerialNumber})");
            }
            else
            {
                _logger.LogDebug($"Device ID does not represent an FTDI device: {deviceId}");
            }

            return ftdiInfo;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error parsing device ID: {deviceId}");
            return null;
        }
    }

    /// <summary>
    /// Check if a port is an FTDI device
    /// </summary>
    public async Task<bool> IsFtdiDeviceAsync(string portName)
    {
        var ftdiInfo = await ReadDeviceInfoAsync(portName);
        return ftdiInfo != null && ftdiInfo.IsGenuineFtdi;
    }

    /// <summary>
    /// Check if a port is specifically a 4232H chip
    /// </summary>
    public async Task<bool> IsFtdi4232HAsync(string portName)
    {
        var ftdiInfo = await ReadDeviceInfoAsync(portName);
        return ftdiInfo?.Is4232H ?? false;
    }

    /// <summary>
    /// Read EEPROM data from an FTDI device (basic implementation)
    /// </summary>
    public async Task<Dictionary<string, string>> ReadEepromDataAsync(string portName)
    {
        var eepromData = new Dictionary<string, string>();

        try
        {
            _logger.LogDebug($"Reading EEPROM data for port {portName}");

            // For now, we'll extract what we can from WMI
            // In a full implementation, this would use FTDI D2XX drivers
            var ftdiInfo = await ReadDeviceInfoAsync(portName);
            if (ftdiInfo != null)
            {
                eepromData["VendorId"] = ftdiInfo.VendorId;
                eepromData["ProductId"] = ftdiInfo.ProductId;
                eepromData["ChipType"] = ftdiInfo.ChipType;
                eepromData["SerialNumber"] = ftdiInfo.SerialNumber;
                eepromData["Manufacturer"] = ftdiInfo.Manufacturer;
                eepromData["ProductDescription"] = ftdiInfo.ProductDescription;
                
                _logger.LogInformation($"EEPROM data extracted for {portName}: {eepromData.Count} entries");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error reading EEPROM data for port {portName}");
        }

        return eepromData;
    }

    /// <summary>
    /// Get all FTDI devices currently connected to the system
    /// </summary>
    public async Task<IEnumerable<FtdiDeviceInfo>> GetAllFtdiDevicesAsync()
    {
        var ftdiDevices = new List<FtdiDeviceInfo>();

        try
        {
            _logger.LogDebug("Scanning for all FTDI devices in system");

            await Task.Run(() =>
            {
                // Query for all FTDI devices using WMI
                using var searcher = new ManagementObjectSearcher(
                    "SELECT * FROM Win32_PnPEntity WHERE DeviceID LIKE 'FTDIBUS%'");

                using var collection = searcher.Get();

                foreach (ManagementObject device in collection)
                {
                    try
                    {
                        var deviceId = device["DeviceID"]?.ToString();
                        if (!string.IsNullOrEmpty(deviceId))
                        {
                            var ftdiInfo = FtdiDeviceInfo.ParseFromDeviceId(deviceId);
                            if (ftdiInfo != null)
                            {
                                // Enrich with additional WMI data
                                ftdiInfo.Manufacturer = device["Manufacturer"]?.ToString() ?? "FTDI";
                                ftdiInfo.ProductDescription = device["Description"]?.ToString() ?? "FTDI Device";
                                
                                ftdiDevices.Add(ftdiInfo);
                                _logger.LogDebug($"Found FTDI device: {ftdiInfo.ChipType} (Serial: {ftdiInfo.SerialNumber})");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug(ex, "Error processing FTDI device from WMI");
                    }
                    finally
                    {
                        device?.Dispose();
                    }
                }
            });

            _logger.LogInformation($"Found {ftdiDevices.Count} FTDI devices in system");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scanning for FTDI devices");
        }

        return ftdiDevices;
    }

    /// <summary>
    /// Analyze a device ID to extract FTDI information
    /// </summary>
    public FtdiDeviceInfo? AnalyzeDeviceId(string deviceId)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
        {
            return null;
        }

        try
        {
            return FtdiDeviceInfo.ParseFromDeviceId(deviceId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error analyzing device ID: {deviceId}");
            return null;
        }
    }

    #endregion

    #region NEW Sprint 3 ÉTAPE 3 - SystemInfo Extension

    /// <summary>
    /// Read comprehensive system information from a port, including EEPROM data and hardware details
    /// </summary>
    /// <param name="portName">Port name to read system info from</param>
    /// <returns>System information or null if not available</returns>
    public async Task<SystemInfo?> ReadSystemInfoAsync(string portName)
    {
        if (string.IsNullOrWhiteSpace(portName))
        {
            _logger.LogWarning("Port name is null or empty for system info reading");
            return null;
        }

        try
        {
            _logger.LogDebug($"Reading system information for port {portName}");

            // First, get basic FTDI device info
            var ftdiInfo = await ReadDeviceInfoAsync(portName);
            if (ftdiInfo == null)
            {
                _logger.LogDebug($"No FTDI device found for port {portName}");
                return SystemInfo.CreateError(portName, "Not an FTDI device or device not found");
            }

            // Create SystemInfo from FTDI device info
            var systemInfo = SystemInfo.FromFtdiDevice(ftdiInfo);

            // Enhance with additional EEPROM data
            await EnhanceWithEepromDataAsync(systemInfo, portName);

            // Add system-level properties
            await EnhanceWithSystemPropertiesAsync(systemInfo, portName);

            // Add client-specific configuration if available
            await EnhanceWithClientConfigurationAsync(systemInfo, ftdiInfo);

            _logger.LogInformation($"System information successfully read for {portName}: {systemInfo.GetSummary()}");
            return systemInfo;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error reading system information for port {portName}");
            return SystemInfo.CreateError(portName, $"Error reading system info: {ex.Message}");
        }
    }

    /// <summary>
    /// Enhance SystemInfo with additional EEPROM data beyond basic FTDI info
    /// </summary>
    private async Task EnhanceWithEepromDataAsync(SystemInfo systemInfo, string portName)
    {
        try
        {
            _logger.LogDebug($"Enhancing EEPROM data for port {portName}");

            // Read additional EEPROM properties using existing method
            var eepromData = await ReadEepromDataAsync(portName);
            
            // Merge additional EEPROM data
            foreach (var kvp in eepromData)
            {
                if (!systemInfo.EepromData.ContainsKey(kvp.Key))
                {
                    systemInfo.EepromData[kvp.Key] = kvp.Value;
                }
            }

            // Extract specific fields from EEPROM data
            systemInfo.FirmwareVersion = systemInfo.GetEepromValue("FirmwareVersion") ?? 
                                       systemInfo.GetEepromValue("Version") ?? 
                                       "Unknown";
            
            systemInfo.HardwareRevision = systemInfo.GetEepromValue("HardwareRevision") ?? 
                                        systemInfo.GetEepromValue("HwRevision") ?? 
                                        systemInfo.GetEepromValue("Revision") ?? 
                                        "Unknown";

            _logger.LogDebug($"EEPROM enhancement complete for {portName}: {systemInfo.EepromData.Count} entries");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, $"Could not enhance EEPROM data for {portName}");
            systemInfo.EepromData["EepromReadError"] = ex.Message;
        }
    }

    /// <summary>
    /// Enhance SystemInfo with system-level properties from WMI and other sources
    /// </summary>
    private async Task EnhanceWithSystemPropertiesAsync(SystemInfo systemInfo, string portName)
    {
        try
        {
            await Task.Run(() =>
            {
                _logger.LogDebug($"Reading system properties for port {portName}");

                // Add basic system information
                systemInfo.SystemProperties["OperatingSystem"] = Environment.OSVersion.ToString();
                systemInfo.SystemProperties["MachineName"] = Environment.MachineName;
                systemInfo.SystemProperties["UserName"] = Environment.UserName;
                systemInfo.SystemProperties["ProcessorCount"] = Environment.ProcessorCount.ToString();
                systemInfo.SystemProperties["PortReadTime"] = DateTime.Now.ToString("O");

                // Try to get additional WMI properties for the specific port
                try
                {
                    using var searcher = new ManagementObjectSearcher(
                        $"SELECT * FROM Win32_SerialPort WHERE DeviceID = '{portName}'");
                    using var collection = searcher.Get();

                    foreach (ManagementObject port in collection)
                    {
                        try
                        {
                            // Add WMI properties to system info
                            var maxBaudRate = port["MaxBaudRate"]?.ToString();
                            if (!string.IsNullOrEmpty(maxBaudRate))
                            {
                                systemInfo.SystemProperties["MaxBaudRate"] = maxBaudRate;
                            }

                            var settableBaudRate = port["SettableBaudRate"]?.ToString();
                            if (!string.IsNullOrEmpty(settableBaudRate))
                            {
                                systemInfo.SystemProperties["SettableBaudRate"] = settableBaudRate;
                            }

                            var settableDataBits = port["SettableDataBits"]?.ToString();
                            if (!string.IsNullOrEmpty(settableDataBits))
                            {
                                systemInfo.SystemProperties["SettableDataBits"] = settableDataBits;
                            }

                            var capabilities = port["Capabilities"];
                            if (capabilities != null)
                            {
                                systemInfo.SystemProperties["Capabilities"] = capabilities.ToString() ?? "";
                            }
                        }
                        finally
                        {
                            port?.Dispose();
                        }
                        break; // Only process first match
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, $"Could not read WMI system properties for {portName}");
                    systemInfo.SystemProperties["WmiError"] = ex.Message;
                }

                _logger.LogDebug($"System properties enhancement complete for {portName}: {systemInfo.SystemProperties.Count} entries");
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, $"Error enhancing system properties for {portName}");
            systemInfo.SystemProperties["SystemPropertiesError"] = ex.Message;
        }
    }

    /// <summary>
    /// Enhance SystemInfo with client-specific configuration based on device type and requirements
    /// </summary>
    private async Task EnhanceWithClientConfigurationAsync(SystemInfo systemInfo, FtdiDeviceInfo ftdiInfo)
    {
        try
        {
            await Task.Run(() =>
            {
                _logger.LogDebug($"Adding client configuration for device {ftdiInfo.ChipType}");

                // Add device type classification
                systemInfo.ClientConfiguration["DeviceType"] = ftdiInfo.ChipType;
                systemInfo.ClientConfiguration["IsGenuineFtdi"] = ftdiInfo.IsGenuineFtdi.ToString();
                systemInfo.ClientConfiguration["Is4232H"] = ftdiInfo.Is4232H.ToString();

                // Add client compatibility information
                if (ftdiInfo.Is4232H)
                {
                    systemInfo.ClientConfiguration["ClientCompatible"] = "true";
                    systemInfo.ClientConfiguration["CompatibilityLevel"] = "Full";
                    systemInfo.ClientConfiguration["RecommendedUsage"] = "Production";
                }
                else if (ftdiInfo.IsGenuineFtdi)
                {
                    systemInfo.ClientConfiguration["ClientCompatible"] = "partial";
                    systemInfo.ClientConfiguration["CompatibilityLevel"] = "Development";
                    systemInfo.ClientConfiguration["RecommendedUsage"] = "Testing";
                }
                else
                {
                    systemInfo.ClientConfiguration["ClientCompatible"] = "false";
                    systemInfo.ClientConfiguration["CompatibilityLevel"] = "None";
                    systemInfo.ClientConfiguration["RecommendedUsage"] = "Not recommended";
                }

                // Add performance recommendations based on chip type
                switch (ftdiInfo.ChipType)
                {
                    case "FT4232H":
                        systemInfo.ClientConfiguration["MaxBaudRate"] = "12000000";
                        systemInfo.ClientConfiguration["MultiPortCapable"] = "true";
                        systemInfo.ClientConfiguration["HighSpeedCapable"] = "true";
                        break;
                    case "FT232H":
                        systemInfo.ClientConfiguration["MaxBaudRate"] = "12000000";
                        systemInfo.ClientConfiguration["MultiPortCapable"] = "false";
                        systemInfo.ClientConfiguration["HighSpeedCapable"] = "true";
                        break;
                    case "FT232R":
                        systemInfo.ClientConfiguration["MaxBaudRate"] = "3000000";
                        systemInfo.ClientConfiguration["MultiPortCapable"] = "false";
                        systemInfo.ClientConfiguration["HighSpeedCapable"] = "false";
                        break;
                    default:
                        systemInfo.ClientConfiguration["MaxBaudRate"] = "Unknown";
                        systemInfo.ClientConfiguration["MultiPortCapable"] = "Unknown";
                        systemInfo.ClientConfiguration["HighSpeedCapable"] = "Unknown";
                        break;
                }

                // Add timestamp for configuration
                systemInfo.ClientConfiguration["ConfigurationGeneratedAt"] = DateTime.Now.ToString("O");

                _logger.LogDebug($"Client configuration complete: {systemInfo.ClientConfiguration.Count} entries");
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, $"Error adding client configuration for {ftdiInfo.ChipType}");
            systemInfo.ClientConfiguration["ConfigurationError"] = ex.Message;
        }
    }

    #endregion

    #region Private Helper Methods (Sprint 2)

    /// <summary>
    /// Get device ID from port name using WMI
    /// </summary>
    private async Task<string?> GetDeviceIdFromPortAsync(string portName)
    {
        try
        {
            return await Task.Run(() =>
            {
                // First try Win32_SerialPort
                using var serialSearcher = new ManagementObjectSearcher(
                    $"SELECT * FROM Win32_SerialPort WHERE DeviceID = '{portName}'");

                using var serialCollection = serialSearcher.Get();

                foreach (ManagementObject port in serialCollection)
                {
                    try
                    {
                        var deviceId = port["PNPDeviceID"]?.ToString();
                        if (!string.IsNullOrEmpty(deviceId))
                        {
                            return deviceId;
                        }
                    }
                    finally
                    {
                        port?.Dispose();
                    }
                }

                // If not found, try Win32_PnPEntity
                using var pnpSearcher = new ManagementObjectSearcher(
                    $"SELECT * FROM Win32_PnPEntity WHERE Name LIKE '%{portName}%'");

                using var pnpCollection = pnpSearcher.Get();

                foreach (ManagementObject entity in pnpCollection)
                {
                    try
                    {
                        var deviceId = entity["DeviceID"]?.ToString();
                        if (!string.IsNullOrEmpty(deviceId) && deviceId.Contains("FTDIBUS"))
                        {
                            return deviceId;
                        }
                    }
                    finally
                    {
                        entity?.Dispose();
                    }
                }

                return null;
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting device ID for port {portName}");
            return null;
        }
    }

    /// <summary>
    /// Enrich FTDI info with additional WMI data
    /// </summary>
    private async Task EnrichWithWmiDataAsync(FtdiDeviceInfo ftdiInfo, string portName)
    {
        try
        {
            await Task.Run(() =>
            {
                using var searcher = new ManagementObjectSearcher(
                    $"SELECT * FROM Win32_PnPEntity WHERE DeviceID = '{ftdiInfo.RawDeviceId}'");

                using var collection = searcher.Get();

                foreach (ManagementObject entity in collection)
                {
                    try
                    {
                        if (string.IsNullOrEmpty(ftdiInfo.Manufacturer))
                        {
                            ftdiInfo.Manufacturer = entity["Manufacturer"]?.ToString() ?? "FTDI";
                        }

                        if (string.IsNullOrEmpty(ftdiInfo.ProductDescription))
                        {
                            ftdiInfo.ProductDescription = entity["Description"]?.ToString() ?? "FTDI Device";
                        }

                        // Add to EEPROM data
                        var service = entity["Service"]?.ToString();
                        if (!string.IsNullOrEmpty(service))
                        {
                            ftdiInfo.EepromData["Service"] = service;
                        }

                        break;
                    }
                    finally
                    {
                        entity?.Dispose();
                    }
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, $"Error enriching FTDI info for {portName}");
        }
    }

    #endregion
}