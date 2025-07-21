namespace SerialPortPool.Core.Models;

/// <summary>
/// Status of a port allocation in the pool management system
/// </summary>
public enum AllocationStatus
{
    /// <summary>
    /// Port is available and ready to be allocated to a client
    /// </summary>
    Available,

    /// <summary>
    /// Port is currently allocated to a client and in active use
    /// </summary>
    Allocated,

    /// <summary>
    /// Port is temporarily reserved for a pending allocation
    /// (e.g., during validation process or brief hold period)
    /// </summary>
    Reserved,

    /// <summary>
    /// Port is in an error state and cannot be allocated
    /// (e.g., hardware failure, driver issues, validation failed)
    /// </summary>
    Error
}