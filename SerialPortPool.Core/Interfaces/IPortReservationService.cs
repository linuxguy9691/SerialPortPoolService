// SerialPortPool.Core/Interfaces/IPortReservationService.cs - CORRECT LOCATION
using SerialPortPool.Core.Models;

namespace SerialPortPool.Core.Interfaces;

/// <summary>
/// POC Interface for port reservation service (ZERO TOUCH extension)
/// Provides reservation layer on top of existing ISerialPortPool
/// </summary>
public interface IPortReservationService : IDisposable
{
    /// <summary>
    /// Reserve a port from the pool with specific criteria
    /// </summary>
    /// <param name="criteria">Reservation criteria (preferences, validation config)</param>
    /// <param name="clientId">Client identifier</param>
    /// <param name="reservationDuration">Optional reservation duration (default from criteria)</param>
    /// <returns>Port reservation if successful, null if no ports available</returns>
    Task<PortReservation?> ReservePortAsync(
        PortReservationCriteria criteria,
        string clientId,
        TimeSpan? reservationDuration = null);

    /// <summary>
    /// Release a specific reservation
    /// </summary>
    /// <param name="reservationId">Unique reservation identifier</param>
    /// <param name="clientId">Client identifier (for security validation)</param>
    /// <returns>True if reservation was successfully released</returns>
    Task<bool> ReleaseReservationAsync(string reservationId, string clientId);

    /// <summary>
    /// Get all currently active reservations
    /// </summary>
    /// <returns>Collection of active reservations</returns>
    Task<IEnumerable<PortReservation>> GetActiveReservationsAsync();

    /// <summary>
    /// Get all reservations for a specific client
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
    /// <param name="reservationId">Reservation identifier</param>
    /// <param name="additionalTime">Additional time to extend</param>
    /// <param name="clientId">Client identifier (for security validation)</param>
    /// <returns>True if extension was successful</returns>
    Task<bool> ExtendReservationAsync(string reservationId, TimeSpan additionalTime, string clientId);

    /// <summary>
    /// Release all reservations for a client (cleanup operation)
    /// </summary>
    /// <param name="clientId">Client identifier</param>
    /// <returns>Number of reservations released</returns>
    Task<int> ReleaseAllReservationsForClientAsync(string clientId);

    /// <summary>
    /// Clean up expired reservations
    /// </summary>
    /// <returns>Number of reservations cleaned up</returns>
    Task<int> CleanupExpiredReservationsAsync();

    /// <summary>
    /// Get reservation service statistics
    /// </summary>
    /// <returns>Reservation statistics</returns>
    Task<ReservationStatistics> GetReservationStatisticsAsync();
}