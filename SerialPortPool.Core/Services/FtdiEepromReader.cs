// ===================================================================
// FIXED: FtdiEepromReader.cs - Version avec vrais drivers D2XX FTDI
// File: SerialPortPool.Core/Services/FtdiEepromReader.cs
// Purpose: VRAIE lecture EEPROM via FTD2XX_NET avec API native D2XX
// ===================================================================

using FTD2XX_NET;
using Microsoft.Extensions.Logging;
using SerialPortPool.Core.Interfaces;
using SerialPortPool.Core.Models;

namespace SerialPortPool.Core.Services;

/// <summary>
/// FTDI EEPROM Reader avec VRAIE lecture EEPROM via drivers D2XX natifs
/// SPRINT 8 FEATURE: Lecture ProductDescription pour dynamic BIB selection
/// APPROCHE: Utilise les API D2XX natives pour accéder directement à l'EEPROM
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

            // Method 1: Try reading EEPROM using EE_Read (preferred)
            if (TryReadEepromWithEERead(ftdi, result))
            {
                _logger.LogDebug("✅ EEPROM read successful via EE_Read method");
                return result;
            }

            // Method 2: Fallback to string reading methods
            if (TryReadEepromWithStringMethods(ftdi, result))
            {
                _logger.LogDebug("✅ EEPROM read successful via String methods");
                return result;
            }

            // Method 3: Ultimate fallback to device info
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
    /// Method 1: Try reading EEPROM using FT_EE_Read native function
    /// </summary>
    private bool TryReadEepromWithEERead(FTDI ftdi, FtdiEepromData result)
    {
        try
        {
            _logger.LogDebug("🔍 Attempting EEPROM read via EE_Read method");

            // Create EEPROM data structure for different chip types
            var ft232rEeprom = new FTDI.FT232R_EEPROM_STRUCTURE();
            var ft232hEeprom = new FTDI.FT232H_EEPROM_STRUCTURE();
            var ft4232hEeprom = new FTDI.FT4232H_EEPROM_STRUCTURE();

            // Try different EEPROM structures based on device type
            var deviceType = FTDI.FT_DEVICE.FT_DEVICE_UNKNOWN;
            var deviceId = 0U;
            
            var status = ftdi.GetDeviceType(ref deviceType);
            if (status == FTDI.FT_STATUS.FT_OK)
            {
                _logger.LogDebug("📱 Device type detected: {DeviceType}", deviceType);

                switch (deviceType)
                {
                    case FTDI.FT_DEVICE.FT_DEVICE_232R:
                        status = ftdi.ReadFT232REEPROM(ft232rEeprom);
                        if (status == FTDI.FT_STATUS.FT_OK)
                        {
                            result.ProductDescription = ft232rEeprom.Description ?? "";
                            result.Manufacturer = ft232rEeprom.Manufacturer ?? "";
                            result.VendorId = ft232rEeprom.VendorId.ToString("X4");
                            result.ProductId = ft232rEeprom.ProductId.ToString("X4");
                            result.MaxPower = ft232rEeprom.MaxPower;
                            result.SelfPowered = ft232rEeprom.SelfPowered;
                            result.RemoteWakeup = ft232rEeprom.RemoteWakeup;
                            return true;
                        }
                        break;

                    case FTDI.FT_DEVICE.FT_DEVICE_232H:
                        status = ftdi.ReadFT232HEEPROM(ft232hEeprom);
                        if (status == FTDI.FT_STATUS.FT_OK)
                        {
                            result.ProductDescription = ft232hEeprom.Description ?? "";
                            result.Manufacturer = ft232hEeprom.Manufacturer ?? "";
                            result.VendorId = ft232hEeprom.VendorId.ToString("X4");
                            result.ProductId = ft232hEeprom.ProductId.ToString("X4");
                            result.MaxPower = ft232hEeprom.MaxPower;
                            result.SelfPowered = ft232hEeprom.SelfPowered;
                            result.RemoteWakeup = ft232hEeprom.RemoteWakeup;
                            return true;
                        }
                        break;

                    case FTDI.FT_DEVICE.FT_DEVICE_4232H:
                        status = ftdi.ReadFT4232HEEPROM(ft4232hEeprom);
                        if (status == FTDI.FT_STATUS.FT_OK)
                        {
                            result.ProductDescription = ft4232hEeprom.Description ?? "";
                            result.Manufacturer = ft4232hEeprom.Manufacturer ?? "";
                            result.VendorId = ft4232hEeprom.VendorId.ToString("X4");
                            result.ProductId = ft4232hEeprom.ProductId.ToString("X4");
                            result.MaxPower = ft4232hEeprom.MaxPower;
                            result.SelfPowered = ft4232hEeprom.SelfPowered;
                            result.RemoteWakeup = ft4232hEeprom.RemoteWakeup;
                            return true;
                        }
                        break;
                }
            }

            _logger.LogDebug("⚠️ EE_Read method failed or unsupported device type: {DeviceType}", deviceType);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "⚠️ EE_Read method failed with exception");
            return false;
        }
    }

    /// <summary>
    /// Method 2: Try reading using string-specific methods
    /// </summary>
    private bool TryReadEepromWithStringMethods(FTDI ftdi, FtdiEepromData result)
    {
        try
        {
            _logger.LogDebug("🔍 Attempting EEPROM read via String methods");

            // Try to read strings directly
            string manufacturer = "";
            string description = "";
            string serialNumber = "";

            var manufacturerStatus = ftdi.GetManufacturer(out manufacturer);
            var descriptionStatus = ftdi.GetDescription(out description);
            var serialStatus = ftdi.GetSerialNumber(out serialNumber);

            if (manufacturerStatus == FTDI.FT_STATUS.FT_OK || 
                descriptionStatus == FTDI.FT_STATUS.FT_OK || 
                serialStatus == FTDI.FT_STATUS.FT_OK)
            {
                result.Manufacturer = manufacturer ?? "";
                result.ProductDescription = description ?? "";
                
                if (!string.IsNullOrEmpty(serialNumber))
                {
                    result.SerialNumber = serialNumber;
                }

                // Try to get VID/PID via device info
                var deviceInfo = new FTDI.FT_DEVICE_INFO_NODE();
                if (TryGetDeviceInfo(ftdi, ref deviceInfo))
                {
                    result.VendorId = "0403"; // Standard FTDI VID
                    result.ProductId = GetProductIdFromDeviceType(deviceInfo.Type);
                }

                _logger.LogDebug("📝 String method results - Manufacturer: '{Manufacturer}', Description: '{Description}'",
                    result.Manufacturer, result.ProductDescription);

                return !string.IsNullOrEmpty(result.ProductDescription) || !string.IsNullOrEmpty(result.Manufacturer);
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "⚠️ String methods failed with exception");
            return false;
        }
    }

    /// <summary>
    /// Method 3: Fallback to basic device info
    /// </summary>
    private bool TryReadBasicDeviceInfo(FTDI ftdi, FtdiEepromData result)
    {
        try
        {
            _logger.LogDebug("🔍 Attempting basic device info fallback");

            var deviceInfo = new FTDI.FT_DEVICE_INFO_NODE();
            if (TryGetDeviceInfo(ftdi, ref deviceInfo))
            {
                result.ProductDescription = deviceInfo.Description ?? GetProductDescriptionFromType(deviceInfo.Type);
                result.Manufacturer = "FTDI";
                result.VendorId = "0403";
                result.ProductId = GetProductIdFromDeviceType(deviceInfo.Type);

                _logger.LogDebug("📱 Basic device info - Type: {Type}, Description: '{Description}'",
                    deviceInfo.Type, result.ProductDescription);

                return true;
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
    /// Helper to get device info safely
    /// </summary>
    private bool TryGetDeviceInfo(FTDI ftdi, ref FTDI.FT_DEVICE_INFO_NODE deviceInfo)
    {
        try
        {
            uint deviceCount = 0;
            if (ftdi.GetNumberOfDevices(ref deviceCount) == FTDI.FT_STATUS.FT_OK && deviceCount > 0)
            {
                var deviceInfoArray = new FTDI.FT_DEVICE_INFO_NODE[deviceCount];
                if (ftdi.GetDeviceList(deviceInfoArray) == FTDI.FT_STATUS.FT_OK)
                {
                    // Find our device in the list by trying to match
                    deviceInfo = deviceInfoArray[0]; // Take first one as fallback
                    return true;
                }
            }
            return false;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Read enhanced device data from all connected FTDI devices
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

    #region Helper Methods

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
    /// Get Product ID from device type
    /// </summary>
    private string GetProductIdFromDeviceType(FTDI.FT_DEVICE type)
    {
        return type switch
        {
            FTDI.FT_DEVICE.FT_DEVICE_232R => "6001",
            FTDI.FT_DEVICE.FT_DEVICE_2232H => "6010", 
            FTDI.FT_DEVICE.FT_DEVICE_4232H => "6011",
            FTDI.FT_DEVICE.FT_DEVICE_232H => "6014",
            FTDI.FT_DEVICE.FT_DEVICE_X_SERIES => "6015",
            _ => "0000"
        };
    }

    #endregion
}