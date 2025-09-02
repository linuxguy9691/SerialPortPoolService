// ===================================================================
// SPRINT 12 FIX: BibUutLogger - Corrected Format String Issue
// File: SerialPortPool.Core/Services/BibUutLogger.cs
// Purpose: Fix regression in string formatting that broke XML interpretation
// ISSUE: Incompatible format strings between logging calls and string.Format
// ===================================================================

using Microsoft.Extensions.Logging;

namespace SerialPortPool.Core.Services;

/// <summary>
/// SPRINT 12 FIXED: Enhanced logger providing BIB/UUT specific logging
/// REGRESSION FIX: Corrected string formatting compatibility issue
/// ZERO TOUCH: Composes with existing ILogger without modification
/// </summary>
public class BibUutLogger
{
    private readonly ILogger _serviceLogger;
    private readonly string _bibId;
    private readonly string _uutId;
    private readonly int _portNumber;
    private readonly string _logDirectory;
    private readonly object _lockObject = new();

    public BibUutLogger(ILogger serviceLogger, string bibId, string uutId, int portNumber)
    {
        _serviceLogger = serviceLogger;
        _bibId = bibId;
        _uutId = uutId;
        _portNumber = portNumber;
        _logDirectory = CreateLogDirectory(bibId);
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

    /// <summary>
    /// Create structured log directory for BIB
    /// Structure: Logs/SerialPortPool/BIB_{bibId}/{date}/
    /// </summary>
    private static string CreateLogDirectory(string bibId)
    {
        var baseLogPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "SerialPortPool", "Logs");
            
        var bibLogPath = Path.Combine(baseLogPath, $"BIB_{bibId}", DateTime.Now.ToString("yyyy-MM-dd"));
        
        Directory.CreateDirectory(bibLogPath);
        
        return bibLogPath;
    }

    /// <summary>
    /// Write structured BIB-specific log entry
    /// </summary>
    private void WriteStructuredBibLog(LogLevel level, string message, DateTime timestamp)
    {
        try
        {
            var logFileName = $"{_uutId}_port{_portNumber}_{timestamp:HHmm}.log";
            var logFilePath = Path.Combine(_logDirectory, logFileName);
            
            var logEntry = $"[{timestamp:HH:mm:ss.fff}] [{level.ToString().ToUpper()}] {message}\n";
            
            lock (_lockObject)
            {
                File.AppendAllText(logFilePath, logEntry);
            }
        }
        catch (Exception ex)
        {
            // Fallback to service logger if structured logging fails
            _serviceLogger.LogError(ex, "Failed to write structured log for {BibId}/{UutId}", _bibId, _uutId);
        }
    }

    /// <summary>
    /// Write daily summary entry for reporting
    /// </summary>
    private void WriteDailySummaryEntry(bool success, TimeSpan duration, int commandCount)
    {
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
            _serviceLogger.LogError(ex, "Failed to write daily summary for {BibId}/{UutId}", _bibId, _uutId);
        }
    }

    /// <summary>
    /// Create current execution symlink/marker for latest logs
    /// </summary>
    public void CreateCurrentExecutionMarker()
    {
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
            _serviceLogger.LogError(ex, "Failed to create execution marker for {BibId}/{UutId}", _bibId, _uutId);
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