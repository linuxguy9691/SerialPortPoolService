// ===================================================================
// SPRINT 10: MultiBibWorkflowResult Model
// File: SerialPortPool.Core/Models/MultiBibWorkflowResult.cs
// Purpose: Aggregated reporting for Multi-BIB workflows
// ===================================================================

namespace SerialPortPool.Core.Models;

/// <summary>
/// SPRINT 10: Multi-BIB workflow execution result
/// Aggregated reporting for multiple BIB_ID execution with enhanced statistics
/// CLIENT PRIORITY: Professional reporting for Multi-BIB workflows
/// </summary>
public class MultiBibWorkflowResult
{
    /// <summary>
    /// Target BIB_IDs that were executed
    /// </summary>
    public List<string> TargetBibIds { get; set; } = new();
    
    /// <summary>
    /// Total number of BIBs executed
    /// </summary>
    public int TotalBibsExecuted { get; set; }
    
    /// <summary>
    /// Number of BIBs with at least one successful workflow
    /// </summary>
    public int SuccessfulBibs { get; set; }
    
    /// <summary>
    /// Number of BIBs with no successful workflows
    /// </summary>
    public int FailedBibs { get; set; }
    
    /// <summary>
    /// Total number of individual workflows executed across all BIBs
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
    /// Total execution time for all BIBs
    /// </summary>
    public TimeSpan TotalExecutionTime { get; set; }
    
    /// <summary>
    /// All individual workflow results across all BIBs
    /// </summary>
    public List<BibWorkflowResult> AllResults { get; set; } = new();
    
    /// <summary>
    /// When this Multi-BIB result was generated
    /// </summary>
    public DateTime GeneratedAt { get; set; } = DateTime.Now;
    
    /// <summary>
    /// Error message if Multi-BIB execution failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    // ✨ CALCULATED PROPERTIES
    
    /// <summary>
    /// BIB-level success rate as percentage (0-100)
    /// </summary>
    public double BibSuccessRate => TotalBibsExecuted > 0 ? 
        (SuccessfulBibs * 100.0) / TotalBibsExecuted : 0.0;
    
    /// <summary>
    /// Workflow-level success rate as percentage (0-100)
    /// </summary>
    public double WorkflowSuccessRate => TotalWorkflows > 0 ? 
        (SuccessfulWorkflows * 100.0) / TotalWorkflows : 0.0;
    
    /// <summary>
    /// Whether all BIBs were completely successful
    /// </summary>
    public bool AllBibsSuccessful => TotalBibsExecuted > 0 && FailedBibs == 0;
    
    /// <summary>
    /// Whether any BIBs were successful
    /// </summary>
    public bool AnyBibsSuccessful => SuccessfulBibs > 0;
    
    /// <summary>
    /// Average execution time per individual workflow
    /// </summary>
    public TimeSpan AverageWorkflowDuration { get; set; }
    
    /// <summary>
    /// Number of unique UUTs processed across all BIBs
    /// </summary>
    public int UniqueUuts { get; set; }

    // 📊 SUMMARY METHODS
    
    /// <summary>
    /// Get basic Multi-BIB summary string
    /// </summary>
    public string GetSummary()
    {
        var status = AllBibsSuccessful ? "✅ ALL SUCCESS" : 
                    AnyBibsSuccessful ? "⚠️ PARTIAL SUCCESS" : "❌ ALL FAILED";
        
        return $"{status}: {SuccessfulBibs}/{TotalBibsExecuted} BIBs, {SuccessfulWorkflows}/{TotalWorkflows} workflows in {TotalExecutionTime.TotalMinutes:F1}min";
    }
    
    /// <summary>
    /// Get detailed Multi-BIB summary with statistics
    /// </summary>
    public string GetDetailedSummary()
    {
        var lines = new List<string>
        {
            $"Multi-BIB Execution: {string.Join(", ", TargetBibIds)}",
            $"BIB Success: {SuccessfulBibs}/{TotalBibsExecuted} ({BibSuccessRate:F1}%)",
            $"Workflow Success: {SuccessfulWorkflows}/{TotalWorkflows} ({WorkflowSuccessRate:F1}%)",
            $"Unique UUTs: {UniqueUuts}",
            $"Duration: {TotalExecutionTime.TotalMinutes:F1} minutes total",
            $"Average: {AverageWorkflowDuration.TotalSeconds:F1}s per workflow"
        };
        
        if (!string.IsNullOrEmpty(ErrorMessage))
        {
            lines.Add($"Error: {ErrorMessage}");
        }
        
        return string.Join(", ", lines);
    }
    
    /// <summary>
    /// Get breakdown by BIB_ID
    /// </summary>
    public Dictionary<string, BibSummary> GetBibBreakdown()
    {
        return AllResults
            .GroupBy(r => r.BibId)
            .ToDictionary(
                g => g.Key,
                g => new BibSummary
                {
                    BibId = g.Key,
                    TotalWorkflows = g.Count(),
                    SuccessfulWorkflows = g.Count(r => r.Success),
                    AverageDuration = TimeSpan.FromTicks((long)g.Average(r => r.Duration.Ticks)),
                    TotalDuration = TimeSpan.FromTicks(g.Sum(r => r.Duration.Ticks)),
                    UniqueUuts = g.Select(r => r.UutId).Distinct().Count()
                });
    }
    
    public override string ToString()
    {
        return GetSummary();
    }
}

/// <summary>
/// Summary information for a specific BIB within Multi-BIB results
/// </summary>
public class BibSummary
{
    public string BibId { get; set; } = string.Empty;
    public int TotalWorkflows { get; set; }
    public int SuccessfulWorkflows { get; set; }
    public int FailedWorkflows => TotalWorkflows - SuccessfulWorkflows;
    public double SuccessRate => TotalWorkflows > 0 ? (SuccessfulWorkflows * 100.0) / TotalWorkflows : 0.0;
    public TimeSpan TotalDuration { get; set; }
    public TimeSpan AverageDuration { get; set; }
    public int UniqueUuts { get; set; }
    
    public override string ToString()
    {
        var status = SuccessfulWorkflows == TotalWorkflows ? "✅" : SuccessfulWorkflows > 0 ? "⚠️" : "❌";
        return $"{status} {BibId}: {SuccessfulWorkflows}/{TotalWorkflows} workflows ({SuccessRate:F1}%), {UniqueUuts} UUTs";
    }
}