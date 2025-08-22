// ===================================================================
// SPRINT 11: HotReloadConfigurationService Implementation
// File: SerialPortPool.Core/Services/Configuration/HotReloadConfigurationService.cs
// Purpose: File system monitoring + event-driven hot reload for BIB configurations
// ===================================================================

using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace SerialPortPool.Core.Services.Configuration;

/// <summary>
/// Service for monitoring configuration files and triggering hot reload events
/// Provides debounced file system monitoring with BIB-specific event handling
/// </summary>
public class HotReloadConfigurationService : IDisposable
{
    private readonly ILogger<HotReloadConfigurationService> _logger;
    private FileSystemWatcher? _fileWatcher;
    private readonly Dictionary<string, DateTime> _pendingChanges = new();
    private readonly Timer _debounceTimer;
    private readonly object _lockObject = new();
    private readonly SemaphoreSlim _processingLock = new(1, 1);
    
    private bool _isDisposed = false;
    private DateTime _startTime;
    private int _totalEventsProcessed = 0;
    private int _configurationChangedEvents = 0;
    private int _configurationErrorEvents = 0;
    
    // Configuration
    private const int DebounceDelayMs = 500;
    private const string BibFilePattern = "*.xml";

    #region Properties

    public bool IsMonitoring { get; private set; } = false;
    public string? MonitoredDirectory { get; private set; }

    #endregion

    #region Events

    public event EventHandler<ConfigurationChangedEventArgs>? ConfigurationChanged;
    public event EventHandler<ConfigurationErrorEventArgs>? ConfigurationError;

    #endregion

    #region Constructor

    public HotReloadConfigurationService(ILogger<HotReloadConfigurationService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        // Create debounce timer (initially disabled)
        _debounceTimer = new Timer(ProcessPendingChanges, null, Timeout.Infinite, Timeout.Infinite);
        
        _logger.LogDebug("🔥 HotReloadConfigurationService initialized");
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Start monitoring a directory for configuration changes
    /// </summary>
    public bool StartMonitoring(string directory)
    {
        if (string.IsNullOrWhiteSpace(directory))
            throw new ArgumentException("Directory path cannot be null or empty", nameof(directory));

        lock (_lockObject)
        {
            try
            {
                // Stop current monitoring if active
                if (IsMonitoring)
                {
                    StopMonitoring();
                }

                if (!Directory.Exists(directory))
                {
                    _logger.LogWarning("⚠️ Directory does not exist: {Directory}", directory);
                    return false;
                }

                _logger.LogInformation("🔥 Starting hot reload monitoring: {Directory}", directory);

                // Configure file system watcher
                _fileWatcher = new FileSystemWatcher(directory, BibFilePattern)
                {
                    NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.CreationTime,
                    IncludeSubdirectories = false,
                    EnableRaisingEvents = true
                };

                // Subscribe to events
                _fileWatcher.Changed += OnFileChanged;
                _fileWatcher.Created += OnFileChanged;
                _fileWatcher.Deleted += OnFileChanged;
                _fileWatcher.Renamed += OnFileRenamed;
                _fileWatcher.Error += OnWatcherError;

                MonitoredDirectory = directory;
                IsMonitoring = true;
                _startTime = DateTime.Now;
                
                _logger.LogInformation("✅ Hot reload monitoring started successfully");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to start monitoring: {Error}", ex.Message);
                return false;
            }
        }
    }

    /// <summary>
    /// Stop monitoring configuration changes
    /// </summary>
    public void StopMonitoring()
    {
        lock (_lockObject)
        {
            if (!IsMonitoring)
                return;

            _logger.LogInformation("🛑 Stopping hot reload monitoring");

            try
            {
                // Disable timer
                _debounceTimer.Change(Timeout.Infinite, Timeout.Infinite);

                // Dispose file watcher
                if (_fileWatcher != null)
                {
                    _fileWatcher.EnableRaisingEvents = false;
                    _fileWatcher.Changed -= OnFileChanged;
                    _fileWatcher.Created -= OnFileChanged;
                    _fileWatcher.Deleted -= OnFileChanged;
                    _fileWatcher.Renamed -= OnFileRenamed;
                    _fileWatcher.Error -= OnWatcherError;
                    _fileWatcher.Dispose();
                    _fileWatcher = null;
                }

                // Clear pending changes
                _pendingChanges.Clear();

                IsMonitoring = false;
                MonitoredDirectory = null;
                
                _logger.LogInformation("✅ Hot reload monitoring stopped");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "⚠️ Warning while stopping monitoring: {Error}", ex.Message);
            }
        }
    }

    /// <summary>
    /// Get monitoring statistics
    /// </summary>
    public MonitoringStatistics GetMonitoringStatistics()
    {
        lock (_lockObject)
        {
            return new MonitoringStatistics
            {
                IsMonitoring = IsMonitoring,
                MonitoredDirectory = MonitoredDirectory,
                TotalEventsProcessed = _totalEventsProcessed,
                MonitoringDuration = IsMonitoring ? DateTime.Now - _startTime : TimeSpan.Zero,
                StartTime = IsMonitoring ? _startTime : null,
                ConfigurationChangedEvents = _configurationChangedEvents,
                ConfigurationErrorEvents = _configurationErrorEvents
            };
        }
    }

    #endregion

    #region File System Event Handlers

    private void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        try
        {
            if (!IsXmlConfigurationFile(e.FullPath))
                return;

            _logger.LogDebug("📁 File change detected: {File} ({ChangeType})", e.Name, e.ChangeType);

            lock (_lockObject)
            {
                // Add to pending changes with current timestamp
                _pendingChanges[e.FullPath] = DateTime.Now;
                
                // Reset debounce timer
                _debounceTimer.Change(DebounceDelayMs, Timeout.Infinite);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "⚠️ Error processing file change event: {Error}", ex.Message);
        }
    }

    private void OnFileRenamed(object sender, RenamedEventArgs e)
    {
        try
        {
            if (!IsXmlConfigurationFile(e.FullPath))
                return;

            _logger.LogDebug("📁 File renamed: {OldName} → {NewName}", e.OldName, e.Name);

            lock (_lockObject)
            {
                _pendingChanges[e.FullPath] = DateTime.Now;
                _debounceTimer.Change(DebounceDelayMs, Timeout.Infinite);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "⚠️ Error processing file rename event: {Error}", ex.Message);
        }
    }

    private void OnWatcherError(object sender, ErrorEventArgs e)
    {
        _logger.LogError(e.GetException(), "❌ File system watcher error");
        
        // Try to recover by restarting monitoring
        var currentDirectory = MonitoredDirectory;
        if (!string.IsNullOrEmpty(currentDirectory))
        {
            _logger.LogInformation("🔄 Attempting to restart monitoring after error...");
            Task.Run(async () =>
            {
                await Task.Delay(2000); // Brief delay before restart
                StartMonitoring(currentDirectory);
            });
        }
    }

    #endregion

    #region Debounced Processing

    private async void ProcessPendingChanges(object? state)
    {
        if (!_processingLock.Wait(100)) // Non-blocking attempt
            return;

        try
        {
            Dictionary<string, DateTime> changesToProcess;
            
            lock (_lockObject)
            {
                if (_pendingChanges.Count == 0)
                    return;

                // Copy and clear pending changes
                changesToProcess = new Dictionary<string, DateTime>(_pendingChanges);
                _pendingChanges.Clear();
            }

            _logger.LogDebug("⚡ Processing {Count} debounced file changes", changesToProcess.Count);

            foreach (var change in changesToProcess)
            {
                await ProcessSingleFileChangeAsync(change.Key);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error processing pending changes: {Error}", ex.Message);
        }
        finally
        {
            _processingLock.Release();
        }
    }

    private async Task ProcessSingleFileChangeAsync(string filePath)
    {
        try
        {
            var fileName = Path.GetFileName(filePath);
            var bibId = ExtractBibIdFromFileName(fileName);
            
            _logger.LogDebug("🔄 Processing configuration change: {File} (BIB: {BibId})", fileName, bibId ?? "Unknown");

            // Validate file if it exists
            if (File.Exists(filePath))
            {
                await ValidateConfigurationFileAsync(filePath, bibId);
            }

            // Fire configuration changed event
            var changeArgs = new ConfigurationChangedEventArgs
            {
                FilePath = filePath,
                BibId = bibId,
                ChangeType = File.Exists(filePath) ? "Modified" : "Deleted",
                Timestamp = DateTime.Now
            };

            ConfigurationChanged?.Invoke(this, changeArgs);
            
            Interlocked.Increment(ref _totalEventsProcessed);
            Interlocked.Increment(ref _configurationChangedEvents);
            
            _logger.LogInformation("✅ Configuration change processed: {File}", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error processing file change: {File}", filePath);
            
            // Fire error event
            var errorArgs = new ConfigurationErrorEventArgs
            {
                FilePath = filePath,
                ErrorMessage = ex.Message,
                Exception = ex,
                Timestamp = DateTime.Now
            };

            ConfigurationError?.Invoke(this, errorArgs);
            
            Interlocked.Increment(ref _totalEventsProcessed);
            Interlocked.Increment(ref _configurationErrorEvents);
        }
    }

    #endregion

    #region Helper Methods

    private static bool IsXmlConfigurationFile(string filePath)
    {
        if (string.IsNullOrEmpty(filePath))
            return false;

        var fileName = Path.GetFileName(filePath);
        return fileName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase);
    }

    private static string? ExtractBibIdFromFileName(string fileName)
    {
        if (string.IsNullOrEmpty(fileName))
            return null;

        try
        {
            // Pattern: bib_<bibid>.xml or <bibid>.xml
            var match = Regex.Match(fileName, @"(?:bib_)?([a-zA-Z0-9_-]+)\.xml$", RegexOptions.IgnoreCase);
            return match.Success ? match.Groups[1].Value : null;
        }
        catch
        {
            return null;
        }
    }

    private async Task ValidateConfigurationFileAsync(string filePath, string? bibId)
    {
        try
        {
            // Basic XML validation - try to load it
            var content = await File.ReadAllTextAsync(filePath);
            
            if (string.IsNullOrWhiteSpace(content))
            {
                throw new InvalidDataException("Configuration file is empty");
            }

            // Basic XML structure check
            if (!content.TrimStart().StartsWith("<?xml") && !content.TrimStart().StartsWith("<"))
            {
                throw new InvalidDataException("File does not appear to be valid XML");
            }

            _logger.LogDebug("✅ Configuration file validation passed: {File}", Path.GetFileName(filePath));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "⚠️ Configuration file validation failed: {File}", Path.GetFileName(filePath));
            throw;
        }
    }

    #endregion

    #region IDisposable Implementation

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_isDisposed && disposing)
        {
            StopMonitoring();
            
            _debounceTimer?.Dispose();
            _processingLock?.Dispose();
            
            _logger.LogDebug("🧹 HotReloadConfigurationService disposed");
            _isDisposed = true;
        }
    }

    #endregion
}

/// <summary>
/// Event arguments for configuration change events
/// </summary>
public class ConfigurationChangedEventArgs : EventArgs
{
    public string FilePath { get; set; } = string.Empty;
    public string? BibId { get; set; }
    public string ChangeType { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.Now;
}

/// <summary>
/// Event arguments for configuration error events
/// </summary>
public class ConfigurationErrorEventArgs : EventArgs
{
    public string FilePath { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public Exception? Exception { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.Now;
}

/// <summary>
/// Statistics for monitoring operations
/// </summary>
public class MonitoringStatistics
{
    public bool IsMonitoring { get; set; }
    public string? MonitoredDirectory { get; set; }
    public int TotalEventsProcessed { get; set; }
    public TimeSpan MonitoringDuration { get; set; }
    public DateTime? StartTime { get; set; }
    public int ConfigurationChangedEvents { get; set; }
    public int ConfigurationErrorEvents { get; set; }
}