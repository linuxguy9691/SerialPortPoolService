// tests/SerialPortPool.Core.Tests/Services/EnhancedSerialPortDiscoveryServiceDeviceGroupingIntegrationTests.cs - ÉTAPE 5 Phase 2
using Microsoft.Extensions.Logging;
using Moq;
using SerialPortPool.Core.Interfaces;
using SerialPortPool.Core.Models;
using SerialPortPool.Core.Services;
using Xunit;

namespace SerialPortPool.Core.Tests.Services;

/// <summary>
/// Integration tests for Enhanced Discovery Service with Device Grouping (ÉTAPE 5 Phase 2)
/// Tests the complete integration of device grouping functionality within the discovery service
/// </summary>
public class EnhancedSerialPortDiscoveryServiceDeviceGroupingIntegrationTests
{
    private readonly Mock<ILogger<EnhancedSerialPortDiscoveryService>> _mockLogger;
    private readonly Mock<IFtdiDeviceReader> _mockFtdiReader;
    private readonly Mock<ISerialPortValidator> _mockValidator;
    private readonly EnhancedSerialPortDiscoveryService _discoveryService;

    public EnhancedSerialPortDiscoveryServiceDeviceGroupingIntegrationTests()
    {
        _mockLogger = new Mock<ILogger<EnhancedSerialPortDiscoveryService>>();
        _mockFtdiReader = new Mock<IFtdiDeviceReader>();
        _mockValidator = new Mock<ISerialPortValidator>();
        
        _discoveryService = new EnhancedSerialPortDiscoveryService(
            _mockLogger.Object,
            _mockFtdiReader.Object,
            _mockValidator.Object,
            PortValidationConfiguration.CreateDevelopmentDefault()
        );
    }

    #region Integration Tests - Discovery + Device Grouping

    [Fact]
    public async Task DiscoverDeviceGroups_IntegratesWithExistingDiscovery_WorksCorrectly()
    {
        // Arrange - Setup complete mock chain for integrated discovery
        var testPorts = CreateRealisticTestScenario();
        SetupCompleteMockChain(testPorts);

        // Act - Use the integrated discovery + grouping
        var deviceGroups = await _discoveryService.DiscoverDeviceGroupsAsync();
        var groupList = deviceGroups.ToList();

        // Assert - Verify complete integration
        Assert.True(groupList.Count > 0, "Should discover device groups");
        
        // Should have both FT4232H (multi-port) and FT232R (single-port) groups
        var multiPortGroup = groupList.FirstOrDefault(g => g.IsMultiPortDevice);
        var singlePortGroup = groupList.FirstOrDefault(g => !g.IsMultiPortDevice);
        
        Assert.NotNull(multiPortGroup);
        Assert.NotNull(singlePortGroup);
        
        // Verify FT4232H group
        Assert.True(multiPortGroup.PortCount > 1);
        Assert.True(multiPortGroup.IsClientValidDevice);
        Assert.Contains("FT4232H", multiPortGroup.DeviceInfo?.ChipType ?? "");
        
        // Verify FT232R group  
        Assert.Equal(1, singlePortGroup.PortCount);
        Assert.False(singlePortGroup.IsClientValidDevice);
        Assert.Contains("FT232R", singlePortGroup.DeviceInfo?.ChipType ?? "");

        // Verify all original discovery functionality still works
        Assert.All(groupList.SelectMany(g => g.Ports), port =>
        {
            Assert.NotNull(port.FriendlyName);
            Assert.NotEqual(PortStatus.Unknown, port.Status);
            Assert.NotNull(port.ValidationResult);
        });
    }

    [Fact]
    public async Task DiscoverDeviceGroups_WithValidationConfig_FiltersProperly()
    {
        // Arrange
        var mixedValidityPorts = CreateMixedValidityTestPorts();
        SetupCompleteMockChain(mixedValidityPorts);
        
        // Setup validator to filter based on config
        var clientConfig = PortValidationConfiguration.CreateClientDefault();
        var validPorts = mixedValidityPorts.Where(p => p.IsFtdi4232H).ToList(); // Only 4232H valid for client
        
        _mockValidator.Setup(v => v.GetValidPortsAsync(It.IsAny<IEnumerable<SerialPortInfo>>(), clientConfig))
                     .ReturnsAsync(validPorts);

        // Act
        var deviceGroups = await _discoveryService.DiscoverDeviceGroupsAsync(clientConfig);
        var groupList = deviceGroups.ToList();

        // Assert - Should only contain client-valid devices
        Assert.All(groupList, group => 
            Assert.True(group.IsClientValidDevice, "All returned groups should be client-valid when client config used"));

        // Verify validator was called with correct configuration
        _mockValidator.Verify(v => v.GetValidPortsAsync(It.IsAny<IEnumerable<SerialPortInfo>>(), clientConfig), Times.Once);
    }

    [Fact]
    public async Task FindDeviceGroupByPort_IntegratesWithDiscovery_FindsCorrectGroup()
    {
        // Arrange
        var testPorts = CreateRealisticTestScenario();
        SetupCompleteMockChain(testPorts);

        // Act - Find group containing a specific port from FT4232H device
        var deviceGroup = await _discoveryService.FindDeviceGroupByPortAsync("COM4");

        // Assert
        Assert.NotNull(deviceGroup);
        Assert.True(deviceGroup!.PortCount > 1, "Should find the multi-port group");
        Assert.Contains(deviceGroup.Ports, p => p.PortName == "COM4");
        Assert.True(deviceGroup.IsMultiPortDevice);

        // Verify it's the FT4232H group
        Assert.Equal("FT4232H", deviceGroup.DeviceInfo?.ChipType);
    }

    [Fact]
    public async Task DiscoverMultiPortDevices_FilterCorrectly_ReturnsOnlyMultiPort()
    {
        // Arrange
        var testPorts = CreateRealisticTestScenario(); // Mix of single and multi-port
        SetupCompleteMockChain(testPorts);

        // Act
        var multiPortDevices = await _discoveryService.DiscoverMultiPortDevicesAsync();
        var deviceList = multiPortDevices.ToList();

        // Assert - Should only return multi-port devices
        Assert.All(deviceList, device => 
            Assert.True(device.IsMultiPortDevice, "Should only return multi-port devices"));
        
        // Should contain the FT4232H device
        var ft4232hDevice = deviceList.FirstOrDefault(d => d.DeviceInfo?.ChipType == "FT4232H");
        Assert.NotNull(ft4232hDevice);
        Assert.Equal(4, ft4232hDevice!.PortCount);
    }

    [Fact]
    public async Task GetDeviceGroupingStatistics_ProvideAccurateStats()
    {
        // Arrange
        var testPorts = CreateRealisticTestScenario();
        SetupCompleteMockChain(testPorts);

        // Act
        var stats = await _discoveryService.GetDeviceGroupingStatisticsAsync();

        // Assert
        Assert.True(stats.TotalDevices >= 2, "Should have at least 2 devices (FT4232H + FT232R)");
        Assert.True(stats.TotalPorts >= 5, "Should have at least 5 ports total");
        Assert.True(stats.MultiPortDevices >= 1, "Should have at least 1 multi-port device");
        Assert.True(stats.FtdiDevices >= 2, "Should have at least 2 FTDI devices");
        Assert.Equal(stats.MultiPortDevices + stats.SinglePortDevices, stats.TotalDevices);
        Assert.True(stats.AveragePortsPerDevice > 1.0, "Average should be > 1 due to multi-port device");
    }

    #endregion

    #region Error Handling & Edge Cases

    [Fact]
    public async Task DiscoverDeviceGroups_WithDiscoveryError_HandlesGracefully()
    {
        // Arrange - Setup mocks to simulate discovery error
        _mockFtdiReader.Setup(f => f.ReadDeviceInfoFromIdAsync(It.IsAny<string>()))
                      .ThrowsAsync(new Exception("WMI Error"));

        // Act
        var deviceGroups = await _discoveryService.DiscoverDeviceGroupsAsync();

        // Assert - Should handle error gracefully
        Assert.NotNull(deviceGroups);
        // May be empty or contain partial results depending on error handling
    }

    [Fact]
    public async Task FindDeviceGroupByPort_WithInvalidPortName_ReturnsNull()
    {
        // Arrange
        SetupCompleteMockChain(Enumerable.Empty<SerialPortInfo>());

        // Act
        var result1 = await _discoveryService.FindDeviceGroupByPortAsync("INVALID_PORT");
        var result2 = await _discoveryService.FindDeviceGroupByPortAsync(null!);
        var result3 = await _discoveryService.FindDeviceGroupByPortAsync("");

        // Assert
        Assert.Null(result1);
        Assert.Null(result2);
        Assert.Null(result3);
    }

    [Fact]
    public async Task GetDeviceGroupAnalysis_WithValidDeviceId_ReturnsDetailedAnalysis()
    {
        // Arrange
        var testPorts = CreateRealisticTestScenario();
        SetupCompleteMockChain(testPorts);

        // First discover to get device IDs
        var allGroups = await _discoveryService.DiscoverDeviceGroupsAsync();
        var firstGroup = allGroups.First();

        // Act
        var analysis = await _discoveryService.GetDeviceGroupAnalysisAsync(firstGroup.DeviceId);

        // Assert
        Assert.NotNull(analysis);
        Assert.Equal(firstGroup.DeviceId, analysis!.DeviceId);
        Assert.Equal(firstGroup.PortCount, analysis.PortCount);
    }

    #endregion

    #region Helper Methods

    private List<SerialPortInfo> CreateRealisticTestScenario()
    {
        var ports = new List<SerialPortInfo>();

        // FT4232H device (4 ports) - client valid
        var ft4232hSerial = "FT4232H_CLIENT_DEVICE";
        for (int i = 3; i <= 6; i++)
        {
            var ftdiInfo = FtdiDeviceInfo.ParseFromDeviceId($"FTDIBUS\\VID_0403+PID_6011+{ft4232hSerial}\\000{i-3}");
            ports.Add(new SerialPortInfo
            {
                PortName = $"COM{i}",
                FriendlyName = $"USB Serial Port (COM{i})",
                Status = PortStatus.Available,
                IsFtdiDevice = true,
                FtdiInfo = ftdiInfo,
                DeviceId = $"FTDIBUS\\VID_0403+PID_6011+{ft4232hSerial}\\000{i-3}",
                IsValidForPool = true,
                ValidationResult = PortValidationResult.Success("Valid 4232H device", 
                    new[] { ValidationCriteria.FtdiDeviceDetected, ValidationCriteria.Is4232HChip })
            });
        }

        // FT232R device (1 port) - dev valid, not client valid
        var ft232rSerial = "FT232R_DEV_DEVICE";
        var ft232rInfo = FtdiDeviceInfo.ParseFromDeviceId($"FTDIBUS\\VID_0403+PID_6001+{ft232rSerial}\\0000");
        ports.Add(new SerialPortInfo
        {
            PortName = "COM7",
            FriendlyName = "USB Serial Port (COM7)",
            Status = PortStatus.Available,
            IsFtdiDevice = true,
            FtdiInfo = ft232rInfo,
            DeviceId = $"FTDIBUS\\VID_0403+PID_6001+{ft232rSerial}\\0000",
            IsValidForPool = true, // Valid for development
            ValidationResult = PortValidationResult.Failure("Not 4232H chip", 
                new[] { ValidationCriteria.Not4232HChip },
                new[] { ValidationCriteria.FtdiDeviceDetected, ValidationCriteria.GenuineFtdiDevice })
        });

        return ports;
    }

    private List<SerialPortInfo> CreateMixedValidityTestPorts()
    {
        var ports = CreateRealisticTestScenario();
        
        // Add a non-FTDI device (invalid)
        ports.Add(new SerialPortInfo
        {
            PortName = "COM10",
            FriendlyName = "Non-FTDI Serial Port",
            Status = PortStatus.Available,
            IsFtdiDevice = false,
            IsValidForPool = false,
            ValidationResult = PortValidationResult.Failure("Not FTDI device", 
                new[] { ValidationCriteria.NotFtdiDevice })
        });

        return ports;
    }

    private void SetupCompleteMockChain(IEnumerable<SerialPortInfo> testPorts)
    {
        var portList = testPorts.ToList();

        // Mock FTDI device reading
        _mockFtdiReader.Setup(f => f.ReadDeviceInfoFromIdAsync(It.IsAny<string>()))
                      .ReturnsAsync((string deviceId) => FtdiDeviceInfo.ParseFromDeviceId(deviceId));

        _mockFtdiReader.Setup(f => f.ReadDeviceInfoAsync(It.IsAny<string>()))
                      .ReturnsAsync((string portName) =>
                      {
                          var port = portList.FirstOrDefault(p => p.PortName == portName);
                          return port?.FtdiInfo;
                      });

        // Mock EEPROM reading
        _mockFtdiReader.Setup(f => f.ReadEepromDataAsync(It.IsAny<string>()))
                      .ReturnsAsync((string portName) =>
                      {
                          var port = portList.FirstOrDefault(p => p.PortName == portName);
                          if (port?.FtdiInfo != null)
                          {
                              return new Dictionary<string, string>
                              {
                                  ["SerialNumber"] = port.FtdiInfo.SerialNumber,
                                  ["Manufacturer"] = port.FtdiInfo.Manufacturer,
                                  ["ProductDescription"] = port.FtdiInfo.ProductDescription,
                                  ["VendorId"] = port.FtdiInfo.VendorId,
                                  ["ProductId"] = port.FtdiInfo.ProductId,
                                  ["ChipType"] = port.FtdiInfo.ChipType
                              };
                          }
                          return new Dictionary<string, string>();
                      });

        // Mock validation
        _mockValidator.Setup(v => v.ValidatePortAsync(It.IsAny<SerialPortInfo>(), It.IsAny<PortValidationConfiguration>()))
                     .ReturnsAsync((SerialPortInfo port, PortValidationConfiguration config) =>
                         port.ValidationResult ?? PortValidationResult.Success("Mock validation", Array.Empty<ValidationCriteria>()));

        _mockValidator.Setup(v => v.GetValidPortsAsync(It.IsAny<IEnumerable<SerialPortInfo>>(), It.IsAny<PortValidationConfiguration>()))
                     .ReturnsAsync((IEnumerable<SerialPortInfo> ports, PortValidationConfiguration config) =>
                     {
                         if (config != null && config.Require4232HChip)
                         {
                             return ports.Where(p => p.IsFtdi4232H); // Client config - only 4232H
                         }
                         return ports.Where(p => p.IsValidForPool); // Dev config - all valid
                     });
    }

    #endregion
}