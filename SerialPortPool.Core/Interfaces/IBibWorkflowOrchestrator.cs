// SerialPortPool.Core/Services/BibWorkflowOrchestrator.cs - NEW Week 2
using Microsoft.Extensions.Logging;
using SerialPortPool.Core.Interfaces;  // ← AJOUTÉ: Pour IBibConfigurationLoader et autres interfaces
using SerialPortPool.Core.Models;

namespace SerialPortPool.Core.Interfaces;

/// <summary>
/// Interface for BIB workflow orchestration
/// Week 2: Integration of BIB mapping + XML configuration + RS232 protocol
/// </summary>
public interface IBibWorkflowOrchestrator
{
    /// <summary>
    /// Execute complete BIB workflow for specific port
    /// </summary>
    /// <param name="bibId">BIB identifier</param>
    /// <param name="uutId">UUT identifier</param>
    /// <param name="portNumber">Port number within UUT</param>
    /// <param name="clientId">Client identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Complete workflow execution result</returns>
    Task<BibWorkflowResult> ExecuteBibWorkflowAsync(
        string bibId,
        string uutId,
        int portNumber,
        string clientId = "BibWorkflow",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Execute BIB workflow with automatic port discovery
    /// </summary>
    /// <param name="bibId">BIB identifier</param>
    /// <param name="uutId">UUT identifier</param>
    /// <param name="clientId">Client identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Workflow execution result</returns>
    Task<BibWorkflowResult> ExecuteBibWorkflowAutoPortAsync(
        string bibId,
        string uutId,
        string clientId = "BibWorkflowAuto",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get workflow execution statistics
    /// </summary>
    /// <returns>Workflow statistics</returns>
    Task<BibWorkflowStatistics> GetWorkflowStatisticsAsync();
}

/// <summary>
/// BIB workflow execution result
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

/// <summary>
/// BIB workflow statistics
/// </summary>
public class BibWorkflowStatistics
{
    public int TotalWorkflows { get; set; }
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