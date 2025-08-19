// ===================================================================
// EXTRACTED: BibWorkflowStatistics Model
// File: SerialPortPool.Core/Models/BibWorkflowStatistics.cs  
// Purpose: Workflow execution statistics (extracted from interface)
// ===================================================================

namespace SerialPortPool.Core.Models;

/// <summary>
/// BIB workflow statistics - Statistics for workflow execution tracking
/// </summary>
public class BibWorkflowStatistics
{
    /// <summary>
    /// Total number of workflows executed
    /// </summary>
    public int TotalWorkflows { get; set; }
    
    /// <summary>
    /// Alias for compatibility with existing code
    /// </summary>
    public int TotalWorkflowsExecuted => TotalWorkflows;
    
    public int SuccessfulWorkflows { get; set; }
    public int FailedWorkflows { get; set; }
    public TimeSpan AverageWorkflowDuration { get; set; }
    public DateTime GeneratedAt { get; set; }
    
    public double SuccessRate => TotalWorkflows > 0 ? (double)SuccessfulWorkflows / TotalWorkflows * 100.0 : 0.0;
    
    public override string ToString()
    {
        return $"Workflows: {SuccessfulWorkflows}/{TotalWorkflows} successful ({SuccessRate:F1}%), avg duration: {AverageWorkflowDuration.TotalSeconds:F1}s";
    }
}