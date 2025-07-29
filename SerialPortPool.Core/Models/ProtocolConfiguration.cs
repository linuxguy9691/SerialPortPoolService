// SerialPortPool.Core/Models/ProtocolConfiguration.cs - NEW Sprint 5
using System.IO.Ports;
using System.Text.Json.Serialization;

namespace SerialPortPool.Core.Models;

/// <summary>
/// Protocol configuration for multi-protocol communication
/// Sprint 5: RS232 support with XML configuration
/// Sprint 6: Extended for RS485, USB, CAN, I2C, SPI
/// </summary>
public class ProtocolConfiguration
{
    /// <summary>
    /// Physical port name (e.g., "COM11", "COM12")
    /// </summary>
    public string PortName { get; set; } = string.Empty;

    /// <summary>
    /// Protocol type (e.g., "rs232", "rs485", "usb", "can", "i2c", "spi")
    /// </summary>
    public string Protocol { get; set; } = string.Empty;

    /// <summary>
    /// Communication speed (baud rate for serial protocols, bit rate for CAN, clock for I2C/SPI)
    /// </summary>
    public int Speed { get; set; } = 115200;

    /// <summary>
    /// Data pattern (e.g., "n81", "e71", "n82" for serial protocols)
    /// </summary>
    public string DataPattern { get; set; } = "n81";

    /// <summary>
    /// Protocol-specific settings
    /// </summary>
    public Dictionary<string, object> Settings { get; set; } = new();

    /// <summary>
    /// BIB identifier (temporary mapping until EEPROM integration)
    /// </summary>
    public string BibId { get; set; } = string.Empty;

    /// <summary>
    /// UUT identifier within BIB
    /// </summary>
    public string UutId { get; set; } = string.Empty;

    /// <summary>
    /// Port number within UUT
    /// </summary>
    public int PortNumber { get; set; }

    /// <summary>
    /// Configuration creation timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    /// <summary>
    /// Configuration metadata
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();

    #region RS232 Specific Methods (Sprint 5)

    /// <summary>
    /// Get baud rate for RS232 protocol
    /// </summary>
    /// <returns>Baud rate value</returns>
    public int GetBaudRate() => Speed;

    /// <summary>
    /// Parse data pattern and get parity setting
    /// </summary>
    /// <returns>Parity enumeration value</returns>
    public Parity GetParity()
    {
        var pattern = DataPattern.ToLowerInvariant();
        if (pattern.Length < 1) return Parity.None;

        return pattern[0] switch
        {
            'n' => Parity.None,
            'e' => Parity.Even,
            'o' => Parity.Odd,
            'm' => Parity.Mark,
            's' => Parity.Space,
            _ => Parity.None
        };
    }

    /// <summary>
    /// Parse data pattern and get data bits count
    /// </summary>
    /// <returns>Data bits count (5-8)</returns>
    public int GetDataBits()
    {
        var pattern = DataPattern.ToLowerInvariant();
        if (pattern.Length < 2) return 8;

        return pattern[1] switch
        {
            '5' => 5,
            '6' => 6,
            '7' => 7,
            '8' => 8,
            _ => 8
        };
    }

    /// <summary>
    /// Parse data pattern and get stop bits setting
    /// </summary>
    /// <returns>Stop bits enumeration value</returns>
    public StopBits GetStopBits()
    {
        var pattern = DataPattern.ToLowerInvariant();
        if (pattern.Length < 3) return StopBits.One;

        return pattern[2] switch
        {
            '1' => StopBits.One,
            '2' => StopBits.Two,
            '5' => StopBits.OnePointFive,
            _ => StopBits.One
        };
    }

    /// <summary>
    /// Get read timeout for RS232 protocol
    /// </summary>
    /// <returns>Read timeout in milliseconds</returns>
    public int GetReadTimeout()
    {
        return Settings.TryGetValue("read_timeout", out var timeout) && timeout is int value
            ? value
            : 2000;
    }

    /// <summary>
    /// Get write timeout for RS232 protocol
    /// </summary>
    /// <returns>Write timeout in milliseconds</returns>
    public int GetWriteTimeout()
    {
        return Settings.TryGetValue("write_timeout", out var timeout) && timeout is int value
            ? value
            : 2000;
    }

    #endregion

    #region Future Protocol Methods (Sprint 6 Planned)

    /// <summary>
    /// Get RS485 specific address
    /// </summary>
    /// <returns>RS485 device address</returns>
    public byte GetRS485Address()
    {
        return Settings.TryGetValue("address", out var addr) && addr is byte address
            ? address
            : (byte)1;
    }

    /// <summary>
    /// Get RS485 termination setting
    /// </summary>
    /// <returns>True if bus termination enabled</returns>
    public bool GetRS485Termination()
    {
        return Settings.TryGetValue("termination", out var term) && term is bool termination
            ? termination
            : false;
    }

    /// <summary>
    /// Get USB vendor ID
    /// </summary>
    /// <returns>USB vendor ID (hex)</returns>
    public ushort GetUSBVendorId()
    {
        if (Settings.TryGetValue("vendor_id", out var vid))
        {
            if (vid is string hexStr && hexStr.StartsWith("0x"))
                return Convert.ToUInt16(hexStr, 16);
            if (vid is ushort id)
                return id;
        }
        return 0x0403; // Default FTDI
    }

    /// <summary>
    /// Get USB product ID
    /// </summary>
    /// <returns>USB product ID (hex)</returns>
    public ushort GetUSBProductId()
    {
        if (Settings.TryGetValue("product_id", out var pid))
        {
            if (pid is string hexStr && hexStr.StartsWith("0x"))
                return Convert.ToUInt16(hexStr, 16);
            if (pid is ushort id)
                return id;
        }
        return 0x6001; // Default FT232R
    }

    /// <summary>
    /// Get CAN frame ID
    /// </summary>
    /// <returns>CAN message ID</returns>
    public uint GetCANId()
    {
        if (Settings.TryGetValue("can_id", out var id))
        {
            if (id is string hexStr && hexStr.StartsWith("0x"))
                return Convert.ToUInt32(hexStr, 16);
            if (id is uint canId)
                return canId;
        }
        return 0x123; // Default CAN ID
    }

    /// <summary>
    /// Get I2C slave address
    /// </summary>
    /// <returns>I2C 7-bit address</returns>
    public byte GetI2CAddress()
    {
        if (Settings.TryGetValue("slave_address", out var addr))
        {
            if (addr is string hexStr && hexStr.StartsWith("0x"))
                return Convert.ToByte(hexStr, 16);
            if (addr is byte address)
                return address;
        }
        return 0x48; // Default I2C address
    }

    /// <summary>
    /// Get SPI mode configuration
    /// </summary>
    /// <returns>SPI mode (0-3)</returns>
    public int GetSPIMode()
    {
        return Settings.TryGetValue("mode", out var mode) && mode is int spiMode
            ? Math.Max(0, Math.Min(3, spiMode))
            : 0;
    }

    #endregion

    #region Factory Methods

    /// <summary>
    /// Create RS232 configuration for specific port
    /// </summary>
    /// <param name="portName">COM port name</param>
    /// <param name="baudRate">Baud rate (default 115200)</param>
    /// <param name="dataPattern">Data pattern (default "n81")</param>
    /// <param name="bibId">BIB identifier</param>
    /// <param name="uutId">UUT identifier</param>
    /// <param name="portNumber">Port number within UUT</param>
    /// <returns>RS232 protocol configuration</returns>
    public static ProtocolConfiguration CreateRS232(
        string portName,
        int baudRate = 115200,
        string dataPattern = "n81",
        string bibId = "bib_001",
        string uutId = "uut_001",
        int portNumber = 1)
    {
        return new ProtocolConfiguration
        {
            PortName = portName,
            Protocol = "rs232",
            Speed = baudRate,
            DataPattern = dataPattern,
            BibId = bibId,
            UutId = uutId,
            PortNumber = portNumber,
            Settings = new Dictionary<string, object>
            {
                { "read_timeout", 2000 },
                { "write_timeout", 2000 },
                { "buffer_size", 4096 }
            }
        };
    }

    /// <summary>
    /// Create RS485 configuration (Sprint 6)
    /// </summary>
    /// <param name="portName">COM port name</param>
    /// <param name="baudRate">Baud rate</param>
    /// <param name="address">RS485 device address</param>
    /// <param name="termination">Bus termination enabled</param>
    /// <param name="bibId">BIB identifier</param>
    /// <param name="uutId">UUT identifier</param>
    /// <param name="portNumber">Port number</param>
    /// <returns>RS485 protocol configuration</returns>
    public static ProtocolConfiguration CreateRS485(
        string portName,
        int baudRate = 9600,
        byte address = 1,
        bool termination = false,
        string bibId = "bib_001",
        string uutId = "uut_001",
        int portNumber = 2)
    {
        return new ProtocolConfiguration
        {
            PortName = portName,
            Protocol = "rs485",
            Speed = baudRate,
            DataPattern = "e71", // Common for RS485
            BibId = bibId,
            UutId = uutId,
            PortNumber = portNumber,
            Settings = new Dictionary<string, object>
            {
                { "address", address },
                { "termination", termination },
                { "read_timeout", 3000 },
                { "write_timeout", 3000 }
            }
        };
    }

    /// <summary>
    /// Create USB configuration (Sprint 6)
    /// </summary>
    /// <param name="vendorId">USB vendor ID</param>
    /// <param name="productId">USB product ID</param>
    /// <param name="baudRate">Virtual baud rate</param>
    /// <param name="bibId">BIB identifier</param>
    /// <param name="uutId">UUT identifier</param>
    /// <param name="portNumber">Port number</param>
    /// <returns>USB protocol configuration</returns>
    public static ProtocolConfiguration CreateUSB(
        ushort vendorId = 0x0403,
        ushort productId = 0x6001,
        int baudRate = 9600,
        string bibId = "bib_001",
        string uutId = "uut_001",
        int portNumber = 3)
    {
        return new ProtocolConfiguration
        {
            PortName = $"USB_{vendorId:X4}_{productId:X4}",
            Protocol = "usb",
            Speed = baudRate,
            DataPattern = "n81",
            BibId = bibId,
            UutId = uutId,
            PortNumber = portNumber,
            Settings = new Dictionary<string, object>
            {
                { "vendor_id", vendorId },
                { "product_id", productId },
                { "interface", 0 },
                { "read_timeout", 2000 },
                { "write_timeout", 2000 }
            }
        };
    }

    /// <summary>
    /// Create temporary BIB mapping for ports (until EEPROM integration)
    /// </summary>
    /// <param name="portName">Physical port name</param>
    /// <returns>Temporary BIB mapping configuration</returns>
    public static ProtocolConfiguration CreateTemporaryBIBMapping(string portName)
    {
        // Temporary mapping logic until BIB_ID in EEPROM
        var (bibId, uutId, portNumber) = portName switch
        {
            "COM11" => ("bib_001", "uut_001", 1), // FT4232HL Port A
            "COM12" => ("bib_001", "uut_001", 2), // FT4232HL Port B
            "COM13" => ("bib_001", "uut_001", 3), // FT4232HL Port C
            "COM14" => ("bib_001", "uut_001", 4), // FT4232HL Port D
            "COM6" => ("bib_002", "uut_002", 1),  // FT232R (Development)
            _ => ("bib_unknown", "uut_unknown", 1)
        };

        return CreateRS232(portName, 115200, "n81", bibId, uutId, portNumber);
    }

    #endregion

    #region Validation

    /// <summary>
    /// Validate protocol configuration
    /// </summary>
    /// <returns>Validation result with errors if any</returns>
    public ValidationResult Validate()
    {
        var result = new ValidationResult();

        // Basic validation
        if (string.IsNullOrWhiteSpace(PortName))
            result.Errors.Add("PortName cannot be empty");

        if (string.IsNullOrWhiteSpace(Protocol))
            result.Errors.Add("Protocol cannot be empty");

        if (Speed <= 0)
            result.Errors.Add("Speed must be positive");

        // Protocol-specific validation
        switch (Protocol.ToLowerInvariant())
        {
            case "rs232":
            case "rs485":
                ValidateSerialProtocol(result);
                break;
            case "usb":
                ValidateUSBProtocol(result);
                break;
            case "can":
                ValidateCANProtocol(result);
                break;
            case "i2c":
                ValidateI2CProtocol(result);
                break;
            case "spi":
                ValidateSPIProtocol(result);
                break;
            default:
                result.Errors.Add($"Unknown protocol: {Protocol}");
                break;
        }

        return result;
    }

    private void ValidateSerialProtocol(ValidationResult result)
    {
        // Validate baud rate
        var validBaudRates = new[] { 1200, 2400, 4800, 9600, 19200, 38400, 57600, 115200, 230400, 460800, 921600 };
        if (!validBaudRates.Contains(Speed))
            result.Warnings.Add($"Unusual baud rate: {Speed}");

        // Validate data pattern
        if (!IsValidDataPattern(DataPattern))
            result.Errors.Add($"Invalid data pattern: {DataPattern}");
    }

    private void ValidateUSBProtocol(ValidationResult result)
    {
        var vendorId = GetUSBVendorId();
        var productId = GetUSBProductId();

        if (vendorId == 0)
            result.Errors.Add("USB vendor ID cannot be 0");

        if (productId == 0)
            result.Errors.Add("USB product ID cannot be 0");
    }

    private void ValidateCANProtocol(ValidationResult result)
    {
        var validBitRates = new[] { 125000, 250000, 500000, 1000000 };
        if (!validBitRates.Contains(Speed))
            result.Warnings.Add($"Unusual CAN bit rate: {Speed}");
    }

    private void ValidateI2CProtocol(ValidationResult result)
    {
        var validClockSpeeds = new[] { 100000, 400000, 1000000, 3400000 };
        if (!validClockSpeeds.Contains(Speed))
            result.Warnings.Add($"Unusual I2C clock speed: {Speed}");

        var address = GetI2CAddress();
        if (address > 127)
            result.Errors.Add("I2C address must be 7-bit (0-127)");
    }

    private void ValidateSPIProtocol(ValidationResult result)
    {
        var mode = GetSPIMode();
        if (mode < 0 || mode > 3)
            result.Errors.Add("SPI mode must be 0-3");
    }

    private static bool IsValidDataPattern(string pattern)
    {
        if (string.IsNullOrWhiteSpace(pattern) || pattern.Length != 3)
            return false;

        var parity = pattern[0];
        var dataBits = pattern[1];
        var stopBits = pattern[2];

        return "neoms".Contains(parity) &&
               "5678".Contains(dataBits) &&
               "125".Contains(stopBits);
    }

    #endregion

    #region Display Methods

    /// <summary>
    /// Get human-readable configuration summary
    /// </summary>
    /// <returns>Configuration summary string</returns>
    public string GetSummary()
    {
        var protocolInfo = Protocol.ToUpperInvariant() switch
        {
            "RS232" => $"RS232 @ {Speed} baud ({DataPattern})",
            "RS485" => $"RS485 @ {Speed} baud (addr: {GetRS485Address()})",
            "USB" => $"USB {GetUSBVendorId():X4}:{GetUSBProductId():X4}",
            "CAN" => $"CAN @ {Speed} bps (ID: 0x{GetCANId():X})",
            "I2C" => $"I2C @ {Speed} Hz (addr: 0x{GetI2CAddress():X2})",
            "SPI" => $"SPI @ {Speed} Hz (mode: {GetSPIMode()})",
            _ => $"{Protocol} @ {Speed}"
        };

        return $"{BibId}.{UutId}.{PortNumber} → {PortName} ({protocolInfo})";
    }

    public override string ToString() => GetSummary();

    #endregion
}

/// <summary>
/// Configuration validation result
/// </summary>
public class ValidationResult
{
    /// <summary>
    /// Validation errors (must be fixed)
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// Validation warnings (should be reviewed)
    /// </summary>
    public List<string> Warnings { get; set; } = new();

    /// <summary>
    /// Whether configuration is valid (no errors)
    /// </summary>
    public bool IsValid => !Errors.Any();

    /// <summary>
    /// Whether configuration has warnings
    /// </summary>
    public bool HasWarnings => Warnings.Any();

    /// <summary>
    /// Get validation summary
    /// </summary>
    /// <returns>Summary string</returns>
    public string GetSummary()
    {
        if (IsValid && !HasWarnings)
            return "✅ Configuration valid";

        if (IsValid && HasWarnings)
            return $"⚠️ Configuration valid with {Warnings.Count} warning(s)";

        return $"❌ Configuration invalid: {Errors.Count} error(s), {Warnings.Count} warning(s)";
    }

    public override string ToString() => GetSummary();
}