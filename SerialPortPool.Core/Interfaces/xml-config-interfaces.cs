// ===================================================================
// SPRINT 6 - XML CONFIGURATION LOADER INTERFACE
// File: SerialPortPool.Core/Interfaces/IXmlConfigurationLoader.cs
// ===================================================================

using SerialPortPool.Core.Models;

namespace SerialPortPool.Core.Interfaces;

/// <summary>
/// Sprint 6 - XML Configuration Loader Interface
/// Provides direct XML configuration loading capabilities
/// Complements IBibConfigurationLoader with system-wide configuration support
/// </summary>
public interface IXmlConfigurationLoader
{
    /// <summary>
    /// Load complete system configuration from XML file
    /// </summary>
    /// <param name="xmlPath">Path to XML configuration file</param>
    /// <returns>Complete system configuration with all BIBs</returns>
    Task<SystemConfiguration> LoadConfigurationAsync(string xmlPath);
    
    /// <summary>
    /// Load all BIB configurations from XML file as dictionary
    /// </summary>
    /// <param name="xmlPath">Path to XML configuration file</param>
    /// <returns>Dictionary of BIB ID to BIB configuration</returns>
    Task<Dictionary<string, BibConfiguration>> LoadAllBibsAsync(string xmlPath);
    
    /// <summary>
    /// Load specific BIB configuration from XML file
    /// </summary>
    /// <param name="xmlPath">Path to XML configuration file</param>
    /// <param name="bibId">BIB identifier to load</param>
    /// <returns>Specific BIB configuration</returns>
    Task<BibConfiguration> LoadBibAsync(string xmlPath, string bibId);
    
    /// <summary>
    /// Validate XML configuration file against schema and business rules
    /// </summary>
    /// <param name="xmlPath">Path to XML configuration file</param>
    /// <returns>True if configuration is valid</returns>
    Task<ConfigurationValidationResult> ValidateConfigurationAsync(string xmlPath);
    
    /// <summary>
    /// Get all supported protocols from configuration
    /// </summary>
    /// <param name="xmlPath">Path to XML configuration file</param>
    /// <returns>List of unique protocols used in configuration</returns>
    Task<IEnumerable<string>> GetSupportedProtocolsAsync(string xmlPath);
}

// ===================================================================
// SPRINT 6 - XML CONFIGURATION LOADER IMPLEMENTATION
// File: SerialPortPool.Core/Services/XmlConfigurationLoader.cs
// ===================================================================

using System.Xml;
using Microsoft.Extensions.Logging;
using SerialPortPool.Core.Interfaces;
using SerialPortPool.Core.Models;

namespace SerialPortPool.Core.Services;

/// <summary>
/// Sprint 6 - XML Configuration Loader Implementation
/// Provides comprehensive XML configuration loading for system-wide configurations
/// Works alongside XmlBibConfigurationLoader for complete configuration support
/// </summary>
public class XmlConfigurationLoader : IXmlConfigurationLoader
{
    private readonly ILogger<XmlConfigurationLoader> _logger;
    private readonly IConfigurationValidator _validator;

    public XmlConfigurationLoader(
        ILogger<XmlConfigurationLoader> logger,
        IConfigurationValidator validator)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
    }

    /// <summary>
    /// Load complete system configuration from XML file
    /// </summary>
    public async Task<SystemConfiguration> LoadConfigurationAsync(string xmlPath)
    {
        try
        {
            _logger.LogInformation("🔄 Loading system configuration from: {XmlPath}", xmlPath);

            if (!File.Exists(xmlPath))
            {
                throw new FileNotFoundException($"XML configuration file not found: {xmlPath}");
            }

            var xmlDoc = new XmlDocument();
            xmlDoc.Load(xmlPath);

            var systemConfig = new SystemConfiguration
            {
                SystemName = "SerialPortPool",
                Version = "Sprint6",
                LoadedAt = DateTime.Now,
                SourcePath = xmlPath,
                ConfigurationSource = "XML"
            };

            // Load all BIBs
            var bibNodes = xmlDoc.SelectNodes("//bib");
            if (bibNodes != null)
            {
                foreach (XmlNode bibNode in bibNodes)
                {
                    var bib = await ParseBibConfigurationAsync(bibNode);
                    systemConfig.Bibs.Add(bib);
                }
            }

            _logger.LogInformation("✅ System configuration loaded: {BibCount} BIBs, {TotalPorts} total ports", 
                systemConfig.Bibs.Count, systemConfig.Bibs.Sum(b => b.TotalPortCount));

            return systemConfig;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to load system configuration from: {XmlPath}", xmlPath);
            throw;
        }
    }

    /// <summary>
    /// Load all BIB configurations from XML file as dictionary
    /// </summary>
    public async Task<Dictionary<string, BibConfiguration>> LoadAllBibsAsync(string xmlPath)
    {
        var systemConfig = await LoadConfigurationAsync(xmlPath);
        return systemConfig.Bibs.ToDictionary(b => b.BibId, b => b);
    }

    /// <summary>
    /// Load specific BIB configuration from XML file
    /// </summary>
    public async Task<BibConfiguration> LoadBibAsync(string xmlPath, string bibId)
    {
        try
        {
            _logger.LogDebug("🔍 Loading BIB {BibId} from: {XmlPath}", bibId, xmlPath);

            if (!File.Exists(xmlPath))
            {
                throw new FileNotFoundException($"XML configuration file not found: {xmlPath}");
            }

            var xmlDoc = new XmlDocument();
            xmlDoc.Load(xmlPath);

            var bibNode = xmlDoc.SelectSingleNode($"//bib[@id='{bibId}']");
            if (bibNode == null)
            {
                throw new ArgumentException($"BIB '{bibId}' not found in configuration file: {xmlPath}");
            }

            var bib = await ParseBibConfigurationAsync(bibNode);
            
            _logger.LogInformation("✅ BIB loaded: {BibId} with {UutCount} UUTs, {PortCount} ports", 
                bib.BibId, bib.Uuts.Count, bib.TotalPortCount);

            return bib;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to load BIB {BibId} from: {XmlPath}", bibId, xmlPath);
            throw;
        }
    }

    /// <summary>
    /// Validate XML configuration file
    /// </summary>
    public async Task<ConfigurationValidationResult> ValidateConfigurationAsync(string xmlPath)
    {
        try
        {
            var systemConfig = await LoadConfigurationAsync(xmlPath);
            return _validator.ValidateConfiguration(systemConfig);
        }
        catch (Exception ex)
        {
            var result = new ConfigurationValidationResult();
            result.AddError($"Configuration loading failed: {ex.Message}");
            return result;
        }
    }

    /// <summary>
    /// Get all supported protocols from configuration
    /// </summary>
    public async Task<IEnumerable<string>> GetSupportedProtocolsAsync(string xmlPath)
    {
        var systemConfig = await LoadConfigurationAsync(xmlPath);
        return systemConfig.GetAllProtocols();
    }

    #region Private Parsing Methods

    /// <summary>
    /// Parse BIB configuration from XML node
    /// </summary>
    private async Task<BibConfiguration> ParseBibConfigurationAsync(XmlNode bibNode)
    {
        var bib = new BibConfiguration
        {
            BibId = GetRequiredAttribute(bibNode, "id"),
            Description = GetOptionalAttribute(bibNode, "description") ?? "",
            CreatedAt = DateTime.Now
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
                var uut = await ParseUutConfigurationAsync(uutNode);
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
    private async Task<UutConfiguration> ParseUutConfigurationAsync(XmlNode uutNode)
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
                var port = await ParsePortConfigurationAsync(portNode);
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
    private async Task<PortConfiguration> ParsePortConfigurationAsync(XmlNode portNode)
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
        port.StartCommands = await ParseCommandSequenceAsync(portNode.SelectSingleNode("start"));
        port.TestCommands = await ParseCommandSequenceAsync(portNode.SelectSingleNode("test"));
        port.StopCommands = await ParseCommandSequenceAsync(portNode.SelectSingleNode("stop"));

        return port;
    }

    /// <summary>
    /// Parse command sequence from XML node
    /// </summary>
    private async Task<CommandSequence> ParseCommandSequenceAsync(XmlNode? sequenceNode)
    {
        await Task.CompletedTask; // Make async for consistency
        
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
                    TimeoutMs = int.Parse(GetOptionalElement(sequenceNode, "timeout_ms") ?? "3000"),
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

    #endregion
}