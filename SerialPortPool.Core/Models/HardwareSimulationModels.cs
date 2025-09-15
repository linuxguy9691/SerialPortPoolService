// ===================================================================
// SPRINT 13 BOUCHÉE #1: Hardware Simulation Models
// File: SerialPortPool.Core/Models/HardwareSimulationModels.cs
// Purpose: XML-driven hardware simulation for demo without real hardware
// Philosophy: "Simple & Powerful" - Easy XML configuration, rich behavior
// ===================================================================

namespace SerialPortPool.Core.Models;

/// <summary>
/// SPRINT 13: Hardware simulation configuration for BIB demo mode
/// Enables comprehensive hardware simulation when real hardware is unavailable
/// XML-driven configuration with intelligent defaults
/// </summary>
public class HardwareSimulationConfig
{
    /// <summary>
    /// Enable/disable hardware simulation for this BIB
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// Simulation mode - affects timing and behavior patterns
    /// </summary>
    public SimulationMode Mode { get; set; } = SimulationMode.Realistic;

    /// <summary>
    /// Global simulation speed multiplier (1.0 = normal, 0.5 = half speed, 2.0 = double speed)
    /// </summary>
    public double SpeedMultiplier { get; set; } = 1.0;

    /// <summary>
    /// Start trigger configuration - when to begin hardware simulation
    /// </summary>
    public StartTriggerConfig StartTrigger { get; set; } = new();

    /// <summary>
    /// Stop trigger configuration - when to end hardware simulation  
    /// </summary>
    public StopTriggerConfig? StopTrigger { get; set; } = null;

    /// <summary>
    /// Critical trigger configuration - emergency scenarios
    /// </summary>
    public CriticalTriggerConfig CriticalTrigger { get; set; } = new();

    /// <summary>
    /// Random behavior configuration for realistic simulation
    /// </summary>
    public RandomBehaviorConfig RandomBehavior { get; set; } = new();

    /// <summary>
    /// Metadata for simulation context and debugging
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; } = new();

    /// <summary>
    /// When this simulation config was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    /// <summary>
    /// Calculate adjusted delay with speed multiplier
    /// </summary>
    public TimeSpan GetAdjustedDelay(TimeSpan originalDelay)
    {
        var adjustedMs = originalDelay.TotalMilliseconds / SpeedMultiplier;
        return TimeSpan.FromMilliseconds(Math.Max(100, adjustedMs)); // Minimum 100ms
    }

    /// <summary>
    /// Get simulation summary for logging and diagnostics
    /// </summary>
    public string GetSimulationSummary()
    {
        return $"Hardware Simulation: {Mode} mode, Speed: {SpeedMultiplier:F1}x, " +
               $"Start: {StartTrigger.DelaySeconds}s, Stop: {(StopTrigger?.DelaySeconds.ToString() ?? "Infinite")}s, " +
               $"Critical: {(CriticalTrigger.Enabled ? "Enabled" : "Disabled")}";
    }

    /// <summary>
    /// Validate simulation configuration
    /// </summary>
    public List<string> Validate()
    {
        var errors = new List<string>();

        if (SpeedMultiplier <= 0 || SpeedMultiplier > 10)
            errors.Add("Speed multiplier must be between 0.1 and 10.0");

        if (StartTrigger.DelaySeconds < 0)
            errors.Add("Start trigger delay cannot be negative");

        if (StopTrigger.DelaySeconds < 0)
            errors.Add("Stop trigger delay cannot be negative");

        return errors;
    }

    public override string ToString()
    {
        var status = Enabled ? "ENABLED" : "DISABLED";
        return $"HW Simulation: {status} ({Mode}, {SpeedMultiplier:F1}x)";
    }
}

/// <summary>
/// Start trigger configuration - defines when hardware simulation begins
/// </summary>
public class StartTriggerConfig
{
    /// <summary>
    /// Delay before simulation starts (seconds)
    /// </summary>
    public double DelaySeconds { get; set; } = 1.0;

    /// <summary>
    /// Type of start trigger
    /// </summary>
    public TriggerType Type { get; set; } = TriggerType.Immediate;

    /// <summary>
    /// Pattern to match for trigger activation (for pattern-based triggers)
    /// </summary>
    public string? TriggerPattern { get; set; }

    /// <summary>
    /// Success response when start trigger activates
    /// </summary>
    public string SuccessResponse { get; set; } = "SIMULATION_READY";

    /// <summary>
    /// Enable startup diagnostics logging
    /// </summary>
    public bool EnableDiagnostics { get; set; } = true;

    /// <summary>
    /// Custom metadata for start trigger
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();

    public override string ToString()
    {
        return $"Start Trigger: {Type}, Delay: {DelaySeconds}s, Response: '{SuccessResponse}'";
    }
}

/// <summary>
/// Stop trigger configuration - defines when hardware simulation ends
/// </summary>
public class StopTriggerConfig
{
    /// <summary>
    /// Delay before simulation stops (seconds)
    /// </summary>
    public double DelaySeconds { get; set; } = 0.5;

    /// <summary>
    /// Type of stop trigger
    /// </summary>
    public TriggerType Type { get; set; } = TriggerType.Immediate;

    /// <summary>
    /// Pattern to match for trigger activation
    /// </summary>
    public string? TriggerPattern { get; set; }

    /// <summary>
    /// Response when stop trigger activates
    /// </summary>
    public string SuccessResponse { get; set; } = "SIMULATION_STOPPED";

    /// <summary>
    /// Graceful shutdown - allow current operations to complete
    /// </summary>
    public bool GracefulShutdown { get; set; } = true;

    /// <summary>
    /// Maximum time to wait for graceful shutdown (seconds)
    /// </summary>
    public double GracefulTimeoutSeconds { get; set; } = 5.0;

    /// <summary>
    /// Custom metadata for stop trigger
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();

    public override string ToString()
    {
        var shutdown = GracefulShutdown ? "Graceful" : "Immediate";
        return $"Stop Trigger: {Type}, Delay: {DelaySeconds}s, Shutdown: {shutdown}";
    }
}

/// <summary>
/// Critical trigger configuration - emergency scenarios and failure simulation
/// </summary>
public class CriticalTriggerConfig
{
    /// <summary>
    /// Enable critical trigger simulation
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// Probability of critical trigger activation (0.0 to 1.0)
    /// </summary>
    public double ActivationProbability { get; set; } = 0.05; // 5% chance

    /// <summary>
    /// Delay before critical trigger activates (seconds)
    /// </summary>
    public double DelaySeconds { get; set; } = 0.0;

    /// <summary>
    /// Type of critical scenario to simulate
    /// </summary>
    public CriticalScenarioType ScenarioType { get; set; } = CriticalScenarioType.HardwareFailure;

    /// <summary>
    /// Critical error message to return
    /// </summary>
    public string ErrorMessage { get; set; } = "CRITICAL_HARDWARE_FAILURE";

    /// <summary>
    /// Error code for critical scenario
    /// </summary>
    public int ErrorCode { get; set; } = 500;

    /// <summary>
    /// Whether to trigger hardware notification (for real hardware integration)
    /// </summary>
    public bool TriggerHardwareNotification { get; set; } = false;

    /// <summary>
    /// Recovery possible after critical trigger
    /// </summary>
    public bool RecoveryPossible { get; set; } = false;

    /// <summary>
    /// Time required for recovery (seconds)
    /// </summary>
    public double RecoveryTimeSeconds { get; set; } = 10.0;

    /// <summary>
    /// Custom metadata for critical trigger
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// Check if critical trigger should activate (based on probability)
    /// </summary>
    public bool ShouldActivate()
    {
        if (!Enabled) return false;
        
        var random = new Random();
        return random.NextDouble() <= ActivationProbability;
    }

    public override string ToString()
    {
        var status = Enabled ? $"ENABLED ({ActivationProbability:P1})" : "DISABLED";
        return $"Critical Trigger: {status}, Scenario: {ScenarioType}";
    }
}

/// <summary>
/// Random behavior configuration for realistic hardware simulation
/// </summary>
public class RandomBehaviorConfig
{
    /// <summary>
    /// Enable random behavior simulation
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Random delay variation (0.0 to 1.0) - adds realism to timing
    /// </summary>
    public double DelayVariation { get; set; } = 0.2; // ±20% variation

    /// <summary>
    /// Minimum delay variation (milliseconds)
    /// </summary>
    public int MinDelayMs { get; set; } = 50;

    /// <summary>
    /// Maximum delay variation (milliseconds)
    /// </summary>
    public int MaxDelayMs { get; set; } = 500;

    /// <summary>
    /// Probability of response variation (0.0 to 1.0)
    /// </summary>
    public double ResponseVariationProbability { get; set; } = 0.1; // 10% chance

    /// <summary>
    /// List of alternative responses for variation
    /// </summary>
    public List<string> AlternativeResponses { get; set; } = new()
    {
        "OK_ALT", "READY_ALT", "SUCCESS_VAR"
    };

    /// <summary>
    /// Random seed for reproducible behavior (null = random seed)
    /// </summary>
    public int? RandomSeed { get; set; }

    /// <summary>
    /// Apply random delay variation to a base delay
    /// </summary>
    public TimeSpan ApplyRandomDelay(TimeSpan baseDelay)
    {
        if (!Enabled) return baseDelay;

        var random = RandomSeed.HasValue ? new Random(RandomSeed.Value) : new Random();
        var variation = (random.NextDouble() - 0.5) * 2 * DelayVariation; // -variation to +variation
        var variationMs = baseDelay.TotalMilliseconds * variation;
        
        // Clamp to min/max bounds
        variationMs = Math.Max(MinDelayMs, Math.Min(MaxDelayMs, Math.Abs(variationMs))) * Math.Sign(variationMs);
        
        var newDelayMs = baseDelay.TotalMilliseconds + variationMs;
        return TimeSpan.FromMilliseconds(Math.Max(10, newDelayMs)); // Minimum 10ms
    }

    /// <summary>
    /// Get random response variation
    /// </summary>
    public string? GetRandomResponse(string originalResponse)
    {
        if (!Enabled || !AlternativeResponses.Any()) return originalResponse;

        var random = RandomSeed.HasValue ? new Random(RandomSeed.Value) : new Random();
        
        if (random.NextDouble() <= ResponseVariationProbability)
        {
            var index = random.Next(AlternativeResponses.Count);
            return AlternativeResponses[index];
        }

        return originalResponse;
    }

    public override string ToString()
    {
        var status = Enabled ? $"ENABLED (±{DelayVariation:P1})" : "DISABLED";
        return $"Random Behavior: {status}";
    }
}

/// <summary>
/// Simulation modes affecting behavior patterns
/// </summary>
public enum SimulationMode
{
    /// <summary>
    /// Fast simulation for quick testing - minimal delays
    /// </summary>
    Fast = 0,

    /// <summary>
    /// Realistic simulation - mimics real hardware timing
    /// </summary>
    Realistic = 1,

    /// <summary>
    /// Slow simulation for debugging and analysis
    /// </summary>
    Debug = 2,

    /// <summary>
    /// Custom simulation - user-defined behavior
    /// </summary>
    Custom = 3
}

/// <summary>
/// Types of triggers for simulation events
/// </summary>
public enum TriggerType
{
    /// <summary>
    /// Trigger immediately when conditions are met
    /// </summary>
    Immediate = 0,

    /// <summary>
    /// Trigger after a specific delay
    /// </summary>
    Delayed = 1,

    /// <summary>
    /// Trigger when a pattern is matched
    /// </summary>
    Pattern = 2,

    /// <summary>
    /// Trigger on specific conditions
    /// </summary>
    Conditional = 3
}

/// <summary>
/// Types of critical scenarios for failure simulation
/// </summary>
public enum CriticalScenarioType
{
    /// <summary>
    /// Simulate hardware failure
    /// </summary>
    HardwareFailure = 0,

    /// <summary>
    /// Simulate communication timeout
    /// </summary>
    CommunicationTimeout = 1,

    /// <summary>
    /// Simulate power loss
    /// </summary>
    PowerLoss = 2,

    /// <summary>
    /// Simulate overheating condition
    /// </summary>
    Overheating = 3,

    /// <summary>
    /// Simulate safety violation
    /// </summary>
    SafetyViolation = 4,

    /// <summary>
    /// Custom critical scenario
    /// </summary>
    Custom = 99
}

/// <summary>
/// Extension methods for hardware simulation enums
/// </summary>
public static class HardwareSimulationExtensions
{
    /// <summary>
    /// Get default timing for simulation mode
    /// </summary>
    public static TimeSpan GetDefaultTiming(this SimulationMode mode)
    {
        return mode switch
        {
            SimulationMode.Fast => TimeSpan.FromMilliseconds(100),
            SimulationMode.Realistic => TimeSpan.FromMilliseconds(500),
            SimulationMode.Debug => TimeSpan.FromSeconds(2),
            SimulationMode.Custom => TimeSpan.FromSeconds(1),
            _ => TimeSpan.FromMilliseconds(500)
        };
    }

    /// <summary>
    /// Get description for simulation mode
    /// </summary>
    public static string GetDescription(this SimulationMode mode)
    {
        return mode switch
        {
            SimulationMode.Fast => "Fast simulation with minimal delays for quick testing",
            SimulationMode.Realistic => "Realistic simulation mimicking real hardware timing", 
            SimulationMode.Debug => "Slow simulation with extended delays for debugging",
            SimulationMode.Custom => "Custom simulation with user-defined behavior",
            _ => "Unknown simulation mode"
        };
    }

    /// <summary>
    /// Get icon for simulation mode
    /// </summary>
    public static string GetIcon(this SimulationMode mode)
    {
        return mode switch
        {
            SimulationMode.Fast => "⚡",
            SimulationMode.Realistic => "🎯",
            SimulationMode.Debug => "🐛",
            SimulationMode.Custom => "⚙️",
            _ => "❓"
        };
    }

    /// <summary>
    /// Get severity level for critical scenario
    /// </summary>
    public static int GetSeverityLevel(this CriticalScenarioType scenario)
    {
        return scenario switch
        {
            CriticalScenarioType.HardwareFailure => 5,
            CriticalScenarioType.SafetyViolation => 5,
            CriticalScenarioType.PowerLoss => 4,
            CriticalScenarioType.Overheating => 4,
            CriticalScenarioType.CommunicationTimeout => 3,
            CriticalScenarioType.Custom => 3,
            _ => 1
        };
    }

    /// <summary>
    /// Check if critical scenario requires immediate attention
    /// </summary>
    public static bool RequiresImmediateAttention(this CriticalScenarioType scenario)
    {
        return scenario.GetSeverityLevel() >= 4;
    }
}

/// <summary>
/// Helper methods for hardware simulation
/// </summary>
public static class HardwareSimulationHelper
{
    /// <summary>
    /// Create fast simulation configuration for demos
    /// </summary>
    public static HardwareSimulationConfig CreateFastDemo()
    {
        return new HardwareSimulationConfig
        {
            Enabled = true,
            Mode = SimulationMode.Fast,
            SpeedMultiplier = 2.0,
            StartTrigger = new StartTriggerConfig
            {
                DelaySeconds = 0.5,
                SuccessResponse = "DEMO_READY"
            },
            StopTrigger = new StopTriggerConfig
            {
                DelaySeconds = 0.2,
                SuccessResponse = "DEMO_COMPLETE"
            },
            CriticalTrigger = new CriticalTriggerConfig
            {
                Enabled = false // Disabled for smooth demos
            },
            RandomBehavior = new RandomBehaviorConfig
            {
                Enabled = false // Predictable for demos
            },
            Metadata = new Dictionary<string, string>
            {
                ["Purpose"] = "Fast demo simulation",
                ["Created"] = DateTime.Now.ToString("yyyy-MM-dd"),
                ["Type"] = "Demo"
            }
        };
    }

    /// <summary>
    /// Create realistic simulation configuration for testing
    /// </summary>
    public static HardwareSimulationConfig CreateRealisticTest()
    {
        return new HardwareSimulationConfig
        {
            Enabled = true,
            Mode = SimulationMode.Realistic,
            SpeedMultiplier = 1.0,
            StartTrigger = new StartTriggerConfig
            {
                DelaySeconds = 2.0,
                SuccessResponse = "HARDWARE_INITIALIZED"
            },
            StopTrigger = new StopTriggerConfig
            {
                DelaySeconds = 1.0,
                SuccessResponse = "HARDWARE_SHUTDOWN",
                GracefulShutdown = true
            },
            CriticalTrigger = new CriticalTriggerConfig
            {
                Enabled = true,
                ActivationProbability = 0.02, // 2% chance
                ScenarioType = CriticalScenarioType.CommunicationTimeout
            },
            RandomBehavior = new RandomBehaviorConfig
            {
                Enabled = true,
                DelayVariation = 0.15 // ±15% variation
            },
            Metadata = new Dictionary<string, string>
            {
                ["Purpose"] = "Realistic hardware simulation",
                ["Created"] = DateTime.Now.ToString("yyyy-MM-dd"),
                ["Type"] = "Test"
            }
        };
    }

    /// <summary>
    /// Validate hardware simulation configuration
    /// </summary>
    public static List<string> ValidateSimulationConfig(HardwareSimulationConfig config)
    {
        var errors = new List<string>();

        if (config == null)
        {
            errors.Add("Hardware simulation configuration cannot be null");
            return errors;
        }

        // Validate core configuration
        errors.AddRange(config.Validate());

        // Validate trigger configurations
        if (config.StartTrigger == null)
            errors.Add("Start trigger configuration is required");

        if (config.StopTrigger == null)
            errors.Add("Stop trigger configuration is required");

        if (config.CriticalTrigger == null)
            errors.Add("Critical trigger configuration is required");

        return errors;
    }
}