namespace SerialPortPool.Core.Models;

/// <summary>
/// Status of a serial port in the pool
/// </summary>
public enum PortStatus
{
    /// <summary>
    /// Port status is unknown or not yet determined
    /// </summary>
    Unknown,

    /// <summary>
    /// Port is available for allocation
    /// </summary>
    Available,

    /// <summary>
    /// Port is currently allocated and in use
    /// </summary>
    Allocated,

    /// <summary>
    /// Port is physically connected but not allocated
    /// </summary>
    Connected,

    /// <summary>
    /// Port was previously seen but is now disconnected
    /// </summary>
    Disconnected,

    /// <summary>
    /// Port is in an error state
    /// </summary>
    Error
}
