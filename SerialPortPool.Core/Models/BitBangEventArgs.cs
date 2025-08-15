// ===================================================================
// SPRINT 9: BitBang Event Args - GPIO Event Models
// File: SerialPortPool.Core/Models/BitBangEventArgs.cs
// Purpose: Event arguments for GPIO state changes and hardware events
// ===================================================================

namespace SerialPortPool.Core.Models;

/// <summary>
/// Base event arguments for bit bang protocol GPIO events
/// </summary>
public class BitBangEventArgs : EventArgs
{
    /// <summary>
    /// Type of hardware event that occurred
    /// </summary>
    public BitBangEventType EventType { get; set; }
    
    /// <summary>
    /// New state after the event
    /// </summary>
    public bool NewState { get; set; }
    
    /// <summary>
    /// Previous state before the event
    /// </summary>
    public bool PreviousState { get; set; }
    
    /// <summary>
    /// When the event occurred
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.Now;
    
    /// <summary>
    /// Optional event message or details
    /// </summary>
    public string? Message { get; set; }
    
    /// <summary>
    /// GPIO bit index that changed (0-7)
    /// </summary>
    public int BitIndex { get; set; }
    
    /// <summary>
    /// Raw GPIO byte value
    /// </summary>
    public byte RawValue { get; set; }
    
    public override string ToString()
    {
        var stateChange = $"{PreviousState} → {NewState}";
        return $"{EventType}: Bit{BitIndex} {stateChange} at {Timestamp:HH:mm:ss.fff}";
    }
}

/// <summary>
/// Event arguments for Power On Ready signal changes
/// </summary>
public class PowerReadyEventArgs : EventArgs
{
    /// <summary>
    /// Is power ready for workflow start?
    /// </summary>
    public bool IsPowerReady { get; set; }
    
    /// <summary>
    /// When the power state changed
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.Now;
    
    /// <summary>
    /// How long power has been in this state
    /// </summary>
    public TimeSpan StateDuration { get; set; }
    
    /// <summary>
    /// Optional message about power state
    /// </summary>
    public string? Message { get; set; }
    
    public override string ToString()
    {
        var status = IsPowerReady ? "READY" : "NOT READY";
        return $"Power On Ready: {status} (Duration: {StateDuration.TotalSeconds:F1}s)";
    }
}

/// <summary>
/// Event arguments for Power Down Heads-Up signal changes
/// </summary>
public class PowerDownEventArgs : EventArgs
{
    /// <summary>
    /// Is power down requested?
    /// </summary>
    public bool IsPowerDownRequested { get; set; }
    
    /// <summary>
    /// When the power down state changed
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.Now;
    
    /// <summary>
    /// Urgency level of power down request
    /// </summary>
    public PowerDownUrgency Urgency { get; set; } = PowerDownUrgency.Normal;
    
    /// <summary>
    /// Expected time until power down
    /// </summary>
    public TimeSpan? EstimatedTimeUntilPowerDown { get; set; }
    
    /// <summary>
    /// Optional message about power down request
    /// </summary>
    public string? Message { get; set; }
    
    public override string ToString()
    {
        var status = IsPowerDownRequested ? "REQUESTED" : "CLEARED";
        var urgency = Urgency != PowerDownUrgency.Normal ? $" ({Urgency})" : "";
        return $"Power Down Heads-Up: {status}{urgency}";
    }
}

/// <summary>
/// Event arguments for hardware connection status changes
/// </summary>
public class HardwareConnectionEventArgs : EventArgs
{
    /// <summary>
    /// Is hardware currently connected?
    /// </summary>
    public bool IsConnected { get; set; }
    
    /// <summary>
    /// Previous connection state
    /// </summary>
    public bool WasConnected { get; set; }
    
    /// <summary>
    /// When the connection state changed
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.Now;
    
    /// <summary>
    /// Reason for connection change
    /// </summary>
    public string? Reason { get; set; }
    
    /// <summary>
    /// Hardware device identifier
    /// </summary>
    public string? DeviceId { get; set; }
    
    /// <summary>
    /// Connection error details (if disconnected due to error)
    /// </summary>
    public string? ErrorDetails { get; set; }
    
    /// <summary>
    /// How long the hardware was connected before disconnect
    /// </summary>
    public TimeSpan? ConnectionDuration { get; set; }
    
    public override string ToString()
    {
        var status = IsConnected ? "CONNECTED" : "DISCONNECTED";
        var device = !string.IsNullOrEmpty(DeviceId) ? $" ({DeviceId})" : "";
        var reason = !string.IsNullOrEmpty(Reason) ? $" - {Reason}" : "";
        return $"Hardware Connection: {status}{device}{reason}";
    }
}

/// <summary>
/// Types of bit bang protocol events
/// </summary>
public enum BitBangEventType
{
    /// <summary>
    /// Power On Ready input signal changed
    /// </summary>
    PowerOnReadyChanged,
    
    /// <summary>
    /// Power Down Heads-Up input signal changed
    /// </summary>
    PowerDownHeadsUpChanged,
    
    /// <summary>
    /// Critical Fail output signal was triggered
    /// </summary>
    CriticalFailTriggered,
    
    /// <summary>
    /// Critical Fail output signal was cleared
    /// </summary>
    CriticalFailCleared,
    
    /// <summary>
    /// Workflow Active output signal changed
    /// </summary>
    WorkflowActiveChanged,
    
    /// <summary>
    /// Hardware device was disconnected
    /// </summary>
    HardwareDisconnected,
    
    /// <summary>
    /// Hardware device was connected
    /// </summary>
    HardwareConnected,
    
    /// <summary>
    /// Hardware operation error occurred
    /// </summary>
    HardwareError,
    
    /// <summary>
    /// Hardware initialization completed
    /// </summary>
    HardwareInitialized,
    
    /// <summary>
    /// Hardware test completed
    /// </summary>
    HardwareTestCompleted,
    
    /// <summary>
    /// Generic GPIO input changed
    /// </summary>
    InputChanged,
    
    /// <summary>
    /// Generic GPIO output changed
    /// </summary>
    OutputChanged
}

/// <summary>
/// Power down urgency levels
/// </summary>
public enum PowerDownUrgency
{
    /// <summary>
    /// Normal power down - finish current operations gracefully
    /// </summary>
    Normal,
    
    /// <summary>
    /// Urgent power down - complete current test but don't start new ones
    /// </summary>
    Urgent,
    
    /// <summary>
    /// Emergency power down - stop immediately
    /// </summary>
    Emergency
}