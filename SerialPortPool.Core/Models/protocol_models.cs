// ===================================================================
// PROTOCOL MODELS - TYPES MANQUANTS
// Fichier: SerialPortPool.Core/Models/ProtocolModels.cs
// ===================================================================

using System;
using System.Collections.Generic;

namespace SerialPortPool.Core.Models
{
    /// <summary>
    /// Représente une session de communication protocole
    /// </summary>
    public class ProtocolSession
    {
        public string SessionId { get; set; } = Guid.NewGuid().ToString();
        public string PortName { get; set; } = string.Empty;
        public ProtocolConfiguration Configuration { get; set; } = new();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; }
        public object? Context { get; set; }
    }

    /// <summary>
    /// Session de communication (alias pour compatibilité)
    /// </summary>
    public class CommunicationSession : ProtocolSession
    {
        public string Status { get; set; } = "Initialized";
        public TimeSpan Duration => DateTime.UtcNow - CreatedAt;
    }

    /// <summary>
    /// Configuration d'un protocole
    /// </summary>
    public class ProtocolConfiguration
    {
        public string PortName { get; set; } = string.Empty;
        public int BaudRate { get; set; } = 9600;
        public int DataBits { get; set; } = 8;
        public string Parity { get; set; } = "None";
        public string StopBits { get; set; } = "One";
        public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(5);
        public Dictionary<string, object> Parameters { get; set; } = new();
    }

    /// <summary>
    /// Configuration de port (version simplifiée)
    /// </summary>
    public class PortConfiguration
    {
        public int BaudRate { get; set; } = 9600;
        public int DataBits { get; set; } = 8;
        public string Parity { get; set; } = "None";
        public string StopBits { get; set; } = "One";
        public TimeSpan ReadTimeout { get; set; } = TimeSpan.FromSeconds(1);
        public TimeSpan WriteTimeout { get; set; } = TimeSpan.FromSeconds(1);
    }

    /// <summary>
    /// Requête de protocole
    /// </summary>
    public class ProtocolRequest
    {
        public string CommandId { get; set; } = Guid.NewGuid().ToString();
        public string Command { get; set; } = string.Empty;
        public byte[] Data { get; set; } = Array.Empty<byte>();
        public Dictionary<string, object> Parameters { get; set; } = new();
        public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(5);
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Réponse de protocole
    /// </summary>
    public class ProtocolResponse
    {
        public string RequestId { get; set; } = string.Empty;
        public bool Success { get; set; }
        public byte[] Data { get; set; } = Array.Empty<byte>();
        public string DataAsString => System.Text.Encoding.UTF8.GetString(Data);
        public string ErrorMessage { get; set; } = string.Empty;
        public TimeSpan ExecutionTime { get; set; }
        public DateTime CompletedAt { get; set; } = DateTime.UtcNow;
        public Dictionary<string, object> Metadata { get; set; } = new();

        public static ProtocolResponse Success(byte[] data) => new() 
        { 
            Success = true, 
            Data = data 
        };

        public static ProtocolResponse Error(string message) => new() 
        { 
            Success = false, 
            ErrorMessage = message 
        };
    }

    /// <summary>
    /// Commande de protocole
    /// </summary>
    public class ProtocolCommand
    {
        public string Name { get; set; } = string.Empty;
        public string CommandText { get; set; } = string.Empty;
        public byte[] Data { get; set; } = Array.Empty<byte>();
        public Dictionary<string, object> Parameters { get; set; } = new();
        public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(5);
        public bool ExpectsResponse { get; set; } = true;
    }

    /// <summary>
    /// Capacités d'un protocole
    /// </summary>
    public class ProtocolCapabilities
    {
        public string ProtocolName { get; set; } = string.Empty;
        public string Version { get; set; } = "1.0";
        public bool SupportsAsyncOperations { get; set; } = true;
        public bool SupportsSequenceCommands { get; set; } = true;
        public bool SupportsBidirectionalCommunication { get; set; } = true;
        public int MaxConcurrentSessions { get; set; } = 1;
        public TimeSpan DefaultTimeout { get; set; } = TimeSpan.FromSeconds(5);
        public List<string> SupportedCommands { get; set; } = new();
        public List<int> SupportedBaudRates { get; set; } = new() { 9600, 19200, 38400, 57600, 115200 };
        public Dictionary<string, object> ExtendedProperties { get; set; } = new();
    }

    /// <summary>
    /// Statistiques de protocole
    /// </summary>
    public class ProtocolStatistics
    {
        public int TotalCommands { get; set; }
        public int SuccessfulCommands { get; set; }
        public int FailedCommands { get; set; }
        public int TimeoutCommands { get; set; }
        public TimeSpan TotalExecutionTime { get; set; }
        public TimeSpan AverageExecutionTime => TotalCommands > 0 
            ? TimeSpan.FromTicks(TotalExecutionTime.Ticks / TotalCommands) 
            : TimeSpan.Zero;
        public DateTime LastCommandTime { get; set; }
        public DateTime StatisticsStartTime { get; set; } = DateTime.UtcNow;
        
        public double SuccessRate => TotalCommands > 0 
            ? (double)SuccessfulCommands / TotalCommands * 100 
            : 0;

        public void Reset()
        {
            TotalCommands = SuccessfulCommands = FailedCommands = TimeoutCommands = 0;
            TotalExecutionTime = TimeSpan.Zero;
            StatisticsStartTime = DateTime.UtcNow;
        }
    }
}