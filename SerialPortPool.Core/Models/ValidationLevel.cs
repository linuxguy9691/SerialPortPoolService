// ===================================================================
// SPRINT 9: ValidationLevel.cs - Multi-Level Validation Classification
// File: SerialPortPool.Core/Models/ValidationLevel.cs
// Purpose: 4-level validation system for enhanced UUT response analysis
// Client Requirements: PASS, WARN, FAIL, CRITICAL classification
// ===================================================================

namespace SerialPortPool.Core.Models;

/// <summary>
/// Multi-level validation classification for enhanced UUT response analysis
/// SPRINT 9 CLIENT REQUESTED: 4-level system for professional production environments
/// 
/// HIERARCHY: PASS (continue) → WARN (continue with alert) → FAIL (stop) → CRITICAL (emergency stop + hardware)
/// </summary>
public enum ValidationLevel
{
    /// <summary>
    /// ✅ SUCCESS: Validation passed completely
    /// WORKFLOW: Continue normally
    /// LOGGING: Information level
    /// HARDWARE: No action required
    /// </summary>
    PASS = 0,
    
    /// <summary>
    /// ⚠️ WARNING: Validation passed but with concerns
    /// WORKFLOW: Continue but log alert for monitoring
    /// LOGGING: Warning level with enhanced detail
    /// HARDWARE: Optional notification (configurable)
    /// EXAMPLES: "SLOW_START", "TEMP_HIGH", "LOW_BATTERY"
    /// </summary>
    WARN = 1,
    
    /// <summary>
    /// ❌ FAILURE: Validation failed, workflow should stop
    /// WORKFLOW: Stop execution with error reporting
    /// LOGGING: Error level with full context
    /// HARDWARE: Optional error signaling
    /// EXAMPLES: "ERROR", "FAIL", "TIMEOUT"
    /// </summary>
    FAIL = 2,
    
    /// <summary>
    /// 🚨 CRITICAL: Validation failed critically, emergency stop required
    /// WORKFLOW: Emergency stop + immediate attention
    /// LOGGING: Critical level + immediate alerting
    /// HARDWARE: Trigger critical fail output signal (client requirement)
    /// EXAMPLES: "CRITICAL", "EMERGENCY", "SHUTDOWN", "OVERVOLTAGE", "SAFETY_VIOLATION"
    /// </summary>
    CRITICAL = 3
}

/// <summary>
/// Extension methods for ValidationLevel enum to provide rich functionality
/// </summary>
public static class ValidationLevelExtensions
{
    /// <summary>
    /// Should the workflow continue after this validation level?
    /// PASS/WARN: Continue, FAIL/CRITICAL: Stop
    /// </summary>
    public static bool ShouldContinueWorkflow(this ValidationLevel level)
    {
        return level == ValidationLevel.PASS || level == ValidationLevel.WARN;
    }
    
    /// <summary>
    /// Does this validation level require immediate attention?
    /// Only CRITICAL requires immediate attention
    /// </summary>
    public static bool RequiresImmediateAttention(this ValidationLevel level)
    {
        return level == ValidationLevel.CRITICAL;
    }
    
    /// <summary>
    /// Should this validation level trigger enhanced logging/alerting?
    /// Everything except PASS should be logged with enhanced detail
    /// </summary>
    public static bool ShouldLogAlert(this ValidationLevel level)
    {
        return level != ValidationLevel.PASS;
    }
    
    /// <summary>
    /// Should this validation level trigger hardware notification?
    /// CRITICAL always triggers hardware, FAIL optionally (configurable)
    /// </summary>
    public static bool ShouldTriggerHardware(this ValidationLevel level, bool enableFailHardware = false)
    {
        return level == ValidationLevel.CRITICAL || (level == ValidationLevel.FAIL && enableFailHardware);
    }
    
    /// <summary>
    /// Get appropriate emoji icon for this validation level
    /// </summary>
    public static string GetIcon(this ValidationLevel level)
    {
        return level switch
        {
            ValidationLevel.PASS => "✅",
            ValidationLevel.WARN => "⚠️",
            ValidationLevel.FAIL => "❌",
            ValidationLevel.CRITICAL => "🚨",
            _ => "❓"
        };
    }
    
    /// <summary>
    /// Get appropriate log level for Microsoft.Extensions.Logging
    /// </summary>
    public static Microsoft.Extensions.Logging.LogLevel GetLogLevel(this ValidationLevel level)
    {
        return level switch
        {
            ValidationLevel.PASS => Microsoft.Extensions.Logging.LogLevel.Information,
            ValidationLevel.WARN => Microsoft.Extensions.Logging.LogLevel.Warning,
            ValidationLevel.FAIL => Microsoft.Extensions.Logging.LogLevel.Error,
            ValidationLevel.CRITICAL => Microsoft.Extensions.Logging.LogLevel.Critical,
            _ => Microsoft.Extensions.Logging.LogLevel.Debug
        };
    }
    
    /// <summary>
    /// Get human-readable description of this validation level
    /// </summary>
    public static string GetDescription(this ValidationLevel level)
    {
        return level switch
        {
            ValidationLevel.PASS => "Validation passed - workflow continues normally",
            ValidationLevel.WARN => "Validation passed with warnings - workflow continues with alert logging",
            ValidationLevel.FAIL => "Validation failed - workflow stops with error reporting", 
            ValidationLevel.CRITICAL => "Critical validation failure - emergency stop with hardware notification",
            _ => "Unknown validation level"
        };
    }
    
    /// <summary>
    /// Get priority score for validation level (higher = more severe)
    /// Used for determining overall workflow result when multiple levels occur
    /// </summary>
    public static int GetPriorityScore(this ValidationLevel level)
    {
        return (int)level; // PASS=0, WARN=1, FAIL=2, CRITICAL=3
    }
    
    /// <summary>
    /// Compare two validation levels and return the most severe
    /// </summary>
    public static ValidationLevel GetMostSevere(this ValidationLevel level1, ValidationLevel level2)
    {
        return level1.GetPriorityScore() >= level2.GetPriorityScore() ? level1 : level2;
    }
}

/// <summary>
/// Helper class for validation level operations and conversions
/// </summary>
public static class ValidationLevelHelper
{
    /// <summary>
    /// All validation levels in order of severity (least to most severe)
    /// </summary>
    public static readonly ValidationLevel[] AllLevels = 
    {
        ValidationLevel.PASS,
        ValidationLevel.WARN, 
        ValidationLevel.FAIL,
        ValidationLevel.CRITICAL
    };
    
    /// <summary>
    /// Validation levels that should stop workflow execution
    /// </summary>
    public static readonly ValidationLevel[] StopWorkflowLevels = 
    {
        ValidationLevel.FAIL,
        ValidationLevel.CRITICAL
    };
    
    /// <summary>
    /// Validation levels that should trigger hardware actions
    /// </summary>
    public static readonly ValidationLevel[] HardwareTriggerLevels = 
    {
        ValidationLevel.CRITICAL
        // FAIL can be added based on configuration
    };
    
    /// <summary>
    /// Parse validation level from string (case-insensitive)
    /// </summary>
    public static ValidationLevel ParseFromString(string levelString)
    {
        if (string.IsNullOrWhiteSpace(levelString))
            return ValidationLevel.PASS;
            
        return levelString.ToUpperInvariant() switch
        {
            "PASS" or "SUCCESS" or "OK" or "GOOD" => ValidationLevel.PASS,
            "WARN" or "WARNING" or "CAUTION" => ValidationLevel.WARN,
            "FAIL" or "FAILURE" or "ERROR" or "BAD" => ValidationLevel.FAIL,
            "CRITICAL" or "EMERGENCY" or "SEVERE" => ValidationLevel.CRITICAL,
            _ => ValidationLevel.PASS // Default to PASS for unknown strings
        };
    }
    
    /// <summary>
    /// Get the most severe validation level from a collection
    /// </summary>
    public static ValidationLevel GetMostSevereLevel(IEnumerable<ValidationLevel> levels)
    {
        return levels.DefaultIfEmpty(ValidationLevel.PASS)
                    .Aggregate((level1, level2) => level1.GetMostSevere(level2));
    }
    
    /// <summary>
    /// Get summary statistics for a collection of validation levels
    /// </summary>
    public static ValidationLevelStatistics GetStatistics(IEnumerable<ValidationLevel> levels)
    {
        var levelList = levels.ToList();
        
        return new ValidationLevelStatistics
        {
            TotalCount = levelList.Count,
            PassCount = levelList.Count(l => l == ValidationLevel.PASS),
            WarnCount = levelList.Count(l => l == ValidationLevel.WARN),
            FailCount = levelList.Count(l => l == ValidationLevel.FAIL),
            CriticalCount = levelList.Count(l => l == ValidationLevel.CRITICAL),
            MostSevereLevel = GetMostSevereLevel(levelList)
        };
    }
}

/// <summary>
/// Statistics for validation level analysis
/// </summary>
public class ValidationLevelStatistics
{
    public int TotalCount { get; set; }
    public int PassCount { get; set; }
    public int WarnCount { get; set; }
    public int FailCount { get; set; }
    public int CriticalCount { get; set; }
    public ValidationLevel MostSevereLevel { get; set; } = ValidationLevel.PASS;
    
    /// <summary>
    /// Success rate (PASS + WARN) / Total
    /// </summary>
    public double SuccessRate => TotalCount > 0 ? (PassCount + WarnCount) * 100.0 / TotalCount : 0.0;
    
    /// <summary>
    /// Failure rate (FAIL + CRITICAL) / Total  
    /// </summary>
    public double FailureRate => TotalCount > 0 ? (FailCount + CriticalCount) * 100.0 / TotalCount : 0.0;
    
    /// <summary>
    /// Critical rate (CRITICAL) / Total
    /// </summary>
    public double CriticalRate => TotalCount > 0 ? CriticalCount * 100.0 / TotalCount : 0.0;
    
    public override string ToString()
    {
        return $"Validation Stats: {PassCount}✅ {WarnCount}⚠️ {FailCount}❌ {CriticalCount}🚨 " +
               $"(Success: {SuccessRate:F1}%, Critical: {CriticalRate:F1}%)";
    }
}