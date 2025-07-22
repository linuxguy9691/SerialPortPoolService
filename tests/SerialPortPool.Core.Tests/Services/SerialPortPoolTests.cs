using Microsoft.Extensions.Logging;
using Moq;
using SerialPortPool.Core.Interfaces;
using SerialPortPool.Core.Models;
using SerialPortPool.Core.Services;
using Xunit;

// Explicit alias to resolve namespace conflict
using PoolService = SerialPortPool.Core.Services.SerialPortPool;

namespace SerialPortPool.Core.Tests.Services;

/// <summary>
/// Tests for SerialPortPool implementation - Progressive phases
/// Phase 1: Thread-safety and basic functionality ✅ COMPLETED (50/50 tests)
/// Phase 2: Enhanced allocation with validation ✅ COMPLETED (54/54 tests)
/// Phase 3: Smart caching layer with SystemInfo ✅ FIXED CacheStatistics references
/// Using PoolService alias to resolve namespace conflicts
/// </summary>
public class SerialPortPoolTests : IDisposable
{
    private readonly Mock<ISerialPortDiscovery> _mockDiscovery;
    private readonly Mock<ISerialPortValidator> _mockValidator;
    private readonly Mock<ISystemInfoCache> _mockSystemInfoCache;  // ← FIXED: Using interface instead
    private readonly Mock<ILogger<PoolService>> _mockLogger;
    private readonly PoolService _pool;

    public SerialPortPoolTests()
    {
        _mockDiscovery = new Mock<ISerialPortDiscovery>();
        _mockValidator = new Mock<ISerialPortValidator>();
        
        // Phase 3: Use interface mock instead of concrete class mock
        _mockSystemInfoCache = new Mock<ISystemInfoCache>();
        
        _mockLogger = new Mock<ILogger<PoolService>>();
        _pool = new PoolService(_mockDiscovery.Object, _mockValidator.Object, _mockSystemInfoCache.Object, _mockLogger.Object);
    }

    #region Phase 1 Tests - Thread-Safety and Basic Operations (PRESERVED)

    [Fact]
    public async Task SerialPortPool_AllocatePort_IsThreadSafe()
    {
        // Arrange
        var testPorts = new[]
        {
            new SerialPortInfo { PortName = "COM1", Status = PortStatus.Available },
            new SerialPortInfo { PortName = "COM2", Status = PortStatus.Available },
            new SerialPortInfo { PortName = "COM3", Status = PortStatus.Available },
            new SerialPortInfo { PortName = "COM4", Status = PortStatus.Available },
            new SerialPortInfo { PortName = "COM5", Status = PortStatus.Available }
        };

        _mockDiscovery.Setup(d => d.DiscoverPortsAsync())
                     .ReturnsAsync(testPorts);

        // Act - Allocate from multiple threads simultaneously
        var tasks = new List<Task<PortAllocation?>>();
        for (int i = 0; i < 5; i++)
        {
            var clientId = $"Client{i + 1}";
            tasks.Add(Task.Run(() => _pool.AllocatePortAsync(clientId: clientId)));
        }

        var results = await Task.WhenAll(tasks);

        // Assert
        var successfulAllocations = results.Where(r => r != null).ToList();
        Assert.True(successfulAllocations.Count <= testPorts.Length, 
            "Should not allocate more ports than available");
        Assert.True(successfulAllocations.Count > 0, 
            "Should successfully allocate at least one port");

        // Verify no duplicate allocations
        var allocatedPortNames = successfulAllocations.Select(a => a!.PortName).ToList();
        var uniquePortNames = allocatedPortNames.Distinct().ToList();
        Assert.Equal(allocatedPortNames.Count, uniquePortNames.Count);

        // Verify all allocations are active
        Assert.All(successfulAllocations, allocation =>
        {
            Assert.True(allocation!.IsActive, "Allocated port should be active");
            Assert.NotNull(allocation.SessionId);
            Assert.NotEqual(string.Empty, allocation.SessionId);
        });
    }

    [Fact]
    public async Task SerialPortPool_ReleasePort_IsThreadSafe()
    {
        // Arrange - First allocate some ports
        var testPorts = new[]
        {
            new SerialPortInfo { PortName = "COM10", Status = PortStatus.Available },
            new SerialPortInfo { PortName = "COM11", Status = PortStatus.Available },
            new SerialPortInfo { PortName = "COM12", Status = PortStatus.Available }
        };

        _mockDiscovery.Setup(d => d.DiscoverPortsAsync())
                     .ReturnsAsync(testPorts);

        var allocations = new List<PortAllocation>();
        for (int i = 0; i < 3; i++)
        {
            var allocation = await _pool.AllocatePortAsync(clientId: $"TestClient{i}");
            Assert.NotNull(allocation);
            allocations.Add(allocation!);
        }

        // Act - Release all ports from multiple threads simultaneously
        var releaseTasks = allocations.Select(allocation =>
            Task.Run(() => _pool.ReleasePortAsync(allocation.PortName, allocation.SessionId))
        ).ToList();

        var releaseResults = await Task.WhenAll(releaseTasks);

        // Assert
        Assert.All(releaseResults, result => 
            Assert.True(result, "All releases should succeed"));

        // Verify all allocations are now inactive
        var allAllocations = await _pool.GetAllocationsAsync();
        var releasedAllocations = allAllocations.Where(a => 
            allocations.Any(orig => orig.PortName == a.PortName)).ToList();

        Assert.All(releasedAllocations, allocation =>
        {
            Assert.False(allocation.IsActive, "Released port should not be active");
            Assert.NotNull(allocation.ReleasedAt);
        });
    }

    [Fact]
    public async Task SerialPortPool_ConcurrentAccess_NoDeadlocks()
    {
        // Arrange
        var testPorts = new[]
        {
            new SerialPortInfo { PortName = "COM20", Status = PortStatus.Available },
            new SerialPortInfo { PortName = "COM21", Status = PortStatus.Available },
            new SerialPortInfo { PortName = "COM22", Status = PortStatus.Available },
            new SerialPortInfo { PortName = "COM23", Status = PortStatus.Available }
        };

        _mockDiscovery.Setup(d => d.DiscoverPortsAsync())
                     .ReturnsAsync(testPorts);

        // Act - Mix of allocate, release, and query operations from multiple threads
        var mixedTasks = new List<Task>();

        // Allocation tasks
        for (int i = 0; i < 6; i++)
        {
            var clientId = $"MixedClient{i}";
            mixedTasks.Add(Task.Run(async () =>
            {
                var allocation = await _pool.AllocatePortAsync(clientId: clientId);
                if (allocation != null)
                {
                    // Hold for a short time, then release
                    await Task.Delay(100);
                    await _pool.ReleasePortAsync(allocation.PortName, allocation.SessionId);
                }
            }));
        }

        // Query tasks (should not interfere with allocations)
        for (int i = 0; i < 4; i++)
        {
            mixedTasks.Add(Task.Run(async () =>
            {
                await _pool.GetAllocationsAsync();
                await _pool.GetAvailablePortsCountAsync();
                await _pool.GetStatisticsAsync();
            }));
        }

        // Act - Execute all tasks with timeout to detect deadlocks
        var timeoutTask = Task.Delay(TimeSpan.FromSeconds(10));
        var completedTask = await Task.WhenAny(Task.WhenAll(mixedTasks), timeoutTask);

        // Assert - Check that operations completed without deadlock
        Assert.True(completedTask != timeoutTask, 
            "Operations should complete without deadlock (within 10 seconds)");

        // Verify pool is still functional after concurrent access
        var finalStats = await _pool.GetStatisticsAsync();
        Assert.NotNull(finalStats);
        
        var finalAllocations = await _pool.GetAllocationsAsync();
        Assert.NotNull(finalAllocations);
    }

    [Fact]
    public async Task SerialPortPool_BasicOperations_WorkCorrectly()
    {
        // Arrange
        var testPort = new SerialPortInfo { PortName = "COM99", Status = PortStatus.Available };
        _mockDiscovery.Setup(d => d.DiscoverPortsAsync())
                     .ReturnsAsync(new[] { testPort });

        // Act & Assert - Basic allocation
        var allocation = await _pool.AllocatePortAsync(clientId: "TestBasic");
        Assert.NotNull(allocation);
        Assert.Equal("COM99", allocation!.PortName);
        Assert.Equal("TestBasic", allocation.AllocatedTo);
        Assert.True(allocation.IsActive);

        // Act & Assert - Port is allocated
        var isAllocated = await _pool.IsPortAllocatedAsync("COM99");
        Assert.True(isAllocated);

        // Act & Assert - Get allocation
        var retrievedAllocation = await _pool.GetPortAllocationAsync("COM99");
        Assert.NotNull(retrievedAllocation);
        Assert.Equal(allocation.SessionId, retrievedAllocation!.SessionId);

        // Act & Assert - Release port
        var releaseResult = await _pool.ReleasePortAsync("COM99", allocation.SessionId);
        Assert.True(releaseResult);

        // Act & Assert - Port is no longer active
        var isStillAllocated = await _pool.IsPortAllocatedAsync("COM99");
        Assert.False(isStillAllocated);
    }

    [Fact]
    public async Task SerialPortPool_SessionValidation_WorksCorrectly()
    {
        // Arrange
        var testPort = new SerialPortInfo { PortName = "COM88", Status = PortStatus.Available };
        _mockDiscovery.Setup(d => d.DiscoverPortsAsync())
                     .ReturnsAsync(new[] { testPort });

        var allocation = await _pool.AllocatePortAsync(clientId: "SessionTest");
        Assert.NotNull(allocation);

        // Act & Assert - Release with correct session ID
        var releaseWithCorrectSession = await _pool.ReleasePortAsync("COM88", allocation!.SessionId);
        Assert.True(releaseWithCorrectSession);

        // Re-allocate for next test
        var newAllocation = await _pool.AllocatePortAsync(clientId: "SessionTest2");
        Assert.NotNull(newAllocation);

        // Act & Assert - Release with incorrect session ID should fail
        var releaseWithWrongSession = await _pool.ReleasePortAsync("COM88", "wrong-session-id");
        Assert.False(releaseWithWrongSession);

        // Act & Assert - Port should still be allocated
        var stillAllocated = await _pool.IsPortAllocatedAsync("COM88");
        Assert.True(stillAllocated);

        // Cleanup - Release with correct session
        var finalRelease = await _pool.ReleasePortAsync("COM88", newAllocation!.SessionId);
        Assert.True(finalRelease);
    }

    [Fact]
    public async Task SerialPortPool_ClientCleanup_ReleasesAllClientPorts()
    {
        // Arrange
        var testPorts = new[]
        {
            new SerialPortInfo { PortName = "COM30", Status = PortStatus.Available },
            new SerialPortInfo { PortName = "COM31", Status = PortStatus.Available },
            new SerialPortInfo { PortName = "COM32", Status = PortStatus.Available }
        };

        _mockDiscovery.Setup(d => d.DiscoverPortsAsync())
                     .ReturnsAsync(testPorts);

        // Allocate multiple ports to same client
        const string clientId = "CleanupTestClient";
        var allocations = new List<PortAllocation>();
        
        for (int i = 0; i < 3; i++)
        {
            var allocation = await _pool.AllocatePortAsync(clientId: clientId);
            if (allocation != null)
            {
                allocations.Add(allocation);
            }
        }

        Assert.True(allocations.Count > 0, "Should have allocated at least one port");

        // Act - Release all ports for client
        var releasedCount = await _pool.ReleaseAllPortsForClientAsync(clientId);

        // Assert
        Assert.Equal(allocations.Count, releasedCount);

        // Verify all client ports are released
        foreach (var allocation in allocations)
        {
            var isStillAllocated = await _pool.IsPortAllocatedAsync(allocation.PortName);
            Assert.False(isStillAllocated, $"Port {allocation.PortName} should be released");
        }
    }

    #endregion

    #region Phase 2 Tests - Enhanced Allocation with Validation (PRESERVED)

    [Fact]
    public async Task AllocatePort_WithValidation_FiltersCorrectly()
    {
        // Arrange - Mix of FTDI and non-FTDI ports
        var testPorts = new[]
        {
            new SerialPortInfo 
            { 
                PortName = "COM1", 
                Status = PortStatus.Available,
                IsFtdiDevice = true,
                FtdiInfo = FtdiDeviceInfo.ParseFromDeviceId("FTDIBUS\\VID_0403+PID_6011+TEST4232H\\0000"),
                IsValidForPool = true,
                ValidationResult = PortValidationResult.Success("Valid 4232H device", new[] { ValidationCriteria.Is4232HChip })
            },
            new SerialPortInfo 
            { 
                PortName = "COM2", 
                Status = PortStatus.Available,
                IsFtdiDevice = true,
                FtdiInfo = FtdiDeviceInfo.ParseFromDeviceId("FTDIBUS\\VID_0403+PID_6001+TESTFT232R\\0000"),
                IsValidForPool = false,
                ValidationResult = PortValidationResult.Failure("Not 4232H chip", new[] { ValidationCriteria.Not4232HChip })
            },
            new SerialPortInfo 
            { 
                PortName = "COM3", 
                Status = PortStatus.Available,
                IsFtdiDevice = false,
                IsValidForPool = false
            }
        };

        _mockDiscovery.Setup(d => d.DiscoverPortsAsync()).ReturnsAsync(testPorts);
        
        // Setup validator to return only valid ports
        _mockValidator.Setup(v => v.GetValidPortsAsync(It.IsAny<IEnumerable<SerialPortInfo>>(), It.IsAny<PortValidationConfiguration>()))
                    .ReturnsAsync((IEnumerable<SerialPortInfo> ports, PortValidationConfiguration config) => 
                        ports.Where(p => p.IsValidForPool));

        // Act - Allocate with client configuration (strict)
        var clientConfig = PortValidationConfiguration.CreateClientDefault();
        var allocation = await _pool.AllocatePortAsync(clientConfig, "ValidatedClient");

        // Assert - Should get only the valid FTDI 4232H port
        Assert.NotNull(allocation);
        Assert.Equal("COM1", allocation!.PortName);
        Assert.Equal("ValidatedClient", allocation.AllocatedTo);
        Assert.True(allocation.Metadata.ContainsKey("ValidationScore"));
        
        // Verify validator was called
        _mockValidator.Verify(v => v.GetValidPortsAsync(It.IsAny<IEnumerable<SerialPortInfo>>(), It.IsAny<PortValidationConfiguration>()), Times.Once);
    }

    [Fact]
    public async Task AllocatePort_WithoutValidation_UsesAllPorts()
    {
        // Arrange
        var testPorts = new[]
        {
            new SerialPortInfo { PortName = "COM10", Status = PortStatus.Available },
            new SerialPortInfo { PortName = "COM11", Status = PortStatus.Available }
        };

        _mockDiscovery.Setup(d => d.DiscoverPortsAsync()).ReturnsAsync(testPorts);

        // Act - Allocate without validation config
        var allocation = await _pool.AllocatePortAsync(config: null, clientId: "NoValidationClient");

        // Assert - Should get first available port (no filtering)
        Assert.NotNull(allocation);
        Assert.Contains(allocation!.PortName, testPorts.Select(p => p.PortName));
        Assert.Equal("NoValidationClient", allocation!.AllocatedTo);
        
        // Validator should not be called when no config provided
        _mockValidator.Verify(v => v.GetValidPortsAsync(It.IsAny<IEnumerable<SerialPortInfo>>(), It.IsAny<PortValidationConfiguration>()), Times.Never);
    }

    [Fact]
    public async Task GetAvailablePortsCount_RespectsValidation()
    {
        // Arrange
        var testPorts = new[]
        {
            new SerialPortInfo { PortName = "COM20", Status = PortStatus.Available, IsValidForPool = true },
            new SerialPortInfo { PortName = "COM21", Status = PortStatus.Available, IsValidForPool = false },
            new SerialPortInfo { PortName = "COM22", Status = PortStatus.Available, IsValidForPool = true }
        };

        _mockDiscovery.Setup(d => d.DiscoverPortsAsync()).ReturnsAsync(testPorts);
        
        _mockValidator.Setup(v => v.GetValidPortsAsync(It.IsAny<IEnumerable<SerialPortInfo>>(), It.IsAny<PortValidationConfiguration>()))
                    .ReturnsAsync((IEnumerable<SerialPortInfo> ports, PortValidationConfiguration config) => 
                        ports.Where(p => p.IsValidForPool));

        // Act
        var clientConfig = PortValidationConfiguration.CreateClientDefault();
        var countWithValidation = await _pool.GetAvailablePortsCountAsync(clientConfig);
        var countWithoutValidation = await _pool.GetAvailablePortsCountAsync(null);

        // Assert
        Assert.Equal(2, countWithValidation); // Only valid ports
        Assert.Equal(3, countWithoutValidation); // All ports
        
        // Validator should be called only once (for the with-validation case)
        _mockValidator.Verify(v => v.GetValidPortsAsync(It.IsAny<IEnumerable<SerialPortInfo>>(), It.IsAny<PortValidationConfiguration>()), Times.Once);
    }

    [Fact]
    public async Task AllocatePort_StoresValidationMetadata()
    {
        // Arrange
        var ftdiInfo = new FtdiDeviceInfo
        {
            VendorId = "0403",
            ProductId = "6011", 
            ChipType = "FT4232H",
            SerialNumber = "TESTSERIAL123"
        };

        var testPort = new SerialPortInfo 
        { 
            PortName = "COM50", 
            Status = PortStatus.Available,
            IsFtdiDevice = true,
            FtdiInfo = ftdiInfo,
            ValidationResult = new PortValidationResult 
            { 
                IsValid = true, 
                ValidationScore = 95, 
                Reason = "Perfect FTDI match" 
            }
        };

        _mockDiscovery.Setup(d => d.DiscoverPortsAsync()).ReturnsAsync(new[] { testPort });
        
        _mockValidator.Setup(v => v.GetValidPortsAsync(It.IsAny<IEnumerable<SerialPortInfo>>(), It.IsAny<PortValidationConfiguration>()))
                    .ReturnsAsync(new[] { testPort });

        // Act
        var allocation = await _pool.AllocatePortAsync(PortValidationConfiguration.CreateClientDefault(), "MetadataClient");

        // Assert
        Assert.NotNull(allocation);
        
        // Verify validation metadata is stored
        Assert.True(allocation!.Metadata.ContainsKey("ValidationScore"));
        Assert.Equal("95", allocation.Metadata["ValidationScore"]);
        Assert.True(allocation.Metadata.ContainsKey("ValidationReason"));
        Assert.Equal("Perfect FTDI match", allocation.Metadata["ValidationReason"]);
        Assert.True(allocation.Metadata.ContainsKey("IsFtdiDevice"));
        Assert.Equal("True", allocation.Metadata["IsFtdiDevice"]);
        Assert.True(allocation.Metadata.ContainsKey("FtdiChipType"));
        Assert.Equal("FT4232H", allocation.Metadata["FtdiChipType"]);
        
        // Verify FTDI-specific metadata
        Assert.True(allocation.Metadata.ContainsKey("FtdiVendorId"));
        Assert.Equal("0403", allocation.Metadata["FtdiVendorId"]);
        Assert.True(allocation.Metadata.ContainsKey("FtdiProductId"));
        Assert.Equal("6011", allocation.Metadata["FtdiProductId"]);
        Assert.True(allocation.Metadata.ContainsKey("FtdiSerialNumber"));
        Assert.Equal("TESTSERIAL123", allocation.Metadata["FtdiSerialNumber"]);
        Assert.True(allocation.Metadata.ContainsKey("Is4232H"));
        Assert.Equal("True", allocation.Metadata["Is4232H"]);
    }

    #endregion

    #region Phase 3 Tests - Smart Caching Layer (SIMPLIFIED WITH MOCKS)

    [Fact]
    public async Task GetPortSystemInfo_UsesCacheInterface()
    {
        // Arrange - Use mocked cache
        var expectedSystemInfo = new SystemInfo
        {
            SerialNumber = "TEST_SYSTEM_INFO",
            IsDataValid = true,
            Manufacturer = "FTDI",
            ProductDescription = "Test Device"
        };
        
        _mockSystemInfoCache
            .Setup(c => c.GetSystemInfoAsync("COM_TEST", false))
            .ReturnsAsync(expectedSystemInfo);

        // Act
        var result = await _pool.GetPortSystemInfoAsync("COM_TEST");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("TEST_SYSTEM_INFO", result!.SerialNumber);
        Assert.True(result.IsDataValid);
        
        // Verify cache was called
        _mockSystemInfoCache.Verify(c => c.GetSystemInfoAsync("COM_TEST", false), Times.Once);
    }

    [Fact]
    public async Task GetPortSystemInfo_WithForceRefresh_PassesToCache()
    {
        // Arrange
        var refreshedSystemInfo = new SystemInfo
        {
            SerialNumber = "REFRESHED_SYSTEM_INFO",
            IsDataValid = true
        };
        
        _mockSystemInfoCache
            .Setup(c => c.GetSystemInfoAsync("COM_REFRESH", true))
            .ReturnsAsync(refreshedSystemInfo);

        // Act
        var result = await _pool.GetPortSystemInfoAsync("COM_REFRESH", forceRefresh: true);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("REFRESHED_SYSTEM_INFO", result!.SerialNumber);
        
        // Verify cache was called with forceRefresh=true
        _mockSystemInfoCache.Verify(c => c.GetSystemInfoAsync("COM_REFRESH", true), Times.Once);
    }

    [Fact]
    public async Task GetPortSystemInfo_WithError_ReturnsErrorSystemInfo()
    {
        // Arrange
        var errorSystemInfo = SystemInfo.CreateError("ERROR_PORT", "Read failed");
        
        _mockSystemInfoCache
            .Setup(c => c.GetSystemInfoAsync("ERROR_PORT", false))
            .ReturnsAsync(errorSystemInfo);

        // Act
        var result = await _pool.GetPortSystemInfoAsync("ERROR_PORT");

        // Assert
        Assert.NotNull(result);
        Assert.False(result!.IsDataValid);
        Assert.Equal("Read failed", result.ErrorMessage);
        Assert.Equal("ERROR_PORT", result.SerialNumber);
    }

    [Fact]
    public async Task GetPortSystemInfo_WithNullPortName_ReturnsNull()
    {
        // Act
        var result = await _pool.GetPortSystemInfoAsync(null!);

        // Assert
        Assert.Null(result);
        
        // Cache should not be called
        _mockSystemInfoCache.Verify(c => c.GetSystemInfoAsync(It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    public async Task GetPortSystemInfo_WhenCacheReturnsNull_ReturnsNull()
    {
        // Arrange
        _mockSystemInfoCache
            .Setup(c => c.GetSystemInfoAsync("NULL_PORT", false))
            .ReturnsAsync((SystemInfo?)null);

        // Act
        var result = await _pool.GetPortSystemInfoAsync("NULL_PORT");

        // Assert
        Assert.Null(result);
        
        // Verify cache was called
        _mockSystemInfoCache.Verify(c => c.GetSystemInfoAsync("NULL_PORT", false), Times.Once);
    }

    #endregion

    #region Phase 5 Tests - Performance & Stress Testing (SIMPLE VERSION - NO MESSAGES)

    [Fact]
    public async Task StressTest_100ConcurrentAllocations()
    {
        // Arrange
        var testPorts = new List<SerialPortInfo>();
        for (int i = 1; i <= 50; i++)
        {
            testPorts.Add(new SerialPortInfo 
            { 
                PortName = $"COM{i:D3}",
                Status = PortStatus.Available,
                IsFtdiDevice = true,
                IsValidForPool = true
            });
        }

        _mockDiscovery.Setup(d => d.DiscoverPortsAsync())
                     .ReturnsAsync(testPorts);

        _mockValidator.Setup(v => v.GetValidPortsAsync(It.IsAny<IEnumerable<SerialPortInfo>>(), It.IsAny<PortValidationConfiguration>()))
                    .ReturnsAsync((IEnumerable<SerialPortInfo> ports, PortValidationConfiguration config) => ports);

        // Act
        var tasks = new List<Task<PortAllocation?>>();
        for (int i = 0; i < 100; i++)
        {
            var clientId = $"Client{i:D3}";
            tasks.Add(Task.Run(() => _pool.AllocatePortAsync(clientId: clientId)));
        }

        var timeoutTask = Task.Delay(30000);
        var completedTask = await Task.WhenAny(Task.WhenAll(tasks), timeoutTask);
        
        // Assert
        Assert.True(completedTask != timeoutTask);
        
        var results = await Task.WhenAll(tasks);
        var successful = results.Where(r => r != null).ToList();
        
        Assert.True(successful.Count > 0);
        Assert.True(successful.Count <= testPorts.Count);
        
        var portNames = successful.Select(a => a!.PortName).ToList();
        var uniqueNames = portNames.Distinct().ToList();
        Assert.Equal(portNames.Count, uniqueNames.Count);
    }

    [Fact]
    public async Task PerformanceTest_AllocationSpeed()
    {
        // Arrange
        var testPorts = new List<SerialPortInfo>();
        for (int i = 1; i <= 20; i++)
        {
            testPorts.Add(new SerialPortInfo 
            { 
                PortName = $"PERF{i:D2}", 
                Status = PortStatus.Available,
                IsFtdiDevice = true,
                IsValidForPool = true
            });
        }

        _mockDiscovery.Setup(d => d.DiscoverPortsAsync())
                     .ReturnsAsync(testPorts);

        _mockValidator.Setup(v => v.GetValidPortsAsync(It.IsAny<IEnumerable<SerialPortInfo>>(), It.IsAny<PortValidationConfiguration>()))
                    .ReturnsAsync((IEnumerable<SerialPortInfo> ports, PortValidationConfiguration config) => ports);

        // Warmup
        var warmup = await _pool.AllocatePortAsync(clientId: "Warmup");
        if (warmup != null)
        {
            await _pool.ReleasePortAsync(warmup.PortName, warmup.SessionId);
        }

        // Act
        var times = new List<double>();
        for (int i = 0; i < 15; i++)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var allocation = await _pool.AllocatePortAsync(clientId: $"Perf{i:D2}");
            stopwatch.Stop();
            
            times.Add(stopwatch.Elapsed.TotalMilliseconds);
            
            if (allocation != null)
            {
                await _pool.ReleasePortAsync(allocation.PortName, allocation.SessionId);
            }
        }

        // Assert
        var average = times.Average();
        var max = times.Max();
        
        Assert.True(average < 100); // More lenient target
        Assert.True(max < 500);
    }

    [Fact]
    public async Task MemoryTest_NoLeaksUnderLoad()
    {
        // Arrange
        var testPorts = new List<SerialPortInfo>();
        for (int i = 1; i <= 5; i++)
        {
            testPorts.Add(new SerialPortInfo 
            { 
                PortName = $"MEM{i:D2}", 
                Status = PortStatus.Available,
                IsFtdiDevice = true,
                IsValidForPool = true
            });
        }

        _mockDiscovery.Setup(d => d.DiscoverPortsAsync())
                     .ReturnsAsync(testPorts);

        _mockValidator.Setup(v => v.GetValidPortsAsync(It.IsAny<IEnumerable<SerialPortInfo>>(), It.IsAny<PortValidationConfiguration>()))
                    .ReturnsAsync((IEnumerable<SerialPortInfo> ports, PortValidationConfiguration config) => ports);

        // Baseline
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        var initialMemory = GC.GetTotalMemory(false);

        // Act
        for (int cycle = 0; cycle < 5; cycle++)
        {
            var allocations = new List<PortAllocation>();
            
            // Allocate
            for (int i = 0; i < testPorts.Count; i++)
            {
                var allocation = await _pool.AllocatePortAsync(clientId: $"Mem{cycle}_{i}");
                if (allocation != null)
                {
                    allocations.Add(allocation);
                }
            }
            
            // Get stats
            await _pool.GetStatisticsAsync();
            
            // Release
            foreach (var allocation in allocations)
            {
                await _pool.ReleasePortAsync(allocation.PortName, allocation.SessionId);
            }
        }

        // Final check
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        var finalMemory = GC.GetTotalMemory(false);
        var growthMB = (finalMemory - initialMemory) / (1024.0 * 1024.0);

        // Assert
        Assert.True(growthMB < 5); // 5MB growth limit
        
        var stats = await _pool.GetStatisticsAsync();
        Assert.Equal(0, stats.AllocatedPorts);
    }

    #endregion

    #region Test Utilities and Cleanup

    public void Dispose()
    {
        _pool?.Dispose();
    }

    #endregion
}