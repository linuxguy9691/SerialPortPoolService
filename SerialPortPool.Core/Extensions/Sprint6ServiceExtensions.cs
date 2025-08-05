// ===================================================================
// SIMPLIFIED: Sprint6ServiceExtensions.cs - Core services only
// File: SerialPortPool.Core/Extensions/Sprint6ServiceExtensions.cs
// ===================================================================

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using SerialPortPool.Core.Interfaces;
using SerialPortPool.Core.Services;
using SerialPortPool.Core.Protocols;
using SerialPortPool.Core.Models;

namespace SerialPortPool.Core.Extensions;

/// <summary>
/// Sprint 6 - Core Services Registration (Simplified)
/// Registers ONLY the essential services needed for the 4 critical lines
/// </summary>
public static class Sprint6ServiceExtensions
{
    /// <summary>
    /// Add Sprint 6 Core Services - PRODUCTION READY
    /// </summary>
    public static IServiceCollection AddSprint6CoreServices(this IServiceCollection services)
    {
        // 1️⃣ Memory Cache (required by XML services)
        services.AddMemoryCache();

        // 2️⃣ XML Configuration Services
        services.AddScoped<IBibConfigurationLoader, XmlBibConfigurationLoader>();
        services.AddScoped<IXmlConfigurationLoader, XmlConfigurationLoader>();
        services.AddScoped<IConfigurationValidator, ConfigurationValidator>();

        // 3️⃣ Protocol Handler Services  
        services.AddScoped<IProtocolHandlerFactory, ProtocolHandlerFactory>();
        services.AddScoped<RS232ProtocolHandler>();

        // 4️⃣ Essential Supporting Services
        services.AddScoped<ISerialPortDiscovery, EnhancedSerialPortDiscoveryService>();
        services.AddScoped<IFtdiDeviceReader, FtdiDeviceReader>();
        services.AddScoped<ISerialPortValidator, SerialPortValidator>();
        services.AddScoped<ISystemInfoCache, SystemInfoCache>();
        services.AddScoped<ISerialPortPool, SerialPortPool>();

        return services;
    }

    /// <summary>
    /// Add Sprint 6 Demo Services - Optimized for demo
    /// </summary>
    public static IServiceCollection AddSprint6DemoServices(this IServiceCollection services)
    {
        // Core services first
        services.AddSprint6CoreServices();

        // Demo-friendly validation config
        services.AddSingleton(new PortValidationConfiguration
        {
            RequireFtdiDevice = false,      // Allow any device for demo
            Require4232HChip = false,
            StrictValidation = false,
            MinimumValidationScore = 50
        });

        return services;
    }

    /// <summary>
    /// Validate Sprint 6 service registration
    /// </summary>
    public static void ValidateSprint6Services(this IServiceProvider serviceProvider)
    {
        try
        {
            Console.WriteLine("🔍 Validating Sprint 6 service registration...");

            // Test critical services
            var configLoader = serviceProvider.GetRequiredService<IBibConfigurationLoader>();
            Console.WriteLine("✅ IBibConfigurationLoader registered");

            var protocolFactory = serviceProvider.GetRequiredService<IProtocolHandlerFactory>();
            var protocols = protocolFactory.GetSupportedProtocols();
            Console.WriteLine($"✅ IProtocolHandlerFactory registered - Protocols: {string.Join(", ", protocols)}");

            // Test RS232 handler creation
            if (protocolFactory.IsProtocolSupported("rs232"))
            {
                var rs232Handler = protocolFactory.CreateHandler("rs232");
                Console.WriteLine("✅ RS232ProtocolHandler creation successful");
                rs232Handler.Dispose();
            }

            // Test cache
            var cache = serviceProvider.GetRequiredService<IMemoryCache>();
            Console.WriteLine("✅ IMemoryCache registered");

            Console.WriteLine("🎉 Sprint 6 validation PASSED - All systems ready!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Sprint 6 validation FAILED: {ex.Message}");
            throw;
        }
    }
}