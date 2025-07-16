using SerialPortPool.Core.Models;
using Xunit;

namespace SerialPortPool.Core.Tests.Models;

public class SerialPortInfoTests
{
    [Fact]
    public void SerialPortInfo_DefaultConstructor_SetsDefaultValues()
    {
        // Arrange & Act
        var portInfo = new SerialPortInfo();
        
        // Assert
        Assert.Equal(string.Empty, portInfo.PortName);
        Assert.Equal(string.Empty, portInfo.FriendlyName);
        Assert.Equal(PortStatus.Unknown, portInfo.Status);
        Assert.False(portInfo.IsValidForPool);
        Assert.True(portInfo.LastSeen <= DateTime.Now);
        Assert.True(portInfo.LastSeen > DateTime.Now.AddMinutes(-1));
    }
    
    [Fact]
    public void SerialPortInfo_WithPortName_SetsProperties()
    {
        // Arrange & Act
        var portInfo = new SerialPortInfo
        {
            PortName = "COM3",
            FriendlyName = "USB Serial Port",
            Status = PortStatus.Available,
            IsValidForPool = true
        };
        
        // Assert
        Assert.Equal("COM3", portInfo.PortName);
        Assert.Equal("USB Serial Port", portInfo.FriendlyName);
        Assert.Equal(PortStatus.Available, portInfo.Status);
        Assert.True(portInfo.IsValidForPool);
    }
    
    [Fact]
    public void SerialPortInfo_ToString_ReturnsExpectedFormat()
    {
        // Arrange
        var portInfo = new SerialPortInfo
        {
            PortName = "COM3",
            FriendlyName = "USB Serial Port",
            Status = PortStatus.Available
        };
        
        // Act
        var result = portInfo.ToString();
        
        // Assert
        Assert.Equal("COM3 - USB Serial Port (Available)", result);
    }
    
    [Fact]
    public void SerialPortInfo_Equals_SamePortName_ReturnsTrue()
    {
        // Arrange
        var port1 = new SerialPortInfo { PortName = "COM3" };
        var port2 = new SerialPortInfo { PortName = "COM3", FriendlyName = "Different Name" };
        
        // Act & Assert
        Assert.True(port1.Equals(port2));
        Assert.Equal(port1.GetHashCode(), port2.GetHashCode());
    }
    
    [Fact]
    public void SerialPortInfo_Equals_DifferentPortName_ReturnsFalse()
    {
        // Arrange
        var port1 = new SerialPortInfo { PortName = "COM3" };
        var port2 = new SerialPortInfo { PortName = "COM4" };
        
        // Act & Assert
        Assert.False(port1.Equals(port2));
    }
}

public class PortStatusTests
{
    [Fact]
    public void PortStatus_AllValues_AreDefined()
    {
        // Arrange & Act
        var allStatuses = Enum.GetValues<PortStatus>();

        // Assert
        Assert.Contains(PortStatus.Unknown, allStatuses);
        Assert.Contains(PortStatus.Available, allStatuses);
        Assert.Contains(PortStatus.Allocated, allStatuses);
        Assert.Contains(PortStatus.Connected, allStatuses);
        Assert.Contains(PortStatus.Disconnected, allStatuses);
        Assert.Contains(PortStatus.Error, allStatuses);
    }
}

