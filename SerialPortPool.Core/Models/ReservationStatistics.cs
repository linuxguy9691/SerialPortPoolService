// SerialPortPool.Core/Models/ReservationStatistics.cs - CORRECT NAMESPACE
namespace SerialPortPool.Core.Models;

/// <summary>
/// POC: Statistics about the reservation service
/// </summary>
public class ReservationStatistics
{
    /// <summary>
    /// Total number of reservations managed
    /// </summary>
    public int TotalReservations { get; set; }
    
    /// <summary>
    /// Number of currently active reservations
    /// </summary>
    public int ActiveReservations { get; set; }
    
    /// <summary>
    /// Number of expired reservations
    /// </summary>
    public int ExpiredReservations { get; set; }
    
    /// <summary>
    /// Number of unique clients with reservations
    /// </summary>
    public int TotalClients { get; set; }
    
    /// <summary>
    /// Average reservation duration
    /// </summary>
    public TimeSpan AverageReservationDuration { get; set; }
    
    /// <summary>
    /// Reservation utilization percentage
    /// </summary>
    public double UtilizationPercentage => TotalReservations > 0 ? 
        (double)ActiveReservations / TotalReservations * 100.0 : 0.0;
    
    /// <summary>
    /// When these statistics were generated
    /// </summary>
    public DateTime GeneratedAt { get; set; } = DateTime.Now;
    
    public override string ToString()
    {
        return $"Reservations: {ActiveReservations}/{TotalReservations} active " +
               $"({UtilizationPercentage:F1}%), " +
               $"{TotalClients} clients, " +
               $"avg duration: {AverageReservationDuration.TotalMinutes:F1}min";
    }
}