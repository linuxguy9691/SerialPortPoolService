// ===================================================================
// SPRINT 11 TESTING: ConfigurationValidator Unit Tests
// File: tests/SerialPortPool.Core.Tests/Services/ConfigurationValidatorTests.cs
// Purpose: Comprehensive tests for XML schema + business rules validation
// ===================================================================

using Microsoft.Extensions.Logging;
using Moq;
using SerialPortPool.Core.Models;
using SerialPortPool.Core.Services;
using Xunit;

namespace SerialPortPool.Core.Tests.Services;

/// <summary>
/// Unit tests for ConfigurationValidator (Sprint 11)
/// Tests XML schema validation, business rules, and error reporting
/// </summary>
public class ConfigurationValidatorTests : IDisposable
{
    private readonly Mock<ILogger<ConfigurationValidator>> _mockLogger;
    private readonly ConfigurationValidator _validator;
    private readonly string _testDirectory;

    public ConfigurationValidatorTests()
    {
        _mockLogger = new Mock<ILogger<ConfigurationValidator>>();
        _validator = new ConfigurationValidator(_mockLogger.Object);
        
        _testDirectory = Path.Combine(Path.GetTempPath(), "Sprint11_ValidatorTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDirectory);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidLogger_InitializesCorrectly()
    {
        // Arrange & Act
        var validator = new ConfigurationValidator(_mockLogger.Object);

        // Assert
        Assert.NotNull(validator);
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new ConfigurationValidator(null!));
    }

    #endregion

    #region ValidateXmlSchema Tests

    [Fact]
    public async Task ValidateXmlSchema_WithValidBibXml_ReturnsSuccess()
    {
        // Arrange
        var validXml = CreateValidBibXml("valid_schema_test");
        var xmlPath = Path.Combine(_testDirectory, "valid.xml");
        await File.WriteAllTextAsync(xmlPath, validXml);

        // Act
        var result = await _validator.ValidateXmlSchemaAsync(xmlPath);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
        Assert.Contains("Schema validation", result.GetSummary());
    }

    [Fact]
    public async Task ValidateXmlSchema_WithInvalidXml_ReturnsErrors()
    {
        // Arrange
        var invalidXml = """
<?xml version="1.0" encoding="UTF-8"?>
<bib id="invalid_test">
  <invalid_element>
    <unclosed_tag>
  </invalid_element>
</bib>
""";
        var xmlPath = Path.Combine(_testDirectory, "invalid.xml");
        await File.WriteAllTextAsync(xmlPath, invalidXml);

        // Act
        var result = await _validator.ValidateXmlSchemaAsync(xmlPath);

        // Assert
        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public async Task ValidateXmlSchema_WithMissingRequiredElements_ReturnsErrors()
    {
        // Arrange - Missing required BIB id attribute
        var xmlWithMissingId = """
<?xml version="1.0" encoding="UTF-8"?>
<bib description="Missing ID test">
  <uut id="test_uut">
    <port number="1">
      <protocol>rs232</protocol>
    </port>
  </uut>
</bib>
""";
        var xmlPath = Path.Combine(_testDirectory, "missing_id.xml");
        await File.WriteAllTextAsync(xmlPath, xmlWithMissingId);

        // Act
        var result = await _validator.ValidateXmlSchemaAsync(xmlPath);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Contains("id"));
    }

    [Fact]
    public async Task ValidateXmlSchema_WithNonExistentFile_ReturnsFileError()
    {
        // Arrange
        var nonExistentPath = Path.Combine(_testDirectory, "nonexistent.xml");

        // Act
        var result = await _validator.ValidateXmlSchemaAsync(nonExistentPath);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Contains("not found") || error.Contains("does not exist"));
    }

    #endregion

    #region ValidateBusinessRules Tests

    [Fact]
    public async Task ValidateBusinessRules_WithValidConfiguration_ReturnsSuccess()
    {
        // Arrange
        var validBibConfig = CreateValidBibConfiguration("business_rules_test");

        // Act
        var result = await _validator.ValidateBusinessRulesAsync(validBibConfig);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
        Assert.Contains("Business rules validation", result.GetSummary());
    }

    [Fact]
    public async Task ValidateBusinessRules_WithEmptyBibId_ReturnsError()
    {
        // Arrange
        var bibConfig = CreateValidBibConfiguration("test");
        bibConfig.BibId = string.Empty; // Invalid empty BIB ID

        // Act
        var result = await _validator.ValidateBusinessRulesAsync(bibConfig);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Contains("BIB ID"));
    }

    [Fact]
    public async Task ValidateBusinessRules_WithInvalidPortNumber_ReturnsError()
    {
        // Arrange
        var bibConfig = CreateValidBibConfiguration("port_test");
        bibConfig.Uuts.First().Ports.First().PortNumber = 0; // Invalid port number

        // Act
        var result = await _validator.ValidateBusinessRulesAsync(bibConfig);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Contains("port number") || error.Contains("positive"));
    }

    [Fact]
    public async Task ValidateBusinessRules_WithUnsupportedProtocol_ReturnsError()
    {
        // Arrange
        var bibConfig = CreateValidBibConfiguration("protocol_test");
        bibConfig.Uuts.First().Ports.First().Protocol = "unsupported_protocol";

        // Act
        var result = await _validator.ValidateBusinessRulesAsync(bibConfig);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => 
            error.Contains("protocol") || error.Contains("unsupported"));
    }

    [Fact]
    public async Task ValidateBusinessRules_WithInvalidBaudRate_ReturnsWarning()
    {
        // Arrange
        var bibConfig = CreateValidBibConfiguration("baud_test");
        bibConfig.Uuts.First().Ports.First().Speed = 12345; // Unusual baud rate

        // Act
        var result = await _validator.ValidateBusinessRulesAsync(bibConfig);

        // Assert
        Assert.True(result.IsValid); // Should be valid but with warning
        Assert.NotEmpty(result.Warnings);
        Assert.Contains(result.Warnings, warning => 
            warning.Contains("baud") || warning.Contains("unusual"));
    }

    [Fact]
    public async Task ValidateBusinessRules_WithInvalidDataPattern_ReturnsError()
    {
        // Arrange
        var bibConfig = CreateValidBibConfiguration("data_pattern_test");
        bibConfig.Uuts.First().Ports.First().DataPattern = "invalid"; // Invalid data pattern

        // Act
        var result = await _validator.ValidateBusinessRulesAsync(bibConfig);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Contains("data pattern"));
    }

    [Fact]
    public async Task ValidateBusinessRules_WithDuplicateUutIds_ReturnsError()
    {
        // Arrange
        var bibConfig = CreateValidBibConfiguration("duplicate_test");
        
        // Add duplicate UUT
        var duplicateUut = new UutConfiguration
        {
            UutId = bibConfig.Uuts.First().UutId, // Same ID as first UUT
            Description = "Duplicate UUT",
            Ports = new List<PortConfiguration>
            {
                new() { PortNumber = 2, Protocol = "rs232", Speed = 115200, DataPattern = "n81" }
            }
        };
        bibConfig.Uuts.Add(duplicateUut);

        // Act
        var result = await _validator.ValidateBusinessRulesAsync(bibConfig);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => 
            error.Contains("duplicate") || error.Contains("UUT ID"));
    }

    [Fact]
    public async Task ValidateBusinessRules_WithDuplicatePortNumbers_ReturnsError()
    {
        // Arrange
        var bibConfig = CreateValidBibConfiguration("duplicate_port_test");
        var uut = bibConfig.Uuts.First();
        
        // Add duplicate port number
        var duplicatePort = new PortConfiguration
        {
            PortNumber = uut.Ports.First().PortNumber, // Same port number
            Protocol = "rs232",
            Speed = 115200,
            DataPattern = "n81"
        };
        uut.Ports.Add(duplicatePort);

        // Act
        var result = await _validator.ValidateBusinessRulesAsync(bibConfig);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => 
            error.Contains("duplicate") || error.Contains("port number"));
    }

    [Fact]
    public async Task ValidateBusinessRules_WithEmptyCommandSequence_ReturnsWarning()
    {
        // Arrange
        var bibConfig = CreateValidBibConfiguration("empty_commands_test");
        var port = bibConfig.Uuts.First().Ports.First();
        port.StartCommands.Commands.Clear(); // Empty start commands

        // Act
        var result = await _validator.ValidateBusinessRulesAsync(bibConfig);

        // Assert
        Assert.True(result.IsValid); // Valid but with warning
        Assert.NotEmpty(result.Warnings);
        Assert.Contains(result.Warnings, warning => 
            warning.Contains("empty") || warning.Contains("command"));
    }

    #endregion

    #region Comprehensive Validation Tests

    [Fact]
    public async Task ValidateAsync_WithValidXmlFile_ReturnsSuccess()
    {
        // Arrange
        var validXml = CreateValidBibXml("comprehensive_valid_test");
        var xmlPath = Path.Combine(_testDirectory, "comprehensive_valid.xml");
        await File.WriteAllTextAsync(xmlPath, validXml);

        // Act
        var result = await _validator.ValidateAsync(xmlPath);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
        Assert.Contains("validation successful", result.GetSummary());
    }

    [Fact]
    public async Task ValidateAsync_WithBothSchemaAndBusinessErrors_ReturnsCombinedErrors()
    {
        // Arrange - Create XML with both schema and business rule violations
        var invalidXml = """
<?xml version="1.0" encoding="UTF-8"?>
<bib description="Missing ID and invalid content">
  <uut id="">
    <port number="-1">
      <protocol>invalid_protocol</protocol>
      <speed>invalid_speed</speed>
    </port>
  </uut>
</bib>
""";
        var xmlPath = Path.Combine(_testDirectory, "combined_errors.xml");
        await File.WriteAllTextAsync(xmlPath, invalidXml);

        // Act
        var result = await _validator.ValidateAsync(xmlPath);

        // Assert
        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Errors);
        
        // Should have errors from both schema and business rule validation
        var errorText = string.Join(" ", result.Errors);
        Assert.True(result.Errors.Count > 1, "Should have multiple errors from different validation types");
    }

    [Fact]
    public async Task ValidateAsync_WithConfiguration_ValidatesBusinessRulesOnly()
    {
        // Arrange
        var bibConfig = CreateValidBibConfiguration("config_only_test");

        // Act
        var result = await _validator.ValidateAsync(bibConfig);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
        Assert.Contains("Business rules validation", result.GetSummary());
    }

    #endregion

    #region Performance and Edge Case Tests

    [Fact]
    public async Task ValidateAsync_WithLargeConfiguration_CompletesInReasonableTime()
    {
        // Arrange - Create large configuration with many UUTs and ports
        var largeBibConfig = CreateLargeBibConfiguration("performance_test", 20, 5);

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var result = await _validator.ValidateAsync(largeBibConfig);
        stopwatch.Stop();

        // Assert
        Assert.True(result.IsValid);
        Assert.True(stopwatch.ElapsedMilliseconds < 3000); // Should complete within 3 seconds
    }

    [Fact]
    public async Task ValidateAsync_WithNullConfiguration_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _validator.ValidateAsync((BibConfiguration)null!));
    }

    [Fact]
    public async Task ValidateAsync_WithNullFilePath_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _validator.ValidateAsync((string)null!));
    }

    [Fact]
    public async Task ValidateAsync_WithEmptyFilePath_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _validator.ValidateAsync(string.Empty));
    }

    [Fact]
    public async Task ValidateBusinessRules_WithComplexConfiguration_ValidatesAllRules()
    {
        // Arrange - Complex configuration with multiple UUTs, ports, and command sequences
        var complexConfig = CreateComplexBibConfiguration("complex_validation_test");

        // Act
        var result = await _validator.ValidateBusinessRulesAsync(complexConfig);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
        
        // Verify it actually validated all components
        Assert.Equal(3, complexConfig.Uuts.Count);
        Assert.Equal(6, complexConfig.GetAllPorts().Count); // 2 ports per UUT
    }

    #endregion

    #region Validation Result Tests

    [Fact]
    public void BibValidationResult_GetSummary_FormatsCorrectly()
    {
        // Arrange
        var result = new BibValidationResult();
        result.AddError("Test error message");
        result.AddWarning("Test warning message");

        // Act
        var summary = result.GetSummary();

        // Assert
        Assert.Contains("1 error", summary);
        Assert.Contains("1 warning", summary);
        Assert.Contains("❌", summary); // Error icon
    }

    [Fact]
    public void BibValidationResult_WithOnlyWarnings_IsValid()
    {
        // Arrange
        var result = new BibValidationResult();
        result.AddWarning("Warning message");

        // Act & Assert
        Assert.True(result.IsValid);
        Assert.True(result.HasWarnings);
        Assert.Contains("⚠️", result.GetSummary()); // Warning icon
    }

    [Fact]
    public void BibValidationResult_WithNoIssues_ShowsSuccess()
    {
        // Arrange
        var result = new BibValidationResult();

        // Act
        var summary = result.GetSummary();

        // Assert
        Assert.True(result.IsValid);
        Assert.False(result.HasWarnings);
        Assert.Contains("✅", summary); // Success icon
        Assert.Contains("valid", summary);
    }

    #endregion

    #region Helper Methods

    private string CreateValidBibXml(string bibId)
    {
        return $"""
<?xml version="1.0" encoding="UTF-8"?>
<bib id="{bibId}" description="Valid BIB for testing - {bibId}">
  <metadata>
    <board_type>test</board_type>
    <sprint>11</sprint>
    <validation_test>true</validation_test>
  </metadata>
  
  <uut id="test_uut_{bibId}" description="Test UUT for {bibId}">
    <port number="1">
      <protocol>rs232</protocol>
      <speed>115200</speed>
      <data_pattern>n81</data_pattern>
      <read_timeout>3000</read_timeout>
      <write_timeout>3000</write_timeout>
      
      <start>
        <command>INIT_{bibId}</command>
        <expected_response>READY_{bibId}</expected_response>
        <timeout_ms>3000</timeout_ms>
      </start>
      
      <test>
        <command>TEST_{bibId}</command>
        <expected_response>PASS_{bibId}</expected_response>
        <timeout_ms>5000</timeout_ms>
      </test>
      
      <stop>
        <command>QUIT_{bibId}</command>
        <expected_response>BYE_{bibId}</expected_response>
        <timeout_ms>2000</timeout_ms>
      </stop>
    </port>
  </uut>
</bib>
""";
    }

    private BibConfiguration CreateValidBibConfiguration(string bibId)
    {
        return new BibConfiguration
        {
            BibId = bibId,
            Description = $"Valid test BIB configuration - {bibId}",
            Uuts = new List<UutConfiguration>
            {
                new()
                {
                    UutId = $"test_uut_{bibId}",
                    Description = $"Test UUT for {bibId}",
                    Ports = new List<PortConfiguration>
                    {
                        new()
                        {
                            PortNumber = 1,
                            Protocol = "rs232",
                            Speed = 115200,
                            DataPattern = "n81",
                            StartCommands = new CommandSequence
                            {
                                Commands = new List<ProtocolCommand>
                                {
                                    new() { Command = $"INIT_{bibId}", ExpectedResponse = $"READY_{bibId}", TimeoutMs = 3000 }
                                }
                            },
                            TestCommands = new CommandSequence
                            {
                                Commands = new List<ProtocolCommand>
                                {
                                    new() { Command = $"TEST_{bibId}", ExpectedResponse = $"PASS_{bibId}", TimeoutMs = 5000 }
                                }
                            },
                            StopCommands = new CommandSequence
                            {
                                Commands = new List<ProtocolCommand>
                                {
                                    new() { Command = $"QUIT_{bibId}", ExpectedResponse = $"BYE_{bibId}", TimeoutMs = 2000 }
                                }
                            }
                        }
                    }
                }
            },
            Metadata = new Dictionary<string, string>
            {
                ["board_type"] = "test",
                ["sprint"] = "11",
                ["validation_test"] = "true"
            }
        };
    }

    private BibConfiguration CreateLargeBibConfiguration(string bibId, int uutCount, int portsPerUut)
    {
        var bibConfig = new BibConfiguration
        {
            BibId = bibId,
            Description = $"Large test BIB configuration - {bibId}",
            Uuts = new List<UutConfiguration>(),
            Metadata = new Dictionary<string, string>
            {
                ["board_type"] = "performance_test",
                ["uut_count"] = uutCount.ToString(),
                ["ports_per_uut"] = portsPerUut.ToString()
            }
        };

        for (int uutIndex = 1; uutIndex <= uutCount; uutIndex++)
        {
            var uut = new UutConfiguration
            {
                UutId = $"uut_{uutIndex:D3}",
                Description = $"Performance test UUT {uutIndex}",
                Ports = new List<PortConfiguration>()
            };

            for (int portIndex = 1; portIndex <= portsPerUut; portIndex++)
            {
                uut.Ports.Add(new PortConfiguration
                {
                    PortNumber = portIndex,
                    Protocol = "rs232",
                    Speed = 115200,
                    DataPattern = "n81",
                    StartCommands = new CommandSequence
                    {
                        Commands = new List<ProtocolCommand>
                        {
                            new() { Command = $"INIT_UUT{uutIndex}_PORT{portIndex}", ExpectedResponse = "READY", TimeoutMs = 3000 }
                        }
                    },
                    TestCommands = new CommandSequence
                    {
                        Commands = new List<ProtocolCommand>
                        {
                            new() { Command = $"TEST_UUT{uutIndex}_PORT{portIndex}", ExpectedResponse = "PASS", TimeoutMs = 5000 }
                        }
                    },
                    StopCommands = new CommandSequence
                    {
                        Commands = new List<ProtocolCommand>
                        {
                            new() { Command = $"QUIT_UUT{uutIndex}_PORT{portIndex}", ExpectedResponse = "BYE", TimeoutMs = 2000 }
                        }
                    }
                });
            }

            bibConfig.Uuts.Add(uut);
        }

        return bibConfig;
    }

    private BibConfiguration CreateComplexBibConfiguration(string bibId)
    {
        return new BibConfiguration
        {
            BibId = bibId,
            Description = $"Complex test BIB configuration - {bibId}",
            Uuts = new List<UutConfiguration>
            {
                new()
                {
                    UutId = "primary_uut",
                    Description = "Primary UUT with multiple ports",
                    Ports = new List<PortConfiguration>
                    {
                        new()
                        {
                            PortNumber = 1,
                            Protocol = "rs232",
                            Speed = 115200,
                            DataPattern = "n81"
                        },
                        new()
                        {
                            PortNumber = 2,
                            Protocol = "rs232",
                            Speed = 57600,
                            DataPattern = "e71"
                        }
                    }
                },
                new()
                {
                    UutId = "secondary_uut",
                    Description = "Secondary UUT",
                    Ports = new List<PortConfiguration>
                    {
                        new()
                        {
                            PortNumber = 1,
                            Protocol = "rs232",
                            Speed = 9600,
                            DataPattern = "n82"
                        },
                        new()
                        {
                            PortNumber = 2,
                            Protocol = "rs232",
                            Speed = 115200,
                            DataPattern = "n81"
                        }
                    }
                },
                new()
                {
                    UutId = "tertiary_uut",
                    Description = "Tertiary UUT",
                    Ports = new List<PortConfiguration>
                    {
                        new()
                        {
                            PortNumber = 1,
                            Protocol = "rs232",
                            Speed = 38400,
                            DataPattern = "o71"
                        },
                        new()
                        {
                            PortNumber = 2,
                            Protocol = "rs232",
                            Speed = 19200,
                            DataPattern = "n81"
                        }
                    }
                }
            },
            Metadata = new Dictionary<string, string>
            {
                ["board_type"] = "complex_test",
                ["sprint"] = "11",
                ["complexity"] = "high"
            }
        };
    }

    #endregion

    #region Cleanup

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_testDirectory))
            {
                Directory.Delete(_testDirectory, true);
            }
        }
        catch
        {
            // Ignore cleanup errors in tests
        }
    }

    #endregion
}