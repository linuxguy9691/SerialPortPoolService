namespace SerialPortPool.Core.Models;

/// <summary>
/// Represents an allocation of a serial port within the pool management system
/// </summary>
public class PortAllocation
{
    /// <summary>
    /// The name of the allocated port (e.g., "COM3", "COM10")
    /// </summary>
    public string PortName { get; set; } = string.Empty;

    /// <summary>
    /// Current allocation status of the port
    /// </summary>
    public AllocationStatus Status { get; set; } = AllocationStatus.Available;

    /// <summary>
    /// Timestamp when the port was allocated
    /// </summary>
    public DateTime AllocatedAt { get; set; } = DateTime.Now;

    /// <summary>
    /// Timestamp when the port was released (null if currently allocated)
    /// </summary>
    public DateTime? ReleasedAt { get; set; }

    /// <summary>
    /// Identifier of the client or process that allocated this port
    /// </summary>
    public string? AllocatedTo { get; set; }

    /// <summary>
    /// Unique session identifier for tracking this allocation
    /// </summary>
    public string SessionId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Optional metadata or notes about this allocation
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; } = new();

    /// <summary>
    /// Calculated duration of the allocation (current time - allocated time, or released time - allocated time)
    /// </summary>
    public TimeSpan AllocationDuration => ReleasedAt?.Subtract(AllocatedAt) ?? DateTime.Now.Subtract(AllocatedAt);

    /// <summary>
    /// Whether this allocation is currently active (allocated and not released)
    /// </summary>
    public bool IsActive => Status == AllocationStatus.Allocated && !ReleasedAt.HasValue;

    /// <summary>
    /// Create a new allocation record for a port
    /// </summary>
    /// <param name="portName">Port name to allocate</param>
    /// <param name="allocatedTo">Client identifier</param>
    /// <returns>New allocation instance</returns>
    public static PortAllocation Create(string portName, string? allocatedTo = null)
    {
        return new PortAllocation
        {
            PortName = portName,
            Status = AllocationStatus.Allocated,
            AllocatedAt = DateTime.Now,
            AllocatedTo = allocatedTo,
            SessionId = Guid.NewGuid().ToString()
        };
    }

    /// <summary>
    /// Mark this allocation as released
    /// </summary>
    public void Release()
    {
        Status = AllocationStatus.Available;
        ReleasedAt = DateTime.Now;
    }

    /// <summary>
    /// Mark this allocation as having an error
    /// </summary>
    /// <param name="errorDetails">Optional error details to store in metadata</param>
    public void SetError(string? errorDetails = null)
    {
        Status = AllocationStatus.Error;
        if (!string.IsNullOrEmpty(errorDetails))
        {
            Metadata["ErrorDetails"] = errorDetails;
            Metadata["ErrorAt"] = DateTime.Now.ToString("O");
        }
    }

    public override string ToString()
    {
        var duration = AllocationDuration;
        var statusIcon = Status switch
        {
            AllocationStatus.Available => "🟢",
            AllocationStatus.Allocated => "🔴",
            AllocationStatus.Reserved => "🟡",
            AllocationStatus.Error => "❌",
            _ => "❓"
        };

        return $"{statusIcon} {PortName} → {AllocatedTo ?? "Unknown"} ({Status}, {duration.TotalMinutes:F1}min)";
    }

    public override bool Equals(object? obj)
    {
        return obj is PortAllocation other && 
               PortName == other.PortName && 
               SessionId == other.SessionId;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(PortName, SessionId);
    }
}