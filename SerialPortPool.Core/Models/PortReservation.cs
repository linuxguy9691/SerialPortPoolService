// SerialPortPool.Core/Models/PortReservation.cs - CORRECT NAMESPACE
namespace SerialPortPool.Core.Models;

/// <summary>
/// POC: Port reservation wrapper using composition pattern (ZERO TOUCH)
/// Extends existing PortAllocation functionality without modification
/// </summary>
public class PortReservation
{
    /// <summary>
    /// Unique reservation identifier
    /// </summary>
    public string ReservationId { get; set; } = Guid.NewGuid().ToString();
    
    /// <summary>
    /// COMPOSITION: Uses existing PortAllocation (NO MODIFICATION)
    /// This is the core of ZERO TOUCH strategy
    /// </summary>
    public PortAllocation UnderlyingAllocation { get; set; } = null!;
    
    /// <summary>
    /// When this reservation expires (TTL management)
    /// </summary>
    public DateTime ExpiresAt { get; set; }
    
    /// <summary>
    /// When this reservation was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    
    /// <summary>
    /// Optional reservation metadata
    /// </summary>
    public Dictionary<string, string> ReservationMetadata { get; set; } = new();
    
    // ✅ DELEGATION: Properties delegate to existing allocation (ZERO TOUCH)
    /// <summary>
    /// Port name - delegates to underlying allocation
    /// </summary>
    public string PortName => UnderlyingAllocation?.PortName ?? "";
    
    /// <summary>
    /// Client ID - delegates to underlying allocation
    /// </summary>
    public string ClientId => UnderlyingAllocation?.AllocatedTo ?? "";
    
    /// <summary>
    /// Allocation timestamp - delegates to underlying allocation
    /// </summary>
    public DateTime AllocatedAt => UnderlyingAllocation?.AllocatedAt ?? DateTime.MinValue;
    
    /// <summary>
    /// Session ID - delegates to underlying allocation
    /// </summary>
    public string SessionId => UnderlyingAllocation?.SessionId ?? "";
    
    /// <summary>
    /// Whether this reservation has expired
    /// </summary>
    public bool IsExpired => DateTime.Now > ExpiresAt;
    
    /// <summary>
    /// Time remaining before expiration
    /// </summary>
    public TimeSpan TimeRemaining => IsExpired ? TimeSpan.Zero : ExpiresAt - DateTime.Now;
    
    /// <summary>
    /// Whether the underlying allocation is still active
    /// </summary>
    public bool IsActive => UnderlyingAllocation?.IsActive ?? false;
    
    /// <summary>
    /// Duration since reservation was created
    /// </summary>
    public TimeSpan ReservationDuration => DateTime.Now - CreatedAt;
    
    /// <summary>
    /// Create a new reservation wrapping an existing allocation
    /// </summary>
    /// <param name="allocation">Existing allocation to wrap</param>
    /// <param name="reservationDuration">How long to reserve (default 30 min)</param>
    /// <returns>New reservation</returns>
    public static PortReservation CreateFromAllocation(
        PortAllocation allocation, 
        TimeSpan? reservationDuration = null)
    {
        return new PortReservation
        {
            ReservationId = Guid.NewGuid().ToString(),
            UnderlyingAllocation = allocation,
            ExpiresAt = DateTime.Now.Add(reservationDuration ?? TimeSpan.FromMinutes(30)),
            CreatedAt = DateTime.Now
        };
    }
    
    /// <summary>
    /// Extend the reservation expiration time
    /// </summary>
    /// <param name="additionalTime">Additional time to add</param>
    public void ExtendReservation(TimeSpan additionalTime)
    {
        ExpiresAt = ExpiresAt.Add(additionalTime);
        ReservationMetadata["LastExtended"] = DateTime.Now.ToString("O");
    }
    
    /// <summary>
    /// Get formatted summary for logging/display
    /// </summary>
    /// <returns>Human-readable reservation summary</returns>
    public string GetSummary()
    {
        var status = IsExpired ? "⏰ EXPIRED" : IsActive ? "✅ ACTIVE" : "❌ INACTIVE";
        var timeInfo = IsExpired ? $"Expired {TimeRemaining.TotalMinutes:F0}min ago" : $"{TimeRemaining.TotalMinutes:F0}min remaining";
        
        return $"{status}: {PortName} → {ClientId} ({timeInfo})";
    }
    
    public override string ToString()
    {
        return GetSummary();
    }
    
    public override bool Equals(object? obj)
    {
        return obj is PortReservation other && ReservationId == other.ReservationId;
    }
    
    public override int GetHashCode()
    {
        return ReservationId.GetHashCode();
    }
}