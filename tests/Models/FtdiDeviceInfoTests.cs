using SerialPortPool.Core.Models;
using Xunit;

namespace SerialPortPool.Core.Tests.Models;

public class FtdiDeviceInfoTests
{
    [Fact]
    public void ParseFromDeviceId_WithValidFtdiDeviceId_ReturnsCorrectInfo()
    {
        // Arrange - Your real COM6 device ID!
        const string deviceId = "FTDIBUS\\VID_0403+PID_6001+AG0JU7O1A\\0000";
        
        // Act
        var result = FtdiDeviceInfo.ParseFromDeviceId(deviceId);
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal("0403", result.VendorId);
        Assert.Equal("6001", result.ProductId);
        Assert.Equal("FT232R", result.ChipType);
        Assert.Equal("AG0JU7O1A", result.SerialNumber);
        Assert.True(result.IsGenuineFtdi);
        Assert.False(result.Is4232H); // Your device is FT232R, not 4232H
        Assert.Equal(deviceId, result.RawDeviceId);
    }
    
    [Fact]
    public void ParseFromDeviceId_With4232HDeviceId_ReturnsCorrect4232HInfo()
    {
        // Arrange - Simulated 4232H device (client requirement)
        const string deviceId = "FTDIBUS\\VID_0403+PID_6011+CLIENTSERIAL\\0000";
        
        // Act
        var result = FtdiDeviceInfo.ParseFromDeviceId(deviceId);
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal("0403", result.VendorId);
        Assert.Equal("6011", result.ProductId);
        Assert.Equal("FT4232H", result.ChipType);
        Assert.Equal("CLIENTSERIAL", result.SerialNumber);
        Assert.True(result.IsGenuineFtdi);
        Assert.True(result.Is4232H); // This would be valid for client
    }
    
    [Fact]
    public void ParseFromDeviceId_WithNonFtdiDevice_ReturnsNull()
    {
        // Arrange
        const string deviceId = "USB\\VID_1234+PID_5678+SOMESERIAL\\0000";
        
        // Act
        var result = FtdiDeviceInfo.ParseFromDeviceId(deviceId);
        
        // Assert
        Assert.Null(result);
    }
    
    [Fact]
    public void ParseFromDeviceId_WithNullOrEmpty_ReturnsNull()
    {
        // Act & Assert
        Assert.Null(FtdiDeviceInfo.ParseFromDeviceId(null!));
        Assert.Null(FtdiDeviceInfo.ParseFromDeviceId(string.Empty));
        Assert.Null(FtdiDeviceInfo.ParseFromDeviceId("   "));
    }
    
    [Theory]
    [InlineData("6001", "FT232R", false)] // Your device
    [InlineData("6011", "FT4232H", true)] // Client requirement
    [InlineData("6014", "FT232H", false)]
    [InlineData("6010", "FT2232H", false)]
    [InlineData("9999", "Unknown FTDI Chip (PID: 9999)", false)]
    public void ChipTypeMapping_WorksCorrectly(string pid, string expectedChipType, bool expectedIs4232H)
    {
        // Arrange
        var deviceId = $"FTDIBUS\\VID_0403+PID_{pid}+TESTSERIAL\\0000";
        
        // Act
        var result = FtdiDeviceInfo.ParseFromDeviceId(deviceId);
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedChipType, result.ChipType);
        Assert.Equal(expectedIs4232H, result.Is4232H);
    }
    
    [Fact]
    public void ToString_WithValidFtdiInfo_ReturnsFormattedString()
    {
        // Arrange - Your device info
        var ftdiInfo = FtdiDeviceInfo.ParseFromDeviceId("FTDIBUS\\VID_0403+PID_6001+AG0JU7O1A\\0000");
        
        // Act
        var result = ftdiInfo!.ToString();
        
        // Assert
        Assert.Contains("FTDI FT232R", result);
        Assert.Contains("VID: 0403", result);
        Assert.Contains("PID: 6001", result);
        Assert.Contains("❌ INVALID (Not 4232H)", result); // Your device won't be valid for client
    }
    
    [Fact]
    public void ToString_With4232HInfo_ShowsValid()
    {
        // Arrange - Valid client device
        var ftdiInfo = FtdiDeviceInfo.ParseFromDeviceId("FTDIBUS\\VID_0403+PID_6011+CLIENTSERIAL\\0000");
        
        // Act
        var result = ftdiInfo!.ToString();
        
        // Assert
        Assert.Contains("FTDI FT4232H", result);
        Assert.Contains("✅ VALID (4232H)", result);
    }
}

public class PortValidationTests
{
    [Fact]
    public void PortValidationResult_Success_CreatesValidResult()
    {
        // Arrange
        var passedCriteria = new[] { ValidationCriteria.FtdiDeviceDetected, ValidationCriteria.Is4232HChip };
        
        // Act
        var result = PortValidationResult.Success("Valid 4232H device", passedCriteria);
        
        // Assert
        Assert.True(result.IsValid);
        Assert.Equal("Valid 4232H device", result.Reason);
        Assert.Equal(100, result.ValidationScore);
        Assert.Equal(passedCriteria, result.PassedCriteria);
        Assert.Empty(result.FailedCriteria);
    }
    
    [Fact]
    public void PortValidationResult_Failure_CreatesInvalidResult()
    {
        // Arrange - Your COM6 scenario
        var failedCriteria = new[] { ValidationCriteria.Not4232HChip };
        var passedCriteria = new[] { ValidationCriteria.FtdiDeviceDetected, ValidationCriteria.GenuineFtdiDevice };
        
        // Act
        var result = PortValidationResult.Failure("FTDI device but not 4232H chip", failedCriteria, passedCriteria);
        
        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("FTDI device but not 4232H chip", result.Reason);
        Assert.Equal(67, result.ValidationScore); // 2 passed out of 3 total = 67%
        Assert.Equal(failedCriteria, result.FailedCriteria);
        Assert.Equal(passedCriteria, result.PassedCriteria);
    }
    
    [Fact]
    public void PortValidationConfiguration_ClientDefault_HasCorrectSettings()
    {
        // Act
        var config = PortValidationConfiguration.CreateClientDefault();
        
        // Assert
        Assert.True(config.RequireFtdiDevice);
        Assert.True(config.Require4232HChip);
        Assert.Equal("FTDI", config.ExpectedManufacturer);
        Assert.Contains("6011", config.AllowedFtdiProductIds); // Only 4232H
        Assert.Equal(100, config.MinimumValidationScore);
        Assert.True(config.StrictValidation);
    }
    
    [Fact]
    public void PortValidationConfiguration_DevelopmentDefault_IsMorePermissive()
    {
        // Act
        var config = PortValidationConfiguration.CreateDevelopmentDefault();
        
        // Assert
        Assert.True(config.RequireFtdiDevice);
        Assert.False(config.Require4232HChip); // More permissive for dev
        Assert.Contains("6001", config.AllowedFtdiProductIds); // Your device allowed
        Assert.Contains("6011", config.AllowedFtdiProductIds); // 4232H also allowed
        Assert.Equal(70, config.MinimumValidationScore);
        Assert.False(config.StrictValidation);
    }
}