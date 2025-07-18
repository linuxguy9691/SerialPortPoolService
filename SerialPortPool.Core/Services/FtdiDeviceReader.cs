using System.Management;
using Microsoft.Extensions.Logging;
using SerialPortPool.Core.Interfaces;
using SerialPortPool.Core.Models;

namespace SerialPortPool.Core.Services;

/// <summary>
/// Service for reading and analyzing FTDI device information
/// </summary>
public class FtdiDeviceReader : IFtdiDeviceReader
{
    private readonly ILogger<FtdiDeviceReader> _logger;

    public FtdiDeviceReader(ILogger<FtdiDeviceReader> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

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
}