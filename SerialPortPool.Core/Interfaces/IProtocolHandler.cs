// ===================================================================
// INTERFACE IPROTOCOL HANDLER - VERSION CLEAN
// Fichier: SerialPortPool.Core/Interfaces/IProtocolHandler.cs
// ===================================================================

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SerialPortPool.Core.Models;

namespace SerialPortPool.Core.Interfaces
{
    /// <summary>
    /// Interface principale pour les gestionnaires de protocole
    /// </summary>
    public interface IProtocolHandler : IDisposable
    {
        // Propriétés de base
        string ProtocolName { get; }
        string ProtocolVersion { get; }
        string SupportedProtocol { get; }
        bool IsSessionActive { get; }
        ProtocolSession? CurrentSession { get; }

        // Méthodes de capacités
        ProtocolCapabilities GetCapabilities();
        Task<bool> CanHandleProtocolAsync(string protocolName);

        // Gestion des sessions
        Task<ProtocolSession> OpenSessionAsync(string portName, PortConfiguration config, CancellationToken cancellationToken);
        Task<ProtocolSession> OpenSessionAsync(ProtocolConfiguration config, CancellationToken cancellationToken);
        Task CloseSessionAsync(CancellationToken cancellationToken);
        Task CloseSessionAsync(ProtocolSession session, CancellationToken cancellationToken);

        // Exécution de commandes
        Task<ProtocolResponse> SendCommandAsync(ProtocolRequest request, CancellationToken cancellationToken);
        Task<ProtocolResponse> ExecuteCommandAsync(ProtocolSession session, ProtocolCommand command, CancellationToken cancellationToken);
        
        // Séquences de commandes
        Task<IEnumerable<ProtocolResponse>> SendCommandSequenceAsync(IEnumerable<ProtocolRequest> requests, CancellationToken cancellationToken);
        Task<IEnumerable<ProtocolResponse>> ExecuteCommandSequenceAsync(ProtocolSession session, IEnumerable<ProtocolCommand> commands, CancellationToken cancellationToken);

        // Tests et diagnostics
        Task<bool> TestConnectionAsync(CancellationToken cancellationToken);
        Task<bool> TestConnectivityAsync(ProtocolConfiguration config, CancellationToken cancellationToken);
        
        // Statistiques
        ProtocolStatistics GetStatistics();
    }
}