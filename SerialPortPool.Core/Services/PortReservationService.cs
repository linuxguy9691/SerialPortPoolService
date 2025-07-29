// SerialPortPool.Core/Services/PortReservationService.cs - CORRECT REFERENCES
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using SerialPortPool.Core.Interfaces;
using SerialPortPool.Core.Models;

namespace SerialPortPool.Core.Services;

/// <summary>
/// POC: Port reservation service using ZERO TOUCH composition pattern
/// Wraps existing ISerialPortPool without any modifications
/// </summary>
public class PortReservationService : IPortReservationService, IDisposable
{
    private readonly ISerialPortPool _existingPool;  // ← COMPOSITION: Uses existing pool
    private readonly ILogger<PortReservationService> _logger;
    private readonly ConcurrentDictionary<string, PortReservation> _reservations = new();
    private readonly Timer _cleanupTimer;
    private volatile bool _disposed;

    public PortReservationService(
        ISerialPortPool existingPool,  // ← ZERO TOUCH: No modification required
        ILogger<PortReservationService> logger)
    {
        _existingPool = existingPool ?? throw new ArgumentNullException(nameof(existingPool));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        // Setup periodic cleanup of expired reservations (every 5 minutes)
        _cleanupTimer = new Timer(PerformPeriodicCleanup, null, 
            TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
        
        _logger.LogInformation("🔒 PortReservationService initialized with ZERO TOUCH composition pattern");
    }

    /// <summary>
    /// POC: Reserve port using existing pool (ZERO TOUCH approach)
    /// </summary>
    public async Task<PortReservation?> ReservePortAsync(
        PortReservationCriteria criteria,
        string clientId,
        TimeSpan? reservationDuration = null)
    {
        if (_disposed)
        {
            _logger.LogWarning("Cannot reserve port - service is disposed");
            return null;
        }

        try
        {
            _logger.LogDebug("🔒 POC: Attempting port reservation for client {ClientId}", clientId);
            
            // 1. Use existing pool allocation (NO MODIFICATION TO EXISTING CODE)
            var allocation = await _existingPool.AllocatePortAsync(
                criteria.ValidationConfig, clientId);
                
            if (allocation == null)
            {
                _logger.LogWarning("❌ POC: No ports available for reservation (client: {ClientId})", clientId);
                return null;
            }
            
            // 2. Wrap in reservation using composition pattern
            var duration = reservationDuration ?? criteria.DefaultReservationDuration;
            var reservation = PortReservation.CreateFromAllocation(allocation, duration);
            
            // 3. Store reservation metadata
            reservation.ReservationMetadata["ReservationCriteria"] = criteria.GetType().Name;
            reservation.ReservationMetadata["PreferMultiPort"] = criteria.PreferMultiPortDevice.ToString();
            if (!string.IsNullOrEmpty(criteria.PreferredDeviceId))
            {
                reservation.ReservationMetadata["PreferredDeviceId"] = criteria.PreferredDeviceId;
            }
            
            // 4. Add to reservation tracking
            _reservations[reservation.ReservationId] = reservation;
            
            _logger.LogInformation("✅ POC: Port reserved - {PortName} → {ClientId} (ID: {ReservationId}, Expires: {ExpiresAt:HH:mm:ss})", 
                reservation.PortName, clientId, reservation.ReservationId, reservation.ExpiresAt);
                
            return reservation;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "💥 POC: Port reservation failed for client {ClientId}", clientId);
            return null;
        }
    }

    /// <summary>
    /// POC: Release reservation using existing pool (ZERO TOUCH)
    /// </summary>
    public async Task<bool> ReleaseReservationAsync(string reservationId, string clientId)
    {
        if (_disposed)
        {
            _logger.LogWarning("Cannot release reservation - service is disposed");
            return false;
        }

        if (!_reservations.TryGetValue(reservationId, out var reservation))
        {
            _logger.LogWarning("❌ POC: Reservation not found - {ReservationId}", reservationId);
            return false;
        }
        
        // Verify client ID for security
        if (reservation.ClientId != clientId)
        {
            _logger.LogWarning("❌ POC: Client ID mismatch for reservation {ReservationId} (expected: {Expected}, got: {Actual})", 
                reservationId, reservation.ClientId, clientId);
            return false;
        }
            
        try
        {
            // Release using existing pool method (NO MODIFICATION)
            var released = await _existingPool.ReleasePortAsync(
                reservation.PortName, 
                reservation.SessionId);
                
            if (released)
            {
                _reservations.TryRemove(reservationId, out _);
                _logger.LogInformation("🔓 POC: Reservation released - {ReservationId} ({PortName})", 
                    reservationId, reservation.PortName);
            }
            
            return released;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "💥 POC: Release failed - {ReservationId}", reservationId);
            return false;
        }
    }

    /// <summary>
    /// Get all active reservations
    /// </summary>
    public async Task<IEnumerable<PortReservation>> GetActiveReservationsAsync()
    {
        await Task.CompletedTask;
        return _reservations.Values.Where(r => !r.IsExpired && r.IsActive).ToList();
    }

    /// <summary>
    /// Get reservations for specific client
    /// </summary>
    public async Task<IEnumerable<PortReservation>> GetReservationsForClientAsync(string clientId)
    {
        await Task.CompletedTask;
        return _reservations.Values.Where(r => r.ClientId == clientId).ToList();
    }

    /// <summary>
    /// Get specific reservation by ID
    /// </summary>
    public async Task<PortReservation?> GetReservationAsync(string reservationId)
    {
        await Task.CompletedTask;
        _reservations.TryGetValue(reservationId, out var reservation);
        return reservation;
    }

    /// <summary>
    /// Extend existing reservation
    /// </summary>
    public async Task<bool> ExtendReservationAsync(string reservationId, TimeSpan additionalTime, string clientId)
    {
        await Task.CompletedTask;
        
        if (!_reservations.TryGetValue(reservationId, out var reservation))
        {
            _logger.LogWarning("❌ Cannot extend - reservation not found: {ReservationId}", reservationId);
            return false;
        }
        
        if (reservation.ClientId != clientId)
        {
            _logger.LogWarning("❌ Cannot extend - client ID mismatch: {ReservationId}", reservationId);
            return false;
        }
        
        if (reservation.IsExpired)
        {
            _logger.LogWarning("❌ Cannot extend - reservation expired: {ReservationId}", reservationId);
            return false;
        }
        
        reservation.ExtendReservation(additionalTime);
        _logger.LogInformation("⏰ Reservation extended: {ReservationId} (+{Minutes}min, new expiry: {ExpiresAt:HH:mm:ss})", 
            reservationId, additionalTime.TotalMinutes, reservation.ExpiresAt);
            
        return true;
    }

    /// <summary>
    /// Release all reservations for a client (cleanup)
    /// </summary>
    public async Task<int> ReleaseAllReservationsForClientAsync(string clientId)
    {
        var clientReservations = _reservations.Values
            .Where(r => r.ClientId == clientId && !r.IsExpired && r.IsActive)
            .ToList();
            
        int releasedCount = 0;
        foreach (var reservation in clientReservations)
        {
            if (await ReleaseReservationAsync(reservation.ReservationId, clientId))
            {
                releasedCount++;
            }
        }
        
        _logger.LogInformation("🧹 Released {Count} reservations for client {ClientId}", releasedCount, clientId);
        return releasedCount;
    }

    /// <summary>
    /// Clean up expired reservations
    /// </summary>
    public async Task<int> CleanupExpiredReservationsAsync()
    {
        var expiredReservations = _reservations.Values
            .Where(r => r.IsExpired)
            .ToList();
            
        int cleanedCount = 0;
        foreach (var reservation in expiredReservations)
        {
            try
            {
                // Release from underlying pool if still active
                if (reservation.IsActive)
                {
                    await _existingPool.ReleasePortAsync(reservation.PortName, reservation.SessionId);
                }
                
                // Remove from reservation tracking
                _reservations.TryRemove(reservation.ReservationId, out _);
                cleanedCount++;
                
                _logger.LogDebug("🧹 Cleaned expired reservation: {ReservationId} ({PortName})", 
                    reservation.ReservationId, reservation.PortName);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "⚠️ Error cleaning expired reservation: {ReservationId}", 
                    reservation.ReservationId);
            }
        }
        
        if (cleanedCount > 0)
        {
            _logger.LogInformation("🧹 Cleanup completed: {Count} expired reservations removed", cleanedCount);
        }
        
        return cleanedCount;
    }

    /// <summary>
    /// Get reservation statistics
    /// </summary>
    public async Task<ReservationStatistics> GetReservationStatisticsAsync()
    {
        await Task.CompletedTask;
        
        var allReservations = _reservations.Values.ToList();
        var activeReservations = allReservations.Where(r => !r.IsExpired && r.IsActive).ToList();
        var expiredReservations = allReservations.Where(r => r.IsExpired).ToList();
        
        return new ReservationStatistics
        {
            TotalReservations = allReservations.Count,
            ActiveReservations = activeReservations.Count,
            ExpiredReservations = expiredReservations.Count,
            TotalClients = allReservations.Select(r => r.ClientId).Distinct().Count(),
            AverageReservationDuration = allReservations.Any() ? 
                TimeSpan.FromTicks((long)allReservations.Average(r => r.ReservationDuration.Ticks)) : 
                TimeSpan.Zero
        };
    }

    /// <summary>
    /// Periodic cleanup timer callback
    /// </summary>
    private async void PerformPeriodicCleanup(object? state)
    {
        try
        {
            await CleanupExpiredReservationsAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during periodic cleanup");
        }
    }

    /// <summary>
    /// Dispose resources
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            _cleanupTimer?.Dispose();
            
            // Final cleanup
            try
            {
                CleanupExpiredReservationsAsync().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error during final cleanup");
            }
            
            _logger.LogInformation("🔒 PortReservationService disposed");
        }
    }
}