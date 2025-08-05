# Sprint 6 - Complete Sprint 5 Core Services

![Sprint](https://img.shields.io/badge/Sprint%206-🔧%20CORE%20SERVICES-brightgreen.svg)
![Duration](https://img.shields.io/badge/Duration-2%20weeks-blue.svg)
![Focus](https://img.shields.io/badge/Focus-PRODUCTION%20FOUNDATION-purple.svg)
![Scope](https://img.shields.io/badge/Scope-FOCUSED%20COMPLETION-gold.svg)

## 🎯 **Sprint 6 Mission - Complete Sprint 5 Foundation**

**OBJECTIF FOCUSED :** Compléter uniquement les 4 services core manquants de Sprint 5 pour transformer le demo spectaculaire en implémentation production avec **vraie communication série et XML configuration**.

### **Sprint 6 Focused Scope - ONLY**
```csharp
// Ces 4 lignes de code doivent fonctionner en production
var bibConfig = await configLoader.LoadBibAsync(xmlPath, bibId);        // ← Sprint 6
var protocolHandler = factory.CreateHandler("rs232");                   // ← Sprint 6  
var session = await protocolHandler.OpenSessionAsync(portName);         // ← Sprint 6
var result = await protocolHandler.ExecuteCommandAsync(command);        // ← Sprint 6
```

### **EVERYTHING ELSE → Sprint 7**
- ❌ Multi-Protocol Expansion (RS485, USB, CAN, I2C, SPI) → **Sprint 7**
- ❌ Advanced Error Handling → **Sprint 7**
- ❌ Performance Optimization → **Sprint 7**
- ❌ Production Hardening → **Sprint 7**

---

## 📋 **Sprint 5 Gap Analysis - ONLY These 4 Services**

### **✅ Sprint 5 Delivered (Preserved)**
```
✅ RS232Demo Application        - Spectacular console demo
✅ Python Dummy UUT            - Complete RS232 simulator  
✅ ZERO TOUCH Architecture     - Extension layer validated
✅ Foundation Preserved        - 65+ existing tests intact
✅ Hardware Detection          - FTDI device discovery working
```

### **❌ Sprint 5 Missing (Sprint 6 ONLY Focus)**
```
❌ IXmlConfigurationLoader     - configLoader.LoadBibAsync()
❌ IProtocolHandlerFactory     - factory.CreateHandler("rs232")  
❌ IProtocolHandler           - protocolHandler.OpenSessionAsync()
❌ Command Execution          - protocolHandler.ExecuteCommandAsync()
```

---

## 🗓️ **Sprint 6 Planning - 2 Weeks ONLY**

### **🔹 WEEK 1: XML Configuration + Protocol Foundation**

#### **Day 1-2: XML Configuration Loader**
```csharp
// ONLY implement XML configuration loading
SerialPortPool.Core/Services/
├── XmlConfigurationLoader.cs           ← Parse BIB→UUT→PORT from XML
├── ConfigurationValidator.cs           ← Basic XML validation
└── Models/
    ├── BibConfiguration.cs             ← BIB definition
    ├── UutConfiguration.cs             ← UUT definition
    ├── PortConfiguration.cs            ← Port + RS232 settings ONLY
    └── CommandSequence.cs              ← 3-phase commands

// Interface - MINIMAL
public interface IXmlConfigurationLoader
{
    Task<BibConfiguration> LoadBibAsync(string xmlPath, string bibId);
}

// Implementation - MINIMAL but FUNCTIONAL
public class XmlConfigurationLoader : IXmlConfigurationLoader
{
    public async Task<BibConfiguration> LoadBibAsync(string xmlPath, string bibId)
    {
        var xmlDoc = new XmlDocument();
        xmlDoc.Load(xmlPath);
        
        var bibNode = xmlDoc.SelectSingleNode($"//bib[@id='{bibId}']");
        if (bibNode == null)
            throw new ArgumentException($"BIB {bibId} not found in {xmlPath}");
        
        return ParseBibConfiguration(bibNode);
    }
    
    private BibConfiguration ParseBibConfiguration(XmlNode bibNode)
    {
        // Parse XML → BibConfiguration object
        // ONLY support RS232 protocol for Sprint 6
    }
}
```

#### **Day 3-4: Protocol Handler Foundation**
```csharp
// ONLY implement RS232 protocol handler
SerialPortPool.Core/Protocols/
├── IProtocolHandler.cs                 ← Basic protocol interface
├── ProtocolHandlerFactory.cs           ← Factory for RS232 ONLY
├── RS232ProtocolHandler.cs             ← REAL RS232 implementation
└── Models/
    ├── ProtocolSession.cs              ← Session management
    ├── ProtocolCommand.cs              ← Command structure
    └── ProtocolResponse.cs             ← Response structure

// Factory - RS232 ONLY
public interface IProtocolHandlerFactory
{
    IProtocolHandler CreateHandler(string protocolName);
}

public class ProtocolHandlerFactory : IProtocolHandlerFactory
{
    private readonly IServiceProvider _serviceProvider;
    
    public IProtocolHandler CreateHandler(string protocolName)
    {
        return protocolName.ToLower() switch
        {
            "rs232" => _serviceProvider.GetRequiredService<RS232ProtocolHandler>(),
            _ => throw new ArgumentException($"Protocol {protocolName} not supported in Sprint 6. Available: RS232 only.")
        };
    }
}
```

#### **Day 5: Integration Testing**
```csharp
// tests/SerialPortPool.Core.Tests/Sprint6/
├── XmlConfigurationLoaderTests.cs      ← XML parsing tests
├── ProtocolHandlerFactoryTests.cs      ← Factory tests (RS232 only)
└── RS232ProtocolHandlerTests.cs        ← RS232 communication tests
```

### **🔹 WEEK 2: Real RS232 Implementation + Demo Integration**

#### **Day 1-3: Production RS232 Protocol Handler**
```csharp
// SerialPortPool.Core/Protocols/RS232ProtocolHandler.cs
public class RS232ProtocolHandler : IProtocolHandler
{
    private SerialPort? _serialPort;
    private readonly ILogger<RS232ProtocolHandler> _logger;
    
    public async Task<ProtocolSession> OpenSessionAsync(string portName, ProtocolConfiguration config)
    {
        try
        {
            _serialPort = new SerialPort(portName)
            {
                BaudRate = config.BaudRate,
                Parity = config.Parity,
                DataBits = config.DataBits,
                StopBits = config.StopBits,
                ReadTimeout = config.ReadTimeoutMs,
                WriteTimeout = config.WriteTimeoutMs
            };
            
            await Task.Run(() => _serialPort.Open());
            
            _logger.LogInformation("📡 RS232 session opened: {PortName} @ {BaudRate} baud", 
                portName, config.BaudRate);
            
            return new ProtocolSession
            {
                SessionId = Guid.NewGuid().ToString(),
                PortName = portName,
                IsActive = true,
                OpenedAt = DateTime.Now
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to open RS232 session on {PortName}", portName);
            throw;
        }
    }
    
    public async Task<ProtocolResponse> ExecuteCommandAsync(ProtocolCommand command)
    {
        if (_serialPort == null || !_serialPort.IsOpen)
            throw new InvalidOperationException("Session not open");
        
        var response = new ProtocolResponse
        {
            Command = command.Command,
            StartTime = DateTime.Now
        };
        
        try
        {
            _logger.LogDebug("📤 RS232 TX: {Command}", command.Command.Trim());
            
            // Send command
            var commandBytes = Encoding.UTF8.GetBytes(command.Command);
            await _serialPort.BaseStream.WriteAsync(commandBytes, 0, commandBytes.Length);
            
            // Read response with timeout
            var responseText = await ReadResponseAsync(command.TimeoutMs);
            
            _logger.LogDebug("📥 RS232 RX: {Response}", responseText?.Trim() ?? "NO_RESPONSE");
            
            response.Response = responseText;
            response.Success = ValidateResponse(responseText, command.ExpectedResponse);
            response.EndTime = DateTime.Now;
            
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ RS232 command failed: {Command}", command.Command);
            response.Success = false;
            response.ErrorMessage = ex.Message;
            response.EndTime = DateTime.Now;
            return response;
        }
    }
    
    private async Task<string?> ReadResponseAsync(int timeoutMs)
    {
        var buffer = new byte[1024];
        var responseBuilder = new StringBuilder();
        var cancellationToken = new CancellationTokenSource(timeoutMs).Token;
        
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                if (_serialPort!.BytesToRead > 0)
                {
                    var bytesRead = await _serialPort.BaseStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
                    var chunk = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    responseBuilder.Append(chunk);
                    
                    // Check for line ending
                    if (chunk.Contains('\n') || chunk.Contains('\r'))
                        break;
                }
                
                await Task.Delay(10, cancellationToken);
            }
            
            return responseBuilder.ToString().Trim();
        }
        catch (OperationCanceledException)
        {
            return null; // Timeout
        }
    }
    
    private bool ValidateResponse(string? actual, string? expected)
    {
        if (string.IsNullOrEmpty(expected))
            return true; // No validation required
        
        if (string.IsNullOrEmpty(actual))
            return false;
        
        // Support regex patterns (^OK$) or simple string matching
        if (expected.StartsWith('^') && expected.EndsWith('$'))
        {
            var pattern = expected;
            return System.Text.RegularExpressions.Regex.IsMatch(actual, pattern);
        }
        
        return actual.Contains(expected);
    }
    
    public async Task CloseSessionAsync()
    {
        if (_serialPort?.IsOpen == true)
        {
            _serialPort.Close();
            _serialPort.Dispose();
            _serialPort = null;
            
            _logger.LogInformation("🔒 RS232 session closed");
        }
    }
}
```

#### **Day 4-5: Demo Integration - Replace Simulation**
```csharp
// tests/RS232Demo/DemoOrchestrator.cs - UPDATED to use REAL services
public class DemoOrchestrator
{
    private readonly IXmlConfigurationLoader _configLoader;      // ← REAL XML service
    private readonly IProtocolHandlerFactory _protocolFactory;  // ← REAL protocol factory
    private readonly ILogger<DemoOrchestrator> _logger;
    
    public async Task<DemoResult> RunPythonSimulatorDemo()
    {
        Console.WriteLine("🤖 Demo Scenario: REAL Workflow with Python UUT");
        Console.WriteLine("📋 Using: REAL XML configuration parsing");
        Console.WriteLine("📡 Using: REAL RS232 serial communication");
        Console.WriteLine();
        
        try
        {
            // 1. Load REAL configuration from XML (no more hardcoded)
            var bibConfig = await _configLoader.LoadBibAsync("Configuration/demo-config.xml", "bib_demo");
            var uutConfig = bibConfig.Uuts.First(u => u.UutId == "uut_python_simulator");
            var portConfig = uutConfig.Ports.First(p => p.PortNumber == 1);
            
            Console.WriteLine($"📋 Loaded BIB configuration: {bibConfig.BibId}");
            Console.WriteLine($"📋 Target UUT: {uutConfig.UutId}");
            Console.WriteLine($"📋 Port: {portConfig.PortNumber} (Protocol: {portConfig.Protocol})");
            Console.WriteLine();
            
            // 2. Create REAL protocol handler (no more simulation)
            var protocolHandler = _protocolFactory.CreateHandler(portConfig.Protocol);
            
            // 3. Execute REAL 3-phase workflow
            var workflowResult = await ExecuteRealWorkflow(protocolHandler, portConfig);
            
            DisplayRealWorkflowResult(workflowResult);
            return DemoResult.FromRealResult(workflowResult);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Demo failed: {ex.Message}");
            return DemoResult.Failed(ex.Message);
        }
    }
    
    private async Task<WorkflowResult> ExecuteRealWorkflow(
        IProtocolHandler protocolHandler, 
        PortConfiguration portConfig)
    {
        var result = new WorkflowResult
        {
            WorkflowId = Guid.NewGuid().ToString(),
            StartTime = DateTime.Now
        };
        
        ProtocolSession? session = null;
        
        try
        {
            // Open REAL session (no more Task.Delay simulation)
            Console.WriteLine("🔗 Opening RS232 session...");
            session = await protocolHandler.OpenSessionAsync("COM8", CreateProtocolConfig(portConfig));
            Console.WriteLine($"✅ Session opened: {session.SessionId}");
            
            // Execute REAL 3-phase workflow
            result.PowerOnResult = await ExecutePhase("PowerOn", protocolHandler, portConfig.StartCommands);
            result.TestResult = await ExecutePhase("Test", protocolHandler, portConfig.TestCommands);
            result.PowerOffResult = await ExecutePhase("PowerOff", protocolHandler, portConfig.StopCommands);
            
            result.Success = result.PowerOnResult.Success && 
                           result.TestResult.Success && 
                           result.PowerOffResult.Success;
            result.EndTime = DateTime.Now;
            
            return result;
        }
        finally
        {
            if (session != null)
            {
                await protocolHandler.CloseSessionAsync();
                Console.WriteLine("🔒 Session closed");
            }
        }
    }
    
    private async Task<PhaseResult> ExecutePhase(
        string phaseName, 
        IProtocolHandler protocolHandler, 
        CommandSequence commands)
    {
        Console.WriteLine($"⚡ Executing {phaseName} phase...");
        
        var phaseResult = new PhaseResult { PhaseName = phaseName };
        
        foreach (var cmd in commands.Commands)
        {
            Console.WriteLine($"   📤 Sending: {cmd.Command.Trim()}");
            
            // REAL command execution (no more Task.Delay)
            var response = await protocolHandler.ExecuteCommandAsync(new ProtocolCommand
            {
                Command = cmd.Command,
                ExpectedResponse = cmd.ExpectedResponse,
                TimeoutMs = cmd.TimeoutMs
            });
            
            Console.WriteLine($"   📥 Received: {response.Response?.Trim() ?? "NO_RESPONSE"}");
            Console.WriteLine($"   {(response.Success ? "✅" : "❌")} Result: {(response.Success ? "SUCCESS" : "FAILED")}");
            
            phaseResult.CommandResults.Add(response);
        }
        
        phaseResult.Success = phaseResult.CommandResults.All(r => r.Success);
        Console.WriteLine($"📊 {phaseName} phase: {(phaseResult.Success ? "✅ SUCCESS" : "❌ FAILED")}");
        Console.WriteLine();
        
        return phaseResult;
    }
}
```

---

## 🎯 **Sprint 6 Success Criteria - MINIMAL & FOCUSED**

### **✅ Core Services Implementation**
- ✅ **IXmlConfigurationLoader** - `configLoader.LoadBibAsync()` works
- ✅ **IProtocolHandlerFactory** - `factory.CreateHandler("rs232")` works
- ✅ **IProtocolHandler** - `protocolHandler.OpenSessionAsync()` works  
- ✅ **Command Execution** - `protocolHandler.ExecuteCommandAsync()` works

### **✅ Demo Integration**
- ✅ **Real XML Configuration** - Demo reads from `demo-config.xml` instead of hardcoded
- ✅ **Real RS232 Communication** - Demo communicates with Python Dummy UUT via SerialPort
- ✅ **Real 3-Phase Workflow** - PowerOn → Test → PowerOff with actual commands/responses
- ✅ **Zero Regression** - All existing 65+ tests continue passing

### **✅ Production Readiness**
- ✅ **Service Integration** - All new services properly registered in DI container
- ✅ **Error Handling** - Basic error handling for communication failures
- ✅ **Logging** - Structured logging for all protocol operations
- ✅ **Testing** - Unit tests for all new services

---

## 📦 **Sprint 6 Deliverables - MINIMAL**

### **Enhanced Demo Package**
```
SerialPortPool-Sprint6-Production/
├── RS232Demo.exe                       ← REAL services (no more simulation)
├── Configuration/
│   ├── demo-config.xml                 ← REAL XML configuration used
│   └── bib-schema.xsd                  ← XML validation schema
├── SerialPortPoolService.exe           ← Enhanced Windows Service
├── dummy_uut.py                        ← Python UUT simulator
└── README-Sprint6.md                   ← Updated documentation
```

### **Core Services Implementation**
```csharp
// Sprint 6 - REAL services implementation
SerialPortPool.Core/
├── Services/
│   └── XmlConfigurationLoader.cs       ← REAL XML parsing
├── Protocols/
│   ├── IProtocolHandler.cs             ← Protocol interface
│   ├── ProtocolHandlerFactory.cs       ← RS232 factory
│   └── RS232ProtocolHandler.cs         ← REAL RS232 implementation
└── Models/
    ├── BibConfiguration.cs             ← Configuration models
    ├── ProtocolSession.cs              ← Session management
    └── ProtocolCommand.cs              ← Command/response models
```

---

## 🚫 **Sprint 7 Scope - DEFERRED**

### **❌ Multi-Protocol Support → Sprint 7**
```csharp
// These will be implemented in Sprint 7
❌ RS485ProtocolHandler
❌ USBProtocolHandler  
❌ CANProtocolHandler
❌ I2CProtocolHandler
❌ SPIProtocolHandler
```

### **❌ Advanced Features → Sprint 7**
```csharp
// These will be implemented in Sprint 7
❌ Advanced Error Recovery
❌ Connection Pooling
❌ Performance Optimization
❌ Advanced Configuration Validation
❌ Multi-BIB Support
❌ Production Monitoring
```

---

## 🚀 **Ready for Focused Sprint 6!**

**Sprint 6 = Complete ONLY the 4 missing core services to make the spectacular demo work with REAL implementation** 🔥

### **Success Formula:**
1. **Week 1** → XML Configuration + Protocol Foundation  
2. **Week 2** → Real RS232 Implementation + Demo Integration

### **Key Advantages:**
- **Focused Scope** - ONLY what's needed to complete Sprint 5
- **Realistic Timeline** - 2 weeks for 4 core services
- **Immediate Value** - Demo works with real XML + real RS232
- **Foundation Ready** - Sprint 7 can add multi-protocol support

### **Expected Result:**
```csharp   
// These 4 lines will work in production after Sprint 6
var bibConfig = await configLoader.LoadBibAsync(xmlPath, bibId);        // ✅ WORKS
var protocolHandler = factory.CreateHandler("rs232");                   // ✅ WORKS
var session = await protocolHandler.OpenSessionAsync(portName);         // ✅ WORKS  
var result = await protocolHandler.ExecuteCommandAsync(command);        // ✅ WORKS
```

---

*Sprint 6 Planning created: July 30, 2025*  
*Duration: 2 weeks (Focused completion)*  
*Scope: ONLY 4 core services to complete Sprint 5*  
*Everything else: Sprint 7*

**🎯 Sprint 6 Mission: Demo Simulation → Production Reality! 🎯**

**Sprint 6 = Complete ONLY the 4 missing core services to make the spectacular demo work with REAL implementation** 🔥

### **Success Formula:**
1. **Week 1** → XML Configuration + Protocol Foundation  
2. **Week 2** → Real RS232 Implementation + Demo Integration

### **Key Advantages:**
- **Focused Scope** - ONLY what's needed to complete Sprint 5
- **Realistic Timeline** - 2 weeks for 4 core services
- **Immediate Value** - Demo works with real XML + real RS232
- **Foundation Ready** - Sprint 7 can add multi-protocol support

### **Expected Result:**
```csharp
// These 4 lines will work in production after Sprint 6
var bibConfig = await configLoader.LoadBibAsync(xmlPath, bibId);        // ✅ WORKS
var protocolHandler = factory.CreateHandler("rs232");                   // ✅ WORKS
var session = await protocolHandler.OpenSessionAsync(portName);         // ✅ WORKS  
var result = await protocolHandler.ExecuteCommandAsync(command);        // ✅ WORKS
```

---

*Sprint 6 Planning created: July 30, 2025*  
*Duration: 2 weeks (Focused completion)*  
*Scope: ONLY 4 core services to complete Sprint 5*  
*Everything else: Sprint 7*

**🎯 Sprint 6 Mission: Demo Simulation → Production Reality! 🎯**