// SerialPortPool.Core/Models/CacheStatistics.cs - Shared model to avoid circular dependencies
namespace SerialPortPool.Core.Models;

/// <summary>
/// Cache statistics for monitoring smart caching system
/// </summary>
public class CacheStatistics
{
    public int TotalEntries { get; set; }
    public int ExpiredEntries { get; set; }
    public long TotalHits { get; set; }
    public long TotalMisses { get; set; }
    public double HitRatio => TotalHits + TotalMisses > 0 ? (double)TotalHits / (TotalHits + TotalMisses) * 100 : 0;
    public DateTime GeneratedAt { get; set; } = DateTime.Now;
    
    public override string ToString()
    {
        return $"Cache Stats: {TotalEntries} entries, {HitRatio:F1}% hit ratio, {ExpiredEntries} expired";
    }
}