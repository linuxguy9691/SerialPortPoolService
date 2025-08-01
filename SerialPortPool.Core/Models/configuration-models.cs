// ===================================================================
// CONFIGURATION MODELS - VERSION PROPRE COMPLÈTE
// Fichier: SerialPortPool.Core/Models/configuration-models.cs
// ===================================================================

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SerialPortPool.Core.Models;

/// <summary>
/// Configuration pour les tests BIB (Built-In Test)
/// </summary>
public class BibConfiguration
{
    [Required]
    public string Name { get; set; } = string.Empty;
    
    public string Description { get; set; } = string.Empty;
    
    public bool IsEnabled { get; set; } = true;
    
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
    
    public List<UutConfiguration> UutConfigurations { get; set; } = new();
    
    public Dictionary<string, object> Parameters { get; set; } = new();

    /// <summary>
    /// Obtient la configuration UUT par nom
    /// </summary>
    public UutConfiguration? GetUut(string name)
    {
        return UutConfigurations.FirstOrDefault(u => 
            string.Equals(u.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Valide la configuration BIB
    /// </summary>
    public bool IsValid()
    {
        return !string.IsNullOrWhiteSpace(Name) && 
               UutConfigurations.Any() && 
               UutConfigurations.All(u => u.IsValid());
    }
}

/// <summary>
/// Configuration pour un Unit Under Test (UUT)
/// </summary>
public class UutConfiguration
{
    [Required]
    public string Name { get; set; } = string.Empty;
    
    public string Type { get; set; } = string.Empty;
    
    public string SerialNumber { get; set; } = string.Empty;
    
    public bool IsEnabled { get; set; } = true;
    
    public List<PortConfiguration> PortConfigurations { get; set; } = new();
    
    public Dictionary<string, object> Properties { get; set; } = new();

    /// <summary>
    /// Obtient la configuration de port par nom
    /// </summary>
    public PortConfiguration? GetPort(string portName)
    {
        return PortConfigurations.FirstOrDefault(p => 
            string.Equals(p.PortName, portName, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Valide la configuration UUT
    /// </summary>
    public bool IsValid()
    {
        return !string.IsNullOrWhiteSpace(Name) && 
               !string.IsNullOrWhiteSpace(Type) &&
               PortConfigurations.Any() &&
               PortConfigurations.All(p => p.IsValid());
    }
}

/// <summary>
/// Configuration d'un port série
/// </summary>
public class PortConfiguration
{
    [Required]
    public string PortName { get; set; } = string.Empty;
    
    public int BaudRate { get; set; } = 9600;
    
    public int DataBits { get; set; } = 8;
    
    public string Parity { get; set; } = "None";
    
    public string StopBits { get; set; } = "One";
    
    public TimeSpan ReadTimeout { get; set; } = TimeSpan.FromSeconds(1);
    
    public TimeSpan WriteTimeout { get; set; } = TimeSpan.FromSeconds(1);
    
    public string Protocol { get; set; } = "RS232";
    
    public bool IsEnabled { get; set; } = true;
    
    public Dictionary<string, object> AdvancedSettings { get; set; } = new();

    /// <summary>
    /// Valide la configuration du port
    /// </summary>
    public bool IsValid()
    {
        return !string.IsNullOrWhiteSpace(PortName) &&
               BaudRate > 0 &&
               DataBits >= 5 && DataBits <= 8 &&
               ReadTimeout > TimeSpan.Zero &&
               WriteTimeout > TimeSpan.Zero;
    }

    /// <summary>
    /// Obtient une description lisible de la configuration
    /// </summary>
    public string GetDescription()
    {
        return $"{PortName}: {BaudRate},{DataBits},{Parity},{StopBits}";
    }
}

/// <summary>
/// Séquence de commandes pour un test
/// </summary>
public class CommandSequence
{
    [Required]
    public string Name { get; set; } = string.Empty;
    
    public string Description { get; set; } = string.Empty;
    
    public List<CommandStep> Steps { get; set; } = new();
    
    public TimeSpan TotalTimeout { get; set; } = TimeSpan.FromMinutes(5);
    
    public bool StopOnError { get; set; } = true;
    
    public Dictionary<string, object> Variables { get; set; } = new();

    /// <summary>
    /// Valide la séquence de commandes
    /// </summary>
    public bool IsValid()
    {
        return !string.IsNullOrWhiteSpace(Name) &&
               Steps.Any() &&
               Steps.All(s => s.IsValid());
    }

    /// <summary>
    /// Obtient le nombre total d'étapes
    /// </summary>
    public int TotalSteps => Steps.Count;
}

/// <summary>
/// Étape individuelle dans une séquence de commandes
/// </summary>
public class CommandStep
{
    [Required]
    public string Name { get; set; } = string.Empty;
    
    public string Command { get; set; } = string.Empty;
    
    public string ExpectedResponse { get; set; } = string.Empty;
    
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(5);
    
    public int RetryCount { get; set; } = 0;
    
    public bool IsCritical { get; set; } = true;
    
    public Dictionary<string, object> Parameters { get; set; } = new();

    /// <summary>
    /// Valide l'étape de commande
    /// </summary>
    public bool IsValid()
    {
        return !string.IsNullOrWhiteSpace(Name) &&
               !string.IsNullOrWhiteSpace(Command) &&
               Timeout > TimeSpan.Zero &&
               RetryCount >= 0;
    }
}

/// <summary>
/// Configuration système principale
/// </summary>
public class SystemConfiguration
{
    [Required]
    public string SystemName { get; set; } = "SerialPortPool";
    
    [Required]
    public string Version { get; set; } = "1.0.0";
    
    public LoggingConfiguration Logging { get; set; } = new();
    
    public PortPoolConfiguration PortPool { get; set; } = new();
    
    public List<ProtocolConfiguration> Protocols { get; set; } = new();
    
    public SecurityConfiguration Security { get; set; } = new();
    
    public PerformanceConfiguration Performance { get; set; } = new();
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime LastModified { get; set; } = DateTime.UtcNow;
    
    public string ConfigurationSource { get; set; } = "XML";
    
    public Dictionary<string, object> CustomSettings { get; set; } = new();

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
    public bool ValidateOnLoad { get; set; } = true;
    
    public bool CacheConfiguration { get; set; } = true;
    
    public TimeSpan CacheTimeout { get; set; } = TimeSpan.FromMinutes(30);
    
    public bool ThrowOnValidationError { get; set; } = false;
    
    public bool LoadDefaults { get; set; } = true;
    
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
    public string? SystemName { get; set; }
    
    public string? Version { get; set; }
    
    public string? ProtocolName { get; set; }
    
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