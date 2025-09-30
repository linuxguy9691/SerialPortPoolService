// ===================================================================
// SPRINT 12 ENHANCED: BibUutLogger with Stable Filenames
// File: SerialPortPool.Core/Services/BibUutLogger.cs
// FIX: Stable daily log files + robust validation like Program.cs
// ===================================================================

using Microsoft.Extensions.Logging;

namespace SerialPortPool.Core.Services;

/// <summary>
/// SPRINT 12 ENHANCED: Logger with stable daily filenames and robust validation
/// IMPROVEMENT: One file per day per UUT (not per hour)
/// ROBUSTNESS: Explicit validation and fail-safe fallback
/// </summary>
public class BibUutLogger
{
    private readonly ILogger _serviceLogger;
    private readonly string _bibId;
    private readonly string _uutId;
    private readonly int _portNumber;
    private readonly string? _logDirectory;
    private readonly string? _dailyLogFile;
    private readonly object _lockObject = new();
    
    // Validation and error tracking
    private readonly bool _structuredLoggingAvailable;
    private int _consecutiveWriteFailures = 0;
    private DateTime _lastSuccessfulWrite = DateTime.Now;
    private DateTime _lastWarningTime = DateTime.MinValue;
    private static readonly TimeSpan WARNING_INTERVAL = TimeSpan.FromMinutes(5);
    
    // Validation status
    private readonly List<string> _initializationErrors = new();
    private readonly LoggingStatus _status;

    public BibUutLogger(ILogger serviceLogger, string bibId, string uutId, int portNumber)
    {
        _serviceLogger = serviceLogger;
        _bibId = bibId;
        _uutId = uutId;
        _portNumber = portNumber;
        
        // Perform robust validation and setup
        (_logDirectory, _dailyLogFile, _status) = ValidateAndInitializeStructuredLogging(bibId, uutId, portNumber);
        _structuredLoggingAvailable = _status == LoggingStatus.Optimal || _status == LoggingStatus.Degraded;
        
        // Log initialization status
        LogInitializationStatus();
    }

    /// <summary>
    /// NOUVEAU: Robust validation inspired by Program.cs ValidateAndConfigureLogging
    /// Returns: (logDirectory, dailyLogFile, status)
    /// </summary>
    private (string?, string?, LoggingStatus) ValidateAndInitializeStructuredLogging(
        string bibId, string uutId, int portNumber)
    {
        var errors = new List<string>();
        string? logDirectory = null;
        string? dailyLogFile = null;
        
        try
        {
            // STEP 1: Validate base log path
            var baseLogPath = Path.Combine("C:", "Logs", "SerialPortPool");
            if (!ValidateBasePath(baseLogPath, errors))
            {
                return (null, null, LoggingStatus.Failed);
            }
            
            // STEP 2: Create and validate BIB directory
            var bibLogPath = Path.Combine(baseLogPath, $"BIB_{bibId}");
            if (!ValidateAndCreateDirectory(bibLogPath, "BIB directory", errors))
            {
                return (null, null, LoggingStatus.Failed);
            }
            
            // STEP 3: Create and validate daily directory
            var dailyPath = Path.Combine(bibLogPath, DateTime.Now.ToString("yyyy-MM-dd"));
            if (!ValidateAndCreateDirectory(dailyPath, "daily directory", errors))
            {
                return (null, null, LoggingStatus.Degraded);
            }
            
            // STEP 4: Generate stable daily filename (NO timestamp in name)
            var stableFileName = $"{uutId}_port{portNumber}.log";
            var fullLogPath = Path.Combine(dailyPath, stableFileName);
            
            // STEP 5: Validate write permissions
            if (!ValidateWritePermissions(fullLogPath, errors))
            {
                return (dailyPath, fullLogPath, LoggingStatus.Degraded);
            }
            
            // SUCCESS: All validations passed
            logDirectory = dailyPath;
            dailyLogFile = fullLogPath;
            return (logDirectory, dailyLogFile, LoggingStatus.Optimal);
        }
        catch (Exception ex)
        {
            errors.Add($"Unexpected initialization error: {ex.Message}");
            _initializationErrors.AddRange(errors);
            return (null, null, LoggingStatus.Failed);
        }
        finally
        {
            _initializationErrors.AddRange(errors);
        }
    }

    /// <summary>
    /// NOUVEAU: Validate base logging path exists and is accessible
    /// </summary>
    private bool ValidateBasePath(string basePath, List<string> errors)
    {
        try
        {
            if (!Directory.Exists(basePath))
            {
                Directory.CreateDirectory(basePath);
            }
            return true;
        }
        catch (UnauthorizedAccessException ex)
        {
            errors.Add($"Access denied to base log path {basePath}: {ex.Message}");
            return false;
        }
        catch (Exception ex)
        {
            errors.Add($"Cannot access base log path {basePath}: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// NOUVEAU: Validate and create directory with clear error reporting
    /// </summary>
    private bool ValidateAndCreateDirectory(string path, string description, List<string> errors)
    {
        try
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            return true;
        }
        catch (UnauthorizedAccessException ex)
        {
            errors.Add($"Access denied creating {description} at {path}: {ex.Message}");
            return false;
        }
        catch (IOException ex)
        {
            errors.Add($"IO error creating {description} at {path}: {ex.Message}");
            return false;
        }
        catch (Exception ex)
        {
            errors.Add($"Unexpected error creating {description} at {path}: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// NOUVEAU: Validate write permissions with test file
    /// </summary>
    private bool ValidateWritePermissions(string logFilePath, List<string> errors)
    {
        try
        {
            // Test write by appending a validation marker
            var testContent = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Log file initialized\n";
            File.AppendAllText(logFilePath, testContent);
            return true;
        }
        catch (UnauthorizedAccessException ex)
        {
            errors.Add($"No write permission for {logFilePath}: {ex.Message}");
            return false;
        }
        catch (IOException ex) when (ex.Message.Contains("disk") || ex.Message.Contains("space"))
        {
            errors.Add($"Disk space issue for {logFilePath}: {ex.Message}");
            return false;
        }
        catch (Exception ex)
        {
            errors.Add($"Cannot write to {logFilePath}: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// NOUVEAU: Log initialization status clearly (like Program.cs)
    /// </summary>
    private void LogInitializationStatus()
    {
        var statusLine = "=".PadRight(70, '=');
        
        switch (_status)
        {
            case LoggingStatus.Optimal:
                Console.WriteLine($"✅ BibUutLogger OPTIMAL: {_bibId}/{_uutId}");
                Console.WriteLine($"   📁 Log file: {Path.GetFileName(_dailyLogFile)}");
                Console.WriteLine($"   📂 Directory: {_logDirectory}");
                
                _serviceLogger.LogInformation(
                    "BibUutLogger initialized: {BibId}/{UutId} → {LogFile}", 
                    _bibId, _uutId, _dailyLogFile);
                break;
                
            case LoggingStatus.Degraded:
                Console.WriteLine($"⚠️  BibUutLogger DEGRADED: {_bibId}/{_uutId}");
                Console.WriteLine($"   🔄 Using service log fallback");
                Console.WriteLine($"   📋 Issues:");
                foreach (var error in _initializationErrors)
                {
                    Console.WriteLine($"      • {error}");
                }
                
                _serviceLogger.LogWarning(
                    "BibUutLogger degraded mode for {BibId}/{UutId}: {Errors}",
                    _bibId, _uutId, string.Join("; ", _initializationErrors));
                break;
                
            case LoggingStatus.Failed:
                Console.WriteLine($"❌ BibUutLogger FAILED: {_bibId}/{_uutId}");
                Console.WriteLine($"   🔄 Service log only");
                Console.WriteLine($"   💥 Critical issues:");
                foreach (var error in _initializationErrors)
                {
                    Console.WriteLine($"      • {error}");
                }
                
                _serviceLogger.LogError(
                    "BibUutLogger initialization failed for {BibId}/{UutId}: {Errors}",
                    _bibId, _uutId, string.Join("; ", _initializationErrors));
                break;
        }
    }

    /// <summary>
    /// Log BIB execution event with STABLE daily file
    /// DUAL OUTPUT: Service log (always) + BIB-specific log (when available)
    /// </summary>
    public void LogBibExecution(LogLevel level, string message, params object[] args)
    {
        string formattedMessage;
        
        try
        {
            if (args.Any())
            {
                formattedMessage = message;
                var safeMessage = ConvertToIndexedFormat(message, args.Length);
                formattedMessage = string.Format(safeMessage, args);
            }
            else
            {
                formattedMessage = message;
            }
        }
        catch (FormatException ex)
        {
            _serviceLogger.LogError(ex, 
                "Format error in BibUutLogger for {BibId}/{UutId}: {Message}", 
                _bibId, _uutId, message);
            formattedMessage = $"{message} [FORMAT_ERROR: {string.Join(", ", args)}]";
        }
        
        var timestamp = DateTime.Now;
        
        // ALWAYS log to service logger
        _serviceLogger.Log(level, "[{BibId}/{UutId}] " + message, _bibId, _uutId, args);
        
        // Write to structured log if available
        if (_structuredLoggingAvailable)
        {
            WriteStructuredBibLog(level, formattedMessage, timestamp);
        }
    }

    /// <summary>
    /// Convert named placeholders to indexed format for string.Format
    /// </summary>
    private string ConvertToIndexedFormat(string message, int argCount)
    {
        var result = message;
        
        var commonPatterns = new Dictionary<string, int>
        {
            {"{ClientId}", 0},
            {"{Error}", 0},
            {"{Message}", 0},
            {"{Duration}", 0},
            {"{Command}", 0},
            {"{Response}", 0},
            {"{Phase}", 0}
        };
        
        var index = 0;
        foreach (var pattern in commonPatterns.Keys)
        {
            if (result.Contains(pattern) && index < argCount)
            {
                result = result.Replace(pattern, $"{{{index}}}");
                index++;
                break;
            }
        }
        
        if (result.Contains("{") && !result.Contains("{0}") && argCount > 0)
        {
            var regex = new System.Text.RegularExpressions.Regex(@"\{[^}]+\}");
            var matches = regex.Matches(result);
            
            for (int i = 0; i < matches.Count && i < argCount; i++)
            {
                result = result.Replace(matches[i].Value, $"{{{i}}}");
            }
        }
        
        return result;
    }

    /// <summary>
    /// FIX: Write to STABLE daily file (not hourly files)
    /// </summary>
    private void WriteStructuredBibLog(LogLevel level, string message, DateTime timestamp)
    {
        if (!_structuredLoggingAvailable || string.IsNullOrEmpty(_dailyLogFile))
        {
            LogStructuredLoggingWarning();
            return;
        }

        try
        {
            var logEntry = $"[{timestamp:HH:mm:ss.fff}] [{level.ToString().ToUpper()}] {message}\n";
            
            lock (_lockObject)
            {
                File.AppendAllText(_dailyLogFile, logEntry);
            }
            
            _consecutiveWriteFailures = 0;
            _lastSuccessfulWrite = DateTime.Now;
        }
        catch (UnauthorizedAccessException ex)
        {
            HandleWriteFailure($"Access denied: {ex.Message}");
        }
        catch (IOException ex) when (ex.Message.Contains("disk") || ex.Message.Contains("space"))
        {
            HandleWriteFailure($"Disk full: {ex.Message}");
        }
        catch (Exception ex)
        {
            HandleWriteFailure($"Write error: {ex.Message}");
        }
    }

    /// <summary>
    /// Handle write failures with periodic warnings
    /// </summary>
    private void HandleWriteFailure(string errorMessage)
    {
        _consecutiveWriteFailures++;
        
        var now = DateTime.Now;
        if (now - _lastWarningTime > WARNING_INTERVAL)
        {
            var timeSinceSuccess = now - _lastSuccessfulWrite;
            
            Console.WriteLine($"⚠️  BibUutLogger write failure: {_bibId}/{_uutId}");
            Console.WriteLine($"    Reason: {errorMessage}");
            Console.WriteLine($"    Failures: {_consecutiveWriteFailures} consecutive");
            Console.WriteLine($"    Time since success: {timeSinceSuccess:mm\\:ss}");
            
            _serviceLogger.LogWarning(
                "Structured logging failed for {BibId}/{UutId}: {Error}. Failures: {Count}",
                _bibId, _uutId, errorMessage, _consecutiveWriteFailures);
            
            _lastWarningTime = now;
        }
    }

    /// <summary>
    /// Periodic warning for unavailable structured logging
    /// </summary>
    private void LogStructuredLoggingWarning()
    {
        var now = DateTime.Now;
        if (now - _lastWarningTime > WARNING_INTERVAL)
        {
            _serviceLogger.LogWarning(
                "Structured logging unavailable for {BibId}/{UutId}",
                _bibId, _uutId);
            _lastWarningTime = now;
        }
    }

    /// <summary>
    /// Log workflow phase with enhanced context
    /// </summary>
    public void LogWorkflowPhase(string phase, string command, string? response, LogLevel level, TimeSpan duration)
    {
        var message = response != null 
            ? $"{phase}: {command} → {response} [{duration.TotalMilliseconds:F0}ms]"
            : $"{phase}: {command} [{duration.TotalMilliseconds:F0}ms]";
            
        LogBibExecution(level, message);
    }

    /// <summary>
    /// Log workflow completion with summary
    /// </summary>
    public void LogWorkflowSummary(bool success, TimeSpan totalDuration, int commandCount)
    {
        var status = success ? "✅ SUCCESS" : "❌ FAILED";
        var summary = $"WORKFLOW COMPLETE: {status} - {commandCount} commands in {totalDuration.TotalSeconds:F1}s";
        
        LogBibExecution(success ? LogLevel.Information : LogLevel.Error, summary);
        WriteDailySummaryEntry(success, totalDuration, commandCount);
    }

    /// <summary>
    /// Write daily summary entry
    /// </summary>
    private void WriteDailySummaryEntry(bool success, TimeSpan duration, int commandCount)
    {
        if (!_structuredLoggingAvailable || string.IsNullOrEmpty(_logDirectory))
        {
            _serviceLogger.LogInformation(
                "DAILY SUMMARY {BibId}/{UutId}: {Status} - {Commands} commands, {Duration:F1}s",
                _bibId, _uutId, success ? "SUCCESS" : "FAILED", commandCount, duration.TotalSeconds);
            return;
        }

        try
        {
            var summaryFile = Path.Combine(_logDirectory, $"daily_summary_{DateTime.Now:yyyy-MM-dd}.log");
            var summaryEntry = $"[{DateTime.Now:HH:mm:ss}] {_uutId}_port{_portNumber}: " +
                             $"{(success ? "SUCCESS" : "FAILED")} - {commandCount} cmds, {duration.TotalSeconds:F1}s\n";
            
            lock (_lockObject)
            {
                File.AppendAllText(summaryFile, summaryEntry);
            }
        }
        catch (Exception ex)
        {
            _serviceLogger.LogWarning(ex, 
                "Failed to write summary for {BibId}/{UutId}",
                _bibId, _uutId);
        }
    }

    /// <summary>
    /// Create execution marker for "latest" tracking
    /// </summary>
    public void CreateCurrentExecutionMarker()
    {
        if (!_structuredLoggingAvailable || string.IsNullOrEmpty(_logDirectory))
        {
            return;
        }

        try
        {
            var latestDir = Path.Combine(Path.GetDirectoryName(_logDirectory)!, "latest");
            Directory.CreateDirectory(latestDir);
            
            var currentFile = Path.Combine(latestDir, $"{_uutId}_current.log");
            var markerContent = $"Current execution: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n" +
                               $"Log file: {_dailyLogFile}\n" +
                               $"BIB: {_bibId}, UUT: {_uutId}, Port: {_portNumber}\n";
            
            File.WriteAllText(currentFile, markerContent);
        }
        catch (Exception ex)
        {
            _serviceLogger.LogDebug(ex, 
                "Could not create execution marker for {BibId}/{UutId}", 
                _bibId, _uutId);
        }
    }

    /// <summary>
    /// Factory method to create BibUutLogger
    /// </summary>
    public static BibUutLogger Create(ILogger serviceLogger, string bibId, string uutId, int portNumber)
    {
        var logger = new BibUutLogger(serviceLogger, bibId, uutId, portNumber);
        logger.CreateCurrentExecutionMarker();
        return logger;
    }
}

/// <summary>
/// Logging status enum for clear state tracking
/// </summary>
public enum LoggingStatus
{
    Optimal,    // File + Console logging working
    Degraded,   // Only console or only file working
    Failed      // No logging available
}

/// <summary>
/// Extension methods for easy integration
/// </summary>
public static class BibUutLoggerExtensions
{
    public static BibUutLogger ForBibUut(this ILogger logger, string bibId, string uutId, int portNumber)
    {
        return BibUutLogger.Create(logger, bibId, uutId, portNumber);
    }
}