// ===================================================================
// SPRINT 11 BOUCHÉE #3: Configuration Backup Service - Production Safety
// File: SerialPortPool.Core/Services/Configuration/ConfigurationBackupService.cs
// Purpose: Backup/rollback system for production-safe BIB management
// Philosophy: "Single Responsibility" - ONLY backup/rollback concerns
// ===================================================================

using Microsoft.Extensions.Logging;

namespace SerialPortPool.Core.Services.Configuration;

/// <summary>
/// SPRINT 11: Production-safe configuration backup and rollback service
/// SINGLE RESPONSIBILITY: Backup/restore operations only
/// ZERO TOUCH: Works with existing configuration loaders
/// </summary>
public class ConfigurationBackupService : IConfigurationBackupService
{
    private readonly ILogger<ConfigurationBackupService> _logger;
    private readonly string _backupDirectory;
    private readonly ConfigurationBackupOptions _options;

    public ConfigurationBackupService(
        ILogger<ConfigurationBackupService> logger,
        ConfigurationBackupOptions? options = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options ?? ConfigurationBackupOptions.CreateDefault();
        _backupDirectory = Path.Combine(_options.BaseDirectory, "backups");
        
        EnsureBackupDirectoryExists();
        _logger.LogInformation("🛡️ Configuration Backup Service initialized: {BackupDir}", _backupDirectory);
    }

    /// <summary>
    /// Create backup before configuration change
    /// </summary>
    public async Task<BackupResult> CreateBackupAsync(string bibId, string sourceFilePath)
    {
        try
        {
            _logger.LogDebug("📁 Creating backup for BIB: {BibId}", bibId);

            if (!File.Exists(sourceFilePath))
            {
                return BackupResult.Failure($"Source file not found: {sourceFilePath}");
            }

            var backupInfo = GenerateBackupInfo(bibId, sourceFilePath);
            
            // Create timestamped backup
            File.Copy(sourceFilePath, backupInfo.BackupPath, overwrite: true);
            
            // Update latest backup link
            await UpdateLatestBackupLinkAsync(bibId, backupInfo.BackupPath);
            
            // Cleanup old backups if needed
            await CleanupOldBackupsAsync(bibId);

            _logger.LogInformation("✅ Backup created: {BibId} → {BackupFile}", 
                bibId, Path.GetFileName(backupInfo.BackupPath));

            return BackupResult.Success(backupInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Backup creation failed for BIB: {BibId}", bibId);
            return BackupResult.Failure($"Backup creation failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Restore from latest backup
    /// </summary>
    public async Task<RestoreResult> RestoreFromBackupAsync(string bibId, string targetFilePath)
    {
        try
        {
            _logger.LogWarning("🔄 Attempting restore for BIB: {BibId}", bibId);

            var latestBackup = await GetLatestBackupPathAsync(bibId);
            if (string.IsNullOrEmpty(latestBackup) || !File.Exists(latestBackup))
            {
                return RestoreResult.Failure($"No valid backup found for BIB: {bibId}");
            }

            // Create safety backup of current corrupted file
            var corruptedBackupPath = GenerateCorruptedFileBackupPath(bibId);
            if (File.Exists(targetFilePath))
            {
                File.Copy(targetFilePath, corruptedBackupPath, overwrite: true);
                _logger.LogInformation("📁 Corrupted file backed up: {CorruptedBackup}", 
                    Path.GetFileName(corruptedBackupPath));
            }

            // Restore from backup
            File.Copy(latestBackup, targetFilePath, overwrite: true);

            var restoreInfo = new RestoreInfo
            {
                BibId = bibId,
                RestoredFromPath = latestBackup,
                RestoredToPath = targetFilePath,
                CorruptedFileBackup = corruptedBackupPath,
                RestoredAt = DateTime.Now
            };

            _logger.LogInformation("✅ Successfully restored BIB from backup: {BibId}", bibId);
            return RestoreResult.Success(restoreInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Restore failed for BIB: {BibId}", bibId);
            return RestoreResult.Failure($"Restore failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Verify backup integrity
    /// </summary>
    public async Task<bool> VerifyBackupIntegrityAsync(string bibId)
    {
        try
        {
            var latestBackup = await GetLatestBackupPathAsync(bibId);
            if (string.IsNullOrEmpty(latestBackup) || !File.Exists(latestBackup))
            {
                _logger.LogWarning("⚠️ No backup found for verification: {BibId}", bibId);
                return false;
            }

            // Basic file integrity checks
            var fileInfo = new FileInfo(latestBackup);
            if (fileInfo.Length == 0)
            {
                _logger.LogError("❌ Backup file is empty: {BibId}", bibId);
                return false;
            }

            // Try to read as XML (basic validation)
            try
            {
                await File.ReadAllTextAsync(latestBackup);
                var xmlContent = await File.ReadAllTextAsync(latestBackup);
                
                // Basic XML well-formedness check
                if (!xmlContent.TrimStart().StartsWith("<?xml") && !xmlContent.TrimStart().StartsWith("<"))
                {
                    _logger.LogError("❌ Backup file doesn't appear to be valid XML: {BibId}", bibId);
                    return false;
                }

                _logger.LogDebug("✅ Backup integrity verified: {BibId}", bibId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Backup file read failed: {BibId}", bibId);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Backup verification failed: {BibId}", bibId);
            return false;
        }
    }

    /// <summary>
    /// Get backup history for a BIB
    /// </summary>
    public async Task<List<BackupInfo>> GetBackupHistoryAsync(string bibId)
    {
        try
        {
            var bibBackupDir = Path.Combine(_backupDirectory, bibId);
            if (!Directory.Exists(bibBackupDir))
            {
                return new List<BackupInfo>();
            }

            var backupFiles = Directory.GetFiles(bibBackupDir, "*.xml")
                .Where(f => !Path.GetFileName(f).StartsWith("latest_") && !Path.GetFileName(f).StartsWith("corrupted_"))
                .OrderByDescending(f => File.GetCreationTime(f))
                .ToList();

            var backupHistory = new List<BackupInfo>();
            
            foreach (var backupFile in backupFiles)
            {
                var fileInfo = new FileInfo(backupFile);
                var backupInfo = new BackupInfo
                {
                    BibId = bibId,
                    BackupPath = backupFile,
                    CreatedAt = fileInfo.CreationTime,
                    FileSize = fileInfo.Length,
                    IsLatest = backupFile == await GetLatestBackupPathAsync(bibId)
                };
                
                backupHistory.Add(backupInfo);
            }

            return backupHistory;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to get backup history for BIB: {BibId}", bibId);
            return new List<BackupInfo>();
        }
    }

    #region Private Helper Methods

    /// <summary>
    /// Ensure backup directory structure exists
    /// </summary>
    private void EnsureBackupDirectoryExists()
    {
        try
        {
            if (!Directory.Exists(_backupDirectory))
            {
                Directory.CreateDirectory(_backupDirectory);
                _logger.LogInformation("📁 Created backup directory: {BackupDir}", _backupDirectory);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to create backup directory: {BackupDir}", _backupDirectory);
            throw;
        }
    }

    /// <summary>
    /// Generate backup information for a BIB
    /// </summary>
    private BackupInfo GenerateBackupInfo(string bibId, string sourceFilePath)
    {
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var bibBackupDir = Path.Combine(_backupDirectory, bibId);
        
        Directory.CreateDirectory(bibBackupDir);
        
        var backupFileName = $"bib_{bibId}_{timestamp}.xml";
        var backupPath = Path.Combine(bibBackupDir, backupFileName);

        return new BackupInfo
        {
            BibId = bibId,
            BackupPath = backupPath,
            SourcePath = sourceFilePath,
            CreatedAt = DateTime.Now,
            FileSize = new FileInfo(sourceFilePath).Length
        };
    }

    /// <summary>
    /// Update latest backup symbolic link
    /// </summary>
    private async Task UpdateLatestBackupLinkAsync(string bibId, string backupPath)
    {
        try
        {
            var bibBackupDir = Path.Combine(_backupDirectory, bibId);
            var latestBackupPath = Path.Combine(bibBackupDir, $"latest_{bibId}.xml");
            
            if (File.Exists(latestBackupPath))
            {
                File.Delete(latestBackupPath);
            }
            
            File.Copy(backupPath, latestBackupPath);
            _logger.LogDebug("🔗 Updated latest backup link: {BibId}", bibId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "⚠️ Failed to update latest backup link: {BibId}", bibId);
            // Non-critical error, don't fail the backup
        }
    }

    /// <summary>
    /// Get path to latest backup
    /// </summary>
    private async Task<string?> GetLatestBackupPathAsync(string bibId)
    {
        try
        {
            var bibBackupDir = Path.Combine(_backupDirectory, bibId);
            var latestBackupPath = Path.Combine(bibBackupDir, $"latest_{bibId}.xml");
            
            if (File.Exists(latestBackupPath))
            {
                return latestBackupPath;
            }

            // Fallback: find most recent backup file
            if (Directory.Exists(bibBackupDir))
            {
                var backupFiles = Directory.GetFiles(bibBackupDir, "*.xml")
                    .Where(f => !Path.GetFileName(f).StartsWith("latest_") && !Path.GetFileName(f).StartsWith("corrupted_"))
                    .OrderByDescending(f => File.GetCreationTime(f))
                    .FirstOrDefault();

                return backupFiles;
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to get latest backup path: {BibId}", bibId);
            return null;
        }
    }

    /// <summary>
    /// Generate path for corrupted file backup
    /// </summary>
    private string GenerateCorruptedFileBackupPath(string bibId)
    {
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var bibBackupDir = Path.Combine(_backupDirectory, bibId);
        return Path.Combine(bibBackupDir, $"corrupted_{bibId}_{timestamp}.xml");
    }

    /// <summary>
    /// Cleanup old backups based on retention policy
    /// </summary>
    private async Task CleanupOldBackupsAsync(string bibId)
    {
        try
        {
            var backupHistory = await GetBackupHistoryAsync(bibId);
            var backupsToDelete = backupHistory
                .Skip(_options.MaxBackupsPerBib)
                .Where(b => !b.IsLatest)
                .ToList();

            foreach (var backup in backupsToDelete)
            {
                File.Delete(backup.BackupPath);
                _logger.LogDebug("🧹 Cleaned up old backup: {BackupFile}", Path.GetFileName(backup.BackupPath));
            }

            if (backupsToDelete.Any())
            {
                _logger.LogInformation("🧹 Cleaned up {Count} old backups for BIB: {BibId}", 
                    backupsToDelete.Count, bibId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "⚠️ Backup cleanup failed for BIB: {BibId}", bibId);
            // Non-critical error
        }
    }

    #endregion
}

/// <summary>
/// Interface for configuration backup service
/// </summary>
public interface IConfigurationBackupService
{
    Task<BackupResult> CreateBackupAsync(string bibId, string sourceFilePath);
    Task<RestoreResult> RestoreFromBackupAsync(string bibId, string targetFilePath);
    Task<bool> VerifyBackupIntegrityAsync(string bibId);
    Task<List<BackupInfo>> GetBackupHistoryAsync(string bibId);
}

/// <summary>
/// Configuration backup options
/// </summary>
public class ConfigurationBackupOptions
{
    public string BaseDirectory { get; set; } = "Configuration";
    public int MaxBackupsPerBib { get; set; } = 10;
    public bool EnableAutoCleanup { get; set; } = true;

    public static ConfigurationBackupOptions CreateDefault() => new();
}

/// <summary>
/// Backup operation information
/// </summary>
public class BackupInfo
{
    public string BibId { get; set; } = string.Empty;
    public string BackupPath { get; set; } = string.Empty;
    public string SourcePath { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public long FileSize { get; set; }
    public bool IsLatest { get; set; }

    public override string ToString() =>
        $"Backup {BibId}: {Path.GetFileName(BackupPath)} ({FileSize} bytes, {CreatedAt:yyyy-MM-dd HH:mm:ss})";
}

/// <summary>
/// Restore operation information
/// </summary>
public class RestoreInfo
{
    public string BibId { get; set; } = string.Empty;
    public string RestoredFromPath { get; set; } = string.Empty;
    public string RestoredToPath { get; set; } = string.Empty;
    public string CorruptedFileBackup { get; set; } = string.Empty;
    public DateTime RestoredAt { get; set; }

    public override string ToString() =>
        $"Restored {BibId}: {Path.GetFileName(RestoredFromPath)} → {Path.GetFileName(RestoredToPath)} at {RestoredAt:yyyy-MM-dd HH:mm:ss}";
}

/// <summary>
/// Backup operation result
/// </summary>
public class BackupResult
{
    public bool IsSuccess { get; set; }
    public string Message { get; set; } = string.Empty;
    public BackupInfo? BackupInfo { get; set; }

    public static BackupResult Success(BackupInfo backupInfo) =>
        new() { IsSuccess = true, Message = "Backup created successfully", BackupInfo = backupInfo };

    public static BackupResult Failure(string message) =>
        new() { IsSuccess = false, Message = message };
}

/// <summary>
/// Restore operation result
/// </summary>
public class RestoreResult
{
    public bool IsSuccess { get; set; }
    public string Message { get; set; } = string.Empty;
    public RestoreInfo? RestoreInfo { get; set; }

    public static RestoreResult Success(RestoreInfo restoreInfo) =>
        new() { IsSuccess = true, Message = "Restore completed successfully", RestoreInfo = restoreInfo };

    public static RestoreResult Failure(string message) =>
        new() { IsSuccess = false, Message = message };
}