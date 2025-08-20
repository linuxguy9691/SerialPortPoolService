// ===================================================================
// SPRINT 11 TESTING: ConfigurationBackupService Unit Tests
// File: tests/SerialPortPool.Core.Tests/Services/ConfigurationBackupServiceTests.cs
// Purpose: Comprehensive tests for enterprise-grade backup/rollback system
// ===================================================================

using Microsoft.Extensions.Logging;
using Moq;
using SerialPortPool.Core.Services;
using Xunit;

namespace SerialPortPool.Core.Tests.Services;

/// <summary>
/// Unit tests for ConfigurationBackupService (Sprint 11)
/// Tests backup/rollback functionality and enterprise-grade error handling
/// </summary>
public class ConfigurationBackupServiceTests : IDisposable
{
    private readonly Mock<ILogger<ConfigurationBackupService>> _mockLogger;
    private readonly ConfigurationBackupService _backupService;
    private readonly string _testDirectory;
    private readonly string _testConfigFile;

    public ConfigurationBackupServiceTests()
    {
        _mockLogger = new Mock<ILogger<ConfigurationBackupService>>();
        _testDirectory = Path.Combine(Path.GetTempPath(), "Sprint11_BackupTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDirectory);
        
        _testConfigFile = Path.Combine(_testDirectory, "test_config.xml");
        _backupService = new ConfigurationBackupService(_mockLogger.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidLogger_InitializesCorrectly()
    {
        // Arrange & Act
        var service = new ConfigurationBackupService(_mockLogger.Object);

        // Assert
        Assert.NotNull(service);
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new ConfigurationBackupService(null!));
    }

    #endregion

    #region CreateBackupAsync Tests

    [Fact]
    public async Task CreateBackupAsync_WithValidFile_CreatesBackupSuccessfully()
    {
        // Arrange
        var originalContent = CreateTestXmlContent("original");
        await File.WriteAllTextAsync(_testConfigFile, originalContent);

        // Act
        var backupPath = await _backupService.CreateBackupAsync(_testConfigFile);

        // Assert
        Assert.NotNull(backupPath);
        Assert.True(File.Exists(backupPath));
        
        var backupContent = await File.ReadAllTextAsync(backupPath);
        Assert.Equal(originalContent, backupContent);
        
        // Verify backup naming convention
        var fileName = Path.GetFileName(backupPath);
        Assert.Contains("test_config", fileName);
        Assert.Contains(".backup.", fileName);
        Assert.EndsWith(".xml", fileName);
    }

    [Fact]
    public async Task CreateBackupAsync_WithNonExistentFile_ThrowsFileNotFoundException()
    {
        // Arrange
        var nonExistentFile = Path.Combine(_testDirectory, "nonexistent.xml");

        // Act & Assert
        await Assert.ThrowsAsync<FileNotFoundException>(() =>
            _backupService.CreateBackupAsync(nonExistentFile));
    }

    [Fact]
    public async Task CreateBackupAsync_WithNullFilePath_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _backupService.CreateBackupAsync(null!));
    }

    [Fact]
    public async Task CreateBackupAsync_WithEmptyFilePath_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _backupService.CreateBackupAsync(string.Empty));
    }

    [Fact]
    public async Task CreateBackupAsync_MultipleBackups_CreatesUniqueFiles()
    {
        // Arrange
        var originalContent = CreateTestXmlContent("multiple_backups");
        await File.WriteAllTextAsync(_testConfigFile, originalContent);

        // Act - Create multiple backups
        var backup1 = await _backupService.CreateBackupAsync(_testConfigFile);
        await Task.Delay(1100); // Ensure different timestamps
        var backup2 = await _backupService.CreateBackupAsync(_testConfigFile);
        await Task.Delay(1100);
        var backup3 = await _backupService.CreateBackupAsync(_testConfigFile);

        // Assert
        Assert.NotEqual(backup1, backup2);
        Assert.NotEqual(backup2, backup3);
        Assert.NotEqual(backup1, backup3);
        
        Assert.True(File.Exists(backup1));
        Assert.True(File.Exists(backup2));
        Assert.True(File.Exists(backup3));
        
        // All should have same content
        var content1 = await File.ReadAllTextAsync(backup1);
        var content2 = await File.ReadAllTextAsync(backup2);
        var content3 = await File.ReadAllTextAsync(backup3);
        
        Assert.Equal(content1, content2);
        Assert.Equal(content2, content3);
    }

    #endregion

    #region RestoreFromBackupAsync Tests

    [Fact]
    public async Task RestoreFromBackupAsync_WithValidBackup_RestoresSuccessfully()
    {
        // Arrange
        var originalContent = CreateTestXmlContent("restore_test");
        var modifiedContent = CreateTestXmlContent("modified");
        
        await File.WriteAllTextAsync(_testConfigFile, originalContent);
        var backupPath = await _backupService.CreateBackupAsync(_testConfigFile);
        
        // Modify original file
        await File.WriteAllTextAsync(_testConfigFile, modifiedContent);
        Assert.Equal(modifiedContent, await File.ReadAllTextAsync(_testConfigFile));

        // Act
        var success = await _backupService.RestoreFromBackupAsync(_testConfigFile, backupPath);

        // Assert
        Assert.True(success);
        var restoredContent = await File.ReadAllTextAsync(_testConfigFile);
        Assert.Equal(originalContent, restoredContent);
    }

    [Fact]
    public async Task RestoreFromBackupAsync_WithNonExistentBackup_ReturnsFalse()
    {
        // Arrange
        var nonExistentBackup = Path.Combine(_testDirectory, "nonexistent.backup.xml");
        await File.WriteAllTextAsync(_testConfigFile, "test content");

        // Act
        var success = await _backupService.RestoreFromBackupAsync(_testConfigFile, nonExistentBackup);

        // Assert
        Assert.False(success);
    }

    [Fact]
    public async Task RestoreFromBackupAsync_WithNullPaths_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _backupService.RestoreFromBackupAsync(null!, "backup.xml"));
        
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _backupService.RestoreFromBackupAsync("config.xml", null!));
    }

    [Fact]
    public async Task RestoreFromBackupAsync_PreservesOriginalOnFailure()
    {
        // Arrange
        var originalContent = CreateTestXmlContent("preserve_test");
        await File.WriteAllTextAsync(_testConfigFile, originalContent);
        
        var invalidBackupPath = Path.Combine(_testDirectory, "invalid.backup.xml");

        // Act
        var success = await _backupService.RestoreFromBackupAsync(_testConfigFile, invalidBackupPath);

        // Assert
        Assert.False(success);
        Assert.True(File.Exists(_testConfigFile));
        
        var contentAfterFailedRestore = await File.ReadAllTextAsync(_testConfigFile);
        Assert.Equal(originalContent, contentAfterFailedRestore);
    }

    #endregion

    #region GetBackupsAsync Tests

    [Fact]
    public async Task GetBackupsAsync_WithExistingBackups_ReturnsCorrectList()
    {
        // Arrange
        var originalContent = CreateTestXmlContent("get_backups_test");
        await File.WriteAllTextAsync(_testConfigFile, originalContent);
        
        // Create multiple backups
        var backup1 = await _backupService.CreateBackupAsync(_testConfigFile);
        await Task.Delay(1100);
        var backup2 = await _backupService.CreateBackupAsync(_testConfigFile);
        await Task.Delay(1100);
        var backup3 = await _backupService.CreateBackupAsync(_testConfigFile);

        // Act
        var backups = await _backupService.GetBackupsAsync(_testConfigFile);
        var backupList = backups.ToList();

        // Assert
        Assert.Equal(3, backupList.Count);
        Assert.Contains(backup1, backupList);
        Assert.Contains(backup2, backupList);
        Assert.Contains(backup3, backupList);
        
        // Should be ordered by creation time (newest first)
        Assert.True(File.GetCreationTime(backupList[0]) >= File.GetCreationTime(backupList[1]));
        Assert.True(File.GetCreationTime(backupList[1]) >= File.GetCreationTime(backupList[2]));
    }

    [Fact]
    public async Task GetBackupsAsync_WithNoBackups_ReturnsEmptyList()
    {
        // Arrange
        var configWithoutBackups = Path.Combine(_testDirectory, "no_backups.xml");
        await File.WriteAllTextAsync(configWithoutBackups, CreateTestXmlContent("no_backups"));

        // Act
        var backups = await _backupService.GetBackupsAsync(configWithoutBackups);

        // Assert
        Assert.Empty(backups);
    }

    [Fact]
    public async Task GetBackupsAsync_WithNonExistentFile_ReturnsEmptyList()
    {
        // Arrange
        var nonExistentFile = Path.Combine(_testDirectory, "nonexistent.xml");

        // Act
        var backups = await _backupService.GetBackupsAsync(nonExistentFile);

        // Assert
        Assert.Empty(backups);
    }

    #endregion

    #region CleanupOldBackupsAsync Tests

    [Fact]
    public async Task CleanupOldBackupsAsync_RemovesOnlyOldBackups()
    {
        // Arrange
        var originalContent = CreateTestXmlContent("cleanup_test");
        await File.WriteAllTextAsync(_testConfigFile, originalContent);
        
        // Create multiple backups with artificial old timestamps
        var backup1 = await _backupService.CreateBackupAsync(_testConfigFile);
        var backup2 = await _backupService.CreateBackupAsync(_testConfigFile);
        var backup3 = await _backupService.CreateBackupAsync(_testConfigFile);
        
        // Make first two backups appear old
        var oldTime = DateTime.Now.AddDays(-10);
        File.SetCreationTime(backup1, oldTime);
        File.SetLastWriteTime(backup1, oldTime);
        File.SetCreationTime(backup2, oldTime.AddHours(1));
        File.SetLastWriteTime(backup2, oldTime.AddHours(1));

        // Act
        var cleanedCount = await _backupService.CleanupOldBackupsAsync(_testConfigFile, maxAge: TimeSpan.FromDays(7), maxCount: 2);

        // Assert
        Assert.Equal(1, cleanedCount); // Should clean 1 backup (oldest)
        Assert.False(File.Exists(backup1)); // Oldest should be removed
        Assert.True(File.Exists(backup2));  // Second oldest kept (within maxCount)
        Assert.True(File.Exists(backup3));  // Newest kept
    }

    [Fact]
    public async Task CleanupOldBackupsAsync_RespectsMaxCount()
    {
        // Arrange
        var originalContent = CreateTestXmlContent("max_count_test");
        await File.WriteAllTextAsync(_testConfigFile, originalContent);
        
        // Create 5 backups
        var backups = new List<string>();
        for (int i = 0; i < 5; i++)
        {
            var backup = await _backupService.CreateBackupAsync(_testConfigFile);
            backups.Add(backup);
            await Task.Delay(100); // Small delay for timestamp differences
        }

        // Act - Keep only 3 newest
        var cleanedCount = await _backupService.CleanupOldBackupsAsync(_testConfigFile, maxAge: TimeSpan.FromDays(365), maxCount: 3);

        // Assert
        Assert.Equal(2, cleanedCount); // Should remove 2 oldest
        
        // First 2 should be removed, last 3 should remain
        Assert.False(File.Exists(backups[0]));
        Assert.False(File.Exists(backups[1]));
        Assert.True(File.Exists(backups[2]));
        Assert.True(File.Exists(backups[3]));
        Assert.True(File.Exists(backups[4]));
    }

    #endregion

    #region GetBackupInfoAsync Tests

    [Fact]
    public async Task GetBackupInfoAsync_WithValidBackup_ReturnsCorrectInfo()
    {
        // Arrange
        var originalContent = CreateTestXmlContent("backup_info_test");
        await File.WriteAllTextAsync(_testConfigFile, originalContent);
        var backupPath = await _backupService.CreateBackupAsync(_testConfigFile);

        // Act
        var backupInfo = await _backupService.GetBackupInfoAsync(backupPath);

        // Assert
        Assert.NotNull(backupInfo);
        Assert.Equal(backupPath, backupInfo!.BackupPath);
        Assert.Equal(_testConfigFile, backupInfo.OriginalFilePath);
        Assert.True(backupInfo.CreatedAt <= DateTime.Now);
        Assert.True(backupInfo.CreatedAt > DateTime.Now.AddMinutes(-1));
        Assert.True(backupInfo.Size > 0);
        Assert.True(backupInfo.IsValid);
    }

    [Fact]
    public async Task GetBackupInfoAsync_WithNonExistentBackup_ReturnsNull()
    {
        // Arrange
        var nonExistentBackup = Path.Combine(_testDirectory, "nonexistent.backup.xml");

        // Act
        var backupInfo = await _backupService.GetBackupInfoAsync(nonExistentBackup);

        // Assert
        Assert.Null(backupInfo);
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task CreateBackupAsync_WithReadOnlyFile_HandlesGracefully()
    {
        // Arrange
        await File.WriteAllTextAsync(_testConfigFile, CreateTestXmlContent("readonly_test"));
        var fileInfo = new FileInfo(_testConfigFile);
        fileInfo.IsReadOnly = true;

        try
        {
            // Act & Assert - Should still be able to read and backup
            var backupPath = await _backupService.CreateBackupAsync(_testConfigFile);
            Assert.NotNull(backupPath);
            Assert.True(File.Exists(backupPath));
        }
        finally
        {
            // Cleanup - Remove readonly attribute
            fileInfo.IsReadOnly = false;
        }
    }

    [Fact]
    public async Task RestoreFromBackupAsync_WithReadOnlyTarget_ReturnsFalse()
    {
        // Arrange
        var originalContent = CreateTestXmlContent("readonly_restore_test");
        await File.WriteAllTextAsync(_testConfigFile, originalContent);
        var backupPath = await _backupService.CreateBackupAsync(_testConfigFile);
        
        var fileInfo = new FileInfo(_testConfigFile);
        fileInfo.IsReadOnly = true;

        try
        {
            // Act
            var success = await _backupService.RestoreFromBackupAsync(_testConfigFile, backupPath);

            // Assert
            Assert.False(success);
        }
        finally
        {
            // Cleanup
            fileInfo.IsReadOnly = false;
        }
    }

    #endregion

    #region Performance Tests

    [Fact]
    public async Task CreateBackupAsync_LargeFile_CompletesInReasonableTime()
    {
        // Arrange - Create a larger XML file (around 1MB)
        var largeContent = CreateLargeTestXmlContent(50000); // 50k lines
        await File.WriteAllTextAsync(_testConfigFile, largeContent);

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var backupPath = await _backupService.CreateBackupAsync(_testConfigFile);
        stopwatch.Stop();

        // Assert
        Assert.NotNull(backupPath);
        Assert.True(File.Exists(backupPath));
        Assert.True(stopwatch.ElapsedMilliseconds < 5000); // Should complete within 5 seconds
        
        var backupContent = await File.ReadAllTextAsync(backupPath);
        Assert.Equal(largeContent, backupContent);
    }

    #endregion

    #region Helper Methods

    private string CreateTestXmlContent(string identifier)
    {
        return $"""
<?xml version="1.0" encoding="UTF-8"?>
<root>
  <bib id="{identifier}" description="Test BIB for Sprint 11 - {identifier}">
    <metadata>
      <board_type>test</board_type>
      <sprint>11</sprint>
      <test_identifier>{identifier}</test_identifier>
      <created_date>{DateTime.Now:yyyy-MM-dd}</created_date>
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
</root>
""";
    }

    private string CreateLargeTestXmlContent(int lineCount)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        sb.AppendLine("<root>");
        sb.AppendLine("  <bib id=\"large_test\" description=\"Large test file for performance testing\">");
        sb.AppendLine("    <metadata>");
        sb.AppendLine("      <board_type>performance_test</board_type>");
        sb.AppendLine("      <sprint>11</sprint>");
        sb.AppendLine("    </metadata>");
        
        for (int i = 1; i <= lineCount; i++)
        {
            sb.AppendLine($"    <test_line id=\"line_{i}\" value=\"test_data_{i}\" />");
        }
        
        sb.AppendLine("  </bib>");
        sb.AppendLine("</root>");
        
        return sb.ToString();
    }

    #endregion

    #region Cleanup

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_testDirectory))
            {
                // Remove readonly attributes from all files
                var files = Directory.GetFiles(_testDirectory, "*", SearchOption.AllDirectories);
                foreach (var file in files)
                {
                    var fileInfo = new FileInfo(file);
                    if (fileInfo.IsReadOnly)
                    {
                        fileInfo.IsReadOnly = false;
                    }
                }
                
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