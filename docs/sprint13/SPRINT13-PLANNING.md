# 🚀 SPRINT 13 - Real Hardware BitBang GPIO Integration

**Sprint Period:** September 8-22, 2025  
**Phase:** Hardware-Triggered Workflow Control + GPIO Integration  
**Status:** CLIENT PRIORITY - REAL HARDWARE CONTROL  

---

## 📋 Sprint 13 Overview - HARDWARE-FIRST APPROACH

**Mission:** Real FT4232HA BitBang GPIO Integration with Hardware-Triggered Start/Stop Control

**CLIENT NEW PRIORITY:** ✅ **HARDWARE CONTROL FOUNDATION**  
- Real GPIO monitoring for workflow start/stop triggers
- Command line simulation for testing without hardware  
- XML-based critical condition configuration
- Critical validation → Hardware GPIO signal output
- Return to original service behavior patterns

**SPRINT 13 FOCUS:**
- 🔌 **Real GPIO Input Monitoring** - Start/Stop triggered by hardware signals
- 💻 **Command Line Simulation** - `-simStart`, `-simStop` for testing
- 🚨 **Critical Validation → GPIO Output** - XML config + DD2 hardware signal
- 📡 **FT4232HA Integration** - Real FTD2XX_NET implementation
- 🔄 **Original Service Behavior** - Restore first commit patterns

**CORE PHILOSOPHY:** 
- Hardware foundation first - establish real GPIO control before advanced features
- Simulation capability for development and testing without physical hardware
- Critical conditions must trigger actual hardware responses
- Return to original service architecture and behavior

---

## 🎯 Sprint 13 Core Objectives - HARDWARE INTEGRATION

### **🔌 OBJECTIVE 1: Real GPIO Input Monitoring (Priority 1)**
**Priority:** ⭐ **HIGHEST** | **Effort:** 4-5 hours | **Status:** HARDWARE-TRIGGERED WORKFLOWS

**FT4232HA Port D GPIO Implementation:**
```csharp
// ✅ Real FTD2XX_NET GPIO monitoring implementation
public class RealFtdiBitBangProvider : IBitBangProtocolProvider
{
    private FTDI _ftdiDevice = new FTDI();
    private const string FT4232H_SERIAL = "FT9A9OFO"; // Client hardware
    private CancellationTokenSource _monitoringCancellation;
    private Task _monitoringTask;
    
    // ✅ Port D Pin Mapping (from hardware spec)
    private const int POWER_ON_READY_BIT = 0;    // DD0 (CN3-17) - START trigger
    private const int POWER_DOWN_HEADS_UP_BIT = 1; // DD1 (CN3-16) - STOP trigger  
    private const int CRITICAL_FAIL_BIT = 2;     // DD2 (CN3-15) - Critical output
    private const int WORKFLOW_ACTIVE_BIT = 3;   // DD3 (CN3-14) - Status output
    
    public async Task InitializeAsync(BitBangConfiguration config)
    {
        _logger.LogInformation("🔌 Initializing FT4232HA Port D GPIO...");
        
        // ✅ Open Port D (interface index 3 for FT4232H)
        var status = _ftdiDevice.OpenByIndex(3);
        if (status != FTDI.FT_STATUS.FT_OK)
            throw new FtdiException($"Failed to open Port D: {status}", config.SerialNumber);
        
        // ✅ Configure for async bit-bang mode
        status = _ftdiDevice.SetBaudRate(62500); // 1MHz actual rate (62500 × 16)
        status |= _ftdiDevice.SetBitMode(0x00, FTDI.FT_BIT_MODES.FT_BIT_MODE_RESET);
        await Task.Delay(50); // Critical delay for mode reset
        
        // ✅ Direction mask: DD0,DD1 = inputs, DD2,DD3 = outputs
        status |= _ftdiDevice.SetBitMode(0x0C, FTDI.FT_BIT_MODES.FT_BIT_MODE_ASYNC_BITBANG);
        
        if (status != FTDI.FT_STATUS.FT_OK)
            throw new FtdiException($"Failed to configure bit-bang mode: {status}");
        
        _logger.LogInformation("✅ FT4232HA Port D GPIO initialized successfully");
        
        // ✅ Start continuous input monitoring
        StartInputMonitoring();
    }
    
    private void StartInputMonitoring()
    {
        _monitoringCancellation = new CancellationTokenSource();
        _monitoringTask = Task.Run(async () =>
        {
            _logger.LogInformation("🔍 Starting GPIO input monitoring...");
            
            bool lastPowerOnReady = false;
            bool lastPowerDownHeadsUp = false;
            
            while (!_monitoringCancellation.Token.IsCancellationRequested)
            {
                try
                {
                    var currentState = await ReadGpioInputsAsync();
                    
                    // ✅ Detect POWER ON READY rising edge (START trigger)
                    if (currentState.PowerOnReady && !lastPowerOnReady)
                    {
                        _logger.LogInformation("🚀 GPIO TRIGGER: Power On Ready detected - triggering workflow START");
                        await TriggerWorkflowStartAsync();
                    }
                    
                    // ✅ Detect POWER DOWN HEADS-UP rising edge (STOP trigger)
                    if (currentState.PowerDownHeadsUp && !lastPowerDownHeadsUp)
                    {
                        _logger.LogWarning("⚠️ GPIO TRIGGER: Power Down Heads-Up detected - triggering workflow STOP");
                        await TriggerWorkflowStopAsync();
                    }
                    
                    lastPowerOnReady = currentState.PowerOnReady;
                    lastPowerDownHeadsUp = currentState.PowerDownHeadsUp;
                    
                    // ✅ Configurable polling interval (default 100ms from spec)
                    await Task.Delay(100, _monitoringCancellation.Token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ GPIO monitoring error - continuing...");
                    await Task.Delay(1000); // Error recovery delay
                }
            }
            
            _logger.LogInformation("🔍 GPIO input monitoring stopped");
        });
    }
    
    private async Task<BitBangInputState> ReadGpioInputsAsync()
    {
        uint bytesRead = 0;
        byte[] buffer = new byte[1];
        
        var status = _ftdiDevice.Read(buffer, 1, ref bytesRead);
        if (status != FTDI.FT_STATUS.FT_OK || bytesRead == 0)
            throw new FtdiException($"GPIO read failed: {status}");
        
        byte rawValue = buffer[0];
        
        return new BitBangInputState
        {
            PowerOnReady = (rawValue & (1 << POWER_ON_READY_BIT)) != 0,
            PowerDownHeadsUp = (rawValue & (1 << POWER_DOWN_HEADS_UP_BIT)) != 0,
            RawInputValue = rawValue,
            Timestamp = DateTime.Now
        };
    }
    
    // ✅ Critical fail signal output (CLIENT REQUIREMENT)
    public async Task SetCriticalFailSignalAsync(bool state)
    {
        var currentOutput = await ReadCurrentOutputState();
        byte newOutput = state 
            ? (byte)(currentOutput | (1 << CRITICAL_FAIL_BIT))
            : (byte)(currentOutput & ~(1 << CRITICAL_FAIL_BIT));
        
        uint bytesWritten = 0;
        var status = _ftdiDevice.Write(new byte[] { newOutput }, 1, ref bytesWritten);
        
        if (status != FTDI.FT_STATUS.FT_OK)
            throw new FtdiException($"Failed to set critical fail signal: {status}");
        
        _logger.LogCritical("🚨 CRITICAL FAIL SIGNAL: {State} via GPIO DD2", state ? "ACTIVE" : "CLEARED");
        
        // ✅ Auto-clear after configured time (default 2s from spec)
        if (state)
        {
            _ = Task.Run(async () =>
            {
                await Task.Delay(TimeSpan.FromSeconds(2));
                await SetCriticalFailSignalAsync(false);
            });
        }
    }
    
    // ✅ Workflow active signal for status indication
    public async Task SetWorkflowActiveSignalAsync(bool state)
    {
        var currentOutput = await ReadCurrentOutputState();
        byte newOutput = state 
            ? (byte)(currentOutput | (1 << WORKFLOW_ACTIVE_BIT))
            : (byte)(currentOutput & ~(1 << WORKFLOW_ACTIVE_BIT));
        
        uint bytesWritten = 0;
        var status = _ftdiDevice.Write(new byte[] { newOutput }, 1, ref bytesWritten);
        
        if (status != FTDI.FT_STATUS.FT_OK)
            throw new FtdiException($"Failed to set workflow active signal: {status}");
        
        _logger.LogInformation("📡 WORKFLOW ACTIVE SIGNAL: {State} via GPIO DD3", state ? "ACTIVE" : "CLEARED");
    }
}

// ✅ Service integration with hardware triggers
public class HardwareTriggeredWorkflowService
{
    private readonly IBitBangProtocolProvider _bitBangProvider;
    private readonly IMultiBibWorkflowService _workflowService;
    
    private async Task TriggerWorkflowStartAsync()
    {
        try
        {
            _logger.LogInformation("🚀 HARDWARE TRIGGER: Starting workflow execution...");
            
            // ✅ Set workflow active signal
            await _bitBangProvider.SetWorkflowActiveSignalAsync(true);
            
            // ✅ Execute configured BIB workflow
            var result = await _workflowService.ExecuteConfiguredBibAsync();
            
            _logger.LogInformation("✅ HARDWARE TRIGGERED WORKFLOW: {Result}", result.Success ? "SUCCESS" : "FAILED");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Hardware triggered workflow failed");
        }
        finally
        {
            // ✅ Clear workflow active signal
            await _bitBangProvider.SetWorkflowActiveSignalAsync(false);
        }
    }
    
    private async Task TriggerWorkflowStopAsync()
    {
        _logger.LogWarning("⚠️ HARDWARE TRIGGER: Stopping workflow execution...");
        
        // ✅ Request graceful stop
        await _workflowService.RequestStopAsync();
        
        // ✅ Clear workflow active signal
        await _bitBangProvider.SetWorkflowActiveSignalAsync(false);
    }
}
```

### **💻 OBJECTIVE 2: Command Line Simulation (Priority 1)**
**Priority:** ⭐ **HIGHEST** | **Effort:** 2-3 hours | **Status:** TESTING WITHOUT HARDWARE

**Simulation Commands for Development:**
```csharp
// ✅ Extended command line options for Sprint 13
public class Sprint13CommandLineOptions
{
    // ✅ Existing options preserved
    [Option("mode", Required = false, HelpText = "Execution mode")]
    public string Mode { get; set; } = "single";
    
    [Option("bib-ids", Required = false, HelpText = "BIB identifiers")]  
    public string BibIds { get; set; } = "client_demo";
    
    // ✅ NEW Sprint 13: GPIO simulation options
    [Option("sim-start", Required = false, HelpText = "Simulate GPIO start trigger")]
    public bool SimulateStart { get; set; } = false;
    
    [Option("sim-stop", Required = false, HelpText = "Simulate GPIO stop trigger")]
    public bool SimulateStop { get; set; } = false;
    
    [Option("sim-critical", Required = false, HelpText = "Simulate critical condition")]
    public bool SimulateCritical { get; set; } = false;
    
    [Option("gpio-mode", Required = false, HelpText = "GPIO mode: real, simulation, disabled")]
    public string GpioMode { get; set; } = "real";
    
    [Option("gpio-polling", Required = false, HelpText = "GPIO polling interval in ms")]
    public int GpioPollingMs { get; set; } = 100;
}

// ✅ Simulation provider for testing without hardware
public class SimulatedBitBangProvider : IBitBangProtocolProvider
{
    private bool _simulatedPowerReady = false;
    private bool _simulatedPowerDown = false;
    private bool _simulatedCriticalSignal = false;
    private bool _simulatedWorkflowActive = false;
    
    public async Task SimulateStartTriggerAsync()
    {
        _logger.LogInformation("🎭 SIMULATION: Triggering start signal...");
        _simulatedPowerReady = true;
        
        // ✅ Trigger same events as real hardware
        await TriggerWorkflowStartAsync();
        
        await Task.Delay(100);
        _simulatedPowerReady = false;
    }
    
    public async Task SimulateStopTriggerAsync()
    {
        _logger.LogWarning("🎭 SIMULATION: Triggering stop signal...");
        _simulatedPowerDown = true;
        
        // ✅ Trigger same events as real hardware
        await TriggerWorkflowStopAsync();
        
        await Task.Delay(100);
        _simulatedPowerDown = false;
    }
    
    public async Task SimulateCriticalConditionAsync()
    {
        _logger.LogCritical("🎭 SIMULATION: Triggering critical condition...");
        
        // ✅ Same behavior as real critical validation
        await SetCriticalFailSignalAsync(true);
    }
    
    public async Task SetCriticalFailSignalAsync(bool state)
    {
        _simulatedCriticalSignal = state;
        _logger.LogCritical("🎭 SIMULATED CRITICAL SIGNAL: {State}", state ? "ACTIVE" : "CLEARED");
        
        // ✅ Auto-clear simulation after 2 seconds
        if (state)
        {
            _ = Task.Run(async () =>
            {
                await Task.Delay(2000);
                _simulatedCriticalSignal = false;
                _logger.LogInformation("🎭 SIMULATED CRITICAL SIGNAL: Auto-cleared");
            });
        }
    }
}

// ✅ Program.cs integration with simulation
public static async Task Main(string[] args)
{
    var options = Parser.Default.ParseArguments<Sprint13CommandLineOptions>(args);
    
    await options.WithParsedAsync(async opts =>
    {
        // ✅ Configure GPIO provider based on mode
        IBitBangProtocolProvider bitBangProvider = opts.GpioMode.ToLower() switch
        {
            "real" => new RealFtdiBitBangProvider(),
            "simulation" => new SimulatedBitBangProvider(), 
            "disabled" => new DisabledBitBangProvider(),
            _ => new RealFtdiBitBangProvider()
        };
        
        var workflowService = ConfigureWorkflowService(bitBangProvider);
        
        // ✅ Handle simulation commands
        if (opts.SimulateStart && bitBangProvider is SimulatedBitBangProvider sim)
        {
            Console.WriteLine("🎭 Simulating GPIO START trigger...");
            await sim.SimulateStartTriggerAsync();
        }
        
        if (opts.SimulateStop && bitBangProvider is SimulatedBitBangProvider sim2)
        {
            Console.WriteLine("🎭 Simulating GPIO STOP trigger...");
            await sim2.SimulateStopTriggerAsync();
        }
        
        if (opts.SimulateCritical && bitBangProvider is SimulatedBitBangProvider sim3)
        {
            Console.WriteLine("🎭 Simulating CRITICAL condition...");
            await sim3.SimulateCriticalConditionAsync();
        }
        
        // ✅ Start hardware monitoring or simulation
        await workflowService.StartAsync();
        
        Console.WriteLine("Press any key to stop...");
        Console.ReadKey();
        
        await workflowService.StopAsync();
    });
}
```

**Example Command Line Usage:**
```bash
# ✅ Real hardware monitoring
.\SerialPortPoolService.exe --gpio-mode real --bib-ids client_demo

# ✅ Simulation mode for testing
.\SerialPortPoolService.exe --gpio-mode simulation --sim-start --bib-ids production_test

# ✅ Test critical condition
.\SerialPortPoolService.exe --gpio-mode simulation --sim-critical

# ✅ Faster GPIO polling for testing
.\SerialPortPoolService.exe --gpio-mode real --gpio-polling 50
```

### **🚨 OBJECTIVE 3: XML-Based Critical Configuration (Priority 1)**
**Priority:** ⭐ **HIGHEST** | **Effort:** 2-3 hours | **Status:** CRITICAL → GPIO INTEGRATION

**Enhanced XML Configuration with Critical Triggers:**
```xml
<!-- ✅ Enhanced BIB configuration with critical conditions -->
<BibConfiguration>
    <BibId>client_demo</BibId>
    <Description>Client Demo with Critical Conditions</Description>
    
    <!-- ✅ NEW: Critical condition configuration -->
    <CriticalConditions>
        <Condition>
            <Name>HARDWARE_FAULT</Name>
            <Pattern>FAULT|ERROR|FAIL</Pattern>
            <IsRegex>true</IsRegex>
            <TriggerHardwareSignal>true</TriggerHardwareSignal>
            <HoldTimeSeconds>2</HoldTimeSeconds>
            <Description>Hardware fault detection</Description>
        </Condition>
        <Condition>
            <Name>OVERVOLTAGE</Name>
            <Pattern>OVERVOLT|VOLTAGE.*HIGH</Pattern>
            <IsRegex>true</IsRegex>
            <TriggerHardwareSignal>true</TriggerHardwareSignal>
            <HoldTimeSeconds>5</HoldTimeSeconds>
            <Description>Overvoltage protection</Description>
        </Condition>
        <Condition>
            <Name>EMERGENCY_STOP</Name>
            <Pattern>EMERGENCY|ESTOP</Pattern>
            <IsRegex>true</IsRegex>
            <TriggerHardwareSignal>true</TriggerHardwareSignal>
            <HoldTimeSeconds>10</HoldTimeSeconds>
            <Description>Emergency stop condition</Description>
        </Condition>
    </CriticalConditions>
    
    <Uuts>
        <Uut>
            <UutId>production_uut</UutId>
            <Description>Production UUT with Critical Monitoring</Description>
            <Ports>
                <Port>
                    <PortNumber>1</PortNumber>
                    <Protocol>rs232</Protocol>
                    <Speed>115200</Speed>
                    <DataPattern>n81</DataPattern>
                    
                    <!-- ✅ Enhanced commands with critical validation -->
                    <TestCommands>
                        <Command>
                            <Command>TEST</Command>
                            <ExpectedResponse>OK</ExpectedResponse>
                            <TimeoutMs>5000</TimeoutMs>
                            <!-- ✅ Multi-level validation patterns -->
                            <ValidationPatterns>
                                <Pattern Level="PASS">OK|PASS</Pattern>
                                <Pattern Level="WARN">SLOW|DELAY</Pattern>
                                <Pattern Level="FAIL">FAIL|ERROR</Pattern>
                                <Pattern Level="CRITICAL">FAULT|EMERGENCY|OVERVOLT</Pattern>
                            </ValidationPatterns>
                        </Command>
                    </TestCommands>
                </Port>
            </Ports>
        </Uut>
    </Uuts>
</BibConfiguration>
```

**Critical Condition Processing:**
```csharp
// ✅ XML critical condition loader
public class CriticalConditionLoader
{
    public List<CriticalCondition> LoadFromXml(string bibConfigPath)
    {
        var doc = XDocument.Load(bibConfigPath);
        var conditions = new List<CriticalCondition>();
        
        foreach (var conditionElement in doc.Descendants("Condition"))
        {
            var condition = new CriticalCondition
            {
                Name = conditionElement.Element("Name")?.Value ?? "",
                Pattern = conditionElement.Element("Pattern")?.Value ?? "",
                IsRegex = bool.Parse(conditionElement.Element("IsRegex")?.Value ?? "false"),
                TriggerHardwareSignal = bool.Parse(conditionElement.Element("TriggerHardwareSignal")?.Value ?? "false"),
                HoldTimeSeconds = int.Parse(conditionElement.Element("HoldTimeSeconds")?.Value ?? "2"),
                Description = conditionElement.Element("Description")?.Value ?? ""
            };
            
            if (condition.IsRegex)
            {
                condition.CompiledPattern = new Regex(condition.Pattern, RegexOptions.IgnoreCase);
            }
            
            conditions.Add(condition);
        }
        
        return conditions;
    }
}

// ✅ Critical condition evaluation with hardware integration
public class CriticalConditionEvaluator
{
    private readonly List<CriticalCondition> _conditions;
    private readonly IBitBangProtocolProvider _bitBangProvider;
    
    public async Task<EnhancedValidationResult> EvaluateResponseAsync(string response)
    {
        foreach (var condition in _conditions)
        {
            bool matches = condition.IsRegex 
                ? condition.CompiledPattern.IsMatch(response)
                : response.Contains(condition.Pattern, StringComparison.OrdinalIgnoreCase);
            
            if (matches)
            {
                _logger.LogCritical("🚨 CRITICAL CONDITION DETECTED: {Name} - {Description}", 
                    condition.Name, condition.Description);
                
                // ✅ Trigger hardware signal if configured
                if (condition.TriggerHardwareSignal)
                {
                    _logger.LogCritical("🔌 TRIGGERING HARDWARE CRITICAL SIGNAL for condition: {Name}", condition.Name);
                    await _bitBangProvider.SetCriticalFailSignalAsync(true);
                    
                    // ✅ Schedule auto-clear based on XML configuration
                    _ = Task.Run(async () =>
                    {
                        await Task.Delay(TimeSpan.FromSeconds(condition.HoldTimeSeconds));
                        await _bitBangProvider.SetCriticalFailSignalAsync(false);
                        _logger.LogInformation("✅ Auto-cleared critical signal for condition: {Name}", condition.Name);
                    });
                }
                
                return EnhancedValidationResult.Critical(
                    $"Critical condition '{condition.Name}': {condition.Description}",
                    pattern: condition.Pattern, 
                    triggerHardware: condition.TriggerHardwareSignal);
            }
        }
        
        // ✅ No critical conditions matched
        return EnhancedValidationResult.Pass("No critical conditions detected");
    }
}
```

### **🔄 OBJECTIVE 4: Original Service Behavior Restoration (Priority 2)**
**Priority:** 🎯 **HIGH** | **Effort:** 2-3 hours | **Status:** RETURN TO FOUNDATIONS

**Service Architecture Aligned with First Commits:**
```csharp
// ✅ Restore original service behavior patterns
public class SerialPortPoolService : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("🚀 SerialPortPool Service Starting - Sprint 13 Hardware Integration");
        
        // ✅ Initialize hardware GPIO monitoring
        await InitializeHardwareMonitoringAsync();
        
        // ✅ Wait for hardware triggers (original pattern)
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // ✅ Hardware monitoring handles start/stop triggers
                // Service remains responsive but waits for GPIO signals
                await Task.Delay(1000, stoppingToken);
                
                // ✅ Optional: heartbeat logging
                if (DateTime.Now.Second == 0) // Once per minute
                {
                    _logger.LogInformation("💓 Service heartbeat - GPIO monitoring active");
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("🛑 Service shutdown requested");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Service error - continuing operation");
            }
        }
        
        // ✅ Cleanup hardware monitoring
        await CleanupHardwareMonitoringAsync();
        
        _logger.LogInformation("✅ SerialPortPool Service Stopped");
    }
    
    private async Task InitializeHardwareMonitoringAsync()
    {
        _logger.LogInformation("🔌 Initializing hardware GPIO monitoring...");
        
        // ✅ Load configuration from XML (original pattern)
        var config = await LoadBitBangConfigurationAsync();
        
        // ✅ Initialize GPIO provider (real or simulated)
        await _bitBangProvider.InitializeAsync(config);
        
        _logger.LogInformation("✅ Hardware GPIO monitoring initialized");
    }
}

// ✅ Original startup pattern with hardware integration
public class Program
{
    public static async Task Main(string[] args)
    {
        // ✅ Original logging configuration
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .WriteTo.File("C:\\Logs\\SerialPortPool\\service-.log", rollingInterval: RollingInterval.Day)
            .CreateLogger();
        
        try
        {
            _logger.LogInformation("🚀 SerialPortPool Service Starting - Sprint 13");
            
            // ✅ Parse command line (original + new GPIO options)
            var options = ParseCommandLine(args);
            
            // ✅ Build service with hardware integration
            var host = CreateHostBuilder(args, options).Build();
            
            // ✅ Start service (original async pattern)
            await host.RunAsync();
        }
        catch (Exception ex)
        {
            _logger.LogFatal(ex, "💥 Service failed to start");
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }
}
```

### **🧪 OBJECTIVE 5: Integration Testing (Priority 3)**
**Priority:** ✅ **MEDIUM** | **Effort:** 2-3 hours | **Status:** HARDWARE + SIMULATION

**Comprehensive Testing Strategy:**
```csharp
[TestFixture]
public class HardwareBitBangIntegrationTests
{
    [Test]
    public async Task RealGpioProvider_InitializePortD_ConfiguresCorrectly()
    {
        // Arrange
        var config = BitBangConfiguration.CreateDefault();
        var provider = new RealFtdiBitBangProvider();
        
        // Act & Assert (requires actual hardware)
        if (IsHardwareAvailable())
        {
            await provider.InitializeAsync(config);
            var status = await provider.GetStatusAsync();
            
            Assert.That(status.IsConnected, Is.True);
            Assert.That(status.DeviceId, Is.EqualTo("FT9A9OFO"));
        }
        else
        {
            Assert.Ignore("Hardware not available - skipping real GPIO test");
        }
    }
    
    [Test]
    public async Task SimulatedProvider_StartTrigger_ExecutesWorkflow()
    {
        // Arrange
        var simulatedProvider = new SimulatedBitBangProvider();
        var workflowService = new Mock<IMultiBibWorkflowService>();
        
        // Act
        await simulatedProvider.SimulateStartTriggerAsync();
        
        // Assert
        workflowService.Verify(s => s.ExecuteConfiguredBibAsync(), Times.Once);
    }
    
    [Test]
    public async Task CriticalConditionEvaluator_HardwareFault_TriggersCriticalSignal()
    {
        // Arrange
        var conditions = new List<CriticalCondition>
        {
            new CriticalCondition 
            { 
                Name = "HARDWARE_FAULT", 
                Pattern = "FAULT", 
                TriggerHardwareSignal = true 
            }
        };
        
        var mockProvider = new Mock<IBitBangProtocolProvider>();
        var evaluator = new CriticalConditionEvaluator(conditions, mockProvider.Object);
        
        // Act
        var result = await evaluator.EvaluateResponseAsync("SYSTEM FAULT DETECTED");
        
        // Assert
        Assert.That(result.Level, Is.EqualTo(ValidationLevel.CRITICAL));
        mockProvider.Verify(p => p.SetCriticalFailSignalAsync(true), Times.Once);
    }
    
    [Test]
    public async Task CommandLineOptions_SimulationMode_UseSimulatedProvider()
    {
        // Arrange
        var args = new[] { "--gpio-mode", "simulation", "--sim-start" };
        
        // Act
        var options = Parser.Default.ParseArguments<Sprint13CommandLineOptions>(args);
        
        // Assert
        await options.WithParsedAsync(opts =>
        {
            Assert.That(opts.GpioMode, Is.EqualTo("simulation"));
            Assert.That(opts.SimulateStart, Is.True);
            return Task.CompletedTask;
        });
    }
}
```

---

## 📊 Sprint 13 Timeline - HARDWARE FOCUSED

| **Objective** | **Effort** | **Priority** | **Days** |
|---------------|------------|--------------|----------|
| **Real GPIO Input Monitoring** | 4-5h | ⭐ **HIGHEST** | Day 1-2 |
| **Command Line Simulation** | 2-3h | ⭐ **HIGHEST** | Day 2 |
| **XML Critical Configuration** | 2-3h | ⭐ **HIGHEST** | Day 3 |
| **Original Service Behavior** | 2-3h | 🎯 **HIGH** | Day 4 |
| **Integration Testing** | 2-3h | ✅ **MEDIUM** | Day 5 |

**Total Sprint 13 Effort:** 12-17 hours  
**Timeline:** 5 days  
**Dependencies:** FT4232HA hardware availability

---

## ✅ Sprint 13 Success Criteria

### **🔌 Real Hardware Integration**
- ✅ **FT4232HA Port D GPIO** - Successful bit-bang mode configuration
- ✅ **Hardware Start Trigger** - DD0 signal triggers workflow execution
- ✅ **Hardware Stop Trigger** - DD1 signal gracefully stops workflow
- ✅ **Critical Signal Output** - DD2 activates on critical conditions
- ✅ **Status Signal Output** - DD3 indicates workflow active state

### **💻 Simulation Capability**
- ✅ **Command Line Simulation** - `-simStart`, `-simStop`, `-simCritical` working
- ✅ **Development Testing** - Full functionality without hardware requirements
- ✅ **GPIO Mode Selection** - `--gpio-mode real|simulation|disabled`
- ✅ **Configurable Polling** - `--gpio-polling` for different update rates

### **🚨 Critical Condition System**
- ✅ **XML Configuration** - Critical conditions loaded from BIB XML files
- ✅ **Pattern Matching** - Regex and string-based critical detection
- ✅ **Hardware Triggering** - Critical validation → DD2 GPIO signal
- ✅ **Auto-Clear Timing** - Configurable signal hold times
- ✅ **Comprehensive Logging** - Full critical condition audit trail

### **🔄 Service Behavior**
- ✅ **Original Architecture** - Return to first commit service patterns
- ✅ **Hardware-Driven Operation** - Service responds to GPIO triggers
- ✅ **Background Service** - Proper Windows Service behavior
- ✅ **Error Recovery** - Robust hardware error handling
- ✅ **Performance** - <100ms GPIO response times

---

## 🚧 Sprint 13 Risk Assessment

### **Risk 1: Hardware Availability**
- **Impact:** Cannot test real GPIO functionality
- **Mitigation:** Comprehensive simulation mode, hardware detection logic
- **Status:** LOW RISK (simulation provides full testing capability)

### **Risk 2: FTD2XX_NET Integration**
- **Impact:** Library conflicts, GPIO timing issues
- **Mitigation:** Use proven Sprint 10 implementation, error recovery
- **Status:** LOW RISK (Sprint 10 research provides solutions)

### **Risk 3: GPIO Timing Requirements**
- **Impact:** 100ms polling might miss fast signals
- **Mitigation:** Configurable polling rate, edge detection logic
- **Status:** LOW RISK (hardware spec allows for 100ms response)

---

## 🎯 Sprint 13 = Hardware Foundation Excellence

### **✅ Real Hardware Control**
- **Physical GPIO Integration** - Actual FT4232HA Port D control
- **Hardware-Triggered Workflows** - Start/Stop controlled by external signals
- **Critical Hardware Response** - Emergency signal output capability
- **Professional Hardware Interface** - Industrial-grade GPIO control

### **✅ Development & Testing Excellence**
- **Simulation Mode** - Complete testing without hardware requirements
- **Command Line Control** - Easy testing and demonstration capability
- **XML Configuration** - Flexible critical condition management
- **Comprehensive Testing** - Both hardware and simulation paths validated

### **✅ Production Ready Foundation**
- **Original Service Behavior** - Return to proven architecture patterns
- **Enterprise Logging** - Full audit trail for hardware interactions
- **Error Recovery** - Robust handling of hardware failures
- **Scalable Architecture** - Ready for Sprint 14 parallel execution

---

## 🎬 Expected Client Demo Flow

### **Demo Scenario: Real Hardware Control**

```bash
🎬 DEMO: Real Hardware GPIO Control

[14:30:00] 💻 Command: .\SerialPortPoolService.exe --gpio-mode real --bib-ids client_demo
[14:30:01] 🔌 FT4232HA Port D GPIO initialized successfully
[14:30:02] 🔍 Starting GPIO input monitoring...
[14:30:02] 💓 Service heartbeat - GPIO monitoring active

[14:30:15] 🚀 GPIO TRIGGER: Power On Ready detected (DD0 HIGH)
[14:30:15] 📡 WORKFLOW ACTIVE SIGNAL: ACTIVE via GPIO DD3  
[14:30:16] ✅ client_demo/production_uut 🚀 WORKFLOW STARTING
[14:30:20] ✅ client_demo/production_uut ✅ COMPLETE: Workflow SUCCESS
[14:30:21] 📡 WORKFLOW ACTIVE SIGNAL: CLEARED via GPIO DD3

[14:30:45] 🚨 Critical response detected: "HARDWARE FAULT"
[14:30:45] 🔌 TRIGGERING HARDWARE CRITICAL SIGNAL (DD2 HIGH)
[14:30:47] ✅ Auto-cleared critical signal (2s hold time)

[14:31:00] ⚠️ GPIO TRIGGER: Power Down Heads-Up detected (DD1 HIGH)
[14:31:00] 🛑 Service shutdown requested - graceful termination

CLIENT REACTION: "Perfect! Real hardware control exactly as specified!"
```

### **Demo Scenario: Simulation Mode Testing**

```bash
🎬 DEMO: Simulation Mode Development Testing

[14:35:00] 💻 Command: .\SerialPortPoolService.exe --gpio-mode simulation --sim-start
[14:35:01] 🎭 SIMULATION: Triggering start signal...
[14:35:02] 🚀 HARDWARE TRIGGER: Starting workflow execution...
[14:35:02] 📡 WORKFLOW ACTIVE SIGNAL: ACTIVE (simulated)

[14:35:10] 💻 Command: .\SerialPortPoolService.exe --gpio-mode simulation --sim-critical
[14:35:11] 🎭 SIMULATION: Triggering critical condition...
[14:35:11] 🎭 SIMULATED CRITICAL SIGNAL: ACTIVE
[14:35:13] 🎭 SIMULATED CRITICAL SIGNAL: Auto-cleared

CLIENT REACTION: "Excellent! Full development capability without hardware dependency!"
```

---

## 🚀 Sprint 14 Ready

### **Sprint 13 Hardware Foundation Enables:**
- **Parallel Multi-BIB** - Hardware control during concurrent execution  
- **Advanced Analytics** - GPIO interaction performance metrics
- **Enterprise Monitoring** - Hardware status in dashboard
- **Production Deployment** - Proven hardware integration reliability

---

*Sprint 13 Planning - Real Hardware BitBang GPIO Integration*  
*Created: September 4, 2025*  
*Client Priority: Hardware Control Foundation + Simulation*  
*Risk Level: LOW-MEDIUM | Impact Level: HIGH*

**🚀 Sprint 13 = Hardware Control Foundation Excellence! 🚀**