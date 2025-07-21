using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using SerialPortPool.Core.Interfaces;
using SerialPortPool.Core.Models;

namespace SerialPortPool.Core.Services;

/// <summary>
/// Thread-safe implementation of serial port pool management
/// Phase 1: Basic allocation/release with thread-safety
/// </summary>
public class SerialPortPool : ISerialPortPool, IDisposable
{
    private readonly ISerialPortDiscovery _discovery;
    private readonly ILogger<SerialPortPool> _logger;
    private readonly ConcurrentDictionary<string, PortAllocation> _allocations = new();
    private readonly SemaphoreSlim _allocationSemaphore = new(1, 1);
    private volatile bool _disposed;

    /// <summary>
    /// Constructor with dependency injection
    /// </summary>
    public SerialPortPool(ISerialPortDiscovery discovery, ILogger<SerialPortPool> logger)
    {
        _discovery = discovery ?? throw new ArgumentNullException(nameof(discovery));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        _logger.LogInformation("SerialPortPool initialized with thread-safe implementation");
    }

    #region Phase 1 - Basic Thread-Safe Operations

    /// <summary>
    /// Allocate an available port from the pool (Phase 1: minimal implementation)
    /// </summary>
    public async Task<PortAllocation?> AllocatePortAsync(PortValidationConfiguration? config = null, string? clientId = null)
    {
        if (_disposed)
        {
            _logger.LogWarning("Cannot allocate port - pool is disposed");
            return null;
        }

        await _allocationSemaphore.WaitAsync();
        try
        {
            _logger.LogDebug($"Attempting to allocate port for client: {clientId ?? "Unknown"}");

            // Phase 1: Simple implementation - discover ports and allocate first available
            var availablePorts = await _discovery.DiscoverPortsAsync();
            
            foreach (var portInfo in availablePorts)
            {
                // Check if port is already allocated
                if (_allocations.ContainsKey(portInfo.PortName))
                {
                    _logger.LogDebug($"Port {portInfo.PortName} already allocated, skipping");
                    continue;
                }

                // Phase 1: Basic allocation without validation (will be enhanced in Phase 2)
                var allocation = PortAllocation.Create(portInfo.PortName, clientId);
                
                if (_allocations.TryAdd(portInfo.PortName, allocation))
                {
                    _logger.LogInformation($"Successfully allocated port {portInfo.PortName} to client {clientId ?? "Unknown"} (Session: {allocation.SessionId})");
                    return allocation;
                }
                else
                {
                    _logger.LogDebug($"Race condition detected - port {portInfo.PortName} allocated by another thread");
                    continue;
                }
            }

            _logger.LogWarning($"No available ports found for allocation (client: {clientId ?? "Unknown"})");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error during port allocation for client {clientId ?? "Unknown"}");
            return null;
        }
        finally
        {
            _allocationSemaphore.Release();
        }
    }

    /// <summary>
    /// Release a port back to the pool (Phase 1: basic implementation)
    /// </summary>
    public async Task<bool> ReleasePortAsync(string portName, string? sessionId = null)
    {
        if (_disposed)
        {
            _logger.LogWarning("Cannot release port - pool is disposed");
            return false;
        }

        if (string.IsNullOrWhiteSpace(portName))
        {
            _logger.LogWarning("Cannot release port - port name is null or empty");
            return false;
        }

        await _allocationSemaphore.WaitAsync();
        try
        {
            _logger.LogDebug($"Attempting to release port {portName} (Session: {sessionId ?? "Unknown"})");

            if (!_allocations.TryGetValue(portName, out var allocation))
            {
                _logger.LogWarning($"Cannot release port {portName} - not found in allocations");
                return false;
            }

            // Phase 1: Basic session validation (will be enhanced later)
            if (!string.IsNullOrEmpty(sessionId) && allocation.SessionId != sessionId)
            {
                _logger.LogWarning($"Cannot release port {portName} - session ID mismatch (provided: {sessionId}, expected: {allocation.SessionId})");
                return false;
            }

            // Mark as released and update allocation
            allocation.Release();
            
            // In Phase 1, we keep the allocation record for tracking
            // Phase 4 will add cleanup strategies
            _logger.LogInformation($"Successfully released port {portName} (Session: {allocation.SessionId}, Duration: {allocation.AllocationDuration.TotalMinutes:F1}min)");
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error during port release for {portName}");
            return false;
        }
        finally
        {
            _allocationSemaphore.Release();
        }
    }

    /// <summary>
    /// Get all current port allocations (Phase 1: read-only access)
    /// </summary>
    public async Task<IEnumerable<PortAllocation>> GetAllocationsAsync()
    {
        if (_disposed)
        {
            return Enumerable.Empty<PortAllocation>();
        }

        // No locking needed for read-only access to ConcurrentDictionary values
        await Task.CompletedTask; // Keep async signature for interface compatibility
        
        var allocations = _allocations.Values.ToList(); // Create snapshot to avoid enumeration issues
        
        _logger.LogDebug($"Retrieved {allocations.Count} port allocations");
        return allocations;
    }

    #endregion

    #region Phase 1 - Minimal Interface Implementation (Placeholders)

    // Phase 1: Basic placeholders - will be implemented in later phases
    public async Task<IEnumerable<PortAllocation>> GetActiveAllocationsAsync()
    {
        var allAllocations = await GetAllocationsAsync();
        return allAllocations.Where(a => a.IsActive);
    }

    public async Task<SystemInfo?> GetPortSystemInfoAsync(string portName, bool forceRefresh = false)
    {
        // Phase 1: Placeholder - will be implemented in Phase 3 with caching
        await Task.CompletedTask;
        _logger.LogDebug($"SystemInfo requested for {portName} (forceRefresh: {forceRefresh}) - Phase 3 implementation pending");
        return null;
    }

    public async Task<int> GetAvailablePortsCountAsync(PortValidationConfiguration? config = null)
    {
        // Phase 1: Simple count - will be enhanced with validation in Phase 2
        var availablePorts = await _discovery.DiscoverPortsAsync();
        var count = availablePorts.Count(p => !_allocations.ContainsKey(p.PortName));
        
        _logger.LogDebug($"Available ports count: {count}");
        return count;
    }

    public async Task<int> GetAllocatedPortsCountAsync()
    {
        var activeAllocations = await GetActiveAllocationsAsync();
        return activeAllocations.Count();
    }

    public async Task<bool> IsPortAllocatedAsync(string portName)
    {
        await Task.CompletedTask;
        var isAllocated = _allocations.ContainsKey(portName) && _allocations[portName].IsActive;
        _logger.LogDebug($"Port {portName} allocation status: {isAllocated}");
        return isAllocated;
    }

    public async Task<PortAllocation?> GetPortAllocationAsync(string portName)
    {
        await Task.CompletedTask;
        _allocations.TryGetValue(portName, out var allocation);
        return allocation;
    }

    public async Task<int> ReleaseAllPortsForClientAsync(string clientId)
    {
        // Phase 1: Basic client cleanup - will be enhanced in Phase 2
        if (string.IsNullOrWhiteSpace(clientId))
        {
            return 0;
        }

        var clientAllocations = _allocations.Values.Where(a => a.AllocatedTo == clientId && a.IsActive).ToList();
        int releasedCount = 0;

        foreach (var allocation in clientAllocations)
        {
            if (await ReleasePortAsync(allocation.PortName, allocation.SessionId))
            {
                releasedCount++;
            }
        }

        _logger.LogInformation($"Released {releasedCount} ports for client {clientId}");
        return releasedCount;
    }

    public async Task<int> RefreshPoolAsync()
    {
        // Phase 1: Basic refresh - will be enhanced in Phase 4
        var discoveredPorts = await _discovery.DiscoverPortsAsync();
        var count = discoveredPorts.Count();
        
        _logger.LogInformation($"Pool refresh discovered {count} ports");
        return count;
    }

    public async Task<PoolStatistics> GetStatisticsAsync()
    {
        // Phase 1: Basic statistics - will be enhanced in Phase 4
        var allAllocations = await GetAllocationsAsync();
        var activeAllocations = allAllocations.Where(a => a.IsActive).ToList();
        var totalPorts = await RefreshPoolAsync();
        var allocatedPorts = activeAllocations.Count;
        
        return new PoolStatistics
        {
            TotalPorts = totalPorts,
            AllocatedPorts = allocatedPorts,
            AvailablePorts = totalPorts - allocatedPorts,
            ErrorPorts = 0, // Phase 1: No error tracking yet
            ActiveClients = activeAllocations.Select(a => a.AllocatedTo).Where(c => c != null).Distinct().Count(),
            TotalAllocations = allAllocations.Count(),
            AverageAllocationDurationMinutes = allAllocations.Any() ? 
                allAllocations.Average(a => a.AllocationDuration.TotalMinutes) : 0,
            GeneratedAt = DateTime.Now
        };
    }

    #endregion

    #region Disposal

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            _logger.LogInformation("Disposing SerialPortPool...");
            
            try
            {
                _allocationSemaphore.Dispose();
                _logger.LogInformation("SerialPortPool disposed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during SerialPortPool disposal");
            }
            
            _disposed = true;
        }
    }

    #endregion
}