// ===================================================================
// NEW SPRINT 8: FtdiEepromData.cs - EEPROM Data Model
// File: SerialPortPool.Core/Models/FtdiEepromData.cs
// Purpose: EXTENDED FTDI EEPROM data via FTD2XX_NET API
// ===================================================================

namespace SerialPortPool.Core.Models;

/// <summary>
/// EXTENDED FTDI EEPROM data via FTD2XX_NET API
/// SPRINT 8 FEATURE: Contains ProductDescription for dynamic BIB selection
/// PHILOSOPHY: Supplements existing WMI data, doesn't replace it
/// Contains fields NOT available through WMI queries
/// </summary>
public class FtdiEepromData
{
    /// <summary>
    /// FTDI device serial number (unique identifier)
    /// </summary>
    public string SerialNumber { get; set; } = string.Empty;
    
    // ✨ KEY CLIENT FIELDS for Sprint 8 (not populated by WMI)
    /// <summary>
    /// CRITICAL FIELD: ProductDescription from EEPROM for dynamic BIB selection
    /// This is the PRIMARY field requested by client for automatic BIB mapping
    /// </summary>
    public string ProductDescription { get; set; } = string.Empty;
    
    /// <summary>
    /// Enhanced manufacturer from EEPROM (more accurate than WMI)
    /// </summary>
    public string Manufacturer { get; set; } = string.Empty;
    
    // Standard device identification (enhanced from EEPROM)
    /// <summary>
    /// Vendor ID from EEPROM (hexadecimal string)
    /// </summary>
    public string VendorId { get; set; } = string.Empty;
    
    /// <summary>
    /// Product ID from EEPROM (hexadecimal string) 
    /// </summary>
    public string ProductId { get; set; } = string.Empty;
    
    // ✨ BONUS: EEPROM-specific fields (not available in WMI)
    /// <summary>
    /// Maximum power consumption in mA
    /// </summary>
    public int MaxPower { get; set; }
    
    /// <summary>
    /// Device is self-powered (not bus-powered)
    /// </summary>
    public bool SelfPowered { get; set; }
    
    /// <summary>
    /// Device supports remote wakeup
    /// </summary>
    public bool RemoteWakeup { get; set; }
    
    /// <summary>
    /// Device supports pull-down enables
    /// </summary>
    public bool PullDownEnable { get; set; }
    
    /// <summary>
    /// Serial number is enabled in EEPROM
    /// </summary>
    public bool SerNumEnable { get; set; }
    
    /// <summary>
    /// USB version supported (e.g., "2.0")
    /// </summary>
    public string USBVersion { get; set; } = string.Empty;
    
    // Metadata and validation
    /// <summary>
    /// When this EEPROM data was read
    /// </summary>
    public DateTime ReadAt { get; set; } = DateTime.Now;
    
    /// <summary>
    /// Source of this data (always "FTD2XX_NET_EEPROM")
    /// </summary>
    public string Source { get; set; } = "FTD2XX_NET_EEPROM";
    
    /// <summary>
    /// Any error message from EEPROM reading
    /// </summary>
    public string? ErrorMessage { get; set; }
    
    /// <summary>
    /// EEPROM data is valid and reliable
    /// </summary>
    public bool IsValid => !string.IsNullOrEmpty(SerialNumber) && 
                          !string.IsNullOrEmpty(ProductDescription) && 
                          string.IsNullOrEmpty(ErrorMessage);
    
    /// <summary>
    /// Age of this EEPROM data
    /// </summary>
    public TimeSpan Age => DateTime.Now - ReadAt;
    
    /// <summary>
    /// EEPROM data is fresh (less than 5 minutes old)
    /// </summary>
    public bool IsFresh => Age.TotalMinutes < 5;
    
    /// <summary>
    /// Additional raw EEPROM fields as key-value pairs
    /// For storing any extra fields not explicitly modeled
    /// </summary>
    public Dictionary<string, string> AdditionalFields { get; set; } = new();
    
    /// <summary>
    /// Create EEPROM data with error state
    /// </summary>
    /// <param name="serialNumber">Device serial number</param>
    /// <param name="errorMessage">Error message</param>
    /// <returns>EEPROM data in error state</returns>
    public static FtdiEepromData CreateError(string serialNumber, string errorMessage)
    {
        return new FtdiEepromData
        {
            SerialNumber = serialNumber,
            ErrorMessage = errorMessage,
            ReadAt = DateTime.Now,
            Source = "FTD2XX_NET_EEPROM_ERROR"
        };
    }
    
    /// <summary>
    /// Get summary of EEPROM data for logging
    /// </summary>
    /// <returns>Formatted summary string</returns>
    public string GetSummary()
    {
        if (!IsValid)
        {
            return $"❌ EEPROM Error: {ErrorMessage} (Serial: {SerialNumber})";
        }
        
        var age = IsFresh ? "Fresh" : $"{Age.TotalMinutes:F0}min old";
        return $"✅ EEPROM: {SerialNumber} - '{ProductDescription}' by {Manufacturer} ({age})";
    }
    
    public override string ToString()
    {
        return GetSummary();
    }
    
    public override bool Equals(object? obj)
    {
        return obj is FtdiEepromData other && SerialNumber == other.SerialNumber;
    }
    
    public override int GetHashCode()
    {
        return SerialNumber.GetHashCode();
    }
}