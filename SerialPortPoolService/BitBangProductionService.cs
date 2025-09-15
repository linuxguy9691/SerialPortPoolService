// ===================================================================
// SPRINT 14 BOUCHÉE #2: BitBang Production Service
// File: SerialPortPoolService/BitBangProductionService.cs
// Purpose: Per UUT_ID BitBang signal management with simulation transparency
// FIX: Respecter l'absence de StopTrigger dans XML = boucle infinie
// ===================================================================

using Microsoft.Extensions.Logging;
using SerialPortPool.Core.Interfaces;
using SerialPortPool.Core.Models;

namespace SerialPortPoolService.Services;

/// <summary>
/// SPRINT 14: BitBang Production Service for per UUT_ID signal management
/// TRANSPARENT: Works with XML simulation (current) and physical hardware (future)
/// PER UUT_ID: Each UUT has independent START/LOOP/STOP cycle
/// FIX: Respecte l'absence de StopTrigger = boucle infinie
/// </summary>
public class BitBangProductionService
{
    private readonly ILogger<BitBangProductionService> _logger;
    private readonly Dictionary<string, IBitBangProtocolProvider> _bitBangProviders = new();
    private readonly Dictionary<string, UutSignalState> _uutStates = new();
    private readonly SemaphoreSlim _providerLock = new(1, 1);

    public BitBangProductionService(ILogger<BitBangProductionService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _logger.LogInformation("🔌 BitBangProductionService initialized for per UUT_ID signal management");
    }

    /// <summary>
    /// TRANSPARENT: Wait for START signal per UUT_ID (simulation or physical)
    /// </summary>
    public async Task<bool> WaitForStartSignalAsync(string uutId, HardwareSimulationConfig config)
    {
        _logger.LogInformation($"⏳ Waiting for START signal: UUT_ID={uutId}");
        
        try
        {
            if (IsSimulationMode(config))
            {
                _logger.LogDebug($"🎭 Using XML simulation for START: {uutId}");
                return await SimulateStartTrigger(uutId, config.StartTrigger);
            }
            else
            {
                _logger.LogDebug($"🔌 Using physical BitBang for START: {uutId}");
                var provider = await GetOrCreateBitBangProviderAsync(uutId, config);
                return await WaitForPhysicalBitBangStart(provider, uutId, config.StartTrigger);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"❌ Error waiting for START signal: {uutId}");
            return false;
        }
    }

    /// <summary>
    /// TRANSPARENT: Wait for STOP signal per UUT_ID (simulation or physical)
    /// FIX: Si aucun StopTrigger défini, ne jamais s'arrêter (boucle infinie)
    /// </summary>
    public async Task<bool> WaitForStopSignalAsync(string uutId, HardwareSimulationConfig config)
    {
        _logger.LogDebug($"🛑 Checking for STOP signal: UUT_ID={uutId}");
        
        try
        {
            if (IsSimulationMode(config))
            {
                // ✅ FIX: Vérifier si un StopTrigger est réellement défini
                if (config.StopTrigger == null || !IsStopTriggerDefined(config.StopTrigger))
                {
                    _logger.LogDebug($"🔄 No StopTrigger defined for {uutId} - continuing infinite loop");
                    return false; // Pas de STOP = continue la boucle
                }
                
                return await SimulateStopTrigger(uutId, config.StopTrigger, config.CriticalTrigger);
            }
            else
            {
                var provider = await GetOrCreateBitBangProviderAsync(uutId, config);
                return await CheckPhysicalBitBangStop(provider, uutId, config.StopTrigger);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"❌ Error checking STOP signal: {uutId}");
            return false; // Don't stop on error - continue TEST loop
        }
    }

    /// <summary>
    /// TRANSPARENT: Check for critical failure per UUT_ID (simulation or physical)
    /// </summary>
    public async Task<bool> CheckCriticalFailureAsync(string uutId, HardwareSimulationConfig config)
    {
        _logger.LogDebug($"🚨 Checking critical failure status: UUT_ID={uutId}");
        
        try
        {
            if (IsSimulationMode(config))
            {
                return await SimulateCriticalFailure(uutId, config.CriticalTrigger);
            }
            else
            {
                var provider = await GetOrCreateBitBangProviderAsync(uutId, config);
                return await ReadPhysicalCriticalStatus(provider, uutId, config.CriticalTrigger);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"❌ Error checking critical failure: {uutId}");
            return false; // Don't trigger critical on error
        }
    }

    #region Simulation Methods (Current Sprint 13)

    /// <summary>
    /// Detect if we're in simulation mode or physical hardware mode
    /// </summary>
    private bool IsSimulationMode(HardwareSimulationConfig config) =>
        config?.Enabled == true;

    /// <summary>
    /// ✅ FIX: Vérifier si un StopTrigger est réellement défini dans le XML
    /// </summary>
    private bool IsStopTriggerDefined(StopTriggerConfig stopTrigger)
    {
        if (stopTrigger == null) return false;
        
        // Si DelaySeconds est 0 ou très petit, considérer comme non défini
        // Cela évite les valeurs par défaut artificielles
        if (stopTrigger.DelaySeconds <= 0) return false;
        
        // Si aucun signal ou condition n'est défini, considérer comme non défini
        if (string.IsNullOrEmpty(stopTrigger.SuccessResponse) && stopTrigger.DelaySeconds < 1)
        {
            return false;
        }
        
        return true; // StopTrigger valide trouvé
    }

    /// <summary>
    /// Simulate START trigger with realistic delays and UUT_ID tracking
    /// </summary>
    private async Task<bool> SimulateStartTrigger(string uutId, StartTriggerConfig trigger)
    {
        var adjustedDelay = TimeSpan.FromSeconds(trigger.DelaySeconds);
        _logger.LogDebug($"🎭 Simulating START delay: {adjustedDelay.TotalSeconds}s for {uutId}");
        
        UpdateUutState(uutId, UutSignalPhase.WaitingForStart);
        
        await Task.Delay(adjustedDelay);
        
        UpdateUutState(uutId, UutSignalPhase.Started);
        
        _logger.LogInformation($"✅ Simulated START trigger activated: {uutId} → {trigger.SuccessResponse}");
        return true;
    }

    /// <summary>
    /// Simulate STOP trigger with various stop conditions per UUT_ID
    /// ✅ FIX: Seulement si un StopTrigger est vraiment défini
    /// </summary>
    private async Task<bool> SimulateStopTrigger(string uutId, StopTriggerConfig stopTrigger, CriticalTriggerConfig criticalTrigger)
    {
        var uutState = GetUutState(uutId);
        var runningDuration = DateTime.Now - uutState.LastStateChange;
        
        _logger.LogInformation($"🔍 STOP CHECK DETAILS: {uutId}");
        _logger.LogInformation($"         Current Time: {DateTime.Now:HH:mm:ss.fff}");
        _logger.LogInformation($"         Test Start Time: {uutState.LastStateChange:HH:mm:ss.fff}");
        _logger.LogInformation($"         Running Duration: {runningDuration.TotalSeconds:F1}s");
        _logger.LogInformation($"         Configured Delay: {stopTrigger.DelaySeconds}s");
        
        var shouldStop = false;
        
        // ✅ FIX: Seulement arrêter si la durée configurée est atteinte ET valide
        if (stopTrigger.DelaySeconds > 0 && runningDuration.TotalSeconds >= stopTrigger.DelaySeconds)
        {
            shouldStop = true;
            _logger.LogInformation($"         Should Stop: True");
            _logger.LogInformation($"🛑 Simulated STOP trigger (duration): {uutId} after {runningDuration.TotalSeconds:F1}s");
        }
        else
        {
            _logger.LogInformation($"         Should Stop: False");
            _logger.LogDebug($"🔄 Duration condition not met: {runningDuration.TotalSeconds:F1}s < {stopTrigger.DelaySeconds}s");
        }
        
        // ✅ REMOVE: Suppression de la logique de STOP aléatoire qui était problématique
        // else if (new Random().NextDouble() < 0.1) // 10% chance per check
        
        if (shouldStop)
        {
            UpdateUutState(uutId, UutSignalPhase.Stopping);
            
            // Graceful shutdown delay
            _logger.LogDebug($"🛑 Graceful shutdown delay: 5s for {uutId}");
            await Task.Delay(5000); // Graceful stop delay
            
            UpdateUutState(uutId, UutSignalPhase.Stopped);
        }
        
        return shouldStop;
    }

    /// <summary>
    /// Simulate critical failure scenarios per UUT_ID
    /// </summary>
    private async Task<bool> SimulateCriticalFailure(string uutId, CriticalTriggerConfig trigger)
    {
        if (!trigger.Enabled) return false;
        
        var random = new Random();
        var shouldFail = random.NextDouble() < trigger.ActivationProbability;
        
        if (shouldFail)
        {
            UpdateUutState(uutId, UutSignalPhase.CriticalFailure);
            
            _logger.LogCritical($"🚨 Simulated CRITICAL failure: {uutId} - {trigger.ErrorMessage}");
            _logger.LogCritical($"🚨 Scenario: {trigger.ScenarioType}, Recovery: {(trigger.RecoveryPossible ? "Possible" : "Not possible")}");
            
            await Task.Delay(200);
        }
        
        return shouldFail;
    }

    #endregion

    #region Physical Hardware Methods (Future Implementation)

    /// <summary>
    /// Get or create BitBang provider for specific UUT_ID (thread-safe)
    /// </summary>
    private async Task<IBitBangProtocolProvider> GetOrCreateBitBangProviderAsync(string uutId, HardwareSimulationConfig config)
    {
        await _providerLock.WaitAsync();
        try
        {
            if (_bitBangProviders.TryGetValue(uutId, out var existingProvider))
            {
                return existingProvider;
            }
            
            _logger.LogDebug($"🔌 Creating BitBang provider for UUT: {uutId}");
            throw new NotImplementedException($"Physical BitBang provider for UUT {uutId} - Sprint 15+");
        }
        finally
        {
            _providerLock.Release();
        }
    }

    /// <summary>
    /// Wait for physical hardware START signal (Future)
    /// </summary>
    private async Task<bool> WaitForPhysicalBitBangStart(IBitBangProtocolProvider provider, string uutId, StartTriggerConfig trigger)
    {
        await Task.CompletedTask;
        _logger.LogWarning($"🔌 Physical BitBang START not implemented: {uutId}");
        throw new NotImplementedException("Physical BitBang hardware - Sprint 15+");
    }

    /// <summary>
    /// Check physical hardware STOP signal (Future)
    /// </summary>
    private async Task<bool> CheckPhysicalBitBangStop(IBitBangProtocolProvider provider, string uutId, StopTriggerConfig trigger)
    {
        await Task.CompletedTask;
        _logger.LogWarning($"🔌 Physical BitBang STOP not implemented: {uutId}");
        throw new NotImplementedException("Physical BitBang hardware - Sprint 15+");
    }

    /// <summary>
    /// Read physical critical status (Future)
    /// </summary>
    private async Task<bool> ReadPhysicalCriticalStatus(IBitBangProtocolProvider provider, string uutId, CriticalTriggerConfig trigger)
    {
        await Task.CompletedTask;
        _logger.LogWarning($"🔌 Physical BitBang CRITICAL not implemented: {uutId}");
        throw new NotImplementedException("Physical BitBang hardware - Sprint 15+");
    }

    #endregion

    #region UUT State Management (Per UUT_ID tracking)

    /// <summary>
    /// Update UUT signal state (per UUT_ID tracking)
    /// </summary>
    private void UpdateUutState(string uutId, UutSignalPhase phase)
    {
        _uutStates[uutId] = new UutSignalState
        {
            UutId = uutId,
            CurrentPhase = phase,
            LastStateChange = DateTime.Now
        };
        
        _logger.LogDebug($"🔄 UUT state updated: {uutId} → {phase}");
    }

    /// <summary>
    /// Get current UUT signal state
    /// </summary>
    private UutSignalState GetUutState(string uutId)
    {
        if (_uutStates.TryGetValue(uutId, out var state))
        {
            return state;
        }
        
        var defaultState = new UutSignalState
        {
            UutId = uutId,
            CurrentPhase = UutSignalPhase.NotStarted,
            LastStateChange = DateTime.Now
        };
        
        _uutStates[uutId] = defaultState;
        return defaultState;
    }

    /// <summary>
    /// Get summary of all UUT states (for monitoring/debugging)
    /// </summary>
    public Dictionary<string, UutSignalState> GetAllUutStates()
    {
        return new Dictionary<string, UutSignalState>(_uutStates);
    }

    /// <summary>
    /// Reset UUT state (for cleanup)
    /// </summary>
    public void ResetUutState(string uutId)
    {
        _uutStates.Remove(uutId);
        _logger.LogDebug($"🧹 UUT state reset: {uutId}");
    }

    #endregion

    #region IDisposable Implementation

    /// <summary>
    /// Cleanup resources
    /// </summary>
    public void Dispose()
    {
        _logger.LogInformation("🧹 Disposing BitBangProductionService...");
        
        try
        {
            foreach (var provider in _bitBangProviders.Values)
            {
                provider?.Dispose();
            }
            _bitBangProviders.Clear();
            
            _uutStates.Clear();
            
            _providerLock?.Dispose();
            
            _logger.LogInformation("✅ BitBangProductionService disposed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error during BitBangProductionService disposal");
        }
    }

    #endregion
}

/// <summary>
/// UUT signal state tracking per UUT_ID
/// </summary>
public class UutSignalState
{
    public string UutId { get; set; } = string.Empty;
    public UutSignalPhase CurrentPhase { get; set; }
    public DateTime LastStateChange { get; set; } = DateTime.Now;
    
    public override string ToString()
    {
        var duration = DateTime.Now - LastStateChange;
        return $"{UutId}: {CurrentPhase} (for {duration.TotalSeconds:F1}s)";
    }
}

/// <summary>
/// UUT signal phases for per UUT_ID lifecycle tracking
/// </summary>
public enum UutSignalPhase
{
    NotStarted,
    WaitingForStart,
    Started,
    Testing,
    Stopping,
    Stopped,
    CriticalFailure
}