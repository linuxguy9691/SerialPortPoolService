// SerialPortPool.Core/Interfaces/IPortReservationService.cs - NEW POC
// ZERO TOUCH - Clean interface for extension layer
using SerialPortPool.Core.Models;

namespace SerialPortPool.Core.Interfaces;

/// <summary>
/// POC: Port reservation service interface (ZERO TOUCH extension)
/// Provides reservation layer on top of existing pool management
/// </summary>
public interface IPortReservationService
{
    /// <summary>
    /// Reserve a port from the pool with enhanced reservation management
    /// </summary>
    /// <param name="criteria">Reservation criteria (wraps existing validation config)</param>
    /// <param name="clientId">Client identifier</param>
    /// <param name="reservationDuration">How long to reserve (default 30 min)</param>
    /// <returns>Port reservation if successful, null if no ports available</returns>
    Task<PortReservation?> ReservePortAsync(
        PortReservationCriteria criteria,
        string clientId,
        TimeSpan? reservationDuration = null);
    
    /// <summary>
    /// Release a port reservation back to the pool
    /// </summary>
    /// <param name="reservationId">Reservation ID to release</param>
    /// <param name="clientId">Client ID for verification</param>
    /// <returns>True if reservation was successfully released</returns>
    Task<bool> ReleaseReservationAsync(string reservationId, string clientId);
    
    /// <summary>
    /// Get all active (non-expired) reservations
    /// </summary>
    /// <returns>Collection of active reservations</returns>
    Task<IEnumerable<PortReservation>> GetActiveReservationsAsync();
    
    /// <summary>
    /// Get all reservations (active and expired) for a specific client
    /// </summary>
    /// <param name="clientId">Client identifier</param>
    /// <returns>Collection of client reservations</returns>
    Task<IEnumerable<PortReservation>> GetReservationsForClientAsync(string clientId);
    
    /// <summary>
    /// Get specific reservation by ID
    /// </summary>
    /// <param name="reservationId">Reservation identifier</param>
    /// <returns>Reservation if found, null otherwise</returns>
    Task<PortReservation?> GetReservationAsync(string reservationId);
    
    /// <summary>
    /// Extend an existing reservation
    /// </summary>
    /// <param name="reservationId">Reservation to extend</param>
    /// <param name="additionalTime">Additional time to add</param>
    /// <param name="clientId">Client ID for verification</param>
    /// <returns>True if successfully extended</returns>
    Task<bool> ExtendReservationAsync(string reservationId, TimeSpan additionalTime, string clientId);
    
    /// <summary>
    /// Release all reservations for a specific client (cleanup)
    /// </summary>
    /// <param name="clientId">Client identifier</param>
    /// <returns>Number of reservations released</returns>
    Task<int> ReleaseAllReservationsForClientAsync(string clientId);
    
    /// <summary>
    /// Clean up expired reservations (maintenance operation)
    /// </summary>
    /// <returns>Number of expired reservations cleaned up</returns>
    Task<int> CleanupExpiredReservationsAsync();
    
    /// <summary>
    /// Get reservation statistics for monitoring
    /// </summary>
    /// <returns>Reservation statistics</returns>
    Task<ReservationStatistics> GetReservationStatisticsAsync();
}

/// <summary>
/// POC: Reservation criteria (ZERO TOUCH - uses existing validation config)
/// </summary>
public class PortReservationCriteria
{
    /// <summary>
    /// COMPOSITION: Uses existing validation configuration (NO MODIFICATION)
    /// </summary>
    public PortValidationConfiguration? ValidationConfig { get; set; }
    
    /// <summary>
    /// Default reservation duration for this criteria
    /// </summary>
    public TimeSpan DefaultReservationDuration { get; set; } = TimeSpan.FromMinutes(30);
    
    /// <summary>
    /// Prefer multi-port devices (FT4232H) for reservation
    /// </summary>
    public bool PreferMultiPortDevice { get; set; } = false;
    
    /// <summary>
    /// Specific device ID preference (optional)
    /// </summary>
    public string? PreferredDeviceId { get; set; }
    
    /// <summary>
    /// Port name preference (optional)
    /// </summary>
    public string? PreferredPortName { get; set; }
    
    /// <summary>
    /// Client priority level (for future enhancement)
    /// </summary>
    public int Priority { get; set; } = 0;
    
    /// <summary>
    /// Create criteria for client validation (strict requirements)
    /// </summary>
    /// <returns>Criteria with client validation config</returns>
    public static PortReservationCriteria CreateClientCriteria()
    {
        return new PortReservationCriteria
        {
            ValidationConfig = PortValidationConfiguration.CreateClientDefault(),
            PreferMultiPortDevice = true, // Prefer FT4232H for clients
            DefaultReservationDuration = TimeSpan.FromMinutes(60) // Longer for production
        };
    }
    
    /// <summary>
    /// Create criteria for development/testing (permissive)
    /// </summary>
    /// <returns>Criteria with development validation config</returns>
    public static PortReservationCriteria CreateDevelopmentCriteria()
    {
        return new PortReservationCriteria
        {
            ValidationConfig = PortValidationConfiguration.CreateDevelopmentDefault(),
            PreferMultiPortDevice = false, // Accept any FTDI device
            DefaultReservationDuration = TimeSpan.FromMinutes(15) // Shorter for dev
        };
    }
}

/// <summary>
/// POC: Reservation statistics for monitoring
/// </summary>
public class ReservationStatistics
{
    public int TotalReservations { get; set; }
    public int ActiveReservations { get; set; }
    public int ExpiredReservations { get; set; }
    public int TotalClients { get; set; }
    public TimeSpan AverageReservationDuration { get; set; }
    public DateTime GeneratedAt { get; set; } = DateTime.Now;
    
    public override string ToString()
    {
        return $"Reservations: {ActiveReservations}/{TotalReservations} active, {ExpiredReservations} expired, {TotalClients} clients";
    }
}