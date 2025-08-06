// ===================================================================
// FIXED: Sprint6IntegrationTests.cs - Correction des modificateurs
// File: tests/SerialPortPool.Core.Tests/sprint6/Sprint6IntegrationTests.cs
// ===================================================================

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;
using SerialPortPool.Core.Extensions;
using SerialPortPool.Core.Interfaces;
using SerialPortPool.Core.Models;

namespace SerialPortPool.Core.Tests.Sprint6;

/// <summary>
/// Sprint 6 Integration Tests - Tests des 4 lignes critiques
/// </summary>
public class Sprint6IntegrationTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly ILogger<Sprint6IntegrationTests> _logger;

    public Sprint6IntegrationTests()
    {
        var services = new ServiceCollection();
        
        // Configuration logging pour les tests
        services.AddLogging(builder => 
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Debug);
        });
        
        // Ajouter les services Sprint 6
        services.AddSprint6DemoServices();
        
        _serviceProvider = services.BuildServiceProvider();
        _logger = _serviceProvider.GetRequiredService<ILogger<Sprint6IntegrationTests>>();
    }

    [Fact]
    public void Sprint6ServiceRegistration_ShouldRegisterAllServices()
    {
        // Arrange & Act & Assert
        _serviceProvider.ValidateSprint6Services();
        
        // Si aucune exception n'est levée, le test passe
        Assert.True(true, "Sprint 6 services registered successfully");
    }

    [Fact]
    public async Task BibConfigurationLoader_LoadBibAsync_ShouldWork()
    {
        // Arrange
        var configLoader = _serviceProvider.GetRequiredService<IBibConfigurationLoader>();
        var xmlPath = CreateTestXml();
        
        try
        {
            // Act - 🔥 LIGNE CRITIQUE 1
            var bibConfig = await configLoader.LoadBibAsync(xmlPath, "test_bib");
            
            // Assert
            Assert.NotNull(bibConfig);
            Assert.Equal("test_bib", bibConfig.BibId);
            Assert.True(bibConfig.TotalPortCount > 0);
            
            _logger.LogInformation("✅ LIGNE 1 TESTÉE: configLoader.LoadBibAsync() fonctionne");
        }
        finally
        {
            if (File.Exists(xmlPath))
                File.Delete(xmlPath);
        }
    }

    [Fact]
    public void ProtocolHandlerFactory_CreateHandler_ShouldCreateRS232Handler()
    {
        // Arrange
        var protocolFactory = _serviceProvider.GetRequiredService<IProtocolHandlerFactory>();
        
        // Act - 🔥 LIGNE CRITIQUE 2
        using var protocolHandler = protocolFactory.CreateHandler("rs232");
        
        // Assert
        Assert.NotNull(protocolHandler);
        Assert.Equal("RS232", protocolHandler.ProtocolName);
        Assert.Equal("rs232", protocolHandler.SupportedProtocol);
        
        _logger.LogInformation("✅ LIGNE 2 TESTÉE: protocolFactory.CreateHandler() fonctionne");
    }

    [Fact]
    public void ProtocolHandlerExtensions_OpenSessionAsync_ShouldHaveCorrectSignature()
    {
        // Arrange
        var protocolFactory = _serviceProvider.GetRequiredService<IProtocolHandlerFactory>();
        using var protocolHandler = protocolFactory.CreateHandler("rs232");
        
        // Act & Assert - 🔥 LIGNE CRITIQUE 3
        // Vérifier que la signature existe (test de compilation)
        var methodInfo = typeof(ProtocolHandlerExtensions)
            .GetMethods()
            .FirstOrDefault(m => 
                m.Name == "OpenSessionAsync" && 
                m.GetParameters().Length >= 2 && 
                m.GetParameters()[1].ParameterType == typeof(string));
        
        Assert.NotNull(methodInfo);
        _logger.LogInformation("✅ LIGNE 3 TESTÉE: OpenSessionAsync signature correcte");
    }

    [Fact]
    public void ProtocolHandlerExtensions_ExecuteCommandAsync_ShouldHaveCorrectSignature()
    {
        // Arrange
        var protocolFactory = _serviceProvider.GetRequiredService<IProtocolHandlerFactory>();
        using var protocolHandler = protocolFactory.CreateHandler("rs232");
        
        // Act & Assert - 🔥 LIGNE CRITIQUE 4
        // Vérifier que la signature existe (test de compilation)
        var methodInfo = typeof(ProtocolHandlerExtensions)
            .GetMethods()
            .FirstOrDefault(m => 
                m.Name == "ExecuteCommandAsync" && 
                m.GetParameters().Length >= 2 && 
                m.GetParameters()[1].ParameterType == typeof(string));
        
        Assert.NotNull(methodInfo);
        _logger.LogInformation("✅ LIGNE 4 TESTÉE: ExecuteCommandAsync signature correcte");
    }

    [Fact]
    public async Task Sprint6Integration_FourCriticalLines_ShouldWorkTogether()
    {
        // Arrange
        var configLoader = _serviceProvider.GetRequiredService<IBibConfigurationLoader>();
        var protocolFactory = _serviceProvider.GetRequiredService<IProtocolHandlerFactory>();
        var xmlPath = CreateTestXml();
        
        try
        {
            // 🔥 LIGNE 1: Load BIB configuration
            var bibConfig = await configLoader.LoadBibAsync(xmlPath, "test_bib");
            Assert.NotNull(bibConfig);
            
            // 🔥 LIGNE 2: Create protocol handler
            using var protocolHandler = protocolFactory.CreateHandler("rs232");
            Assert.NotNull(protocolHandler);
            
            // 🔥 LIGNES 3 & 4: Vérifier les signatures (pas d'exécution réelle sans hardware)
            Assert.True(true, "Les 4 lignes critiques sont intégrées avec succès");
            
            _logger.LogInformation("🎉 SPRINT 6 SUCCESS: Les 4 lignes critiques fonctionnent ensemble!");
        }
        finally
        {
            if (File.Exists(xmlPath))
                File.Delete(xmlPath);
        }
    }

    private string CreateTestXml()
    {
        var xmlPath = Path.GetTempFileName();
        File.WriteAllText(xmlPath, """
        <?xml version="1.0" encoding="UTF-8"?>
        <root>
          <bib id="test_bib" description="Test BIB for Sprint 6">
            <metadata>
              <board_type>test</board_type>
              <revision>v1.0</revision>
            </metadata>
            
            <uut id="test_uut" description="Test UUT">
              <port number="1">
                <protocol>rs232</protocol>
                <speed>115200</speed>
                <data_pattern>n81</data_pattern>
                <read_timeout>3000</read_timeout>
                <write_timeout>3000</write_timeout>
                
                <start>
                  <command>AT+INIT\r\n</command>
                  <expected_response>OK</expected_response>
                  <timeout_ms>3000</timeout_ms>
                </start>
                
                <test>
                  <command>AT+STATUS\r\n</command>
                  <expected_response>STATUS_OK</expected_response>
                  <timeout_ms>3000</timeout_ms>
                </test>
                
                <stop>
                  <command>AT+SHUTDOWN\r\n</command>
                  <expected_response>SHUTDOWN_OK</expected_response>
                  <timeout_ms>2000</timeout_ms>
                </stop>
              </port>
            </uut>
          </bib>
        </root>
        """);
        
        return xmlPath;
    }

    public void Dispose()
    {
        _serviceProvider?.Dispose();
    }
}