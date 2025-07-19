using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using SerialPortPool.Core.Interfaces;
using SerialPortPool.Core.Models;

namespace SerialPortPoolService.Services;

/// <summary>
/// Background service for continuous serial port discovery and monitoring
/// </summary>
public class PortDiscoveryBackgroundService : BackgroundService
{
    private readonly ILogger<PortDiscoveryBackgroundService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly TimeSpan _discoveryInterval = TimeSpan.FromSeconds(30);
    
    // State tracking for change detection
    private List<SerialPortInfo> _lastKnownPorts = new();
    private DateTime _lastDiscoveryTime = DateTime.MinValue;

    public PortDiscoveryBackgroundService(
        ILogger<PortDiscoveryBackgroundService> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("🔄 Background Discovery Service starting...");
        _logger.LogInformation($"📊 Discovery interval: {_discoveryInterval.TotalSeconds} seconds");

        try
        {
            // Initial discovery
            await PerformDiscoveryAsync(isInitial: true);

            // Periodic discovery loop
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(_discoveryInterval, stoppingToken);
                    await PerformDiscoveryAsync(isInitial: false);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Background Discovery Service cancellation requested");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in background discovery loop - continuing...");
                    // Continue the loop even if one iteration fails
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Background Discovery Service stopped via cancellation");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error in Background Discovery Service");
            throw; // Re-throw fatal errors
        }
        finally
        {
            _logger.LogInformation("🛑 Background Discovery Service stopped");
        }
    }

    /// <summary>
    /// Perform a single discovery cycle
    /// </summary>
    private async Task PerformDiscoveryAsync(bool isInitial)
    {
        try
        {
            var discoveryStartTime = DateTime.Now;
            
            _logger.LogDebug($"🔍 Starting discovery cycle (Initial: {isInitial})...");

            // Create a new scope for DI services (important for background services)
            using var scope = _serviceProvider.CreateScope();
            var discovery = scope.ServiceProvider.GetRequiredService<ISerialPortDiscovery>();

            // Perform discovery
            var discoveredPorts = await discovery.DiscoverPortsAsync();
            var portList = discoveredPorts.ToList();

            var discoveryDuration = DateTime.Now - discoveryStartTime;
            _logger.LogDebug($"⏱️ Discovery completed in {discoveryDuration.TotalMilliseconds:F0}ms");

            if (isInitial)
            {
                await HandleInitialDiscovery(portList);
            }
            else
            {
                await HandlePeriodicDiscovery(portList);
            }

            // Update state
            _lastKnownPorts = portList;
            _lastDiscoveryTime = discoveryStartTime;

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during discovery cycle");
            // Don't re-throw - let the service continue
        }
    }

    /// <summary>
    /// Handle initial discovery on service startup
    /// </summary>
    private async Task HandleInitialDiscovery(List<SerialPortInfo> ports)
    {
        _logger.LogInformation("📡 Initial Discovery Results:");
        _logger.LogInformation($"📊 Found {ports.Count} serial port(s)");

        if (ports.Count == 0)
        {
            _logger.LogInformation("❌ No serial ports detected");
            return;
        }

        // Log details of each port
        foreach (var port in ports)
        {
            var ftdiStatus = port.IsFtdiDevice ? $"FTDI {port.FtdiChipType}" : "Non-FTDI";
            var validStatus = port.IsValidForPool ? "VALID" : "INVALID";
            
            _logger.LogInformation($"  📍 {port.PortName}: {port.FriendlyName}");
            _logger.LogInformation($"      🏭 Type: {ftdiStatus} | Status: {validStatus}");
            
            if (port.IsFtdiDevice && port.FtdiInfo != null)
            {
                _logger.LogDebug($"      🔍 VID/PID: {port.FtdiInfo.VendorId}/{port.FtdiInfo.ProductId}, Serial: {port.FtdiInfo.SerialNumber}");
            }

            if (port.ValidationResult != null)
            {
                _logger.LogDebug($"      📋 Validation: {port.ValidationResult.Reason} (Score: {port.ValidationResult.ValidationScore}%)");
            }
        }

        // Summary
        var ftdiCount = ports.Count(p => p.IsFtdiDevice);
        var validCount = ports.Count(p => p.IsValidForPool);
        
        _logger.LogInformation($"📊 Summary: {ftdiCount} FTDI devices, {validCount} valid for pool");

        await Task.CompletedTask; // Placeholder for async operations
    }

    /// <summary>
    /// Handle periodic discovery and detect changes
    /// </summary>
    private async Task HandlePeriodicDiscovery(List<SerialPortInfo> currentPorts)
    {
        _logger.LogDebug($"🔄 Periodic discovery: {currentPorts.Count} ports found");

        // Detect changes
        var changes = DetectPortChanges(_lastKnownPorts, currentPorts);
        
        if (changes.HasChanges)
        {
            _logger.LogInformation("📡 Port changes detected:");
            
            // Log new ports
            foreach (var newPort in changes.NewPorts)
            {
                var ftdiStatus = newPort.IsFtdiDevice ? $"FTDI {newPort.FtdiChipType}" : "Non-FTDI";
                _logger.LogInformation($"  ➕ CONNECTED: {newPort.PortName} ({newPort.FriendlyName}) [{ftdiStatus}]");
                
                if (newPort.IsFtdiDevice && newPort.FtdiInfo != null)
                {
                    _logger.LogInformation($"      🔍 VID/PID: {newPort.FtdiInfo.VendorId}/{newPort.FtdiInfo.ProductId}, Serial: {newPort.FtdiInfo.SerialNumber}");
                }
            }
            
            // Log removed ports
            foreach (var removedPort in changes.RemovedPorts)
            {
                var ftdiStatus = removedPort.IsFtdiDevice ? $"FTDI {removedPort.FtdiChipType}" : "Non-FTDI";
                _logger.LogInformation($"  ➖ DISCONNECTED: {removedPort.PortName} ({removedPort.FriendlyName}) [{ftdiStatus}]");
            }
            
            // Log summary
            _logger.LogInformation($"📊 Changes: +{changes.NewPorts.Count} added, -{changes.RemovedPorts.Count} removed");
        }
        else
        {
            _logger.LogDebug("📊 No port changes detected");
        }

        await Task.CompletedTask; // Placeholder for async operations
    }

    /// <summary>
    /// Detect changes between previous and current port lists
    /// </summary>
    private PortChanges DetectPortChanges(List<SerialPortInfo> previousPorts, List<SerialPortInfo> currentPorts)
    {
        var previousPortNames = previousPorts.Select(p => p.PortName).ToHashSet();
        var currentPortNames = currentPorts.Select(p => p.PortName).ToHashSet();

        var newPortNames = currentPortNames.Except(previousPortNames).ToList();
        var removedPortNames = previousPortNames.Except(currentPortNames).ToList();

        var newPorts = currentPorts.Where(p => newPortNames.Contains(p.PortName)).ToList();
        var removedPorts = previousPorts.Where(p => removedPortNames.Contains(p.PortName)).ToList();

        return new PortChanges
        {
            NewPorts = newPorts,
            RemovedPorts = removedPorts,
            HasChanges = newPorts.Any() || removedPorts.Any()
        };
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("🛑 Background Discovery Service stop requested...");
        await base.StopAsync(cancellationToken);
        _logger.LogInformation("✅ Background Discovery Service stopped successfully");
    }
}

/// <summary>
/// Represents changes in port discovery between two scans
/// </summary>
public class PortChanges
{
    public List<SerialPortInfo> NewPorts { get; set; } = new();
    public List<SerialPortInfo> RemovedPorts { get; set; } = new();
    public bool HasChanges { get; set; }
}