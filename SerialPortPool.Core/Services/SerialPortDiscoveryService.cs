using System.IO.Ports;
using System.Management;
using Microsoft.Extensions.Logging;
using SerialPortPool.Core.Interfaces;
using SerialPortPool.Core.Models;

namespace SerialPortPool.Core.Services;

/// <summary>
/// Enhanced serial port discovery service with FTDI analysis, validation, and device grouping (ÉTAPE 5 Complete)
/// </summary>
public class EnhancedSerialPortDiscoveryService : ISerialPortDiscovery
{
    private readonly ILogger<EnhancedSerialPortDiscoveryService> _logger;
    private readonly IFtdiDeviceReader _ftdiReader;
    private readonly ISerialPortValidator _validator;
    private readonly PortValidationConfiguration _defaultConfig;

    public EnhancedSerialPortDiscoveryService(
        ILogger<EnhancedSerialPortDiscoveryService> logger,
        IFtdiDeviceReader ftdiReader,
        ISerialPortValidator validator,
        PortValidationConfiguration? defaultConfig = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _ftdiReader = ftdiReader ?? throw new ArgumentNullException(nameof(ftdiReader));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _defaultConfig = defaultConfig ?? PortValidationConfiguration.CreateDevelopmentDefault();
    }

    #region Original Discovery Methods (Sprint 2)

    /// <summary>
    /// Discover all serial ports with FTDI analysis and validation
    /// </summary>
    public async Task<IEnumerable<SerialPortInfo>> DiscoverPortsAsync()
    {
        try
        {
            _logger.LogDebug("Starting enhanced serial port discovery with FTDI analysis...");
            
            // Get basic port names from System.IO.Ports
            var portNames = SerialPort.GetPortNames();
            _logger.LogInformation($"Found {portNames.Length} serial ports: {string.Join(", ", portNames)}");

            var portInfos = new List<SerialPortInfo>();

            // Process each port with full analysis
            foreach (var portName in portNames)
            {
                var portInfo = await GetPortInfoAsync(portName);
                if (portInfo != null)
                {
                    portInfos.Add(portInfo);
                }
            }

            // Log summary
            var ftdiCount = portInfos.Count(p => p.IsFtdiDevice);
            var validCount = portInfos.Count(p => p.IsValidForPool);
            
            _logger.LogInformation($"Discovery complete: {portInfos.Count} ports processed, {ftdiCount} FTDI devices, {validCount} valid for pool");

            return portInfos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during enhanced serial port discovery");
            return Enumerable.Empty<SerialPortInfo>();
        }
    }

    /// <summary>
    /// Get comprehensive information about a specific port with FTDI analysis
    /// </summary>
    public async Task<SerialPortInfo?> GetPortInfoAsync(string portName)
    {
        if (string.IsNullOrWhiteSpace(portName))
        {
            _logger.LogWarning("Port name is null or empty");
            return null;
        }

        try
        {
            _logger.LogDebug($"Getting comprehensive information for port {portName}");

            // Create basic port info
            var portInfo = new SerialPortInfo
            {
                PortName = portName,
                Status = PortStatus.Available,
                LastSeen = DateTime.Now
            };

            // Enrich with WMI data
            await EnrichPortInfoWithWmiAsync(portInfo);

            // Analyze FTDI information
            await AnalyzeFtdiDeviceAsync(portInfo);

            // Test port accessibility
            await TestPortAccessibilityAsync(portInfo);

            // Validate port for pool eligibility
            await ValidatePortAsync(portInfo);

            _logger.LogDebug($"Port analysis complete: {portInfo.ToDetailedString()}");
            return portInfo;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, $"Error getting information for port {portName}");
            
            // Return basic info even if enrichment fails
            return new SerialPortInfo
            {
                PortName = portName,
                FriendlyName = portName,
                Status = PortStatus.Error,
                LastSeen = DateTime.Now
            };
        }
    }

    /// <summary>
    /// Discover only ports that are valid for the pool
    /// </summary>
    public async Task<IEnumerable<SerialPortInfo>> DiscoverValidPortsAsync(PortValidationConfiguration? configuration = null)
    {
        var allPorts = await DiscoverPortsAsync();
        var validPorts = await _validator.GetValidPortsAsync(allPorts, configuration);
        
        _logger.LogInformation($"Valid ports discovery: {validPorts.Count()} out of {allPorts.Count()} ports are valid for pool");
        return validPorts;
    }

    #endregion

    #region ÉTAPE 5 NEW - Device Grouping Methods

    /// <summary>
    /// Discover ports and group them by physical device (ÉTAPE 5 Phase 2)
    /// This method combines port discovery with device grouping analysis
    /// </summary>
    /// <param name="configuration">Optional validation configuration for filtering</param>
    /// <returns>Collection of device groups with their associated ports</returns>
    public async Task<IEnumerable<DeviceGroup>> DiscoverDeviceGroupsAsync(PortValidationConfiguration? configuration = null)
    {
        try
        {
            _logger.LogDebug("Starting enhanced discovery with device grouping analysis");
            
            // Step 1: Discover all ports with FTDI analysis (existing functionality)
            var allPorts = await DiscoverPortsAsync();
            var portList = allPorts.ToList();
            
            if (!portList.Any())
            {
                _logger.LogInformation("No ports discovered - returning empty device groups");
                return Enumerable.Empty<DeviceGroup>();
            }
            
            // Step 2: Apply validation filtering if configuration provided
            IEnumerable<SerialPortInfo> portsToGroup = portList;
            if (configuration != null)
            {
                var validPorts = await _validator.GetValidPortsAsync(portList, configuration);
                portsToGroup = validPorts;
                
                _logger.LogDebug($"Port filtering applied: {portList.Count} total → {portsToGroup.Count()} valid");
            }
            
            // Step 3: Analyze and group ports by physical device
            var deviceAnalyzer = CreateDeviceAnalyzer();
            var deviceGroups = await deviceAnalyzer.AnalyzeDeviceGroupsAsync(portsToGroup);
            var groupList = deviceGroups.ToList();
            
            // Step 4: Log comprehensive results
            LogDeviceGroupingResults(portList, groupList);
            
            return groupList;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during device grouping discovery");
            return Enumerable.Empty<DeviceGroup>();
        }
    }
    
    /// <summary>
    /// Get device grouping statistics for current system (ÉTAPE 5 Phase 2)
    /// </summary>
    /// <returns>Statistics about discovered device groups</returns>
    public async Task<DeviceGroupingStatistics> GetDeviceGroupingStatisticsAsync()
    {
        try
        {
            var deviceGroups = await DiscoverDeviceGroupsAsync();
            var analyzer = CreateDeviceAnalyzer();
            
            return analyzer.GetGroupingStatistics(deviceGroups);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting device grouping statistics");
            return new DeviceGroupingStatistics();
        }
    }
    
    /// <summary>
    /// Discover only multi-port devices (FT4232H, FT2232H, etc.) (ÉTAPE 5 Phase 2)
    /// </summary>
    /// <param name="configuration">Optional validation configuration</param>
    /// <returns>Collection of multi-port device groups only</returns>
    public async Task<IEnumerable<DeviceGroup>> DiscoverMultiPortDevicesAsync(PortValidationConfiguration? configuration = null)
    {
        try
        {
            _logger.LogDebug("Discovering multi-port devices only");
            
            var allDeviceGroups = await DiscoverDeviceGroupsAsync(configuration);
            var multiPortGroups = allDeviceGroups.Where(g => g.IsMultiPortDevice).ToList();
            
            _logger.LogInformation($"Multi-port device discovery complete: {multiPortGroups.Count} multi-port devices found");
            
            return multiPortGroups;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error discovering multi-port devices");
            return Enumerable.Empty<DeviceGroup>();
        }
    }
    
    /// <summary>
    /// Find device groups that contain a specific port (ÉTAPE 5 Phase 2)
    /// </summary>
    /// <param name="portName">Port name to search for</param>
    /// <returns>Device group containing the port, or null if not found</returns>
    public async Task<DeviceGroup?> FindDeviceGroupByPortAsync(string portName)
    {
        if (string.IsNullOrWhiteSpace(portName))
            return null;
            
        try
        {
            _logger.LogDebug($"Finding device group for port {portName}");
            
            var deviceGroups = await DiscoverDeviceGroupsAsync();
            var deviceGroup = deviceGroups.FirstOrDefault(g => 
                g.Ports.Any(p => p.PortName.Equals(portName, StringComparison.OrdinalIgnoreCase)));
            
            if (deviceGroup != null)
            {
                _logger.LogDebug($"Port {portName} found in device group {deviceGroup.DeviceId} with {deviceGroup.PortCount} total ports");
            }
            else
            {
                _logger.LogWarning($"Port {portName} not found in any device group");
            }
            
            return deviceGroup;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error finding device group for port {portName}");
            return null;
        }
    }
    
    /// <summary>
    /// Get detailed analysis of a specific device group (ÉTAPE 5 Phase 2)
    /// </summary>
    /// <param name="deviceId">Device identifier</param>
    /// <returns>Device group with detailed analysis, or null if not found</returns>
    public async Task<DeviceGroup?> GetDeviceGroupAnalysisAsync(string deviceId)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
            return null;
            
        try
        {
            var deviceGroups = await DiscoverDeviceGroupsAsync();
            var deviceGroup = deviceGroups.FirstOrDefault(g => 
                g.DeviceId.Equals(deviceId, StringComparison.OrdinalIgnoreCase));
            
            if (deviceGroup != null)
            {
                _logger.LogInformation($"Device group analysis: {deviceGroup.GetSummary()}");
                
                // Log detailed port information
                foreach (var port in deviceGroup.Ports.OrderBy(p => p.PortName))
                {
                    _logger.LogDebug($"  Port {port.PortName}: {port.Status}, FTDI: {port.IsFtdiDevice}, Valid: {port.IsValidForPool}");
                }
            }
            
            return deviceGroup;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting device group analysis for {deviceId}");
            return null;
        }
    }

    #endregion

    #region Private Helper Methods (Original Sprint 2)

    /// <summary>
    /// Enrich port information using Windows Management Instrumentation (WMI)
    /// </summary>
    private async Task EnrichPortInfoWithWmiAsync(SerialPortInfo portInfo)
    {
        try
        {
            await Task.Run(() =>
            {
                // Query Win32_SerialPort for additional information
                using var searcher = new ManagementObjectSearcher(
                    "SELECT * FROM Win32_SerialPort WHERE DeviceID = '" + portInfo.PortName + "'");
                
                using var collection = searcher.Get();
                
                foreach (ManagementObject port in collection)
                {
                    try
                    {
                        // Get friendly name
                        var description = port["Description"]?.ToString();
                        var caption = port["Caption"]?.ToString();
                        
                        portInfo.FriendlyName = !string.IsNullOrEmpty(description) ? description :
                                              !string.IsNullOrEmpty(caption) ? caption :
                                              portInfo.PortName;

                        // Get device ID for hardware identification
                        portInfo.DeviceId = port["PNPDeviceID"]?.ToString() ?? string.Empty;

                        _logger.LogDebug($"WMI enrichment for {portInfo.PortName}: {portInfo.FriendlyName}");
                        break;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug(ex, $"Error reading WMI properties for {portInfo.PortName}");
                    }
                    finally
                    {
                        port?.Dispose();
                    }
                }

                // If not found in Win32_SerialPort, try Win32_PnPEntity
                if (string.IsNullOrEmpty(portInfo.FriendlyName) || portInfo.FriendlyName == portInfo.PortName)
                {
                    EnrichFromPnPEntity(portInfo);
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, $"WMI query failed for {portInfo.PortName}, using basic name");
            if (string.IsNullOrEmpty(portInfo.FriendlyName))
            {
                portInfo.FriendlyName = portInfo.PortName;
            }
        }
    }

    /// <summary>
    /// Try to get information from Win32_PnPEntity (for USB serial devices)
    /// </summary>
    private void EnrichFromPnPEntity(SerialPortInfo portInfo)
    {
        try
        {
            using var searcher = new ManagementObjectSearcher(
                $"SELECT * FROM Win32_PnPEntity WHERE Name LIKE '%{portInfo.PortName}%'");
            
            using var collection = searcher.Get();
            
            foreach (ManagementObject entity in collection)
            {
                try
                {
                    var name = entity["Name"]?.ToString();
                    var deviceId = entity["DeviceID"]?.ToString();
                    
                    if (!string.IsNullOrEmpty(name) && name.Contains(portInfo.PortName))
                    {
                        portInfo.FriendlyName = name;
                        if (string.IsNullOrEmpty(portInfo.DeviceId) && !string.IsNullOrEmpty(deviceId))
                        {
                            portInfo.DeviceId = deviceId;
                        }
                        
                        _logger.LogDebug($"PnP enrichment for {portInfo.PortName}: {portInfo.FriendlyName}");
                        break;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, $"Error reading PnP entity for {portInfo.PortName}");
                }
                finally
                {
                    entity?.Dispose();
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, $"PnP query failed for {portInfo.PortName}");
        }
    }

    /// <summary>
    /// Analyze FTDI device information
    /// </summary>
    private async Task AnalyzeFtdiDeviceAsync(SerialPortInfo portInfo)
    {
        try
        {
            if (!string.IsNullOrEmpty(portInfo.DeviceId) && portInfo.DeviceId.Contains("FTDIBUS"))
            {
                _logger.LogDebug($"FTDI device detected for {portInfo.PortName}, analyzing...");
                
                var ftdiInfo = await _ftdiReader.ReadDeviceInfoFromIdAsync(portInfo.DeviceId);
                if (ftdiInfo != null)
                {
                    portInfo.IsFtdiDevice = true;
                    portInfo.FtdiInfo = ftdiInfo;
                    
                    _logger.LogInformation($"FTDI analysis complete: {portInfo.PortName} → {ftdiInfo.ChipType} (VID: {ftdiInfo.VendorId}, PID: {ftdiInfo.ProductId})");
                }
            }
            else
            {
                // Try to read FTDI info anyway in case device ID doesn't contain FTDIBUS
                var ftdiInfo = await _ftdiReader.ReadDeviceInfoAsync(portInfo.PortName);
                if (ftdiInfo != null)
                {
                    portInfo.IsFtdiDevice = true;
                    portInfo.FtdiInfo = ftdiInfo;
                    
                    _logger.LogInformation($"FTDI device found: {portInfo.PortName} → {ftdiInfo.ChipType}");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, $"Error analyzing FTDI device for {portInfo.PortName}");
        }
    }

    /// <summary>
    /// Test if the port is accessible (can be opened)
    /// </summary>
    private async Task TestPortAccessibilityAsync(SerialPortInfo portInfo)
    {
        try
        {
            await Task.Run(() =>
            {
                using var serialPort = new SerialPort(portInfo.PortName);
                
                // Try to open the port briefly to test accessibility
                serialPort.Open();
                
                // Port is accessible
                portInfo.Status = PortStatus.Available;
                
                _logger.LogDebug($"Port {portInfo.PortName} is accessible");
            });
        }
        catch (UnauthorizedAccessException)
        {
            // Port is in use by another application
            portInfo.Status = PortStatus.Allocated;
            _logger.LogDebug($"Port {portInfo.PortName} is already in use");
        }
        catch (Exception ex)
        {
            // Port has some other issue
            portInfo.Status = PortStatus.Error;
            _logger.LogDebug(ex, $"Port {portInfo.PortName} has accessibility issues");
        }
    }

    /// <summary>
    /// Validate port for pool eligibility
    /// </summary>
    private async Task ValidatePortAsync(SerialPortInfo portInfo)
    {
        try
        {
            var validationResult = await _validator.ValidatePortAsync(portInfo, _defaultConfig);
            portInfo.ValidationResult = validationResult;
            portInfo.IsValidForPool = validationResult.IsValid;
            
            _logger.LogDebug($"Port {portInfo.PortName} validation: {validationResult.Reason} (Score: {validationResult.ValidationScore}%)");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error validating port {portInfo.PortName}");
            portInfo.IsValidForPool = false;
        }
    }

    #endregion

    #region Private Helper Methods (ÉTAPE 5 NEW)

    /// <summary>
    /// Create device analyzer instance with proper dependencies (ÉTAPE 5 Phase 2)
    /// </summary>
    private MultiPortDeviceAnalyzer CreateDeviceAnalyzer()
    {
        // Use NullLogger for now to avoid circular dependencies with DI
        var analyzerLogger = Microsoft.Extensions.Logging.Abstractions.NullLogger<MultiPortDeviceAnalyzer>.Instance;
        return new MultiPortDeviceAnalyzer(analyzerLogger, _ftdiReader);
    }
    
    /// <summary>
    /// Log comprehensive device grouping results (ÉTAPE 5 Phase 2)
    /// </summary>
    private void LogDeviceGroupingResults(List<SerialPortInfo> allPorts, List<DeviceGroup> deviceGroups)
    {
        _logger.LogInformation($"=== DEVICE GROUPING RESULTS ===");
        _logger.LogInformation($"Total ports discovered: {allPorts.Count}");
        _logger.LogInformation($"Physical devices found: {deviceGroups.Count}");
        
        // Group statistics
        var multiPortDevices = deviceGroups.Where(g => g.IsMultiPortDevice).ToList();
        var singlePortDevices = deviceGroups.Where(g => !g.IsMultiPortDevice).ToList();
        var ftdiDevices = deviceGroups.Where(g => g.IsFtdiDevice).ToList();
        var clientValidDevices = deviceGroups.Where(g => g.IsClientValidDevice).ToList();
        
        _logger.LogInformation($"Multi-port devices: {multiPortDevices.Count}");
        _logger.LogInformation($"Single-port devices: {singlePortDevices.Count}");
        _logger.LogInformation($"FTDI devices: {ftdiDevices.Count}");
        _logger.LogInformation($"Client-valid devices (FT4232H): {clientValidDevices.Count}");
        
        // Detail each device group
        foreach (var group in deviceGroups.OrderBy(g => g.DeviceId))
        {
            var deviceType = group.IsFtdiDevice ? $"FTDI {group.DeviceInfo!.ChipType}" : "Non-FTDI";
            var portCount = group.PortCount > 1 ? $" ({group.PortCount} ports)" : "";
            var portNames = string.Join(", ", group.GetPortNames());
            var validIcon = group.IsClientValidDevice ? "✅" : "❌";
            
            _logger.LogInformation($"  {validIcon} {deviceType}{portCount}: {portNames}");
            
            if (group.SharedSystemInfo != null)
            {
                _logger.LogDebug($"    System info: {group.SharedSystemInfo.GetSummary()}");
            }
        }
        
        _logger.LogInformation($"=== END DEVICE GROUPING RESULTS ===");
    }

    #endregion
}