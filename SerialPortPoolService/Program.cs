// SerialPortPoolService/Program.cs - FIXED pour Sprint 7 Client Demo
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using SerialPortPool.Core.Interfaces;
using SerialPortPool.Core.Models;
using SerialPortPool.Core.Services;
using SerialPortPoolService.Services;
using SerialPortPool.Core.Protocols; 

namespace SerialPortPoolService;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("🚀 SerialPortPoolService - Sprint 7 Client Demo");
        Console.WriteLine("Auto-Execution BIB Workflow + FT4232 Detection + RS232 TEST");
        Console.WriteLine("=".PadRight(80, '='));
        Console.WriteLine();

        try
        {
            if (args.Length > 0 && args[0] == "--console")
            {
                await RunInteractiveMode();
            }
            else
            {
                await RunClientDemoService(); // ✅ NOUVEAU: Mode client demo au lieu de service Windows
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Fatal error: {ex.Message}");
            Console.WriteLine($"📋 Details: {ex.StackTrace}");
            Environment.Exit(1);
        }
    }

    // ✅ NOUVEAU: Mode spécifique pour le client demo
    static async Task RunClientDemoService()
    {
        Console.WriteLine("🎬 Starting Client Demo Service mode...");
        
        var builder = Host.CreateApplicationBuilder();
        
        // Configure services
        ConfigureServicesForClientDemo(builder.Services);
        
        // ✅ CRUCIAL: Register Worker au lieu de PortDiscoveryBackgroundService
        builder.Services.AddHostedService<Worker>();
        
        var host = builder.Build();
        
        Console.WriteLine("✅ Client Demo Service configured and starting...");
        await host.RunAsync();
    }

    static async Task RunAsWindowsService()
    {
        Console.WriteLine("🔧 Starting Windows Service mode...");
        
        var builder = Host.CreateApplicationBuilder();
        
        // Configure services
        ConfigureServices(builder.Services);
        
        // Add Windows Service support
        builder.Services.AddWindowsService();
        
        // Add background service
        builder.Services.AddHostedService<PortDiscoveryBackgroundService>();
        
        var host = builder.Build();
        
        Console.WriteLine("✅ Windows Service configured and starting...");
        await host.RunAsync();
    }

    // ✅ NOUVEAU: Configuration spécifique pour le client demo
    static void ConfigureServicesForClientDemo(IServiceCollection services)
    {
        Console.WriteLine("⚙️ Configuring Client Demo services...");
        
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

            // ✅ CRUCIAL: Configuration permissive pour le demo
            var demoConfig = PortValidationConfiguration.CreateDevelopmentDefault();
            services.AddSingleton(demoConfig);
            Console.WriteLine($"📋 Demo validation config: RequireFtdi={demoConfig.RequireFtdiDevice}, Require4232H={demoConfig.Require4232HChip}");

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

            // ✅ CRUCIAL: Sprint 5 services pour le workflow
            services.AddMemoryCache();
            services.AddScoped<IBibConfigurationLoader, XmlBibConfigurationLoader>();
            services.AddScoped<IBibMappingService, TemporaryBibMappingService>();
            services.AddScoped<IProtocolHandlerFactory, ProtocolHandlerFactory>();
            services.AddScoped<RS232ProtocolHandler>();
            
            // ✅ CRUCIAL: BibWorkflowOrchestrator pour le client demo
            services.AddScoped<IBibWorkflowOrchestrator, BibWorkflowOrchestrator>();

            LoadClientDemoConfiguration(services);

            Console.WriteLine("✅ Client Demo services configured successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ ERROR configuring client demo services: {ex.Message}");
            throw;
        }
    }

    // ✅ NOUVEAU: Configuration spécifique client demo
    static void LoadClientDemoConfiguration(IServiceCollection services)
    {
        Console.WriteLine("📄 Loading Client Demo BIB configuration...");
        
        try
        {
            // Configuration client demo
            var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Configuration", "client-demo.xml");
            
            if (!File.Exists(configPath))
            {
                Console.WriteLine($"⚠️ Client demo config not found: {configPath}");
                Console.WriteLine("📄 Creating client demo configuration...");
                
                var configDir = Path.GetDirectoryName(configPath);
                if (!Directory.Exists(configDir))
                {
                    Directory.CreateDirectory(configDir!);
                }
                
                CreateClientDemoConfiguration(configPath);
            }
            
            // Pre-configure pour client demo
            services.AddSingleton<Dictionary<string, object>>(provider =>
            {
                var dict = new Dictionary<string, object>
                {
                    ["client_demo_path"] = configPath,
                    ["demo_mode"] = true,
                    ["_metadata"] = new { LoadedAt = DateTime.Now, Mode = "CLIENT_DEMO" }
                };
                
                Console.WriteLine($"✅ Client demo configuration ready: {configPath}");
                return dict;
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ ERROR loading client demo config: {ex.Message}");
        }
    }

    // ✅ NOUVEAU: Création de la config client demo
    static void CreateClientDemoConfiguration(string configPath)
    {
        Console.WriteLine("📄 Creating client demo configuration...");
        
        var clientDemoXml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<root>
  <bib id=""client_demo"" description=""Production Client Demo BIB"">
    <metadata>
      <board_type>production</board_type>
      <revision>v1.0</revision>
      <client>CLIENT_DEMO</client>
    </metadata>
    
    <uut id=""production_uut"" description=""Client Production UUT"">
      <port number=""1"">
        <protocol>rs232</protocol>
        <speed>115200</speed>
        <data_pattern>n81</data_pattern>
        <auto_discover>true</auto_discover>
        
        <!-- Simple TEST command as requested -->
        <start>
          <command>INIT_RS232</command>
          <expected_response>READY</expected_response>
          <timeout_ms>3000</timeout_ms>
        </start>
        
        <test>
          <command>TEST</command>
          <expected_response>PASS</expected_response>
          <timeout_ms>5000</timeout_ms>
        </test>
        
        <stop>
          <command>AT+QUIT</command>
          <expected_response>BYE</expected_response>
          <timeout_ms>2000</timeout_ms>
        </stop>
      </port>
    </uut>
  </bib>
</root>";

        try
        {
            File.WriteAllText(configPath, clientDemoXml);
            Console.WriteLine($"✅ Client demo configuration created: {configPath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ Could not create client demo config: {ex.Message}");
        }
    }

    static void ConfigureServices(IServiceCollection services)
    {
        // ... existing ConfigureServices code (unchanged)
        Console.WriteLine("⚙️ Configuring dependency injection services (COMPLETE Sprint 5)...");
        
        try
        {
            // Logging configuration - PRESERVED
            services.AddLogging(builder =>
            {
                builder.ClearProviders()
                       .AddNLog()
                       .AddConsole()
                       .SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Information);
            });

            // Validation configuration - PRESERVED
            var devConfig = PortValidationConfiguration.CreateDevelopmentDefault();
            services.AddSingleton(devConfig);
            Console.WriteLine($"📋 Using validation config: RequireFtdi={devConfig.RequireFtdiDevice}, Require4232H={devConfig.Require4232HChip}");

            // Core services - PRESERVED
            services.AddScoped<IFtdiDeviceReader, FtdiDeviceReader>();
            services.AddScoped<ISerialPortValidator, SerialPortValidator>();
            services.AddScoped<ISerialPortDiscovery, EnhancedSerialPortDiscoveryService>();
            
            // Device grouping service - PRESERVED
            services.AddScoped<IMultiPortDeviceAnalyzer, MultiPortDeviceAnalyzer>();
            
            // SystemInfo caching - PRESERVED
            services.AddScoped<SystemInfoCache>();
            services.AddScoped<ISystemInfoCache>(provider => provider.GetRequiredService<SystemInfoCache>());
            
            // Thread-safe pool management - PRESERVED
            services.AddScoped<ISerialPortPool, SerialPortPool.Core.Services.SerialPortPool>();

            Console.WriteLine("✅ Foundation services (Sprint 1-4) configured - ZERO TOUCH");

            // POC Extension Layer Services - ENHANCED
            services.AddScoped<IPortReservationService, PortReservationService>();

            Console.WriteLine("✅ Enhanced POC Sprint 5 extension layer services configured");

            ConfigureCompleteSprint5Services(services);
            LoadBibConfigurations(services);

            Console.WriteLine("✅ All dependency injection services configured successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ ERROR configuring services: {ex.Message}");
            throw;
        }
    }

    static void ConfigureCompleteSprint5Services(IServiceCollection services)
    {
        Console.WriteLine("⚙️ Configuring COMPLETE Sprint 5 services: XML + BIB + RS232 + Reservation...");
        
        try
        {
            services.AddMemoryCache();
            services.AddScoped<IBibConfigurationLoader, XmlBibConfigurationLoader>();
            services.AddScoped<IBibMappingService, TemporaryBibMappingService>();
            services.AddScoped<IProtocolHandlerFactory, ProtocolHandlerFactory>();
            services.AddScoped<RS232ProtocolHandler>();
            services.AddScoped<IBibWorkflowOrchestrator, BibWorkflowOrchestrator>();
            
            Console.WriteLine("✅ COMPLETE Sprint 5 services configured successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ ERROR configuring Sprint 5 services: {ex.Message}");
            throw;
        }
    }

    static void LoadBibConfigurations(IServiceCollection services)
    {
        // ... existing LoadBibConfigurations code (unchanged)
        Console.WriteLine("📄 Loading BIB configurations from XML...");
        
        try
        {
            var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Configuration", "bib-configurations.xml");
            
            if (!File.Exists(configPath))
            {
                Console.WriteLine($"⚠️ BIB configuration file not found: {configPath}");
                Console.WriteLine("📄 Creating default configuration...");
                
                var configDir = Path.GetDirectoryName(configPath);
                if (!Directory.Exists(configDir))
                {
                    Directory.CreateDirectory(configDir!);
                    Console.WriteLine($"📁 Created directory: {configDir}");
                }
                
                CreateDefaultBibConfiguration(configPath);
            }
            
            services.AddSingleton<Dictionary<string, object>>(provider =>
            {
                try
                {
                    var loader = provider.GetRequiredService<IBibConfigurationLoader>();
                    var configurations = loader.LoadConfigurationsAsync(configPath).GetAwaiter().GetResult();
                    
                    Console.WriteLine($"✅ Loaded {configurations.Count} BIB configuration(s) from XML");
                    foreach (var config in configurations.Values)
                    {
                        Console.WriteLine($"   📦 {config.BibId}: {config.Uuts.Count} UUT(s), {config.TotalPortCount} port(s)");
                    }
                    
                    var objectDict = new Dictionary<string, object>();
                    foreach (var kvp in configurations)
                    {
                        objectDict[kvp.Key] = kvp.Value;
                    }
                    objectDict["_metadata"] = new { LoadedAt = DateTime.Now, Source = configPath };
                    
                    return objectDict;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Error loading BIB configurations: {ex.Message}");
                    Console.WriteLine("📄 Using empty configuration dictionary");
                    return new Dictionary<string, object>
                    {
                        ["_error"] = ex.Message,
                        ["_metadata"] = new { LoadedAt = DateTime.Now, Source = "error_fallback" }
                    };
                }
            });
            
            Console.WriteLine("✅ BIB configurations loaded successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ ERROR loading BIB configurations: {ex.Message}");
        }
    }

    static void CreateDefaultBibConfiguration(string configPath)
    {
        // ... existing CreateDefaultBibConfiguration code (unchanged)
        Console.WriteLine("📄 Creating default BIB configuration for COMPLETE Sprint 5...");
        
        var defaultXml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<root>
  <!-- COMPLETE Sprint 5 Configuration -->
  <bib id=""bib_sprint5_complete"" description=""Complete Sprint 5 Production Configuration"">
    <metadata>
      <board_type>production</board_type>
      <revision>v5.0</revision>
      <purpose>Complete Sprint 5 XML Configuration + BIB Workflow + RS232 Protocol</purpose>
      <created_date>2025-07-31</created_date>
    </metadata>
    
    <uut id=""uut_rs232_production"" description=""Production RS232 UUT"">
      <metadata>
        <uut_type>rs232_production</uut_type>
        <test_mode>sprint5_complete</test_mode>
      </metadata>
      
      <port number=""1"">
        <protocol>rs232</protocol>
        <speed>115200</speed>
        <data_pattern>n81</data_pattern>
        <read_timeout>3000</read_timeout>
        <write_timeout>3000</write_timeout>
        <handshake>None</handshake>
        
        <start>
          <command>INIT_SPRINT5\r\n</command>
          <expected_response>READY_COMPLETE</expected_response>
          <timeout_ms>3000</timeout_ms>
          <retry_count>2</retry_count>
        </start>
        
        <test>
          <command>TEST_COMPLETE\r\n</command>
          <expected_response>PASS_SPRINT5</expected_response>
          <timeout_ms>5000</timeout_ms>
          <retry_count>2</retry_count>
        </test>
        
        <stop>
          <command>STOP_SPRINT5\r\n</command>
          <expected_response>BYE_COMPLETE</expected_response>
          <timeout_ms>2000</timeout_ms>
          <retry_count>1</retry_count>
        </stop>
      </port>
    </uut>
  </bib>
  
  <!-- Python Demo Configuration (backward compatibility) -->
  <bib id=""bib_demo"" description=""Python Dummy UUT Demo"">
    <uut id=""uut_python_simulator"">
      <port number=""1"">
        <protocol>rs232</protocol>
        <speed>115200</speed>
        <data_pattern>n81</data_pattern>
        
        <start>
          <command>INIT_RS232\r\n</command>
          <expected_response>READY</expected_response>
          <timeout_ms>3000</timeout_ms>
          <retry_count>2</retry_count>
        </start>
        
        <test>
          <command>RUN_TEST_1\r\n</command>
          <expected_response>PASS</expected_response>
          <timeout_ms>5000</timeout_ms>
          <retry_count>1</retry_count>
        </test>
        
        <stop>
          <command>STOP_RS232\r\n</command>
          <expected_response>BYE</expected_response>
          <timeout_ms>2000</timeout_ms>
          <retry_count>1</retry_count>
        </stop>
      </port>
    </uut>
  </bib>
</root>";

        try
        {
            File.WriteAllText(configPath, defaultXml);
            Console.WriteLine($"✅ Default COMPLETE Sprint 5 configuration created: {configPath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ Could not create default configuration: {ex.Message}");
        }
    }

    // ... rest of existing test methods (unchanged)
    static async Task RunInteractiveMode()
    {
        // ... existing code (unchanged)
    }

    static async Task TestFoundationPreservation(IServiceProvider serviceProvider)
    {
        // ... existing code (unchanged)
    }

    static async Task TestCompleteSprint5Integration(IServiceProvider serviceProvider)
    {
        // ... existing code (unchanged)
    }
}