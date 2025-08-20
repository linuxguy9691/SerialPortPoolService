using System;
using System.IO;
using System.Xml;
using Microsoft.Extensions.Caching.Memory;  // ✅ Correct
using Microsoft.Extensions.Logging;
using SerialPortPool.Core.Models;

namespace SerialPortPool.Core.Services;

/// <summary>
/// XML Configuration Loader - Parses BIB → UUT → PORT → PROTOCOL hierarchy
/// Supports caching, validation, and protocol-specific settings
/// </summary>
public class XmlConfigurationLoader : IXmlConfigurationLoader
{
    private readonly ILogger<XmlConfigurationLoader> _logger;
    private readonly IConfigurationValidator _validator;
    private readonly IMemoryCache _cache;
    private readonly ConfigurationLoadOptions _options;

    public XmlConfigurationLoader(
        ILogger<XmlConfigurationLoader> logger,
        IConfigurationValidator validator,
        IMemoryCache cache,
        ConfigurationLoadOptions? options = null)
    {
        _logger = logger;
        _validator = validator;
        _cache = cache;
        _options = options ?? new ConfigurationLoadOptions();
    }

    /// <summary>
    /// Load complete system configuration from XML file
    /// </summary>
    public async Task<SystemConfiguration> LoadConfigurationAsync(string xmlPath)
    {
        var cacheKey = $"config_{xmlPath}_{File.GetLastWriteTime(xmlPath).Ticks}";
        
        if (_cache.TryGetValue(cacheKey, out SystemConfiguration? cachedConfig) && cachedConfig != null)
        {
            _logger.LogDebug("Configuration loaded from cache: {XmlPath}", xmlPath);
            return cachedConfig;
        }

        _logger.LogInformation("Loading XML configuration from: {XmlPath}", xmlPath);

        if (!File.Exists(xmlPath))
        {
            throw new FileNotFoundException($"Configuration file not found: {xmlPath}");
        }

        try
        {
            // Load and parse XML document
            var xmlDoc = new XmlDocument();
            xmlDoc.Load(xmlPath);

            // Validate schema if requested
            if (_options.ValidateSchema)
            {
                var schemaValidation = await _validator.ValidateSchemaAsync(xmlPath);
                if (!schemaValidation.IsValid && _options.ThrowOnValidationErrors)
                {
                    throw new InvalidOperationException($"XML schema validation failed: {schemaValidation.GetSummary()}");
                }
            }

            // Parse configuration
            var configuration = ParseSystemConfiguration(xmlDoc, xmlPath);

            // Validate business rules if requested
            if (_options.ValidateBusinessRules)
            {
                var businessValidation = _validator.ValidateConfiguration(configuration);
                if (!businessValidation.IsValid && _options.ThrowOnValidationErrors)
                {
                    throw new InvalidOperationException($"Configuration validation failed: {businessValidation.GetSummary()}");
                }
                
                LogValidationMessages(businessValidation);
            }

            // Cache the configuration
            var cacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = _options.CacheExpiration,
                Priority = CacheItemPriority.High
            };
            _cache.Set(cacheKey, configuration, cacheOptions);

            _logger.LogInformation("Configuration loaded successfully: {BibCount} BIBs, {TotalPorts} total ports", 
                configuration.Bibs.Count, 
                configuration.Bibs.Sum(b => b.TotalPorts));

            return configuration;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load configuration from: {XmlPath}", xmlPath);
            throw;
        }
    }

    /// <summary>
    /// Load all BIB configurations as dictionary
    /// </summary>
    public async Task<Dictionary<string, BibConfiguration>> LoadAllBibsAsync(string xmlPath)
    {
        var configuration = await LoadConfigurationAsync(xmlPath);
        return configuration.Bibs.ToDictionary(b => b.BibId, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Load specific BIB configuration
    /// </summary>
    public async Task<BibConfiguration> LoadBibAsync(string xmlPath, string bibId)
    {
        var configuration = await LoadConfigurationAsync(xmlPath);
        var bib = configuration.GetBib(bibId);
        
        if (bib == null)
        {
            throw new ArgumentException($"BIB '{bibId}' not found in configuration: {xmlPath}");
        }

        return bib;
    }

    /// <summary>
    /// Validate configuration file
    /// </summary>
    public async Task<ConfigurationValidationResult> ValidateConfigurationAsync(string xmlPath)
    {
        try
        {
            var configuration = await LoadConfigurationAsync(xmlPath);
            return _validator.ValidateConfiguration(configuration);
        }
        catch (Exception ex)
        {
            var result = new ConfigurationValidationResult();
            result.AddError($"Failed to load configuration: {ex.Message}");
            return result;
        }
    }

    /// <summary>
    /// Get all supported protocols from configuration
    /// </summary>
    public async Task<IEnumerable<string>> GetSupportedProtocolsAsync(string xmlPath)
    {
        var configuration = await LoadConfigurationAsync(xmlPath);
        return configuration.GetAllProtocols();
    }

    /// <summary>
    /// Find port configurations matching criteria
    /// </summary>
    public async Task<IEnumerable<PortConfiguration>> FindPortConfigurationsAsync(string xmlPath, ConfigurationSearchCriteria criteria)
    {
        var configuration = await LoadConfigurationAsync(xmlPath);
        
        var query = configuration.Bibs.AsQueryable();
        
        // Filter by BIB ID
        if (!string.IsNullOrEmpty(criteria.BibId))
        {
            query = query.Where(b => b.BibId.Equals(criteria.BibId, StringComparison.OrdinalIgnoreCase));
        }

        var ports = query.SelectMany(b => b.Uuts)
                         .Where(u => string.IsNullOrEmpty(criteria.UutId) || 
                                   u.UutId.Equals(criteria.UutId, StringComparison.OrdinalIgnoreCase))
                         .SelectMany(u => u.Ports)
                         .Where(p => MatchesCriteria(p, criteria));

        return ports.ToList();
    }

    /// <summary>
    /// Parse XML document into system configuration
    /// </summary>
    private SystemConfiguration ParseSystemConfiguration(XmlDocument xmlDoc, string sourcePath)
    {
        var configuration = new SystemConfiguration
        {
            SourcePath = sourcePath,
            LoadedAt = DateTime.UtcNow
        };

        var rootNode = xmlDoc.DocumentElement;
        if (rootNode?.Name != "root")
        {
            throw new InvalidOperationException("XML root element must be 'root'");
        }

        // Parse each BIB
        foreach (XmlNode bibNode in rootNode.SelectNodes("bib") ?? new XmlNodeList())
        {
            var bib = ParseBibConfiguration(bibNode);
            configuration.Bibs.Add(bib);
        }

        return configuration;
    }

    /// <summary>
    /// Parse BIB configuration from XML node
    /// </summary>
    private BibConfiguration ParseBibConfiguration(XmlNode bibNode)
    {
        var bib = new BibConfiguration
        {
            BibId = GetRequiredAttribute(bibNode, "id"),
            Description = GetOptionalElement(bibNode, "description") ?? ""
        };

        // Parse each UUT
        foreach (XmlNode uutNode in bibNode.SelectNodes("uut") ?? new XmlNodeList())
        {
            var uut = ParseUutConfiguration(uutNode);
            bib.Uuts.Add(uut);
        }

        if (!bib.Uuts.Any())
        {
            throw new InvalidOperationException($"BIB '{bib.BibId}' must contain at least one UUT");
        }

        return bib;
    }

    /// <summary>
    /// Parse UUT configuration from XML node
    /// </summary>
    private UutConfiguration ParseUutConfiguration(XmlNode uutNode)
    {
        var uut = new UutConfiguration
        {
            UutId = GetRequiredAttribute(uutNode, "id"),
            Description = GetOptionalElement(uutNode, "description") ?? ""
        };

        // Parse each port
        foreach (XmlNode portNode in uutNode.SelectNodes("port") ?? new XmlNodeList())
        {
            var port = ParsePortConfiguration(portNode);
            uut.Ports.Add(port);
        }

        if (!uut.Ports.Any())
        {
            throw new InvalidOperationException($"UUT '{uut.UutId}' must contain at least one port");
        }

        return uut;
    }

    /// <summary>
    /// Parse port configuration from XML node
    /// </summary>
    private PortConfiguration ParsePortConfiguration(XmlNode portNode)
    {
        var port = new PortConfiguration
        {
            PortNumber = int.Parse(GetRequiredAttribute(portNode, "number")),
            Protocol = GetRequiredElement(portNode, "protocol"),
            Speed = int.Parse(GetRequiredElement(portNode, "speed")),
            DataPattern = GetOptionalElement(portNode, "data_pattern") ?? "n81",
            Description = GetOptionalElement(portNode, "description") ?? ""
        };

        // Parse protocol-specific settings
        if (_options.LoadProtocolSpecificSettings)
        {
            ParseProtocolSpecificSettings(portNode, port);
        }

        // Parse command sequences
        port.StartCommands = ParseCommandSequence(portNode.SelectSingleNode("start"));
        port.TestCommands = ParseCommandSequence(portNode.SelectSingleNode("test"));
        port.StopCommands = ParseCommandSequence(portNode.SelectSingleNode("stop"));

        return port;
    }

    /// <summary>
    /// Parse command sequence from XML node
    /// </summary>
    private CommandSequence ParseCommandSequence(XmlNode? sequenceNode)
    {
        if (sequenceNode == null)
        {
            return new CommandSequence();
        }

        var sequence = new CommandSequence
        {
            Description = GetOptionalElement(sequenceNode, "description") ?? ""
        };

        // Parse single command (backward compatibility)
        var commandElement = sequenceNode.SelectSingleNode("command");
        if (commandElement != null)
        {
            var command = new CommandDefinition
            {
                Command = commandElement.InnerText,
                ExpectedResponse = GetRequiredElement(sequenceNode, "expected_response"),
                TimeoutMs = int.Parse(GetOptionalElement(sequenceNode, "timeout_ms") ?? "5000"),
                RetryCount = int.Parse(GetOptionalElement(sequenceNode, "retry_count") ?? "1"),
                Description = GetOptionalElement(sequenceNode, "description") ?? ""
            };

            sequence.Commands.Add(command);
        }

        // Parse multiple commands (future enhancement)
        foreach (XmlNode cmdNode in sequenceNode.SelectNodes("commands/command") ?? new XmlNodeList())
        {
            var command = new CommandDefinition
            {
                Command = GetRequiredElement(cmdNode, "text"),
                ExpectedResponse = GetRequiredElement(cmdNode, "expected_response"),
                TimeoutMs = int.Parse(GetOptionalElement(cmdNode, "timeout_ms") ?? "5000"),
                RetryCount = int.Parse(GetOptionalElement(cmdNode, "retry_count") ?? "1"),
                Description = GetOptionalElement(cmdNode, "description") ?? "",
                IsRegex = bool.Parse(GetOptionalElement(cmdNode, "is_regex") ?? "false"),
                IsCritical = bool.Parse(GetOptionalElement(cmdNode, "is_critical") ?? "true")
            };

            sequence.Commands.Add(command);
        }

        return sequence;
    }

    /// <summary>
    /// Parse protocol-specific settings (RS485, USB, CAN, etc.)
    /// </summary>
    private void ParseProtocolSpecificSettings(XmlNode portNode, PortConfiguration port)
    {
        switch (port.Protocol.ToLowerInvariant())
        {
            case "rs485":
                port.ProtocolSettings["termination"] = bool.Parse(GetOptionalElement(portNode, "termination") ?? "false");
                port.ProtocolSettings["address"] = GetOptionalElement(portNode, "address") ?? "01";
                break;

            case "usb":
                port.ProtocolSettings["vendor_id"] = GetOptionalElement(portNode, "vendor_id") ?? "0x0403";
                port.ProtocolSettings["product_id"] = GetOptionalElement(portNode, "product_id") ?? "0x6001";
                port.ProtocolSettings["interface"] = int.Parse(GetOptionalElement(portNode, "interface") ?? "0");
                break;

            case "can":
                port.ProtocolSettings["can_id"] = GetOptionalElement(portNode, "can_id") ?? "0x123";
                port.ProtocolSettings["extended_id"] = bool.Parse(GetOptionalElement(portNode, "extended_id") ?? "false");
                break;

            case "i2c":
                port.ProtocolSettings["slave_address"] = GetOptionalElement(portNode, "slave_address") ?? "0x48";
                port.ProtocolSettings["addressing_mode"] = GetOptionalElement(portNode, "addressing_mode") ?? "7bit";
                break;

            case "spi":
                port.ProtocolSettings["mode"] = int.Parse(GetOptionalElement(portNode, "mode") ?? "0");
                port.ProtocolSettings["bit_order"] = GetOptionalElement(portNode, "bit_order") ?? "MSBFirst";
                port.ProtocolSettings["cs_pin"] = int.Parse(GetOptionalElement(portNode, "cs_pin") ?? "0");
                break;
        }
    }

    /// <summary>
    /// Check if port matches search criteria
    /// </summary>
    private bool MatchesCriteria(PortConfiguration port, ConfigurationSearchCriteria criteria)
    {
        if (!string.IsNullOrEmpty(criteria.Protocol) && 
            !port.Protocol.Equals(criteria.Protocol, StringComparison.OrdinalIgnoreCase))
            return false;

        if (criteria.PortNumber.HasValue && port.PortNumber != criteria.PortNumber.Value)
            return false;

        if (criteria.MinSpeed.HasValue && port.Speed < criteria.MinSpeed.Value)
            return false;

        if (criteria.MaxSpeed.HasValue && port.Speed > criteria.MaxSpeed.Value)
            return false;

        if (!string.IsNullOrEmpty(criteria.DataPattern) && 
            !port.DataPattern.Equals(criteria.DataPattern, StringComparison.OrdinalIgnoreCase))
            return false;

        return true;
    }

    /// <summary>
    /// Get required XML attribute value
    /// </summary>
    private string GetRequiredAttribute(XmlNode node, string attributeName)
    {
        var value = node.Attributes?[attributeName]?.Value;
        if (string.IsNullOrEmpty(value))
        {
            throw new InvalidOperationException($"Required attribute '{attributeName}' not found in {node.Name} element");
        }
        return value;
    }

    /// <summary>
    /// Get required XML element value
    /// </summary>
    private string GetRequiredElement(XmlNode node, string elementName)
    {
        var element = node.SelectSingleNode(elementName);
        if (element == null || string.IsNullOrEmpty(element.InnerText))
        {
            throw new InvalidOperationException($"Required element '{elementName}' not found in {node.Name}");
        }
        return element.InnerText;
    }

    /// <summary>
    /// Get optional XML element value
    /// </summary>
    private string? GetOptionalElement(XmlNode node, string elementName)
    {
        return node.SelectSingleNode(elementName)?.InnerText;
    }

    /// <summary>
    /// Log validation messages
    /// </summary>
    private void LogValidationMessages(ConfigurationValidationResult validation)
    {
        foreach (var error in validation.Errors)
        {
            _logger.LogError("Configuration error: {Error}", error);
        }

        foreach (var warning in validation.Warnings)
        {
            _logger.LogWarning("Configuration warning: {Warning}", warning);
        }

        foreach (var info in validation.Info)
        {
            _logger.LogInformation("Configuration info: {Info}", info);
        }
    }
}