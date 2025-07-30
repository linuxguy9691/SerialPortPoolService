// SerialPortPool.Core/Models/ProtocolCommand.cs - NEW (Missing model)
namespace SerialPortPool.Core.Models;

/// <summary>
/// Protocol command for execution in communication workflows
/// Week 2: Core model for 3-phase workflows (Start/Test/Stop)
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