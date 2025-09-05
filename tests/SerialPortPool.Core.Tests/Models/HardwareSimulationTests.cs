// ===================================================================
// SPRINT 13 UNIT TESTS: Hardware Simulation Models - COMPLETS
// File: tests/SerialPortPool.Core.Tests/Models/HardwareSimulationTests.cs
// Purpose: Tests complets pour tous les modèles de simulation Sprint 13
// ===================================================================

using SerialPortPool.Core.Models;
using Xunit;

namespace SerialPortPool.Core.Tests.Models;

/// <summary>
/// Unit tests complets pour les modèles de simulation hardware Sprint 13
/// </summary>
public class HardwareSimulationTests
{
    #region HardwareSimulationConfig Tests

    [Fact]
    public void HardwareSimulationConfig_DefaultConstructor_SetsDefaults()
    {
        // Act
        var config = new HardwareSimulationConfig();

        // Assert
        Assert.False(config.Enabled);
        Assert.Equal(SimulationMode.Realistic, config.Mode);
        Assert.Equal(1.0, config.SpeedMultiplier);
        Assert.NotNull(config.StartTrigger);
        Assert.NotNull(config.StopTrigger);
        Assert.NotNull(config.CriticalTrigger);
        Assert.NotNull(config.RandomBehavior);
        Assert.NotNull(config.Metadata);
        Assert.Empty(config.Metadata);
        Assert.True(config.CreatedAt <= DateTime.Now);
    }

    [Fact]
    public void HardwareSimulationConfig_Validate_WithValidConfig_ReturnsNoErrors()
    {
        // Arrange
        var config = new HardwareSimulationConfig
        {
            Enabled = true,
            Mode = SimulationMode.Fast,
            SpeedMultiplier = 2.0,
            StartTrigger = new StartTriggerConfig { DelaySeconds = 1.0 },
            StopTrigger = new StopTriggerConfig { DelaySeconds = 0.5 },
            CriticalTrigger = new CriticalTriggerConfig { Enabled = false }
        };

        // Act
        var errors = config.Validate();

        // Assert
        Assert.Empty(errors);
    }

    [Theory]
    [InlineData(-0.5)]  // Negative speed
    [InlineData(0)]     // Zero speed
    [InlineData(15.0)]  // Too fast
    public void HardwareSimulationConfig_Validate_WithInvalidSpeedMultiplier_ReturnsError(double speed)
    {
        // Arrange
        var config = new HardwareSimulationConfig
        {
            SpeedMultiplier = speed
        };

        // Act
        var errors = config.Validate();

        // Assert
        Assert.Contains(errors, e => e.Contains("Speed multiplier"));
    }

    [Theory]
    [InlineData(-1.0)]  // Negative delay
    [InlineData(-5.0)]  // More negative
    public void HardwareSimulationConfig_Validate_WithNegativeDelays_ReturnsErrors(double delay)
    {
        // Arrange
        var config = new HardwareSimulationConfig
        {
            StartTrigger = new StartTriggerConfig { DelaySeconds = delay },
            StopTrigger = new StopTriggerConfig { DelaySeconds = delay }
        };

        // Act
        var errors = config.Validate();

        // Assert
        Assert.True(errors.Count >= 2); // Both start and stop delays should error
        Assert.Contains(errors, e => e.Contains("Start trigger delay"));
        Assert.Contains(errors, e => e.Contains("Stop trigger delay"));
    }

    [Fact]
    public void HardwareSimulationConfig_GetAdjustedDelay_WithSpeedMultiplier_CalculatesCorrectly()
    {
        // Arrange
        var config = new HardwareSimulationConfig
        {
            SpeedMultiplier = 2.0 // 2x speed = half delay
        };
        var originalDelay = TimeSpan.FromSeconds(4);

        // Act
        var adjustedDelay = config.GetAdjustedDelay(originalDelay);

        // Assert
        Assert.Equal(2000, adjustedDelay.TotalMilliseconds); // 4000ms / 2.0 = 2000ms
    }

    [Fact]
    public void HardwareSimulationConfig_GetAdjustedDelay_WithMinimumDelay_EnforcesMinimum()
    {
        // Arrange
        var config = new HardwareSimulationConfig
        {
            SpeedMultiplier = 100.0 // Very fast = very small delay
        };
        var originalDelay = TimeSpan.FromMilliseconds(50);

        // Act
        var adjustedDelay = config.GetAdjustedDelay(originalDelay);

        // Assert
        Assert.True(adjustedDelay.TotalMilliseconds >= 100); // Minimum 100ms enforced
    }

    [Fact]
    public void HardwareSimulationConfig_GetSimulationSummary_FormatsCorrectly()
    {
        // Arrange
        var config = new HardwareSimulationConfig
        {
            Mode = SimulationMode.Fast,
            SpeedMultiplier = 2.5,
            StartTrigger = new StartTriggerConfig { DelaySeconds = 1.5 },
            StopTrigger = new StopTriggerConfig { DelaySeconds = 0.8 },
            CriticalTrigger = new CriticalTriggerConfig { Enabled = true }
        };

        // Act
        var summary = config.GetSimulationSummary();

        // Assert
        Assert.Contains("Fast mode", summary);
        Assert.Contains("2.5", summary);
        Assert.Contains("1.5s", summary);
        Assert.Contains("0.8s", summary);
        Assert.Contains("Enabled", summary);
    }

    [Fact]
    public void HardwareSimulationConfig_ToString_ReturnsStatusSummary()
    {
        // Arrange
        var enabledConfig = new HardwareSimulationConfig { Enabled = true, Mode = SimulationMode.Debug, SpeedMultiplier = 0.5 };
        var disabledConfig = new HardwareSimulationConfig { Enabled = false };

        // Act
        var enabledString = enabledConfig.ToString();
        var disabledString = disabledConfig.ToString();

        // Assert
        Assert.Contains("ENABLED", enabledString);
        Assert.Contains("Debug", enabledString);
        Assert.Contains("0.5", enabledString);
        Assert.Contains("DISABLED", disabledString);
    }

    #endregion

    #region StartTriggerConfig Tests

    [Fact]
    public void StartTriggerConfig_DefaultConstructor_SetsDefaults()
    {
        // Act
        var trigger = new StartTriggerConfig();

        // Assert
        Assert.Equal(1.0, trigger.DelaySeconds);
        Assert.Equal(TriggerType.Immediate, trigger.Type);
        Assert.Null(trigger.TriggerPattern);
        Assert.Equal("SIMULATION_READY", trigger.SuccessResponse);
        Assert.True(trigger.EnableDiagnostics);
        Assert.NotNull(trigger.Metadata);
        Assert.Empty(trigger.Metadata);
    }

    [Fact]
    public void StartTriggerConfig_ToString_FormatsCorrectly()
    {
        // Arrange
        var trigger = new StartTriggerConfig
        {
            Type = TriggerType.Pattern,
            DelaySeconds = 2.5,
            SuccessResponse = "CUSTOM_READY"
        };

        // Act
        var result = trigger.ToString();

        // Assert
        Assert.Contains("Pattern", result);
        Assert.Contains("2.5s", result);
        Assert.Contains("CUSTOM_READY", result);
    }

    #endregion

    #region StopTriggerConfig Tests

    [Fact]
    public void StopTriggerConfig_DefaultConstructor_SetsDefaults()
    {
        // Act
        var trigger = new StopTriggerConfig();

        // Assert
        Assert.Equal(0.5, trigger.DelaySeconds);
        Assert.Equal(TriggerType.Immediate, trigger.Type);
        Assert.Equal("SIMULATION_STOPPED", trigger.SuccessResponse);
        Assert.True(trigger.GracefulShutdown);
        Assert.Equal(5.0, trigger.GracefulTimeoutSeconds);
    }

    [Fact]
    public void StopTriggerConfig_ToString_FormatsCorrectly()
    {
        // Arrange
        var trigger = new StopTriggerConfig
        {
            Type = TriggerType.Delayed,
            DelaySeconds = 3.0,
            GracefulShutdown = false
        };

        // Act
        var result = trigger.ToString();

        // Assert
        Assert.Contains("Delayed", result);
        Assert.Contains("3", result);
        Assert.Contains("Immediate", result); // Graceful=false -> Immediate shutdown
    }

    #endregion

    #region CriticalTriggerConfig Tests

    [Fact]
    public void CriticalTriggerConfig_DefaultConstructor_SetsDefaults()
    {
        // Act
        var trigger = new CriticalTriggerConfig();

        // Assert
        Assert.False(trigger.Enabled);
        Assert.Equal(0.05, trigger.ActivationProbability); // 5%
        Assert.Equal(0.0, trigger.DelaySeconds);
        Assert.Equal(CriticalScenarioType.HardwareFailure, trigger.ScenarioType);
        Assert.Equal("CRITICAL_HARDWARE_FAILURE", trigger.ErrorMessage);
        Assert.Equal(500, trigger.ErrorCode);
        Assert.False(trigger.TriggerHardwareNotification);
        Assert.False(trigger.RecoveryPossible);
        Assert.Equal(10.0, trigger.RecoveryTimeSeconds);
    }

    [Theory]
    [InlineData(true, 0.5)]   // 50% chance - should activate sometimes
    [InlineData(true, 1.0)]   // 100% chance - should always activate
    [InlineData(false, 1.0)]  // Disabled - should never activate
    public void CriticalTriggerConfig_ShouldActivate_RespectsEnabledAndProbability(bool enabled, double probability)
    {
        // Arrange
        var trigger = new CriticalTriggerConfig
        {
            Enabled = enabled,
            ActivationProbability = probability
        };

        // Act - Test multiple times for probability-based logic
        var activationResults = new List<bool>();
        for (int i = 0; i < 100; i++)
        {
            activationResults.Add(trigger.ShouldActivate());
        }

        // Assert
        if (!enabled)
        {
            Assert.All(activationResults, result => Assert.False(result));
        }
        else if (probability >= 1.0)
        {
            Assert.All(activationResults, result => Assert.True(result));
        }
        else if (probability >= 0.5)
        {
            // With 50%+ probability, should activate at least some times
            Assert.True(activationResults.Count(r => r) > 0);
        }
    }

    [Fact]
    public void CriticalTriggerConfig_ToString_FormatsCorrectly()
    {
        // Arrange
        var enabledTrigger = new CriticalTriggerConfig
        {
            Enabled = true,
            ActivationProbability = 0.25,
            ScenarioType = CriticalScenarioType.PowerLoss
        };
        var disabledTrigger = new CriticalTriggerConfig { Enabled = false };

        // Act
        var enabledString = enabledTrigger.ToString();
        var disabledString = disabledTrigger.ToString();

        // Assert
        Assert.Contains("ENABLED", enabledString);
        Assert.Contains("25", enabledString); // 25% probability
        Assert.Contains("PowerLoss", enabledString);
        Assert.Contains("DISABLED", disabledString);
    }

    #endregion

    #region RandomBehaviorConfig Tests

    [Fact]
    public void RandomBehaviorConfig_DefaultConstructor_SetsDefaults()
    {
        // Act
        var behavior = new RandomBehaviorConfig();

        // Assert
        Assert.True(behavior.Enabled);
        Assert.Equal(0.2, behavior.DelayVariation); // ±20%
        Assert.Equal(50, behavior.MinDelayMs);
        Assert.Equal(500, behavior.MaxDelayMs);
        Assert.Equal(0.1, behavior.ResponseVariationProbability); // 10%
        Assert.NotNull(behavior.AlternativeResponses);
        Assert.Contains("OK_ALT", behavior.AlternativeResponses);
        Assert.Null(behavior.RandomSeed);
    }

    [Fact]
    public void RandomBehaviorConfig_ApplyRandomDelay_WithDisabled_ReturnsOriginal()
    {
        // Arrange
        var behavior = new RandomBehaviorConfig { Enabled = false };
        var originalDelay = TimeSpan.FromSeconds(2);

        // Act
        var result = behavior.ApplyRandomDelay(originalDelay);

        // Assert
        Assert.Equal(originalDelay, result);
    }

    [Fact]
    public void RandomBehaviorConfig_ApplyRandomDelay_WithEnabled_VariesDelay()
    {
        // Arrange
        var behavior = new RandomBehaviorConfig
        {
            Enabled = true,
            DelayVariation = 0.5, // ±50%
            RandomSeed = 12345    // Fixed seed for reproducible test
        };
        var originalDelay = TimeSpan.FromSeconds(1);

        // Act
        var result = behavior.ApplyRandomDelay(originalDelay);

        // Assert
        Assert.NotEqual(originalDelay, result);
        Assert.True(result.TotalMilliseconds >= 10); // Minimum 10ms enforced
        // Variation should be within reasonable bounds
        var variationRatio = Math.Abs((result.TotalMilliseconds - originalDelay.TotalMilliseconds) / originalDelay.TotalMilliseconds);
        Assert.True(variationRatio <= 0.6); // Should be within variation + some tolerance
    }

    [Theory]
    [InlineData("ORIGINAL", false, "ORIGINAL")]        // No variation
    [InlineData("ORIGINAL", true, null)]               // Variation possible, result varies
    public void RandomBehaviorConfig_GetRandomResponse_BehavesCorrectly(string original, bool enabled, string? expectedWhenNoVariation)
    {
        // Arrange
        var behavior = new RandomBehaviorConfig
        {
            Enabled = enabled,
            ResponseVariationProbability = enabled ? 0.5 : 0.0, // 50% if enabled
            AlternativeResponses = new List<string> { "ALT1", "ALT2" },
            RandomSeed = 54321 // Fixed seed
        };

        // Act
        var result = behavior.GetRandomResponse(original);

        // Assert
        if (!enabled)
        {
            Assert.Equal(original, result);
        }
        else
        {
            Assert.NotNull(result);
            // Result should be either original or one of the alternatives
            var validResponses = new List<string> { original };
            validResponses.AddRange(behavior.AlternativeResponses);
            Assert.Contains(result, validResponses);
        }
    }

    [Fact]
    public void RandomBehaviorConfig_ToString_FormatsCorrectly()
    {
        // Arrange
        var enabledBehavior = new RandomBehaviorConfig
        {
            Enabled = true,
            DelayVariation = 0.3
        };
        var disabledBehavior = new RandomBehaviorConfig { Enabled = false };

        // Act
        var enabledString = enabledBehavior.ToString();
        var disabledString = disabledBehavior.ToString();

        // Assert
        Assert.Contains("ENABLED", enabledString);
        Assert.Contains("30", enabledString); // 30% variation
        Assert.Contains("DISABLED", disabledString);
    }

    #endregion

    #region Enum Extension Tests

    [Theory]
    [InlineData(SimulationMode.Fast, 100)]
    [InlineData(SimulationMode.Realistic, 500)]
    [InlineData(SimulationMode.Debug, 2000)]
    [InlineData(SimulationMode.Custom, 1000)]
    public void SimulationMode_GetDefaultTiming_ReturnsCorrectValues(SimulationMode mode, int expectedMs)
    {
        // Act
        var timing = mode.GetDefaultTiming();

        // Assert
        Assert.Equal(expectedMs, timing.TotalMilliseconds);
    }

    [Theory]
    [InlineData(SimulationMode.Fast, "⚡")]
    [InlineData(SimulationMode.Realistic, "🎯")]
    [InlineData(SimulationMode.Debug, "🐛")]
    [InlineData(SimulationMode.Custom, "⚙️")]
    public void SimulationMode_GetIcon_ReturnsCorrectEmoji(SimulationMode mode, string expectedIcon)
    {
        // Act
        var icon = mode.GetIcon();

        // Assert
        Assert.Equal(expectedIcon, icon);
    }

    [Theory]
    [InlineData(CriticalScenarioType.HardwareFailure, 5)]
    [InlineData(CriticalScenarioType.SafetyViolation, 5)]
    [InlineData(CriticalScenarioType.PowerLoss, 4)]
    [InlineData(CriticalScenarioType.CommunicationTimeout, 3)]
    public void CriticalScenarioType_GetSeverityLevel_ReturnsCorrectSeverity(CriticalScenarioType scenario, int expectedSeverity)
    {
        // Act
        var severity = scenario.GetSeverityLevel();

        // Assert
        Assert.Equal(expectedSeverity, severity);
    }

    [Theory]
    [InlineData(CriticalScenarioType.HardwareFailure, true)]
    [InlineData(CriticalScenarioType.SafetyViolation, true)]
    [InlineData(CriticalScenarioType.PowerLoss, true)]
    [InlineData(CriticalScenarioType.CommunicationTimeout, false)]
    public void CriticalScenarioType_RequiresImmediateAttention_ReturnsCorrectValue(CriticalScenarioType scenario, bool expected)
    {
        // Act
        var requiresAttention = scenario.RequiresImmediateAttention();

        // Assert
        Assert.Equal(expected, requiresAttention);
    }

    #endregion

    #region Helper Factory Tests

    [Fact]
    public void HardwareSimulationHelper_CreateFastDemo_CreatesCorrectConfig()
    {
        // Act
        var config = HardwareSimulationHelper.CreateFastDemo();

        // Assert
        Assert.True(config.Enabled);
        Assert.Equal(SimulationMode.Fast, config.Mode);
        Assert.Equal(2.0, config.SpeedMultiplier);
        Assert.Equal(0.5, config.StartTrigger.DelaySeconds);
        Assert.Equal("DEMO_READY", config.StartTrigger.SuccessResponse);
        Assert.Equal(0.2, config.StopTrigger.DelaySeconds);
        Assert.False(config.CriticalTrigger.Enabled);
        Assert.False(config.RandomBehavior.Enabled);
        Assert.Contains("Demo", config.Metadata["Purpose"]);
    }

    [Fact]
    public void HardwareSimulationHelper_CreateRealisticTest_CreatesCorrectConfig()
    {
        // Act
        var config = HardwareSimulationHelper.CreateRealisticTest();

        // Assert
        Assert.True(config.Enabled);
        Assert.Equal(SimulationMode.Realistic, config.Mode);
        Assert.Equal(1.0, config.SpeedMultiplier);
        Assert.Equal(2.0, config.StartTrigger.DelaySeconds);
        Assert.Equal("HARDWARE_INITIALIZED", config.StartTrigger.SuccessResponse);
        Assert.True(config.StopTrigger.GracefulShutdown);
        Assert.True(config.CriticalTrigger.Enabled);
        Assert.Equal(0.02, config.CriticalTrigger.ActivationProbability); // 2%
        Assert.True(config.RandomBehavior.Enabled);
        Assert.Equal(0.15, config.RandomBehavior.DelayVariation);
        Assert.Contains("Realistic", config.Metadata["Purpose"]);
    }

    [Fact]
    public void HardwareSimulationHelper_ValidateSimulationConfig_WithValidConfig_ReturnsNoErrors()
    {
        // Arrange
        var validConfig = HardwareSimulationHelper.CreateFastDemo();

        // Act
        var errors = HardwareSimulationHelper.ValidateSimulationConfig(validConfig);

        // Assert
        Assert.Empty(errors);
    }

    [Fact]
    public void HardwareSimulationHelper_ValidateSimulationConfig_WithNullConfig_ReturnsError()
    {
        // Act
        var errors = HardwareSimulationHelper.ValidateSimulationConfig(null!);

        // Assert
        Assert.Single(errors);
        Assert.Contains("cannot be null", errors[0]);
    }

    [Fact]
    public void HardwareSimulationHelper_ValidateSimulationConfig_WithInvalidConfig_ReturnsErrors()
    {
        // Arrange
        var invalidConfig = new HardwareSimulationConfig
        {
            SpeedMultiplier = -1.0, // Invalid
            StartTrigger = null!,   // Invalid
            StopTrigger = null!,    // Invalid
            CriticalTrigger = null! // Invalid
        };

        // Act
        var errors = HardwareSimulationHelper.ValidateSimulationConfig(invalidConfig);

        // Assert
        Assert.True(errors.Count >= 4); // Speed + 3 null triggers
        Assert.Contains(errors, e => e.Contains("Speed multiplier"));
        Assert.Contains(errors, e => e.Contains("Start trigger"));
        Assert.Contains(errors, e => e.Contains("Stop trigger"));
        Assert.Contains(errors, e => e.Contains("Critical trigger"));
    }

    #endregion

    #region BibConfiguration Integration Tests

    [Fact]
    public void BibConfiguration_WithHardwareSimulation_IsHardwareSimulationEnabled_ReturnsCorrectValue()
    {
        // Arrange
        var bibWithSimulation = new BibConfiguration
        {
            BibId = "test_bib",
            HardwareSimulation = new HardwareSimulationConfig { Enabled = true }
        };
        var bibWithoutSimulation = new BibConfiguration { BibId = "test_bib" };
        var bibWithDisabledSimulation = new BibConfiguration
        {
            BibId = "test_bib",
            HardwareSimulation = new HardwareSimulationConfig { Enabled = false }
        };

        // Act & Assert
        Assert.True(bibWithSimulation.IsHardwareSimulationEnabled);
        Assert.False(bibWithoutSimulation.IsHardwareSimulationEnabled);
        Assert.False(bibWithDisabledSimulation.IsHardwareSimulationEnabled);
    }

    [Fact]
    public void BibConfiguration_GetSimulationSummary_ReturnsCorrectSummary()
    {
        // Arrange
        var bibWithSimulation = new BibConfiguration
        {
            BibId = "test_bib",
            HardwareSimulation = HardwareSimulationHelper.CreateFastDemo()
        };
        var bibWithoutSimulation = new BibConfiguration { BibId = "test_bib" };

        // Act
        var summaryWithSimulation = bibWithSimulation.GetSimulationSummary();
        var summaryWithoutSimulation = bibWithoutSimulation.GetSimulationSummary();

        // Assert
        Assert.Contains("Simulation Mode", summaryWithSimulation);
        Assert.Contains("Start=", summaryWithSimulation);
        Assert.Contains("Stop=", summaryWithSimulation);
        Assert.Equal("Real Hardware Mode", summaryWithoutSimulation);
    }

    [Fact]
    public void BibConfiguration_ToString_IncludesSimulationMode()
    {
        // Arrange
        var bibWithSimulation = new BibConfiguration
        {
            BibId = "test_bib",
            Description = "Test BIB",
            Uuts = new List<UutConfiguration>(),
            HardwareSimulation = new HardwareSimulationConfig { Enabled = true }
        };
        var bibWithoutSimulation = new BibConfiguration
        {
            BibId = "test_bib",
            Description = "Test BIB",
            Uuts = new List<UutConfiguration>()
        };

        // Act
        var stringWithSimulation = bibWithSimulation.ToString();
        var stringWithoutSimulation = bibWithoutSimulation.ToString();

        // Assert
        Assert.Contains("(Simulation)", stringWithSimulation);
        Assert.Contains("(Real HW)", stringWithoutSimulation);
    }

    #endregion
}

// ===================================================================
// SPRINT 13 UNIT TESTS: XML Parsing Tests
// File: tests/SerialPortPool.Core.Tests/Services/XmlBibConfigurationLoader_SimulationTests.cs
// ===================================================================

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