// SerialPortPool.Core/Interfaces/IBibConfigurationLoader.cs

using SerialPortPool.Core.Models;

namespace SerialPortPool.Core.Interfaces;

/// <summary>
/// Interface for BIB configuration loading
/// Week 2: XML configuration loader for BIB → UUT → PORT hierarchy
/// </summary>
public interface IBibConfigurationLoader
{
    /// <summary>
    /// Load all BIB configurations from XML file
    /// </summary>
    /// <param name="xmlPath">Path to XML configuration file</param>
    /// <returns>Dictionary of BIB ID to BIB configuration</returns>
    Task<Dictionary<string, BibConfiguration>> LoadConfigurationsAsync(string xmlPath);
    
    /// <summary>
    /// Load single BIB configuration by ID
    /// </summary>
    /// <param name="bibId">BIB identifier to load</param>
    /// <returns>BIB configuration or null if not found</returns>
    Task<BibConfiguration?> LoadBibConfigurationAsync(string bibId);
    
    /// <summary>
    /// Get all loaded configurations
    /// </summary>
    /// <returns>Dictionary of all loaded configurations</returns>
    Task<Dictionary<string, BibConfiguration>> GetLoadedConfigurationsAsync();
    
    /// <summary>
    /// Clear all loaded configurations from memory
    /// </summary>
    Task ClearConfigurationsAsync();
}