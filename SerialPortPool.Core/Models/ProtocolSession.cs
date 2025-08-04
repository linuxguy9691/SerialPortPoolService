// ===================================================================
// PROTOCOL SESSION - VERSION CORRIGÉE
// Fichier: SerialPortPool.Core/Models/ProtocolSession.cs
// ===================================================================

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SerialPortPool.Core.Models
{
    /// <summary>
    /// Représente une session de protocole de communication
    /// </summary>
    // SerialPortPool.Core/Models/ProtocolSession.cs - SECTION À AJOUTER

    /// <summary>
    /// Représente une session de protocole de communication
    /// FIXED: Added ProtocolName property
    /// </summary>
    public class ProtocolSession
    {
        /// <summary>
        /// Identifiant unique de la session
        /// </summary>
        [Required]
        public string SessionId { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Nom du port série utilisé
        /// </summary>
        [Required]
        public string PortName { get; set; } = string.Empty;

        /// <summary>
        /// AJOUTÉ: Nom du protocole utilisé
        /// </summary>
        public string ProtocolName { get; set; } = string.Empty;

        /// <summary>
        /// Configuration du protocole
        /// </summary>
        public ProtocolConfiguration Configuration { get; set; } = new();

        // ... rest of existing properties remain the same ...

        /// <summary>
        /// Date et heure de création de la session
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Date et heure de la dernière activité
        /// </summary>
        public DateTime LastActivityAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Indique si la session est active
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// Statut de la session
        /// </summary>
        public SessionStatus Status { get; set; } = SessionStatus.Initialized;

        /// <summary>
        /// Informations sur l'erreur si la session a échoué
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Métadonnées additionnelles de la session
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new();

        /// <summary>
        /// Statistiques de la session
        /// </summary>
        public SessionStatistics Statistics { get; set; } = new();

        /// <summary>
        /// Contexte personnalisé de la session
        /// </summary>
        public object? Context { get; set; }

        /// <summary>
        /// Durée de la session
        /// </summary>
        public TimeSpan Duration => DateTime.UtcNow - CreatedAt;

        /// <summary>
        /// Temps depuis la dernière activité
        /// </summary>
        public TimeSpan TimeSinceLastActivity => DateTime.UtcNow - LastActivityAt;

        /// <summary>
        /// Met à jour l'heure de la dernière activité
        /// </summary>
        public void UpdateLastActivity()
        {
            LastActivityAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Démarre la session
        /// </summary>
        public void Start()
        {
            IsActive = true;
            Status = SessionStatus.Active;
            UpdateLastActivity();
        }

        /// <summary>
        /// Termine la session
        /// </summary>
        /// <param name="reason">Raison de la fermeture</param>
        public void Close(string? reason = null)
        {
            IsActive = false;
            Status = SessionStatus.Closed;
            if (!string.IsNullOrEmpty(reason))
            {
                ErrorMessage = reason;
            }
            UpdateLastActivity();
        }

        /// <summary>
        /// Marque la session comme ayant échoué
        /// </summary>
        /// <param name="errorMessage">Message d'erreur</param>
        public void MarkAsFailed(string errorMessage)
        {
            IsActive = false;
            Status = SessionStatus.Failed;
            ErrorMessage = errorMessage;
            UpdateLastActivity();
        }

        /// <summary>
        /// Obtient une représentation textuelle de la session
        /// </summary>
        public override string ToString()
        {
            return $"Session {SessionId} - Port: {PortName} - Status: {Status} - Duration: {Duration:hh\\:mm\\:ss}";
        }
    }

    /// <summary>
    /// Statut d'une session de protocole
    /// </summary>
    public enum SessionStatus
    {
        /// <summary>Session initialisée mais pas encore démarrée</summary>
        Initialized = 0,

        /// <summary>Session active et fonctionnelle</summary>
        Active = 1,

        /// <summary>Session fermée normalement</summary>
        Closed = 2,

        /// <summary>Session fermée en raison d'une erreur</summary>
        Failed = 3,

        /// <summary>Session en cours de fermeture</summary>
        Closing = 4,

        /// <summary>Session suspendue temporairement</summary>
        Suspended = 5
    }

    /// <summary>
    /// Statistiques d'une session de protocole
    /// </summary>
    public class SessionStatistics
    {
        /// <summary>
        /// Nombre total de commandes envoyées
        /// </summary>
        public int TotalCommandsSent { get; set; }

        /// <summary>
        /// Nombre de commandes réussies
        /// </summary>
        public int SuccessfulCommands { get; set; }

        /// <summary>
        /// Nombre de commandes échouées
        /// </summary>
        public int FailedCommands { get; set; }

        /// <summary>
        /// Nombre total d'octets envoyés
        /// </summary>
        public long BytesSent { get; set; }

        /// <summary>
        /// Nombre total d'octets reçus
        /// </summary>
        public long BytesReceived { get; set; }

        /// <summary>
        /// Temps total d'exécution des commandes
        /// </summary>
        public TimeSpan TotalExecutionTime { get; set; }

        /// <summary>
        /// Temps moyen d'exécution par commande
        /// </summary>
        public TimeSpan AverageExecutionTime => TotalCommandsSent > 0 
            ? TimeSpan.FromTicks(TotalExecutionTime.Ticks / TotalCommandsSent) 
            : TimeSpan.Zero;

        /// <summary>
        /// Taux de réussite des commandes (en pourcentage)
        /// </summary>
        public double SuccessRate => TotalCommandsSent > 0 
            ? (double)SuccessfulCommands / TotalCommandsSent * 100 
            : 0;

        /// <summary>
        /// Remet à zéro toutes les statistiques
        /// </summary>
        public void Reset()
        {
            TotalCommandsSent = 0;
            SuccessfulCommands = 0;
            FailedCommands = 0;
            BytesSent = 0;
            BytesReceived = 0;
            TotalExecutionTime = TimeSpan.Zero;
        }

        /// <summary>
        /// Enregistre l'exécution d'une commande
        /// </summary>
        /// <param name="success">Indique si la commande a réussi</param>
        /// <param name="executionTime">Temps d'exécution de la commande</param>
        /// <param name="bytesSent">Nombre d'octets envoyés</param>
        /// <param name="bytesReceived">Nombre d'octets reçus</param>
        public void RecordCommand(bool success, TimeSpan executionTime, int bytesSent = 0, int bytesReceived = 0)
        {
            TotalCommandsSent++;
            if (success)
                SuccessfulCommands++;
            else
                FailedCommands++;

            TotalExecutionTime += executionTime;
            BytesSent += bytesSent;
            BytesReceived += bytesReceived;
        }

        /// <summary>
        /// Obtient une représentation textuelle des statistiques
        /// </summary>
        public override string ToString()
        {
            return $"Commands: {TotalCommandsSent} (Success: {SuccessfulCommands}, Failed: {FailedCommands}) - " +
                   $"Success Rate: {SuccessRate:F1}% - Avg Time: {AverageExecutionTime.TotalMilliseconds:F0}ms";
        }
    }

    /// <summary>
    /// Session de communication (alias pour compatibilité)
    /// </summary>
    public class CommunicationSession : ProtocolSession
    {
        /// <summary>
        /// Statut sous forme de chaîne
        /// </summary>
        public string StatusString => Status.ToString();

        /// <summary>
        /// Indique si la session est connectée
        /// </summary>
        public bool IsConnected => IsActive && Status == SessionStatus.Active;

        /// <summary>
        /// Obtient des informations détaillées sur la session
        /// </summary>
        public string GetDetailedInfo()
        {
            return $"{ToString()} - Connected: {IsConnected} - Statistics: {Statistics}";
        }
    }
}