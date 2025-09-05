// ===================================================================
// SPRINT 13 UNIT TESTS: XmlDrivenHardwareSimulator Models & Services
// File: tests/SerialPortPool.Core.Tests/Models/XmlDrivenHardwareSimulatorTests.cs
// Purpose: Tests complets pour les modèles et services manquants
// ===================================================================

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SerialPortPool.Core.Models;
using SerialPortPool.Core.Services;
using Xunit;

namespace SerialPortPool.Core.Tests.Models;

/// <summary>
/// Unit tests pour les modèles et services XmlDrivenHardwareSimulator
/// Complète la couverture des tests Sprint 13
/// </summary>
public class XmlDrivenHardwareSimulatorTests : IDisposable
{
    private readonly XmlDrivenHardwareSimulator _simulator;
    private readonly ILogger<XmlDrivenHardwareSimulator> _logger;

    public XmlDrivenHardwareSimulatorTests()
    {
        _logger = NullLogger<XmlDrivenHardwareSimulator>.Instance;
        _simulator = new XmlDrivenHardwareSimulator(_logger);
    }

    #region SimulationSession Tests

    [Fact]
    public void SimulationSession_DefaultConstructor_SetsDefaults()
    {
        // Act
        var session = new SimulationSession();

        // Assert
        Assert.Equal(string.Empty, session.SessionId);
        Assert.Equal(string.Empty, session.BibId);
        Assert.Null(session.BibConfiguration);
        Assert.Null(session.HardwareConfig);
        Assert.Equal(SimulationPhase.Initializing, session.CurrentPhase);
        Assert.Equal(0, session.CommandsSimulated);
        Assert.Equal(0, session.SuccessfulCommands);
        Assert.Equal(0, session.FailedCommands);
        Assert.Equal(0, session.CriticalEvents);
        Assert.Equal(TimeSpan.Zero, session.TotalCommandTime);
        Assert.Equal(TimeSpan.Zero, session.AverageCommandDuration);
    }

    [Fact]
    public void SimulationSession_Duration_CalculatesCorrectly()
    {
        // Arrange
        var session = new SimulationSession
        {
            StartTime = DateTime.Now.AddMinutes(-5)
        };

        // Act
        var duration = session.Duration;

        // Assert
        Assert.True(duration.TotalMinutes >= 4.9 && duration.TotalMinutes <= 5.1);
    }

    [Fact]
    public void SimulationSession_Duration_WithEndTime_UsesEndTime()
    {
        // Arrange
        var startTime = DateTime.Now.AddMinutes(-10);
        var endTime = DateTime.Now.AddMinutes(-5);
        var session = new SimulationSession
        {
            StartTime = startTime,
            EndTime = endTime
        };

        // Act
        var duration = session.Duration;

        // Assert
        Assert.True(duration.TotalMinutes >= 4.9 && duration.TotalMinutes <= 5.1);
    }

    [Fact]
    public void SimulationSession_SuccessRate_CalculatesCorrectly()
    {
        // Arrange
        var session = new SimulationSession
        {
            CommandsSimulated = 10,
            SuccessfulCommands = 8,
            FailedCommands = 2
        };

        // Act
        var successRate = session.SuccessRate;

        // Assert
        Assert.Equal(80.0, successRate);
    }

    [Fact]
    public void SimulationSession_SuccessRate_WithZeroCommands_ReturnsZero()
    {
        // Arrange
        var session = new SimulationSession();

        // Act
        var successRate = session.SuccessRate;

        // Assert
        Assert.Equal(0.0, successRate);
    }

    #endregion

    #region SimulationResponse Tests

    [Fact]
    public void SimulationResponse_DefaultConstructor_SetsDefaults()
    {
        // Act
        var response = new SimulationResponse();

        // Assert
        Assert.Equal(string.Empty, response.SessionId);
        Assert.Equal(string.Empty, response.Command);
        Assert.Equal(string.Empty, response.Response);
        Assert.True(response.Success);
        Assert.Null(response.ErrorMessage);
        Assert.Null(response.ErrorCode);
        Assert.False(response.IsCritical);
        Assert.Equal(TimeSpan.Zero, response.Duration);
    }

    [Fact]
    public void SimulationResponse_ToString_FormatsCorrectly()
    {
        // Arrange
        var successResponse = new SimulationResponse
        {
            Command = "TEST_CMD",
            Response = "OK",
            Success = true,
            Duration = TimeSpan.FromMilliseconds(150)
        };

        var failureResponse = new SimulationResponse
        {
            Command = "FAIL_CMD",
            Response = "ERROR",
            Success = false,
            Duration = TimeSpan.FromMilliseconds(250)
        };

        // Act
        var successString = successResponse.ToString();
        var failureString = failureResponse.ToString();

        // Assert
        Assert.Contains("✅", successString);
        Assert.Contains("TEST_CMD", successString);
        Assert.Contains("OK", successString);
        Assert.Contains("150ms", successString);

        Assert.Contains("❌", failureString);
        Assert.Contains("FAIL_CMD", failureString);
        Assert.Contains("ERROR", failureString);
        Assert.Contains("250ms", failureString);
    }

    [Fact]
    public void SimulationResponse_WithCriticalError_SetsPropertiesCorrectly()
    {
        // Arrange & Act
        var response = new SimulationResponse
        {
            SessionId = "test-session",
            Command = "CRITICAL_TEST",
            Success = false,
            ErrorMessage = "Critical hardware failure",
            ErrorCode = 500,
            IsCritical = true
        };

        // Assert
        Assert.False(response.Success);
        Assert.True(response.IsCritical);
        Assert.Equal(500, response.ErrorCode);
        Assert.Equal("Critical hardware failure", response.ErrorMessage);
    }

    #endregion

    #region SimulationStatistics Tests

    [Fact]
    public void SimulationStatistics_DefaultConstructor_SetsDefaults()
    {
        // Act
        var stats = new SimulationStatistics();

        // Assert
        Assert.Equal(string.Empty, stats.SessionId);
        Assert.Equal(string.Empty, stats.BibId);
        Assert.False(stats.IsActive);
        Assert.Equal(SimulationPhase.Initializing, stats.CurrentPhase);
        Assert.Equal(TimeSpan.Zero, stats.Duration);
        Assert.Equal(0, stats.CommandsSimulated);
        Assert.Equal(0, stats.SuccessfulCommands);
        Assert.Equal(0, stats.FailedCommands);
        Assert.Equal(TimeSpan.Zero, stats.AverageCommandDuration);
    }

    [Fact]
    public void SimulationStatistics_SuccessRate_CalculatesCorrectly()
    {
        // Arrange
        var stats = new SimulationStatistics
        {
            CommandsSimulated = 20,
            SuccessfulCommands = 18,
            FailedCommands = 2
        };

        // Act
        var successRate = stats.SuccessRate;

        // Assert
        Assert.Equal(90.0, successRate);
    }

    [Fact]
    public void SimulationStatistics_ToString_FormatsCorrectly()
    {
        // Arrange
        var stats = new SimulationStatistics
        {
            BibId = "test_bib",
            CommandsSimulated = 15,
            SuccessfulCommands = 12,
            Duration = TimeSpan.FromSeconds(30.5)
        };

        // Act
        var result = stats.ToString();

        // Assert
        Assert.Contains("test_bib", result);
        Assert.Contains("15 commands", result);
        Assert.Contains("80.0% success", result);
        Assert.Contains("30.5s duration", result);
    }

    #endregion

    #region SimulationPhase Enum Tests

    [Theory]
    [InlineData(SimulationPhase.Initializing, "🔄")]
    [InlineData(SimulationPhase.Starting, "🚀")]
    [InlineData(SimulationPhase.Ready, "✅")]
    [InlineData(SimulationPhase.Executing, "⚙️")]
    [InlineData(SimulationPhase.Testing, "🧪")]
    [InlineData(SimulationPhase.Stopping, "🛑")]
    [InlineData(SimulationPhase.Stopped, "⏹️")]
    [InlineData(SimulationPhase.Critical, "🚨")]
    [InlineData(SimulationPhase.Error, "❌")]
    public void SimulationPhase_GetIcon_ReturnsCorrectIcon(SimulationPhase phase, string expectedIcon)
    {
        // Act
        var icon = phase.GetIcon();

        // Assert
        Assert.Equal(expectedIcon, icon);
    }

    [Theory]
    [InlineData(SimulationPhase.Ready, true)]
    [InlineData(SimulationPhase.Executing, true)]
    [InlineData(SimulationPhase.Testing, true)]
    [InlineData(SimulationPhase.Initializing, false)]
    [InlineData(SimulationPhase.Starting, false)]
    [InlineData(SimulationPhase.Stopping, false)]
    [InlineData(SimulationPhase.Stopped, false)]
    [InlineData(SimulationPhase.Critical, false)]
    [InlineData(SimulationPhase.Error, false)]
    public void SimulationPhase_AllowsCommandExecution_ReturnsCorrectValue(SimulationPhase phase, bool expected)
    {
        // Act
        var result = phase.AllowsCommandExecution();

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void SimulationPhase_GetDescription_ReturnsNonEmptyString()
    {
        // Arrange
        var allPhases = Enum.GetValues<SimulationPhase>();

        // Act & Assert
        foreach (var phase in allPhases)
        {
            var description = phase.GetDescription();
            Assert.False(string.IsNullOrEmpty(description));
            Assert.True(description.Length > 5); // Reasonable minimum length
        }
    }

    #endregion

    #region XmlDrivenHardwareSimulator Service Tests

    [Fact]
    public async Task StartSimulationAsync_WithDisabledSimulation_ThrowsException()
    {
        // Arrange
        var bibConfig = new BibConfiguration
        {
            BibId = "test_bib",
            HardwareSimulation = new HardwareSimulationConfig { Enabled = false }
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _simulator.StartSimulationAsync(bibConfig, "test-session"));
    }

    [Fact]
    public async Task StartSimulationAsync_WithEnabledSimulation_CreatesSession()
    {
        // Arrange
        var bibConfig = new BibConfiguration
        {
            BibId = "test_bib",
            HardwareSimulation = new HardwareSimulationConfig 
            { 
                Enabled = true,
                Mode = SimulationMode.Fast,
                StartTrigger = new StartTriggerConfig { DelaySeconds = 0.1 }
            }
        };

        // Act
        var session = await _simulator.StartSimulationAsync(bibConfig, "test-session");

        // Assert
        Assert.NotNull(session);
        Assert.Equal("test-session", session.SessionId);
        Assert.Equal("test_bib", session.BibId);
        Assert.Equal(bibConfig, session.BibConfiguration);
        Assert.True(session.StartTime <= DateTime.Now);
        Assert.Equal(SimulationPhase.Ready, session.CurrentPhase);
    }

    [Fact]
    public async Task SimulateCommandAsync_WithNonExistentSession_ThrowsException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _simulator.SimulateCommandAsync("non-existent", "TEST", "OK"));
    }

    [Fact]
    public async Task SimulateCommandAsync_WithValidSession_ReturnsResponse()
    {
        // Arrange
        var bibConfig = new BibConfiguration
        {
            BibId = "test_bib",
            HardwareSimulation = new HardwareSimulationConfig 
            { 
                Enabled = true,
                Mode = SimulationMode.Fast,
                StartTrigger = new StartTriggerConfig { DelaySeconds = 0.01 },
                CriticalTrigger = new CriticalTriggerConfig { Enabled = false }
            }
        };

        var session = await _simulator.StartSimulationAsync(bibConfig, "test-session");

        // Act
        var response = await _simulator.SimulateCommandAsync("test-session", "TEST_CMD", "EXPECTED_RESPONSE");

        // Assert
        Assert.NotNull(response);
        Assert.Equal("test-session", response.SessionId);
        Assert.Equal("TEST_CMD", response.Command);
        Assert.Equal("EXPECTED_RESPONSE", response.Response);
        Assert.True(response.Success);
        Assert.True(response.Duration > TimeSpan.Zero);
    }

    [Fact]
    public async Task StopSimulationAsync_WithValidSession_ReturnsTrue()
    {
        // Arrange
        var bibConfig = new BibConfiguration
        {
            BibId = "test_bib",
            HardwareSimulation = new HardwareSimulationConfig 
            { 
                Enabled = true,
                Mode = SimulationMode.Fast,
                StartTrigger = new StartTriggerConfig { DelaySeconds = 0.01 },
                StopTrigger = new StopTriggerConfig { DelaySeconds = 0.01 }
            }
        };

        var session = await _simulator.StartSimulationAsync(bibConfig, "test-session");

        // Act
        var result = await _simulator.StopSimulationAsync("test-session");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task StopSimulationAsync_WithNonExistentSession_ReturnsFalse()
    {
        // Act
        var result = await _simulator.StopSimulationAsync("non-existent");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task GetSessionStatistics_WithNonExistentSession_ReturnsNull()
    {
        // Act
        var stats = _simulator.GetSessionStatistics("non-existent");

        // Assert
        Assert.Null(stats);
    }

    [Fact]
    public async Task GetSessionStatistics_WithValidSession_ReturnsStatistics()
    {
        // Arrange
        var bibConfig = new BibConfiguration
        {
            BibId = "test_bib",
            HardwareSimulation = new HardwareSimulationConfig 
            { 
                Enabled = true,
                Mode = SimulationMode.Fast,
                StartTrigger = new StartTriggerConfig { DelaySeconds = 0.01 }
            }
        };

        var session = await _simulator.StartSimulationAsync(bibConfig, "test-session");

        // Act
        var stats = _simulator.GetSessionStatistics("test-session");

        // Assert
        Assert.NotNull(stats);
        Assert.Equal("test-session", stats!.SessionId);
        Assert.Equal("test_bib", stats.BibId);
        Assert.True(stats.IsActive);
        Assert.Equal(SimulationPhase.Ready, stats.CurrentPhase);
    }

    [Fact]
    public void GetAllActiveSessionStatistics_WithNoSessions_ReturnsEmptyList()
    {
        // Act
        var stats = _simulator.GetAllActiveSessionStatistics();

        // Assert
        Assert.NotNull(stats);
        Assert.Empty(stats);
    }

    [Fact]
    public async Task GetAllActiveSessionStatistics_WithMultipleSessions_ReturnsAllSessions()
    {
        // Arrange
        var bibConfig1 = new BibConfiguration
        {
            BibId = "test_bib_1",
            HardwareSimulation = new HardwareSimulationConfig 
            { 
                Enabled = true,
                Mode = SimulationMode.Fast,
                StartTrigger = new StartTriggerConfig { DelaySeconds = 0.01 }
            }
        };

        var bibConfig2 = new BibConfiguration
        {
            BibId = "test_bib_2",
            HardwareSimulation = new HardwareSimulationConfig 
            { 
                Enabled = true,
                Mode = SimulationMode.Fast,
                StartTrigger = new StartTriggerConfig { DelaySeconds = 0.01 }
            }
        };

        await _simulator.StartSimulationAsync(bibConfig1, "session-1");
        await _simulator.StartSimulationAsync(bibConfig2, "session-2");

        // Act
        var stats = _simulator.GetAllActiveSessionStatistics();

        // Assert
        Assert.Equal(2, stats.Count);
        Assert.Contains(stats, s => s.SessionId == "session-1");
        Assert.Contains(stats, s => s.SessionId == "session-2");
    }

    [Fact]
    public async Task SimulateCommandAsync_WithCriticalTrigger_ReturnsFailure()
    {
        // Arrange
        var bibConfig = new BibConfiguration
        {
            BibId = "test_bib",
            HardwareSimulation = new HardwareSimulationConfig 
            { 
                Enabled = true,
                Mode = SimulationMode.Fast,
                StartTrigger = new StartTriggerConfig { DelaySeconds = 0.01 },
                CriticalTrigger = new CriticalTriggerConfig 
                { 
                    Enabled = true,
                    ActivationProbability = 1.0, // 100% pour tester
                    ErrorMessage = "CRITICAL_TEST_ERROR",
                    ErrorCode = 999
                }
            }
        };

        var session = await _simulator.StartSimulationAsync(bibConfig, "test-session");

        // Act
        var response = await _simulator.SimulateCommandAsync("test-session", "TEST_CMD", "OK");

        // Assert
        Assert.False(response.Success);
        Assert.True(response.IsCritical);
        Assert.Equal("CRITICAL_TEST_ERROR", response.ErrorMessage);
        Assert.Equal(999, response.ErrorCode);
    }

    #endregion

    #region Cleanup

    public void Dispose()
    {
        _simulator?.Dispose();
    }

    #endregion
}