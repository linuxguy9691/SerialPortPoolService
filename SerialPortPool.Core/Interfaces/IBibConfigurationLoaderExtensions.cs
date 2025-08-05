// ===================================================================
// File: SerialPortPool.Core/Interfaces/IBibConfigurationLoaderExtensions.cs
// ===================================================================

using SerialPortPool.Core.Models;

namespace SerialPortPool.Core.Interfaces;

/// <summary>
/// Extension methods to provide exact method signatures needed for Sprint 6
/// Makes the 4 critical lines work exactly as specified
/// </summary>
public static class BibConfigurationLoaderExtensions
{
    /// <summary>
    /// 🔥 Sprint 6: Exact method signature for line 1
    /// var bibConfig = await configLoader.LoadBibAsync(xmlPath, bibId);
    /// </summary>
    public static async Task<BibConfiguration?> LoadBibAsync(
        this IBibConfigurationLoader loader, 
        string xmlPath, 
        string bibId)
    {
        return await loader.LoadBibConfigurationAsync(xmlPath, bibId);
    }

    /// <summary>
    /// 🔥 Sprint 6: Simplified method signature for common usage
    /// var bibConfig = await configLoader.LoadBibAsync(bibId);
    /// </summary>
    public static async Task<BibConfiguration?> LoadBibAsync(
        this IBibConfigurationLoader loader, 
        string bibId)
    {
        return await loader.LoadBibConfigurationAsync(bibId);
    }
}