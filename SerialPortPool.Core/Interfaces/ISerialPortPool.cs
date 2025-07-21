using SerialPortPool.Core.Models;

namespace SerialPortPool.Core.Interfaces;

/// <summary>
/// Interface for managing a pool of serial ports with allocation/release functionality
/// </summary>
public interface ISerialPortPool
{
    /// <summary>
    /// Allocate an available port from the pool with optional validation configuration
    /// </summary>
    /// <param name="config">Optional validation configuration to filter eligible ports</param>
    /// <param name="clientId">Optional client identifier for tracking</param>
    /// <returns>Port allocation if successful, null if no ports available</returns>
    Task<PortAllocation?> AllocatePortAsync(PortValidationConfiguration? config = null, string? clientId = null);

    /// <summary>
    /// Release a port back to the pool, making it available for other clients
    /// </summary>
    /// <param name="portName">Name of the port to release</param>
    /// <param name="sessionId">Session ID of the allocation (for verification)</param>
    /// <returns>True if port was successfully released, false if not found or session mismatch</returns>
    Task<bool> ReleasePortAsync(string portName, string? sessionId = null);

    /// <summary>
    /// Get all current port allocations in the pool
    /// </summary>
    /// <returns>Collection of all port allocations (active and released)</returns>
    Task<IEnumerable<PortAllocation>> GetAllocationsAsync();

    /// <summary>
    /// Get only currently active (allocated) port allocations
    /// </summary>
    /// <returns>Collection of active port allocations</returns>
    Task<IEnumerable<PortAllocation>> GetActiveAllocationsAsync();

    /// <summary>
    /// Get detailed system information for a specific port
    /// </summary>
    /// <param name="portName">Name of the port to query</param>
    /// <param name="forceRefresh">Force reading from hardware instead of cache</param>
    /// <returns>System information for the port, null if port not found</returns>
    Task<SystemInfo?> GetPortSystemInfoAsync(string portName, bool forceRefresh = false);

    /// <summary>
    /// Get count of available ports that can be allocated
    /// </summary>
    /// <param name="config">Optional validation configuration to filter eligible ports</param>
    /// <returns>Number of ports available for allocation</returns>
    Task<int> GetAvailablePortsCountAsync(PortValidationConfiguration? config = null);

    /// <summary>
    /// Get count of currently allocated ports
    /// </summary>
    /// <returns>Number of ports currently allocated</returns>
    Task<int> GetAllocatedPortsCountAsync();

    /// <summary>
    /// Check if a specific port is currently allocated
    /// </summary>
    /// <param name="portName">Name of the port to check</param>
    /// <returns>True if port is allocated, false otherwise</returns>
    Task<bool> IsPortAllocatedAsync(string portName);

    /// <summary>
    /// Get allocation information for a specific port
    /// </summary>
    /// <param name="portName">Name of the port to query</param>
    /// <returns>Port allocation if found, null otherwise</returns>
    Task<PortAllocation?> GetPortAllocationAsync(string portName);

    /// <summary>
    /// Force release all allocations for a specific client
    /// (useful for cleanup when client disconnects unexpectedly)
    /// </summary>
    /// <param name="clientId">Client identifier</param>
    /// <returns>Number of ports released</returns>
    Task<int> ReleaseAllPortsForClientAsync(string clientId);

    /// <summary>
    /// Refresh the pool by rediscovering available ports
    /// (detects newly connected or disconnected devices)
    /// </summary>
    /// <returns>Number of ports discovered after refresh</returns>
    Task<int> RefreshPoolAsync();

    /// <summary>
    /// Get basic statistics about the pool
    /// </summary>
    /// <returns>Pool statistics</returns>
    Task<PoolStatistics> GetStatisticsAsync();
}