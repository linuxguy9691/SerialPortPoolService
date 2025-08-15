// ===================================================================
// SPRINT 9: BitBang Configuration & Status Models
// File: SerialPortPool.Core/Models/BitBangConfiguration.cs
// Purpose: Configuration and status models for GPIO hardware integration
// ===================================================================

namespace SerialPortPool.Core.Models;

/// <summary>
/// Configuration for Bit Bang Protocol GPIO integration
/// SPRINT 9: Complete GPIO configuration for FTDI device hardware control
/// CLIENT REQUIREMENTS: Power control + critical fail signaling
/// </summary>
public class BitBangConfiguration
{
    // 🔧 Hardware Identification
    /// <summary>
    /// FTDI device identifier for GPIO operations
    /// </summary>
    public string? DeviceId { get; set; }
    
    /// <summary>
    /// FTDI device serial number (alternative to DeviceId)
    /// </summary>
    public string? SerialNumber { get; set; }
    
    /// <summary>
    /// Human-readable configuration name
    /// </summary>
    public string ConfigurationName { get; set; } = "Default BitBang Config";

    // 📡 INPUT BIT CONFIGURATION (Client Requirements)
    /// <summary>
    /// GPIO bit index for Power On Ready signal (default: bit 0)
    /// CLIENT REQUIREMENT: Wait for this signal before starting workflow
    /// </summary>
    public int PowerOnReadyBitIndex { get; set; } = 0;
    
    /// <summary>
    /// GPIO bit index for Power Down Heads-Up signal (default: bit 1)
    /// CLIENT REQUIREMENT: Monitor this during test execution
    /// </summary>
    public int PowerDownHeadsUpBitIndex { get; set; } = 1;
    
    /// <summary>
    /// Input signal logic (false = active high, true = active low)
    /// </summary>
    public bool InputActiveLow { get; set; } = false;

    // 📤 OUTPUT BIT CONFIGURATION (Client Requirements)  
    /// <summary>
    /// GPIO bit index for Critical Fail Signal (default: bit 2)
    /// CLIENT REQUIREMENT: Hardware notification for critical conditions
    /// </summary>
    public int CriticalFailBitIndex { get; set; } = 2;
    
    /// <summary>
    /// GPIO bit index for Workflow Active Signal (default: bit 3)
    /// BONUS: Indicate workflow execution status
    /// </summary>
    public int WorkflowActiveBitIndex { get; set; } = 3;
    
    /// <summary>
    /// Output signal logic (false = active high, true = active low)
    /// </summary>
    public bool OutputActiveLow { get; set; } = false;

    // ⏱️ TIMING & BEHAVIOR CONFIGURATION
    /// <summary>
    /// How often to poll input signals during continuous monitoring
    /// </summary>
    public TimeSpan PollingInterval { get; set; } = TimeSpan.FromMilliseconds(100);
    
    /// <summary>
    /// How long to hold output signals before auto-clear
    /// </summary>
    public TimeSpan SignalHoldTime { get; set; } = TimeSpan.FromSeconds(2);
    
    /// <summary>
    /// Automatically clear output signals after hold time
    /// </summary>
    public bool AutoClearSignals { get; set; } = true;
    
    /// <summary>
    /// Timeout for hardware operations
    /// </summary>
    public TimeSpan OperationTimeout { get; set; } = TimeSpan.FromSeconds(5);
    
    /// <summary>
    /// Maximum retries for failed hardware operations
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    // 🔍 ADVANCED CONFIGURATION
    /// <summary>
    /// Enable continuous monitoring of input signals
    /// </summary>
    public bool EnableContinuousMonitoring { get; set; } = true;
    
    /// <summary>
    /// Enable hardware event notifications
    /// </summary>
    public bool EnableEventNotifications { get; set; } = true;
    
    /// <summary>
    /// Reset all outputs to safe state on initialization
    /// </summary>
    public bool ResetOutputsOnInit { get; set; } = true;
    
    /// <summary>
    /// GPIO direction mask (1 = output, 0 = input)
    /// Default: bits 0,1 = input, bits 2,3 = output
    /// </summary>
    public byte DirectionMask { get; set; } = 0x0C; // 0000 1100 = bits 2,3 output
    
    /// <summary>
    /// Initial output state on initialization
    /// </summary>
    public byte InitialOutputState { get; set; } = 0x00; // All outputs low initially

    // 📋 VALIDATION & METADATA
    /// <summary>
    /// Configuration metadata
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; } = new();
    
    /// <summary>
    /// When this configuration was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    
    /// <summary>
    /// Configuration version for compatibility tracking
    /// </summary>
    public string ConfigVersion { get; set; } = "9.0.0";

    /// <summary>
    /// Validate configuration for correctness
    /// </summary>
    public bool IsValid => !string.IsNullOrEmpty(DeviceId) || !string.IsNullOrEmpty(SerialNumber);
    
    /// <summary>
    /// Get validation errors
    /// </summary>
    public List<string> GetValidationErrors()
    {
        var errors = new List<string>();
        
        if (string.IsNullOrEmpty(DeviceId) && string.IsNullOrEmpty(SerialNumber))
            errors.Add("Either DeviceId or SerialNumber must be specified");
            
        if (PowerOnReadyBitIndex < 0 || PowerOnReadyBitIndex > 7)
            errors.Add("PowerOnReadyBitIndex must be 0-7");
            
        if (PowerDownHeadsUpBitIndex < 0 || PowerDownHeadsUpBitIndex > 7)
            errors.Add("PowerDownHeadsUpBitIndex must be 0-7");
            
        if (CriticalFailBitIndex < 0 || CriticalFailBitIndex > 7)
            errors.Add("CriticalFailBitIndex must be 0-7");
            
        if (WorkflowActiveBitIndex < 0 || WorkflowActiveBitIndex > 7)
            errors.Add("WorkflowActiveBitIndex must be 0-7");
            
        if (PollingInterval.TotalMilliseconds < 10)
            errors.Add("PollingInterval must be at least 10ms");
            
        return errors;
    }
    
    /// <summary>
    /// Create default configuration for client requirements
    /// </summary>
    public static BitBangConfiguration CreateDefault(string? deviceId = null, string? serialNumber = null)
    {
        return new BitBangConfiguration
        {
            DeviceId = deviceId,
            SerialNumber = serialNumber,
            ConfigurationName = "Default Client Config",
            PowerOnReadyBitIndex = 0,      // Input bit 0
            PowerDownHeadsUpBitIndex = 1,  // Input bit 1
            CriticalFailBitIndex = 2,      // Output bit 2
            WorkflowActiveBitIndex = 3,    // Output bit 3
            InputActiveLow = false,        // Active high inputs
            OutputActiveLow = false,       // Active high outputs
            PollingInterval = TimeSpan.FromMilliseconds(100),
            SignalHoldTime = TimeSpan.FromSeconds(2),
            AutoClearSignals = true,
            EnableContinuousMonitoring = true,
            EnableEventNotifications = true,
            ResetOutputsOnInit = true
        };
    }
    
    public override string ToString()
    {
        var device = !string.IsNullOrEmpty(DeviceId) ? DeviceId : SerialNumber ?? "Unknown";
        return $"BitBang Config: {device} - In[{PowerOnReadyBitIndex},{PowerDownHeadsUpBitIndex}] Out[{CriticalFailBitIndex},{WorkflowActiveBitIndex}]";
    }
}

/// <summary>
/// Current status of bit bang protocol hardware
/// SPRINT 9: Complete status monitoring for GPIO operations
/// </summary>
public class BitBangStatus
{
    // 🔌 CONNECTION STATUS
    /// <summary>
    /// Is hardware device connected and accessible?
    /// </summary>
    public bool IsConnected { get; set; }
    
    /// <summary>
    /// Is hardware responding to operations?
    /// </summary>
    public bool IsResponding { get; set; }
    
    /// <summary>
    /// Hardware device identifier
    /// </summary>
    public string? DeviceId { get; set; }

    // 📡 INPUT SIGNAL STATES (Client Requirements)
    /// <summary>
    /// Current state of Power On Ready signal (bit 0)
    /// CLIENT REQUIREMENT: Power ready status
    /// </summary>
    public bool PowerOnReady { get; set; }
    
    /// <summary>
    /// Current state of Power Down Heads-Up signal (bit 1)
    /// CLIENT REQUIREMENT: Power down request status
    /// </summary>
    public bool PowerDownHeadsUp { get; set; }

    // 📤 OUTPUT SIGNAL STATES (Client Requirements)
    /// <summary>
    /// Current state of Critical Fail Signal (bit 2)
    /// CLIENT REQUIREMENT: Critical failure notification
    /// </summary>
    public bool CriticalFailSignal { get; set; }
    
    /// <summary>
    /// Current state of Workflow Active Signal (bit 3)
    /// BONUS: Workflow execution status
    /// </summary>
    public bool WorkflowActiveSignal { get; set; }

    // 🔧 RAW HARDWARE DATA
    /// <summary>
    /// Raw GPIO input byte value
    /// </summary>
    public byte RawInputValue { get; set; }
    
    /// <summary>
    /// Raw GPIO output byte value
    /// </summary>
    public byte RawOutputValue { get; set; }
    
    /// <summary>
    /// GPIO direction mask (1 = output, 0 = input)
    /// </summary>
    public byte DirectionMask { get; set; }

    // ⏱️ TIMING & MONITORING
    /// <summary>
    /// When this status was last updated
    /// </summary>
    public DateTime LastUpdated { get; set; } = DateTime.Now;
    
    /// <summary>
    /// How long ago this status was read
    /// </summary>
    public TimeSpan Age => DateTime.Now - LastUpdated;
    
    /// <summary>
    /// Is this status fresh (less than 1 second old)?
    /// </summary>
    public bool IsFresh => Age.TotalSeconds < 1.0;
    
    /// <summary>
    /// Connection uptime
    /// </summary>
    public TimeSpan Uptime { get; set; }

    // 📊 OPERATIONAL STATUS
    /// <summary>
    /// Last error message (if any)
    /// </summary>
    public string? LastError { get; set; }
    
    /// <summary>
    /// Status message or description
    /// </summary>
    public string? StatusMessage { get; set; }
    
    /// <summary>
    /// Number of successful operations since connection
    /// </summary>
    public long SuccessfulOperations { get; set; }
    
    /// <summary>
    /// Number of failed operations since connection
    /// </summary>
    public long FailedOperations { get; set; }
    
    /// <summary>
    /// Is continuous monitoring currently active?
    /// </summary>
    public bool IsMonitoringActive { get; set; }

    // 🎯 COMPUTED PROPERTIES
    /// <summary>
    /// Overall hardware health status
    /// </summary>
    public HardwareHealthLevel HealthLevel => 
        !IsConnected ? HardwareHealthLevel.Disconnected :
        !IsResponding ? HardwareHealthLevel.Error :
        !string.IsNullOrEmpty(LastError) ? HardwareHealthLevel.Warning :
        HardwareHealthLevel.Healthy;
    
    /// <summary>
    /// Success rate for hardware operations
    /// </summary>
    public double SuccessRate => SuccessfulOperations + FailedOperations > 0 
        ? (SuccessfulOperations * 100.0) / (SuccessfulOperations + FailedOperations) 
        : 0.0;
    
    /// <summary>
    /// Are all input signals in expected state for workflow start?
    /// CLIENT LOGIC: PowerOnReady=true, PowerDownHeadsUp=false
    /// </summary>
    public bool IsReadyForWorkflow => PowerOnReady && !PowerDownHeadsUp;
    
    /// <summary>
    /// Are any critical output signals active?
    /// </summary>
    public bool HasActiveCriticalSignals => CriticalFailSignal;

    /// <summary>
    /// Get input signals as structured data
    /// </summary>
    public BitBangInputState GetInputState()
    {
        return new BitBangInputState
        {
            PowerOnReady = PowerOnReady,
            PowerDownHeadsUp = PowerDownHeadsUp,
            RawInputValue = RawInputValue,
            Timestamp = LastUpdated
        };
    }
    
    /// <summary>
    /// Get output signals as structured data
    /// </summary>
    public BitBangOutputState GetOutputState()
    {
        return new BitBangOutputState
        {
            CriticalFailSignal = CriticalFailSignal,
            WorkflowActiveSignal = WorkflowActiveSignal,
            RawOutputValue = RawOutputValue,
            Timestamp = LastUpdated
        };
    }
    
    /// <summary>
    /// Create status with error state
    /// </summary>
    public static BitBangStatus CreateError(string deviceId, string errorMessage)
    {
        return new BitBangStatus
        {
            DeviceId = deviceId,
            IsConnected = false,
            IsResponding = false,
            LastError = errorMessage,
            StatusMessage = $"Error: {errorMessage}",
            LastUpdated = DateTime.Now
        };
    }
    
    /// <summary>
    /// Create status for disconnected device
    /// </summary>
    public static BitBangStatus CreateDisconnected(string deviceId)
    {
        return new BitBangStatus
        {
            DeviceId = deviceId,
            IsConnected = false,
            IsResponding = false,
            StatusMessage = "Device disconnected",
            LastUpdated = DateTime.Now
        };
    }
    
    /// <summary>
    /// Get comprehensive status summary
    /// </summary>
    public string GetSummary()
    {
        var healthIcon = HealthLevel switch
        {
            HardwareHealthLevel.Healthy => "✅",
            HardwareHealthLevel.Warning => "⚠️",
            HardwareHealthLevel.Error => "❌",
            HardwareHealthLevel.Disconnected => "🔌❌",
            _ => "❓"
        };
        
        var inputStatus = $"In[PR:{PowerOnReady},PD:{PowerDownHeadsUp}]";
        var outputStatus = $"Out[CF:{CriticalFailSignal},WF:{WorkflowActiveSignal}]";
        var ageInfo = IsFresh ? "Fresh" : $"{Age.TotalSeconds:F0}s old";
        
        return $"{healthIcon} {DeviceId}: {inputStatus} {outputStatus} ({ageInfo})";
    }
    
    public override string ToString()
    {
        return GetSummary();
    }
}

/// <summary>
/// Hardware health levels for status monitoring
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