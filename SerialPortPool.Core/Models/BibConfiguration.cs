// ===================================================================
// SPRINT 13 BOUCHÉE #1: BibConfiguration.cs - Extended with Hardware Simulation
// File: SerialPortPool.Core/Models/BibConfiguration.cs
// Purpose: Add HardwareSimulation capability for demo without real hardware
// Philosophy: "Minimum Change" - ONLY add HardwareSimulation property
// ===================================================================

namespace SerialPortPool.Core.Models;

/// <summary>
/// Complete BIB (Board In Board) configuration supporting hierarchical structure
/// SPRINT 13 ENHANCED: Added hardware simulation capability for demo mode
/// Week 2: XML Configuration System with temporary BIB_ID mapping
/// </summary>
public class BibConfiguration
{
    /// <summary>
    /// Unique BIB identifier (e.g., "bib_001", "bib_002")
    /// </summary>
    public string BibId { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable BIB description
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// List of UUTs (Units Under Test) within this BIB
    /// </summary>
    public List<UutConfiguration> Uuts { get; set; } = new();

    /// <summary>
    /// BIB-level metadata and settings
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; } = new();

    /// <summary>
    /// 🆕 SPRINT 13: Hardware simulation configuration for demo mode
    /// When hardware is not available, enables simulated hardware responses
    /// </summary>
    public HardwareSimulationConfig? HardwareSimulation { get; set; }

    /// <summary>
    /// When this configuration was created/loaded
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    /// <summary>
    /// Get UUT by ID within this BIB
    /// </summary>
    /// <param name="uutId">UUT identifier</param>
    /// <returns>UUT configuration</returns>
    /// <exception cref="ArgumentException">UUT not found</exception>
    public UutConfiguration GetUut(string uutId)
    {
        return Uuts.FirstOrDefault(u => u.UutId.Equals(uutId, StringComparison.OrdinalIgnoreCase)) ??
               throw new ArgumentException($"UUT '{uutId}' not found in BIB '{BibId}'");
    }

    /// <summary>
    /// Get all port configurations across all UUTs in this BIB
    /// </summary>
    /// <returns>Flattened list of all ports</returns>
    public List<PortConfiguration> GetAllPorts()
    {
        return Uuts.SelectMany(uut => uut.Ports).ToList();
    }

    /// <summary>
    /// Get total number of ports in this BIB
    /// </summary>
    public int TotalPortCount => Uuts.Sum(uut => uut.Ports.Count);

    /// <summary>
    /// Get count of ports by protocol
    /// </summary>
    /// <param name="protocol">Protocol name (e.g., "rs232")</param>
    /// <returns>Number of ports using this protocol</returns>
    public int GetPortCountByProtocol(string protocol)
    {
        return GetAllPorts().Count(p => p.Protocol.Equals(protocol, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// 🆕 SPRINT 13: Check if hardware simulation is enabled
    /// </summary>
    public bool IsHardwareSimulationEnabled => HardwareSimulation?.Enabled == true;

    /// <summary>
    /// 🆕 SPRINT 13: Get simulation mode summary for logging
    /// </summary>
    public string GetSimulationSummary()
    {
        if (!IsHardwareSimulationEnabled)
            return "Real Hardware Mode";

        return $"Simulation Mode: Start={HardwareSimulation!.StartTrigger?.DelaySeconds.ToString() ?? "None"}s, " +
               $"Stop={(HardwareSimulation.StopTrigger != null ? $"{HardwareSimulation.StopTrigger.DelaySeconds}s" : "Infinite")}, " +
               $"Critical={HardwareSimulation.CriticalTrigger.Enabled}";
    }

    public override string ToString()
    {
        var mode = IsHardwareSimulationEnabled ? " (Simulation)" : " (Real HW)";
        return $"BIB {BibId}: {Uuts.Count} UUT(s), {TotalPortCount} port(s){mode}";
    }
}

/// <summary>
/// UUT (Unit Under Test) configuration within a BIB
/// </summary>
public class UutConfiguration
{
    /// <summary>
    /// Unique UUT identifier within the BIB (e.g., "uut_001", "uut_002")
    /// </summary>
    public string UutId { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable UUT description
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// List of ports within this UUT
    /// </summary>
    public List<PortConfiguration> Ports { get; set; } = new();

    /// <summary>
    /// UUT-level metadata and settings
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; } = new();

    /// <summary>
    /// Parent BIB reference (set during loading)
    /// </summary>
    public string ParentBibId { get; set; } = string.Empty;

    /// <summary>
    /// Get port by number within this UUT
    /// </summary>
    /// <param name="portNumber">Port number (e.g., 1, 2, 3)</param>
    /// <returns>Port configuration</returns>
    /// <exception cref="ArgumentException">Port not found</exception>
    public PortConfiguration GetPort(int portNumber)
    {
        return Ports.FirstOrDefault(p => p.PortNumber == portNumber) ??
               throw new ArgumentException($"Port {portNumber} not found in UUT '{UutId}'");
    }

    /// <summary>
    /// Get ports by protocol within this UUT
    /// </summary>
    /// <param name="protocol">Protocol name</param>
    /// <returns>Ports using the specified protocol</returns>
    public List<PortConfiguration> GetPortsByProtocol(string protocol)
    {
        return Ports.Where(p => p.Protocol.Equals(protocol, StringComparison.OrdinalIgnoreCase)).ToList();
    }

    public override string ToString()
    {
        return $"UUT {UutId}: {Ports.Count} port(s)";
    }
}

/// <summary>
/// Individual port configuration within a UUT
/// Week 2: Focus on RS232 protocol with temporary BIB mapping
/// </summary>
public class PortConfiguration
{
    /// <summary>
    /// Port number within the UUT (1, 2, 3, etc.)
    /// </summary>
    public int PortNumber { get; set; }

    /// <summary>
    /// Communication protocol (Week 2: "rs232" only)
    /// </summary>
    public string Protocol { get; set; } = string.Empty;

    /// <summary>
    /// Protocol speed (baud rate for serial protocols)
    /// </summary>
    public int Speed { get; set; } = 115200;

    /// <summary>
    /// Data pattern for serial protocols (e.g., "n81", "e71")
    /// </summary>
    public string DataPattern { get; set; } = "n81";

    /// <summary>
    /// Protocol-specific settings
    /// </summary>
    public Dictionary<string, object> Settings { get; set; } = new();

    /// <summary>
    /// 3-Phase workflow: Start commands
    /// </summary>
    public CommandSequence StartCommands { get; set; } = new();

    /// <summary>
    /// 3-Phase workflow: Test commands
    /// </summary>
    public CommandSequence TestCommands { get; set; } = new();

    /// <summary>
    /// 3-Phase workflow: Stop commands
    /// </summary>
    public CommandSequence StopCommands { get; set; } = new();

    /// <summary>
    /// Port-level metadata
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; } = new();

    /// <summary>
    /// Parent references (set during loading)
    /// </summary>
    public string ParentBibId { get; set; } = string.Empty;
    public string ParentUutId { get; set; } = string.Empty;

    /// <summary>
    /// Full port identifier: BIB.UUT.PORT
    /// </summary>
    public string FullPortId => $"{ParentBibId}.{ParentUutId}.{PortNumber}";

    #region RS232 Specific Methods (Week 2 Focus)

    /// <summary>
    /// Get baud rate for RS232 protocol
    /// </summary>
    public int GetBaudRate() => Speed;

    /// <summary>
    /// Parse data pattern and get parity setting
    /// </summary>
    public System.IO.Ports.Parity GetParity()
    {
        if (string.IsNullOrEmpty(DataPattern) || DataPattern.Length < 1) 
            return System.IO.Ports.Parity.None;

        return DataPattern[0] switch
        {
            'n' or 'N' => System.IO.Ports.Parity.None,
            'e' or 'E' => System.IO.Ports.Parity.Even,
            'o' or 'O' => System.IO.Ports.Parity.Odd,
            'm' or 'M' => System.IO.Ports.Parity.Mark,
            's' or 'S' => System.IO.Ports.Parity.Space,
            _ => System.IO.Ports.Parity.None
        };
    }

    /// <summary>
    /// Parse data pattern and get data bits count
    /// </summary>
    public int GetDataBits()
    {
        if (string.IsNullOrEmpty(DataPattern) || DataPattern.Length < 2)
            return 8;

        return DataPattern[1] switch
        {
            '5' => 5,
            '6' => 6,
            '7' => 7,
            '8' => 8,
            _ => 8
        };
    }

    /// <summary>
    /// Parse data pattern and get stop bits setting
    /// </summary>
    public System.IO.Ports.StopBits GetStopBits()
    {
        if (string.IsNullOrEmpty(DataPattern) || DataPattern.Length < 3)
            return System.IO.Ports.StopBits.One;

        return DataPattern[2] switch
        {
            '1' => System.IO.Ports.StopBits.One,
            '2' => System.IO.Ports.StopBits.Two,
            '5' => System.IO.Ports.StopBits.OnePointFive,
            _ => System.IO.Ports.StopBits.One
        };
    }

    /// <summary>
    /// Get read timeout for RS232 protocol
    /// </summary>
    public int GetReadTimeout()
    {
        return Settings.TryGetValue("read_timeout", out var timeout) && timeout is int value
            ? value
            : 2000;
    }

    /// <summary>
    /// Get write timeout for RS232 protocol
    /// </summary>
    public int GetWriteTimeout()
    {
        return Settings.TryGetValue("write_timeout", out var timeout) && timeout is int value
            ? value
            : 2000;
    }

    #endregion

    /// <summary>
    /// Validate this port configuration
    /// FIXED: Using BibValidationResult instead of ValidationResult
    /// </summary>
    public BibValidationResult Validate()
    {
        var result = new BibValidationResult();

        if (PortNumber <= 0)
            result.AddError("Port number must be positive");

        if (string.IsNullOrEmpty(Protocol))
            result.AddError("Protocol must be specified");

        if (Protocol.Equals("rs232", StringComparison.OrdinalIgnoreCase))
        {
            // RS232-specific validation
            var validBaudRates = new[] { 1200, 2400, 4800, 9600, 19200, 38400, 57600, 115200 };
            if (!validBaudRates.Contains(Speed))
                result.AddWarning($"Unusual baud rate: {Speed}");

            if (!IsValidDataPattern(DataPattern))
                result.AddError($"Invalid data pattern: {DataPattern}");
        }

        return result;
    }

    private static bool IsValidDataPattern(string pattern)
    {
        if (string.IsNullOrEmpty(pattern) || pattern.Length != 3)
            return false;

        var parity = char.ToLower(pattern[0]);
        var dataBits = pattern[1];
        var stopBits = pattern[2];

        return "neoms".Contains(parity) &&
               "5678".Contains(dataBits) &&
               "125".Contains(stopBits);
    }

    public override string ToString()
    {
        return $"Port {PortNumber}: {Protocol.ToUpper()} @ {Speed} ({DataPattern})";
    }
}

/// <summary>
/// Command sequence for 3-phase workflow (Start/Test/Stop)
/// </summary>
public class CommandSequence
{
    /// <summary>
    /// List of commands in this sequence
    /// </summary>
    public List<ProtocolCommand> Commands { get; set; } = new();

    /// <summary>
    /// Sequence-level timeout (overall timeout for all commands)
    /// </summary>
    public int SequenceTimeoutMs { get; set; } = 10000;

    /// <summary>
    /// Whether to continue sequence on command failure
    /// </summary>
    public bool ContinueOnFailure { get; set; } = false;

    /// <summary>
    /// Add a command to this sequence
    /// </summary>
    public void AddCommand(string command, string? expectedResponse = null, int timeoutMs = 2000)
    {
        Commands.Add(new ProtocolCommand
        {
            Command = command,
            ExpectedResponse = expectedResponse,
            TimeoutMs = timeoutMs
        });
    }

    /// <summary>
    /// Get total estimated duration for this sequence
    /// </summary>
    public TimeSpan EstimatedDuration => TimeSpan.FromMilliseconds(Commands.Sum(c => c.TimeoutMs));

    public override string ToString()
    {
        return $"Sequence: {Commands.Count} command(s), ~{EstimatedDuration.TotalSeconds:F1}s";
    }
}

/// <summary>
/// Validation result for configuration validation
/// FIXED: Using our custom BibValidationResult class
/// </summary>
public class BibValidationResult
{
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();

    public bool IsValid => !Errors.Any();
    public bool HasWarnings => Warnings.Any();

    public void AddError(string error) => Errors.Add(error);
    public void AddWarning(string warning) => Warnings.Add(warning);

    public string GetSummary()
    {
        if (IsValid && !HasWarnings)
            return "✅ Configuration valid";

        if (IsValid && HasWarnings)
            return $"⚠️ Configuration valid with {Warnings.Count} warning(s)";

        return $"❌ Configuration invalid: {Errors.Count} error(s)";
    }

    public override string ToString() => GetSummary();
}