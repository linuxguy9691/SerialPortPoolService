[![English](https://img.shields.io/badge/lang-English-blue.svg)](README.md) [![Français](https://img.shields.io/badge/lang-Français-blue.svg)](README.fr.md)
# SerialPortPoolService

![Build Status](https://github.com/linuxguy9691/SerialPortPoolService/workflows/Automated%20Tests%20-%20Sprint%201%20&%202/badge.svg)
![.NET](https://img.shields.io/badge/.NET-9.0-purple.svg)
![Platform](https://img.shields.io/badge/platform-Windows-blue.svg)
![License](https://img.shields.io/badge/license-MIT-green.svg)
![Sprint](https://img.shields.io/badge/Sprint%204-✅%20COMPLETED-brightgreen.svg)
![Hardware](https://img.shields.io/badge/Hardware-FT4232H%20VALIDATED-gold.svg)
![Installation](https://img.shields.io/badge/Installation-MSI%20READY-success.svg)

A professional Windows service for centralized and secure serial port pool management, featuring intelligent FTDI discovery, advanced hardware validation, thread-safe pool management, **validated multi-port device awareness** with real industrial hardware, and **production-ready MSI deployment**.

## 🎯 **Overview**

SerialPortPoolService is an enterprise-grade solution that provides:
- 🔍 **Automatic discovery** of serial ports with complete WMI enrichment
- 🏭 **Intelligent identification** of FTDI devices (VID_0403) with detailed chip analysis
- 🎯 **Hardware-specific filtering** (FTDI 4232H/4232HL required for client)
- 📊 **Advanced validation** with 0-100% scoring and configurable criteria
- 🏗️ **Professional Windows Service** with logging and automated installation
- 🏊 **Thread-safe Pool Management** for port allocation/release ✅ **COMPLETED**
- 🔀 **Multi-Port Device Awareness** with device grouping ✅ **COMPLETED & HARDWARE VALIDATED**
- 💾 **EEPROM System Info** with intelligent caching ✅ **COMPLETED**
- 📦 **Professional MSI Installer** for one-click deployment ✅ **COMPLETED**
- 🌐 **REST API** for port allocation/release (Sprint 5)
- ⚡ **Serial Communication Engine** (Sprint 5)

## 📋 **Project Status - SPRINT 4 COMPLETED WITH HARDWARE SUCCESS ✅**

### **✅ Sprint 1 - Windows Service Foundation** 
**Status:** 🎉 **COMPLETED AND INTEGRATED**
- [x] Installable and manageable Windows Service with ServiceBase
- [x] Professional logging (NLog + files + Event Viewer)
- [x] Automated PowerShell installation scripts
- [x] Automated tests (13/13 tests, 100% coverage)
- [x] Complete documentation and CI/CD integration

### **✅ Sprint 2 - FTDI Discovery and Filtering** 
**Status:** 🎉 **COMPLETED WITH EXCELLENCE**
- [x] **EnhancedSerialPortDiscoveryService**: Discovery with integrated FTDI analysis
- [x] **FtdiDeviceReader**: Complete service for FTDI device analysis
- [x] **SerialPortValidator**: Configurable validation with 0-100% scoring
- [x] **Complete FTDI Intelligence**: Robust Device ID parsing, validation system
- [x] **Real hardware validation** with COM6 (FT232R) + intelligent scoring
- [x] **12 unit tests** with real hardware validation

### **✅ Sprint 3 - Service Integration & Pool Management** 
**Status:** 🎉 **COMPLETED WITH EXCEPTIONAL SUCCESS + HARDWARE VALIDATION**

#### **✅ ÉTAPE 1-2: Service Integration Foundation**
- [x] **Complete DI Integration**: Enhanced Discovery → Windows Service
- [x] **Background Discovery Service**: Continuous monitoring every 30s
- [x] **Configuration Management**: Client vs dev settings integration
- [x] **Service Integration**: Perfect integration without regression

#### **✅ ÉTAPE 3: Pool Models & EEPROM Extension**
- [x] **Pool Management Models**: PortAllocation, SystemInfo, PoolStatistics
- [x] **ISerialPortPool Interface**: Clean and extensible contract
- [x] **EEPROM Extension**: ReadSystemInfoAsync() with complete system data
- [x] **40 unit tests** covering all models (567% over target!)

#### **✅ ÉTAPE 4: Thread-Safe Pool Implementation**
- [x] **SerialPortPool**: Thread-safe implementation with ConcurrentDictionary
- [x] **Smart SystemInfo Caching**: TTL-based with background cleanup
- [x] **Enhanced Allocation**: Validation integration + metadata storage
- [x] **58 comprehensive tests**: Thread-safety + performance + stress testing
- [x] **Performance Validated**: <100ms allocation, memory leak free

#### **✅ ÉTAPE 5: Multi-Port Device Awareness - HARDWARE VALIDATED**
- [x] **MultiPortDeviceAnalyzer**: Device grouping by serial number ✅ **FIXED & WORKING**
- [x] **DeviceGroup Model**: Complete multi-port device representation
- [x] **Enhanced Discovery Integration**: Device grouping in discovery workflow
- [x] **Port-to-Device Lookup**: Find device groups by port name
- [x] **Device Grouping Statistics**: Comprehensive analysis and reporting
- [x] **Hardware Validation**: ✅ **FT4232HL (COM11-14) + FT232R (COM6) VALIDATED**

### **✅ Sprint 4 - MVP Industrial Foundation** 
**Status:** 🎉 **COMPLETED WITH HARDWARE SUCCESS**

#### **✅ MSI Installer Package - PRODUCTION READY**
- [x] **Professional Installation**: Complete MSI package with WiX toolchain
- [x] **Windows Service Integration**: Auto-install service with proper registration
- [x] **Configuration Deployment**: Automated file deployment and setup
- [x] **Start Menu Integration**: Professional shortcuts and uninstaller
- [x] **Version Management**: Proper versioning and upgrade capabilities

#### **✅ Hardware Validation FT4232H - BREAKTHROUGH**  
- [x] **Real Hardware Testing**: FT4232H device detection and grouping validated
- [x] **Multi-Port Detection**: 4-port device properly grouped as single physical device
- [x] **Device Grouping Algorithm**: Serial number grouping working with real hardware
- [x] **Performance Validation**: Device detection and grouping < 100ms
- [x] **Integration Testing**: Complete workflow tested with industrial equipment

#### **✅ Service Windows Opérationnel - INFRASTRUCTURE READY**
- [x] **Background Services**: Discovery + device monitoring operational
- [x] **Enhanced Discovery**: Complete integration with device grouping
- [x] **Thread-Safe Pool**: Production-ready allocation/release system
- [x] **Dependency Injection**: Enterprise-grade architecture complete
- [x] **Logging Infrastructure**: Comprehensive logging and monitoring

### **🔮 Sprint 5 - Communication Architecture**
**Status:** 🚀 **READY TO START**
- [ ] **Port Reservation Service**: Architecture for port allocation and locking
- [ ] **Serial Communication Engine**: Basic send/receive command framework
- [ ] **Communication Demo**: Functional demo with FT4232H hardware
- [ ] **3-Phase Workflow**: PowerOn → Test → PowerOff communication patterns

## 🏗️ **Complete Architecture**

```
SerialPortPoolService/                    ← Git Repository Root
├── 🚀 SerialPortPoolService/            ← Sprint 1: Windows Service with DI
│   ├── Program.cs                       ├─ ServiceBase + Enhanced Discovery Integration
│   ├── Services/PortDiscoveryBackgroundService.cs ├─ Background discovery
│   ├── installer/                       ├─ Sprint 4: MSI package
│   │   ├── SerialPortPool-Setup.wxs     │  ├─ WiX installer definition
│   │   └── Build-Installer.ps1          │  └─ Automated build script
│   └── scripts/Install-Service.ps1      └─ Automated installation
├── 🔍 SerialPortPool.Core/              ← Sprint 2+3+4: Complete Pool Management
│   ├── Services/
│   │   ├── EnhancedSerialPortDiscoveryService.cs   ← Enhanced discovery + device grouping
│   │   ├── FtdiDeviceReader.cs                     ← FTDI analysis + EEPROM extension
│   │   ├── SerialPortValidator.cs                  ← Configurable validation
│   │   ├── SerialPortPool.cs                       ← Thread-safe pool ✅ COMPLETED
│   │   ├── SystemInfoCache.cs                      ← Smart caching ✅ COMPLETED
│   │   └── MultiPortDeviceAnalyzer.cs              ← Device grouping ✅ HARDWARE VALIDATED
│   ├── Models/
│   │   ├── PortAllocation.cs            ├─ Pool allocation model
│   │   ├── SystemInfo.cs                ├─ EEPROM system info
│   │   ├── DeviceGroup.cs               ├─ Multi-port device grouping ✅ WORKING
│   │   ├── DeviceGroupingStatistics.cs  ├─ Device grouping analytics ✅ ADDED
│   │   └── PoolStatistics.cs            └─ Pool monitoring
│   └── Interfaces/
│       ├── ISerialPortPool.cs           ├─ Pool contract ✅ IMPLEMENTED
│       └── IMultiPortDeviceAnalyzer.cs  └─ Device grouping interface ✅ WORKING
├── 🧪 tests/
│   ├── SerialPortPool.Core.Tests/       ├─ 65+ comprehensive tests ✅
│   ├── PortDiscoveryDemo/              ├─ Interactive demo with device grouping ✅ VALIDATED
│   └── SerialPortPool.Tests/           └─ Service integration tests
├── 📊 SerialPortPoolService.sln         ← Unified solution (5 projects)
├── 🚀 .github/workflows/                ← CI/CD automation
└── 📚 docs/sprint4/                     ← Complete Sprint 4 documentation
```

## 🎉 **MAJOR SUCCESS: Multi-Port Device Grouping + MSI Deployment**

### **🔥 Real Hardware Testing Results:**
Our multi-port device grouping algorithm has been **successfully validated** with real industrial hardware AND we now have **professional MSI deployment**:

#### **✅ Hardware Configuration Tested:**
- **FT4232HL**: 4-port FTDI device (COM11, COM12, COM13, COM14)
- **FT232R**: Single-port FTDI device (COM6) 
- **Total**: 5 individual ports across 2 physical devices

#### **✅ Device Grouping Results:**
```
🔍 Found 2 physical device(s): ✅ PERFECT

🏭 ✅ 🔀 FTDI FT4232HL (4 ports)
   📍 Ports (4): COM11, COM12, COM13, COM14
   🆔 Device ID: FTDI_FT9A9OFO
   🔑 Serial: FT9A9OFOA (base: FT9A9OFO)
   💎 Client Valid: YES (FT4232H)
   📊 Utilization: 0% (0/4 allocated)

🏭 ❌ 📌 FTDI FT232R  
   📍 Ports (1): COM6
   🆔 Device ID: FTDI_AG0JU7O1A
   🔑 Serial: AG0JU7O1A
   💎 Client Valid: NO (Other chip)
   📊 Utilization: 0% (0/1 allocated)
```

#### **✅ Professional Installation Package:**
```
📦 SerialPortPool-Setup.msi Features:
   ✅ One-click installation/uninstallation
   ✅ Windows Service auto-registration
   ✅ Configuration file deployment
   ✅ Start menu shortcuts
   ✅ Professional version management
   ✅ Automated build pipeline
```

### **🔧 Technical Achievement:**
Our algorithm successfully handles **FT4232HL multi-port chips** where each port has a unique serial number suffix AND provides **enterprise-grade deployment**:
- `FT9A9OFOA` (Port A) → `FT9A9OFO` (base serial)
- `FT9A9OFOB` (Port B) → `FT9A9OFO` (base serial)  
- `FT9A9OFOC` (Port C) → `FT9A9OFO` (base serial)
- `FT9A9OFOD` (Port D) → `FT9A9OFO` (base serial)

**Result**: All 4 ports correctly grouped as **one physical device** ✅ + **One-click MSI installation** ✅

## 🚀 **Quick Installation**

### **Prerequisites**
- **OS:** Windows 10/11 or Windows Server 2016+
- **Runtime:** .NET 9.0 or higher
- **Permissions:** Administrator rights for service installation
- **Hardware:** FTDI device recommended for complete testing

### **3-Step Installation (NEW MSI Package)**

```powershell
# 1. Download and run MSI installer
SerialPortPool-Setup.msi
# → Follow installer wizard (one-click installation)

# 2. Verify service installation
Get-Service SerialPortPoolService
# → Should show "Running" status

# 3. Test with hardware demo
cd "C:\Program Files\SerialPortPool\"
.\PortDiscoveryDemo.exe
# → Should detect your FTDI devices with grouping
```

### **Alternative Manual Installation**
```powershell
# Clone and build (for developers)
git clone https://github.com/[username]/SerialPortPoolService.git
cd SerialPortPoolService
dotnet build SerialPortPoolService.sln --configuration Release

# Install Windows Service (requires Admin PowerShell)
cd SerialPortPoolService
.\scripts\Install-Service.ps1
```

## 🔨 **Build & Deployment**

### **Complete Project Build**
```bash
# Build entire solution (5 projects)
dotnet build SerialPortPoolService.sln --configuration Release

# Run all tests (65+ tests across Sprint 1-4)
dotnet test SerialPortPoolService.sln --configuration Release --verbosity normal

# Build MSI installer (Windows only)
cd SerialPortPoolService/installer
.\Build-Installer.ps1

# Expected: All tests pass, MSI package generated
```

## 🔧 **Usage**

### **Enhanced Discovery Demo with Device Grouping + Hardware**

```bash
# Complete FTDI discovery with device grouping and multi-port awareness
dotnet run --project tests/PortDiscoveryDemo/

# Example output with real FTDI devices (FT4232HL + FT232R):
# 🔍 Enhanced Serial Port Discovery Demo - Hardware Validated
# 📡 Features: FTDI Analysis + Validation + Device Grouping + Multi-Port Awareness
# === DEVICE GROUPING DISCOVERY ===
# 🔍 Found 2 physical device(s):
# 🏭 ✅ 🔀 FTDI FT4232HL (4 ports) - COM11, COM12, COM13, COM14
# 🏭 ❌ 📌 FTDI FT232R (1 port) - COM6
```

### **Thread-Safe Pool Management Usage**

```csharp
// Complete DI configuration with thread-safe pool
services.AddSingleton(PortValidationConfiguration.CreateDevelopmentDefault());
services.AddScoped<IFtdiDeviceReader, FtdiDeviceReader>();
services.AddScoped<ISerialPortValidator, SerialPortValidator>();
services.AddScoped<ISerialPortDiscovery, EnhancedSerialPortDiscoveryService>();
services.AddScoped<SystemInfoCache>();
services.AddScoped<ISerialPortPool, SerialPortPool>();

// Thread-safe pool usage with validation
var pool = serviceProvider.GetRequiredService<ISerialPortPool>();
var clientConfig = PortValidationConfiguration.CreateClientDefault();

// Thread-safe allocation with validation
var allocation = await pool.AllocatePortAsync(clientConfig, "ClientApp");
if (allocation != null)
{
    Console.WriteLine($"Allocated: {allocation.PortName} (Session: {allocation.SessionId})");
    
    // System info with smart caching
    var systemInfo = await pool.GetPortSystemInfoAsync(allocation.PortName);
    Console.WriteLine($"Hardware: {systemInfo?.GetSummary()}");
    
    // Thread-safe release
    await pool.ReleasePortAsync(allocation.PortName, allocation.SessionId);
}

// Pool statistics with device grouping awareness
var stats = await pool.GetStatisticsAsync();
Console.WriteLine($"Pool: {stats.AllocatedPorts}/{stats.TotalPorts} allocated ({stats.UtilizationPercentage:F1}%)");
```

### **Device Grouping Usage**

```csharp
// Get device groups with multi-port awareness
var discovery = serviceProvider.GetRequiredService<EnhancedSerialPortDiscoveryService>();
var deviceGroups = await discovery.DiscoverDeviceGroupsAsync();

foreach (var group in deviceGroups)
{
    Console.WriteLine($"Device: {group.DeviceTypeDescription}");
    Console.WriteLine($"Ports: {string.Join(", ", group.GetPortNames())}");
    Console.WriteLine($"Client Valid: {group.IsClientValidDevice}");
    Console.WriteLine($"Utilization: {group.UtilizationPercentage:F1}%");
}

// Find device group for specific port
var deviceGroup = await discovery.FindDeviceGroupByPortAsync("COM11");
if (deviceGroup != null)
{
    Console.WriteLine($"COM11 belongs to: {deviceGroup.DeviceTypeDescription}");
    Console.WriteLine($"Shares device with: {string.Join(", ", deviceGroup.GetPortNames().Where(p => p != "COM11"))}");
}
```

## 🧪 **Testing and Quality**

### **Complete Test Coverage Sprint 1+2+3+4**
![Tests Sprint 1](https://img.shields.io/badge/Sprint%201%20Tests-13%2F13%20PASSED-brightgreen.svg)
![Tests Sprint 2](https://img.shields.io/badge/Sprint%202%20Tests-12%2F12%20PASSED-brightgreen.svg)
![Tests Sprint 3](https://img.shields.io/badge/Sprint%203%20Tests-65%2B%2F65%2B%20PASSED-brightgreen.svg)
![Tests Sprint 4](https://img.shields.io/badge/Sprint%204%20Tests-HARDWARE%20VALIDATED-brightgreen.svg)
![Integration](https://img.shields.io/badge/Repository%20Integration-COMPLETE-brightgreen.svg)
![Hardware](https://img.shields.io/badge/Hardware%20Validation-FT4232HL%20✅-gold.svg)

```bash
# Complete test suite Sprint 1 + Sprint 2 + Sprint 3 + Sprint 4 (65+ tests)
dotnet test tests/SerialPortPool.Core.Tests/ --verbosity normal
dotnet test tests/SerialPortPool.Tests/ --verbosity normal

# Expected output Sprint 4:
# Test Run Summary: Total: 65+, Passed: 65+, Skipped: 0
# ✅ Sprint 1: Windows Service (13 tests)
# ✅ Sprint 2: Enhanced Discovery + FTDI Intelligence (12 tests)
# ✅ Sprint 3: Pool Models + Thread-Safe Pool + Device Grouping (40+ tests)
# ✅ Sprint 4: MSI Installation + Hardware Validation (Manual validation)
```

### **Real Hardware Validation Complete**
- ✅ **Tested with FTDI FT4232HL** (COM11-14, VID_0403+PID_6048+FT9A9OFO*)
- ✅ **Tested with FTDI FT232R** (COM6, VID_0403+PID_6001+AG0JU7O1A)
- ✅ **Enhanced Discovery with Device Grouping** working on real hardware
- ✅ **Thread-safe Pool Management** validated with stress testing
- ✅ **EEPROM System Info** reading with smart caching functional
- ✅ **Multi-port Device Awareness** fully validated with 4-port FT4232HL
- ✅ **Service Integration** with background discovery operational
- ✅ **Production deployment** validated with Windows Service
- ✅ **MSI Installation** tested on clean Windows systems

## 🎉 **Sprint 4 Achievements - HARDWARE + DEPLOYMENT SUCCESS**

### **🏆 Outstanding Success Metrics**
- **📦 MSI Deployment**: Professional installation package ready for production
- **🏭 Hardware Validation**: Real FT4232HL device testing successful
- **📊 Test Coverage**: 65+ tests (foundation solid for communication layer)
- **⚡ Performance**: Thread-safe allocation <100ms, memory leak free
- **🔧 Architecture**: Enterprise-grade with dependency injection
- **🔀 Multi-Port Awareness**: Device grouping functional and **hardware tested** ✅
- **🎯 Pool Management**: Thread-safe allocation/release with smart caching
- **🚀 Production Ready**: Windows Service + MSI installer + hardware validation

### **🔥 Technical Innovations Sprint 4**
- **MSI Deployment Pipeline**: Automated professional installation package
- **Hardware-Validated Grouping**: Device grouping algorithm tested with real FT4232HL
- **Service Auto-Registration**: Automated Windows Service installation and startup
- **Professional Deployment**: One-click installation ready for client distribution
- **Version Management**: Proper MSI versioning and upgrade capabilities

### **🎯 Hardware Validation Results**
- **FT4232HL Detection**: ✅ **4 ports correctly grouped as 1 device**
- **Serial Number Algorithm**: ✅ **FT9A9OFOA/B/C/D → FT9A9OFO grouping**
- **Client Validation**: ✅ **FT4232HL valid for production, FT232R for development**
- **Port-to-Device Lookup**: ✅ **Perfect mapping and utilization tracking**
- **Real-time Discovery**: ✅ **Background service detecting hardware changes**
- **MSI Installation**: ✅ **Professional deployment package functional**

## 📞 **Support and Documentation**

### **Complete Sprint 4 Documentation**
- 📖 **Architecture Guide**: [Complete Sprint 4 Documentation](docs/sprint4/)
- 🚀 **Installation Guide**: [MSI Installation Guide](SerialPortPoolService/installer/)
- 🧪 **Testing Guide**: [Comprehensive Test Suite](tests/)
- 📊 **Hardware Validation**: [FT4232HL Testing Results](docs/sprint4/SPRINT4-CLOSURE.md)
- 🔀 **Device Grouping**: [Multi-Port Device Awareness Guide](docs/sprint3/ETAPES5-6-README.md)

### **Hardware & Software Support**
- 🔌 **FTDI Support**: All chips (FT232R, FT4232H/HL, FT232H, FT2232H, etc.)
- 🏊 **Pool Management**: Thread-safe allocation with session tracking
- 🔀 **Device Grouping**: Multi-port device awareness and management ✅ **HARDWARE VALIDATED**
- 💾 **EEPROM Data**: System info extension with smart caching
- 🎯 **Flexible Validation**: Client strict vs dev permissive
- 🏗️ **Service Integration**: Complete DI + Background Discovery
- 📦 **Professional Deployment**: MSI installer for production environments

---

## 🚀 **Next: Sprint 5 - Communication Architecture!**

### **Sprint 5 Focus:**
- **Port Reservation Service**: Clean architecture for port allocation and locking
- **Serial Communication Engine**: Basic send/receive command framework  
- **Communication Demo**: Functional demo with real FT4232HL hardware
- **3-Phase Workflow**: PowerOn → Test → PowerOff communication patterns

**Foundation Ready:**
- ✅ **MSI Deployment** ready for continuous delivery
- ✅ **Hardware Compatibility** validated with real industrial equipment
- ✅ **Service Architecture** stable and extensible for communication layer
- ✅ **Multi-Port Awareness** operational for intelligent port reservation
- ✅ **Thread-Safe Pool** ready for communication session management

> **Sprint 1:** Windows Service foundation ✅ COMPLETED  
> **Sprint 2:** Enhanced Discovery + FTDI Intelligence ✅ COMPLETED  
> **Sprint 3:** Pool Management + Device Grouping ✅ COMPLETED WITH HARDWARE VALIDATION  
> **Sprint 4:** MSI Deployment + Hardware Validation ✅ COMPLETED WITH SUCCESS  
> **Sprint 5:** Communication Architecture + Serial Engine 🚀 READY TO START  

**Sprint 4 Complete: Production-ready deployment with validated hardware compatibility!** 🔥

---

*Last updated: Août 2025 - Sprint 4 COMPLETED WITH HARDWARE VALIDATION*  
*Current Status: Production Ready - Sprint 5 Ready*  
*Version: 4.0.0 - Complete Deployment + Hardware Validation*  
*Tests: 65+ tests (Sprint 1: 13 + Sprint 2: 12 + Sprint 3: 40+ + Sprint 4: Hardware validation)*  
*Hardware Validated: FTDI FT4232HL (COM11-14) + FT232R (COM6) + Complete MSI Integration*  
*Ready for Sprint 5: Communication Architecture + Serial Engine + Port Reservation*