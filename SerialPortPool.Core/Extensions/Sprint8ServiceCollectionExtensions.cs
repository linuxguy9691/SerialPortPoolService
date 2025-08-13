// ===================================================================
// SPRINT 8: Service Collection Extensions - ZERO TOUCH Approach
// File: SerialPortPool.Core/Extensions/Sprint8ServiceCollectionExtensions.cs
// Purpose: Simple service registration for Sprint 8 features  
// Philosophy: "Zero Touch" - Additive registration, preserves existing services
// ===================================================================

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SerialPortPool.Core.Interfaces;
using SerialPortPool.Core.Services;
using SerialPortPool.Core.Models;

namespace SerialPortPool.Core.Extensions;

/// <summary>
/// SPRINT 8: Service collection extensions - ZERO TOUCH approach
/// ADDITIVE: Adds Sprint 8 services without modifying existing registrations
/// </summary>
public static class Sprint8ServiceCollectionExtensions
{
    /// <summary>
    /// Add Sprint 8 services using existing foundation (ZERO TOUCH)
    /// PRESERVES: All existing service registrations unchanged
    /// ADDS: Only new Sprint 8 dynamic BIB mapping capabilities
    /// </summary>
    public static IServiceCollection AddSprint8Services(this IServiceCollection services)
    {
        // ✨ SPRINT 8: New EEPROM services
        services.AddScoped<IFtdiEepromReader, FtdiEepromReader>();
        
        // ✨ SPRINT 8: Dynamic BIB mapping
        services.AddScoped<IDynamicBibMappingService, DynamicBibMappingService>();
        
        // ✨ SPRINT 8: Wrapper service (uses existing discovery via DI)
        services.AddScoped<ISprint8DynamicBibService, Sprint8DynamicBibService>();
        
        return services;
    }

    /// <summary>
    /// Add complete Sprint 8 setup with existing Sprint 6 foundation
    /// ONE-LINE: Complete setup including existing services + Sprint 8 enhancements
    /// </summary>
    public static IServiceCollection AddSprint8CompleteServices(this IServiceCollection services)
    {
        // ✅ FOUNDATION: Use existing Sprint 6 services (ZERO TOUCH)
        services.AddSprint6CoreServices();
        
        // ✨ ENHANCEMENT: Add Sprint 8 services on top
        services.AddSprint8Services();
        
        return services;
    }

    /// <summary>
    /// Add Sprint 8 services in DEMO mode
    /// DEMO: Enhanced logging and permissive validation for demonstration
    /// </summary>
    public static IServiceCollection AddSprint8DemoServices(this IServiceCollection services)
    {
        // Enhanced logging for demo
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Debug); // More verbose for demo
        });

        // Demo-friendly validation configuration
        services.AddSingleton(new PortValidationConfiguration
        {
            RequireFtdiDevice = false,        // Accept any device for demo
            Require4232HChip = false,
            StrictValidation = false,
            MinimumValidationScore = 50       // Lower threshold for demo
        });

        // Complete Sprint 8 setup
        services.AddSprint8CompleteServices();

        return services;
    }

    /// <summary>
    /// Add Sprint 8 services for PRODUCTION use
    /// PRODUCTION: Optimized logging and strict validation
    /// </summary>
    public static IServiceCollection AddSprint8ProductionServices(this IServiceCollection services)
    {
        // Production logging
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information); // Production level
        });

        // Production validation configuration
        services.AddSingleton(new PortValidationConfiguration
        {
            RequireFtdiDevice = true,         // Only FTDI devices
            Require4232HChip = true,          // Only 4232H/HL chips
            StrictValidation = true,
            MinimumValidationScore = 90       // High threshold for production
        });

        // Complete Sprint 8 setup
        services.AddSprint8CompleteServices();

        return services;
    }

    /// <summary>
    /// Validate Sprint 8 service registration
    /// TESTING: Verify all Sprint 8 services are properly registered
    /// </summary>
    public static void ValidateSprint8Services(this IServiceProvider serviceProvider)
    {
        try
        {
            Console.WriteLine("🔍 Validating Sprint 8 service registration...");

            // Test Sprint 8 core services
            var eepromReader = serviceProvider.GetRequiredService<IFtdiEepromReader>();
            Console.WriteLine("✅ IFtdiEepromReader registered");

            var dynamicBibMapping = serviceProvider.GetRequiredService<IDynamicBibMappingService>();
            Console.WriteLine("✅ IDynamicBibMappingService registered");

            var sprint8Service = serviceProvider.GetRequiredService<ISprint8DynamicBibService>();
            Console.WriteLine("✅ ISprint8DynamicBibService registered");

            // Test that existing services still work (ZERO TOUCH validation)
            var existingDiscovery = serviceProvider.GetRequiredService<ISerialPortDiscovery>();
            Console.WriteLine("✅ Existing ISerialPortDiscovery still available");

            // Test dynamic BIB mapping statistics
            var stats = dynamicBibMapping.GetMappingStatistics();
            Console.WriteLine($"✅ Dynamic BIB mapping initialized: {stats.GetSummary()}");

            Console.WriteLine("🎉 Sprint 8 validation PASSED - All services ready!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Sprint 8 validation FAILED: {ex.Message}");
            throw;
        }
    }
}