// ===================================================================
// NEW SPRINT 8: IFtdiEepromReader.cs - EEPROM Reading Interface
// File: SerialPortPool.Core/Interfaces/IFtdiEepromReader.cs
// Purpose: FTDI EEPROM access via FTD2XX_NET for Dynamic BIB Selection
// ===================================================================

using SerialPortPool.Core.Models;

namespace SerialPortPool.Core.Interfaces;

/// <summary>
/// Interface for reading FTDI EEPROM data via FTD2XX_NET API
/// SPRINT 8 FEATURE: Dynamic BIB Selection based on ProductDescription
/// PHILOSOPHY: Supplements existing WMI-based system (ADDITIVE approach)
/// </summary>
public interface IFtdiEepromReader
{
    /// <summary>
    /// Read EEPROM data from FTDI device by serial number
    /// PRIMARY METHOD for dynamic BIB selection
    /// </summary>
    /// <param name="serialNumber">FTDI device serial number</param>
    /// <returns>EEPROM data including ProductDescription for BIB mapping</returns>
    Task<FtdiEepromData> ReadEepromAsync(string serialNumber);
    
    /// <summary>
    /// Read EEPROM data from all connected FTDI devices
    /// Useful for system discovery and inventory
    /// </summary>
    /// <returns>Dictionary of SerialNumber → EEPROM data</returns>
    Task<Dictionary<string, FtdiEepromData>> ReadAllConnectedDevicesAsync();
    
    /// <summary>
    /// Check if FTDI device is accessible via FTD2XX_NET
    /// Pre-validation before attempting EEPROM read
    /// </summary>
    /// <param name="serialNumber">FTDI device serial number</param>
    /// <returns>True if device can be opened and read</returns>
    Task<bool> IsDeviceAccessibleAsync(string serialNumber);
    
    /// <summary>
    /// Get list of all connected FTDI device serial numbers
    /// Discovery method for available devices
    /// </summary>
    /// <returns>List of FTDI serial numbers currently connected</returns>
    Task<List<string>> GetConnectedDeviceSerialNumbersAsync();
    
    /// <summary>
    /// Read specific EEPROM field by name
    /// For advanced scenarios requiring specific data
    /// </summary>
    /// <param name="serialNumber">FTDI device serial number</param>
    /// <param name="fieldName">EEPROM field name (e.g., "ProductDescription")</param>
    /// <returns>Field value or empty string if not found</returns>
    Task<string> ReadEepromFieldAsync(string serialNumber, string fieldName);
}