// ===================================================================
// SPRINT 13 BOUCHÉE #3: Service Registration Extensions - FIXED VERSION (MINIMUM CHANGES)
// File: SerialPortPool.Core/Extensions/Sprint13ServiceExtensions.cs
// Purpose: Complete Sprint 13 service registration following established patterns
// PHILOSOPHY: Minimum changes - only add missing services
// ===================================================================

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SerialPortPool.Core.Interfaces;
using SerialPortPool.Core.Services;

namespace SerialPortPool.Core.Extensions;

/// <summary>
/// SPRINT 13: Service registration extensions for Hot-Add Multi-BIB system
/// FOUNDATION EXCELLENCE: Builds on existing Sprint 6/8 patterns
/// </summary>
public static class Sprint13ServiceExtensions
{
    /// <summary>
    /// Add Sprint 13 Hot-Add Multi-BIB services with foundation excellence
    /// COMPLETE: All Sprint 13 capabilities in one registration call
    /// </summary>
    public static IServiceCollection AddSprint13HotAddServices(this IServiceCollection services)
    {
        // ✅ FOUNDATION: Use existing excellent services
        services.AddSprint6CoreServices();      // XML loading, protocol handling
        services.AddSprint8Services();          // Dynamic BIB mapping, EEPROM reading
        
        // 🔧 SPRINT 13 CRITICAL FIXES: Add missing services that Sprint 6/8 don't register
        
        // Fix 1: Register XmlBibConfigurationLoader directly (DynamicBibConfigurationService needs concrete type)
        services.AddScoped<XmlBibConfigurationLoader>();
        
        // Fix 2: Register IBibWorkflowOrchestrator (not registered in Sprint 6/8)
        services.AddScoped<IBibWorkflowOrchestrator, BibWorkflowOrchestrator>();
        
        // Fix 3: Register IPortReservationService (exists but not registered)
        services.AddScoped<IPortReservationService, PortReservationService>();
        
        // 🆕 SPRINT 13: Hot-Add specific services
        services.AddSingleton<DynamicBibConfigurationOptions>(provider =>
        {
            return DynamicBibConfigurationOptions.CreateFastDemo(); // Demo-friendly defaults
        });
        
        // 🔧 FIXED: Simplified registration without casting issues
        services.AddSingleton<IHostedService, DynamicBibConfigurationService>();
        services.AddSingleton<DynamicBibConfigurationService>(provider =>
        {
            return provider.GetServices<IHostedService>()
                          .OfType<DynamicBibConfigurationService>()
                          .First();
        });
        
        // 🎭 BONUS: Hardware simulation services
        services.AddScoped<XmlDrivenHardwareSimulator>();
        
        return services;
    }
    
    /// <summary>
    /// Add Sprint 13 services for production environment
    /// PRODUCTION: Full validation and monitoring enabled
    /// </summary>
    public static IServiceCollection AddSprint13ProductionServices(this IServiceCollection services)
    {
        // Production logging
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });
        
        // Production-grade Hot-Add configuration
        services.AddSingleton<DynamicBibConfigurationOptions>(provider =>
        {
            return new DynamicBibConfigurationOptions
            {
                WatchDirectory = "C:\\ProgramData\\SerialPortPool\\Configuration",
                DebounceDelayMs = 1000,        // Longer debounce for production
                AutoExecuteOnDiscovery = true,
                PerformInitialDiscovery = true,
                CreateSampleFiles = false      // No sample files in production
            };
        });
        
        // Core Sprint 13 services
        services.AddSprint13HotAddServices();
        
        return services;
    }
    
    /// <summary>
    /// Add Sprint 13 services for demo/development environment  
    /// DEMO: Fast, permissive, sample files enabled
    /// </summary>
    public static IServiceCollection AddSprint13DemoServices(this IServiceCollection services)
    {
        // Demo-friendly logging
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Debug); // Verbose for demo
        });
        
        // Demo configuration with samples
        services.AddSingleton<DynamicBibConfigurationOptions>(provider =>
        {
            return new DynamicBibConfigurationOptions
            {
                WatchDirectory = "Configuration\\",
                DebounceDelayMs = 200,         // Fast response for demo
                AutoExecuteOnDiscovery = true,
                PerformInitialDiscovery = true,
                CreateSampleFiles = true       // Create samples for demo
            };
        });
        
        // Core Sprint 13 services
        services.AddSprint13HotAddServices();
        
        return services;
    }
    
    /// <summary>
    /// Validate Sprint 13 service registration
    /// TESTING: Verify all services are properly registered and functional
    /// </summary>
    public static void ValidateSprint13Services(this IServiceProvider serviceProvider)
    {
        try
        {
            Console.WriteLine("🔍 Validating Sprint 13 service registration...");
            
            // Test foundation services (should already work)
            serviceProvider.ValidateSprint6Services();
            serviceProvider.ValidateSprint8Services();
            
            // 🔧 FIXED: Test Sprint 13 specific services
            
            // Test concrete XmlBibConfigurationLoader
            var xmlConfigLoader = serviceProvider.GetRequiredService<XmlBibConfigurationLoader>();
            Console.WriteLine("✅ XmlBibConfigurationLoader (concrete) registered");
            
            // Test IBibWorkflowOrchestrator
            var orchestrator = serviceProvider.GetRequiredService<IBibWorkflowOrchestrator>();
            Console.WriteLine("✅ IBibWorkflowOrchestrator registered");
            
            // Test IPortReservationService
            var portReservation = serviceProvider.GetRequiredService<IPortReservationService>();
            Console.WriteLine("✅ IPortReservationService registered");
            
            // Test DynamicBibConfigurationService
            var dynamicBibService = serviceProvider.GetRequiredService<DynamicBibConfigurationService>();
            Console.WriteLine("✅ DynamicBibConfigurationService registered");
            
            var hostedServices = serviceProvider.GetServices<IHostedService>();
            var dynamicBibHostedService = hostedServices.OfType<DynamicBibConfigurationService>().FirstOrDefault();
            if (dynamicBibHostedService != null)
            {
                Console.WriteLine("✅ DynamicBibConfigurationService registered as IHostedService");
            }
            
            var hardwareSimulator = serviceProvider.GetRequiredService<XmlDrivenHardwareSimulator>();
            Console.WriteLine("✅ XmlDrivenHardwareSimulator registered");
            
            var options = serviceProvider.GetRequiredService<DynamicBibConfigurationOptions>();
            Console.WriteLine($"✅ DynamicBibConfigurationOptions registered - Watch: {options.WatchDirectory}");
            
            // Test service statistics
            var stats = dynamicBibService.GetStatistics();
            Console.WriteLine($"✅ Service statistics accessible: {stats}");
            
            Console.WriteLine("🎉 Sprint 13 validation PASSED - Hot-Add Multi-BIB system ready!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Sprint 13 validation FAILED: {ex.Message}");
            throw;
        }
    }
}