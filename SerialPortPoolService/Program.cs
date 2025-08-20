// ===================================================================
// SPRINT 11 INTEGRATION: Enhanced Program.cs with Multi-File Discovery
// File: SerialPortPoolService/Program.cs
// Purpose: Integrate Sprint 11 multi-file capability + test compatibility
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
using System.CommandLine.Invocation;

namespace SerialPortPoolService;

// ✅ COMPATIBILITY: Keep legacy ClientDemoConfiguration for Worker.cs
public record ClientDemoConfiguration
{
    public string XmlConfigFile { get; init; } = "client-demo.xml";
    public bool LoopMode { get; init; } = false;
    public int LoopIntervalSeconds { get; init; } = 30;
    public bool ServiceDemo { get; init; } = false;
    public bool ConsoleMode { get; init; } = false;
    public int? MaxCycles { get; init; } = null;
}

class Program
{
    static async Task<int> Main(string[] args)
    {
        Console.WriteLine("🚀 SerialPortPoolService - Multi-BIB Production Service");
        Console.WriteLine("🎛️ SPRINT 11 INTEGRATION: Multi-File Configuration Discovery");
        Console.WriteLine("=".PadRight(80, '='));
        Console.WriteLine();

        // ✅ SPRINT 11: Enhanced Command Line Interface with multi-file options
        var rootCommand = CreateEnhancedMultiBibCommandLine();
        return await rootCommand.InvokeAsync(args);
    }

    /// <summary>
    /// SPRINT 11: Enhanced command line interface with multi-file discovery (InvocationContext)
    /// </summary>
    static RootCommand CreateEnhancedMultiBibCommandLine()
    {
        // Configuration options
        var xmlConfigOption = new Option<string>(
            "--xml-config", 
            getDefaultValue: () => "client-demo.xml",
            description: "Path to XML configuration file (legacy single-file mode)");

        var configDirOption = new Option<string>(
            "--config-dir",
            getDefaultValue: () => "Configuration/",
            description: "SPRINT 11: Directory for individual BIB files (bib_*.xml)");

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

        // SPRINT 11: Multi-file discovery options
        var discoverBibsOption = new Option<bool>(
            "--discover-bibs",
            getDefaultValue: () => false,
            description: "SPRINT 11: Auto-discover BIB files in config directory");

        var enableMultiFileOption = new Option<bool>(
            "--enable-multi-file",
            getDefaultValue: () => true,
            description: "SPRINT 11: Enable multi-file configuration discovery");

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

        var rootCommand = new RootCommand("SerialPortPool Multi-BIB Production Service - SPRINT 11")
        {
            xmlConfigOption,
            configDirOption,        // 🆕 SPRINT 11
            bibIdsOption,
            allBibsOption,
            discoverBibsOption,     // 🆕 SPRINT 11
            enableMultiFileOption,  // 🆕 SPRINT 11
            modeOption,
            intervalOption,
            detailedLogsOption,
            legacyLoopOption,
            serviceDemoOption
        };

        // 🎯 OPTION 1: InvocationContext - PAS DE LIMITE DE PARAMÈTRES!
        rootCommand.SetHandler(async (InvocationContext context) =>
        {
            try
            {
                // ✅ Récupérer TOUS les paramètres via ParseResult
                var parseResult = context.ParseResult;
                
                var xmlConfig = parseResult.GetValueForOption(xmlConfigOption)!;
                var configDir = parseResult.GetValueForOption(configDirOption)!;
                var bibIds = parseResult.GetValueForOption(bibIdsOption) ?? Array.Empty<string>();
                var allBibs = parseResult.GetValueForOption(allBibsOption);
                var discoverBibs = parseResult.GetValueForOption(discoverBibsOption);
                var enableMultiFile = parseResult.GetValueForOption(enableMultiFileOption);
                var mode = parseResult.GetValueForOption(modeOption)!;
                var interval = parseResult.GetValueForOption(intervalOption);
                var detailedLogs = parseResult.GetValueForOption(detailedLogsOption);
                var legacyLoop = parseResult.GetValueForOption(legacyLoopOption);
                var serviceDemo = parseResult.GetValueForOption(serviceDemoOption);

                // ✅ SPRINT 11: Configuration complète avec TOUTES les options
                var config = CreateEnhancedMultiBibConfiguration(
                    xmlConfig, configDir, bibIds, allBibs, discoverBibs, enableMultiFile, 
                    mode, interval, detailedLogs, legacyLoop, serviceDemo);

                await RunEnhancedMultiBibService(config);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Fatal error: {ex.Message}");
                Console.WriteLine($"📋 Details: {ex.StackTrace}");
                Environment.Exit(1);
            }
        });

        return rootCommand;
    }

    /// <summary>
    /// SPRINT 11: Create enhanced configuration with multi-file support
    /// </summary>
    static MultiBibServiceConfiguration CreateEnhancedMultiBibConfiguration(
        string xmlConfig, string configDir, string[] bibIds, bool allBibs, bool discoverBibs, 
        bool enableMultiFile, string mode, int interval, bool detailedLogs, bool legacyLoop, bool serviceDemo)
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

        // 🆕 SPRINT 11: Enhanced configuration path resolution
        var configPath = ResolveConfigPath(xmlConfig, configDir);

        // Create enhanced configuration
        var config = new MultiBibServiceConfiguration
        {
            ExecutionMode = executionMode,
            TargetBibIds = bibIds?.Any() == true ? bibIds.ToList() : null,
            DefaultConfigurationPath = configPath,
            IncludeDetailedLogs = detailedLogs,
            ContinuousInterval = TimeSpan.FromMinutes(interval),
            ScheduleInterval = TimeSpan.FromMinutes(interval * 4)
        };

        // 🆕 SPRINT 11: Add multi-file configuration
        config.Metadata = new Dictionary<string, object>
        {
            ["ConfigurationDirectory"] = configDir,
            ["EnableMultiFileDiscovery"] = enableMultiFile,
            ["DiscoverBibs"] = discoverBibs,
            ["Sprint11Enhanced"] = true
        };

        return config;
    }

    /// <summary>
    /// SPRINT 11: Run enhanced Multi-BIB service with discovery
    /// </summary>
    static async Task RunEnhancedMultiBibService(MultiBibServiceConfiguration config)
    {
        Console.WriteLine("🎬 Starting SPRINT 11 Enhanced Multi-BIB Service...");
        Console.WriteLine($"📋 Configuration Summary:");
        Console.WriteLine($"   📄 XML Config: {Path.GetFileName(config.DefaultConfigurationPath ?? "default")}");
        Console.WriteLine($"   📁 Config Directory: {config.Metadata?["ConfigurationDirectory"]}");
        Console.WriteLine($"   🔍 Multi-File Discovery: {config.Metadata?["EnableMultiFileDiscovery"]}");
        Console.WriteLine($"   🎯 Execution Mode: {config.ExecutionMode}");
        Console.WriteLine($"   📋 Target BIBs: {(config.TargetBibIds?.Any() == true ? string.Join(", ", config.TargetBibIds) : "AUTO-DISCOVER")}");
        Console.WriteLine($"   📊 Detailed Logs: {(config.IncludeDetailedLogs ? "ENABLED" : "DISABLED")}");
        
        if (config.ExecutionMode == MultiBibExecutionMode.Continuous)
        {
            Console.WriteLine($"   ⏱️ Continuous Interval: {config.ContinuousInterval.TotalMinutes:F0} minutes");
        }
        
        Console.WriteLine();

        // ✅ SPRINT 11: Enhanced service discovery phase
        await PerformSprint11Discovery(config);

        var builder = Host.CreateApplicationBuilder();
        
        // Configure services with Sprint 11 enhancements
        ConfigureEnhancedMultiBibServices(builder.Services, config);
        
        // Register Multi-BIB service
        builder.Services.AddSingleton(config);
        builder.Services.AddHostedService<MultiBibWorkflowService>();
        
        var host = builder.Build();
        
        Console.WriteLine("✅ SPRINT 11 Enhanced Multi-BIB Service configured and starting...");
        
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
    /// SPRINT 11: Perform enhanced BIB discovery
    /// </summary>
    static async Task PerformSprint11Discovery(MultiBibServiceConfiguration config)
    {
        try
        {
            var enableDiscovery = config.Metadata?.GetValueOrDefault("EnableMultiFileDiscovery", true) as bool? ?? true;
            var performDiscovery = config.Metadata?.GetValueOrDefault("DiscoverBibs", false) as bool? ?? false;
            
            if (!enableDiscovery && !performDiscovery)
            {
                Console.WriteLine("📄 Using legacy single-file configuration mode");
                return;
            }

            Console.WriteLine("🔍 SPRINT 11: Performing BIB Discovery...");
            Console.WriteLine("=".PadRight(60, '='));

            var configDir = config.Metadata?.GetValueOrDefault("ConfigurationDirectory", "Configuration/") as string ?? "Configuration/";
            
            // Ensure configuration directory exists
            if (!Directory.Exists(configDir))
            {
                Console.WriteLine($"📁 Creating configuration directory: {configDir}");
                Directory.CreateDirectory(configDir);
                
                // Create sample individual BIB files for testing
                await CreateSampleIndividualBibFiles(configDir);
            }

            // Discovery phase
            if (performDiscovery)
            {
                Console.WriteLine($"🔍 Scanning for individual BIB files in: {configDir}");
                
                var bibFiles = Directory.GetFiles(configDir, "bib_*.xml");
                
                Console.WriteLine($"📄 Found {bibFiles.Length} individual BIB files:");
                foreach (var file in bibFiles)
                {
                    var fileName = Path.GetFileName(file);
                    var bibId = ExtractBibIdFromFileName(fileName);
                    Console.WriteLine($"   ✅ {fileName} → BIB_ID: {bibId}");
                }
                
                if (bibFiles.Length == 0)
                {
                    Console.WriteLine("⚠️ No individual BIB files found - will use legacy mode");
                }
            }

            Console.WriteLine("=".PadRight(60, '='));
            Console.WriteLine();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ Discovery error (continuing with legacy mode): {ex.Message}");
        }
    }

    /// <summary>
    /// SPRINT 11: Configure enhanced services with multi-file support
    /// </summary>
    static void ConfigureEnhancedMultiBibServices(IServiceCollection services, MultiBibServiceConfiguration config)
    {
        Console.WriteLine("⚙️ Configuring SPRINT 11 Enhanced Multi-BIB Services...");
        
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

            // ✅ SPRINT 11: Enhanced Configuration Loader (with multi-file support)
            services.AddMemoryCache();
            services.AddScoped<IBibConfigurationLoader, XmlBibConfigurationLoader>(); // 🆕 ENHANCED

            // ✅ Core Services (Sprint 10 foundation)
            services.AddScoped<IFtdiDeviceReader, FtdiDeviceReader>();
            services.AddScoped<ISerialPortValidator, SerialPortValidator>();
            services.AddScoped<ISerialPortDiscovery, EnhancedSerialPortDiscoveryService>();
            services.AddScoped<SystemInfoCache>();
            services.AddScoped<ISystemInfoCache>(provider => provider.GetRequiredService<SystemInfoCache>());
            services.AddScoped<ISerialPortPool, SerialPortPool.Core.Services.SerialPortPool>();

            // ✅ POC Extension Layer (Reservation System)
            services.AddScoped<IPortReservationService, PortReservationService>();

            // ✅ Protocol Services
            services.AddScoped<IProtocolHandlerFactory, ProtocolHandlerFactory>();
            services.AddScoped<RS232ProtocolHandler>();

            // ✅ EEPROM & Dynamic Mapping (Auto-discovery)
            services.AddScoped<IFtdiEepromReader, FtdiEepromReader>();
            services.AddScoped<IDynamicBibMappingService, DynamicBibMappingService>();
            services.AddScoped<IDynamicPortMappingService, DynamicPortMappingService>();

            // ✅ Multi-BIB Orchestrator
            services.AddScoped<IBibWorkflowOrchestrator, BibWorkflowOrchestrator>();

            Console.WriteLine("✅ SPRINT 11 Enhanced Services configured successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ ERROR configuring SPRINT 11 services: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// SPRINT 11: Create sample individual BIB files for testing
    /// </summary>
    static async Task CreateSampleIndividualBibFiles(string configDir)
    {
        try
        {
            Console.WriteLine("📝 Creating sample individual BIB files for SPRINT 11 testing...");

            // Sample BIB 1: client_demo
            var clientDemoBib = $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<bib id=""client_demo"" description=""SPRINT 11: Individual BIB File - Client Demo"">
  <metadata>
    <board_type>demo</board_type>
    <sprint>11</sprint>
    <file_type>individual</file_type>
    <created_date>{DateTime.Now:yyyy-MM-dd}</created_date>
  </metadata>
  
  <uut id=""production_uut"" description=""Production UUT for Individual File Testing"">
    <port number=""1"">
      <protocol>rs232</protocol>
      <speed>115200</speed>
      <data_pattern>n81</data_pattern>
      
      <start>
        <command>INIT</command>
        <expected_response>READY</expected_response>
        <timeout_ms>3000</timeout_ms>
      </start>
      
      <test>
        <command>TEST</command>
        <expected_response>PASS</expected_response>
        <timeout_ms>5000</timeout_ms>
      </test>
      
      <stop>
        <command>QUIT</command>
        <expected_response>BYE</expected_response>
        <timeout_ms>2000</timeout_ms>
      </stop>
    </port>
  </uut>
</bib>";

            // Sample BIB 2: production_line_1
            var productionLine1Bib = $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<bib id=""production_line_1"" description=""SPRINT 11: Production Line 1 - Isolated Configuration"">
  <metadata>
    <board_type>production</board_type>
    <sprint>11</sprint>
    <file_type>individual</file_type>
    <line_number>1</line_number>
    <created_date>{DateTime.Now:yyyy-MM-dd}</created_date>
  </metadata>
  
  <uut id=""line1_uut"" description=""Production Line 1 UUT"">
    <port number=""1"">
      <protocol>rs232</protocol>
      <speed>115200</speed>
      <data_pattern>n81</data_pattern>
      
      <start>
        <command>INIT_LINE1</command>
        <expected_response>READY_LINE1</expected_response>
        <timeout_ms>3000</timeout_ms>
      </start>
      
      <test>
        <command>TEST_LINE1</command>
        <expected_response>PASS_LINE1</expected_response>
        <timeout_ms>5000</timeout_ms>
      </test>
      
      <stop>
        <command>QUIT_LINE1</command>
        <expected_response>BYE_LINE1</expected_response>
        <timeout_ms>2000</timeout_ms>
      </stop>
    </port>
  </uut>
</bib>";

            // Write individual BIB files
            var clientDemoPath = Path.Combine(configDir, "bib_client_demo.xml");
            var productionLine1Path = Path.Combine(configDir, "bib_production_line_1.xml");

            await File.WriteAllTextAsync(clientDemoPath, clientDemoBib);
            await File.WriteAllTextAsync(productionLine1Path, productionLine1Bib);

            Console.WriteLine($"✅ Created: {Path.GetFileName(clientDemoPath)}");
            Console.WriteLine($"✅ Created: {Path.GetFileName(productionLine1Path)}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ Could not create sample BIB files: {ex.Message}");
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
            DefaultConfigurationPath = ResolveConfigPath("demo.xml", "Configuration/"),
            IncludeDetailedLogs = true
        };
    }

    /// <summary>
    /// SPRINT 11: Enhanced configuration path resolution
    /// </summary>
    static string ResolveConfigPath(string xmlFileName, string configDir = "Configuration/")
    {
        if (Path.IsPathRooted(xmlFileName))
        {
            return xmlFileName;
        }
        
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        var fullConfigDir = Path.Combine(baseDir, configDir);
        
        if (!Path.HasExtension(xmlFileName))
        {
            xmlFileName += ".xml";
        }
        
        var fullPath = Path.Combine(fullConfigDir, xmlFileName);
        
        // Create configuration directory if it doesn't exist
        Directory.CreateDirectory(fullConfigDir);
        
        // Create default configuration if file doesn't exist
        if (!File.Exists(fullPath))
        {
            CreateDefaultMultiBibConfiguration(fullPath);
        }
        
        return fullPath;
    }

    /// <summary>
    /// Extract BIB ID from filename (bib_xyz.xml → xyz)
    /// </summary>
    static string ExtractBibIdFromFileName(string fileName)
    {
        if (fileName.StartsWith("bib_") && fileName.EndsWith(".xml"))
        {
            return fileName.Substring(4, fileName.Length - 8);
        }
        return fileName;
    }

    /// <summary>
    /// Create default Multi-BIB configuration (legacy compatibility)
    /// </summary>
    static void CreateDefaultMultiBibConfiguration(string configPath)
    {
        Console.WriteLine($"📄 Creating default Multi-BIB configuration: {Path.GetFileName(configPath)}");
        
        var multiBibXml = $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<root>
  <!-- ✅ SPRINT 11: Legacy Multi-BIB Configuration Example -->
  
  <bib id=""client_demo"" description=""Legacy Client Demo BIB - Multi-BIB Example"">
    <metadata>
      <board_type>demo</board_type>
      <multi_bib_group>client_demo</multi_bib_group>
      <sprint>11</sprint>
      <config_type>legacy</config_type>
      <created_date>{DateTime.Now:yyyy-MM-dd}</created_date>
    </metadata>
    
    <uut id=""production_uut"" description=""Legacy Production UUT"">
      <port number=""1"">
        <protocol>rs232</protocol>
        <speed>115200</speed>
        <data_pattern>n81</data_pattern>
        
        <start>
          <command>INIT</command>
          <expected_response>READY</expected_response>
          <timeout_ms>3000</timeout_ms>
        </start>
        
        <test>
          <command>TEST</command>
          <expected_response>PASS</expected_response>
          <timeout_ms>5000</timeout_ms>
        </test>
        
        <stop>
          <command>QUIT</command>
          <expected_response>BYE</expected_response>
          <timeout_ms>2000</timeout_ms>
        </stop>
      </port>
    </uut>
  </bib>

  <!-- Additional BIBs can be added here for backward compatibility -->
  
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