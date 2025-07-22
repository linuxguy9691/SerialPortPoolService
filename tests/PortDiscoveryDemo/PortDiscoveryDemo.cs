using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SerialPortPool.Core.Interfaces;
using SerialPortPool.Core.Models;
using SerialPortPool.Core.Services;

namespace PortDiscoveryDemo;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("🔍 Enhanced Serial Port Discovery Demo - ÉTAPE 5 Phase 2");
        Console.WriteLine("===========================================================");
        Console.WriteLine("🎯 Features: FTDI Analysis + Validation + Device Grouping + Multi-Port Awareness");
        Console.WriteLine();
        
        // Setup dependency injection
        var serviceProvider = SetupServices();
        
        try
        {
            // Get services
            var discovery = serviceProvider.GetRequiredService<ISerialPortDiscovery>() as EnhancedSerialPortDiscoveryService;
            var ftdiReader = serviceProvider.GetRequiredService<IFtdiDeviceReader>();
            var validator = serviceProvider.GetRequiredService<ISerialPortValidator>();
            
            if (discovery == null)
            {
                Console.WriteLine("❌ Could not get Enhanced Discovery Service");
                return;
            }
            
            Console.WriteLine("📡 Scanning for serial ports with comprehensive analysis...");
            Console.WriteLine();
            
            // === PHASE 1: Traditional Port Discovery ===
            Console.WriteLine("=== PHASE 1: TRADITIONAL PORT DISCOVERY ===");
            await TestTraditionalDiscovery(discovery);
            
            Console.WriteLine();
            
            // === PHASE 2: NEW - Device Grouping Discovery ===
            Console.WriteLine("=== PHASE 2: DEVICE GROUPING DISCOVERY (NEW) ===");
            await TestDeviceGroupingDiscovery(discovery);
            
            Console.WriteLine();
            
            // === PHASE 3: Multi-Port Device Focus ===  
            Console.WriteLine("=== PHASE 3: MULTI-PORT DEVICE ANALYSIS ===");
            await TestMultiPortDeviceDiscovery(discovery);
            
            Console.WriteLine();
            
            // === PHASE 4: Device Grouping Statistics ===
            Console.WriteLine("=== PHASE 4: DEVICE GROUPING STATISTICS ===");
            await TestDeviceGroupingStatistics(discovery);
            
            Console.WriteLine();
            
            // === PHASE 5: Port-to-Device Lookup ===
            Console.WriteLine("=== PHASE 5: PORT-TO-DEVICE LOOKUP ===");
            await TestPortToDeviceLookup(discovery);
            
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error during discovery: {ex.Message}");
            Console.WriteLine($"📋 Stack trace: {ex.StackTrace}");
        }
        
        Console.WriteLine();
        Console.WriteLine("🎯 Enhanced discovery demo with device grouping completed!");
        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();
    }
    
    static ServiceProvider SetupServices()
    {
        var services = new ServiceCollection();
        
        // Logging
        services.AddLogging(builder =>
            builder.AddConsole().SetMinimumLevel(LogLevel.Information));
        
        // Configuration - Use development config to see all devices
        services.AddSingleton(PortValidationConfiguration.CreateDevelopmentDefault());
        
        // Services
        services.AddScoped<IFtdiDeviceReader, FtdiDeviceReader>();
        services.AddScoped<ISerialPortValidator, SerialPortValidator>();
        services.AddScoped<ISerialPortDiscovery, EnhancedSerialPortDiscoveryService>();
        
        return services.BuildServiceProvider();
    }
    
    static async Task TestTraditionalDiscovery(EnhancedSerialPortDiscoveryService discovery)
    {
        var ports = await discovery.DiscoverPortsAsync();
        var portList = ports.ToList();
        
        if (portList.Count == 0)
        {
            Console.WriteLine("❌ No serial ports found on this system.");
            return;
        }
        
        Console.WriteLine($"✅ Found {portList.Count} individual serial port(s):");
        foreach (var port in portList)
        {
            var ftdiIcon = port.IsFtdiDevice ? "🏭" : "🔌";
            var validIcon = port.IsValidForPool ? "✅" : "❌";
            var chipType = port.IsFtdiDevice ? $"({port.FtdiChipType})" : "(Non-FTDI)";
            
            Console.WriteLine($"  {ftdiIcon} {validIcon} {port.PortName} - {port.FriendlyName} {chipType}");
        }
    }
    
    static async Task TestDeviceGroupingDiscovery(EnhancedSerialPortDiscoveryService discovery)
    {
        var deviceGroups = await discovery.DiscoverDeviceGroupsAsync();
        var groupList = deviceGroups.ToList();
        
        if (groupList.Count == 0)
        {
            Console.WriteLine("❌ No device groups found.");
            return;
        }
        
        Console.WriteLine($"🔍 Found {groupList.Count} physical device(s):");
        Console.WriteLine();
        
        foreach (var group in groupList.OrderBy(g => g.DeviceId))
        {
            DisplayDeviceGroupDetails(group);
            Console.WriteLine();
        }
    }
    
    static async Task TestMultiPortDeviceDiscovery(EnhancedSerialPortDiscoveryService discovery)
    {
        var multiPortDevices = await discovery.DiscoverMultiPortDevicesAsync();
        var deviceList = multiPortDevices.ToList();
        
        Console.WriteLine($"🎯 Multi-port devices found: {deviceList.Count}");
        Console.WriteLine();
        
        if (deviceList.Count == 0)
        {
            Console.WriteLine("   No multi-port devices detected on this system.");
            Console.WriteLine("   💡 Multi-port devices include FT4232H (4 ports), FT2232H (2 ports), etc.");
        }
        else
        {
            foreach (var device in deviceList)
            {
                Console.WriteLine($"   🎯 {device.DeviceTypeDescription}");
                Console.WriteLine($"      📍 Ports: {string.Join(", ", device.GetPortNames())}");
                Console.WriteLine($"      📊 Utilization: {device.UtilizationPercentage:F0}% ({device.AllocatedPortCount}/{device.PortCount})");
                Console.WriteLine($"      ✅ Client Valid: {(device.IsClientValidDevice ? "YES" : "NO")}");
                
                if (device.SharedSystemInfo != null)
                {
                    Console.WriteLine($"      💾 System Info: {device.SharedSystemInfo.GetSummary()}");
                }
                Console.WriteLine();
            }
        }
    }
    
    static async Task TestDeviceGroupingStatistics(EnhancedSerialPortDiscoveryService discovery)
    {
        var stats = await discovery.GetDeviceGroupingStatisticsAsync();
        
        Console.WriteLine("📊 DEVICE GROUPING STATISTICS");
        Console.WriteLine("────────────────────────────");
        Console.WriteLine($"📱 Total Physical Devices: {stats.TotalDevices}");
        Console.WriteLine($"📍 Total Individual Ports: {stats.TotalPorts}");
        Console.WriteLine($"🔀 Multi-Port Devices: {stats.MultiPortDevices}");
        Console.WriteLine($"📌 Single-Port Devices: {stats.SinglePortDevices}");
        Console.WriteLine($"🏭 FTDI Devices: {stats.FtdiDevices}");
        Console.WriteLine($"🔌 Non-FTDI Devices: {stats.NonFtdiDevices}");
        Console.WriteLine($"📊 Average Ports/Device: {stats.AveragePortsPerDevice:F1}");
        Console.WriteLine($"🎯 Largest Device: {stats.LargestDevicePortCount} ports");
        
        // Analysis
        if (stats.TotalDevices > 0)
        {
            var ftdiRatio = (stats.FtdiDevices * 100.0) / stats.TotalDevices;
            var multiPortRatio = (stats.MultiPortDevices * 100.0) / stats.TotalDevices;
            
            Console.WriteLine();
            Console.WriteLine("📈 ANALYSIS:");
            Console.WriteLine($"   🏭 FTDI Coverage: {ftdiRatio:F0}% of devices");
            Console.WriteLine($"   🔀 Multi-Port Ratio: {multiPortRatio:F0}% of devices");
            
            if (multiPortRatio > 0)
            {
                Console.WriteLine("   ✅ Multi-port devices detected - ideal for bulk operations");
            }
            
            if (ftdiRatio >= 80)
            {
                Console.WriteLine("   ✅ High FTDI coverage - good for professional applications");
            }
        }
    }
    
    static async Task TestPortToDeviceLookup(EnhancedSerialPortDiscoveryService discovery)
    {
        // Get all device groups first
        var deviceGroups = await discovery.DiscoverDeviceGroupsAsync();
        var allPorts = deviceGroups.SelectMany(g => g.Ports).ToList();
        
        if (!allPorts.Any())
        {
            Console.WriteLine("❌ No ports available for lookup test");
            return;
        }
        
        // Test lookup for first few ports
        var portsToTest = allPorts.Take(3).ToList();
        
        Console.WriteLine($"🔍 Testing port-to-device lookup for {portsToTest.Count} port(s):");
        Console.WriteLine();
        
        foreach (var port in portsToTest)
        {
            var deviceGroup = await discovery.FindDeviceGroupByPortAsync(port.PortName);
            
            if (deviceGroup != null)
            {
                Console.WriteLine($"   📍 Port {port.PortName}:");
                Console.WriteLine($"      🏠 Belongs to: {deviceGroup.DeviceTypeDescription}");
                Console.WriteLine($"      👥 Shares device with: {string.Join(", ", deviceGroup.GetPortNames().Where(p => p != port.PortName))}");
                Console.WriteLine($"      📊 Device utilization: {deviceGroup.UtilizationPercentage:F0}%");
            }
            else
            {
                Console.WriteLine($"   ❌ Port {port.PortName}: Device group not found");
            }
            Console.WriteLine();
        }
        
        // Test specific device analysis if we have a multi-port device
        var multiPortDevice = deviceGroups.FirstOrDefault(g => g.IsMultiPortDevice);
        if (multiPortDevice != null)
        {
            Console.WriteLine($"🎯 Detailed analysis of multi-port device: {multiPortDevice.DeviceId}");
            var analysis = await discovery.GetDeviceGroupAnalysisAsync(multiPortDevice.DeviceId);
            
            if (analysis != null)
            {
                Console.WriteLine($"   📋 Summary: {analysis.GetSummary()}");
                Console.WriteLine($"   📍 Available ports: {string.Join(", ", analysis.GetAvailablePorts().Select(p => p.PortName))}");
                Console.WriteLine($"   🔒 Allocated ports: {string.Join(", ", analysis.GetAllocatedPorts().Select(p => p.PortName))}");
            }
        }
    }
    
    static void DisplayDeviceGroupDetails(DeviceGroup group)
    {
        var deviceIcon = group.IsFtdiDevice ? "🏭" : "🔌";
        var validIcon = group.IsClientValidDevice ? "✅" : "❌";
        var multiPortIcon = group.IsMultiPortDevice ? "🔀" : "📌";
        
        Console.WriteLine($"{deviceIcon} {validIcon} {multiPortIcon} {group.DeviceTypeDescription}");
        Console.WriteLine($"   📍 Ports ({group.PortCount}): {string.Join(", ", group.GetPortNames())}");
        Console.WriteLine($"   🆔 Device ID: {group.DeviceId}");
        Console.WriteLine($"   📊 Utilization: {group.UtilizationPercentage:F0}% ({group.AllocatedPortCount}/{group.PortCount} allocated)");
        
        if (group.IsFtdiDevice && group.DeviceInfo != null)
        {
            Console.WriteLine($"   🏭 FTDI Info: VID/PID {group.DeviceInfo.VendorId}/{group.DeviceInfo.ProductId}");
            Console.WriteLine($"   🔑 Serial: {group.SerialNumber}");
            Console.WriteLine($"   💎 Client Valid: {(group.IsClientValidDevice ? "YES (FT4232H)" : "NO (Other chip)")}");
        }
        
        if (group.SharedSystemInfo != null)
        {
            Console.WriteLine($"   💾 System Info: {group.SharedSystemInfo.GetSummary()}");
            
            if (group.SharedSystemInfo.EepromData.Any())
            {
                Console.WriteLine($"   💿 EEPROM Data: {group.SharedSystemInfo.EepromData.Count} entries");
                var importantEntries = group.SharedSystemInfo.EepromData
                    .Where(kvp => new[] { "Manufacturer", "ProductDescription", "ChipType" }.Contains(kvp.Key))
                    .Take(3);
                
                foreach (var entry in importantEntries)
                {
                    Console.WriteLine($"      - {entry.Key}: {entry.Value}");
                }
            }
        }
        
        // Port details for multi-port devices
        if (group.IsMultiPortDevice)
        {
            Console.WriteLine($"   📋 Individual port details:");
            foreach (var port in group.Ports.OrderBy(p => p.PortName))
            {
                var statusIcon = port.Status switch
                {
                    PortStatus.Available => "🟢",
                    PortStatus.Allocated => "🔴", 
                    PortStatus.Error => "❌",
                    _ => "❓"
                };
                var validPortIcon = port.IsValidForPool ? "✅" : "❌";
                
                Console.WriteLine($"      {statusIcon} {validPortIcon} {port.PortName} ({port.Status})");
            }
        }
    }
}