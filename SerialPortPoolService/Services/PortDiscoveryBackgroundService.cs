using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SerialPortPool.Core.Interfaces;
using SerialPortPool.Core.Models;
using SerialPortPool.Core.Services;

namespace SerialPortPoolService.Services;

/// <summary>
/// Background service for continuous port discovery with enhanced device grouping support
/// </summary>
public class PortDiscoveryBackgroundService : BackgroundService
{
    private readonly ILogger<PortDiscoveryBackgroundService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly TimeSpan _discoveryInterval = TimeSpan.FromSeconds(30);

    // State tracking
    private List<SerialPortInfo> _lastKnownPorts = new();
    private List<DeviceGroup> _lastKnownDeviceGroups = new();
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
        _logger.LogInformation("🔄 PortDiscoveryBackgroundService started with enhanced device grouping");
        _logger.LogInformation($"📅 Discovery interval: {_discoveryInterval.TotalSeconds} seconds");

        try
        {
            // Initial discovery with device grouping
            await PerformEnhancedDiscoveryAsync(isInitial: true);

            // Continuous discovery loop
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(_discoveryInterval, stoppingToken);
                    await PerformEnhancedDiscoveryAsync(isInitial: false);
                }
                catch (OperationCanceledException)
                {
                    // Expected when cancellation is requested
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Error during periodic discovery cycle");
                    // Continue running despite errors
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "💥 Fatal error in PortDiscoveryBackgroundService");
        }
        finally
        {
            _logger.LogInformation("🛑 PortDiscoveryBackgroundService stopped");
        }
    }

    /// <summary>
    /// Enhanced discovery with device grouping (main discovery method)
    /// </summary>
    private async Task PerformEnhancedDiscoveryAsync(bool isInitial)
    {
        try
        {
            var discoveryStartTime = DateTime.Now;
            
            _logger.LogDebug($"🔍 Starting enhanced discovery with device grouping (Initial: {isInitial})...");

            // Create a new scope for DI services
            using var scope = _serviceProvider.CreateScope();
            var discovery = scope.ServiceProvider.GetRequiredService<ISerialPortDiscovery>() as EnhancedSerialPortDiscoveryService;

            if (discovery == null)
            {
                _logger.LogWarning("⚠️ Enhanced Discovery Service not available, falling back to basic discovery");
                await PerformBasicDiscoveryAsync(isInitial);
                return;
            }

            // PHASE 1: Traditional port discovery
            var discoveredPorts = await discovery.DiscoverPortsAsync();
            var portList = discoveredPorts.ToList();

            // PHASE 2: Device grouping discovery (NEW)
            var deviceGroups = await discovery.DiscoverDeviceGroupsAsync();
            var groupList = deviceGroups.ToList();

            var discoveryDuration = DateTime.Now - discoveryStartTime;
            _logger.LogDebug($"⏱️ Enhanced discovery completed in {discoveryDuration.TotalMilliseconds:F0}ms");

            if (isInitial)
            {
                await HandleInitialEnhancedDiscovery(portList, groupList);
            }
            else
            {
                await HandlePeriodicEnhancedDiscovery(portList, groupList);
            }

            // Update state
            _lastKnownPorts = portList;
            _lastKnownDeviceGroups = groupList;
            _lastDiscoveryTime = discoveryStartTime;

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error during enhanced discovery cycle with device grouping");
            // Fallback to basic discovery
            await PerformBasicDiscoveryAsync(isInitial);
        }
    }

    /// <summary>
    /// Handle initial enhanced discovery with device grouping
    /// </summary>
    private async Task HandleInitialEnhancedDiscovery(List<SerialPortInfo> ports, List<DeviceGroup> deviceGroups)
    {
        _logger.LogInformation("📡 Initial Enhanced Discovery Results with Device Grouping:");
        _logger.LogInformation($"📊 Found {ports.Count} individual ports in {deviceGroups.Count} physical devices");

        if (deviceGroups.Count == 0)
        {
            _logger.LogInformation("❌ No device groups detected");
            return;
        }

        // Log device groups
        foreach (var group in deviceGroups.OrderBy(g => g.DeviceId))
        {
            var deviceIcon = group.IsFtdiDevice ? "🏭" : "🔌";
            var validIcon = group.IsClientValidDevice ? "✅" : "❌";
            var multiPortIcon = group.IsMultiPortDevice ? "🔀" : "📌";
            
            _logger.LogInformation($"  {deviceIcon} {validIcon} {multiPortIcon} {group.DeviceTypeDescription}");
            _logger.LogInformation($"      📍 Ports ({group.PortCount}): {string.Join(", ", group.GetPortNames())}");
            
            if (group.IsFtdiDevice && group.DeviceInfo != null)
            {
                _logger.LogDebug($"      🔍 VID/PID: {group.DeviceInfo.VendorId}/{group.DeviceInfo.ProductId}, Serial: {group.SerialNumber}");
            }
        }

        // Summary statistics
        var ftdiCount = deviceGroups.Count(g => g.IsFtdiDevice);
        var multiPortCount = deviceGroups.Count(g => g.IsMultiPortDevice);
        var clientValidCount = deviceGroups.Count(g => g.IsClientValidDevice);
        
        _logger.LogInformation($"📊 Device Summary: {ftdiCount} FTDI, {multiPortCount} multi-port, {clientValidCount} client-valid");

        await Task.CompletedTask;
    }

    /// <summary>
    /// Handle periodic enhanced discovery with device grouping change detection
    /// </summary>
    private async Task HandlePeriodicEnhancedDiscovery(List<SerialPortInfo> currentPorts, List<DeviceGroup> currentDeviceGroups)
    {
        _logger.LogDebug($"🔄 Enhanced periodic discovery: {currentPorts.Count} ports in {currentDeviceGroups.Count} devices");

        // Enhanced change detection at device level
        var changes = DetectDeviceGroupChanges(_lastKnownPorts, currentPorts, currentDeviceGroups);
        
        if (changes.HasChanges)
        {
            _logger.LogInformation("📡 Device-level changes detected:");
            
            // Log new devices
            foreach (var newDevice in changes.NewDevices)
            {
                var deviceType = newDevice.IsFtdiDevice ? $"FTDI {newDevice.DeviceInfo?.ChipType}" : "Non-FTDI";
                var portNames = string.Join(", ", newDevice.GetPortNames());
                _logger.LogInformation($"  ➕ NEW DEVICE: {newDevice.DeviceTypeDescription} - Ports: {portNames}");
            }
            
            // Log removed devices
            foreach (var removedDevice in changes.RemovedDevices)
            {
                var deviceType = removedDevice.IsFtdiDevice ? $"FTDI {removedDevice.DeviceInfo?.ChipType}" : "Non-FTDI";
                var portNames = string.Join(", ", removedDevice.GetPortNames());
                _logger.LogInformation($"  ➖ REMOVED DEVICE: {removedDevice.DeviceTypeDescription} - Ports: {portNames}");
            }
            
            _logger.LogInformation($"📊 Device Changes: +{changes.NewDevices.Count} devices, -{changes.RemovedDevices.Count} devices");
        }
        else
        {
            _logger.LogDebug("📊 No device-level changes detected");
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// Detect device-level changes (enhanced version)
    /// </summary>
    private DeviceGroupChanges DetectDeviceGroupChanges(List<SerialPortInfo> previousPorts, List<SerialPortInfo> currentPorts, List<DeviceGroup> currentDeviceGroups)
    {
        // For now, detect at port level and infer device changes
        var portChanges = DetectPortChanges(previousPorts, currentPorts);
        
        // Convert to device group changes
        var newDevices = new List<DeviceGroup>();
        var removedDevices = new List<DeviceGroup>();
        
        // Find devices with new ports
        foreach (var group in currentDeviceGroups)
        {
            var groupPortNames = group.GetPortNames();
            var hasNewPort = portChanges.NewPorts.Any(p => groupPortNames.Contains(p.PortName));
            
            if (hasNewPort)
            {
                newDevices.Add(group);
            }
        }
        
        // For removed devices, we'd need to track previous device groups
        // For now, approximate based on removed ports
        
        return new DeviceGroupChanges
        {
            NewDevices = newDevices,
            RemovedDevices = removedDevices,
            HasChanges = newDevices.Any() || removedDevices.Any()
        };
    }

    /// <summary>
    /// Fallback to basic discovery if enhanced discovery fails
    /// </summary>
    private async Task PerformBasicDiscoveryAsync(bool isInitial)
    {
        try
        {
            var discoveryStartTime = DateTime.Now;
            
            _logger.LogDebug($"🔍 Starting basic discovery (fallback mode, Initial: {isInitial})...");

            using var scope = _serviceProvider.CreateScope();
            var discovery = scope.ServiceProvider.GetRequiredService<ISerialPortDiscovery>();

            var discoveredPorts = await discovery.DiscoverPortsAsync();
            var portList = discoveredPorts.ToList();

            var discoveryDuration = DateTime.Now - discoveryStartTime;
            _logger.LogDebug($"⏱️ Basic discovery completed in {discoveryDuration.TotalMilliseconds:F0}ms");

            if (isInitial)
            {
                await HandleInitialDiscovery(portList);
            }
            else
            {
                await HandlePeriodicDiscovery(portList);
            }

            _lastKnownPorts = portList;
            _lastDiscoveryTime = discoveryStartTime;

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error during basic discovery cycle");
        }
    }

    /// <summary>
    /// Handle initial discovery (basic mode)
    /// </summary>
    private async Task HandleInitialDiscovery(List<SerialPortInfo> ports)
    {
        _logger.LogInformation("📡 Initial Discovery Results:");
        _logger.LogInformation($"📊 Found {ports.Count} serial ports");

        foreach (var port in ports.OrderBy(p => p.PortName))
        {
            var ftdiStatus = port.IsFtdiDevice ? $"FTDI {port.FtdiChipType}" : "Non-FTDI";
            var validStatus = port.IsValidForPool ? "✅ VALID" : "❌ INVALID";
            
            _logger.LogInformation($"  📍 {port.PortName}: {port.FriendlyName} [{ftdiStatus}] [{validStatus}]");
            
            if (port.IsFtdiDevice && port.FtdiInfo != null)
            {
                _logger.LogDebug($"      🔍 VID/PID: {port.FtdiInfo.VendorId}/{port.FtdiInfo.ProductId}, Serial: {port.FtdiInfo.SerialNumber}");
            }
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// Handle periodic discovery with change detection (basic mode)
    /// </summary>
    private async Task HandlePeriodicDiscovery(List<SerialPortInfo> currentPorts)
    {
        _logger.LogDebug($"🔄 Periodic discovery: {currentPorts.Count} ports found");

        var changes = DetectPortChanges(_lastKnownPorts, currentPorts);
        
        if (changes.HasChanges)
        {
            _logger.LogInformation("📡 Port changes detected:");
            
            foreach (var newPort in changes.NewPorts)
            {
                var ftdiStatus = newPort.IsFtdiDevice ? $"FTDI {newPort.FtdiChipType}" : "Non-FTDI";
                _logger.LogInformation($"  ➕ NEW: {newPort.PortName} - {newPort.FriendlyName} [{ftdiStatus}]");
            }
            
            foreach (var removedPort in changes.RemovedPorts)
            {
                _logger.LogInformation($"  ➖ REMOVED: {removedPort.PortName} - {removedPort.FriendlyName}");
            }
            
            _logger.LogInformation($"📊 Port Changes: +{changes.NewPorts.Count} ports, -{changes.RemovedPorts.Count} ports");
        }
        else
        {
            _logger.LogDebug("📊 No port changes detected");
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// Detect changes between previous and current port lists
    /// </summary>
    private PortChanges DetectPortChanges(List<SerialPortInfo> previousPorts, List<SerialPortInfo> currentPorts)
    {
        var previousPortNames = previousPorts.Select(p => p.PortName).ToHashSet();
        var currentPortNames = currentPorts.Select(p => p.PortName).ToHashSet();

        var newPortNames = currentPortNames.Except(previousPortNames).ToHashSet();
        var removedPortNames = previousPortNames.Except(currentPortNames).ToHashSet();

        var newPorts = currentPorts.Where(p => newPortNames.Contains(p.PortName)).ToList();
        var removedPorts = previousPorts.Where(p => removedPortNames.Contains(p.PortName)).ToList();

        return new PortChanges
        {
            NewPorts = newPorts,
            RemovedPorts = removedPorts,
            HasChanges = newPorts.Any() || removedPorts.Any()
        };
    }
}

/// <summary>
/// Port change tracking
/// </summary>
public class PortChanges
{
    public List<SerialPortInfo> NewPorts { get; set; } = new();
    public List<SerialPortInfo> RemovedPorts { get; set; } = new();
    public bool HasChanges { get; set; }
}

/// <summary>
/// Device group change tracking
/// </summary>
public class DeviceGroupChanges
{
    public List<DeviceGroup> NewDevices { get; set; } = new();
    public List<DeviceGroup> RemovedDevices { get; set; } = new();
    public bool HasChanges { get; set; }
}