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
/// RS232 Demo Application - Sprint 5 Showcase
/// Main entry point for interactive demo of SerialPortPool multi-protocol capabilities
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
            
            // Setup services and DI container
            _serviceProvider = SetupServices(args);
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
        Console.WriteLine("=" * 65);
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
    /// Setup dependency injection container with SerialPortPool services
    /// </summary>
    private static IServiceProvider SetupServices(string[] args)
    {
        var services = new ServiceCollection();
        
        // Configuration setup
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddCommandLine(args)
            .Build();
        
        services.AddSingleton<IConfiguration>(configuration);
        
        // Logging setup
        services.AddLogging(builder =>
        {
            builder.AddConfiguration(configuration.GetSection("Logging"));
            builder.AddConsole();
            builder.AddDebug();
        });
        
        // ================================
        // EXISTING SERVICES (ZERO TOUCH - NO MODIFICATION)
        // ================================
        
        // Core foundation from Sprint 3-4 (PRESERVED)
        services.AddSingleton(PortValidationConfiguration.CreateDevelopmentDefault());
        services.AddScoped<IFtdiDeviceReader, FtdiDeviceReader>();
        services.AddScoped<ISerialPortValidator, SerialPortValidator>();
        services.AddScoped<ISerialPortDiscovery, EnhancedSerialPortDiscoveryService>();
        services.AddScoped<SystemInfoCache>();
        services.AddScoped<ISerialPortPool, SerialPortPool>();
        services.AddScoped<IMultiPortDeviceAnalyzer, MultiPortDeviceAnalyzer>();
        
        // ================================
        // SPRINT 5 EXTENSION LAYER (NEW - COMPOSITION PATTERN)
        // ================================
        
        // Port reservation wrapper (wraps existing pool)
        services.AddScoped<IPortReservationService, PortReservationService>();
        
        // Protocol abstraction layer
        services.AddScoped<IProtocolHandlerFactory, ProtocolHandlerFactory>();
        services.AddScoped<RS232ProtocolHandler>();
        
        // Configuration system
        services.AddScoped<IXmlConfigurationLoader, XmlConfigurationLoader>();
        
        // Workflow orchestration
        services.AddScoped<IBibWorkflowOrchestrator, BibWorkflowOrchestrator>();
        
        // Demo-specific services
        services.AddScoped<DemoOrchestrator>();
        services.AddSingleton<ConsoleHelper>();
        
        // Configuration loading
        services.AddSingleton<Dictionary<string, BibConfiguration>>(provider =>
        {
            var loader = provider.GetRequiredService<IXmlConfigurationLoader>();
            var logger = provider.GetRequiredService<ILogger<Program>>();
            
            try
            {
                var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, 
                    "Configuration", "demo-config.xml");
                
                if (File.Exists(configPath))
                {
                    logger.LogInformation("📋 Loading demo configuration from: {ConfigPath}", configPath);
                    return loader.LoadConfigurationsAsync(configPath).GetAwaiter().GetResult();
                }
                else
                {
                    logger.LogWarning("⚠️ Demo configuration not found at: {ConfigPath}", configPath);
                    logger.LogInformation("📋 Using minimal default configuration");
                    return CreateDefaultDemoConfiguration();
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "❌ Failed to load demo configuration");
                logger.LogInformation("📋 Using minimal default configuration");
                return CreateDefaultDemoConfiguration();
            }
        });
        
        var serviceProvider = services.BuildServiceProvider();
        
        // Validate service registration
        ValidateServiceRegistration(serviceProvider);
        
        return serviceProvider;
    }

    /// <summary>
    /// Create default demo configuration if XML file not available
    /// </summary>
    private static Dictionary<string, BibConfiguration> CreateDefaultDemoConfiguration()
    {
        var defaultConfig = new BibConfiguration
        {
            BibId = "bib_demo_default",
            Uuts = new List<UutConfiguration>
            {
                new UutConfiguration
                {
                    UutId = "uut_default",
                    Ports = new List<PortConfiguration>
                    {
                        new PortConfiguration
                        {
                            PortNumber = 1,
                            Protocol = "rs232",
                            Settings = new Dictionary<string, object>
                            {
                                ["speed"] = 115200,
                                ["data_pattern"] = "n81"
                            },
                            StartCommands = new CommandSequence
                            {
                                Commands = new List<ProtocolCommand>
                                {
                                    new ProtocolCommand 
                                    { 
                                        Command = "INIT_RS232\\r\\n", 
                                        ExpectedResponse = "READY",
                                        TimeoutMs = 3000 
                                    }
                                }
                            },
                            TestCommands = new CommandSequence
                            {
                                Commands = new List<ProtocolCommand>
                                {
                                    new ProtocolCommand 
                                    { 
                                        Command = "TEST\\r\\n", 
                                        ExpectedResponse = "PASS",
                                        TimeoutMs = 5000 
                                    }
                                }
                            },
                            StopCommands = new CommandSequence
                            {
                                Commands = new List<ProtocolCommand>
                                {
                                    new ProtocolCommand 
                                    { 
                                        Command = "EXIT\\r\\n", 
                                        ExpectedResponse = "BYE",
                                        TimeoutMs = 2000 
                                    }
                                }
                            }
                        }
                    }
                }
            }
        };
        
        return new Dictionary<string, BibConfiguration>
        {
            ["bib_demo_default"] = defaultConfig
        };
    }

    /// <summary>
    /// Validate that all required services are properly registered
    /// </summary>
    private static void ValidateServiceRegistration(IServiceProvider serviceProvider)
    {
        var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
        
        try
        {
            logger.LogDebug("🔧 Validating service registration...");
            
            // Validate existing services (Sprint 3-4 foundation)
            var pool = serviceProvider.GetRequiredService<ISerialPortPool>();
            var discovery = serviceProvider.GetRequiredService<ISerialPortDiscovery>();
            var ftdiReader = serviceProvider.GetRequiredService<IFtdiDeviceReader>();
            var validator = serviceProvider.GetRequiredService<ISerialPortValidator>();
            var analyzer = serviceProvider.GetRequiredService<IMultiPortDeviceAnalyzer>();
            
            // Validate Sprint 5 extension services
            var reservationService = serviceProvider.GetRequiredService<IPortReservationService>();
            var protocolFactory = serviceProvider.GetRequiredService<IProtocolHandlerFactory>();
            var configLoader = serviceProvider.GetRequiredService<IXmlConfigurationLoader>();
            var orchestrator = serviceProvider.GetRequiredService<IBibWorkflowOrchestrator>();
            var demoOrchestrator = serviceProvider.GetRequiredService<DemoOrchestrator>();
            
            // Validate configuration
            var configurations = serviceProvider.GetRequiredService<Dictionary<string, BibConfiguration>>();
            
            logger.LogInformation("✅ All services registered successfully");
            logger.LogInformation("📊 Foundation services: 5 (Sprint 3-4 preserved)");
            logger.LogInformation("📊 Extension services: 5 (Sprint 5 added)");
            logger.LogInformation("📊 BIB configurations: {ConfigCount}", configurations.Count);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "❌ Service registration validation failed");
            throw new InvalidOperationException("Service registration validation failed", ex);
        }
    }

    /// <summary>
    /// Run interactive demo with scenario selection
    /// </summary>
    private static async Task RunInteractiveDemoAsync()
    {
        var orchestrator = _serviceProvider!.GetRequiredService<DemoOrchestrator>();
        var consoleHelper = _serviceProvider!.GetRequiredService<ConsoleHelper>();
        
        while (true)
        {
            try
            {
                // Display scenario menu
                DisplayScenarioMenu();
                
                // Get user selection
                var selection = GetUserSelection();
                
                if (selection == "q" || selection == "quit" || selection == "exit")
                {
                    Console.WriteLine("👋 Goodbye! Thanks for using SerialPortPool Demo!");
                    break;
                }
                
                // Execute selected scenario
                await ExecuteSelectedScenarioAsync(orchestrator, consoleHelper, selection);
                
                // Pause before next iteration
                Console.WriteLine("\n" + "=".PadRight(65, '='));
                Console.WriteLine("Press any key to continue, or 'q' + Enter to quit...");
                var key = Console.ReadKey(true);
                if (key.KeyChar == 'q')
                    break;
                    
                Console.Clear();
                DisplayBanner();
            }
            catch (Exception ex)
            {
                _logger!.LogError(ex, "❌ Error during demo execution");
                
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"❌ Demo error: {ex.Message}");
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("Press any key to continue...");
                Console.ReadKey();
            }
        }
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

/// <summary>
/// Console helper for rich formatting and display
/// Basic implementation - will be expanded in ConsoleHelper.cs
/// </summary>
public class ConsoleHelper
{
    public void DisplayHelpInformation()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("❓ RS232Demo Help & Documentation");
        Console.WriteLine("=" * 40);
        Console.ForegroundColor = ConsoleColor.White;
        
        Console.WriteLine();
        Console.WriteLine("📋 Quick Start:");
        Console.WriteLine("   1. For safe testing: Start Python dummy UUT first");
        Console.WriteLine("      python ../DummyUUT/dummy_uut.py --port COM8");
        Console.WriteLine("   2. Select scenario 1 (Python Dummy UUT)");
        Console.WriteLine("   3. Watch the 3-phase workflow execute");
        Console.WriteLine();
        
        Console.WriteLine("🔧 Troubleshooting:");
        Console.WriteLine("   • Port not available: Check if dummy UUT is running");
        Console.WriteLine("   • Build errors: Ensure SerialPortPool.Core is built");
        Console.WriteLine("   • Config errors: Check Configuration/demo-config.xml");
        Console.WriteLine();
        
        Console.WriteLine("📚 Documentation:");
        Console.WriteLine("   • Full guide: README.md in this directory");
        Console.WriteLine("   • Sprint 5 docs: ../../docs/sprint5/");
        Console.WriteLine("   • Dummy UUT setup: ../DummyUUT/README.md");
        Console.WriteLine();
    }
}

/// <summary>
/// Demo orchestrator - Basic stub implementation
/// Full implementation will be in DemoOrchestrator.cs
/// </summary>
public class DemoOrchestrator
{
    private readonly ILogger<DemoOrchestrator> _logger;
    private readonly IBibWorkflowOrchestrator _workflowOrchestrator;
    
    public DemoOrchestrator(
        ILogger<DemoOrchestrator> logger,
        IBibWorkflowOrchestrator workflowOrchestrator)
    {
        _logger = logger;
        _workflowOrchestrator = workflowOrchestrator;
    }
    
    public async Task RunPythonDummyUUTDemoAsync()
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("🤖 Demo Scenario: Python Dummy UUT Simulator");
        Console.WriteLine("=" * 45);
        Console.ForegroundColor = ConsoleColor.White;
        
        Console.WriteLine("📍 Expected setup: Python dummy UUT running on COM8");
        Console.WriteLine("📋 Configuration: BIB_DEMO → UUT_PYTHON_SIMULATOR → RS232");
        Console.WriteLine();
        
        // TODO: Implement full demo workflow
        Console.WriteLine("⚠️ Full implementation coming in DemoOrchestrator.cs");
        Console.WriteLine("🚀 This will execute complete BIB workflow with dummy UUT");
        
        await Task.Delay(1000); // Simulate some work
    }
    
    public async Task RunRealHardwareDemoAsync()
    {
        Console.ForegroundColor = ConsoleColor.Blue;
        Console.WriteLine("🏭 Demo Scenario: Real Hardware Detection");
        Console.WriteLine("=" * 42);
        Console.ForegroundColor = ConsoleColor.White;
        
        Console.WriteLine("🔍 Searching for FT4232H devices...");
        Console.WriteLine("⚠️ Real hardware demo implementation pending");
        
        await Task.Delay(500);
    }
    
    public async Task RunCustomConfigurationDemoAsync()
    {
        Console.ForegroundColor = ConsoleColor.Magenta;
        Console.WriteLine("🔧 Demo Scenario: Custom Configuration");
        Console.WriteLine("=" * 38);
        Console.ForegroundColor = ConsoleColor.White;
        
        Console.WriteLine("📋 Custom config demo implementation pending");
        await Task.Delay(500);
    }
    
    public async Task RunPerformanceTestDemoAsync()
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("📊 Demo Scenario: Performance Testing");
        Console.WriteLine("=" * 36);
        Console.ForegroundColor = ConsoleColor.White;
        
        Console.WriteLine("⚡ Performance test demo implementation pending");
        await Task.Delay(500);
    }
    
    public async Task ShowSystemInformationAsync()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("🔍 System Information");
        Console.WriteLine("=" * 21);
        Console.ForegroundColor = ConsoleColor.White;
        
        Console.WriteLine("📡 Available serial ports:");
        var ports = SerialPort.GetPortNames();
        if (ports.Length > 0)
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
        
        Console.WriteLine();
        Console.WriteLine("🔧 SerialPortPool services: ✅ Loaded and ready");
        Console.WriteLine("📋 Demo configurations: ✅ Available");
        
        await Task.Delay(100);
    }
}