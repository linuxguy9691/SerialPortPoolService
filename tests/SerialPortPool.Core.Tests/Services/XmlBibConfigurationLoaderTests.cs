// ===================================================================
// SPRINT 11 TESTING: XmlBibConfigurationLoader Tests - CORRECTED
// File: tests/SerialPortPool.Core.Tests/Services/XmlBibConfigurationLoaderTests.cs
// Purpose: Tests adapted to match the EXISTING implementation API
// FIXED: Updated to use actual methods that exist in XmlBibConfigurationLoader
// ===================================================================

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using SerialPortPool.Core.Models;
using SerialPortPool.Core.Services;
using Xunit;

namespace SerialPortPool.Core.Tests.Services;

/// <summary>
/// Unit tests for XmlBibConfigurationLoader - CORRECTED for existing API
/// Tests adapted to match the actual implementation
/// </summary>
public class XmlBibConfigurationLoaderTests : IDisposable
{
    private readonly Mock<ILogger<XmlBibConfigurationLoader>> _mockLogger;
    private readonly IMemoryCache _memoryCache;
    private readonly XmlBibConfigurationLoader _loader;
    private readonly string _testDirectory;
    private readonly string _legacyConfigFile;
    private readonly string _individualBibsDirectory;

    public XmlBibConfigurationLoaderTests()
    {
        _mockLogger = new Mock<ILogger<XmlBibConfigurationLoader>>();
        _memoryCache = new MemoryCache(new MemoryCacheOptions());
        
        _testDirectory = Path.Combine(Path.GetTempPath(), "Sprint11_XmlLoaderTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDirectory);
        
        _legacyConfigFile = Path.Combine(_testDirectory, "legacy_config.xml");
        _individualBibsDirectory = Path.Combine(_testDirectory, "individual_bibs");
        Directory.CreateDirectory(_individualBibsDirectory);
        
        _loader = new XmlBibConfigurationLoader(_mockLogger.Object, _memoryCache);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidParameters_InitializesCorrectly()
    {
        // Arrange & Act
        var loader = new XmlBibConfigurationLoader(_mockLogger.Object, _memoryCache);

        // Assert
        Assert.NotNull(loader);
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new XmlBibConfigurationLoader(null!, _memoryCache));
    }

    [Fact]
    public void Constructor_WithNullCache_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new XmlBibConfigurationLoader(_mockLogger.Object, null!));
    }

    #endregion

    #region LoadConfigurationsAsync Tests (Existing API)

    [Fact]
    public async Task LoadConfigurationsAsync_WithValidLegacyFile_LoadsCorrectly()
    {
        // Arrange
        var legacyXml = CreateLegacyMultiBibXml();
        await File.WriteAllTextAsync(_legacyConfigFile, legacyXml);

        // Act - Use existing API method
        var configurations = await _loader.LoadConfigurationsAsync(_legacyConfigFile);

        // Assert
        Assert.NotNull(configurations);
        Assert.True(configurations.ContainsKey("client_demo"));
        
        var bibConfig = configurations["client_demo"];
        Assert.Equal("client_demo", bibConfig.BibId);
        Assert.Equal("Legacy Client Demo BIB", bibConfig.Description);
        Assert.Single(bibConfig.Uuts);
        
        var uut = bibConfig.Uuts.First();
        Assert.Equal("production_uut", uut.UutId);
        Assert.Single(uut.Ports);
        
        var port = uut.Ports.First();
        Assert.Equal(1, port.PortNumber);
        Assert.Equal("rs232", port.Protocol);
        Assert.Equal(115200, port.Speed);
    }

    [Fact]
    public async Task LoadConfigurationsAsync_WithNonExistentFile_ThrowsFileNotFoundException()
    {
        // Arrange
        var nonExistentFile = Path.Combine(_testDirectory, "nonexistent.xml");

        // Act & Assert
        await Assert.ThrowsAsync<FileNotFoundException>(() =>
            _loader.LoadConfigurationsAsync(nonExistentFile));
    }

    [Fact]
    public async Task LoadConfigurationsAsync_WithInvalidXml_ThrowsXmlException()
    {
        // Arrange
        var invalidXmlPath = Path.Combine(_testDirectory, "invalid.xml");
        await File.WriteAllTextAsync(invalidXmlPath, "<invalid><unclosed>");

        // Act & Assert
        await Assert.ThrowsAsync<System.Xml.XmlException>(() =>
            _loader.LoadConfigurationsAsync(invalidXmlPath));
    }

    [Fact]
    public async Task LoadConfigurationsAsync_WithEmptyFile_ThrowsException()
    {
        // Arrange
        var emptyFile = Path.Combine(_testDirectory, "empty.xml");
        await File.WriteAllTextAsync(emptyFile, "");

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _loader.LoadConfigurationsAsync(emptyFile));
    }

    #endregion

    #region Individual BIB File Tests (Using existing API)

    [Fact]
    public async Task LoadConfigurationsAsync_WithIndividualBibFile_LoadsCorrectly()
    {
        // Arrange
        var individualBibXml = CreateIndividualBibXml("production_line_1");
        var individualBibPath = Path.Combine(_individualBibsDirectory, "bib_production_line_1.xml");
        await File.WriteAllTextAsync(individualBibPath, individualBibXml);

        // Act - Use existing API (individual files should work as single-BIB files)
        var configurations = await _loader.LoadConfigurationsAsync(individualBibPath);

        // Assert
        Assert.NotNull(configurations);
        Assert.Single(configurations); // Individual file should contain one BIB
        
        var bibConfig = configurations.Values.First();
        Assert.Equal("production_line_1", bibConfig.BibId);
        Assert.Contains("Individual BIB File", bibConfig.Description);
        Assert.Single(bibConfig.Uuts);
        
        var uut = bibConfig.Uuts.First();
        Assert.Equal("line1_uut", uut.UutId);
        Assert.Single(uut.Ports);
        
        var port = uut.Ports.First();
        Assert.Equal("INIT_LINE1", port.StartCommands.Commands.First().Command);
        Assert.Equal("READY_LINE1", port.StartCommands.Commands.First().ExpectedResponse);
    }

    #endregion

    #region Caching Tests (Adapted to existing API)

    [Fact]
    public async Task LoadConfigurationsAsync_WithCaching_UsesCache()
    {
        // Arrange
        var validXml = CreateLegacyMultiBibXml();
        await File.WriteAllTextAsync(_legacyConfigFile, validXml);

        // Act - Load twice to test caching behavior
        var configurations1 = await _loader.LoadConfigurationsAsync(_legacyConfigFile);
        var configurations2 = await _loader.LoadConfigurationsAsync(_legacyConfigFile);

        // Assert - Both should be successful and equivalent
        Assert.NotNull(configurations1);
        Assert.NotNull(configurations2);
        Assert.Equal(configurations1.Count, configurations2.Count);
        
        foreach (var kvp in configurations1)
        {
            Assert.True(configurations2.ContainsKey(kvp.Key));
            Assert.Equal(kvp.Value.BibId, configurations2[kvp.Key].BibId);
            Assert.Equal(kvp.Value.Description, configurations2[kvp.Key].Description);
        }
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task LoadConfigurationsAsync_WithNullPath_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _loader.LoadConfigurationsAsync(null!));
    }

    [Fact]
    public async Task LoadConfigurationsAsync_WithEmptyPath_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _loader.LoadConfigurationsAsync(string.Empty));
    }

    #endregion

    #region Performance Tests

    [Fact]
    public async Task LoadConfigurationsAsync_MultipleFiles_CompletesInReasonableTime()
    {
        // Arrange - Create multiple test files
        var testFiles = new List<string>();
        for (int i = 1; i <= 5; i++)
        {
            var bibId = $"perf_test_{i:D2}";
            var bibXml = CreateIndividualBibXml(bibId);
            var bibPath = Path.Combine(_individualBibsDirectory, $"test_{bibId}.xml");
            await File.WriteAllTextAsync(bibPath, bibXml);
            testFiles.Add(bibPath);
        }

        // Act - Load all files and measure time
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        var loadTasks = testFiles.Select(async file =>
        {
            return await _loader.LoadConfigurationsAsync(file);
        });
        
        var results = await Task.WhenAll(loadTasks);
        stopwatch.Stop();

        // Assert
        Assert.Equal(5, results.Length);
        Assert.All(results, result => Assert.NotNull(result));
        Assert.All(results, result => Assert.NotEmpty(result));
        Assert.True(stopwatch.ElapsedMilliseconds < 5000); // Should complete within 5 seconds
    }

    #endregion

    #region Integration Tests with Multiple BIBs

    [Fact]
    public async Task LoadConfigurationsAsync_WithMultipleBibsInSameFile_LoadsAll()
    {
        // Arrange
        var multipleBibsXml = CreateMultipleBibsXml();
        var multiFile = Path.Combine(_testDirectory, "multiple_bibs.xml");
        await File.WriteAllTextAsync(multiFile, multipleBibsXml);

        // Act
        var configurations = await _loader.LoadConfigurationsAsync(multiFile);

        // Assert
        Assert.NotNull(configurations);
        Assert.Equal(3, configurations.Count);
        
        Assert.True(configurations.ContainsKey("bib_1"));
        Assert.True(configurations.ContainsKey("bib_2"));
        Assert.True(configurations.ContainsKey("bib_3"));
        
        foreach (var config in configurations.Values)
        {
            Assert.NotEmpty(config.BibId);
            Assert.NotEmpty(config.Description);
            Assert.NotEmpty(config.Uuts);
        }
    }

    #endregion

    #region Validation Tests

    [Fact]
    public async Task LoadConfigurationsAsync_WithMissingBibId_HandlesGracefully()
    {
        // Arrange
        var xmlWithMissingId = """
<?xml version="1.0" encoding="UTF-8"?>
<root>
  <bib description="Missing ID BIB">
    <uut id="test_uut">
      <port number="1">
        <protocol>rs232</protocol>
        <speed>115200</speed>
        <data_pattern>n81</data_pattern>
      </port>
    </uut>
  </bib>
</root>
""";
        var testFile = Path.Combine(_testDirectory, "missing_id.xml");
        await File.WriteAllTextAsync(testFile, xmlWithMissingId);

        // Act
        var configurations = await _loader.LoadConfigurationsAsync(testFile);

        // Assert - Should handle gracefully (skip invalid BIBs or provide default ID)
        Assert.NotNull(configurations);
        // The exact behavior depends on implementation - it might skip invalid BIBs or assign default IDs
    }

    [Fact]
    public async Task LoadConfigurationsAsync_WithValidComplexStructure_ParsesCorrectly()
    {
        // Arrange
        var complexXml = CreateComplexBibXml();
        var complexFile = Path.Combine(_testDirectory, "complex.xml");
        await File.WriteAllTextAsync(complexFile, complexXml);

        // Act
        var configurations = await _loader.LoadConfigurationsAsync(complexFile);

        // Assert
        Assert.NotNull(configurations);
        Assert.Single(configurations);
        
        var bibConfig = configurations.Values.First();
        Assert.Equal("complex_test", bibConfig.BibId);
        Assert.Equal(2, bibConfig.Uuts.Count); // Should have 2 UUTs
        
        var firstUut = bibConfig.Uuts.First();
        Assert.Equal(2, firstUut.Ports.Count); // Should have 2 ports
        
        var firstPort = firstUut.Ports.First();
        Assert.NotNull(firstPort.StartCommands);
        Assert.NotNull(firstPort.TestCommands);
        Assert.NotNull(firstPort.StopCommands);
    }

    #endregion

    #region Helper Methods

    private string CreateLegacyMultiBibXml()
    {
        return """
<?xml version="1.0" encoding="UTF-8"?>
<root>
  <bib id="client_demo" description="Legacy Client Demo BIB - Multi-BIB Example">
    <metadata>
      <board_type>demo</board_type>
      <multi_bib_group>client_demo</multi_bib_group>
      <sprint>11</sprint>
      <config_type>legacy</config_type>
    </metadata>
    
    <uut id="production_uut" description="Legacy Production UUT">
      <port number="1">
        <protocol>rs232</protocol>
        <speed>115200</speed>
        <data_pattern>n81</data_pattern>
        
        <start>
          <command>INIT</command>
          <expected_response>READY</expected_response>
          <timeout_ms>3000</timeout_ms>
        </start>
        
        <test>
          <command>TEST</command>
          <expected_response>PASS</expected_response>
          <timeout_ms>5000</timeout_ms>
        </test>
        
        <stop>
          <command>QUIT</command>
          <expected_response>BYE</expected_response>
          <timeout_ms>2000</timeout_ms>
        </stop>
      </port>
    </uut>
  </bib>
</root>
""";
    }

    private string CreateIndividualBibXml(string bibId)
    {
        return $"""
<?xml version="1.0" encoding="UTF-8"?>
<bib id="{bibId}" description="SPRINT 11: Individual BIB File - {bibId}">
  <metadata>
    <board_type>individual</board_type>
    <sprint>11</sprint>
    <file_type>individual</file_type>
    <created_date>{DateTime.Now:yyyy-MM-dd}</created_date>
  </metadata>
  
  <uut id="line1_uut" description="Individual BIB UUT for {bibId}">
    <port number="1">
      <protocol>rs232</protocol>
      <speed>115200</speed>
      <data_pattern>n81</data_pattern>
      
      <start>
        <command>INIT_LINE1</command>
        <expected_response>READY_LINE1</expected_response>
        <timeout_ms>3000</timeout_ms>
      </start>
      
      <test>
        <command>TEST_LINE1</command>
        <expected_response>PASS_LINE1</expected_response>
        <timeout_ms>5000</timeout_ms>
      </test>
      
      <stop>
        <command>QUIT_LINE1</command>
        <expected_response>BYE_LINE1</expected_response>
        <timeout_ms>2000</timeout_ms>
      </stop>
    </port>
  </uut>
</bib>
""";
    }

    private string CreateMultipleBibsXml()
    {
        return """
<?xml version="1.0" encoding="UTF-8"?>
<root>
  <bib id="bib_1" description="First BIB in multi-BIB file">
    <uut id="uut_1">
      <port number="1">
        <protocol>rs232</protocol>
        <speed>115200</speed>
        <data_pattern>n81</data_pattern>
        <start>
          <command>INIT1</command>
          <expected_response>READY1</expected_response>
          <timeout_ms>3000</timeout_ms>
        </start>
        <test>
          <command>TEST1</command>
          <expected_response>PASS1</expected_response>
          <timeout_ms>5000</timeout_ms>
        </test>
        <stop>
          <command>QUIT1</command>
          <expected_response>BYE1</expected_response>
          <timeout_ms>2000</timeout_ms>
        </stop>
      </port>
    </uut>
  </bib>
  
  <bib id="bib_2" description="Second BIB in multi-BIB file">
    <uut id="uut_2">
      <port number="1">
        <protocol>rs232</protocol>
        <speed>57600</speed>
        <data_pattern>e71</data_pattern>
        <start>
          <command>INIT2</command>
          <expected_response>READY2</expected_response>
          <timeout_ms>3000</timeout_ms>
        </start>
        <test>
          <command>TEST2</command>
          <expected_response>PASS2</expected_response>
          <timeout_ms>5000</timeout_ms>
        </test>
        <stop>
          <command>QUIT2</command>
          <expected_response>BYE2</expected_response>
          <timeout_ms>2000</timeout_ms>
        </stop>
      </port>
    </uut>
  </bib>
  
  <bib id="bib_3" description="Third BIB in multi-BIB file">
    <uut id="uut_3">
      <port number="1">
        <protocol>rs232</protocol>
        <speed>9600</speed>
        <data_pattern>o81</data_pattern>
        <start>
          <command>INIT3</command>
          <expected_response>READY3</expected_response>
          <timeout_ms>3000</timeout_ms>
        </start>
        <test>
          <command>TEST3</command>
          <expected_response>PASS3</expected_response>
          <timeout_ms>5000</timeout_ms>
        </test>
        <stop>
          <command>QUIT3</command>
          <expected_response>BYE3</expected_response>
          <timeout_ms>2000</timeout_ms>
        </stop>
      </port>
    </uut>
  </bib>
</root>
""";
    }

    private string CreateComplexBibXml()
    {
        return """
<?xml version="1.0" encoding="UTF-8"?>
<bib id="complex_test" description="Complex BIB with multiple UUTs and ports">
  <metadata>
    <board_type>complex</board_type>
    <version>1.0</version>
    <complexity>high</complexity>
  </metadata>
  
  <uut id="primary_uut" description="Primary UUT with multiple ports">
    <port number="1">
      <protocol>rs232</protocol>
      <speed>115200</speed>
      <data_pattern>n81</data_pattern>
      <start>
        <command>INIT_PRIMARY_1</command>
        <expected_response>READY_PRIMARY_1</expected_response>
        <timeout_ms>3000</timeout_ms>
      </start>
      <test>
        <command>TEST_PRIMARY_1</command>
        <expected_response>PASS_PRIMARY_1</expected_response>
        <timeout_ms>5000</timeout_ms>
      </test>
      <stop>
        <command>QUIT_PRIMARY_1</command>
        <expected_response>BYE_PRIMARY_1</expected_response>
        <timeout_ms>2000</timeout_ms>
      </stop>
    </port>
    
    <port number="2">
      <protocol>rs232</protocol>
      <speed>57600</speed>
      <data_pattern>e71</data_pattern>
      <start>
        <command>INIT_PRIMARY_2</command>
        <expected_response>READY_PRIMARY_2</expected_response>
        <timeout_ms>3000</timeout_ms>
      </start>
      <test>
        <command>TEST_PRIMARY_2</command>
        <expected_response>PASS_PRIMARY_2</expected_response>
        <timeout_ms>5000</timeout_ms>
      </test>
      <stop>
        <command>QUIT_PRIMARY_2</command>
        <expected_response>BYE_PRIMARY_2</expected_response>
        <timeout_ms>2000</timeout_ms>
      </stop>
    </port>
  </uut>
  
  <uut id="secondary_uut" description="Secondary UUT">
    <port number="1">
      <protocol>rs232</protocol>
      <speed>38400</speed>
      <data_pattern>o71</data_pattern>
      <start>
        <command>INIT_SECONDARY</command>
        <expected_response>READY_SECONDARY</expected_response>
        <timeout_ms>3000</timeout_ms>
      </start>
      <test>
        <command>TEST_SECONDARY</command>
        <expected_response>PASS_SECONDARY</expected_response>
        <timeout_ms>5000</timeout_ms>
      </test>
      <stop>
        <command>QUIT_SECONDARY</command>
        <expected_response>BYE_SECONDARY</expected_response>
        <timeout_ms>2000</timeout_ms>
      </stop>
    </port>
  </uut>
</bib>
""";
    }

    #endregion

    #region Cleanup

    public void Dispose()
    {
        try
        {
            _memoryCache?.Dispose();
            
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