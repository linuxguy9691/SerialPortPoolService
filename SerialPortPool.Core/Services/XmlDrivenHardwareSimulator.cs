// ===================================================================
// SPRINT 13 BOUCHÉE #2 BONUS: XML-Driven Hardware Simulator Service
// File: SerialPortPool.Core/Services/XmlDrivenHardwareSimulator.cs
// Purpose: Hardware simulation engine driven by XML configuration
// Philosophy: "Smart Simulation" - Realistic behavior based on BIB config
// ===================================================================

using Microsoft.Extensions.Logging;
using SerialPortPool.Core.Interfaces;
using SerialPortPool.Core.Models;
using System.Text.RegularExpressions;

namespace SerialPortPool.Core.Services;

/// <summary>
/// SPRINT 13: XML-driven hardware simulation service
/// Provides realistic hardware simulation based on BIB configuration
/// Integrates with existing protocol handlers transparently
/// </summary>
public class XmlDrivenHardwareSimulator : IDisposable
{
    private readonly ILogger<XmlDrivenHardwareSimulator> _logger;
    private readonly Dictionary<string, SimulationSession> _activeSessions = new();
    private readonly Random _random = new();
    private bool _isDisposed;

    public XmlDrivenHardwareSimulator(ILogger<XmlDrivenHardwareSimulator> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _logger.LogInformation("🎬 XML-Driven Hardware Simulator initialized");
    }

    /// <summary>
    /// Start hardware simulation for a BIB configuration
    /// </summary>
    public async Task<SimulationSession> StartSimulationAsync(BibConfiguration bibConfig, string sessionId)
    {
        if (!bibConfig.IsHardwareSimulationEnabled)
        {
            throw new InvalidOperationException($"Hardware simulation is not enabled for BIB: {bibConfig.BibId}");
        }

        var session = new SimulationSession
        {
            SessionId = sessionId,
            BibId = bibConfig.BibId,
            BibConfiguration = bibConfig,
            HardwareConfig = bibConfig.HardwareSimulation!,
            StartTime = DateTime.Now,
            CurrentPhase = SimulationPhase.Initializing
        };

        _activeSessions[sessionId] = session;

        _logger.LogInformation("🎬 Starting hardware simulation: {BibId} (Session: {SessionId})", 
            bibConfig.BibId, sessionId);
        _logger.LogInformation("🛠️ Simulation Config: {SimulationSummary}", 
            bibConfig.HardwareSimulation.GetSimulationSummary());

        // Execute start trigger
        await ExecuteStartTriggerAsync(session);

        return session;
    }

    /// <summary>
    /// Simulate command execution based on XML configuration
    /// </summary>
    public async Task<SimulationResponse> SimulateCommandAsync(string sessionId, string command, 
        string? expectedResponse = null, int timeoutMs = 5000)
    {
        if (!_activeSessions.TryGetValue(sessionId, out var session))
        {
            throw new InvalidOperationException($"Simulation session not found: {sessionId}");
        }

        _logger.LogDebug("🎮 Simulating command: '{Command}' (Session: {SessionId})", command, sessionId);

        var response = new SimulationResponse
        {
            SessionId = sessionId,
            Command = command,
            StartTime = DateTime.Now
        };

        try
        {
            // Check for critical trigger activation
            if (session.HardwareConfig.CriticalTrigger.ShouldActivate())
            {
                return await HandleCriticalScenarioAsync(session, response);
            }

            // Determine simulation behavior based on command and configuration
            await DetermineSimulationBehaviorAsync(session, response, command, expectedResponse, timeoutMs);

            response.EndTime = DateTime.Now;
            response.Duration = response.EndTime - response.StartTime;

            _logger.LogDebug("🎮 Command simulation completed: '{Command}' → '{Response}' ({Duration}ms)", 
                command, response.Response, response.Duration.TotalMilliseconds);

            return response;
        }
        catch (Exception ex)
        {
            response.Success = false;
            response.ErrorMessage = ex.Message;
            response.EndTime = DateTime.Now;
            response.Duration = response.EndTime - response.StartTime;

            _logger.LogError(ex, "❌ Command simulation failed: '{Command}' (Session: {SessionId})", command, sessionId);
            return response;
        }
    }

    /// <summary>
    /// Stop hardware simulation
    /// </summary>
    public async Task<bool> StopSimulationAsync(string sessionId)
    {
        if (!_activeSessions.TryGetValue(sessionId, out var session))
        {
            _logger.LogWarning("⚠️ Attempted to stop non-existent simulation session: {SessionId}", sessionId);
            return false;
        }

        _logger.LogInformation("🛑 Stopping hardware simulation: {BibId} (Session: {SessionId})", 
            session.BibId, sessionId);

        try
        {
            // Execute stop trigger
            await ExecuteStopTriggerAsync(session);

            session.CurrentPhase = SimulationPhase.Stopped;
            session.EndTime = DateTime.Now;

            _activeSessions.Remove(sessionId);

            _logger.LogInformation("✅ Hardware simulation stopped successfully: {SessionId} (Duration: {Duration})", 
                sessionId, session.Duration.TotalSeconds);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error stopping hardware simulation: {SessionId}", sessionId);
            return false;
        }
    }

    /// <summary>
    /// Get simulation session statistics
    /// </summary>
    public SimulationStatistics? GetSessionStatistics(string sessionId)
    {
        if (!_activeSessions.TryGetValue(sessionId, out var session))
            return null;

        return new SimulationStatistics
        {
            SessionId = sessionId,
            BibId = session.BibId,
            IsActive = session.CurrentPhase != SimulationPhase.Stopped,
            CurrentPhase = session.CurrentPhase,
            Duration = session.Duration,
            CommandsSimulated = session.CommandsSimulated,
            SuccessfulCommands = session.SuccessfulCommands,
            FailedCommands = session.FailedCommands,
            AverageCommandDuration = session.AverageCommandDuration
        };
    }

    /// <summary>
    /// Get all active simulation sessions
    /// </summary>
    public List<SimulationStatistics> GetAllActiveSessionStatistics()
    {
        return _activeSessions.Values.Select(session => new SimulationStatistics
        {
            SessionId = session.SessionId,
            BibId = session.BibId,
            IsActive = session.CurrentPhase != SimulationPhase.Stopped,
            CurrentPhase = session.CurrentPhase,
            Duration = session.Duration,
            CommandsSimulated = session.CommandsSimulated,
            SuccessfulCommands = session.SuccessfulCommands,
            FailedCommands = session.FailedCommands,
            AverageCommandDuration = session.AverageCommandDuration
        }).ToList();
    }

    #region Private Implementation

    /// <summary>
    /// Execute start trigger simulation
    /// </summary>
    private async Task ExecuteStartTriggerAsync(SimulationSession session)
    {
        session.CurrentPhase = SimulationPhase.Starting;
        
        var startTrigger = session.HardwareConfig.StartTrigger;
        var delay = session.HardwareConfig.GetAdjustedDelay(TimeSpan.FromSeconds(startTrigger.DelaySeconds));
        
        if (startTrigger.EnableDiagnostics)
        {
            _logger.LogInformation("🔧 Start diagnostics: Delay={DelayMs}ms, Response='{Response}'", 
                delay.TotalMilliseconds, startTrigger.SuccessResponse);
        }

        await Task.Delay(delay);
        
        session.CurrentPhase = SimulationPhase.Ready;
        _logger.LogInformation("✅ Hardware simulation ready: {BibId}", session.BibId);
    }

    /// <summary>
    /// Execute stop trigger simulation
    /// </summary>
    private async Task ExecuteStopTriggerAsync(SimulationSession session)
    {
        session.CurrentPhase = SimulationPhase.Stopping;
        
        var stopTrigger = session.HardwareConfig.StopTrigger;
        
        if (stopTrigger.GracefulShutdown)
        {
            _logger.LogDebug("🔄 Graceful shutdown initiated (timeout: {TimeoutMs}ms)", 
                stopTrigger.GracefulTimeoutSeconds * 1000);
            
            var gracefulDelay = TimeSpan.FromSeconds(stopTrigger.GracefulTimeoutSeconds);
            await Task.Delay(session.HardwareConfig.GetAdjustedDelay(gracefulDelay));
        }
        
        var delay = session.HardwareConfig.GetAdjustedDelay(TimeSpan.FromSeconds(stopTrigger.DelaySeconds));
        await Task.Delay(delay);
    }

    /// <summary>
    /// Determine simulation behavior based on command and configuration
    /// </summary>
    private async Task DetermineSimulationBehaviorAsync(SimulationSession session, SimulationResponse response, 
        string command, string? expectedResponse, int timeoutMs)
    {
        // Update session phase based on command type
        session.CurrentPhase = DeterminePhaseFromCommand(command);
        session.CommandsSimulated++;

        // Calculate realistic delay based on simulation mode and random behavior
        var baseDelay = session.HardwareConfig.Mode.GetDefaultTiming();
        var adjustedDelay = session.HardwareConfig.GetAdjustedDelay(baseDelay);
        
        if (session.HardwareConfig.RandomBehavior.Enabled)
        {
            adjustedDelay = session.HardwareConfig.RandomBehavior.ApplyRandomDelay(adjustedDelay);
        }

        // Apply the delay
        await Task.Delay(adjustedDelay);

        // Determine response
        if (!string.IsNullOrEmpty(expectedResponse))
        {
            // Use expected response with possible random variation
            response.Response = session.HardwareConfig.RandomBehavior.GetRandomResponse(expectedResponse) ?? expectedResponse;
            response.Success = true;
            session.SuccessfulCommands++;
        }
        else
        {
            // Generate intelligent response based on command
            response.Response = GenerateIntelligentResponse(command, session);
            response.Success = true;
            session.SuccessfulCommands++;
        }

        // Update session timing statistics
        UpdateSessionStatistics(session, adjustedDelay);
    }

    /// <summary>
    /// Handle critical scenario simulation
    /// </summary>
    private async Task<SimulationResponse> HandleCriticalScenarioAsync(SimulationSession session, SimulationResponse response)
    {
        var criticalTrigger = session.HardwareConfig.CriticalTrigger;
        
        _logger.LogCritical("🚨 CRITICAL SCENARIO TRIGGERED: {ScenarioType} (Session: {SessionId})", 
            criticalTrigger.ScenarioType, session.SessionId);

        session.CurrentPhase = SimulationPhase.Critical;
        
        var delay = session.HardwareConfig.GetAdjustedDelay(TimeSpan.FromSeconds(criticalTrigger.DelaySeconds));
        await Task.Delay(delay);

        response.Success = false;
        response.ErrorMessage = criticalTrigger.ErrorMessage;
        response.Response = $"CRITICAL_{criticalTrigger.ScenarioType}";
        response.IsCritical = true;
        response.ErrorCode = criticalTrigger.ErrorCode;

        session.FailedCommands++;
        session.CriticalEvents++;

        if (criticalTrigger.TriggerHardwareNotification)
        {
            _logger.LogCritical("🚨 Hardware notification triggered for critical scenario");
            // In real implementation, this would trigger actual hardware notification
        }

        return response;
    }

    /// <summary>
    /// Determine simulation phase from command
    /// </summary>
    private SimulationPhase DeterminePhaseFromCommand(string command)
    {
        var cmdLower = command.ToLowerInvariant();
        
        if (cmdLower.Contains("init") || cmdLower.Contains("start"))
            return SimulationPhase.Starting;
        
        if (cmdLower.Contains("test") || cmdLower.Contains("run"))
            return SimulationPhase.Testing;
        
        if (cmdLower.Contains("quit") || cmdLower.Contains("stop") || cmdLower.Contains("exit"))
            return SimulationPhase.Stopping;
        
        return SimulationPhase.Executing;
    }

    /// <summary>
    /// Generate intelligent response based on command pattern
    /// </summary>
    private string GenerateIntelligentResponse(string command, SimulationSession session)
    {
        var cmdLower = command.ToLowerInvariant();
        
        // Pattern-based response generation
        if (cmdLower.Contains("init"))
            return "READY";
        
        if (cmdLower.Contains("test"))
            return "PASS";
        
        if (cmdLower.Contains("status"))
            return "OK";
        
        if (cmdLower.Contains("quit") || cmdLower.Contains("exit"))
            return "BYE";
        
        if (cmdLower.Contains("version"))
            return $"SIM_v{session.HardwareConfig.Mode}_{DateTime.Now:yyyyMMdd}";
        
        if (cmdLower.Contains("id"))
            return $"SIM_{session.BibId}";
        
        // Default intelligent response
        return $"ACK_{command.Take(3).ToArray()}".ToUpperInvariant();
    }

    /// <summary>
    /// Update session timing and performance statistics
    /// </summary>
    private void UpdateSessionStatistics(SimulationSession session, TimeSpan commandDuration)
    {
        session.TotalCommandTime += commandDuration;
        
        if (session.CommandsSimulated > 0)
        {
            session.AverageCommandDuration = TimeSpan.FromTicks(session.TotalCommandTime.Ticks / session.CommandsSimulated);
        }
    }

    #endregion

    /// <summary>
    /// Dispose simulation resources
    /// </summary>
    public void Dispose()
    {
        if (!_isDisposed)
        {
            _isDisposed = true;

            // Stop all active sessions
            var activeSessions = _activeSessions.Keys.ToList();
            foreach (var sessionId in activeSessions)
            {
                try
                {
                    StopSimulationAsync(sessionId).Wait(TimeSpan.FromSeconds(5));
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Warning during simulation cleanup: {SessionId}", sessionId);
                }
            }

            _activeSessions.Clear();
            _logger.LogInformation("🧹 XML-Driven Hardware Simulator disposed");
        }
    }
}

/// <summary>
/// Active simulation session data
/// </summary>
public class SimulationSession
{
    public string SessionId { get; set; } = string.Empty;
    public string BibId { get; set; } = string.Empty;
    public BibConfiguration? BibConfiguration { get; set; }
    public HardwareSimulationConfig HardwareConfig { get; set; } = null!;
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public SimulationPhase CurrentPhase { get; set; }
    
    // Statistics
    public int CommandsSimulated { get; set; }
    public int SuccessfulCommands { get; set; }
    public int FailedCommands { get; set; }
    public int CriticalEvents { get; set; }
    public TimeSpan TotalCommandTime { get; set; }
    public TimeSpan AverageCommandDuration { get; set; }
    
    /// <summary>
    /// Total session duration
    /// </summary>
    public TimeSpan Duration => (EndTime ?? DateTime.Now) - StartTime;
    
    /// <summary>
    /// Success rate for commands
    /// </summary>
    public double SuccessRate => CommandsSimulated > 0 ? (SuccessfulCommands * 100.0) / CommandsSimulated : 0.0;
}

/// <summary>
/// Response from hardware simulation
/// </summary>
public class SimulationResponse
{
    public string SessionId { get; set; } = string.Empty;
    public string Command { get; set; } = string.Empty;
    public string Response { get; set; } = string.Empty;
    public bool Success { get; set; } = true;
    public string? ErrorMessage { get; set; }
    public int? ErrorCode { get; set; }
    public bool IsCritical { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public TimeSpan Duration { get; set; }
    
    public override string ToString()
    {
        var status = Success ? "✅" : "❌";
        return $"{status} '{Command}' → '{Response}' ({Duration.TotalMilliseconds:F0}ms)";
    }
}

/// <summary>
/// Simulation session statistics
/// </summary>
public class SimulationStatistics
{
    public string SessionId { get; set; } = string.Empty;
    public string BibId { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public SimulationPhase CurrentPhase { get; set; }
    public TimeSpan Duration { get; set; }
    public int CommandsSimulated { get; set; }
    public int SuccessfulCommands { get; set; }
    public int FailedCommands { get; set; }
    public TimeSpan AverageCommandDuration { get; set; }
    
    public double SuccessRate => CommandsSimulated > 0 ? (SuccessfulCommands * 100.0) / CommandsSimulated : 0.0;
    
    public override string ToString()
    {
        return $"Simulation {BibId}: {CommandsSimulated} commands, {SuccessRate:F1}% success, {Duration.TotalSeconds:F1}s duration";
    }
}

/// <summary>
/// Simulation phases
/// </summary>
public enum SimulationPhase
{
    Initializing,
    Starting,
    Ready,
    Executing,
    Testing,
    Stopping,
    Stopped,
    Critical,
    Error
}

/// <summary>
/// Extension methods for simulation operations
/// </summary>
public static class SimulationExtensions
{
    /// <summary>
    /// Get icon for simulation phase
    /// </summary>
    public static string GetIcon(this SimulationPhase phase)
    {
        return phase switch
        {
            SimulationPhase.Initializing => "🔄",
            SimulationPhase.Starting => "🚀",
            SimulationPhase.Ready => "✅",
            SimulationPhase.Executing => "⚙️",
            SimulationPhase.Testing => "🧪",
            SimulationPhase.Stopping => "🛑",
            SimulationPhase.Stopped => "⏹️",
            SimulationPhase.Critical => "🚨",
            SimulationPhase.Error => "❌",
            _ => "❓"
        };
    }

    /// <summary>
    /// Get description for simulation phase
    /// </summary>
    public static string GetDescription(this SimulationPhase phase)
    {
        return phase switch
        {
            SimulationPhase.Initializing => "Simulation is initializing",
            SimulationPhase.Starting => "Hardware simulation starting up",
            SimulationPhase.Ready => "Simulation ready for commands",
            SimulationPhase.Executing => "Executing commands",
            SimulationPhase.Testing => "Running test sequences",
            SimulationPhase.Stopping => "Simulation shutting down",
            SimulationPhase.Stopped => "Simulation stopped",
            SimulationPhase.Critical => "Critical scenario active",
            SimulationPhase.Error => "Simulation error state",
            _ => "Unknown phase"
        };
    }

    /// <summary>
    /// Check if phase allows command execution
    /// </summary>
    public static bool AllowsCommandExecution(this SimulationPhase phase)
    {
        return phase == SimulationPhase.Ready || 
               phase == SimulationPhase.Executing || 
               phase == SimulationPhase.Testing;
    }
}