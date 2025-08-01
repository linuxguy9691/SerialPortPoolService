// ===================================================================
// PROTOCOL TYPES - VERSION NETTOYÉE (SANS DUPLICATION)
// Fichier: SerialPortPool.Core/Models/ProtocolTypes.cs
// ===================================================================

using System;
using System.Collections.Generic;

namespace SerialPortPool.Core.Models
{
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

        public static ProtocolResponse FromSuccess(byte[] data) => new() 
        { 
            Success = true, 
            Data = data 
        };

        public static ProtocolResponse FromError(string message) => new() 
        { 
            Success = false, 
            ErrorMessage = message 
        };
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

    // NOTE: CommunicationSession est maintenant définie uniquement dans ProtocolSession.cs
    // NOTE: ProtocolCapabilities est définie dans CommandResult.cs
    // pour éviter la duplication CS0101
}