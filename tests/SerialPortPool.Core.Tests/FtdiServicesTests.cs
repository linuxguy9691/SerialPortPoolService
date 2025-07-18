using Microsoft.Extensions.Logging;
using SerialPortPool.Core.Models;
using SerialPortPool.Core.Services;
using Xunit;

namespace SerialPortPool.Core.Tests.Services;

public class FtdiDeviceReaderTests
{
    private readonly FtdiDeviceReader _reader;

    public FtdiDeviceReaderTests()
    {
        var loggerFactory = LoggerFactory.Create(builder => 
            builder.AddConsole().SetMinimumLevel(LogLevel.Warning));
        var logger = loggerFactory.CreateLogger<FtdiDeviceReader>();
        _reader = new FtdiDeviceReader(logger);
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new FtdiDeviceReader(null!));
    }

    [Fact]
    public async Task ReadDeviceInfoFromIdAsync_WithValidFtdiDeviceId_ReturnsCorrectInfo()
    {
        // Arrange - Your real COM6 device ID!
        const string deviceId = "FTDIBUS\\VID_0403+PID_6001+AG0JU7O1A\\0000";
        
        // Act
        var result = await _reader.ReadDeviceInfoFromIdAsync(deviceId);
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal("0403", result.VendorId);
        Assert.Equal("6001", result.ProductId);
        Assert.Equal("FT232R", result.ChipType);
        Assert.Equal("AG0JU7O1A", result.SerialNumber);
        Assert.True(result.IsGenuineFtdi);
        Assert.False(result.Is4232H); // Your device is not 4232H
    }

    [Fact]
    public async Task ReadDeviceInfoFromIdAsync_WithNonFtdiDevice_ReturnsNull()
    {
        // Arrange
        const string deviceId = "USB\\VID_1234+PID_5678+SOMESERIAL\\0000";
        
        // Act
        var result = await _reader.ReadDeviceInfoFromIdAsync(deviceId);
        
        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task ReadDeviceInfoFromIdAsync_WithNullOrEmpty_ReturnsNull()
    {
        // Act & Assert
        Assert.Null(await _reader.ReadDeviceInfoFromIdAsync(null!));
        Assert.Null(await _reader.ReadDeviceInfoFromIdAsync(string.Empty));
        Assert.Null(await _reader.ReadDeviceInfoFromIdAsync("   "));
    }

    [Fact]
    public void AnalyzeDeviceId_WithValidFtdiDeviceId_ReturnsCorrectInfo()
    {
        // Arrange
        const string deviceId = "FTDIBUS\\VID_0403+PID_6011+CLIENTSERIAL\\0000";
        
        // Act
        var result = _reader.AnalyzeDeviceId(deviceId);
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal("0403", result.VendorId);
        Assert.Equal("6011", result.ProductId);
        Assert.Equal("FT4232H", result.ChipType);
        Assert.True(result.Is4232H); // This would be valid for client
    }

    [Fact]
    public async Task GetAllFtdiDevicesAsync_ReturnsValidCollection()
    {
        // Act
        var result = await _reader.GetAllFtdiDevicesAsync();
        
        // Assert
        Assert.NotNull(result);
        // Can't test exact count as it depends on system hardware
        // but the method should not throw
    }

    [Fact]
    public async Task ReadEepromDataAsync_WithValidPortName_ReturnsData()
    {
        // Arrange
        const string portName = "COM999"; // Unlikely to exist but valid format
        
        // Act
        var result = await _reader.ReadEepromDataAsync(portName);
        
        // Assert
        Assert.NotNull(result);
        // Should return empty dictionary for non-existent port
    }
}

public class SerialPortValidatorTests
{
    private readonly SerialPortValidator _validator;
    private readonly FtdiDeviceReader _ftdiReader;

    public SerialPortValidatorTests()
    {
        var loggerFactory = LoggerFactory.Create(builder => 
            builder.AddConsole().SetMinimumLevel(LogLevel.Warning));
        
        var ftdiLogger = loggerFactory.CreateLogger<FtdiDeviceReader>();
        var validatorLogger = loggerFactory.CreateLogger<SerialPortValidator>();
        
        _ftdiReader = new FtdiDeviceReader(ftdiLogger);
        _validator = new SerialPortValidator(_ftdiReader, validatorLogger);
    }

    [Fact]
    public void Constructor_WithNullDependencies_ThrowsArgumentNullException()
    {
        var logger = LoggerFactory.Create(b => b.AddConsole()).CreateLogger<SerialPortValidator>();
        
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new SerialPortValidator(null!, logger));
        Assert.Throws<ArgumentNullException>(() => new SerialPortValidator(_ftdiReader, null!));
    }

    [Fact]
    public async Task ValidatePortAsync_WithFtdiDevice_ReturnsValidationResult()
    {
        // Arrange - Simulate your COM6 device
        var portInfo = new SerialPortInfo
        {
            PortName = "COM6",
            FriendlyName = "USB Serial Port (COM6)",
            Status = PortStatus.Available,
            DeviceId = "FTDIBUS\\VID_0403+PID_6001+AG0JU7O1A\\0000",
            IsFtdiDevice = true,
            FtdiInfo = FtdiDeviceInfo.ParseFromDeviceId("FTDIBUS\\VID_0403+PID_6001+AG0JU7O1A\\0000")
        };

        // Act - Test with client configuration (strict)
        var clientConfig = PortValidationConfiguration.CreateClientDefault();
        var result = await _validator.ValidatePortAsync(portInfo, clientConfig);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsValid); // Your FT232R won't be valid for client (needs 4232H)
        Assert.Contains("4232H", result.Reason); // Should mention 4232H requirement
        Assert.True(result.ValidationScore > 0); // Should have some score (FTDI detected)
    }

    [Fact]
    public async Task ValidatePortAsync_WithFtdi4232H_PassesClientValidation()
    {
        // Arrange - Simulate valid client device
        var portInfo = new SerialPortInfo
        {
            PortName = "COM7",
            FriendlyName = "USB Serial Port (COM7)",
            Status = PortStatus.Available,
            DeviceId = "FTDIBUS\\VID_0403+PID_6011+CLIENTSERIAL\\0000",
            IsFtdiDevice = true,
            FtdiInfo = FtdiDeviceInfo.ParseFromDeviceId("FTDIBUS\\VID_0403+PID_6011+CLIENTSERIAL\\0000")
        };

        // Act - Test with client configuration
        var clientConfig = PortValidationConfiguration.CreateClientDefault();
        var result = await _validator.ValidatePortAsync(portInfo, clientConfig);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsValid); // FT4232H should be valid for client
        Assert.Equal(100, result.ValidationScore); // Should have perfect score
        Assert.Contains("4232H", result.Reason); // Should mention 4232H success
    }

    [Fact]
    public async Task ValidatePortAsync_WithFtdiDeviceInDevMode_IsMorePermissive()
    {
        // Arrange - Your COM6 device again
        var portInfo = new SerialPortInfo
        {
            PortName = "COM6",
            FriendlyName = "USB Serial Port (COM6)",
            Status = PortStatus.Available,
            DeviceId = "FTDIBUS\\VID_0403+PID_6001+AG0JU7O1A\\0000",
            IsFtdiDevice = true,
            FtdiInfo = FtdiDeviceInfo.ParseFromDeviceId("FTDIBUS\\VID_0403+PID_6001+AG0JU7O1A\\0000")
        };

        // Act - Test with development configuration (permissive)
        var devConfig = PortValidationConfiguration.CreateDevelopmentDefault();
        var result = await _validator.ValidatePortAsync(portInfo, devConfig);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsValid); // Your FT232R should be valid in dev mode
        Assert.True(result.ValidationScore > 70); // Should have good score
        Assert.Contains("FTDI", result.Reason); // Should mention FTDI detected
    }

    [Fact]
    public async Task ValidatePortAsync_WithNonFtdiDevice_FailsWhenFtdiRequired()
    {
        // Arrange - Non-FTDI device
        var portInfo = new SerialPortInfo
        {
            PortName = "COM1",
            FriendlyName = "Communications Port (COM1)",
            Status = PortStatus.Available,
            DeviceId = "ACPI\\PNP0501\\1",
            IsFtdiDevice = false,
            FtdiInfo = null
        };

        // Act
        var result = await _validator.ValidatePortAsync(portInfo);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsValid);
        Assert.Contains("FTDI", result.Reason);
        Assert.Contains(ValidationCriteria.NotFtdiDevice, result.FailedCriteria);
    }

    [Fact]
    public async Task GetValidPortsAsync_FiltersPortsCorrectly()
    {
        // Arrange
        var ports = new List<SerialPortInfo>
        {
            new SerialPortInfo
            {
                PortName = "COM6",
                IsFtdiDevice = true,
                FtdiInfo = FtdiDeviceInfo.ParseFromDeviceId("FTDIBUS\\VID_0403+PID_6001+AG0JU7O1A\\0000"),
                Status = PortStatus.Available
            },
            new SerialPortInfo
            {
                PortName = "COM1",
                IsFtdiDevice = false,
                Status = PortStatus.Available
            }
        };

        // Act - Use dev configuration (permissive)
        var devConfig = PortValidationConfiguration.CreateDevelopmentDefault();
        var validPorts = await _validator.GetValidPortsAsync(ports, devConfig);

        // Assert
        var validPortsList = validPorts.ToList();
        Assert.Single(validPortsList); // Only COM6 should be valid
        Assert.Equal("COM6", validPortsList[0].PortName);
        Assert.True(validPortsList[0].IsValidForPool);
    }

    [Fact]
    public void GetConfiguration_ReturnsCurrentConfiguration()
    {
        // Act
        var config = _validator.GetConfiguration();
        
        // Assert
        Assert.NotNull(config);
        Assert.True(config.RequireFtdiDevice); // Default should require FTDI
    }

    [Fact]
    public void SetConfiguration_UpdatesConfiguration()
    {
        // Arrange
        var newConfig = PortValidationConfiguration.CreateDevelopmentDefault();
        
        // Act
        _validator.SetConfiguration(newConfig);
        var retrievedConfig = _validator.GetConfiguration();
        
        // Assert
        Assert.Equal(newConfig.RequireFtdiDevice, retrievedConfig.RequireFtdiDevice);
        Assert.Equal(newConfig.Require4232HChip, retrievedConfig.Require4232HChip);
    }
}