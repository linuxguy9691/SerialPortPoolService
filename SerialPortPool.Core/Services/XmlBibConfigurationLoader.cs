// SerialPortPool.Core/Services/XmlBibConfigurationLoader.cs - NEW Week 2
using System.Xml;
using Microsoft.Extensions.Logging;
using SerialPortPool.Core.Interfaces;
using SerialPortPool.Core.Models;

namespace SerialPortPool.Core.Services;

/// <summary>
/// XML configuration loader for BIB configurations
/// Week 2: Focus on RS232 protocol with hierarchical BIB→UUT→PORT structure
/// </summary>
public class XmlBibConfigurationLoader : IBibConfigurationLoader
{
    private readonly ILogger<XmlBibConfigurationLoader> _logger;
    private readonly Dictionary<string, BibConfiguration> _loadedConfigurations;

    public XmlBibConfigurationLoader(ILogger<XmlBibConfigurationLoader> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _loadedConfigurations = new Dictionary<string, BibConfiguration>();
    }

    /// <summary>
    /// Load all BIB configurations from XML file
    /// </summary>
    public async Task<Dictionary<string, BibConfiguration>> LoadConfigurationsAsync(string xmlPath)
    {
        if (string.IsNullOrEmpty(xmlPath))
            throw new ArgumentException("XML path cannot be null or empty", nameof(xmlPath));

        if (!File.Exists(xmlPath))
            throw new FileNotFoundException($"Configuration file not found: {xmlPath}");

        try
        {
            _logger.LogInformation($"📄 Loading BIB configurations from: {xmlPath}");

            var xmlDoc = new XmlDocument();
            await Task.Run(() => xmlDoc.Load(xmlPath));

            // Validate XML structure
            ValidateXmlStructure(xmlDoc);

            // Parse configurations
            var configurations = ParseConfigurations(xmlDoc);

            // Cache loaded configurations
            _loadedConfigurations.Clear();
            foreach (var config in configurations)
            {
                _loadedConfigurations[config.Key] = config.Value;
            }

            _logger.LogInformation($"✅ Successfully loaded {configurations.Count} BIB configuration(s)");
            LogConfigurationSummary(configurations);

            return configurations;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"❌ Failed to load BIB configurations from {xmlPath}");
            throw;
        }
    }

    /// <summary>
    /// Load single BIB configuration by ID
    /// </summary>
    public async Task<BibConfiguration?> LoadBibConfigurationAsync(string bibId)
    {
        if (string.IsNullOrEmpty(bibId))
            return null;

        try
        {
            if (_loadedConfigurations.TryGetValue(bibId, out var cachedConfig))
            {
                _logger.LogDebug($"📄 Returning cached configuration for BIB: {bibId}");
                return cachedConfig;
            }

            _logger.LogWarning($"⚠️ BIB configuration not found: {bibId}");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"❌ Error loading BIB configuration: {bibId}");
            return null;
        }
    }

    /// <summary>
    /// Validate XML document structure
    /// </summary>
    private void ValidateXmlStructure(XmlDocument xmlDoc)
    {
        var root = xmlDoc.DocumentElement;
        if (root == null || root.Name != "root")
        {
            throw new InvalidOperationException("XML must have 'root' as the document element");
        }

        var bibNodes = root.SelectNodes("bib");
        if (bibNodes == null || bibNodes.Count == 0)
        {
            throw new InvalidOperationException("XML must contain at least one 'bib' element");
        }

        _logger.LogDebug($"📄 XML validation passed: {bibNodes.Count} BIB(s) found");
    }

    /// <summary>
    /// Parse all BIB configurations from XML document
    /// </summary>
    private Dictionary<string, BibConfiguration> ParseConfigurations(XmlDocument xmlDoc)
    {
        var configurations = new Dictionary<string, BibConfiguration>();
        var bibNodes = xmlDoc.DocumentElement!.SelectNodes("bib")!;

        foreach (XmlNode bibNode in bibNodes)
        {
            try
            {
                var bibConfig = ParseBibConfiguration(bibNode);
                
                if (configurations.ContainsKey(bibConfig.BibId))
                {
                    throw new InvalidOperationException($"Duplicate BIB ID found: {bibConfig.BibId}");
                }

                configurations[bibConfig.BibId] = bibConfig;
                _logger.LogDebug($"📄 Parsed BIB configuration: {bibConfig.BibId}");
            }
            catch (Exception ex)
            {
                var bibId = bibNode.Attributes?["id"]?.Value ?? "Unknown";
                _logger.LogError(ex, $"❌ Error parsing BIB configuration: {bibId}");
                throw;
            }
        }

        return configurations;
    }

    /// <summary>
    /// Parse individual BIB configuration
    /// </summary>
    private BibConfiguration ParseBibConfiguration(XmlNode bibNode)
    {
        var bibId = GetRequiredAttribute(bibNode, "id");
        
        var bibConfig = new BibConfiguration
        {
            BibId = bibId,
            Description = GetOptionalAttribute(bibNode, "description") ?? $"BIB {bibId}",
            CreatedAt = DateTime.Now
        };

        // Parse metadata
        ParseBibMetadata(bibNode, bibConfig);

        // Parse UUTs
        var uutNodes = bibNode.SelectNodes("uut");
        if (uutNodes != null)
        {
            foreach (XmlNode uutNode in uutNodes)
            {
                var uutConfig = ParseUutConfiguration(uutNode, bibId);
                bibConfig.Uuts.Add(uutConfig);
            }
        }

        if (!bibConfig.Uuts.Any())
        {
            throw new InvalidOperationException($"BIB {bibId} must contain at least one UUT");
        }

        return bibConfig;
    }

    /// <summary>
    /// Parse UUT configuration
    /// </summary>
    private UutConfiguration ParseUutConfiguration(XmlNode uutNode, string parentBibId)
    {
        var uutId = GetRequiredAttribute(uutNode, "id");

        var uutConfig = new UutConfiguration
        {
            UutId = uutId,
            Description = GetOptionalAttribute(uutNode, "description") ?? $"UUT {uutId}",
            ParentBibId = parentBibId
        };

        // Parse metadata
        ParseUutMetadata(uutNode, uutConfig);

        // Parse ports
        var portNodes = uutNode.SelectNodes("port");
        if (portNodes != null)
        {
            foreach (XmlNode portNode in portNodes)
            {
                var portConfig = ParsePortConfiguration(portNode, parentBibId, uutId);
                uutConfig.Ports.Add(portConfig);
            }
        }

        if (!uutConfig.Ports.Any())
        {
            throw new InvalidOperationException($"UUT {uutId} must contain at least one port");
        }

        // Validate unique port numbers
        var portNumbers = uutConfig.Ports.Select(p => p.PortNumber).ToList();
        if (portNumbers.Count != portNumbers.Distinct().Count())
        {
            throw new InvalidOperationException($"UUT {uutId} contains duplicate port numbers");
        }

        return uutConfig;
    }

    /// <summary>
    /// Parse port configuration (Week 2: RS232 focus)
    /// </summary>
    private PortConfiguration ParsePortConfiguration(XmlNode portNode, string parentBibId, string parentUutId)
    {
        var portNumber = GetRequiredIntAttribute(portNode, "number");
        
        var portConfig = new PortConfiguration
        {
            PortNumber = portNumber,
            ParentBibId = parentBibId,
            ParentUutId = parentUutId
        };

        // Parse required protocol elements
        portConfig.Protocol = GetRequiredChildText(portNode, "protocol");
        portConfig.Speed = GetOptionalChildInt(portNode, "speed") ?? 115200;
        portConfig.DataPattern = GetOptionalChildText(portNode, "data_pattern") ?? "n81";

        // Week 2: Validate RS232 protocol
        if (!portConfig.Protocol.Equals("rs232", StringComparison.OrdinalIgnoreCase))
        {
            throw new NotSupportedException($"Week 2: Only RS232 protocol supported, found: {portConfig.Protocol}");
        }

        // Parse protocol-specific settings
        ParseProtocolSettings(portNode, portConfig);

        // Parse 3-phase command sequences
        portConfig.StartCommands = ParseCommandSequence(portNode, "start");
        portConfig.TestCommands = ParseCommandSequence(portNode, "test");
        portConfig.StopCommands = ParseCommandSequence(portNode, "stop");

        // Validate configuration
        var validationResult = portConfig.Validate();
        if (!validationResult.IsValid)
        {
            throw new InvalidOperationException($"Port configuration validation failed: {validationResult.GetSummary()}");
        }

        if (validationResult.HasWarnings)
        {
            foreach (var warning in validationResult.Warnings)
            {
                _logger.LogWarning($"⚠️ Port {portConfig.FullPortId}: {warning}");
            }
        }

        return portConfig;
    }

    /// <summary>
    /// Parse protocol-specific settings (Week 2: RS232 focus)
    /// </summary>
    private void ParseProtocolSettings(XmlNode portNode, PortConfiguration portConfig)
    {
        // RS232-specific settings
        if (portConfig.Protocol.Equals("rs232", StringComparison.OrdinalIgnoreCase))
        {
            portConfig.Settings["read_timeout"] = GetOptionalChildInt(portNode, "read_timeout") ?? 2000;
            portConfig.Settings["write_timeout"] = GetOptionalChildInt(portNode, "write_timeout") ?? 2000;
            portConfig.Settings["handshake"] = GetOptionalChildText(portNode, "handshake") ?? "None";
            portConfig.Settings["rts_enable"] = GetOptionalChildBool(portNode, "rts_enable") ?? false;
            portConfig.Settings["dtr_enable"] = GetOptionalChildBool(portNode, "dtr_enable") ?? false;
        }
    }

    /// <summary>
    /// Parse command sequence (start/test/stop)
    /// </summary>
    private CommandSequence ParseCommandSequence(XmlNode portNode, string sequenceName)
    {
        var sequenceNode = portNode.SelectSingleNode(sequenceName);
        if (sequenceNode == null)
        {
            throw new InvalidOperationException($"Missing required command sequence: {sequenceName}");
        }

        var sequence = new CommandSequence();
        
        // Single command format
        var commandText = GetOptionalChildText(sequenceNode, "command");
        if (!string.IsNullOrEmpty(commandText))
        {
            var expectedResponse = GetOptionalChildText(sequenceNode, "expected_response");
            var timeoutMs = GetOptionalChildInt(sequenceNode, "timeout_ms") ?? 2000;
            var retryCount = GetOptionalChildInt(sequenceNode, "retry_count") ?? 0;

            var command = new ProtocolCommand
            {
                Command = commandText,
                ExpectedResponse = expectedResponse,
                TimeoutMs = timeoutMs,
                RetryCount = retryCount
            };

            sequence.Commands.Add(command);
        }
        else
        {
            // Multiple commands format (future expansion)
            var commandNodes = sequenceNode.SelectNodes("cmd");
            if (commandNodes != null)
            {
                foreach (XmlNode cmdNode in commandNodes)
                {
                    var command = new ProtocolCommand
                    {
                        Command = GetRequiredAttribute(cmdNode, "text"),
                        ExpectedResponse = GetOptionalAttribute(cmdNode, "expected"),
                        TimeoutMs = GetOptionalIntAttribute(cmdNode, "timeout") ?? 2000,
                        RetryCount = GetOptionalIntAttribute(cmdNode, "retry") ?? 0
                    };
                    
                    sequence.Commands.Add(command);
                }
            }
        }

        if (!sequence.Commands.Any())
        {
            throw new InvalidOperationException($"Command sequence '{sequenceName}' must contain at least one command");
        }

        // Set sequence-level properties
        sequence.SequenceTimeoutMs = GetOptionalChildInt(sequenceNode, "sequence_timeout") ?? 
                                   sequence.Commands.Sum(c => c.TimeoutMs) + 5000;
        sequence.ContinueOnFailure = GetOptionalChildBool(sequenceNode, "continue_on_failure") ?? false;

        return sequence;
    }

    #region XML Helper Methods

    private static string GetRequiredAttribute(XmlNode node, string attributeName)
    {
        var value = node.Attributes?[attributeName]?.Value;
        if (string.IsNullOrEmpty(value))
        {
            throw new InvalidOperationException($"Required attribute '{attributeName}' not found or empty");
        }
        return value;
    }

    private static string? GetOptionalAttribute(XmlNode node, string attributeName)
    {
        return node.Attributes?[attributeName]?.Value;
    }

    private static int GetRequiredIntAttribute(XmlNode node, string attributeName)
    {
        var value = GetRequiredAttribute(node, attributeName);
        if (!int.TryParse(value, out var intValue))
        {
            throw new InvalidOperationException($"Attribute '{attributeName}' must be a valid integer");
        }
        return intValue;
    }

    private static int? GetOptionalIntAttribute(XmlNode node, string attributeName)
    {
        var value = GetOptionalAttribute(node, attributeName);
        if (string.IsNullOrEmpty(value))
            return null;
        
        if (!int.TryParse(value, out var intValue))
        {
            throw new InvalidOperationException($"Attribute '{attributeName}' must be a valid integer");
        }
        return intValue;
    }

    private static string GetRequiredChildText(XmlNode node, string childName)
    {
        var child = node.SelectSingleNode(childName);
        var value = child?.InnerText;
        if (string.IsNullOrEmpty(value))
        {
            throw new InvalidOperationException($"Required child element '{childName}' not found or empty");
        }
        return value;
    }

    private static string? GetOptionalChildText(XmlNode node, string childName)
    {
        var child = node.SelectSingleNode(childName);
        return child?.InnerText;
    }

    private static int? GetOptionalChildInt(XmlNode node, string childName)
    {
        var value = GetOptionalChildText(node, childName);
        if (string.IsNullOrEmpty(value))
            return null;
        
        if (!int.TryParse(value, out var intValue))
        {
            throw new InvalidOperationException($"Child element '{childName}' must be a valid integer");
        }
        return intValue;
    }

    private static bool? GetOptionalChildBool(XmlNode node, string childName)
    {
        var value = GetOptionalChildText(node, childName);
        if (string.IsNullOrEmpty(value))
            return null;
        
        if (!bool.TryParse(value, out var boolValue))
        {
            // Support common string representations
            return value.ToLowerInvariant() switch
            {
                "yes" or "1" or "on" or "enabled" => true,
                "no" or "0" or "off" or "disabled" => false,
                _ => throw new InvalidOperationException($"Child element '{childName}' must be a valid boolean")
            };
        }
        return boolValue;
    }

    #endregion

    #region Metadata Parsing

    private static void ParseBibMetadata(XmlNode bibNode, BibConfiguration bibConfig)
    {
        var metadataNode = bibNode.SelectSingleNode("metadata");
        if (metadataNode != null)
        {
            foreach (XmlNode childNode in metadataNode.ChildNodes)
            {
                if (childNode.NodeType == XmlNodeType.Element)
                {
                    bibConfig.Metadata[childNode.Name] = childNode.InnerText;
                }
            }
        }
    }

    private static void ParseUutMetadata(XmlNode uutNode, UutConfiguration uutConfig)
    {
        var metadataNode = uutNode.SelectSingleNode("metadata");
        if (metadataNode != null)
        {
            foreach (XmlNode childNode in metadataNode.ChildNodes)
            {
                if (childNode.NodeType == XmlNodeType.Element)
                {
                    uutConfig.Metadata[childNode.Name] = childNode.InnerText;
                }
            }
        }
    }

    #endregion

    #region Logging

    private void LogConfigurationSummary(Dictionary<string, BibConfiguration> configurations)
    {
        _logger.LogInformation("📊 Configuration Summary:");
        
        foreach (var config in configurations.Values)
        {
            _logger.LogInformation($"  📦 BIB {config.BibId}: {config.Uuts.Count} UUT(s), {config.TotalPortCount} port(s)");
            
            foreach (var uut in config.Uuts)
            {
                _logger.LogDebug($"    🔧 UUT {uut.UutId}: {uut.Ports.Count} port(s)");
                
                foreach (var port in uut.Ports)
                {
                    _logger.LogDebug($"      📍 Port {port.PortNumber}: {port.Protocol.ToUpper()} @ {port.Speed} ({port.DataPattern})");
                }
            }
        }

        // Protocol statistics
        var allPorts = configurations.Values.SelectMany(c => c.GetAllPorts()).ToList();
        var rs232Count = allPorts.Count(p => p.Protocol.Equals("rs232", StringComparison.OrdinalIgnoreCase));
        
        _logger.LogInformation($"📊 Protocol Distribution: RS232: {rs232Count}/{allPorts.Count} ports");
    }

    #endregion

    /// <summary>
    /// Get all loaded configurations
    /// </summary>
    public async Task<Dictionary<string, BibConfiguration>> GetLoadedConfigurationsAsync()
    {
        await Task.CompletedTask;
        return new Dictionary<string, BibConfiguration>(_loadedConfigurations);
    }

    /// <summary>
    /// Clear loaded configurations
    /// </summary>
    public async Task ClearConfigurationsAsync()
    {
        await Task.CompletedTask;
        _loadedConfigurations.Clear();
        _logger.LogInformation("🗑️ All loaded configurations cleared");
    }
}

/// <summary>
/// Interface for BIB configuration loading
/// </summary>
public interface IBibConfigurationLoader
{
    Task<Dictionary<string, BibConfiguration>> LoadConfigurationsAsync(string xmlPath);
    Task<BibConfiguration?> LoadBibConfigurationAsync(string bibId);
    Task<Dictionary<string, BibConfiguration>> GetLoadedConfigurationsAsync();
    Task ClearConfigurationsAsync();
}