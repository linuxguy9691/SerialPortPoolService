// ===================================================================
// SIMPLIFIED: FtdiEepromReader.cs - Version pragmatique avec vraie API
// File: SerialPortPool.Core/Services/FtdiEepromReader.cs
// Purpose: FTDI device info via FTD2XX_NET (sans EEPROM direct pour l'instant)
// ===================================================================

using FTD2XX_NET;
using Microsoft.Extensions.Logging;
using SerialPortPool.Core.Interfaces;
using SerialPortPool.Core.Models;

namespace SerialPortPool.Core.Services;

/// <summary>
/// FTDI Device Reader using vraie API FTD2XX_NET (version simplifiée)
/// SPRINT 8 FEATURE: Enhanced device info pour dynamic BIB selection
/// APPROACH: Utilise seulement les méthodes qui existent vraiment dans FTD2XX_NET
/// </summary>
public class FtdiEepromReader : IFtdiEepromReader
{
    private readonly ILogger<FtdiEepromReader> _logger;

    public FtdiEepromReader(ILogger<FtdiEepromReader> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Read enhanced device data via FTD2XX_NET (simplified approach)
    /// </summary>
    public async Task<FtdiEepromData> ReadEepromAsync(string serialNumber)
    {
        if (string.IsNullOrWhiteSpace(serialNumber))
        {
            var error = "Serial number cannot be empty for device reading";
            _logger.LogWarning("⚠️ {Error}", error);
            return FtdiEepromData.CreateError("", error);
        }

        try
        {
            _logger.LogDebug("🔬 Reading enhanced device data via FTD2XX_NET: {SerialNumber}", serialNumber);

            return await Task.Run(() =>
            {
                var ftdi = new FTDI();
                
                try
                {
                    // Step 1: Open device by serial number
                    _logger.LogDebug("📡 Opening FTDI device: {SerialNumber}", serialNumber);
                    var status = ftdi.OpenBySerialNumber(serialNumber);
                    
                    if (status != FTDI.FT_STATUS.FT_OK)
                    {
                        var error = $"Cannot open FTDI device {serialNumber}: {status}";
                        _logger.LogWarning("❌ {Error}", error);
                        return FtdiEepromData.CreateError(serialNumber, error);
                    }

                    _logger.LogDebug("✅ FTDI device opened successfully: {SerialNumber}", serialNumber);

                    // Step 2: Get device info using available methods
                    var result = GetEnhancedDeviceInfo(ftdi, serialNumber);
                    
                    _logger.LogInformation("✅ Enhanced device data read - ProductDescription: '{ProductDescription}', Manufacturer: '{Manufacturer}'", 
                        result.ProductDescription, result.Manufacturer);
                    
                    return result;
                }
                catch (Exception ex)
                {
                    var error = $"Exception reading enhanced device data from {serialNumber}: {ex.Message}";
                    _logger.LogError(ex, "💥 {Error}", error);
                    return FtdiEepromData.CreateError(serialNumber, error);
                }
                finally
                {
                    // Always close the device
                    try
                    {
                        ftdi.Close();
                        _logger.LogDebug("🔒 FTDI device closed: {SerialNumber}", serialNumber);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "⚠️ Error closing FTDI device {SerialNumber}", serialNumber);
                    }
                }
            });
        }
        catch (Exception ex)
        {
            var error = $"Unexpected error reading enhanced device data from {serialNumber}: {ex.Message}";
            _logger.LogError(ex, "💥 {Error}", error);
            return FtdiEepromData.CreateError(serialNumber, error);
        }
    }

    /// <summary>
    /// Read enhanced device data from all connected FTDI devices
    /// </summary>
    public async Task<Dictionary<string, FtdiEepromData>> ReadAllConnectedDevicesAsync()
    {
        try
        {
            _logger.LogInformation("🔍 Discovering all connected FTDI devices for enhanced reading...");
            
            var serialNumbers = await GetConnectedDeviceSerialNumbersAsync();
            var results = new Dictionary<string, FtdiEepromData>();
            
            _logger.LogInformation("📋 Found {Count} FTDI devices to read", serialNumbers.Count);
            
            foreach (var serialNumber in serialNumbers)
            {
                var deviceData = await ReadEepromAsync(serialNumber);
                results[serialNumber] = deviceData;
                
                if (deviceData.IsValid)
                {
                    _logger.LogDebug("✅ Enhanced device data read: {SerialNumber} → '{ProductDescription}'", 
                        serialNumber, deviceData.ProductDescription);
                }
                else
                {
                    _logger.LogWarning("❌ Enhanced device data read failed: {SerialNumber} → {Error}", 
                        serialNumber, deviceData.ErrorMessage);
                }
            }
            
            var validCount = results.Values.Count(d => d.IsValid);
            _logger.LogInformation("📊 Enhanced device reading complete: {Valid}/{Total} devices read successfully", 
                validCount, results.Count);
            
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "💥 Error reading enhanced device data from all connected devices");
            return new Dictionary<string, FtdiEepromData>();
        }
    }

    /// <summary>
    /// Check if FTDI device is accessible via FTD2XX_NET
    /// </summary>
    public async Task<bool> IsDeviceAccessibleAsync(string serialNumber)
    {
        if (string.IsNullOrWhiteSpace(serialNumber))
            return false;

        try
        {
            return await Task.Run(() =>
            {
                var ftdi = new FTDI();
                
                try
                {
                    var status = ftdi.OpenBySerialNumber(serialNumber);
                    var accessible = status == FTDI.FT_STATUS.FT_OK;
                    
                    _logger.LogDebug("🔍 Device accessibility check: {SerialNumber} → {Accessible}", 
                        serialNumber, accessible ? "✅ Accessible" : "❌ Not accessible");
                    
                    return accessible;
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "⚠️ Device accessibility check failed: {SerialNumber}", serialNumber);
                    return false;
                }
                finally
                {
                    try { ftdi.Close(); } catch { /* Ignore close errors in accessibility check */ }
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "⚠️ Error checking device accessibility: {SerialNumber}", serialNumber);
            return false;
        }
    }

    /// <summary>
    /// Get list of all connected FTDI device serial numbers
    /// </summary>
    public async Task<List<string>> GetConnectedDeviceSerialNumbersAsync()
    {
        try
        {
            return await Task.Run(() =>
            {
                var ftdi = new FTDI();
                var serialNumbers = new List<string>();
                
                try
                {
                    _logger.LogDebug("🔍 Scanning for connected FTDI devices...");
                    
                    // Get number of FTDI devices
                    uint deviceCount = 0;
                    var status = ftdi.GetNumberOfDevices(ref deviceCount);
                    
                    if (status != FTDI.FT_STATUS.FT_OK)
                    {
                        _logger.LogWarning("⚠️ Cannot get FTDI device count: {Status}", status);
                        return serialNumbers;
                    }
                    
                    if (deviceCount == 0)
                    {
                        _logger.LogDebug("📋 No FTDI devices found");
                        return serialNumbers;
                    }
                    
                    _logger.LogDebug("📋 Found {DeviceCount} FTDI device(s)", deviceCount);
                    
                    // Get device info list
                    var deviceInfoArray = new FTDI.FT_DEVICE_INFO_NODE[deviceCount];
                    status = ftdi.GetDeviceList(deviceInfoArray);
                    
                    if (status != FTDI.FT_STATUS.FT_OK)
                    {
                        _logger.LogWarning("⚠️ Cannot get FTDI device list: {Status}", status);
                        return serialNumbers;
                    }
                    
                    // Extract serial numbers
                    foreach (var deviceInfo in deviceInfoArray)
                    {
                        if (!string.IsNullOrEmpty(deviceInfo.SerialNumber))
                        {
                            serialNumbers.Add(deviceInfo.SerialNumber);
                            _logger.LogDebug("📱 Found FTDI device: {SerialNumber} (Type: {Type}, Description: {Description})", 
                                deviceInfo.SerialNumber, deviceInfo.Type, deviceInfo.Description);
                        }
                    }
                    
                    _logger.LogInformation("✅ FTDI device discovery complete: {Count} devices found", serialNumbers.Count);
                    return serialNumbers;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "💥 Error during FTDI device discovery");
                    return serialNumbers;
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "💥 Unexpected error during FTDI device discovery");
            return new List<string>();
        }
    }

    /// <summary>
    /// Read specific device field by name
    /// </summary>
    public async Task<string> ReadEepromFieldAsync(string serialNumber, string fieldName)
    {
        try
        {
            var deviceData = await ReadEepromAsync(serialNumber);
            
            if (!deviceData.IsValid)
            {
                _logger.LogWarning("⚠️ Cannot read field '{Field}' - device data invalid for {SerialNumber}", 
                    fieldName, serialNumber);
                return string.Empty;
            }
            
            return fieldName.ToLowerInvariant() switch
            {
                "productdescription" => deviceData.ProductDescription,
                "manufacturer" => deviceData.Manufacturer,
                "vendorid" => deviceData.VendorId,
                "productid" => deviceData.ProductId,
                "serialnumber" => deviceData.SerialNumber,
                "maxpower" => deviceData.MaxPower.ToString(),
                "selfpowered" => deviceData.SelfPowered.ToString(),
                "remotewakeup" => deviceData.RemoteWakeup.ToString(),
                _ => deviceData.AdditionalFields.GetValueOrDefault(fieldName, string.Empty)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "💥 Error reading device field '{Field}' from {SerialNumber}", fieldName, serialNumber);
            return string.Empty;
        }
    }

    #region Private Helper Methods - Simplified Version

    /// <summary>
    /// Get enhanced device info using available FTD2XX_NET methods
    /// SIMPLIFIED: Uses only methods that definitely exist in the API
    /// </summary>
    private FtdiEepromData GetEnhancedDeviceInfo(FTDI ftdi, string serialNumber)
    {
        try
        {
            var result = new FtdiEepromData
            {
                SerialNumber = serialNumber,
                ReadAt = DateTime.Now,
                Source = "FTD2XX_NET_ENHANCED"
            };

            // APPROACH: Use device info that we can get from GetDeviceList
            // since we already have the device open, we can get basic info
            
            // Get device type and description from the device list
            // (we'll cross-reference with our open device)
            try
            {
                uint deviceCount = 0;
                if (ftdi.GetNumberOfDevices(ref deviceCount) == FTDI.FT_STATUS.FT_OK && deviceCount > 0)
                {
                    var deviceInfoArray = new FTDI.FT_DEVICE_INFO_NODE[deviceCount];
                    if (ftdi.GetDeviceList(deviceInfoArray) == FTDI.FT_STATUS.FT_OK)
                    {
                        // Find our device in the list
                        var ourDevice = deviceInfoArray.FirstOrDefault(d => d.SerialNumber == serialNumber);
                        if (ourDevice.SerialNumber != null)
                        {
                            // Extract enhanced info from device node
                            result.ProductDescription = ourDevice.Description ?? GetProductDescriptionFromType(ourDevice.Type);
                            result.Manufacturer = "FTDI"; // Always FTDI for FTD2XX_NET devices
                            
                            // Try to extract VID/PID from device type
                            ExtractVidPidFromDeviceType(ourDevice.Type, result);
                            
                            _logger.LogDebug("📝 Enhanced device info: Type={Type}, Description={Description}", 
                                ourDevice.Type, ourDevice.Description);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "⚠️ Could not get enhanced device list info for {SerialNumber}", serialNumber);
            }

            // Set reasonable defaults if we couldn't get specific info
            if (string.IsNullOrEmpty(result.VendorId))
            {
                result.VendorId = "0403"; // Standard FTDI VID
            }

            if (string.IsNullOrEmpty(result.ProductDescription))
            {
                result.ProductDescription = "FTDI USB Serial Device";
            }

            if (string.IsNullOrEmpty(result.Manufacturer))
            {
                result.Manufacturer = "FTDI";
            }

            // Set default EEPROM-style values (can be enhanced later)
            result.MaxPower = 100; // 100mA default
            result.SelfPowered = false;
            result.RemoteWakeup = false;
            result.PullDownEnable = false;
            result.SerNumEnable = true;
            result.USBVersion = "2.0";

            // Add metadata about our enhanced approach
            result.AdditionalFields["EnhancedMethod"] = "FTD2XX_NET_DeviceList";
            result.AdditionalFields["ReadTimestamp"] = DateTime.Now.ToString("O");

            _logger.LogDebug("📊 Enhanced device data ready: {Summary}", result.GetSummary());
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "💥 Error getting enhanced device info for {SerialNumber}", serialNumber);
            return FtdiEepromData.CreateError(serialNumber, $"Enhanced info error: {ex.Message}");
        }
    }

    /// <summary>
    /// Get product description from FTDI device type
    /// </summary>
    private string GetProductDescriptionFromType(FTDI.FT_DEVICE type)
    {
        return type switch
        {
            FTDI.FT_DEVICE.FT_DEVICE_232R => "FT232R USB UART",
            FTDI.FT_DEVICE.FT_DEVICE_2232H => "FT2232H Dual USB UART/FIFO",
            FTDI.FT_DEVICE.FT_DEVICE_4232H => "FT4232H Quad USB UART/FIFO",
            FTDI.FT_DEVICE.FT_DEVICE_232H => "FT232H Single Channel USB 2.0 Hi-Speed USB to MULTIPURPOSE UART/FIFO",
            FTDI.FT_DEVICE.FT_DEVICE_X_SERIES => "FT X-Series USB UART",
            _ => "FTDI USB Serial Device"
        };
    }

    /// <summary>
    /// Extract VID/PID from device type (best guess)
    /// </summary>
    private void ExtractVidPidFromDeviceType(FTDI.FT_DEVICE type, FtdiEepromData result)
    {
        // Set VID (always FTDI)
        result.VendorId = "0403";

        // Set PID based on device type
        result.ProductId = type switch
        {
            FTDI.FT_DEVICE.FT_DEVICE_232R => "6001",
            FTDI.FT_DEVICE.FT_DEVICE_2232H => "6010", 
            FTDI.FT_DEVICE.FT_DEVICE_4232H => "6011",
            FTDI.FT_DEVICE.FT_DEVICE_232H => "6014",
            FTDI.FT_DEVICE.FT_DEVICE_X_SERIES => "6015",
            _ => "0000"
        };

        result.AdditionalFields["DeviceType"] = type.ToString();
    }

    #endregion
}