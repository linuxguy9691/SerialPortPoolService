// SerialPortPool.Core/Interfaces/IBibConfigurationLoader.cs - CORRECTED VERSION

using SerialPortPool.Core.Models;

namespace SerialPortPool.Core.Interfaces;

/// <summary>
/// Interface for BIB configuration loading - CORRECTED VERSION
/// Week 2: XML configuration loader for BIB → UUT → PORT hierarchy
/// Compatible with BibWorkflowOrchestrator usage patterns
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
    /// ✅ FIXED: Load single BIB configuration by ID from default/configured path
    /// Used by BibWorkflowOrchestrator without xmlPath parameter
    /// </summary>
    /// <param name="bibId">BIB identifier to load</param>
    /// <returns>BIB configuration or null if not found</returns>
    Task<BibConfiguration?> LoadBibConfigurationAsync(string bibId);
    
    /// <summary>
    /// ✅ ADDED: Load single BIB configuration by ID from specific XML file
    /// Alternative method with explicit path
    /// </summary>
    /// <param name="xmlPath">Path to XML configuration file</param>
    /// <param name="bibId">BIB identifier to load</param>
    /// <returns>BIB configuration or null if not found</returns>
    Task<BibConfiguration?> LoadBibConfigurationAsync(string xmlPath, string bibId);
    
    /// <summary>
    /// Get all loaded configurations from cache/memory
    /// </summary>
    /// <returns>Dictionary of all loaded configurations</returns>
    Task<Dictionary<string, BibConfiguration>> GetLoadedConfigurationsAsync();
    
    /// <summary>
    /// Clear all loaded configurations from memory
    /// </summary>
    Task ClearConfigurationsAsync();
    
    /// <summary>
    /// ✅ ADDED: Set default XML configuration path for single-parameter methods
    /// </summary>
    /// <param name="defaultXmlPath">Default path to XML configuration file</param>
    void SetDefaultConfigurationPath(string defaultXmlPath);
    
    /// <summary>
    /// ✅ ADDED: Validate BIB configuration exists and is accessible
    /// </summary>
    /// <param name="bibId">BIB identifier to validate</param>
    /// <returns>True if BIB configuration exists and is valid</returns>
    Task<bool> ValidateBibConfigurationAsync(string bibId);
}