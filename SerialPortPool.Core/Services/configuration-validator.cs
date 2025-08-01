using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Schema;
using Microsoft.Extensions.Logging;
using SerialPortPool.Core.Interfaces;
using SerialPortPool.Core.Models;  // ← FIXED: Suppression de .Configuration

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
            _logger.LogDebug("Validating system configuration");

            // Validate basic structure
            if (configuration.Bibs?.Any() != true)
            {
                result.AddError("System configuration must contain at least one BIB");
                return result;
            }

            // Validate each BIB
            foreach (var bib in configuration.Bibs)
            {
                ValidateBibInternal(bib, result);
            }

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

        // Validate UUTs
        if (!bib.Uuts.Any())
        {
            result.AddError($"BIB '{bib.BibId}' must contain at least one UUT");
            return;
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
        if (string.IsNullOrWhiteSpace(uut.UutId))
        {
            result.AddError($"UUT ID cannot be empty in BIB '{bibId}'");
            return;
        }

        if (!uut.Ports.Any())
        {
            result.AddError($"UUT '{uut.UutId}' must contain at least one port");
            return;
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
        if (port.PortNumber <= 0)
        {
            result.AddError($"Port number must be positive in UUT '{uutId}': {port.PortNumber}");
        }

        if (string.IsNullOrWhiteSpace(port.Protocol))
        {
            result.AddError($"Protocol cannot be empty for port {port.PortNumber} in UUT '{uutId}'");
        }
    }
}