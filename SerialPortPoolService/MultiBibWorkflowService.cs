// ===================================================================
// SPRINT 10 FINAL: Multi-BIB Service Integration
// File: SerialPortPoolService/Services/MultiBibWorkflowService.cs
// Purpose: Production-ready Multi-BIB execution service
// ===================================================================

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SerialPortPool.Core.Interfaces;
using SerialPortPool.Core.Models;

namespace SerialPortPoolService.Services;

/// <summary>
/// Production Multi-BIB Workflow Service
/// SPRINT 10 FINAL: Integrates Multi-BIB implementation into main service
/// </summary>
public class MultiBibWorkflowService : IHostedService
{
    private readonly IBibWorkflowOrchestrator _orchestrator;
    private readonly IBibConfigurationLoader _configLoader;
    private readonly ILogger<MultiBibWorkflowService> _logger;
    private readonly MultiBibServiceConfiguration _config;
    
    private Timer? _scheduledExecutionTimer;
    private CancellationTokenSource? _cancellationTokenSource;

    public MultiBibWorkflowService(
        IBibWorkflowOrchestrator orchestrator,
        IBibConfigurationLoader configLoader,
        ILogger<MultiBibWorkflowService> logger,
        MultiBibServiceConfiguration config)
    {
        _orchestrator = orchestrator;
        _configLoader = configLoader;
        _logger = logger;
        _config = config;
    }

    /// <summary>
    /// Start Multi-BIB service
    /// </summary>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("🚀 Multi-BIB Workflow Service Starting...");
        _logger.LogInformation($"📋 Mode: {_config.ExecutionMode}");
        
        _cancellationTokenSource = new CancellationTokenSource();
        
        try
        {
            // Initialize configuration loader
            if (!string.IsNullOrEmpty(_config.DefaultConfigurationPath))
            {
                _configLoader.SetDefaultConfigurationPath(_config.DefaultConfigurationPath);
                _logger.LogInformation($"📄 Configuration path: {_config.DefaultConfigurationPath}");
            }

            // Start based on execution mode
            switch (_config.ExecutionMode)
            {
                case MultiBibExecutionMode.SingleRun:
                    _ = Task.Run(() => ExecuteSingleRunAsync(_cancellationTokenSource.Token), cancellationToken);
                    break;

                case MultiBibExecutionMode.Scheduled:
                    StartScheduledExecution();
                    break;

                case MultiBibExecutionMode.Continuous:
                    _ = Task.Run(() => ExecuteContinuousAsync(_cancellationTokenSource.Token), cancellationToken);
                    break;

                case MultiBibExecutionMode.OnDemand:
                    _logger.LogInformation("📡 On-demand mode - waiting for execution requests");
                    break;

                default:
                    throw new InvalidOperationException($"Unknown execution mode: {_config.ExecutionMode}");
            }

            _logger.LogInformation("✅ Multi-BIB Workflow Service Started Successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to start Multi-BIB Workflow Service");
            throw;
        }
    }

    /// <summary>
    /// Stop Multi-BIB service gracefully
    /// </summary>
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("🛑 Multi-BIB Workflow Service Stopping...");

        _scheduledExecutionTimer?.Dispose();
        _cancellationTokenSource?.Cancel();

        try
        {
            // Give running workflows time to complete
            var timeoutTask = Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);
            var completed = await Task.WhenAny(timeoutTask);
            
            if (completed == timeoutTask)
            {
                _logger.LogWarning("⏰ Service stop timeout - some workflows may have been interrupted");
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Service stop was cancelled");
        }

        _logger.LogInformation("✅ Multi-BIB Workflow Service Stopped");
    }

    #region Execution Modes

    /// <summary>
    /// Execute single Multi-BIB run
    /// </summary>
    private async Task ExecuteSingleRunAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("🎯 Single Multi-BIB Run Mode");

            MultiBibWorkflowResult result;

            if (_config.TargetBibIds?.Any() == true)
            {
                // Execute specific BIB_IDs
                _logger.LogInformation($"📋 Target BIB_IDs: {string.Join(", ", _config.TargetBibIds)}");
                result = await _orchestrator.ExecuteMultipleBibsWithSummaryAsync(
                    _config.TargetBibIds, 
                    _config.IncludeDetailedLogs, 
                    "MultiBibService",
                    cancellationToken);
            }
            else
            {
                // Execute all configured BIBs
                _logger.LogInformation("📋 Executing ALL configured BIB_IDs");
                result = await _orchestrator.ExecuteAllConfiguredBibsWithSummaryAsync(
                    _config.IncludeDetailedLogs,
                    "MultiBibService",
                    cancellationToken);
            }

            LogFinalResults(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Single Multi-BIB run failed");
        }
    }

    /// <summary>
    /// Execute continuous Multi-BIB loop
    /// </summary>
    private async Task ExecuteContinuousAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("🔄 Continuous Multi-BIB Mode");
        _logger.LogInformation($"⏱️ Interval: {_config.ContinuousInterval.TotalMinutes:F1} minutes");

        var cycleNumber = 0;

        while (!cancellationToken.IsCancellationRequested)
        {
            cycleNumber++;
            
            try
            {
                _logger.LogInformation($"🔄 Multi-BIB Cycle #{cycleNumber} Starting...");

                MultiBibWorkflowResult result;

                if (_config.TargetBibIds?.Any() == true)
                {
                    result = await _orchestrator.ExecuteMultipleBibsWithSummaryAsync(
                        _config.TargetBibIds,
                        _config.IncludeDetailedLogs,
                        $"MultiBibService_Cycle_{cycleNumber}",
                        cancellationToken);
                }
                else
                {
                    result = await _orchestrator.ExecuteAllConfiguredBibsWithSummaryAsync(
                        _config.IncludeDetailedLogs,
                        $"MultiBibService_Cycle_{cycleNumber}",
                        cancellationToken);
                }

                _logger.LogInformation($"✅ Multi-BIB Cycle #{cycleNumber} Completed: {result.GetSummary()}");

                // Wait for next cycle
                if (!cancellationToken.IsCancellationRequested)
                {
                    _logger.LogInformation($"⏳ Waiting {_config.ContinuousInterval.TotalMinutes:F1} minutes until next cycle...");
                    await Task.Delay(_config.ContinuousInterval, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation($"🛑 Continuous mode cancelled at cycle #{cycleNumber}");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Multi-BIB Cycle #{cycleNumber} failed");
                
                // Continue with next cycle after error delay
                if (!cancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(TimeSpan.FromMinutes(1), cancellationToken);
                }
            }
        }
    }

    /// <summary>
    /// Start scheduled execution
    /// </summary>
    private void StartScheduledExecution()
    {
        _logger.LogInformation("📅 Scheduled Multi-BIB Mode");
        _logger.LogInformation($"⏰ Schedule: {_config.ScheduleInterval.TotalHours:F1} hours");

        _scheduledExecutionTimer = new Timer(
            async _ => await ExecuteScheduledAsync(),
            null,
            TimeSpan.Zero, // Start immediately
            _config.ScheduleInterval);
    }

    /// <summary>
    /// Execute scheduled Multi-BIB run
    /// </summary>
    private async Task ExecuteScheduledAsync()
    {
        try
        {
            _logger.LogInformation("📅 Scheduled Multi-BIB Execution Starting...");

            var result = _config.TargetBibIds?.Any() == true
                ? await _orchestrator.ExecuteMultipleBibsWithSummaryAsync(
                    _config.TargetBibIds,
                    _config.IncludeDetailedLogs,
                    "MultiBibService_Scheduled",
                    _cancellationTokenSource?.Token ?? CancellationToken.None)
                : await _orchestrator.ExecuteAllConfiguredBibsWithSummaryAsync(
                    _config.IncludeDetailedLogs,
                    "MultiBibService_Scheduled",
                    _cancellationTokenSource?.Token ?? CancellationToken.None);

            LogFinalResults(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Scheduled Multi-BIB execution failed");
        }
    }

    #endregion

    #region Public API Methods

    /// <summary>
    /// Execute Multi-BIB on demand (for API/external triggers)
    /// </summary>
    public async Task<MultiBibWorkflowResult> ExecuteOnDemandAsync(
        List<string>? targetBibIds = null,
        string clientId = "OnDemand",
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("📡 On-Demand Multi-BIB Execution Requested");

        var bibIds = targetBibIds ?? _config.TargetBibIds ?? new List<string>();

        if (bibIds.Any())
        {
            return await _orchestrator.ExecuteMultipleBibsWithSummaryAsync(
                bibIds,
                _config.IncludeDetailedLogs,
                clientId,
                cancellationToken);
        }
        else
        {
            return await _orchestrator.ExecuteAllConfiguredBibsWithSummaryAsync(
                _config.IncludeDetailedLogs,
                clientId,
                cancellationToken);
        }
    }

    /// <summary>
    /// Get service status and statistics
    /// </summary>
    public MultiBibServiceStatus GetServiceStatus()
    {
        return new MultiBibServiceStatus
        {
            IsRunning = _cancellationTokenSource?.Token.IsCancellationRequested == false,
            ExecutionMode = _config.ExecutionMode,
            ConfiguredBibIds = _config.TargetBibIds ?? new List<string>(),
            ServiceStartTime = DateTime.Now, // In real implementation, track actual start time
            LastExecutionTime = DateTime.Now // In real implementation, track actual last execution
        };
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Log final Multi-BIB results with professional formatting
    /// </summary>
    private void LogFinalResults(MultiBibWorkflowResult result)
    {
        _logger.LogInformation("📊 " + "=".PadRight(60, '='));
        _logger.LogInformation("📊 MULTI-BIB EXECUTION COMPLETE");
        _logger.LogInformation("📊 " + "=".PadRight(60, '='));
        _logger.LogInformation($"🎯 Target BIBs: {result.TotalBibsExecuted}");
        _logger.LogInformation($"✅ Successful BIBs: {result.SuccessfulBibs} ({result.BibSuccessRate:F1}%)");
        _logger.LogInformation($"🔧 Total Workflows: {result.TotalWorkflows}");
        _logger.LogInformation($"📈 Workflow Success Rate: {result.WorkflowSuccessRate:F1}%");
        _logger.LogInformation($"⏱️ Total Execution Time: {result.TotalExecutionTime.TotalMinutes:F1} minutes");
        _logger.LogInformation($"⚡ Average per Workflow: {result.AverageWorkflowDuration.TotalSeconds:F1} seconds");
        
        if (!string.IsNullOrEmpty(result.ErrorMessage))
        {
            _logger.LogError($"❌ Error: {result.ErrorMessage}");
        }
        
        _logger.LogInformation("📊 " + "=".PadRight(60, '='));

        // BIB-level breakdown
        var bibBreakdown = result.GetBibBreakdown();
        foreach (var bib in bibBreakdown)
        {
            _logger.LogInformation($"📋 {bib.Value}");
        }
        
        _logger.LogInformation("📊 " + "=".PadRight(60, '='));
    }

    #endregion
}

/// <summary>
/// Multi-BIB Service Configuration
/// </summary>
public class MultiBibServiceConfiguration
{
    /// <summary>
    /// Execution mode for the service
    /// </summary>
    public MultiBibExecutionMode ExecutionMode { get; set; } = MultiBibExecutionMode.SingleRun;

    /// <summary>
    /// Specific BIB_IDs to execute (null = all configured)
    /// </summary>
    public List<string>? TargetBibIds { get; set; }

    /// <summary>
    /// Default XML configuration path
    /// </summary>
    public string? DefaultConfigurationPath { get; set; }

    /// <summary>
    /// Include detailed logs in execution
    /// </summary>
    public bool IncludeDetailedLogs { get; set; } = true;

    /// <summary>
    /// Interval for continuous mode
    /// </summary>
    public TimeSpan ContinuousInterval { get; set; } = TimeSpan.FromMinutes(30);

    /// <summary>
    /// Interval for scheduled mode
    /// </summary>
    public TimeSpan ScheduleInterval { get; set; } = TimeSpan.FromHours(4);

    /// <summary>
    /// 🆕 SPRINT 11: Metadata for enhanced configuration options
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Multi-BIB Execution Modes
/// </summary>
public enum MultiBibExecutionMode
{
    /// <summary>
    /// Execute once and stop
    /// </summary>
    SingleRun,

    /// <summary>
    /// Execute continuously with intervals
    /// </summary>
    Continuous,

    /// <summary>
    /// Execute on fixed schedule
    /// </summary>
    Scheduled,

    /// <summary>
    /// Execute only when requested (API mode)
    /// </summary>
    OnDemand
}

/// <summary>
/// Multi-BIB Service Status
/// </summary>
public class MultiBibServiceStatus
{
    public bool IsRunning { get; set; }
    public MultiBibExecutionMode ExecutionMode { get; set; }
    public List<string> ConfiguredBibIds { get; set; } = new();
    public DateTime ServiceStartTime { get; set; }
    public DateTime LastExecutionTime { get; set; }
}

