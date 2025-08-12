// SerialPortPoolService/EnhancedWorker.cs - ENHANCED CLIENT DEMO avec mode boucle
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SerialPortPool.Core.Interfaces;
using SerialPortPool.Core.Models;

namespace SerialPortPoolService;

public class EnhancedWorker : BackgroundService
{
    private readonly IBibWorkflowOrchestrator _orchestrator;
    private readonly IBibConfigurationLoader _configLoader;
    private readonly ILogger<EnhancedWorker> _logger;
    private readonly ClientDemoConfiguration _config;
    private readonly Dictionary<string, object> _runtimeConfig;

    // ✅ NOUVEAU: Statistiques de performance en mode boucle
    private int _totalCycles = 0;
    private int _successfulCycles = 0;
    private int _failedCycles = 0;
    private TimeSpan _totalExecutionTime = TimeSpan.Zero;
    private DateTime _startTime;

    public EnhancedWorker(
        IBibWorkflowOrchestrator orchestrator, 
        IBibConfigurationLoader configLoader,
        ILogger<EnhancedWorker> logger,
        ClientDemoConfiguration config,
        Dictionary<string, object> runtimeConfig)
    {
        _orchestrator = orchestrator;
        _configLoader = configLoader;
        _logger = logger;
        _config = config;
        _runtimeConfig = runtimeConfig;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _startTime = DateTime.Now;
        
        _logger.LogInformation("🚀 SerialPortPool Service - Enhanced Client Demo");
        _logger.LogInformation("📋 Configuration: {XmlFile} | Loop: {LoopMode} | Interval: {Interval}s", 
            _config.XmlConfigFile, _config.LoopMode, _config.LoopIntervalSeconds);
        _logger.LogInformation("=".PadRight(80, '='));
        
        // Délai de démarrage pour laisser les services s'initialiser
        await Task.Delay(5000, stoppingToken);
        
        try
        {
            // ✅ ÉTAPE 1: Configuration du loader avec le chemin configuré
            var configPath = _runtimeConfig["config_path"] as string ?? throw new InvalidOperationException("Config path not set");
            _configLoader.SetDefaultConfigurationPath(configPath);
            
            _logger.LogInformation("📄 Configuration path set: {ConfigPath}", configPath);
            
            // ✅ ÉTAPE 2: Vérification de la configuration
            if (!File.Exists(configPath))
            {
                _logger.LogError("❌ Configuration file not found: {ConfigPath}", configPath);
                _logger.LogError("💡 Please ensure the XML configuration file exists");
                return;
            }
            
            // ✅ ÉTAPE 3: Mode d'exécution
            if (_config.LoopMode)
            {
                await ExecuteLoopMode(stoppingToken);
            }
            else
            {
                await ExecuteSingleRun(stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("🛑 Enhanced Client Demo stopped by user request");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "💥 Enhanced Client Demo Exception");
            LogTroubleshootingInformation();
        }
        finally
        {
            LogFinalStatistics();
        }
    }

    // ✅ NOUVEAU: Mode boucle continue
    private async Task ExecuteLoopMode(CancellationToken stoppingToken)
    {
        _logger.LogInformation("🔄 LOOP MODE ACTIVATED");
        _logger.LogInformation("⏱️ Interval: {Interval} seconds between cycles", _config.LoopIntervalSeconds);
        _logger.LogInformation("🛑 Press Ctrl+C to stop gracefully");
        _logger.LogInformation("");

        var cycleNumber = 0;
        var maxCycles = _config.MaxCycles;

        while (!stoppingToken.IsCancellationRequested)
        {
            cycleNumber++;
            
            // Check if we should stop (for limited demo)
            if (maxCycles.HasValue && cycleNumber > maxCycles.Value)
            {
                _logger.LogInformation("🎯 Reached maximum cycles ({MaxCycles}), stopping...", maxCycles.Value);
                break;
            }

            _logger.LogInformation("🔄 =".PadRight(50, '='));
            _logger.LogInformation("🔄 CYCLE #{CycleNumber} - {Time:HH:mm:ss}", cycleNumber, DateTime.Now);
            _logger.LogInformation("🔄 =".PadRight(50, '='));

            var cycleStartTime = DateTime.Now;
            
            try
            {
                // Exécuter le workflow
                var success = await ExecuteWorkflowCycle(cycleNumber, stoppingToken);
                
                var cycleDuration = DateTime.Now - cycleStartTime;
                _totalExecutionTime += cycleDuration;
                _totalCycles++;
                
                if (success)
                {
                    _successfulCycles++;
                    _logger.LogInformation("✅ CYCLE #{CycleNumber} COMPLETED - Duration: {Duration:F1}s", 
                        cycleNumber, cycleDuration.TotalSeconds);
                }
                else
                {
                    _failedCycles++;
                    _logger.LogError("❌ CYCLE #{CycleNumber} FAILED - Duration: {Duration:F1}s", 
                        cycleNumber, cycleDuration.TotalSeconds);
                }

                // ✅ NOUVEAU: Statistiques intermédiaires toutes les 5 cycles
                if (cycleNumber % 5 == 0)
                {
                    LogIntermediateStatistics(cycleNumber);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("🛑 Loop interrupted during cycle #{CycleNumber}", cycleNumber);
                break;
            }
            catch (Exception ex)
            {
                _failedCycles++;
                _totalCycles++;
                _logger.LogError(ex, "💥 Exception during cycle #{CycleNumber}", cycleNumber);
            }

            // ✅ NOUVEAU: Attente intelligente avec cancellation
            if (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("⏳ Waiting {Interval}s until next cycle...", _config.LoopIntervalSeconds);
                _logger.LogInformation("");
                
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(_config.LoopIntervalSeconds), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("🛑 Loop cancelled during wait period");
                    break;
                }
            }
        }

        _logger.LogInformation("🔄 Loop mode completed after {Cycles} cycles", cycleNumber);
    }

    // ✅ NOUVEAU: Exécution simple (mode original)
    private async Task ExecuteSingleRun(CancellationToken stoppingToken)
    {
        _logger.LogInformation("🎯 SINGLE RUN MODE");
        _logger.LogInformation("📋 Executing Enhanced Client Production Workflow...");
        
        var success = await ExecuteWorkflowCycle(1, stoppingToken);
        
        if (success)
        {
            _logger.LogInformation("🎉 SINGLE RUN COMPLETED SUCCESSFULLY!");
        }
        else
        {
            _logger.LogError("❌ SINGLE RUN FAILED - See details above");
        }
    }

    // ✅ NOUVEAU: Exécution d'un cycle de workflow
    private async Task<bool> ExecuteWorkflowCycle(int cycleNumber, CancellationToken stoppingToken)
    {
        try
        {
            _logger.LogInformation("🔍 BIB: client_demo");
            _logger.LogInformation("🔧 UUT: production_uut");
            _logger.LogInformation("📍 Port: 1 (auto-discover)");
            _logger.LogInformation("🏭 Client: ENHANCED_PRODUCTION_CLIENT");
            
            // ✅ Exécution du workflow enhanced
            var result = await _orchestrator.ExecuteBibWorkflowAsync(
                bibId: "client_demo",
                uutId: "production_uut", 
                portNumber: 1,
                clientId: $"ENHANCED_CLIENT_CYCLE_{cycleNumber}",
                cancellationToken: stoppingToken
            );
            
            // ✅ Affichage des résultats enhanced
            LogEnhancedWorkflowResults(result, cycleNumber);
            
            return GetWorkflowSuccess(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "💥 Workflow cycle #{CycleNumber} exception", cycleNumber);
            return false;
        }
    }

    // ✅ NOUVEAU: Logging amélioré des résultats
    private void LogEnhancedWorkflowResults(object result, int cycleNumber)
    {
        var resultType = result.GetType();
        
        try
        {
            var successProperty = resultType.GetProperty("Success");
            var isSuccess = successProperty?.GetValue(result) as bool? ?? false;
            
            _logger.LogInformation("📊 =".PadRight(50, '='));
            _logger.LogInformation("📊 CYCLE #{CycleNumber} RESULTS", cycleNumber);
            _logger.LogInformation("📊 =".PadRight(50, '='));
            
            if (isSuccess)
            {
                _logger.LogInformation("🎉 CYCLE STATUS: ✅ SUCCESS");
                
                // Phase results
                var startResult = resultType.GetProperty("StartResult")?.GetValue(result);
                var testResult = resultType.GetProperty("TestResult")?.GetValue(result);
                var stopResult = resultType.GetProperty("StopResult")?.GetValue(result);
                
                _logger.LogInformation("📋 PHASE RESULTS:");
                _logger.LogInformation("   🔋 Start Phase: {Result}", GetPhaseResult(startResult));
                _logger.LogInformation("   🧪 Test Phase: {Result}", GetPhaseResult(testResult));  
                _logger.LogInformation("   🔌 Stop Phase: {Result}", GetPhaseResult(stopResult));
                
                // Timing et port info
                LogTimingAndPortInfo(result);
                
                // ✅ NOUVEAU: Informations enhanced pour mode boucle
                if (_config.LoopMode)
                {
                    var successRate = _totalCycles > 0 ? (_successfulCycles * 100.0 / _totalCycles) : 0;
                    _logger.LogInformation("📈 Success Rate: {SuccessRate:F1}% ({Success}/{Total})", 
                        successRate, _successfulCycles, _totalCycles);
                }
            }
            else
            {
                _logger.LogError("❌ CYCLE STATUS: FAILED");
                
                var errorProperty = resultType.GetProperty("ErrorMessage");
                var errorMessage = errorProperty?.GetValue(result) as string;
                
                if (!string.IsNullOrEmpty(errorMessage))
                {
                    _logger.LogError("💬 Error Details: {Error}", errorMessage);
                }
                
                // ✅ NOUVEAU: Troubleshooting contextuel pour mode boucle
                if (_config.LoopMode)
                {
                    _logger.LogWarning("🔄 Continuing loop - next attempt in {Interval}s", _config.LoopIntervalSeconds);
                }
                else
                {
                    LogTroubleshootingInformation();
                }
            }
            
            _logger.LogInformation("📊 =".PadRight(50, '='));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error displaying cycle #{CycleNumber} results", cycleNumber);
            _logger.LogInformation("Raw result: {Result}", result.ToString());
        }
    }

    // ✅ NOUVEAU: Statistiques intermédiaires
    private void LogIntermediateStatistics(int currentCycle)
    {
        var uptime = DateTime.Now - _startTime;
        var avgCycleDuration = _totalCycles > 0 ? _totalExecutionTime.TotalSeconds / _totalCycles : 0;
        var successRate = _totalCycles > 0 ? (_successfulCycles * 100.0 / _totalCycles) : 0;
        
        _logger.LogInformation("📊 INTERMEDIATE STATISTICS (Cycle #{CurrentCycle})", currentCycle);
        _logger.LogInformation("   ⏱️ Uptime: {Uptime:hh\\:mm\\:ss}", uptime);
        _logger.LogInformation("   🔄 Total Cycles: {Total}", _totalCycles);
        _logger.LogInformation("   ✅ Successful: {Successful} ({SuccessRate:F1}%)", _successfulCycles, successRate);
        _logger.LogInformation("   ❌ Failed: {Failed}", _failedCycles);
        _logger.LogInformation("   ⚡ Avg Cycle Duration: {AvgDuration:F1}s", avgCycleDuration);
        _logger.LogInformation("");
    }

    // ✅ NOUVEAU: Statistiques finales
    private void LogFinalStatistics()
    {
        var totalUptime = DateTime.Now - _startTime;
        var avgCycleDuration = _totalCycles > 0 ? _totalExecutionTime.TotalSeconds / _totalCycles : 0;
        var successRate = _totalCycles > 0 ? (_successfulCycles * 100.0 / _totalCycles) : 0;
        
        _logger.LogInformation("📊 =".PadRight(60, '='));
        _logger.LogInformation("📊 FINAL ENHANCED DEMO STATISTICS");
        _logger.LogInformation("📊 =".PadRight(60, '='));
        _logger.LogInformation("🕐 Total Runtime: {Runtime:hh\\:mm\\:ss}", totalUptime);
        _logger.LogInformation("🔄 Total Cycles Executed: {Total}", _totalCycles);
        _logger.LogInformation("✅ Successful Cycles: {Successful}", _successfulCycles);
        _logger.LogInformation("❌ Failed Cycles: {Failed}", _failedCycles);
        _logger.LogInformation("📈 Overall Success Rate: {SuccessRate:F1}%", successRate);
        _logger.LogInformation("⚡ Average Cycle Duration: {AvgDuration:F1} seconds", avgCycleDuration);
        _logger.LogInformation("📄 Configuration Used: {ConfigFile}", _config.XmlConfigFile);
        
        if (_config.LoopMode)
        {
            _logger.LogInformation("🔄 Loop Interval: {Interval} seconds", _config.LoopIntervalSeconds);
            var cyclesPerHour = 3600.0 / _config.LoopIntervalSeconds;
            _logger.LogInformation("📊 Potential Cycles/Hour: {CyclesPerHour:F0}", cyclesPerHour);
        }
        
        _logger.LogInformation("📊 =".PadRight(60, '='));
        
        // ✅ Recommandations basées sur les performances
        if (_totalCycles > 0)
        {
            if (successRate >= 95)
            {
                _logger.LogInformation("🏆 EXCELLENT: System is performing optimally!");
            }
            else if (successRate >= 80)
            {
                _logger.LogWarning("⚠️ GOOD: System is stable but some improvement possible");
            }
            else
            {
                _logger.LogError("🔧 NEEDS ATTENTION: Success rate below 80% - check configuration and hardware");
            }
        }
    }

    // Helper methods
    private void LogTimingAndPortInfo(object result)
    {
        var resultType = result.GetType();
        
        // Timing info
        var durationProperty = resultType.GetProperty("Duration");
        if (durationProperty?.GetValue(result) is TimeSpan duration)
        {
            _logger.LogInformation("⏱️ Cycle Duration: {Duration:F1} seconds", duration.TotalSeconds);
        }
        
        // Port info
        var physicalPortProperty = resultType.GetProperty("PhysicalPort");
        var physicalPort = physicalPortProperty?.GetValue(result) as string;
        if (!string.IsNullOrEmpty(physicalPort))
        {
            _logger.LogInformation("🔌 Port Used: {Port}", physicalPort);
        }
        
        // Protocol info
        var protocolProperty = resultType.GetProperty("ProtocolName");
        var protocol = protocolProperty?.GetValue(result) as string;
        if (!string.IsNullOrEmpty(protocol))
        {
            _logger.LogInformation("📡 Protocol: {Protocol}", protocol.ToUpper());
        }
    }

    private void LogTroubleshootingInformation()
    {
        _logger.LogError("🔧 TROUBLESHOOTING CHECKLIST:");
        _logger.LogError("   • Verify FT4232 device is connected and drivers installed");
        _logger.LogError("   • Check if dummy UUT is running: python dummy_uut.py --port COM8");
        _logger.LogError("   • Ensure no other software is using the serial ports");
        _logger.LogError("   • Verify XML configuration file exists and is valid");
        _logger.LogError("   • Check port mapping in BIB configuration");
        _logger.LogError("   • Try running with different --xml-config file");
        _logger.LogError("   • Verify COM port assignments in Device Manager");
    }
    
    private string GetPhaseResult(object? phaseResult)
    {
        if (phaseResult == null) return "❓ UNKNOWN";
        
        try
        {
            var successProperty = phaseResult.GetType().GetProperty("IsSuccess");
            var isSuccess = successProperty?.GetValue(phaseResult) as bool? ?? false;
            return isSuccess ? "✅ SUCCESS" : "❌ FAILED";
        }
        catch
        {
            return "❓ UNKNOWN";
        }
    }

    private bool GetWorkflowSuccess(object result)
    {
        try
        {
            var successProperty = result.GetType().GetProperty("Success");
            return successProperty?.GetValue(result) as bool? ?? false;
        }
        catch
        {
            return false;
        }
    }
}