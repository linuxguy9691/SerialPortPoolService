// SerialPortPool.Core/Services/XmlBibConfigurationLoader.cs - COMPLETE IMPLEMENTATION
using System.Xml;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using SerialPortPool.Core.Interfaces;
using SerialPortPool.Core.Models;

namespace SerialPortPool.Core.Services;

/// <summary>
/// XML BIB Configuration Loader - Complete Implementation
/// Parses BIB → UUT → PORT → PROTOCOL hierarchy from XML files
/// </summary>
public class XmlBibConfigurationLoader : IBibConfigurationLoader
{
    private readonly ILogger<XmlBibConfigurationLoader> _logger;
    private readonly IMemoryCache _cache;
    private string _defaultXmlPath = string.Empty;
    private readonly Dictionary<string, BibConfiguration> _loadedConfigurations = new();

    public XmlBibConfigurationLoader(
        ILogger<XmlBibConfigurationLoader> logger,
        IMemoryCache cache)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    }

    /// <summary>
    /// Set default XML configuration path
    /// </summary>
    public void SetDefaultConfigurationPath(string defaultXmlPath)
    {
        _defaultXmlPath = defaultXmlPath;
        _logger.LogDebug($"Default XML configuration path set: {defaultXmlPath}");
    }

    /// <summary>
    /// Load all BIB configurations from XML file
    /// </summary>
    public async Task<Dictionary<string, BibConfiguration>> LoadConfigurationsAsync(string xmlPath)
    {
        var cacheKey = $"xml_config_{xmlPath}_{GetFileHash(xmlPath)}";
        
        if (_cache.TryGetValue(cacheKey, out Dictionary<string, BibConfiguration>? cached) && cached != null)
        {
            _logger.LogDebug($"Loaded BIB configurations from cache: {xmlPath}");
            return cached;
        }

        try
        {
            _logger.LogInformation($"Loading BIB configurations from XML: {xmlPath}");

            if (!File.Exists(xmlPath))
            {
                throw new FileNotFoundException($"XML configuration file not found: {xmlPath}");
            }

            var configurations = new Dictionary<string, BibConfiguration>();
            
            // Load and parse XML
            var xmlDoc = new XmlDocument();
            xmlDoc.Load(xmlPath);

            var rootNode = xmlDoc.DocumentElement;
            if (rootNode?.Name != "root")
            {
                throw new InvalidOperationException("XML root element must be 'root'");
            }

            // Parse each BIB node
            var bibNodes = rootNode.SelectNodes("bib");
            if (bibNodes != null)
            {
                foreach (XmlNode bibNode in bibNodes)
                {
                    var bibConfig = ParseBibConfiguration(bibNode);
                    configurations[bibConfig.BibId] = bibConfig;
                    _logger.LogDebug($"Parsed BIB: {bibConfig.BibId} with {bibConfig.Uuts.Count} UUTs");
                }
            }

            // Cache the results
            var cacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30),
                Priority = CacheItemPriority.High
            };
            _cache.Set(cacheKey, configurations, cacheOptions);

            // Store in memory for single-parameter methods
            foreach (var config in configurations)
            {
                _loadedConfigurations[config.Key] = config.Value;
            }

            _logger.LogInformation($"Successfully loaded {configurations.Count} BIB configurations from {xmlPath}");
            return configurations;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to load BIB configurations from: {xmlPath}");
            throw;
        }
    }

    /// <summary>
    /// Load single BIB configuration by ID (using default path)
    /// </summary>
    public async Task<BibConfiguration?> LoadBibConfigurationAsync(string bibId)
    {
        // Try from loaded configurations first
        if (_loadedConfigurations.TryGetValue(bibId, out var cached))
        {
            _logger.LogDebug($"BIB configuration found in memory: {bibId}");
            return cached;
        }

        // Load from default path if available
        if (string.IsNullOrEmpty(_defaultXmlPath))
        {
            _logger.LogWarning($"No default XML path set and BIB {bibId} not in memory");
            return null;
        }

        try
        {
            var allConfigs = await LoadConfigurationsAsync(_defaultXmlPath);
            return allConfigs.TryGetValue(bibId, out var config) ? config : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to load BIB configuration {bibId} from default path");
            return null;
        }
    }

    /// <summary>
    /// Load single BIB configuration by ID from specific XML file
    /// </summary>
    public async Task<BibConfiguration?> LoadBibConfigurationAsync(string xmlPath, string bibId)
    {
        var allConfigs = await LoadConfigurationsAsync(xmlPath);
        return allConfigs.TryGetValue(bibId, out var config) ? config : null;
    }

    /// <summary>
    /// Get all loaded configurations from memory
    /// </summary>
    public async Task<Dictionary<string, BibConfiguration>> GetLoadedConfigurationsAsync()
    {
        await Task.CompletedTask;
        return new Dictionary<string, BibConfiguration>(_loadedConfigurations);
    }

    /// <summary>
    /// Clear all loaded configurations from memory
    /// </summary>
    public async Task ClearConfigurationsAsync()
    {
        await Task.CompletedTask;
        _loadedConfigurations.Clear();
        _logger.LogInformation("Cleared all loaded BIB configurations from memory");
    }

    /// <summary>
    /// Validate BIB configuration exists and is accessible
    /// </summary>
    public async Task<bool> ValidateBibConfigurationAsync(string bibId)
    {
        try
        {
            var config = await LoadBibConfigurationAsync(bibId);
            return config != null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, $"BIB configuration validation failed for {bibId}");
            return false;
        }
    }

    #region Private Parsing Methods

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

        // Parse metadata
        var metadataNode = bibNode.SelectSingleNode("metadata");
        if (metadataNode != null)
        {
            foreach (XmlNode child in metadataNode.ChildNodes)
            {
                if (child.NodeType == XmlNodeType.Element)
                {
                    bib.Metadata[child.Name] = child.InnerText;
                }
            }
        }

        // Parse UUTs
        var uutNodes = bibNode.SelectNodes("uut");
        if (uutNodes != null)
        {
            foreach (XmlNode uutNode in uutNodes)
            {
                var uut = ParseUutConfiguration(uutNode);
                uut.ParentBibId = bib.BibId;
                bib.Uuts.Add(uut);
            }
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

        // Parse metadata
        var metadataNode = uutNode.SelectSingleNode("metadata");
        if (metadataNode != null)
        {
            foreach (XmlNode child in metadataNode.ChildNodes)
            {
                if (child.NodeType == XmlNodeType.Element)
                {
                    uut.Metadata[child.Name] = child.InnerText;
                }
            }
        }

        // Parse ports
        var portNodes = uutNode.SelectNodes("port");
        if (portNodes != null)
        {
            foreach (XmlNode portNode in portNodes)
            {
                var port = ParsePortConfiguration(portNode);
                port.ParentUutId = uut.UutId;
                uut.Ports.Add(port);
            }
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

        // Parse protocol-specific settings
        var settingsToCheck = new[]
        {
            ("read_timeout", "3000"),
            ("write_timeout", "3000"), 
            ("handshake", "None"),
            ("rts_enable", "false"),
            ("dtr_enable", "true")
        };

        foreach (var (settingName, defaultValue) in settingsToCheck)
        {
            var value = GetOptionalElement(portNode, settingName) ?? defaultValue;
            if (int.TryParse(value, out var intValue))
            {
                port.Settings[settingName] = intValue;
            }
            else if (bool.TryParse(value, out var boolValue))
            {
                port.Settings[settingName] = boolValue;
            }
            else
            {
                port.Settings[settingName] = value;
            }
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
                    TimeoutMs = int.Parse(GetOptionalElement(sequenceNode, "timeout_ms") ?? "2000"),
                    RetryCount = int.Parse(GetOptionalElement(sequenceNode, "retry_count") ?? "0")
                };
                sequence.Commands.Add(command);
            }
        }

        return sequence;
    }

    /// <summary>
    /// Get required XML attribute
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
    /// Get optional XML attribute
    /// </summary>
    private string? GetOptionalAttribute(XmlNode node, string attributeName)
    {
        return node.Attributes?[attributeName]?.Value;
    }

    /// <summary>
    /// Get required XML element
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
    /// Get optional XML element
    /// </summary>
    private string? GetOptionalElement(XmlNode node, string elementName)
    {
        return node.SelectSingleNode(elementName)?.InnerText;
    }

    /// <summary>
    /// Get file hash for caching
    /// </summary>
    private string GetFileHash(string filePath)
    {
        try
        {
            if (File.Exists(filePath))
            {
                var lastWrite = File.GetLastWriteTime(filePath);
                return lastWrite.Ticks.ToString();
            }
        }
        catch
        {
            // Ignore errors, return default
        }
        return DateTime.Now.Ticks.ToString();
    }

    #endregion
}