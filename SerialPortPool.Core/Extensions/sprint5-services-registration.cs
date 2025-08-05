using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Caching.Memory;  // ✅ Correct
using Microsoft.Extensions.Logging;
using SerialPortPool.Core.Interfaces;
using SerialPortPool.Core.Services;
using SerialPortPool.Core.Models;
using SerialPortPool.Core.Protocols;  // ← AJOUTÉ: Pour RS232ProtocolHandler

namespace SerialPortPool.Core.Extensions;

/// <summary>
/// Service registration extensions for Sprint 5 services
/// Registers XML Configuration, Protocol Handlers, and Workflow Orchestration
/// </summary>
public static class Sprint5ServiceExtensions
{
    /// <summary>
    /// Add Sprint 5 services to dependency injection container
    /// </summary>
    public static IServiceCollection AddSprint5Services(this IServiceCollection services,
        ConfigurationLoadOptions? configOptions = null)
    {
        // Configuration services
        services.AddSingleton(configOptions ?? new ConfigurationLoadOptions());
        services.AddScoped<IConfigurationValidator, ConfigurationValidator>();
        services.AddScoped<IBibConfigurationLoader, XmlBibConfigurationLoader>();

        // Protocol services
        services.AddScoped<IProtocolHandlerFactory, ProtocolHandlerFactory>();
        services.AddScoped<RS232ProtocolHandler>();  // ← FIXED: Now found with proper using

        // Future Sprint 6 protocol handlers (commented for now):
        // services.AddScoped<RS485ProtocolHandler>();
        // services.AddScoped<USBProtocolHandler>();
        // services.AddScoped<CANProtocolHandler>();
        // services.AddScoped<I2CProtocolHandler>();
        // services.AddScoped<SPIProtocolHandler>();

        // Workflow orchestration (to be implemented)
        // services.AddScoped<IBibWorkflowOrchestrator, BibWorkflowOrchestrator>();

        // Enhanced port reservation (to be implemented)
        // services.AddScoped<IPortReservationService, PortReservationService>();

        // Memory cache for configuration caching
        services.AddMemoryCache();

        return services;
    }

    /// <summary>
    /// Add Sprint 5 services with custom configuration
    /// </summary>
    public static IServiceCollection AddSprint5Services(this IServiceCollection services,
        Action<ConfigurationLoadOptions> configureOptions)
    {
        var options = new ConfigurationLoadOptions();
        configureOptions(options);

        return services.AddSprint5Services(options);
    }

    /// <summary>
    /// Add Sprint 5 services for demo mode (optimized for demo application)
    /// </summary>
    public static IServiceCollection AddSprint5DemoServices(this IServiceCollection services)
    {
        var demoOptions = new ConfigurationLoadOptions
        {
            ValidateSchema = false, // Skip schema validation for demo speed
            ValidateBusinessRules = true,
            ThrowOnValidationErrors = false, // More forgiving for demo
            LoadProtocolSpecificSettings = true,
            CacheExpiration = TimeSpan.FromMinutes(10) // Longer cache for demo
        };

        return services.AddSprint5Services(demoOptions);
    }

    /// <summary>
    /// Add Sprint 5 services for production mode (full validation)
    /// </summary>
    public static IServiceCollection AddSprint5ProductionServices(this IServiceCollection services)
    {
        var productionOptions = new ConfigurationLoadOptions
        {
            ValidateSchema = true,
            ValidateBusinessRules = true,
            ThrowOnValidationErrors = true,
            LoadProtocolSpecificSettings = true,
            CacheExpiration = TimeSpan.FromMinutes(5)
        };

        return services.AddSprint5Services(productionOptions);
    }

    /// <summary>
    /// Validate Sprint 5 service registration
    /// FIXED: Remove static type usage in generic method
    /// </summary>
    public static void ValidateSprint5Services(this IServiceProvider serviceProvider)
    {
        try
        {
            // Test service resolution
            var configLoader = serviceProvider.GetRequiredService<IXmlConfigurationLoader>();
            var validator = serviceProvider.GetRequiredService<IConfigurationValidator>();
            var protocolFactory = serviceProvider.GetRequiredService<IProtocolHandlerFactory>();
            var cache = serviceProvider.GetRequiredService<IMemoryCache>();

            // Test protocol handler creation
            var supportedProtocols = protocolFactory.GetSupportedProtocols();

            if (!supportedProtocols.Any())
            {
                throw new InvalidOperationException("No protocol handlers registered");
            }

            // Test RS232 handler creation
            if (protocolFactory.IsProtocolSupported("rs232"))
            {
                using var rs232Handler = protocolFactory.CreateHandler("rs232");
                if (rs232Handler.SupportedProtocol != "rs232")
                {
                    throw new InvalidOperationException("RS232 handler configuration error");
                }
            }

            // Log successful validation (if logger available)
            try
            {
                // FIXED: Don't use Sprint5ServiceExtensions as generic type argument
                var loggerFactory = serviceProvider.GetService<ILoggerFactory>();
                var logger = loggerFactory?.CreateLogger("Sprint5ServiceExtensions");
                logger?.LogInformation("✅ Sprint 5 services validated successfully. Protocols: {Protocols}",
                    string.Join(", ", supportedProtocols));
            }
            catch
            {
                // Logger not available, ignore
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Sprint 5 service validation failed: {ex.Message}", ex);
        }
    }
}

/// <summary>
/// Extension methods for configuring Sprint 5 options
/// </summary>
public static class ConfigurationLoadOptionsExtensions
{
    /// <summary>
    /// Configure for development environment
    /// </summary>
    public static ConfigurationLoadOptions ForDevelopment(this ConfigurationLoadOptions options)
    {
        options.ValidateSchema = false;
        options.ValidateBusinessRules = true;
        options.ThrowOnValidationErrors = false;
        options.LoadProtocolSpecificSettings = true;
        options.CacheExpiration = TimeSpan.FromMinutes(1); // Short cache for development

        return options;
    }

    /// <summary>
    /// Configure for production environment
    /// </summary>
    public static ConfigurationLoadOptions ForProduction(this ConfigurationLoadOptions options)
    {
        options.ValidateSchema = true;
        options.ValidateBusinessRules = true;
        options.ThrowOnValidationErrors = true;
        options.LoadProtocolSpecificSettings = true;
        options.CacheExpiration = TimeSpan.FromMinutes(15); // Longer cache for production

        return options;
    }

    /// <summary>
    /// Configure for demo/presentation mode
    /// </summary>
    public static ConfigurationLoadOptions ForDemo(this ConfigurationLoadOptions options)
    {
        options.ValidateSchema = false;
        options.ValidateBusinessRules = false; // Skip validation for demo speed
        options.ThrowOnValidationErrors = false;
        options.LoadProtocolSpecificSettings = true;
        options.CacheExpiration = TimeSpan.FromHours(1); // Very long cache for demo

        return options;
    }
}