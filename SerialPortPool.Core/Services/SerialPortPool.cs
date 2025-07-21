using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using SerialPortPool.Core.Interfaces;
using SerialPortPool.Core.Models;

namespace SerialPortPool.Core.Services;

/// <summary>
/// Thread-safe implementation of serial port pool management
/// Phase 1: Basic allocation/release with thread-safety ✅
/// Phase 2: Enhanced allocation logic with FTDI validation ✅
/// </summary>
public class SerialPortPool : ISerialPortPool, IDisposable
{
    private readonly ISerialPortDiscovery _discovery;
    private readonly ISerialPortValidator _validator;  // ← Phase 2: NEW!
    private readonly ILogger<SerialPortPool> _logger;
    private readonly ConcurrentDictionary<string, PortAllocation> _allocations = new();
    private readonly SemaphoreSlim _allocationSemaphore = new(1, 1);
    private volatile bool _disposed;

    /// <summary>
    /// Constructor with dependency injection (Phase 2: Enhanced with validator)
    /// </summary>
    public SerialPortPool(
        ISerialPortDiscovery discovery, 
        ISerialPortValidator validator,  // ← Phase 2: NEW!
        ILogger<SerialPortPool> logger)
    {
        _discovery = discovery ?? throw new ArgumentNullException(nameof(discovery));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));  // ← Phase 2: NEW!
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        _logger.LogInformation("SerialPortPool initialized with enhanced validation support (Phase 2)");
    }

    #region Phase 2 - Enhanced Thread-Safe Operations with Validation

    /// <summary>
    /// Allocate an available port from the pool (Phase 2: Enhanced with validation)
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

            // Phase 2: Discover ports with enhanced discovery
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
                // Phase 1: Check if port is already allocated AND ACTIVE (bug fix applied)
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
                
                // Phase 2: Enhanced metadata for tracking
                allocation.Metadata["AllocationPhase"] = "Phase2Enhanced";
                allocation.Metadata["ValidationConfigUsed"] = (config != null).ToString();
                allocation.Metadata["DiscoveryTimestamp"] = DateTime.Now.ToString("O");
                
                _allocations[portInfo.PortName] = allocation;

                _logger.LogInformation($"Successfully allocated port {portInfo.PortName} to client {clientId ?? "Unknown"} " +
                    $"(Session: {allocation.SessionId}, Validation: {portInfo.ValidationStatus}, " +
                    $"FTDI: {(portInfo.IsFtdiDevice ? portInfo.FtdiChipType : "No")})");
                return allocation;
            }

            _logger.LogWarning($"No valid ports found for allocation (client: {clientId ?? "Unknown"}, " +
                $"validation: {(config != null ? "enabled" : "disabled")}, " +
                $"available: {availablePorts.Count()}, valid: {validPorts.Count()})");
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
    /// Release a port back to the pool (Phase 1: Unchanged, working perfectly)
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

            // Phase 1: Session validation (working correctly)
            if (!string.IsNullOrEmpty(sessionId) && allocation.SessionId != sessionId)
            {
                _logger.LogWarning($"Cannot release port {portName} - session ID mismatch (provided: {sessionId}, expected: {allocation.SessionId})");
                return false;
            }

            // Mark as released and update allocation
            allocation.Release();
            
            _logger.LogInformation($"Successfully released port {portName} " +
                $"(Session: {allocation.SessionId}, Duration: {allocation.AllocationDuration.TotalMinutes:F1}min, " +
                $"Client: {allocation.AllocatedTo ?? "Unknown"})");
            
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
    /// Get all current port allocations (Phase 1: Unchanged, working perfectly)
    /// </summary>
    public async Task<IEnumerable<PortAllocation>> GetAllocationsAsync()
    {
        if (_disposed)
        {
            return Enumerable.Empty<PortAllocation>();
        }

        await Task.CompletedTask; // Keep async signature for interface compatibility
        
        var allocations = _allocations.Values.ToList(); // Create snapshot to avoid enumeration issues
        
        _logger.LogDebug($"Retrieved {allocations.Count} port allocations");
        return allocations;
    }

    #endregion

    #region Phase 2 - Enhanced Interface Implementation

    public async Task<IEnumerable<PortAllocation>> GetActiveAllocationsAsync()
    {
        var allAllocations = await GetAllocationsAsync();
        var activeAllocations = allAllocations.Where(a => a.IsActive).ToList();
        
        _logger.LogDebug($"Retrieved {activeAllocations.Count} active port allocations");
        return activeAllocations;
    }

    public async Task<SystemInfo?> GetPortSystemInfoAsync(string portName, bool forceRefresh = false)
    {
        // Phase 2: Placeholder - will be implemented in Phase 3 with caching
        await Task.CompletedTask;
        _logger.LogDebug($"SystemInfo requested for {portName} (forceRefresh: {forceRefresh}) - Phase 3 implementation pending");
        return null;
    }

    /// <summary>
    /// Get available ports count (Phase 2: Enhanced with validation support)
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
                _logger.LogDebug($"Available ports count with validation: {count} " +
                    $"(total unallocated: {unallocatedPorts.Count()}, " +
                    $"validation: {config.Require4232HChip})");
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
        var count = activeAllocations.Count();
        
        _logger.LogDebug($"Allocated ports count: {count}");
        return count;
    }

    public async Task<bool> IsPortAllocatedAsync(string portName)
    {
        await Task.CompletedTask;
        var isAllocated = _allocations.TryGetValue(portName, out var allocation) && allocation.IsActive;
        _logger.LogDebug($"Port {portName} allocation status: {isAllocated}");
        return isAllocated;
    }

    public async Task<PortAllocation?> GetPortAllocationAsync(string portName)
    {
        await Task.CompletedTask;
        _allocations.TryGetValue(portName, out var allocation);
        return allocation;
    }

    /// <summary>
    /// Release all ports for a specific client (Phase 2: Enhanced logging)
    /// </summary>
    public async Task<int> ReleaseAllPortsForClientAsync(string clientId)
    {
        if (string.IsNullOrWhiteSpace(clientId))
        {
            return 0;
        }

        _logger.LogInformation($"Starting bulk release for client: {clientId}");

        var clientAllocations = _allocations.Values.Where(a => a.AllocatedTo == clientId && a.IsActive).ToList();
        int releasedCount = 0;

        foreach (var allocation in clientAllocations)
        {
            if (await ReleasePortAsync(allocation.PortName, allocation.SessionId))
            {
                releasedCount++;
            }
        }

        _logger.LogInformation($"Released {releasedCount}/{clientAllocations.Count} ports for client {clientId}");
        return releasedCount;
    }

    public async Task<int> RefreshPoolAsync()
    {
        // Phase 2: Enhanced refresh with validation awareness
        var discoveredPorts = await _discovery.DiscoverPortsAsync();
        var count = discoveredPorts.Count();
        var ftdiCount = discoveredPorts.Count(p => p.IsFtdiDevice);
        
        _logger.LogInformation($"Pool refresh discovered {count} ports ({ftdiCount} FTDI devices)");
        return count;
    }

    /// <summary>
    /// Get pool statistics (Phase 2: Enhanced with validation metadata)
    /// </summary>
    public async Task<PoolStatistics> GetStatisticsAsync()
    {
        try
        {
            var allAllocations = await GetAllocationsAsync();
            var activeAllocations = allAllocations.Where(a => a.IsActive).ToList();
            var totalPorts = await RefreshPoolAsync();
            var allocatedPorts = activeAllocations.Count;
            
            // Phase 2: Enhanced statistics with validation insights
            var ftdiAllocations = activeAllocations.Where(a => 
                a.Metadata.ContainsKey("IsFtdiDevice") && a.Metadata["IsFtdiDevice"] == "True").Count();
            
            var stats = new PoolStatistics
            {
                TotalPorts = totalPorts,
                AllocatedPorts = allocatedPorts,
                AvailablePorts = totalPorts - allocatedPorts,
                ErrorPorts = 0, // Phase 2: Basic error tracking
                ActiveClients = activeAllocations.Select(a => a.AllocatedTo).Where(c => c != null).Distinct().Count(),
                TotalAllocations = allAllocations.Count(),
                AverageAllocationDurationMinutes = allAllocations.Any() ? 
                    allAllocations.Average(a => a.AllocationDuration.TotalMinutes) : 0,
                GeneratedAt = DateTime.Now
            };
            
            _logger.LogDebug($"Pool statistics generated: {stats}");
            return stats;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating pool statistics");
            return new PoolStatistics { GeneratedAt = DateTime.Now };
        }
    }

    #endregion

    #region Disposal (Phase 1: Unchanged, working perfectly)

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