// ===================================================================
// INTERFACE IPROTOCOL HANDLER FACTORY - VERSION CLEAN
// Fichier: SerialPortPool.Core/Interfaces/IProtocolHandlerFactory.cs
// ===================================================================

using System.Collections.Generic;

// SerialPortPool.Core/Interfaces/IProtocolHandlerFactory.cs - SECTION À AJOUTER

namespace SerialPortPool.Core.Interfaces
{
    /// <summary>
    /// Factory pour créer des gestionnaires de protocole
    /// FIXED: Added GetHandler method for compatibility
    /// </summary>
    public interface IProtocolHandlerFactory
    {
        /// <summary>
        /// Obtient la liste des protocoles supportés
        /// </summary>
        /// <returns>Collection des noms de protocoles supportés</returns>
        IEnumerable<string> GetSupportedProtocols();

        /// <summary>
        /// Vérifie si un protocole est supporté
        /// </summary>
        /// <param name="protocolName">Nom du protocole à vérifier</param>
        /// <returns>True si le protocole est supporté</returns>
        bool IsProtocolSupported(string protocolName);

        /// <summary>
        /// Crée un gestionnaire pour le protocole spécifié
        /// </summary>
        /// <param name="protocolName">Nom du protocole</param>
        /// <returns>Instance du gestionnaire de protocole</returns>
        /// <exception cref="NotSupportedException">Si le protocole n'est pas supporté</exception>
        IProtocolHandler CreateHandler(string protocolName);

        /// <summary>
        /// AJOUTÉ: Obtient un gestionnaire pour le protocole spécifié (alias pour CreateHandler)
        /// </summary>
        /// <param name="protocolName">Nom du protocole</param>
        /// <returns>Instance du gestionnaire de protocole</returns>
        /// <exception cref="NotSupportedException">Si le protocole n'est pas supporté</exception>
        IProtocolHandler GetHandler(string protocolName);
    }
}

// ===================================================================
// IMPLEMENTATION DANS ProtocolHandlerFactory.cs - MÉTHODE À AJOUTER
// ===================================================================

namespace SerialPortPool.Core.Services
{
    public class ProtocolHandlerFactory : IProtocolHandlerFactory
    {
        // ... existing methods ...

        /// <summary>
        /// AJOUTÉ: Obtient un gestionnaire pour le protocole spécifié (alias pour CreateHandler)
        /// </summary>
        /// <param name="protocolName">Nom du protocole</param>
        /// <returns>Instance du gestionnaire de protocole</returns>
        public IProtocolHandler GetHandler(string protocolName)
        {
            // Delegate to CreateHandler - same functionality
            return CreateHandler(protocolName);
        }

        // ... rest of existing methods ...
    }
}