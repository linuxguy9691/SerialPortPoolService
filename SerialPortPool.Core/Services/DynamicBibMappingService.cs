// ===================================================================
// SPRINT 8: Dynamic BIB Mapping Service Implementation
// File: SerialPortPool.Core/Services/DynamicBibMappingService.cs  
// Purpose: Ultra-simple EEPROM → BIB_ID mapping with robust fallback
// Philosophy: "Minimum Change" - ProductDescription = BIB_ID directly
// ===================================================================

using Microsoft.Extensions.Logging;
using SerialPortPool.Core.Interfaces;

namespace SerialPortPool.Core.Services;

/// <summary>
/// SPRINT 8: Dynamic BIB mapping via FTDI EEPROM ProductDescription
/// SIMPLE STRATEGY: ProductDescription content = BIB_ID (no parsing required)
/// ROBUST DESIGN: Always provides valid BIB_ID (fallback strategy)
/// </summary>
public class DynamicBibMappingService : IDynamicBibMappingService
{
    private readonly IFtdiEepromReader _eepromReader;
    private readonly ILogger<DynamicBibMappingService> _logger;
    private readonly DynamicBibMappingStatistics _statistics = new();
    
    // Configuration
    private readonly string _defaultBibId = "client_demo";
    private readonly TimeSpan _eepromReadTimeout = TimeSpan.FromSeconds(5);

    public DynamicBibMappingService(
        IFtdiEepromReader eepromReader,
        ILogger<DynamicBibMappingService> logger)
    {
        _eepromReader = eepromReader ?? throw new ArgumentNullException(nameof(eepromReader));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Get BIB_ID from FTDI device EEPROM ProductDescription
    /// SIMPLE APPROACH: ProductDescription = BIB_ID directly (no parsing)
    /// </summary>
    public async Task<string?> GetBibIdFromEepromAsync(string portName, string serialNumber)
    {
        if (string.IsNullOrWhiteSpace(serialNumber))
        {
            _logger.LogWarning("🔍 Invalid serial number for dynamic BIB mapping: {PortName}", portName);
            UpdateStatistics(success: false, usedFallback: false, hadError: true);
            return null;
        }

        try
        {
            _logger.LogDebug("🔬 Reading BIB_ID from EEPROM: {SerialNumber} (Port: {PortName})", 
                serialNumber, portName);

            // Read EEPROM via existing SAFE MODE reader
            var eepromData = await _eepromReader.ReadEepromAsync(serialNumber);

            if (!eepromData.IsValid)
            {
                _logger.LogWarning("⚠️ Invalid EEPROM data for {SerialNumber}: {Error}", 
                    serialNumber, eepromData.ErrorMessage);
                UpdateStatistics(success: false, usedFallback: false, hadError: true);
                return null;
            }

            if (string.IsNullOrWhiteSpace(eepromData.ProductDescription))
            {
                _logger.LogWarning("⚠️ Empty ProductDescription in EEPROM for {SerialNumber}", serialNumber);
                UpdateStatistics(success: false, usedFallback: false, hadError: false);
                return null;
            }

            // ✨ SPRINT 8 CORE: ProductDescription = BIB_ID directly (no parsing)
            var bibId = eepromData.ProductDescription.Trim();

            // Validate BIB_ID format (basic sanity check)
            if (IsValidBibId(bibId))
            {
                _logger.LogInformation("✅ Dynamic BIB detected: {SerialNumber} → '{BibId}' (Port: {PortName})", 
                    serialNumber, bibId, portName);
                UpdateStatistics(success: true, usedFallback: false, hadError: false);
                return bibId;
            }
            else
            {
                _logger.LogWarning("⚠️ Invalid BIB_ID format in ProductDescription: '{ProductDescription}' (Serial: {SerialNumber})", 
                    eepromData.ProductDescription, serialNumber);
                UpdateStatistics(success: false, usedFallback: false, hadError: false);
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "💥 Error reading BIB_ID from EEPROM: {SerialNumber} (Port: {PortName})", 
                serialNumber, portName);
            UpdateStatistics(success: false, usedFallback: false, hadError: true);
            return null;
        }
    }

    /// <summary>
    /// Get BIB_ID with robust fallback strategy - NEVER FAILS
    /// STEP 1: Try EEPROM dynamic reading
    /// STEP 2: Fallback to safe default
    /// GUARANTEE: Always returns valid BIB_ID
    /// </summary>
    public async Task<string> GetBibIdWithFallbackAsync(string portName, string serialNumber)
    {
        try
        {
            _logger.LogDebug("🎯 Dynamic BIB mapping with fallback: {PortName} (Serial: {SerialNumber})", 
                portName, serialNumber);

            // STEP 1: Try EEPROM dynamic mapping
            var dynamicBibId = await GetBibIdFromEepromAsync(portName, serialNumber);
            if (!string.IsNullOrEmpty(dynamicBibId))
            {
                _logger.LogInformation("🚀 Dynamic BIB mapping SUCCESS: {PortName} → '{BibId}' (via EEPROM)", 
                    portName, dynamicBibId);
                return dynamicBibId;
            }

            // STEP 2: Fallback to safe default
            _logger.LogDebug("🛡️ Using fallback BIB_ID for {PortName}: '{DefaultBibId}'", 
                portName, _defaultBibId);
            UpdateStatistics(success: false, usedFallback: true, hadError: false);
            
            return _defaultBibId;
        }
        catch (Exception ex)
        {
            // ULTIMATE FALLBACK: Even if something goes wrong, never fail
            _logger.LogError(ex, "💥 Error in dynamic BIB mapping with fallback: {PortName} - using default", portName);
            UpdateStatistics(success: false, usedFallback: true, hadError: true);
            return _defaultBibId;
        }
    }

    /// <summary>
    /// Check if dynamic BIB mapping is available for a device
    /// </summary>
    public async Task<bool> IsDynamicMappingAvailableAsync(string serialNumber)
    {
        if (string.IsNullOrWhiteSpace(serialNumber))
            return false;

        try
        {
            _logger.LogDebug("🔍 Checking dynamic mapping availability: {SerialNumber}", serialNumber);

            // Check if device is accessible
            var accessible = await _eepromReader.IsDeviceAccessibleAsync(serialNumber);
            if (!accessible)
            {
                _logger.LogDebug("❌ Device not accessible for dynamic mapping: {SerialNumber}", serialNumber);
                return false;
            }

            // Try to read EEPROM ProductDescription
            var eepromData = await _eepromReader.ReadEepromAsync(serialNumber);
            var available = eepromData.IsValid && !string.IsNullOrWhiteSpace(eepromData.ProductDescription);

            _logger.LogDebug("📊 Dynamic mapping availability: {SerialNumber} → {Available}", 
                serialNumber, available ? "✅ Available" : "❌ Not available");

            return available;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "⚠️ Error checking dynamic mapping availability: {SerialNumber}", serialNumber);
            return false;
        }
    }

    /// <summary>
    /// Get mapping statistics for monitoring
    /// </summary>
    public DynamicBibMappingStatistics GetMappingStatistics()
    {
        _statistics.LastUpdated = DateTime.Now;
        return _statistics;
    }

    #region Private Helper Methods

    /// <summary>
    /// Validate BIB_ID format (basic sanity checks)
    /// Ensures the ProductDescription looks like a valid BIB identifier
    /// </summary>
    private bool IsValidBibId(string bibId)
    {
        if (string.IsNullOrWhiteSpace(bibId))
            return false;

        // Basic format validation
        // BIB_ID should be alphanumeric with underscores, reasonable length
        if (bibId.Length < 3 || bibId.Length > 50)
        {
            _logger.LogDebug("⚠️ BIB_ID length validation failed: '{BibId}' (Length: {Length})", 
                bibId, bibId.Length);
            return false;
        }

        // Check for reasonable characters (alphanumeric, underscore, dash)
        if (!System.Text.RegularExpressions.Regex.IsMatch(bibId, @"^[a-zA-Z0-9_\-\.]+$"))
        {
            _logger.LogDebug("⚠️ BIB_ID character validation failed: '{BibId}'", bibId);
            return false;
        }

        return true;
    }

    /// <summary>
    /// Update internal statistics for monitoring
    /// </summary>
    private void UpdateStatistics(bool success, bool usedFallback, bool hadError)
    {
        _statistics.TotalRequests++;
        
        if (success)
            _statistics.SuccessfulEepromReads++;
        
        if (usedFallback)
            _statistics.FallbacksUsed++;
        
        if (hadError)
            _statistics.EepromReadErrors++;

        // Log statistics periodically
        if (_statistics.TotalRequests % 10 == 0)
        {
            _logger.LogDebug("📊 Dynamic BIB mapping stats: {Summary}", _statistics.GetSummary());
        }
    }

    #endregion
}