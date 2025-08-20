// ===================================================================
// SPRINT 11 BOUCHÉE #3.2: Configuration Validator - Production Safety
// File: SerialPortPool.Core/Services/Configuration/ConfigurationValidator.cs
// Purpose: Multi-level validation (XML Schema + Business Rules)
// Philosophy: "Single Responsibility" - ONLY validation concerns
// ===================================================================

using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using Microsoft.Extensions.Logging;
using SerialPortPool.Core.Models;

namespace SerialPortPool.Core.Services.Configuration;

/// <summary>
/// SPRINT 11: Production-grade configuration validator
/// SINGLE RESPONSIBILITY: XML Schema + Business Rules validation only
/// ZERO TOUCH: Works with any XML configuration file
/// </summary>
public class ConfigurationValidator : IConfigurationValidator
{
    private readonly ILogger<ConfigurationValidator> _logger;
    private readonly ConfigurationValidationOptions _options;
    private readonly XmlSchemaSet? _schemaSet;

    public ConfigurationValidator(
        ILogger<ConfigurationValidator> logger,
        ConfigurationValidationOptions? options = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options ?? ConfigurationValidationOptions.CreateDefault();
        
        // Load XSD schema if available
        _schemaSet = LoadXmlSchema();
        
        _logger.LogInformation("🔍 Configuration Validator initialized (Schema: {HasSchema})", 
            _schemaSet != null ? "Loaded" : "Not Available");
    }

    /// <summary>
    /// Comprehensive validation: XML Schema + Business Rules
    /// </summary>
    public async Task<ConfigurationValidationResult> ValidateConfigurationFileAsync(string filePath)
    {
        var result = new ConfigurationValidationResult();
        
        try
        {
            _logger.LogDebug("🔍 Validating configuration file: {FilePath}", Path.GetFileName(filePath));

            // STEP 1: Basic file checks
            if (!await ValidateFileAccessibilityAsync(filePath, result))
            {
                return result;
            }

            // STEP 2: XML well-formedness validation
            var xmlDocument = await ValidateXmlWellFormednessAsync(filePath, result);
            if (xmlDocument == null)
            {
                return result;
            }

            // STEP 3: XML Schema validation (if schema available)
            await ValidateXmlSchemaAsync(xmlDocument, result);

            // STEP 4: Business rules validation
            await ValidateBusinessRulesAsync(xmlDocument, result);

            // STEP 5: Performance and size checks
            await ValidatePerformanceConstraintsAsync(filePath, xmlDocument, result);

            if (result.IsValid)
            {
                _logger.LogInformation("✅ Configuration validation passed: {FilePath}", Path.GetFileName(filePath));
            }
            else
            {
                _logger.LogWarning("⚠️ Configuration validation failed: {FilePath} - {ErrorCount} errors, {WarningCount} warnings", 
                    Path.GetFileName(filePath), result.Errors.Count, result.Warnings.Count);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Configuration validation exception: {FilePath}", filePath);
            result.AddError($"Validation exception: {ex.Message}");
            return result;
        }
    }

    /// <summary>
    /// Quick validation for BIB ID format and basic structure
    /// </summary>
    public async Task<bool> ValidateBibIdAsync(string bibId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(bibId))
                return false;

            // Basic BIB ID format validation
            if (bibId.Length < 3 || bibId.Length > 50)
                return false;

            // Alphanumeric with underscores/hyphens only
            if (!System.Text.RegularExpressions.Regex.IsMatch(bibId, @"^[a-zA-Z0-9_\-\.]+$"))
                return false;

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "BIB ID validation error: {BibId}", bibId);
            return false;
        }
    }

    /// <summary>
    /// Validate XML content (string) before writing to file
    /// </summary>
    public async Task<ConfigurationValidationResult> ValidateXmlContentAsync(string xmlContent, string contextName = "XML Content")
    {
        var result = new ConfigurationValidationResult();

        try
        {
            _logger.LogDebug("🔍 Validating XML content: {Context}", contextName);

            if (string.IsNullOrWhiteSpace(xmlContent))
            {
                result.AddError("XML content is empty or whitespace");
                return result;
            }

            // Parse XML from string
            XDocument xmlDocument;
            try
            {
                xmlDocument = XDocument.Parse(xmlContent);
            }
            catch (XmlException ex)
            {
                result.AddError($"XML parsing error: {ex.Message}");
                return result;
            }

            // Validate against schema if available
            if (_schemaSet != null)
            {
                await ValidateXmlSchemaAsync(xmlDocument, result);
            }

            // Business rules validation
            await ValidateBusinessRulesAsync(xmlDocument, result);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ XML content validation exception: {Context}", contextName);
            result.AddError($"Content validation exception: {ex.Message}");
            return result;
        }
    }

    #region Private Validation Methods

    /// <summary>
    /// Validate file accessibility and basic requirements
    /// </summary>
    private async Task<bool> ValidateFileAccessibilityAsync(string filePath, ConfigurationValidationResult result)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                result.AddError($"Configuration file not found: {filePath}");
                return false;
            }

            var fileInfo = new FileInfo(filePath);
            
            // File size validation
            if (fileInfo.Length == 0)
            {
                result.AddError("Configuration file is empty");
                return false;
            }

            if (fileInfo.Length > _options.MaxFileSizeBytes)
            {
                result.AddError($"Configuration file too large: {fileInfo.Length} bytes (max: {_options.MaxFileSizeBytes})");
                return false;
            }

            // File extension validation
            if (_options.RequiredFileExtension != null && 
                !filePath.EndsWith(_options.RequiredFileExtension, StringComparison.OrdinalIgnoreCase))
            {
                result.AddWarning($"Unexpected file extension. Expected: {_options.RequiredFileExtension}");
            }

            return true;
        }
        catch (Exception ex)
        {
            result.AddError($"File accessibility check failed: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Validate XML well-formedness
    /// </summary>
    private async Task<XDocument?> ValidateXmlWellFormednessAsync(string filePath, ConfigurationValidationResult result)
    {
        try
        {
            var xmlContent = await File.ReadAllTextAsync(filePath);
            
            // Basic content checks
            if (!xmlContent.TrimStart().StartsWith("<?xml") && !xmlContent.TrimStart().StartsWith("<"))
            {
                result.AddError("File does not appear to be XML (missing XML declaration or root element)");
                return null;
            }

            // Parse XML document
            var xmlDocument = XDocument.Load(filePath);
            
            // Basic structure validation
            if (xmlDocument.Root == null)
            {
                result.AddError("XML document has no root element");
                return null;
            }

            return xmlDocument;
        }
        catch (XmlException ex)
        {
            result.AddError($"XML parsing error at line {ex.LineNumber}, position {ex.LinePosition}: {ex.Message}");
            return null;
        }
        catch (Exception ex)
        {
            result.AddError($"XML well-formedness validation failed: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Validate against XML Schema (XSD) if available
    /// </summary>
    private async Task ValidateXmlSchemaAsync(XDocument xmlDocument, ConfigurationValidationResult result)
    {
        if (_schemaSet == null)
        {
            _logger.LogDebug("📝 Skipping schema validation (no XSD schema available)");
            return;
        }

        try
        {
            _logger.LogDebug("📋 Performing XML Schema validation");

            xmlDocument.Validate(_schemaSet, (sender, e) =>
            {
                if (e.Severity == XmlSeverityType.Error)
                {
                    result.AddError($"Schema validation error: {e.Message}");
                }
                else if (e.Severity == XmlSeverityType.Warning)
                {
                    result.AddWarning($"Schema validation warning: {e.Message}");
                }
            });

            if (!result.Errors.Any())
            {
                _logger.LogDebug("✅ XML Schema validation passed");
            }
        }
        catch (Exception ex)
        {
            result.AddError($"Schema validation exception: {ex.Message}");
        }
    }

    /// <summary>
    /// Validate business rules and logical constraints
    /// </summary>
    private async Task ValidateBusinessRulesAsync(XDocument xmlDocument, ConfigurationValidationResult result)
    {
        try
        {
            _logger.LogDebug("🔧 Performing business rules validation");

            var root = xmlDocument.Root;
            if (root == null) return;

            // Validate based on root element type
            if (root.Name.LocalName == "bib")
            {
                await ValidateIndividualBibRulesAsync(root, result);
            }
            else if (root.Name.LocalName == "root")
            {
                await ValidateLegacyMultiBibRulesAsync(root, result);
            }
            else
            {
                result.AddError($"Unknown root element: {root.Name.LocalName}. Expected 'bib' or 'root'");
            }
        }
        catch (Exception ex)
        {
            result.AddError($"Business rules validation exception: {ex.Message}");
        }
    }

    /// <summary>
    /// Validate individual BIB file business rules
    /// </summary>
    private async Task ValidateIndividualBibRulesAsync(XElement bibElement, ConfigurationValidationResult result)
    {
        // BIB ID validation
        var bibId = bibElement.Attribute("id")?.Value;
        if (string.IsNullOrWhiteSpace(bibId))
        {
            result.AddError("BIB element must have a non-empty 'id' attribute");
        }
        else if (!await ValidateBibIdAsync(bibId))
        {
            result.AddError($"Invalid BIB ID format: '{bibId}'");
        }

        // UUT validation
        var uuts = bibElement.Elements("uut").ToList();
        if (!uuts.Any())
        {
            result.AddError("BIB must contain at least one UUT element");
            return;
        }

        foreach (var uut in uuts)
        {
            await ValidateUutRulesAsync(uut, result, bibId);
        }
    }

    /// <summary>
    /// Validate legacy multi-BIB file business rules
    /// </summary>
    private async Task ValidateLegacyMultiBibRulesAsync(XElement rootElement, ConfigurationValidationResult result)
    {
        var bibs = rootElement.Elements("bib").ToList();
        if (!bibs.Any())
        {
            result.AddError("Root element must contain at least one BIB element");
            return;
        }

        var bibIds = new HashSet<string>();
        foreach (var bib in bibs)
        {
            var bibId = bib.Attribute("id")?.Value;
            if (string.IsNullOrWhiteSpace(bibId))
            {
                result.AddError("BIB element must have a non-empty 'id' attribute");
                continue;
            }

            // Check for duplicate BIB IDs
            if (!bibIds.Add(bibId))
            {
                result.AddError($"Duplicate BIB ID found: '{bibId}'");
            }

            await ValidateIndividualBibRulesAsync(bib, result);
        }
    }

    /// <summary>
    /// Validate UUT business rules
    /// </summary>
    private async Task ValidateUutRulesAsync(XElement uutElement, ConfigurationValidationResult result, string? parentBibId)
    {
        var uutId = uutElement.Attribute("id")?.Value;
        if (string.IsNullOrWhiteSpace(uutId))
        {
            result.AddError($"UUT element must have a non-empty 'id' attribute (BIB: {parentBibId})");
            return;
        }

        // Port validation
        var ports = uutElement.Elements("port").ToList();
        if (!ports.Any())
        {
            result.AddError($"UUT '{uutId}' must contain at least one port element");
            return;
        }

        var portNumbers = new HashSet<int>();
        foreach (var port in ports)
        {
            var portNumberAttr = port.Attribute("number")?.Value;
            if (!int.TryParse(portNumberAttr, out var portNumber) || portNumber <= 0)
            {
                result.AddError($"Port must have a valid positive number attribute (UUT: {uutId})");
                continue;
            }

            // Check for duplicate port numbers within UUT
            if (!portNumbers.Add(portNumber))
            {
                result.AddError($"Duplicate port number {portNumber} in UUT '{uutId}'");
            }

            await ValidatePortRulesAsync(port, result, uutId, portNumber);
        }
    }

    /// <summary>
    /// Validate port business rules
    /// </summary>
    private async Task ValidatePortRulesAsync(XElement portElement, ConfigurationValidationResult result, string uutId, int portNumber)
    {
        // Protocol validation
        var protocolElement = portElement.Element("protocol");
        if (protocolElement == null || string.IsNullOrWhiteSpace(protocolElement.Value))
        {
            result.AddError($"Port {portNumber} in UUT '{uutId}' must have a protocol element");
            return;
        }

        var protocol = protocolElement.Value.ToLowerInvariant();
        if (!_options.SupportedProtocols.Contains(protocol))
        {
            result.AddWarning($"Protocol '{protocol}' may not be supported (Port {portNumber}, UUT: {uutId})");
        }

        // Speed validation for serial protocols
        if (protocol == "rs232" || protocol == "rs485")
        {
            var speedElement = portElement.Element("speed");
            if (speedElement != null && int.TryParse(speedElement.Value, out var speed))
            {
                if (!_options.ValidBaudRates.Contains(speed))
                {
                    result.AddWarning($"Unusual baud rate {speed} for {protocol.ToUpper()} (Port {portNumber}, UUT: {uutId})");
                }
            }
        }
    }

    /// <summary>
    /// Validate performance constraints
    /// </summary>
    private async Task ValidatePerformanceConstraintsAsync(string filePath, XDocument xmlDocument, ConfigurationValidationResult result)
    {
        try
        {
            // Element count validation
            var totalElements = xmlDocument.Descendants().Count();
            if (totalElements > _options.MaxElementCount)
            {
                result.AddWarning($"Configuration has {totalElements} elements (recommended max: {_options.MaxElementCount})");
            }

            // Depth validation
            var maxDepth = GetMaxDepth(xmlDocument.Root);
            if (maxDepth > _options.MaxXmlDepth)
            {
                result.AddWarning($"XML depth is {maxDepth} levels (recommended max: {_options.MaxXmlDepth})");
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Performance validation failed for: {FilePath}", filePath);
            // Non-critical, don't add to result
        }
    }

    /// <summary>
    /// Calculate maximum XML depth
    /// </summary>
    private int GetMaxDepth(XElement? element, int currentDepth = 1)
    {
        if (element == null || !element.HasElements)
            return currentDepth;

        return element.Elements().Max(child => GetMaxDepth(child, currentDepth + 1));
    }

    /// <summary>
    /// Load XML Schema from file system
    /// </summary>
    private XmlSchemaSet? LoadXmlSchema()
    {
        try
        {
            var schemaPath = Path.Combine(_options.BaseDirectory, "schemas", "bib_configuration.xsd");
            if (!File.Exists(schemaPath))
            {
                _logger.LogDebug("📝 No XSD schema found at: {SchemaPath}", schemaPath);
                return null;
            }

            var schemaSet = new XmlSchemaSet();
            schemaSet.Add("", schemaPath);
            schemaSet.Compile();

            _logger.LogInformation("📋 XML Schema loaded successfully: {SchemaPath}", Path.GetFileName(schemaPath));
            return schemaSet;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "⚠️ Failed to load XML schema, schema validation will be skipped");
            return null;
        }
    }

    #endregion
}

/// <summary>
/// Interface for configuration validator
/// </summary>
public interface IConfigurationValidator
{
    Task<ConfigurationValidationResult> ValidateConfigurationFileAsync(string filePath);
    Task<ConfigurationValidationResult> ValidateXmlContentAsync(string xmlContent, string contextName = "XML Content");
    Task<bool> ValidateBibIdAsync(string bibId);
}

/// <summary>
/// Configuration validation options
/// </summary>
public class ConfigurationValidationOptions
{
    public string BaseDirectory { get; set; } = "Configuration";
    public string? RequiredFileExtension { get; set; } = ".xml";
    public long MaxFileSizeBytes { get; set; } = 10 * 1024 * 1024; // 10MB
    public int MaxElementCount { get; set; } = 1000;
    public int MaxXmlDepth { get; set; } = 10;
    
    public HashSet<string> SupportedProtocols { get; set; } = new(StringComparer.OrdinalIgnoreCase)
    {
        "rs232", "rs485", "usb", "can", "i2c", "spi"
    };
    
    public HashSet<int> ValidBaudRates { get; set; } = new()
    {
        1200, 2400, 4800, 9600, 19200, 38400, 57600, 115200, 230400
    };

    public static ConfigurationValidationOptions CreateDefault() => new();
}