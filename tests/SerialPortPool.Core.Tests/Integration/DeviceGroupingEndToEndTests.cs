// tests/SerialPortPool.Core.Tests/Integration/DeviceGroupingEndToEndTests.cs - FIXED USING STATEMENTS
using Microsoft.Extensions.DependencyInjection;  // ← ADDED: Missing using for ServiceProvider
using Microsoft.Extensions.Logging;
using SerialPortPool.Core.Interfaces;
using SerialPortPool.Core.Models;
using SerialPortPool.Core.Services;
using Xunit;

namespace SerialPortPool.Core.Tests.Integration;

/// <summary>
/// End-to-end integration tests for Device Grouping functionality (ÉTAPE 5 Phase 2 Final)
/// Tests complete workflow: Discovery → Grouping → Statistics → Lookup
/// </summary>
public class DeviceGroupingEndToEndTests
{
    #region Complete Integration Scenarios

    [Fact]
    public async Task EndToEnd_DeviceGrouping_CompleteWorkflow()
    {
        // Arrange - Setup realistic service chain
        var serviceProvider = CreateTestServiceProvider();
        var discovery = serviceProvider.GetRequiredService<ISerialPortDiscovery>() as EnhancedSerialPortDiscoveryService;
        
        Assert.NotNull(discovery);

        // Act - Execute complete device grouping workflow
        
        // Step 1: Traditional port discovery
        var individualPorts = await discovery!.DiscoverPortsAsync();
        var portList = individualPorts.ToList();
        
        // Step 2: Device grouping discovery  
        var deviceGroups = await discovery.DiscoverDeviceGroupsAsync();
        var groupList = deviceGroups.ToList();
        
        // Step 3: Multi-port device filtering
        var multiPortDevices = await discovery.DiscoverMultiPortDevicesAsync();
        var multiPortList = multiPortDevices.ToList();
        
        // Step 4: Device grouping statistics
        var stats = await discovery.GetDeviceGroupingStatisticsAsync();
        
        // Step 5: Port-to-device lookup
        DeviceGroup? foundGroup = null;
        if (portList.Any())
        {
            var firstPort = portList.First();
            foundGroup = await discovery.FindDeviceGroupByPortAsync(firstPort.PortName);
        }

        // Assert - Verify complete workflow consistency
        
        // Consistency checks
        var totalPortsInGroups = groupList.Sum(g => g.PortCount);
        Assert.Equal(portList.Count, totalPortsInGroups);
        
        // Statistics consistency
        Assert.Equal(groupList.Count, stats.TotalDevices);
        Assert.Equal(portList.Count, stats.TotalPorts);
        Assert.Equal(multiPortList.Count, stats.MultiPortDevices);
        
        // Multi-port filtering consistency
        Assert.All(multiPortList, device => 
            Assert.True(device.IsMultiPortDevice));
        
        // Lookup consistency
        if (foundGroup != null && portList.Any())
        {
            var firstPort = portList.First();
            Assert.Contains(foundGroup.Ports, p => p.PortName == firstPort.PortName);
        }
        
        // Verify no data loss in transformations
        var allGroupPorts = groupList.SelectMany(g => g.Ports).ToList();
        Assert.Equal(portList.Count, allGroupPorts.Count);
        
        // Verify original port properties preserved
        foreach (var originalPort in portList)
        {
            var groupPort = allGroupPorts.FirstOrDefault(p => p.PortName == originalPort.PortName);
            Assert.NotNull(groupPort);
            Assert.Equal(originalPort.IsFtdiDevice, groupPort!.IsFtdiDevice);
            Assert.Equal(originalPort.Status, groupPort.Status);
        }
    }

    [Fact]
    public async Task EndToEnd_MultiPortDevice_SharedSystemInfo()
    {
        // Arrange
        var serviceProvider = CreateTestServiceProvider();
        var discovery = serviceProvider.GetRequiredService<ISerialPortDiscovery>() as EnhancedSerialPortDiscoveryService;
        
        Assert.NotNull(discovery);

        // Act - Focus on multi-port devices with shared system info
        var deviceGroups = await discovery!.DiscoverDeviceGroupsAsync();
        var multiPortDevice = deviceGroups.FirstOrDefault(g => g.IsMultiPortDevice);

        if (multiPortDevice != null)
        {
            // Get detailed analysis
            var deviceAnalysis = await discovery.GetDeviceGroupAnalysisAsync(multiPortDevice.DeviceId);

            // Test shared system info across ports
            SystemInfo? sharedInfo = multiPortDevice.SharedSystemInfo;
            
            // Assert - Verify multi-port device behavior
            Assert.NotNull(deviceAnalysis);
            Assert.Equal(multiPortDevice.DeviceId, deviceAnalysis!.DeviceId);
            Assert.True(multiPortDevice.PortCount > 1);
            
            // Verify all ports share same device characteristics
            Assert.All(multiPortDevice.Ports, port =>
            {
                if (port.IsFtdiDevice && multiPortDevice.DeviceInfo != null)
                {
                    Assert.Equal(multiPortDevice.SerialNumber, port.FtdiInfo?.SerialNumber);
                    Assert.Equal(multiPortDevice.DeviceInfo.ChipType, port.FtdiInfo?.ChipType);
                }
            });
            
            // Verify shared system info if available
            if (sharedInfo != null)
            {
                Assert.True(sharedInfo.IsDataValid);
                Assert.Equal(multiPortDevice.SerialNumber, sharedInfo.SerialNumber);
                Assert.NotEmpty(sharedInfo.EepromData);
            }
        }
    }

    [Fact]
    public async Task EndToEnd_ValidationConfig_AffectsGrouping()
    {
        // Arrange
        var serviceProvider = CreateTestServiceProvider();
        var discovery = serviceProvider.GetRequiredService<ISerialPortDiscovery>() as EnhancedSerialPortDiscoveryService;
        
        Assert.NotNull(discovery);

        // Act - Test both validation configurations
        var devConfig = PortValidationConfiguration.CreateDevelopmentDefault();
        var clientConfig = PortValidationConfiguration.CreateClientDefault();
        
        var devGroups = await discovery!.DiscoverDeviceGroupsAsync(devConfig);
        var clientGroups = await discovery.DiscoverDeviceGroupsAsync(clientConfig);
        
        var devGroupList = devGroups.ToList();
        var clientGroupList = clientGroups.ToList();

        // Assert - Client config should be more restrictive
        Assert.True(clientGroupList.Count <= devGroupList.Count, 
            "Client config should return same or fewer device groups");
        
        // All client groups should be client-valid
        Assert.All(clientGroupList, group =>
            Assert.True(group.IsClientValidDevice || !group.IsFtdiDevice, 
                "Client config should only return client-valid FTDI devices"));
        
        // Dev config may include non-client-valid FTDI devices
        var devFtdiGroups = devGroupList.Where(g => g.IsFtdiDevice).ToList();
        var clientFtdiGroups = clientGroupList.Where(g => g.IsFtdiDevice).ToList();
        
        Assert.True(clientFtdiGroups.Count <= devFtdiGroups.Count,
            "Client should have same or fewer FTDI groups than dev");
    }

    [Fact]
    public async Task EndToEnd_Performance_DeviceGrouping()
    {
        // Arrange
        var serviceProvider = CreateTestServiceProvider();
        var discovery = serviceProvider.GetRequiredService<ISerialPortDiscovery>() as EnhancedSerialPortDiscoveryService;
        
        Assert.NotNull(discovery);

        // Act - Measure device grouping performance
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        var deviceGroups = await discovery!.DiscoverDeviceGroupsAsync();
        var groupList = deviceGroups.ToList();
        
        stopwatch.Stop();
        var groupingTime = stopwatch.ElapsedMilliseconds;

        // Act - Measure statistics generation
        stopwatch.Restart();
        var stats = await discovery.GetDeviceGroupingStatisticsAsync();
        stopwatch.Stop();
        var statisticsTime = stopwatch.ElapsedMilliseconds;

        // Act - Measure lookup performance
        string? testPortName = groupList.SelectMany(g => g.Ports).FirstOrDefault()?.PortName;
        long lookupTime = 0;
        
        if (testPortName != null)
        {
            stopwatch.Restart();
            var foundGroup = await discovery.FindDeviceGroupByPortAsync(testPortName);
            stopwatch.Stop();
            lookupTime = stopwatch.ElapsedMilliseconds;
            
            Assert.NotNull(foundGroup);
        }

        // Assert - Performance targets from ÉTAPE 5
        Assert.True(groupingTime < 100, $"Device grouping should complete < 100ms (actual: {groupingTime}ms)");
        Assert.True(statisticsTime < 50, $"Statistics generation should complete < 50ms (actual: {statisticsTime}ms)");
        Assert.True(lookupTime < 10, $"Port lookup should complete < 10ms (actual: {lookupTime}ms)");
        
        // Memory efficiency check
        var totalPorts = stats.TotalPorts;
        Assert.True(totalPorts == 0 || groupingTime / totalPorts < 10, 
            "Should process ports efficiently (< 10ms per port for grouping)");
    }

    [Fact]
    public async Task EndToEnd_ErrorHandling_GracefulDegradation()
    {
        // Arrange - Service with potential error conditions
        var serviceProvider = CreateTestServiceProvider();
        var discovery = serviceProvider.GetRequiredService<ISerialPortDiscovery>() as EnhancedSerialPortDiscoveryService;
        
        Assert.NotNull(discovery);

        // Act & Assert - Test error scenarios gracefully
        
        // Invalid port lookup
        var invalidGroup = await discovery!.FindDeviceGroupByPortAsync("INVALID_PORT_999");
        Assert.Null(invalidGroup);
        
        // Invalid device analysis
        var invalidAnalysis = await discovery.GetDeviceGroupAnalysisAsync("INVALID_DEVICE_ID");
        Assert.Null(invalidAnalysis);
        
        // Empty/null parameter handling
        var nullPortGroup = await discovery.FindDeviceGroupByPortAsync(null!);
        Assert.Null(nullPortGroup);
        
        var emptyPortGroup = await discovery.FindDeviceGroupByPortAsync("");
        Assert.Null(emptyPortGroup);
        
        // Statistics should work even with no devices
        var stats = await discovery.GetDeviceGroupingStatisticsAsync();
        Assert.NotNull(stats);
        Assert.True(stats.TotalDevices >= 0);
        Assert.True(stats.TotalPorts >= 0);
    }

    [Fact]
    public async Task QualityMetrics_DeviceGrouping_MeetsStandards()
    {
        // Arrange
        var serviceProvider = CreateTestServiceProvider();
        var discovery = serviceProvider.GetRequiredService<ISerialPortDiscovery>() as EnhancedSerialPortDiscoveryService;
        
        Assert.NotNull(discovery);

        // Act
        var deviceGroups = await discovery!.DiscoverDeviceGroupsAsync();
        var groupList = deviceGroups.ToList();
        var stats = await discovery.GetDeviceGroupingStatisticsAsync();

        // Assert - Quality metrics
        
        // Data integrity
        var totalPortsInGroups = groupList.Sum(g => g.PortCount);
        Assert.Equal(stats.TotalPorts, totalPortsInGroups);
        
        // Grouping logic quality
        if (groupList.Any())
        {
            // Each group should have at least one port
            Assert.All(groupList, group => Assert.True(group.PortCount > 0));
            
            // Multi-port groups should have consistent device info
            var multiPortGroups = groupList.Where(g => g.IsMultiPortDevice).ToList();
            foreach (var group in multiPortGroups)
            {
                if (group.IsFtdiDevice)
                {
                    // All ports should share same serial number
                    var serialNumbers = group.Ports
                        .Where(p => p.FtdiInfo != null)
                        .Select(p => p.FtdiInfo!.SerialNumber)
                        .Distinct()
                        .ToList();
                    
                    Assert.True(serialNumbers.Count <= 1, 
                        $"Multi-port group should have consistent serial number (found: {string.Join(", ", serialNumbers)})");
                }
            }
            
            // Single-port groups should be properly classified
            var singlePortGroups = groupList.Where(g => !g.IsMultiPortDevice).ToList();
            Assert.All(singlePortGroups, group => 
                Assert.Equal(1, group.PortCount));
        }
        
        // Statistics accuracy
        Assert.Equal(groupList.Count(g => g.IsMultiPortDevice), stats.MultiPortDevices);
        Assert.Equal(groupList.Count(g => !g.IsMultiPortDevice), stats.SinglePortDevices);
        Assert.Equal(groupList.Count(g => g.IsFtdiDevice), stats.FtdiDevices);
        Assert.Equal(groupList.Count(g => !g.IsFtdiDevice), stats.NonFtdiDevices);
        
        if (stats.TotalDevices > 0)
        {
            var calculatedAverage = (double)stats.TotalPorts / stats.TotalDevices;
            Assert.True(Math.Abs(stats.AveragePortsPerDevice - calculatedAverage) < 0.01, 
                "Average ports per device calculation should be accurate");
        }
    }

    #endregion

    #region Test Infrastructure

    private ServiceProvider CreateTestServiceProvider()
    {
        var services = new ServiceCollection();
        
        // Logging
        services.AddLogging(builder => builder
            .AddConsole()
            .SetMinimumLevel(LogLevel.Information));
        
        // Configuration - Use development for wider device support
        services.AddSingleton(PortValidationConfiguration.CreateDevelopmentDefault());
        
        // Core services
        services.AddScoped<IFtdiDeviceReader, FtdiDeviceReader>();
        services.AddScoped<ISerialPortValidator, SerialPortValidator>();
        services.AddScoped<ISerialPortDiscovery, EnhancedSerialPortDiscoveryService>();
        
        return services.BuildServiceProvider();
    }

    #endregion
}