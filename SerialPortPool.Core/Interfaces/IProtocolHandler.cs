// SerialPortPool.Core/Interfaces/IProtocolHandler.cs - FIXED Sprint 5
using SerialPortPool.Core.Models;

namespace SerialPortPool.Core.Interfaces;

/// <summary>
/// Protocol handler interface for multi-protocol communication system
/// Sprint 5: RS232 implementation
/// Sprint 6: RS485, USB, CAN, I2C, SPI expansion
/// </summary>
public interface IProtocolHandler : IDisposable
{
    /// <summary>
    /// Protocol identifier (e.g., "rs232", "rs485", "usb", "can", "i2c", "spi")
    /// </summary>
    string ProtocolName { get; }

    /// <summary>
    /// Protocol version for compatibility tracking
    /// </summary>
    string ProtocolVersion { get; }

    /// <summary>
    /// Check if this handler can process the specified protocol
    /// </summary>
    /// <param name="protocol">Protocol name to check</param>
    /// <returns>True if handler supports the protocol</returns>
    Task<bool> CanHandleProtocolAsync(string protocol);

    /// <summary>
    /// Open a communication session with the specified configuration
    /// </summary>
    /// <param name="config">Protocol-specific configuration</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Active protocol session</returns>
    Task<ProtocolSession> OpenSessionAsync(
        ProtocolConfiguration config, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Execute a single command in the protocol session
    /// </summary>
    /// <param name="session">Active protocol session</param>
    /// <param name="command">Command to execute</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Command execution result</returns>
    Task<CommandResult> ExecuteCommandAsync(
        ProtocolSession session, 
        ProtocolCommand command,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Execute a sequence of commands (3-phase workflow support)
    /// </summary>
    /// <param name="session">Active protocol session</param>
    /// <param name="commands">Command sequence to execute</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Sequence execution results</returns>
    Task<CommandSequenceResult> ExecuteCommandSequenceAsync(
        ProtocolSession session,
        IEnumerable<ProtocolCommand> commands,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Close and cleanup the protocol session
    /// </summary>
    /// <param name="session">Session to close</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if closed successfully</returns>
    Task<bool> CloseSessionAsync(
        ProtocolSession session, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Test protocol connectivity without opening full session
    /// </summary>
    /// <param name="config">Protocol configuration to test</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Connectivity test result</returns>
    Task<ProtocolTestResult> TestConnectivityAsync(
        ProtocolConfiguration config,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get protocol-specific capabilities and limitations
    /// </summary>
    /// <returns>Protocol capabilities information</returns>
    ProtocolCapabilities GetCapabilities();

    /// <summary>
    /// Get current protocol statistics (session count, error rates, etc.)
    /// </summary>
    /// <returns>Protocol handler statistics</returns>
    ProtocolHandlerStatistics GetStatistics();
}

/// <summary>
/// Protocol command for execution
/// </summary>
public class ProtocolCommand
{
    /// <summary>
    /// Unique command identifier
    /// </summary>
    public string CommandId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Raw command string to send
    /// </summary>
    public string Command { get; set; } = string.Empty;

    /// <summary>
    /// Expected response pattern (regex supported)
    /// </summary>
    public string? ExpectedResponse { get; set; }

    /// <summary>
    /// Command timeout in milliseconds
    /// </summary>
    public int TimeoutMs { get; set; } = 2000;

    /// <summary>
    /// Number of retry attempts on failure
    /// </summary>
    public int RetryCount { get; set; } = 0;

    /// <summary>
    /// Delay between retries in milliseconds
    /// </summary>
    public int RetryDelayMs { get; set; } = 100;

    /// <summary>
    /// Command metadata for tracking and logging
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// Command creation timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    /// <summary>
    /// Create a basic protocol command
    /// </summary>
    /// <param name="command">Command string</param>
    /// <param name="expectedResponse">Expected response pattern</param>
    /// <param name="timeoutMs">Timeout in milliseconds</param>
    /// <returns>Protocol command instance</returns>
    public static ProtocolCommand Create(string command, string? expectedResponse = null, int timeoutMs = 2000)
    {
        return new ProtocolCommand
        {
            Command = command,
            ExpectedResponse = expectedResponse,
            TimeoutMs = timeoutMs
        };
    }

    /// <summary>
    /// Create command with retry configuration
    /// </summary>
    /// <param name="command">Command string</param>
    /// <param name="expectedResponse">Expected response pattern</param>
    /// <param name="timeoutMs">Timeout in milliseconds</param>
    /// <param name="retryCount">Number of retries</param>
    /// <param name="retryDelayMs">Delay between retries</param>
    /// <returns>Protocol command with retry settings</returns>
    public static ProtocolCommand CreateWithRetry(
        string command, 
        string? expectedResponse = null, 
        int timeoutMs = 2000,
        int retryCount = 2,
        int retryDelayMs = 100)
    {
        return new ProtocolCommand
        {
            Command = command,
            ExpectedResponse = expectedResponse,
            TimeoutMs = timeoutMs,
            RetryCount = retryCount,
            RetryDelayMs = retryDelayMs
        };
    }

    public override string ToString()
    {
        var timeout = TimeoutMs != 2000 ? $" (timeout: {TimeoutMs}ms)" : "";
        var retry = RetryCount > 0 ? $" (retry: {RetryCount}x)" : "";
        return $"CMD: {Command.Trim()}{timeout}{retry}";
    }
}

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

/// <summary>
/// Protocol handler capabilities
/// </summary>
public class ProtocolCapabilities
{
    /// <summary>
    /// Supported speed/baud rates
    /// </summary>
    public List<int> SupportedSpeeds { get; set; } = new();

    /// <summary>
    /// Supported data patterns
    /// </summary>
    public List<string> SupportedDataPatterns { get; set; } = new();

    /// <summary>
    /// Maximum command length
    /// </summary>
    public int MaxCommandLength { get; set; } = 1024;

    /// <summary>
    /// Maximum response length
    /// </summary>
    public int MaxResponseLength { get; set; } = 4096;

    /// <summary>
    /// Supports concurrent sessions
    /// </summary>
    public bool SupportsConcurrentSessions { get; set; } = false;

    /// <summary>
    /// Maximum concurrent sessions
    /// </summary>
    public int MaxConcurrentSessions { get; set; } = 1;

    /// <summary>
    /// Protocol-specific features
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