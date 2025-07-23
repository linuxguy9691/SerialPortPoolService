# Sprint 5 Planning - Industrial Business Logic & Communication Sequences

![Sprint](https://img.shields.io/badge/Sprint%205-📋%20PLANNED-blue.svg)
![Focus](https://img.shields.io/badge/Focus-EEPROM%20%2B%20Sequences-purple.svg)
![Duration](https://img.shields.io/badge/Duration-4%20weeks-green.svg)
![Foundation](https://img.shields.io/badge/Foundation-Sprint%204%20Complete-gold.svg)

## 🎯 **Objectif Sprint 5 - Industrial Business Logic Layer**

**OBJECTIF :** Implémenter la **couche business logic industrielle** complète en s'appuyant sur l'excellente foundation technique des Sprints 3-4.

**FOUNDATION DISPONIBLE :**
- ✅ **Service Windows** + Enhanced Discovery + Pool Management (Sprint 3)
- ✅ **Device Grouping** + Multi-port awareness (Sprint 3)  
- ✅ **Bit-Bang Communication** + Power signals (Sprint 4)
- ✅ **Background Monitoring** + Service integration (Sprint 4)

**BUSINESS VALUE SPRINT 5 :**
- 📋 **Industrial EEPROM Parsing** : Extract BIB ID, DUT#, UART#, Slot Position
- 🔄 **Communication Sequence Management** : Sequences spécifiques par BIB
- 🏭 **Complete Industrial Workflow** : End-to-end automation ready

---

## 📋 **Scope Sprint 5 - Business Logic Focused**

### **✅ CORE DELIVERABLES**
- 🔍 **Industrial EEPROM Parsing** : BIB/DUT/UART/Slot extraction
- 📋 **Communication Sequence Engine** : Init → Polling → Shutdown sequences
- 🏭 **BIB-to-Sequence Mapping** : Configuration-driven sequences
- 🔧 **Enhanced Pool Integration** : Industrial allocation workflow
- 🎬 **Complete Industrial Demo** : End-to-end workflow demonstration

### **🎯 BUSINESS ALIGNMENT**
Addresses core client requirements :
- ✅ *"Get information about Driver Board via USB2Serial"*
- ✅ *"Extract BIB ID, DUT#, UART# from USB2Serial"*  
- ✅ *"List of communication sequences specific to BIB"*
- ✅ *"Power ON/OFF signals for UART polling"* (Sprint 4 ✅)

---

## 🏗️ **Architecture Sprint 5 - Business Logic Layer**

```
SerialPortPool.Core/                           ← Enhanced Core Library
├── Models/
│   ├── IndustrialInfo.cs                     ← NEW: BIB/DUT/UART/Slot data
│   ├── CommunicationSequence.cs              ← NEW: Init/Polling/Shutdown
│   ├── CommandStep.cs                        ← NEW: Individual command
│   ├── SequenceConfiguration.cs              ← NEW: BIB-to-sequence mapping
│   └── IndustrialWorkflow.cs                 ← NEW: Complete workflow state
├── Services/
│   ├── IndustrialEepromParser.cs             ← NEW: EEPROM business parsing
│   ├── SequenceManager.cs                    ← NEW: Sequence execution engine
│   ├── IndustrialPoolManager.cs              ← NEW: Industrial allocation
│   ├── WorkflowOrchestrator.cs               ← NEW: Complete workflow
│   └── SequenceConfigurationLoader.cs        ← NEW: Configuration management
└── Interfaces/
    ├── IIndustrialEepromParser.cs            ← NEW: EEPROM parsing contract
    ├── ISequenceManager.cs                   ← NEW: Sequence execution
    └── IIndustrialPoolManager.cs             ← NEW: Industrial pool contract

SerialPortPoolService/                          ← Enhanced Windows Service
├── Services/
│   ├── IndustrialBackgroundService.cs        ← NEW: Industrial monitoring
│   ├── SequenceExecutionService.cs           ← NEW: Background execution
│   └── BitBangBackgroundService.cs           ← EXISTING: From Sprint 4
├── Configuration/
│   ├── IndustrialSettings.cs                 ← NEW: Industrial configuration
│   └── SequenceSettings.cs                   ← NEW: Sequence configuration
└── Controllers/ (Optional)
    └── IndustrialController.cs               ← OPTIONAL: Simple API if needed

tests/SerialPortPool.Core.Tests/               ← Enhanced Test Suite
├── Services/
│   ├── IndustrialEepromParserTests.cs        ← NEW: 10 tests
│   ├── SequenceManagerTests.cs               ← NEW: 12 tests  
│   ├── IndustrialPoolManagerTests.cs         ← NEW: 8 tests
│   └── WorkflowOrchestratorTests.cs          ← NEW: 10 tests
└── Integration/
    └── IndustrialWorkflowIntegrationTests.cs ← NEW: 12 tests end-to-end
```

---

## 📅 **Sprint 5 Planning - 4 Semaines Business Logic**

### **🔹 SEMAINE 1: Industrial EEPROM Parsing (5 jours)**
**Objectif :** Extract BIB ID, DUT#, UART#, Slot Position depuis EEPROM

#### **Jour 1-2: Industrial Models**
```csharp
// SerialPortPool.Core/Models/IndustrialInfo.cs
public class IndustrialInfo
{
    public string BibId { get; set; } = "";           // "BIB_001"
    public string DutNumber { get; set; } = "";       // "DUT_05"  
    public string UartNumber { get; set; } = "";      // "UART_2"
    public string SlotPosition { get; set; } = "";    // "Slot_01"
    public string OvenId { get; set; } = "";          // "Oven_A"
    public string TestBoardType { get; set; } = "";   // "TestBoard_v2"
    public Dictionary<string, string> CustomProperties { get; set; } = new();
    
    /// <summary>
    /// Parse industrial info from EEPROM data
    /// </summary>
    public static IndustrialInfo ParseFromEeprom(Dictionary<string, string> eepromData)
    {
        return new IndustrialInfo
        {
            BibId = ExtractValue(eepromData, "BibId", "BIB_ID", "BIB"),
            DutNumber = ExtractValue(eepromData, "DutNumber", "DUT_NUMBER", "DUT"),
            UartNumber = ExtractValue(eepromData, "UartNumber", "UART_NUMBER", "UART"),
            SlotPosition = ExtractValue(eepromData, "SlotPosition", "SLOT_POSITION", "SLOT"),
            OvenId = ExtractValue(eepromData, "OvenId", "OVEN_ID", "OVEN"),
            TestBoardType = ExtractValue(eepromData, "TestBoardType", "BOARD_TYPE", "TYPE")
        };
    }
    
    private static string ExtractValue(Dictionary<string, string> data, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (data.TryGetValue(key, out var value) && !string.IsNullOrEmpty(value))
                return value;
        }
        return "Unknown";
    }
    
    /// <summary>
    /// Generate unique identifier for this industrial configuration
    /// </summary>
    public string GetUniqueId() => $"{BibId}_{DutNumber}_{UartNumber}";
    
    /// <summary>
    /// Check if this industrial info is valid for automation
    /// </summary>
    public bool IsValidForAutomation => 
        !string.IsNullOrEmpty(BibId) && BibId != "Unknown" &&
        !string.IsNullOrEmpty(DutNumber) && DutNumber != "Unknown";
}
```

#### **Jour 3-4: EEPROM Parser Service**
```csharp
// SerialPortPool.Core/Services/IndustrialEepromParser.cs
public class IndustrialEepromParser : IIndustrialEepromParser
{
    private readonly IFtdiDeviceReader _ftdiReader;
    private readonly ILogger<IndustrialEepromParser> _logger;
    
    /// <summary>
    /// Extract industrial information from allocated port
    /// </summary>
    public async Task<IndustrialInfo?> ParseIndustrialInfoAsync(PortAllocation allocation)
    {
        try
        {
            _logger.LogDebug("Parsing industrial info for port {PortName}", allocation.PortName);
            
            // Read EEPROM data using existing Sprint 2 functionality
            var eepromData = await _ftdiReader.ReadEepromDataAsync(allocation.PortName);
            
            if (!eepromData.Any())
            {
                _logger.LogWarning("No EEPROM data found for port {PortName}", allocation.PortName);
                return null;
            }
            
            // Parse using business logic
            var industrialInfo = IndustrialInfo.ParseFromEeprom(eepromData);
            
            _logger.LogInformation("Industrial info parsed: {UniqueId} for port {PortName}", 
                industrialInfo.GetUniqueId(), allocation.PortName);
                
            return industrialInfo;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing industrial info for port {PortName}", allocation.PortName);
            return null;
        }
    }
    
    /// <summary>
    /// Find devices by industrial criteria
    /// </summary>
    public async Task<IEnumerable<(DeviceGroup device, IndustrialInfo info)>> FindDevicesByIndustrialCriteriaAsync(
        string? bibId = null, 
        string? ovenId = null, 
        string? slotPosition = null)
    {
        var results = new List<(DeviceGroup, IndustrialInfo)>();
        
        // Use existing Sprint 3 device discovery
        var deviceGroups = await _discovery.DiscoverDeviceGroupsAsync();
        
        foreach (var device in deviceGroups.Where(d => d.IsFtdiDevice))
        {
            try
            {
                var firstPort = device.Ports.FirstOrDefault();
                if (firstPort != null)
                {
                    var eepromData = await _ftdiReader.ReadEepromDataAsync(firstPort.PortName);
                    var industrialInfo = IndustrialInfo.ParseFromEeprom(eepromData);
                    
                    // Apply filters
                    if (MatchesCriteria(industrialInfo, bibId, ovenId, slotPosition))
                    {
                        results.Add((device, industrialInfo));
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not parse industrial info for device {DeviceId}", device.DeviceId);
            }
        }
        
        return results;
    }
}
```

#### **Jour 5: Integration avec Pool Management**
```csharp
// Extension de SerialPortPool.cs avec industrial awareness
public class SerialPortPool : ISerialPortPool
{
    // Existing methods...
    
    /// <summary>
    /// Allocate port with industrial requirements
    /// </summary>
    public async Task<(PortAllocation? allocation, IndustrialInfo? industrialInfo)> 
        AllocateIndustrialPortAsync(
            string? requiredBibId = null,
            string? requiredOvenId = null,
            string? clientId = null)
    {
        // Use existing allocation with client config (FT4232H only)
        var clientConfig = PortValidationConfiguration.CreateClientDefault();
        var allocation = await AllocatePortAsync(clientConfig, clientId);
        
        if (allocation == null)
            return (null, null);
            
        // Parse industrial info
        var industrialInfo = await _industrialParser.ParseIndustrialInfoAsync(allocation);
        
        // Validate industrial requirements
        if (!string.IsNullOrEmpty(requiredBibId) && 
            industrialInfo?.BibId != requiredBibId)
        {
            // Release and try next port
            await ReleasePortAsync(allocation.PortName, allocation.SessionId);
            return await AllocateIndustrialPortAsync(requiredBibId, requiredOvenId, clientId);
        }
        
        return (allocation, industrialInfo);
    }
}
```

**Deliverable Semaine 1 :** ✅ Industrial EEPROM parsing operational

---

### **🔹 SEMAINE 2: Communication Sequence Engine (5 jours)**
**Objectif :** Business logic sequences (Init → Polling → Shutdown)

#### **Jour 1-2: Sequence Models**
```csharp
// SerialPortPool.Core/Models/CommunicationSequence.cs
public class CommunicationSequence
{
    public string SequenceId { get; set; } = "";       // "StandardTest_v1"
    public string BibId { get; set; } = "";            // "BIB_001" 
    public string Description { get; set; } = "";       // "Standard test sequence"
    
    // Serial port configuration
    public SerialConfiguration PortConfig { get; set; } = new()
    {
        BaudRate = 115200,
        Parity = Parity.None,
        DataBits = 8,
        StopBits = StopBits.One,
        ReadTimeout = 2000,
        WriteTimeout = 2000
    };
    
    // Sequence phases
    public List<CommandStep> InitializationCommands { get; set; } = new();
    public List<CommandStep> PollingCommands { get; set; } = new();
    public List<CommandStep> ShutdownCommands { get; set; } = new();
    
    // Timing configuration
    public TimeSpan PollingInterval { get; set; } = TimeSpan.FromSeconds(1);
    public TimeSpan MaxExecutionTime { get; set; } = TimeSpan.FromMinutes(30);
    public int MaxPollingCycles { get; set; } = 1000;
}

// SerialPortPool.Core/Models/CommandStep.cs
public class CommandStep
{
    public string StepId { get; set; } = "";            // "INIT_01"
    public string Command { get; set; } = "";           // "AT+STATUS\r\n"
    public string? ExpectedResponse { get; set; }       // "OK"
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(2);
    public int RetryCount { get; set; } = 3;
    public TimeSpan DelayAfter { get; set; } = TimeSpan.FromMilliseconds(100);
    public bool IsOptional { get; set; } = false;
    public string Description { get; set; } = "";
    
    /// <summary>
    /// Validate if response matches expected
    /// </summary>
    public bool ValidateResponse(string response)
    {
        if (string.IsNullOrEmpty(ExpectedResponse))
            return true; // No validation required
            
        return response.Contains(ExpectedResponse, StringComparison.OrdinalIgnoreCase);
    }
}

// SerialPortPool.Core/Models/SequenceConfiguration.cs
public class SequenceConfiguration
{
    public Dictionary<string, CommunicationSequence> BibSequences { get; set; } = new();
    public CommunicationSequence DefaultSequence { get; set; } = new();
    
    /// <summary>
    /// Get sequence for specific BIB ID
    /// </summary>
    public CommunicationSequence GetSequenceForBib(string bibId)
    {
        return BibSequences.TryGetValue(bibId, out var sequence) ? sequence : DefaultSequence;
    }
    
    /// <summary>
    /// Load configuration from JSON file
    /// </summary>
    public static SequenceConfiguration LoadFromFile(string filePath)
    {
        var json = File.ReadAllText(filePath);
        return JsonSerializer.Deserialize<SequenceConfiguration>(json) ?? new();
    }
}
```

#### **Jour 3-4: Sequence Manager**
```csharp
// SerialPortPool.Core/Services/SequenceManager.cs
public class SequenceManager : ISequenceManager
{
    private readonly ILogger<SequenceManager> _logger;
    private readonly SequenceConfiguration _configuration;
    
    /// <summary>
    /// Execute complete communication sequence
    /// </summary>
    public async Task<SequenceExecutionResult> ExecuteSequenceAsync(
        PortAllocation allocation, 
        IndustrialInfo industrialInfo,
        CancellationToken cancellationToken = default)
    {
        var sequence = _configuration.GetSequenceForBib(industrialInfo.BibId);
        var result = new SequenceExecutionResult { StartTime = DateTime.Now };
        
        SerialPort? serialPort = null;
        
        try
        {
            _logger.LogInformation("Starting sequence {SequenceId} for {BibId} on port {PortName}", 
                sequence.SequenceId, industrialInfo.BibId, allocation.PortName);
                
            // Configure and open serial port
            serialPort = new SerialPort(allocation.PortName);
            ConfigureSerialPort(serialPort, sequence.PortConfig);
            serialPort.Open();
            
            // Phase 1: Initialization
            result.InitializationResult = await ExecuteCommandPhaseAsync(
                serialPort, sequence.InitializationCommands, "Initialization", cancellationToken);
                
            if (!result.InitializationResult.Success)
            {
                result.Success = false;
                result.ErrorMessage = "Initialization phase failed";
                return result;
            }
            
            // Phase 2: Polling Loop
            result.PollingResult = await ExecutePollingPhaseAsync(
                serialPort, sequence, cancellationToken);
                
            // Phase 3: Shutdown (always execute)
            result.ShutdownResult = await ExecuteCommandPhaseAsync(
                serialPort, sequence.ShutdownCommands, "Shutdown", cancellationToken);
                
            result.Success = result.PollingResult.Success;
            result.EndTime = DateTime.Now;
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing sequence for {BibId}", industrialInfo.BibId);
            result.Success = false;
            result.ErrorMessage = ex.Message;
            result.EndTime = DateTime.Now;
            return result;
        }
        finally
        {
            serialPort?.Dispose();
        }
    }
    
    private async Task<PhaseResult> ExecutePollingPhaseAsync(
        SerialPort serialPort, 
        CommunicationSequence sequence,
        CancellationToken cancellationToken)
    {
        var result = new PhaseResult { PhaseName = "Polling" };
        var cycleCount = 0;
        var startTime = DateTime.Now;
        
        while (!cancellationToken.IsCancellationRequested && 
               cycleCount < sequence.MaxPollingCycles &&
               DateTime.Now - startTime < sequence.MaxExecutionTime)
        {
            var cycleResult = await ExecuteCommandPhaseAsync(
                serialPort, sequence.PollingCommands, $"Polling-{cycleCount}", cancellationToken);
                
            result.StepResults.AddRange(cycleResult.StepResults);
            
            if (!cycleResult.Success && !sequence.PollingCommands.All(c => c.IsOptional))
            {
                result.Success = false;
                result.ErrorMessage = $"Polling cycle {cycleCount} failed";
                break;
            }
            
            cycleCount++;
            await Task.Delay(sequence.PollingInterval, cancellationToken);
        }
        
        result.Success = !cancellationToken.IsCancellationRequested;
        return result;
    }
}
```

#### **Jour 5: Configuration Management**
```csharp
// Default sequence configurations
// SerialPortPoolService/Configuration/default-sequences.json
{
  "BibSequences": {
    "BIB_001": {
      "SequenceId": "BIB_001_Standard",
      "BibId": "BIB_001",
      "Description": "Standard test sequence for BIB 001",
      "PortConfig": {
        "BaudRate": 115200,
        "Parity": "None",
        "DataBits": 8,
        "StopBits": "One"
      },
      "InitializationCommands": [
        {
          "StepId": "INIT_01",
          "Command": "ATZ\r\n",
          "ExpectedResponse": "OK",
          "Timeout": "00:00:02",
          "Description": "Reset device"
        },
        {
          "StepId": "INIT_02", 
          "Command": "AT+ECHO=0\r\n",
          "ExpectedResponse": "OK",
          "Description": "Disable echo"
        }
      ],
      "PollingCommands": [
        {
          "StepId": "POLL_01",
          "Command": "AT+STATUS\r\n",
          "ExpectedResponse": "STATUS",
          "Description": "Check device status"
        }
      ],
      "ShutdownCommands": [
        {
          "StepId": "SHUT_01",
          "Command": "AT+DISCONNECT\r\n",
          "ExpectedResponse": "OK",
          "Description": "Graceful disconnect"
        }
      ],
      "PollingInterval": "00:00:01",
      "MaxExecutionTime": "00:30:00"
    }
  },
  "DefaultSequence": {
    "SequenceId": "Default",
    "BibId": "DEFAULT",
    "Description": "Default sequence for unknown BIBs"
  }
}
```

**Deliverable Semaine 2 :** ✅ Communication sequence engine operational

---

### **🔹 SEMAINE 3: Complete Industrial Workflow (5 jours)**
**Objectif :** End-to-end industrial automation workflow

#### **Jour 1-2: Workflow Orchestrator**
```csharp
// SerialPortPool.Core/Services/WorkflowOrchestrator.cs
public class WorkflowOrchestrator
{
    private readonly IIndustrialPoolManager _poolManager;
    private readonly ISequenceManager _sequenceManager;
    private readonly IBitBangService _bitBangService; // From Sprint 4
    private readonly ILogger<WorkflowOrchestrator> _logger;
    
    /// <summary>
    /// Execute complete industrial workflow for a BIB
    /// </summary>
    public async Task<IndustrialWorkflowResult> ExecuteIndustrialWorkflowAsync(
        string bibId,
        string clientId = "IndustrialWorkflow",
        CancellationToken cancellationToken = default)
    {
        var workflowResult = new IndustrialWorkflowResult 
        { 
            BibId = bibId,
            StartTime = DateTime.Now 
        };
        
        try
        {
            _logger.LogInformation("Starting industrial workflow for BIB {BibId}", bibId);
            
            // Step 1: Allocate industrial port
            var (allocation, industrialInfo) = await _poolManager
                .AllocateIndustrialPortAsync(requiredBibId: bibId, clientId: clientId);
                
            if (allocation == null || industrialInfo == null)
            {
                workflowResult.Success = false;
                workflowResult.ErrorMessage = $"Could not allocate port for BIB {bibId}";
                return workflowResult;
            }
            
            workflowResult.Allocation = allocation;
            workflowResult.IndustrialInfo = industrialInfo;
            
            // Step 2: Send PowerOn signal (Sprint 4 bit-bang)
            var deviceGroup = await FindDeviceGroupForPort(allocation.PortName);
            if (deviceGroup != null)
            {
                await _bitBangService.SendPowerSignalAsync(
                    deviceGroup.SerialNumber, PowerSignal.PowerOn);
                workflowResult.PowerSignalsSent.Add("PowerOn");
            }
            
            // Step 3: Execute communication sequence
            workflowResult.SequenceResult = await _sequenceManager
                .ExecuteSequenceAsync(allocation, industrialInfo, cancellationToken);
                
            // Step 4: Send TestStart signal
            if (workflowResult.SequenceResult.InitializationResult.Success && deviceGroup != null)
            {
                await _bitBangService.SendPowerSignalAsync(
                    deviceGroup.SerialNumber, PowerSignal.TestStart);
                workflowResult.PowerSignalsSent.Add("TestStart");
            }
            
            // Step 5: Wait for sequence completion or cancellation
            // (Sequence manager handles the polling loop)
            
            // Step 6: Send TestStop signal
            if (deviceGroup != null)
            {
                await _bitBangService.SendPowerSignalAsync(
                    deviceGroup.SerialNumber, PowerSignal.TestStop);
                workflowResult.PowerSignalsSent.Add("TestStop");
            }
            
            // Step 7: Send PowerOff signal
            if (deviceGroup != null)
            {
                await _bitBangService.SendPowerSignalAsync(
                    deviceGroup.SerialNumber, PowerSignal.PowerOff);
                workflowResult.PowerSignalsSent.Add("PowerOff");
            }
            
            // Step 8: Release port
            await _poolManager.ReleasePortAsync(allocation.PortName, allocation.SessionId);
            workflowResult.PortReleased = true;
            
            workflowResult.Success = workflowResult.SequenceResult.Success;
            workflowResult.EndTime = DateTime.Now;
            
            return workflowResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in industrial workflow for BIB {BibId}", bibId);
            workflowResult.Success = false;
            workflowResult.ErrorMessage = ex.Message;
            workflowResult.EndTime = DateTime.Now;
            return workflowResult;
        }
    }
    
    /// <summary>
    /// Execute workflows for multiple BIBs concurrently
    /// </summary>
    public async Task<List<IndustrialWorkflowResult>> ExecuteMultipleBibWorkflowsAsync(
        IEnumerable<string> bibIds,
        int maxConcurrency = 3,
        CancellationToken cancellationToken = default)
    {
        var semaphore = new SemaphoreSlim(maxConcurrency, maxConcurrency);
        var tasks = bibIds.Select(async bibId =>
        {
            await semaphore.WaitAsync(cancellationToken);
            try
            {
                return await ExecuteIndustrialWorkflowAsync(bibId, 
                    $"MultiWorkflow-{bibId}", cancellationToken);
            }
            finally
            {
                semaphore.Release();
            }
        });
        
        return (await Task.WhenAll(tasks)).ToList();
    }
}
```

#### **Jour 3-4: Service Integration**
```csharp
// SerialPortPoolService/Services/IndustrialBackgroundService.cs
public class IndustrialBackgroundService : BackgroundService
{
    private readonly WorkflowOrchestrator _orchestrator;
    private readonly ILogger<IndustrialBackgroundService> _logger;
    private readonly IndustrialSettings _settings;
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("🏭 Starting Industrial Background Service...");
        
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Check for scheduled workflows
                await ProcessScheduledWorkflowsAsync(stoppingToken);
                
                // Monitor ongoing workflows
                await MonitorActiveWorkflowsAsync();
                
                // Cleanup completed workflows
                await CleanupCompletedWorkflowsAsync();
                
                await Task.Delay(_settings.MonitoringInterval, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in industrial background service");
            }
        }
    }
}

// SerialPortPoolService/Program.cs - Enhanced DI
services.AddSingleton<IndustrialSettings>();
services.AddScoped<IIndustrialEepromParser, IndustrialEepromParser>();
services.AddScoped<ISequenceManager, SequenceManager>();
services.AddScoped<IIndustrialPoolManager, IndustrialPoolManager>();
services.AddScoped<WorkflowOrchestrator>();
services.AddHostedService<IndustrialBackgroundService>();

// Load sequence configuration
var sequenceConfig = SequenceConfiguration.LoadFromFile("Configuration/sequences.json");
services.AddSingleton(sequenceConfig);
```

#### **Jour 5: Complete Integration Testing**
```csharp
// tests/SerialPortPool.Core.Tests/Integration/IndustrialWorkflowIntegrationTests.cs
[Fact] EndToEnd_IndustrialWorkflow_BIB001_CompleteSuccess()
[Fact] EndToEnd_MultipleWorkflows_ConcurrentExecution()
[Fact] EndToEnd_WorkflowWithBitBang_PowerSignalsIntegrated()
[Fact] EndToEnd_ErrorRecovery_GracefulFailure()
```

**Deliverable Semaine 3 :** ✅ Complete industrial workflow operational

---

### **🔹 SEMAINE 4: Production Polish & Demo (5 jours)**
**Objectif :** Production readiness + impressive industrial demo

#### **Jour 1-2: Enhanced Industrial Demo**
```csharp
// tests/IndustrialDemo/IndustrialDemo.cs
class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("🏭 Complete Industrial Automation Demo");
        Console.WriteLine("====================================");
        
        // Phase 1: Discovery + Industrial Info (Sprint 3 + 5)
        await DemoIndustrialDiscovery();
        
        // Phase 2: Bit-Bang Communication (Sprint 4)
        await DemoBitBangCommunication();
        
        // Phase 3: Communication Sequences (Sprint 5)
        await DemoCommunicationSequences();
        
        // Phase 4: Complete Industrial Workflow (Sprint 5)
        await DemoCompleteWorkflow();
        
        Console.WriteLine("✅ Industrial automation demo completed!");
    }
    
    static async Task DemoCompleteWorkflow()
    {
        Console.WriteLine("\n🏭 === COMPLETE INDUSTRIAL WORKFLOW ===");
        
        var bibIds = new[] { "BIB_001", "BIB_002" };
        var results = await orchestrator.ExecuteMultipleBibWorkflowsAsync(bibIds);
        
        foreach (var result in results)
        {
            Console.WriteLine($"📋 BIB {result.BibId}: {(result.Success ? "✅ SUCCESS" : "❌ FAILED")}");
            Console.WriteLine($"   Duration: {result.Duration.TotalSeconds:F1}s");
            Console.WriteLine($"   Power Signals: {string.Join(", ", result.PowerSignalsSent)}");
            Console.WriteLine($"   Sequence Steps: {result.SequenceResult?.StepResults.Count ?? 0}");
        }
    }
}
```

#### **Jour 3-4: Production Features**
- **Enhanced Logging** : Structured logging pour workflows industriels
- **Performance Monitoring** : Metrics pour sequence execution times
- **Error Recovery** : Robust error handling et retry strategies
- **Configuration Validation** : Startup validation des sequences
- **Health Checks** : Industrial service health monitoring

#### **Jour 5: Documentation & Deployment**
- **User Guide** : Industrial workflow documentation
- **Configuration Guide** : Sequence setup et BIB mapping
- **Troubleshooting** : Common issues et solutions
- **Performance Guide** : Optimization recommendations

**Deliverable Semaine 4 :** ✅ Production-ready industrial automation

---

## 🧪 **Testing Strategy Sprint 5**

### **Test Coverage Targets :**
- **Industrial EEPROM :** 10 tests parsing logic
- **Sequence Management :** 12 tests execution engine
- **Pool Integration :** 8 tests industrial allocation
- **Workflow Orchestration :** 10 tests end-to-end
- **Integration Tests :** 12 tests complete scenarios
- **Total :** 52+ nouveaux tests

### **Business Logic Testing :**
- ✅ **Real EEPROM data** avec BIB/DUT/UART scenarios
- ✅ **Communication sequences** avec hardware simulation
- ✅ **Multi-BIB workflows** concurrent execution
- ✅ **Error scenarios** et recovery testing

---

## 🎯 **Success Criteria Sprint 5**

### **Must Have :**
- ✅ Industrial EEPROM parsing extracting BIB/DUT/UART
- ✅ Communication sequences configurable par BIB
- ✅ Complete workflow : allocation → sequence → bit-bang → release
- ✅ Multi-BIB concurrent processing
- ✅ 52+ nouveaux tests passing
- ✅ Production logging et monitoring
- ✅ Zero regression sur Sprints 3-4

### **Nice to Have :**
- 🎯 Advanced sequence debugging et tracing
- 🎯 Real-time workflow monitoring dashboard
- 🎯 Sequence performance optimization
- 🎯 Configuration hot-reload capability

---

## 🚀 **Sprint 6 Foundation Ready**

**Avec Sprint 5 completed :**
- **Complete Industrial Layer** : EEPROM + Sequences ✅
- **End-to-End Automation** : Workflow orchestration ✅
- **Production Ready** : Monitoring + error handling ✅
- **Client Requirements** : All core needs addressed ✅

**Sprint 6 Focus Candidates :**
- **Advanced Monitoring & Analytics** : Performance dashboards
- **High Availability Features** : Clustering et fault tolerance
- **Advanced Sequence Features** : Conditional logic, loops
- **Client-Specific Customizations** : Additional BIB types

---

## 📈 **Expected Business Value**

| Capability | Client Requirement | Sprint 5 Deliverable |
|------------|-------------------|---------------------|
| **Driver Board Info** | "Get information about Driver Board" | ✅ Industrial EEPROM parsing |
| **BIB/DUT/UART Extraction** | "Extract BIB ID, DUT#, UART#" | ✅ IndustrialInfo.ParseFromEeprom() |
| **BIB-Specific Sequences** | "Communication sequences specific to BIB" | ✅ SequenceConfiguration + Manager |
| **UART Polling** | "Power ON/OFF signals for UART polling" | ✅ Integration with Sprint 4 bit-bang |
| **Complete Automation** | End-to-end workflow | ✅ WorkflowOrchestrator |

---

## 🔥 **Sprint 5 - Industrial Business Logic Ready !**

**Foundation Parfaite :**
- **Technical Excellence** : Sprints 3-4 provide solid base
- **Business Focus** : Direct client value delivery  
- **Incremental Approach** : Build on proven components
- **Production Ready** : Full industrial automation capability

**Next Action après Sprint 4 :** Start with `IndustrialInfo` model and EEPROM parsing !

---

*Document créé : 22 Juillet 2025*  
*Sprint 5 Status : 📋 PLANNED - Ready after Sprint 4*  
*Focus : Industrial Business Logic (EEPROM + Sequences)*  
*Duration : 4 semaines*  
*Foundation : Sprint 3 + Sprint 4 Complete*