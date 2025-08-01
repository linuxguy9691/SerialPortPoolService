// ===================================================================
// CONFIGURATION MODELS - VERSION NETTOYÉE (SANS DOUBLONS)
// Fichier: SerialPortPool.Core/Models/configuration-models.cs
// ===================================================================

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SerialPortPool.Core.Models;

// NOTE: BibConfiguration, UutConfiguration, PortConfiguration et CommandSequence 
// sont maintenant définis dans BibConfiguration.cs pour éviter la duplication

/// <summary>
/// Configuration système principale
/// </summary>
public class SystemConfiguration
{
    [Required]
    public string SystemName { get; set; } = "SerialPortPool";
    
    [Required]
    public string Version { get; set; } = "1.0.0";
    
    public List<BibConfiguration> Bibs { get; set; } = new();
    
    public LoggingConfiguration Logging { get; set; } = new();
    
    public PortPoolConfiguration PortPool { get; set; } = new();
    
    public List<ProtocolConfiguration> Protocols { get; set; } = new();
    
    public SecurityConfiguration Security { get; set; } = new();
    
    public PerformanceConfiguration Performance { get; set; } = new();
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime LastModified { get; set; } = DateTime.UtcNow;
    
    public string ConfigurationSource { get; set; } = "XML";
    
    public string SourcePath { get; set; } = string.Empty;
    
    public DateTime LoadedAt { get; set; }
    
    public Dictionary<string, object> CustomSettings { get; set; } = new();

    /// <summary>
    /// Get BIB by ID
    /// </summary>
    public BibConfiguration? GetBib(string bibId)
    {
        return Bibs.FirstOrDefault(b => b.BibId.Equals(bibId, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Get all protocols used across all BIBs
    /// </summary>
    public IEnumerable<string> GetAllProtocols()
    {
        return Bibs.SelectMany(b => b.GetAllPorts())
                  .Select(p => p.Protocol)
                  .Distinct(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Valide la configuration système
    /// </summary>
    public bool IsValid()
    {
        return !string.IsNullOrWhiteSpace(SystemName) &&
               !string.IsNullOrWhiteSpace(Version) &&
               Logging != null &&
               PortPool != null;
    }
}

/// <summary>
/// Configuration de logging
/// </summary>
public class LoggingConfiguration
{
    public string LogLevel { get; set; } = "Information";
    
    public string LogPath { get; set; } = "C:\\Logs\\SerialPortPool";
    
    public bool EnableConsoleLogging { get; set; } = true;
    
    public bool EnableFileLogging { get; set; } = true;
    
    public int MaxLogFileSizeMB { get; set; } = 10;
    
    public int MaxLogFiles { get; set; } = 5;
    
    public List<string> EnabledLoggers { get; set; } = new();
}

/// <summary>
/// Configuration du pool de ports
/// </summary>
public class PortPoolConfiguration
{
    public int MaxConcurrentPorts { get; set; } = 10;
    
    public TimeSpan DefaultTimeout { get; set; } = TimeSpan.FromSeconds(30);
    
    public bool AutoDiscovery { get; set; } = true;
    
    public List<string> ExcludedPorts { get; set; } = new();
    
    public Dictionary<string, object> AdvancedSettings { get; set; } = new();
    
    public int PoolRefreshInterval { get; set; } = 5000; // ms
}

/// <summary>
/// Configuration de sécurité
/// </summary>
public class SecurityConfiguration
{
    public bool RequireAuthentication { get; set; } = false;
    
    public bool EnableEncryption { get; set; } = false;
    
    public string AuthenticationMethod { get; set; } = "None";
    
    public List<string> AllowedUsers { get; set; } = new();
    
    public TimeSpan SessionTimeout { get; set; } = TimeSpan.FromHours(8);
}

/// <summary>
/// Configuration de performance
/// </summary>
public class PerformanceConfiguration
{
    public int ThreadPoolSize { get; set; } = 4;
    
    public int BufferSize { get; set; } = 4096;
    
    public TimeSpan ConnectionTimeout { get; set; } = TimeSpan.FromSeconds(10);
    
    public bool EnableStatistics { get; set; } = true;
    
    public int MaxRetryAttempts { get; set; } = 3;
}

/// <summary>
/// Options de chargement de configuration
/// </summary>
public class ConfigurationLoadOptions
{
    public bool ValidateSchema { get; set; } = true;
    
    public bool ValidateBusinessRules { get; set; } = true;
    
    public bool CacheConfiguration { get; set; } = true;
    
    public TimeSpan CacheExpiration { get; set; } = TimeSpan.FromMinutes(30);
    
    public bool ThrowOnValidationErrors { get; set; } = false;
    
    public bool LoadDefaults { get; set; } = true;
    
    public bool LoadProtocolSpecificSettings { get; set; } = true;
    
    public string ConfigurationPath { get; set; } = string.Empty;
    
    public Dictionary<string, object> CustomOptions { get; set; } = new();
    
    public bool BackupOnSave { get; set; } = true;
}

/// <summary>
/// Résultat de validation de configuration
/// </summary>
public class ConfigurationValidationResult
{
    public bool IsValid { get; set; }
    
    public List<string> Errors { get; set; } = new();
    
    public List<string> Warnings { get; set; } = new();
    
    public List<string> Info { get; set; } = new();
    
    public DateTime ValidatedAt { get; set; } = DateTime.UtcNow;
    
    public string ValidatorVersion { get; set; } = "1.0.0";
    
    public TimeSpan ValidationDuration { get; set; }

    /// <summary>
    /// Crée un résultat de succès
    /// </summary>
    public static ConfigurationValidationResult Success() => new() { IsValid = true };
    
    /// <summary>
    /// Crée un résultat d'échec avec erreurs
    /// </summary>
    public static ConfigurationValidationResult Failure(params string[] errors) => new() 
    { 
        IsValid = false, 
        Errors = new List<string>(errors) 
    };

    /// <summary>
    /// Ajoute une erreur au résultat
    /// </summary>
    public void AddError(string error)
    {
        if (!string.IsNullOrWhiteSpace(error))
        {
            Errors.Add(error);
            IsValid = false;
        }
    }

    /// <summary>
    /// Ajoute un avertissement au résultat
    /// </summary>
    public void AddWarning(string warning)
    {
        if (!string.IsNullOrWhiteSpace(warning))
        {
            Warnings.Add(warning);
        }
    }

    /// <summary>
    /// Ajoute une information au résultat
    /// </summary>
    public void AddInfo(string info)
    {
        if (!string.IsNullOrWhiteSpace(info))
        {
            Info.Add(info);
        }
    }

    /// <summary>
    /// Obtient un résumé du résultat de validation
    /// </summary>
    public string GetSummary()
    {
        return $"Validation: {(IsValid ? "SUCCESS" : "FAILED")} - " +
               $"Errors: {Errors.Count}, Warnings: {Warnings.Count}";
    }
}

/// <summary>
/// Critères de recherche de configuration
/// </summary>
public class ConfigurationSearchCriteria
{
    public string? BibId { get; set; }
    
    public string? UutId { get; set; }
    
    public string? SystemName { get; set; }
    
    public string? Version { get; set; }
    
    public string? Protocol { get; set; }
    
    public int? PortNumber { get; set; }
    
    public int? MinSpeed { get; set; }
    
    public int? MaxSpeed { get; set; }
    
    public string? DataPattern { get; set; }
    
    public DateTime? CreatedAfter { get; set; }
    
    public DateTime? CreatedBefore { get; set; }
    
    public Dictionary<string, object> CustomFilters { get; set; } = new();
    
    public int MaxResults { get; set; } = 100;
    
    public int Skip { get; set; } = 0;
    
    public string SortBy { get; set; } = "CreatedAt";
    
    public bool SortDescending { get; set; } = true;
    
    public bool IncludeDisabled { get; set; } = false;

    /// <summary>
    /// Valide les critères de recherche
    /// </summary>
    public bool IsValid()
    {
        return MaxResults > 0 && 
               Skip >= 0 && 
               !string.IsNullOrWhiteSpace(SortBy);
    }

    /// <summary>
    /// Applique les filtres par défaut
    /// </summary>
    public void ApplyDefaults()
    {
        if (MaxResults <= 0) MaxResults = 100;
        if (Skip < 0) Skip = 0;
        if (string.IsNullOrWhiteSpace(SortBy)) SortBy = "CreatedAt";
    }
}

/// <summary>
/// Définition de commande
/// </summary>
public class CommandDefinition
{
    public string Command { get; set; } = string.Empty;
    
    public string ExpectedResponse { get; set; } = string.Empty;
    
    public int TimeoutMs { get; set; } = 5000;
    
    public int RetryCount { get; set; } = 1;
    
    public string Description { get; set; } = string.Empty;
    
    public bool IsRegex { get; set; } = false;
    
    public bool IsCritical { get; set; } = true;
}