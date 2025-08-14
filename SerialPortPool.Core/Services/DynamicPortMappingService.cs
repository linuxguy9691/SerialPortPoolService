// ===================================================================
// SPRINT 8 PHASE 3: Dynamic Port Mapping Service - CRITICAL COMPONENT
// File: SerialPortPool.Core/Services/DynamicPortMappingService.cs
// Purpose: Map EEPROM ProductDescription to UUT logical ports
// Philosophy: "Smart Hardware, Simple Software"
// ===================================================================

using Microsoft.Extensions.Logging;
using SerialPortPool.Core.Interfaces;
using SerialPortPool.Core.Models;

namespace SerialPortPool.Core.Services;

/// <summary>
/// SPRINT 8 PHASE 3: Dynamic Port Mapping Service
/// MISSION: Transform "client_demo A" (COM7) → UUT Port 1, BIB_ID "client_demo"
/// PHILOSOPHY: Hardware tells us what it is, we adapt accordingly
/// </summary>
public class DynamicPortMappingService : IDynamicPortMappingService
{
    private readonly ISerialPortDiscovery _discovery;
    private readonly IDynamicBibMappingService _bibMapping;
    private readonly ILogger<DynamicPortMappingService> _logger;
    
    // Cache for discovered mappings
    private readonly Dictionary<string, PortMapping> _portMappings = new();
    private DateTime _lastDiscovery = DateTime.MinValue;
    private readonly TimeSpan _cacheExpiry = TimeSpan.FromMinutes(5);

    public DynamicPortMappingService(
        ISerialPortDiscovery discovery,
        IDynamicBibMappingService bibMapping,
        ILogger<DynamicPortMappingService> logger)
    {
        _discovery = discovery ?? throw new ArgumentNullException(nameof(discovery));
        _bibMapping = bibMapping ?? throw new ArgumentNullException(nameof(bibMapping));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 🎯 SPRINT 8 CORE: Get dynamic physical port for UUT logical port
    /// Example: GetDynamicPortForUutPortAsync("client_demo", "production_uut", 1) → "COM7"
    /// </summary>
    public async Task<string?> GetDynamicPortForUutPortAsync(string bibId, string uutId, int uutPortNumber)
    {
        try
        {
            _logger.LogDebug("🎯 Dynamic port lookup: {BibId}.{UutId}.{PortNumber}", bibId, uutId, uutPortNumber);

            // Ensure fresh discovery
            await EnsureFreshDiscoveryAsync();

            // Find mapping for this BIB and UUT port
            var mapping = _portMappings.Values.FirstOrDefault(m => 
                m.BibId.Equals(bibId, StringComparison.OrdinalIgnoreCase) &&
                m.UutId.Equals(uutId, StringComparison.OrdinalIgnoreCase) &&
                m.UutPortNumber == uutPortNumber);

            if (mapping != null)
            {
                _logger.LogInformation("✅ Dynamic mapping found: {BibId}.{UutId}.{PortNumber} → {PhysicalPort}", 
                    bibId, uutId, uutPortNumber, mapping.PhysicalPort);
                return mapping.PhysicalPort;
            }

            _logger.LogWarning("❌ No dynamic mapping found for: {BibId}.{UutId}.{PortNumber}", bibId, uutId, uutPortNumber);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "💥 Error getting dynamic port for {BibId}.{UutId}.{PortNumber}", bibId, uutId, uutPortNumber);
            return null;
        }
    }

    /// <summary>
    /// 🔍 SPRINT 8: Discover and map all available ports dynamically
    /// </summary>
    public async Task<List<PortMapping>> DiscoverPortMappingsAsync()
    {
        try
        {
            _logger.LogInformation("🔍 Starting dynamic port discovery and mapping...");

            // Step 1: Discover all ports
            var discoveredPorts = await _discovery.DiscoverPortsAsync();
            var ftdiPorts = discoveredPorts.Where(p => p.IsFtdiDevice).ToList();

            _logger.LogInformation("📡 Discovered {Total} ports, {Ftdi} are FTDI devices", 
                discoveredPorts.Count(), ftdiPorts.Count);

            var mappings = new List<PortMapping>();

            // Step 2: Create mappings for each FTDI port
            foreach (var port in ftdiPorts)
            {
                var mapping = await CreatePortMappingAsync(port);
                if (mapping != null)
                {
                    mappings.Add(mapping);
                    _portMappings[port.PortName] = mapping;
                }
            }

            // Step 3: Log discovery results
            LogDiscoveryResults(mappings);

            _lastDiscovery = DateTime.Now;
            return mappings;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "💥 Error during dynamic port discovery");
            return new List<PortMapping>();
        }
    }

    /// <summary>
    /// 🔄 Get all discovered port mappings (with caching)
    /// </summary>
    public async Task<List<PortMapping>> GetAllPortMappingsAsync()
    {
        await EnsureFreshDiscoveryAsync();
        return _portMappings.Values.ToList();
    }

    /// <summary>
    /// 📊 Get mapping statistics for monitoring
    /// </summary>
    public DynamicPortMappingStatistics GetMappingStatistics()
    {
        var allMappings = _portMappings.Values.ToList();
        
        return new DynamicPortMappingStatistics
        {
            TotalMappings = allMappings.Count,
            UniqueBibIds = allMappings.Select(m => m.BibId).Distinct().Count(),
            UniqueUutIds = allMappings.Select(m => $"{m.BibId}.{m.UutId}").Distinct().Count(),
            PhysicalPorts = allMappings.Select(m => m.PhysicalPort).Distinct().Count(),
            LastDiscovery = _lastDiscovery,
            GeneratedAt = DateTime.Now
        };
    }

    #region Private Implementation

    /// <summary>
    /// Ensure we have fresh discovery data (with caching)
    /// </summary>
    private async Task EnsureFreshDiscoveryAsync()
    {
        if (DateTime.Now - _lastDiscovery > _cacheExpiry || !_portMappings.Any())
        {
            _logger.LogDebug("🔄 Cache expired or empty, performing fresh discovery");
            await DiscoverPortMappingsAsync();
        }
        else
        {
            _logger.LogDebug("📋 Using cached port mappings (age: {Age:F1}min)", 
                (DateTime.Now - _lastDiscovery).TotalMinutes);
        }
    }

    /// <summary>
    /// 🏗️ Create port mapping from discovered port
    /// </summary>
    private async Task<PortMapping?> CreatePortMappingAsync(SerialPortInfo portInfo)
    {
        try
        {
            if (portInfo.FtdiInfo?.SerialNumber == null)
            {
                _logger.LogDebug("⚠️ Port {PortName} has no FTDI serial number", portInfo.PortName);
                return null;
            }

            _logger.LogDebug("🔍 Creating mapping for {PortName} (Serial: {Serial})", 
                portInfo.PortName, portInfo.FtdiInfo.SerialNumber);

            // Get BIB_ID from EEPROM via existing Sprint 8 service
            var dynamicBibId = await _bibMapping.GetBibIdFromEepromAsync(portInfo.PortName, portInfo.FtdiInfo.SerialNumber);
            
            if (string.IsNullOrEmpty(dynamicBibId))
            {
                _logger.LogDebug("⚠️ No dynamic BIB_ID found for {PortName}, skipping", portInfo.PortName);
                return null;
            }

            // Parse BIB_ID and port suffix
            var (bibId, portSuffix) = ParseProductDescription(dynamicBibId);
            var uutPortNumber = ConvertSuffixToPortNumber(portSuffix);

            if (uutPortNumber == 0)
            {
                _logger.LogWarning("⚠️ Could not determine UUT port number for {PortName} (BIB: {BibId}, Suffix: {Suffix})", 
                    portInfo.PortName, bibId, portSuffix);
                return null;
            }

            var mapping = new PortMapping
            {
                PhysicalPort = portInfo.PortName,
                BibId = bibId,
                UutId = "production_uut", // Default UUT ID (could be configurable)
                UutPortNumber = uutPortNumber,
                PortSuffix = portSuffix,
                DeviceType = portInfo.FtdiChipType,
                SerialNumber = portInfo.FtdiInfo.SerialNumber,
                ProductDescription = dynamicBibId,
                DiscoveredAt = DateTime.Now
            };

            _logger.LogInformation("✅ Port mapping created: {PhysicalPort} → {BibId}.{UutId}.{PortNumber} (Suffix: {Suffix})", 
                mapping.PhysicalPort, mapping.BibId, mapping.UutId, mapping.UutPortNumber, mapping.PortSuffix);

            return mapping;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "💥 Error creating port mapping for {PortName}", portInfo.PortName);
            return null;
        }
    }

    /// <summary>
    /// 📝 Parse ProductDescription to extract BIB_ID and port suffix
    /// Examples: "client_demo A" → ("client_demo", "A")
    ///          "client_demo" → ("client_demo", "")
    /// </summary>
    private (string bibId, string portSuffix) ParseProductDescription(string productDescription)
    {
        if (string.IsNullOrEmpty(productDescription))
            return ("", "");

        var trimmed = productDescription.Trim();
        
        // Check if ends with single letter (A, B, C, D)
        if (trimmed.Length > 2 && char.IsLetter(trimmed[trimmed.Length - 1]) && trimmed[trimmed.Length - 2] == ' ')
        {
            var suffix = trimmed[trimmed.Length - 1].ToString().ToUpper();
            var bibId = trimmed.Substring(0, trimmed.Length - 2).Trim();
            
            _logger.LogDebug("📝 Parsed: '{ProductDescription}' → BIB_ID: '{BibId}', Suffix: '{Suffix}'", 
                productDescription, bibId, suffix);
            
            return (bibId, suffix);
        }

        // No suffix found, use as-is
        _logger.LogDebug("📝 No suffix found in: '{ProductDescription}' → BIB_ID: '{BibId}'", 
            productDescription, trimmed);
        
        return (trimmed, "");
    }

    /// <summary>
    /// 🔢 Convert port suffix to UUT port number
    /// A=1, B=2, C=3, D=4, empty=""=1 (default)
    /// </summary>
    private int ConvertSuffixToPortNumber(string portSuffix)
    {
        if (string.IsNullOrEmpty(portSuffix))
            return 1; // Default to port 1

        return portSuffix.ToUpper() switch
        {
            "A" => 1,
            "B" => 2,
            "C" => 3,
            "D" => 4,
            _ => 0 // Invalid suffix
        };
    }

    /// <summary>
    /// 📊 Log comprehensive discovery results
    /// </summary>
    private void LogDiscoveryResults(List<PortMapping> mappings)
    {
        _logger.LogInformation("📊 =".PadRight(60, '='));
        _logger.LogInformation("📊 DYNAMIC PORT DISCOVERY RESULTS");
        _logger.LogInformation("📊 =".PadRight(60, '='));
        
        _logger.LogInformation("🔍 Total Mappings Created: {Count}", mappings.Count);
        
        // Group by BIB_ID
        var bibGroups = mappings.GroupBy(m => m.BibId).ToList();
        foreach (var bibGroup in bibGroups)
        {
            _logger.LogInformation("📋 BIB: {BibId} ({Count} ports)", bibGroup.Key, bibGroup.Count());
            
            foreach (var mapping in bibGroup.OrderBy(m => m.UutPortNumber))
            {
                _logger.LogInformation("   🔌 {PhysicalPort} → Port {PortNumber} (Suffix: {Suffix})", 
                    mapping.PhysicalPort, mapping.UutPortNumber, 
                    string.IsNullOrEmpty(mapping.PortSuffix) ? "None" : mapping.PortSuffix);
            }
        }

        // Summary statistics
        var physicalPorts = mappings.Select(m => m.PhysicalPort).ToList();
        var portRange = physicalPorts.Any() ? $"{physicalPorts.Min()} - {physicalPorts.Max()}" : "None";
        
        _logger.LogInformation("📊 Physical Port Range: {Range}", portRange);
        _logger.LogInformation("📊 Unique BIB_IDs: {Count}", bibGroups.Count);
        _logger.LogInformation("📊 Discovery Timestamp: {Timestamp:HH:mm:ss}", DateTime.Now);
        _logger.LogInformation("📊 =".PadRight(60, '='));
    }

    #endregion
}

/// <summary>
/// Interface for Dynamic Port Mapping Service
/// </summary>
public interface IDynamicPortMappingService
{
    Task<string?> GetDynamicPortForUutPortAsync(string bibId, string uutId, int uutPortNumber);
    Task<List<PortMapping>> DiscoverPortMappingsAsync();
    Task<List<PortMapping>> GetAllPortMappingsAsync();
    DynamicPortMappingStatistics GetMappingStatistics();
}

/// <summary>
/// Port mapping model - maps physical ports to logical UUT ports
/// </summary>
public class PortMapping
{
    public string PhysicalPort { get; set; } = string.Empty;
    public string BibId { get; set; } = string.Empty;
    public string UutId { get; set; } = string.Empty;
    public int UutPortNumber { get; set; }
    public string PortSuffix { get; set; } = string.Empty;
    public string DeviceType { get; set; } = string.Empty;
    public string SerialNumber { get; set; } = string.Empty;
    public string ProductDescription { get; set; } = string.Empty;
    public DateTime DiscoveredAt { get; set; } = DateTime.Now;

    /// <summary>
    /// Full logical port identifier
    /// </summary>
    public string FullPortId => $"{BibId}.{UutId}.{UutPortNumber}";

    /// <summary>
    /// Whether this is a multi-port device mapping
    /// </summary>
    public bool IsMultiPortMapping => !string.IsNullOrEmpty(PortSuffix);

    public override string ToString()
    {
        var suffix = IsMultiPortMapping ? $" (Suffix: {PortSuffix})" : "";
        return $"{PhysicalPort} → {FullPortId}{suffix}";
    }
}

/// <summary>
/// Statistics for dynamic port mapping operations
/// </summary>
public class DynamicPortMappingStatistics
{
    public int TotalMappings { get; set; }
    public int UniqueBibIds { get; set; }
    public int UniqueUutIds { get; set; }
    public int PhysicalPorts { get; set; }
    public DateTime LastDiscovery { get; set; }
    public DateTime GeneratedAt { get; set; } = DateTime.Now;

    public string GetSummary() =>
        $"Mappings: {TotalMappings}, BIBs: {UniqueBibIds}, Physical Ports: {PhysicalPorts}, " +
        $"Last Discovery: {LastDiscovery:HH:mm:ss}";
}