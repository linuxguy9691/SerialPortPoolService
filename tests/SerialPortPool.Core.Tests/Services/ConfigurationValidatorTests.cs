// ===================================================================
// SPRINT 11: ConfigurationValidator Implementation
// File: SerialPortPool.Core/Services/Configuration/ConfigurationValidator.cs
// Purpose: XML schema validation + business rules validation for BIB configurations
// ===================================================================

using Microsoft.Extensions.Logging;
using SerialPortPool.Core.Models;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace SerialPortPool.Core.Services.Configuration;

/// <summary>
/// Service for validating BIB configuration files and objects
/// Provides XML schema validation and business rules validation
/// </summary>
public class ConfigurationValidator
{
    private readonly ILogger<ConfigurationValidator> _logger;
    
    // Supported protocols
    private static readonly HashSet<string> SupportedProtocols = new(StringComparer.OrdinalIgnoreCase)
    {
        "rs232", "serial", "uart"
    };
    
    // Common baud rates
    private static readonly HashSet<int> CommonBaudRates = new()
    {
        9600, 19200, 38400, 57600, 115200, 230400, 460800, 921600
    };
    
    // Valid data patterns (databits-parity-stopbits)
    private static readonly HashSet<string> ValidDataPatterns = new(StringComparer.OrdinalIgnoreCase)
    {
        "n81", "n82", "e71", "e81", "o71", "o81", "n71"
    };

    public ConfigurationValidator(ILogger<ConfigurationValidator> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region Public Methods

    /// <summary>
    /// Validate XML file (schema + business rules)
    /// </summary>
    public async Task<BibValidationResult> ValidateAsync(string xmlFilePath)
    {
        if (string.IsNullOrWhiteSpace(xmlFilePath))
            throw new ArgumentException("XML file path cannot be null or empty", nameof(xmlFilePath));

        _logger.LogDebug("🔍 Validating XML file: {FilePath}", xmlFilePath);

        var result = new BibValidationResult();

        try
        {
            // Step 1: XML Schema validation
            var schemaResult = await ValidateXmlSchemaAsync(xmlFilePath);
            result.Errors.AddRange(schemaResult.Errors);
            result.Warnings.AddRange(schemaResult.Warnings);

            if (!schemaResult.IsValid)
            {
                _logger.LogWarning("⚠️ XML schema validation failed for: {FilePath}", xmlFilePath);
                return result; // Don't proceed to business rules if schema is invalid
            }

            // Step 2: Parse and validate business rules
            var bibConfig = await ParseBibConfigurationAsync(xmlFilePath);
            if (bibConfig != null)
            {
                var businessResult = await ValidateBusinessRulesAsync(bibConfig);
                result.Errors.AddRange(businessResult.Errors);
                result.Warnings.AddRange(businessResult.Warnings);
            }

            _logger.LogInformation("✅ XML validation completed: {Status}", result.IsValid ? "Valid" : "Invalid");
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error during XML validation: {Error}", ex.Message);
            result.AddError($"Validation error: {ex.Message}");
            return result;
        }
    }

    /// <summary>
    /// Validate BIB configuration object (business rules only)
    /// </summary>
    public async Task<BibValidationResult> ValidateAsync(BibConfiguration bibConfiguration)
    {
        if (bibConfiguration == null)
            throw new ArgumentNullException(nameof(bibConfiguration));

        _logger.LogDebug("🔍 Validating BIB configuration: {BibId}", bibConfiguration.BibId);

        var result = await ValidateBusinessRulesAsync(bibConfiguration);
        
        _logger.LogInformation("✅ BIB validation completed: {Status}", result.IsValid ? "Valid" : "Invalid");
        return result;
    }

    /// <summary>
    /// Validate XML schema
    /// </summary>
    public async Task<BibValidationResult> ValidateXmlSchemaAsync(string xmlFilePath)
    {
        var result = new BibValidationResult();

        try
        {
            if (!File.Exists(xmlFilePath))
            {
                result.AddError($"XML file not found: {xmlFilePath}");
                return result;
            }

            _logger.LogDebug("📄 Validating XML schema: {FilePath}", xmlFilePath);

            // Read and parse XML
            var content = await File.ReadAllTextAsync(xmlFilePath);
            if (string.IsNullOrWhiteSpace(content))
            {
                result.AddError("XML file is empty");
                return result;
            }

            // Basic XML structure validation
            XDocument doc;
            try
            {
                doc = XDocument.Parse(content);
            }
            catch (XmlException ex)
            {
                result.AddError($"Invalid XML structure: {ex.Message}");
                return result;
            }

            // Validate BIB structure
            await ValidateBibStructureAsync(doc, result);

            if (result.IsValid)
            {
                _logger.LogDebug("✅ XML schema validation passed");
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error during XML schema validation: {Error}", ex.Message);
            result.AddError($"Schema validation error: {ex.Message}");
            return result;
        }
    }

    /// <summary>
    /// Validate business rules for BIB configuration
    /// </summary>
    public async Task<BibValidationResult> ValidateBusinessRulesAsync(BibConfiguration bibConfiguration)
    {
        var result = new BibValidationResult();

        try
        {
            _logger.LogDebug("🔧 Validating business rules for BIB: {BibId}", bibConfiguration.BibId);

            // Basic BIB validation
            ValidateBasicBibProperties(bibConfiguration, result);

            // UUT validation
            ValidateUuts(bibConfiguration, result);

            // Port validation
            ValidatePorts(bibConfiguration, result);

            // Command validation
            ValidateCommands(bibConfiguration, result);

            await Task.CompletedTask; // Make it properly async

            if (result.IsValid)
            {
                _logger.LogDebug("✅ Business rules validation passed");
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error during business rules validation: {Error}", ex.Message);
            result.AddError($"Business rules validation error: {ex.Message}");
            return result;
        }
    }

    #endregion

    #region XML Schema Validation

    private async Task ValidateBibStructureAsync(XDocument doc, BibValidationResult result)
    {
        var root = doc.Root;
        if (root == null)
        {
            result.AddError("XML document has no root element");
            return;
        }

        // Check for BIB element (could be root or child of root)
        var bibElement = root.Name.LocalName == "bib" ? root : root.Element("bib");
        
        if (bibElement == null)
        {
            result.AddError("No 'bib' element found in XML");
            return;
        }

        // Validate required BIB attributes
        var bibId = bibElement.Attribute("id")?.Value;
        if (string.IsNullOrWhiteSpace(bibId))
        {
            result.AddError("BIB element must have an 'id' attribute");
        }

        // Validate UUT elements
        var uutElements = bibElement.Elements("uut");
        if (!uutElements.Any())
        {
            result.AddError("BIB must contain at least one 'uut' element");
            return;
        }

        foreach (var uutElement in uutElements)
        {
            ValidateUutElement(uutElement, result);
        }

        await Task.CompletedTask;
    }

    private void ValidateUutElement(XElement uutElement, BibValidationResult result)
    {
        // Validate UUT ID
        var uutId = uutElement.Attribute("id")?.Value;
        if (string.IsNullOrWhiteSpace(uutId))
        {
            result.AddError("UUT element must have an 'id' attribute");
        }

        // Validate port elements
        var portElements = uutElement.Elements("port");
        if (!portElements.Any())
        {
            result.AddError($"UUT '{uutId}' must contain at least one 'port' element");
            return;
        }

        foreach (var portElement in portElements)
        {
            ValidatePortElement(portElement, result, uutId);
        }
    }

    private void ValidatePortElement(XElement portElement, BibValidationResult result, string? uutId)
    {
        // Validate port number
        var portNumberStr = portElement.Attribute("number")?.Value;
        if (string.IsNullOrWhiteSpace(portNumberStr) || !int.TryParse(portNumberStr, out var portNumber) || portNumber <= 0)
        {
            result.AddError($"Port in UUT '{uutId}' must have a valid positive 'number' attribute");
        }

        // Validate required elements
        var requiredElements = new[] { "protocol", "speed", "data_pattern" };
        foreach (var required in requiredElements)
        {
            if (portElement.Element(required) == null)
            {
                result.AddError($"Port {portNumber} in UUT '{uutId}' is missing required element: {required}");
            }
        }

        // Validate command sequences
        var commandSequences = new[] { "start", "test", "stop" };
        foreach (var sequence in commandSequences)
        {
            var sequenceElement = portElement.Element(sequence);
            if (sequenceElement != null)
            {
                ValidateCommandSequenceElement(sequenceElement, result, uutId, portNumber, sequence);
            }
        }
    }

   private void ValidateCommandSequenceElement(XElement sequenceElement, BibValidationResult result, string? uutId, int portNumber, string sequenceType)
    {
        var commandElement = sequenceElement.Element("command");
        var responseElement = sequenceElement.Element("expected_response");
        var timeoutElement = sequenceElement.Element("timeout_ms");

        // Use safe port display for error messages (FIXED: CS0165)
        var portDisplay = portNumber > 0 ? portNumber.ToString() : "unknown";

        if (commandElement == null || string.IsNullOrWhiteSpace(commandElement.Value))
        {
            result.AddError($"Port {portDisplay} in UUT '{uutId}': {sequenceType} sequence missing 'command' element");
        }

        if (responseElement == null || string.IsNullOrWhiteSpace(responseElement.Value))
        {
            result.AddError($"Port {portDisplay} in UUT '{uutId}': {sequenceType} sequence missing 'expected_response' element");
        }

        if (timeoutElement != null && !int.TryParse(timeoutElement.Value, out var timeout))
        {
            result.AddError($"Port {portDisplay} in UUT '{uutId}': {sequenceType} sequence has invalid timeout value");
        }
    }

    #endregion

    #region Business Rules Validation

    private void ValidateBasicBibProperties(BibConfiguration bibConfiguration, BibValidationResult result)
    {
        // BIB ID validation
        if (string.IsNullOrWhiteSpace(bibConfiguration.BibId))
        {
            result.AddError("BIB ID cannot be empty");
        }
        else if (!IsValidIdentifier(bibConfiguration.BibId))
        {
            result.AddError("BIB ID contains invalid characters");
        }

        // Description validation
        if (string.IsNullOrWhiteSpace(bibConfiguration.Description))
        {
            result.AddWarning("BIB description is empty");
        }
    }

    private void ValidateUuts(BibConfiguration bibConfiguration, BibValidationResult result)
    {
        if (!bibConfiguration.Uuts.Any())
        {
            result.AddError("BIB must contain at least one UUT");
            return;
        }

        var uutIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        
        foreach (var uut in bibConfiguration.Uuts)
        {
            // UUT ID validation
            if (string.IsNullOrWhiteSpace(uut.UutId))
            {
                result.AddError("UUT ID cannot be empty");
                continue;
            }

            // Check for duplicate UUT IDs
            if (!uutIds.Add(uut.UutId))
            {
                result.AddError($"Duplicate UUT ID found: {uut.UutId}");
            }

            // UUT must have ports
            if (!uut.Ports.Any())
            {
                result.AddError($"UUT '{uut.UutId}' must have at least one port");
            }
        }
    }

    private void ValidatePorts(BibConfiguration bibConfiguration, BibValidationResult result)
    {
        foreach (var uut in bibConfiguration.Uuts)
        {
            var portNumbers = new HashSet<int>();
            
            foreach (var port in uut.Ports)
            {
                // Port number validation
                if (port.PortNumber <= 0)
                {
                    result.AddError($"UUT '{uut.UutId}': Port number must be positive");
                    continue;
                }

                // Check for duplicate port numbers within UUT
                if (!portNumbers.Add(port.PortNumber))
                {
                    result.AddError($"UUT '{uut.UutId}': Duplicate port number {port.PortNumber}");
                }

                // Protocol validation
                if (string.IsNullOrWhiteSpace(port.Protocol))
                {
                    result.AddError($"UUT '{uut.UutId}' Port {port.PortNumber}: Protocol cannot be empty");
                }
                else if (!SupportedProtocols.Contains(port.Protocol))
                {
                    result.AddError($"UUT '{uut.UutId}' Port {port.PortNumber}: Unsupported protocol '{port.Protocol}'");
                }

                // Baud rate validation
                if (port.Speed <= 0)
                {
                    result.AddError($"UUT '{uut.UutId}' Port {port.PortNumber}: Speed must be positive");
                }
                else if (!CommonBaudRates.Contains(port.Speed))
                {
                    result.AddWarning($"UUT '{uut.UutId}' Port {port.PortNumber}: Unusual baud rate {port.Speed}");
                }

                // Data pattern validation
                if (string.IsNullOrWhiteSpace(port.DataPattern))
                {
                    result.AddError($"UUT '{uut.UutId}' Port {port.PortNumber}: Data pattern cannot be empty");
                }
                else if (!ValidDataPatterns.Contains(port.DataPattern))
                {
                    result.AddError($"UUT '{uut.UutId}' Port {port.PortNumber}: Invalid data pattern '{port.DataPattern}'");
                }
            }
        }
    }

    private void ValidateCommands(BibConfiguration bibConfiguration, BibValidationResult result)
    {
        foreach (var uut in bibConfiguration.Uuts)
        {
            foreach (var port in uut.Ports)
            {
                // Validate command sequences
                ValidateCommandSequence(port.StartCommands, "Start", uut.UutId, port.PortNumber, result);
                ValidateCommandSequence(port.TestCommands, "Test", uut.UutId, port.PortNumber, result);
                ValidateCommandSequence(port.StopCommands, "Stop", uut.UutId, port.PortNumber, result);
            }
        }
    }

    private void ValidateCommandSequence(CommandSequence? sequence, string sequenceType, string uutId, int portNumber, BibValidationResult result)
    {
        if (sequence == null || !sequence.Commands.Any())
        {
            result.AddWarning($"UUT '{uutId}' Port {portNumber}: {sequenceType} command sequence is empty");
            return;
        }

        foreach (var command in sequence.Commands)
        {
            if (string.IsNullOrWhiteSpace(command.Command))
            {
                result.AddError($"UUT '{uutId}' Port {portNumber}: {sequenceType} sequence contains empty command");
            }

            if (string.IsNullOrWhiteSpace(command.ExpectedResponse))
            {
                result.AddWarning($"UUT '{uutId}' Port {portNumber}: {sequenceType} command '{command.Command}' has no expected response");
            }

            if (command.TimeoutMs <= 0)
            {
                result.AddWarning($"UUT '{uutId}' Port {portNumber}: {sequenceType} command '{command.Command}' has invalid timeout");
            }
        }
    }

    #endregion

    #region Helper Methods

    private static bool IsValidIdentifier(string identifier)
    {
        // Allow alphanumeric, underscore, and hyphen
        return Regex.IsMatch(identifier, @"^[a-zA-Z0-9_-]+$");
    }

    private async Task<BibConfiguration?> ParseBibConfigurationAsync(string xmlFilePath)
    {
        try
        {
            // This is a simplified parser - in real implementation, 
            // you'd use the actual XmlBibConfigurationLoader
            var content = await File.ReadAllTextAsync(xmlFilePath);
            var doc = XDocument.Parse(content);
            
            // Basic parsing for validation purposes
            var bibElement = doc.Root?.Name.LocalName == "bib" ? doc.Root : doc.Root?.Element("bib");
            if (bibElement == null) return null;

            var bibConfig = new BibConfiguration
            {
                BibId = bibElement.Attribute("id")?.Value ?? "",
                Description = bibElement.Attribute("description")?.Value ?? "",
                Uuts = new List<UutConfiguration>()
            };

            foreach (var uutElement in bibElement.Elements("uut"))
            {
                var uut = new UutConfiguration
                {
                    UutId = uutElement.Attribute("id")?.Value ?? "",
                    Description = uutElement.Attribute("description")?.Value ?? "",
                    Ports = new List<PortConfiguration>()
                };

                foreach (var portElement in uutElement.Elements("port"))
                {
                    var port = new PortConfiguration
                    {
                        PortNumber = int.TryParse(portElement.Attribute("number")?.Value, out var num) ? num : 0,
                        Protocol = portElement.Element("protocol")?.Value ?? "",
                        Speed = int.TryParse(portElement.Element("speed")?.Value, out var speed) ? speed : 0,
                        DataPattern = portElement.Element("data_pattern")?.Value ?? ""
                    };

                    // Parse command sequences (simplified)
                    port.StartCommands = ParseCommandSequence(portElement.Element("start"));
                    port.TestCommands = ParseCommandSequence(portElement.Element("test"));
                    port.StopCommands = ParseCommandSequence(portElement.Element("stop"));

                    uut.Ports.Add(port);
                }

                bibConfig.Uuts.Add(uut);
            }

            return bibConfig;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "⚠️ Could not parse BIB configuration for validation: {Error}", ex.Message);
            return null;
        }
    }

    private static CommandSequence ParseCommandSequence(XElement? sequenceElement)
    {
        var sequence = new CommandSequence();
        
        if (sequenceElement != null)
        {
            var command = new ProtocolCommand
            {
                Command = sequenceElement.Element("command")?.Value ?? "",
                ExpectedResponse = sequenceElement.Element("expected_response")?.Value ?? "",
                TimeoutMs = int.TryParse(sequenceElement.Element("timeout_ms")?.Value, out var timeout) ? timeout : 3000
            };
            
            sequence.Commands.Add(command);
        }

        return sequence;
    }

    #endregion
}

/// <summary>
/// Result of BIB validation
/// </summary>
public class BibValidationResult
{
    public List<string> Errors { get; } = new();
    public List<string> Warnings { get; } = new();
    
    public bool IsValid => !Errors.Any();
    public bool HasWarnings => Warnings.Any();

    public void AddError(string error)
    {
        if (!string.IsNullOrWhiteSpace(error))
            Errors.Add(error);
    }

    public void AddWarning(string warning)
    {
        if (!string.IsNullOrWhiteSpace(warning))
            Warnings.Add(warning);
    }

    public string GetSummary()
    {
        if (IsValid && !HasWarnings)
            return "✅ Configuration validation successful";
        
        if (IsValid && HasWarnings)
            return $"⚠️ Configuration valid with {Warnings.Count} warning(s)";
        
        return $"❌ Configuration invalid: {Errors.Count} error(s), {Warnings.Count} warning(s)";
    }
}