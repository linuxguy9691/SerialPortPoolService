// ===================================================================
// SPRINT 10 FINAL: Multi-BIB Service Integration
// File: SerialPortPoolService/Services/MultiBibWorkflowService.cs
// Purpose: Production-ready Multi-BIB execution service
// SPRINT 14 FIX: Logger type correction for BitBangProductionService + Hot-Add Integration
// ===================================================================

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SerialPortPool.Core.Interfaces;
using SerialPortPool.Core.Models;
using SerialPortPool.Core.Services; // 🆕 SPRINT 14 FIX: Add for DynamicBibConfigurationService and EventArgs
using Microsoft.Extensions.DependencyInjection;

namespace SerialPortPoolService.Services;


/// <summary>
/// Production Multi-BIB Workflow Service
/// SPRINT 10 FINAL: Integrates Multi-BIB implementation into main service
/// SPRINT 14: Added Production BitBang mode with per UUT_ID control + Hot-Add Integration
/// </summary>
public class MultiBibWorkflowService : IHostedService
{
    private readonly IBibWorkflowOrchestrator _orchestrator;
    private readonly IBibConfigurationLoader _configLoader;
    private readonly ILogger<MultiBibWorkflowService> _logger;
    private readonly ILoggerFactory _loggerFactory;  // ← SPRINT 14: Logger factory for BitBang service
    private readonly IPortReservationService _portReservationService;
    private readonly MultiBibServiceConfiguration _config;
    private readonly IServiceProvider _serviceProvider;

    
    // 🆕 SPRINT 14 FIX: Add DynamicBibConfigurationService for hot-add
    private readonly DynamicBibConfigurationService _dynamicBibService;

    private Timer? _scheduledExecutionTimer;
    private CancellationTokenSource? _cancellationTokenSource;
    private BitBangProductionService? _bitBangService;  // ← SPRINT 14: BitBang service
    
    // 🆕 SPRINT 14 FIX: Track active production workflows per BIB
    private readonly Dictionary<string, Task> _activeProductionTasks = new();
    private readonly SemaphoreSlim _productionTaskLock = new(1, 1);

    public MultiBibWorkflowService(
        IBibWorkflowOrchestrator orchestrator,
        IBibConfigurationLoader configLoader,
        ILogger<MultiBibWorkflowService> logger,
        ILoggerFactory loggerFactory,  // ← SPRINT 14: Added for BitBang service creation
        IPortReservationService portReservationService,
        DynamicBibConfigurationService dynamicBibService, // 🆕 SPRINT 14 FIX: Inject for hot-add
        IServiceProvider serviceProvider, 
        MultiBibServiceConfiguration config)
    {
        _orchestrator = orchestrator;
        _configLoader = configLoader;
        _logger = logger;
        _loggerFactory = loggerFactory;  // ← SPRINT 14: Store logger factory
        _portReservationService = portReservationService;
        _dynamicBibService = dynamicBibService; // 🆕 SPRINT 14 FIX: Store for hot-add
        _serviceProvider = serviceProvider;
        _config = config;

        _dynamicBibService.BibConfigurationChanged += (sender, e) => HandleBibEvent(e);
    }

    

    /// <summary>
    /// Start Multi-BIB service
    /// </summary>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogCritical($"🚨 DEBUG: MultiBibWorkflowService starting with mode: {_config.ExecutionMode}");

        // 🏭 SPRINT 14 FIX: Handle production mode specially
        if (_config.ExecutionMode == MultiBibExecutionMode.Production)
        {
            _logger.LogCritical("🚨 DEBUG: Production case REACHED!");
            _logger.LogInformation("🏭 Production BitBang mode - taking direct control...");
            _cancellationTokenSource = new CancellationTokenSource();
            
            // 🆕 SPRINT 14 FIX: Subscribe to dynamic BIB events
            _dynamicBibService.BibDiscovered += OnBibDiscoveredForProduction;
            _dynamicBibService.BibRemoved += OnBibRemovedFromProduction;
            
            try
            {
                _ = Task.Run(async () => 
                {
                    try
                    {
                        await ExecuteProductionModeAsync(_cancellationTokenSource.Token);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogCritical(ex, "🚨 FATAL: Exception in Task.Run ExecuteProductionModeAsync");
                    }
                }, cancellationToken);
                
                _logger.LogInformation("✅ Production mode started - bypassing standard workflow orchestration");
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "🚨 FATAL: Exception creating Task.Run");
            }
            
            return;
        }

        // Continue avec le switch existant pour les autres modes...
        switch (_config.ExecutionMode)
        {
            case MultiBibExecutionMode.SingleRun:
                await ExecuteSingleRunAsync(cancellationToken);
                break;
            case MultiBibExecutionMode.Continuous:
                await ExecuteContinuousAsync(cancellationToken);
                break;
            case MultiBibExecutionMode.Scheduled:
                StartScheduledExecution();
                break;
            case MultiBibExecutionMode.OnDemand:
                _logger.LogInformation("🔌 On-Demand mode - service ready for API calls");
                break;
            default:
                _logger.LogWarning("⚠️ Unknown execution mode: {Mode}", _config.ExecutionMode);
                break;
        }
    }

    /// <summary>
    /// Stop Multi-BIB service gracefully
    /// </summary>
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("🛑 Multi-BIB Workflow Service Stopping...");

        // 🆕 SPRINT 14 FIX: Unsubscribe from events
        if (_dynamicBibService != null)
        {
            _dynamicBibService.BibDiscovered -= OnBibDiscoveredForProduction;
            _dynamicBibService.BibRemoved -= OnBibRemovedFromProduction;
        }

        // Stop all active production tasks
        await StopAllProductionTasksAsync();

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

        // SPRINT 14: Cleanup BitBang service
        _bitBangService?.Dispose();
        _bitBangService = null;

        _logger.LogInformation("✅ Multi-BIB Workflow Service Stopped");
    }

    // 🆕 SPRINT 14 FIX: Handle hot-add BIB discovery in production mode
    private async void OnBibDiscoveredForProduction(object? sender, BibDiscoveredEventArgs e)
    {
        if (_config.ExecutionMode != MultiBibExecutionMode.Production)
            return;

        try
        {
            _logger.LogInformation("🔥 HOT-ADD: New BIB discovered in production: {BibId}", e.BibId);

            await _productionTaskLock.WaitAsync();
            try
            {
                // Check if already running
                if (_activeProductionTasks.ContainsKey(e.BibId))
                {
                    _logger.LogInformation("ℹ️ BIB {BibId} already has active production workflow", e.BibId);
                    return;
                }

                // Start production workflow for new BIB
                _logger.LogInformation("🚀 Starting production workflow for hot-added BIB: {BibId}", e.BibId);
                
                var productionTask = ExecuteSingleBibProductionAsync(e.BibId, _cancellationTokenSource?.Token ?? CancellationToken.None);
                _activeProductionTasks[e.BibId] = productionTask;

                // Monitor task completion
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await productionTask;
                        _logger.LogInformation("✅ Production workflow completed for hot-added BIB: {BibId}", e.BibId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "❌ Production workflow failed for hot-added BIB: {BibId}", e.BibId);
                    }
                    finally
                    {
                        // Cleanup completed task
                        await _productionTaskLock.WaitAsync();
                        try
                        {
                            _activeProductionTasks.Remove(e.BibId);
                        }
                        finally
                        {
                            _productionTaskLock.Release();
                        }
                    }
                });
            }
            finally
            {
                _productionTaskLock.Release();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "💥 Error handling hot-add for BIB: {BibId}", e.BibId);
        }
    }

    private void HandleBibEvent(BibConfigurationChangedEventArgs e)
{
    _ = Task.Run(async () =>
    {
        try
        {
            _logger.LogCritical($"🔥 HOT-ADD EVENT: BIB {e.BibId} - Change: {e.ChangeType}");
            
            if (e.ChangeType == BibChangeType.Created || e.ChangeType == BibChangeType.Modified)
            {
                _logger.LogInformation($"🚀 Starting hot-add workflow for BIB: {e.BibId}");
                // Le traitement sera ajouté ici
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"❌ Hot-add event handling failed for BIB: {e.BibId}");
        }
    });
}

    // 🆕 SPRINT 14 FIX: Handle BIB removal in production mode
    private async void OnBibRemovedFromProduction(object? sender, BibRemovedEventArgs e)
    {
        if (_config.ExecutionMode != MultiBibExecutionMode.Production)
            return;

        try
        {
            _logger.LogWarning("🔥 HOT-REMOVE: BIB removed in production: {BibId}", e.BibId);

            await _productionTaskLock.WaitAsync();
            try
            {
                if (_activeProductionTasks.TryGetValue(e.BibId, out var activeTask))
                {
                    _logger.LogInformation("🛑 Stopping production workflow for removed BIB: {BibId}", e.BibId);
                    
                    // Note: We can't cancel just this task, as cancellation is global
                    // In a production system, you'd want per-task cancellation tokens
                    
                    // Wait for graceful shutdown with timeout
                    try
                    {
                        await activeTask.WaitAsync(TimeSpan.FromSeconds(30));
                    }
                    catch (TimeoutException)
                    {
                        _logger.LogWarning("⚠️ Production workflow stop timeout for BIB: {BibId}", e.BibId);
                    }
                    
                    _activeProductionTasks.Remove(e.BibId);
                    _logger.LogInformation("✅ Production workflow stopped for removed BIB: {BibId}", e.BibId);
                }
            }
            finally
            {
                _productionTaskLock.Release();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "💥 Error handling hot-remove for BIB: {BibId}", e.BibId);
        }
    }

    // 🆕 SPRINT 14 FIX: Stop all production tasks during shutdown
    private async Task StopAllProductionTasksAsync()
    {
        await _productionTaskLock.WaitAsync();
        try
        {
            if (_activeProductionTasks.Any())
            {
                _logger.LogInformation("🛑 Stopping {Count} active production workflows...", _activeProductionTasks.Count);

                _cancellationTokenSource?.Cancel();

                var stopTasks = _activeProductionTasks.Values.Select(task => 
                    task.WaitAsync(TimeSpan.FromSeconds(30)).ContinueWith(t =>
                    {
                        if (t.Exception != null)
                        {
                            _logger.LogDebug("Production task stopped with exception (expected during shutdown)");
                        }
                    }));

                await Task.WhenAll(stopTasks);

                _activeProductionTasks.Clear();
                _logger.LogInformation("✅ All production workflows stopped");
            }
        }
        finally
        {
            _productionTaskLock.Release();
        }
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

    #region Sprint 14: Production Mode Implementation

    /// <summary>
    /// SPRINT 14: Execute production mode with per UUT_ID BitBang-driven execution + Hot-Add monitoring
    /// Pattern: START-once → TEST(loop) → STOP-once per UUT_ID
    /// </summary>
private async Task ExecuteProductionModeAsync(CancellationToken cancellationToken)
{
    try
    {
        _logger.LogCritical("🚨 DEBUG: ExecuteProductionModeAsync STARTED");
        _logger.LogInformation("🏭 PRODUCTION MODE: Per UUT_ID BitBang-driven execution starting...");
        
        // Initialize BitBang service
        if (_bitBangService == null)
        {
            _logger.LogCritical("🚨 DEBUG: Creating BitBangProductionService...");
            _bitBangService = new BitBangProductionService(
                _loggerFactory.CreateLogger<BitBangProductionService>());
            _logger.LogInformation("🔌 BitBang Production Service initialized");
        }

        // Get initial BIBs
        _logger.LogCritical("🚨 DEBUG: About to discover BIBs...");
        var discoveredBibs = await DiscoverConfiguredBibsAsync();
        _logger.LogCritical($"🚨 DEBUG: Discovered {discoveredBibs.Count} BIBs: {string.Join(", ", discoveredBibs)}");
        
        // Start initial workflows
        await _productionTaskLock.WaitAsync(cancellationToken);
        try
        {
            foreach (var bibId in discoveredBibs)
            {
                if (!_activeProductionTasks.ContainsKey(bibId))
                {
                    _logger.LogInformation("🚀 Starting initial production workflow: {BibId}", bibId);
                    var productionTask = ExecuteSingleBibProductionAsync(bibId, cancellationToken);
                    _activeProductionTasks[bibId] = productionTask;
                }
            }
        }
        finally
        {
            _productionTaskLock.Release();
        }
        // Test de diagnostic direct
        _logger.LogCritical($"DEBUG: Testing direct event trigger...");
        
        // Déclencher manuellement un événement de test
        var testArgs = new BibConfigurationChangedEventArgs("test_bib", BibChangeType.Created, "test_path");
        _dynamicBibService.GetType()
            .GetEvent("BibConfigurationChanged")?
            .GetRaiseMethod(true)?
            .Invoke(_dynamicBibService, new object[] { _dynamicBibService, testArgs });

        // Continuer avec le monitoring loop...
        while (!cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(5000, cancellationToken);
        }

        // 🎯 AJOUTER CETTE PARTIE - ABONNEMENT AUX ÉVÉNEMENTS
        _logger.LogInformation("🔥 Setting up hot-add event monitoring...");
        
        _dynamicBibService.BibConfigurationChanged += async (sender, e) =>
        {
            _logger.LogCritical($"🔥 HOT-ADD EVENT: BIB {e.BibId} - Change: {e.ChangeType}");
            
            if (e.ChangeType == BibChangeType.Created || e.ChangeType == BibChangeType.Modified)
            {
                try 
                {
                    _logger.LogInformation($"🚀 Starting hot-add workflow for BIB: {e.BibId}");
                    
                    await _productionTaskLock.WaitAsync(cancellationToken);
                    try
                    {
                        if (!_activeProductionTasks.ContainsKey(e.BibId))
                        {
                            var productionTask = ExecuteSingleBibProductionAsync(e.BibId, cancellationToken);
                            _activeProductionTasks[e.BibId] = productionTask;
                            _logger.LogInformation($"✅ Hot-add workflow started for BIB: {e.BibId}");
                        }
                        else
                        {
                            _logger.LogInformation($"ℹ️ BIB {e.BibId} already has active workflow");
                        }
                    }
                    finally
                    {
                        _productionTaskLock.Release();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"❌ Hot-add workflow failed for BIB: {e.BibId}");
                }
            }
        };
        
        _logger.LogInformation("✅ Hot-add event monitoring configured successfully");

        // Monitoring loop
        _logger.LogInformation("🔄 Production mode monitoring active - waiting for hot-add events...");
        
        while (!cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(5000, cancellationToken);
            
            await _productionTaskLock.WaitAsync(cancellationToken);
            try
            {
                var activeCount = _activeProductionTasks.Count;
                if (activeCount > 0)
                {
                    _logger.LogDebug("📊 Production status: {Count} active BIB workflows", activeCount);
                }
            }
            finally
            {
                _productionTaskLock.Release();
            }
        }
        
        _logger.LogInformation("🏁 Production mode execution stopping...");
    }
    catch (OperationCanceledException)
    {
        _logger.LogInformation("🛑 Production mode cancelled");
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "💥 Production mode execution failed");
    }
    finally
    {
        _bitBangService?.Dispose();
        _bitBangService = null;
    }
}
    /// <summary>
    /// Execute production workflow for a single BIB with all its UUTs
    /// </summary>
    private async Task ExecuteSingleBibProductionAsync(string bibId, CancellationToken cancellationToken)
    {
        _logger.LogInformation($"🏭 Starting production execution for BIB: {bibId}");
        
        try
        {
            var bibConfig = await _configLoader.LoadBibConfigurationAsync(bibId);
            if (bibConfig == null)
            {
                _logger.LogError($"❌ Could not load BIB configuration: {bibId}");
                return;
            }

            var simConfig = bibConfig.HardwareSimulation;
            if (simConfig == null)
            {
                _logger.LogWarning($"⚠️ No hardware simulation config for BIB: {bibId} - using defaults");
                simConfig = new HardwareSimulationConfig { Enabled = true };
            }
            
            _logger.LogInformation($"📊 BIB {bibId}: {bibConfig.Uuts.Count} UUTs detected for production");
            _logger.LogInformation($"🎭 Simulation: {simConfig.GetSimulationSummary()}");

            var uutTasks = bibConfig.Uuts.Select(uut => 
                ExecuteUutProductionCycleAsync(bibId, uut, simConfig, cancellationToken));
                
            await Task.WhenAll(uutTasks);
            
            _logger.LogInformation($"✅ Production execution completed for BIB: {bibId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"💥 Production execution failed for BIB: {bibId}");
        }
    }

    /// <summary>
    /// Execute production cycle for a single UUT: START-once → TEST(loop) → STOP-once
    /// </summary>
    private async Task ExecuteUutProductionCycleAsync(string bibId, UutConfiguration uut,
        HardwareSimulationConfig simConfig, CancellationToken cancellationToken)
    {
        var uutId = uut.UutId;
        
        try
        {
            _logger.LogInformation($"🔧 Starting workflow with STICKY PORT for {bibId}.{uutId}");

            // 1. Wait for START signal
            var startReceived = await _bitBangService!.WaitForStartSignalAsync(uutId, simConfig);
            if (!startReceived) 
            {
                _logger.LogWarning($"⚠️ START signal not received for {uutId}");
                return;
            }

            // 2. START phase using orchestrator
            _logger.LogInformation($"🚀 Executing START phase for {bibId}.{uutId}");
            var startResult = await _orchestrator.ExecuteStartPhaseOnlyAsync(bibId, uutId, 1, $"Production_START_{bibId}_{uutId}");
            if (!startResult.IsSuccess) 
            {
                _logger.LogError($"❌ START phase failed for {bibId}.{uutId}");
                return;
            }

            // 3. TEST LOOP
            _logger.LogInformation($"🔄 Starting continuous TEST loop for {bibId}.{uutId}");
            await ExecuteContinuousTestLoopAsync(bibId, uutId, simConfig, cancellationToken);

            // 4. STOP phase
            _logger.LogInformation($"🛑 Executing STOP phase for {bibId}.{uutId}");
            await _orchestrator.ExecuteStopPhaseOnlyAsync(bibId, uutId, 1, $"Production_STOP_{bibId}_{uutId}");

            _logger.LogInformation($"✅ Production execution completed for BIB: {bibId}.{uutId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"💥 Production cycle exception: {bibId}.{uutId}");
        }
    }

    /// <summary>
    /// FIXED: Continuous TEST loop with proper phase timing
    /// </summary>
    private async Task ExecuteContinuousTestLoopAsync(string bibId, string uutId, 
        HardwareSimulationConfig simConfig, CancellationToken cancellationToken)
    {
        var cycleCount = 0;
        _logger.LogInformation($"🔄 Continuous TEST loop started: {bibId}.{uutId}");
        
        while (!cancellationToken.IsCancellationRequested)
        {
            cycleCount++;
            _logger.LogInformation($"🧪 TEST cycle #{cycleCount}: {bibId}.{uutId}");
            
            var testResult = await ExecuteTestPhaseAsync(bibId, uutId);
            
            _logger.LogInformation($"📊 TEST cycle #{cycleCount} result: {testResult}");
            
            // Test interval
            var testInterval = GetTestInterval(simConfig);
            _logger.LogInformation($"⏱️ Waiting {testInterval.TotalSeconds}s before next cycle...");
            await Task.Delay(testInterval, cancellationToken);

            // Check for STOP
            _logger.LogInformation($"🔍 Checking stop conditions for cycle #{cycleCount}...");
            var shouldStop = await CheckStopConditionsAsync(uutId, simConfig);
            _logger.LogInformation($"🛑 Should stop after cycle #{cycleCount}: {shouldStop}");
            
            if (shouldStop) 
            {
                _logger.LogInformation($"🛑 STOP condition detected after {cycleCount} cycles: {bibId}.{uutId}");
                break;
            }
            
            _logger.LogInformation($"✅ Continuing to cycle #{cycleCount + 1}...");
        }
    }
    
    /// <summary>
    /// Check STOP conditions per UUT_ID: BitBang signal or critical failure
    /// </summary>
    private async Task<bool> CheckStopConditionsAsync(string uutId, HardwareSimulationConfig simConfig)
    {
        try
        {
            // Check BitBang STOP signal (per UUT_ID)
            var stopSignal = await _bitBangService!.WaitForStopSignalAsync(uutId, simConfig);
            if (stopSignal)
            {
                _logger.LogInformation($"🛑 BitBang STOP signal received: {uutId}");
                return true;
            }

            // Check critical failure (per UUT_ID)
            var criticalFailure = await _bitBangService.CheckCriticalFailureAsync(uutId, simConfig);
            if (criticalFailure)
            {
                _logger.LogCritical($"🚨 Critical failure detected: {uutId}");
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"❌ Error checking stop conditions: {uutId}");
            return false; // Don't stop on error
        }
    }

    /// <summary>
    /// Execute START phase for UUT - CLEAN VERSION using orchestrator phases
    /// </summary>
    private async Task<bool> ExecuteStartPhaseAsync(string bibId, string uutId)
    {
        try
        {
            _logger.LogInformation($"🚀 START phase execution: {bibId}.{uutId}");
            
            // CLEAN: Use the new orchestrator phase method
            var result = await _orchestrator.ExecuteStartPhaseOnlyAsync(
                bibId, 
                uutId, 
                1, // Port 1
                $"Production_START_{bibId}_{uutId}",
                _cancellationTokenSource?.Token ?? CancellationToken.None);
            
            var success = result.IsSuccess;
            
            if (success)
            {
                _logger.LogInformation($"✅ START phase completed: {bibId}.{uutId} - {result.SuccessfulCommands}/{result.TotalCommands} commands successful");
            }
            else
            {
                // FIX: Get error message from failed command results
                var errorMessage = GetErrorMessageFromResult(result);
                _logger.LogError($"❌ START phase failed: {bibId}.{uutId} - {errorMessage}");
            }
            
            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"❌ START phase failed: {bibId}.{uutId}");
            return false;
        }
    }

    /// <summary>
    /// Execute TEST phase for UUT - CLEAN VERSION using orchestrator phases
    /// </summary>
    private async Task<bool> ExecuteTestPhaseAsync(string bibId, string uutId)
    {
        try
        {
            _logger.LogDebug($"🧪 TEST phase execution: {bibId}.{uutId}");
            
            // CLEAN: Use the new orchestrator phase method
            var result = await _orchestrator.ExecuteTestPhaseOnlyAsync(
                bibId, 
                uutId, 
                1, // Port 1
                $"Production_TEST_{bibId}_{uutId}",
                _cancellationTokenSource?.Token ?? CancellationToken.None);
            
            var success = result.IsSuccess;
            
            if (success)
            {
                _logger.LogDebug($"✅ TEST phase success: {bibId}.{uutId} - {result.SuccessfulCommands}/{result.TotalCommands} commands");
            }
            else
            {
                // FIX: Get error message from failed command results
                var errorMessage = GetErrorMessageFromResult(result);
                _logger.LogWarning($"⚠️ TEST phase failed: {bibId}.{uutId} - {errorMessage}");
            }
            
            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"❌ TEST phase exception: {bibId}.{uutId}");
            return false;
        }
    }

    /// <summary>
    /// Execute STOP phase for UUT - CLEAN VERSION using orchestrator phases
    /// </summary>
    private async Task<bool> ExecuteStopPhaseAsync(string bibId, string uutId)
    {
        try
        {
            _logger.LogInformation($"🛑 STOP phase execution: {bibId}.{uutId}");
            
            // CLEAN: Use the new orchestrator phase method
            var result = await _orchestrator.ExecuteStopPhaseOnlyAsync(
                bibId, 
                uutId, 
                1, // Port 1
                $"Production_STOP_{bibId}_{uutId}",
                _cancellationTokenSource?.Token ?? CancellationToken.None);
            
            var success = result.IsSuccess;
            
            if (success)
            {
                _logger.LogInformation($"✅ STOP phase completed: {bibId}.{uutId} - {result.SuccessfulCommands}/{result.TotalCommands} commands successful");
            }
            else
            {
                // FIX: Get error message from failed command results
                var errorMessage = GetErrorMessageFromResult(result);
                _logger.LogWarning($"⚠️ STOP phase failed: {bibId}.{uutId} - {errorMessage}");
            }
            
            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"❌ STOP phase failed: {bibId}.{uutId}");
            return false;
        }
    } 

    private async Task<PortReservation?> ReserveWorkflowPortAsync(string bibId, string uutId)
    {
        var clientId = $"Production_WORKFLOW_{bibId}_{uutId}";
        var criteria = new PortReservationCriteria 
        { 
            DefaultReservationDuration = TimeSpan.FromHours(1)
        };
        
        return await _portReservationService.ReservePortAsync(criteria, clientId);
    }

    private async Task<bool> ExecutePhaseWithFixedPortAsync(string phase, string bibId, string uutId, PortReservation reservation)
    {
        try
        {
            _logger.LogInformation($"🎯 Executing {phase} phase with fixed port {reservation.PortName} for {bibId}.{uutId}");
            
            // Call orchestrator with SPECIFIC PORT instead of letting it reserve
            var result = await _orchestrator.ExecutePhaseWithFixedPortAsync(
                phase, bibId, uutId, 1, reservation.PortName, 
                $"Production_{phase}_{bibId}_{uutId}",
                _cancellationTokenSource?.Token ?? CancellationToken.None);
            
            var success = result.IsSuccess;
            _logger.LogInformation($"✅ {phase} phase result: {success} - {result.SuccessfulCommands}/{result.TotalCommands} commands");
            
            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"❌ {phase} phase failed with fixed port {reservation.PortName}");
            return false;
        }
    }

    private async Task ExecuteContinuousTestLoopWithFixedPortAsync(string bibId, string uutId, 
        PortReservation reservation, HardwareSimulationConfig simConfig, CancellationToken cancellationToken)
    {
        var cycleCount = 0;
        _logger.LogInformation($"🔄 Starting TEST loop with STICKY PORT {reservation.PortName} for {bibId}.{uutId}");
        
        while (!cancellationToken.IsCancellationRequested)
        {
            cycleCount++;
            _logger.LogInformation($"🧪 TEST cycle #{cycleCount} with port {reservation.PortName}");
            
            // TEST with SAME fixed port
            var testResult = await ExecutePhaseWithFixedPortAsync("TEST", bibId, uutId, reservation);
            
            // Check STOP conditions
            var shouldStop = await CheckStopConditionsAsync(uutId, simConfig);
            if (shouldStop) break;
            
            await Task.Delay(GetTestInterval(simConfig), cancellationToken);
        }
    }

    /// <summary>
    /// Helper: Extract error message from CommandSequenceResult
    /// </summary>
    private string GetErrorMessageFromResult(CommandSequenceResult result)
    {
        if (result.CommandResults?.Any() == true)
        {
            // Find first failed command with error message
            var failedCommand = result.CommandResults.FirstOrDefault(cr => !cr.IsSuccess && !string.IsNullOrEmpty(cr.ErrorMessage));
            if (failedCommand != null)
            {
                return failedCommand.ErrorMessage ?? "Unknown error";
            }

            // If no specific error message, return general failure info
            var failedCount = result.CommandResults.Count(cr => !cr.IsSuccess);
            return $"{failedCount} command(s) failed";
        }

        return "No command results available";
    }

    /// <summary>
    /// Helper: Discover configured BIB IDs - FIX: Use real multi-file discovery
    /// </summary>
    private async Task<List<string>> DiscoverConfiguredBibsAsync()
    {
        try
        {
            // FIX: Use the real XmlBibConfigurationLoader multi-file discovery
            var configDir = "Configuration/"; // ou récupérer de _config.Metadata
            var bibFiles = Directory.GetFiles(configDir, "bib_*.xml");
            var discoveredBibs = new List<string>();
            
            foreach (var filePath in bibFiles)
            {
                var fileName = Path.GetFileName(filePath);
                var bibId = ExtractBibIdFromFileName(fileName);
                
                if (!string.IsNullOrEmpty(bibId))
                {
                    try
                    {
                        // Valider que le BIB peut être chargé
                        var config = await _configLoader.LoadBibConfigurationAsync(bibId);
                        if (config != null)
                        {
                            discoveredBibs.Add(bibId);
                            _logger.LogDebug($"📋 Discovered BIB: {bibId}");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, $"⚠️ Failed to load BIB: {bibId}");
                    }
                }
            }
            
            _logger.LogInformation($"📋 Multi-file discovery found {discoveredBibs.Count} BIBs: {string.Join(", ", discoveredBibs)}");
            return discoveredBibs;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error discovering BIBs");
            return new List<string>();
        }
    }

    /// <summary>
    /// Extract BIB ID from filename (bib_xyz.xml → xyz)
    /// </summary>
    private string ExtractBibIdFromFileName(string fileName)
    {
        if (fileName.StartsWith("bib_") && fileName.EndsWith(".xml"))
        {
            return fileName.Substring(4, fileName.Length - 8);
        }
        return string.Empty;
    }

    /// <summary>
    /// Helper: Check if result indicates critical failure
    /// </summary>
    private bool IsCriticalFailure(bool testResult)
    {
        // For now, simple check - will enhance in Bouchée #4 with real validation results
        return !testResult; // Any failure is considered critical for now
    }

    /// <summary>
    /// Helper: Get test interval from simulation config
    /// </summary>
    private TimeSpan GetTestInterval(HardwareSimulationConfig simConfig)
    {
        // Extract test interval from simulation config or use default
        var baseInterval = TimeSpan.FromSeconds(2); // Default 2 seconds between tests
        
        if (simConfig?.Enabled == true)
        {
            // Adjust based on simulation speed multiplier
            var adjustedInterval = baseInterval.TotalMilliseconds / simConfig.SpeedMultiplier;
            return TimeSpan.FromMilliseconds(Math.Max(100, adjustedInterval)); // Minimum 100ms
        }
        
        return baseInterval;
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
    /// SPRINT 11: Metadata for enhanced configuration options
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
    OnDemand,

    /// <summary>
    /// SPRINT 14: Production BitBang-driven mode (new default)
    /// Per UUT_ID hardware-driven START → TEST(loop) → STOP
    /// </summary>
    Production
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