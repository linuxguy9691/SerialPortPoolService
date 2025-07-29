// tests/SerialPortPool.Core.Tests/POC/CriticalPOCTests.cs - NEW
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using SerialPortPool.Core.Interfaces;
using SerialPortPool.Core.Models;
using SerialPortPool.Core.Services;
using System.Diagnostics;
using Xunit;

namespace SerialPortPool.Core.Tests.POC;

/// <summary>
/// CRITICAL POC Tests - GO/NO-GO Decision (4h maximum)
/// These tests determine if ZERO TOUCH Extension Layer strategy works
/// </summary>
public class CriticalPOCTests : IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ISerialPortPool _existingPool;
    private readonly IPortReservationService _reservationService;
    private readonly Mock<ISerialPortDiscovery> _mockDiscovery;
    private readonly Mock<ISerialPortValidator> _mockValidator;
    private readonly Mock<ISystemInfoCache> _mockSystemInfoCache;

    public CriticalPOCTests()
    {
        // Setup service provider with existing + new services
        var services = new ServiceCollection();
        
        // Add logging
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Debug));
        
        // Setup mocks for existing services
        _mockDiscovery = new Mock<ISerialPortDiscovery>();
        _mockValidator = new Mock<ISerialPortValidator>();
        _mockSystemInfoCache = new Mock<ISystemInfoCache>();
        
        // Register existing services (NO MODIFICATION)
        services.AddSingleton(_mockDiscovery.Object);
        services.AddSingleton(_mockValidator.Object);
        services.AddSingleton(_mockSystemInfoCache.Object);
        services.AddScoped<ISerialPortPool, SerialPortPool.Core.Services.SerialPortPool>();
        
        // Register NEW POC services (EXTENSION LAYER)
        services.AddScoped<IPortReservationService, PortReservationService>();
        
        _serviceProvider = services.BuildServiceProvider();
        _existingPool = _serviceProvider.GetRequiredService<ISerialPortPool>();
        _reservationService = _serviceProvider.GetRequiredService<IPortReservationService>();
        
        // Setup default mock behavior
        SetupDefaultMockBehavior();
    }

    private void SetupDefaultMockBehavior()
    {
        // Setup discovery to return test ports
        var testPorts = new[]
        {
            new SerialPortInfo { PortName = "COM1", Status = PortStatus.Available },
            new SerialPortInfo { PortName = "COM2", Status = PortStatus.Available },
            new SerialPortInfo { PortName = "COM3", Status = PortStatus.Available }
        };
        
        _mockDiscovery.Setup(d => d.DiscoverPortsAsync()).ReturnsAsync(testPorts);
        
        // Setup validator to pass through all ports
        _mockValidator.Setup(v => v.GetValidPortsAsync(It.IsAny<IEnumerable<SerialPortInfo>>(), It.IsAny<PortValidationConfiguration>()))
                    .ReturnsAsync((IEnumerable<SerialPortInfo> ports, PortValidationConfiguration config) => ports);
        
        // Setup cache to return basic system info
        _mockSystemInfoCache.Setup(c => c.GetSystemInfoAsync(It.IsAny<string>(), It.IsAny<bool>()))
                          .ReturnsAsync((string portName, bool forceRefresh) => 
                              new SystemInfo { SerialNumber = $"TEST_{portName}", IsDataValid = true });
    }

    /// <summary>
    /// POC TEST 1: CRITICAL - Prove composition pattern works
    /// </summary>
    [Fact]
    public async Task POC_Test1_CompositionPattern_Works()
    {
        // ✅ CRITICAL: Prove composition pattern works without modifying existing code
        
        // Arrange
        var criteria = PortReservationCriteria.CreatePermissive();
        
        // Act: Reserve port using new service (wraps existing pool)
        var reservation = await _reservationService.ReservePortAsync(criteria, "POC_Test1");
        
        // Assert: Reservation successful AND existing pool affected
        Assert.NotNull(reservation);
        Assert.NotEmpty(reservation.PortName);
        Assert.NotEmpty(reservation.ReservationId);
        Assert.Equal("POC_Test1", reservation.ClientId);
        Assert.True(reservation.IsActive);
        Assert.False(reservation.IsExpired);
        
        // Verify underlying allocation exists in existing pool
        var poolStats = await _existingPool.GetStatisticsAsync();
        Assert.Equal(1, poolStats.AllocatedPorts);
        
        // Verify we can retrieve the allocation from existing pool
        var poolAllocation = await _existingPool.GetPortAllocationAsync(reservation.PortName);
        Assert.NotNull(poolAllocation);
        Assert.Equal(reservation.SessionId, poolAllocation.SessionId);
        
        // Cleanup
        var released = await _reservationService.ReleaseReservationAsync(reservation.ReservationId, "POC_Test1");
        Assert.True(released);
        
        // Verify cleanup worked
        var finalStats = await _existingPool.GetStatisticsAsync();
        Assert.Equal(0, finalStats.AllocatedPorts);
        
        Console.WriteLine("✅ POC Test 1 PASSED: Composition pattern works perfectly");
    }

    /// <summary>
    /// POC TEST 2: CRITICAL - Prove no regression in existing functionality
    /// </summary>
    [Fact]
    public async Task POC_Test2_ZeroRegression_ExistingPoolStillWorks()
    {
        // ✅ CRITICAL: Prove existing pool functionality unchanged
        
        // Act 1: Test existing pool directly (as if POC layer doesn't exist)
        var directAllocation = await _existingPool.AllocatePortAsync(clientId: "DirectClient");
        
        // Assert 1: Direct allocation works exactly as before
        Assert.NotNull(directAllocation);
        Assert.Equal("DirectClient", directAllocation.AllocatedTo);
        Assert.True(directAllocation.IsActive);
        
        // Act 2: Test basic pool operations
        var isAllocated = await _existingPool.IsPortAllocatedAsync(directAllocation.PortName);
        var allocation = await _existingPool.GetPortAllocationAsync(directAllocation.PortName);
        var activeCount = await _existingPool.GetAllocatedPortsCountAsync();
        
        // Assert 2: All existing operations work unchanged
        Assert.True(isAllocated);
        Assert.NotNull(allocation);
        Assert.Equal(1, activeCount);
        
        // Act 3: Release using existing pool
        var released = await _existingPool.ReleasePortAsync(directAllocation.PortName, directAllocation.SessionId);
        
        // Assert 3: Release works as before
        Assert.True(released);
        
        var finalActiveCount = await _existingPool.GetAllocatedPortsCountAsync();
        Assert.Equal(0, finalActiveCount);
        
        Console.WriteLine("✅ POC Test 2 PASSED: Zero regression - existing pool works unchanged");
    }

    /// <summary>
    /// POC TEST 3: CRITICAL - Prove performance impact is negligible
    /// </summary>
    [Fact]
    public async Task POC_Test3_PerformanceImpact_Negligible()
    {
        // ✅ CRITICAL: Prove performance overhead < 10ms
        
        const int iterations = 50;
        
        // Measure existing pool performance (baseline)
        var existingTimes = new List<TimeSpan>();
        for (int i = 0; i < iterations; i++)
        {
            var stopwatch = Stopwatch.StartNew();
            var allocation = await _existingPool.AllocatePortAsync(clientId: $"Perf_Existing_{i}");
            stopwatch.Stop();
            
            if (allocation != null)
            {
                existingTimes.Add(stopwatch.Elapsed);
                await _existingPool.ReleasePortAsync(allocation.PortName, allocation.SessionId);
            }
        }
        
        // Measure new reservation service performance
        var reservationTimes = new List<TimeSpan>();
        var criteria = PortReservationCriteria.CreatePermissive();
        
        for (int i = 0; i < iterations; i++)
        {
            var stopwatch = Stopwatch.StartNew();
            var reservation = await _reservationService.ReservePortAsync(criteria, $"Perf_Reservation_{i}");
            stopwatch.Stop();
            
            if (reservation != null)
            {
                reservationTimes.Add(stopwatch.Elapsed);
                await _reservationService.ReleaseReservationAsync(reservation.ReservationId, $"Perf_Reservation_{i}");
            }
        }
        
        // Calculate performance metrics
        var existingAverage = existingTimes.Average(t => t.TotalMilliseconds);
        var reservationAverage = reservationTimes.Average(t => t.TotalMilliseconds);
        var overhead = reservationAverage - existingAverage;
        
        // Assert: Performance overhead < 10ms average
        Assert.True(existingTimes.Count > 0, "Should have existing pool measurements");
        Assert.True(reservationTimes.Count > 0, "Should have reservation service measurements");
        Assert.True(overhead < 10, 
            $"Performance overhead too high: {overhead:F2}ms (expected < 10ms). " +
            $"Existing: {existingAverage:F2}ms, Reservation: {reservationAverage:F2}ms");
        
        Console.WriteLine($"✅ POC Test 3 PASSED: Performance overhead {overhead:F2}ms (acceptable)");
        Console.WriteLine($"   Existing pool: {existingAverage:F2}ms avg");
        Console.WriteLine($"   Reservation service: {reservationAverage:F2}ms avg");
    }

    /// <summary>
    /// POC TEST 4: CRITICAL - Prove thread safety with concurrent operations
    /// </summary>
    [Fact]
    public async Task POC_Test4_ConcurrentReservations_ThreadSafe()
    {
        // ✅ CRITICAL: Prove thread safety with concurrent operations
        
        const int concurrentClients = 8;
        var criteria = PortReservationCriteria.CreatePermissive();
        
        // Act: Create multiple concurrent reservations
        var concurrentTasks = new List<Task<PortReservation?>>();
        for (int i = 0; i < concurrentClients; i++)
        {
            var clientId = $"Concurrent_Client_{i}";
            var task = Task.Run(() => _reservationService.ReservePortAsync(criteria, clientId));
            concurrentTasks.Add(task);
        }
        
        // Wait for all operations to complete with timeout
        var timeoutTask = Task.Delay(TimeSpan.FromSeconds(10));
        var completedTask = await Task.WhenAny(Task.WhenAll(concurrentTasks), timeoutTask);
        
        // Assert: No deadlock
        Assert.True(completedTask != timeoutTask, "Operations should complete without deadlock");
        
        var reservations = await Task.WhenAll(concurrentTasks);
        var successfulReservations = reservations.Where(r => r != null).ToList();
        
        // Assert: Some reservations successful (limited by available ports)
        Assert.NotEmpty(successfulReservations);
        Assert.True(successfulReservations.Count <= 3, "Should not exceed available ports");
        
        // Assert: All successful reservations have unique IDs and ports
        var reservationIds = successfulReservations.Select(r => r!.ReservationId).ToList();
        var portNames = successfulReservations.Select(r => r!.PortName).ToList();
        
        Assert.Equal(reservationIds.Count, reservationIds.Distinct().Count()); // Unique reservation IDs
        Assert.Equal(portNames.Count, portNames.Distinct().Count()); // Unique port allocations
        
        // Test concurrent cleanup
        var cleanupTasks = successfulReservations.Select(reservation =>
            Task.Run(() => _reservationService.ReleaseReservationAsync(reservation!.ReservationId, reservation.ClientId))
        ).ToList();
        
        var cleanupResults = await Task.WhenAll(cleanupTasks);
        Assert.All(cleanupResults, result => Assert.True(result));
        
        // Verify final state
        var finalStats = await _existingPool.GetStatisticsAsync();
        Assert.Equal(0, finalStats.AllocatedPorts);
        
        Console.WriteLine($"✅ POC Test 4 PASSED: {successfulReservations.Count} concurrent reservations successful, thread-safe cleanup");
    }

    /// <summary>
    /// BONUS POC TEST: Integration with existing services
    /// </summary>
    [Fact]
    public async Task POC_Test5_IntegrationWithExistingServices_Works()
    {
        // Test that reservation service integrates properly with existing validation
        var clientCriteria = PortReservationCriteria.CreateForClient();
        var devCriteria = PortReservationCriteria.CreateForDevelopment();
        
        // Act: Reserve with different criteria
        var clientReservation = await _reservationService.ReservePortAsync(clientCriteria, "ClientUser");
        var devReservation = await _reservationService.ReservePortAsync(devCriteria, "DevUser");
        
        // Assert: Both reservations should work
        Assert.NotNull(clientReservation);
        Assert.NotNull(devReservation);
        Assert.NotEqual(clientReservation.PortName, devReservation.PortName); // Different ports
        
        // Test statistics integration
        var reservationStats = await _reservationService.GetReservationStatisticsAsync();
        Assert.Equal(2, reservationStats.ActiveReservations);
        Assert.Equal(2, reservationStats.TotalClients);
        
        // Cleanup
        await _reservationService.ReleaseReservationAsync(clientReservation.ReservationId, "ClientUser");
        await _reservationService.ReleaseReservationAsync(devReservation.ReservationId, "DevUser");
        
        Console.WriteLine("✅ POC Test 5 PASSED: Integration with existing services works");
    }

    public void Dispose()
    {
        _reservationService?.Dispose();
        _serviceProvider?.GetService<IDisposable>()?.Dispose();
    }
}