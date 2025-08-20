// ===================================================================
// SPRINT 11 BOUCHÉE #3.3: Hot Reload Configuration Service - Dynamic Updates
// File: SerialPortPool.Core/Services/Configuration/HotReloadConfigurationService.cs
// Purpose: Real-time configuration file monitoring and hot reload capability
// Philosophy: "Single Responsibility" - ONLY file watching and hot reload events
// ===================================================================

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace SerialPortPool.Core.Services.Configuration;

/// <summary>
/// SPRINT 11: Hot reload configuration monitoring service
/// SINGLE RESPONSIBILITY: File watching and configuration change notifications only
/// ZERO TOUCH: Works with any configuration system via events
/// </summary>
public class HotReloadConfigurationService : IHostedService, IDisposable
{
    private readonly ILogger<HotReloadConfigurationService> _logger;
    private readonly IConfigurationValidator? _validator;
    private readonly IConfigurationBackupService? _backupService;
    private readonly HotReloadOptions _options;
    
    private FileSystemWatcher? _configWatcher;
    private readonly Dictionary<string, Timer> _debounceTimers = new();
    private readonly object _timerLock = new();
    private bool _isDisposed;

    // Events for external integration
    public event EventHandler<ConfigurationChangedEventArgs>? ConfigurationChanged;
    public event EventHandler<ConfigurationAddedEventArgs>? ConfigurationAdded;
    public event EventHandler<ConfigurationRemovedEventArgs>? ConfigurationRemoved;
    public event EventHandler<ConfigurationErrorEventArgs>? ConfigurationError;

    public HotReloadConfigurationService(
        ILogger<HotReloadConfigurationService> logger,
        IConfigurationValidator? validator = null,
        IConfigurationBackupService? backupService = null,
        HotReloadOptions? options = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _validator = validator;
        _backupService = backupService;
        _options = options ?? HotReloadOptions.CreateDefault();
        
        _logger.LogInformation("🔄 Hot Reload Configuration Service initialized");
        _logger.LogInformation("📁 Watch Directory: {WatchDirectory}", _options.WatchDirectory);
        _logger.LogInformation("📄 Watch Pattern: {WatchPattern}", _options.WatchPattern);
        _logger.LogInformation("⏱️ Debounce Delay: {DebounceMs}ms", _options.DebounceDelayMs);
    }

    /// <summary>
    /// Start file system monitoring
    /// </summary>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("🚀 Starting Hot Reload Configuration Service...");

            // Ensure watch directory exists
            if (!Directory.Exists(_options.WatchDirectory))
            {
                Directory.CreateDirectory(_options.WatchDirectory);
                _logger.LogInformation("📁 Created watch directory: {WatchDirectory}", _options.WatchDirectory);
            }

            // Setup file system watcher
            _configWatcher = new FileSystemWatcher(_options.WatchDirectory, _options.WatchPattern)
            {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.CreationTime | NotifyFilters.FileName,
                EnableRaisingEvents = false // Start disabled, enable after setup
            };

            // Wire up event handlers
            _configWatcher.Changed += OnConfigurationFileChanged;
            _configWatcher.Created += OnConfigurationFileCreated;
            _configWatcher.Deleted += OnConfigurationFileDeleted;
            _configWatcher.Renamed += OnConfigurationFileRenamed;
            _configWatcher.Error += OnWatcherError;

            // Enable monitoring
            _configWatcher.EnableRaisingEvents = true;

            _logger.LogInformation("✅ Hot Reload monitoring started successfully");
            _logger.LogInformation("👀 Watching: {WatchDirectory}/{WatchPattern}", _options.WatchDirectory, _options.WatchPattern);

            // Perform initial scan if requested
            if (_options.PerformInitialScan)
            {
                await PerformInitialScanAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to start Hot Reload Configuration Service");
            throw;
        }
    }

    /// <summary>
    /// Stop file system monitoring
    /// </summary>
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("🛑 Stopping Hot Reload Configuration Service...");

            if (_configWatcher != null)
            {
                _configWatcher.EnableRaisingEvents = false;
                _configWatcher.Dispose();
                _configWatcher = null;
            }

            // Clear pending debounce timers
            ClearAllDebounceTimers();

            _logger.LogInformation("✅ Hot Reload Configuration Service stopped");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error stopping Hot Reload Configuration Service");
        }
    }

    #region File System Event Handlers

    /// <summary>
    /// Handle configuration file changes (with debouncing)
    /// </summary>
    private void OnConfigurationFileChanged(object sender, FileSystemEventArgs e)
    {
        if (_isDisposed) return;

        try
        {
            var bibId = ExtractBibIdFromFilename(e.Name);
            if (string.IsNullOrEmpty(bibId))
            {
                _logger.LogDebug("⚠️ Could not extract BIB ID from filename: {FileName}", e.Name);
                return;
            }

            _logger.LogDebug("📝 Configuration file changed: {FileName} (BIB: {BibId})", e.Name, bibId);

            // Debounce rapid file changes
            DebounceFileOperation(bibId, e.FullPath, () => ProcessConfigurationChangedAsync(bibId, e.FullPath));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error handling configuration file change: {FileName}", e.Name);
        }
    }

    /// <summary>
    /// Handle new configuration file creation
    /// </summary>
    private void OnConfigurationFileCreated(object sender, FileSystemEventArgs e)
    {
        if (_isDisposed) return;

        try
        {
            var bibId = ExtractBibIdFromFilename(e.Name);
            if (string.IsNullOrEmpty(bibId))
            {
                _logger.LogDebug("⚠️ Could not extract BIB ID from filename: {FileName}", e.Name);
                return;
            }

            _logger.LogInformation("🆕 New configuration file detected: {FileName} (BIB: {BibId})", e.Name, bibId);

            // Debounce to ensure file write is complete
            DebounceFileOperation(bibId, e.FullPath, () => ProcessConfigurationAddedAsync(bibId, e.FullPath));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error handling new configuration file: {FileName}", e.Name);
        }
    }

    /// <summary>
    /// Handle configuration file deletion
    /// </summary>
    private void OnConfigurationFileDeleted(object sender, FileSystemEventArgs e)
    {
        if (_isDisposed) return;

        try
        {
            var bibId = ExtractBibIdFromFilename(e.Name);
            if (string.IsNullOrEmpty(bibId))
            {
                _logger.LogDebug("⚠️ Could not extract BIB ID from filename: {FileName}", e.Name);
                return;
            }

            _logger.LogWarning("🗑️ Configuration file deleted: {FileName} (BIB: {BibId})", e.Name, bibId);

            // No debouncing needed for deletion
            _ = Task.Run(() => ProcessConfigurationRemovedAsync(bibId, e.FullPath));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error handling configuration file deletion: {FileName}", e.Name);
        }
    }

    /// <summary>
    /// Handle configuration file rename
    /// </summary>
    private void OnConfigurationFileRenamed(object sender, RenamedEventArgs e)
    {
        if (_isDisposed) return;

        try
        {
            var oldBibId = ExtractBibIdFromFilename(e.OldName);
            var newBibId = ExtractBibIdFromFilename(e.Name);

            _logger.LogInformation("🔄 Configuration file renamed: {OldName} → {NewName} (BIB: {OldBibId} → {NewBibId})", 
                e.OldName, e.Name, oldBibId, newBibId);

            // Handle as removal + addition
            if (!string.IsNullOrEmpty(oldBibId))
            {
                _ = Task.Run(() => ProcessConfigurationRemovedAsync(oldBibId, e.OldFullPath));
            }

            if (!string.IsNullOrEmpty(newBibId))
            {
                DebounceFileOperation(newBibId, e.FullPath, () => ProcessConfigurationAddedAsync(newBibId, e.FullPath));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error handling configuration file rename: {OldName} → {NewName}", e.OldName, e.Name);
        }
    }

    /// <summary>
    /// Handle file system watcher errors
    /// </summary>
    private void OnWatcherError(object sender, ErrorEventArgs e)
    {
        try
        {
            _logger.LogError(e.GetException(), "❌ File system watcher error occurred");

            // Attempt to restart the watcher
            _ = Task.Run(async () =>
            {
                await Task.Delay(TimeSpan.FromSeconds(5)); // Wait before restart
                await RestartWatcherAsync();
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error handling watcher error");
        }
    }

    #endregion

    #region Processing Methods

    /// <summary>
    /// Process configuration file change
    /// </summary>
    private async Task ProcessConfigurationChangedAsync(string bibId, string filePath)
    {
        try
        {
            _logger.LogDebug("🔄 Processing configuration change: {BibId}", bibId);

            // Validate changed configuration
            var isValid = await ValidateConfigurationAsync(bibId, filePath);

            if (isValid)
            {
                // Create backup before notifying about changes
                if (_backupService != null)
                {
                    var backupResult = await _backupService.CreateBackupAsync(bibId, filePath);
                    if (!backupResult.IsSuccess)
                    {
                        _logger.LogWarning("⚠️ Backup failed for changed configuration: {BibId} - {Error}", 
                            bibId, backupResult.Message);
                    }
                }

                // Notify about successful change
                ConfigurationChanged?.Invoke(this, new ConfigurationChangedEventArgs
                {
                    BibId = bibId,
                    FilePath = filePath,
                    ChangeType = ConfigurationChangeType.Modified,
                    IsValid = true,
                    Timestamp = DateTime.Now
                });

                _logger.LogInformation("✅ Configuration change processed successfully: {BibId}", bibId);
            }
            else
            {
                await HandleInvalidConfigurationAsync(bibId, filePath, "Configuration validation failed after change");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error processing configuration change: {BibId}", bibId);
            await HandleConfigurationErrorAsync(bibId, filePath, $"Processing error: {ex.Message}");
        }
    }

    /// <summary>
    /// Process new configuration file addition
    /// </summary>
    private async Task ProcessConfigurationAddedAsync(string bibId, string filePath)
    {
        try
        {
            _logger.LogDebug("🆕 Processing new configuration: {BibId}", bibId);

            // Validate new configuration
            var isValid = await ValidateConfigurationAsync(bibId, filePath);

            if (isValid)
            {
                // Create initial backup
                if (_backupService != null)
                {
                    var backupResult = await _backupService.CreateBackupAsync(bibId, filePath);
                    if (!backupResult.IsSuccess)
                    {
                        _logger.LogWarning("⚠️ Initial backup failed for new configuration: {BibId} - {Error}", 
                            bibId, backupResult.Message);
                    }
                }

                // Notify about successful addition
                ConfigurationAdded?.Invoke(this, new ConfigurationAddedEventArgs
                {
                    BibId = bibId,
                    FilePath = filePath,
                    IsValid = true,
                    Timestamp = DateTime.Now
                });

                _logger.LogInformation("✅ New configuration added successfully: {BibId}", bibId);
            }
            else
            {
                await HandleInvalidConfigurationAsync(bibId, filePath, "New configuration validation failed");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error processing new configuration: {BibId}", bibId);
            await HandleConfigurationErrorAsync(bibId, filePath, $"Processing error: {ex.Message}");
        }
    }

    /// <summary>
    /// Process configuration file removal
    /// </summary>
    private async Task ProcessConfigurationRemovedAsync(string bibId, string filePath)
    {
        try
        {
            _logger.LogDebug("🗑️ Processing configuration removal: {BibId}", bibId);

            // Notify about removal
            ConfigurationRemoved?.Invoke(this, new ConfigurationRemovedEventArgs
            {
                BibId = bibId,
                FilePath = filePath,
                Timestamp = DateTime.Now
            });

            _logger.LogInformation("✅ Configuration removal processed: {BibId}", bibId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error processing configuration removal: {BibId}", bibId);
        }
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Extract BIB ID from filename (bib_xyz.xml → xyz)
    /// </summary>
    private string? ExtractBibIdFromFilename(string? fileName)
    {
        if (string.IsNullOrEmpty(fileName) || !fileName.StartsWith("bib_") || !fileName.EndsWith(".xml"))
            return null;

        return fileName.Substring(4, fileName.Length - 8);
    }

    /// <summary>
    /// Debounce rapid file operations to avoid duplicate processing
    /// </summary>
    private void DebounceFileOperation(string bibId, string filePath, Func<Task> operation)
    {
        lock (_timerLock)
        {
            // Cancel existing timer for this BIB
            if (_debounceTimers.TryGetValue(bibId, out var existingTimer))
            {
                existingTimer.Dispose();
            }

            // Create new debounce timer
            var timer = new Timer(async _ =>
            {
                lock (_timerLock)
                {
                    _debounceTimers.Remove(bibId);
                }

                await operation();
            }, null, _options.DebounceDelayMs, Timeout.Infinite);

            _debounceTimers[bibId] = timer;
        }
    }

    /// <summary>
    /// Clear all pending debounce timers
    /// </summary>
    private void ClearAllDebounceTimers()
    {
        lock (_timerLock)
        {
            foreach (var timer in _debounceTimers.Values)
            {
                timer.Dispose();
            }
            _debounceTimers.Clear();
        }
    }

    /// <summary>
    /// Validate configuration using injected validator
    /// </summary>
    private async Task<bool> ValidateConfigurationAsync(string bibId, string filePath)
    {
        if (_validator == null)
        {
            _logger.LogDebug("📝 No validator configured, skipping validation for: {BibId}", bibId);
            return true; // Assume valid if no validator
        }

        try
        {
            var result = await _validator.ValidateConfigurationFileAsync(filePath);
            return result.IsValid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Validation failed for: {BibId}", bibId);
            return false;
        }
    }

    /// <summary>
    /// Handle invalid configuration scenario
    /// </summary>
    private async Task HandleInvalidConfigurationAsync(string bibId, string filePath, string reason)
    {
        _logger.LogError("❌ Invalid configuration detected: {BibId} - {Reason}", bibId, reason);

        // Attempt restore from backup if available
        if (_backupService != null)
        {
            var restoreResult = await _backupService.RestoreFromBackupAsync(bibId, filePath);
            if (restoreResult.IsSuccess)
            {
                _logger.LogInformation("✅ Successfully restored {BibId} from backup", bibId);
                return;
            }
        }

        await HandleConfigurationErrorAsync(bibId, filePath, reason);
    }

    /// <summary>
    /// Handle configuration error scenario
    /// </summary>
    private async Task HandleConfigurationErrorAsync(string bibId, string filePath, string errorMessage)
    {
        ConfigurationError?.Invoke(this, new ConfigurationErrorEventArgs
        {
            BibId = bibId,
            FilePath = filePath,
            ErrorMessage = errorMessage,
            Timestamp = DateTime.Now
        });
    }

    /// <summary>
    /// Restart file system watcher after error
    /// </summary>
    private async Task RestartWatcherAsync()
    {
        try
        {
            _logger.LogInformation("🔄 Attempting to restart file system watcher...");

            if (_configWatcher != null)
            {
                _configWatcher.Dispose();
                _configWatcher = null;
            }

            await StartAsync(CancellationToken.None);
            _logger.LogInformation("✅ File system watcher restarted successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to restart file system watcher");
        }
    }

    /// <summary>
    /// Perform initial scan of existing configuration files
    /// </summary>
    private async Task PerformInitialScanAsync()
    {
        try
        {
            _logger.LogInformation("🔍 Performing initial configuration scan...");

            var configFiles = Directory.GetFiles(_options.WatchDirectory, _options.WatchPattern);
            _logger.LogInformation("📄 Found {Count} existing configuration files", configFiles.Length);

            foreach (var filePath in configFiles)
            {
                var fileName = Path.GetFileName(filePath);
                var bibId = ExtractBibIdFromFilename(fileName);

                if (!string.IsNullOrEmpty(bibId))
                {
                    _logger.LogDebug("📋 Initial scan: {FileName} (BIB: {BibId})", fileName, bibId);
                    // Note: We don't trigger events for initial scan, just log the discovery
                }
            }

            _logger.LogInformation("✅ Initial configuration scan completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error during initial configuration scan");
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
            
            _configWatcher?.Dispose();
            ClearAllDebounceTimers();
            
            _logger.LogDebug("🧹 Hot Reload Configuration Service disposed");
        }
    }
}

/// <summary>
/// Hot reload configuration options
/// </summary>
public class HotReloadOptions
{
    public string WatchDirectory { get; set; } = "Configuration";
    public string WatchPattern { get; set; } = "bib_*.xml";
    public int DebounceDelayMs { get; set; } = 500;
    public bool PerformInitialScan { get; set; } = true;

    public static HotReloadOptions CreateDefault() => new();
}

/// <summary>
/// Configuration change event arguments
/// </summary>
public class ConfigurationChangedEventArgs : EventArgs
{
    public string BibId { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public ConfigurationChangeType ChangeType { get; set; }
    public bool IsValid { get; set; }
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Configuration added event arguments
/// </summary>
public class ConfigurationAddedEventArgs : EventArgs
{
    public string BibId { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public bool IsValid { get; set; }
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Configuration removed event arguments
/// </summary>
public class ConfigurationRemovedEventArgs : EventArgs
{
    public string BibId { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Configuration error event arguments
/// </summary>
public class ConfigurationErrorEventArgs : EventArgs
{
    public string BibId { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Configuration change types
/// </summary>
public enum ConfigurationChangeType
{
    Modified,
    Added,
    Removed,
    Renamed
}