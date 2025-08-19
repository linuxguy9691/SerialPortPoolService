// ===================================================================
// EXTRACTED: BibWorkflowResult Model  
// File: SerialPortPool.Core/Models/BibWorkflowResult.cs
// Purpose: Individual workflow execution result (extracted from interface)
// ===================================================================

namespace SerialPortPool.Core.Models;

/// <summary>
/// BIB workflow execution result - Individual workflow result
/// </summary>
public class BibWorkflowResult
{
    public string WorkflowId { get; set; } = string.Empty;
    public string BibId { get; set; } = string.Empty;
    public string UutId { get; set; } = string.Empty;
    public int PortNumber { get; set; }
    public string ClientId { get; set; } = string.Empty;
    public string ProtocolName { get; set; } = string.Empty;
    public string PhysicalPort { get; set; } = string.Empty;
    public string ReservationId { get; set; } = string.Empty;
    public string SessionId { get; set; } = string.Empty;
    
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public TimeSpan Duration => EndTime - StartTime;
    
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    
    // Workflow phases
    public bool ConfigurationLoaded { get; set; }
    public bool PortReserved { get; set; }
    public bool SessionOpened { get; set; }
    
    // 3-phase results
    public CommandSequenceResult? StartResult { get; set; }
    public CommandSequenceResult? TestResult { get; set; }
    public CommandSequenceResult? StopResult { get; set; }
    
    /// <summary>
    /// Check if all 3 phases completed successfully
    /// </summary>
    public bool AllPhasesSuccessful()
    {
        return StartResult?.IsSuccess == true &&
               TestResult?.IsSuccess == true &&
               StopResult?.IsSuccess == true;
    }
    
    /// <summary>
    /// Set error state
    /// </summary>
    public BibWorkflowResult WithError(string errorMessage)
    {
        Success = false;
        ErrorMessage = errorMessage;
        if (EndTime == default)
            EndTime = DateTime.Now;
        return this;
    }
    
    /// <summary>
    /// Get workflow summary
    /// </summary>
    public string GetSummary()
    {
        var status = Success ? "✅ SUCCESS" : "❌ FAILED";
        var phases = $"Start: {StartResult?.IsSuccess ?? false}, Test: {TestResult?.IsSuccess ?? false}, Stop: {StopResult?.IsSuccess ?? false}";
        return $"{status}: {BibId}.{UutId}.{PortNumber} ({ProtocolName}) - {phases} [{Duration.TotalSeconds:F1}s]";
    }
    
    public override string ToString() => GetSummary();
}