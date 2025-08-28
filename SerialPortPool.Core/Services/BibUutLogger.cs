// ===================================================================
// SPRINT 12: BibUutLogger - Enhanced Structured Logging
// File: SerialPortPool.Core/Services/BibUutLogger.cs
// Purpose: BIB/UUT specific logging with automatic file organization
// STRATEGY: ADDITIVE - compose with existing loggers, zero touch
// ===================================================================

using Microsoft.Extensions.Logging;

namespace SerialPortPool.Core.Services;

/// <summary>
/// SPRINT 12: Enhanced logger providing BIB/UUT specific logging
/// ZERO TOUCH: Composes with existing ILogger without modification
/// IMMEDIATE VALUE: Structured logs for better troubleshooting
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
    /// DUAL OUTPUT: Service log (existing) + BIB-specific log (new)
    /// </summary>
    public void LogBibExecution(LogLevel level, string message, params object[] args)
    {
        var formattedMessage = args.Any() ? string.Format(message, args) : message;
        var timestamp = DateTime.Now;
        
        // ✅ EXISTING: Continue using service logger (ZERO TOUCH)
        _serviceLogger.Log(level, "[{BibId}/{UutId}] {Message}", _bibId, _uutId, formattedMessage);
        
        // 🆕 NEW: Add structured BIB-specific logging
        WriteStructuredBibLog(level, formattedMessage, timestamp);
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