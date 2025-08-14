// ===================================================================
// SPRINT 8 REGEX ENHANCEMENT: Enhanced ProtocolCommand Model
// File: SerialPortPool.Core/Models/ProtocolCommand.cs
// Purpose: Add regex validation support to existing ProtocolCommand
// ===================================================================

using System.Text.RegularExpressions;

namespace SerialPortPool.Core.Models;

/// <summary>
/// Protocol command for execution in communication workflows
/// SPRINT 8 ENHANCED: Added regex validation support
/// BACKWARD COMPATIBLE: All existing functionality preserved
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
    /// Command name alias for compatibility
    /// </summary>
    public string Name { get => Command; set => Command = value; }

    /// <summary>
    /// Command data as byte array
    /// </summary>
    public byte[] Data { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// Command parameters dictionary
    /// </summary>
    public Dictionary<string, object> Parameters { get; set; } = new();

    /// <summary>
    /// Expected response pattern (supports both string and regex)
    /// </summary>
    public string? ExpectedResponse { get; set; }

    /// <summary>
    /// Command timeout in milliseconds
    /// </summary>
    public int TimeoutMs { get; set; } = 2000;

    /// <summary>
    /// Command timeout as TimeSpan (for compatibility)
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

    // ✨ SPRINT 8 NEW: Regex validation support
    /// <summary>
    /// Whether ExpectedResponse should be treated as regex pattern
    /// </summary>
    public bool IsRegexPattern { get; set; } = false;

    /// <summary>
    /// Regex options for pattern matching
    /// </summary>
    public RegexOptions RegexOptions { get; set; } = RegexOptions.None;

    /// <summary>
    /// Error message if regex pattern is invalid
    /// </summary>
    public string? RegexValidationError { get; set; }

    // ✨ SPRINT 8: Compiled regex for performance
    private Regex? _compiledRegex;
    public Regex? CompiledRegex 
    { 
        get
        {
            if (_compiledRegex == null && IsRegexPattern && !string.IsNullOrEmpty(ExpectedResponse))
            {
                try
                {
                    _compiledRegex = new Regex(ExpectedResponse, RegexOptions | RegexOptions.Compiled);
                    RegexValidationError = null; // Clear any previous error
                }
                catch (ArgumentException ex)
                {
                    RegexValidationError = ex.Message;
                    return null;
                }
            }
            return _compiledRegex;
        }
    }

    /// <summary>
    /// Command metadata for tracking and logging
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// Command creation timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // ✨ SPRINT 8: Enhanced validation method with regex support
    /// <summary>
    /// Validate response using string matching or regex pattern
    /// </summary>
    public CommandValidationResult ValidateResponse(string actualResponse)
    {
        if (string.IsNullOrEmpty(ExpectedResponse))
        {
            return CommandValidationResult.Success("No validation required");
        }
        
        if (IsRegexPattern)
        {
            return ValidateWithRegex(actualResponse);
        }
        else
        {
            return ValidateWithStringMatch(actualResponse);
        }
    }

    /// <summary>
    /// Validate response using regex pattern
    /// </summary>
    private CommandValidationResult ValidateWithRegex(string actualResponse)
    {
        if (CompiledRegex == null)
        {
            return CommandValidationResult.Failure($"Invalid regex pattern: {RegexValidationError}");
        }
        
        var match = CompiledRegex.Match(actualResponse);
        if (match.Success)
        {
            return CommandValidationResult.Success($"Regex pattern '{ExpectedResponse}' matched successfully", match);
        }
        else
        {
            return CommandValidationResult.Failure(
                $"Regex pattern '{ExpectedResponse}' did not match response '{actualResponse}'");
        }
    }

    /// <summary>
    /// Validate response using simple string matching (backward compatibility)
    /// </summary>
    private CommandValidationResult ValidateWithStringMatch(string actualResponse)
    {
        if (actualResponse.Trim().Equals(ExpectedResponse?.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            return CommandValidationResult.Success("String match successful");
        }
        else
        {
            return CommandValidationResult.Failure(
                $"Expected '{ExpectedResponse}' but got '{actualResponse}'");
        }
    }

    /// <summary>
    /// Create a basic protocol command (backward compatibility)
    /// </summary>
    public static ProtocolCommand Create(string command, string? expectedResponse = null, int timeoutMs = 2000)
    {
        return new ProtocolCommand
        {
            Command = command,
            ExpectedResponse = expectedResponse,
            TimeoutMs = timeoutMs,
            Data = System.Text.Encoding.UTF8.GetBytes(command)
        };
    }

    /// <summary>
    /// Create command with regex pattern validation
    /// </summary>
    public static ProtocolCommand CreateWithRegex(
        string command, 
        string regexPattern, 
        int timeoutMs = 2000,
        RegexOptions regexOptions = RegexOptions.None)
    {
        return new ProtocolCommand
        {
            Command = command,
            ExpectedResponse = regexPattern,
            IsRegexPattern = true,
            RegexOptions = regexOptions,
            TimeoutMs = timeoutMs,
            Data = System.Text.Encoding.UTF8.GetBytes(command)
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
            Data = System.Text.Encoding.UTF8.GetBytes(command)
        };
    }

    public override string ToString()
    {
        var timeout = TimeoutMs != 2000 ? $" (timeout: {TimeoutMs}ms)" : "";
        var retry = RetryCount > 0 ? $" (retry: {RetryCount}x)" : "";
        var regexInfo = IsRegexPattern ? " [REGEX]" : "";
        return $"CMD: {Command.Trim()}{regexInfo}{timeout}{retry}";
    }
}