// SerialPortPool.Core/Interfaces/ISystemInfoCache.cs - FIXED
using SerialPortPool.Core.Models;

namespace SerialPortPool.Core.Interfaces;

/// <summary>
/// Interface for smart caching system for SystemInfo with TTL and background cleanup
/// Phase 3: Enables proper mocking and testability
/// </summary>
public interface ISystemInfoCache : IDisposable
{
    /// <summary>
    /// Get SystemInfo with smart caching (Phase 3 main method)
    /// </summary>
    /// <param name="portName">Port name to get system info for</param>
    /// <param name="forceRefresh">Force refresh ignoring cache</param>
    /// <returns>System information or null if not available</returns>
    Task<SystemInfo?> GetSystemInfoAsync(string portName, bool forceRefresh = false);

    /// <summary>
    /// Invalidate cache entry for a specific port
    /// </summary>
    /// <param name="portName">Port name to invalidate</param>
    void InvalidateCache(string portName);

    /// <summary>
    /// Clear all expired entries from cache
    /// </summary>
    void ClearExpiredEntries();

    /// <summary>
    /// Get cache statistics for monitoring
    /// </summary>
    /// <returns>Cache statistics</returns>
    CacheStatistics GetStatistics(); // ← FIXED: Using CacheStatistics model

    /// <summary>
    /// Clear all cache entries
    /// </summary>
    void ClearAll();
}