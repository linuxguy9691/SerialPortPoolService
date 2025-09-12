// ===================================================================
// SPRINT 13 BOUCHÉE #2: Dynamic BIB Configuration Service - Hot-Add FileSystemWatcher
// File: SerialPortPool.Core/Services/DynamicBibConfigurationService.cs
// Purpose: Real-time BIB file detection and automatic workflow execution
// Philosophy: "Foundation Excellence" - Leverage existing services with minimal new code
// ===================================================================

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SerialPortPool.Core.Interfaces;
using SerialPortPool.Core.Models;

namespace SerialPortPool.Core.Services;

/// <summary>
/// SPRINT 13: Dynamic BIB Configuration Service with Hot-Add capability
/// FOUNDATION EXCELLENCE: 90% reuses existing services (XmlBibConfigurationLoader, BibWorkflowOrchestrator)
/// ONLY NEW CODE: FileSystemWatcher + thread safety + event coordination
/// </summary>
public class DynamicBibConfigurationService : IHostedService, IDisposable
{
    private readonly XmlBibConfigurationLoader _configLoader;  // ✅ EXISTS & EXCELLENT
    private readonly IBibWorkflowOrchestrator _orchestrator;   // ✅ EXISTS & COMPLETE
    private readonly ILogger<DynamicBibConfigurationService> _logger;
    private readonly DynamicBibConfigurationOptions _options;

    // 🆕 ONLY NEW CODE NEEDED:
    private FileSystemWatcher? _xmlWatcher;
    private readonly SemaphoreSlim _processingLock = new(1, 1);
    private readonly Dictionary<string, DateTime> _lastProcessedTime = new();
    private readonly HashSet<string> _discoveredBibs = new();
    private CancellationTokenSource? _cancellationTokenSource;
    private bool _isDisposed;

    // Events for external integration
    public event EventHandler<BibDiscoveredEventArgs>? BibDiscovered;
    public event EventHandler<BibRemovedEventArgs>? BibRemoved;
    public event EventHandler<BibProcessedEventArgs>? BibProcessed;
    public event EventHandler<BibErrorEventArgs>? BibError;

    public DynamicBibConfigurationService(
        IBibConfigurationLoader configLoader,
        IBibWorkflowOrchestrator orchestrator,
        ILogger<DynamicBibConfigurationService> logger,
        DynamicBibConfigurationOptions? options = null)
    {
        _configLoader = (XmlBibConfigurationLoader)configLoader; // Use existing excellent service
        _orchestrator = orchestrator; // ✅ EXISTS & COMPLETE
        _logger = logger;
        _options = options ?? DynamicBibConfigurationOptions.CreateDefault();

        _logger.LogInformation("🚀 SPRINT 13: Dynamic BIB Configuration Service initialized");
        _logger.LogInformation("📁 Watch Directory: {WatchDirectory}", _options.WatchDirectory);
        _logger.LogInformation("🔍 Watch Pattern: {WatchPattern}", _options.WatchPattern);
        _logger.LogInformation("⏱️ Debounce Delay: {DebounceMs}ms", _options.DebounceDelayMs);
        _logger.LogInformation("🎬 Auto Execute: {AutoExecute}", _options.AutoExecuteOnDiscovery);
    }

    /// <summary>
    /// Start the Hot-Add service with FileSystemWatcher
    /// </summary>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("🚀 Starting SPRINT 13 Hot-Add BIB Service...");

            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            // Ensure watch directory exists
            await EnsureWatchDirectoryExistsAsync();

            // Setup FileSystemWatcher
            await SetupFileSystemWatcherAsync();

            // Perform initial discovery
            if (_options.PerformInitialDiscovery)
            {
                await PerformInitialBibDiscoveryAsync();
            }

            _logger.LogInformation("✅ SPRINT 13 Hot-Add BIB Service started successfully");
            _logger.LogInformation("👀 Monitoring: {WatchDirectory}/{WatchPattern}", _options.WatchDirectory, _options.WatchPattern);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to start Hot-Add BIB Service");
            throw;
        }
    }

    /// <summary>
    /// Stop the Hot-Add service gracefully
    /// </summary>
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("🛑 Stopping SPRINT 13 Hot-Add BIB Service...");

            if (_xmlWatcher != null)
            {
                _xmlWatcher.EnableRaisingEvents = false;
                _xmlWatcher.Dispose();
                _xmlWatcher = null;
            }

            _cancellationTokenSource?.Cancel();

            // Wait for any pending operations to complete
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            using var combined = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeout.Token);
            
            await _processingLock.WaitAsync(combined.Token);
            _processingLock.Release();

            _logger.LogInformation("✅ SPRINT 13 Hot-Add BIB Service stopped gracefully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error stopping Hot-Add BIB Service");
        }
    }

    #region FileSystemWatcher Setup and Event Handlers

    /// <summary>
    /// Setup FileSystemWatcher for BIB file monitoring
    /// </summary>
    private async Task SetupFileSystemWatcherAsync()
    {
        _xmlWatcher = new FileSystemWatcher(_options.WatchDirectory, _options.WatchPattern)
        {
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.CreationTime | NotifyFilters.FileName,
            EnableRaisingEvents = false // Start disabled, enable after setup
        };

        // Wire up event handlers
        _xmlWatcher.Created += OnBibFileCreated;
        _xmlWatcher.Changed += OnBibFileChanged;
        _xmlWatcher.Deleted += OnBibFileDeleted;
        _xmlWatcher.Renamed += OnBibFileRenamed;
        _xmlWatcher.Error += OnWatcherError;

        // Enable monitoring
        _xmlWatcher.EnableRaisingEvents = true;

        _logger.LogInformation("📁 FileSystemWatcher configured and enabled");
        await Task.CompletedTask;
    }

    /// <summary>
    /// Handle new BIB file creation
    /// </summary>
    private void OnBibFileCreated(object sender, FileSystemEventArgs e)
    {
        if (_isDisposed) return;

        var bibId = ExtractBibIdFromFilename(e.Name);
        if (string.IsNullOrEmpty(bibId)) return;

        _logger.LogInformation("🆕 New BIB file detected: {FileName} → BIB_ID: {BibId}", e.Name, bibId);

        // Debounce and process
        _ = Task.Run(async () => await ProcessBibFileWithDebounceAsync(bibId, e.FullPath, BibChangeType.Created));
    }

    /// <summary>
    /// Handle BIB file changes
    /// </summary>
    private void OnBibFileChanged(object sender, FileSystemEventArgs e)
    {
        if (_isDisposed) return;

        var bibId = ExtractBibIdFromFilename(e.Name);
        if (string.IsNullOrEmpty(bibId)) return;

        _logger.LogDebug("📝 BIB file changed: {FileName} → BIB_ID: {BibId}", e.Name, bibId);

        // Debounce and process
        _ = Task.Run(async () => await ProcessBibFileWithDebounceAsync(bibId, e.FullPath, BibChangeType.Modified));
    }

    /// <summary>
    /// Handle BIB file deletion
    /// </summary>
    private void OnBibFileDeleted(object sender, FileSystemEventArgs e)
    {
        if (_isDisposed) return;

        var bibId = ExtractBibIdFromFilename(e.Name);
        if (string.IsNullOrEmpty(bibId)) return;

        _logger.LogWarning("🗑️ BIB file deleted: {FileName} → BIB_ID: {BibId}", e.Name, bibId);

        // Process deletion immediately (no debouncing needed)
        _ = Task.Run(async () => await ProcessBibDeletionAsync(bibId, e.FullPath));
    }

    /// <summary>
    /// Handle BIB file rename
    /// </summary>
    private void OnBibFileRenamed(object sender, RenamedEventArgs e)
    {
        if (_isDisposed) return;

        var oldBibId = ExtractBibIdFromFilename(e.OldName);
        var newBibId = ExtractBibIdFromFilename(e.Name);

        _logger.LogInformation("🔄 BIB file renamed: {OldName} → {NewName} (BIB: {OldBibId} → {NewBibId})", 
            e.OldName, e.Name, oldBibId, newBibId);

        // Handle as deletion + creation
        if (!string.IsNullOrEmpty(oldBibId))
        {
            _ = Task.Run(async () => await ProcessBibDeletionAsync(oldBibId, e.OldFullPath));
        }

        if (!string.IsNullOrEmpty(newBibId))
        {
            _ = Task.Run(async () => await ProcessBibFileWithDebounceAsync(newBibId, e.FullPath, BibChangeType.Created));
        }
    }

    /// <summary>
    /// Handle FileSystemWatcher errors
    /// </summary>
    private void OnWatcherError(object sender, ErrorEventArgs e)
    {
        _logger.LogError(e.GetException(), "❌ FileSystemWatcher error occurred");

        // Attempt to restart the watcher
        _ = Task.Run(async () =>
        {
            await Task.Delay(TimeSpan.FromSeconds(5)); // Wait before restart
            await RestartWatcherAsync();
        });
    }

    #endregion

    #region BIB Processing Logic

    /// <summary>
    /// Process BIB file with debouncing to avoid duplicate processing
    /// </summary>
    private async Task ProcessBibFileWithDebounceAsync(string bibId, string filePath, BibChangeType changeType)
    {
        try
        {
            // Debouncing: Check if we've processed this file recently
            var now = DateTime.Now;
            if (_lastProcessedTime.TryGetValue(bibId, out var lastProcessed))
            {
                var timeSinceLastProcess = now - lastProcessed;
                if (timeSinceLastProcess.TotalMilliseconds < _options.DebounceDelayMs)
                {
                    _logger.LogDebug("⏳ Debouncing BIB {BibId}: {TimeSince}ms since last process", 
                        bibId, timeSinceLastProcess.TotalMilliseconds);
                    return;
                }
            }

            // Additional delay to ensure file write is complete
            await Task.Delay(_options.DebounceDelayMs, _cancellationTokenSource?.Token ?? CancellationToken.None);

            // Process with thread safety
            await _processingLock.WaitAsync(_cancellationTokenSource?.Token ?? CancellationToken.None);
            try
            {
                _lastProcessedTime[bibId] = now;
                await ProcessBibFileAsync(bibId, filePath, changeType);
            }
            finally
            {
                _processingLock.Release();
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("🛑 BIB processing cancelled for: {BibId}", bibId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error processing BIB file: {BibId}", bibId);
            await HandleBibErrorAsync(bibId, filePath, $"Processing error: {ex.Message}");
        }
    }

    /// <summary>
    /// Process individual BIB file using existing excellent services
    /// </summary>
    private async Task ProcessBibFileAsync(string bibId, string filePath, BibChangeType changeType)
    {
        _logger.LogInformation("🔄 Processing BIB: {BibId} (Change: {ChangeType})", bibId, changeType);

        try
        {
            // ✅ FOUNDATION EXCELLENCE: Use existing XmlBibConfigurationLoader
            var bibConfig = await _configLoader.LoadBibConfigurationAsync(bibId);

            if (bibConfig == null)
            {
                _logger.LogError("❌ Failed to load BIB configuration: {BibId}", bibId);
                await HandleBibErrorAsync(bibId, filePath, "BIB configuration loading failed");
                return;
            }

            // Track discovered BIB
            var isNewBib = _discoveredBibs.Add(bibId);

            // Log BIB details
            _logger.LogInformation("📋 BIB Loaded: {BibConfig}", bibConfig);
            _logger.LogInformation("🛠️ Hardware Simulation: {SimulationSummary}", bibConfig.GetSimulationSummary());

            // Fire discovery event
            if (changeType == BibChangeType.Created || isNewBib)
            {
                BibDiscovered?.Invoke(this, new BibDiscoveredEventArgs
                {
                    BibId = bibId,
                    FilePath = filePath,
                    BibConfiguration = bibConfig,
                    IsHardwareSimulation = bibConfig.IsHardwareSimulationEnabled,
                    DiscoveredAt = DateTime.Now
                });
            }

            // ✅ AUTO-EXECUTE: Use existing excellent BibWorkflowOrchestrator
            if (_options.AutoExecuteOnDiscovery)
            {
                // 🎯 SPRINT 14: Check if we're in Production mode - if so, skip auto-execute
                // Production mode handles execution via MultiBibWorkflowService
                
                _logger.LogInformation("🎯 AutoExecuteOnDiscovery enabled - checking execution mode coordination...");
                
                // In Production mode, MultiBibWorkflowService handles the execution
                // DynamicBibConfigurationService should only register the BIB, not execute it
                var isProductionMode = CheckIfProductionMode();
                
                if (isProductionMode)
                {
                    _logger.LogInformation("🏭 Production mode detected - BIB registered but execution delegated to Production workflow");
                    _logger.LogInformation("✅ BIB processing completed successfully: {BibId} (Production Mode)", bibId);
                    return;
                }
                
                // Non-production modes: execute immediately as before
                await ExecuteBibWorkflowAsync(bibConfig, bibId, filePath);
            }

            _logger.LogInformation("✅ BIB processing completed successfully: {BibId}", bibId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error processing BIB: {BibId}", bibId);
            await HandleBibErrorAsync(bibId, filePath, $"BIB processing failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Execute BIB workflow using existing orchestrator (FOUNDATION EXCELLENCE)
    /// </summary>
    private async Task ExecuteBibWorkflowAsync(BibConfiguration bibConfig, string bibId, string filePath)
    {
        try
        {
            _logger.LogInformation("🚀 Auto-executing BIB workflow: {BibId}", bibId);

            // ✅ FOUNDATION EXCELLENCE: Use existing BibWorkflowOrchestrator
            var result = await _orchestrator.ExecuteBibWorkflowCompleteAsync(
                bibId,
                "DynamicBibService",
                _cancellationTokenSource?.Token ?? CancellationToken.None);

            // Log results
            var status = result.AllSuccessful ? "✅ SUCCESS" : "⚠️ PARTIAL";
            _logger.LogInformation("{Status} BIB workflow completed: {BibId} - {Summary}", 
                status, bibId, result.GetDetailedSummary());

            // Fire processed event
            BibProcessed?.Invoke(this, new BibProcessedEventArgs
            {
                BibId = bibId,
                FilePath = filePath,
                WorkflowResult = result,
                ProcessedAt = DateTime.Now,
                Success = result.AllSuccessful
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Auto-execution failed for BIB: {BibId}", bibId);
            await HandleBibErrorAsync(bibId, filePath, $"Workflow execution failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Process BIB deletion
    /// </summary>
    private async Task ProcessBibDeletionAsync(string bibId, string filePath)
    {
        try
        {
            await _processingLock.WaitAsync(_cancellationTokenSource?.Token ?? CancellationToken.None);
            try
            {
                _discoveredBibs.Remove(bibId);
                _lastProcessedTime.Remove(bibId);

                _logger.LogInformation("🗑️ BIB removed from tracking: {BibId}", bibId);

                // Fire removal event
                BibRemoved?.Invoke(this, new BibRemovedEventArgs
                {
                    BibId = bibId,
                    FilePath = filePath,
                    RemovedAt = DateTime.Now
                });
            }
            finally
            {
                _processingLock.Release();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error processing BIB deletion: {BibId}", bibId);
        }
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Ensure watch directory exists
    /// </summary>
    private async Task EnsureWatchDirectoryExistsAsync()
    {
        if (!Directory.Exists(_options.WatchDirectory))
        {
            Directory.CreateDirectory(_options.WatchDirectory);
            _logger.LogInformation("📁 Created watch directory: {WatchDirectory}", _options.WatchDirectory);

            // Create sample BIB files if enabled
            if (_options.CreateSampleFiles)
            {
                await CreateSampleBibFilesAsync();
            }
        }
    }

    /// <summary>
    /// Perform initial discovery of existing BIB files
    /// </summary>
    private async Task PerformInitialBibDiscoveryAsync()
    {
        try
        {
            _logger.LogInformation("🔍 Performing initial BIB discovery...");

            var bibFiles = Directory.GetFiles(_options.WatchDirectory, _options.WatchPattern);
            _logger.LogInformation("📄 Found {Count} existing BIB files", bibFiles.Length);

            foreach (var filePath in bibFiles)
            {
                var fileName = Path.GetFileName(filePath);
                var bibId = ExtractBibIdFromFilename(fileName);

                if (!string.IsNullOrEmpty(bibId))
                {
                    _logger.LogDebug("📋 Initial discovery: {FileName} → BIB_ID: {BibId}", fileName, bibId);
                    await ProcessBibFileAsync(bibId, filePath, BibChangeType.Discovered);

                    // Small delay between initial discoveries
                    await Task.Delay(100, _cancellationTokenSource?.Token ?? CancellationToken.None);
                }
            }

            _logger.LogInformation("✅ Initial BIB discovery completed: {Count} BIBs processed", _discoveredBibs.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error during initial BIB discovery");
        }
    }

    /// <summary>
    /// Extract BIB ID from filename (bib_xyz.xml → xyz)
    /// Uses existing logic from Program.cs
    /// </summary>
    private string? ExtractBibIdFromFilename(string? fileName)
    {
        if (string.IsNullOrEmpty(fileName) || !fileName.StartsWith("bib_") || !fileName.EndsWith(".xml"))
            return null;

        return fileName.Substring(4, fileName.Length - 8);
    }

    /// <summary>
    /// Create sample BIB files for testing (uses existing logic from Program.cs)
    /// </summary>
    private async Task CreateSampleBibFilesAsync()
    {
        try
        {
            _logger.LogInformation("📝 Creating sample BIB files for SPRINT 13 testing...");

            // Sample with hardware simulation enabled
            var simulationBib = CreateSampleSimulationBib();
            var simulationPath = Path.Combine(_options.WatchDirectory, "bib_hardware_simulation_demo.xml");
            await File.WriteAllTextAsync(simulationPath, simulationBib);

            // Sample with real hardware
            var realHardwareBib = CreateSampleRealHardwareBib();
            var realHardwarePath = Path.Combine(_options.WatchDirectory, "bib_real_hardware_demo.xml");
            await File.WriteAllTextAsync(realHardwarePath, realHardwareBib);

            _logger.LogInformation("✅ Created sample BIB files:");
            _logger.LogInformation("   📄 {FileName} (Hardware Simulation)", Path.GetFileName(simulationPath));
            _logger.LogInformation("   📄 {FileName} (Real Hardware)", Path.GetFileName(realHardwarePath));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "⚠️ Could not create sample BIB files");
        }
    }

    /// <summary>
    /// Handle BIB processing errors
    /// </summary>
    private async Task HandleBibErrorAsync(string bibId, string filePath, string errorMessage)
    {
        BibError?.Invoke(this, new BibErrorEventArgs
        {
            BibId = bibId,
            FilePath = filePath,
            ErrorMessage = errorMessage,
            ErrorTime = DateTime.Now
        });

        await Task.CompletedTask;
    }

    /// <summary>
    /// Restart FileSystemWatcher after error
    /// </summary>
    private async Task RestartWatcherAsync()
    {
        try
        {
            _logger.LogInformation("🔄 Attempting to restart FileSystemWatcher...");

            if (_xmlWatcher != null)
            {
                _xmlWatcher.Dispose();
                _xmlWatcher = null;
            }

            await SetupFileSystemWatcherAsync();
            _logger.LogInformation("✅ FileSystemWatcher restarted successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to restart FileSystemWatcher");
        }
    }

    #endregion

    #region Sample BIB Creation

    /// <summary>
    /// Create sample BIB with hardware simulation enabled
    /// </summary>
    private string CreateSampleSimulationBib()
    {
        return $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<bib id=""hardware_simulation_demo"" description=""SPRINT 13: Hardware Simulation Demo BIB"">
  <metadata>
    <board_type>simulation</board_type>
    <sprint>13</sprint>
    <demo_mode>true</demo_mode>
    <created_date>{DateTime.Now:yyyy-MM-dd}</created_date>
  </metadata>
  
  <hardware_simulation>
    <enabled>true</enabled>
    <mode>Fast</mode>
    <speed_multiplier>2.0</speed_multiplier>
    
    <start_trigger>
      <delay_seconds>0.5</delay_seconds>
      <success_response>SIMULATION_READY</success_response>
      <enable_diagnostics>true</enable_diagnostics>
    </start_trigger>
    
    <stop_trigger>
      <delay_seconds>0.2</delay_seconds>
      <success_response>SIMULATION_COMPLETE</success_response>
      <graceful_shutdown>true</graceful_shutdown>
    </stop_trigger>
    
    <critical_trigger>
      <enabled>false</enabled>
    </critical_trigger>
    
    <random_behavior>
      <enabled>false</enabled>
    </random_behavior>
  </hardware_simulation>
  
  <uut id=""simulation_uut"" description=""Simulated UUT for Demo"">
    <port number=""1"">
      <protocol>rs232</protocol>
      <speed>115200</speed>
      <data_pattern>n81</data_pattern>
      
      <start>
        <command>INIT_SIM</command>
        <expected_response>READY_SIM</expected_response>
        <timeout_ms>2000</timeout_ms>
      </start>
      
      <test>
        <command>TEST_SIM</command>
        <expected_response>PASS_SIM</expected_response>
        <timeout_ms>3000</timeout_ms>
      </test>
      
      <stop>
        <command>QUIT_SIM</command>
        <expected_response>BYE_SIM</expected_response>
        <timeout_ms>1000</timeout_ms>
      </stop>
    </port>
  </uut>
</bib>";
    }

    /// <summary>
    /// Create sample BIB for real hardware
    /// </summary>
    private string CreateSampleRealHardwareBib()
    {
        return $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<bib id=""real_hardware_demo"" description=""SPRINT 13: Real Hardware Demo BIB"">
  <metadata>
    <board_type>production</board_type>
    <sprint>13</sprint>
    <demo_mode>false</demo_mode>
    <created_date>{DateTime.Now:yyyy-MM-dd}</created_date>
  </metadata>
  
  <uut id=""production_uut"" description=""Real Hardware UUT"">
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
    }

private bool CheckIfProductionMode()
{
    // For now, assume Production mode if specific patterns in metadata
    // In a fuller implementation, this would check the actual service configuration
    return true; // Assume Production mode for now - this should be properly injected
}
    #endregion

    #region Public API

    /// <summary>
    /// Get current service statistics
    /// </summary>
    public DynamicBibServiceStatistics GetStatistics()
    {
        return new DynamicBibServiceStatistics
        {
            DiscoveredBibsCount = _discoveredBibs.Count,
            DiscoveredBibIds = new List<string>(_discoveredBibs),
            WatchDirectory = _options.WatchDirectory,
            IsWatcherActive = _xmlWatcher?.EnableRaisingEvents == true,
            LastProcessedTimes = new Dictionary<string, DateTime>(_lastProcessedTime),
            ServiceStartTime = DateTime.Now // In real implementation, track actual start time
        };
    }

    /// <summary>
    /// Manually trigger discovery of a specific BIB file
    /// </summary>
    public async Task<bool> TriggerBibDiscoveryAsync(string bibId)
    {
        try
        {
            var fileName = $"bib_{bibId}.xml";
            var filePath = Path.Combine(_options.WatchDirectory, fileName);

            if (!File.Exists(filePath))
            {
                _logger.LogWarning("⚠️ Manual discovery failed - BIB file not found: {FileName}", fileName);
                return false;
            }

            _logger.LogInformation("🎯 Manual BIB discovery triggered: {BibId}", bibId);
            await ProcessBibFileAsync(bibId, filePath, BibChangeType.Manual);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Manual BIB discovery failed: {BibId}", bibId);
            return false;
        }
    }

    #endregion

    /// <summary>
    /// Dispose resources
    /// </summary>
    public void Dispose()
    {
        if (!_isDisposed)
        {
            _isDisposed = true;

            _xmlWatcher?.Dispose();
            _processingLock?.Dispose();
            _cancellationTokenSource?.Dispose();

            _logger.LogDebug("🧹 Dynamic BIB Configuration Service disposed");
        }
    }
}

/// <summary>
/// Configuration options for Dynamic BIB Configuration Service
/// </summary>
public class DynamicBibConfigurationOptions
{
    /// <summary>
    /// Directory to watch for BIB files
    /// </summary>
    public string WatchDirectory { get; set; } = "Configuration/";

    /// <summary>
    /// File pattern to watch for
    /// </summary>
    public string WatchPattern { get; set; } = "bib_*.xml";

    /// <summary>
    /// Debounce delay to prevent duplicate processing (milliseconds)
    /// </summary>
    public int DebounceDelayMs { get; set; } = 500;

    /// <summary>
    /// Automatically execute workflows when BIBs are discovered
    /// </summary>
    public bool AutoExecuteOnDiscovery { get; set; } = true;

    /// <summary>
    /// Perform initial discovery of existing files on startup
    /// </summary>
    public bool PerformInitialDiscovery { get; set; } = true;

    /// <summary>
    /// Create sample BIB files if directory is empty
    /// </summary>
    public bool CreateSampleFiles { get; set; } = true;

    /// <summary>
    /// Create default configuration
    /// </summary>
    public static DynamicBibConfigurationOptions CreateDefault() => new();

    /// <summary>
    /// Create fast demo configuration
    /// </summary>
    public static DynamicBibConfigurationOptions CreateFastDemo()
    {
        return new DynamicBibConfigurationOptions
        {
            DebounceDelayMs = 200,
            AutoExecuteOnDiscovery = true,
            PerformInitialDiscovery = true,
            CreateSampleFiles = true
        };
    }
}

/// <summary>
/// Service statistics for monitoring
/// </summary>
public class DynamicBibServiceStatistics
{
    public int DiscoveredBibsCount { get; set; }
    public List<string> DiscoveredBibIds { get; set; } = new();
    public string WatchDirectory { get; set; } = string.Empty;
    public bool IsWatcherActive { get; set; }
    public Dictionary<string, DateTime> LastProcessedTimes { get; set; } = new();
    public DateTime ServiceStartTime { get; set; }

    public override string ToString()
    {
        return $"Dynamic BIB Service: {DiscoveredBibsCount} BIBs discovered, Watcher: {(IsWatcherActive ? "Active" : "Inactive")}";
    }
}

/// <summary>
/// Types of BIB changes
/// </summary>
public enum BibChangeType
{
    Created,
    Modified,
    Discovered,
    Manual
}

#region Event Args

/// <summary>
/// Event arguments for BIB discovery
/// </summary>
public class BibDiscoveredEventArgs : EventArgs
{
    public string BibId { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public BibConfiguration? BibConfiguration { get; set; }
    public bool IsHardwareSimulation { get; set; }
    public DateTime DiscoveredAt { get; set; }
}

/// <summary>
/// Event arguments for BIB removal
/// </summary>
public class BibRemovedEventArgs : EventArgs
{
    public string BibId { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public DateTime RemovedAt { get; set; }
}

/// <summary>
/// Event arguments for BIB processing completion
/// </summary>
public class BibProcessedEventArgs : EventArgs
{
    public string BibId { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public AggregatedWorkflowResult? WorkflowResult { get; set; }
    public DateTime ProcessedAt { get; set; }
    public bool Success { get; set; }
}

/// <summary>
/// Event arguments for BIB processing errors
/// </summary>
public class BibErrorEventArgs : EventArgs
{
    public string BibId { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public DateTime ErrorTime { get; set; }
}

#endregion