[![English](https://img.shields.io/badge/lang-English-blue.svg)](README.md) [![Français](https://img.shields.io/badge/lang-Français-blue.svg)](README.fr.md)

# SerialPortPoolService

![Build Status](https://github.com/linuxguy9691/SerialPortPoolService/workflows/Automated%20Tests%20-%20Sprint%2014/badge.svg)
![.NET](https://img.shields.io/badge/.NET-9.0-purple.svg)
![Platform](https://img.shields.io/badge/platform-Windows-blue.svg)
![License](https://img.shields.io/badge/license-MIT-green.svg)
![Sprint](https://img.shields.io/badge/Sprint%2014-COMPLETED-brightgreen.svg)
![Architecture](https://img.shields.io/badge/Architecture-PRODUCTION%20READY-gold.svg)
![Hardware](https://img.shields.io/badge/Hardware-FT4232HA%20VALIDATED-gold.svg)

A professional Windows service for centralized and secure serial port pool management, featuring **BitBang production mode**, **hot reload configuration**, **Multi-BIB orchestration**, and **robust logging infrastructure** for production test automation environments.

## 🎯 **Overview**

SerialPortPoolService is a production-ready solution that enables:
- 🏭 **BitBang Production Mode** - START→TEST(loop)→STOP simulation per UUT_ID with hardware trigger support
- 🔄 **Configuration Hot Reload** - Runtime BIB file addition without service restart
- 🎯 **Multi-BIB Orchestration** - Parallel execution of multiple Board Interface Box configurations
- 📊 **Robust Logging Infrastructure** - Fail-fast validation with structured BIB/UUT logging
- 🔌 **Real Hardware Integration** - FT4232HA support with dynamic EEPROM port mapping
- 🏗️ **Enterprise Windows Service** - Production deployment with comprehensive NLog integration

## 📋 **Project Status - SPRINT 14 PRODUCTION READY ✅**

### **✅ Sprint 1-11 - Enterprise Foundation** 
**Status:** 🎉 **COMPLETED AND VALIDATED**
- [x] Production Windows service with comprehensive NLog logging
- [x] Thread-safe pool with intelligent FTDI discovery
- [x] Production-ready RS232 communication
- [x] Multi-file XML configuration with complete BIB isolation
- [x] 4-level validation (PASS/WARN/FAIL/CRITICAL)
- [x] EEPROM integration for automatic BIB selection
- [x] Configuration hot reload with backup/rollback

### **🔥 Sprint 14 - BitBang Production Mode + Architecture Cleanup** 
**Status:** 🎉 **COMPLETED - PRODUCTION SIMULATION FUNCTIONAL**

#### **✅ BitBang Production Mode Implemented**
- [x] **MultiBibWorkflowService** - Production engine with per UUT_ID independent control
- [x] **BitBangProductionService** - START/STOP trigger simulation with configurable XML timing
- [x] **Production Pattern** - START-once → TEST(continuous loop) → STOP-once per UUT_ID
- [x] **Parallel Execution** - Multiple BIBs operate simultaneously and independently
- [x] **Persistent Sessions** - Connections maintained during continuous TEST loops

#### **✅ Hot Reload Configuration Restored**
- [x] **Sprint 11++ Consolidation** - Mature `HotReloadConfigurationService` reactivated
- [x] **Runtime BIB Addition** - New BIB files detected and executed automatically
- [x] **Zero Downtime** - Hot reload works during production execution
- [x] **Architecture Cleanup** - Obsolete Sprint 13 services removed, unified architecture

#### **✅ Robust Logging Infrastructure**
- [x] **Complete Startup Validation** - NLog.config and permissions verification with fail-fast
- [x] **Transparent Diagnostics** - Clear messages with exact paths and failure reasons
- [x] **Graceful Fallback** - BibUutLogger with fallback to main logs when issues occur
- [x] **Runtime Monitoring** - Disk/permission issue detection with periodic alerts

### **📊 Production Hardware Validation**
- ✅ **Real FT4232HL** - Serial communication validated on COM11-COM14
- ✅ **EEPROM Reading** - Automatic BIB detection via ProductDescription
- ✅ **Dynamic Port Mapping** - client_demo → COM11 association with reservation
- ✅ **BitBang Simulation** - Configurable START/STOP triggers via XML
- ✅ **Parallel Multi-BIB** - 2 BIBs executed simultaneously without conflicts

---

## 🚀 **Quick Start & Installation**

### **Prerequisites**
- **OS:** Windows 10/11 or Windows Server 2019+
- **Runtime:** .NET 9.0
- **Permissions:** Administrator rights for service installation
- **Hardware:** FTDI FT4232HA/HL device recommended

### **Installation**

```powershell
# 1. Clone and build
git clone https://github.com/your-repo/SerialPortPoolService.git
cd SerialPortPoolService
dotnet build --configuration Release

# 2. Test in interactive mode
cd SerialPortPoolService/bin/Release/net9.0-windows
.\SerialPortPoolService.exe --config-dir Configuration --discover-bibs --mode production
```

### **Production Mode (New in Sprint 14)**

```powershell
# Production mode with automatic BIB discovery
.\SerialPortPoolService.exe --config-dir Configuration --discover-bibs --mode production

# Expected output:
# 🏭 Production mode detected - using BIB file discovery only
# 📊 BIB client_demo: 1 UUTs detected for production
# 🔄 Starting continuous TEST loop for client_demo.production_uut
# ✅ TEST cycle #1 result: True
```

### **Hot Reload Configuration**

```powershell
# While service is running, add a new BIB file:
copy bib_new_test.xml Configuration\

# Automatic logs:
# 🆕 New configuration file detected: bib_new_test.xml (BIB: new_test)
# 🏭 Starting production workflow for changed BIB: new_test
# ✅ Hot-changed BIB task started: new_test
```

### **Windows Service Installation**

```powershell
# Create and start service
sc create SerialPortPoolService binPath="C:\Path\To\SerialPortPoolService.exe --mode production --config-dir Configuration"
sc start SerialPortPoolService

# Monitor via Event Viewer or logs
Get-EventLog -LogName Application -Source SerialPortPoolService -Newest 10
type C:\Logs\SerialPortPool\service-*.log
```

---

## 🏗️ **Architecture - Sprint 14 Production**

### **Project Structure**
```
SerialPortPoolService/                          ← Production Service
├── Services/
│   ├── ✅ MultiBibWorkflowService.cs          # Production mode per UUT_ID
│   └── ✅ BitBangProductionService.cs         # Hardware trigger simulation
├── Configuration/
│   ├── ✅ bib_client_demo.xml                 # Example BIB configuration
│   └── ✅ bib_client_demo_2.xml               # Additional BIB configuration
├── ✅ Program.cs                              # Robust logging validation
└── ✅ NLog.config                             # Production logging configuration
```

### **Core Services**
```
SerialPortPool.Core/
├── Services/
│   ├── Configuration/
│   │   └── ✅ HotReloadConfigurationService.cs # Mature hot reload (Sprint 11++)
│   ├── ✅ BibWorkflowOrchestrator.cs          # Enhanced orchestration
│   ├── ✅ XmlBibConfigurationLoader.cs        # Multi-file loading
│   ├── ✅ DynamicPortMappingService.cs        # Automatic EEPROM mapping
│   └── ✅ BibUutLogger.cs                     # Structured BIB/UUT logging
└── Models/
    ├── ✅ BibConfiguration.cs                 # BIB configuration with simulation
    ├── ✅ HardwareSimulationConfig.cs         # Hardware simulation configuration
    └── ✅ MultiBibWorkflowResult.cs           # Aggregated Multi-BIB results
```

---

## 🔧 **XML Configuration - Production Mode**

### **Individual BIB File with BitBang Simulation**

```xml
<?xml version="1.0" encoding="UTF-8"?>
<bib id="client_demo" description="Client Demo with BitBang Simulation">
  <!-- Hardware simulation configuration -->
  <hardware_simulation enabled="true" mode="Simulation">
    <start_trigger>
      <delay_seconds>0.5</delay_seconds>
    </start_trigger>
    <stop_trigger>
      <delay_seconds>20</delay_seconds>
    </stop_trigger>
    <critical_trigger enabled="false" />
    <speed_multiplier>2.0</speed_multiplier>
  </hardware_simulation>
  
  <uut id="production_uut" description="Production UUT">
    <port number="1">
      <protocol>rs232</protocol>
      <speed>115200</speed>
      <data_pattern>n81</data_pattern>
      
      <start>
        <command>ATZ\r\n</command>
        <expected_response>OK</expected_response>
        <timeout_ms>3000</timeout_ms>
      </start>
      
      <test>
        <command>INIT_RS232\r\n</command>
        <expected_response>READY</expected_response>
        <timeout_ms>3000</timeout_ms>
      </test>
      
      <test>
        <command>TEST\r\n</command>
        <expected_response>PASS</expected_response>
        <timeout_ms>3000</timeout_ms>
      </test>
      
      <stop>
        <command>EXIT\r\n</command>
        <expected_response>BYE</expected_response>
        <timeout_ms>2000</timeout_ms>
      </stop>
    </port>
  </uut>
</bib>
```

### **Command Line Options**

```bash
SerialPortPoolService.exe [OPTIONS]

Production Options:
  --mode production                 BitBang production mode (recommended)
  --config-dir <directory>          BIB files directory (default: Configuration/)
  --discover-bibs                   Auto-discover bib_*.xml files

Legacy Options:
  --xml-config <file>               Single XML file (legacy mode)
  --bib-ids <list>                  Specific BIB IDs (space-separated)
  --mode continuous                 Legacy continuous mode
  --interval <minutes>              Interval between cycles

Diagnostic Options:
  --detailed-logs                   Enable detailed logging
  --help                           Show help information
```

---

## 📊 **Logging and Monitoring**

### **Production Log Structure**

```
C:\Logs\SerialPortPool\
├── service-2025-09-25.log                    # Main service logs (NLog)
├── BIB_client_demo\                          # Structured logs by BIB
│   ├── 2025-09-25\
│   │   ├── production_uut_port1_1735.log     # Specific execution logs
│   │   └── daily_summary_2025-09-25.log      # Daily summary
│   └── latest\
│       └── production_uut_current.log        # Current execution marker
└── BIB_other_bib\
    └── ...                                   # Similar structure per BIB
```

### **Robust Logging Validation**

At startup, the service automatically validates:
```
🔧 Validating logging configuration...
📁 Checking NLog.config at: [exact path]
✅ NLog.config file found and readable
📁 Checking log directory: C:\Logs\SerialPortPool
✅ Log directory writable
✅ OPTIMAL: File logging + Console logging active
```

If logging issues detected:
```
❌ CRITICAL FAILURE: NO LOGGING AVAILABLE
🛑 SERVICE CANNOT START
💥 ISSUES:
   • NLog.config file not found at: [path]
   • Cannot write to log directory: Access denied
```

---

## 🧪 **Testing and Validation**

### **Sprint 14 Hardware Validation**

```powershell
# Complete production mode test with real hardware
.\SerialPortPoolService.exe --config-dir Configuration --discover-bibs --mode production

# Expected results:
# - FTDI detection: 5 ports (COM6, COM11-COM14)
# - EEPROM reading: client_demo A/B/C/D detected
# - Dynamic mapping: client_demo → COM11
# - Production workflow: START → TEST(loop) → STOP
# - Hot reload: New BIB files detected automatically
```

### **Validated Integration Tests**

- ✅ **Parallel Multi-BIB** - 2 BIBs simultaneously without resource conflicts
- ✅ **Runtime Hot Reload** - BIB file addition during production execution
- ✅ **Hardware Communication** - Real RS232 on FT4232HL validated
- ✅ **Robust Logging** - Fail-fast startup + graceful fallback
- ✅ **BitBang Simulation** - Configurable XML timing with continuous loops
- ✅ **EEPROM Port Mapping** - Automatic BIB_ID detection via ProductDescription

### **Identified Limitations**

- **BIB Mapping** - Requires EEPROM programmed with corresponding ProductDescription
- **Production Mode** - Optimized for simulation, physical GPIO hardware requires additional development
- **CLI Parsing** - Commas in `--bib-ids` require using spaces instead
- **Concurrent Limits** - Maximum simultaneous UUTs not established (requires load testing)

---

## 🎯 **Production Readiness Status**

### **✅ Production-Validated Features**
- **Windows Service** - Installation, startup, monitoring via Event Viewer
- **Multi-File Configuration** - Complete BIB isolation with hot reload
- **Serial Communication** - RS232 validated with FT4232HL hardware
- **BitBang Production Mode** - START→TEST(loop)→STOP pattern functional
- **Enterprise Logging** - NLog + structured BibUutLogger + fail-fast validation
- **Dynamic Port Mapping** - Automatic EEPROM-based BIB detection

### **⚠️ Requires Additional Validation**
- **Long-Term Stability** - Continuous TEST loops over hours/days
- **Error Recovery** - Graceful handling of communication failures
- **Load Performance** - Simultaneous UUT limits and resource usage
- **Physical GPIO Hardware** - Real hardware trigger integration

### **🔮 Future Development**
- **Real GPIO Hardware** - Physical BitBang trigger integration
- **REST API** - HTTP endpoints for external monitoring and control
- **Web Dashboard** - Real-time monitoring interface
- **Advanced Analytics** - Performance metrics and alerting

---

## 📞 **Support and Documentation**

### **Technical Documentation**
- 📖 **Installation Guide**: Complete Windows service instructions
- 🔧 **XML Configuration**: Complete BIB syntax reference
- 📊 **Architecture**: Sprint 14 conclusion with technical details
- 🧪 **Testing**: Hardware and integration validation procedures

### **Hardware Support**
- 🔌 **Supported FTDI**: FT232R, FT4232H/HL, FT232H with EEPROM reading
- 📡 **Communication**: Production-ready RS232 with timeout management
- 🎯 **Port Discovery**: Intelligent WMI + EEPROM with TTL cache
- 🔬 **BIB Detection**: Automatic ProductDescription to BIB_ID mapping

### **Deployment Support**
- 🏗️ **Windows Service**: Automated installation/uninstallation
- 📋 **Configuration**: Multi-file with automatic backup/rollback
- 📊 **Monitoring**: NLog + Event Viewer + structured logs
- 🔄 **Hot Reload**: Runtime configuration without service restart

---

## 🚀 **Project Evolution - Sprint 15+**

### **Next Priorities**
- **Stability Testing** - Long-term validation and error handling
- **Performance Optimization** - Load limits and resource monitoring
- **User Documentation** - Operational guides and troubleshooting
- **Hardware GPIO** - Physical production trigger integration

### **Long-Term Vision**
SerialPortPoolService is evolving toward an **enterprise test automation platform** featuring:
- Advanced parallel Multi-BIB orchestration
- Complete physical GPIO hardware integration
- REST API and real-time monitoring dashboard
- Predictive analytics and intelligent alerting

---

## 🎉 **Sprint 14 Conclusion**

**Sprint 14** represents a **major milestone** toward production readiness with:

**🏭 BitBang Production Mode** - Production START→TEST(loop)→STOP pattern implemented and validated  
**🔄 Hot Reload Restored** - Runtime configuration without interruption functional  
**📊 Robust Logging** - Fail-fast validation with transparent monitoring  
**🏗️ Mature Architecture** - Service consolidation with complete cleanup  

The **SerialPortPoolService** system is now **ready for production deployment** with complete BitBang simulation and enterprise-grade logging infrastructure. Advanced features (physical GPIO hardware, REST API) represent the natural next evolution of this solid foundation.

---

*Last Updated: September 2025 - Sprint 14 Production BitBang + Hot Reload*  
*Current Status: Production Ready with BitBang Mode + Configuration Hot Reload*  
*Version: 14.0.0 - Production Service with Hardware Simulation + Robust Logging*  
*Hardware Validated: FTDI FT4232HL with RS232 communication + EEPROM reading*  
*Ready For: Production deployment + long-term stability validation*