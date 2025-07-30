// SerialPortPool.Core/Services/TemporaryBibMappingService.cs - NEW Week 2 CORRECTED COMPLETE FILE
// DEBUGGED: Fixed async warnings and compilation issues
using Microsoft.Extensions.Logging;
using SerialPortPool.Core.Interfaces;
using SerialPortPool.Core.Models;

namespace SerialPortPool.Core.Services;

/// <summary>
/// Temporary mapping service for BIB_ID until EEPROM integration
/// Week 2: Maps physical ports to BIB configurations based on hardware detection
/// </summary>
public class TemporaryBibMappingService : IBibMappingService
{
    private readonly ILogger<TemporaryBibMappingService> _logger;
    private readonly ISerialPortDiscovery _discovery;
    private readonly Dictionary<string, BibPortMapping> _portMappings;

    public TemporaryBibMappingService(
        ILogger<TemporaryBibMappingService> logger,
        ISerialPortDiscovery discovery)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _discovery = discovery ?? throw new ArgumentNullException(nameof(discovery));
        _portMappings = new Dictionary<string, BibPortMapping>();
        
        InitializeDefaultMappings();
    }

    /// <summary>
    /// Initialize default port-to-BIB mappings based on known hardware
    /// TODO: Replace with EEPROM reading in future sprints
    /// </summary>
    private void InitializeDefaultMappings()
    {
        _logger.LogInformation("🔧 Initializing temporary BIB mappings (until EEPROM integration)");

        // FT4232HL Device Mappings (based on your hardware)
        // TODO: Read from EEPROM when available
        var ft4232hlMappings = new[]
        {
            new BibPortMapping
            {
                PhysicalPort = "COM11",
                BibId = "bib_001",
                UutId = "uut_001", 
                PortNumber = 1,
                DeviceType = "FT4232HL",
                SerialNumber = "FT9A9OFO", // Base serial from your hardware
                Description = "FT4232HL Port A - Production Test"
            },
            new BibPortMapping
            {
                PhysicalPort = "COM12",
                BibId = "bib_001",
                UutId = "uut_001",
                PortNumber = 2,
                DeviceType = "FT4232HL", 
                SerialNumber = "FT9A9OFO",
                Description = "FT4232HL Port B - Production Test"
            },
            new BibPortMapping
            {
                PhysicalPort = "COM13",
                BibId = "bib_001",
                UutId = "uut_001",
                PortNumber = 3,
                DeviceType = "FT4232HL",
                SerialNumber = "FT9A9OFO", 
                Description = "FT4232HL Port C - Production Test"
            },
            new BibPortMapping
            {
                PhysicalPort = "COM14",
                BibId = "bib_001",
                UutId = "uut_001",
                PortNumber = 4,
                DeviceType = "FT4232HL",
                SerialNumber = "FT9A9OFO",
                Description = "FT4232HL Port D - Production Test"
            }
        };

        // FT232R Device Mappings (development/testing)
        var ft232rMappings = new[]
        {
            new BibPortMapping
            {
                PhysicalPort = "COM6",
                BibId = "bib_002",
                UutId = "uut_002",
                PortNumber = 1,
                DeviceType = "FT232R",
                SerialNumber = "AG0JU7O1A", // From your hardware
                Description = "FT232R - Development Testing"
            }
        };

        // Register all mappings
        foreach (var mapping in ft4232hlMappings.Concat(ft232rMappings))
        {
            _portMappings[mapping.PhysicalPort] = mapping;
            _logger.LogDebug($"📍 Mapped {mapping.PhysicalPort} → {mapping.BibId}.{mapping.UutId}.{mapping.PortNumber}");
        }

        _logger.LogInformation($"✅ Initialized {_portMappings.Count} temporary BIB mappings");
    }

    /// <summary>
    /// Get BIB mapping for a physical port
    /// </summary>
    public async Task<BibPortMapping?> GetBibMappingAsync(string physicalPort)
    {
        if (string.IsNullOrEmpty(physicalPort))
            return null;

        try
        {
            // Check direct mapping first
            if (_portMappings.TryGetValue(physicalPort, out var mapping))
            {
                _logger.LogDebug($"📍 Direct mapping found: {physicalPort} → {mapping.BibId}.{mapping.UutId}.{mapping.PortNumber}");
                return mapping;
            }

            // Try dynamic discovery for unmapped ports
            var discoveredMapping = await DiscoverBibMappingAsync(physicalPort);
            if (discoveredMapping != null)
            {
                _portMappings[physicalPort] = discoveredMapping;
                _logger.LogInformation($"🔍 Dynamic mapping created: {physicalPort} → {discoveredMapping.BibId}.{discoveredMapping.UutId}.{discoveredMapping.PortNumber}");
                return discoveredMapping;
            }

            _logger.LogWarning($"❌ No BIB mapping found for port {physicalPort}");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"❌ Error getting BIB mapping for port {physicalPort}");
            return null;
        }
    }

    /// <summary>
    /// Dynamic discovery of BIB mapping for unknown ports
    /// </summary>
    private async Task<BibPortMapping?> DiscoverBibMappingAsync(string physicalPort)
    {
        try
        {
            var portInfo = await _discovery.GetPortInfoAsync(physicalPort);
            if (portInfo == null || !portInfo.IsFtdiDevice)
            {
                _logger.LogDebug($"🔍 Port {physicalPort} is not an FTDI device or not found");
                return null;
            }

            // Create dynamic mapping based on device type
            var mapping = new BibPortMapping
            {
                PhysicalPort = physicalPort,
                DeviceType = portInfo.FtdiInfo?.ChipType ?? "Unknown",
                SerialNumber = portInfo.FtdiInfo?.SerialNumber ?? "Unknown",
                Description = $"Dynamic mapping for {portInfo.FriendlyName}"
            };

            // Assign BIB based on device type and client validity
            if (portInfo.IsFtdi4232H)
            {
                // Production device
                mapping.BibId = "bib_production";
                mapping.UutId = "uut_dynamic";
                mapping.PortNumber = GetNextAvailablePortNumber("bib_production", "uut_dynamic");
                
                _logger.LogInformation($"🏭 Dynamic production mapping: {physicalPort} → {mapping.BibId}.{mapping.UutId}.{mapping.PortNumber}");
            }
            else
            {
                // Development device
                mapping.BibId = "bib_development";
                mapping.UutId = "uut_dynamic";
                mapping.PortNumber = GetNextAvailablePortNumber("bib_development", "uut_dynamic");
                
                _logger.LogInformation($"🔧 Dynamic development mapping: {physicalPort} → {mapping.BibId}.{mapping.UutId}.{mapping.PortNumber}");
            }

            return mapping;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"❌ Error during dynamic BIB mapping discovery for {physicalPort}");
            return null;
        }
    }

    /// <summary>
    /// Get next available port number for dynamic mapping
    /// </summary>
    private int GetNextAvailablePortNumber(string bibId, string uutId)
    {
        var existingPorts = _portMappings.Values
            .Where(m => m.BibId == bibId && m.UutId == uutId)
            .Select(m => m.PortNumber)
            .ToList();

        return existingPorts.Any() ? existingPorts.Max() + 1 : 1;
    }

    /// <summary>
    /// Get all mapped ports for a specific BIB
    /// </summary>
    public Task<List<BibPortMapping>> GetBibPortsAsync(string bibId)
    {
        var mappings = _portMappings.Values
            .Where(m => m.BibId.Equals(bibId, StringComparison.OrdinalIgnoreCase))
            .OrderBy(m => m.UutId)
            .ThenBy(m => m.PortNumber)
            .ToList();
            
        return Task.FromResult(mappings);
    }

    /// <summary>
    /// Get all mapped ports for a specific UUT
    /// </summary>
    public Task<List<BibPortMapping>> GetUutPortsAsync(string bibId, string uutId)
    {
        var mappings = _portMappings.Values
            .Where(m => m.BibId.Equals(bibId, StringComparison.OrdinalIgnoreCase) && 
                       m.UutId.Equals(uutId, StringComparison.OrdinalIgnoreCase))
            .OrderBy(m => m.PortNumber)
            .ToList();
            
        return Task.FromResult(mappings);
    }

    /// <summary>
    /// Get all available BIB IDs
    /// </summary>
    public Task<List<string>> GetAvailableBibIdsAsync()
    {
        var bibIds = _portMappings.Values
            .Select(m => m.BibId)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(b => b)
            .ToList();
            
        return Task.FromResult(bibIds);
    }

    /// <summary>
    /// Refresh mappings by rediscovering ports
    /// </summary>
    public async Task RefreshMappingsAsync()
    {
        try
        {
            _logger.LogInformation("🔄 Refreshing BIB mappings...");
            
            var discoveredPorts = await _discovery.DiscoverPortsAsync();
            var ftdiPorts = discoveredPorts.Where(p => p.IsFtdiDevice).ToList();
            
            var newMappings = 0;
            foreach (var port in ftdiPorts)
            {
                if (!_portMappings.ContainsKey(port.PortName))
                {
                    var mapping = await DiscoverBibMappingAsync(port.PortName);
                    if (mapping != null)
                    {
                        _portMappings[port.PortName] = mapping;
                        newMappings++;
                    }
                }
            }
            
            _logger.LogInformation($"✅ BIB mapping refresh complete: {newMappings} new mappings added");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error during BIB mapping refresh");
        }
    }

    /// <summary>
    /// Get mapping statistics
    /// </summary>
    public Task<BibMappingStatistics> GetMappingStatisticsAsync()
    {
        var bibIds = _portMappings.Values.Select(m => m.BibId).Distinct().Count();
        var uutIds = _portMappings.Values.Select(m => $"{m.BibId}.{m.UutId}").Distinct().Count();
        var ft4232hPorts = _portMappings.Values.Count(m => m.DeviceType == "FT4232HL");
        var otherPorts = _portMappings.Values.Count(m => m.DeviceType != "FT4232HL");
        
        var statistics = new BibMappingStatistics
        {
            TotalMappings = _portMappings.Count,
            TotalBibs = bibIds,
            TotalUuts = uutIds,
            FT4232HLPorts = ft4232hPorts,
            OtherDevicePorts = otherPorts,
            GeneratedAt = DateTime.Now
        };
        
        return Task.FromResult(statistics);
    }

    /// <summary>
    /// Manually add or update a port mapping
    /// </summary>
    public Task<bool> AddOrUpdateMappingAsync(BibPortMapping mapping)
    {
        try
        {
            _portMappings[mapping.PhysicalPort] = mapping;
            _logger.LogInformation($"✅ Mapping added/updated: {mapping.PhysicalPort} → {mapping.BibId}.{mapping.UutId}.{mapping.PortNumber}");
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"❌ Error adding/updating mapping for {mapping.PhysicalPort}");
            return Task.FromResult(false);
        }
    }

    /// <summary>
    /// Remove a port mapping
    /// </summary>
    public Task<bool> RemoveMappingAsync(string physicalPort)
    {
        try
        {
            if (_portMappings.Remove(physicalPort))
            {
                _logger.LogInformation($"✅ Mapping removed: {physicalPort}");
                return Task.FromResult(true);
            }
            
            _logger.LogWarning($"⚠️ No mapping found to remove: {physicalPort}");
            return Task.FromResult(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"❌ Error removing mapping for {physicalPort}");
            return Task.FromResult(false);
        }
    }
}

/// <summary>
/// BIB port mapping interface
/// </summary>
public interface IBibMappingService
{
    Task<BibPortMapping?> GetBibMappingAsync(string physicalPort);
    Task<List<BibPortMapping>> GetBibPortsAsync(string bibId);
    Task<List<BibPortMapping>> GetUutPortsAsync(string bibId, string uutId);
    Task<List<string>> GetAvailableBibIdsAsync();
    Task RefreshMappingsAsync();
    Task<BibMappingStatistics> GetMappingStatisticsAsync();
    Task<bool> AddOrUpdateMappingAsync(BibPortMapping mapping);
    Task<bool> RemoveMappingAsync(string physicalPort);
}

/// <summary>
/// Mapping between physical port and BIB configuration
/// </summary>
public class BibPortMapping
{
    /// <summary>
    /// Physical COM port name (e.g., "COM11", "COM6")
    /// </summary>
    public string PhysicalPort { get; set; } = string.Empty;

    /// <summary>
    /// BIB identifier
    /// </summary>
    public string BibId { get; set; } = string.Empty;

    /// <summary>
    /// UUT identifier within the BIB
    /// </summary>
    public string UutId { get; set; } = string.Empty;

    /// <summary>
    /// Port number within the UUT
    /// </summary>
    public int PortNumber { get; set; }

    /// <summary>
    /// FTDI device type (e.g., "FT4232HL", "FT232R")
    /// </summary>
    public string DeviceType { get; set; } = string.Empty;

    /// <summary>
    /// Device serial number
    /// </summary>
    public string SerialNumber { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable description
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Mapping creation time
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    /// <summary>
    /// Whether this is a static (predefined) or dynamic mapping
    /// </summary>
    public bool IsStaticMapping { get; set; } = true;

    /// <summary>
    /// Full BIB path: BIB.UUT.PORT
    /// </summary>
    public string FullPath => $"{BibId}.{UutId}.{PortNumber}";

    public override string ToString()
    {
        return $"{PhysicalPort} → {FullPath} ({DeviceType})";
    }
}

/// <summary>
/// BIB mapping statistics
/// </summary>
public class BibMappingStatistics
{
    public int TotalMappings { get; set; }
    public int TotalBibs { get; set; }
    public int TotalUuts { get; set; }
    public int FT4232HLPorts { get; set; }
    public int OtherDevicePorts { get; set; }
    public DateTime GeneratedAt { get; set; }

    public override string ToString()
    {
        return $"BIB Mappings: {TotalMappings} ports, {TotalBibs} BIBs, {TotalUuts} UUTs, {FT4232HLPorts} FT4232HL";
    }
}