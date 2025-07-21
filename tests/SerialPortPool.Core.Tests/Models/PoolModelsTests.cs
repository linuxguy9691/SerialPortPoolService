using SerialPortPool.Core.Models;
using Xunit;

namespace SerialPortPool.Core.Tests.Models;

/// <summary>
/// Unit tests for the pool management models (ÉTAPE 3)
/// </summary>
public class PoolModelsTests
{
    #region AllocationStatus Tests

    [Fact]
    public void AllocationStatus_HasExpectedValues()
    {
        // Act & Assert - Verify all expected enum values exist
        Assert.True(Enum.IsDefined(typeof(AllocationStatus), AllocationStatus.Available));
        Assert.True(Enum.IsDefined(typeof(AllocationStatus), AllocationStatus.Allocated));
        Assert.True(Enum.IsDefined(typeof(AllocationStatus), AllocationStatus.Reserved));
        Assert.True(Enum.IsDefined(typeof(AllocationStatus), AllocationStatus.Error));
    }

    [Fact]
    public void AllocationStatus_HasCorrectCount()
    {
        // Act
        var values = Enum.GetValues<AllocationStatus>();

        // Assert
        Assert.Equal(4, values.Length);
    }

    #endregion

    #region PortAllocation Tests

    [Fact]
    public void PortAllocation_DefaultConstructor_SetsDefaults()
    {
        // Act
        var allocation = new PortAllocation();

        // Assert
        Assert.Equal(string.Empty, allocation.PortName);
        Assert.Equal(AllocationStatus.Available, allocation.Status);
        Assert.True(allocation.AllocatedAt <= DateTime.Now);
        Assert.Null(allocation.ReleasedAt);
        Assert.Null(allocation.AllocatedTo);
        Assert.NotNull(allocation.SessionId);
        Assert.NotEqual(string.Empty, allocation.SessionId);
        Assert.NotNull(allocation.Metadata);
        Assert.Empty(allocation.Metadata);
    }

    [Fact]
    public void PortAllocation_Create_SetsCorrectValues()
    {
        // Arrange
        var portName = "COM5";
        var clientId = "TestClient";
        var beforeCreate = DateTime.Now;

        // Act
        var allocation = PortAllocation.Create(portName, clientId);
        var afterCreate = DateTime.Now;

        // Assert
        Assert.Equal(portName, allocation.PortName);
        Assert.Equal(AllocationStatus.Allocated, allocation.Status);
        Assert.Equal(clientId, allocation.AllocatedTo);
        Assert.True(allocation.AllocatedAt >= beforeCreate && allocation.AllocatedAt <= afterCreate);
        Assert.Null(allocation.ReleasedAt);
        Assert.NotNull(allocation.SessionId);
        Assert.NotEqual(string.Empty, allocation.SessionId);
        Assert.True(allocation.IsActive);
    }

    [Fact]
    public void PortAllocation_Create_WithoutClientId_Works()
    {
        // Act
        var allocation = PortAllocation.Create("COM3");

        // Assert
        Assert.Equal("COM3", allocation.PortName);
        Assert.Equal(AllocationStatus.Allocated, allocation.Status);
        Assert.Null(allocation.AllocatedTo);
        Assert.True(allocation.IsActive);
    }

    [Fact]
    public void PortAllocation_Release_UpdatesStatus()
    {
        // Arrange
        var allocation = PortAllocation.Create("COM4", "Client1");
        var beforeRelease = DateTime.Now;

        // Act
        allocation.Release();
        var afterRelease = DateTime.Now;

        // Assert
        Assert.Equal(AllocationStatus.Available, allocation.Status);
        Assert.NotNull(allocation.ReleasedAt);
        Assert.True(allocation.ReleasedAt >= beforeRelease && allocation.ReleasedAt <= afterRelease);
        Assert.False(allocation.IsActive);
    }

    [Fact]
    public void PortAllocation_SetError_UpdatesStatus()
    {
        // Arrange
        var allocation = PortAllocation.Create("COM6", "Client2");
        var errorMessage = "Hardware failure";

        // Act
        allocation.SetError(errorMessage);

        // Assert
        Assert.Equal(AllocationStatus.Error, allocation.Status);
        Assert.Equal(errorMessage, allocation.Metadata["ErrorDetails"]);
        Assert.True(allocation.Metadata.ContainsKey("ErrorAt"));
        Assert.False(allocation.IsActive);
    }

    [Fact]
    public void PortAllocation_SetError_WithoutMessage_Works()
    {
        // Arrange
        var allocation = PortAllocation.Create("COM7");

        // Act
        allocation.SetError();

        // Assert
        Assert.Equal(AllocationStatus.Error, allocation.Status);
        Assert.False(allocation.Metadata.ContainsKey("ErrorDetails"));
        Assert.False(allocation.IsActive);
    }

    [Fact]
    public void PortAllocation_AllocationDuration_CalculatesCorrectly()
    {
        // Arrange
        var allocation = new PortAllocation
        {
            AllocatedAt = DateTime.Now.AddMinutes(-5)
        };

        // Act
        var duration = allocation.AllocationDuration;

        // Assert
        Assert.True(duration.TotalMinutes >= 4.9 && duration.TotalMinutes <= 5.1);
    }

    [Fact]
    public void PortAllocation_AllocationDuration_WithRelease_UsesReleaseTime()
    {
        // Arrange
        var allocatedAt = DateTime.Now.AddMinutes(-10);
        var releasedAt = DateTime.Now.AddMinutes(-5);
        var allocation = new PortAllocation
        {
            AllocatedAt = allocatedAt,
            ReleasedAt = releasedAt
        };

        // Act
        var duration = allocation.AllocationDuration;

        // Assert
        Assert.True(duration.TotalMinutes >= 4.9 && duration.TotalMinutes <= 5.1);
    }

    [Fact]
    public void PortAllocation_ToString_FormatsCorrectly()
    {
        // Arrange
        var allocation = PortAllocation.Create("COM8", "TestClient");

        // Act
        var result = allocation.ToString();

        // Assert
        Assert.Contains("COM8", result);
        Assert.Contains("TestClient", result);
        Assert.Contains("Allocated", result);
        Assert.Contains("🔴", result); // Allocated icon
    }

    [Fact]
    public void PortAllocation_Equals_WorksCorrectly()
    {
        // Arrange
        var sessionId = Guid.NewGuid().ToString();
        var allocation1 = new PortAllocation { PortName = "COM9", SessionId = sessionId };
        var allocation2 = new PortAllocation { PortName = "COM9", SessionId = sessionId };
        var allocation3 = new PortAllocation { PortName = "COM10", SessionId = sessionId };

        // Act & Assert
        Assert.Equal(allocation1, allocation2);
        Assert.NotEqual(allocation1, allocation3);
        Assert.Equal(allocation1.GetHashCode(), allocation2.GetHashCode());
    }

    #endregion

    #region SystemInfo Tests

    [Fact]
    public void SystemInfo_DefaultConstructor_SetsDefaults()
    {
        // Act
        var systemInfo = new SystemInfo();

        // Assert
        Assert.Equal(string.Empty, systemInfo.SerialNumber);
        Assert.Equal(string.Empty, systemInfo.FirmwareVersion);
        Assert.Equal(string.Empty, systemInfo.HardwareRevision);
        Assert.Equal(string.Empty, systemInfo.Manufacturer);
        Assert.Equal(string.Empty, systemInfo.ProductDescription);
        Assert.NotNull(systemInfo.EepromData);
        Assert.Empty(systemInfo.EepromData);
        Assert.NotNull(systemInfo.SystemProperties);
        Assert.Empty(systemInfo.SystemProperties);
        Assert.NotNull(systemInfo.ClientConfiguration);
        Assert.Empty(systemInfo.ClientConfiguration);
        Assert.True(systemInfo.LastRead <= DateTime.Now);
        Assert.True(systemInfo.IsDataValid);
        Assert.Null(systemInfo.ErrorMessage);
    }

    [Fact]
    public void SystemInfo_IsFresh_WorksCorrectly()
    {
        // Arrange
        var freshInfo = new SystemInfo { LastRead = DateTime.Now.AddMinutes(-3) };
        var staleInfo = new SystemInfo { LastRead = DateTime.Now.AddMinutes(-10) };

        // Act & Assert
        Assert.True(freshInfo.IsFresh);
        Assert.False(staleInfo.IsFresh);
    }

    [Fact]
    public void SystemInfo_Age_CalculatesCorrectly()
    {
        // Arrange
        var systemInfo = new SystemInfo
        {
            LastRead = DateTime.Now.AddMinutes(-7)
        };

        // Act
        var age = systemInfo.Age;

        // Assert
        Assert.True(age.TotalMinutes >= 6.9 && age.TotalMinutes <= 7.1);
    }

    [Fact]
    public void SystemInfo_GetClientConfig_ReturnsCorrectValue()
    {
        // Arrange
        var systemInfo = new SystemInfo();
        systemInfo.ClientConfiguration["TestKey"] = "TestValue";

        // Act
        var result = systemInfo.GetClientConfig("TestKey");
        var notFound = systemInfo.GetClientConfig("NonExistentKey");

        // Assert
        Assert.Equal("TestValue", result);
        Assert.Null(notFound);
    }

    [Fact]
    public void SystemInfo_SetClientConfig_SetsValue()
    {
        // Arrange
        var systemInfo = new SystemInfo();

        // Act
        systemInfo.SetClientConfig("NewKey", "NewValue");

        // Assert
        Assert.Equal("NewValue", systemInfo.ClientConfiguration["NewKey"]);
        Assert.Equal("NewValue", systemInfo.GetClientConfig("NewKey"));
    }

    [Fact]
    public void SystemInfo_GetSystemProperty_ReturnsCorrectValue()
    {
        // Arrange
        var systemInfo = new SystemInfo();
        systemInfo.SystemProperties["OS"] = "Windows";

        // Act
        var result = systemInfo.GetSystemProperty("OS");
        var notFound = systemInfo.GetSystemProperty("CPU");

        // Assert
        Assert.Equal("Windows", result);
        Assert.Null(notFound);
    }

    [Fact]
    public void SystemInfo_GetEepromValue_ReturnsCorrectValue()
    {
        // Arrange
        var systemInfo = new SystemInfo();
        systemInfo.EepromData["Version"] = "1.2.3";

        // Act
        var result = systemInfo.GetEepromValue("Version");
        var notFound = systemInfo.GetEepromValue("Unknown");

        // Assert
        Assert.Equal("1.2.3", result);
        Assert.Null(notFound);
    }

    [Fact]
    public void SystemInfo_SetError_UpdatesState()
    {
        // Arrange
        var systemInfo = new SystemInfo();
        var errorMessage = "Read failure";

        // Act
        systemInfo.SetError(errorMessage);

        // Assert
        Assert.False(systemInfo.IsDataValid);
        Assert.Equal(errorMessage, systemInfo.ErrorMessage);
        Assert.True(systemInfo.LastRead <= DateTime.Now);
    }

    [Fact]
    public void SystemInfo_FromFtdiDevice_CreatesCorrectly()
    {
        // Arrange
        var ftdiInfo = new FtdiDeviceInfo
        {
            SerialNumber = "ABC123",
            Manufacturer = "FTDI",
            ProductDescription = "USB Serial Port"
        };
        ftdiInfo.EepromData["TestKey"] = "TestValue";

        // Act
        var systemInfo = SystemInfo.FromFtdiDevice(ftdiInfo);

        // Assert
        Assert.Equal("ABC123", systemInfo.SerialNumber);
        Assert.Equal("FTDI", systemInfo.Manufacturer);
        Assert.Equal("USB Serial Port", systemInfo.ProductDescription);
        Assert.Equal("TestValue", systemInfo.EepromData["TestKey"]);
        Assert.True(systemInfo.IsDataValid);
        Assert.True(systemInfo.LastRead <= DateTime.Now);
    }

    [Fact]
    public void SystemInfo_CreateError_CreatesErrorState()
    {
        // Arrange
        var serialNumber = "ERR001";
        var errorMessage = "Device not found";

        // Act
        var systemInfo = SystemInfo.CreateError(serialNumber, errorMessage);

        // Assert
        Assert.Equal(serialNumber, systemInfo.SerialNumber);
        Assert.False(systemInfo.IsDataValid);
        Assert.Equal(errorMessage, systemInfo.ErrorMessage);
        Assert.True(systemInfo.LastRead <= DateTime.Now);
    }

    [Fact]
    public void SystemInfo_GetSummary_FormatsCorrectly()
    {
        // Arrange
        var systemInfo = new SystemInfo
        {
            SerialNumber = "DEV001",
            Manufacturer = "FTDI",
            ProductDescription = "Test Device",
            IsDataValid = true
        };
        systemInfo.EepromData["Key1"] = "Value1";
        systemInfo.SystemProperties["Key2"] = "Value2";

        // Act
        var summary = systemInfo.GetSummary();

        // Assert
        Assert.Contains("DEV001", summary);
        Assert.Contains("FTDI", summary);
        Assert.Contains("Test Device", summary);
        Assert.Contains("✅", summary);
        Assert.Contains("2 properties", summary);
    }

    [Fact]
    public void SystemInfo_GetSummary_WithError_ShowsError()
    {
        // Arrange
        var systemInfo = SystemInfo.CreateError("ERR002", "Read failed");

        // Act
        var summary = systemInfo.GetSummary();

        // Assert
        Assert.Contains("❌", summary);
        Assert.Contains("ERR002", summary);
        Assert.Contains("Read failed", summary);
    }

    [Fact]
    public void SystemInfo_Equals_WorksCorrectly()
    {
        // Arrange
        var info1 = new SystemInfo { SerialNumber = "SN001" };
        var info2 = new SystemInfo { SerialNumber = "SN001" };
        var info3 = new SystemInfo { SerialNumber = "SN002" };

        // Act & Assert
        Assert.Equal(info1, info2);
        Assert.NotEqual(info1, info3);
        Assert.Equal(info1.GetHashCode(), info2.GetHashCode());
    }

    #endregion

    #region PoolStatistics Tests

    [Fact]
    public void PoolStatistics_UtilizationPercentage_CalculatesCorrectly()
    {
        // Arrange
        var stats = new PoolStatistics
        {
            TotalPorts = 10,
            AllocatedPorts = 7
        };

        // Act
        var utilization = stats.UtilizationPercentage;

        // Assert
        Assert.Equal(70.0, utilization);
    }

    [Fact]
    public void PoolStatistics_UtilizationPercentage_WithZeroTotal_ReturnsZero()
    {
        // Arrange
        var stats = new PoolStatistics
        {
            TotalPorts = 0,
            AllocatedPorts = 0
        };

        // Act
        var utilization = stats.UtilizationPercentage;

        // Assert
        Assert.Equal(0.0, utilization);
    }

    [Fact]
    public void PoolStatistics_ToString_FormatsCorrectly()
    {
        // Arrange
        var stats = new PoolStatistics
        {
            TotalPorts = 8,
            AllocatedPorts = 3,
            ActiveClients = 2,
            ErrorPorts = 1
        };

        // Act
        var result = stats.ToString();

        // Assert
        Assert.Contains("3/8", result);
        Assert.Contains("37.5%", result);
        Assert.Contains("2 clients", result);
        Assert.Contains("1 errors", result);
    }

    #endregion
}