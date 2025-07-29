// SerialPortPool.Core/Models/PortReservationCriteria.cs - CORRECT NAMESPACE
using SerialPortPool.Core.Models;

namespace SerialPortPool.Core.Models;

/// <summary>
/// POC: Criteria for port reservation requests
/// Uses existing PortValidationConfiguration via composition (ZERO TOUCH)
/// </summary>
public class PortReservationCriteria
{
    /// <summary>
    /// COMPOSITION: Uses existing validation configuration (ZERO TOUCH)
    /// </summary>
    public PortValidationConfiguration? ValidationConfig { get; set; }
    
    /// <summary>
    /// Default reservation duration if not specified
    /// </summary>
    public TimeSpan DefaultReservationDuration { get; set; } = TimeSpan.FromMinutes(30);
    
    /// <summary>
    /// Preferred device ID (for specific device targeting)
    /// </summary>
    public string? PreferredDeviceId { get; set; }
    
    /// <summary>
    /// Whether to prefer multi-port devices (FT4232H) over single-port devices
    /// </summary>
    public bool PreferMultiPortDevice { get; set; } = false;
    
    /// <summary>
    /// Whether to prefer single-port devices over multi-port devices
    /// </summary>
    public bool PreferSinglePortDevice { get; set; } = false;
    
    /// <summary>
    /// Priority level for this reservation (higher = more priority)
    /// </summary>
    public int Priority { get; set; } = 0;
    
    /// <summary>
    /// Optional metadata for this reservation criteria
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; } = new();
    
    /// <summary>
    /// Create criteria for client use (strict FTDI validation)
    /// </summary>
    /// <param name="reservationDuration">Optional reservation duration</param>
    /// <returns>Client-optimized criteria</returns>
    public static PortReservationCriteria CreateForClient(TimeSpan? reservationDuration = null)
    {
        return new PortReservationCriteria
        {
            ValidationConfig = PortValidationConfiguration.CreateClientDefault(),
            DefaultReservationDuration = reservationDuration ?? TimeSpan.FromMinutes(60), // Longer for client
            PreferMultiPortDevice = true, // Prefer FT4232H for client
            Priority = 10
        };
    }
    
    /// <summary>
    /// Create criteria for development use (permissive validation)
    /// </summary>
    /// <param name="reservationDuration">Optional reservation duration</param>
    /// <returns>Development-optimized criteria</returns>
    public static PortReservationCriteria CreateForDevelopment(TimeSpan? reservationDuration = null)
    {
        return new PortReservationCriteria
        {
            ValidationConfig = PortValidationConfiguration.CreateDevelopmentDefault(),
            DefaultReservationDuration = reservationDuration ?? TimeSpan.FromMinutes(15), // Shorter for dev
            PreferMultiPortDevice = false, // Any device for development
            Priority = 1
        };
    }
    
    /// <summary>
    /// Create criteria with no validation (any available port)
    /// </summary>
    /// <param name="reservationDuration">Optional reservation duration</param>
    /// <returns>Permissive criteria</returns>
    public static PortReservationCriteria CreatePermissive(TimeSpan? reservationDuration = null)
    {
        return new PortReservationCriteria
        {
            ValidationConfig = null, // No validation
            DefaultReservationDuration = reservationDuration ?? TimeSpan.FromMinutes(10),
            PreferMultiPortDevice = false,
            Priority = 0
        };
    }
    
    public override string ToString()
    {
        var preferences = new List<string>();
        if (PreferMultiPortDevice) preferences.Add("MultiPort");
        if (PreferSinglePortDevice) preferences.Add("SinglePort");
        if (!string.IsNullOrEmpty(PreferredDeviceId)) preferences.Add($"Device:{PreferredDeviceId}");
        
        var validation = ValidationConfig != null ? "Validated" : "Any";
        var prefs = preferences.Any() ? $" ({string.Join(", ", preferences)})" : "";
        
        return $"{validation} reservation for {DefaultReservationDuration.TotalMinutes}min{prefs}";
    }
}