[![English](https://img.shields.io/badge/lang-English-blue.svg)](README.md) [![Français](https://img.shields.io/badge/lang-Français-blue.svg)](README.fr.md)
# SerialPortPoolService

![Build Status](https://github.com/linuxguy9691/SerialPortPoolService/workflows/Automated%20Tests%20-%20Sprint%201%20&%202/badge.svg)
![.NET](https://img.shields.io/badge/.NET-9.0-purple.svg)
![Platform](https://img.shields.io/badge/platform-Windows-blue.svg)
![License](https://img.shields.io/badge/license-MIT-green.svg)
![Sprint](https://img.shields.io/badge/Sprint%204-🚀%20IN%20PROGRESS-blue.svg)
![Hardware](https://img.shields.io/badge/Hardware-FT4232HL%20VALIDATED-gold.svg)

A professional Windows service for centralized serial port pool management with intelligent FTDI discovery, advanced hardware validation, thread-safe pool management, and **validated multi-port device awareness**.

## 🎯 **Current Status - Sprint 4 MVP**

**✅ Sprint 3 COMPLETED WITH EXCELLENCE**
- Enterprise-grade thread-safe pool management ✅
- Multi-port device grouping (FT4232HL validated) ✅  
- Enhanced Discovery with FTDI intelligence ✅
- Windows Service with background discovery ✅
- 65+ comprehensive tests passing ✅

**🚀 Sprint 4 IN PROGRESS - MVP Client Ready**
- MSI installer package ✅ **COMPLETED**
- Enhanced Discovery in Service ✅ **FIXED & VALIDATED**
- JSON configuration system (in progress)
- Serial communication engine (planned)
- 3-phase workflow automation (planned)

## 🏭 **Hardware Validation Results**

Our multi-port device detection has been **successfully validated** with real industrial hardware:

### **✅ Validated Hardware Configuration:**
- **FT4232HL**: 4-port FTDI device (COM11, COM12, COM13, COM14) ✅
- **FT232R**: Single-port FTDI device (COM6) ✅

### **✅ Device Grouping Success:**
```
🔍 Found 2 physical device(s): ✅ PERFECT

🏭 ✅ 🔀 FTDI FT4232HL (4 ports)
   📍 Ports (4): COM11, COM12, COM13, COM14
   💎 Client Valid: YES (FT4232HL)
   📊 Utilization: 0% (0/4 allocated)

🏭 ❌ 📌 FTDI FT232R  
   📍 Ports (1): COM6
   💎 Client Valid: NO (Development only)
   📊 Utilization: 0% (0/1 allocated)
```

## 🚀 **Quick Installation**

### **Prerequisites**
- Windows 10/11 or Windows Server 2016+
- .NET 9.0 Runtime
- Administrator rights

### **Installation (Sprint 4)**
```powershell
# 1. Download and run MSI installer
SerialPortPool-Setup.msi

# 2. Service starts automatically
Get-Service SerialPortPoolService

# 3. Verify device detection
# Check logs: C:\Logs\SerialPortPool\
```

## 🔧 **Usage**

### **Enhanced Discovery Demo**
```bash
# Live hardware detection with device grouping
dotnet run --project tests/PortDiscoveryDemo/

# Expected output with FT4232HL:
# 🔍 Found 2 physical device(s):
# 🏭 ✅ 🔀 FTDI FT4232HL (4 ports) - COM11, COM12, COM13, COM14
# 🏭 ❌ 📌 FTDI FT232R (1 port) - COM6
```

### **Windows Service**
```powershell
# Service management
Start-Service SerialPortPoolService
Stop-Service SerialPortPoolService
Get-Service SerialPortPoolService

# Interactive mode for development
SerialPortPoolService.exe --console
```

## 🧪 **Testing Results**

### **Test Coverage Sprint 3 + 4**
![Tests](https://img.shields.io/badge/Total%20Tests-65%2B-brightgreen.svg)
![Hardware](https://img.shields.io/badge/Hardware%20Tests-VALIDATED-gold.svg)
![Integration](https://img.shields.io/badge/Service%20Integration-WORKING-brightgreen.svg)

```bash
# Run complete test suite
dotnet test SerialPortPoolService.sln --configuration Release

# Expected: 65+ tests passing
# ✅ Sprint 1: Windows Service (13 tests)
# ✅ Sprint 2: Enhanced Discovery (12 tests)  
# ✅ Sprint 3: Pool Management + Device Grouping (40+ tests)
# ✅ Sprint 4: MSI Installer + Service Integration (validated)
```

## 🏗️ **Architecture**

```
SerialPortPoolService/                    ← Windows Service + MSI Installer
├── installer/SerialPortPool-Setup.msi   ← Sprint 4: Professional installer ✅
├── Services/PortDiscoveryBackgroundService.cs ← Enhanced Discovery integration ✅
└── Program.cs                           ← Complete DI with device grouping ✅

SerialPortPool.Core/                     ← Enterprise-grade core library
├── Services/
│   ├── EnhancedSerialPortDiscoveryService.cs   ← Device grouping validated ✅
│   ├── SerialPortPool.cs                       ← Thread-safe pool ✅
│   ├── MultiPortDeviceAnalyzer.cs              ← Multi-port awareness ✅
│   └── SystemInfoCache.cs                      ← Smart caching ✅
├── Models/
│   ├── DeviceGroup.cs                   ← Multi-port device model ✅
│   ├── PortAllocation.cs               ← Pool management ✅
│   └── SystemInfo.cs                   ← EEPROM integration ✅
└── tests/ (65+ tests)                  ← Comprehensive validation ✅
```

## 🎯 **Sprint 4 Roadmap**

### **✅ Completed**
- MSI installer package with professional deployment
- Enhanced Discovery integration in Windows Service  
- Hardware validation with FT4232HL + FT232R
- Device grouping working in both Demo and Service

### **🚀 Next Milestones**
- JSON configuration system (BIB_ID, DUT_ID, serial parameters)
- Serial communication engine (3-phase workflow)
- Command/response framework with timeouts
- Complete MVP client package

## 📞 **Support**

- **Installation**: Run `SerialPortPool-Setup.msi` as Administrator
- **Configuration**: Service auto-starts, check `C:\Logs\SerialPortPool\`
- **Hardware**: Supports all FTDI devices (FT232R, FT4232H/HL, etc.)
- **Development**: Use `--console` mode for debugging

---

## 🚀 **Next: Sprint 4 MVP - Industrial Communication Ready!**

**Current Achievement:** Enterprise-grade foundation with validated multi-port device awareness  
**Next Target:** Complete MVP package for industrial serial communication  
**Hardware Support:** Professional FTDI devices validated and working

---

*Last updated: July 28, 2025 - Sprint 4 MSI + Service Integration*  
*Status: Production-ready foundation, MVP development in progress*  
*Hardware: FT4232HL (4-port) + FT232R (single) validated*