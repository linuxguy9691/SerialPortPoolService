// SerialPortPoolService/Program.cs - ENHANCED CLIENT DEMO avec paramètres
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using SerialPortPool.Core.Interfaces;
using SerialPortPool.Core.Models;
using SerialPortPool.Core.Services;
using SerialPortPoolService.Services;
using SerialPortPool.Core.Protocols;
using System.CommandLine;

namespace SerialPortPoolService;

class Program
{
    static async Task<int> Main(string[] args)
    {
        Console.WriteLine("🚀 SerialPortPoolService - Enhanced Client Demo");
        Console.WriteLine("🎛️ XML Configuration + Loop Mode + Service Demo");
        Console.WriteLine("=".PadRight(80, '='));
        Console.WriteLine();

        // ✅ NOUVEAU: Command Line Interface avec System.CommandLine
        var xmlConfigOption = new Option<string>(
            "--xml-config", 
            getDefaultValue: () => "client-demo.xml",
            description: "Path to XML configuration file (relative to Configuration folder)");

        var loopOption = new Option<bool>(
            "--loop", 
            getDefaultValue: () => false,
            description: "Run workflow in continuous loop mode");

        var intervalOption = new Option<int>(
            "--interval", 
            getDefaultValue: () => 30,
            description: "Interval between loops in seconds (default: 30)");

        var serviceDemoOption = new Option<bool>(
            "--service-demo", 
            getDefaultValue: () => false,
            description: "Demonstrate Windows Service capabilities");

        var consoleOption = new Option<bool>(
            "--console", 
            getDefaultValue: () => false,
            description: "Run in interactive console mode");

        var rootCommand = new RootCommand("SerialPortPool Service - Enhanced Client Demo")
        {
            xmlConfigOption,
            loopOption,
            intervalOption,
            serviceDemoOption,
            consoleOption
        };

        rootCommand.SetHandler(async (xmlConfig, loop, interval, serviceDemo, console) =>
        {
            try
            {
                var demoConfig = new ClientDemoConfiguration
                {
                    XmlConfigFile = xmlConfig,
                    LoopMode = loop,
                    LoopIntervalSeconds = interval,
                    ServiceDemo = serviceDemo,
                    ConsoleMode = console
                };

                Console.WriteLine($"📋 Configuration:");
                Console.WriteLine($"   📄 XML Config: {demoConfig.XmlConfigFile}");
                Console.WriteLine($"   🔄 Loop Mode: {(demoConfig.LoopMode ? "ENABLED" : "DISABLED")}");
                if (demoConfig.LoopMode)
                {
                    Console.WriteLine($"   ⏱️ Loop Interval: {demoConfig.LoopIntervalSeconds} seconds");
                }
                Console.WriteLine($"   🔧 Service Demo: {(demoConfig.ServiceDemo ? "ENABLED" : "DISABLED")}");
                Console.WriteLine();

                if (demoConfig.ServiceDemo)
                {
                    await RunServiceDemo();
                }
                else if (demoConfig.ConsoleMode)
                {
                    await RunInteractiveMode();
                }
                else
                {
                    await RunEnhancedClientDemo(demoConfig);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Fatal error: {ex.Message}");
                Console.WriteLine($"📋 Details: {ex.StackTrace}");
                Environment.Exit(1);
            }
        }, xmlConfigOption, loopOption, intervalOption, serviceDemoOption, consoleOption);

        return await rootCommand.InvokeAsync(args);
    }

    // ✅ NOUVEAU: Enhanced Client Demo avec configuration
    static async Task RunEnhancedClientDemo(ClientDemoConfiguration config)
    {
        Console.WriteLine("🎬 Starting Enhanced Client Demo Service...");
        
        var builder = Host.CreateApplicationBuilder();
        
        // Configure services avec configuration custom
        ConfigureServicesForEnhancedDemo(builder.Services, config);
        
        // Register enhanced worker
        builder.Services.AddSingleton(config);
        builder.Services.AddHostedService<EnhancedWorker>();
        
        var host = builder.Build();
        
        Console.WriteLine("✅ Enhanced Client Demo Service configured and starting...");
        
        // ✅ NOUVEAU: Gestion gracieuse d'arrêt en mode loop
        Console.CancelKeyPress += (sender, e) =>
        {
            e.Cancel = true;
            Console.WriteLine("\n🛑 Graceful shutdown requested...");
            host.StopAsync().Wait(TimeSpan.FromSeconds(10));
        };
        
        await host.RunAsync();
    }

    // ✅ NOUVEAU: Service Windows Demo
    static async Task RunServiceDemo()
    {
        Console.WriteLine("🔧 Windows Service Demonstration Mode");
        Console.WriteLine("🎯 Showing service installation, status, and management...");
        Console.WriteLine();

        try
        {
            // Check if running as Administrator
            bool isAdmin = IsRunningAsAdministrator();
            Console.WriteLine($"👤 Administrator Rights: {(isAdmin ? "✅ YES" : "❌ NO")}");
            
            if (!isAdmin)
            {
                Console.WriteLine("⚠️ Administrator rights required for service operations");
                Console.WriteLine("💡 Run as Administrator for full service demo");
                Console.WriteLine();
            }

            // Demonstrate service commands
            await DemonstrateServiceCommands(isAdmin);
            
            // Show how service would run
            Console.WriteLine("🎬 Simulating service execution...");
            var config = new ClientDemoConfiguration
            {
                XmlConfigFile = "client-demo.xml",
                LoopMode = true,
                LoopIntervalSeconds = 10,
                ServiceDemo = false,
                ConsoleMode = false
            };
            
            Console.WriteLine("⏱️ Running 3 demo cycles...");
            await RunLimitedDemo(config, 3);
            
            Console.WriteLine("✅ Service demo completed!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Service demo error: {ex.Message}");
        }
    }

    // ✅ NOUVEAU: Démonstration des commandes service Windows
    static async Task DemonstrateServiceCommands(bool isAdmin)
    {
        Console.WriteLine("📋 Windows Service Commands Demonstration:");
        Console.WriteLine();

        var serviceName = "SerialPortPoolService";
        var commands = new[]
        {
            ($"sc create {serviceName}", "Install service"),
            ($"sc start {serviceName}", "Start service"),
            ($"sc query {serviceName}", "Check service status"),
            ($"sc stop {serviceName}", "Stop service"),
            ($"sc delete {serviceName}", "Uninstall service")
        };

        foreach (var (command, description) in commands)
        {
            Console.WriteLine($"🔧 {description}:");
            Console.WriteLine($"   💻 Command: {command}");
            
            if (isAdmin && command.Contains("query"))
            {
                // Actually try to query the service
                try
                {
                    var result = await RunCommand("sc", $"query {serviceName}");
                    Console.WriteLine($"   📊 Result: {result}");
                }
                catch
                {
                    Console.WriteLine($"   📊 Result: Service not currently installed");
                }
            }
            else
            {
                Console.WriteLine($"   📊 Result: {(isAdmin ? "Would execute if service were installed" : "Requires Administrator")}");
            }
            Console.WriteLine();
        }

        // Show service installation example
        Console.WriteLine("💡 Service Installation Example:");
        Console.WriteLine($"   sc create {serviceName} binPath= \"C:\\Path\\To\\SerialPortPoolService.exe\"");
        Console.WriteLine($"   sc description {serviceName} \"Serial Port Pool Management Service\"");
        Console.WriteLine($"   sc config {serviceName} start= auto");
        Console.WriteLine();
    }

    // ✅ NOUVEAU: Demo limité pour simulation service
    static async Task RunLimitedDemo(ClientDemoConfiguration config, int maxCycles)
    {
        var builder = Host.CreateApplicationBuilder();
        ConfigureServicesForEnhancedDemo(builder.Services, config);
        
        // Override configuration for limited demo
        var limitedConfig = config with { MaxCycles = maxCycles };
        builder.Services.AddSingleton(limitedConfig);
        builder.Services.AddHostedService<EnhancedWorker>();
        
        var host = builder.Build();
        
        using var cts = new CancellationTokenSource();
        var runTask = host.RunAsync(cts.Token);
        
        // Wait for demo completion or timeout
        var completed = await Task.WhenAny(runTask, Task.Delay(TimeSpan.FromMinutes(2)));
        
        if (completed != runTask)
        {
            Console.WriteLine("⏰ Demo timeout - stopping...");
        }
        
        cts.Cancel();
        try
        {
            await runTask;
        }
        catch (OperationCanceledException)
        {
            // Expected
        }
    }

    // ✅ NOUVEAU: Configuration services pour demo enhanced
    static void ConfigureServicesForEnhancedDemo(IServiceCollection services, ClientDemoConfiguration config)
    {
        Console.WriteLine("⚙️ Configuring Enhanced Demo services...");
        
        try
        {
            // Logging configuration
            services.AddLogging(builder =>
            {
                builder.ClearProviders()
                       .AddNLog()
                       .AddConsole()
                       .SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Information);
            });

            // Configuration permissive pour le demo
            var demoValidationConfig = PortValidationConfiguration.CreateDevelopmentDefault();
            services.AddSingleton(demoValidationConfig);

            // Core services
            services.AddScoped<IFtdiDeviceReader, FtdiDeviceReader>();
            services.AddScoped<ISerialPortValidator, SerialPortValidator>();
            services.AddScoped<ISerialPortDiscovery, EnhancedSerialPortDiscoveryService>();
            
            // SystemInfo caching
            services.AddScoped<SystemInfoCache>();
            services.AddScoped<ISystemInfoCache>(provider => provider.GetRequiredService<SystemInfoCache>());
            
            // Thread-safe pool management
            services.AddScoped<ISerialPortPool, SerialPortPool.Core.Services.SerialPortPool>();

            // POC Extension Layer Services
            services.AddScoped<IPortReservationService, PortReservationService>();

            // Sprint 5/6 services pour le workflow
            services.AddMemoryCache();
            services.AddScoped<IBibConfigurationLoader, XmlBibConfigurationLoader>();
            services.AddScoped<IBibMappingService, TemporaryBibMappingService>();
            services.AddScoped<IProtocolHandlerFactory, ProtocolHandlerFactory>();
            services.AddScoped<RS232ProtocolHandler>();
            
            // BibWorkflowOrchestrator pour le client demo
            services.AddScoped<IBibWorkflowOrchestrator, BibWorkflowOrchestrator>();

            LoadEnhancedDemoConfiguration(services, config);

            Console.WriteLine("✅ Enhanced Demo services configured successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ ERROR configuring enhanced demo services: {ex.Message}");
            throw;
        }
    }

    // ✅ NOUVEAU: Configuration enhanced avec paramètres XML
    static void LoadEnhancedDemoConfiguration(IServiceCollection services, ClientDemoConfiguration config)
    {
        Console.WriteLine($"📄 Loading Enhanced Demo configuration: {config.XmlConfigFile}...");
        
        try
        {
            // Résoudre le chemin du fichier XML
            var configPath = ResolveConfigPath(config.XmlConfigFile);
            
            if (!File.Exists(configPath))
            {
                Console.WriteLine($"⚠️ Config file not found: {configPath}");
                Console.WriteLine("📄 Creating default configuration...");
                
                var configDir = Path.GetDirectoryName(configPath);
                if (!Directory.Exists(configDir))
                {
                    Directory.CreateDirectory(configDir!);
                }
                
                CreateEnhancedDemoConfiguration(configPath, config);
            }
            
            services.AddSingleton<Dictionary<string, object>>(provider =>
            {
                var dict = new Dictionary<string, object>
                {
                    ["config_path"] = configPath,
                    ["xml_file"] = config.XmlConfigFile,
                    ["loop_mode"] = config.LoopMode,
                    ["loop_interval"] = config.LoopIntervalSeconds,
                    ["service_demo"] = config.ServiceDemo,
                    ["_metadata"] = new { 
                        LoadedAt = DateTime.Now, 
                        Mode = "ENHANCED_CLIENT_DEMO",
                        ConfigFile = config.XmlConfigFile
                    }
                };
                
                Console.WriteLine($"✅ Enhanced demo configuration ready: {configPath}");
                return dict;
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ ERROR loading enhanced demo config: {ex.Message}");
        }
    }

    // ✅ NOUVEAU: Résolution du chemin de configuration
    static string ResolveConfigPath(string xmlFileName)
    {
        // Si chemin absolu, utiliser tel quel
        if (Path.IsPathRooted(xmlFileName))
        {
            return xmlFileName;
        }
        
        // Sinon, chercher dans Configuration folder
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        var configDir = Path.Combine(baseDir, "Configuration");
        
        // Ajouter .xml si pas d'extension
        if (!Path.HasExtension(xmlFileName))
        {
            xmlFileName += ".xml";
        }
        
        return Path.Combine(configDir, xmlFileName);
    }

    // ✅ NOUVEAU: Création configuration enhanced
    static void CreateEnhancedDemoConfiguration(string configPath, ClientDemoConfiguration config)
    {
        Console.WriteLine($"📄 Creating enhanced demo configuration: {Path.GetFileName(configPath)}...");
        
        var enhancedDemoXml = $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<root>
  <bib id=""client_demo"" description=""Enhanced Production Client Demo with Loop Support"">
    <metadata>
      <board_type>production</board_type>
      <revision>v2.0</revision>
      <client>ENHANCED_CLIENT_DEMO</client>
      <loop_support>true</loop_support>
      <created_date>{DateTime.Now:yyyy-MM-dd}</created_date>
    </metadata>
    
    <uut id=""production_uut"" description=""Enhanced Client Production UUT"">
      <metadata>
        <uut_type>enhanced_production</uut_type>
        <loop_interval>{config.LoopIntervalSeconds}</loop_interval>
      </metadata>
      
      <port number=""1"">
        <protocol>rs232</protocol>
        <speed>115200</speed>
        <data_pattern>n81</data_pattern>
        <auto_discover>true</auto_discover>
        
        <!-- Enhanced TEST commands sequence -->
        <start>
          <command>ENHANCED_INIT</command>
          <expected_response>ENHANCED_READY</expected_response>
          <timeout_ms>3000</timeout_ms>
        </start>
        
        <test>
          <command>ENHANCED_TEST</command>
          <expected_response>ENHANCED_PASS</expected_response>
          <timeout_ms>5000</timeout_ms>
        </test>
        
        <stop>
          <command>ENHANCED_QUIT</command>
          <expected_response>ENHANCED_BYE</expected_response>
          <timeout_ms>2000</timeout_ms>
        </stop>
      </port>
    </uut>
  </bib>
</root>";

        try
        {
            File.WriteAllText(configPath, enhancedDemoXml);
            Console.WriteLine($"✅ Enhanced demo configuration created: {configPath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ Could not create enhanced demo config: {ex.Message}");
        }
    }

    // Existing methods (unchanged)
    static async Task RunInteractiveMode()
    {
        Console.WriteLine("🔧 Interactive Mode - Not implemented in this demo");
        Console.WriteLine("💡 Use enhanced demo modes instead:");
        Console.WriteLine("   --xml-config myconfig.xml --loop --interval 30");
        await Task.Delay(1000);
    }

    // ✅ HELPER: Check Administrator rights
    static bool IsRunningAsAdministrator()
    {
        try
        {
            var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
            var principal = new System.Security.Principal.WindowsPrincipal(identity);
            return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
        }
        catch
        {
            return false;
        }
    }

    // ✅ HELPER: Run command
    static async Task<string> RunCommand(string command, string arguments)
    {
        try
        {
            var processInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = command,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = System.Diagnostics.Process.Start(processInfo);
            if (process == null) return "Failed to start process";

            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();
            
            await process.WaitForExitAsync();
            
            return string.IsNullOrEmpty(error) ? output : $"Error: {error}";
        }
        catch (Exception ex)
        {
            return $"Exception: {ex.Message}";
        }
    }
}

// ✅ NOUVEAU: Configuration pour enhanced demo
public record ClientDemoConfiguration
{
    public string XmlConfigFile { get; init; } = "client-demo.xml";
    public bool LoopMode { get; init; } = false;
    public int LoopIntervalSeconds { get; init; } = 30;
    public bool ServiceDemo { get; init; } = false;
    public bool ConsoleMode { get; init; } = false;
    public int? MaxCycles { get; init; } = null; // Pour demo limité
}