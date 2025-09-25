// ===================================================================
// SPRINT 12 FIX: BibUutLogger Path Alignment
// File: SerialPortPool.Core/Services/BibUutLogger.cs
// Purpose: Fix path inconsistency with existing NLog configuration
// Issue: BibUutLogger used C:\ProgramData\ but NLog uses C:\Logs\
// ===================================================================

using Microsoft.Extensions.Logging;

namespace SerialPortPool.Core.Services;

/// <summary>
/// SPRINT 12 FIXED: Enhanced logger providing BIB/UUT specific logging
/// PATH FIX: Now aligned with existing NLog configuration path
/// ZERO TOUCH: Composes with existing ILogger without modification
/// </summary>
public class BibUutLogger
{
    private readonly ILogger _serviceLogger;
    private readonly string _bibId;
    private readonly string _uutId;
    private readonly int _portNumber;
    private readonly string _logDirectory;  // CHANGÉ: Plus "readonly" car peut être string.Empty
    private readonly object _lockObject = new();

    // NOUVELLES PROPRIÉTÉS AJOUTÉES pour tracking des erreurs
    private int _consecutiveWriteFailures = 0;
    private DateTime _lastSuccessfulWrite = DateTime.Now;
    private DateTime _lastWarningTime = DateTime.MinValue;
    private static readonly TimeSpan WARNING_INTERVAL = TimeSpan.FromMinutes(5);

    // CONSTRUCTEUR MODIFIÉ
    public BibUutLogger(ILogger serviceLogger, string bibId, string uutId, int portNumber)
    {
        _serviceLogger = serviceLogger;
        _bibId = bibId;
        _uutId = uutId;
        _portNumber = portNumber;
        
        // CHANGÉ: Try to create log directory, but handle failure gracefully
        _logDirectory = CreateLogDirectory(bibId) ?? string.Empty;
        
        if (string.IsNullOrEmpty(_logDirectory))
        {
            // Log the fallback situation once at startup
            Console.WriteLine($"ℹ️  BibUutLogger: Using service log fallback for {bibId}/{uutId} (structured logging unavailable)");
            _serviceLogger.LogWarning(
                "BibUutLogger: Structured logging unavailable for {BibId}/{UutId} - using service log fallback",
                bibId, uutId);
        }
    }

    /// <summary>
    /// Log BIB execution event with structured output
    /// FIXED: Corrected string formatting compatibility issue
    /// DUAL OUTPUT: Service log (existing) + BIB-specific log (new)
    /// </summary>
    public void LogBibExecution(LogLevel level, string message, params object[] args)
    {
        string formattedMessage;
        
        try
        {
            // SPRINT 12 FIX: Handle both indexed ({0}) and named ({ClientId}) format strings
            if (args.Any())
            {
                // Try Microsoft.Extensions.Logging format first (supports named placeholders)
                // The service logger will handle the actual formatting properly
                formattedMessage = message; // Keep original for service logger
                
                // For our local logging, create a safe version
                // Replace named placeholders with indexed ones for string.Format compatibility
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
            // DEFENSIVE: If formatting fails, log the error and use original message
            _serviceLogger.LogError(ex, "Format error in BibUutLogger for {BibId}/{UutId}: {Message}", _bibId, _uutId, message);
            formattedMessage = $"{message} [FORMAT_ERROR: {string.Join(", ", args)}]";
        }
        
        var timestamp = DateTime.Now;
        
        // ✅ EXISTING: Continue using service logger (ZERO TOUCH) 
        // Service logger handles named placeholders correctly
        _serviceLogger.Log(level, "[{BibId}/{UutId}] " + message, _bibId, _uutId, args);
        
        // 🆕 NEW: Add structured BIB-specific logging with safe formatted message
        WriteStructuredBibLog(level, formattedMessage, timestamp);
    }

    /// <summary>
    /// SPRINT 12 FIX: Convert named placeholders to indexed format for string.Format
    /// Handles common patterns like {ClientId} -> {0}, {Error} -> {1}, etc.
    /// </summary>
    private string ConvertToIndexedFormat(string message, int argCount)
    {
        var result = message;
        
        // Common named placeholders to indexed conversion
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
                break; // Only replace the first pattern found
            }
        }
        
        // Fallback: If no known patterns, assume it's meant to be {0}
        if (result.Contains("{") && !result.Contains("{0}") && argCount > 0)
        {
            // Find all {name} patterns and replace with {0}, {1}, etc.
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
        
        // Write daily summary entry
        WriteDailySummaryEntry(success, totalDuration, commandCount);
    }

// <summary>
/// ROBUSTNESS FIX: Create structured log directory with fallback protection
/// FAIL SAFE: Returns null if directory creation fails, enables fallback logging
/// </summary>
private static string? CreateLogDirectory(string bibId)
{
    try
    {
        // Use same base path as NLog configuration
        var baseLogPath = Path.Combine("C:", "Logs", "SerialPortPool");
        var bibLogPath = Path.Combine(baseLogPath, $"BIB_{bibId}", DateTime.Now.ToString("yyyy-MM-dd"));
        
        Directory.CreateDirectory(bibLogPath);
        
        // Test write permissions
        var testFile = Path.Combine(bibLogPath, $"test_{DateTime.Now:HHmmss}.tmp");
        File.WriteAllText(testFile, "test");
        File.Delete(testFile);
        
        return bibLogPath;
    }
    catch (UnauthorizedAccessException ex)
    {
        Console.WriteLine($"⚠️  BibUutLogger: Access denied to log directory for BIB {bibId}: {ex.Message}");
        Console.WriteLine($"🔄 Falling back to main service logs only");
        return null;
    }
    catch (DirectoryNotFoundException ex)
    {
        Console.WriteLine($"⚠️  BibUutLogger: Directory not found error for BIB {bibId}: {ex.Message}");
        Console.WriteLine($"🔄 Falling back to main service logs only");
        return null;
    }
    catch (IOException ex)
    {
        Console.WriteLine($"⚠️  BibUutLogger: IO error creating log directory for BIB {bibId}: {ex.Message}");
        Console.WriteLine($"🔄 Falling back to main service logs only (disk full?)");
        return null;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"⚠️  BibUutLogger: Unexpected error creating log directory for BIB {bibId}: {ex.Message}");
        Console.WriteLine($"🔄 Falling back to main service logs only");
        return null;
    }
}

    // <summary>
/// ROBUSTNESS FIX: Write structured BIB log with runtime failure detection
/// FAIL SAFE: Falls back to service logger + periodic warnings if file logging fails
/// </summary>
private void WriteStructuredBibLog(LogLevel level, string message, DateTime timestamp)
{
    // If no log directory available, skip structured logging silently
    if (string.IsNullOrEmpty(_logDirectory))
    {
        LogStructuredLoggingWarning();
        return;
    }

    try
    {
        var logFileName = $"{_uutId}_port{_portNumber}_{timestamp:HHmm}.log";
        var logFilePath = Path.Combine(_logDirectory, logFileName);
        
        var logEntry = $"[{timestamp:HH:mm:ss.fff}] [{level.ToString().ToUpper()}] {message}\n";
        
        lock (_lockObject)
        {
            File.AppendAllText(logFilePath, logEntry);
        }
        
        // Reset failure tracking on success
        _consecutiveWriteFailures = 0;
        _lastSuccessfulWrite = DateTime.Now;
    }
    catch (UnauthorizedAccessException ex)
    {
        HandleWriteFailure($"Access denied to log file: {ex.Message}");
    }
    catch (DirectoryNotFoundException ex)
    {
        HandleWriteFailure($"Log directory disappeared: {ex.Message}");
    }
    catch (IOException ex) when (ex.Message.Contains("disk") || ex.Message.Contains("space"))
    {
        HandleWriteFailure($"Disk space issue: {ex.Message}");
    }
    catch (IOException ex)
    {
        HandleWriteFailure($"IO error: {ex.Message}");
    }
    catch (Exception ex)
    {
        HandleWriteFailure($"Unexpected error: {ex.Message}");
    }
}


/// <summary>
/// NOUVELLE MÉTHODE: Handle write failures with periodic warnings
/// </summary>
private void HandleWriteFailure(string errorMessage)
{
    _consecutiveWriteFailures++;
    
    // Show warning on console and service logger periodically
    var now = DateTime.Now;
    if (now - _lastWarningTime > WARNING_INTERVAL)
    {
        var timeSinceSuccess = now - _lastSuccessfulWrite;
        
        Console.WriteLine($"⚠️  BibUutLogger: Structured logging failing for {_bibId}/{_uutId}");
        Console.WriteLine($"    Reason: {errorMessage}");
        Console.WriteLine($"    Failures: {_consecutiveWriteFailures} consecutive");
        Console.WriteLine($"    Time since last success: {timeSinceSuccess:mm\\:ss}");
        Console.WriteLine($"    Using main service logs as fallback");
        
        _serviceLogger.LogWarning(
            "BibUutLogger structured logging failed for {BibId}/{UutId}: {Error}. " +
            "Consecutive failures: {Failures}. Using service log fallback.",
            _bibId, _uutId, errorMessage, _consecutiveWriteFailures);
        
        _lastWarningTime = now;
    }
}

/// <summary>
/// NOUVELLE MÉTHODE: Periodic warning for missing structured logging capability
/// </summary>
private void LogStructuredLoggingWarning()
{
    var now = DateTime.Now;
    if (now - _lastWarningTime > WARNING_INTERVAL)
    {
        Console.WriteLine($"⚠️  BibUutLogger: No structured logging available for {_bibId}/{_uutId} (directory creation failed at startup)");
        Console.WriteLine($"    All logs going to main service log only");
        
        _serviceLogger.LogWarning(
            "BibUutLogger: Structured logging unavailable for {BibId}/{UutId} - directory creation failed at startup",
            _bibId, _uutId);
            
        _lastWarningTime = now;
    }
}


   // <summary>
/// ROBUSTNESS FIX: Write daily summary with failure handling
/// </summary>
private void WriteDailySummaryEntry(bool success, TimeSpan duration, int commandCount)
{
    if (string.IsNullOrEmpty(_logDirectory))
    {
        // No structured logging available - log to service logger instead
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
        // Fallback to service logger for daily summary
        _serviceLogger.LogWarning(ex, 
            "Failed to write daily summary to structured log for {BibId}/{UutId} - using service log instead",
            _bibId, _uutId);
            
        _serviceLogger.LogInformation(
            "DAILY SUMMARY {BibId}/{UutId}: {Status} - {Commands} commands, {Duration:F1}s",
            _bibId, _uutId, success ? "SUCCESS" : "FAILED", commandCount, duration.TotalSeconds);
    }
}


/// <summary>
/// ROBUSTNESS FIX: Create execution marker with failure handling
/// </summary>
public void CreateCurrentExecutionMarker()
{
    if (string.IsNullOrEmpty(_logDirectory))
    {
        // Log to service instead
        _serviceLogger.LogInformation(
            "Current execution started for {BibId}/{UutId} on port {Port} (no structured logging available)",
            _bibId, _uutId, _portNumber);
        return;
    }

    try
    {
        var latestDir = Path.Combine(Path.GetDirectoryName(_logDirectory)!, "latest");
        Directory.CreateDirectory(latestDir);
        
        var currentFile = Path.Combine(latestDir, $"{_uutId}_current.log");
        var markerContent = $"Current execution started: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n" +
                           $"Log directory: {_logDirectory}\n" +
                           $"BIB: {_bibId}, UUT: {_uutId}, Port: {_portNumber}\n";
        
        File.WriteAllText(currentFile, markerContent);
    }
    catch (Exception ex)
    {
        // Not critical - just log the issue
        _serviceLogger.LogDebug(ex, 
            "Could not create execution marker for {BibId}/{UutId} (not critical)", 
            _bibId, _uutId);
    }
}
    /// <summary>
    /// Factory method to create BibUutLogger from workflow context
    /// </summary>
    public static BibUutLogger Create(ILogger serviceLogger, string bibId, string uutId, int portNumber)
    {
        var logger = new BibUutLogger(serviceLogger, bibId, uutId, portNumber);
        logger.CreateCurrentExecutionMarker();
        return logger;
    }
}

/// <summary>
/// Extension methods for easy integration with existing workflow code
/// </summary>
public static class BibUutLoggerExtensions
{
    /// <summary>
    /// Create BIB/UUT logger from existing service logger
    /// INTEGRATION: Easy to add to existing workflow without changes
    /// </summary>
    public static BibUutLogger ForBibUut(this ILogger logger, string bibId, string uutId, int portNumber)
    {
        return BibUutLogger.Create(logger, bibId, uutId, portNumber);
    }
}