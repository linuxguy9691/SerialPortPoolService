// ===================================================================
// SPRINT 8 NEW: CommandValidationResult Model
// File: SerialPortPool.Core/Models/CommandValidationResult.cs
// Purpose: Result model for command response validation (string + regex)
// ===================================================================

using System.Text.RegularExpressions;

namespace SerialPortPool.Core.Models;

/// <summary>
/// Result of command response validation (string matching or regex)
/// SPRINT 8 FEATURE: Supports both simple string matching and regex validation
/// </summary>
public class CommandValidationResult
{
    /// <summary>
    /// Whether the validation passed
    /// </summary>
    public bool IsValid { get; set; }
    
    /// <summary>
    /// Validation result message
    /// </summary>
    public string Message { get; set; } = string.Empty;
    
    /// <summary>
    /// Regex match result (if regex validation was used)
    /// </summary>
    public Match? RegexMatch { get; set; }
    
    /// <summary>
    /// Captured regex groups as key-value pairs
    /// </summary>
    public Dictionary<string, string> CapturedGroups { get; set; } = new();
    
    /// <summary>
    /// When this validation was performed
    /// </summary>
    public DateTime ValidatedAt { get; set; } = DateTime.Now;
    
    /// <summary>
    /// Type of validation that was performed
    /// </summary>
    public ValidationType ValidationMethod { get; set; } = ValidationType.StringMatch;
    
    /// <summary>
    /// Original expected pattern/string
    /// </summary>
    public string? ExpectedPattern { get; set; }
    
    /// <summary>
    /// Actual response that was validated
    /// </summary>
    public string? ActualResponse { get; set; }
    
    /// <summary>
    /// Additional validation metadata
    /// </summary>
    public Dictionary<string, object> ValidationMetadata { get; set; } = new();

    /// <summary>
    /// Create successful validation result
    /// </summary>
    public static CommandValidationResult Success(string message, Match? regexMatch = null)
    {
        var result = new CommandValidationResult
        {
            IsValid = true,
            Message = message,
            RegexMatch = regexMatch,
            ValidationMethod = regexMatch != null ? ValidationType.RegexMatch : ValidationType.StringMatch
        };
        
        // Extract regex groups if available
        if (regexMatch?.Success == true)
        {
            foreach (Group group in regexMatch.Groups)
            {
                // Skip the full match group (index 0)
                if (!string.IsNullOrEmpty(group.Name) && group.Name != "0" && group.Success)
                {
                    result.CapturedGroups[group.Name] = group.Value;
                }
            }
            
            // Also add numbered groups for convenience
            for (int i = 1; i < regexMatch.Groups.Count; i++)
            {
                if (regexMatch.Groups[i].Success)
                {
                    result.CapturedGroups[$"Group{i}"] = regexMatch.Groups[i].Value;
                }
            }
        }
        
        return result;
    }
    
    /// <summary>
    /// Create failed validation result
    /// </summary>
    public static CommandValidationResult Failure(string message)
    {
        return new CommandValidationResult
        {
            IsValid = false,
            Message = message,
            ValidationMethod = ValidationType.StringMatch
        };
    }
    
    /// <summary>
    /// Create regex-specific successful result
    /// </summary>
    public static CommandValidationResult RegexSuccess(string pattern, string response, Match match)
    {
        var result = Success($"Regex pattern matched: '{pattern}'", match);
        result.ExpectedPattern = pattern;
        result.ActualResponse = response;
        result.ValidationMethod = ValidationType.RegexMatch;
        
        // Add detailed regex metadata
        result.ValidationMetadata["RegexPattern"] = pattern;
        result.ValidationMetadata["MatchIndex"] = match.Index;
        result.ValidationMetadata["MatchLength"] = match.Length;
        result.ValidationMetadata["MatchValue"] = match.Value;
        result.ValidationMetadata["GroupCount"] = match.Groups.Count - 1; // Exclude full match
        
        return result;
    }
    
    /// <summary>
    /// Create regex-specific failure result
    /// </summary>
    public static CommandValidationResult RegexFailure(string pattern, string response)
    {
        var result = Failure($"Regex pattern '{pattern}' did not match response '{response}'");
        result.ExpectedPattern = pattern;
        result.ActualResponse = response;
        result.ValidationMethod = ValidationType.RegexMatch;
        
        return result;
    }
    
    /// <summary>
    /// Create string matching specific successful result
    /// </summary>
    public static CommandValidationResult StringSuccess(string expected, string actual)
    {
        var result = Success($"String match successful: '{expected}'");
        result.ExpectedPattern = expected;
        result.ActualResponse = actual;
        result.ValidationMethod = ValidationType.StringMatch;
        
        return result;
    }
    
    /// <summary>
    /// Create string matching specific failure result
    /// </summary>
    public static CommandValidationResult StringFailure(string expected, string actual)
    {
        var result = Failure($"Expected '{expected}' but got '{actual}'");
        result.ExpectedPattern = expected;
        result.ActualResponse = actual;
        result.ValidationMethod = ValidationType.StringMatch;
        
        return result;
    }
    
    /// <summary>
    /// Get validation summary for logging
    /// </summary>
    public string GetSummary()
    {
        var status = IsValid ? "✅ VALID" : "❌ INVALID";
        var method = ValidationMethod == ValidationType.RegexMatch ? "REGEX" : "STRING";
        var groups = CapturedGroups.Any() ? $" ({CapturedGroups.Count} groups)" : "";
        
        return $"{status} [{method}]: {Message}{groups}";
    }
    
    /// <summary>
    /// Get detailed validation information
    /// </summary>
    public string GetDetailedInfo()
    {
        var info = new List<string> { GetSummary() };
        
        if (!string.IsNullOrEmpty(ExpectedPattern))
        {
            info.Add($"Expected: '{ExpectedPattern}'");
        }
        
        if (!string.IsNullOrEmpty(ActualResponse))
        {
            info.Add($"Actual: '{ActualResponse}'");
        }
        
        if (CapturedGroups.Any())
        {
            info.Add("Captured Groups:");
            foreach (var group in CapturedGroups)
            {
                info.Add($"  {group.Key}: '{group.Value}'");
            }
        }
        
        return string.Join("\n", info);
    }
    
    public override string ToString()
    {
        return GetSummary();
    }
}

/// <summary>
/// Type of validation that was performed
/// </summary>
public enum ValidationType
{
    /// <summary>
    /// Simple string matching (case-insensitive)
    /// </summary>
    StringMatch,
    
    /// <summary>
    /// Regular expression pattern matching
    /// </summary>
    RegexMatch,
    
    /// <summary>
    /// No validation required
    /// </summary>
    None
}