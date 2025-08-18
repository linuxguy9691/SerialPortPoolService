// SerialPortPool.Core/Services/XmlBibConfigurationLoader.cs - SPRINT 9 ENHANCED VERSION
using System.Xml;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using SerialPortPool.Core.Interfaces;
using SerialPortPool.Core.Models;

namespace SerialPortPool.Core.Services;

/// <summary>
/// XML BIB Configuration Loader - SPRINT 9 Enhanced with Multi-Level Validation
/// Parses BIB → UUT → PORT → PROTOCOL hierarchy from XML files with 4-level validation support
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
        port.StartCommands = ParseMultipleStartCommands(portNode);
        port.TestCommands = ParseMultipleTestCommands(portNode);
        port.StopCommands = ParseMultipleStopCommands(portNode);

        return port;
    }

    /// <summary>
    /// Parse command sequence from XML node
    /// </summary>
   // <summary>
/// Parse command sequence from XML node - FIXED: Added continue_on_failure support
/// </summary>
private CommandSequence ParseCommandSequence(XmlNode? sequenceNode)
{
    var sequence = new CommandSequence();
    
    if (sequenceNode != null)
    {
        // 🔥 FIX: Read continue_on_failure attribute
        var continueOnFailureAttr = GetOptionalAttribute(sequenceNode, "continue_on_failure");
        sequence.ContinueOnFailure = bool.Parse(continueOnFailureAttr ?? "false");
        
        _logger.LogDebug($"🔧 ParseCommandSequence: continue_on_failure = {sequence.ContinueOnFailure} (from XML: '{continueOnFailureAttr}')");
        
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
/// 🆕 SPRINT 9: Parse multiple <test> elements with multi-level validation support - FIXED: Added continue_on_failure support
/// </summary>

/// <summary>
/// 🆕 SPRINT 9+: Parse multiple <start> elements with continue_on_failure support
/// </summary>
private CommandSequence ParseMultipleStartCommands(XmlNode portNode)
{
    var sequence = new CommandSequence();
    
    // Obtenir TOUS les éléments <start>
    var startNodes = portNode.SelectNodes("start");
    
    if (startNodes != null && startNodes.Count > 0)
    {
        _logger.LogDebug($"📊 Found {startNodes.Count} start elements in XML");
        
        // Déterminer continue_on_failure pour la séquence
        bool shouldContinueOnFailure = false;
        
        for (int i = 0; i < startNodes.Count; i++)
        {
            var startNode = startNodes[i];
            
            // Lire continue_on_failure de chaque élément start
            var continueOnFailureAttr = GetOptionalAttribute(startNode, "continue_on_failure");
            var startContinueOnFailure = bool.Parse(continueOnFailureAttr ?? "false");
            
            _logger.LogDebug($"🔧 Start {i + 1}: continue_on_failure = {startContinueOnFailure} (from XML: '{continueOnFailureAttr}')");
            
            // Si ANY start a continue_on_failure=true, alors la séquence continue
            if (startContinueOnFailure)
            {
                shouldContinueOnFailure = true;
            }
            
            var command = ParseSingleStartCommand(startNode, i + 1);
            
            if (command != null)
            {
                sequence.Commands.Add(command);
                _logger.LogDebug($"✅ Start {i + 1}: Command added - {command.Command}");
            }
        }
        
        // Définir continue_on_failure pour toute la séquence
        sequence.ContinueOnFailure = shouldContinueOnFailure;
        
        _logger.LogInformation($"🎯 START Sequence: continue_on_failure = {sequence.ContinueOnFailure} (from {startNodes.Count} start commands)");
        _logger.LogInformation($"🎯 Parsed {sequence.Commands.Count} start commands");
    }
    else
    {
        _logger.LogDebug("⚠️ No start elements found in XML");
    }
    
    return sequence;
}

/// <summary>
/// 🆕 SPRINT 9+: Parse multiple <stop> elements with continue_on_failure support
/// </summary>
private CommandSequence ParseMultipleStopCommands(XmlNode portNode)
{
    var sequence = new CommandSequence();
    
    // Obtenir TOUS les éléments <stop>
    var stopNodes = portNode.SelectNodes("stop");
    
    if (stopNodes != null && stopNodes.Count > 0)
    {
        _logger.LogDebug($"📊 Found {stopNodes.Count} stop elements in XML");
        
        // Déterminer continue_on_failure pour la séquence
        bool shouldContinueOnFailure = false;
        
        for (int i = 0; i < stopNodes.Count; i++)
        {
            var stopNode = stopNodes[i];
            
            // Lire continue_on_failure de chaque élément stop
            var continueOnFailureAttr = GetOptionalAttribute(stopNode, "continue_on_failure");
            var stopContinueOnFailure = bool.Parse(continueOnFailureAttr ?? "false");
            
            _logger.LogDebug($"🔧 Stop {i + 1}: continue_on_failure = {stopContinueOnFailure} (from XML: '{continueOnFailureAttr}')");
            
            // Si ANY stop a continue_on_failure=true, alors la séquence continue
            if (stopContinueOnFailure)
            {
                shouldContinueOnFailure = true;
            }
            
            var command = ParseSingleStopCommand(stopNode, i + 1);
            
            if (command != null)
            {
                sequence.Commands.Add(command);
                _logger.LogDebug($"✅ Stop {i + 1}: Command added - {command.Command}");
            }
        }
        
        // Définir continue_on_failure pour toute la séquence
        sequence.ContinueOnFailure = shouldContinueOnFailure;
        
        _logger.LogInformation($"🎯 STOP Sequence: continue_on_failure = {sequence.ContinueOnFailure} (from {stopNodes.Count} stop commands)");
        _logger.LogInformation($"🎯 Parsed {sequence.Commands.Count} stop commands");
    }
    else
    {
        _logger.LogDebug("⚠️ No stop elements found in XML");
    }
    
    return sequence;
}

/// <summary>
/// 🆕 Parse single <start> command (standard protocol command only)
/// </summary>
private ProtocolCommand? ParseSingleStartCommand(XmlNode startNode, int startIndex)
{
    var commandText = GetOptionalElement(startNode, "command");
    if (string.IsNullOrEmpty(commandText))
    {
        _logger.LogWarning($"⚠️ Start command {startIndex} has no command text, skipping");
        return null;
    }

    // Créer commande standard (pas de multi-level pour start)
    var command = CreateStandardProtocolCommand(startNode, commandText, startIndex);
    _logger.LogDebug($"📊 Start {startIndex}: Standard command created - {commandText}");
    
    return command;
}

/// <summary>
/// 🆕 Parse single <stop> command (standard protocol command only)
/// </summary>
private ProtocolCommand? ParseSingleStopCommand(XmlNode stopNode, int stopIndex)
{
    var commandText = GetOptionalElement(stopNode, "command");
    if (string.IsNullOrEmpty(commandText))
    {
        _logger.LogWarning($"⚠️ Stop command {stopIndex} has no command text, skipping");
        return null;
    }

    // Créer commande standard (pas de multi-level pour stop)
    var command = CreateStandardProtocolCommand(stopNode, commandText, stopIndex);
    _logger.LogDebug($"📊 Stop {stopIndex}: Standard command created - {commandText}");
    
    return command;
}

private CommandSequence ParseMultipleTestCommands(XmlNode portNode)
{
    var sequence = new CommandSequence();
    
    // 🔥 FIX: Use SelectNodes("test") to get ALL <test> elements
    var testNodes = portNode.SelectNodes("test");
    
    if (testNodes != null && testNodes.Count > 0)
    {
        _logger.LogDebug($"📊 Found {testNodes.Count} test elements in XML");
        
        // 🔥 FIX: Determine continue_on_failure from test nodes
        bool shouldContinueOnFailure = false;
        
        for (int i = 0; i < testNodes.Count; i++)
        {
            var testNode = testNodes[i];
            
            // 🔥 FIX: Read continue_on_failure from each test node
            var continueOnFailureAttr = GetOptionalAttribute(testNode, "continue_on_failure");
            var testContinueOnFailure = bool.Parse(continueOnFailureAttr ?? "false");
            
            _logger.LogDebug($"🔧 Test {i + 1}: continue_on_failure = {testContinueOnFailure} (from XML: '{continueOnFailureAttr}')");
            
            // 🎯 LOGIC: If ANY test has continue_on_failure=true, then sequence continues on failure
            if (testContinueOnFailure)
            {
                shouldContinueOnFailure = true;
            }
            
            var command = ParseSingleTestCommand(testNode, i + 1);
            
            if (command != null)
            {
                sequence.Commands.Add(command);
                
                // ✨ Enhanced logging for multi-level commands
                if (command is MultiLevelProtocolCommand multiLevelCmd)
                {
                    _logger.LogInformation($"✨ Test {i + 1}: Multi-level command - {multiLevelCmd.ValidationPatterns.Count} additional validation levels");
                }
                else
                {
                    _logger.LogDebug($"✅ Test {i + 1}: Standard command - {command.Command}");
                }
            }
        }
        
        // 🔥 FIX: Set the continue_on_failure for the entire sequence
        sequence.ContinueOnFailure = shouldContinueOnFailure;
        
        _logger.LogInformation($"🎯 TEST Sequence: continue_on_failure = {sequence.ContinueOnFailure} (from {testNodes.Count} test commands)");
        _logger.LogInformation($"🎯 Parsed {sequence.Commands.Count} test commands ({sequence.Commands.OfType<MultiLevelProtocolCommand>().Count()} multi-level)");
    }
    else
    {
        _logger.LogDebug("⚠️ No test elements found in XML");
    }
    
    return sequence;
}

    /// <summary>
    /// 🆕 SPRINT 9: Parse single <test> command with multi-level validation support
    /// </summary>
    private ProtocolCommand? ParseSingleTestCommand(XmlNode testNode, int testIndex)
    {
        var commandText = GetOptionalElement(testNode, "command");
        if (string.IsNullOrEmpty(commandText))
        {
            _logger.LogWarning($"⚠️ Test command {testIndex} has no command text, skipping");
            return null;
        }

        // 🔥 NEW: Check if this test has validation_levels (multi-level) or just expected_response (legacy)
        var validationLevelsNode = testNode.SelectSingleNode("validation_levels");
        
        ProtocolCommand command;
        
        if (validationLevelsNode != null)
        {
            // ✨ Create MultiLevelProtocolCommand for advanced validation
            command = CreateMultiLevelProtocolCommand(testNode, commandText, testIndex);
            _logger.LogDebug($"🎯 Test {testIndex}: Multi-level command created with validation levels");
        }
        else
        {
            // ✅ Create standard ProtocolCommand for backward compatibility
            command = CreateStandardProtocolCommand(testNode, commandText, testIndex);
            _logger.LogDebug($"📊 Test {testIndex}: Standard command created (legacy mode)");
        }
        
        return command;
    }

    /// <summary>
    /// 🆕 SPRINT 9: Create MultiLevelProtocolCommand with validation levels
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
        
        // Parse primary expected_response (PASS level)
        var responseNode = testNode.SelectSingleNode("expected_response");
        if (responseNode != null)
        {
            command.ExpectedResponse = responseNode.InnerText;
            var regexAttr = responseNode.Attributes?["regex"]?.Value;
            command.IsRegexPattern = bool.Parse(regexAttr ?? "false");
            
            _logger.LogDebug($"🎯 Test {testIndex} PASS pattern: {command.ExpectedResponse} (Regex: {command.IsRegexPattern})");
        }
        
        // ✨ Parse validation_levels node
        var validationLevelsNode = testNode.SelectSingleNode("validation_levels");
        if (validationLevelsNode != null)
        {
            ParseValidationLevelsIntoCommand(command, validationLevelsNode, testIndex);
        }
        
        return command;
    }

    /// <summary>
    /// 🆕 SPRINT 9: Parse validation levels from XML into MultiLevelProtocolCommand
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
                    // Add pattern to command
                    command.ValidationPatterns[level] = pattern;
                    
                    // Check for regex attribute
                    var regexAttr = levelNode.Attributes?["regex"]?.Value;
                    var isRegex = bool.Parse(regexAttr ?? "false");
                    
                    // For now, assume all patterns use same regex setting
                    if (isRegex)
                    {
                        command.IsRegexPattern = true;
                    }
                    
                    // 🔌 Parse hardware trigger attribute (for CRITICAL level)
                    if (level == ValidationLevel.CRITICAL)
                    {
                        var triggerHardware = bool.Parse(levelNode.Attributes?["trigger_hardware"]?.Value ?? "false");
                        command.TriggerHardwareOnCritical = triggerHardware;
                        
                        if (triggerHardware)
                        {
                            _logger.LogInformation($"🔌 Test {testIndex}: Hardware trigger ENABLED for CRITICAL validation");
                        }
                    }
                    
                    _logger.LogDebug($"🎯 Test {testIndex} {level} pattern: {pattern} (Regex: {isRegex})");
                }
            }
        }
        
        _logger.LogInformation($"✨ Test {testIndex}: Multi-level validation configured - {command.ValidationPatterns.Count} additional levels");
    }

    /// <summary>
    /// ✅ Create standard ProtocolCommand (backward compatibility)
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
        
        // Parse expected_response
        var responseNode = testNode.SelectSingleNode("expected_response");
        if (responseNode != null)
        {
            command.ExpectedResponse = responseNode.InnerText;
            
            // Check for regex attribute
            var regexAttr = responseNode.Attributes?["regex"]?.Value;
            command.IsRegexPattern = bool.Parse(regexAttr ?? "false");
            
            _logger.LogDebug($"📊 Test {testIndex}: Standard pattern: {command.ExpectedResponse} (Regex: {command.IsRegexPattern})");
        }

        return command;
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