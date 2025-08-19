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

// ===================================================================
// SPRINT 10: IBibWorkflowOrchestrator Interface Update - Multi-BIB Methods
// ADD to IBibWorkflowOrchestrator.cs (after existing methods)
// ===================================================================

// 🆕 SPRINT 10: Multi-BIB Wrapper Methods (Client Priority #1)

/// <summary>
/// 🆕 SPRINT 10: Execute workflow for MULTIPLE BIB_IDs sequentially
/// CLIENT PRIORITY #1: Support multiple BIB configurations (client_demo_A, client_demo_B, etc.)
/// </summary>
Task<List<BibWorkflowResult>> ExecuteMultipleBibsAsync(
    List<string> bibIds,
    string clientId = "MultiBibWorkflow",
    CancellationToken cancellationToken = default);

/// <summary>
/// 🆕 SPRINT 10: Execute workflow for ALL configured BIB_IDs
/// CLIENT CONVENIENCE: Discover and execute all BIBs in configuration
/// </summary>
Task<List<BibWorkflowResult>> ExecuteAllConfiguredBibsAsync(
    string clientId = "AllBibsWorkflow",
    CancellationToken cancellationToken = default);

/// <summary>
/// 🆕 SPRINT 10: Execute MULTIPLE BIBs with enhanced summary reporting
/// CLIENT PRIORITY #1: Multi-BIB execution with professional reporting
/// </summary>
Task<MultiBibWorkflowResult> ExecuteMultipleBibsWithSummaryAsync(
    List<string> bibIds,
    bool includeDetailedLogs = true,
    string clientId = "MultiBibSummaryWorkflow",
    CancellationToken cancellationToken = default);

/// <summary>
/// 🆕 SPRINT 10: Execute ALL configured BIBs with complete summary
/// CLIENT ULTIMATE: Complete system execution with comprehensive reporting
/// </summary>
Task<MultiBibWorkflowResult> ExecuteAllConfiguredBibsWithSummaryAsync(
    bool includeDetailedLogs = true,
    string clientId = "CompleteBibSystemWorkflow",
    CancellationToken cancellationToken = default);
}

