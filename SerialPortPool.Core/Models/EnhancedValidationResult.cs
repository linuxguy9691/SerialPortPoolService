// ===================================================================
// SPRINT 9: EnhancedValidationResult.cs - Multi-Level Validation Results
// File: SerialPortPool.Core/Models/EnhancedValidationResult.cs
// Purpose: Enhanced validation result with multi-level classification + hardware hooks
// EXTENDS: Sprint 8 regex validation with severity levels + GPIO integration
// ===================================================================

using System.Text.RegularExpressions;

namespace SerialPortPool.Core.Models;

/// <summary>
/// Enhanced validation result with multi-level classification + hardware hooks
/// SPRINT 9 ENHANCEMENT: Extends Sprint 8 regex validation with severity levels + GPIO integration
/// CLIENT REQUIREMENTS: 4-level system with hardware notification capabilities
/// </summary>
public class EnhancedValidationResult
{
    /// <summary>
    /// Validation level classification (PASS, WARN, FAIL, CRITICAL)
    /// </summary>
    public ValidationLevel Level { get; set; } = ValidationLevel.PASS;
    
    /// <summary>
    /// Human-readable validation message
    /// </summary>
    public string Message { get; set; } = string.Empty;
    
    /// <summary>
    /// Regex match result (if regex validation was used)
    /// INHERITED FROM SPRINT 8: Preserves existing regex functionality
    /// </summary>
    public Match? RegexMatch { get; set; }
    
    /// <summary>
    /// Captured regex groups as key-value pairs
    /// INHERITED FROM SPRINT 8: Enhanced group capturing
    /// </summary>
    public Dictionary<string, string> CapturedGroups { get; set; } = new();
    
    /// <summary>
    /// The regex pattern that matched (for reference)
    /// NEW SPRINT 9: Track which pattern matched for multi-level scenarios
    /// </summary>
    public string MatchedPattern { get; set; } = string.Empty;
    
    /// <summary>
    /// Actual response that was validated
    /// </summary>
    public string ActualResponse { get; set; } = string.Empty;
    
    /// <summary>
    /// When this validation was performed
    /// </summary>
    public DateTime ValidatedAt { get; set; } = DateTime.Now;
    
    /// <summary>
    /// Type of validation that was performed
    /// INHERITED FROM SPRINT 8: StringMatch or RegexMatch
    /// </summary>
    public ValidationType ValidationMethod { get; set; } = ValidationType.StringMatch;
    
    /// <summary>
    /// Additional validation metadata
    /// </summary>
    public Dictionary<string, object> ValidationMetadata { get; set; } = new();

    // ✨ NEW SPRINT 9: Smart workflow control properties
    /// <summary>
    /// Should the workflow continue after this validation?
    /// SMART LOGIC: PASS/WARN continue, FAIL/CRITICAL stop
    /// </summary>
    public bool ShouldContinueWorkflow => Level.ShouldContinueWorkflow();
    
    /// <summary>
    /// Does this validation require immediate attention?
    /// CRITICAL ONLY: For emergency response procedures
    /// </summary>
    public bool RequiresImmediateAttention => Level.RequiresImmediateAttention();
    
    /// <summary>
    /// Should this validation trigger enhanced logging/alerting?
    /// ALL EXCEPT PASS: Enhanced visibility for issues
    /// </summary>
    public bool ShouldLogAlert => Level.ShouldLogAlert();
    
    // 🔌 NEW SPRINT 9: Hardware integration properties
    /// <summary>
    /// Should this validation trigger critical hardware output?
    /// CLIENT REQUIREMENT: GPIO signal for critical conditions
    /// </summary>
    public bool ShouldTriggerCriticalOutput { get; set; } = false;
    
    /// <summary>
    /// Should this validation trigger any hardware notification?
    /// CONFIGURABLE: Based on level + configuration
    /// </summary>
    public bool RequiresHardwareNotification => ShouldTriggerCriticalOutput || Level == ValidationLevel.CRITICAL;
    
    /// <summary>
    /// Hardware trigger configuration override
    /// FLEXIBILITY: Allow manual control of hardware triggers
    /// </summary>
    public bool HardwareTriggerOverride { get; set; } = false;

    // ✨ SPRINT 9: Factory methods for enhanced validation results
    
    /// <summary>
    /// Create a PASS validation result
    /// </summary>
    public static EnhancedValidationResult Pass(string message, Match? regexMatch = null, string pattern = "")
    {
        var result = new EnhancedValidationResult
        {
            Level = ValidationLevel.PASS,
            Message = message,
            RegexMatch = regexMatch,
            MatchedPattern = pattern,
            ValidationMethod = regexMatch != null ? ValidationType.RegexMatch : ValidationType.StringMatch
        };
        
        ExtractRegexGroups(result, regexMatch);
        return result;
    }
    
    /// <summary>
    /// Create a WARN validation result
    /// </summary>
    public static EnhancedValidationResult Warn(string message, Match? regexMatch = null, string pattern = "")
    {
        var result = new EnhancedValidationResult
        {
            Level = ValidationLevel.WARN,
            Message = message,
            RegexMatch = regexMatch,
            MatchedPattern = pattern,
            ValidationMethod = regexMatch != null ? ValidationType.RegexMatch : ValidationType.StringMatch
        };
        
        ExtractRegexGroups(result, regexMatch);
        return result;
    }
    
    /// <summary>
    /// Create a FAIL validation result
    /// </summary>
    public static EnhancedValidationResult Fail(string message, Match? regexMatch = null, string pattern = "", bool triggerHardware = false)
    {
        var result = new EnhancedValidationResult
        {
            Level = ValidationLevel.FAIL,
            Message = message,
            RegexMatch = regexMatch,
            MatchedPattern = pattern,
            ShouldTriggerCriticalOutput = triggerHardware,
            ValidationMethod = regexMatch != null ? ValidationType.RegexMatch : ValidationType.StringMatch
        };
        
        ExtractRegexGroups(result, regexMatch);
        return result;
    }
    
    /// <summary>
    /// Create a CRITICAL validation result
    /// CLIENT REQUIREMENT: Hardware trigger by default for critical conditions
    /// </summary>
    public static EnhancedValidationResult Critical(string message, Match? regexMatch = null, string pattern = "", bool triggerHardware = true)
    {
        var result = new EnhancedValidationResult
        {
            Level = ValidationLevel.CRITICAL,
            Message = message,
            RegexMatch = regexMatch,
            MatchedPattern = pattern,
            ShouldTriggerCriticalOutput = triggerHardware,
            ValidationMethod = regexMatch != null ? ValidationType.RegexMatch : ValidationType.StringMatch
        };
        
        ExtractRegexGroups(result, regexMatch);
        return result;
    }

    // 🔄 SPRINT 8 COMPATIBILITY: Support existing CommandValidationResult creation
    /// <summary>
    /// Create from legacy CommandValidationResult for backward compatibility
    /// SPRINT 8 COMPATIBILITY: Seamless upgrade path
    /// </summary>
    public static EnhancedValidationResult FromLegacyResult(CommandValidationResult legacyResult)
    {
        var level = legacyResult.IsValid ? ValidationLevel.PASS : ValidationLevel.FAIL;
        
        return new EnhancedValidationResult
        {
            Level = level,
            Message = legacyResult.Message,
            RegexMatch = legacyResult.RegexMatch,
            CapturedGroups = new Dictionary<string, string>(legacyResult.CapturedGroups),
            ActualResponse = legacyResult.ActualResponse ?? "",
            MatchedPattern = legacyResult.ExpectedPattern ?? "",
            ValidationMethod = legacyResult.ValidationMethod,
            ValidatedAt = legacyResult.ValidatedAt,
            ValidationMetadata = new Dictionary<string, object>(legacyResult.ValidationMetadata)
        };
    }
    
    /// <summary>
    /// Convert to legacy CommandValidationResult for backward compatibility
    /// </summary>
    public CommandValidationResult ToLegacyResult()
    {
        return new CommandValidationResult
        {
            IsValid = Level == ValidationLevel.PASS || Level == ValidationLevel.WARN,
            Message = Message,
            RegexMatch = RegexMatch,
            CapturedGroups = new Dictionary<string, string>(CapturedGroups),
            ValidatedAt = ValidatedAt,
            ValidationMethod = ValidationMethod,
            ExpectedPattern = MatchedPattern,
            ActualResponse = ActualResponse,
            ValidationMetadata = new Dictionary<string, object>(ValidationMetadata)
        };
    }

    // 🔧 Helper methods
    /// <summary>
    /// Extract regex groups from match into CapturedGroups dictionary
    /// INHERITED FROM SPRINT 8: Enhanced group extraction
    /// </summary>
    private static void ExtractRegexGroups(EnhancedValidationResult result, Match? regexMatch)
    {
        if (regexMatch?.Success == true)
        {
            // Named groups
            foreach (Group group in regexMatch.Groups)
            {
                if (!string.IsNullOrEmpty(group.Name) && group.Name != "0" && group.Success)
                {
                    result.CapturedGroups[group.Name] = group.Value;
                }
            }
            
            // Numbered groups for convenience
            for (int i = 1; i < regexMatch.Groups.Count; i++)
            {
                if (regexMatch.Groups[i].Success)
                {
                    result.CapturedGroups[$"Group{i}"] = regexMatch.Groups[i].Value;
                }
            }
            
            // Add metadata
            result.ValidationMetadata["RegexMatchIndex"] = regexMatch.Index;
            result.ValidationMetadata["RegexMatchLength"] = regexMatch.Length;
            result.ValidationMetadata["RegexMatchValue"] = regexMatch.Value;
            result.ValidationMetadata["RegexGroupCount"] = regexMatch.Groups.Count - 1;
        }
    }
    
    /// <summary>
    /// Get validation summary for logging
    /// ENHANCED: Includes hardware trigger status
    /// </summary>
    public string GetSummary()
    {
        var icon = Level.GetIcon();
        var method = ValidationMethod == ValidationType.RegexMatch ? "REGEX" : "STRING";
        var groups = CapturedGroups.Any() ? $" ({CapturedGroups.Count} groups)" : "";
        var hardware = ShouldTriggerCriticalOutput ? " 🔌💥" : "";
        
        return $"{icon} {Level} [{method}]: {Message}{groups}{hardware}";
    }
    
    /// <summary>
    /// Get detailed validation information including hardware status
    /// </summary>
    public string GetDetailedInfo()
    {
        var info = new List<string> { GetSummary() };
        
        if (!string.IsNullOrEmpty(MatchedPattern))
        {
            info.Add($"Pattern: '{MatchedPattern}'");
        }
        
        if (!string.IsNullOrEmpty(ActualResponse))
        {
            info.Add($"Response: '{ActualResponse}'");
        }
        
        if (CapturedGroups.Any())
        {
            info.Add("Captured Groups:");
            foreach (var group in CapturedGroups)
            {
                info.Add($"  {group.Key}: '{group.Value}'");
            }
        }
        
        // Hardware status
        if (RequiresHardwareNotification)
        {
            info.Add($"🔌 Hardware Action: {(ShouldTriggerCriticalOutput ? "Critical Output Signal" : "Notification Required")}");
        }
        
        // Workflow control
        info.Add($"Workflow: {(ShouldContinueWorkflow ? "Continue" : "Stop")}");
        
        if (RequiresImmediateAttention)
        {
            info.Add("🚨 IMMEDIATE ATTENTION REQUIRED");
        }
        
        return string.Join("\n", info);
    }
    
    /// <summary>
    /// Get hardware action summary
    /// </summary>
    public string GetHardwareActionSummary()
    {
        if (!RequiresHardwareNotification)
            return "No hardware action required";
            
        var actions = new List<string>();
        
        if (ShouldTriggerCriticalOutput)
            actions.Add("Critical Fail Output Signal");
            
        if (Level == ValidationLevel.CRITICAL)
            actions.Add("Emergency Hardware Notification");
            
        return string.Join(", ", actions);
    }
    
    public override string ToString()
    {
        return GetSummary();
    }
    
    public override bool Equals(object? obj)
    {
        return obj is EnhancedValidationResult other && 
               Level == other.Level && 
               Message == other.Message &&
               ActualResponse == other.ActualResponse;
    }
    
    public override int GetHashCode()
    {
        return HashCode.Combine(Level, Message, ActualResponse);
    }
}

/// <summary>
/// Enhanced validation result builder for fluent API
/// CONVENIENCE: Fluent construction of complex validation results
/// </summary>
public class EnhancedValidationResultBuilder
{
    private ValidationLevel _level = ValidationLevel.PASS;
    private string _message = string.Empty;
    private Match? _regexMatch;
    private string _pattern = string.Empty;
    private string _actualResponse = string.Empty;
    private bool _triggerHardware = false;
    private readonly Dictionary<string, object> _metadata = new();
    
    public EnhancedValidationResultBuilder WithLevel(ValidationLevel level)
    {
        _level = level;
        return this;
    }
    
    public EnhancedValidationResultBuilder WithMessage(string message)
    {
        _message = message;
        return this;
    }
    
    public EnhancedValidationResultBuilder WithRegexMatch(Match regexMatch, string pattern)
    {
        _regexMatch = regexMatch;
        _pattern = pattern;
        return this;
    }
    
    public EnhancedValidationResultBuilder WithResponse(string actualResponse)
    {
        _actualResponse = actualResponse;
        return this;
    }
    
    public EnhancedValidationResultBuilder WithHardwareTrigger(bool triggerHardware = true)
    {
        _triggerHardware = triggerHardware;
        return this;
    }
    
    public EnhancedValidationResultBuilder WithMetadata(string key, object value)
    {
        _metadata[key] = value;
        return this;
    }
    
    public EnhancedValidationResult Build()
    {
        var result = _level switch
        {
            ValidationLevel.PASS => EnhancedValidationResult.Pass(_message, _regexMatch, _pattern),
            ValidationLevel.WARN => EnhancedValidationResult.Warn(_message, _regexMatch, _pattern),
            ValidationLevel.FAIL => EnhancedValidationResult.Fail(_message, _regexMatch, _pattern, _triggerHardware),
            ValidationLevel.CRITICAL => EnhancedValidationResult.Critical(_message, _regexMatch, _pattern, _triggerHardware),
            _ => EnhancedValidationResult.Pass(_message, _regexMatch, _pattern)
        };
        
        result.ActualResponse = _actualResponse;
        
        foreach (var metadata in _metadata)
        {
            result.ValidationMetadata[metadata.Key] = metadata.Value;
        }
        
        return result;
    }
}

/// <summary>
/// Collection of enhanced validation results with aggregate analysis
/// ANALYSIS: Multiple validation results with overall assessment
/// </summary>
public class EnhancedValidationResultCollection
{
    private readonly List<EnhancedValidationResult> _results = new();
    
    /// <summary>
    /// All validation results in this collection
    /// </summary>
    public IReadOnlyList<EnhancedValidationResult> Results => _results.AsReadOnly();
    
    /// <summary>
    /// Add a validation result to the collection
    /// </summary>
    public void Add(EnhancedValidationResult result)
    {
        _results.Add(result);
    }
    
    /// <summary>
    /// Overall validation level (most severe in collection)
    /// </summary>
    public ValidationLevel OverallLevel => 
        ValidationLevelHelper.GetMostSevereLevel(_results.Select(r => r.Level));
    
    /// <summary>
    /// Should workflow continue based on overall assessment?
    /// </summary>
    public bool ShouldContinueWorkflow => OverallLevel.ShouldContinueWorkflow();
    
    /// <summary>
    /// Any results requiring hardware notification?
    /// </summary>
    public bool RequiresHardwareNotification => 
        _results.Any(r => r.RequiresHardwareNotification);
    
    /// <summary>
    /// Any results requiring immediate attention?
    /// </summary>
    public bool RequiresImmediateAttention => 
        _results.Any(r => r.RequiresImmediateAttention);
    
    /// <summary>
    /// Get validation statistics for this collection
    /// </summary>
    public ValidationLevelStatistics GetStatistics()
    {
        return ValidationLevelHelper.GetStatistics(_results.Select(r => r.Level));
    }
    
    /// <summary>
    /// Get summary of all validation results
    /// </summary>
    public string GetSummary()
    {
        var stats = GetStatistics();
        var hardwareIcon = RequiresHardwareNotification ? "🔌" : "";
        return $"{OverallLevel.GetIcon()} {hardwareIcon} Overall: {OverallLevel} - {stats}";
    }
    
    public override string ToString()
    {
        return GetSummary();
    }
}