// SerialPortPool.Core/Models/FtdiDeviceInfo.cs - FIXED pour FT4232HL PID 6048
namespace SerialPortPool.Core.Models;

/// <summary>
/// Detailed information about an FTDI device
/// UPDATED: Enhanced support for FT4232HL (PID 6048) and improved device detection
/// </summary>
public class FtdiDeviceInfo
{
    /// <summary>
    /// FTDI Vendor ID (should be 0x0403 for genuine FTDI devices)
    /// </summary>
    public string VendorId { get; set; } = string.Empty;
    
    /// <summary>
    /// FTDI Product ID (identifies the specific chip type)
    /// </summary>
    public string ProductId { get; set; } = string.Empty;
    
    /// <summary>
    /// Human-readable chip type (e.g., "FT232R", "FT4232H", "FT4232HL", "FT232H")
    /// </summary>
    public string ChipType { get; set; } = string.Empty;
    
    /// <summary>
    /// Manufacturer name from device descriptor
    /// </summary>
    public string Manufacturer { get; set; } = string.Empty;
    
    /// <summary>
    /// Product description from device descriptor
    /// </summary>
    public string ProductDescription { get; set; } = string.Empty;
    
    /// <summary>
    /// Device serial number (unique identifier)
    /// </summary>
    public string SerialNumber { get; set; } = string.Empty;
    
    /// <summary>
    /// Raw device ID from Windows (e.g., FTDIBUS\VID_0403+PID_6048+SERIALNUMBER\0000)
    /// </summary>
    public string RawDeviceId { get; set; } = string.Empty;
    
    /// <summary>
    /// Whether this is a genuine FTDI device (VID_0403)
    /// </summary>
    public bool IsGenuineFtdi => VendorId.Equals("0403", StringComparison.OrdinalIgnoreCase) ||
                                 VendorId.Equals("VID_0403", StringComparison.OrdinalIgnoreCase);
    
    /// <summary>
    /// Whether this is a multi-port device (like FT4232H/HL which has 4 ports, FT2232H which has 2 ports)
    /// </summary>
    public bool IsMultiPortDevice => ChipType.Contains("4232") || ChipType.Contains("2232");
    
    /// <summary>
    /// Whether this is specifically a 4232H or 4232HL chip (client requirement)
    /// ENHANCED: Now supports both FT4232H (6011) and FT4232HL (6048)
    /// </summary>
    public bool Is4232H => ChipType.Equals("FT4232H", StringComparison.OrdinalIgnoreCase) ||
                          ChipType.Equals("FT4232HL", StringComparison.OrdinalIgnoreCase) ||
                          ProductId.Equals("6011", StringComparison.OrdinalIgnoreCase) ||
                          ProductId.Equals("6048", StringComparison.OrdinalIgnoreCase); // ← FT4232HL support
    
    /// <summary>
    /// Additional data from EEPROM if available
    /// </summary>
    public Dictionary<string, string> EepromData { get; set; } = new();
    
    /// <summary>
    /// Parse FTDI device information from Windows Device ID
    /// </summary>
    /// <param name="deviceId">Windows device ID (e.g., FTDIBUS\VID_0403+PID_6048+SERIALNUMBER\0000)</param>
    /// <returns>Parsed FTDI device info or null if not an FTDI device</returns>
    public static FtdiDeviceInfo? ParseFromDeviceId(string deviceId)
    {
        if (string.IsNullOrWhiteSpace(deviceId) || !deviceId.Contains("VID_0403"))
        {
            return null; // Not an FTDI device
        }
        
        var ftdiInfo = new FtdiDeviceInfo
        {
            RawDeviceId = deviceId
        };
        
        // Parse VID (should be 0403 for FTDI)
        var vidMatch = System.Text.RegularExpressions.Regex.Match(deviceId, @"VID_([0-9A-Fa-f]{4})");
        if (vidMatch.Success)
        {
            ftdiInfo.VendorId = vidMatch.Groups[1].Value;
        }
        
        // Parse PID (identifies chip type)
        var pidMatch = System.Text.RegularExpressions.Regex.Match(deviceId, @"PID_([0-9A-Fa-f]{4})");
        if (pidMatch.Success)
        {
            ftdiInfo.ProductId = pidMatch.Groups[1].Value;
            ftdiInfo.ChipType = GetChipTypeFromPid(ftdiInfo.ProductId);
        }
        
        // Parse Serial Number
        var serialMatch = System.Text.RegularExpressions.Regex.Match(deviceId, @"PID_[0-9A-Fa-f]{4}\+([^\\]+)");
        if (serialMatch.Success)
        {
            ftdiInfo.SerialNumber = serialMatch.Groups[1].Value;
        }
        
        return ftdiInfo;
    }
    
    /// <summary>
    /// Get human-readable chip type from Product ID
    /// ENHANCED: Added support for FT4232HL (6048) and improved detection
    /// </summary>
    private static string GetChipTypeFromPid(string productId)
    {
        return productId.ToUpper() switch
        {
            "6001" => "FT232R",        // Single port, common USB-to-serial
            "6011" => "FT4232H",       // 4-port, high speed (original)
            "6048" => "FT4232HL",      // 4-port, high speed, low power ← ENHANCED: Added FT4232HL support
            "6014" => "FT232H",        // Single port, high speed
            "6010" => "FT2232H",       // 2-port, high speed
            "6015" => "FT X-Series",   // Various X-Series chips
            "0000" => "FT8U232AM",     // Legacy chip
            "FC08" => "FT232BM",       // Legacy chip
            "FC09" => "FT232RL",       // Legacy chip
            _ => $"Unknown FTDI Chip (PID: {productId})"
        };
    }
    
    /// <summary>
    /// Get expected number of ports for this chip type
    /// </summary>
    public int ExpectedPortCount => ChipType switch
    {
        "FT4232H" => 4,
        "FT4232HL" => 4,  // ← NEW: FT4232HL also has 4 ports
        "FT2232H" => 2,
        "FT232R" => 1,
        "FT232H" => 1,
        _ => 1
    };
    
    /// <summary>
    /// Whether this chip supports high-speed USB 2.0 (480Mbps)
    /// </summary>
    public bool IsHighSpeedCapable => ChipType switch
    {
        "FT4232H" => true,
        "FT4232HL" => true,  // ← NEW: FT4232HL is high-speed capable
        "FT2232H" => true,
        "FT232H" => true,
        _ => false
    };
    
    /// <summary>
    /// Get detailed device description for logging/display
    /// </summary>
    public virtual string GetDetailedDescription()
    {
        var speed = IsHighSpeedCapable ? "High-Speed" : "Full-Speed";
        var ports = ExpectedPortCount > 1 ? $"{ExpectedPortCount}-Port" : "Single-Port";
        var clientValid = Is4232H ? "✅ CLIENT-VALID" : "❌ NOT CLIENT-VALID";
        
        return $"{ChipType} ({ports}, {speed} USB) - {clientValid}";
    }
    
    public override string ToString()
    {
        var status = Is4232H ? "✅ VALID (4232H/4232HL)" : "❌ INVALID (Not 4232H/HL)";
        return $"FTDI {ChipType} (VID: {VendorId}, PID: {ProductId}) - {status}";
    }
}