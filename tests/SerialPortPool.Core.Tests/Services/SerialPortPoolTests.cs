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
/// Phase 1: Thread-safety and basic functionality
/// Using PoolService alias to resolve namespace conflicts
/// </summary>
public class SerialPortPoolTests : IDisposable
{
    private readonly Mock<ISerialPortDiscovery> _mockDiscovery;
    private readonly Mock<ILogger<PoolService>> _mockLogger;
    private readonly PoolService _pool;

    public SerialPortPoolTests()
    {
        _mockDiscovery = new Mock<ISerialPortDiscovery>();
        _mockLogger = new Mock<ILogger<PoolService>>();
        _pool = new PoolService(_mockDiscovery.Object, _mockLogger.Object);
    }

    #region Phase 1 Tests - Thread-Safety and Basic Operations

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
        Assert.Equal(allocatedPortNames.Count, uniquePortNames.Count, 
            "Should not allocate the same port to multiple clients");

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

        // Assert
        Assert.NotSame(timeoutTask, completedTask, 
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

    #region Test Utilities and Cleanup

    public void Dispose()
    {
        _pool?.Dispose();
    }

    #endregion
}