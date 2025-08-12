// ===================================================================
// NEW SPRINT 8: FtdiEepromReader.cs - FTD2XX_NET Implementation  
// File: SerialPortPool.Core/Services/FtdiEepromReader.cs
// Purpose: FTDI EEPROM access via FTD2XX_NET for Dynamic BIB Selection
// ===================================================================

using FTD2XX_NET;
using Microsoft.Extensions.Logging;
using SerialPortPool.Core.Interfaces;
using SerialPortPool.Core.Models;

namespace SerialPortPool.Core.Services;

/// <summary>
/// FTDI EEPROM Reader using FTD2XX_NET API
/// SPRINT 8 FEATURE: Reads ProductDescription for dynamic BIB selection
/// PHILOSOPHY: "Changement minimum" - supplements existing WMI system
/// </summary>
public class FtdiEepromReader : IFtdiEepromReader
{
    private readonly ILogger<FtdiEepromReader> _logger;

    public FtdiEepromReader(ILogger<FtdiEepromReader> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Read EXTENDED EEPROM data via FTD2XX_NET (supplements existing WMI data)
    /// PRIMARY METHOD for dynamic BIB selection based on ProductDescription
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
            _logger.LogDebug("🔬 Reading EXTENDED EEPROM data via FTD2XX_NET: {SerialNumber}", serialNumber);

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

                    // Step 2: Read EEPROM data via native API
                    _logger.LogDebug("📖 Reading EEPROM data from device...");
                    
                    var eepromData = new FTDI.FT_EEPROM_DATA();
                    status = ftdi.ReadEEPROM(eepromData);
                    
                    if (status != FTDI.FT_STATUS.FT_OK)
                    {
                        var error = $"Cannot read EEPROM from {serialNumber}: {status}";
                        _logger.LogWarning("❌ {Error}", error);
                        return FtdiEepromData.CreateError(serialNumber, error);
                    }

                    // Step 3: Extract and validate data
                    var result = ExtractEepromData(eepromData, serialNumber);
                    
                    _logger.LogInformation("✅ EEPROM read successful - ProductDescription: '{ProductDescription}', Manufacturer: '{Manufacturer}'", 
                        result.ProductDescription, result.Manufacturer);
                    
                    return result;
                }
                catch (Exception ex)
                {
                    var error = $"Exception reading EEPROM from {serialNumber}: {ex.Message}";
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
            var error = $"Unexpected error reading EEPROM from {serialNumber}: {ex.Message}";
            _logger.LogError(ex, "💥 {Error}", error);
            return FtdiEepromData.CreateError(serialNumber, error);
        }
    }

    /// <summary>
    /// Read EEPROM data from all connected FTDI devices
    /// </summary>
    public async Task<Dictionary<string, FtdiEepromData>> ReadAllConnectedDevicesAsync()
    {
        try
        {
            _logger.LogInformation("🔍 Discovering all connected FTDI devices for EEPROM reading...");
            
            var serialNumbers = await GetConnectedDeviceSerialNumbersAsync();
            var results = new Dictionary<string, FtdiEepromData>();
            
            _logger.LogInformation("📋 Found {Count} FTDI devices to read", serialNumbers.Count);
            
            foreach (var serialNumber in serialNumbers)
            {
                var eepromData = await ReadEepromAsync(serialNumber);
                results[serialNumber] = eepromData;
                
                if (eepromData.IsValid)
                {
                    _logger.LogDebug("✅ EEPROM read completed: {SerialNumber} → '{ProductDescription}'", 
                        serialNumber, eepromData.ProductDescription);
                }
                else
                {
                    _logger.LogWarning("❌ EEPROM read failed: {SerialNumber} → {Error}", 
                        serialNumber, eepromData.ErrorMessage);
                }
            }
            
            var validCount = results.Values.Count(d => d.IsValid);
            _logger.LogInformation("📊 EEPROM reading complete: {Valid}/{Total} devices read successfully", 
                validCount, results.Count);
            
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "💥 Error reading EEPROM from all connected devices");
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
    /// Read specific EEPROM field by name
    /// </summary>
    public async Task<string> ReadEepromFieldAsync(string serialNumber, string fieldName)
    {
        try
        {
            var eepromData = await ReadEepromAsync(serialNumber);
            
            if (!eepromData.IsValid)
            {
                _logger.LogWarning("⚠️ Cannot read field '{Field}' - EEPROM data invalid for {SerialNumber}", 
                    fieldName, serialNumber);
                return string.Empty;
            }
            
            return fieldName.ToLowerInvariant() switch
            {
                "productdescription" => eepromData.ProductDescription,
                "manufacturer" => eepromData.Manufacturer,
                "vendorid" => eepromData.VendorId,
                "productid" => eepromData.ProductId,
                "serialnumber" => eepromData.SerialNumber,
                "maxpower" => eepromData.MaxPower.ToString(),
                "selfpowered" => eepromData.SelfPowered.ToString(),
                "remotewakeup" => eepromData.RemoteWakeup.ToString(),
                _ => eepromData.AdditionalFields.GetValueOrDefault(fieldName, string.Empty)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "💥 Error reading EEPROM field '{Field}' from {SerialNumber}", fieldName, serialNumber);
            return string.Empty;
        }
    }

    #region Private Helper Methods

    /// <summary>
    /// Extract structured data from FTDI EEPROM structure
    /// </summary>
    private FtdiEepromData ExtractEepromData(FTDI.FT_EEPROM_DATA eepromData, string serialNumber)
    {
        try
        {
            var result = new FtdiEepromData
            {
                SerialNumber = serialNumber,
                
                // ✨ KEY FIELDS: Custom EEPROM fields (not available via WMI)
                ProductDescription = eepromData.ProductDescription ?? string.Empty,
                Manufacturer = eepromData.Manufacturer ?? string.Empty,
                
                // Standard identification
                VendorId = eepromData.VendorId.ToString("X4"),
                ProductId = eepromData.ProductId.ToString("X4"),
                
                // ✨ BONUS: Additional EEPROM-specific fields
                MaxPower = eepromData.MaxPower,
                SelfPowered = eepromData.SelfPowered,
                RemoteWakeup = eepromData.RemoteWakeup,
                PullDownEnable = eepromData.PullDownEnable,
                SerNumEnable = eepromData.SerNumEnable,
                USBVersion = eepromData.USBVersion.ToString(),
                
                ReadAt = DateTime.Now,
                Source = "FTD2XX_NET_EEPROM"
            };
            
            // Add any additional fields that might be useful
            result.AdditionalFields["VendorIdDecimal"] = eepromData.VendorId.ToString();
            result.AdditionalFields["ProductIdDecimal"] = eepromData.ProductId.ToString();
            result.AdditionalFields["MaxPowerMa"] = eepromData.MaxPower.ToString();
            result.AdditionalFields["USBVersionHex"] = eepromData.USBVersion.ToString("X4");
            
            _logger.LogDebug("📊 EEPROM data extracted: {Summary}", result.GetSummary());
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "💥 Error extracting EEPROM data for {SerialNumber}", serialNumber);
            return FtdiEepromData.CreateError(serialNumber, $"Data extraction error: {ex.Message}");
        }
    }

    #endregion
}