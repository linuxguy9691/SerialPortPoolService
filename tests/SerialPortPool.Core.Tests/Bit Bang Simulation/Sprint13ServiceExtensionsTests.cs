using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SerialPortPool.Core.Extensions;
using SerialPortPool.Core.Models;
using SerialPortPool.Core.Services;
using Xunit;

namespace SerialPortPool.Core.Tests.Extensions;

/// <summary>
/// Tests pour Sprint13ServiceExtensions - Validation des extensions DI
/// </summary>
public class Sprint13ServiceExtensionsTests
{
    #region AddSprint13HotAddServices Tests

    [Fact]
    public void AddSprint13HotAddServices_RegistersAllRequiredServices()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddSprint13HotAddServices();

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        
        // Verify core Sprint 13 services
        Assert.NotNull(serviceProvider.GetService<DynamicBibConfigurationOptions>());
        Assert.NotNull(serviceProvider.GetService<DynamicBibConfigurationService>());
        Assert.NotNull(serviceProvider.GetService<XmlDrivenHardwareSimulator>());
        
        // Verify it's registered as IHostedService
        var hostedServices = serviceProvider.GetServices<IHostedService>();
        Assert.Contains(hostedServices, s => s is DynamicBibConfigurationService);
    }

    [Fact]
    public void AddSprint13HotAddServices_WithMultipleCalls_RegistersCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act - Call multiple times to test idempotency
        services.AddSprint13HotAddServices();
        services.AddSprint13HotAddServices();

        // Assert - Should not throw and should work correctly
        var serviceProvider = services.BuildServiceProvider();
        Assert.NotNull(serviceProvider.GetService<DynamicBibConfigurationOptions>());
    }

    [Fact]
    public void AddSprint13HotAddServices_ConfiguresDefaultOptions()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddSprint13HotAddServices();

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<DynamicBibConfigurationOptions>();
        
        Assert.NotNull(options);
        Assert.NotNull(options.WatchDirectory);
        Assert.True(options.DebounceDelayMs > 0);
        Assert.True(options.AutoExecuteOnDiscovery);
        Assert.True(options.PerformInitialDiscovery);
    }

    #endregion

    #region AddSprint13ProductionServices Tests

    [Fact]
    public void AddSprint13ProductionServices_ConfiguresProductionSettings()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddSprint13ProductionServices();

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<DynamicBibConfigurationOptions>();
        
        Assert.NotNull(options);
        Assert.Equal("C:\\ProgramData\\SerialPortPool\\Configuration", options.WatchDirectory);
        Assert.Equal(1000, options.DebounceDelayMs); // Longer debounce for production
        Assert.True(options.AutoExecuteOnDiscovery);
        Assert.True(options.PerformInitialDiscovery);
        Assert.False(options.CreateSampleFiles); // No sample files in production
    }

    [Fact]
    public void AddSprint13ProductionServices_ConfiguresLogging()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddSprint13ProductionServices();

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var logger = serviceProvider.GetService<ILogger<DynamicBibConfigurationService>>();
        
        Assert.NotNull(logger);
    }

    #endregion

    #region AddSprint13DemoServices Tests

    [Fact]
    public void AddSprint13DemoServices_ConfiguresDemoSettings()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddSprint13DemoServices();

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<DynamicBibConfigurationOptions>();
        
        Assert.NotNull(options);
        Assert.Equal("Configuration\\", options.WatchDirectory);
        Assert.Equal(200, options.DebounceDelayMs); // Fast response for demo
        Assert.True(options.AutoExecuteOnDiscovery);
        Assert.True(options.PerformInitialDiscovery);
        Assert.True(options.CreateSampleFiles); // Create samples for demo
    }

    [Fact]
    public void AddSprint13DemoServices_ConfiguresDebugLogging()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddSprint13DemoServices();

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        
        // Verify logging is configured (even if we can't test exact level)
        var logger = serviceProvider.GetService<ILogger<DynamicBibConfigurationService>>();
        Assert.NotNull(logger);
    }

    #endregion

    #region ValidateSprint13Services Tests

    [Fact]
    public void ValidateSprint13Services_WithCompleteRegistration_Succeeds()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSprint13DemoServices(); // This should register everything needed
        var serviceProvider = services.BuildServiceProvider();

        // Act & Assert - Should not throw
        serviceProvider.ValidateSprint13Services();
        
        // If we get here, validation passed
        Assert.True(true);
    }

    [Fact]
    public void ValidateSprint13Services_WithMissingServices_ThrowsException()
    {
        // Arrange
        var services = new ServiceCollection();
        // Don't register Sprint 13 services
        var serviceProvider = services.BuildServiceProvider();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => 
            serviceProvider.ValidateSprint13Services());
    }

    [Fact]
    public void ValidateSprint13Services_WithPartialRegistration_ThrowsException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        // Only register options, not the full service stack
        services.AddSingleton(DynamicBibConfigurationOptions.CreateFastDemo());
        var serviceProvider = services.BuildServiceProvider();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => 
            serviceProvider.ValidateSprint13Services());
    }

    #endregion

    #region Integration with Foundation Services Tests

    [Fact]
    public void AddSprint13Services_IntegratesWithSprint6And8Services()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddSprint13HotAddServices();

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        
        // Sprint 13 should have triggered registration of foundation services
        // This is implicit in the implementation, but we can verify key services exist
        var options = serviceProvider.GetService<DynamicBibConfigurationOptions>();
        var dynamicService = serviceProvider.GetService<DynamicBibConfigurationService>();
        
        Assert.NotNull(options);
        Assert.NotNull(dynamicService);
        
        // Verify the services can be resolved without circular dependencies
        Assert.NotNull(serviceProvider.GetServices<IHostedService>());
    }

    #endregion

    #region Configuration Options Tests

    [Fact]
    public void DynamicBibConfigurationOptions_CreateFastDemo_HasCorrectSettings()
    {
        // Act
        var options = DynamicBibConfigurationOptions.CreateFastDemo();

        // Assert
        Assert.NotNull(options);
        Assert.True(options.DebounceDelayMs <= 500); // Fast demo should be quick
        Assert.True(options.AutoExecuteOnDiscovery);
        Assert.True(options.PerformInitialDiscovery);
        Assert.True(options.CreateSampleFiles);
    }

    [Fact]
    public void DynamicBibConfigurationOptions_CreateFastDemo_IsReproducible()
    {
        // Act
        var options1 = DynamicBibConfigurationOptions.CreateFastDemo();
        var options2 = DynamicBibConfigurationOptions.CreateFastDemo();

        // Assert
        Assert.Equal(options1.DebounceDelayMs, options2.DebounceDelayMs);
        Assert.Equal(options1.AutoExecuteOnDiscovery, options2.AutoExecuteOnDiscovery);
        Assert.Equal(options1.PerformInitialDiscovery, options2.PerformInitialDiscovery);
        Assert.Equal(options1.CreateSampleFiles, options2.CreateSampleFiles);
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public void AddSprint13Services_WithNullServiceCollection_ThrowsArgumentNullException()
    {
        // Arrange
        ServiceCollection? services = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            services!.AddSprint13HotAddServices());
    }

    [Fact]
    public void ValidateSprint13Services_WithNullServiceProvider_ThrowsArgumentNullException()
    {
        // Arrange
        ServiceProvider? serviceProvider = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            serviceProvider!.ValidateSprint13Services());
    }

    #endregion
}
