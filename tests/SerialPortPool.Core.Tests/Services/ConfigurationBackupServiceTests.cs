// ===================================================================
// SPRINT 11: ConfigurationBackupService Implementation
// File: SerialPortPool.Core/Services/Configuration/ConfigurationBackupService.cs
// Purpose: Enterprise-grade backup/rollback system for BIB configurations
// ===================================================================

using Microsoft.Extensions.Logging;

namespace SerialPortPool.Core.Services.Configuration;

/// <summary>
/// Service for creating and managing configuration file backups
/// Provides enterprise-grade backup/rollback functionality with version control
/// </summary>
public class ConfigurationBackupService
{
    private readonly ILogger<ConfigurationBackupService> _logger;
    private const string BackupExtension = ".backup.";
    private const string BackupDateFormat = "yyyyMMdd_HHmmss";

    public ConfigurationBackupService(ILogger<ConfigurationBackupService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region Public Methods

    /// <summary>
    /// Create a backup of the specified configuration file
    /// </summary>
    public async Task<string> CreateBackupAsync(string configFilePath)
    {
        if (string.IsNullOrWhiteSpace(configFilePath))
            throw new ArgumentException("Configuration file path cannot be null or empty", nameof(configFilePath));

        if (!File.Exists(configFilePath))
            throw new FileNotFoundException($"Configuration file not found: {configFilePath}");

        try
        {
            _logger.LogDebug("💾 Creating backup for: {FilePath}", configFilePath);

            var backupPath = GenerateBackupPath(configFilePath);
            var content = await File.ReadAllTextAsync(configFilePath);
            
            // Ensure backup directory exists
            var backupDir = Path.GetDirectoryName(backupPath);
            if (!string.IsNullOrEmpty(backupDir) && !Directory.Exists(backupDir))
            {
                Directory.CreateDirectory(backupDir);
            }

            // Create backup file
            await File.WriteAllTextAsync(backupPath, content);
            
            _logger.LogInformation("✅ Backup created successfully: {BackupPath}", backupPath);
            return backupPath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to create backup for: {FilePath}", configFilePath);
            throw;
        }
    }

    /// <summary>
    /// Restore configuration from backup
    /// </summary>
    public async Task<bool> RestoreFromBackupAsync(string configFilePath, string backupFilePath)
    {
        if (string.IsNullOrWhiteSpace(configFilePath))
            throw new ArgumentException("Configuration file path cannot be null or empty", nameof(configFilePath));

        if (string.IsNullOrWhiteSpace(backupFilePath))
            throw new ArgumentException("Backup file path cannot be null or empty", nameof(backupFilePath));

        try
        {
            if (!File.Exists(backupFilePath))
            {
                _logger.LogWarning("⚠️ Backup file not found: {BackupPath}", backupFilePath);
                return false;
            }

            _logger.LogDebug("🔄 Restoring from backup: {BackupPath} → {ConfigPath}", backupFilePath, configFilePath);

            // Read backup content
            var backupContent = await File.ReadAllTextAsync(backupFilePath);
            
            // Create temporary backup of current file before restore
            string? tempBackup = null;
            if (File.Exists(configFilePath))
            {
                tempBackup = configFilePath + ".restore_temp";
                await File.WriteAllTextAsync(tempBackup, await File.ReadAllTextAsync(configFilePath));
            }

            try
            {
                // Restore from backup
                await File.WriteAllTextAsync(configFilePath, backupContent);
                
                // Clean up temporary backup on success
                if (tempBackup != null && File.Exists(tempBackup))
                {
                    File.Delete(tempBackup);
                }

                _logger.LogInformation("✅ Configuration restored successfully from backup");
                return true;
            }
            catch (Exception)
            {
                // Restore original on failure
                if (tempBackup != null && File.Exists(tempBackup))
                {
                    await File.WriteAllTextAsync(configFilePath, await File.ReadAllTextAsync(tempBackup));
                    File.Delete(tempBackup);
                }
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to restore from backup: {BackupPath}", backupFilePath);
            return false;
        }
    }

    /// <summary>
    /// Get all backup files for a configuration file
    /// </summary>
    public async Task<IEnumerable<string>> GetBackupsAsync(string configFilePath)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(configFilePath) || !File.Exists(configFilePath))
            {
                return Enumerable.Empty<string>();
            }

            var directory = Path.GetDirectoryName(configFilePath);
            var fileName = Path.GetFileNameWithoutExtension(configFilePath);
            var extension = Path.GetExtension(configFilePath);

            if (string.IsNullOrEmpty(directory))
            {
                return Enumerable.Empty<string>();
            }

            _logger.LogDebug("🔍 Searching for backups in: {Directory}", directory);

            // Find backup files matching pattern
            var searchPattern = $"{fileName}{BackupExtension}*{extension}";
            var backupFiles = Directory.GetFiles(directory, searchPattern)
                .Where(f => IsValidBackupFile(f, fileName, extension))
                .OrderByDescending(f => File.GetCreationTime(f))
                .ToList();

            await Task.CompletedTask; // Make it properly async

            _logger.LogDebug("📋 Found {Count} backup files", backupFiles.Count);
            return backupFiles;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "⚠️ Error searching for backups: {Error}", ex.Message);
            return Enumerable.Empty<string>();
        }
    }

    /// <summary>
    /// Clean up old backup files
    /// </summary>
    public async Task<int> CleanupOldBackupsAsync(string configFilePath, TimeSpan maxAge, int maxCount = 10)
    {
        try
        {
            var backups = (await GetBackupsAsync(configFilePath)).ToList();
            if (!backups.Any())
            {
                return 0;
            }

            _logger.LogDebug("🧹 Cleaning up old backups (max age: {MaxAge}, max count: {MaxCount})", maxAge, maxCount);

            var cutoffTime = DateTime.Now - maxAge;
            var cleanedCount = 0;

            // Sort by creation time (newest first)
            var sortedBackups = backups
                .OrderByDescending(f => File.GetCreationTime(f))
                .ToList();

            // Keep only the most recent maxCount files
            var toDelete = new List<string>();

            // Remove files older than maxAge
            foreach (var backup in sortedBackups)
            {
                var creationTime = File.GetCreationTime(backup);
                if (creationTime < cutoffTime)
                {
                    toDelete.Add(backup);
                }
            }

            // Remove excess files beyond maxCount
            if (sortedBackups.Count > maxCount)
            {
                var excess = sortedBackups.Skip(maxCount);
                foreach (var backup in excess)
                {
                    if (!toDelete.Contains(backup))
                    {
                        toDelete.Add(backup);
                    }
                }
            }

            // Delete the files
            foreach (var fileToDelete in toDelete)
            {
                try
                {
                    File.Delete(fileToDelete);
                    cleanedCount++;
                    _logger.LogDebug("🗑️ Deleted old backup: {FilePath}", Path.GetFileName(fileToDelete));
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "⚠️ Could not delete backup file: {FilePath}", fileToDelete);
                }
            }

            _logger.LogInformation("✅ Cleanup completed: {Count} backup files removed", cleanedCount);
            return cleanedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error during backup cleanup: {Error}", ex.Message);
            return 0;
        }
    }

    /// <summary>
    /// Get backup file information
    /// </summary>
    public async Task<BackupInfo?> GetBackupInfoAsync(string backupFilePath)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(backupFilePath) || !File.Exists(backupFilePath))
            {
                return null;
            }

            var fileInfo = new FileInfo(backupFilePath);
            var originalPath = ExtractOriginalFilePath(backupFilePath);

            await Task.CompletedTask; // Make it properly async

            return new BackupInfo
            {
                BackupPath = backupFilePath,
                OriginalFilePath = originalPath,
                CreatedAt = fileInfo.CreationTime,
                Size = fileInfo.Length,
                IsValid = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "⚠️ Error getting backup info: {FilePath}", backupFilePath);
            return null;
        }
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Generate backup file path with timestamp
    /// </summary>
    private string GenerateBackupPath(string originalFilePath)
    {
        var directory = Path.GetDirectoryName(originalFilePath) ?? "";
        var fileName = Path.GetFileNameWithoutExtension(originalFilePath);
        var extension = Path.GetExtension(originalFilePath);
        var timestamp = DateTime.Now.ToString(BackupDateFormat);

        return Path.Combine(directory, $"{fileName}{BackupExtension}{timestamp}{extension}");
    }

    /// <summary>
    /// Check if file is a valid backup file
    /// </summary>
    private bool IsValidBackupFile(string filePath, string originalFileName, string originalExtension)
    {
        try
        {
            var fileName = Path.GetFileNameWithoutExtension(filePath);
            var extension = Path.GetExtension(filePath);

            // Check extension matches
            if (!string.Equals(extension, originalExtension, StringComparison.OrdinalIgnoreCase))
                return false;

            // Check filename pattern
            var expectedPrefix = originalFileName + BackupExtension;
            if (!fileName.StartsWith(expectedPrefix, StringComparison.OrdinalIgnoreCase))
                return false;

            // Check timestamp format
            var timestampPart = fileName.Substring(expectedPrefix.Length);
            return DateTime.TryParseExact(timestampPart, BackupDateFormat, null, System.Globalization.DateTimeStyles.None, out _);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Extract original file path from backup file path
    /// </summary>
    private string ExtractOriginalFilePath(string backupFilePath)
    {
        try
        {
            var directory = Path.GetDirectoryName(backupFilePath) ?? "";
            var fileName = Path.GetFileNameWithoutExtension(backupFilePath);
            var extension = Path.GetExtension(backupFilePath);

            // Find backup extension pattern
            var backupIndex = fileName.LastIndexOf(BackupExtension, StringComparison.OrdinalIgnoreCase);
            if (backupIndex > 0)
            {
                var originalFileName = fileName.Substring(0, backupIndex);
                return Path.Combine(directory, originalFileName + extension);
            }

            return backupFilePath; // Fallback
        }
        catch
        {
            return backupFilePath; // Fallback
        }
    }

    #endregion
}

/// <summary>
/// Information about a backup file
/// </summary>
public class BackupInfo
{
    public string BackupPath { get; set; } = string.Empty;
    public string OriginalFilePath { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public long Size { get; set; }
    public bool IsValid { get; set; }

    public string GetSummary()
    {
        var sizeKB = Size / 1024.0;
        return $"📄 Backup: {Path.GetFileName(BackupPath)} | Created: {CreatedAt:yyyy-MM-dd HH:mm:ss} | Size: {sizeKB:F1} KB";
    }
}