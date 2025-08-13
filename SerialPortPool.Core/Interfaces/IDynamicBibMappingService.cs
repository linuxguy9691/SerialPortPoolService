// ===================================================================
// SPRINT 8: Dynamic BIB Mapping Interface
// File: SerialPortPool.Core/Interfaces/IDynamicBibMappingService.cs
// Purpose: Dynamic BIB_ID selection via EEPROM ProductDescription
// Philosophy: "Minimum Change" - ProductDescription = BIB_ID directly
// ===================================================================

namespace SerialPortPool.Core.Interfaces;

/// <summary>
/// Service for dynamic BIB mapping via FTDI EEPROM ProductDescription
/// SPRINT 8 FEATURE: Auto-detect BIB configuration from hardware
/// PHILOSOPHY: ProductDescription in EEPROM = exact BIB_ID (no parsing)
/// </summary>
public interface IDynamicBibMappingService
{
    /// <summary>
    /// Get BIB_ID from FTDI device EEPROM ProductDescription
    /// SIMPLE STRATEGY: ProductDescription content = BIB_ID directly
    /// </summary>
    /// <param name="portName">Serial port name (e.g., "COM11")</param>
    /// <param name="serialNumber">FTDI device serial number</param>
    /// <returns>BIB_ID from EEPROM or null if not available</returns>
    Task<string?> GetBibIdFromEepromAsync(string portName, string serialNumber);

    /// <summary>
    /// Get BIB_ID with robust fallback strategy
    /// STEP 1: Try EEPROM dynamic reading
    /// STEP 2: Fallback to safe default if EEPROM unavailable
    /// GUARANTEE: Always returns a valid BIB_ID (never null)
    /// </summary>
    /// <param name="portName">Serial port name</param>
    /// <param name="serialNumber">FTDI device serial number</param>
    /// <returns>BIB_ID (either from EEPROM or fallback default)</returns>
    Task<string> GetBibIdWithFallbackAsync(string portName, string serialNumber);

    /// <summary>
    /// Check if dynamic BIB mapping is available for a device
    /// Useful for diagnostics and conditional logic
    /// </summary>
    /// <param name="serialNumber">FTDI device serial number</param>
    /// <returns>True if EEPROM contains valid ProductDescription for BIB mapping</returns>
    Task<bool> IsDynamicMappingAvailableAsync(string serialNumber);

    /// <summary>
    /// Get mapping statistics for monitoring and diagnostics
    /// Tracks success/failure rates for dynamic mapping
    /// </summary>
    /// <returns>Statistics about dynamic BIB mapping operations</returns>
    DynamicBibMappingStatistics GetMappingStatistics();
}

/// <summary>
/// Statistics for dynamic BIB mapping operations
/// Useful for monitoring system health and performance
/// </summary>
public class DynamicBibMappingStatistics
{
    public int TotalRequests { get; set; }
    public int SuccessfulEepromReads { get; set; }
    public int FallbacksUsed { get; set; }
    public int EepromReadErrors { get; set; }
    public DateTime LastUpdated { get; set; } = DateTime.Now;
    
    public double SuccessRate => TotalRequests > 0 ? (SuccessfulEepromReads * 100.0) / TotalRequests : 0.0;
    public double FallbackRate => TotalRequests > 0 ? (FallbacksUsed * 100.0) / TotalRequests : 0.0;
    
    public string GetSummary() => 
        $"Requests: {TotalRequests}, Success: {SuccessfulEepromReads} ({SuccessRate:F1}%), " +
        $"Fallbacks: {FallbacksUsed} ({FallbackRate:F1}%), Errors: {EepromReadErrors}";
}