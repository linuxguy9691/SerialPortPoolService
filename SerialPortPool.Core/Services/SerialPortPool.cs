// SerialPortPool.Core/Services/SerialPortPool.cs - VERSION COMPLÈTE PHASE 3
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using SerialPortPool.Core.Interfaces;
using SerialPortPool.Core.Models;

namespace SerialPortPool.Core.Services;

/// <summary>
/// Thread-safe implementation of serial port pool management
/// Phase 1: Basic allocation/release with thread-safety ✅ COMPLETED
/// Phase 2: Enhanced allocation with FTDI validation ✅ COMPLETED  
/// Phase 3: Smart caching layer with SystemInfo ✅ COMPLETED - Using ISystemInfoCache interface
/// </summary>
public class SerialPortPool : ISerialPortPool, IDisposable
{
    private readonly ISerialPortDiscovery _discovery;
    private readonly ISerialPortValidator _validator;
    private readonly ISystemInfoCache _systemInfoCache;  // ← PHASE 3: Using interface for testability
    private readonly ILogger<SerialPortPool> _logger;
    private readonly ConcurrentDictionary<string, PortAllocation> _allocations = new();
    private readonly SemaphoreSlim _allocationSemaphore = new(1, 1);
    private volatile bool _disposed;

    /// <summary>
    /// Constructor with dependency injection - Phase 3 Enhanced with Smart Caching
    /// </summary>
    public SerialPortPool(
        ISerialPortDiscovery discovery, 
        ISerialPortValidator validator,
        ISystemInfoCache systemInfoCache,  // ← PHASE 3: Interface parameter
        ILogger<SerialPortPool> logger)
    {
        _discovery = discovery ?? throw new ArgumentNullException(nameof(discovery));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _systemInfoCache = systemInfoCache ?? throw new ArgumentNullException(nameof(systemInfoCache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        _logger.LogInformation("SerialPortPool initialized with enhanced validation support + smart caching (Phase 3)");
    }

    #region Phase 1 & 2 - Enhanced Thread-Safe Operations with Validation

    /// <summary>
    /// Allocate an available port from the pool with enhanced validation support (Phase 2)
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
            _logger.LogDebug($"Attempting to allocate port for client: {clientId ?? "Unknown"} with validation config: {(config != null ? "Custom" : "Default")}");

            // Phase 2: Discover ports with validation filtering
            var availablePorts = await _discovery.DiscoverPortsAsync();
            
            // Phase 2: Apply validation filtering if config provided
            IEnumerable<SerialPortInfo> validPorts = availablePorts;
            if (config != null)
            {
                validPorts = await _validator.GetValidPortsAsync(availablePorts, config);
                _logger.LogDebug($"Validation filtered ports: {availablePorts.Count()} → {validPorts.Count()}");
            }
            
            foreach (var portInfo in validPorts)
            {
                // Check if port is already allocated AND ACTIVE (Phase 1 thread-safety)
                if (_allocations.TryGetValue(portInfo.PortName, out var existingAllocation) 
                    && existingAllocation.IsActive)
                {
                    _logger.LogDebug($"Port {portInfo.PortName} already allocated and active, skipping");
                    continue;
                }

                // Phase 2: Enhanced allocation with validation result logging
                var allocation = PortAllocation.Create(portInfo.PortName, clientId);
                
                // Phase 2: Store validation metadata in allocation
                if (portInfo.ValidationResult != null)
                {
                    allocation.Metadata["ValidationScore"] = portInfo.ValidationResult.ValidationScore.ToString();
                    allocation.Metadata["ValidationReason"] = portInfo.ValidationResult.Reason;
                    allocation.Metadata["IsFtdiDevice"] = portInfo.IsFtdiDevice.ToString();
                    allocation.Metadata["FtdiChipType"] = portInfo.FtdiChipType;
                    
                    if (portInfo.FtdiInfo != null)
                    {
                        allocation.Metadata["FtdiVendorId"] = portInfo.FtdiInfo.VendorId;
                        allocation.Metadata["FtdiProductId"] = portInfo.FtdiInfo.ProductId;
                        allocation.Metadata["FtdiSerialNumber"] = portInfo.FtdiInfo.SerialNumber;
                        allocation.Metadata["Is4232H"] = portInfo.FtdiInfo.Is4232H.ToString();
                    }
                }
                
                _allocations[portInfo.PortName] = allocation;

                _logger.LogInformation($"Successfully allocated port {portInfo.PortName} to client {clientId ?? "Unknown"} (Session: {allocation.SessionId}, Validation: {portInfo.ValidationStatus})");
                return allocation;
            }

            _logger.LogWarning($"No valid ports found for allocation (client: {clientId ?? "Unknown"}, validation: {(config != null ? "enabled" : "disabled")})");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error during enhanced port allocation for client {clientId ?? "Unknown"}");
            return null;
        }
        finally
        {
            _allocationSemaphore.Release();
        }
    }

    /// <summary>
    /// Release a port back to the pool (Phase 1: proven thread-safe implementation)
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

            // Phase 1: Session validation (security feature)
            if (!string.IsNullOrEmpty(sessionId) && allocation.SessionId != sessionId)
            {
                _logger.LogWarning($"Cannot release port {portName} - session ID mismatch (provided: {sessionId}, expected: {allocation.SessionId})");
                return false;
            }

            // Mark as released and update allocation
            allocation.Release();
            
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
    /// Get all current port allocations (Phase 1: thread-safe read-only access)
    /// </summary>
    public async Task<IEnumerable<PortAllocation>> GetAllocationsAsync()
    {
        if (_disposed)
        {
            return Enumerable.Empty<PortAllocation>();
        }

        await Task.CompletedTask; 
        
        var allocations = _allocations.Values.ToList(); 
        
        _logger.LogDebug($"Retrieved {allocations.Count} port allocations");
        return allocations;
    }

    #endregion

    #region Phase 3 - Smart Caching Implementation

    /// <summary>
    /// Get system information for a specific port with smart caching (Phase 3 main feature)
    /// </summary>
    public async Task<SystemInfo?> GetPortSystemInfoAsync(string portName, bool forceRefresh = false)
    {
        if (_disposed)
        {
            _logger.LogWarning("Cannot get SystemInfo - pool is disposed");
            return null;
        }

        if (string.IsNullOrWhiteSpace(portName))
        {
            _logger.LogWarning("Cannot get SystemInfo - port name is null or empty");
            return null;
        }

        try
        {
            _logger.LogDebug($"Getting SystemInfo for {portName} via smart cache (forceRefresh: {forceRefresh})");

            // Phase 3: Use smart caching system
            var systemInfo = await _systemInfoCache.GetSystemInfoAsync(portName, forceRefresh);
            
            if (systemInfo != null && systemInfo.IsDataValid)
            {
                _logger.LogInformation($"SystemInfo retrieved for {portName}: {systemInfo.GetSummary()}");
                return systemInfo;
            }
            else if (systemInfo != null)
            {
                _logger.LogWarning($"SystemInfo retrieved for {portName} but has errors: {systemInfo.ErrorMessage}");
                return systemInfo;
            }
            else
            {
                _logger.LogWarning($"No SystemInfo available for {portName}");
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting SystemInfo for {portName}");
            return SystemInfo.CreateError(portName, $"Pool error: {ex.Message}");
        }
    }

    #endregion

    #region ISerialPortPool Interface Implementation

    public async Task<IEnumerable<PortAllocation>> GetActiveAllocationsAsync()
    {
        var allAllocations = await GetAllocationsAsync();
        return allAllocations.Where(a => a.IsActive);
    }

    /// <summary>
    /// Get available ports count with enhanced validation support (Phase 2)
    /// </summary>
    public async Task<int> GetAvailablePortsCountAsync(PortValidationConfiguration? config = null)
    {
        try
        {
            // Phase 2: Enhanced count with validation support
            var availablePorts = await _discovery.DiscoverPortsAsync();
            
            // Filter out already allocated ports
            var unallocatedPorts = availablePorts.Where(p => 
                !_allocations.TryGetValue(p.PortName, out var allocation) || !allocation.IsActive);
            
            // Phase 2: Apply validation if config provided
            if (config != null)
            {
                var validPorts = await _validator.GetValidPortsAsync(unallocatedPorts, config);
                var count = validPorts.Count();
                _logger.LogDebug($"Available ports count with validation: {count} (total unallocated: {unallocatedPorts.Count()})");
                return count;
            }
            else
            {
                var count = unallocatedPorts.Count();
                _logger.LogDebug($"Available ports count (no validation): {count}");
                return count;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting available ports count");
            return 0;
        }
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
        // Phase 1: Proven client cleanup implementation
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
        // Phase 2: Enhanced refresh
        var discoveredPorts = await _discovery.DiscoverPortsAsync();
        var count = discoveredPorts.Count();
        
        _logger.LogInformation($"Pool refresh discovered {count} ports");
        return count;
    }

    /// <summary>
    /// Enhanced statistics with cache metrics (Phase 3)
    /// </summary>
    public async Task<PoolStatistics> GetStatisticsAsync()
    {
        // Phase 2: Enhanced statistics with validation awareness + Phase 3: Cache metrics
        var allAllocations = await GetAllocationsAsync();
        var activeAllocations = allAllocations.Where(a => a.IsActive).ToList();
        var totalPorts = await RefreshPoolAsync();
        var allocatedPorts = activeAllocations.Count;
        
        // Phase 2: Count FTDI devices in active allocations
        var ftdiAllocations = activeAllocations.Where(a => 
            a.Metadata.ContainsKey("IsFtdiDevice") && 
            a.Metadata["IsFtdiDevice"] == "True").Count();
        
        var stats = new PoolStatistics
        {
            TotalPorts = totalPorts,
            AllocatedPorts = allocatedPorts,
            AvailablePorts = totalPorts - allocatedPorts,
            ErrorPorts = 0, // Basic error tracking
            ActiveClients = activeAllocations.Select(a => a.AllocatedTo).Where(c => c != null).Distinct().Count(),
            TotalAllocations = allAllocations.Count(),
            AverageAllocationDurationMinutes = allAllocations.Any() ? 
                allAllocations.Average(a => a.AllocationDuration.TotalMinutes) : 0,
            GeneratedAt = DateTime.Now
        };
        
        // Phase 3: Add cache statistics to logging
        try
        {
            var cacheStats = _systemInfoCache.GetStatistics();
            _logger.LogDebug($"Pool statistics generated with cache metrics: {stats} | Cache: {cacheStats}");
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Could not get cache statistics");
        }
        
        return stats;
    }

    #endregion

    #region Disposal (Phase 1-3 - Proper Resource Cleanup)

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
                
                // Phase 3: Dispose cache
                _systemInfoCache?.Dispose();
                
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