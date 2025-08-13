// ===================================================================
// SAFE MODE: FtdiEepromReader.cs - No Native EEPROM Structure Calls
// File: SerialPortPool.Core/Services/FtdiEepromReader.cs
// Purpose: STABLE EEPROM reading avoiding crash-prone native structures
// STRATEGY: Use only stable API methods, avoid ReadFTxxxEEPROM() completely
// ===================================================================

using FTD2XX_NET;
using Microsoft.Extensions.Logging;
using SerialPortPool.Core.Interfaces;
using SerialPortPool.Core.Models;

namespace SerialPortPool.Core.Services;

/// <summary>
/// SAFE MODE FTDI EEPROM Reader - Avoids unstable native EEPROM structure calls
/// SPRINT 8 FEATURE: Stable ProductDescription reading for dynamic BIB selection
/// PHILOSOPHY: Stability over completeness - basic info reliably vs full info unreliably
/// </summary>
public class FtdiEepromReader : IFtdiEepromReader
{
    private readonly ILogger<FtdiEepromReader> _logger;

    public FtdiEepromReader(ILogger<FtdiEepromReader> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Read STABLE EEPROM data via D2XX safe methods only
    /// SAFE MODE: Avoids crash-prone ReadFTxxxEEPROM() native calls
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
            _logger.LogDebug("🛡️ SAFE MODE: Reading EEPROM data via stable D2XX methods: {SerialNumber}", serialNumber);

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

                    // Step 2: SAFE MODE - Use only stable methods
                    var result = ReadStableEepromData(ftdi, serialNumber);
                    
                    _logger.LogInformation("✅ SAFE EEPROM data read - ProductDescription: '{ProductDescription}', Manufacturer: '{Manufacturer}'", 
                        result.ProductDescription, result.Manufacturer);
                    
                    return result;
                }
                catch (Exception ex)
                {
                    var error = $"Exception reading SAFE EEPROM from {serialNumber}: {ex.Message}";
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
            var error = $"Unexpected error reading SAFE EEPROM from {serialNumber}: {ex.Message}";
            _logger.LogError(ex, "💥 {Error}", error);
            return FtdiEepromData.CreateError(serialNumber, error);
        }
    }

    /// <summary>
    /// SAFE MODE: Read EEPROM data using only stable D2XX API methods
    /// NO native structure calls to prevent crashes
    /// </summary>
    private FtdiEepromData ReadStableEepromData(FTDI ftdi, string serialNumber)
    {
        try
        {
            var result = new FtdiEepromData
            {
                SerialNumber = serialNumber,
                ReadAt = DateTime.Now,
                Source = "D2XX_SAFE_MODE"
            };

            _logger.LogDebug("🛡️ Using SAFE MODE - stable API methods only");

            // SAFE: Read VID/PID via GetDeviceID (stable)
            if (ReadVidPidSafe(ftdi, result))
            {
                _logger.LogDebug("✅ VID/PID read successfully");
            }

            // SAFE: Read basic device info (stable)
            if (ReadBasicDeviceInfoSafe(ftdi, result))
            {
                _logger.LogDebug("✅ Basic device info read successfully");
            }

            // SAFE: Set reasonable defaults for EEPROM-specific fields
            SetSafeDefaults(result);

            // Validate we got useful data
            if (string.IsNullOrEmpty(result.ProductDescription) && string.IsNullOrEmpty(result.VendorId))
            {
                _logger.LogWarning("⚠️ Minimal EEPROM data available for {SerialNumber}", serialNumber);
                result.ProductDescription = "FTDI Device"; // Safe fallback
                result.Manufacturer = "FTDI";
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "💥 Error in ReadStableEepromData for {SerialNumber}", serialNumber);
            return FtdiEepromData.CreateError(serialNumber, $"Safe EEPROM read error: {ex.Message}");
        }
    }

    /// <summary>
    /// SAFE: Read VID/PID using GetDeviceID() - proven stable method
    /// </summary>
    private bool ReadVidPidSafe(FTDI ftdi, FtdiEepromData result)
    {
        try
        {
            _logger.LogDebug("🔍 SAFE: Reading VID/PID via GetDeviceID()");

            uint deviceId = 0;
            var status = ftdi.GetDeviceID(ref deviceId);
            
            if (status == FTDI.FT_STATUS.FT_OK)
            {
                // Extract VID/PID using bit manipulation (safe and reliable)
                ushort vendorId = (ushort)(deviceId >> 16);    // Upper 16 bits = VID
                ushort productId = (ushort)(deviceId & 0xFFFF); // Lower 16 bits = PID
                
                result.VendorId = vendorId.ToString("X4");
                result.ProductId = productId.ToString("X4");
                
                _logger.LogDebug("✅ VID/PID extracted safely: {VendorId}/{ProductId}", result.VendorId, result.ProductId);
                return true;
            }
            
            _logger.LogDebug("⚠️ GetDeviceID failed: {Status}", status);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "⚠️ Error reading VID/PID safely");
            return false;
        }
    }

    /// <summary>
    /// SAFE: Read basic device info using stable string methods
    /// </summary>
    private bool ReadBasicDeviceInfoSafe(FTDI ftdi, FtdiEepromData result)
    {
        try
        {
            _logger.LogDebug("🔍 SAFE: Reading basic device info");

            bool anySuccess = false;

            // SAFE: Try GetDescription (usually stable)
            try
            {
                string description = "";
                var descStatus = ftdi.GetDescription(out description);
                
                if (descStatus == FTDI.FT_STATUS.FT_OK && !string.IsNullOrWhiteSpace(description))
                {
                    result.ProductDescription = description;
                    anySuccess = true;
                    _logger.LogDebug("✅ Description read: '{Description}'", description);
                }
                else
                {
                    _logger.LogDebug("⚠️ GetDescription failed or empty: {Status}", descStatus);
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "⚠️ GetDescription exception");
            }

            // SAFE: Try GetSerialNumber (usually stable)
            try
            {
                string serialNumber = "";
                var serialStatus = ftdi.GetSerialNumber(out serialNumber);
                
                if (serialStatus == FTDI.FT_STATUS.FT_OK && !string.IsNullOrWhiteSpace(serialNumber))
                {
                    // Verify serial matches expected
                    if (serialNumber == result.SerialNumber || string.IsNullOrEmpty(result.SerialNumber))
                    {
                        result.SerialNumber = serialNumber;
                        anySuccess = true;
                        _logger.LogDebug("✅ Serial number confirmed: '{SerialNumber}'", serialNumber);
                    }
                }
                else
                {
                    _logger.LogDebug("⚠️ GetSerialNumber failed: {Status}", serialStatus);
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "⚠️ GetSerialNumber exception");
            }

            // SAFE: Set basic manufacturer
            result.Manufacturer = "FTDI"; // Safe default

            _logger.LogDebug("📱 SAFE basic info - Description: '{Description}', Serial: '{Serial}'",
                result.ProductDescription, result.SerialNumber);

            return anySuccess;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "⚠️ Safe basic device info failed");
            return false;
        }
    }

    /// <summary>
    /// SAFE: Set reasonable defaults for EEPROM-specific fields
    /// Since we can't read EEPROM structures safely, use educated defaults
    /// </summary>
    private void SetSafeDefaults(FtdiEepromData result)
    {
        try
        {
            // Set safe defaults based on chip type (inferred from PID)
            if (!string.IsNullOrEmpty(result.ProductId))
            {
                switch (result.ProductId.ToUpper())
                {
                    case "6001": // FT232R
                        result.MaxPower = 90;  // Typical for FT232R
                        result.SelfPowered = false;
                        break;
                        
                    case "6011": // FT4232H
                    case "6048": // FT4232HL
                        result.MaxPower = 500; // Typical for FT4232H
                        result.SelfPowered = false;
                        break;
                        
                    case "6014": // FT232H
                        result.MaxPower = 500; // Typical for FT232H
                        result.SelfPowered = false;
                        break;
                        
                    default:
                        result.MaxPower = 100; // Conservative default
                        result.SelfPowered = false;
                        break;
                }
            }
            else
            {
                // Unknown device - ultra-safe defaults
                result.MaxPower = 100;
                result.SelfPowered = false;
            }

            // Common safe defaults
            result.RemoteWakeup = false;  // Conservative default
            result.SerNumEnable = true;   // Usually enabled
            
            _logger.LogDebug("🛡️ Safe defaults applied: MaxPower={MaxPower}mA, SelfPowered={SelfPowered}", 
                result.MaxPower, result.SelfPowered);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "⚠️ Error setting safe defaults");
        }
    }

    /// <summary>
    /// Read enhanced device data from all connected FTDI devices - SAFE MODE
    /// </summary>
    public async Task<Dictionary<string, FtdiEepromData>> ReadAllConnectedDevicesAsync()
    {
        try
        {
            _logger.LogInformation("🛡️ SAFE MODE: Discovering all connected FTDI devices...");
            
            var serialNumbers = await GetConnectedDeviceSerialNumbersAsync();
            var results = new Dictionary<string, FtdiEepromData>();
            
            _logger.LogInformation("📋 Found {Count} FTDI devices to read via SAFE MODE", serialNumbers.Count);
            
            foreach (var serialNumber in serialNumbers)
            {
                var deviceData = await ReadEepromAsync(serialNumber);
                results[serialNumber] = deviceData;
                
                if (deviceData.IsValid)
                {
                    _logger.LogDebug("✅ SAFE EEPROM data read: {SerialNumber} → '{ProductDescription}'", 
                        serialNumber, deviceData.ProductDescription);
                }
                else
                {
                    _logger.LogWarning("❌ SAFE EEPROM read failed: {SerialNumber} → {Error}", 
                        serialNumber, deviceData.ErrorMessage);
                }
            }
            
            var validCount = results.Values.Count(d => d.IsValid);
            _logger.LogInformation("📊 SAFE EEPROM reading complete: {Valid}/{Total} devices read successfully", 
                validCount, results.Count);
            
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "💥 Error reading SAFE EEPROM from all connected devices");
            return new Dictionary<string, FtdiEepromData>();
        }
    }

    /// <summary>
    /// Check if FTDI device is accessible via D2XX - SAFE MODE
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
                    
                    _logger.LogDebug("🔍 SAFE device accessibility: {SerialNumber} → {Accessible}", 
                        serialNumber, accessible ? "✅ Accessible" : "❌ Not accessible");
                    
                    return accessible;
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "⚠️ SAFE accessibility check failed: {SerialNumber}", serialNumber);
                    return false;
                }
                finally
                {
                    try { ftdi.Close(); } catch { /* Ignore close errors */ }
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "⚠️ Error in safe accessibility check: {SerialNumber}", serialNumber);
            return false;
        }
    }

    /// <summary>
    /// Get list of all connected FTDI device serial numbers - SAFE MODE
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
                    _logger.LogDebug("🔍 SAFE: Scanning for connected FTDI devices...");
                    
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
                    
                    // Get device info list using stable API
                    var deviceInfoArray = new FTDI.FT_DEVICE_INFO_NODE[deviceCount];
                    status = ftdi.GetDeviceList(deviceInfoArray);
                    
                    if (status != FTDI.FT_STATUS.FT_OK)
                    {
                        _logger.LogWarning("⚠️ Cannot get FTDI device list: {Status}", status);
                        return serialNumbers;
                    }
                    
                    // Extract serial numbers safely
                    foreach (var deviceInfo in deviceInfoArray)
                    {
                        if (!string.IsNullOrEmpty(deviceInfo.SerialNumber))
                        {
                            serialNumbers.Add(deviceInfo.SerialNumber);
                            _logger.LogDebug("📱 SAFE: Found FTDI device: {SerialNumber} (Type: {Type}, Description: {Description})", 
                                deviceInfo.SerialNumber, deviceInfo.Type, deviceInfo.Description);
                        }
                    }
                    
                    _logger.LogInformation("✅ SAFE FTDI device discovery complete: {Count} devices found", serialNumbers.Count);
                    return serialNumbers;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "💥 Error during SAFE FTDI device discovery");
                    return serialNumbers;
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "💥 Unexpected error during SAFE FTDI device discovery");
            return new List<string>();
        }
    }

    /// <summary>
    /// Read specific device field by name - SAFE MODE
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