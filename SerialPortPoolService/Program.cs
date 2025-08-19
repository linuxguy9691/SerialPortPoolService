// ===================================================================
// SPRINT 10 FINAL: Enhanced Program.cs with Multi-BIB Integration
// File: SerialPortPoolService/Program.cs
// Purpose: Production Multi-BIB service with command line interface
// ===================================================================

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

// ✅ COMPATIBILITY: Keep legacy ClientDemoConfiguration for Worker.cs
public record ClientDemoConfiguration
{
    public string XmlConfigFile { get; init; } = "client-demo.xml";
    public bool LoopMode { get; init; } = false;
    public int LoopIntervalSeconds { get; init; } = 30;
    public bool ServiceDemo { get; init; } = false;
    public bool ConsoleMode { get; init; } = false;
    public int? MaxCycles { get; init; } = null; // Pour demo limité
}

class Program
{
    static async Task<int> Main(string[] args)
    {
        Console.WriteLine("🚀 SerialPortPoolService - Multi-BIB Production Service");
        Console.WriteLine("🎛️ SPRINT 10 COMPLETE: Multi-BIB + Multi-UUT + GPIO Integration");
        Console.WriteLine("=".PadRight(80, '='));
        Console.WriteLine();

        // ✅ SPRINT 10 FINAL: Enhanced Command Line Interface
        var rootCommand = CreateMultiBibCommandLine();
        return await rootCommand.InvokeAsync(args);
    }

    /// <summary>
    /// Create comprehensive Multi-BIB command line interface
    /// </summary>
    static RootCommand CreateMultiBibCommandLine()
    {
        // Configuration options
        var xmlConfigOption = new Option<string>(
            "--xml-config", 
            getDefaultValue: () => "client-demo.xml",
            description: "Path to XML configuration file");

        var bibIdsOption = new Option<string[]>(
            "--bib-ids",
            description: "Specific BIB_IDs to execute (comma-separated)")
        {
            AllowMultipleArgumentsPerToken = true
        };

        var allBibsOption = new Option<bool>(
            "--all-bibs",
            getDefaultValue: () => false,
            description: "Execute ALL configured BIB_IDs");

        // Execution mode options
        var modeOption = new Option<string>(
            "--mode",
            getDefaultValue: () => "single",
            description: "Execution mode: single, continuous, scheduled, on-demand");

        var intervalOption = new Option<int>(
            "--interval",
            getDefaultValue: () => 30,
            description: "Interval for continuous/scheduled mode (minutes)");

        var detailedLogsOption = new Option<bool>(
            "--detailed-logs",
            getDefaultValue: () => true,
            description: "Include detailed execution logs");

        // Legacy compatibility options
        var legacyLoopOption = new Option<bool>(
            "--loop",
            getDefaultValue: () => false,
            description: "Legacy: Run in continuous loop mode");

        var serviceDemoOption = new Option<bool>(
            "--service-demo",
            getDefaultValue: () => false,
            description: "Legacy: Windows Service demonstration");

        var rootCommand = new RootCommand("SerialPortPool Multi-BIB Production Service")
        {
            xmlConfigOption,
            bibIdsOption,
            allBibsOption,
            modeOption,
            intervalOption,
            detailedLogsOption,
            legacyLoopOption,
            serviceDemoOption
        };

        rootCommand.SetHandler(async (xmlConfig, bibIds, allBibs, mode, interval, detailedLogs, legacyLoop, serviceDemo) =>
        {
            try
            {
                // ✅ Convert legacy options to new configuration
                var config = CreateMultiBibConfiguration(
                    xmlConfig, bibIds, allBibs, mode, interval, detailedLogs, legacyLoop, serviceDemo);

                await RunMultiBibService(config);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Fatal error: {ex.Message}");
                Console.WriteLine($"📋 Details: {ex.StackTrace}");
                Environment.Exit(1);
            }
        }, xmlConfigOption, bibIdsOption, allBibsOption, modeOption, intervalOption, detailedLogsOption, legacyLoopOption, serviceDemoOption);

        return rootCommand;
    }

    /// <summary>
    /// Create Multi-BIB configuration from command line options
    /// </summary>
    static MultiBibServiceConfiguration CreateMultiBibConfiguration(
        string xmlConfig, string[] bibIds, bool allBibs, string mode, int interval, 
        bool detailedLogs, bool legacyLoop, bool serviceDemo)
    {
        // Handle legacy options
        if (legacyLoop) mode = "continuous";
        if (serviceDemo) return CreateServiceDemoConfiguration();

        // Parse execution mode
        var executionMode = mode.ToLowerInvariant() switch
        {
            "single" => MultiBibExecutionMode.SingleRun,
            "continuous" => MultiBibExecutionMode.Continuous,
            "scheduled" => MultiBibExecutionMode.Scheduled,
            "on-demand" => MultiBibExecutionMode.OnDemand,
            _ => MultiBibExecutionMode.SingleRun
        };

        // Resolve configuration path
        var configPath = ResolveConfigPath(xmlConfig);

        // Create configuration
        return new MultiBibServiceConfiguration
        {
            ExecutionMode = executionMode,
            TargetBibIds = bibIds?.Any() == true ? bibIds.ToList() : null,
            DefaultConfigurationPath = configPath,
            IncludeDetailedLogs = detailedLogs,
            ContinuousInterval = TimeSpan.FromMinutes(interval),
            ScheduleInterval = TimeSpan.FromMinutes(interval * 4) // 4x interval for scheduled
        };
    }

    /// <summary>
    /// Run Multi-BIB service with configuration
    /// </summary>
    static async Task RunMultiBibService(MultiBibServiceConfiguration config)
    {
        Console.WriteLine("🎬 Starting Multi-BIB Production Service...");
        Console.WriteLine($"📋 Configuration Summary:");
        Console.WriteLine($"   📄 XML Config: {Path.GetFileName(config.DefaultConfigurationPath ?? "default")}");
        Console.WriteLine($"   🎯 Execution Mode: {config.ExecutionMode}");
        Console.WriteLine($"   📋 Target BIBs: {(config.TargetBibIds?.Any() == true ? string.Join(", ", config.TargetBibIds) : "ALL CONFIGURED")}");
        Console.WriteLine($"   📊 Detailed Logs: {(config.IncludeDetailedLogs ? "ENABLED" : "DISABLED")}");
        
        if (config.ExecutionMode == MultiBibExecutionMode.Continuous)
        {
            Console.WriteLine($"   ⏱️ Continuous Interval: {config.ContinuousInterval.TotalMinutes:F0} minutes");
        }
        
        Console.WriteLine();

        var builder = Host.CreateApplicationBuilder();
        
        // Configure services
        ConfigureMultiBibServices(builder.Services, config);
        
        // Register Multi-BIB service
        builder.Services.AddSingleton(config);
        builder.Services.AddHostedService<MultiBibWorkflowService>();
        
        var host = builder.Build();
        
        Console.WriteLine("✅ Multi-BIB Production Service configured and starting...");
        
        // Graceful shutdown handling
        Console.CancelKeyPress += (sender, e) =>
        {
            e.Cancel = true;
            Console.WriteLine("\n🛑 Graceful shutdown requested...");
            host.StopAsync().Wait(TimeSpan.FromSeconds(30));
        };
        
        await host.RunAsync();
    }

    /// <summary>
    /// Configure all services for Multi-BIB production service
    /// </summary>
    static void ConfigureMultiBibServices(IServiceCollection services, MultiBibServiceConfiguration config)
    {
        Console.WriteLine("⚙️ Configuring Multi-BIB Production Services...");
        
        try
        {
            // Logging configuration
            services.AddLogging(builder =>
            {
                builder.ClearProviders()
                       .AddNLog()
                       .AddConsole()
                       .SetMinimumLevel(config.IncludeDetailedLogs 
                           ? Microsoft.Extensions.Logging.LogLevel.Information 
                           : Microsoft.Extensions.Logging.LogLevel.Warning);
            });

            // Validation configuration (development mode for broader hardware support)
            var validationConfig = PortValidationConfiguration.CreateDevelopmentDefault();
            services.AddSingleton(validationConfig);

            // ✅ SPRINT 10: Core Services (Proven Foundation)
            services.AddScoped<IFtdiDeviceReader, FtdiDeviceReader>();
            services.AddScoped<ISerialPortValidator, SerialPortValidator>();
            services.AddScoped<ISerialPortDiscovery, EnhancedSerialPortDiscoveryService>();
            services.AddScoped<SystemInfoCache>();
            services.AddScoped<ISystemInfoCache>(provider => provider.GetRequiredService<SystemInfoCache>());
            services.AddScoped<ISerialPortPool, SerialPortPool.Core.Services.SerialPortPool>();

            // ✅ SPRINT 10: POC Extension Layer (Reservation System)
            services.AddScoped<IPortReservationService, PortReservationService>();

            // ✅ SPRINT 10: Configuration & Protocol Services
            services.AddMemoryCache();
            services.AddScoped<IBibConfigurationLoader, XmlBibConfigurationLoader>();
            services.AddScoped<IProtocolHandlerFactory, ProtocolHandlerFactory>();
            services.AddScoped<RS232ProtocolHandler>();

            // ✅ SPRINT 10: EEPROM & Dynamic Mapping (Auto-discovery)
            services.AddScoped<IFtdiEepromReader, FtdiEepromReader>();
            services.AddScoped<IDynamicBibMappingService, DynamicBibMappingService>();
            services.AddScoped<IDynamicPortMappingService, DynamicPortMappingService>();

            // ✅ SPRINT 10: Multi-BIB Orchestrator (The Star!)
            services.AddScoped<IBibWorkflowOrchestrator, BibWorkflowOrchestrator>();

            Console.WriteLine("✅ Multi-BIB Production Services configured successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ ERROR configuring Multi-BIB services: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Create service demo configuration (legacy support)
    /// </summary>
    static MultiBibServiceConfiguration CreateServiceDemoConfiguration()
    {
        Console.WriteLine("🔧 Windows Service Demo Mode (Legacy)");
        return new MultiBibServiceConfiguration
        {
            ExecutionMode = MultiBibExecutionMode.SingleRun,
            TargetBibIds = new List<string> { "client_demo" },
            DefaultConfigurationPath = ResolveConfigPath("demo.xml"),
            IncludeDetailedLogs = true
        };
    }

    /// <summary>
    /// Resolve configuration file path
    /// </summary>
    static string ResolveConfigPath(string xmlFileName)
    {
        if (Path.IsPathRooted(xmlFileName))
        {
            return xmlFileName;
        }
        
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        var configDir = Path.Combine(baseDir, "Configuration");
        
        if (!Path.HasExtension(xmlFileName))
        {
            xmlFileName += ".xml";
        }
        
        var fullPath = Path.Combine(configDir, xmlFileName);
        
        // Create configuration directory if it doesn't exist
        Directory.CreateDirectory(configDir);
        
        // Create default configuration if file doesn't exist
        if (!File.Exists(fullPath))
        {
            CreateDefaultMultiBibConfiguration(fullPath);
        }
        
        return fullPath;
    }

    /// <summary>
    /// Create default Multi-BIB configuration
    /// </summary>
    static void CreateDefaultMultiBibConfiguration(string configPath)
    {
        Console.WriteLine($"📄 Creating default Multi-BIB configuration: {Path.GetFileName(configPath)}");
        
        var multiBibXml = $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<root>
  <!-- ✅ SPRINT 10: Multi-BIB Configuration Example -->
  
  <bib id=""client_demo_A"" description=""Client Demo BIB A - Multi-BIB Example"">
    <metadata>
      <board_type>production</board_type>
      <multi_bib_group>client_demo</multi_bib_group>
      <created_date>{DateTime.Now:yyyy-MM-dd}</created_date>
    </metadata>
    
    <uut id=""production_uut_A"" description=""Production UUT A"">
      <port number=""1"">
        <protocol>rs232</protocol>
        <speed>115200</speed>
        <data_pattern>n81</data_pattern>
        
        <start>
          <command>INIT_A</command>
          <expected_response>READY_A</expected_response>
          <timeout_ms>3000</timeout_ms>
        </start>
        
        <test>
          <command>TEST_A</command>
          <expected_response>PASS_A</expected_response>
          <timeout_ms>5000</timeout_ms>
        </test>
        
        <stop>
          <command>QUIT_A</command>
          <expected_response>BYE_A</expected_response>
          <timeout_ms>2000</timeout_ms>
        </stop>
      </port>
    </uut>
  </bib>

  <bib id=""client_demo_B"" description=""Client Demo BIB B - Multi-BIB Example"">
    <metadata>
      <board_type>production</board_type>
      <multi_bib_group>client_demo</multi_bib_group>
      <created_date>{DateTime.Now:yyyy-MM-dd}</created_date>
    </metadata>
    
    <uut id=""production_uut_B"" description=""Production UUT B"">
      <port number=""1"">
        <protocol>rs232</protocol>
        <speed>115200</speed>
        <data_pattern>n81</data_pattern>
        
        <start>
          <command>INIT_B</command>
          <expected_response>READY_B</expected_response>
          <timeout_ms>3000</timeout_ms>
        </start>
        
        <test>
          <command>TEST_B</command>
          <expected_response>PASS_B</expected_response>
          <timeout_ms>5000</timeout_ms>
        </test>
        
        <stop>
          <command>QUIT_B</command>
          <expected_response>BYE_B</expected_response>
          <timeout_ms>2000</timeout_ms>
        </stop>
      </port>
    </uut>
  </bib>

  <!-- Additional BIBs can be added here for Multi-BIB scenarios -->
  
</root>";

        try
        {
            File.WriteAllText(configPath, multiBibXml);
            Console.WriteLine($"✅ Multi-BIB configuration created: {configPath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ Could not create Multi-BIB config: {ex.Message}");
        }
    }
}