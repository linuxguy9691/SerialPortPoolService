using SerialPortPool.Core.Models;

namespace SerialPortPool.Core.Interfaces;

/// <summary>
/// Service for validating serial ports against pool eligibility criteria
/// </summary>
public interface ISerialPortValidator
{
    /// <summary>
    /// Validate a single port for pool eligibility
    /// </summary>
    /// <param name="portInfo">Port information to validate</param>
    /// <param name="configuration">Validation configuration (optional, uses default if null)</param>
    /// <returns>Detailed validation result</returns>
    Task<PortValidationResult> ValidatePortAsync(SerialPortInfo portInfo, PortValidationConfiguration? configuration = null);
    
    /// <summary>
    /// Quick validation check - returns only boolean result
    /// </summary>
    /// <param name="portInfo">Port information to validate</param>
    /// <param name="configuration">Validation configuration (optional)</param>
    /// <returns>True if port is valid for pool</returns>
    Task<bool> IsValidPortAsync(SerialPortInfo portInfo, PortValidationConfiguration? configuration = null);
    
    /// <summary>
    /// Validate multiple ports and return only the valid ones
    /// </summary>
    /// <param name="ports">Collection of ports to validate</param>
    /// <param name="configuration">Validation configuration (optional)</param>
    /// <returns>Only the ports that pass validation</returns>
    Task<IEnumerable<SerialPortInfo>> GetValidPortsAsync(IEnumerable<SerialPortInfo> ports, PortValidationConfiguration? configuration = null);
    
    /// <summary>
    /// Validate multiple ports and return detailed results for each
    /// </summary>
    /// <param name="ports">Collection of ports to validate</param>
    /// <param name="configuration">Validation configuration (optional)</param>
    /// <returns>Validation results for each port</returns>
    Task<IDictionary<SerialPortInfo, PortValidationResult>> ValidatePortsAsync(IEnumerable<SerialPortInfo> ports, PortValidationConfiguration? configuration = null);
    
    /// <summary>
    /// Get the current validation configuration
    /// </summary>
    PortValidationConfiguration GetConfiguration();
    
    /// <summary>
    /// Update the validation configuration
    /// </summary>
    /// <param name="configuration">New configuration to use</param>
    void SetConfiguration(PortValidationConfiguration configuration);
}

/// <summary>
/// Service for reading and analyzing FTDI device information
/// </summary>
public interface IFtdiDeviceReader
{
    /// <summary>
    /// Read detailed FTDI device information from a port
    /// </summary>
    /// <param name="portName">Port name (e.g., "COM3")</param>
    /// <returns>FTDI device information or null if not an FTDI device</returns>
    Task<FtdiDeviceInfo?> ReadDeviceInfoAsync(string portName);
    
    /// <summary>
    /// Read FTDI device information from Windows Device ID
    /// </summary>
    /// <param name="deviceId">Windows device ID string</param>
    /// <returns>FTDI device information or null if not an FTDI device</returns>
    Task<FtdiDeviceInfo?> ReadDeviceInfoFromIdAsync(string deviceId);
    
    /// <summary>
    /// Check if a port is an FTDI device
    /// </summary>
    /// <param name="portName">Port name to check</param>
    /// <returns>True if the port is an FTDI device</returns>
    Task<bool> IsFtdiDeviceAsync(string portName);
    
    /// <summary>
    /// Check if a port is specifically a 4232H chip
    /// </summary>
    /// <param name="portName">Port name to check</param>
    /// <returns>True if the port is a 4232H chip</returns>
    Task<bool> IsFtdi4232HAsync(string portName);
    
    /// <summary>
    /// Read EEPROM data from an FTDI device (if supported)
    /// </summary>
    /// <param name="portName">Port name</param>
    /// <returns>EEPROM data dictionary or empty if not available</returns>
    Task<Dictionary<string, string>> ReadEepromDataAsync(string portName);
    
    /// <summary>
    /// Get all FTDI devices currently connected to the system
    /// </summary>
    /// <returns>Collection of FTDI device information</returns>
    Task<IEnumerable<FtdiDeviceInfo>> GetAllFtdiDevicesAsync();
    
    /// <summary>
    /// Analyze a device ID to extract FTDI information
    /// </summary>
    /// <param name="deviceId">Windows device ID</param>
    /// <returns>FTDI device info or null if not FTDI</returns>
    FtdiDeviceInfo? AnalyzeDeviceId(string deviceId);
}