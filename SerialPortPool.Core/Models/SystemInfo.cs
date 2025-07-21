namespace SerialPortPool.Core.Models;

/// <summary>
/// Extended system information for a serial port device, including EEPROM data and hardware details
/// </summary>
public class SystemInfo
{
    /// <summary>
    /// Device serial number (unique identifier)
    /// </summary>
    public string SerialNumber { get; set; } = string.Empty;

    /// <summary>
    /// Firmware version of the device
    /// </summary>
    public string FirmwareVersion { get; set; } = string.Empty;

    /// <summary>
    /// Hardware revision or version
    /// </summary>
    public string HardwareRevision { get; set; } = string.Empty;

    /// <summary>
    /// Manufacturer name from EEPROM
    /// </summary>
    public string Manufacturer { get; set; } = string.Empty;

    /// <summary>
    /// Product description from EEPROM
    /// </summary>
    public string ProductDescription { get; set; } = string.Empty;

    /// <summary>
    /// Raw EEPROM data as key-value pairs
    /// </summary>
    public Dictionary<string, string> EepromData { get; set; } = new();

    /// <summary>
    /// System-level properties and configuration
    /// </summary>
    public Dictionary<string, string> SystemProperties { get; set; } = new();

    /// <summary>
    /// Client-specific configuration or metadata
    /// </summary>
    public Dictionary<string, string> ClientConfiguration { get; set; } = new();

    /// <summary>
    /// Timestamp when this information was last read from the device
    /// </summary>
    public DateTime LastRead { get; set; } = DateTime.Now;

    /// <summary>
    /// Whether the EEPROM data was successfully read
    /// </summary>
    public bool IsDataValid { get; set; } = true;

    /// <summary>
    /// Error message if EEPROM reading failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// How long ago this data was read (computed property)
    /// </summary>
    public TimeSpan Age => DateTime.Now.Subtract(LastRead);

    /// <summary>
    /// Whether this data should be considered fresh (less than 5 minutes old)
    /// </summary>
    public bool IsFresh => Age.TotalMinutes < 5;

    /// <summary>
    /// Get a client-specific configuration value
    /// </summary>
    /// <param name="key">Configuration key</param>
    /// <returns>Configuration value or null if not found</returns>
    public string? GetClientConfig(string key)
    {
        return ClientConfiguration.TryGetValue(key, out var value) ? value : null;
    }

    /// <summary>
    /// Set a client-specific configuration value
    /// </summary>
    /// <param name="key">Configuration key</param>
    /// <param name="value">Configuration value</param>
    public void SetClientConfig(string key, string value)
    {
        ClientConfiguration[key] = value;
    }

    /// <summary>
    /// Get a system property value
    /// </summary>
    /// <param name="key">Property key</param>
    /// <returns>Property value or null if not found</returns>
    public string? GetSystemProperty(string key)
    {
        return SystemProperties.TryGetValue(key, out var value) ? value : null;
    }

    /// <summary>
    /// Get an EEPROM data value
    /// </summary>
    /// <param name="key">EEPROM data key</param>
    /// <returns>EEPROM value or null if not found</returns>
    public string? GetEepromValue(string key)
    {
        return EepromData.TryGetValue(key, out var value) ? value : null;
    }

    /// <summary>
    /// Mark this system info as having an error
    /// </summary>
    /// <param name="errorMessage">Error details</param>
    public void SetError(string errorMessage)
    {
        IsDataValid = false;
        ErrorMessage = errorMessage;
        LastRead = DateTime.Now;
    }

    /// <summary>
    /// Create a SystemInfo instance from FTDI device info
    /// </summary>
    /// <param name="ftdiInfo">FTDI device information</param>
    /// <returns>SystemInfo instance</returns>
    public static SystemInfo FromFtdiDevice(FtdiDeviceInfo ftdiInfo)
    {
        return new SystemInfo
        {
            SerialNumber = ftdiInfo.SerialNumber,
            Manufacturer = ftdiInfo.Manufacturer,
            ProductDescription = ftdiInfo.ProductDescription,
            EepromData = new Dictionary<string, string>(ftdiInfo.EepromData),
            LastRead = DateTime.Now,
            IsDataValid = true
        };
    }

    /// <summary>
    /// Create a SystemInfo with error state
    /// </summary>
    /// <param name="serialNumber">Device serial number</param>
    /// <param name="errorMessage">Error message</param>
    /// <returns>SystemInfo in error state</returns>
    public static SystemInfo CreateError(string serialNumber, string errorMessage)
    {
        return new SystemInfo
        {
            SerialNumber = serialNumber,
            IsDataValid = false,
            ErrorMessage = errorMessage,
            LastRead = DateTime.Now
        };
    }

    /// <summary>
    /// Get a summary of all available data
    /// </summary>
    /// <returns>Formatted summary string</returns>
    public string GetSummary()
    {
        if (!IsDataValid)
        {
            return $"❌ SystemInfo Error: {ErrorMessage} (Serial: {SerialNumber})";
        }

        var age = IsFresh ? "Fresh" : $"{Age.TotalMinutes:F0}min old";
        var dataCount = EepromData.Count + SystemProperties.Count + ClientConfiguration.Count;
        
        return $"✅ {SerialNumber} - {Manufacturer} {ProductDescription} ({age}, {dataCount} properties)";
    }

    public override string ToString()
    {
        return GetSummary();
    }

    public override bool Equals(object? obj)
    {
        return obj is SystemInfo other && SerialNumber == other.SerialNumber;
    }

    public override int GetHashCode()
    {
        return SerialNumber.GetHashCode();
    }
}