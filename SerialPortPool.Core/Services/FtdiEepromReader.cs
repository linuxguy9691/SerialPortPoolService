// ===================================================================
// FIXED: FtdiEepromReader.cs - Corrected API calls based on FTD2XX_NET
// File: SerialPortPool.Core/Services/FtdiEepromReader.cs
// Purpose: VRAIE lecture EEPROM via FTD2XX_NET avec API correcte
// FIXES: VendorId/ProductId via GetDeviceID() bit manipulation + ref parameters
// ===================================================================

using FTD2XX_NET;
using Microsoft.Extensions.Logging;
using SerialPortPool.Core.Interfaces;
using SerialPortPool.Core.Models;

namespace SerialPortPool.Core.Services;

/// <summary>
/// FTDI EEPROM Reader avec VRAIE lecture EEPROM via drivers D2XX natifs
/// SPRINT 8 FEATURE: Lecture ProductDescription pour dynamic BIB selection
/// FIXED: Utilise les vraies méthodes de l'API FTD2XX_NET actuelle
/// </summary>
public class FtdiEepromReader : IFtdiEepromReader
{
    private readonly ILogger<FtdiEepromReader> _logger;

    public FtdiEepromReader(ILogger<FtdiEepromReader> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Read VRAIE EEPROM data via D2XX drivers natifs
    /// FIXED: Utilise les vraies méthodes API avec GetDeviceID() pour VID/PID
    /// </summary>
    public async Task<FtdiEepromData> ReadEepromAsync(string serialNumber)
    {
        if (string.IsNullOrWhiteSpace(serialNumber))
        {
            var error = "Serial number cannot be empty for EEPROM reading";
            _logger.LogWarning("⚠️ {Error}", error);
            return FtdiEepromData.CreateError("", error);
        }

        try
        {
            _logger.LogDebug("🔬 Reading REAL EEPROM data via D2XX drivers: {SerialNumber}", serialNumber);

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

                    // Step 2: VRAIE lecture EEPROM avec API native D2XX
                    var result = ReadRealEepromData(ftdi, serialNumber);
                    
                    _logger.LogInformation("✅ REAL EEPROM data read - ProductDescription: '{ProductDescription}', Manufacturer: '{Manufacturer}'", 
                        result.ProductDescription, result.Manufacturer);
                    
                    return result;
                }
                catch (Exception ex)
                {
                    var error = $"Exception reading REAL EEPROM from {serialNumber}: {ex.Message}";
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
            var error = $"Unexpected error reading REAL EEPROM from {serialNumber}: {ex.Message}";
            _logger.LogError(ex, "💥 {Error}", error);
            return FtdiEepromData.CreateError(serialNumber, error);
        }
    }

    /// <summary>
    /// Read VRAIE EEPROM data using D2XX native API
    /// FIXED: Utilise les vraies structures et méthodes EEPROM + GetDeviceID() pour VID/PID
    /// </summary>
    private FtdiEepromData ReadRealEepromData(FTDI ftdi, string serialNumber)
    {
        try
        {
            var result = new FtdiEepromData
            {
                SerialNumber = serialNumber,
                ReadAt = DateTime.Now,
                Source = "D2XX_NATIVE_EEPROM"
            };

            // FIXED: First get VID/PID via GetDeviceID() (common to all device types)
            if (!ReadVidPidFromDevice(ftdi, result))
            {
                _logger.LogWarning("⚠️ Cannot read VID/PID from device");
            }

            // Method 1: Try reading EEPROM using specific structure methods (preferred)
            if (TryReadEepromWithStructures(ftdi, result))
            {
                _logger.LogDebug("✅ EEPROM read successful via EEPROM structures");
                return result;
            }

            // Method 2: Fallback to basic device info methods
            if (TryReadBasicDeviceInfo(ftdi, result))
            {
                _logger.LogDebug("✅ Basic device info read successful");
                return result;
            }

            return FtdiEepromData.CreateError(serialNumber, "All EEPROM reading methods failed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "💥 Error in ReadRealEepromData for {SerialNumber}", serialNumber);
            return FtdiEepromData.CreateError(serialNumber, $"EEPROM read error: {ex.Message}");
        }
    }

    /// <summary>
    /// FIXED: Read VID/PID using GetDeviceID() and bit manipulation
    /// This is the CORRECT way according to current FTD2XX_NET API
    /// </summary>
    private bool ReadVidPidFromDevice(FTDI ftdi, FtdiEepromData result)
    {
        try
        {
            _logger.LogDebug("🔍 Reading VID/PID via GetDeviceID()");

            uint deviceId = 0;
            var status = ftdi.GetDeviceID(ref deviceId);
            
            if (status == FTDI.FT_STATUS.FT_OK)
            {
                // FIXED: Extract VID/PID using bit manipulation (as per documentation)
                ushort vendorId = (ushort)(deviceId >> 16);    // Upper 16 bits = VID
                ushort productId = (ushort)(deviceId & 0xFFFF); // Lower 16 bits = PID
                
                result.VendorId = vendorId.ToString("X4");
                result.ProductId = productId.ToString("X4");
                
                _logger.LogDebug("✅ VID/PID extracted: {VendorId}/{ProductId}", result.VendorId, result.ProductId);
                return true;
            }
            
            _logger.LogDebug("⚠️ GetDeviceID failed: {Status}", status);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "⚠️ Error reading VID/PID from device");
            return false;
        }
    }

    /// <summary>
    /// Method 1: Try reading EEPROM using device-specific structure methods
    /// FIXED: Remove VendorId/ProductId from EEPROM structures (use GetDeviceID instead)
    /// </summary>
    private bool TryReadEepromWithStructures(FTDI ftdi, FtdiEepromData result)
    {
        try
        {
            _logger.LogDebug("🔍 Attempting EEPROM read via device-specific structures");

            // Get device type first
            FTDI.FT_DEVICE deviceType = FTDI.FT_DEVICE.FT_DEVICE_UNKNOWN;
            var status = ftdi.GetDeviceType(ref deviceType);
            
            if (status != FTDI.FT_STATUS.FT_OK)
            {
                _logger.LogDebug("⚠️ Cannot get device type: {Status}", status);
                return false;
            }

            _logger.LogDebug("📱 Device type detected: {DeviceType}", deviceType);

            // Read EEPROM based on device type
            switch (deviceType)
            {
                case FTDI.FT_DEVICE.FT_DEVICE_232R:
                    return ReadFT232REeprom(ftdi, result);

                case FTDI.FT_DEVICE.FT_DEVICE_232H:
                    return ReadFT232HEeprom(ftdi, result);

                case FTDI.FT_DEVICE.FT_DEVICE_4232H:
                    return ReadFT4232HEeprom(ftdi, result);

                case FTDI.FT_DEVICE.FT_DEVICE_2232H:
                    // Use FT232H structure for FT2232H (similar EEPROM layout)
                    return ReadFT232HEeprom(ftdi, result);

                default:
                    _logger.LogWarning("⚠️ Unsupported device type for EEPROM reading: {DeviceType}", deviceType);
                    return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "⚠️ EEPROM structure reading failed with exception");
            return false;
        }
    }

    /// <summary>
    /// Read FT232R EEPROM structure
    /// FIXED: Remove non-existent VendorId/ProductId properties
    /// </summary>
    private bool ReadFT232REeprom(FTDI ftdi, FtdiEepromData result)
    {
        try
        {
            var eepromData = new FTDI.FT232R_EEPROM_STRUCTURE();
            var status = ftdi.ReadFT232REEPROM(eepromData);
            
            if (status == FTDI.FT_STATUS.FT_OK)
            {
                // FIXED: Only use properties that actually exist in the structure
                result.ProductDescription = eepromData.Description ?? "";
                result.Manufacturer = eepromData.Manufacturer ?? "";
                
                // EEPROM-specific fields that DO exist
                result.MaxPower = eepromData.MaxPower;
                result.SelfPowered = eepromData.SelfPowered;
                result.RemoteWakeup = eepromData.RemoteWakeup;
                result.SerNumEnable = eepromData.SerNumEnable;
                
                _logger.LogDebug("📖 FT232R EEPROM read: '{Description}' by '{Manufacturer}'", 
                    result.ProductDescription, result.Manufacturer);
                return true;
            }
            
            _logger.LogDebug("⚠️ FT232R EEPROM read failed: {Status}", status);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "⚠️ FT232R EEPROM read exception");
            return false;
        }
    }

    /// <summary>
    /// Read FT232H EEPROM structure
    /// FIXED: Remove non-existent VendorId/ProductId properties
    /// </summary>
    private bool ReadFT232HEeprom(FTDI ftdi, FtdiEepromData result)
    {
        try
        {
            var eepromData = new FTDI.FT232H_EEPROM_STRUCTURE();
            var status = ftdi.ReadFT232HEEPROM(eepromData);
            
            if (status == FTDI.FT_STATUS.FT_OK)
            {
                // FIXED: Only use properties that actually exist in the structure
                result.ProductDescription = eepromData.Description ?? "";
                result.Manufacturer = eepromData.Manufacturer ?? "";
                
                // EEPROM-specific fields that DO exist
                result.MaxPower = eepromData.MaxPower;
                result.SelfPowered = eepromData.SelfPowered;
                result.RemoteWakeup = eepromData.RemoteWakeup;
                result.SerNumEnable = eepromData.SerNumEnable;
                
                _logger.LogDebug("📖 FT232H EEPROM read: '{Description}' by '{Manufacturer}'", 
                    result.ProductDescription, result.Manufacturer);
                return true;
            }
            
            _logger.LogDebug("⚠️ FT232H EEPROM read failed: {Status}", status);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "⚠️ FT232H EEPROM read exception");
            return false;
        }
    }

    /// <summary>
    /// Read FT4232H EEPROM structure
    /// FIXED: Remove non-existent VendorId/ProductId properties
    /// </summary>
    private bool ReadFT4232HEeprom(FTDI ftdi, FtdiEepromData result)
    {
        try
        {
            var eepromData = new FTDI.FT4232H_EEPROM_STRUCTURE();
            var status = ftdi.ReadFT4232HEEPROM(eepromData);
            
            if (status == FTDI.FT_STATUS.FT_OK)
            {
                // FIXED: Only use properties that actually exist in the structure
                result.ProductDescription = eepromData.Description ?? "";
                result.Manufacturer = eepromData.Manufacturer ?? "";
                
                // EEPROM-specific fields that DO exist
                result.MaxPower = eepromData.MaxPower;
                result.SelfPowered = eepromData.SelfPowered;
                result.RemoteWakeup = eepromData.RemoteWakeup;
                result.SerNumEnable = eepromData.SerNumEnable;
                
                _logger.LogDebug("📖 FT4232H EEPROM read: '{Description}' by '{Manufacturer}'", 
                    result.ProductDescription, result.Manufacturer);
                return true;
            }
            
            _logger.LogDebug("⚠️ FT4232H EEPROM read failed: {Status}", status);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "⚠️ FT4232H EEPROM read exception");
            return false;
        }
    }

    /// <summary>
    /// Method 2: Fallback to basic device info
    /// FIXED: Use proper GetDescription/GetSerialNumber with 'out' parameters
    /// </summary>
    private bool TryReadBasicDeviceInfo(FTDI ftdi, FtdiEepromData result)
    {
        try
        {
            _logger.LogDebug("🔍 Attempting basic device info fallback");

            // FIXED: Use 'out' parameters for API methods
            string description;
            string serialNumber;

            var descStatus = ftdi.GetDescription(out description);
            var serialStatus = ftdi.GetSerialNumber(out serialNumber);

            if (descStatus == FTDI.FT_STATUS.FT_OK || serialStatus == FTDI.FT_STATUS.FT_OK)
            {
                if (descStatus == FTDI.FT_STATUS.FT_OK)
                    result.ProductDescription = description ?? "";
                
                if (serialStatus == FTDI.FT_STATUS.FT_OK && !string.IsNullOrEmpty(serialNumber))
                    result.SerialNumber = serialNumber;

                result.Manufacturer = "FTDI"; // Default manufacturer

                _logger.LogDebug("📱 Basic device info - Description: '{Description}', Serial: '{Serial}'",
                    result.ProductDescription, result.SerialNumber);

                return !string.IsNullOrEmpty(result.ProductDescription) || !string.IsNullOrEmpty(result.VendorId);
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "⚠️ Basic device info failed with exception");
            return false;
        }
    }

    /// <summary>
    /// Read enhanced device data from all connected FTDI devices
    /// FIXED: Utilise GetDeviceList() pour l'énumération efficace
    /// </summary>
    public async Task<Dictionary<string, FtdiEepromData>> ReadAllConnectedDevicesAsync()
    {
        try
        {
            _logger.LogInformation("🔍 Discovering all connected FTDI devices for REAL EEPROM reading...");
            
            var serialNumbers = await GetConnectedDeviceSerialNumbersAsync();
            var results = new Dictionary<string, FtdiEepromData>();
            
            _logger.LogInformation("📋 Found {Count} FTDI devices to read via D2XX", serialNumbers.Count);
            
            foreach (var serialNumber in serialNumbers)
            {
                var deviceData = await ReadEepromAsync(serialNumber);
                results[serialNumber] = deviceData;
                
                if (deviceData.IsValid)
                {
                    _logger.LogDebug("✅ REAL EEPROM data read: {SerialNumber} → '{ProductDescription}'", 
                        serialNumber, deviceData.ProductDescription);
                }
                else
                {
                    _logger.LogWarning("❌ REAL EEPROM read failed: {SerialNumber} → {Error}", 
                        serialNumber, deviceData.ErrorMessage);
                }
            }
            
            var validCount = results.Values.Count(d => d.IsValid);
            _logger.LogInformation("📊 REAL EEPROM reading complete: {Valid}/{Total} devices read successfully", 
                validCount, results.Count);
            
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "💥 Error reading REAL EEPROM from all connected devices");
            return new Dictionary<string, FtdiEepromData>();
        }
    }

    /// <summary>
    /// Check if FTDI device is accessible via D2XX
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
    /// FIXED: Utilise GetDeviceList() efficace avec paramètre ref
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
                    
                    // FIXED: Use 'ref' parameter for GetNumberOfDevices
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
                    
                    // FIXED: Get device info list using proper API
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
}