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
        Console.WriteLine("🔍 Enhanced Serial Port Discovery Demo - SPRINT 8 INTEGRATION");
        Console.WriteLine("===============================================================");
        Console.WriteLine("🎯 Features: FTDI Analysis + Validation + Device Grouping + SPRINT 8 EEPROM Enhancement");
        Console.WriteLine();
        
        // Setup dependency injection with SPRINT 8 integration
        var serviceProvider = SetupServicesWithSprint8();
        
        try
        {
            // Get services
            var discovery = serviceProvider.GetRequiredService<ISerialPortDiscovery>() as EnhancedSerialPortDiscoveryService;
            var ftdiReader = serviceProvider.GetRequiredService<IFtdiDeviceReader>();
            var validator = serviceProvider.GetRequiredService<ISerialPortValidator>();
            var eepromReader = serviceProvider.GetRequiredService<IFtdiEepromReader>(); // ← SPRINT 8
            
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
            Console.WriteLine("=== PHASE 2: DEVICE GROUPING DISCOVERY ===");
            await TestDeviceGroupingDiscovery(discovery);
            
            Console.WriteLine();
            
            // === PHASE 3: SPRINT 8 - EEPROM Enhancement Demo ===  
            Console.WriteLine("=== PHASE 3: SPRINT 8 - EEPROM ENHANCEMENT DEMO ===");
            await TestSprint8EepromEnhancement(eepromReader);
            
            Console.WriteLine();
            
            // === PHASE 4: Enhanced SystemInfo with EEPROM ===
            Console.WriteLine("=== PHASE 4: ENHANCED SYSTEMINFO WITH EEPROM ===");
            await TestEnhancedSystemInfo(serviceProvider);
            
            Console.WriteLine();
            
            // === PHASE 5: Device Grouping Statistics ===
            Console.WriteLine("=== PHASE 5: DEVICE GROUPING STATISTICS ===");
            await TestDeviceGroupingStatistics(discovery);
            
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error during discovery: {ex.Message}");
            Console.WriteLine($"📋 Stack trace: {ex.StackTrace}");
        }
        
        Console.WriteLine();
        Console.WriteLine("🎯 Enhanced discovery demo with SPRINT 8 EEPROM integration completed!");
        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();
    }
    
    static ServiceProvider SetupServicesWithSprint8()
    {
        var services = new ServiceCollection();
        
        // Logging
        services.AddLogging(builder =>
            builder.AddConsole().SetMinimumLevel(LogLevel.Information));
        
        // Configuration - Use development config to see all devices
        services.AddSingleton(PortValidationConfiguration.CreateDevelopmentDefault());
        
        // ✅ SPRINT 8: Add new EEPROM reader service
        services.AddScoped<IFtdiEepromReader, FtdiEepromReader>();
        
        // Original services
        services.AddScoped<IFtdiDeviceReader, FtdiDeviceReader>();
        services.AddScoped<ISerialPortValidator, SerialPortValidator>();
        services.AddScoped<ISerialPortDiscovery, EnhancedSerialPortDiscoveryService>();
        
        // ✅ SPRINT 8: Enhanced SystemInfo with cache
        services.AddScoped<SystemInfoCache>();
        services.AddScoped<ISystemInfoCache>(provider => provider.GetRequiredService<SystemInfoCache>());
        
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
    
    // ✅ SPRINT 8: New EEPROM enhancement demo
    static async Task TestSprint8EepromEnhancement(IFtdiEepromReader eepromReader)
    {
        Console.WriteLine("🚀 SPRINT 8: Testing enhanced FTDI device info via FTD2XX_NET");
        Console.WriteLine();
        
        try
        {
            // Get all connected devices
            var serialNumbers = await eepromReader.GetConnectedDeviceSerialNumbersAsync();
            
            if (!serialNumbers.Any())
            {
                Console.WriteLine("❌ No FTDI devices found via FTD2XX_NET");
                return;
            }
            
            Console.WriteLine($"📱 Found {serialNumbers.Count} FTDI device(s) via FTD2XX_NET:");
            Console.WriteLine();
            
            foreach (var serialNumber in serialNumbers)
            {
                Console.WriteLine($"🔬 Analyzing device: {serialNumber}");
                
                // Test accessibility
                var accessible = await eepromReader.IsDeviceAccessibleAsync(serialNumber);
                Console.WriteLine($"   🔍 Accessible: {(accessible ? "✅ YES" : "❌ NO")}");
                
                if (accessible)
                {
                    // Read enhanced device data
                    var eepromData = await eepromReader.ReadEepromAsync(serialNumber);
                    
                    if (eepromData.IsValid)
                    {
                        Console.WriteLine($"   ✅ Enhanced data read successfully:");
                        Console.WriteLine($"      📝 Product Description: '{eepromData.ProductDescription}'");
                        Console.WriteLine($"      🏭 Manufacturer: '{eepromData.Manufacturer}'");
                        Console.WriteLine($"      🆔 VID/PID: {eepromData.VendorId}/{eepromData.ProductId}");
                        Console.WriteLine($"      ⚡ Max Power: {eepromData.MaxPower}mA");
                        Console.WriteLine($"      🔗 Source: {eepromData.Source}");
                        
                        // Test specific field reading
                        var productDesc = await eepromReader.ReadEepromFieldAsync(serialNumber, "ProductDescription");
                        Console.WriteLine($"      🎯 Field Test (ProductDescription): '{productDesc}'");
                        
                        if (eepromData.AdditionalFields.Any())
                        {
                            Console.WriteLine($"      📊 Additional fields: {eepromData.AdditionalFields.Count}");
                            foreach (var field in eepromData.AdditionalFields.Take(3))
                            {
                                Console.WriteLine($"         - {field.Key}: {field.Value}");
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine($"   ❌ Enhanced data read failed: {eepromData.ErrorMessage}");
                    }
                }
                
                Console.WriteLine();
            }
            
            // Test bulk reading
            Console.WriteLine("📊 Testing bulk enhanced device reading...");
            var allDeviceData = await eepromReader.ReadAllConnectedDevicesAsync();
            var validDevices = allDeviceData.Values.Count(d => d.IsValid);
            
            Console.WriteLine($"   📈 Bulk read result: {validDevices}/{allDeviceData.Count} devices successfully enhanced");
            
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ SPRINT 8 demo error: {ex.Message}");
        }
    }
    
    // ✅ SPRINT 8: Enhanced SystemInfo demo
    static async Task TestEnhancedSystemInfo(ServiceProvider serviceProvider)
    {
        Console.WriteLine("💾 Testing Enhanced SystemInfo with EEPROM integration");
        Console.WriteLine();
        
        try
        {
            var systemInfoCache = serviceProvider.GetRequiredService<ISystemInfoCache>();
            var discovery = serviceProvider.GetRequiredService<ISerialPortDiscovery>();
            
            // Get all ports
            var ports = await discovery.DiscoverPortsAsync();
            var ftdiPorts = ports.Where(p => p.IsFtdiDevice).Take(2).ToList();
            
            if (!ftdiPorts.Any())
            {
                Console.WriteLine("❌ No FTDI ports found for SystemInfo testing");
                return;
            }
            
            Console.WriteLine($"🔍 Testing enhanced SystemInfo for {ftdiPorts.Count} FTDI port(s):");
            Console.WriteLine();
            
            foreach (var port in ftdiPorts)
            {
                Console.WriteLine($"📡 Port {port.PortName} ({port.FtdiChipType}):");
                
                // Get enhanced system info
                var systemInfo = await systemInfoCache.GetSystemInfoAsync(port.PortName, forceRefresh: true);
                
                if (systemInfo?.IsDataValid == true)
                {
                    Console.WriteLine($"   ✅ Enhanced SystemInfo available:");
                    Console.WriteLine($"      📝 Product: {systemInfo.ProductDescription}");
                    Console.WriteLine($"      🏭 Manufacturer: {systemInfo.Manufacturer}");
                    Console.WriteLine($"      🔑 Serial: {systemInfo.SerialNumber}");
                    Console.WriteLine($"      💾 EEPROM entries: {systemInfo.EepromData.Count}");
                    Console.WriteLine($"      🔧 System properties: {systemInfo.SystemProperties.Count}");
                    Console.WriteLine($"      ⚙️ Client config: {systemInfo.ClientConfiguration.Count}");
                    Console.WriteLine($"      ⏰ Age: {systemInfo.Age.TotalSeconds:F1}s");
                    
                    // Show some key EEPROM data
                    var importantEntries = systemInfo.EepromData
                        .Where(kvp => new[] { "Manufacturer", "ProductDescription", "ChipType", "VendorId", "ProductId" }.Contains(kvp.Key))
                        .Take(3);
                    
                    if (importantEntries.Any())
                    {
                        Console.WriteLine($"      📊 Key EEPROM data:");
                        foreach (var entry in importantEntries)
                        {
                            Console.WriteLine($"         - {entry.Key}: {entry.Value}");
                        }
                    }
                    
                    // Show client configuration
                    if (systemInfo.ClientConfiguration.Any())
                    {
                        Console.WriteLine($"      🎯 Client configuration:");
                        foreach (var config in systemInfo.ClientConfiguration.Take(3))
                        {
                            Console.WriteLine($"         - {config.Key}: {config.Value}");
                        }
                    }
                }
                else
                {
                    Console.WriteLine($"   ❌ Enhanced SystemInfo not available: {systemInfo?.ErrorMessage ?? "Unknown error"}");
                }
                
                Console.WriteLine();
            }
            
            // Test cache statistics
            var cacheStats = systemInfoCache.GetStatistics();
            Console.WriteLine($"📊 SystemInfo Cache Statistics:");
            Console.WriteLine($"   📈 Total entries: {cacheStats.TotalEntries}");
            Console.WriteLine($"   ✅ Cache hits: {cacheStats.TotalHits}");
            Console.WriteLine($"   ❌ Cache misses: {cacheStats.TotalMisses}");
            Console.WriteLine($"   📊 Hit ratio: {cacheStats.HitRatio:F1}%");
            
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Enhanced SystemInfo demo error: {ex.Message}");
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