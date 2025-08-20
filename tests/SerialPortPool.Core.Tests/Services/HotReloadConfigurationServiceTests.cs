// ===================================================================
// SPRINT 11 TESTING: HotReloadConfigurationService Unit Tests
// File: tests/SerialPortPool.Core.Tests/Services/HotReloadConfigurationServiceTests.cs
// Purpose: Comprehensive tests for file monitoring + event-driven hot reload
// ===================================================================

using Microsoft.Extensions.Logging;
using Moq;
using SerialPortPool.Core.Services;
using Xunit;

namespace SerialPortPool.Core.Tests.Services;

/// <summary>
/// Unit tests for HotReloadConfigurationService (Sprint 11)
/// Tests file system monitoring, debounced events, and configuration reload
/// </summary>
public class HotReloadConfigurationServiceTests : IDisposable
{
    private readonly Mock<ILogger<HotReloadConfigurationService>> _mockLogger;
    private readonly HotReloadConfigurationService _hotReloadService;
    private readonly string _testDirectory;
    private readonly string _testConfigFile;
    private readonly List<string> _capturedEvents;

    public HotReloadConfigurationServiceTests()
    {
        _mockLogger = new Mock<ILogger<HotReloadConfigurationService>>();
        _testDirectory = Path.Combine(Path.GetTempPath(), "Sprint11_HotReloadTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDirectory);
        
        _testConfigFile = Path.Combine(_testDirectory, "test_config.xml");
        _capturedEvents = new List<string>();
        
        _hotReloadService = new HotReloadConfigurationService(_mockLogger.Object);
        
        // Subscribe to events for testing
        _hotReloadService.ConfigurationChanged += OnConfigurationChanged;
        _hotReloadService.ConfigurationError += OnConfigurationError;
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidLogger_InitializesCorrectly()
    {
        // Arrange & Act
        var service = new HotReloadConfigurationService(_mockLogger.Object);

        // Assert
        Assert.NotNull(service);
        Assert.False(service.IsMonitoring);
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new HotReloadConfigurationService(null!));
    }

    #endregion

    #region StartMonitoring Tests

    [Fact]
    public async Task StartMonitoring_WithValidDirectory_StartSuccessfully()
    {
        // Arrange
        await File.WriteAllTextAsync(_testConfigFile, CreateTestXmlContent("initial"));

        // Act
        var success = _hotReloadService.StartMonitoring(_testDirectory);

        // Assert
        Assert.True(success);
        Assert.True(_hotReloadService.IsMonitoring);
        Assert.Equal(_testDirectory, _hotReloadService.MonitoredDirectory);
    }

    [Fact]
    public void StartMonitoring_WithNonExistentDirectory_ReturnsFalse()
    {
        // Arrange
        var nonExistentDir = Path.Combine(_testDirectory, "nonexistent");

        // Act
        var success = _hotReloadService.StartMonitoring(nonExistentDir);

        // Assert
        Assert.False(success);
        Assert.False(_hotReloadService.IsMonitoring);
    }

    [Fact]
    public void StartMonitoring_WithNullDirectory_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            _hotReloadService.StartMonitoring(null!));
    }

    [Fact]
    public void StartMonitoring_WithEmptyDirectory_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            _hotReloadService.StartMonitoring(string.Empty));
    }

    [Fact]
    public async Task StartMonitoring_WhenAlreadyMonitoring_StopsCurrentAndStartsNew()
    {
        // Arrange
        await File.WriteAllTextAsync(_testConfigFile, CreateTestXmlContent("initial"));
        var firstDirectory = _testDirectory;
        var secondDirectory = Path.Combine(Path.GetTempPath(), "Sprint11_SecondDir", Guid.NewGuid().ToString());
        Directory.CreateDirectory(secondDirectory);

        try
        {
            // Start monitoring first directory
            var firstStart = _hotReloadService.StartMonitoring(firstDirectory);
            Assert.True(firstStart);
            Assert.Equal(firstDirectory, _hotReloadService.MonitoredDirectory);

            // Act - Start monitoring second directory
            var secondStart = _hotReloadService.StartMonitoring(secondDirectory);

            // Assert
            Assert.True(secondStart);
            Assert.Equal(secondDirectory, _hotReloadService.MonitoredDirectory);
            Assert.True(_hotReloadService.IsMonitoring);
        }
        finally
        {
            if (Directory.Exists(secondDirectory))
                Directory.Delete(secondDirectory, true);
        }
    }

    #endregion

    #region StopMonitoring Tests

    [Fact]
    public async Task StopMonitoring_WhenMonitoring_StopsSuccessfully()
    {
        // Arrange
        await File.WriteAllTextAsync(_testConfigFile, CreateTestXmlContent("initial"));
        _hotReloadService.StartMonitoring(_testDirectory);
        Assert.True(_hotReloadService.IsMonitoring);

        // Act
        _hotReloadService.StopMonitoring();

        // Assert
        Assert.False(_hotReloadService.IsMonitoring);
        Assert.Null(_hotReloadService.MonitoredDirectory);
    }

    [Fact]
    public void StopMonitoring_WhenNotMonitoring_DoesNotThrow()
    {
        // Arrange - Service not monitoring
        Assert.False(_hotReloadService.IsMonitoring);

        // Act & Assert - Should not throw
        _hotReloadService.StopMonitoring();
        Assert.False(_hotReloadService.IsMonitoring);
    }

    #endregion

    #region File Change Detection Tests

    [Fact]
    public async Task FileChange_TriggersConfigurationChangedEvent()
    {
        // Arrange
        await File.WriteAllTextAsync(_testConfigFile, CreateTestXmlContent("initial"));
        _hotReloadService.StartMonitoring(_testDirectory);
        _capturedEvents.Clear();

        // Act - Modify the file
        await File.WriteAllTextAsync(_testConfigFile, CreateTestXmlContent("modified"));
        
        // Wait for debounced event processing
        await Task.Delay(1000);

        // Assert
        Assert.Contains(_capturedEvents, e => e.Contains("ConfigurationChanged"));
        Assert.Contains(_capturedEvents, e => e.Contains("test_config.xml"));
    }

    [Fact]
    public async Task FileCreation_TriggersConfigurationChangedEvent()
    {
        // Arrange
        _hotReloadService.StartMonitoring(_testDirectory);
        _capturedEvents.Clear();

        // Act - Create new file
        var newConfigFile = Path.Combine(_testDirectory, "new_config.xml");
        await File.WriteAllTextAsync(newConfigFile, CreateTestXmlContent("new"));
        
        // Wait for debounced event processing
        await Task.Delay(1000);

        // Assert
        Assert.Contains(_capturedEvents, e => e.Contains("ConfigurationChanged"));
        Assert.Contains(_capturedEvents, e => e.Contains("new_config.xml"));
    }

    [Fact]
    public async Task FileDeletion_TriggersConfigurationChangedEvent()
    {
        // Arrange
        await File.WriteAllTextAsync(_testConfigFile, CreateTestXmlContent("to_delete"));
        _hotReloadService.StartMonitoring(_testDirectory);
        _capturedEvents.Clear();

        // Act - Delete the file
        File.Delete(_testConfigFile);
        
        // Wait for debounced event processing
        await Task.Delay(1000);

        // Assert
        Assert.Contains(_capturedEvents, e => e.Contains("ConfigurationChanged"));
    }

    [Fact]
    public async Task NonXmlFile_DoesNotTriggerEvent()
    {
        // Arrange
        _hotReloadService.StartMonitoring(_testDirectory);
        _capturedEvents.Clear();

        // Act - Create non-XML file
        var txtFile = Path.Combine(_testDirectory, "readme.txt");
        await File.WriteAllTextAsync(txtFile, "This is not an XML file");
        
        // Wait for potential event processing
        await Task.Delay(1000);

        // Assert - Should not trigger configuration events
        Assert.DoesNotContain(_capturedEvents, e => e.Contains("ConfigurationChanged"));
    }

    #endregion

    #region Debouncing Tests

    [Fact]
    public async Task RapidFileChanges_AreDebounced()
    {
        // Arrange
        await File.WriteAllTextAsync(_testConfigFile, CreateTestXmlContent("initial"));
        _hotReloadService.StartMonitoring(_testDirectory);
        _capturedEvents.Clear();

        // Act - Make rapid changes to the same file
        for (int i = 1; i <= 10; i++)
        {
            await File.WriteAllTextAsync(_testConfigFile, CreateTestXmlContent($"rapid_change_{i}"));
            await Task.Delay(50); // Rapid changes within debounce window
        }
        
        // Wait for debounced event processing
        await Task.Delay(1500);

        // Assert - Should trigger only once due to debouncing
        var configChangedEvents = _capturedEvents.Count(e => e.Contains("ConfigurationChanged"));
        Assert.True(configChangedEvents <= 2, $"Expected <= 2 events due to debouncing, got {configChangedEvents}");
    }

    [Fact]
    public async Task SlowFileChanges_TriggerMultipleEvents()
    {
        // Arrange
        await File.WriteAllTextAsync(_testConfigFile, CreateTestXmlContent("initial"));
        _hotReloadService.StartMonitoring(_testDirectory);
        _capturedEvents.Clear();

        // Act - Make slow changes (outside debounce window)
        await File.WriteAllTextAsync(_testConfigFile, CreateTestXmlContent("change_1"));
        await Task.Delay(800); // Wait for first event to process
        
        await File.WriteAllTextAsync(_testConfigFile, CreateTestXmlContent("change_2"));
        await Task.Delay(800); // Wait for second event to process

        // Assert - Should trigger multiple events
        var configChangedEvents = _capturedEvents.Count(e => e.Contains("ConfigurationChanged"));
        Assert.True(configChangedEvents >= 2, $"Expected >= 2 events for slow changes, got {configChangedEvents}");
    }

    #endregion

    #region BIB-Specific Monitoring Tests

    [Fact]
    public async Task BibFileChange_ExtractsBibIdCorrectly()
    {
        // Arrange
        var bibFile = Path.Combine(_testDirectory, "bib_production_line_1.xml");
        await File.WriteAllTextAsync(bibFile, CreateTestXmlContent("production_line_1"));
        _hotReloadService.StartMonitoring(_testDirectory);
        _capturedEvents.Clear();

        // Act - Modify BIB file
        await File.WriteAllTextAsync(bibFile, CreateTestXmlContent("production_line_1_modified"));
        
        // Wait for event processing
        await Task.Delay(1000);

        // Assert
        Assert.Contains(_capturedEvents, e => e.Contains("production_line_1"));
        Assert.Contains(_capturedEvents, e => e.Contains("bib_production_line_1.xml"));
    }

    [Fact]
    public async Task MultipleBibFiles_TriggerSeparateEvents()
    {
        // Arrange
        var bib1File = Path.Combine(_testDirectory, "bib_line_1.xml");
        var bib2File = Path.Combine(_testDirectory, "bib_line_2.xml");
        
        await File.WriteAllTextAsync(bib1File, CreateTestXmlContent("line_1"));
        await File.WriteAllTextAsync(bib2File, CreateTestXmlContent("line_2"));
        
        _hotReloadService.StartMonitoring(_testDirectory);
        _capturedEvents.Clear();

        // Act - Modify both files with delay
        await File.WriteAllTextAsync(bib1File, CreateTestXmlContent("line_1_modified"));
        await Task.Delay(800);
        
        await File.WriteAllTextAsync(bib2File, CreateTestXmlContent("line_2_modified"));
        await Task.Delay(800);

        // Assert
        Assert.Contains(_capturedEvents, e => e.Contains("line_1"));
        Assert.Contains(_capturedEvents, e => e.Contains("line_2"));
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task InvalidXmlFile_TriggersErrorEvent()
    {
        // Arrange
        _hotReloadService.StartMonitoring(_testDirectory);
        _capturedEvents.Clear();

        // Act - Create invalid XML file
        var invalidXmlFile = Path.Combine(_testDirectory, "invalid_config.xml");
        await File.WriteAllTextAsync(invalidXmlFile, "<invalid><unclosed>");
        
        // Wait for event processing
        await Task.Delay(1000);

        // Assert
        Assert.Contains(_capturedEvents, e => e.Contains("ConfigurationError"));
        Assert.Contains(_capturedEvents, e => e.Contains("invalid_config.xml"));
    }

    [Fact]
    public async Task FileInUse_HandlesGracefully()
    {
        // Arrange
        await File.WriteAllTextAsync(_testConfigFile, CreateTestXmlContent("initial"));
        _hotReloadService.StartMonitoring(_testDirectory);
        _capturedEvents.Clear();

        // Act - Open file for exclusive write to simulate file in use
        using (var fileStream = new FileStream(_testConfigFile, FileMode.Open, FileAccess.Write, FileShare.None))
        {
            // Simulate another process trying to modify the file
            var modifyTask = Task.Run(async () =>
            {
                try
                {
                    await File.WriteAllTextAsync(_testConfigFile, CreateTestXmlContent("modified"));
                }
                catch
                {
                    // Expected to fail due to file lock
                }
            });

            await Task.Delay(500);
        }

        await Task.Delay(1000);

        // Assert - Should handle gracefully without crashing
        Assert.True(true); // Test passes if no exception thrown
    }

    #endregion

    #region Service Lifecycle Tests

    [Fact]
    public async Task Dispose_StopsMonitoringGracefully()
    {
        // Arrange
        await File.WriteAllTextAsync(_testConfigFile, CreateTestXmlContent("initial"));
        _hotReloadService.StartMonitoring(_testDirectory);
        Assert.True(_hotReloadService.IsMonitoring);

        // Act
        _hotReloadService.Dispose();

        // Assert
        Assert.False(_hotReloadService.IsMonitoring);
        Assert.Null(_hotReloadService.MonitoredDirectory);
    }

    [Fact]
    public void Dispose_WhenNotMonitoring_DoesNotThrow()
    {
        // Arrange - Service not monitoring
        Assert.False(_hotReloadService.IsMonitoring);

        // Act & Assert - Should not throw
        _hotReloadService.Dispose();
    }

    [Fact]
    public async Task MultipleDispose_DoesNotThrow()
    {
        // Arrange
        await File.WriteAllTextAsync(_testConfigFile, CreateTestXmlContent("initial"));
        _hotReloadService.StartMonitoring(_testDirectory);

        // Act & Assert - Multiple dispose calls should not throw
        _hotReloadService.Dispose();
        _hotReloadService.Dispose();
        _hotReloadService.Dispose();
    }

    #endregion

    #region Configuration Statistics Tests

    [Fact]
    public async Task GetMonitoringStatistics_ReturnsCorrectData()
    {
        // Arrange
        await File.WriteAllTextAsync(_testConfigFile, CreateTestXmlContent("initial"));
        _hotReloadService.StartMonitoring(_testDirectory);
        
        // Trigger some events
        await File.WriteAllTextAsync(_testConfigFile, CreateTestXmlContent("modified_1"));
        await Task.Delay(800);
        await File.WriteAllTextAsync(_testConfigFile, CreateTestXmlContent("modified_2"));
        await Task.Delay(800);

        // Act
        var stats = _hotReloadService.GetMonitoringStatistics();

        // Assert
        Assert.NotNull(stats);
        Assert.True(stats.IsMonitoring);
        Assert.Equal(_testDirectory, stats.MonitoredDirectory);
        Assert.True(stats.TotalEventsProcessed >= 1);
        Assert.True(stats.MonitoringDuration > TimeSpan.Zero);
    }

    [Fact]
    public void GetMonitoringStatistics_WhenNotMonitoring_ReturnsCorrectState()
    {
        // Arrange - Service not monitoring
        Assert.False(_hotReloadService.IsMonitoring);

        // Act
        var stats = _hotReloadService.GetMonitoringStatistics();

        // Assert
        Assert.NotNull(stats);
        Assert.False(stats.IsMonitoring);
        Assert.Null(stats.MonitoredDirectory);
        Assert.Equal(0, stats.TotalEventsProcessed);
        Assert.Equal(TimeSpan.Zero, stats.MonitoringDuration);
    }

    #endregion

    #region Performance Tests

    [Fact]
    public async Task HighVolumeFileChanges_HandledEfficiently()
    {
        // Arrange - Create multiple files
        var files = new List<string>();
        for (int i = 1; i <= 20; i++)
        {
            var file = Path.Combine(_testDirectory, $"config_{i:D2}.xml");
            await File.WriteAllTextAsync(file, CreateTestXmlContent($"initial_{i}"));
            files.Add(file);
        }

        _hotReloadService.StartMonitoring(_testDirectory);
        _capturedEvents.Clear();

        // Act - Modify all files rapidly
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        var modifyTasks = files.Select(async (file, index) =>
        {
            await Task.Delay(index * 50); // Stagger modifications slightly
            await File.WriteAllTextAsync(file, CreateTestXmlContent($"modified_{index}"));
        });

        await Task.WhenAll(modifyTasks);
        await Task.Delay(2000); // Wait for all events to process
        
        stopwatch.Stop();

        // Assert
        Assert.True(stopwatch.ElapsedMilliseconds < 10000); // Should complete within 10 seconds
        Assert.True(_capturedEvents.Count > 0); // Should capture some events
        
        var stats = _hotReloadService.GetMonitoringStatistics();
        Assert.True(stats.TotalEventsProcessed > 0);
    }

    #endregion

    #region Event Handler Methods

    private void OnConfigurationChanged(object? sender, ConfigurationChangedEventArgs e)
    {
        _capturedEvents.Add($"ConfigurationChanged: {e.FilePath} | BibId: {e.BibId} | ChangeType: {e.ChangeType}");
    }

    private void OnConfigurationError(object? sender, ConfigurationErrorEventArgs e)
    {
        _capturedEvents.Add($"ConfigurationError: {e.FilePath} | Error: {e.ErrorMessage}");
    }

    #endregion

    #region Helper Methods

    private string CreateTestXmlContent(string identifier)
    {
        return $"""
<?xml version="1.0" encoding="UTF-8"?>
<bib id="{identifier}" description="Test BIB for Hot Reload - {identifier}">
  <metadata>
    <board_type>hot_reload_test</board_type>
    <sprint>11</sprint>
    <test_identifier>{identifier}</test_identifier>
    <modified_at>{DateTime.Now:yyyy-MM-dd HH:mm:ss}</modified_at>
  </metadata>
  
  <uut id="test_uut" description="Test UUT for {identifier}">
    <port number="1">
      <protocol>rs232</protocol>
      <speed>115200</speed>
      <data_pattern>n81</data_pattern>
      
      <start>
        <command>INIT_{identifier}</command>
        <expected_response>READY_{identifier}</expected_response>
        <timeout_ms>3000</timeout_ms>
      </start>
      
      <test>
        <command>TEST_{identifier}</command>
        <expected_response>PASS_{identifier}</expected_response>
        <timeout_ms>5000</timeout_ms>
      </test>
      
      <stop>
        <command>QUIT_{identifier}</command>
        <expected_response>BYE_{identifier}</expected_response>
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
            _hotReloadService?.Dispose();
            
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

// Supporting classes for hot reload service testing
public class ConfigurationChangedEventArgs : EventArgs
{
    public string FilePath { get; set; } = string.Empty;
    public string? BibId { get; set; }
    public string ChangeType { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.Now;
}

public class ConfigurationErrorEventArgs : EventArgs
{
    public string FilePath { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public Exception? Exception { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.Now;
}

public class MonitoringStatistics
{
    public bool IsMonitoring { get; set; }
    public string? MonitoredDirectory { get; set; }
    public int TotalEventsProcessed { get; set; }
    public TimeSpan MonitoringDuration { get; set; }
    public DateTime? StartTime { get; set; }
    public int ConfigurationChangedEvents { get; set; }
    public int ConfigurationErrorEvents { get; set; }
}