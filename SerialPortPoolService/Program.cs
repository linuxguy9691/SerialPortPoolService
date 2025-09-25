// ===================================================================
// SPRINT 13 INTEGRATION: Enhanced Program.cs with Sprint 13 Services
// File: SerialPortPoolService/Program.cs
// FIX: Mode production pur sans configuration legacy
// ===================================================================

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using SerialPortPool.Core.Interfaces;
using SerialPortPool.Core.Models;
using SerialPortPool.Core.Services;
using SerialPortPool.Core.Extensions;
using SerialPortPoolService.Services;
using SerialPortPool.Core.Protocols;
using System.CommandLine;
using System.CommandLine.Invocation;
using SerialPortPool.Core.Services.Configuration; // ← Pour HotReloadConfigurationService

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
            getDefaultValue: () => "production",
            description: "Execution mode: production, single, continuous, scheduled, on-demand, legacy");

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
            configDirOption,
            bibIdsOption,
            allBibsOption,
            discoverBibsOption,
            enableMultiFileOption,
            modeOption,
            intervalOption,
            detailedLogsOption,
            legacyLoopOption,
            serviceDemoOption
        };

        rootCommand.SetHandler(async (InvocationContext context) =>
        {
            try
            {
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

                // ✅ FIX: Configuration complète avec mode detection
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
    /// FIX: Mode production pur sans configuration legacy
    /// </summary>
    static MultiBibServiceConfiguration CreateEnhancedMultiBibConfiguration(
        string xmlConfig, string configDir, string[] bibIds, bool allBibs, bool discoverBibs, 
        bool enableMultiFile, string mode, int interval, bool detailedLogs, bool legacyLoop, bool serviceDemo)
    {
        // Handle legacy options first
        if (legacyLoop) mode = "continuous";
        if (serviceDemo) return CreateServiceDemoConfiguration();

        // Parse execution mode
        var executionMode = mode.ToLowerInvariant() switch
        {
            "production" => MultiBibExecutionMode.Production,
            "single" => MultiBibExecutionMode.SingleRun,
            "continuous" => MultiBibExecutionMode.Continuous,
            "scheduled" => MultiBibExecutionMode.Scheduled,
            "on-demand" => MultiBibExecutionMode.OnDemand,
            "legacy" => MultiBibExecutionMode.SingleRun, // Force legacy to single run
            _ => MultiBibExecutionMode.Production
        };

        // ✅ FIX: Mode-specific configuration path resolution
        string? configPath = null;
        
        // Only set DefaultConfigurationPath for legacy modes
        if (executionMode != MultiBibExecutionMode.Production || mode.ToLowerInvariant() == "legacy")
        {
            configPath = ResolveConfigPath(xmlConfig, configDir);
            Console.WriteLine($"📄 Legacy mode detected - using XML config: {Path.GetFileName(configPath)}");
        }
        else
        {
            Console.WriteLine($"🏭 Production mode detected - using BIB file discovery only");
        }

        // Create enhanced configuration
        var config = new MultiBibServiceConfiguration
        {
            ExecutionMode = executionMode,
            TargetBibIds = bibIds?.Any() == true ? bibIds.ToList() : null,
            DefaultConfigurationPath = configPath, // ✅ FIX: Null en mode production
            IncludeDetailedLogs = detailedLogs,
            ContinuousInterval = TimeSpan.FromMinutes(interval),
            ScheduleInterval = TimeSpan.FromMinutes(interval * 4)
        };

        // ✅ FIX: Enhanced metadata with mode detection
        config.Metadata = new Dictionary<string, object>
        {
            ["ConfigurationDirectory"] = configDir,
            ["EnableMultiFileDiscovery"] = enableMultiFile || executionMode == MultiBibExecutionMode.Production,
            ["DiscoverBibs"] = discoverBibs || executionMode == MultiBibExecutionMode.Production,
            ["Sprint13Enhanced"] = false,  // ← CHANGÉ: false car on utilise Sprint 11++
            ["Sprint11PlusPlus"] = true,   // ← AJOUTÉ: Nouvelle architecture
            ["PureProductionMode"] = executionMode == MultiBibExecutionMode.Production,
            ["UseLegacyXml"] = configPath != null
        };

        return config;
    }

    /// <summary>
    /// SPRINT 13: Run enhanced Multi-BIB service with discovery
    /// FIX: Detection du mode production pur
    /// </summary>
    static async Task RunEnhancedMultiBibService(MultiBibServiceConfiguration config)
    {
        var isPureProduction = config.ExecutionMode == MultiBibExecutionMode.Production && 
                              config.DefaultConfigurationPath == null;

        Console.WriteLine("🎬 Starting SPRINT 13 Enhanced Multi-BIB Service...");
        Console.WriteLine($"📋 Configuration Summary:");
        
        if (isPureProduction)
        {
            Console.WriteLine($"   🏭 Mode: PURE PRODUCTION (BIB Discovery Only)");
            Console.WriteLine($"   📁 Config Directory: {config.Metadata?["ConfigurationDirectory"]}");
            Console.WriteLine($"   🔍 Multi-File Discovery: ENABLED");
        }
        else
        {
            Console.WriteLine($"   📄 XML Config: {Path.GetFileName(config.DefaultConfigurationPath ?? "default")}");
            Console.WriteLine($"   📁 Config Directory: {config.Metadata?["ConfigurationDirectory"]}");
            Console.WriteLine($"   🔍 Multi-File Discovery: {config.Metadata?["EnableMultiFileDiscovery"]}");
        }
        
        Console.WriteLine($"   🎯 Execution Mode: {config.ExecutionMode}");
        Console.WriteLine($"   📋 Target BIBs: {(config.TargetBibIds?.Any() == true ? string.Join(", ", config.TargetBibIds) : "AUTO-DISCOVER")}");
        Console.WriteLine($"   📊 Detailed Logs: {(config.IncludeDetailedLogs ? "ENABLED" : "DISABLED")}");
        Console.WriteLine();

        // ✅ FIX: Enhanced service discovery phase
        await PerformSprint13Discovery(config);

        var builder = Host.CreateApplicationBuilder();
        
        ConfigureEnhancedMultiBibServices(builder.Services, config);
        
        // Register Multi-BIB service
        builder.Services.AddSingleton(config);
        builder.Services.AddHostedService<MultiBibWorkflowService>();
        
        var host = builder.Build();
        
        Console.WriteLine("✅ SPRINT 13 Enhanced Multi-BIB Service configured and starting...");
        
        // Validate services
        try
        {
            using var scope = host.Services.CreateScope();
            scope.ServiceProvider.ValidateSprint6Services();
            scope.ServiceProvider.ValidateSprint8Services();
            
            // Valider que HotReloadConfigurationService est enregistré
            var hotReloadService = scope.ServiceProvider.GetService<HotReloadConfigurationService>();
            if (hotReloadService != null)
            {
                Console.WriteLine("✅ HotReloadConfigurationService registered (Sprint 11++)");
            }
            else
            {
                Console.WriteLine("⚠️ HotReloadConfigurationService not found");
            }
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
    /// SPRINT 14: PRODUCTION-READY discovery
    /// FIX: Différenciation claire entre modes
    /// </summary>
    static async Task PerformSprint13Discovery(MultiBibServiceConfiguration config)
    {
        try
        {
            var enableDiscovery = config.Metadata?.GetValueOrDefault("EnableMultiFileDiscovery", true) as bool? ?? true;
            var performDiscovery = config.Metadata?.GetValueOrDefault("DiscoverBibs", false) as bool? ?? false;
            var isPureProduction = config.Metadata?.GetValueOrDefault("PureProductionMode", false) as bool? ?? false;
            
            if (isPureProduction)
            {
                Console.WriteLine("🏭 SPRINT 14: Production BIB Discovery...");
                Console.WriteLine("=".PadRight(60, '='));
                Console.WriteLine($"📋 Production systems must provide real BIB configuration files");
                Console.WriteLine($"🔍 System will wait for hot-add file discovery...");
            }
            else if (!enableDiscovery && !performDiscovery)
            {
                Console.WriteLine("📄 Using legacy single-file configuration mode");
                return;
            }

            var configDir = config.Metadata?.GetValueOrDefault("ConfigurationDirectory", "Configuration/") as string ?? "Configuration/";
            
            // Ensure configuration directory exists
            if (!Directory.Exists(configDir))
            {
                Console.WriteLine($"📁 Creating configuration directory: {configDir}");
                Directory.CreateDirectory(configDir);
                
                if (isPureProduction)
                {
                    Console.WriteLine($"⚠️ PRODUCTION MODE: Configuration file not found: client-demo.xml");
                    Console.WriteLine($"📁 Expected location: {Path.Combine(AppDomain.CurrentDomain.BaseDirectory, configDir, "client-demo.xml")}");
                    Console.WriteLine($"🔍 Production systems must provide real BIB configuration files");
                    Console.WriteLine($"🔍 System will wait for hot-add file discovery...");
                }
            }
            else
            {
                // Discovery phase for existing files
                if (performDiscovery || isPureProduction)
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
                    
                    if (bibFiles.Length == 0 && isPureProduction)
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
/// CRITICAL FIX: Configure enhanced services with robust logging validation
/// FAIL FAST: Service won't start if logging is broken
/// </summary>
static void ConfigureEnhancedMultiBibServices(IServiceCollection services, MultiBibServiceConfiguration config)
{
    Console.WriteLine("⚙️ Configuring SPRINT 11++ Enhanced Multi-BIB Services...");

    try
    {
        // STEP 1: Validate and configure logging FIRST (fail fast if broken)
        ValidateAndConfigureLogging(services, config);
        
        var isPureProduction = config.Metadata?.GetValueOrDefault("PureProductionMode", false) as bool? ?? false;

        if (isPureProduction)
        {
            Console.WriteLine($"🏭 Production mode: Using Sprint 11++ production services");
            
            // Sprint 11++ PRODUCTION SERVICES
            services.AddSprint6CoreServices();
            services.AddSprint8Services();
            
            // Services manquants pour MultiBibWorkflowService
            services.AddScoped<IBibWorkflowOrchestrator, BibWorkflowOrchestrator>();
            services.AddScoped<IPortReservationService, PortReservationService>();
            
            // Sprint 11: Hot Reload Configuration Service (Production Mode)
            services.AddSingleton<HotReloadConfigurationService>();
            services.AddSingleton<IHostedService>(provider => 
                provider.GetRequiredService<HotReloadConfigurationService>());
        }
        else
        {
            Console.WriteLine($"🔧 Demo mode: Using Sprint 11++ demo services");
            
            // Sprint 11++ DEMO SERVICES
            services.AddSprint6DemoServices(); 
            services.AddSprint8Services();
            
            // Services manquants pour MultiBibWorkflowService
            services.AddScoped<IBibWorkflowOrchestrator, BibWorkflowOrchestrator>();
            services.AddScoped<IPortReservationService, PortReservationService>();
            
            // Sprint 11: Hot Reload Configuration Service (Demo Mode)
            services.AddSingleton<HotReloadConfigurationService>();
            services.AddSingleton<IHostedService>(provider => 
                provider.GetRequiredService<HotReloadConfigurationService>());
        }

        Console.WriteLine("✅ SPRINT 11++ Enhanced Services configured successfully");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ FATAL ERROR configuring services: {ex.Message}");
        Console.WriteLine($"📋 Details: {ex}");
        Console.WriteLine("🛑 Service cannot start - fix configuration and retry");
        throw;
    }
}

/// <summary>
/// NOUVELLE FONCTION: Validate logging configuration and fail fast if broken
/// </summary>
static void ValidateAndConfigureLogging(IServiceCollection services, MultiBibServiceConfiguration config)
{
    Console.WriteLine("🔧 Validating logging configuration...");
    
    string nlogConfigPath = "";
    string logDirectory = "";
    List<string> errors = new List<string>();
    bool nlogOk = false;
    bool consoleOk = false;

    try
    {
        // STEP 1: Check NLog.config exists and is readable
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        nlogConfigPath = Path.Combine(baseDir, "NLog.config");
        
        Console.WriteLine($"📁 Checking NLog.config at: {nlogConfigPath}");
        
        if (!File.Exists(nlogConfigPath))
        {
            errors.Add($"NLog.config file not found at: {nlogConfigPath}");
        }
        else
        {
            // Try to read the file
            try
            {
                var content = File.ReadAllText(nlogConfigPath);
                if (string.IsNullOrWhiteSpace(content))
                {
                    errors.Add($"NLog.config file is empty: {nlogConfigPath}");
                }
                else
                {
                    Console.WriteLine("✅ NLog.config file found and readable");
                }
            }
            catch (Exception ex)
            {
                errors.Add($"Cannot read NLog.config: {ex.Message}");
            }
        }

        // STEP 2: Check log directory exists and is writable
        logDirectory = Path.Combine("C:", "Logs", "SerialPortPool");
        Console.WriteLine($"📁 Checking log directory: {logDirectory}");
        
        try
        {
            Directory.CreateDirectory(logDirectory);
            
            // Test write permissions
            var testFile = Path.Combine(logDirectory, $"startup_test_{DateTime.Now:yyyyMMdd_HHmmss}.tmp");
            File.WriteAllText(testFile, $"Startup test: {DateTime.Now}");
            File.Delete(testFile);
            
            Console.WriteLine("✅ Log directory writable");
        }
        catch (Exception ex)
        {
            errors.Add($"Cannot write to log directory {logDirectory}: {ex.Message}");
        }

        // STEP 3: Configure logging with fallback strategy
        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            
            // Try NLog first
            if (errors.Count == 0)
            {
                try
                {
                    builder.AddNLog();
                    nlogOk = true;
                    Console.WriteLine("✅ NLog provider configured");
                }
                catch (Exception ex)
                {
                    errors.Add($"NLog provider configuration failed: {ex.Message}");
                }
            }
            
            // Always add console as fallback
            try
            {
                builder.AddConsole();
                consoleOk = true;
                Console.WriteLine("✅ Console provider configured");
            }
            catch (Exception ex)
            {
                errors.Add($"Console provider configuration failed: {ex.Message}");
            }
            
            // Set log level
            var logLevel = config.IncludeDetailedLogs ? LogLevel.Information : LogLevel.Warning;
            builder.SetMinimumLevel(logLevel);
        });

        // STEP 4: Display status and fail if critical
        DisplayLoggingStatus(nlogOk, consoleOk, nlogConfigPath, logDirectory, errors);

        // FAIL FAST if no logging available
        if (!nlogOk && !consoleOk)
        {
            throw new InvalidOperationException(
                "CRITICAL: No logging providers configured successfully. Service cannot start without logging capability.");
        }

        // STEP 5: Test actual logging functionality
        TestLoggingFunctionality(services);
        
    }
    catch (Exception ex) when (!(ex is InvalidOperationException))
    {
        Console.WriteLine($"❌ FATAL: Logging validation failed: {ex.Message}");
        throw new InvalidOperationException("Logging validation failed - service cannot start", ex);
    }
}

/// <summary>
/// NOUVELLE FONCTION: Display clear logging status
/// </summary>
static void DisplayLoggingStatus(bool nlogOk, bool consoleOk, string nlogPath, string logDir, List<string> errors)
{
    Console.WriteLine();
    Console.WriteLine("📋 LOGGING CONFIGURATION STATUS");
    Console.WriteLine("=" + new string('=', 60));
    
    if (nlogOk && consoleOk)
    {
        Console.WriteLine("✅ OPTIMAL: File logging + Console logging active");
        Console.WriteLine($"📁 NLog config: {nlogPath}");
        Console.WriteLine($"📂 Log files: {logDir}");
        Console.WriteLine("🖥️  Real-time console output available");
    }
    else if (consoleOk && !nlogOk)
    {
        Console.WriteLine("⚠️  DEGRADED: Console-only logging (file logging failed)");
        Console.WriteLine($"📁 NLog config issues: {nlogPath}");
        Console.WriteLine($"📂 Target log directory: {logDir}");
        Console.WriteLine("🖥️  Console output available but no log files");
        
        Console.WriteLine("\n🔧 ISSUES TO FIX:");
        foreach (var error in errors)
        {
            Console.WriteLine($"   • {error}");
        }
    }
    else if (nlogOk && !consoleOk)
    {
        Console.WriteLine("⚠️  PARTIAL: File logging only (console failed)");
        Console.WriteLine($"📁 Log files active: {logDir}");
        Console.WriteLine("❌ No real-time console output");
    }
    else
    {
        Console.WriteLine("❌ CRITICAL FAILURE: NO LOGGING AVAILABLE");
        Console.WriteLine("🛑 SERVICE CANNOT START");
        
        Console.WriteLine("\n💥 ALL ISSUES:");
        foreach (var error in errors)
        {
            Console.WriteLine($"   • {error}");
        }
        
        Console.WriteLine("\n🔧 REQUIRED FIXES:");
        Console.WriteLine("   1. Ensure NLog.config exists and is readable");
        Console.WriteLine("   2. Check log directory permissions");
        Console.WriteLine("   3. Verify disk space availability");
        Console.WriteLine("   4. Run as administrator if necessary");
    }
    
    Console.WriteLine("=" + new string('=', 60));
    Console.WriteLine();
}

/// <summary>
/// NOUVELLE FONCTION: Test actual logging functionality
/// </summary>
static void TestLoggingFunctionality(IServiceCollection services)
{
    try
    {
        Console.WriteLine("🧪 Testing logging functionality...");
        
        using var serviceProvider = services.BuildServiceProvider();
        var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
        
        // Test different log levels
        logger.LogInformation("🧪 Logging test: INFO level - {Timestamp}", DateTime.Now);
        logger.LogWarning("🧪 Logging test: WARNING level - {Timestamp}", DateTime.Now);
        
        Console.WriteLine("✅ Logging functionality test completed");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Logging functionality test FAILED: {ex.Message}");
        throw new InvalidOperationException("Logging system is not functional", ex);
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
    /// SPRINT 14: Configuration path resolution (legacy mode only)
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
}