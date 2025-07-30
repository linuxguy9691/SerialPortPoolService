using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using SerialPortPool.Core.Interfaces;
using SerialPortPool.Core.Services;
using SerialPortPool.Core.Models;
using System.IO.Ports;

namespace SerialPortPool.Demo;

/// <summary>
/// RS232 Demo Application - Sprint 5 Showcase - SIMPLIFIED VERSION
/// </summary>
public class Program
{
    private static IServiceProvider? _serviceProvider;
    private static ILogger<Program>? _logger;

    public static async Task<int> Main(string[] args)
    {
        try
        {
            // Setup and display banner
            DisplayBanner();
            
            // Setup services and DI container - SIMPLIFIED
            _serviceProvider = SetupServicesSimplified(args);
            _logger = _serviceProvider.GetRequiredService<ILogger<Program>>();
            
            _logger.LogInformation("🚀 RS232Demo application started");
            
            // Run interactive demo
            await RunInteractiveDemoAsync();
            
            _logger.LogInformation("✅ RS232Demo application completed successfully");
            return 0;
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"❌ Fatal error: {ex.Message}");
            Console.ForegroundColor = ConsoleColor.White;
            
            _logger?.LogError(ex, "💥 Fatal error in RS232Demo application");
            return 1;
        }
        finally
        {
            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }
    }

    /// <summary>
    /// Display application banner and information
    /// </summary>
    private static void DisplayBanner()
    {
        Console.Clear();
        Console.ForegroundColor = ConsoleColor.Cyan;
        
        Console.WriteLine("🏭 SerialPortPool Sprint 5 Demo - Multi-Protocol Communication");
        Console.WriteLine(new string('=', 65));
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine("🚀 Demonstrating: BIB → UUT → PORT → RS232 workflow");
        Console.WriteLine("🤖 Integration: Python dummy UUT + Real hardware support");
        Console.WriteLine("🎯 Architecture: ZERO TOUCH extension with Sprint 3-4 foundation");
        Console.WriteLine();
        
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("📋 Sprint 5 Achievements:");
        Console.WriteLine("   ✅ XML Configuration System (BIB→UUT→PORT hierarchy)");
        Console.WriteLine("   ✅ RS232 Protocol Handler (Production ready)");
        Console.WriteLine("   ✅ 3-Phase Workflow Engine (PowerOn → Test → PowerOff)");
        Console.WriteLine("   ✅ ZERO TOUCH Integration (65+ existing tests preserved)");
        Console.WriteLine("   ✅ Multi-Device Support (FT4232H + Dummy UUT)");
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine();
    }

    /// <summary>
    /// SIMPLIFIED setup - Only essential services to get demo working
    /// </summary>
    private static IServiceProvider SetupServicesSimplified(string[] args)
    {
        var services = new ServiceCollection();
        
        // Configuration setup
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddCommandLine(args)
            .Build();
        
        services.AddSingleton<IConfiguration>(configuration);
        
        // Logging setup
        services.AddLogging(builder =>
        {
            builder.AddConfiguration(configuration.GetSection("Logging"));
            builder.AddConsole();
        });
        
        // ================================
        // MINIMAL SERVICES - GET DEMO WORKING
        // ================================
        
        try 
        {
            // Try full setup first
            services.AddSingleton(PortValidationConfiguration.CreateDevelopmentDefault());
            services.AddScoped<IFtdiDeviceReader, FtdiDeviceReader>();
            services.AddScoped<ISerialPortValidator, SerialPortValidator>();
            services.AddScoped<ISerialPortDiscovery, EnhancedSerialPortDiscoveryService>();
            
            // System cache - both concrete and interface
            services.AddSingleton<SystemInfoCache>();
            services.AddSingleton<ISystemInfoCache>(provider => provider.GetRequiredService<SystemInfoCache>());
            
            services.AddScoped<ISerialPortPool, SerialPortPool.Core.Services.SerialPortPool>();
            services.AddScoped<IMultiPortDeviceAnalyzer, MultiPortDeviceAnalyzer>();
            
            // Demo services
            services.AddScoped<DemoOrchestrator>();
            services.AddSingleton<ConsoleHelper>();
            
            // Basic configuration
            services.AddSingleton<Dictionary<string, object>>(provider =>
            {
                return new Dictionary<string, object>
                {
                    ["demo_mode"] = "simplified",
                    ["services_status"] = "minimal_working_set"
                };
            });
            
            var serviceProvider = services.BuildServiceProvider();
            
            // Test service resolution
            var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
            logger.LogInformation("✅ All essential services registered successfully");
            
            return serviceProvider;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Service setup failed: {ex.Message}");
            Console.WriteLine("🔄 Falling back to ultra-minimal setup...");
            
            // Ultra-minimal fallback
            return CreateMinimalServiceProvider(services, configuration);
        }
    }
    
    /// <summary>
    /// Ultra-minimal service provider for basic demo functionality
    /// </summary>
    private static IServiceProvider CreateMinimalServiceProvider(ServiceCollection services, IConfiguration configuration)
    {
        services.Clear(); // Start fresh
        
        services.AddSingleton<IConfiguration>(configuration);
        services.AddLogging(builder => builder.AddConsole());
        services.AddSingleton<ConsoleHelper>();
        
        // Minimal demo orchestrator without full SerialPortPool
        services.AddSingleton<Dictionary<string, object>>(provider =>
        {
            return new Dictionary<string, object>
            {
                ["demo_mode"] = "minimal_fallback",
                ["services_status"] = "basic_console_only"
            };
        });
        
        return services.BuildServiceProvider();
    }

    /// <summary>
    /// Run interactive demo with scenario selection
    /// </summary>
    private static async Task RunInteractiveDemoAsync()
    {
        var consoleHelper = _serviceProvider!.GetRequiredService<ConsoleHelper>();
        
        // Check if we have full services or minimal fallback
        var config = _serviceProvider.GetRequiredService<Dictionary<string, object>>();
        var demoMode = config["demo_mode"].ToString();
        
        if (demoMode == "minimal_fallback")
        {
            await RunMinimalDemoAsync(consoleHelper);
            return;
        }
        
        // Try full demo with DemoOrchestrator
        try
        {
            var orchestrator = _serviceProvider.GetRequiredService<DemoOrchestrator>();
            await RunFullDemoAsync(orchestrator, consoleHelper);
        }
        catch (Exception ex)
        {
            _logger!.LogError(ex, "Full demo failed, falling back to minimal demo");
            await RunMinimalDemoAsync(consoleHelper);
        }
    }
    
    /// <summary>
    /// Full demo with DemoOrchestrator
    /// </summary>
    private static async Task RunFullDemoAsync(DemoOrchestrator orchestrator, ConsoleHelper consoleHelper)
    {
        while (true)
        {
            try
            {
                DisplayScenarioMenu();
                var selection = GetUserSelection();
                
                if (selection == "q" || selection == "quit" || selection == "exit")
                {
                    Console.WriteLine("👋 Goodbye! Thanks for using SerialPortPool Demo!");
                    break;
                }
                
                await ExecuteSelectedScenarioAsync(orchestrator, consoleHelper, selection);
                
                Console.WriteLine("\n" + new string('=', 65));
                Console.WriteLine("Press any key to continue, or 'q' + Enter to quit...");
                var key = Console.ReadKey(true);
                if (key.KeyChar == 'q')
                    break;
                    
                Console.Clear();
                DisplayBanner();
            }
            catch (Exception ex)
            {
                _logger!.LogError(ex, "Error during demo execution");
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"❌ Demo error: {ex.Message}");
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("Press any key to continue...");
                Console.ReadKey();
            }
        }
    }
    
    /// <summary>
    /// Minimal demo without full services
    /// </summary>
    private static async Task RunMinimalDemoAsync(ConsoleHelper consoleHelper)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("⚠️  Running in minimal demo mode (some services unavailable)");
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine();
        
        while (true)
        {
            Console.WriteLine("📋 Minimal Demo Options:");
            Console.WriteLine("   1. 📡 Show available COM ports");
            Console.WriteLine("   2. 🔧 Test simple serial port access");
            Console.WriteLine("   3. ℹ️  Show system information");
            Console.WriteLine("   q. 🚪 Quit");
            Console.WriteLine();
            
            Console.Write("Select option [1-3, q]: ");
            var input = Console.ReadLine()?.Trim().ToLowerInvariant() ?? "";
            Console.WriteLine();
            
            if (input == "q" || input == "quit")
            {
                Console.WriteLine("👋 Goodbye!");
                break;
            }
            
            switch (input)
            {
                case "1":
                    ShowAvailablePorts();
                    break;
                case "2":
                    await TestSimpleSerialAccess();
                    break;
                case "3":
                    ShowBasicSystemInfo();
                    break;
                default:
                    Console.WriteLine("❌ Invalid selection");
                    break;
            }
            
            Console.WriteLine("\nPress any key to continue...");
            Console.ReadKey();
            Console.Clear();
            DisplayBanner();
        }
    }
    
    /// <summary>
    /// Show available COM ports
    /// </summary>
    private static void ShowAvailablePorts()
    {
        Console.WriteLine("📡 Available Serial Ports:");
        var ports = SerialPort.GetPortNames();
        
        if (ports.Any())
        {
            foreach (var port in ports)
            {
                Console.WriteLine($"   • {port}");
            }
        }
        else
        {
            Console.WriteLine("   ❌ No serial ports detected");
        }
    }
    
    /// <summary>
    /// Test simple serial port access
    /// </summary>
    private static async Task TestSimpleSerialAccess()
    {
        var ports = SerialPort.GetPortNames();
        
        if (!ports.Any())
        {
            Console.WriteLine("❌ No serial ports available for testing");
            return;
        }
        
        var testPort = ports[0];
        Console.WriteLine($"🔧 Testing access to {testPort}...");
        
        try
        {
            using var serialPort = new SerialPort(testPort, 9600);
            serialPort.ReadTimeout = 1000;
            serialPort.WriteTimeout = 1000;
            
            serialPort.Open();
            Console.WriteLine($"✅ Successfully opened {testPort}");
            
            await Task.Delay(500);
            
            serialPort.Close();
            Console.WriteLine($"✅ Successfully closed {testPort}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Failed to access {testPort}: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Show basic system information
    /// </summary>
    private static void ShowBasicSystemInfo()
    {
        Console.WriteLine("ℹ️  Basic System Information:");
        Console.WriteLine($"   🖥️  OS: {Environment.OSVersion}");
        Console.WriteLine($"   🏗️  .NET: {Environment.Version}");
        Console.WriteLine($"   📁 Working Directory: {Environment.CurrentDirectory}");
        Console.WriteLine($"   👤 User: {Environment.UserName}");
        Console.WriteLine($"   🔢 Processor Count: {Environment.ProcessorCount}");
        Console.WriteLine($"   💾 Memory: {GC.GetTotalMemory(false) / 1024 / 1024} MB");
    }

    /// <summary>
    /// Display available demo scenarios
    /// </summary>
    private static void DisplayScenarioMenu()
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("📋 Available Demo Scenarios:");
        Console.ForegroundColor = ConsoleColor.White;
        
        Console.WriteLine("   1. 🤖 Python Dummy UUT (Recommended) - Safe testing with simulator");
        Console.WriteLine("   2. 🏭 Real Hardware Detection - Auto-detect FT4232H devices");
        Console.WriteLine("   3. 🔧 Custom Configuration - Manual port and config selection");
        Console.WriteLine("   4. 📊 Performance Test - Stress testing and metrics");
        Console.WriteLine("   5. 🔍 System Information - Show discovered devices and ports");
        Console.WriteLine("   6. ❓ Help & Documentation - Usage guide and troubleshooting");
        Console.WriteLine();
        Console.WriteLine("   q. 🚪 Quit");
        Console.WriteLine();
        
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.Write("Select scenario [1-6, q]: ");
        Console.ForegroundColor = ConsoleColor.White;
    }

    /// <summary>
    /// Get user scenario selection
    /// </summary>
    private static string GetUserSelection()
    {
        var input = Console.ReadLine()?.Trim().ToLowerInvariant() ?? "";
        Console.WriteLine();
        return input;
    }

    /// <summary>
    /// Execute the selected demo scenario
    /// </summary>
    private static async Task ExecuteSelectedScenarioAsync(
        DemoOrchestrator orchestrator, 
        ConsoleHelper consoleHelper, 
        string selection)
    {
        switch (selection)
        {
            case "1":
                await orchestrator.RunPythonDummyUUTDemoAsync();
                break;
                
            case "2":
                await orchestrator.RunRealHardwareDemoAsync();
                break;
                
            case "3":
                await orchestrator.RunCustomConfigurationDemoAsync();
                break;
                
            case "4":
                await orchestrator.RunPerformanceTestDemoAsync();
                break;
                
            case "5":
                await orchestrator.ShowSystemInformationAsync();
                break;
                
            case "6":
                consoleHelper.DisplayHelpInformation();
                break;
                
            default:
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"❌ Invalid selection: '{selection}'");
                Console.WriteLine("Please select a number from 1-6 or 'q' to quit.");
                Console.ForegroundColor = ConsoleColor.White;
                break;
        }
    }
}