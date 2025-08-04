// SerialPortPool.Core/Models/CommandResult.cs - NEW Week 2
namespace SerialPortPool.Core.Models;

/// <summary>
/// Result of command execution
/// </summary>
public class CommandResult
{
    /// <summary>
    /// Command that was executed
    /// </summary>
    public string Command { get; set; } = string.Empty;

    /// <summary>
    /// Response received from device
    /// </summary>
    public string? Response { get; set; }

    /// <summary>
    /// Whether command executed successfully
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Error message if command failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Command execution duration
    /// </summary>
    public TimeSpan Duration { get; set; }

    /// <summary>
    /// Number of retry attempts made
    /// </summary>
    public int RetryAttempts { get; set; }

    /// <summary>
    /// Protocol name that executed the command
    /// </summary>
    public string ProtocolName { get; set; } = string.Empty;

    /// <summary>
    /// Session ID that executed the command
    /// </summary>
    public string SessionId { get; set; } = string.Empty;

    /// <summary>
    /// Command start time
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// Command end time
    /// </summary>
    public DateTime EndTime { get; set; }

    /// <summary>
    /// Additional result metadata
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// Create successful command result
    /// </summary>
    /// <param name="command">Original command</param>
    /// <param name="response">Device response</param>
    /// <param name="duration">Execution duration</param>
    /// <returns>Success result</returns>
    public static CommandResult Success(string command, string? response, TimeSpan duration)
    {
        return new CommandResult
        {
            Command = command,
            Response = response,
            IsSuccess = true,
            Duration = duration,
            EndTime = DateTime.Now,
            StartTime = DateTime.Now - duration
        };
    }

    /// <summary>
    /// Create failed command result
    /// </summary>
    /// <param name="command">Original command</param>
    /// <param name="errorMessage">Error details</param>
    /// <param name="duration">Execution duration</param>
    /// <returns>Failure result</returns>
    public static CommandResult Failure(string command, string errorMessage, TimeSpan duration)
    {
        return new CommandResult
        {
            Command = command,
            IsSuccess = false,
            ErrorMessage = errorMessage,
            Duration = duration,
            EndTime = DateTime.Now,
            StartTime = DateTime.Now - duration
        };
    }

    public override string ToString()
    {
        var status = IsSuccess ? "✅ SUCCESS" : "❌ FAILED";
        var response = !string.IsNullOrEmpty(Response) ? $" → {Response.Trim()}" : "";
        var error = !IsSuccess && !string.IsNullOrEmpty(ErrorMessage) ? $" ({ErrorMessage})" : "";
        return $"{status}: {Command.Trim()}{response}{error} [{Duration.TotalMilliseconds:F0}ms]";
    }
}

/// <summary>
/// Result of command sequence execution
/// </summary>
public class CommandSequenceResult
{
    /// <summary>
    /// Individual command results
    /// </summary>
    public List<CommandResult> CommandResults { get; set; } = new();

    /// <summary>
    /// Overall sequence success (all commands succeeded)
    /// </summary>
    public bool IsSuccess => CommandResults.All(r => r.IsSuccess);

    /// <summary>
    /// Total execution duration
    /// </summary>
    public TimeSpan TotalDuration => CommandResults.Any() ? 
        TimeSpan.FromTicks(CommandResults.Sum(r => r.Duration.Ticks)) : TimeSpan.Zero;

    /// <summary>
    /// Number of successful commands
    /// </summary>
    public int SuccessfulCommands => CommandResults.Count(r => r.IsSuccess);

    /// <summary>
    /// Number of failed commands
    /// </summary>
    public int FailedCommands => CommandResults.Count(r => !r.IsSuccess);

    /// <summary>
    /// Total number of commands
    /// </summary>
    public int TotalCommands => CommandResults.Count;

    /// <summary>
    /// First command that failed (if any)
    /// </summary>
    public CommandResult? FirstFailure => CommandResults.FirstOrDefault(r => !r.IsSuccess);

    /// <summary>
    /// Sequence execution summary
    /// </summary>
    public string GetSummary()
    {
        var status = IsSuccess ? "✅ SUCCESS" : "❌ FAILED";
        return $"{status}: {SuccessfulCommands}/{TotalCommands} commands succeeded [{TotalDuration.TotalMilliseconds:F0}ms]";
    }

    public override string ToString() => GetSummary();
}

/// <summary>
/// Protocol connectivity test result
/// </summary>
public class ProtocolTestResult
{
    /// <summary>
    /// Whether connectivity test passed
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Test error message if failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Test execution duration
    /// </summary>
    public TimeSpan Duration { get; set; }

    /// <summary>
    /// Protocol response time (ping-like)
    /// </summary>
    public TimeSpan? ResponseTime { get; set; }

    /// <summary>
    /// Additional test details
    /// </summary>
    public Dictionary<string, object> TestDetails { get; set; } = new();

    public override string ToString()
    {
        var status = IsSuccess ? "✅ CONNECTED" : "❌ FAILED";
        var responseTime = ResponseTime.HasValue ? $" ({ResponseTime.Value.TotalMilliseconds:F0}ms)" : "";
        return $"{status}{responseTime}";
    }
}

// SerialPortPool.Core/Models/CommandResult.cs - SECTION À CORRIGER
/// <summary>
/// Protocol handler capabilities - FIXED VERSION
/// </summary>
public class ProtocolCapabilities
{
    /// <summary>
    /// Protocol name (AJOUTÉ)
    /// </summary>
    public string ProtocolName { get; set; } = string.Empty;

    /// <summary>
    /// Protocol version (AJOUTÉ)
    /// </summary>
    public string Version { get; set; } = "1.0.0";

    /// <summary>
    /// Supports async operations (AJOUTÉ)
    /// </summary>
    public bool SupportsAsyncOperations { get; set; } = true;

    /// <summary>
    /// Supports sequence commands (AJOUTÉ)
    /// </summary>
    public bool SupportsSequenceCommands { get; set; } = true;

    /// <summary>
    /// Supports bidirectional communication (AJOUTÉ)
    /// </summary>
    public bool SupportsBidirectionalCommunication { get; set; } = true;

    /// <summary>
    /// Default timeout (AJOUTÉ)
    /// </summary>
    public TimeSpan DefaultTimeout { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Supported commands (AJOUTÉ)
    /// </summary>
    public List<string> SupportedCommands { get; set; } = new();

    /// <summary>
    /// Supported baud rates (AJOUTÉ)
    /// </summary>
    public List<int> SupportedBaudRates { get; set; } = new();

    /// <summary>
    /// Supported speed/baud rates (EXISTING)
    /// </summary>
    public List<int> SupportedSpeeds { get; set; } = new();

    /// <summary>
    /// Supported data patterns (EXISTING)
    /// </summary>
    public List<string> SupportedDataPatterns { get; set; } = new();

    /// <summary>
    /// Maximum command length (EXISTING)
    /// </summary>
    public int MaxCommandLength { get; set; } = 1024;

    /// <summary>
    /// Maximum response length (EXISTING)
    /// </summary>
    public int MaxResponseLength { get; set; } = 4096;

    /// <summary>
    /// Supports concurrent sessions (EXISTING)
    /// </summary>
    public bool SupportsConcurrentSessions { get; set; } = false;

    /// <summary>
    /// Maximum concurrent sessions (EXISTING)
    /// </summary>
    public int MaxConcurrentSessions { get; set; } = 1;

    /// <summary>
    /// Protocol-specific features (EXISTING)
    /// </summary>
    public Dictionary<string, object> Features { get; set; } = new();
}

/// <summary>
/// Protocol handler statistics
/// </summary>
public class ProtocolHandlerStatistics
{
    /// <summary>
    /// Current active sessions
    /// </summary>
    public int ActiveSessions { get; set; }

    /// <summary>
    /// Total sessions created
    /// </summary>
    public long TotalSessions { get; set; }

    /// <summary>
    /// Total commands executed
    /// </summary>
    public long TotalCommands { get; set; }

    /// <summary>
    /// Total successful commands
    /// </summary>
    public long SuccessfulCommands { get; set; }

    /// <summary>
    /// Total failed commands
    /// </summary>
    public long FailedCommands { get; set; }

    /// <summary>
    /// Average command duration
    /// </summary>
    public TimeSpan AverageCommandDuration { get; set; }

    /// <summary>
    /// Success rate percentage
    /// </summary>
    public double SuccessRate => TotalCommands > 0 ? 
        (double)SuccessfulCommands / TotalCommands * 100.0 : 0.0;

    /// <summary>
    /// Statistics generation time
    /// </summary>
    public DateTime GeneratedAt { get; set; } = DateTime.Now;

    public override string ToString()
    {
        return $"Sessions: {ActiveSessions}, Commands: {SuccessfulCommands}/{TotalCommands} ({SuccessRate:F1}%), Avg: {AverageCommandDuration.TotalMilliseconds:F0}ms";
    }
}