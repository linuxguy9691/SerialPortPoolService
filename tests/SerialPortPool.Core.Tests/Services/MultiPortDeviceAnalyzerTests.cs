// tests/SerialPortPool.Core.Tests/Services/MultiPortDeviceAnalyzerTests.cs - ÉTAPE 5 PHASE 1 TESTS
using Microsoft.Extensions.Logging;
using Moq;
using SerialPortPool.Core.Interfaces;
using SerialPortPool.Core.Models;
using SerialPortPool.Core.Services;
using Xunit;

namespace SerialPortPool.Core.Tests.Services;

/// <summary>
/// Tests for MultiPortDeviceAnalyzer (ÉTAPE 5 Phase 1)
/// Validates device grouping logic and multi-port detection
/// </summary>
public class MultiPortDeviceAnalyzerTests
{
    private readonly Mock<ILogger<MultiPortDeviceAnalyzer>> _mockLogger;
    private readonly Mock<IFtdiDeviceReader> _mockFtdiReader;
    private readonly MultiPortDeviceAnalyzer _analyzer;

    public MultiPortDeviceAnalyzerTests()
    {
        _mockLogger = new Mock<ILogger<MultiPortDeviceAnalyzer>>();
        _mockFtdiReader = new Mock<IFtdiDeviceReader>();
        _analyzer = new MultiPortDeviceAnalyzer(_mockLogger.Object, _mockFtdiReader.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidParameters_InitializesCorrectly()
    {
        // Arrange & Act
        var analyzer = new MultiPortDeviceAnalyzer(_mockLogger.Object, _mockFtdiReader.Object);

        // Assert
        Assert.NotNull(analyzer);
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new MultiPortDeviceAnalyzer(null!, _mockFtdiReader.Object));
    }

    [Fact]
    public void Constructor_WithNullFtdiReader_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new MultiPortDeviceAnalyzer(_mockLogger.Object, null!));
    }

    #endregion

    #region AnalyzeDeviceGroupsAsync Tests

    [Fact]
    public async Task AnalyzeDeviceGroups_WithNullPorts_ReturnsEmpty()
    {
        // Act
        var result = await _analyzer.AnalyzeDeviceGroupsAsync(null!);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task AnalyzeDeviceGroups_WithEmptyPorts_ReturnsEmpty()
    {
        // Act
        var result = await _analyzer.AnalyzeDeviceGroupsAsync(Enumerable.Empty<SerialPortInfo>());

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task AnalyzeDeviceGroups_WithFT4232H_GroupsCorrectly()
    {
        // Arrange - Simulate 4 ports from same FT4232H device
        var ft4232hPorts = CreateFt4232HPorts();
        
        // Mock EEPROM data reading for the ports
        _mockFtdiReader.Setup(f => f.ReadEepromDataAsync(It.IsAny<string>()))
                      .ReturnsAsync(new Dictionary<string, string>
                      {
                          ["SerialNumber"] = "FT4232H_SERIAL",
                          ["Manufacturer"] = "FTDI",
                          ["ProductDescription"] = "FT4232H Device",
                          ["VendorId"] = "0403",
                          ["ProductId"] = "6011",
                          ["ChipType"] = "FT4232H"
                      });

        // Act
        var result = await _analyzer.AnalyzeDeviceGroupsAsync(ft4232hPorts);
        var deviceGroups = result.ToList();

        // Assert
        Assert.Single(deviceGroups); // Should be grouped into one device
        
        var deviceGroup = deviceGroups.First();
        Assert.Equal(4, deviceGroup.PortCount);
        Assert.True(deviceGroup.IsMultiPortDevice);
        Assert.True(deviceGroup.IsFtdiDevice);
        Assert.Equal("FT4232H_SERIAL", deviceGroup.SerialNumber);
        Assert.Contains("COM3", deviceGroup.GetPortNames());
        Assert.Contains("COM4", deviceGroup.GetPortNames());
        Assert.Contains("COM5", deviceGroup.GetPortNames());
        Assert.Contains("COM6", deviceGroup.GetPortNames());
    }

    [Fact]
    public async Task AnalyzeDeviceGroups_WithMixedDevices_GroupsCorrectly()
    {
        // Arrange - Mix of FT232R (single) and FT4232H (multi) devices
        var mixedPorts = new List<SerialPortInfo>();
        
        // Single FT232R device
        mixedPorts.Add(new SerialPortInfo
        {
            PortName = "COM1",
            IsFtdiDevice = true,
            FtdiInfo = FtdiDeviceInfo.ParseFromDeviceId("FTDIBUS\\VID_0403+PID_6001+FT232R_SERIAL\\0000"),
            Status = PortStatus.Available
        });
        
        // Multi-port FT4232H device (2 ports visible)
        mixedPorts.AddRange(CreateFt4232HPorts().Take(2));

        _mockFtdiReader.Setup(f => f.ReadEepromDataAsync(It.IsAny<string>()))
                      .ReturnsAsync(new Dictionary<string, string> { ["SerialNumber"] = "TEST_SERIAL" });

        // Act
        var result = await _analyzer.AnalyzeDeviceGroupsAsync(mixedPorts);
        var deviceGroups = result.ToList();

        // Assert
        Assert.Equal(2, deviceGroups.Count); // FT232R + FT4232H = 2 devices
        
        // Find single port device
        var singlePortDevice = deviceGroups.First(g => g.PortCount == 1);
        Assert.False(singlePortDevice.IsMultiPortDevice);
        Assert.Equal("COM1", singlePortDevice.Ports.First().PortName);
        
        // Find multi-port device
        var multiPortDevice = deviceGroups.First(g => g.PortCount == 2);
        Assert.True(multiPortDevice.IsMultiPortDevice);
    }

    [Fact]
    public async Task AnalyzeDeviceGroups_WithNonFtdiDevices_CreatesGroups()
    {
        // Arrange
        var nonFtdiPorts = new[]
        {
            new SerialPortInfo 
            { 
                PortName = "COM10", 
                IsFtdiDevice = false, 
                DeviceId = "USB\\VID_1234+PID_5678+SERIAL1\\0000",
                Status = PortStatus.Available 
            },
            new SerialPortInfo 
            { 
                PortName = "COM11", 
                IsFtdiDevice = false,
                DeviceId = "USB\\VID_1234+PID_5678+SERIAL2\\0000", 
                Status = PortStatus.Available 
            }
        };

        // Act
        var result = await _analyzer.AnalyzeDeviceGroupsAsync(nonFtdiPorts);
        var deviceGroups = result.ToList();

        // Assert
        Assert.Equal(2, deviceGroups.Count); // Different serials = different devices
        Assert.All(deviceGroups, g => Assert.False(g.IsFtdiDevice));
        Assert.All(deviceGroups, g => Assert.False(g.IsMultiPortDevice));
        Assert.All(deviceGroups, g => Assert.Equal(1, g.PortCount));
    }

    #endregion

    #region GroupPortsByDevice Tests

    [Fact]
    public void GroupPortsByDevice_SameSerialNumber_GroupsTogether()
    {
        // Arrange
        var ports = CreateFt4232HPorts();

        // Act
        var groups = _analyzer.GroupPortsByDevice(ports);

        // Assert
        Assert.Single(groups); // All ports should be in one group
        var group = groups.First();
        Assert.Equal(4, group.Value.Count);
        Assert.Contains(group.Value, p => p.PortName == "COM3");
        Assert.Contains(group.Value, p => p.PortName == "COM6");
    }

    [Fact]
    public void GroupPortsByDevice_DifferentSerials_GroupsSeparately()
    {
        // Arrange
        var ports = new[]
        {
            new SerialPortInfo
            {
                PortName = "COM1",
                IsFtdiDevice = true,
                FtdiInfo = FtdiDeviceInfo.ParseFromDeviceId("FTDIBUS\\VID_0403+PID_6001+SERIAL1\\0000")
            },
            new SerialPortInfo
            {
                PortName = "COM2",
                IsFtdiDevice = true,
                FtdiInfo = FtdiDeviceInfo.ParseFromDeviceId("FTDIBUS\\VID_0403+PID_6001+SERIAL2\\0000")
            }
        };

        // Act
        var groups = _analyzer.GroupPortsByDevice(ports);

        // Assert
        Assert.Equal(2, groups.Count); // Different serials = separate groups
        Assert.All(groups.Values, portList => Assert.Single(portList));
    }

    [Fact]
    public void GroupPortsByDevice_WithEmptyCollection_ReturnsEmpty()
    {
        // Act
        var groups = _analyzer.GroupPortsByDevice(Enumerable.Empty<SerialPortInfo>());

        // Assert
        Assert.Empty(groups);
    }

    #endregion

    #region DeviceGroup Model Tests

    [Fact]
    public void DeviceGroup_CreateSinglePortGroup_Works()
    {
        // Arrange
        var port = new SerialPortInfo
        {
            PortName = "COM7",
            IsFtdiDevice = true,
            FtdiInfo = FtdiDeviceInfo.ParseFromDeviceId("FTDIBUS\\VID_0403+PID_6001+SINGLE\\0000"),
            Status = PortStatus.Available
        };

        // Act
        var deviceGroup = DeviceGroup.CreateSinglePortGroup(port);

        // Assert
        Assert.Equal("SINGLE_COM7", deviceGroup.DeviceId);
        Assert.Equal("SINGLE", deviceGroup.SerialNumber);
        Assert.Single(deviceGroup.Ports);
        Assert.Equal("COM7", deviceGroup.Ports.First().PortName);
        Assert.False(deviceGroup.IsMultiPortDevice);
        Assert.True(deviceGroup.IsFtdiDevice);
        Assert.Equal(1, deviceGroup.AvailablePortCount);
        Assert.Equal(0, deviceGroup.AllocatedPortCount);
    }

    [Fact]
    public void DeviceGroup_CreateMultiPortGroup_Works()
    {
        // Arrange
        var ports = CreateFt4232HPorts();

        // Act
        var deviceGroup = DeviceGroup.CreateMultiPortGroup("TEST_DEVICE", ports.ToList());

        // Assert
        Assert.Equal("TEST_DEVICE", deviceGroup.DeviceId);
        Assert.Equal(4, deviceGroup.PortCount);
        Assert.True(deviceGroup.IsMultiPortDevice);
        Assert.True(deviceGroup.IsFtdiDevice);
        Assert.Equal(4, deviceGroup.AvailablePortCount);
        Assert.Equal(0, deviceGroup.AllocatedPortCount);
        Assert.Equal(0, deviceGroup.UtilizationPercentage);
    }

    [Fact]
    public void DeviceGroup_UpdatePortStatus_Works()
    {
        // Arrange
        var ports = CreateFt4232HPorts();
        var deviceGroup = DeviceGroup.CreateMultiPortGroup("TEST", ports.ToList());

        // Act
        var updated = deviceGroup.UpdatePortStatus("COM3", PortStatus.Allocated);

        // Assert
        Assert.True(updated);
        Assert.Equal(3, deviceGroup.AvailablePortCount);
        Assert.Equal(1, deviceGroup.AllocatedPortCount);
        Assert.Equal(25, deviceGroup.UtilizationPercentage); // 1/4 = 25%
        Assert.False(deviceGroup.IsFullyAllocated);
        Assert.True(deviceGroup.HasAvailablePorts);
    }

    [Fact]
    public void DeviceGroup_GetSummary_FormatsCorrectly()
    {
        // Arrange
        var ports = CreateFt4232HPorts();
        var deviceGroup = DeviceGroup.CreateMultiPortGroup("FT4232H_TEST", ports.ToList());
        deviceGroup.UpdatePortStatus("COM3", PortStatus.Allocated);

        // Act
        var summary = deviceGroup.GetSummary();

        // Assert
        Assert.Contains("FT4232H", summary);
        Assert.Contains("COM3-COM6", summary); // Port range
        Assert.Contains("1/4", summary); // Allocation ratio
        Assert.Contains("25%", summary); // Utilization
        Assert.Contains("✅", summary); // Client valid (4232H)
    }

    #endregion

    #region GetGroupingStatistics Tests

    [Fact]
    public void GetGroupingStatistics_ReportsCorrectly()
    {
        // Arrange
        var deviceGroups = new[]
        {
            // Multi-port FTDI device (4 ports)
            DeviceGroup.CreateMultiPortGroup("MULTI", CreateFt4232HPorts().ToList()),
            
            // Single-port FTDI device
            DeviceGroup.CreateSinglePortGroup(new SerialPortInfo
            {
                PortName = "COM10",
                IsFtdiDevice = true,
                FtdiInfo = FtdiDeviceInfo.ParseFromDeviceId("FTDIBUS\\VID_0403+PID_6001+SINGLE\\0000")
            }),
            
            // Non-FTDI device
            DeviceGroup.CreateSinglePortGroup(new SerialPortInfo
            {
                PortName = "COM20",
                IsFtdiDevice = false
            })
        };

        // Act
        var stats = _analyzer.GetGroupingStatistics(deviceGroups);

        // Assert
        Assert.Equal(3, stats.TotalDevices);
        Assert.Equal(6, stats.TotalPorts); // 4 + 1 + 1
        Assert.Equal(1, stats.MultiPortDevices);
        Assert.Equal(2, stats.SinglePortDevices);
        Assert.Equal(2, stats.AveragePortsPerDevice); // 6 ports / 3 devices
        Assert.Equal(4, stats.LargestDevicePortCount);
        Assert.Equal(2, stats.FtdiDevices);
        Assert.Equal(1, stats.NonFtdiDevices);
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Create test data simulating 4 ports from same FT4232H device
    /// </summary>
    private List<SerialPortInfo> CreateFt4232HPorts()
    {
        var serialNumber = "FT4232H_SERIAL";
        var ports = new List<SerialPortInfo>();

        for (int i = 3; i <= 6; i++) // COM3, COM4, COM5, COM6
        {
            ports.Add(new SerialPortInfo
            {
                PortName = $"COM{i}",
                FriendlyName = $"USB Serial Port (COM{i})",
                IsFtdiDevice = true,
                Status = PortStatus.Available,
                FtdiInfo = FtdiDeviceInfo.ParseFromDeviceId($"FTDIBUS\\VID_0403+PID_6011+{serialNumber}\\000{i-3}"), // Different instance IDs
                DeviceId = $"FTDIBUS\\VID_0403+PID_6011+{serialNumber}\\000{i-3}"
            });
        }

        return ports;
    }

    #endregion
}