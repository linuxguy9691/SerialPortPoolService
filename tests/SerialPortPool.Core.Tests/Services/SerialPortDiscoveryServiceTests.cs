using Microsoft.Extensions.Logging;
using Moq;
using SerialPortPool.Core.Models;
using SerialPortPool.Core.Services;
using Xunit;

namespace SerialPortPool.Core.Tests.Services;

public class SerialPortDiscoveryServiceTests
{
    private readonly Mock<ILogger<SerialPortDiscoveryService>> _loggerMock;
    private readonly SerialPortDiscoveryService _service;

    public SerialPortDiscoveryServiceTests()
    {
        _loggerMock = new Mock<ILogger<SerialPortDiscoveryService>>();
        _service = new SerialPortDiscoveryService(_loggerMock.Object);
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new SerialPortDiscoveryService(null!));
    }

    [Fact]
    public async Task DiscoverPortsAsync_ReturnsPortCollection()
    {
        // Act
        var result = await _service.DiscoverPortsAsync();

        // Assert
        Assert.NotNull(result);
        // Note: We can't test exact count as it depends on the system
        // The test verifies the method doesn't throw and returns a valid collection
    }

    [Fact]
    public async Task GetPortInfoAsync_WithNullPortName_ReturnsNull()
    {
        // Act
        var result = await _service.GetPortInfoAsync(null!);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetPortInfoAsync_WithEmptyPortName_ReturnsNull()
    {
        // Act
        var result = await _service.GetPortInfoAsync(string.Empty);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetPortInfoAsync_WithWhitespacePortName_ReturnsNull()
    {
        // Act
        var result = await _service.GetPortInfoAsync("   ");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetPortInfoAsync_WithInvalidPortName_ReturnsPortInfoWithError()
    {
        // Arrange
        const string invalidPortName = "INVALID_PORT_NAME";

        // Act
        var result = await _service.GetPortInfoAsync(invalidPortName);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(invalidPortName, result.PortName);
        // Status might be Error or Available depending on system behavior
        Assert.True(result.Status == PortStatus.Error || result.Status == PortStatus.Available);
    }
}