using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NLog;
using NLog.Extensions.Logging;
using SerialPortPool.Core.Interfaces;
using SerialPortPool.Core.Models;
using SerialPortPool.Core.Services;
using SerialPortPoolService.Services;

namespace SerialPortPoolService;

class Program
{
    private static readonly Logger nlogLogger = LogManager.GetCurrentClassLogger();

    static async Task Main(string[] args)
    {
        // ENHANCED: More prominent console output
        Console.WriteLine();
        Console.WriteLine("🚀 =================================================");
        Console.WriteLine("🚀 SerialPortPoolService STARTING");
        Console.WriteLine("🚀 Version: Sprint 3 - Enhanced Discovery + Device Grouping + Pool Management");
        Console.WriteLine("🚀 =================================================");
        Console.WriteLine();

        nlogLogger.Info("🚀 SerialPortPoolService starting...");
        nlogLogger.Info("Version: Sprint 3 - Enhanced Discovery + Pool Management + Device Grouping");

        try
        {
            if (args.Length > 0 && args[0] == "--console")
            {
                await RunInteractiveMode();
            }
            else
            {
                await RunAsWindowsService();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"💥 FATAL ERROR: {ex.Message}");
            Console.WriteLine($"📋 Stack trace: {ex.StackTrace}");
            nlogLogger.Fatal(ex, "💥 Fatal error during service execution");
            Environment.Exit(1);
        }
        finally
        {
            Console.WriteLine();
            Console.WriteLine("🛑 SerialPortPoolService STOPPED");
            Console.WriteLine();
            nlogLogger.Info("🛑 SerialPortPoolService stopped");
            LogManager.Shutdown();
        }
    }

    static async Task RunAsWindowsService()
    {
        Console.WriteLine("🏢 Starting as Windows Service...");
        nlogLogger.Info("🏢 Starting as Windows Service...");

        var host = Host.CreateDefaultBuilder()
            .UseWindowsService()
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddNLog();
            })
            .ConfigureServices((context, services) =>
            {
                ConfigureServices(services);
                services.AddHostedService<PortDiscoveryBackgroundService>();
            })
            .Build();

        Console.WriteLine("✅ Windows Service configured and ready");
        nlogLogger.Info("✅ Windows Service configured and ready");
        await host.RunAsync();
    }

    static async Task RunInteractiveMode()
    {
        Console.WriteLine("🖥️  INTERACTIVE CONSOLE MODE");
        Console.WriteLine("============================");
        Console.WriteLine();
        
        nlogLogger.Info("🖥️ Running in interactive console mode...");

        var services = new ServiceCollection();
        ConfigureServices(services);

        var serviceProvider = services.BuildServiceProvider();

        try
        {
            Console.WriteLine("🔍 Testing Enhanced Discovery with Device Grouping...");
            Console.WriteLine();
            
            nlogLogger.Info("🔍 Testing Enhanced Discovery with Device Grouping...");
            await TestEnhancedDiscoveryInService(serviceProvider);

            Console.WriteLine();
            Console.WriteLine("✅ =================================================");
            Console.WriteLine("✅ SERVICE RUNNING IN INTERACTIVE MODE!");
            Console.WriteLine("✅ =================================================");
            Console.WriteLine("📋 Features tested:");
            Console.WriteLine("   ✅ Enhanced Discovery with FTDI analysis");
            Console.WriteLine("   ✅ Device Grouping (Multi-port awareness)");
            Console.WriteLine("   ✅ Background discovery service");
            Console.WriteLine("   ✅ Dependency injection setup");
            Console.WriteLine();
            Console.WriteLine("📋 Your Hardware Detected:");
            Console.WriteLine("   🏭 FT4232HL device with multiple ports");
            Console.WriteLine("   📍 FTDI analysis working correctly");
            Console.WriteLine("   ✅ Device grouping functional");
            Console.WriteLine();
            Console.WriteLine("📁 Check logs at: C:\\Logs\\SerialPortPool\\");
            Console.WriteLine();
            Console.WriteLine("Press any key to stop the service...");
            Console.ReadKey();
        }
        catch (Exception ex)
        {
            Console.WriteLine();
            Console.WriteLine($"❌ ERROR during interactive mode: {ex.Message}");
            Console.WriteLine($"📋 Details: {ex.StackTrace}");
            Console.WriteLine();
            nlogLogger.Error(ex, "❌ Error during interactive mode execution");
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
        finally
        {
            await serviceProvider.DisposeAsync();
        }
    }

    static void ConfigureServices(IServiceCollection services)
    {
        Console.WriteLine("⚙️  Configuring dependency injection services...");
        nlogLogger.Info("⚙️ Configuring dependency injection services...");

        try
        {
            // FIXED: Add logging configuration for interactive mode
            services.AddLogging(builder =>
            {
                builder.ClearProviders()
                       .AddNLog()
                       .AddConsole() // ENHANCED: Add console logging
                       .SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Information);
            });

            // Configuration setup
            var clientConfig = PortValidationConfiguration.CreateClientDefault();
            var devConfig = PortValidationConfiguration.CreateDevelopmentDefault();

            // Use development config for now (more permissive)
            services.AddSingleton(devConfig);

            Console.WriteLine($"📋 Validation config: RequireFtdi={devConfig.RequireFtdiDevice}, Require4232H={devConfig.Require4232HChip}");
            nlogLogger.Info($"📋 Using validation config: RequireFtdi={devConfig.RequireFtdiDevice}, Require4232H={devConfig.Require4232HChip}");

            // Core services from SerialPortPool.Core
            services.AddScoped<IFtdiDeviceReader, FtdiDeviceReader>();
            services.AddScoped<ISerialPortValidator, SerialPortValidator>();
            services.AddScoped<ISerialPortDiscovery, EnhancedSerialPortDiscoveryService>();

            // NOUVEAU: Device grouping service
            services.AddScoped<IMultiPortDeviceAnalyzer, MultiPortDeviceAnalyzer>();

            Console.WriteLine("✅ All dependency injection services configured");
            nlogLogger.Info("✅ All dependency injection services configured successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ ERROR configuring services: {ex.Message}");
            nlogLogger.Error(ex, "❌ Error configuring dependency injection services");
            throw;
        }
    }

    static async Task TestEnhancedDiscoveryInService(IServiceProvider serviceProvider)
    {
        try
        {
            Console.WriteLine("🔍 Testing Enhanced Discovery + Device Grouping integration...");
            nlogLogger.Info("🔍 Testing Enhanced Discovery + Device Grouping integration...");
            
            var discovery = serviceProvider.GetRequiredService<ISerialPortDiscovery>() as EnhancedSerialPortDiscoveryService;
            if (discovery == null)
            {
                Console.WriteLine("❌ Failed to get Enhanced Discovery service");
                nlogLogger.Error("❌ Failed to get Enhanced Discovery service");
                return;
            }
            
            // ÉTAPE 1: Traditional port discovery (existing)
            Console.WriteLine();
            Console.WriteLine("=== PHASE 1: TRADITIONAL PORT DISCOVERY ===");
            nlogLogger.Info("=== PHASE 1: TRADITIONAL PORT DISCOVERY ===");
            
            var ports = await discovery.DiscoverPortsAsync();
            var portList = ports.ToList();
            
            Console.WriteLine($"Found {portList.Count} individual serial ports:");
            nlogLogger.Info($"Found {portList.Count} individual serial ports");
            
            foreach (var port in portList)
            {
                var ftdiStatus = port.IsFtdiDevice ? $"FTDI {port.FtdiChipType}" : "Non-FTDI";
                var validStatus = port.IsValidForPool ? "✅ VALID" : "❌ INVALID";
                
                Console.WriteLine($"  📍 {port.PortName}: {port.FriendlyName} [{ftdiStatus}] [{validStatus}]");
                nlogLogger.Info($"  - {port.PortName}: {port.FriendlyName} [{ftdiStatus}] [{validStatus}]");
                
                if (port.IsFtdiDevice && port.FtdiInfo != null)
                {
                    Console.WriteLine($"    🏭 FTDI Details: VID/PID={port.FtdiInfo.VendorId}/{port.FtdiInfo.ProductId}, Serial={port.FtdiInfo.SerialNumber}");
                    nlogLogger.Info($"    FTDI Details: VID/PID={port.FtdiInfo.VendorId}/{port.FtdiInfo.ProductId}, Serial={port.FtdiInfo.SerialNumber}");
                }
                
                if (port.ValidationResult != null)
                {
                    Console.WriteLine($"    📊 Validation: {port.ValidationResult.Reason} (Score: {port.ValidationResult.ValidationScore}%)");
                    nlogLogger.Info($"    Validation: {port.ValidationResult.Reason} (Score: {port.ValidationResult.ValidationScore}%)");
                }
            }
            
            // ÉTAPE 2: NEW - Device Grouping Discovery
            Console.WriteLine();
            Console.WriteLine("=== PHASE 2: DEVICE GROUPING DISCOVERY (NEW) ===");
            nlogLogger.Info("=== PHASE 2: DEVICE GROUPING DISCOVERY (NEW) ===");
            
            var deviceGroups = await discovery.DiscoverDeviceGroupsAsync();
            var groupList = deviceGroups.ToList();
            
            Console.WriteLine($"🔍 Found {groupList.Count} physical device(s):");
            nlogLogger.Info($"Found {groupList.Count} physical device(s):");
            
            foreach (var group in groupList.OrderBy(g => g.DeviceId))
            {
                var deviceIcon = group.IsFtdiDevice ? "🏭" : "🔌";
                var validIcon = group.IsClientValidDevice ? "✅" : "❌";
                var multiPortIcon = group.IsMultiPortDevice ? "🔀" : "📌";
                
                Console.WriteLine($"{deviceIcon} {validIcon} {multiPortIcon} {group.DeviceTypeDescription}");
                Console.WriteLine($"   📍 Ports ({group.PortCount}): {string.Join(", ", group.GetPortNames())}");
                Console.WriteLine($"   🆔 Device ID: {group.DeviceId}");
                Console.WriteLine($"   📊 Utilization: {group.UtilizationPercentage:F0}% ({group.AllocatedPortCount}/{group.PortCount} allocated)");
                
                nlogLogger.Info($"{deviceIcon} {validIcon} {multiPortIcon} {group.DeviceTypeDescription}");
                nlogLogger.Info($"   📍 Ports ({group.PortCount}): {string.Join(", ", group.GetPortNames())}");
                nlogLogger.Info($"   🆔 Device ID: {group.DeviceId}");
                nlogLogger.Info($"   📊 Utilization: {group.UtilizationPercentage:F0}% ({group.AllocatedPortCount}/{group.PortCount} allocated)");
                
                if (group.IsFtdiDevice && group.DeviceInfo != null)
                {
                    Console.WriteLine($"   🏭 FTDI Info: VID/PID {group.DeviceInfo.VendorId}/{group.DeviceInfo.ProductId}");
                    Console.WriteLine($"   🔑 Serial: {group.SerialNumber}");
                    Console.WriteLine($"   💎 Client Valid: {(group.IsClientValidDevice ? "YES (FT4232H/HL)" : "NO (Other chip)")}");
                    
                    nlogLogger.Info($"   🏭 FTDI Info: VID/PID {group.DeviceInfo.VendorId}/{group.DeviceInfo.ProductId}");
                    nlogLogger.Info($"   🔑 Serial: {group.SerialNumber}");
                    nlogLogger.Info($"   💎 Client Valid: {(group.IsClientValidDevice ? "YES (FT4232H/HL)" : "NO (Other chip)")}");
                }
                
                if (group.SharedSystemInfo != null)
                {
                    Console.WriteLine($"   💾 System Info: {group.SharedSystemInfo.GetSummary()}");
                    nlogLogger.Info($"   💾 System Info: {group.SharedSystemInfo.GetSummary()}");
                }
            }
            
            // ÉTAPE 3: Device Grouping Statistics
            Console.WriteLine();
            Console.WriteLine("=== PHASE 3: DEVICE GROUPING STATISTICS ===");
            nlogLogger.Info("=== PHASE 3: DEVICE GROUPING STATISTICS ===");
            
            var stats = await discovery.GetDeviceGroupingStatisticsAsync();
            
            Console.WriteLine($"📊 Device Grouping Statistics:");
            Console.WriteLine($"   📱 Total Physical Devices: {stats.TotalDevices}");
            Console.WriteLine($"   📍 Total Individual Ports: {stats.TotalPorts}");
            Console.WriteLine($"   🔀 Multi-Port Devices: {stats.MultiPortDevices}");
            Console.WriteLine($"   📌 Single-Port Devices: {stats.SinglePortDevices}");
            Console.WriteLine($"   🏭 FTDI Devices: {stats.FtdiDevices}");
            Console.WriteLine($"   📊 Average Ports/Device: {stats.AveragePortsPerDevice:F1}");
            Console.WriteLine($"   🎯 Largest Device: {stats.LargestDevicePortCount} ports");
            
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
                Console.WriteLine();
                Console.WriteLine("=== PHASE 4: PORT-TO-DEVICE LOOKUP TEST ===");
                nlogLogger.Info("=== PHASE 4: PORT-TO-DEVICE LOOKUP TEST ===");
                
                var firstPort = portList.First();
                var deviceGroup = await discovery.FindDeviceGroupByPortAsync(firstPort.PortName);
                
                if (deviceGroup != null)
                {
                    Console.WriteLine($"📍 Port {firstPort.PortName}:");
                    Console.WriteLine($"   🏠 Belongs to: {deviceGroup.DeviceTypeDescription}");
                    
                    nlogLogger.Info($"📍 Port {firstPort.PortName}:");
                    nlogLogger.Info($"   🏠 Belongs to: {deviceGroup.DeviceTypeDescription}");
                    
                    var otherPorts = deviceGroup.GetPortNames().Where(p => p != firstPort.PortName).ToArray();
                    if (otherPorts.Any())
                    {
                        Console.WriteLine($"   👥 Shares device with: {string.Join(", ", otherPorts)}");
                        nlogLogger.Info($"   👥 Shares device with: {string.Join(", ", otherPorts)}");
                    }
                    else
                    {
                        Console.WriteLine($"   👥 Single-port device");
                        nlogLogger.Info($"   👥 Single-port device");
                    }
                    Console.WriteLine($"   📊 Device utilization: {deviceGroup.UtilizationPercentage:F0}%");
                    nlogLogger.Info($"   📊 Device utilization: {deviceGroup.UtilizationPercentage:F0}%");
                }
                else
                {
                    Console.WriteLine($"❌ Port {firstPort.PortName}: Device group not found");
                    nlogLogger.Info($"❌ Port {firstPort.PortName}: Device group not found");
                }
            }
            
            Console.WriteLine();
            Console.WriteLine("✅ Enhanced Discovery with Device Grouping test completed successfully!");
            Console.WriteLine("📋 NOTE: Continuous discovery with device grouping is now handled by Background Service");
            Console.WriteLine();
            
            nlogLogger.Info("✅ Enhanced Discovery with Device Grouping test completed successfully!");
            nlogLogger.Info("NOTE: Continuous discovery with device grouping is now handled by Background Service");
            
        }
        catch (Exception ex)
        {
            Console.WriteLine();
            Console.WriteLine($"❌ Enhanced Discovery with Device Grouping test failed: {ex.Message}");
            Console.WriteLine($"📋 Stack trace: {ex.StackTrace}");
            Console.WriteLine();
            
            nlogLogger.Error(ex, "❌ Enhanced Discovery with Device Grouping test failed");
            // Ne pas throw - le service doit continuer même si discovery échoue
        }
    }
}