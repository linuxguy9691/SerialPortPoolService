// ===================================================================
// INTERFACE IPROTOCOL HANDLER FACTORY - VERSION CLEAN
// Fichier: SerialPortPool.Core/Interfaces/IProtocolHandlerFactory.cs
// ===================================================================

using System.Collections.Generic;

namespace SerialPortPool.Core.Interfaces
{
    /// <summary>
    /// Factory pour créer des gestionnaires de protocole
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
    }
}