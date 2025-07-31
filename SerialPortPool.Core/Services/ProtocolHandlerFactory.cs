// ===================================================================
// PROTOCOL HANDLER FACTORY - VERSION COMPLÈTE
// Fichier: SerialPortPool.Core/Services/ProtocolHandlerFactory.cs
// ===================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SerialPortPool.Core.Interfaces;
using SerialPortPool.Core.Protocols;

namespace SerialPortPool.Core.Services
{
    /// <summary>
    /// Factory pour créer des gestionnaires de protocole
    /// Implémentation complète de IProtocolHandlerFactory
    /// </summary>
    public class ProtocolHandlerFactory : IProtocolHandlerFactory
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ProtocolHandlerFactory> _logger;

        // Dictionnaire des protocoles supportés et leurs types de gestionnaires
        private static readonly Dictionary<string, Type> SupportedProtocolHandlers = new(StringComparer.OrdinalIgnoreCase)
        {
            { "RS232", typeof(RS232ProtocolHandler) },
            { "SERIAL", typeof(RS232ProtocolHandler) },
            // Ajoutez d'autres protocoles ici au fur et à mesure
            // { "MODBUS", typeof(ModbusProtocolHandler) },
            // { "TCP", typeof(TcpProtocolHandler) },
            // { "UDP", typeof(UdpProtocolHandler) },
        };

        /// <summary>
        /// Constructeur avec injection de dépendances
        /// </summary>
        /// <param name="serviceProvider">Provider de services pour la DI</param>
        /// <param name="logger">Logger pour traçabilité</param>
        public ProtocolHandlerFactory(IServiceProvider serviceProvider, ILogger<ProtocolHandlerFactory> logger)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _logger.LogInformation("ProtocolHandlerFactory initialisé avec {Count} protocole(s) supporté(s)", 
                SupportedProtocolHandlers.Count);
        }

        #region IProtocolHandlerFactory Implementation

        /// <summary>
        /// Obtient la liste des protocoles supportés
        /// </summary>
        /// <returns>Collection des noms de protocoles supportés</returns>
        public IEnumerable<string> GetSupportedProtocols()
        {
            var protocols = SupportedProtocolHandlers.Keys.Distinct().ToList();
            
            _logger.LogDebug("Protocoles supportés demandés: {Protocols}", string.Join(", ", protocols));
            
            return protocols;
        }

        /// <summary>
        /// Vérifie si un protocole est supporté
        /// </summary>
        /// <param name="protocolName">Nom du protocole à vérifier</param>
        /// <returns>True si le protocole est supporté</returns>
        public bool IsProtocolSupported(string protocolName)
        {
            if (string.IsNullOrWhiteSpace(protocolName))
            {
                _logger.LogWarning("Vérification de protocole avec nom null ou vide");
                return false;
            }

            var isSupported = SupportedProtocolHandlers.ContainsKey(protocolName);
            
            _logger.LogDebug("Protocole '{Protocol}' supporté: {IsSupported}", protocolName, isSupported);
            
            return isSupported;
        }

        /// <summary>
        /// Crée un gestionnaire pour le protocole spécifié
        /// </summary>
        /// <param name="protocolName">Nom du protocole</param>
        /// <returns>Instance du gestionnaire de protocole</returns>
        /// <exception cref="ArgumentException">Si le nom du protocole est invalide</exception>
        /// <exception cref="NotSupportedException">Si le protocole n'est pas supporté</exception>
        /// <exception cref="InvalidOperationException">Si le gestionnaire ne peut pas être créé</exception>
        public IProtocolHandler CreateHandler(string protocolName)
        {
            if (string.IsNullOrWhiteSpace(protocolName))
            {
                var error = "Le nom du protocole ne peut pas être null ou vide";
                _logger.LogError(error);
                throw new ArgumentException(error, nameof(protocolName));
            }

            if (!IsProtocolSupported(protocolName))
            {
                var supportedProtocols = string.Join(", ", GetSupportedProtocols());
                var error = $"Le protocole '{protocolName}' n'est pas supporté. Protocoles supportés: {supportedProtocols}";
                _logger.LogError(error);
                throw new NotSupportedException(error);
            }

            try
            {
                _logger.LogInformation("Création d'un gestionnaire pour le protocole '{Protocol}'", protocolName);

                var handlerType = SupportedProtocolHandlers[protocolName];
                var handler = (IProtocolHandler)_serviceProvider.GetRequiredService(handlerType);

                _logger.LogInformation("Gestionnaire '{HandlerType}' créé avec succès pour le protocole '{Protocol}'", 
                    handlerType.Name, protocolName);

                return handler;
            }
            catch (InvalidOperationException ex)
            {
                var error = $"Impossible de créer le gestionnaire pour le protocole '{protocolName}'. " +
                           $"Vérifiez l'enregistrement des services dans le conteneur DI.";
                _logger.LogError(ex, error);
                throw new InvalidOperationException(error, ex);
            }
            catch (Exception ex)
            {
                var error = $"Erreur inattendue lors de la création du gestionnaire pour le protocole '{protocolName}'";
                _logger.LogError(ex, error);
                throw new InvalidOperationException(error, ex);
            }
        }

        #endregion

        #region Additional Methods

        /// <summary>
        /// Obtient le type de gestionnaire pour un protocole donné
        /// </summary>
        /// <param name="protocolName">Nom du protocole</param>
        /// <returns>Type du gestionnaire ou null si non supporté</returns>
        public Type? GetHandlerType(string protocolName)
        {
            if (string.IsNullOrWhiteSpace(protocolName))
                return null;

            return SupportedProtocolHandlers.TryGetValue(protocolName, out var handlerType) ? handlerType : null;
        }

        /// <summary>
        /// Enregistre un nouveau protocole et son gestionnaire
        /// </summary>
        /// <param name="protocolName">Nom du protocole</param>
        /// <param name="handlerType">Type du gestionnaire</param>
        /// <returns>True si ajouté avec succès</returns>
        public bool RegisterProtocol(string protocolName, Type handlerType)
        {
            if (string.IsNullOrWhiteSpace(protocolName) || handlerType == null)
                return false;

            if (!typeof(IProtocolHandler).IsAssignableFrom(handlerType))
            {
                _logger.LogError("Le type '{HandlerType}' doit implémenter IProtocolHandler", handlerType.Name);
                return false;
            }

            try
            {
                SupportedProtocolHandlers[protocolName] = handlerType;
                _logger.LogInformation("Protocole '{Protocol}' enregistré avec le gestionnaire '{Handler}'", 
                    protocolName, handlerType.Name);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'enregistrement du protocole '{Protocol}'", protocolName);
                return false;
            }
        }

        /// <summary>
        /// Obtient des statistiques sur les protocoles supportés
        /// </summary>
        /// <returns>Dictionnaire avec les statistiques</returns>
        public Dictionary<string, object> GetStatistics()
        {
            return new Dictionary<string, object>
            {
                { "TotalSupportedProtocols", SupportedProtocolHandlers.Count },
                { "SupportedProtocolNames", GetSupportedProtocols().ToList() },
                { "UniqueHandlerTypes", SupportedProtocolHandlers.Values.Distinct().Count() },
                { "FactoryCreatedAt", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") }
            };
        }

        #endregion
    }

    /// <summary>
    /// Extensions pour ProtocolHandlerFactory
    /// </summary>
    public static class ProtocolHandlerFactoryExtensions
    {
        /// <summary>
        /// Crée un gestionnaire avec vérification de type
        /// </summary>
        /// <typeparam name="T">Type de gestionnaire attendu</typeparam>
        /// <param name="factory">Factory de protocoles</param>
        /// <param name="protocolName">Nom du protocole</param>
        /// <returns>Gestionnaire typé</returns>
        public static T CreateHandler<T>(this IProtocolHandlerFactory factory, string protocolName) where T : class, IProtocolHandler
        {
            var handler = factory.CreateHandler(protocolName);
            
            if (handler is not T typedHandler)
            {
                throw new InvalidCastException($"Le gestionnaire créé pour '{protocolName}' n'est pas de type {typeof(T).Name}");
            }

            return typedHandler;
        }

        /// <summary>
        /// Tente de créer un gestionnaire sans lever d'exception
        /// </summary>
        /// <param name="factory">Factory de protocoles</param>
        /// <param name="protocolName">Nom du protocole</param>
        /// <param name="handler">Gestionnaire créé (ou null)</param>
        /// <returns>True si création réussie</returns>
        public static bool TryCreateHandler(this IProtocolHandlerFactory factory, string protocolName, out IProtocolHandler? handler)
        {
            handler = null;
            
            try
            {
                if (!factory.IsProtocolSupported(protocolName))
                    return false;

                handler = factory.CreateHandler(protocolName);
                return handler != null;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Obtient une description complète des protocoles supportés
        /// </summary>
        /// <param name="factory">Factory de protocoles</param>
        /// <returns>Description formatée</returns>
        public static string GetSupportedProtocolsDescription(this IProtocolHandlerFactory factory)
        {
            var protocols = factory.GetSupportedProtocols().ToList();
            
            if (!protocols.Any())
                return "Aucun protocole supporté";

            return $"Protocoles supportés ({protocols.Count}): {string.Join(", ", protocols)}";
        }
    }
}