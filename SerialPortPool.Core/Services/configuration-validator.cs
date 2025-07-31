using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Schema;
using Microsoft.Extensions.Logging;
using SerialPortPool.Core.Interfaces;
using SerialPortPool.Core.Models.Configuration;

namespace SerialPortPool.Core.Services;

/// <summary>
/// Configuration Validator - Validates XML schema and business rules
/// Supports comprehensive validation for BIB → UUT → PORT configurations
/// </summary>
public class ConfigurationValidator : IConfigurationValidator
{
    private readonly ILogger<ConfigurationValidator> _logger;
    private readonly List<string> _validationErrors = new();
    private readonly HashSet<string> _supportedProtocols = new(StringComparer.OrdinalIgnoreCase)
    {
        "rs232", "rs485", "usb", "can", "i2c", "spi"
    };

    public ConfigurationValidator(ILogger<ConfigurationValidator> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Validate system configuration against business rules
    /// </summary>
    public ConfigurationValidationResult ValidateConfiguration(SystemConfiguration configuration)
    {
        var result = new ConfigurationValidationResult();

        try
        {
            _logger.LogDebug("Validating system configuration with {BibCount} BIBs", configuration.Bibs.Count);

            // Validate basic structure
            ValidateSystemStructure(configuration, result);

            // Validate each BIB
            foreach (var bib in configuration.Bibs)
            {
                ValidateBibInternal(bib, result);
            }

            // Cross-BIB validation
            ValidateCrossBibRules(configuration, result);

            _logger.LogInformation("Configuration validation completed: {Summary}", result.GetSummary());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Configuration validation failed with exception");
            result.AddError($"Validation exception: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// Validate XML document against schema
    /// </summary>
    public async Task<ConfigurationValidationResult> ValidateSchemaAsync(string xmlPath)
    {
        var result = new ConfigurationValidationResult();

        try
        {
            _validationErrors.Clear();

            var settings = new XmlReaderSettings();
            
            // Add schema validation if XSD available
            var xsdPath = Path.ChangeExtension(xmlPath, ".xsd");
            if (File.Exists(xsdPath))
            {
                settings.Schemas.Add(null, xsdPath);
                settings.ValidationType = ValidationType.Schema;
                settings.ValidationEventHandler += (sender, e) =>
                {
                    if (e.Severity == XmlSeverityType.Error)
                        _validationErrors.Add($"Schema error: {e.Message}");
                    else
                        result.AddWarning($"Schema warning: {e.Message}");
                };
            }

            // Validate XML structure
            using var reader = XmlReader.Create(xmlPath, settings);
            var doc = new XmlDocument();
            doc.Load(reader);

            // Add any schema validation errors
            foreach (var error in _validationErrors)
            {
                result.AddError(error);
            }

            // Basic XML structure validation
            if (doc.DocumentElement?.Name != "root")
            {
                result.AddError("XML root element must be 'root'");
            }

            _logger.LogDebug("Schema validation completed for: {XmlPath}", xmlPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Schema validation failed for: {XmlPath}", xmlPath);
            result.AddError($"Schema validation exception: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// Validate specific BIB configuration
    /// </summary>
    public ConfigurationValidationResult ValidateBib(BibConfiguration bibConfig)
    {
        var result = new ConfigurationValidationResult();
        ValidateBibInternal(bibConfig, result);
        return result;
    }

    /// <summary>
    /// Validate system-level structure
    /// </summary>
    private void ValidateSystemStructure(SystemConfiguration configuration, ConfigurationValidationResult result)
    {
        if (!configuration.Bibs.Any())
        {
            result.AddError("System configuration must contain at least one BIB");
            return;
        }

        if (configuration.Bibs.Count > 100)
        {
            result.AddWarning($"Large number of BIBs ({configuration.Bibs.Count}) may impact performance");
        }

        // Check for duplicate BIB IDs
        var bibIds = configuration.Bibs.Select(b => b.BibId).ToList();
        var duplicates = bibIds.GroupBy(id => id, StringComparer.OrdinalIgnoreCase)
                              .Where(g => g.Count() > 1)
                              .Select(g => g.Key);

        foreach (var duplicate in duplicates)
        {
            result.AddError($"Duplicate BIB ID: '{duplicate}'");
        }

        // Validate protocol distribution
        var allProtocols = configuration.GetAllProtocols().ToList();
        if (allProtocols.Any(p => !_supportedProtocols.Contains(p)))
        {
            var unsupported = allProtocols.Where(p => !_supportedProtocols.Contains(p));
            result.AddWarning($"Unsupported protocols found: {string.Join(", ", unsupported)}");
        }
    }

    /// <summary>
    /// Validate individual BIB configuration
    /// </summary>
    private void ValidateBibInternal(BibConfiguration bib, ConfigurationValidationResult result)
    {
        // Validate BIB ID
        if (string.IsNullOrWhiteSpace(bib.BibId))
        {
            result.AddError("BIB ID cannot be empty");
            return;
        }

        if (!IsValidIdentifier(bib.BibId))
        {
            result.AddError($"BIB ID '{bib.BibId}' contains invalid characters");
        }

        // Validate UUTs
        if (!bib.Uuts.Any())
        {
            result.AddError($"BIB '{bib.BibId}' must contain at least one UUT");
            return;
        }

        if (bib.Uuts.Count > 50)
        {
            result.AddWarning($"BIB '{bib.BibId}' has many UUTs ({bib.Uuts.Count}) - consider splitting");
        }

        // Check for duplicate UUT IDs within BIB
        var uutIds = bib.Uuts.Select(u => u.UutId).ToList();
        var duplicates = uutIds.GroupBy(id => id, StringComparer.OrdinalIgnoreCase)
                              .Where(g => g.Count() > 1)
                              .Select(g => g.Key);

        foreach (var duplicate in duplicates)
        {
            result.AddError($"Duplicate UUT ID in BIB '{bib.BibId}': '{duplicate}'");
        }

        // Validate each UUT
        foreach (var uut in bib.Uuts)
        {
            ValidateUut(uut, bib.BibId, result);
        }
    }

    /// <summary>
    /// Validate UUT configuration
    /// </summary>
    private void ValidateUut(UutConfiguration uut, string bibId, ConfigurationValidationResult result)
    {
        // Validate UUT ID
        if (string.IsNullOrWhiteSpace(uut.UutId))
        {
            result.AddError($"UUT ID cannot be empty in BIB '{bibId}'");
            return;
        }

        if (!IsValidIdentifier(uut.UutId))
        {
            result.AddError($"UUT ID '{uut.UutId}' contains invalid characters");
        }

        // Validate ports
        if (!uut.Ports.Any())
        {
            result.AddError($"UUT '{uut.UutId}' must contain at least one port");
            return;
        }

        if (uut.Ports.Count > 20)
        {
            result.AddWarning($"UUT '{uut.UutId}' has many ports ({uut.Ports.Count}) - verify this is intended");
        }

        // Check for duplicate port numbers
        var portNumbers = uut.Ports.Select(p => p.PortNumber).ToList();
        var duplicates = portNumbers.GroupBy(n => n)
                                   .Where(g => g.Count() > 1)
                                   .Select(g => g.Key);

        foreach (var duplicate in duplicates)
        {
            result.AddError($"Duplicate port number in UUT '{uut.UutId}': {duplicate}");
        }

        // Validate each port
        foreach (var port in uut.Ports)
        {
            ValidatePort(port, uut.UutId, result);
        }
    }

    /// <summary>
    /// Validate port configuration
    /// </summary>
    private void ValidatePort(PortConfiguration port, string uutId, ConfigurationValidationResult result)
    {
        // Validate port number
        if (port.PortNumber <= 0)
        {
            result.AddError($"Port number must be positive in UUT '{uutId}': {port.PortNumber}");
        }

        if (port.PortNumber > 100)
        {
            result.AddWarning($"High port number in UUT '{uutId}': {port.PortNumber}");
        }

        // Validate protocol
        if (string.IsNullOrWhiteSpace(port.Protocol))
        {
            result.AddError($"Protocol cannot be empty for port {port.PortNumber} in UUT '{uutId}'");
            return;
        }

        if (!_supportedProtocols.Contains(port.Protocol))
        {
            result.AddWarning($"Unsupported protocol '{port.Protocol}' for port {port.PortNumber} in UUT '{uutId}'");
        }

        // Validate speed
        ValidateSpeed(port, uutId, result);

        // Validate data pattern
        ValidateDataPattern(port, uutId, result);

        // Validate command sequences
        ValidateCommandSequence(port.StartCommands, "Start", port.PortNumber, uutId, result);
        ValidateCommandSequence(port.TestCommands, "Test", port.PortNumber, uutId, result);
        ValidateCommandSequence(port.StopCommands, "Stop", port.PortNumber, uutId, result);

        // Protocol-specific validation
        ValidateProtocolSpecificSettings(port, uutId, result);
    }

    /// <summary>
    /// Validate speed settings for protocol
    /// </summary>
    private void ValidateSpeed(PortConfiguration port, string uutId, ConfigurationValidationResult result)
    {
        switch (port.Protocol.ToLowerInvariant())
        {
            case "rs232":
            case "rs485":
                var validBaudRates = new[] { 1200, 2400, 4800, 9600, 19200, 38400, 57600, 115200 };
                if (!validBaudRates.Contains(port.Speed))
                {
                    result.AddWarning($"Unusual baud rate {port.Speed} for {port.Protocol} on port {port.PortNumber} in UUT '{uutId}'");
                }
                break;

            case "can":
                var validCanRates = new[] { 125000, 250000, 500000, 1000000 };
                if (!validCanRates.Contains(port.Speed))
                {
                    result.AddWarning($"Unusual CAN bit rate {port.Speed} for port {port.PortNumber} in UUT '{uutId}'");
                }
                break;

            case "i2c":
                var validI2cRates = new[] { 100000, 400000, 1000000 };
                if (!validI2cRates.Contains(port.Speed))
                {
                    result.AddWarning($"Unusual I2C clock speed {port.Speed} for port {port.PortNumber} in UUT '{uutId}'");
                }
                break;

            case "spi":
                if (port.Speed > 50000000) // 50 MHz max
                {
                    result.AddWarning($"Very high SPI clock speed {port.Speed} for port {port.PortNumber} in UUT '{uutId}'");
                }
                break;
        }
    }

    /// <summary>
    /// Validate data pattern for serial protocols
    /// </summary>
    private void ValidateDataPattern(PortConfiguration port, string uutId, ConfigurationValidationResult result)
    {
        if (port.Protocol.ToLowerInvariant() is "rs232" or "rs485")
        {
            var validPatterns = new[] { "n81", "n82", "e71", "e81", "o71", "o81" };
            if (!validPatterns.Contains(port.DataPattern))
            {
                result.AddWarning($"Unusual data pattern '{port.DataPattern}' for {port.Protocol} on port {port.PortNumber} in UUT '{uutId}'");
            }
        }
    }

    /// <summary>
    /// Validate command sequence
    /// </summary>
    private void ValidateCommandSequence(CommandSequence sequence, string phaseName, int portNumber, string uutId, ConfigurationValidationResult result)
    {
        if (!sequence.Commands.Any())
        {
            result.AddWarning($"{phaseName} phase has no commands for port {portNumber} in UUT '{uutId}'");
            return;
        }

        foreach (var command in sequence.Commands)
        {
            if (string.IsNullOrWhiteSpace(command.Command))
            {
                result.AddError($"Empty command in {phaseName} phase for port {portNumber} in UUT '{uutId}'");
            }

            if (string.IsNullOrWhiteSpace(command.ExpectedResponse))
            {
                result.AddWarning($"No expected response for command '{command.Command}' in {phaseName} phase for port {portNumber} in UUT '{uutId}'");
            }

            if (command.TimeoutMs <= 0 || command.TimeoutMs > 300000) // 5 minutes max
            {
                result.AddWarning($"Unusual timeout {command.TimeoutMs}ms for command '{command.Command}' in {phaseName} phase for port {portNumber} in UUT '{uutId}'");
            }

            if (command.RetryCount < 0 || command.RetryCount > 10)
            {
                result.AddWarning($"Unusual retry count {command.RetryCount} for command '{command.Command}' in {phaseName} phase for port {portNumber} in UUT '{uutId}'");
            }

            // Validate regex patterns if specified
            if (command.IsRegex)
            {
                try
                {
                    _ = new Regex(command.ExpectedResponse);
                }
                catch (ArgumentException)
                {
                    result.AddError($"Invalid regex pattern '{command.ExpectedResponse}' for command '{command.Command}' in {phaseName} phase for port {portNumber} in UUT '{uutId}'");
                }
            }
        }
    }

    /// <summary>
    /// Validate protocol-specific settings
    /// </summary>
    private void ValidateProtocolSpecificSettings(PortConfiguration port, string uutId, ConfigurationValidationResult result)
    {
        switch (port.Protocol.ToLowerInvariant())
        {
            case "rs485":
                if (port.GetProtocolSetting<string>("address") is string address && 
                    !Regex.IsMatch(address, @"^[0-9A-Fa-f]{2}$"))
                {
                    result.AddWarning($"RS485 address '{address}' should be 2-digit hex for port {port.PortNumber} in UUT '{uutId}'");
                }
                break;

            case "usb":
                if (port.GetProtocolSetting<string>("vendor_id") is string vid &&
                    !Regex.IsMatch(vid, @"^0x[0-9A-Fa-f]{4}$"))
                {
                    result.AddWarning($"USB vendor_id '{vid}' should be 4-digit hex with 0x prefix for port {port.PortNumber} in UUT '{uutId}'");
                }
                break;

            case "can":
                if (port.GetProtocolSetting<string>("can_id") is string canId &&
                    !Regex.IsMatch(canId, @"^0x[0-9A-Fa-f]+$"))
                {
                    result.AddWarning($"CAN ID '{canId}' should be hex with 0x prefix for port {port.PortNumber} in UUT '{uutId}'");
                }
                break;

            case "i2c":
                if (port.GetProtocolSetting<string>("slave_address") is string i2cAddr &&
                    !Regex.IsMatch(i2cAddr, @"^0x[0-9A-Fa-f]{2}$"))
                {
                    result.AddWarning($"I2C slave_address '{i2cAddr}' should be 2-digit hex with 0x prefix for port {port.PortNumber} in UUT '{uutId}'");
                }
                break;

            case "spi":
                if (port.GetProtocolSetting<int>("mode") is int spiMode && (spiMode < 0 || spiMode > 3))
                {
                    result.AddError($"SPI mode {spiMode} must be 0-3 for port {port.PortNumber} in UUT '{uutId}'");
                }
                break;
        }
    }

    /// <summary>
    /// Validate cross-BIB business rules
    /// </summary>
    private void ValidateCrossBibRules(SystemConfiguration configuration, ConfigurationValidationResult result)
    {
        // Check for reasonable system size
        var totalPorts = configuration.Bibs.Sum(b => b.TotalPorts);
        if (totalPorts > 1000)
        {
            result.AddWarning($"Very large system configuration with {totalPorts} total ports - consider performance implications");
        }

        // Check protocol diversity
        var protocols = configuration.GetAllProtocols().ToList();
        if (protocols.Count == 1)
        {
            result.AddInfo($"Single protocol system: {protocols.First()}");
        }
        else
        {
            result.AddInfo($"Multi-protocol system: {string.Join(", ", protocols)}");
        }
    }

    /// <summary>
    /// Check if identifier is valid (alphanumeric + underscore)
    /// </summary>
    private static bool IsValidIdentifier(string identifier)
    {
        return !string.IsNullOrWhiteSpace(identifier) && 
               Regex.IsMatch(identifier, @"^[a-zA-Z][a-zA-Z0-9_]*$");
    }
}