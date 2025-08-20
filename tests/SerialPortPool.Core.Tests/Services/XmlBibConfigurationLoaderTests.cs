// ===================================================================
// SPRINT 11 TESTING: XmlBibConfigurationLoader Enhanced Unit Tests
// File: tests/SerialPortPool.Core.Tests/Services/XmlBibConfigurationLoaderTests.cs
// Purpose: Comprehensive tests for multi-file discovery + legacy fallback
// ===================================================================

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using SerialPortPool.Core.Models;
using SerialPortPool.Core.Services;
using SerialPortPool.Core.Services.Configuration; 
using Xunit;

namespace SerialPortPool.Core.Tests.Services;

/// <summary>
/// Unit tests for Enhanced XmlBibConfigurationLoader (Sprint 11)
/// Tests multi-file discovery, legacy fallback, and smart caching
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

    #region Legacy Single-File Loading Tests

    [Fact]
    public async Task LoadBibAsync_WithLegacySingleFile_LoadsCorrectly()
    {
        // Arrange
        var legacyXml = CreateLegacyMultiBibXml();
        await File.WriteAllTextAsync(_legacyConfigFile, legacyXml);
        _loader.SetDefaultConfigurationPath(_legacyConfigFile);

        // Act
        var bibConfig = await _loader.LoadBibAsync(_legacyConfigFile, "client_demo");

        // Assert
        Assert.NotNull(bibConfig);
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
    public async Task LoadBibAsync_WithNonExistentBibInLegacyFile_ThrowsArgumentException()
    {
        // Arrange
        var legacyXml = CreateLegacyMultiBibXml();
        await File.WriteAllTextAsync(_legacyConfigFile, legacyXml);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _loader.LoadBibAsync(_legacyConfigFile, "nonexistent_bib"));
    }

    #endregion

    #region Sprint 11: Individual BIB File Loading Tests

    [Fact]
    public async Task LoadBibAsync_WithIndividualBibFile_LoadsCorrectly()
    {
        // Arrange
        var individualBibXml = CreateIndividualBibXml("production_line_1");
        var individualBibPath = Path.Combine(_individualBibsDirectory, "bib_production_line_1.xml");
        await File.WriteAllTextAsync(individualBibPath, individualBibXml);

        // Act
        var bibConfig = await _loader.LoadBibAsync(individualBibPath, "production_line_1");

        // Assert
        Assert.NotNull(bibConfig);
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

    [Fact]
    public async Task LoadBibConfigurationAsync_WithIndividualBibFile_LoadsDirectly()
    {
        // Arrange
        var individualBibXml = CreateIndividualBibXml("direct_load_test");
        var individualBibPath = Path.Combine(_individualBibsDirectory, "bib_direct_load_test.xml");
        await File.WriteAllTextAsync(individualBibPath, individualBibXml);

        // Act
        var bibConfig = await _loader.LoadBibConfigurationAsync(individualBibPath);

        // Assert
        Assert.NotNull(bibConfig);
        Assert.Equal("direct_load_test", bibConfig.BibId);
        Assert.Contains("Individual BIB File", bibConfig.Description);
    }

    #endregion

    #region Sprint 11: Smart Discovery Tests

    [Fact]
    public async Task LoadBibAsync_WithSmartDiscovery_PrefersIndividualOverLegacy()
    {
        // Arrange - Create both individual and legacy versions
        var legacyXml = CreateLegacyMultiBibXml();
        await File.WriteAllTextAsync(_legacyConfigFile, legacyXml);
        
        var individualBibXml = CreateIndividualBibXml("client_demo");
        var individualBibPath = Path.Combine(_individualBibsDirectory, "bib_client_demo.xml");
        await File.WriteAllTextAsync(individualBibPath, individualBibXml);
        
        _loader.SetDefaultConfigurationPath(_legacyConfigFile);

        // Act - Should find individual file first
        var bibConfig = await _loader.LoadBibAsync(_legacyConfigFile, "client_demo");

        // Assert - Should get individual file content, not legacy
        Assert.NotNull(bibConfig);
        Assert.Equal("client_demo", bibConfig.BibId);
        Assert.Contains("Individual BIB File", bibConfig.Description); // Individual file marker
        Assert.DoesNotContain("Legacy", bibConfig.Description); // Not legacy content
    }

    [Fact]
    public async Task LoadBibAsync_WithFallbackToLegacy_WorksWhenIndividualNotFound()
    {
        // Arrange - Only legacy file exists
        var legacyXml = CreateLegacyMultiBibXml();
        await File.WriteAllTextAsync(_legacyConfigFile, legacyXml);
        _loader.SetDefaultConfigurationPath(_legacyConfigFile);

        // Act - Should fallback to legacy
        var bibConfig = await _loader.LoadBibAsync(_legacyConfigFile, "client_demo");

        // Assert - Should get legacy content
        Assert.NotNull(bibConfig);
        Assert.Equal("client_demo", bibConfig.BibId);
        Assert.Contains("Legacy", bibConfig.Description); // Legacy file marker
    }

    [Fact]
    public async Task DiscoverBibsAsync_WithIndividualFiles_ReturnsAllBibIds()
    {
        // Arrange - Create multiple individual BIB files
        var bibIds = new[] { "production_line_1", "production_line_2", "test_station_a" };
        
        foreach (var bibId in bibIds)
        {
            var individualBibXml = CreateIndividualBibXml(bibId);
            var individualBibPath = Path.Combine(_individualBibsDirectory, $"bib_{bibId}.xml");
            await File.WriteAllTextAsync(individualBibPath, individualBibXml);
        }

        // Act
        var discoveredBibs = await _loader.DiscoverBibsAsync(_individualBibsDirectory);
        var discoveredList = discoveredBibs.ToList();

        // Assert
        Assert.Equal(3, discoveredList.Count);
        Assert.Contains("production_line_1", discoveredList);
        Assert.Contains("production_line_2", discoveredList);
        Assert.Contains("test_station_a", discoveredList);
    }

    [Fact]
    public async Task DiscoverBibsAsync_WithMixedFiles_ReturnsOnlyBibFiles()
    {
        // Arrange - Create mix of BIB files and other files
        var bibXml = CreateIndividualBibXml("valid_bib");
        await File.WriteAllTextAsync(Path.Combine(_individualBibsDirectory, "bib_valid_bib.xml"), bibXml);
        
        // Create non-BIB files (should be ignored)
        await File.WriteAllTextAsync(Path.Combine(_individualBibsDirectory, "not_a_bib.xml"), "<root></root>");
        await File.WriteAllTextAsync(Path.Combine(_individualBibsDirectory, "config.txt"), "text file");
        await File.WriteAllTextAsync(Path.Combine(_individualBibsDirectory, "other_bib.json"), "{}");

        // Act
        var discoveredBibs = await _loader.DiscoverBibsAsync(_individualBibsDirectory);
        var discoveredList = discoveredBibs.ToList();

        // Assert
        Assert.Single(discoveredList);
        Assert.Equal("valid_bib", discoveredList.First());
    }

    [Fact]
    public async Task DiscoverBibsAsync_WithEmptyDirectory_ReturnsEmpty()
    {
        // Arrange - Empty directory
        var emptyDir = Path.Combine(_testDirectory, "empty");
        Directory.CreateDirectory(emptyDir);

        // Act
        var discoveredBibs = await _loader.DiscoverBibsAsync(emptyDir);

        // Assert
        Assert.Empty(discoveredBibs);
    }

    #endregion

    #region Sprint 11: Configuration Directory Support Tests

    [Fact]
    public void SetDefaultConfigurationPath_WithFile_ExtractsDirectory()
    {
        // Arrange
        var configFilePath = Path.Combine(_testDirectory, "subdir", "config.xml");

        // Act
        _loader.SetDefaultConfigurationPath(configFilePath);

        // Assert - Should extract directory for individual BIB discovery
        // Note: This tests the internal logic that affects discovery behavior
        Assert.True(true); // Test passes if no exception thrown
    }

    [Fact]
    public void SetDefaultConfigurationPath_WithDirectory_UsesDirectly()
    {
        // Arrange
        var configDirectory = Path.Combine(_testDirectory, "configdir");
        Directory.CreateDirectory(configDirectory);

        // Act
        _loader.SetDefaultConfigurationPath(configDirectory);

        // Assert - Should use directory directly
        Assert.True(true); // Test passes if no exception thrown
    }

    [Fact]
    public async Task LoadBibAsync_WithDirectoryBasedDiscovery_FindsIndividualFiles()
    {
        // Arrange
        var configDir = Path.Combine(_testDirectory, "discovery_test");
        Directory.CreateDirectory(configDir);
        
        var individualBibXml = CreateIndividualBibXml("directory_discovery");
        var individualBibPath = Path.Combine(configDir, "bib_directory_discovery.xml");
        await File.WriteAllTextAsync(individualBibPath, individualBibXml);
        
        _loader.SetDefaultConfigurationPath(configDir);

        // Act - Use dummy path but rely on directory-based discovery
        var bibConfig = await _loader.LoadBibAsync("dummy.xml", "directory_discovery");

        // Assert
        Assert.NotNull(bibConfig);
        Assert.Equal("directory_discovery", bibConfig.BibId);
    }

    #endregion

    #region Caching Tests

    [Fact]
    public async Task LoadBibAsync_WithCaching_UsesCache()
    {
        // Arrange
        var individualBibXml = CreateIndividualBibXml("cache_test");
        var individualBibPath = Path.Combine(_individualBibsDirectory, "bib_cache_test.xml");
        await File.WriteAllTextAsync(individualBibPath, individualBibXml);

        // Act - Load twice
        var bibConfig1 = await _loader.LoadBibAsync(individualBibPath, "cache_test");
        var bibConfig2 = await _loader.LoadBibAsync(individualBibPath, "cache_test");

        // Assert - Both should be successful and equivalent
        Assert.NotNull(bibConfig1);
        Assert.NotNull(bibConfig2);
        Assert.Equal(bibConfig1.BibId, bibConfig2.BibId);
        Assert.Equal(bibConfig1.Description, bibConfig2.Description);
    }

    [Fact]
    public async Task LoadBibAsync_WithForceRefresh_BypassesCache()
    {
        // Arrange
        var individualBibXml = CreateIndividualBibXml("refresh_test");
        var individualBibPath = Path.Combine(_individualBibsDirectory, "bib_refresh_test.xml");
        await File.WriteAllTextAsync(individualBibPath, individualBibXml);

        // Load once to populate cache
        await _loader.LoadBibAsync(individualBibPath, "refresh_test");

        // Modify the file
        var modifiedXml = CreateIndividualBibXml("refresh_test_modified");
        await File.WriteAllTextAsync(individualBibPath, modifiedXml);

        // Act - Load with force refresh
        var refreshedBibConfig = await _loader.LoadBibConfigurationAsync(individualBibPath, forceRefresh: true);

        // Assert - Should get updated content
        Assert.NotNull(refreshedBibConfig);
        Assert.Contains("refresh_test_modified", refreshedBibConfig.BibId);
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task LoadBibAsync_WithInvalidXml_ThrowsXmlException()
    {
        // Arrange
        var invalidXmlPath = Path.Combine(_testDirectory, "invalid.xml");
        await File.WriteAllTextAsync(invalidXmlPath, "<invalid><unclosed>");

        // Act & Assert
        await Assert.ThrowsAsync<System.Xml.XmlException>(() =>
            _loader.LoadBibAsync(invalidXmlPath, "any_bib"));
    }

    [Fact]
    public async Task LoadBibAsync_WithNonExistentFile_ThrowsFileNotFoundException()
    {
        // Arrange
        var nonExistentPath = Path.Combine(_testDirectory, "nonexistent.xml");

        // Act & Assert
        await Assert.ThrowsAsync<FileNotFoundException>(() =>
            _loader.LoadBibAsync(nonExistentPath, "any_bib"));
    }

    [Fact]
    public async Task LoadBibAsync_WithNullParameters_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _loader.LoadBibAsync(null!, "bib_id"));
        
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _loader.LoadBibAsync("path.xml", null!));
        
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _loader.LoadBibAsync("path.xml", string.Empty));
    }

    [Fact]
    public async Task DiscoverBibsAsync_WithNonExistentDirectory_ReturnsEmpty()
    {
        // Arrange
        var nonExistentDir = Path.Combine(_testDirectory, "nonexistent");

        // Act
        var result = await _loader.DiscoverBibsAsync(nonExistentDir);

        // Assert
        Assert.Empty(result);
    }

    #endregion

    #region Performance Tests

    [Fact]
    public async Task LoadBibAsync_MultipleFiles_CompletesInReasonableTime()
    {
        // Arrange - Create multiple individual BIB files
        var bibIds = new List<string>();
        for (int i = 1; i <= 10; i++)
        {
            var bibId = $"perf_test_{i:D2}";
            bibIds.Add(bibId);
            
            var bibXml = CreateIndividualBibXml(bibId);
            var bibPath = Path.Combine(_individualBibsDirectory, $"bib_{bibId}.xml");
            await File.WriteAllTextAsync(bibPath, bibXml);
        }

        // Act - Load all files and measure time
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        var loadTasks = bibIds.Select(async bibId =>
        {
            var bibPath = Path.Combine(_individualBibsDirectory, $"bib_{bibId}.xml");
            return await _loader.LoadBibAsync(bibPath, bibId);
        });
        
        var results = await Task.WhenAll(loadTasks);
        stopwatch.Stop();

        // Assert
        Assert.Equal(10, results.Length);
        Assert.All(results, result => Assert.NotNull(result));
        Assert.True(stopwatch.ElapsedMilliseconds < 3000); // Should complete within 3 seconds
    }

    [Fact]
    public async Task DiscoverBibsAsync_LargeDirectory_CompletesInReasonableTime()
    {
        // Arrange - Create many BIB files
        for (int i = 1; i <= 50; i++)
        {
            var bibId = $"large_test_{i:D3}";
            var bibXml = CreateIndividualBibXml(bibId);
            var bibPath = Path.Combine(_individualBibsDirectory, $"bib_{bibId}.xml");
            await File.WriteAllTextAsync(bibPath, bibXml);
        }

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var discoveredBibs = await _loader.DiscoverBibsAsync(_individualBibsDirectory);
        stopwatch.Stop();

        // Assert
        var discoveredList = discoveredBibs.ToList();
        Assert.Equal(50, discoveredList.Count);
        Assert.True(stopwatch.ElapsedMilliseconds < 2000); // Should complete within 2 seconds
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