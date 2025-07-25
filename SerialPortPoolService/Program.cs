// SerialPortPoolService/Program.cs - FIXED: Add Device Grouping to Service
// Remplacer la méthode TestEnhancedDiscoveryInService par cette version

private async Task TestEnhancedDiscoveryInService(CancellationToken cancellationToken)
{
    try
    {
        nlogLogger.Info("Testing Enhanced Discovery + Device Grouping integration...");
        
        var discovery = serviceProvider?.GetRequiredService<ISerialPortDiscovery>() as EnhancedSerialPortDiscoveryService;
        if (discovery == null)
        {
            nlogLogger.Error("Failed to get Enhanced Discovery service");
            return;
        }
        
        // ÉTAPE 1: Traditional port discovery (existing)
        nlogLogger.Info("=== PHASE 1: TRADITIONAL PORT DISCOVERY ===");
        var ports = await discovery.DiscoverPortsAsync();
        var portList = ports.ToList();
        
        nlogLogger.Info($"Found {portList.Count} individual serial ports");
        foreach (var port in portList)
        {
            var ftdiStatus = port.IsFtdiDevice ? $"FTDI {port.FtdiChipType}" : "Non-FTDI";
            var validStatus = port.IsValidForPool ? "VALID" : "INVALID";
            
            nlogLogger.Info($"  - {port.PortName}: {port.FriendlyName} [{ftdiStatus}] [{validStatus}]");
            
            if (port.IsFtdiDevice && port.FtdiInfo != null)
            {
                nlogLogger.Info($"    FTDI Details: VID/PID={port.FtdiInfo.VendorId}/{port.FtdiInfo.ProductId}, Serial={port.FtdiInfo.SerialNumber}");
            }
            
            if (port.ValidationResult != null)
            {
                nlogLogger.Info($"    Validation: {port.ValidationResult.Reason} (Score: {port.ValidationResult.ValidationScore}%)");
            }
        }
        
        // ÉTAPE 2: NEW - Device Grouping Discovery (ÉTAPE 5)
        nlogLogger.Info("=== PHASE 2: DEVICE GROUPING DISCOVERY (NEW) ===");
        var deviceGroups = await discovery.DiscoverDeviceGroupsAsync();
        var groupList = deviceGroups.ToList();
        
        nlogLogger.Info($"Found {groupList.Count} physical device(s):");
        
        foreach (var group in groupList.OrderBy(g => g.DeviceId))
        {
            var deviceIcon = group.IsFtdiDevice ? "🏭" : "🔌";
            var validIcon = group.IsClientValidDevice ? "✅" : "❌";
            var multiPortIcon = group.IsMultiPortDevice ? "🔀" : "📌";
            
            nlogLogger.Info($"{deviceIcon} {validIcon} {multiPortIcon} {group.DeviceTypeDescription}");
            nlogLogger.Info($"   📍 Ports ({group.PortCount}): {string.Join(", ", group.GetPortNames())}");
            nlogLogger.Info($"   🆔 Device ID: {group.DeviceId}");
            nlogLogger.Info($"   📊 Utilization: {group.UtilizationPercentage:F0}% ({group.AllocatedPortCount}/{group.PortCount} allocated)");
            
            if (group.IsFtdiDevice && group.DeviceInfo != null)
            {
                nlogLogger.Info($"   🏭 FTDI Info: VID/PID {group.DeviceInfo.VendorId}/{group.DeviceInfo.ProductId}");
                nlogLogger.Info($"   🔑 Serial: {group.SerialNumber}");
                nlogLogger.Info($"   💎 Client Valid: {(group.IsClientValidDevice ? "YES (FT4232H/HL)" : "NO (Other chip)")}");
            }
            
            if (group.SharedSystemInfo != null)
            {
                nlogLogger.Info($"   💾 System Info: {group.SharedSystemInfo.GetSummary()}");
            }
        }
        
        // ÉTAPE 3: Device Grouping Statistics
        nlogLogger.Info("=== PHASE 3: DEVICE GROUPING STATISTICS ===");
        var stats = await discovery.GetDeviceGroupingStatisticsAsync();
        
        nlogLogger.Info($"📊 Device Grouping Statistics:");
        nlogLogger.Info($"   📱 Total Physical Devices: {stats.TotalDevices}");
        nlogLogger.Info($"   📍 Total Individual Ports: {stats.TotalPorts}");
        nlogLogger.Info($"   🔀 Multi-Port Devices: {stats.MultiPortDevices}");
        nlogLogger.Info($"   📌 Single-Port Devices: {stats.SinglePortDevices}");
        nlogLogger.Info($"   🏭 FTDI Devices: {stats.FtdiDevices}");
        nlogLogger.Info($"   📊 Average Ports/Device: {stats.AveragePortsPerDevice:F1}");
        nlogLogger.Info($"   🎯 Largest Device: {stats.LargestDevicePortCount} ports");
        
        // ÉTAPE 4: Port-to-Device Lookup Test
        if (portList.Any())
        {
            nlogLogger.Info("=== PHASE 4: PORT-TO-DEVICE LOOKUP TEST ===");
            var firstPort = portList.First();
            var deviceGroup = await discovery.FindDeviceGroupByPortAsync(firstPort.PortName);
            
            if (deviceGroup != null)
            {
                nlogLogger.Info($"📍 Port {firstPort.PortName}:");
                nlogLogger.Info($"   🏠 Belongs to: {deviceGroup.DeviceTypeDescription}");
                nlogLogger.Info($"   👥 Shares device with: {string.Join(", ", deviceGroup.GetPortNames().Where(p => p != firstPort.PortName))}");
                nlogLogger.Info($"   📊 Device utilization: {deviceGroup.UtilizationPercentage:F0}%");
            }
            else
            {
                nlogLogger.Info($"❌ Port {firstPort.PortName}: Device group not found");
            }
        }
        
        nlogLogger.Info("✅ Enhanced Discovery with Device Grouping test completed successfully!");
        nlogLogger.Info("NOTE: Continuous discovery with device grouping is now handled by Background Service");
        
    }
    catch (Exception ex)
    {
        nlogLogger.Error(ex, "❌ Enhanced Discovery with Device Grouping test failed");
        // Ne pas throw - le service doit continuer même si discovery échoue
    }
}