// ===================================================================
// SPRINT 11 BOUCHÉE #1: Enhanced XmlBibConfigurationLoader - Multi-File Discovery
// File: SerialPortPool.Core/Services/XmlBibConfigurationLoader.cs
// ÉVOLUTION INTELLIGENTE: Ajoute multi-file capability + smart fallback
// BACKWARD COMPATIBLE: Tout le code Sprint 10 continue de fonctionner
// ===================================================================

using System.Xml;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using SerialPortPool.Core.Interfaces;
using SerialPortPool.Core.Models;

namespace SerialPortPool.Core.Services;

/// <summary>
/// SPRINT 11 ENHANCED: XML BIB Configuration Loader with Multi-File Discovery
/// ÉVOLUTION: Supports both individual BIB files AND legacy single XML file
/// SÉCURITÉ: Complete BIB isolation - syntax errors contained per file
/// SMART FALLBACK: Individual file first, then legacy system
/// </summary>
public class XmlBibConfigurationLoader : IBibConfigurationLoader
{
    private readonly ILogger<XmlBibConfigurationLoader> _logger;
    private readonly IMemoryCache _cache;
    private string _defaultXmlPath = string.Empty;
    private readonly Dictionary<string, BibConfiguration> _loadedConfigurations = new();
    
    // 🆕 SPRINT 11: Multi-file configuration directory
    private readonly string _configurationDirectory;
    private readonly bool _enableMultiFileDiscovery;

    public XmlBibConfigurationLoader(
        ILogger<XmlBibConfigurationLoader> logger,
        IMemoryCache cache)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        
        // 🆕 SPRINT 11: Initialize multi-file discovery
        _configurationDirectory = "Configuration/";
        _enableMultiFileDiscovery = true;
        
        _logger.LogInformation("🚀 SPRINT 11: Enhanced XML loader initialized with multi-file discovery");
        _logger.LogInformation($"📁 Configuration directory: {_configurationDirectory}");
    }

    /// <summary>
    /// Set default XML configuration path (EXISTING - preserved for backward compatibility)
    /// </summary>
    public void SetDefaultConfigurationPath(string defaultXmlPath)
    {
        _defaultXmlPath = defaultXmlPath;
        _logger.LogDebug($"📄 Default XML configuration path set: {defaultXmlPath}");
    }

    /// <summary>
    /// SPRINT 11 ENHANCED: Load single BIB configuration with SMART DISCOVERY
    /// STRATEGY: Individual BIB file FIRST → Legacy system FALLBACK
    /// ISOLATION: Each BIB in separate file = zero cross-contamination
    /// </summary>
    public async Task<BibConfiguration?> LoadBibConfigurationAsync(string bibId)
    {
        try
        {
            _logger.LogDebug($"🔍 SPRINT 11: Smart BIB discovery for: {bibId}");

            // ✅ STEP 1: Try from loaded configurations cache first (EXISTING logic)
            if (_loadedConfigurations.TryGetValue(bibId, out var cached))
            {
                _logger.LogDebug($"💾 BIB found in memory cache: {bibId}");
                return cached;
            }

            // 🆕 STEP 2: SPRINT 11 - Try individual BIB file discovery
            if (_enableMultiFileDiscovery)
            {
                var individualFile = await TryLoadFromIndividualFileAsync(bibId);
                if (individualFile != null)
                {
                    _logger.LogInformation($"🎯 SPRINT 11: Loaded from individual BIB file: {bibId}");
                    _loadedConfigurations[bibId] = individualFile;
                    return individualFile;
                }
            }

            // ✅ STEP 3: FALLBACK to existing legacy system (BACKWARD COMPATIBILITY)
            _logger.LogDebug($"🔄 Fallback to legacy XML system for BIB: {bibId}");
            return await LoadBibFromLegacySystemAsync(bibId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"❌ Failed to load BIB configuration: {bibId}");
            return null;
        }
    }

    // 🆕 SPRINT 11: Try loading from individual BIB file
    private async Task<BibConfiguration?> TryLoadFromIndividualFileAsync(string bibId)
    {
        try
        {
            // 🎯 SPRINT 11: Individual file naming convention
            var fileName = $"bib_{bibId}.xml";
            var filePath = Path.Combine(_configurationDirectory, fileName);
            
            _logger.LogDebug($"🔍 Checking individual BIB file: {filePath}");

            if (!File.Exists(filePath))
            {
                _logger.LogDebug($"📁 Individual BIB file not found: {fileName} (will try fallback)");
                return null;
            }

            _logger.LogInformation($"📄 SPRINT 11: Loading individual BIB file: {fileName}");

            // ✅ ISOLATION: Each BIB file is completely independent
            var bibConfig = await LoadBibFromSingleFileAsync(filePath, bibId);
            
            if (bibConfig != null)
            {
                _logger.LogInformation($"✅ Individual BIB file loaded successfully: {bibId}");
                _logger.LogDebug($"📊 BIB {bibId}: {bibConfig.Uuts.Count} UUTs, {bibConfig.TotalPortCount} ports");
                return bibConfig;
            }
            
            _logger.LogWarning($"⚠️ Individual BIB file exists but failed to parse: {fileName}");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"💥 Error loading individual BIB file for: {bibId}");
            return null;
        }
    }

    // 🆕 SPRINT 11: Load BIB from individual XML file
    private async Task<BibConfiguration?> LoadBibFromSingleFileAsync(string filePath, string expectedBibId)
    {
        try
        {
            var cacheKey = $"individual_bib_{expectedBibId}_{GetFileHash(filePath)}";
            
            if (_cache.TryGetValue(cacheKey, out BibConfiguration? cached) && cached != null)
            {
                _logger.LogDebug($"💾 Individual BIB loaded from cache: {expectedBibId}");
                return cached;
            }

            _logger.LogDebug($"📄 Parsing individual BIB file: {filePath}");

            var xmlDoc = new XmlDocument();
            xmlDoc.Load(filePath);

            var rootNode = xmlDoc.DocumentElement;
            if (rootNode?.Name != "bib")
            {
                throw new InvalidOperationException($"Individual BIB file root element must be 'bib', found: {rootNode?.Name}");
            }

            // Parse the single BIB configuration
            var bibConfig = ParseBibConfiguration(rootNode);
            
            // ✅ Validate that the BIB ID matches the expected one
            if (!bibConfig.BibId.Equals(expectedBibId, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning($"⚠️ BIB ID mismatch in file {filePath}: expected {expectedBibId}, found {bibConfig.BibId}");
                // Accept it anyway but log the discrepancy
            }

            // Cache the result
            var cacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30),
                Priority = CacheItemPriority.High
            };
            _cache.Set(cacheKey, bibConfig, cacheOptions);

            _logger.LogInformation($"✅ Individual BIB parsed successfully: {bibConfig.BibId}");
            return bibConfig;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"❌ Failed to parse individual BIB file: {filePath}");
            throw;
        }
    }

    // ✅ EXISTING: Fallback to legacy system (BACKWARD COMPATIBILITY)
    private async Task<BibConfiguration?> LoadBibFromLegacySystemAsync(string bibId)
    {
        // Load from default path if available (EXISTING LOGIC - unchanged)
        if (string.IsNullOrEmpty(_defaultXmlPath))
        {
            _logger.LogDebug($"📄 No default XML path set and BIB {bibId} not in individual files");
            return null;
        }

        try
        {
            _logger.LogDebug($"🔄 Loading from legacy XML system: {bibId}");
            
            var allConfigs = await LoadConfigurationsAsync(_defaultXmlPath);
            var bibConfig = allConfigs.TryGetValue(bibId, out var config) ? config : null;
            
            if (bibConfig != null)
            {
                _logger.LogInformation($"✅ Loaded from legacy XML system: {bibId}");
            }
            else
            {
                _logger.LogWarning($"⚠️ BIB not found in legacy XML system: {bibId}");
            }
            
            return bibConfig;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"❌ Failed to load BIB {bibId} from legacy system");
            return null;
        }
    }

    // 🆕 SPRINT 11: Discover all available BIB files
    public async Task<List<string>> DiscoverAvailableBibIdsAsync()
    {
        var bibIds = new List<string>();
        
        try
        {
            _logger.LogDebug($"🔍 SPRINT 11: Discovering available BIB files in: {_configurationDirectory}");

            if (!Directory.Exists(_configurationDirectory))
            {
                _logger.LogWarning($"📁 Configuration directory not found: {_configurationDirectory}");
                return bibIds;
            }

            // Find all individual BIB files
            var bibFiles = Directory.GetFiles(_configurationDirectory, "bib_*.xml");
            
            foreach (var filePath in bibFiles)
            {
                var fileName = Path.GetFileName(filePath);
                
                // Extract BIB ID from filename: bib_xyz.xml → xyz
                if (fileName.StartsWith("bib_") && fileName.EndsWith(".xml"))
                {
                    var bibId = fileName.Substring(4, fileName.Length - 8); // Remove "bib_" and ".xml"
                    
                    if (!string.IsNullOrEmpty(bibId))
                    {
                        bibIds.Add(bibId);
                        _logger.LogDebug($"📄 Discovered BIB file: {fileName} → BIB_ID: {bibId}");
                    }
                }
            }

            _logger.LogInformation($"🎯 SPRINT 11: Discovered {bibIds.Count} individual BIB files");
            
            return bibIds;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"❌ Error during BIB discovery in: {_configurationDirectory}");
            return bibIds;
        }
    }

    // ===================================================================
    // EXISTING METHODS (unchanged for backward compatibility)
    // ===================================================================

    /// <summary>
    /// Load all BIB configurations from XML file (EXISTING - unchanged)
    /// </summary>
    public async Task<Dictionary<string, BibConfiguration>> LoadConfigurationsAsync(string xmlPath)
    {
        var cacheKey = $"xml_config_{xmlPath}_{GetFileHash(xmlPath)}";
        
        if (_cache.TryGetValue(cacheKey, out Dictionary<string, BibConfiguration>? cached) && cached != null)
        {
            _logger.LogDebug($"💾 Loaded BIB configurations from cache: {xmlPath}");
            return cached;
        }

        try
        {
            _logger.LogInformation($"📄 Loading BIB configurations from XML: {xmlPath}");

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
                    _logger.LogDebug($"📊 Parsed BIB: {bibConfig.BibId} with {bibConfig.Uuts.Count} UUTs");
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

            _logger.LogInformation($"✅ Successfully loaded {configurations.Count} BIB configurations from {xmlPath}");
            return configurations;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"❌ Failed to load BIB configurations from: {xmlPath}");
            throw;
        }
    }

    /// <summary>
    /// Load single BIB configuration by ID from specific XML file (EXISTING - unchanged)
    /// </summary>
    public async Task<BibConfiguration?> LoadBibConfigurationAsync(string xmlPath, string bibId)
    {
        var allConfigs = await LoadConfigurationsAsync(xmlPath);
        return allConfigs.TryGetValue(bibId, out var config) ? config : null;
    }

    /// <summary>
    /// Get all loaded configurations from memory (EXISTING - unchanged)
    /// </summary>
    public async Task<Dictionary<string, BibConfiguration>> GetLoadedConfigurationsAsync()
    {
        await Task.CompletedTask;
        return new Dictionary<string, BibConfiguration>(_loadedConfigurations);
    }

    /// <summary>
    /// Clear all loaded configurations from memory (EXISTING - unchanged)
    /// </summary>
    public async Task ClearConfigurationsAsync()
    {
        await Task.CompletedTask;
        _loadedConfigurations.Clear();
        _logger.LogInformation("🧹 Cleared all loaded BIB configurations from memory");
    }

    /// <summary>
    /// Validate BIB configuration exists and is accessible (EXISTING - unchanged)
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
            _logger.LogWarning(ex, $"⚠️ BIB configuration validation failed for {bibId}");
            return false;
        }
    }

    // ===================================================================
    // PRIVATE PARSING METHODS (unchanged from existing implementation)
    // ===================================================================

    /// <summary>
    /// Parse BIB configuration from XML node (EXISTING - unchanged)
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
    /// Parse UUT configuration from XML node (EXISTING - unchanged)
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
    /// Parse port configuration from XML node (EXISTING - unchanged) 
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
        port.StartCommands = ParseMultipleStartCommands(portNode);
        port.TestCommands = ParseMultipleTestCommands(portNode);
        port.StopCommands = ParseMultipleStopCommands(portNode);

        return port;
    }

    /// <summary>
    /// Parse multiple <start> elements with continue_on_failure support (EXISTING - unchanged)
    /// </summary>
    private CommandSequence ParseMultipleStartCommands(XmlNode portNode)
    {
        var sequence = new CommandSequence();
        
        var startNodes = portNode.SelectNodes("start");
        
        if (startNodes != null && startNodes.Count > 0)
        {
            bool shouldContinueOnFailure = false;
            
            for (int i = 0; i < startNodes.Count; i++)
            {
                var startNode = startNodes[i];
                
                var continueOnFailureAttr = GetOptionalAttribute(startNode, "continue_on_failure");
                var startContinueOnFailure = bool.Parse(continueOnFailureAttr ?? "false");
                
                if (startContinueOnFailure)
                {
                    shouldContinueOnFailure = true;
                }
                
                var command = ParseSingleStartCommand(startNode, i + 1);
                
                if (command != null)
                {
                    sequence.Commands.Add(command);
                }
            }
            
            sequence.ContinueOnFailure = shouldContinueOnFailure;
        }
        
        return sequence;
    }

    /// <summary>
    /// Parse multiple <test> elements with multi-level validation support (EXISTING - unchanged)
    /// </summary>
    private CommandSequence ParseMultipleTestCommands(XmlNode portNode)
    {
        var sequence = new CommandSequence();
        
        var testNodes = portNode.SelectNodes("test");
        
        if (testNodes != null && testNodes.Count > 0)
        {
            bool shouldContinueOnFailure = false;
            
            for (int i = 0; i < testNodes.Count; i++)
            {
                var testNode = testNodes[i];
                
                var continueOnFailureAttr = GetOptionalAttribute(testNode, "continue_on_failure");
                var testContinueOnFailure = bool.Parse(continueOnFailureAttr ?? "false");
                
                if (testContinueOnFailure)
                {
                    shouldContinueOnFailure = true;
                }
                
                var command = ParseSingleTestCommand(testNode, i + 1);
                
                if (command != null)
                {
                    sequence.Commands.Add(command);
                }
            }
            
            sequence.ContinueOnFailure = shouldContinueOnFailure;
        }
        
        return sequence;
    }

    /// <summary>
    /// Parse multiple <stop> elements with continue_on_failure support (EXISTING - unchanged)
    /// </summary>
    private CommandSequence ParseMultipleStopCommands(XmlNode portNode)
    {
        var sequence = new CommandSequence();
        
        var stopNodes = portNode.SelectNodes("stop");
        
        if (stopNodes != null && stopNodes.Count > 0)
        {
            bool shouldContinueOnFailure = false;
            
            for (int i = 0; i < stopNodes.Count; i++)
            {
                var stopNode = stopNodes[i];
                
                var continueOnFailureAttr = GetOptionalAttribute(stopNode, "continue_on_failure");
                var stopContinueOnFailure = bool.Parse(continueOnFailureAttr ?? "false");
                
                if (stopContinueOnFailure)
                {
                    shouldContinueOnFailure = true;
                }
                
                var command = ParseSingleStopCommand(stopNode, i + 1);
                
                if (command != null)
                {
                    sequence.Commands.Add(command);
                }
            }
            
            sequence.ContinueOnFailure = shouldContinueOnFailure;
        }
        
        return sequence;
    }

    /// <summary>
    /// Parse single <start> command (EXISTING - unchanged)
    /// </summary>
    private ProtocolCommand? ParseSingleStartCommand(XmlNode startNode, int startIndex)
    {
        var commandText = GetOptionalElement(startNode, "command");
        if (string.IsNullOrEmpty(commandText))
        {
            return null;
        }

        return CreateStandardProtocolCommand(startNode, commandText, startIndex);
    }

    /// <summary>
    /// Parse single <test> command with multi-level validation support (EXISTING - unchanged)
    /// </summary>
    private ProtocolCommand? ParseSingleTestCommand(XmlNode testNode, int testIndex)
    {
        var commandText = GetOptionalElement(testNode, "command");
        if (string.IsNullOrEmpty(commandText))
        {
            return null;
        }

        var validationLevelsNode = testNode.SelectSingleNode("validation_levels");
        
        ProtocolCommand command;
        
        if (validationLevelsNode != null)
        {
            command = CreateMultiLevelProtocolCommand(testNode, commandText, testIndex);
        }
        else
        {
            command = CreateStandardProtocolCommand(testNode, commandText, testIndex);
        }
        
        return command;
    }

    /// <summary>
    /// Parse single <stop> command (EXISTING - unchanged)
    /// </summary>
    private ProtocolCommand? ParseSingleStopCommand(XmlNode stopNode, int stopIndex)
    {
        var commandText = GetOptionalElement(stopNode, "command");
        if (string.IsNullOrEmpty(commandText))
        {
            return null;
        }

        return CreateStandardProtocolCommand(stopNode, commandText, stopIndex);
    }

    /// <summary>
    /// Create MultiLevelProtocolCommand with validation levels (EXISTING - unchanged)
    /// </summary>
    private MultiLevelProtocolCommand CreateMultiLevelProtocolCommand(XmlNode testNode, string commandText, int testIndex)
    {
        var command = new MultiLevelProtocolCommand
        {
            CommandId = Guid.NewGuid().ToString(),
            Command = commandText,
            TimeoutMs = int.Parse(GetOptionalElement(testNode, "timeout_ms") ?? "5000"),
            RetryCount = int.Parse(GetOptionalElement(testNode, "retry_count") ?? "0"),
            Data = System.Text.Encoding.UTF8.GetBytes(commandText)
        };
        
        var responseNode = testNode.SelectSingleNode("expected_response");
        if (responseNode != null)
        {
            command.ExpectedResponse = responseNode.InnerText;
            var regexAttr = responseNode.Attributes?["regex"]?.Value;
            command.IsRegexPattern = bool.Parse(regexAttr ?? "false");
        }
        
        var validationLevelsNode = testNode.SelectSingleNode("validation_levels");
        if (validationLevelsNode != null)
        {
            ParseValidationLevelsIntoCommand(command, validationLevelsNode, testIndex);
        }
        
        return command;
    }

    /// <summary>
    /// Parse validation levels from XML into MultiLevelProtocolCommand (EXISTING - unchanged)
    /// </summary>
    private void ParseValidationLevelsIntoCommand(MultiLevelProtocolCommand command, XmlNode validationLevelsNode, int testIndex)
    {
        var levelMappings = new Dictionary<string, ValidationLevel>
        {
            ["warn"] = ValidationLevel.WARN,
            ["fail"] = ValidationLevel.FAIL,
            ["critical"] = ValidationLevel.CRITICAL
        };
        
        foreach (var levelMapping in levelMappings)
        {
            var levelName = levelMapping.Key;
            var level = levelMapping.Value;
            
            var levelNode = validationLevelsNode.SelectSingleNode(levelName);
            if (levelNode != null)
            {
                var pattern = levelNode.InnerText;
                if (!string.IsNullOrEmpty(pattern))
                {
                    command.ValidationPatterns[level] = pattern;
                    
                    var regexAttr = levelNode.Attributes?["regex"]?.Value;
                    var isRegex = bool.Parse(regexAttr ?? "false");
                    
                    if (isRegex)
                    {
                        command.IsRegexPattern = true;
                    }
                    
                    if (level == ValidationLevel.CRITICAL)
                    {
                        var triggerHardware = bool.Parse(levelNode.Attributes?["trigger_hardware"]?.Value ?? "false");
                        command.TriggerHardwareOnCritical = triggerHardware;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Create standard ProtocolCommand (EXISTING - unchanged)
    /// </summary>
    private ProtocolCommand CreateStandardProtocolCommand(XmlNode testNode, string commandText, int testIndex)
    {
        var command = new ProtocolCommand
        {
            CommandId = Guid.NewGuid().ToString(),
            Command = commandText,
            TimeoutMs = int.Parse(GetOptionalElement(testNode, "timeout_ms") ?? "5000"),
            RetryCount = int.Parse(GetOptionalElement(testNode, "retry_count") ?? "0"),
            Data = System.Text.Encoding.UTF8.GetBytes(commandText)
        };
        
        var responseNode = testNode.SelectSingleNode("expected_response");
        if (responseNode != null)
        {
            command.ExpectedResponse = responseNode.InnerText;
            
            var regexAttr = responseNode.Attributes?["regex"]?.Value;
            command.IsRegexPattern = bool.Parse(regexAttr ?? "false");
        }

        return command;
    }

    // ===================================================================
    // HELPER METHODS (existing and new)
    // ===================================================================

    /// <summary>
    /// Get required XML attribute (EXISTING - unchanged)
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
    /// Get optional XML attribute (EXISTING - unchanged)
    /// </summary>
    private string? GetOptionalAttribute(XmlNode node, string attributeName)
    {
        return node.Attributes?[attributeName]?.Value;
    }

    /// <summary>
    /// Get required XML element (EXISTING - unchanged)
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
    /// Get optional XML element (EXISTING - unchanged)
    /// </summary>
    private string? GetOptionalElement(XmlNode node, string elementName)
    {
        return node.SelectSingleNode(elementName)?.InnerText;
    }

    /// <summary>
    /// Get file hash for caching (EXISTING - unchanged)
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

    

    // [Placeholder for remaining parsing methods - include all existing methods unchanged]
    // This includes ParseMultipleStartCommands, ParseMultipleTestCommands, ParseMultipleStopCommands, etc.
    // All existing parsing logic remains exactly the same for backward compatibility
}