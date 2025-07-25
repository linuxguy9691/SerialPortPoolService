// SerialPortPoolService/Services/PortDiscoveryBackgroundService.cs - ENHANCED
// Ajouter cette méthode à la classe PortDiscoveryBackgroundService

/// <summary>
/// Enhanced discovery with device grouping (ÉTAPE 5 integration)
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
            _logger.LogWarning("Enhanced Discovery Service not available, falling back to basic discovery");
            await PerformDiscoveryAsync(isInitial); // Fallback
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
        _lastDiscoveryTime = discoveryStartTime;

    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error during enhanced discovery cycle with device grouping");
        // Fallback to basic discovery
        await PerformDiscoveryAsync(isInitial);
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
/// Device group change tracking
/// </summary>
public class DeviceGroupChanges
{
    public List<DeviceGroup> NewDevices { get; set; } = new();
    public List<DeviceGroup> RemovedDevices { get; set; } = new();
    public bool HasChanges { get; set; }
}

// MODIFIER la méthode ExecuteAsync pour utiliser l'enhanced discovery:
// Remplacer l'appel à PerformDiscoveryAsync par PerformEnhancedDiscoveryAsync dans ExecuteAsync