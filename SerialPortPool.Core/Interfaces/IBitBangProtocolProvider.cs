// ===================================================================
// SPRINT 9: IBitBangProtocolProvider.cs - Bit Bang Protocol GPIO Interface
// File: SerialPortPool.Core/Interfaces/IBitBangProtocolProvider.cs
// Purpose: FTDI GPIO hardware integration for production workflow control
// CLIENT REQUIREMENTS: Power control + critical fail signaling
// ===================================================================

using SerialPortPool.Core.Models;

namespace SerialPortPool.Core.Interfaces;

/// <summary>
/// Bit Bang Protocol Provider for GPIO hardware integration via FTDI devices
/// SPRINT 9: Complete GPIO abstraction for hardware-aware workflow control
/// CLIENT REQUIREMENTS: Power On Ready + Power Down Heads-Up + Critical Fail Output
/// 
/// HARDWARE INTEGRATION:
/// - INPUT BIT 0: Power On Ready (wait for this before starting workflow)
/// - INPUT BIT 1: Power Down Heads-Up (monitor during test execution)
/// - OUTPUT BIT 2: Critical Fail Signal (trigger on CRITICAL validation)
/// - OUTPUT BIT 3: Workflow Active (signal workflow execution status)
/// </summary>
public interface IBitBangProtocolProvider : IDisposable
{
    // 📡 INPUT BITS: Hardware state monitoring (CLIENT REQUIREMENTS)
    
    /// <summary>
    /// Read Power On Ready input signal (Bit 0)
    /// CLIENT REQUIREMENT: Wait for this signal before starting any workflow
    /// USAGE: Workflow orchestrator waits for TRUE before proceeding
    /// </summary>
    /// <returns>True if power is ready and workflow can start</returns>
    Task<bool> ReadPowerOnReadyAsync();
    
    /// <summary>
    /// Read Power Down Heads-Up input signal (Bit 1)  
    /// CLIENT REQUIREMENT: Monitor this during test execution for graceful shutdown
    /// USAGE: Test phase monitors this and terminates gracefully if TRUE
    /// </summary>
    /// <returns>True if power down is requested</returns>
    Task<bool> ReadPowerDownHeadsUpAsync();
    
    /// <summary>
    /// Read all input bits at once for efficiency
    /// PERFORMANCE: Single hardware operation to read multiple inputs
    /// </summary>
    /// <returns>Input state with individual bit values</returns>
    Task<BitBangInputState> ReadAllInputsAsync();

    // 📤 OUTPUT BITS: Hardware state signaling (CLIENT REQUIREMENTS)
    
    /// <summary>
    /// Set Critical Fail Signal output (Bit 2)
    /// CLIENT REQUIREMENT: Hardware notification for CRITICAL validation failures
    /// USAGE: Triggered automatically by CRITICAL validation results
    /// </summary>
    /// <param name="state">True to signal critical failure, False to clear</param>
    Task SetCriticalFailSignalAsync(bool state);
    
    /// <summary>
    /// Set Workflow Active Signal output (Bit 3)
    /// BONUS FEATURE: Indicate when workflow is running
    /// USAGE: Set TRUE at workflow start, FALSE at workflow end
    /// </summary>
    /// <param name="state">True when workflow is active, False when idle</param>
    Task SetWorkflowActiveSignalAsync(bool state);
    
    /// <summary>
    /// Set all output bits at once for efficiency
    /// PERFORMANCE: Single hardware operation to set multiple outputs
    /// </summary>
    /// <param name="outputState">Output state with individual bit values</param>
    Task SetAllOutputsAsync(BitBangOutputState outputState);

    // 🔧 Configuration & Management
    
    /// <summary>
    /// Check if bit bang hardware is available and accessible
    /// PRE-VALIDATION: Verify hardware before attempting operations
    /// </summary>
    /// <returns>True if hardware is connected and functional</returns>
    Task<bool> IsAvailableAsync();
    
    /// <summary>
    /// Initialize bit bang protocol with specified configuration
    /// SETUP: Configure GPIO pins, timing, and hardware settings
    /// </summary>
    /// <param name="config">Hardware configuration for GPIO operations</param>
    Task InitializeAsync(BitBangConfiguration config);
    
    /// <summary>
    /// Disconnect and cleanup bit bang hardware resources
    /// CLEANUP: Safely disconnect and release hardware resources
    /// </summary>
    Task DisconnectAsync();
    
    /// <summary>
    /// Reset all GPIO outputs to safe default state
    /// SAFETY: Emergency reset for hardware outputs
    /// </summary>
    Task ResetOutputsToDefaultAsync();

    // 📊 Status & Monitoring
    
    /// <summary>
    /// Get comprehensive hardware status including all GPIO states
    /// MONITORING: Real-time hardware status for diagnostics
    /// </summary>
    /// <returns>Complete hardware status with all bit states</returns>
    Task<BitBangStatus> GetStatusAsync();
    
    /// <summary>
    /// Get hardware connection and health information
    /// DIAGNOSTICS: Hardware connectivity and operational status
    /// </summary>
    /// <returns>Hardware health and connection status</returns>
    Task<BitBangHealthStatus> GetHealthStatusAsync();
    
    /// <summary>
    /// Get performance statistics for GPIO operations
    /// PERFORMANCE: Operation timing and success rates
    /// </summary>
    /// <returns>Performance metrics for hardware operations</returns>
    Task<BitBangPerformanceStats> GetPerformanceStatsAsync();

    // 🎯 Event System: Real-time hardware state monitoring
    
    /// <summary>
    /// Event fired when any hardware state changes
    /// REAL-TIME: Immediate notification of GPIO state changes
    /// USAGE: Subscribe to monitor power signals and hardware events
    /// </summary>
    event EventHandler<BitBangEventArgs>? HardwareStateChanged;
    
    /// <summary>
    /// Event fired specifically for Power On Ready state changes
    /// FOCUSED: Dedicated event for power ready monitoring
    /// </summary>
    event EventHandler<PowerReadyEventArgs>? PowerOnReadyChanged;
    
    /// <summary>
    /// Event fired specifically for Power Down Heads-Up state changes
    /// FOCUSED: Dedicated event for power down monitoring  
    /// </summary>
    event EventHandler<PowerDownEventArgs>? PowerDownHeadsUpChanged;
    
    /// <summary>
    /// Event fired when hardware connection is lost or restored
    /// CONNECTION: Monitor hardware connectivity status
    /// </summary>
    event EventHandler<HardwareConnectionEventArgs>? ConnectionStatusChanged;

    // 🔍 Advanced Operations (Optional/Future)
    
    /// <summary>
    /// Start continuous monitoring of input signals
    /// CONTINUOUS: Background monitoring with configurable polling interval
    /// </summary>
    /// <param name="pollingInterval">How often to check input states</param>
    /// <param name="cancellationToken">Token to stop monitoring</param>
    Task StartContinuousMonitoringAsync(TimeSpan pollingInterval, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Stop continuous monitoring of input signals
    /// </summary>
    Task StopContinuousMonitoringAsync();
    
    /// <summary>
    /// Test all GPIO operations for functionality
    /// TESTING: Comprehensive hardware test suite
    /// </summary>
    /// <returns>Test results for all GPIO operations</returns>
    Task<BitBangTestResult> RunHardwareTestAsync();
    
    /// <summary>
    /// Configure automatic signal clearing behavior
    /// AUTOMATION: Automatically clear output signals after specified time
    /// </summary>
    /// <param name="autoClearConfig">Configuration for automatic signal clearing</param>
    Task ConfigureAutoClearAsync(BitBangAutoClearConfiguration autoClearConfig);

    // 📋 Configuration Properties (Read-only access to current config)
    
    /// <summary>
    /// Current hardware configuration
    /// </summary>
    BitBangConfiguration? CurrentConfiguration { get; }
    
    /// <summary>
    /// Is continuous monitoring currently active?
    /// </summary>
    bool IsMonitoringActive { get; }
    
    /// <summary>
    /// Hardware device identifier (FTDI serial number or device ID)
    /// </summary>
    string? HardwareDeviceId { get; }
    
    /// <summary>
    /// Last successful hardware operation timestamp
    /// </summary>
    DateTime? LastSuccessfulOperation { get; }
}

/// <summary>
/// Input state for all GPIO input bits
/// </summary>
public class BitBangInputState
{
    /// <summary>
    /// Power On Ready signal state (Bit 0)
    /// </summary>
    public bool PowerOnReady { get; set; }
    
    /// <summary>
    /// Power Down Heads-Up signal state (Bit 1)
    /// </summary>
    public bool PowerDownHeadsUp { get; set; }
    
    /// <summary>
    /// Raw input byte value (for advanced use)
    /// </summary>
    public byte RawInputValue { get; set; }
    
    /// <summary>
    /// Timestamp when this state was read
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.Now;
    
    /// <summary>
    /// Individual bit states as array for programmatic access
    /// </summary>
    public bool[] GetBitArray() => new[] { PowerOnReady, PowerDownHeadsUp };
    
    public override string ToString()
    {
        return $"Inputs: PowerReady={PowerOnReady}, PowerDown={PowerDownHeadsUp} [Raw: 0x{RawInputValue:X2}]";
    }
}

/// <summary>
/// Output state for all GPIO output bits
/// </summary>
public class BitBangOutputState
{
    /// <summary>
    /// Critical Fail Signal state (Bit 2)
    /// </summary>
    public bool CriticalFailSignal { get; set; }
    
    /// <summary>
    /// Workflow Active signal state (Bit 3)
    /// </summary>
    public bool WorkflowActiveSignal { get; set; }
    
    /// <summary>
    /// Raw output byte value (for advanced use)
    /// </summary>
    public byte RawOutputValue { get; set; }
    
    /// <summary>
    /// Timestamp when this state was set
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.Now;
    
    /// <summary>
    /// Individual bit states as array for programmatic access
    /// </summary>
    public bool[] GetBitArray() => new[] { false, false, CriticalFailSignal, WorkflowActiveSignal };
    
    /// <summary>
    /// Create output state from individual signals
    /// </summary>
    public static BitBangOutputState Create(bool criticalFail = false, bool workflowActive = false)
    {
        return new BitBangOutputState
        {
            CriticalFailSignal = criticalFail,
            WorkflowActiveSignal = workflowActive,
            RawOutputValue = (byte)((criticalFail ? 0x04 : 0x00) | (workflowActive ? 0x08 : 0x00))
        };
    }
    
    public override string ToString()
    {
        return $"Outputs: CriticalFail={CriticalFailSignal}, WorkflowActive={WorkflowActiveSignal} [Raw: 0x{RawOutputValue:X2}]";
    }
}

/// <summary>
/// Hardware health and connectivity status
/// </summary>
public class BitBangHealthStatus
{
    /// <summary>
    /// Is hardware physically connected?
    /// </summary>
    public bool IsConnected { get; set; }
    
    /// <summary>
    /// Is hardware responding to operations?
    /// </summary>
    public bool IsResponding { get; set; }
    
    /// <summary>
    /// Last error message (if any)
    /// </summary>
    public string? LastError { get; set; }
    
    /// <summary>
    /// Hardware firmware version (if available)
    /// </summary>
    public string? FirmwareVersion { get; set; }
    
    /// <summary>
    /// Connection uptime
    /// </summary>
    public TimeSpan Uptime { get; set; }
    
    /// <summary>
    /// Number of successful operations since connection
    /// </summary>
    public long SuccessfulOperations { get; set; }
    
    /// <summary>
    /// Number of failed operations since connection
    /// </summary>
    public long FailedOperations { get; set; }
    
    /// <summary>
    /// Success rate percentage
    /// </summary>
    public double SuccessRate => SuccessfulOperations + FailedOperations > 0 
        ? (SuccessfulOperations * 100.0) / (SuccessfulOperations + FailedOperations) 
        : 0.0;
    
    /// <summary>
    /// Overall health status
    /// </summary>
    public HardwareHealthLevel HealthLevel => 
        !IsConnected ? HardwareHealthLevel.Disconnected :
        !IsResponding ? HardwareHealthLevel.Error :
        SuccessRate < 90 ? HardwareHealthLevel.Warning :
        HardwareHealthLevel.Healthy;
    
    public override string ToString()
    {
        var icon = HealthLevel switch
        {
            HardwareHealthLevel.Healthy => "✅",
            HardwareHealthLevel.Warning => "⚠️",
            HardwareHealthLevel.Error => "❌",
            HardwareHealthLevel.Disconnected => "🔌❌",
            _ => "❓"
        };
        
        return $"{icon} Hardware Health: {HealthLevel} - Success Rate: {SuccessRate:F1}% ({SuccessfulOperations}/{SuccessfulOperations + FailedOperations})";
    }
}

/// <summary>
/// Hardware health levels
/// </summary>
public enum HardwareHealthLevel
{
    /// <summary>
    /// Hardware is healthy and operating normally
    /// </summary>
    Healthy,
    
    /// <summary>
    /// Hardware is operating but with some issues
    /// </summary>
    Warning,
    
    /// <summary>
    /// Hardware has errors but is still connected
    /// </summary>
    Error,
    
    /// <summary>
    /// Hardware is disconnected
    /// </summary>
    Disconnected
}

/// <summary>
/// Performance statistics for GPIO operations
/// </summary>
public class BitBangPerformanceStats
{
    /// <summary>
    /// Total number of GPIO operations performed
    /// </summary>
    public long TotalOperations { get; set; }
    
    /// <summary>
    /// Average operation duration in milliseconds
    /// </summary>
    public double AverageOperationTimeMs { get; set; }
    
    /// <summary>
    /// Fastest operation time in milliseconds
    /// </summary>
    public double FastestOperationTimeMs { get; set; }
    
    /// <summary>
    /// Slowest operation time in milliseconds
    /// </summary>
    public double SlowestOperationTimeMs { get; set; }
    
    /// <summary>
    /// Operations per second (throughput)
    /// </summary>
    public double OperationsPerSecond { get; set; }
    
    /// <summary>
    /// When statistics collection started
    /// </summary>
    public DateTime CollectionStartTime { get; set; }
    
    /// <summary>
    /// Statistics collection duration
    /// </summary>
    public TimeSpan CollectionDuration => DateTime.Now - CollectionStartTime;
    
    public override string ToString()
    {
        return $"GPIO Performance: {TotalOperations} ops, {AverageOperationTimeMs:F1}ms avg, {OperationsPerSecond:F1} ops/sec";
    }
}

/// <summary>
/// Comprehensive hardware test result
/// </summary>
public class BitBangTestResult
{
    /// <summary>
    /// Overall test result
    /// </summary>
    public bool Success { get; set; }
    
    /// <summary>
    /// Individual test results
    /// </summary>
    public Dictionary<string, bool> TestResults { get; set; } = new();
    
    /// <summary>
    /// Test execution messages
    /// </summary>
    public List<string> Messages { get; set; } = new();
    
    /// <summary>
    /// Test execution duration
    /// </summary>
    public TimeSpan Duration { get; set; }
    
    /// <summary>
    /// Number of tests passed
    /// </summary>
    public int PassedTests => TestResults.Count(r => r.Value);
    
    /// <summary>
    /// Number of tests failed
    /// </summary>
    public int FailedTests => TestResults.Count(r => !r.Value);
    
    /// <summary>
    /// Test success rate
    /// </summary>
    public double SuccessRate => TestResults.Any() ? (PassedTests * 100.0) / TestResults.Count : 0.0;
    
    public override string ToString()
    {
        var icon = Success ? "✅" : "❌";
        return $"{icon} Hardware Test: {PassedTests}/{TestResults.Count} passed ({SuccessRate:F1}%) in {Duration.TotalSeconds:F1}s";
    }
}

/// <summary>
/// Configuration for automatic signal clearing
/// </summary>
public class BitBangAutoClearConfiguration
{
    /// <summary>
    /// Automatically clear Critical Fail Signal after specified time
    /// </summary>
    public bool AutoClearCriticalFail { get; set; } = true;
    
    /// <summary>
    /// How long to hold Critical Fail Signal before clearing
    /// </summary>
    public TimeSpan CriticalFailHoldTime { get; set; } = TimeSpan.FromSeconds(5);
    
    /// <summary>
    /// Automatically clear Workflow Active Signal on disconnect
    /// </summary>
    public bool AutoClearOnDisconnect { get; set; } = true;
    
    /// <summary>
    /// Reset all outputs to safe state on initialization
    /// </summary>
    public bool ResetOnInitialize { get; set; } = true;
}