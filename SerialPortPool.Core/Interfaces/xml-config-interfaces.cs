using SerialPortPool.Core.Models.Configuration;

namespace SerialPortPool.Core.Interfaces;

/// <summary>
/// Service for loading and parsing XML configuration files
/// Supports BIB → UUT → PORT → PROTOCOL hierarchy
/// </summary>
public interface IXmlConfigurationLoader
{
    /// <summary>
    /// Load complete system configuration from XML file
    /// </summary>
    /// <param name="xmlPath">Path to XML configuration file</param>
    /// <returns>Complete system configuration with all BIBs</returns>
    Task<SystemConfiguration> LoadConfigurationAsync(string xmlPath);
    
    /// <summary>
    /// Load all BIB configurations from XML file as dictionary
    /// </summary>
    /// <param name="xmlPath">Path to XML configuration file</param>
    /// <returns>Dictionary of BIB ID to BIB configuration</returns>
    Task<Dictionary<string, BibConfiguration>> LoadAllBibsAsync(string xmlPath);
    
    /// <summary>
    /// Load specific BIB configuration from XML file
    /// </summary>
    /// <param name="xmlPath">Path to XML configuration file</param>
    /// <param name="bibId">BIB identifier to load</param>
    /// <returns>Specific BIB configuration</returns>
    Task<BibConfiguration> LoadBibAsync(string xmlPath, string bibId);
    
    /// <summary>
    /// Validate XML configuration file against schema and business rules
    /// </summary>
    /// <param name="xmlPath">Path to XML configuration file</param>
    /// <returns>True if configuration is valid</returns>
    Task<ConfigurationValidationResult> ValidateConfigurationAsync(string xmlPath);
    
    /// <summary>
    /// Get all supported protocols from configuration
    /// </summary>
    /// <param name="xmlPath">Path to XML configuration file</param>
    /// <returns>List of unique protocols used in configuration</returns>
    Task<IEnumerable<string>> GetSupportedProtocolsAsync(string xmlPath);
    
    /// <summary>
    /// Find configurations matching specific criteria
    /// </summary>
    /// <param name="xmlPath">Path to XML configuration file</param>
    /// <param name="criteria">Search criteria</param>
    /// <returns>Matching port configurations</returns>
    Task<IEnumerable<PortConfiguration>> FindPortConfigurationsAsync(string xmlPath, ConfigurationSearchCriteria criteria);
}

/// <summary>
/// Service for validating XML configurations
/// </summary>
public interface IConfigurationValidator
{
    /// <summary>
    /// Validate system configuration against business rules
    /// </summary>
    /// <param name="configuration">Configuration to validate</param>
    /// <returns>Validation result with errors and warnings</returns>
    ConfigurationValidationResult ValidateConfiguration(SystemConfiguration configuration);
    
    /// <summary>
    /// Validate XML document against schema
    /// </summary>
    /// <param name="xmlPath">Path to XML file</param>
    /// <returns>Schema validation result</returns>
    Task<ConfigurationValidationResult> ValidateSchemaAsync(string xmlPath);
    
    /// <summary>
    /// Validate specific BIB configuration
    /// </summary>
    /// <param name="bibConfig">BIB configuration to validate</param>
    /// <returns>Validation result</returns>
    ConfigurationValidationResult ValidateBib(BibConfiguration bibConfig);
}