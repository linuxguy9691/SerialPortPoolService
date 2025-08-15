# SerialPortPoolService

[![English](https://img.shields.io/badge/lang-English-blue.svg)](README.md) [![Français](https://img.shields.io/badge/lang-Français-blue.svg)](README.fr.md)

![Build Status](https://github.com/linuxguy9691/SerialPortPoolService/workflows/Automated%20Tests%20-%20Sprint%201%20&%202/badge.svg)
![.NET](https://img.shields.io/badge/.NET-9.0-purple.svg)
![Platform](https://img.shields.io/badge/platform-Windows-blue.svg)
![License](https://img.shields.io/badge/license-MIT-green.svg)
![Sprint](https://img.shields.io/badge/Sprint%208-COMPLETED-brightgreen.svg)
![Architecture](https://img.shields.io/badge/Architecture-INTELLIGENT%20HARDWARE-gold.svg)
![Hardware](https://img.shields.io/badge/Hardware-EEPROM%20DYNAMIC-gold.svg)

A professional Windows service for centralized and secure serial port pool management, featuring **intelligent EEPROM-based BIB selection**, **advanced regex validation**, **FT4232H multi-port detection**, **thread-safe pool management**, **professional MSI deployment**, and now **dynamic hardware intelligence** with automatic configuration discovery.

## 🎯 **Overview**

SerialPortPoolService is an enterprise-grade solution that provides:
- 🔬 **Dynamic EEPROM BIB Selection** - Automatic BIB_ID detection from FTDI ProductDescription
- 🎯 **Advanced Regex Validation** - Sophisticated UUT response pattern matching
- 🔍 **Automatic Port Discovery** - Enhanced WMI + EEPROM intelligence
- 🏭 **Intelligent FTDI Analysis** - Detailed chip analysis with multi-port grouping
- 🎯 **Advanced Validation** - Configurable scoring and XML-driven configuration
- 🏗️ **Professional Windows Service** - Enterprise logging and automated installation
- 🏊 **Thread-Safe Pool Management** - Production-ready allocation/release ✅ **COMPLETE**
- 🔀 **Multi-Port Device Detection** - Hardware-validated device grouping ✅ **VALIDATED**
- 💾 **Smart EEPROM System** - Dynamic configuration with intelligent caching ✅ **COMPLETE**
- 📦 **Professional MSI Installer** - One-click deployment package ✅ **COMPLETE**
- 🌐 **XML Configuration System** - Multi-protocol configuration with validation ✅ **COMPLETE**
- ⚡ **Production Communication Engine** - Real RS232 with 3-phase workflows ✅ **COMPLETE**

## 📋 **Project Status - SPRINT 8 INTELLIGENT SUCCESS ✅**

### **✅ Sprint 1-2 - Windows Service Foundation** 
**Status:** 🎉 **COMPLETED & INTEGRATED**
- [x] Installable Windows Service with ServiceBase + automated PowerShell scripts
- [x] Professional logging (NLog + files + Event Viewer)
- [x] Automated installation with comprehensive CI/CD integration
- [x] 13/13 tests passing (100% coverage)

### **✅ Sprint 3-4 - Enhanced Discovery & Pool Management** 
**Status:** 🎉 **COMPLETED WITH HARDWARE VALIDATION**
- [x] **Thread-Safe SerialPortPool** with ConcurrentDictionary + SemaphoreSlim
- [x] **Multi-Port Device Detection** - FT4232H grouping validated with real hardware
- [x] **Smart SystemInfo Caching** - TTL-based with background cleanup
- [x] **65+ comprehensive tests** - Thread-safety + performance + hardware validation
- [x] **Enterprise Architecture** - Complete DI + monitoring + statistics

### **✅ Sprint 5-6 - Production Communication Foundation** 
**Status:** 🎉 **COMPLETED WITH PRODUCTION READY SERVICES**
- [x] **XML Configuration System** - Hierarchical BIB→UUT→PORT with schema validation
- [x] **RS232 Protocol Handler** - Production-grade SerialPort communication
- [x] **Protocol Factory Architecture** - Extensible for multiple communication protocols
- [x] **3-Phase Workflow Engine** - PowerOn → Test → PowerOff automation
- [x] **Service Integration** - Complete Windows Service with enhanced capabilities

### **✅ Sprint 7 - Enhanced Client Demo** 
**Status:** 🎉 **COMPLETED WITH CLIENT SATISFACTION**
- [x] **Enhanced Demo Application** - Professional console interface with 6 scenarios
- [x] **Loop Mode Continuous** - Real-time statistics and configurable intervals
- [x] **Service Demo Mode** - Windows Service installation demonstration
- [x] **XML Configuration** - Parameterizable BIB configurations
- [x] **Hardware Validation** - Tested with FT4232HL (5.9s cycles, 100% success)

### **🔥 Sprint 8 - Dynamic Intelligence & Advanced Validation** 
**Status:** 🎉 **COMPLETED - INTELLIGENT HARDWARE SUCCESS**

#### **✅ EEPROM Dynamic BIB Selection**
- [x] **FTD2XX_NET Integration** - Native FTDI API for direct EEPROM access
- [x] **ProductDescription Reading** - Extract BIB_ID directly from hardware
- [x] **Dynamic Mapping Service** - Automatic ProductDescription → BIB_ID mapping
- [x] **Intelligent Fallback** - Graceful degradation to static mapping when needed
- [x] **Performance Optimization** - EEPROM caching with TTL for rapid access

#### **✅ Advanced Regex Validation System**
- [x] **Regex Pattern Support** - `^READY$`, `STATUS:(?<status>OK)` validation patterns
- [x] **XML Configuration Enhanced** - `<expected_response regex="true">` support
- [x] **Named Group Capture** - Extract and log regex capture groups
- [x] **Performance Optimized** - Compiled regex with intelligent caching
- [x] **Backward Compatible** - Simple string matching preserved

#### **✅ Enhanced Service Integration**
- [x] **Dynamic Port Discovery** - Service adapts to connected hardware automatically
- [x] **Enhanced Error Handling** - Sophisticated timeout, retry, and recovery logic
- [x] **Professional Logging** - EEPROM reading + regex validation details
- [x] **Zero Manual Configuration** - Complete plug-and-play operation

### **🚀 Sprint 9 Foundation - ARCHITECTURE READY**
- **AI-Powered Analytics** - Machine learning for UUT response pattern analysis
- **REST API & Web Dashboard** - HTTP endpoints + browser-based monitoring
- **Multi-Protocol Expansion** - RS485, USB, CAN, I2C, SPI protocol handlers
- **Real-Time Device Management** - Hot-plug detection + dynamic reconfiguration

---

## 🏗️ **Complete Architecture**

```
SerialPortPoolService/                          ← Enhanced Windows Service
├── installer/
│   ├── SerialPortPool-Setup.wxs              ← Professional MSI installer
│   └── Build-Installer.ps1                   ← Automated build pipeline
├── Configuration/
│   ├── client-demo.xml                        ← Production XML configuration
│   └── regex-demo.xml                         ← Advanced regex examples
├── Services/
│   └── PortDiscoveryBackgroundService.cs     ← Continuous discovery service
└── Program.cs                                ← Enhanced DI with Sprint 8 services

SerialPortPool.Core/                           ← Complete Core Library
├── Models/
│   ├── Configuration/                        ← XML Configuration System
│   │   ├── BibConfiguration.cs               ├─ Hierarchical BIB→UUT→PORT
│   │   ├── PortConfiguration.cs              ├─ Multi-protocol settings
│   │   └── CommandSequence.cs                └─ 3-phase workflow definitions
│   ├── EEPROM/                               ← SPRINT 8: EEPROM Intelligence
│   │   ├── FtdiEepromData.cs                 ├─ EEPROM data models
│   │   ├── EnhancedFtdiDeviceInfo.cs         ├─ WMI + EEPROM combined
│   │   └── DynamicBibMapping.cs              └─ ProductDescription → BIB_ID
│   ├── Validation/                           ← SPRINT 8: Advanced Validation
│   │   ├── CommandValidationResult.cs        ├─ Regex validation results
│   │   └── RegexValidationOptions.cs         └─ Regex configuration options
│   ├── PortAllocation.cs                     ├─ Thread-safe allocation model
│   ├── SystemInfo.cs                         ├─ Enhanced EEPROM system info
│   ├── DeviceGroup.cs                        ├─ Multi-port device grouping
│   └── PoolStatistics.cs                     └─ Complete monitoring
├── Services/
│   ├── EEPROM/                               ← SPRINT 8: EEPROM Services
│   │   ├── FtdiEepromReader.cs               ├─ FTD2XX_NET integration
│   │   ├── DynamicBibMappingService.cs       ├─ Intelligent BIB selection
│   │   └── EnhancedFtdiDeviceReader.cs       └─ WMI + EEPROM combined
│   ├── Communication/                        ← Production Communication
│   │   ├── RS232ProtocolHandler.cs           ├─ Production RS232 + regex
│   │   ├── ProtocolHandlerFactory.cs         ├─ Multi-protocol factory
│   │   └── BibWorkflowOrchestrator.cs        └─ Complete 3-phase workflows
│   ├── Configuration/                        ← Enhanced Configuration
│   │   ├── XmlConfigurationLoader.cs         ├─ Hierarchical XML parsing
│   │   └── XmlBibConfigurationLoader.cs      └─ BIB-specific loading
│   ├── EnhancedSerialPortDiscoveryService.cs ← Multi-port + EEPROM discovery
│   ├── FtdiDeviceReader.cs                   ← FTDI analysis + validation
│   ├── SerialPortValidator.cs                ← Configurable validation
│   ├── SerialPortPool.cs                     ← Thread-safe pool management
│   ├── SystemInfoCache.cs                    ← Smart caching with TTL
│   └── MultiPortDeviceAnalyzer.cs            ← Device grouping intelligence
└── Interfaces/
    ├── ISerialPortPool.cs                     ├─ Pool management contract
    ├── IProtocolHandler.cs                    ├─ Multi-protocol abstraction
    ├── IFtdiEepromReader.cs                   ├─ SPRINT 8: EEPROM interface
    ├── IDynamicBibMappingService.cs           ├─ SPRINT 8: Dynamic mapping
    └── IMultiPortDeviceAnalyzer.cs            └─ Device grouping interface

tests/
├── SerialPortPool.Core.Tests/                ├─ 65+ comprehensive tests
├── PortDiscoveryDemo/                        ├─ Interactive discovery demo
├── RS232Demo/                                ├─ Production communication demo
└── EnhancedDemo/                             └─ SPRINT 8: Complete intelligent demo
```

## 🚀 **Quick Start & Demo**

### **Prerequisites**
- **OS:** Windows 10/11 or Windows Server 2016+
- **Runtime:** .NET 9.0 or higher
- **Permissions:** Administrator rights for service installation
- **Hardware:** FTDI device recommended for complete testing

### **Instant Installation (MSI Package)**

```powershell
# 1. Download and run MSI installer
SerialPortPool-Setup.msi
# → Follow installation wizard (one-click installation)

# 2. Verify service installation
Get-Service SerialPortPoolService
# → Should display "Running" status

# 3. Test with enhanced demo
cd "C:\Program Files\SerialPortPool\"
.\EnhancedDemo.exe --xml-config client-demo.xml --loop
# → Demonstrates complete workflow with dynamic EEPROM detection
```

## 🔧 **Sprint 8 Usage - Intelligent Hardware**

### **EEPROM Dynamic BIB Selection**

```xml
<!-- Configure FTDI device EEPROM ProductDescription -->
<!-- ProductDescription = "client_demo" → Automatic BIB_ID selection -->

<!-- SerialPortPool automatically detects and uses correct configuration -->
<!-- Zero manual port mapping required! -->
```

### **Advanced Regex Validation**

```xml
<?xml version="1.0" encoding="UTF-8"?>
<root>
  <bib id="advanced_demo">
    <uut id="production_uut">
      <port number="1">
        <protocol>rs232</protocol>
        <speed>115200</speed>
        
        <!-- Advanced regex patterns for flexible validation -->
        <start>
          <command>INIT_SYSTEM\r\n</command>
          <expected_response regex="true">^(READY|OK|INITIALIZED)(\r\n)?$</expected_response>
          <timeout_ms>3000</timeout_ms>
        </start>
        
        <test>
          <command>GET_STATUS\r\n</command>
          <expected_response regex="true">^STATUS:(?&lt;status&gt;PASS|OK|GOOD)(\r\n)?$</expected_response>
          <timeout_ms>5000</timeout_ms>
        </test>
        
        <stop>
          <command>SHUTDOWN\r\n</command>
          <expected_response regex="true">^(BYE|GOODBYE|TERMINATED)(\r\n)?$</expected_response>
          <timeout_ms>2000</timeout_ms>
        </stop>
      </port>
    </uut>
  </bib>
</root>
```

### **Complete Intelligent Workflow**

```csharp
// Sprint 8: Complete intelligent workflow with EEPROM + Regex
var services = new ServiceCollection();
services.AddSprint8ProductionServices();  // All Sprint 8 intelligence
var provider = services.BuildServiceProvider();

var orchestrator = provider.GetRequiredService<IBibWorkflowOrchestrator>();

// Execute with automatic EEPROM BIB detection + regex validation
var result = await orchestrator.ExecuteBibWorkflowAutoDetectAsync(
    "client_demo",      // BIB_ID (auto-detected from EEPROM)
    "production_uut", 
    portNumber: 1,
    clientId: "IntelligentClient"
);

if (result.Success)
{
    Console.WriteLine($"✅ Intelligent workflow completed!");
    Console.WriteLine($"🔬 EEPROM BIB Detection: {result.EepromDetection}");
    Console.WriteLine($"🎯 Regex Validations: {result.RegexValidations}");
    Console.WriteLine($"⏱️ Duration: {result.Duration.TotalSeconds:F1}s");
}
```

### **Enhanced Discovery with EEPROM Intelligence**

```bash
# Enhanced discovery demo with EEPROM reading
dotnet run --project tests/EnhancedDemo/

# Output: Intelligent hardware detection
# 🔬 Enhanced Serial Port Discovery - EEPROM Intelligence
# 📡 Features: FTDI Analysis + EEPROM Reading + Dynamic BIB Selection + Regex Validation
# === DYNAMIC EEPROM BIB DETECTION ===
# 🔍 Found 4 FTDI device(s) with EEPROM data:
# 🏭 ✅ 🔬 FT4232HL - COM11 (ProductDescription: "client_demo A") → BIB: client_demo
# 🏭 ✅ 🔬 FT4232HL - COM12 (ProductDescription: "client_demo B") → BIB: client_demo
# 🏭 ✅ 🔬 FT4232HL - COM13 (ProductDescription: "client_demo C") → BIB: client_demo
# 🏭 ✅ 🔬 FT4232HL - COM14 (ProductDescription: "client_demo D") → BIB: client_demo
```

## 🧪 **Testing and Quality**

### **Comprehensive Test Coverage - Sprint 8**
![Tests Sprint 1](https://img.shields.io/badge/Sprint%201%20Tests-13%2F13%20PASSED-brightgreen.svg)
![Tests Sprint 2](https://img.shields.io/badge/Sprint%202%20Tests-12%2F12%20PASSED-brightgreen.svg)
![Tests Sprint 3-4](https://img.shields.io/badge/Sprint%203--4%20Tests-65%2B%2F65%2B%20PASSED-brightgreen.svg)
![Tests Sprint 5-6](https://img.shields.io/badge/Sprint%205--6%20Tests-PRODUCTION%20READY-brightgreen.svg)
![Tests Sprint 7](https://img.shields.io/badge/Sprint%207%20Tests-ENHANCED%20DEMO-brightgreen.svg)
![Tests Sprint 8](https://img.shields.io/badge/Sprint%208%20Tests-INTELLIGENT%20VALIDATED-brightgreen.svg)
![Integration](https://img.shields.io/badge/Integration-HARDWARE%20VALIDATED-brightgreen.svg)

```bash
# Complete test suite Sprint 1-8 (65+ tests + Sprint 8 additions)
dotnet test tests/SerialPortPool.Core.Tests/ --verbosity normal
dotnet test tests/SerialPortPool.Tests/ --verbosity normal

# Expected Output Sprint 8:
# Test Run Summary: Total: 70+, Passed: 70+, Skipped: 0
# ✅ Sprint 1-2: Windows Service Foundation (25 tests)
# ✅ Sprint 3-4: Pool Management + Device Grouping (40+ tests)
# ✅ Sprint 5-6: Communication & XML Configuration (Production tests)
# ✅ Sprint 7: Enhanced Demo Features (Validation tests)
# ✅ Sprint 8: EEPROM Intelligence + Regex Validation (New tests)
```

### **Real Hardware Validation - Complete**
- ✅ **Tested with FTDI FT4232HL** (COM11-14, PID 6048, EEPROM ProductDescription)
- ✅ **Tested with FTDI FT232R** (COM6, PID 6001, WMI + EEPROM combined)
- ✅ **EEPROM Dynamic BIB Selection** functional with real FTDI devices
- ✅ **Advanced Regex Validation** tested with production UUT responses
- ✅ **Thread-Safe Pool Management** stress tested with concurrent operations
- ✅ **Service Windows Integration** validated with complete automation
- ✅ **Multi-Protocol Architecture** proven extensible for Sprint 9 expansion

## 🎉 **Sprint 8 Achievements - Intelligent Hardware**

### **🏆 Revolutionary Features Delivered**
- **📦 Professional MSI Deployment**: Enterprise-grade installation package ✅
- **🏭 Real Hardware Validation**: Complete FT4232H + FT232R testing ✅
- **📊 Comprehensive Testing**: 70+ tests across all sprint features ✅
- **⚡ Production Performance**: <6s workflows, thread-safe, memory efficient ✅
- **🔧 Enterprise Architecture**: Complete DI + logging + monitoring ✅
- **🔀 Multi-Port Intelligence**: Hardware-validated device grouping ✅
- **🎯 Production Pool Management**: Thread-safe allocation with smart caching ✅
- **🌐 XML Configuration System**: Multi-protocol with advanced validation ✅
- **📡 Production Communication**: Real RS232 with 3-phase workflows ✅
- **🔬 EEPROM Dynamic Intelligence**: Automatic BIB selection from hardware ✅
- **🎯 Advanced Regex Validation**: Sophisticated pattern matching ✅

### **🔥 Sprint 8 Technical Innovations**
- **FTD2XX_NET Integration**: Native FTDI API for direct EEPROM access
- **Dynamic Hardware Intelligence**: ProductDescription → BIB_ID automatic mapping
- **Advanced Regex Engine**: Compiled patterns with named group capture
- **Intelligent Fallback Strategy**: Graceful degradation from EEPROM to static mapping
- **Enhanced Service Architecture**: Complete automation with zero manual configuration
- **Performance Optimization**: EEPROM caching + compiled regex for production speed

### **🎯 Sprint 8 Results Summary**
- **EEPROM Intelligence**: ✅ **COMPLETE** - Automatic BIB detection from hardware
- **Regex Validation**: ✅ **COMPLETE** - Advanced pattern matching with capture groups
- **Service Integration**: ✅ **COMPLETE** - Windows Service with full automation
- **Performance**: ✅ **PRODUCTION** - <6 second workflows with intelligent caching
- **Quality**: ✅ **ENTERPRISE** - 70+ tests, hardware validated, zero regression

### **🚀 Sprint 9 Foundation Ready**
- **Architecture Proven**: EEPROM + Regex foundations ready for AI/ML expansion
- **API Infrastructure**: REST endpoints can expose EEPROM + validation capabilities
- **Analytics Foundation**: Regex validation data ready for machine learning analysis
- **Multi-Protocol**: Protocol factory architecture ready for RS485, USB, CAN expansion
- **Enterprise Ready**: Complete monitoring, logging, and deployment infrastructure

---

## 📞 **Support and Documentation**

### **Complete Documentation - Sprint 8**
- 📖 **Architecture Guide**: [Sprint 8 Intelligence Architecture](docs/sprint8/)
- 🚀 **Installation Guide**: [Professional MSI Installation](SerialPortPoolService/installer/)
- 🧪 **Testing Guide**: [Complete Test Suite](tests/)
- 📊 **Hardware Validation**: [EEPROM + Regex Testing Results](docs/sprint8/SPRINT8-PLANNING.md)
- 🔀 **Device Intelligence**: [Multi-Port + EEPROM Guide](docs/sprint8/SPRINT8-DYNAMIC-BIB-README.md)
- 🌐 **XML Configuration**: [Advanced Regex Configuration](docs/sprint8/XML-Configuration.md)
- 🔬 **EEPROM Intelligence**: [Dynamic BIB Selection Guide](docs/sprint8/SPRINT8-DYNAMIC-BIB-README.md)

### **Hardware & Software Support**
- 🔌 **FTDI Support**: All chips (FT232R, FT4232H/HL, FT232H, FT2232H, etc.)
- 🔬 **EEPROM Intelligence**: ProductDescription-based BIB selection
- 🎯 **Advanced Validation**: Regex patterns with named group capture
- 🏊 **Thread-Safe Pool**: Production allocation with session tracking
- 🔀 **Device Grouping**: Multi-port device awareness ✅ **HARDWARE VALIDATED**
- 💾 **Smart Caching**: EEPROM + SystemInfo caching with TTL
- 🎯 **Flexible Validation**: Client strict vs dev permissive modes
- 🏗️ **Service Integration**: Complete DI + background discovery
- 📦 **Professional Deployment**: MSI installer for production environments
- 🌐 **Multi-Protocol Foundation**: Architecture ready for 6 protocols

---

## 🚀 **Next: Sprint 9 - AI Intelligence & Enterprise Platform**

### **🧠 Sprint 9 Advanced Intelligence:**
- **AI-Powered Analytics** - Machine learning for UUT response pattern analysis
- **REST API & Web Dashboard** - HTTP endpoints + real-time browser monitoring
- **Multi-Protocol Expansion** - RS485, USB, CAN, I2C, SPI protocol handlers
- **Real-Time Device Management** - Hot-plug detection + dynamic reconfiguration
- **Advanced Configuration UI** - Web-based BIB configuration and regex builder

### **Foundation Excellence Ready:**
- ✅ **EEPROM Intelligence** proven with real hardware validation
- ✅ **Regex Validation** extensible for AI/ML pattern analysis
- ✅ **Service Architecture** scalable for enterprise deployment
- ✅ **Hardware Compatibility** validated with industrial equipment
- ✅ **Performance Optimized** for production workloads

**Sprint Progression:**
> **Sprint 1-2:** Windows Service Foundation ✅ COMPLETE  
> **Sprint 3-4:** Thread-Safe Pool + Device Grouping ✅ COMPLETE  
> **Sprint 5-6:** Production Communication + XML Configuration ✅ COMPLETE  
> **Sprint 7:** Enhanced Demo + Service Integration ✅ COMPLETE  
> **Sprint 8:** EEPROM Intelligence + Regex Validation ✅ COMPLETE  
> **Sprint 9:** AI Analytics + Enterprise Platform 🚀 ARCHITECTURE READY  

**Current Status: Sprint 8 INTELLIGENT HARDWARE SUCCESS with Foundation Ready for Enterprise AI Platform!** 🔥

---

*Last Updated: August 2025 - Sprint 8 Intelligent Hardware Complete*  
*Current Status: Production Ready with EEPROM Intelligence + Advanced Regex Validation*  
*Version: 8.0.0 - Intelligent Hardware with Dynamic BIB Selection*  
*Tests: 70+ comprehensive tests with EEPROM + Regex validation*  
*Hardware Validated: FTDI FT4232HL + FT232R with EEPROM ProductDescription*  
*Ready for: Sprint 9 AI Intelligence & Enterprise Platform Expansion*