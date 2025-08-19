// ===================================================================
// SPRINT 10: AggregatedWorkflowResult Model - Multi-UUT Summary
// File: SerialPortPool.Core/Models/AggregatedWorkflowResult.cs
// Purpose: Aggregated reporting for Multi-UUT workflow execution
// ===================================================================

namespace SerialPortPool.Core.Models;

/// <summary>
/// SPRINT 10: Aggregated result for Multi-UUT workflow execution
/// Provides comprehensive summary and statistics for multiple workflow results
/// </summary>
public class AggregatedWorkflowResult
{
    /// <summary>
    /// BIB identifier for this aggregated result
    /// </summary>
    public string BibId { get; set; } = string.Empty;
    
    /// <summary>
    /// Total number of individual workflows executed
    /// </summary>
    public int TotalWorkflows { get; set; }
    
    /// <summary>
    /// Number of successful individual workflows
    /// </summary>
    public int SuccessfulWorkflows { get; set; }
    
    /// <summary>
    /// Number of failed individual workflows
    /// </summary>
    public int FailedWorkflows { get; set; }
    
    /// <summary>
    /// Total execution time for all workflows
    /// </summary>
    public TimeSpan TotalExecutionTime { get; set; }
    
    /// <summary>
    /// All individual workflow results
    /// </summary>
    public List<BibWorkflowResult> Results { get; set; } = new();
    
    /// <summary>
    /// When this aggregated result was generated
    /// </summary>
    public DateTime GeneratedAt { get; set; } = DateTime.Now;
    
    /// <summary>
    /// Error message if aggregation failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    // ✨ CALCULATED PROPERTIES
    
    /// <summary>
    /// Success rate as percentage (0-100)
    /// </summary>
    public double SuccessRate => TotalWorkflows > 0 ? 
        (SuccessfulWorkflows * 100.0) / TotalWorkflows : 0.0;
    
    /// <summary>
    /// Whether all workflows were successful
    /// </summary>
    public bool AllSuccessful => TotalWorkflows > 0 && FailedWorkflows == 0;
    
    /// <summary>
    /// Whether any workflows were successful
    /// </summary>
    public bool AnySuccessful => SuccessfulWorkflows > 0;
    
    /// <summary>
    /// Average execution time per individual workflow
    /// </summary>
    public TimeSpan AverageWorkflowDuration { get; set; }
    
    /// <summary>
    /// Number of unique UUTs processed
    /// </summary>
    public int UniqueUuts { get; set; }
    
    /// <summary>
    /// Fastest individual workflow duration
    /// </summary>
    public TimeSpan FastestWorkflow => Results.Any() ? 
        Results.Min(r => r.Duration) : TimeSpan.Zero;
    
    /// <summary>
    /// Slowest individual workflow duration
    /// </summary>
    public TimeSpan SlowestWorkflow => Results.Any() ? 
        Results.Max(r => r.Duration) : TimeSpan.Zero;

    // 📊 SUMMARY METHODS
    
    /// <summary>
    /// Get basic summary string
    /// </summary>
    public string GetSummary()
    {
        var status = AllSuccessful ? "✅ ALL SUCCESS" : 
                    AnySuccessful ? "⚠️ PARTIAL SUCCESS" : "❌ ALL FAILED";
        
        return $"{status}: {SuccessfulWorkflows}/{TotalWorkflows} workflows in {TotalExecutionTime.TotalMinutes:F1}min";
    }
    
    /// <summary>
    /// Get detailed summary with statistics
    /// </summary>
    public string GetDetailedSummary()
    {
        var lines = new List<string>
        {
            $"BIB: {BibId}",
            $"Workflows: {SuccessfulWorkflows}/{TotalWorkflows} successful ({SuccessRate:F1}%)",
            $"UUTs: {UniqueUuts} unique",
            $"Duration: {TotalExecutionTime.TotalMinutes:F1} minutes total",
            $"Average: {AverageWorkflowDuration.TotalSeconds:F1}s per workflow"
        };
        
        if (Results.Any())
        {
            lines.Add($"Range: {FastestWorkflow.TotalSeconds:F1}s - {SlowestWorkflow.TotalSeconds:F1}s");
        }
        
        if (!string.IsNullOrEmpty(ErrorMessage))
        {
            lines.Add($"Error: {ErrorMessage}");
        }
        
        return string.Join(", ", lines);
    }
    
    /// <summary>
    /// Get breakdown by UUT
    /// </summary>
    public Dictionary<string, UutSummary> GetUutBreakdown()
    {
        return Results
            .GroupBy(r => r.UutId)
            .ToDictionary(
                g => g.Key,
                g => new UutSummary
                {
                    UutId = g.Key,
                    TotalPorts = g.Count(),
                    SuccessfulPorts = g.Count(r => r.Success),
                    AverageDuration = TimeSpan.FromTicks((long)g.Average(r => r.Duration.Ticks)),
                    TotalDuration = TimeSpan.FromTicks(g.Sum(r => r.Duration.Ticks))
                });
    }
    
    /// <summary>
    /// Get protocols used in this aggregation
    /// </summary>
    public List<string> GetProtocolsUsed()
    {
        return Results
            .Where(r => !string.IsNullOrEmpty(r.ProtocolName))
            .Select(r => r.ProtocolName)
            .Distinct()
            .OrderBy(p => p)
            .ToList();
    }
    
    public override string ToString()
    {
        return GetSummary();
    }
}

/// <summary>
/// Summary information for a specific UUT within aggregated results
/// </summary>
public class UutSummary
{
    public string UutId { get; set; } = string.Empty;
    public int TotalPorts { get; set; }
    public int SuccessfulPorts { get; set; }
    public int FailedPorts => TotalPorts - SuccessfulPorts;
    public double SuccessRate => TotalPorts > 0 ? (SuccessfulPorts * 100.0) / TotalPorts : 0.0;
    public TimeSpan TotalDuration { get; set; }
    public TimeSpan AverageDuration { get; set; }
    
    public override string ToString()
    {
        var status = SuccessfulPorts == TotalPorts ? "✅" : SuccessfulPorts > 0 ? "⚠️" : "❌";
        return $"{status} {UutId}: {SuccessfulPorts}/{TotalPorts} ports ({SuccessRate:F1}%)";
    }
}