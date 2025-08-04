// SerialPortPool.Core/Models/ProtocolCommand.cs - VERSION CORRIGÉE COMPLÈTE
namespace SerialPortPool.Core.Models;

/// <summary>
/// Protocol command for execution in communication workflows
/// Week 2: Core model for 3-phase workflows (Start/Test/Stop)
/// FIXED: Added missing properties for RS232ProtocolHandler
/// </summary>
public class ProtocolCommand
{
    /// <summary>
    /// Unique command identifier
    /// </summary>
    public string CommandId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Raw command string to send (FIXED: Added Name property alias)
    /// </summary>
    public string Command { get; set; } = string.Empty;

    /// <summary>
    /// Command name alias for compatibility (AJOUTÉ)
    /// </summary>
    public string Name { get => Command; set => Command = value; }

    /// <summary>
    /// Command data as byte array (AJOUTÉ)
    /// </summary>
    public byte[] Data { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// Command parameters dictionary (AJOUTÉ)
    /// </summary>
    public Dictionary<string, object> Parameters { get; set; } = new();

    /// <summary>
    /// Expected response pattern (regex supported)
    /// </summary>
    public string? ExpectedResponse { get; set; }

    /// <summary>
    /// Command timeout in milliseconds
    /// </summary>
    public int TimeoutMs { get; set; } = 2000;

    /// <summary>
    /// Command timeout as TimeSpan (AJOUTÉ pour compatibility)
    /// </summary>
    public TimeSpan Timeout 
    { 
        get => TimeSpan.FromMilliseconds(TimeoutMs); 
        set => TimeoutMs = (int)value.TotalMilliseconds; 
    }

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
            TimeoutMs = timeoutMs,
            Data = System.Text.Encoding.UTF8.GetBytes(command) // AJOUTÉ: Auto-convert to bytes
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
            RetryDelayMs = retryDelayMs,
            Data = System.Text.Encoding.UTF8.GetBytes(command) // AJOUTÉ
        };
    }

    public override string ToString()
    {
        var timeout = TimeoutMs != 2000 ? $" (timeout: {TimeoutMs}ms)" : "";
        var retry = RetryCount > 0 ? $" (retry: {RetryCount}x)" : "";
        return $"CMD: {Command.Trim()}{timeout}{retry}";
    }
}