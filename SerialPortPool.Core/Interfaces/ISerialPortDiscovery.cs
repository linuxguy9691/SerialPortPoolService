using SerialPortPool.Core.Models;

namespace SerialPortPool.Core.Interfaces;

/// <summary>
/// Service for discovering serial ports in the system
/// </summary>
public interface ISerialPortDiscovery
{
    /// <summary>
    /// Discover all serial ports currently available in the system
    /// </summary>
    /// <returns>Collection of discovered serial ports</returns>
    Task<IEnumerable<SerialPortInfo>> DiscoverPortsAsync();

    /// <summary>
    /// Get detailed information about a specific port
    /// </summary>
    /// <param name="portName">The port name (e.g., "COM3")</param>
    /// <returns>Port information or null if not found</returns>
    Task<SerialPortInfo?> GetPortInfoAsync(string portName);
}
