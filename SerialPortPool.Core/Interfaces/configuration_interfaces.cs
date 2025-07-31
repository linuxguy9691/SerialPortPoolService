// ===================================================================
// CONFIGURATION INTERFACES - INTERFACES MANQUANTES
// Fichier: SerialPortPool.Core/Interfaces/ConfigurationInterfaces.cs
// ===================================================================

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SerialPortPool.Core.Models;

namespace SerialPortPool.Core.Interfaces
{
    /// <summary>
    /// Interface pour le chargement de configuration XML
    /// </summary>
    public interface IXmlConfigurationLoader
    {
        /// <summary>
        /// Charge une configuration depuis un fichier XML
        /// </summary>
        Task<SystemConfiguration> LoadConfigurationAsync(string filePath, ConfigurationLoadOptions? options = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Sauvegarde une configuration vers un fichier XML
        /// </summary>
        Task SaveConfigurationAsync(SystemConfiguration configuration, string filePath, CancellationToken cancellationToken = default);

        /// <summary>
        /// Valide un fichier de configuration XML
        /// </summary>
        Task<ConfigurationValidationResult> ValidateConfigurationFileAsync(string filePath, CancellationToken cancellationToken = default);

        /// <summary>
        /// Recherche des configurations selon des critères
        /// </summary>
        Task<IEnumerable<SystemConfiguration>> SearchConfigurationsAsync(ConfigurationSearchCriteria criteria, CancellationToken cancellationToken = default);

        /// <summary>
        /// Obtient la liste des fichiers de configuration disponibles
        /// </summary>
        Task<IEnumerable<string>> GetAvailableConfigurationFilesAsync(string directory, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Interface pour la validation de configuration
    /// </summary>
    public interface IConfigurationValidator
    {
        /// <summary>
        /// Valide une configuration système
        /// </summary>
        Task<ConfigurationValidationResult> ValidateAsync(SystemConfiguration configuration, CancellationToken cancellationToken = default);

        /// <summary>
        /// Valide une configuration de protocole
        /// </summary>
        Task<ConfigurationValidationResult> ValidateProtocolConfigurationAsync(ProtocolConfiguration protocolConfig, CancellationToken cancellationToken = default);

        /// <summary>
        /// Obtient les règles de validation disponibles
        /// </summary>
        IEnumerable<string> GetValidationRules();

        /// <summary>
        /// Active ou désactive une règle de validation
        /// </summary>
        void SetValidationRuleEnabled(string ruleName, bool enabled);

        /// <summary>
        /// Valide avec des options personnalisées
        /// </summary>
        Task<ConfigurationValidationResult> ValidateWithOptionsAsync(SystemConfiguration configuration, Dictionary<string, object> validationOptions, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Interface pour le gestionnaire de configuration
    /// </summary>
    public interface IConfigurationManager
    {
        /// <summary>
        /// Configuration actuelle
        /// </summary>
        SystemConfiguration? CurrentConfiguration { get; }

        /// <summary>
        /// Événement déclenché lors du changement de configuration
        /// </summary>
        event EventHandler<SystemConfiguration>? ConfigurationChanged;

        /// <summary>
        /// Charge et applique une nouvelle configuration
        /// </summary>
        Task LoadConfigurationAsync(string configPath, ConfigurationLoadOptions? options = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Recharge la configuration actuelle
        /// </summary>
        Task ReloadConfigurationAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Sauvegarde la configuration actuelle
        /// </summary>
        Task SaveCurrentConfigurationAsync(string? filePath = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Obtient une valeur de configuration
        /// </summary>
        T? GetConfigurationValue<T>(string keyPath, T? defaultValue = default);

        /// <summary>
        /// Définit une valeur de configuration
        /// </summary>
        void SetConfigurationValue<T>(string keyPath, T value);
    }

    /// <summary>
    /// Interface pour le cache de configuration
    /// </summary>
    public interface IConfigurationCache
    {
        /// <summary>
        /// Met en cache une configuration
        /// </summary>
        Task SetAsync<T>(string key, T configuration, TimeSpan? expiration = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Récupère une configuration du cache
        /// </summary>
        Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);

        /// <summary>
        /// Supprime une configuration du cache
        /// </summary>
        Task RemoveAsync(string key, CancellationToken cancellationToken = default);

        /// <summary>
        /// Vide le cache
        /// </summary>
        Task ClearAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Vérifie si une clé existe dans le cache
        /// </summary>
        Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);
    }
}