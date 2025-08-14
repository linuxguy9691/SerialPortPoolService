// ===================================================================
// COMPLETE: XmlConfigurationLoader.cs - Sprint 6 + Sprint 8 Regex
// File: SerialPortPool.Core/Services/XmlConfigurationLoader.cs
// Purpose: Complete implementation with Sprint 8 regex enhancement
// ===================================================================

using System.Text.RegularExpressions;
using System.Xml;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;
using SerialPortPool.Core.Interfaces;
using SerialPortPool.Core.Models;

namespace SerialPortPool.Core.Services;

/// <summary>
/// Sprint 6 + Sprint 8: XML Configuration Loader with Regex Support
/// ENHANCED: Now supports regex="true" attribute in expected_response elements
/// BACKWARD COMPATIBLE: Existing XML files without regex continue to work
/// </summary>
public class XmlConfigurationLoader : IXmlConfigurationLoader
{
    private readonly ILogger<XmlConfigurationLoader> _logger;
    private readonly IMemoryCache _cache;

    public XmlConfigurationLoader(
        ILogger<XmlConfigurationLoader> logger,
        IMemoryCache cache)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    }

    /// <summary>
    /// Load complete system configuration from XML file
    /// </summary>
    public async Task<SystemConfiguration> LoadConfigurationAsync(string xmlPath)
    {
        try
        {
            _logger.LogInformation($"📄 Loading system configuration from: {xmlPath}");

            if (!File.Exists(xmlPath))
            {
                throw new FileNotFoundException($"Configuration file not found: {xmlPath}");
            }

            var xmlDoc = new XmlDocument();
            xmlDoc.Load(xmlPath);

            var config = new SystemConfiguration
            {
                SystemName = "SerialPortPool",
                Version = "1.0.0-Sprint8",
                SourcePath = xmlPath,
                LoadedAt = DateTime.Now
            };

            // Parse all BIBs
            var bibNodes = xmlDoc.SelectNodes("//bib");
            if (bibNodes != null)
            {
                foreach (XmlNode bibNode in bibNodes)
                {
                    var bibConfig = ParseBibConfiguration(bibNode);
                    config.Bibs.Add(bibConfig);
                }
            }

            _logger.LogInformation($"✅ Loaded {config.Bibs.Count} BIB configurations (Sprint 8 regex support enabled)");
            return config;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"❌ Failed to load configuration from {xmlPath}");
            throw;
        }
    }

    /// <summary>
    /// Load specific BIB configuration from XML file
    /// CRITICAL for Sprint 6: configLoader.LoadBibAsync(xmlPath, bibId)
    /// </summary>
    public async Task<BibConfiguration> LoadBibAsync(string xmlPath, string bibId)
    {
        try
        {
            _logger.LogDebug($"🔍 Loading BIB '{bibId}' from: {xmlPath}");

            if (!File.Exists(xmlPath))
            {
                throw new FileNotFoundException($"Configuration file not found: {xmlPath}");
            }

            var xmlDoc = new XmlDocument();
            await Task.Run(() => xmlDoc.Load(xmlPath));

            var bibNode = xmlDoc.SelectSingleNode($"//bib[@id='{bibId}']");
            if (bibNode == null)
            {
                throw new ArgumentException($"BIB '{bibId}' not found in {xmlPath}");
            }

            var bibConfig = ParseBibConfiguration(bibNode);
            
            _logger.LogInformation($"✅ BIB loaded: {bibConfig.BibId} - {bibConfig.Uuts.Count} UUT(s), {bibConfig.TotalPortCount} port(s)");
            return bibConfig;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"❌ Failed to load BIB '{bibId}' from {xmlPath}");
            throw;
        }
    }

    /// <summary>
    /// Load all BIB configurations from XML as dictionary
    /// </summary>
    public async Task<Dictionary<string, BibConfiguration>> LoadAllBibsAsync(string xmlPath)
    {
        var systemConfig = await LoadConfigurationAsync(xmlPath);
        return systemConfig.Bibs.ToDictionary(b => b.BibId);
    }

    /// <summary>
    /// Basic XML validation (Sprint 6 minimal implementation)
    /// </summary>
    public async Task<ConfigurationValidationResult> ValidateConfigurationAsync(string xmlPath)
    {
        var result = new ConfigurationValidationResult();

        try
        {
            if (!File.Exists(xmlPath))
            {
                result.AddError($"Configuration file not found: {xmlPath}");
                return result;
            }

            // Basic XML structure validation
            var xmlDoc = new XmlDocument();
            await Task.Run(() => xmlDoc.Load(xmlPath));

            if (xmlDoc.DocumentElement?.Name != "root")
            {
                result.AddError("XML root element must be 'root'");
            }

            var bibNodes = xmlDoc.SelectNodes("//bib");
            if (bibNodes == null || bibNodes.Count == 0)
            {
                result.AddError("Configuration must contain at least one BIB");
            }

            result.IsValid = !result.Errors.Any();
            _logger.LogDebug($"Configuration validation: {result.GetSummary()}");
        }
        catch (Exception ex)
        {
            result.AddError($"Validation error: {ex.Message}");
            _logger.LogError(ex, $"Configuration validation failed: {xmlPath}");
        }

        return result;
    }

    /// <summary>
    /// Get supported protocols from configuration
    /// </summary>
    public async Task<IEnumerable<string>> GetSupportedProtocolsAsync(string xmlPath)
    {
        try
        {
            var systemConfig = await LoadConfigurationAsync(xmlPath);
            return systemConfig.GetAllProtocols();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to get supported protocols from {xmlPath}");
            return Enumerable.Empty<string>();
        }
    }

    /// <summary>
    /// Find port configurations matching criteria (Sprint 6 basic implementation)
    /// </summary>
    public async Task<IEnumerable<PortConfiguration>> FindPortConfigurationsAsync(string xmlPath, ConfigurationSearchCriteria criteria)
    {
        try
        {
            var systemConfig = await LoadConfigurationAsync(xmlPath);
            var allPorts = systemConfig.Bibs.SelectMany(b => b.GetAllPorts());

            var filtered = allPorts.AsEnumerable();

            if (!string.IsNullOrEmpty(criteria.Protocol))
            {
                filtered = filtered.Where(p => p.Protocol.Equals(criteria.Protocol, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrEmpty(criteria.BibId))
            {
                filtered = filtered.Where(p => p.ParentBibId.Equals(criteria.BibId, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrEmpty(criteria.UutId))
            {
                filtered = filtered.Where(p => p.ParentUutId.Equals(criteria.UutId, StringComparison.OrdinalIgnoreCase));
            }

            if (criteria.PortNumber.HasValue)
            {
                filtered = filtered.Where(p => p.PortNumber == criteria.PortNumber.Value);
            }

            return filtered.Take(criteria.MaxResults).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to find port configurations in {xmlPath}");
            return Enumerable.Empty<PortConfiguration>();
        }
    }

    #region Private Parsing Methods

    /// <summary>
    /// Parse BIB configuration from XML node (Sprint 6 implementation)
    /// </summary>
    private BibConfiguration ParseBibConfiguration(XmlNode bibNode)
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
                port.ParentBibId = uut.ParentBibId;
                port.ParentUutId = uut.UutId;
                uut.Ports.Add(port);
            }
        }

        return uut;
    }

    /// <summary>
    /// Parse port configuration from XML node (Sprint 6 - RS232 focus)
    /// </summary>
    private PortConfiguration ParsePortConfiguration(XmlNode portNode)
    {
        var port = new PortConfiguration
        {
            PortNumber = int.Parse(GetRequiredAttribute(portNode, "number")),
            Protocol = GetOptionalElement(portNode, "protocol") ?? "rs232",
            Speed = int.Parse(GetOptionalElement(portNode, "speed") ?? "115200"),
            DataPattern = GetOptionalElement(portNode, "data_pattern") ?? "n81"
        };

        // Parse RS232-specific settings
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

        // Parse command sequences (3-phase workflow) with Sprint 8 regex support
        port.StartCommands = ParseCommandSequence(portNode.SelectSingleNode("start"));
        port.TestCommands = ParseCommandSequence(portNode.SelectSingleNode("test"));
        port.StopCommands = ParseCommandSequence(portNode.SelectSingleNode("stop"));

        return port;
    }

    /// <summary>
    /// SPRINT 8 ENHANCED: Parse command sequence with regex support
    /// BACKWARD COMPATIBLE: Existing XML files without regex="true" work unchanged
    /// NEW FEATURE: Supports regex="true" attribute for advanced pattern matching
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
                    TimeoutMs = int.Parse(GetOptionalElement(sequenceNode, "timeout_ms") ?? "2000"),
                    RetryCount = int.Parse(GetOptionalElement(sequenceNode, "retry_count") ?? "0")
                };

                // ✨ SPRINT 8 NEW: Enhanced response parsing with regex support
                var responseNode = sequenceNode.SelectSingleNode("expected_response");
                if (responseNode != null)
                {
                    command.ExpectedResponse = responseNode.InnerText;
                    
                    // Check for regex attribute
                    var regexAttr = responseNode.Attributes?["regex"]?.Value;
                    command.IsRegexPattern = bool.Parse(regexAttr ?? "false");
                    
                    if (command.IsRegexPattern)
                    {
                        _logger.LogDebug("📊 Regex pattern detected in XML: {Pattern}", command.ExpectedResponse);
                        
                        // Parse regex options if specified
                        var optionsAttr = responseNode.Attributes?["options"]?.Value;
                        if (!string.IsNullOrEmpty(optionsAttr))
                        {
                            command.RegexOptions = ParseRegexOptions(optionsAttr);
                            _logger.LogDebug("📊 Regex options parsed: {Options}", command.RegexOptions);
                        }
                        
                        // Validate regex pattern at load time
                        try
                        {
                            _ = new Regex(command.ExpectedResponse, command.RegexOptions);
                            _logger.LogDebug("✅ Regex pattern validated successfully: {Pattern}", command.ExpectedResponse);
                        }
                        catch (ArgumentException ex)
                        {
                            _logger.LogError("❌ Invalid regex pattern in XML: {Pattern} - {Error}", 
                                command.ExpectedResponse, ex.Message);
                            command.RegexValidationError = ex.Message;
                            
                            // Add to command metadata for troubleshooting
                            command.Metadata["RegexError"] = ex.Message;
                            command.Metadata["InvalidRegexPattern"] = command.ExpectedResponse;
                        }
                    }
                    else
                    {
                        _logger.LogDebug("📝 String pattern detected in XML: {Pattern}", command.ExpectedResponse);
                    }
                }

                sequence.Commands.Add(command);
            }
        }

        return sequence;
    }

    /// <summary>
    /// SPRINT 8: Parse regex options from XML attribute string
    /// Supports: IgnoreCase, Multiline, Singleline, ExplicitCapture, Compiled, IgnorePatternWhitespace
    /// </summary>
    private RegexOptions ParseRegexOptions(string optionsString)
    {
        var options = RegexOptions.None;
        
        if (string.IsNullOrWhiteSpace(optionsString))
            return options;

        var optionNames = optionsString.Split(',', StringSplitOptions.RemoveEmptyEntries);
        
        foreach (var optionName in optionNames)
        {
            var trimmed = optionName.Trim();
            
            switch (trimmed.ToLowerInvariant())
            {
                case "ignorecase":
                case "i":
                    options |= RegexOptions.IgnoreCase;
                    break;
                    
                case "multiline":
                case "m":
                    options |= RegexOptions.Multiline;
                    break;
                    
                case "singleline":
                case "s":
                    options |= RegexOptions.Singleline;
                    break;
                    
                case "explicitcapture":
                case "n":
                    options |= RegexOptions.ExplicitCapture;
                    break;
                    
                case "compiled":
                case "c":
                    options |= RegexOptions.Compiled;
                    break;
                    
                case "ignorepatternwhitespace":
                case "x":
                    options |= RegexOptions.IgnorePatternWhitespace;
                    break;
                    
                default:
                    _logger.LogWarning("⚠️ Unknown regex option in XML: {Option}", trimmed);
                    break;
            }
        }
        
        return options;
    }

    /// <summary>
    /// Get required XML attribute
    /// </summary>
    private static string GetRequiredAttribute(XmlNode node, string attributeName)
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
    private static string? GetOptionalAttribute(XmlNode node, string attributeName)
    {
        return node.Attributes?[attributeName]?.Value;
    }

    /// <summary>
    /// Get optional XML element text
    /// </summary>
    private static string? GetOptionalElement(XmlNode node, string elementName)
    {
        return node.SelectSingleNode(elementName)?.InnerText;
    }

    #endregion
}