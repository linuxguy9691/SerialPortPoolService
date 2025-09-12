// ===================================================================
// SPRINT 13 INTEGRATION: Enhanced Program.cs with Sprint 13 Services
// File: SerialPortPoolService/Program.cs
// CHANGEMENT MINIMAL: Seulement ConfigureEnhancedMultiBibServices modifiée
// ===================================================================

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using SerialPortPool.Core.Interfaces;
using SerialPortPool.Core.Models;
using SerialPortPool.Core.Services;
using SerialPortPool.Core.Extensions;  // ← AJOUTÉ pour Sprint13ServiceExtensions
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
        Console.WriteLine("🎛️ SPRINT 13 INTEGRATION: Hot-Add Multi-BIB System");
        Console.WriteLine("=".PadRight(80, '='));
        Console.WriteLine();

        // ✅ SPRINT 13: Enhanced Command Line Interface with multi-file options
        var rootCommand = CreateEnhancedMultiBibCommandLine();
        return await rootCommand.InvokeAsync(args);
    }

    /// <summary>
    /// SPRINT 13: Enhanced command line interface with multi-file discovery (InvocationContext)
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
            description: "SPRINT 13: Directory for individual BIB files (bib_*.xml)");

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

        // SPRINT 13: Multi-file discovery options
        var discoverBibsOption = new Option<bool>(
            "--discover-bibs",
            getDefaultValue: () => false,
            description: "SPRINT 13: Auto-discover BIB files in config directory");

        var enableMultiFileOption = new Option<bool>(
            "--enable-multi-file",
            getDefaultValue: () => true,
            description: "SPRINT 13: Enable multi-file configuration discovery");

        // Execution mode options
        var modeOption = new Option<string>(
            "--mode",
            getDefaultValue: () => "production",  // ← NEW (CHANGE 1/2)
            description: "Execution mode: production, single, continuous, scheduled, on-demand");

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

        var rootCommand = new RootCommand("SerialPortPool Multi-BIB Production Service - SPRINT 13")
        {
            xmlConfigOption,
            configDirOption,        // 🆕 SPRINT 13
            bibIdsOption,
            allBibsOption,
            discoverBibsOption,     // 🆕 SPRINT 13
            enableMultiFileOption,  // 🆕 SPRINT 13
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

                // ✅ SPRINT 13: Configuration complète avec TOUTES les options
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
    /// SPRINT 13: Create enhanced configuration with multi-file support
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
            "production" => MultiBibExecutionMode.Production,  // ← NEW (CHANGE 2/2)
            "single" => MultiBibExecutionMode.SingleRun,
            "continuous" => MultiBibExecutionMode.Continuous,
            "scheduled" => MultiBibExecutionMode.Scheduled,
            "on-demand" => MultiBibExecutionMode.OnDemand,
            _ => MultiBibExecutionMode.Production  // ← NEW default fallback
        };

        // 🆕 SPRINT 13: Enhanced configuration path resolution
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

        // 🆕 SPRINT 13: Add multi-file configuration
        config.Metadata = new Dictionary<string, object>
        {
            ["ConfigurationDirectory"] = configDir,
            ["EnableMultiFileDiscovery"] = enableMultiFile,
            ["DiscoverBibs"] = discoverBibs,
            ["Sprint13Enhanced"] = true
        };

        return config;
    }

    /// <summary>
    /// SPRINT 13: Run enhanced Multi-BIB service with discovery
    /// </summary>
    static async Task RunEnhancedMultiBibService(MultiBibServiceConfiguration config)
    {
        Console.WriteLine("🎬 Starting SPRINT 13 Enhanced Multi-BIB Service...");
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

        // ✅ SPRINT 13: Enhanced service discovery phase
        await PerformSprint13Discovery(config);

        var builder = Host.CreateApplicationBuilder();
        
        // 🎯 SPRINT 13: Configure services using new Sprint13ServiceExtensions
        ConfigureEnhancedMultiBibServices(builder.Services, config);
        
        // Register Multi-BIB service
        builder.Services.AddSingleton(config);
        builder.Services.AddHostedService<MultiBibWorkflowService>();
        
        var host = builder.Build();
        
        Console.WriteLine("✅ SPRINT 13 Enhanced Multi-BIB Service configured and starting...");
        
        // 🎯 SPRINT 13: Validate services
        try
        {
            using var scope = host.Services.CreateScope();
            scope.ServiceProvider.ValidateSprint13Services();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ Service validation warning: {ex.Message}");
        }
        
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
    /// SPRINT 14: PRODUCTION-READY discovery - no automatic file creation
    /// </summary>
    static async Task PerformSprint13Discovery(MultiBibServiceConfiguration config)
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

            Console.WriteLine("🔍 SPRINT 14: Production BIB Discovery...");
            Console.WriteLine("=".PadRight(60, '='));

            var configDir = config.Metadata?.GetValueOrDefault("ConfigurationDirectory", "Configuration/") as string ?? "Configuration/";
            
            // Ensure configuration directory exists (but don't create sample files)
            if (!Directory.Exists(configDir))
            {
                Console.WriteLine($"📁 Creating configuration directory: {configDir}");
                Directory.CreateDirectory(configDir);
                
                // PRODUCTION CHANGE: Do NOT create sample files automatically
                Console.WriteLine($"⚠️ PRODUCTION MODE: No BIB files found in {configDir}");
                Console.WriteLine($"📋 Expected file pattern: bib_*.xml");
                Console.WriteLine($"🔍 Hot-add system will monitor for real BIB files...");
            }
            else
            {
                // Discovery phase for existing files
                if (performDiscovery)
                {
                    Console.WriteLine($"🔍 Scanning for BIB files in: {configDir}");
                    
                    var bibFiles = Directory.GetFiles(configDir, "bib_*.xml");
                    
                    Console.WriteLine($"📄 Found {bibFiles.Length} BIB files:");
                    foreach (var file in bibFiles)
                    {
                        var fileName = Path.GetFileName(file);
                        var bibId = ExtractBibIdFromFileName(fileName);
                        var fileSize = new FileInfo(file).Length;
                        Console.WriteLine($"   ✅ {fileName} → BIB_ID: {bibId} ({fileSize} bytes)");
                    }
                    
                    if (bibFiles.Length == 0)
                    {
                        Console.WriteLine("📋 No BIB files found - system will wait for hot-add files");
                        Console.WriteLine("🔍 Place real BIB files in format: bib_[BIB_ID].xml");
                    }
                }
            }

            Console.WriteLine("=".PadRight(60, '='));
            Console.WriteLine();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ Discovery error: {ex.Message}");
        }
    }
    
    /// <summary>
    /// SPRINT 13: Configure services using Sprint13ServiceExtensions (SIMPLIFIED)
    /// CHANGEMENT MINIMAL: Remplace toute la logique complexe par une ligne
    /// </summary>
    static void ConfigureEnhancedMultiBibServices(IServiceCollection services, MultiBibServiceConfiguration config)
    {
        Console.WriteLine("⚙️ Configuring SPRINT 13 Enhanced Multi-BIB Services...");

        try
        {
            // 🎯 SPRINT 13: Une seule ligne remplace tout le code précédent!
            services.AddSprint13DemoServices();

            // Configure the BIB configuration loader with the default path
            if (!string.IsNullOrEmpty(config.DefaultConfigurationPath))
            {
                services.AddSingleton<IBibConfigurationLoader>(provider =>
                {
                    var loader = provider.GetRequiredService<XmlBibConfigurationLoader>();
                    loader.SetDefaultConfigurationPath(config.DefaultConfigurationPath);
                    return loader;
                });
            }

            Console.WriteLine("✅ SPRINT 13 Enhanced Services configured successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ ERROR configuring SPRINT 13 services: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// PRODUCTION CHANGE: Remove automatic sample file creation
    /// This method is now DISABLED in production builds
    /// </summary>
    static async Task CreateSampleIndividualBibFiles(string configDir)
    {
        // PRODUCTION CHANGE: Completely disable sample file creation
        Console.WriteLine($"🏭 PRODUCTION MODE: Sample file creation disabled");
        Console.WriteLine($"📋 Production systems must provide real BIB configuration files");
        Console.WriteLine($"📁 Place your BIB files in: {configDir}");
        Console.WriteLine($"📝 File format: bib_[BIB_ID].xml (e.g., bib_line1.xml, bib_line2.xml)");
        
        await Task.CompletedTask; // Keep method signature for compatibility
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
    /// SPRINT 14: PRODUCTION-READY configuration path resolution
    /// CHANGE: No automatic file creation in production mode
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
        
        // PRODUCTION CHANGE: Do NOT create default files automatically
        // Production systems must provide their own configuration files
        if (!File.Exists(fullPath))
        {
            Console.WriteLine($"📋 PRODUCTION MODE: Configuration file not found: {Path.GetFileName(fullPath)}");
            Console.WriteLine($"📁 Expected location: {fullPath}");
            Console.WriteLine($"⚠️ Production systems must provide real BIB configuration files");
            Console.WriteLine($"🔍 System will wait for hot-add file discovery...");
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
    /// PRODUCTION CHANGE: Disable automatic default configuration creation
    /// </summary>
    static void CreateDefaultMultiBibConfiguration(string configPath)
    {
        // PRODUCTION CHANGE: Do NOT create default files
        Console.WriteLine($"🏭 PRODUCTION MODE: Automatic configuration creation disabled");
        Console.WriteLine($"📋 Missing configuration file: {Path.GetFileName(configPath)}");
        Console.WriteLine($"📁 Expected location: {configPath}");
        Console.WriteLine($"⚠️ Production systems require real configuration files");
        Console.WriteLine($"🔍 System will continue and wait for hot-add discovery...");
        
        // Do NOT create any files automatically in production
    }}