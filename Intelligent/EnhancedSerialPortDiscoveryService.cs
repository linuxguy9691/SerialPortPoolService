using System.IO.Ports;
using System.Management;
using Microsoft.Extensions.Logging;
using SerialPortPool.Core.Interfaces;
using SerialPortPool.Core.Models;

namespace SerialPortPool.Core.Services;

/// <summary>
/// Enhanced serial port discovery service with FTDI analysis and validation
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
}