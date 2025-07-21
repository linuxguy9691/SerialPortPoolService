namespace SerialPortPool.Core.Models;

/// <summary>
/// Statistics about the port pool
/// </summary>
public class PoolStatistics
{
    /// <summary>
    /// Total number of ports in the pool
    /// </summary>
    public int TotalPorts { get; set; }

    /// <summary>
    /// Number of ports currently available for allocation
    /// </summary>
    public int AvailablePorts { get; set; }

    /// <summary>
    /// Number of ports currently allocated
    /// </summary>
    public int AllocatedPorts { get; set; }

    /// <summary>
    /// Number of ports in error state
    /// </summary>
    public int ErrorPorts { get; set; }

    /// <summary>
    /// Number of unique clients with active allocations
    /// </summary>
    public int ActiveClients { get; set; }

    /// <summary>
    /// Total number of allocations made (historical)
    /// </summary>
    public long TotalAllocations { get; set; }

    /// <summary>
    /// Average allocation duration in minutes
    /// </summary>
    public double AverageAllocationDurationMinutes { get; set; }

    /// <summary>
    /// When these statistics were generated
    /// </summary>
    public DateTime GeneratedAt { get; set; } = DateTime.Now;

    /// <summary>
    /// Pool utilization percentage (allocated / total * 100)
    /// </summary>
    public double UtilizationPercentage => TotalPorts > 0 ? (AllocatedPorts * 100.0 / TotalPorts) : 0;

    public override string ToString()
    {
        return $"Pool Stats: {AllocatedPorts}/{TotalPorts} allocated ({UtilizationPercentage:F1}%), {ActiveClients} clients, {ErrorPorts} errors";
    }
}