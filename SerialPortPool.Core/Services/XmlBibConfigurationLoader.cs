using System;
using System.IO;
using System.Xml;
using Microsoft.Extensions.Caching.Memory;  // ✅ Correct
using Microsoft.Extensions.Logging;
using SerialPortPool.Core.Interfaces;       // ← AJOUTÉ: Pour IXmlConfigurationLoader et IConfigurationValidator
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

            _logger.LogInformation("Configuration loaded successfully: {BibCount} BIBs", 
                configuration.Bibs.Count);

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
        
        // Simple filtering - améliorer selon les besoins
        var allPorts = configuration.Bibs.SelectMany(b => b.GetAllPorts());
        
        if (!string.IsNullOrEmpty(criteria.Protocol))
        {
            allPorts = allPorts.Where(p => p.Protocol.Equals(criteria.Protocol, StringComparison.OrdinalIgnoreCase));
        }
        
        return allPorts.ToList();
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
            Description = GetOptionalAttribute(bibNode, "description") ?? ""
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
            Description = GetOptionalAttribute(uutNode, "description") ?? ""
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
            Speed = int.Parse(GetOptionalElement(portNode, "speed") ?? "115200"),
            DataPattern = GetOptionalElement(portNode, "data_pattern") ?? "n81"
        };

        // Parse command sequences (simplified)
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
        var sequence = new CommandSequence();
        
        if (sequenceNode != null)
        {
            var commandText = GetOptionalElement(sequenceNode, "command");
            if (!string.IsNullOrEmpty(commandText))
            {
                var command = new ProtocolCommand
                {
                    Command = commandText,
                    ExpectedResponse = GetOptionalElement(sequenceNode, "expected_response"),
                    TimeoutMs = int.Parse(GetOptionalElement(sequenceNode, "timeout_ms") ?? "2000")
                };
                sequence.Commands.Add(command);
            }
        }

        return sequence;
    }

    #region Helper Methods

    private string GetRequiredAttribute(XmlNode node, string attributeName)
    {
        var value = node.Attributes?[attributeName]?.Value;
        if (string.IsNullOrEmpty(value))
        {
            throw new InvalidOperationException($"Required attribute '{attributeName}' not found in {node.Name} element");
        }
        return value;
    }

    private string? GetOptionalAttribute(XmlNode node, string attributeName)
    {
        return node.Attributes?[attributeName]?.Value;
    }

    private string GetRequiredElement(XmlNode node, string elementName)
    {
        var element = node.SelectSingleNode(elementName);
        if (element == null || string.IsNullOrEmpty(element.InnerText))
        {
            throw new InvalidOperationException($"Required element '{elementName}' not found in {node.Name}");
        }
        return element.InnerText;
    }

    private string? GetOptionalElement(XmlNode node, string elementName)
    {
        return node.SelectSingleNode(elementName)?.InnerText;
    }

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
    }

    #endregion
    
}