// ===================================================================
// SPRINT 8: Simple Wrapper Service - ZERO TOUCH Approach
// File: SerialPortPool.Core/Services/Sprint8DynamicBibService.cs
// Purpose: Wrapper service that USES existing services without modifying them
// Philosophy: "Zero Touch" - Uses existing architecture via composition
// ===================================================================

using Microsoft.Extensions.Logging;
using SerialPortPool.Core.Interfaces;
using SerialPortPool.Core.Models;

namespace SerialPortPool.Core.Services;

/// <summary>
/// SPRINT 8: Dynamic BIB Service Wrapper
/// ZERO TOUCH: Uses existing services via composition without modifying them
/// Provides Sprint 8 dynamic BIB functionality as an additive layer
/// </summary>
public class Sprint8DynamicBibService : ISprint8DynamicBibService
{
    private readonly ISerialPortDiscovery _existingDiscovery;  // ← Uses your existing service
    private readonly IDynamicBibMappingService _dynamicBibMapping;
    private readonly ILogger<Sprint8DynamicBibService> _logger;

    public Sprint8DynamicBibService(
        ISerialPortDiscovery existingDiscovery,           // ← Your existing EnhancedSerialPortDiscoveryService
        IDynamicBibMappingService dynamicBibMapping,     // ← New Sprint 8 service
        ILogger<Sprint8DynamicBibService> logger)
    {
        _existingDiscovery = existingDiscovery ?? throw new ArgumentNullException(nameof(existingDiscovery));
        _dynamicBibMapping = dynamicBibMapping ?? throw new ArgumentNullException(nameof(dynamicBibMapping));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// ✨ SPRINT 8: Get dynamic BIB_ID for a port using EEPROM
    /// ZERO TOUCH: Uses existing GetPortInfoAsync without modification
    /// </summary>
    public async Task<string> GetDynamicBibIdAsync(string portName)
    {
        try
        {
            _logger.LogDebug("🔬 Getting dynamic BIB_ID for port: {PortName}", portName);

            // Step 1: Use existing service to get port info (ZERO TOUCH)
            var portInfo = await _existingDiscovery.GetPortInfoAsync(portName);
            if (portInfo?.FtdiInfo?.SerialNumber == null)
            {
                _logger.LogWarning("⚠️ No FTDI info for {PortName}, using default BIB", portName);
                return "client_demo";
            }

            // Step 2: Use new dynamic mapping service
            var bibId = await _dynamicBibMapping.GetBibIdWithFallbackAsync(portName, portInfo.FtdiInfo.SerialNumber);
            
            _logger.LogInformation("🎯 Dynamic BIB_ID resolved: {PortName} → '{BibId}'", portName, bibId);
            return bibId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "💥 Error getting dynamic BIB_ID for {PortName}, using default", portName);
            return "client_demo"; // Ultimate fallback
        }
    }

    /// <summary>
    /// ✨ SPRINT 8: Enhanced port discovery with BIB mapping
    /// ZERO TOUCH: Wraps existing DiscoverPortsAsync with BIB enhancement
    /// </summary>
    public async Task<IEnumerable<Sprint8PortInfo>> DiscoverPortsWithBibMappingAsync()
    {
        try
        {
            _logger.LogInformation("🔍 Enhanced port discovery with dynamic BIB mapping...");

            // Step 1: Use existing discovery (ZERO TOUCH)
            var ports = await _existingDiscovery.DiscoverPortsAsync();
            var enhancedPorts = new List<Sprint8PortInfo>();

            foreach (var port in ports)
            {
                var enhancedPort = new Sprint8PortInfo
                {
                    // Copy all existing properties (ZERO TOUCH)
                    OriginalPortInfo = port,
                    
                    // Add Sprint 8 enhancements
                    DynamicBibId = "client_demo", // Default
                    HasDynamicMapping = false
                };

                // Try to get dynamic BIB mapping for FTDI devices
                if (port.IsFtdiDevice && port.FtdiInfo?.SerialNumber != null)
                {
                    try
                    {
                        enhancedPort.DynamicBibId = await GetDynamicBibIdAsync(port.PortName);
                        enhancedPort.HasDynamicMapping = await _dynamicBibMapping.IsDynamicMappingAvailableAsync(port.FtdiInfo.SerialNumber);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "⚠️ Dynamic mapping failed for {PortName}", port.PortName);
                    }
                }

                enhancedPorts.Add(enhancedPort);
            }

            // Summary statistics
            var dynamicCount = enhancedPorts.Count(p => p.HasDynamicMapping);
            var ftdiCount = enhancedPorts.Count(p => p.OriginalPortInfo.IsFtdiDevice);
            
            _logger.LogInformation("✅ Enhanced discovery complete: {Total} ports, {Ftdi} FTDI, {Dynamic} with dynamic mapping", 
                enhancedPorts.Count, ftdiCount, dynamicCount);

            return enhancedPorts;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "💥 Error in enhanced port discovery with BIB mapping");
            
            // Fallback to basic discovery
            var basicPorts = await _existingDiscovery.DiscoverPortsAsync();
            return basicPorts.Select(p => new Sprint8PortInfo
            {
                OriginalPortInfo = p,
                DynamicBibId = "client_demo",
                HasDynamicMapping = false
            });
        }
    }

    /// <summary>
    /// ✨ SPRINT 8: Check if dynamic BIB mapping is available for a port
    /// ZERO TOUCH: Uses existing services to determine mapping availability
    /// </summary>
    public async Task<bool> IsDynamicMappingAvailableAsync(string portName)
    {
        try
        {
            var portInfo = await _existingDiscovery.GetPortInfoAsync(portName);
            if (portInfo?.FtdiInfo?.SerialNumber == null)
                return false;

            return await _dynamicBibMapping.IsDynamicMappingAvailableAsync(portInfo.FtdiInfo.SerialNumber);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error checking dynamic mapping availability for {PortName}", portName);
            return false;
        }
    }

    /// <summary>
    /// ✨ SPRINT 8: Get dynamic BIB mapping statistics
    /// </summary>
    public DynamicBibMappingStatistics GetDynamicBibMappingStatistics()
    {
        try
        {
            var stats = _dynamicBibMapping.GetMappingStatistics();
            _logger.LogDebug("📊 Dynamic BIB mapping statistics: {Summary}", stats.GetSummary());
            return stats;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "💥 Error getting dynamic BIB mapping statistics");
            return new DynamicBibMappingStatistics();
        }
    }
}

/// <summary>
/// SPRINT 8: Enhanced port information with BIB mapping
/// ZERO TOUCH: Wraps existing SerialPortInfo without modifying it
/// </summary>
public class Sprint8PortInfo
{
    /// <summary>
    /// Original port info from existing discovery service (ZERO TOUCH)
    /// </summary>
    public SerialPortInfo OriginalPortInfo { get; set; } = new();

    /// <summary>
    /// Dynamic BIB_ID determined from EEPROM ProductDescription
    /// </summary>
    public string DynamicBibId { get; set; } = "client_demo";

    /// <summary>
    /// Indicates if dynamic BIB mapping is available for this port
    /// </summary>
    public bool HasDynamicMapping { get; set; }

    // ✅ CONVENIENCE PROPERTIES: Delegate to original port info
    public string PortName => OriginalPortInfo.PortName;
    public string FriendlyName => OriginalPortInfo.FriendlyName;
    public bool IsFtdiDevice => OriginalPortInfo.IsFtdiDevice;
    public string FtdiChipType => OriginalPortInfo.FtdiChipType;
    public bool IsValidForPool => OriginalPortInfo.IsValidForPool;
    public PortStatus Status => OriginalPortInfo.Status;

    /// <summary>
    /// Indicates if this port is using EEPROM-based BIB detection
    /// </summary>
    public bool IsUsingDynamicMapping => HasDynamicMapping && IsFtdiDevice;

    /// <summary>
    /// Summary of BIB mapping method for display
    /// </summary>
    public string BibMappingMethod => IsUsingDynamicMapping ? "EEPROM Dynamic" : "Static Default";

    /// <summary>
    /// Enhanced summary including BIB information
    /// </summary>
    public string GetEnhancedSummary()
    {
        var baseInfo = $"{PortName} - {FriendlyName}";
        var bibInfo = $"BIB: {DynamicBibId} ({BibMappingMethod})";
        var ftdiInfo = IsFtdiDevice ? $"FTDI: {FtdiChipType}" : "Non-FTDI";
        
        return $"{baseInfo} | {bibInfo} | {ftdiInfo}";
    }

    public override string ToString()
    {
        return GetEnhancedSummary();
    }
}

/// <summary>
/// Interface for Sprint 8 Dynamic BIB Service
/// </summary>
public interface ISprint8DynamicBibService
{
    Task<string> GetDynamicBibIdAsync(string portName);
    Task<IEnumerable<Sprint8PortInfo>> DiscoverPortsWithBibMappingAsync();
    Task<bool> IsDynamicMappingAvailableAsync(string portName);
    DynamicBibMappingStatistics GetDynamicBibMappingStatistics();
}