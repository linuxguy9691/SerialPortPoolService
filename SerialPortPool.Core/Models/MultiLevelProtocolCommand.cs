// ===================================================================
// SPRINT 9: MultiLevelProtocolCommand.cs - Enhanced Protocol Commands
// File: SerialPortPool.Core/Models/MultiLevelProtocolCommand.cs  
// Purpose: Protocol commands with multi-level validation + hardware trigger support
// EXTENDS: Sprint 8 ProtocolCommand with 4-level validation system
// ===================================================================

using System.Text.RegularExpressions;

namespace SerialPortPool.Core.Models;

/// <summary>
/// Enhanced protocol command with multi-level validation support + hardware hooks
/// SPRINT 9 ENHANCEMENT: Extends Sprint 8 ProtocolCommand with 4-level validation system
/// CLIENT REQUIREMENTS: PASS/WARN/FAIL/CRITICAL classification with hardware triggers
/// </summary>
public class MultiLevelProtocolCommand : ProtocolCommand
{
    // ✨ NEW SPRINT 9: Multi-level validation patterns
    /// <summary>
    /// Validation patterns for each level (WARN, FAIL, CRITICAL)
    /// KEY: ValidationLevel → REGEX PATTERN
    /// PRIMARY ExpectedResponse is treated as PASS level
    /// </summary>
    public Dictionary<ValidationLevel, string> ValidationPatterns { get; set; } = new();
    
    /// <summary>
    /// Compiled regex patterns for performance (cached)
    /// PERFORMANCE: Pre-compiled regex for fast execution
    /// </summary>
    private readonly Dictionary<ValidationLevel, Regex> _compiledPatterns = new();
    
    // 🔌 NEW SPRINT 9: Hardware integration settings
    /// <summary>
    /// Should CRITICAL validation trigger hardware output?
    /// CLIENT REQUIREMENT: GPIO signal for critical conditions
    /// </summary>
    public bool TriggerHardwareOnCritical { get; set; } = true;
    
    /// <summary>
    /// Should FAIL validation trigger hardware output? (optional/configurable)
    /// FLEXIBILITY: Configurable hardware trigger for FAIL level
    /// </summary>
    public bool TriggerHardwareOnFail { get; set; } = false;
    
    /// <summary>
    /// Hardware trigger timeout (how long to hold signal)
    /// TIMING: Configure signal duration for hardware integration
    /// </summary>
    public TimeSpan HardwareTriggerTimeout { get; set; } = TimeSpan.FromSeconds(2);
    
    // 🔧 Enhanced configuration
    /// <summary>
    /// Pattern matching priority order
    /// DEFAULT: Check CRITICAL → FAIL → WARN → PASS (most severe first)
    /// </summary>
    public ValidationLevel[] PatternMatchingOrder { get; set; } = 
    {
        ValidationLevel.CRITICAL,
        ValidationLevel.FAIL, 
        ValidationLevel.WARN,
        ValidationLevel.PASS
    };
    
    /// <summary>
    /// Whether to stop at first pattern match or check all patterns
    /// DEFAULT: Stop at first match for performance
    /// </summary>
    public bool StopAtFirstMatch { get; set; } = true;
    
    /// <summary>
    /// Validation error handling mode
    /// </summary>
    public ValidationErrorMode ErrorMode { get; set; } = ValidationErrorMode.TreatAsFailure;

    // ✨ SPRINT 9: Enhanced validation method with multi-level support
    /// <summary>
    /// Validate response using multi-level patterns with hardware awareness
    /// CORE METHOD: Multi-level validation with hardware trigger support
    /// </summary>
    public EnhancedValidationResult ValidateResponseMultiLevel(string actualResponse)
    {
        try
        {
            // Ensure patterns are compiled for performance
            EnsurePatternsCompiled();
            
            // Check patterns in priority order (CRITICAL → FAIL → WARN → PASS)
            foreach (var level in PatternMatchingOrder)
            {
                var result = CheckPatternForLevel(level, actualResponse);
                if (result != null)
                {
                    // Found a match - apply hardware trigger logic
                    ApplyHardwareTriggerLogic(result, level);
                    return result;
                }
                
                // Stop at first match if configured
                if (StopAtFirstMatch && result != null)
                    break;
            }
            
            // No patterns matched - default behavior based on error mode
            return HandleNoPatternMatch(actualResponse);
        }
        catch (Exception ex)
        {
            return EnhancedValidationResult.Fail(
                $"Multi-level validation error: {ex.Message}",
                pattern: "ERROR_HANDLING");
        }
    }
    
    /// <summary>
    /// Check if response matches pattern for specific validation level
    /// </summary>
    private EnhancedValidationResult? CheckPatternForLevel(ValidationLevel level, string actualResponse)
    {
        string? pattern = null;
        
        // Get pattern for this level
        if (level == ValidationLevel.PASS)
        {
            // PASS level uses the primary ExpectedResponse
            pattern = ExpectedResponse;
        }
        else
        {
            // Other levels use ValidationPatterns dictionary
            ValidationPatterns.TryGetValue(level, out pattern);
        }
        
        if (string.IsNullOrEmpty(pattern))
            return null; // No pattern configured for this level
        
        // Perform validation (regex or string matching)
        if (IsRegexPattern || ValidationPatterns.ContainsKey(level))
        {
            return ValidateWithRegexForLevel(level, pattern, actualResponse);
        }
        else
        {
            return ValidateWithStringMatchForLevel(level, pattern, actualResponse);
        }
    }
    
    /// <summary>
    /// Validate using regex pattern for specific level
    /// </summary>
    private EnhancedValidationResult? ValidateWithRegexForLevel(ValidationLevel level, string pattern, string actualResponse)
    {
        try
        {
            if (!_compiledPatterns.TryGetValue(level, out var compiledRegex))
                return null; // Pattern not compiled
                
            var match = compiledRegex.Match(actualResponse);
            if (match.Success)
            {
                var message = $"{level} validation passed: regex pattern '{pattern}' matched";
                
                return level switch
                {
                    ValidationLevel.PASS => EnhancedValidationResult.Pass(message, match, pattern),
                    ValidationLevel.WARN => EnhancedValidationResult.Warn(message, match, pattern),
                    ValidationLevel.FAIL => EnhancedValidationResult.Fail(message, match, pattern),
                    ValidationLevel.CRITICAL => EnhancedValidationResult.Critical(message, match, pattern),
                    _ => EnhancedValidationResult.Pass(message, match, pattern)
                };
            }
        }
        catch (Exception ex)
        {
            return EnhancedValidationResult.Fail(
                $"Regex validation error for {level}: {ex.Message}",
                pattern: pattern);
        }
        
        return null; // No match
    }
    
    /// <summary>
    /// Validate using string matching for specific level
    /// </summary>
    private EnhancedValidationResult? ValidateWithStringMatchForLevel(ValidationLevel level, string pattern, string actualResponse)
    {
        if (actualResponse.Trim().Equals(pattern.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            var message = $"{level} validation passed: string match '{pattern}'";
            
            return level switch
            {
                ValidationLevel.PASS => EnhancedValidationResult.Pass(message, pattern: pattern),
                ValidationLevel.WARN => EnhancedValidationResult.Warn(message, pattern: pattern),
                ValidationLevel.FAIL => EnhancedValidationResult.Fail(message, pattern: pattern),
                ValidationLevel.CRITICAL => EnhancedValidationResult.Critical(message, pattern: pattern),
                _ => EnhancedValidationResult.Pass(message, pattern: pattern)
            };
        }
        
        return null; // No match
    }
    
    /// <summary>
    /// Apply hardware trigger logic based on validation level and configuration
    /// </summary>
    private void ApplyHardwareTriggerLogic(EnhancedValidationResult result, ValidationLevel level)
    {
        // Apply hardware trigger settings
        result.ShouldTriggerCriticalOutput = level switch
        {
            ValidationLevel.CRITICAL => TriggerHardwareOnCritical,
            ValidationLevel.FAIL => TriggerHardwareOnFail,
            _ => false
        };
        
        // Add hardware metadata
        if (result.ShouldTriggerCriticalOutput)
        {
            result.ValidationMetadata["HardwareTriggerLevel"] = level.ToString();
            result.ValidationMetadata["HardwareTriggerTimeout"] = HardwareTriggerTimeout.TotalMilliseconds;
            result.ValidationMetadata["HardwareTriggerReason"] = level == ValidationLevel.CRITICAL 
                ? "Critical condition detected" 
                : "Configurable fail condition";
        }
    }
    
    /// <summary>
    /// Handle case where no patterns matched
    /// </summary>
    private EnhancedValidationResult HandleNoPatternMatch(string actualResponse)
    {
        return ErrorMode switch
        {
            ValidationErrorMode.TreatAsFailure => EnhancedValidationResult.Fail(
                $"No validation patterns matched response: '{actualResponse}'",
                pattern: "NO_MATCH"),
                
            ValidationErrorMode.TreatAsWarning => EnhancedValidationResult.Warn(
                $"No validation patterns matched response: '{actualResponse}' - treating as warning",
                pattern: "NO_MATCH"),
                
            ValidationErrorMode.TreatAsPass => EnhancedValidationResult.Pass(
                $"No validation patterns configured, treating as pass: '{actualResponse}'",
                pattern: "NO_PATTERN"),
                
            _ => EnhancedValidationResult.Fail($"Unknown error mode for unmatched response: '{actualResponse}'")
        };
    }
    
    /// <summary>
    /// Ensure all patterns are compiled for performance
    /// </summary>
    private void EnsurePatternsCompiled()
    {
        // Compile PASS pattern (primary ExpectedResponse)
        if (!string.IsNullOrEmpty(ExpectedResponse) && IsRegexPattern)
        {
            CompilePatternForLevel(ValidationLevel.PASS, ExpectedResponse);
        }
        
        // Compile other level patterns
        foreach (var pattern in ValidationPatterns)
        {
            CompilePatternForLevel(pattern.Key, pattern.Value);
        }
    }
    
    /// <summary>
    /// Compile regex pattern for specific level
    /// </summary>
    private void CompilePatternForLevel(ValidationLevel level, string pattern)
    {
        if (string.IsNullOrEmpty(pattern) || _compiledPatterns.ContainsKey(level))
            return;
            
        try
        {
            var compiledRegex = new Regex(pattern, RegexOptions | RegexOptions.Compiled);
            _compiledPatterns[level] = compiledRegex;
        }
        catch (ArgumentException ex)
        {
            // Invalid regex pattern - log error but don't fail
            Metadata[$"RegexError_{level}"] = ex.Message;
        }
    }

    // 🔧 Enhanced configuration methods
    /// <summary>
    /// Add validation pattern for specific level
    /// FLUENT API: Easy configuration of multi-level patterns
    /// </summary>
    public MultiLevelProtocolCommand AddValidationPattern(ValidationLevel level, string pattern)
    {
        ValidationPatterns[level] = pattern;
        
        // Pre-compile if possible
        if (IsRegexPattern)
        {
            CompilePatternForLevel(level, pattern);
        }
        
        return this;
    }
    
    /// <summary>
    /// Configure hardware triggers for validation levels
    /// </summary>
    public MultiLevelProtocolCommand ConfigureHardwareTriggers(bool onCritical = true, bool onFail = false)
    {
        TriggerHardwareOnCritical = onCritical;
        TriggerHardwareOnFail = onFail;
        return this;
    }
    
    /// <summary>
    /// Set pattern matching order
    /// </summary>
    public MultiLevelProtocolCommand SetPatternOrder(params ValidationLevel[] order)
    {
        PatternMatchingOrder = order;
        return this;
    }

    // 🏭 Enhanced factory methods
    /// <summary>
    /// Create multi-level command with all patterns configured
    /// </summary>
    public static MultiLevelProtocolCommand CreateMultiLevel(
        string command,
        string passPattern,
        string? warnPattern = null,
        string? failPattern = null, 
        string? criticalPattern = null,
        int timeoutMs = 2000,
        bool isRegex = true)
    {
        var cmd = new MultiLevelProtocolCommand
        {
            Command = command,
            ExpectedResponse = passPattern, // PASS pattern
            IsRegexPattern = isRegex,
            TimeoutMs = timeoutMs,
            Data = System.Text.Encoding.UTF8.GetBytes(command)
        };
        
        // Add additional level patterns
        if (!string.IsNullOrEmpty(warnPattern))
            cmd.AddValidationPattern(ValidationLevel.WARN, warnPattern);
            
        if (!string.IsNullOrEmpty(failPattern))
            cmd.AddValidationPattern(ValidationLevel.FAIL, failPattern);
            
        if (!string.IsNullOrEmpty(criticalPattern))
            cmd.AddValidationPattern(ValidationLevel.CRITICAL, criticalPattern);
        
        return cmd;
    }
    
    /// <summary>
    /// Create command with hardware integration configured
    /// CLIENT FOCUS: Easy creation of hardware-aware commands
    /// </summary>
    public static MultiLevelProtocolCommand CreateWithHardware(
        string command,
        string passPattern,
        string criticalPattern,
        bool triggerHardwareOnCritical = true,
        int timeoutMs = 2000)
    {
        return CreateMultiLevel(command, passPattern, null, null, criticalPattern, timeoutMs)
            .ConfigureHardwareTriggers(triggerHardwareOnCritical, false);
    }

    // 📊 Enhanced diagnostic methods
    /// <summary>
    /// Get summary of all configured validation patterns
    /// </summary>
    public string GetValidationPatternSummary()
    {
        var patterns = new List<string>();
        
        if (!string.IsNullOrEmpty(ExpectedResponse))
            patterns.Add($"PASS: '{ExpectedResponse}'");
            
        foreach (var pattern in ValidationPatterns.OrderBy(p => p.Key))
        {
            patterns.Add($"{pattern.Key}: '{pattern.Value}'");
        }
        
        var hardware = (TriggerHardwareOnCritical ? "CRITICAL" : "") + 
                      (TriggerHardwareOnFail ? ",FAIL" : "");
        var hardwareInfo = string.IsNullOrEmpty(hardware) ? "" : $" [Hardware: {hardware}]";
        
        return $"Multi-Level Command: {Command.Trim()}{hardwareInfo}\n  " + string.Join("\n  ", patterns);
    }
    
    /// <summary>
    /// Validate all configured patterns for syntax errors
    /// </summary>
    public ValidationPatternValidationResult ValidatePatterns()
    {
        var result = new ValidationPatternValidationResult();
        
        // Validate PASS pattern
        if (!string.IsNullOrEmpty(ExpectedResponse) && IsRegexPattern)
        {
            result.ValidatePattern(ValidationLevel.PASS, ExpectedResponse);
        }
        
        // Validate other patterns
        foreach (var pattern in ValidationPatterns)
        {
            result.ValidatePattern(pattern.Key, pattern.Value);
        }
        
        return result;
    }
    
    public override string ToString()
    {
        var patternCount = ValidationPatterns.Count + (string.IsNullOrEmpty(ExpectedResponse) ? 0 : 1);
        var hardware = TriggerHardwareOnCritical || TriggerHardwareOnFail ? "🔌" : "";
        return $"Multi-Level CMD: {Command.Trim()} {hardware}({patternCount} patterns)";
    }
}

/// <summary>
/// Validation error handling modes for unmatched responses
/// </summary>
public enum ValidationErrorMode
{
    /// <summary>
    /// Treat unmatched responses as FAIL validation
    /// </summary>
    TreatAsFailure,
    
    /// <summary>
    /// Treat unmatched responses as WARN validation  
    /// </summary>
    TreatAsWarning,
    
    /// <summary>
    /// Treat unmatched responses as PASS validation (permissive)
    /// </summary>
    TreatAsPass
}

/// <summary>
/// Result of pattern validation (syntax checking)
/// </summary>
public class ValidationPatternValidationResult
{
    private readonly List<PatternValidationError> _errors = new();
    
    public IReadOnlyList<PatternValidationError> Errors => _errors.AsReadOnly();
    public bool IsValid => !_errors.Any();
    
    public void ValidatePattern(ValidationLevel level, string pattern)
    {
        if (string.IsNullOrEmpty(pattern))
            return;
            
        try
        {
            // Test regex compilation
            _ = new Regex(pattern, RegexOptions.Compiled);
        }
        catch (ArgumentException ex)
        {
            _errors.Add(new PatternValidationError
            {
                Level = level,
                Pattern = pattern,
                ErrorMessage = ex.Message
            });
        }
    }
    
    public string GetSummary()
    {
        if (IsValid)
            return "✅ All validation patterns are valid";
            
        return $"❌ {_errors.Count} pattern validation errors:\n" +
               string.Join("\n", _errors.Select(e => $"  {e.Level}: {e.ErrorMessage}"));
    }
}

/// <summary>
/// Individual pattern validation error
/// </summary>
public class PatternValidationError
{
    public ValidationLevel Level { get; set; }
    public string Pattern { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    
    public override string ToString()
    {
        return $"{Level} pattern error: {ErrorMessage}";
    }
}