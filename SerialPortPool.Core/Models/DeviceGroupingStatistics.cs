// SerialPortPool.Core/Models/DeviceGroupingStatistics.cs - NEW MISSING CLASS
namespace SerialPortPool.Core.Models;

/// <summary>
/// Statistics about device grouping results for multi-port device analysis
/// </summary>
public class DeviceGroupingStatistics
{
    /// <summary>
    /// Total number of physical devices found
    /// </summary>
    public int TotalDevices { get; set; }
    
    /// <summary>
    /// Total number of individual ports across all devices
    /// </summary>
    public int TotalPorts { get; set; }
    
    /// <summary>
    /// Number of devices that have multiple ports (FT4232H, FT2232H, etc.)
    /// </summary>
    public int MultiPortDevices { get; set; }
    
    /// <summary>
    /// Number of devices that have only one port (FT232R, etc.)
    /// </summary>
    public int SinglePortDevices { get; set; }
    
    /// <summary>
    /// Average number of ports per device
    /// </summary>
    public double AveragePortsPerDevice { get; set; }
    
    /// <summary>
    /// Maximum number of ports on any single device
    /// </summary>
    public int LargestDevicePortCount { get; set; }
    
    /// <summary>
    /// Number of FTDI devices found
    /// </summary>
    public int FtdiDevices { get; set; }
    
    /// <summary>
    /// Number of non-FTDI devices found
    /// </summary>
    public int NonFtdiDevices { get; set; }
    
    /// <summary>
    /// When these statistics were generated
    /// </summary>
    public DateTime GeneratedAt { get; set; } = DateTime.Now;
    
    /// <summary>
    /// Percentage of devices that are multi-port
    /// </summary>
    public double MultiPortPercentage => TotalDevices > 0 ? (MultiPortDevices * 100.0 / TotalDevices) : 0;
    
    /// <summary>
    /// Percentage of devices that are FTDI
    /// </summary>
    public double FtdiPercentage => TotalDevices > 0 ? (FtdiDevices * 100.0 / TotalDevices) : 0;
    
    /// <summary>
    /// Whether any multi-port devices were detected
    /// </summary>
    public bool HasMultiPortDevices => MultiPortDevices > 0;
    
    /// <summary>
    /// Whether the system has good FTDI coverage (80%+)
    /// </summary>
    public bool HasGoodFtdiCoverage => FtdiPercentage >= 80;
    
    public override string ToString()
    {
        return $"Device Grouping: {TotalDevices} devices, {TotalPorts} ports, {MultiPortDevices} multi-port, {FtdiDevices} FTDI";
    }
    
    /// <summary>
    /// Get a detailed summary of the statistics
    /// </summary>
    /// <returns>Formatted summary string</returns>
    public string GetDetailedSummary()
    {
        var summary = new List<string>
        {
            $"Total Devices: {TotalDevices}",
            $"Total Ports: {TotalPorts}",
            $"Multi-Port Devices: {MultiPortDevices} ({MultiPortPercentage:F1}%)",
            $"Single-Port Devices: {SinglePortDevices}",
            $"FTDI Devices: {FtdiDevices} ({FtdiPercentage:F1}%)",
            $"Average Ports/Device: {AveragePortsPerDevice:F1}",
            $"Largest Device: {LargestDevicePortCount} ports"
        };
        
        return string.Join(", ", summary);
    }
}