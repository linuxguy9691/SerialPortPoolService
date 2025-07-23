# Sprint 4 Planning - MVP Industrial Communication

![Sprint](https://img.shields.io/badge/Sprint%204-🚀%20MVP%20READY-brightgreen.svg)
![Focus](https://img.shields.io/badge/Focus-Client%20MVP%20Demo-purple.svg)
![Duration](https://img.shields.io/badge/Duration-4%20weeks-green.svg)
![Foundation](https://img.shields.io/badge/Foundation-Sprint%203%20Excellence-gold.svg)

## 🎯 **Objectif Sprint 4 - MVP Client Ready**

**OBJECTIF MVP :** Livrer un **package d'installation complet** avec **communication série 3-phases** configurable par JSON pour démonstration client concrete avec hardware réel.

**CLIENT VALUE :**
- 📦 **Installation One-Click** : MSI package professionnel
- ⚙️ **Configuration JSON** : BIB_ID, DUT_ID, paramètres série
- 🔄 **Communication 3-Phases** : PowerOn → Test → PowerOff  
- 📋 **Question/Réponse** : Échanges série avec logs structurés
- 🎬 **Demo Hardware** : Validation avec équipement réel

**FOUNDATION DISPONIBLE (Sprint 3 ✅) :**
- ✅ **Service Windows** + Enhanced Discovery + Pool Management thread-safe
- ✅ **Device Grouping** + Multi-port awareness + FTDI intelligence
- ✅ **Background Services** + Dependency injection complète
- ✅ **NLog Infrastructure** + 65+ tests + Enterprise architecture

---

## 📋 **Scope Sprint 4 - MVP Focused**

### **✅ CORE DELIVERABLES**
- 📦 **MSI Installer Package** : Installation/désinstallation professionnelle
- ⚙️ **JSON Configuration System** : Paramètres BIB/DUT/série par device
- 🔄 **Serial Communication Engine** : 3-phases workflow automation
- 📋 **Command/Response Framework** : Question/réponse avec timeouts
- 📊 **Communication Logging** : Logs structurés des échanges série
- 🎬 **Complete MVP Demo** : Validation end-to-end avec hardware

### **🎯 CLIENT REQUIREMENTS ADDRESSED**
- ✅ *"Package d'installation et désinstallation"*
- ✅ *"Configuration JSON avec BIB_ID, DUT_ID, paramètres port"*
- ✅ *"Échanges 3-phases : PowerOn → Test → PowerOff"*
- ✅ *"Question/réponse sur port série avec logs"*

---

## 📅 **Sprint 4 Planning - 4 Semaines MVP**

### **🔹 SEMAINE 1: Installation Package + Configuration Foundation (5 jours)**

#### **Jour 1-2: MSI Installer Package**
```powershell
# SerialPortPoolService/installer/SerialPortPool-Setup.wxs (WiX)
# Professional MSI installer with:
# - Service installation/removal
# - Configuration file deployment  
# - Start menu shortcuts
# - Uninstall capability
# - Version management
```

**Deliverables:**
- MSI installer (`SerialPortPool-Setup.msi`)
- Installation scripts améliorés
- Uninstaller integration
- Version management

#### **Jour 3-4: JSON Configuration System**
```csharp
// SerialPortPool.Core/Models/BibConfiguration.cs
public class BibConfiguration
{
    public string BibId { get; set; } = "";
    public string DutId { get; set; } = "";
    public SerialPortSettings PortSettings { get; set; } = new();
    public List<CommandSequence> PowerOnCommands { get; set; } = new();
    public List<CommandSequence> TestCommands { get; set; } = new();
    public List<CommandSequence> PowerOffCommands { get; set; } = new();
}

// SerialPortPool.Core/Models/SerialPortSettings.cs
public class SerialPortSettings
{
    public int BaudRate { get; set; } = 115200;
    public Parity Parity { get; set; } = Parity.None;
    public int DataBits { get; set; } = 8;
    public StopBits StopBits { get; set; } = StopBits.One;
    public int ReadTimeout { get; set; } = 2000;
    public int WriteTimeout { get; set; } = 2000;
}

// SerialPortPool.Core/Models/CommandSequence.cs
public class CommandSequence
{
    public string Command { get; set; } = "";
    public string? ExpectedResponse { get; set; }
    public int TimeoutMs { get; set; } = 2000;
    public int RetryCount { get; set; } = 3;
    public string Description { get; set; } = "";
}
```

#### **Jour 5: Configuration Loader Service**
```csharp
// SerialPortPool.Core/Services/BibConfigurationLoader.cs
public class BibConfigurationLoader
{
    public Dictionary<string, BibConfiguration> LoadConfigurations(string configPath)
    public BibConfiguration? GetConfigurationForBib(string bibId)
    public bool ValidateConfiguration(BibConfiguration config)
}
```

**Deliverable Semaine 1 :** ✅ Installation + Configuration Foundation

---

### **🔹 SEMAINE 2: Serial Communication Engine (5 jours)**

#### **Jour 1-2: Communication Service Foundation**
```csharp
// SerialPortPool.Core/Services/SerialCommunicationService.cs
public class SerialCommunicationService
{
    private readonly ILogger<SerialCommunicationService> _logger;
    private readonly BibConfigurationLoader _configLoader;
    
    /// <summary>
    /// Execute 3-phase communication workflow
    /// </summary>
    public async Task<WorkflowResult> ExecuteCompleteWorkflowAsync(
        string portName, 
        string bibId,
        CancellationToken cancellationToken = default)
    {
        var config = _configLoader.GetConfigurationForBib(bibId);
        var result = new WorkflowResult { BibId = bibId, PortName = portName };
        
        // Phase 1: PowerOn
        result.PowerOnResult = await ExecutePowerOnSequenceAsync(portName, config, cancellationToken);
        
        // Phase 2: Test (only if PowerOn successful)
        if (result.PowerOnResult.Success)
        {
            result.TestResult = await ExecuteTestSequenceAsync(portName, config, cancellationToken);
        }
        
        // Phase 3: PowerOff (always execute)
        result.PowerOffResult = await ExecutePowerOffSequenceAsync(portName, config, cancellationToken);
        
        return result;
    }
}
```

#### **Jour 3-4: Command/Response Framework**
```csharp
// SerialPortPool.Core/Services/SerialCommandExecutor.cs
public class SerialCommandExecutor
{
    /// <summary>
    /// Send command and wait for response with timeout
    /// </summary>
    public async Task<CommandResult> ExecuteCommandAsync(
        SerialPort serialPort,
        CommandSequence command,
        CancellationToken cancellationToken = default)
    {
        var result = new CommandResult 
        { 
            Command = command.Command,
            StartTime = DateTime.Now 
        };
        
        try
        {
            // Log outgoing command
            _logger.LogInformation("📤 Sending: {Command}", command.Command.Trim());
            
            // Send command
            await serialPort.WriteAsync(Encoding.UTF8.GetBytes(command.Command), cancellationToken);
            
            // Wait for response with timeout
            var response = await ReadResponseAsync(serialPort, command.TimeoutMs, cancellationToken);
            
            // Log incoming response
            _logger.LogInformation("📥 Received: {Response}", response?.Trim() ?? "NO_RESPONSE");
            
            // Validate response
            result.Response = response;
            result.Success = ValidateResponse(response, command.ExpectedResponse);
            result.EndTime = DateTime.Now;
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Command failed: {Command}", command.Command);
            result.Success = false;
            result.ErrorMessage = ex.Message;
            result.EndTime = DateTime.Now;
            return result;
        }
    }
}
```

#### **Jour 5: Integration Testing**
```csharp
// tests/SerialPortPool.Core.Tests/Services/SerialCommunicationTests.cs
// 8 tests pour communication engine
[Fact] ExecuteCompleteWorkflow_WithValidConfig_CompletesAllPhases()
[Fact] ExecuteCommandAsync_WithResponse_ReturnsSuccess()
[Fact] ExecuteCommandAsync_WithTimeout_ReturnsFailure()
[Fact] ExecutePowerOnSequence_WithMultipleCommands_ExecutesInOrder()
```

**Deliverable Semaine 2 :** ✅ Serial Communication Engine

---

### **🔹 SEMAINE 3: 3-Phase Workflow + Logging (5 jours)**

#### **Jour 1-2: Workflow Orchestration**
```csharp
// SerialPortPool.Core/Services/IndustrialWorkflowOrchestrator.cs
public class IndustrialWorkflowOrchestrator
{
    private readonly ISerialPortPool _pool;
    private readonly SerialCommunicationService _communicationService;
    
    /// <summary>
    /// Execute complete industrial workflow with pool management
    /// </summary>
    public async Task<IndustrialWorkflowResult> ExecuteIndustrialWorkflowAsync(
        string bibId,
        string clientId = "IndustrialWorkflow",
        CancellationToken cancellationToken = default)
    {
        var workflowResult = new IndustrialWorkflowResult { BibId = bibId };
        
        try
        {
            // Step 1: Allocate port from pool
            var allocation = await _pool.AllocatePortAsync(clientId: clientId);
            if (allocation == null)
            {
                workflowResult.Success = false;
                workflowResult.ErrorMessage = $"No available ports for BIB {bibId}";
                return workflowResult;
            }
            
            workflowResult.AllocatedPort = allocation.PortName;
            
            // Step 2: Execute communication workflow
            var commResult = await _communicationService.ExecuteCompleteWorkflowAsync(
                allocation.PortName, bibId, cancellationToken);
                
            workflowResult.CommunicationResult = commResult;
            workflowResult.Success = commResult.Success;
            
            // Step 3: Release port
            await _pool.ReleasePortAsync(allocation.PortName, allocation.SessionId);
            
            return workflowResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in industrial workflow for BIB {BibId}", bibId);
            workflowResult.Success = false;
            workflowResult.ErrorMessage = ex.Message;
            return workflowResult;
        }
    }
}
```

#### **Jour 3-4: Enhanced Communication Logging**
```csharp
// Enhanced NLog configuration for communication logs
// SerialPortPoolService/NLog.config (updated)
<target xsi:type="File"
        name="communicationTarget"
        fileName="C:\Logs\SerialPortPool\communication-${shortdate}.log"
        layout="${longdate} [${level}] ${logger} | ${message} ${exception}"
        archiveEvery="Day" />

// Structured logging for serial communication
// SerialPortPool.Core/Services/CommunicationLogger.cs
public class CommunicationLogger
{
    public void LogCommandSent(string portName, string bibId, string command)
    public void LogResponseReceived(string portName, string bibId, string response, double durationMs)
    public void LogWorkflowStarted(string bibId, string portName)
    public void LogWorkflowCompleted(string bibId, bool success, double totalDurationMs)
}
```

#### **Jour 5: Default Configuration Setup**
```json
// SerialPortPoolService/Configuration/bib-configurations.json
{
  "Configurations": {
    "BIB_001": {
      "BibId": "BIB_001",
      "DutId": "DUT_05",
      "Description": "Standard Test Configuration for BIB 001",
      "PortSettings": {
        "BaudRate": 115200,
        "Parity": "None",
        "DataBits": 8,
        "StopBits": "One",
        "ReadTimeout": 2000,
        "WriteTimeout": 2000
      },
      "PowerOnCommands": [
        {
          "Command": "ATZ\r\n",
          "ExpectedResponse": "OK",
          "TimeoutMs": 3000,
          "RetryCount": 3,
          "Description": "Reset device"
        },
        {
          "Command": "AT+INIT\r\n", 
          "ExpectedResponse": "READY",
          "TimeoutMs": 5000,
          "RetryCount": 2,
          "Description": "Initialize device"
        }
      ],
      "TestCommands": [
        {
          "Command": "AT+STATUS\r\n",
          "ExpectedResponse": "STATUS_OK",
          "TimeoutMs": 2000,
          "RetryCount": 3,
          "Description": "Check device status"
        },
        {
          "Command": "AT+DATA?\r\n",
          "ExpectedResponse": "DATA:",
          "TimeoutMs": 3000,
          "RetryCount": 2,
          "Description": "Request test data"
        }
      ],
      "PowerOffCommands": [
        {
          "Command": "AT+SHUTDOWN\r\n",
          "ExpectedResponse": "SHUTDOWN_OK",
          "TimeoutMs": 5000,
          "RetryCount": 1,
          "Description": "Graceful shutdown"
        }
      ]
    },
    "BIB_002": {
      "BibId": "BIB_002",
      "DutId": "DUT_03",
      "Description": "Alternative Configuration for BIB 002",
      "PortSettings": {
        "BaudRate": 57600,
        "Parity": "Even",
        "DataBits": 7,
        "StopBits": "One"
      },
      "PowerOnCommands": [
        {
          "Command": "INIT\r\n",
          "ExpectedResponse": "ACK",
          "TimeoutMs": 2000,
          "Description": "Simple initialization"
        }
      ],
      "TestCommands": [
        {
          "Command": "TEST\r\n",
          "ExpectedResponse": "PASS",
          "TimeoutMs": 1000,
          "Description": "Basic test command"
        }
      ],
      "PowerOffCommands": [
        {
          "Command": "EXIT\r\n",
          "ExpectedResponse": "BYE",
          "TimeoutMs": 1000,
          "Description": "Exit command"
        }
      ]
    }
  },
  "DefaultSettings": {
    "BaudRate": 115200,
    "Parity": "None",
    "DataBits": 8,
    "StopBits": "One",
    "ReadTimeout": 2000,
    "WriteTimeout": 2000
  }
}
```

**Deliverable Semaine 3 :** ✅ Complete Workflow + Enhanced Logging

---

### **🔹 SEMAINE 4: Integration + MVP Demo (5 jours)**

#### **Jour 1-2: Service Integration Complete**
```csharp
// SerialPortPoolService/Program.cs - Enhanced DI for MVP
services.AddSingleton<BibConfigurationLoader>();
services.AddScoped<SerialCommunicationService>();
services.AddScoped<SerialCommandExecutor>();
services.AddScoped<IndustrialWorkflowOrchestrator>();
services.AddScoped<CommunicationLogger>();

// Load BIB configurations
var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Configuration", "bib-configurations.json");
var bibConfigurations = BibConfigurationLoader.LoadFromFile(configPath);
services.AddSingleton(bibConfigurations);
```

#### **Jour 3: MVP Demo Application**
```csharp
// tests/MVPDemo/IndustrialMVPDemo.cs
class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("🏭 SerialPortPool MVP Demo - Industrial Communication");
        Console.WriteLine("=====================================================");
        
        // Setup services (same DI as main service)
        var serviceProvider = SetupMVPServices();
        
        try
        {
            await RunMVPDemo(serviceProvider);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Demo failed: {ex.Message}");
        }
        
        Console.WriteLine("\nPress any key to exit...");
        Console.ReadKey();
    }
    
    static async Task RunMVPDemo(IServiceProvider services)
    {
        var orchestrator = services.GetRequiredService<IndustrialWorkflowOrchestrator>();
        
        Console.WriteLine("📋 Available BIB Configurations:");
        Console.WriteLine("  - BIB_001 (Standard Test Configuration)");
        Console.WriteLine("  - BIB_002 (Alternative Configuration)");
        Console.WriteLine();
        
        // Demo with BIB_001
        Console.WriteLine("🚀 Starting workflow for BIB_001...");
        var result = await orchestrator.ExecuteIndustrialWorkflowAsync("BIB_001", "MVPDemo");
        
        DisplayWorkflowResult(result);
        
        Console.WriteLine("\n" + "=".PadRight(50, '='));
        Console.WriteLine("✅ MVP Demo completed successfully!");
        Console.WriteLine("📋 Check logs at: C:\\Logs\\SerialPortPool\\");
    }
    
    static void DisplayWorkflowResult(IndustrialWorkflowResult result)
    {
        Console.WriteLine($"\n📊 Workflow Result for {result.BibId}:");
        Console.WriteLine($"   Port Used: {result.AllocatedPort}");
        Console.WriteLine($"   Success: {(result.Success ? "✅ YES" : "❌ NO")}");
        
        if (result.CommunicationResult != null)
        {
            var comm = result.CommunicationResult;
            Console.WriteLine($"   PowerOn: {(comm.PowerOnResult?.Success == true ? "✅" : "❌")} ({comm.PowerOnResult?.CommandResults?.Count ?? 0} commands)");
            Console.WriteLine($"   Test: {(comm.TestResult?.Success == true ? "✅" : "❌")} ({comm.TestResult?.CommandResults?.Count ?? 0} commands)");
            Console.WriteLine($"   PowerOff: {(comm.PowerOffResult?.Success == true ? "✅" : "❌")} ({comm.PowerOffResult?.CommandResults?.Count ?? 0} commands)");
            Console.WriteLine($"   Total Duration: {comm.TotalDuration.TotalSeconds:F1}s");
        }
        
        if (!result.Success)
        {
            Console.WriteLine($"   Error: {result.ErrorMessage}");
        }
    }
}
```

#### **Jour 4: Documentation & Packaging**
```markdown
# MVP-README.md
## SerialPortPool MVP - Industrial Communication

### Installation
1. Run `SerialPortPool-Setup.msi` as Administrator
2. Service starts automatically
3. Configure BIBs in `C:\Program Files\SerialPortPool\Configuration\bib-configurations.json`

### Usage
1. Connect FTDI device (FT232R/FT4232H)
2. Update configuration with your BIB_ID and commands
3. Run demo: `MVPDemo.exe`
4. Check logs: `C:\Logs\SerialPortPool\`

### Configuration Example
See bib-configurations.json for BIB_001 and BIB_002 examples.
```

#### **Jour 5: Final Testing + Package**
```bash
# Final validation checklist
✅ MSI installs/uninstalls correctly
✅ Service starts with MVP functionality
✅ Configuration loads from JSON
✅ 3-phase workflow executes
✅ Communication logs correctly
✅ MVP demo runs end-to-end
✅ Hardware validation with real device
✅ Documentation complete
```

**Deliverable Semaine 4 :** ✅ Complete MVP Package Ready for Client

---

## 🏗️ **Architecture MVP Sprint 4**

```
SerialPortPoolService/                          ← Enhanced Windows Service
├── installer/
│   ├── SerialPortPool-Setup.wxs              ← NEW: WiX installer
│   └── build-installer.bat                    ← NEW: Build script
├── Configuration/
│   ├── bib-configurations.json               ← NEW: BIB configurations
│   └── IndustrialSettings.cs                 ← NEW: Settings model
├── Services/
│   └── (existing background services)         ← Sprint 3 foundation
└── Program.cs                                 ← Enhanced DI for MVP

SerialPortPool.Core/                           ← Enhanced Core Library
├── Models/
│   ├── BibConfiguration.cs                   ← NEW: Configuration model
│   ├── SerialPortSettings.cs                 ← NEW: Port settings
│   ├── CommandSequence.cs                    ← NEW: Command model
│   ├── WorkflowResult.cs                     ← NEW: Result models
│   └── (existing Sprint 3 models)            ← Foundation preserved
├── Services/
│   ├── BibConfigurationLoader.cs             ← NEW: Config loader
│   ├── SerialCommunicationService.cs         ← NEW: Communication engine
│   ├── SerialCommandExecutor.cs              ← NEW: Command executor
│   ├── IndustrialWorkflowOrchestrator.cs     ← NEW: Workflow orchestrator
│   ├── CommunicationLogger.cs                ← NEW: Enhanced logging
│   └── (existing Sprint 3 services)          ← Foundation preserved
└── Interfaces/
    ├── (new communication interfaces)         ← NEW: Communication contracts
    └── (existing Sprint 3 interfaces)        ← Foundation preserved

tests/MVPDemo/                                 ← NEW: MVP Demo
├── IndustrialMVPDemo.cs                      ← NEW: Demo application
├── MVPDemo.csproj                            ← NEW: Demo project
└── README-MVP.md                             ← NEW: Demo documentation
```

---

## 🧪 **Testing Strategy Sprint 4**

### **Test Coverage MVP :**
- **Configuration Loading :** 6 tests (JSON parsing, validation)
- **Serial Communication :** 8 tests (command/response, timeouts)
- **3-Phase Workflow :** 6 tests (PowerOn/Test/PowerOff sequences)
- **Integration MVP :** 4 tests (end-to-end scenarios)
- **Installation Package :** Manual validation checklist
- **Total :** 24+ nouveaux tests + MVP validation

### **Hardware Testing :**
- ✅ **Real FTDI device** validation (COM6 FT232R)
- ✅ **3-phase communication** avec device simulé
- ✅ **Configuration JSON** avec multiple BIBs
- ✅ **Error scenarios** et recovery
- ✅ **Installation process** sur machine propre

---

## 🎯 **Success Criteria Sprint 4**

### **Must Have MVP :**
- ✅ MSI installer package fonctionnel (install/uninstall)
- ✅ JSON configuration système avec BIB_ID/DUT_ID/paramètres
- ✅ Communication série 3-phases (PowerOn → Test → PowerOff)
- ✅ Question/réponse framework avec timeouts et retries
- ✅ Logs structurés des échanges série avec timestamps
- ✅ MVP demo fonctionnel avec hardware réel
- ✅ 24+ nouveaux tests passing + validation manuelle
- ✅ Zero regression sur Sprint 3 foundation

### **Nice to Have :**
- 🎯 Advanced error recovery et reconnection
- 🎯 Real-time communication monitoring
- 🎯 Configuration validation avancée
- 🎯 Performance metrics des communications

---

## 📦 **Deliverables Package Sprint 4**

### **Client Package :**
```
SerialPortPool-MVP-v1.0/
├── SerialPortPool-Setup.msi              ← Installation package
├── MVPDemo.exe                           ← Demo application  
├── Configuration/
│   └── bib-configurations.json          ← Example configurations
├── Documentation/
│   ├── MVP-Installation-Guide.pdf       ← Installation guide
│   ├── MVP-Configuration-Guide.pdf      ← Configuration guide
│   └── MVP-Demo-Guide.pdf               ← Demo usage guide
└── README-MVP.txt                       ← Quick start guide
```

### **Technical Deliverables :**
- **Enhanced Windows Service** avec MVP functionality
- **Complete source code** avec 90+ tests (Sprint 3: 65+ + Sprint 4: 24+)
- **Installation scripts** professionnels
- **Configuration system** extensible
- **Communication framework** industrial-ready
- **Documentation complète** pour utilisateurs et développeurs

---

## 🚀 **Ready for Sprint 4 MVP !**

**Foundation Sprint 3 Exceptionnelle ✅ + Client MVP Focus = Success Garanti !**

### **Next Action Sprint 4 :**
1. **Create installer project** (WiX setup)
2. **Design BibConfiguration models** 
3. **Implement JSON configuration loader**
4. **Build serial communication service**
5. **Validate MVP with hardware**

### **Timeline Confidence :**
- **Week 1-2 :** Foundation + Communication engine
- **Week 3-4 :** Integration + MVP demo + package
- **Total :** 4 semaines pour MVP client-ready

---

*Document créé : 23 Juillet 2025*  
*Sprint 4 Status : 🚀 MVP READY TO START*  
*Focus : Client MVP Demo - Installation + Configuration + Communication 3-Phases*  
*Foundation : Sprint 3 Excellence (65+ tests, thread-safe pool, device grouping)*  
*Target : 4 semaines pour package MVP complet avec demo hardware*