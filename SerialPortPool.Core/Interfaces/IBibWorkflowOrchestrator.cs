// ===================================================================
// CLEAN: IBibWorkflowOrchestrator Interface - SPRINT 10 Enhanced  
// File: SerialPortPool.Core/Interfaces/IBibWorkflowOrchestrator.cs
// Purpose: Clean interface without embedded class definitions
// ===================================================================

using SerialPortPool.Core.Models;

namespace SerialPortPool.Core.Interfaces;

/// <summary>
/// Interface for BIB workflow orchestration - SPRINT 10 Enhanced with Multi-UUT support
/// Supports single workflow execution + Multi-UUT wrapper methods (Option 1)
/// </summary>
public interface IBibWorkflowOrchestrator
{
    // ✅ EXISTING METHODS (Sprint 8/9) - PRESERVED
    
    /// <summary>
    /// Execute complete BIB workflow for specific port
    /// </summary>
    Task<BibWorkflowResult> ExecuteBibWorkflowAsync(
        string bibId,
        string uutId,
        int portNumber,
        string clientId = "BibWorkflow",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Execute BIB workflow with automatic port discovery
    /// </summary>
    Task<BibWorkflowResult> ExecuteBibWorkflowAutoPortAsync(
        string bibId,
        string uutId,
        string clientId = "BibWorkflowAuto",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validate if workflow can be executed for specified parameters
    /// </summary>
    Task<bool> ValidateWorkflowAsync(string bibId, string uutId, int portNumber);

    /// <summary>
    /// Get workflow execution statistics
    /// </summary>
    Task<BibWorkflowStatistics> GetWorkflowStatisticsAsync();

    // 🆕 SPRINT 10: Multi-UUT Wrapper Methods (Option 1)
    
    /// <summary>
    /// 🆕 SPRINT 10: Execute workflow for ALL ports in a specific UUT
    /// OPTION 1: Simple sequential execution reusing proven single-port method
    /// </summary>
    Task<List<BibWorkflowResult>> ExecuteBibWorkflowAllPortsAsync(
        string bibId,
        string uutId,
        string clientId = "MultiPortWorkflow",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 🆕 SPRINT 10: Execute workflow for ALL UUTs in a BIB
    /// OPTION 1: Simple sequential execution reusing proven methods
    /// </summary>
    Task<List<BibWorkflowResult>> ExecuteBibWorkflowAllUutsAsync(
        string bibId,
        string clientId = "MultiUutWorkflow",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 🆕 SPRINT 10: Execute COMPLETE BIB workflow (all UUTs, all ports)
    /// OPTION 1: Convenience method with enhanced summary reporting
    /// </summary>
    Task<AggregatedWorkflowResult> ExecuteBibWorkflowCompleteAsync(
        string bibId,
        string clientId = "CompleteBibWorkflow",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 🆕 SPRINT 10: Execute workflow with SUMMARY reporting focus
    /// OPTION 1: Enhanced logging and reporting for management/monitoring
    /// </summary>
    Task<AggregatedWorkflowResult> ExecuteBibWorkflowWithSummaryAsync(
        string bibId,
        bool includeDetailedLogs = true,
        string clientId = "SummaryWorkflow",
        CancellationToken cancellationToken = default);
}