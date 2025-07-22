// SerialPortPool.Core/Interfaces/IMultiPortDeviceAnalyzer.cs - ÉTAPE 5 NEW
using SerialPortPool.Core.Models;
using SerialPortPool.Core.Services;

namespace SerialPortPool.Core.Interfaces;

/// <summary>
/// Interface for analyzing and grouping multi-port FTDI devices (ÉTAPE 5)
/// Provides functionality to detect when multiple serial ports belong to the same physical device
/// </summary>
public interface IMultiPortDeviceAnalyzer
{
    /// <summary>
    /// Analyze discovered ports to identify multi-port devices (FT4232H = 4 ports)
    /// </summary>
    /// <param name="ports">Collection of discovered ports</param>
    /// <returns>Collection of device groups, each representing a physical device</returns>
    Task<IEnumerable<DeviceGroup>> AnalyzeDeviceGroupsAsync(IEnumerable<SerialPortInfo> ports);

    /// <summary>
    /// Group ports by physical device using serial numbers and device IDs
    /// </summary>
    /// <param name="ports">Collection of ports to group</param>
    /// <returns>Dictionary mapping device identifiers to their ports</returns>
    Dictionary<string, List<SerialPortInfo>> GroupPortsByDevice(IEnumerable<SerialPortInfo> ports);

    /// <summary>
    /// Get statistics about device grouping results
    /// </summary>
    /// <param name="deviceGroups">Collection of device groups to analyze</param>
    /// <returns>Statistics about the grouping</returns>
    DeviceGroupingStatistics GetGroupingStatistics(IEnumerable<DeviceGroup> deviceGroups);
}