// Remplacer le Worker.cs avec TOUS les usings nécessaires :
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SerialPortPool.Core.Interfaces;

namespace SerialPortPoolService;

public class Worker : BackgroundService
{
    private readonly IBibWorkflowOrchestrator _orchestrator;
    private readonly ILogger<Worker> _logger;

    public Worker(IBibWorkflowOrchestrator orchestrator, ILogger<Worker> logger)
    {
        _orchestrator = orchestrator;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("🚀 SerialPortPool Service - Client Demo Mode");
        
        await Task.Delay(5000, stoppingToken);
        
        try
        {
            _logger.LogInformation("📋 Executing Client Demo Workflow...");
            
            var result = await _orchestrator.ExecuteBibWorkflowAsync(
                bibId: "client_demo",
                uutId: "production_uut", 
                portNumber: 1,
                clientId: "PRODUCTION_CLIENT"
            );
            
            if (result.Success)
            {
                _logger.LogInformation("🎉 CLIENT DEMO SUCCESS!");
                _logger.LogInformation("📊 Start: {StartResult}", result.StartResult?.IsSuccess);
                _logger.LogInformation("📊 Test: {TestResult}", result.TestResult?.IsSuccess);  
                _logger.LogInformation("📊 Stop: {StopResult}", result.StopResult?.IsSuccess);
                _logger.LogInformation("⏱️ Duration: {Duration:F1} seconds", result.Duration.TotalSeconds);
            }
            else
            {
                _logger.LogError("❌ Client Demo Failed: {Error}", result.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "💥 Client Demo Exception");
        }
        
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(60000, stoppingToken);
        }
    }
}