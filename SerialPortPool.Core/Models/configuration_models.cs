// ===================================================================
// CONFIGURATION MODELS - TYPES CONFIGURATION
// Fichier: SerialPortPool.Core/Models/ConfigurationModels.cs
// ===================================================================

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SerialPortPool.Core.Models
{
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

        public static ConfigurationValidationResult Success() => new() { IsValid = true };
        
        public static ConfigurationValidationResult Failure(params string[] errors) => new() 
        { 
            IsValid = false, 
            Errors = new List<string>(errors) 
        };

        public void AddError(string error) => Errors.Add(error);
        public void AddWarning(string warning) => Warnings.Add(warning);
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
    }
}