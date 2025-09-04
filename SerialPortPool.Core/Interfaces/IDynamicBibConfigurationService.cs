// ===================================================================
// SPRINT 13 BOUCHÉE #2: Interface for Dynamic BIB Configuration Service - FIXED
// File: SerialPortPool.Core/Interfaces/IDynamicBibConfigurationService.cs
// Purpose: Interface contract for Hot-Add BIB file monitoring and processing
// ===================================================================

using SerialPortPool.Core.Models;
using SerialPortPool.Core.Services;

namespace SerialPortPool.Core.Interfaces;

/// <summary>
/// SPRINT 13: Interface for Dynamic BIB Configuration Service
/// Provides contract for Hot-Add BIB file monitoring and processing
/// </summary>
public interface IDynamicBibConfigurationService
{
    /// <summary>
    /// Event fired when a new BIB is discovered
    /// </summary>
    event EventHandler<BibDiscoveredEventArgs>? BibDiscovered;

    /// <summary>
    /// Event fired when a BIB is removed
    /// </summary>
    event EventHandler<BibRemovedEventArgs>? BibRemoved;

    /// <summary>
    /// Event fired when a BIB is processed successfully
    /// </summary>
    event EventHandler<BibProcessedEventArgs>? BibProcessed;

    /// <summary>
    /// Event fired when a BIB processing error occurs
    /// </summary>
    event EventHandler<BibErrorEventArgs>? BibError;

    /// <summary>
    /// Get current service statistics
    /// </summary>
    DynamicBibServiceStatistics GetStatistics();

    /// <summary>
    /// Manually trigger discovery of a specific BIB file
    /// </summary>
    Task<bool> TriggerBibDiscoveryAsync(string bibId);
}