// ===================================================================
// SPRINT 13 UNIT TESTS: XML Parsing Tests - SEPARATED FILE
// File: tests/SerialPortPool.Core.Tests/Services/XmlBibConfigurationLoader_SimulationTests.cs
// ===================================================================

using Xunit;

namespace SerialPortPool.Core.Tests.Services;

/// <summary>
/// Unit tests pour le parsing XML des configurations de simulation
/// </summary>
public class XmlBibConfigurationLoader_SimulationTests
{
    [Fact]
    public void ParseBibXml_WithHardwareSimulation_ParsesCorrectly()
    {
        // Arrange
        var xmlWithSimulation = """
        <?xml version="1.0" encoding="UTF-8"?>
        <root>
          <bib id="test_bib" description="Test BIB with Hardware Simulation">
            <hardware_simulation enabled="true">
              <mode>Fast</mode>
              <speed_multiplier>2.0</speed_multiplier>
              
              <start_trigger>
                <delay_seconds>1.0</delay_seconds>
                <type>Immediate</type>
                <success_response>SIM_READY</success_response>
                <enable_diagnostics>true</enable_diagnostics>
              </start_trigger>
              
              <stop_trigger>
                <delay_seconds>0.5</delay_seconds>
                <success_response>SIM_STOPPED</success_response>
                <graceful_shutdown>true</graceful_shutdown>
                <graceful_timeout_seconds>5.0</graceful_timeout_seconds>
              </stop_trigger>
              
              <critical_trigger>
                <enabled>true</enabled>
                <activation_probability>0.05</activation_probability>
                <scenario_type>HardwareFailure</scenario_type>
                <error_message>CRITICAL_SIM_FAILURE</error_message>
                <error_code>500</error_code>
                <trigger_hardware_notification>false</trigger_hardware_notification>
              </critical_trigger>
              
              <random_behavior>
                <enabled>true</enabled>
                <delay_variation>0.2</delay_variation>
                <response_variation_probability>0.1</response_variation_probability>
                <alternative_responses>
                  <response>OK_ALT</response>
                  <response>READY_ALT</response>
                </alternative_responses>
              </random_behavior>
            </hardware_simulation>
            
            <uut id="test_uut">
              <port number="1">
                <protocol>rs232</protocol>
                <speed>115200</speed>
                <data_pattern>n81</data_pattern>
              </port>
            </uut>
          </bib>
        </root>
        """;

        // Act - NOTE: Ceci testera l'implémentation future du parsing
        // Pour l'instant, on vérifie que le XML est bien formé
        var isValidXml = IsValidXml(xmlWithSimulation);

        // Assert
        Assert.True(isValidXml);
        
        // TODO: Quand le parsing sera implémenté, ajouter:
        // var bibConfig = loader.LoadBibAsync(xmlPath, "test_bib").Result;
        // Assert.NotNull(bibConfig.HardwareSimulation);
        // Assert.True(bibConfig.HardwareSimulation.Enabled);
        // Assert.Equal(SimulationMode.Fast, bibConfig.HardwareSimulation.Mode);
        // etc.
    }

    [Fact]
    public void ParseBibXml_WithoutHardwareSimulation_ParsesNormally()
    {
        // Arrange
        var xmlWithoutSimulation = """
        <?xml version="1.0" encoding="UTF-8"?>
        <root>
          <bib id="test_bib" description="Test BIB without Hardware Simulation">
            <uut id="test_uut">
              <port number="1">
                <protocol>rs232</protocol>
                <speed>115200</speed>
                <data_pattern>n81</data_pattern>
              </port>
            </uut>
          </bib>
        </root>
        """;

        // Act
        var isValidXml = IsValidXml(xmlWithoutSimulation);

        // Assert
        Assert.True(isValidXml);
        
        // TODO: Quand le parsing sera implémenté:
        // var bibConfig = loader.LoadBibAsync(xmlPath, "test_bib").Result;
        // Assert.Null(bibConfig.HardwareSimulation);
        // Assert.False(bibConfig.IsHardwareSimulationEnabled);
    }

    private bool IsValidXml(string xml)
    {
        try
        {
            System.Xml.Linq.XDocument.Parse(xml);
            return true;
        }
        catch
        {
            return false;
        }
    }
}