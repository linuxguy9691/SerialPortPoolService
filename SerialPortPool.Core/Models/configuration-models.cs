namespace SerialPortPool.Core.Models.Configuration;

/// <summary>
/// Root system configuration containing all BIBs
/// </summary>
public class SystemConfiguration
{
    public List<BibConfiguration> Bibs { get; set; } = new();
    public Dictionary<string, object> GlobalSettings { get; set; } = new();
    public DateTime LoadedAt { get; set; } = DateTime.UtcNow;
    public string SourcePath { get; set; } = string.Empty;
    public string Version { get; set; } = "1.0";
    
    /// <summary>
    /// Get BIB configuration by ID
    /// </summary>
    public BibConfiguration? GetBib(string bibId) => 
        Bibs.FirstOrDefault(b => b.BibId.Equals(bibId, StringComparison.OrdinalIgnoreCase));
    
    /// <summary>
    /// Get all supported protocols across all configurations
    /// </summary>
    public IEnumerable<string> GetAllProtocols() =>
        Bibs.SelectMany(b => b.Uuts)
            .SelectMany(u => u.Ports)
            .Select(p => p.Protocol)
            .Distinct(StringComparer.OrdinalIgnoreCase);
}

/// <summary>
/// Board In Board (BIB) configuration
/// Represents a physical test board containing multiple UUTs
/// </summary>
public class BibConfiguration
{
    public string BibId { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<UutConfiguration> Uuts { get; set; } = new();
    public Dictionary<string, object> Properties { get; set; } = new();
    
    /// <summary>
    /// Get UUT configuration by ID
    /// </summary>
    public UutConfiguration? GetUut(string uutId) => 
        Uuts.FirstOrDefault(u => u.UutId.Equals(uutId, StringComparison.OrdinalIgnoreCase));
    
    /// <summary>
    /// Get total number of ports across all UUTs
    /// </summary>
    public int TotalPorts => Uuts.Sum(u => u.Ports.Count);
}

/// <summary>
/// Unit Under Test (UUT) configuration
/// Represents a device being tested within a BIB
/// </summary>
public class UutConfiguration
{
    public string UutId { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<PortConfiguration> Ports { get; set; } = new();
    public Dictionary<string, object> Properties { get; set; } = new();
    
    /// <summary>
    /// Get port configuration by number
    /// </summary>
    public PortConfiguration? GetPort(int portNumber) => 
        Ports.FirstOrDefault(p => p.PortNumber == portNumber);
    
    /// <summary>
    /// Get all protocols used by this UUT
    /// </summary>
    public IEnumerable<string> GetProtocols() => 
        Ports.Select(p => p.Protocol).Distinct(StringComparer.OrdinalIgnoreCase);
}

/// <summary>
/// Port configuration with protocol-specific settings
/// Represents a communication port on a UUT
/// </summary>
public class PortConfiguration
{
    public int PortNumber { get; set; }
    public string Protocol { get; set; } = string.Empty;
    public int Speed { get; set; }
    public string DataPattern { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    
    // Protocol-specific settings (RS485, USB, CAN, etc.)
    public Dictionary<string, object> ProtocolSettings { get; set; } = new();
    
    // 3-Phase command sequences
    public CommandSequence StartCommands { get; set; } = new();
    public CommandSequence TestCommands { get; set; } = new();
    public CommandSequence StopCommands { get; set; } = new();
    
    /// <summary>
    /// Get protocol-specific setting value
    /// </summary>
    public T? GetProtocolSetting<T>(string key) =>
        ProtocolSettings.TryGetValue(key, out var value) && value is T typedValue 
            ? typedValue 
            : default;
    
    /// <summary>
    /// Check if this is a multi-command port (has multiple commands in any phase)
    /// </summary>
    public bool IsMultiCommand => 
        StartCommands.Commands.Count > 1 || 
        TestCommands.Commands.Count > 1 || 
        StopCommands.Commands.Count > 1;
}

/// <summary>
/// Command sequence for a workflow phase (Start/Test/Stop)
/// </summary>
public class CommandSequence
{
    public List<CommandDefinition> Commands { get; set; } = new();
    public int DefaultTimeoutMs { get; set; } = 5000;
    public int DefaultRetryCount { get; set; } = 1;
    public bool ContinueOnFailure { get; set; } = false;
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// Create single-command sequence (backward compatibility)
    /// </summary>
    public static CommandSequence Single(string command, string expectedResponse, int timeoutMs = 5000, int retryCount = 1)
    {
        return new CommandSequence
        {
            Commands = new List<CommandDefinition>
            {
                new CommandDefinition
                {
                    Command = command,
                    ExpectedResponse = expectedResponse,
                    TimeoutMs = timeoutMs,
                    RetryCount = retryCount
                }
            }
        };
    }
}

/// <summary>
/// Individual command definition within a sequence
/// </summary>
public class CommandDefinition
{
    public string Command { get; set; } = string.Empty;
    public string ExpectedResponse { get; set; } = string.Empty;
    public int TimeoutMs { get; set; } = 5000;
    public int RetryCount { get; set; } = 1;
    public string Description { get; set; } = string.Empty;
    public bool IsRegex { get; set; } = false; // True if ExpectedResponse is regex pattern
    public bool IsCritical { get; set; } = true; // False allows continuing on failure
    
    /// <summary>
    /// Additional command parameters (protocol-specific)
    /// </summary>
    public Dictionary<string, object> Parameters { get; set; } = new();
}

/// <summary>
/// Configuration validation result
/// </summary>
public class ConfigurationValidationResult
{
    public bool IsValid => !Errors.Any();
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public List<string> Info { get; set; } = new();
    
    public void AddError(string message) => Errors.Add(message);
    public void AddWarning(string message) => Warnings.Add(message);
    public void AddInfo(string message) => Info.Add(message);
    
    public string GetSummary()
    {
        var summary = $"Validation: {(IsValid ? "PASSED" : "FAILED")}";
        if (Errors.Any()) summary += $" | {Errors.Count} errors";
        if (Warnings.Any()) summary += $" | {Warnings.Count} warnings";
        return summary;
    }
}

/// <summary>
/// Search criteria for finding port configurations
/// </summary>
public class ConfigurationSearchCriteria
{
    public string? Protocol { get; set; }
    public int? MinSpeed { get; set; }
    public int? MaxSpeed { get; set; }
    public string? DataPattern { get; set; }
    public string? BibId { get; set; }
    public string? UutId { get; set; }
    public int? PortNumber { get; set; }
    public Dictionary<string, object> ProtocolSettings { get; set; } = new();
}

/// <summary>
/// Configuration loading options
/// </summary>
public class ConfigurationLoadOptions
{
    public bool ValidateSchema { get; set; } = true;
    public bool ValidateBusinessRules { get; set; } = true;
    public bool ThrowOnValidationErrors { get; set; } = true;
    public bool LoadProtocolSpecificSettings { get; set; } = true;
    public TimeSpan CacheExpiration { get; set; } = TimeSpan.FromMinutes(5);
}