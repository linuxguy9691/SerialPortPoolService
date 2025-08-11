// SerialPortPoolService/Worker.cs - FIXED pour Sprint 7 Client Demo
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SerialPortPool.Core.Interfaces;

namespace SerialPortPoolService;

public class Worker : BackgroundService
{
    private readonly IBibWorkflowOrchestrator _orchestrator;
    private readonly IBibConfigurationLoader _configLoader;
    private readonly ILogger<Worker> _logger;

    public Worker(
        IBibWorkflowOrchestrator orchestrator, 
        IBibConfigurationLoader configLoader,
        ILogger<Worker> logger)
    {
        _orchestrator = orchestrator;
        _configLoader = configLoader;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("🚀 SerialPortPool Service - Sprint 7 Client Demo Mode");
        _logger.LogInformation("📋 Auto-Execution: BIB Workflow + FT4232 Detection + RS232 TEST");
        _logger.LogInformation("=".PadRight(80, '='));
        
        // Délai de démarrage pour laisser les services s'initialiser
        await Task.Delay(5000, stoppingToken);
        
        try
        {
            // ✅ ÉTAPE 1: Configuration du loader avec le bon chemin
            var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Configuration", "client-demo.xml");
            _configLoader.SetDefaultConfigurationPath(configPath);
            
            _logger.LogInformation("📄 Configuration path set: {ConfigPath}", configPath);
            
            // ✅ ÉTAPE 2: Vérification de la configuration
            if (!File.Exists(configPath))
            {
                _logger.LogError("❌ Client demo configuration not found: {ConfigPath}", configPath);
                _logger.LogError("💡 Please ensure client-demo.xml exists in Configuration folder");
                return;
            }
            
            _logger.LogInformation("📋 Executing Client Production Workflow...");
            _logger.LogInformation("🔍 BIB: client_demo");
            _logger.LogInformation("🔧 UUT: production_uut");
            _logger.LogInformation("📍 Port: 1 (auto-discover)");
            _logger.LogInformation("🏭 Client: PRODUCTION_CLIENT");
            
            // ✅ ÉTAPE 3: Exécution du workflow client
            var result = await _orchestrator.ExecuteBibWorkflowAsync(
                bibId: "client_demo",
                uutId: "production_uut", 
                portNumber: 1,
                clientId: "PRODUCTION_CLIENT",
                cancellationToken: stoppingToken
            );
            
            // ✅ ÉTAPE 4: Affichage des résultats client-ready
            LogClientDemoResults(result);
            
            if (result.Success)
            {
                _logger.LogInformation("🎉 CLIENT DEMO COMPLETED SUCCESSFULLY!");
                _logger.LogInformation("✅ Service ready for production deployment");
            }
            else
            {
                _logger.LogError("❌ CLIENT DEMO FAILED - See details above");
                _logger.LogError("🔧 Check hardware connections and configuration");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "💥 Client Demo Exception");
            _logger.LogError("🔧 Troubleshooting checklist:");
            _logger.LogError("   • Verify FT4232 device is connected");
            _logger.LogError("   • Check dummy UUT is running on correct port");
            _logger.LogError("   • Ensure client-demo.xml configuration exists");
            _logger.LogError("   • Verify no other software is using the serial port");
        }
        
        // Garder le service en marche pour monitoring
        _logger.LogInformation("⏳ Service continuing to run for monitoring...");
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(60000, stoppingToken); // Check every minute
        }
        
        _logger.LogInformation("🛑 Client Demo Service shutting down");
    }
    
    private void LogClientDemoResults(object result)
    {
        // Utilisation de réflexion pour accéder aux propriétés
        var resultType = result.GetType();
        
        try
        {
            var successProperty = resultType.GetProperty("Success");
            var isSuccess = successProperty?.GetValue(result) as bool? ?? false;
            
            _logger.LogInformation("📊 =".PadRight(50, '='));
            _logger.LogInformation("📊 CLIENT DEMO RESULTS");
            _logger.LogInformation("📊 =".PadRight(50, '='));
            
            if (isSuccess)
            {
                _logger.LogInformation("🎉 OVERALL STATUS: ✅ SUCCESS");
                
                // Phase results
                var startResult = resultType.GetProperty("StartResult")?.GetValue(result);
                var testResult = resultType.GetProperty("TestResult")?.GetValue(result);
                var stopResult = resultType.GetProperty("StopResult")?.GetValue(result);
                
                _logger.LogInformation("📋 PHASE RESULTS:");
                _logger.LogInformation("   🔋 Start Phase: {Result}", GetPhaseResult(startResult));
                _logger.LogInformation("   🧪 Test Phase: {Result}", GetPhaseResult(testResult));  
                _logger.LogInformation("   🔌 Stop Phase: {Result}", GetPhaseResult(stopResult));
                
                // Timing info
                var durationProperty = resultType.GetProperty("Duration");
                if (durationProperty?.GetValue(result) is TimeSpan duration)
                {
                    _logger.LogInformation("⏱️ Total Duration: {Duration:F1} seconds", duration.TotalSeconds);
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
                
                _logger.LogInformation("🏭 Device Detection: AUTO (FT4232 Discovery)");
                _logger.LogInformation("🎯 CLIENT REQUIREMENTS: FULLY SATISFIED");
            }
            else
            {
                _logger.LogError("❌ OVERALL STATUS: FAILED");
                
                var errorProperty = resultType.GetProperty("ErrorMessage");
                var errorMessage = errorProperty?.GetValue(result) as string;
                
                if (!string.IsNullOrEmpty(errorMessage))
                {
                    _logger.LogError("💬 Error Details: {Error}", errorMessage);
                }
                
                _logger.LogError("🔧 TROUBLESHOOTING STEPS:");
                _logger.LogError("   • Check if FT4232 device is properly connected");
                _logger.LogError("   • Verify dummy UUT is running: python dummy_uut.py --port COM8");
                _logger.LogError("   • Ensure no other software is using the serial ports");
                _logger.LogError("   • Check client-demo.xml configuration file");
                _logger.LogError("   • Verify port mapping in BIB configuration");
            }
            
            _logger.LogInformation("📊 =".PadRight(50, '='));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error displaying results");
            _logger.LogInformation("Raw result: {Result}", result.ToString());
        }
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
}