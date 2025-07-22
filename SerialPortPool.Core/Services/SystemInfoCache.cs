// SerialPortPool.Core/Services/SystemInfoCache.cs - CLEAN PRODUCTION VERSION
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using SerialPortPool.Core.Interfaces;
using SerialPortPool.Core.Models;

namespace SerialPortPool.Core.Services;

/// <summary>
/// Smart caching system for SystemInfo with TTL and background cleanup
/// Phase 3: High-performance EEPROM data caching with proper interface implementation
/// </summary>
public class SystemInfoCache : ISystemInfoCache
{
    private readonly IFtdiDeviceReader _ftdiReader;
    private readonly ILogger<SystemInfoCache> _logger;
    private readonly ConcurrentDictionary<string, CacheEntry> _cache = new();
    private readonly TimeSpan _defaultTtl;
    private readonly Timer _cleanupTimer;
    private volatile bool _disposed;

    // Statistics tracking
    private long _cacheHits = 0;
    private long _cacheMisses = 0;

    /// <summary>
    /// Constructor with configurable TTL
    /// </summary>
    public SystemInfoCache(
        IFtdiDeviceReader ftdiReader,
        ILogger<SystemInfoCache> logger,
        TimeSpan? defaultTtl = null)
    {
        _ftdiReader = ftdiReader ?? throw new ArgumentNullException(nameof(ftdiReader));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _defaultTtl = defaultTtl ?? TimeSpan.FromMinutes(5);

        // Setup background cleanup timer (every 60 seconds)
        _cleanupTimer = new Timer(PerformCleanup, null, TimeSpan.FromSeconds(60), TimeSpan.FromSeconds(60));
        
        _logger.LogInformation($"SystemInfoCache initialized with TTL: {_defaultTtl.TotalMinutes:F1} minutes");
    }

    /// <summary>
    /// Get SystemInfo with smart caching (Phase 3 main method)
    /// </summary>
    public virtual async Task<SystemInfo?> GetSystemInfoAsync(string portName, bool forceRefresh = false)
    {
        if (_disposed)
        {
            _logger.LogWarning("Cannot get SystemInfo - cache is disposed");
            return null;
        }

        if (string.IsNullOrWhiteSpace(portName))
        {
            _logger.LogWarning("Cannot get SystemInfo - port name is null or empty");
            return null;
        }

        try
        {
            _logger.LogDebug($"Getting SystemInfo for {portName} (forceRefresh: {forceRefresh})");

            // Check cache first (unless force refresh)
            if (!forceRefresh && _cache.TryGetValue(portName, out var cacheEntry))
            {
                if (!cacheEntry.IsExpired)
                {
                    // Cache hit - fresh data
                    cacheEntry.LastAccessed = DateTime.Now;
                    cacheEntry.AccessCount++;
                    Interlocked.Increment(ref _cacheHits);
                    
                    _logger.LogDebug($"Cache HIT for {portName} (age: {DateTime.Now - cacheEntry.SystemInfo.LastRead:F1}min, access #{cacheEntry.AccessCount})");
                    return cacheEntry.SystemInfo;
                }
                else if (!cacheEntry.IsRefreshing)
                {
                    // Cache expired but not refreshing - trigger background refresh and return stale data
                    _logger.LogDebug($"Cache STALE for {portName} - triggering background refresh");
                    _ = Task.Run(() => RefreshCacheEntryAsync(portName, cacheEntry));
                    
                    cacheEntry.LastAccessed = DateTime.Now;
                    cacheEntry.AccessCount++;
                    Interlocked.Increment(ref _cacheHits);
                    
                    return cacheEntry.SystemInfo;
                }
                else
                {
                    // Currently refreshing - return stale data for now
                    _logger.LogDebug($"Cache REFRESHING for {portName} - returning stale data");
                    cacheEntry.LastAccessed = DateTime.Now;
                    Interlocked.Increment(ref _cacheHits);
                    return cacheEntry.SystemInfo;
                }
            }

            // Cache miss - read from device and cache
            Interlocked.Increment(ref _cacheMisses);
            _logger.LogDebug($"Cache MISS for {portName} - reading from device");

            var systemInfo = await ReadSystemInfoFromDeviceAsync(portName);
            if (systemInfo != null)
            {
                // Cache the result
                var newEntry = new CacheEntry
                {
                    SystemInfo = systemInfo,
                    ExpiresAt = DateTime.Now.Add(_defaultTtl),
                    IsRefreshing = false,
                    LastAccessed = DateTime.Now,
                    AccessCount = 1
                };

                _cache.AddOrUpdate(portName, newEntry, (key, existing) => newEntry);
                
                _logger.LogInformation($"SystemInfo cached for {portName} (expires: {newEntry.ExpiresAt:HH:mm:ss})");
            }

            return systemInfo;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting SystemInfo for {portName}");
            return null;
        }
    }

    /// <summary>
    /// Invalidate cache entry for a specific port
    /// </summary>
    public virtual void InvalidateCache(string portName)
    {
        if (string.IsNullOrWhiteSpace(portName))
            return;

        if (_cache.TryRemove(portName, out var removedEntry))
        {
            _logger.LogInformation($"Cache invalidated for {portName} (was accessed {removedEntry.AccessCount} times)");
        }
        else
        {
            _logger.LogDebug($"Cache invalidation requested for {portName} but entry not found");
        }
    }

    /// <summary>
    /// Clear all expired entries from cache
    /// </summary>
    public virtual void ClearExpiredEntries()
    {
        try
        {
            var expiredKeys = _cache.Where(kvp => kvp.Value.IsExpired && !kvp.Value.IsRefreshing)
                                   .Select(kvp => kvp.Key)
                                   .ToList();

            int removedCount = 0;
            foreach (var key in expiredKeys)
            {
                if (_cache.TryRemove(key, out var removed))
                {
                    removedCount++;
                    _logger.LogDebug($"Removed expired cache entry for {key} (age: {DateTime.Now - removed.SystemInfo.LastRead:F1}min)");
                }
            }

            if (removedCount > 0)
            {
                _logger.LogInformation($"Cache cleanup removed {removedCount} expired entries");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during cache cleanup");
        }
    }

    /// <summary>
    /// Get cache statistics for monitoring
    /// </summary>
    public virtual CacheStatistics GetStatistics()
    {
        var entries = _cache.Values.ToList();
        var expiredCount = entries.Count(e => e.IsExpired);

        return new CacheStatistics
        {
            TotalEntries = entries.Count,
            ExpiredEntries = expiredCount,
            TotalHits = _cacheHits,
            TotalMisses = _cacheMisses,
            GeneratedAt = DateTime.Now
        };
    }

    /// <summary>
    /// Clear all cache entries
    /// </summary>
    public virtual void ClearAll()
    {
        var count = _cache.Count;
        _cache.Clear();
        _logger.LogInformation($"Cache cleared - removed {count} entries");
    }

    #region Private Methods

    /// <summary>
    /// Read SystemInfo directly from device (bypassing cache)
    /// </summary>
    private async Task<SystemInfo?> ReadSystemInfoFromDeviceAsync(string portName)
    {
        try
        {
            var ftdiInfo = await _ftdiReader.ReadDeviceInfoAsync(portName);
            if (ftdiInfo == null)
            {
                _logger.LogDebug($"No FTDI device found for {portName}");
                return SystemInfo.CreateError(portName, "Not an FTDI device");
            }

            var systemInfo = SystemInfo.FromFtdiDevice(ftdiInfo);
            
            var eepromData = await _ftdiReader.ReadEepromDataAsync(portName);
            foreach (var kvp in eepromData)
            {
                systemInfo.EepromData[kvp.Key] = kvp.Value;
            }

            _logger.LogDebug($"Successfully read SystemInfo for {portName} from device");
            return systemInfo;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error reading SystemInfo from device for {portName}");
            return SystemInfo.CreateError(portName, $"Read error: {ex.Message}");
        }
    }

    /// <summary>
    /// Background refresh of cache entry
    /// </summary>
    private async Task RefreshCacheEntryAsync(string portName, CacheEntry entry)
    {
        if (entry.IsRefreshing)
            return;

        try
        {
            entry.IsRefreshing = true;
            _logger.LogDebug($"Background refresh started for {portName}");

            var refreshedInfo = await ReadSystemInfoFromDeviceAsync(portName);
            if (refreshedInfo != null)
            {
                entry.SystemInfo = refreshedInfo;
                entry.ExpiresAt = DateTime.Now.Add(_defaultTtl);
                entry.LastAccessed = DateTime.Now;
                
                _logger.LogDebug($"Background refresh completed for {portName}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error during background refresh for {portName}");
        }
        finally
        {
            entry.IsRefreshing = false;
        }
    }

    /// <summary>
    /// Periodic cleanup timer callback
    /// </summary>
    private void PerformCleanup(object? state)
    {
        if (!_disposed)
        {
            ClearExpiredEntries();
        }
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
            _logger.LogInformation("Disposing SystemInfoCache...");
            
            try
            {
                _cleanupTimer?.Dispose();
                ClearAll();
                _logger.LogInformation("SystemInfoCache disposed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during SystemInfoCache disposal");
            }
            
            _disposed = true;
        }
    }

    #endregion
}