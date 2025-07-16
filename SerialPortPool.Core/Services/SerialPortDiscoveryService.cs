using System.IO.Ports;
using System.Management;
using Microsoft.Extensions.Logging;
using SerialPortPool.Core.Interfaces;
using SerialPortPool.Core.Models;

namespace SerialPortPool.Core.Services;

/// <summary>
/// Service for discovering serial ports available in the system
/// </summary>
public class SerialPortDiscoveryService : ISerialPortDiscovery
{
    private readonly ILogger<SerialPortDiscoveryService> _logger;

    public SerialPortDiscoveryService(ILogger<SerialPortDiscoveryService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Discover all serial ports currently available in the system
    /// </summary>
    public async Task<IEnumerable<SerialPortInfo>> DiscoverPortsAsync()
    {
        try
        {
            _logger.LogDebug("Starting serial port discovery...");

            // Get basic port names from System.IO.Ports
            var portNames = SerialPort.GetPortNames();
            _logger.LogInformation($"Found {portNames.Length} serial ports: {string.Join(", ", portNames)}");

            var portInfos = new List<SerialPortInfo>();

            // Enrich each port with additional information
            foreach (var portName in portNames)
            {
                var portInfo = await GetPortInfoAsync(portName);
                if (portInfo != null)
                {
                    portInfos.Add(portInfo);
                }
            }

            _logger.LogInformation($"Successfully processed {portInfos.Count} serial ports");
            return portInfos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during serial port discovery");
            return Enumerable.Empty<SerialPortInfo>();
        }
    }

    /// <summary>
    /// Get detailed information about a specific port
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
            _logger.LogDebug($"Getting information for port {portName}");

            var portInfo = new SerialPortInfo
            {
                PortName = portName,
                Status = PortStatus.Available,
                LastSeen = DateTime.Now
            };

            // Enrich with WMI data
            await EnrichPortInfoWithWmiAsync(portInfo);

            // Test if port is accessible
            await TestPortAccessibilityAsync(portInfo);

            _logger.LogDebug($"Port {portName}: {portInfo.FriendlyName} ({portInfo.Status})");
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
                        // Get friendly name (e.g., "USB Serial Port (COM3)")
                        var description = port["Description"]?.ToString();
                        var caption = port["Caption"]?.ToString();

                        portInfo.FriendlyName = !string.IsNullOrEmpty(description) ? description :
                                              !string.IsNullOrEmpty(caption) ? caption :
                                              portInfo.PortName;

                        // Get device ID for hardware identification
                        portInfo.DeviceId = port["PNPDeviceID"]?.ToString() ?? string.Empty;

                        _logger.LogDebug($"WMI enrichment for {portInfo.PortName}: {portInfo.FriendlyName}");
                        break; // Found the port
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

                // If not found in Win32_SerialPort, try Win32_PnPEntity for USB serial devices
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
}
