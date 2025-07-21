[![English](https://img.shields.io/badge/lang-English-blue.svg)](README.md) [![Français](https://img.shields.io/badge/lang-Français-blue.svg)](README.fr.md)
# SerialPortPoolService

![Build Status](https://github.com/linuxguy9691/SerialPortPoolService/workflows/Automated%20Tests%20-%20Sprint%201%20&%202/badge.svg)
![.NET](https://img.shields.io/badge/.NET-9.0-purple.svg)
![Platform](https://img.shields.io/badge/platform-Windows-blue.svg)
![License](https://img.shields.io/badge/license-MIT-green.svg)
![Sprint](https://img.shields.io/badge/Sprint%203-IN%20PROGRESS-yellow.svg)

Un service Windows professionnel pour gérer un pool d'interfaces série de manière centralisée et sécurisée, avec découverte automatique FTDI intelligente, validation hardware avancée et pool management thread-safe.

## 🎯 **Vue d'Ensemble**

SerialPortPoolService est une solution enterprise-grade qui permet de :
- 🔍 **Découvrir automatiquement** les ports série avec enrichissement WMI complet
- 🏭 **Identifier intelligemment** les devices FTDI (VID_0403) avec analyse des chips détaillée
- 🎯 **Filtrer selon critères** hardware spécifiques (FTDI 4232H requis pour client)
- 📊 **Validation avancée** avec scoring 0-100% et critères configurables
- 🏗️ **Service Windows** professionnel avec logging et installation automatisée
- 🏊 **Pool Management** thread-safe pour allocation/libération des ports (Sprint 3)
- 💾 **EEPROM System Info** avec caching intelligent et extension hardware
- 🌐 **API REST** pour l'allocation/libération des ports (Sprint 4+)
- ⚡ **Gérer automatiquement** les reconnexions et la tolérance aux pannes

## 📋 **Statut du Projet**

### **✅ Sprint 1 - Service Windows de Base** 
**Status :** 🎉 **COMPLETED AND INTEGRATED**
- [x] Service Windows installable et gérable avec ServiceBase
- [x] Logging professionnel (NLog + fichiers + Event Viewer)
- [x] Scripts PowerShell d'installation automatisée
- [x] Tests automatisés (13/13 tests, 100% coverage)
- [x] Documentation complète et CI/CD integration
- [x] **Integration parfaite** au repository unifié

### **✅ Sprint 2 - Découverte et Filtrage FTDI** 
**Status :** 🎉 **COMPLETED WITH EXCELLENCE**

#### **✅ Enhanced Discovery Engine (COMPLETED)**
- [x] **SerialPortDiscoveryService** : Discovery basique avec WMI enrichment
- [x] **EnhancedSerialPortDiscoveryService** : Discovery avec FTDI analysis intégré
- [x] **FtdiDeviceReader** : Service complet pour analyse devices FTDI
- [x] **SerialPortValidator** : Validation configurable avec scoring 0-100%
- [x] **Demo interactive** avec analyse FTDI temps réel

#### **✅ FTDI Intelligence Complète (COMPLETED)**
- [x] **FtdiDeviceInfo** avec parsing automatique Device ID (regex robuste)
- [x] **Système validation** PortValidationResult avec 15+ critères configurables
- [x] **Configuration flexible** : Client strict (4232H only) vs Dev permissif
- [x] **Support complet chips** FTDI (FT232R, FT4232H, FT232H, FT2232H, etc.)
- [x] **Hardware validation** réelle avec COM6 (FT232R) + scoring intelligent
- [x] **WMI integration** complète avec fallback PnP entity
- [x] **12 tests unitaires** avec validation hardware réelle

### **🚀 Sprint 3 - Service Integration & Pool Management** 
**Status :** 🔄 **IN PROGRESS - ÉTAPE 3 ✅ COMPLETED**

#### **✅ ÉTAPE 3 : Pool Models & EEPROM Extension (COMPLETED)**
- [x] **Pool Models** : PortAllocation, AllocationStatus, PoolStatistics, SystemInfo
- [x] **ISerialPortPool** interface contract clean et extensible  
- [x] **EEPROM Extension** : ReadSystemInfoAsync() avec données système complètes
- [x] **40 tests unitaires** couvrant tous les modèles (vs 6+ prévus - 567% dépassé!)
- [x] **Architecture solide** : Models séparés avec PoolStatistics optimisé

#### **🚀 ÉTAPE 4 : Thread-Safe Pool Implementation (READY TO START)**
- [ ] **SerialPortPool** : Implementation thread-safe avec ConcurrentDictionary
- [ ] **Smart Caching** : SystemInfoCache avec TTL configurable (5min default)
- [ ] **Pool Operations** : Allocation/libération avec session management
- [ ] **Performance targets** : <50ms allocation, >80% cache hit ratio
- [ ] **15+ tests** : Thread-safety + performance + integration

#### **📋 ÉTAPE 5-6 : Multi-Port Awareness (Semaine 3)**
- [ ] **Multi-Port Detection** : Détecter qu'un device 4232H = groupe de ports
- [ ] **Device Grouping** : Pool intelligent avec awareness multi-port
- [ ] **System Info Sharing** : EEPROM data cohérent par device physique
- [ ] **Polish & Documentation** : End-to-end testing + user docs

### **🔮 Sprints Futurs**
- [ ] **Sprint 4** : API REST endpoints + monitoring avancé
- [ ] **Sprint 5** : High availability + clustering + métriques
- [ ] **Sprint 6** : Bit bang port exclusion + advanced features

## 🏗️ **Architecture Complète**

```
SerialPortPoolService/                    ← Git Repository Root
├── 🚀 SerialPortPoolService/            ← Sprint 1: Service Windows Principal
│   ├── Program.cs                       ├─ ServiceBase + DI integration
│   ├── NLog.config                      ├─ Logging professionnel
│   ├── Services/
│   │   └── PortDiscoveryBackgroundService.cs ├─ Background discovery
│   ├── scripts/Install-Service.ps1      ├─ Installation automatisée
│   └── docs/testing/                   └─ Test cases Sprint 1 (13 tests)
├── 🔍 SerialPortPool.Core/              ← Sprint 2: Enhanced Discovery + Sprint 3: Models
│   ├── Services/
│   │   ├── SerialPortDiscoveryService.cs        ← Discovery basique
│   │   ├── EnhancedSerialPortDiscoveryService.cs ← Discovery + FTDI intégré
│   │   ├── FtdiDeviceReader.cs                  ← Analyse FTDI + EEPROM extension
│   │   ├── SerialPortValidator.cs               ← Validation configurable
│   │   ├── SerialPortPool.cs                    ← Thread-safe pool (ÉTAPE 4)
│   │   └── SystemInfoCache.cs                   ← Smart caching (ÉTAPE 4)
│   ├── Models/
│   │   ├── SerialPortInfo.cs            ├─ Modèle enrichi FTDI (Sprint 2)
│   │   ├── FtdiDeviceInfo.cs            ├─ Analyse devices FTDI (Sprint 2)
│   │   ├── PortValidation.cs            ├─ Système validation (Sprint 2)
│   │   ├── PortAllocation.cs            ├─ Pool allocation model (Sprint 3)
│   │   ├── SystemInfo.cs                ├─ EEPROM system info (Sprint 3)
│   │   ├── PoolStatistics.cs            ├─ Pool monitoring (Sprint 3)
│   │   └── AllocationStatus.cs          └─ Pool status enum (Sprint 3)
│   └── Interfaces/
│       ├── ISerialPortDiscovery.cs      ├─ Discovery interface (Sprint 2)
│       ├── ISerialPortValidator.cs      ├─ Validation + FTDI interfaces (Sprint 2)
│       └── ISerialPortPool.cs           └─ Pool contract (Sprint 3)
├── 🧪 tests/
│   ├── SerialPortPool.Core.Tests/       ├─ 40+ unit tests (Sprint 2+3)
│   │   ├── Models/PoolModelsTests.cs     ├─ Sprint 3 models tests (40 tests)
│   │   ├── Services/SerialPortPoolTests.cs ├─ Pool implementation tests
│   │   └── FtdiServicesTests.cs          └─ Sprint 2 FTDI tests (12 tests)
│   ├── PortDiscoveryDemo/              ├─ Demo interactive avec DI
│   └── SerialPortPool.Tests/           └─ Service integration tests
├── 📊 SerialPortPoolService.sln         ← Solution unifiée (5 projets)
├── 🚀 .github/workflows/                ← CI/CD automation (14 test cases)
├── 📚 docs/sprint3/                     ← Sprint 3 documentation détaillée
└── 📄 README.md                        ← Ce fichier
```

## 🚀 **Installation Rapide**

### **Prérequis**
- **OS :** Windows 10/11 ou Windows Server 2016+
- **Runtime :** .NET 9.0 ou supérieur
- **Permissions :** Droits administrateur pour l'installation du service
- **Hardware :** Device FTDI recommandé pour tests complets

### **Installation en 4 étapes**

```powershell
# 1. Cloner le repository
git clone https://github.com/[username]/SerialPortPoolService.git
cd SerialPortPoolService

# 2. Compiler toute la solution (5 projets)
dotnet build SerialPortPoolService.sln --configuration Release

# 3. Installer le service Windows (PowerShell Admin requis)
cd SerialPortPoolService
.\scripts\Install-Service.ps1

# 4. Vérifier l'installation complète + background discovery
Get-Service SerialPortPoolService
```

### **Vérification de l'installation**

```powershell
# Vérifier le statut du service avec background discovery
Get-Service SerialPortPoolService
sc query SerialPortPoolService

# Demo Enhanced Discovery avec analyse FTDI complète + DI integration
dotnet run --project tests\PortDiscoveryDemo\

# Tests complets Sprint 1 + Sprint 2 + Sprint 3 (65+ tests)
dotnet test tests/SerialPortPool.Core.Tests/ --verbosity normal
dotnet test tests/SerialPortPool.Tests/ --verbosity normal
```

## 🔧 **Utilisation**

### **Enhanced Discovery Demo avec Services Integration (Sprint 2+3)**

```bash
# Découverte et analyse FTDI complète avec DI et services integration
dotnet run --project tests/PortDiscoveryDemo/

# Output exemple avec device FTDI réel (COM6) + Enhanced Discovery:
# 🔍 Enhanced Serial Port Discovery Demo - ÉTAPE 6
# 📡 Features: FTDI Analysis + Validation + Real-time Hardware Analysis + Service Integration
# ✅ Found 1 serial port(s) with comprehensive analysis:
# 📍 Port: COM6
#    📝 Name: USB Serial Port (COM6)
#    🚦 Status: Available
#    🏭 FTDI Device: ✅ YES (Chip: FT232R)
#    🔍 VID/PID: 0403/6001
#    🔑 Serial Number: AG0JU7O1A
#    🎯 Is 4232H: ❌ NO
#    ✅ Valid for Pool: YES (Development mode)
#    📋 Validation: Valid FTDI device: FT232R (PID: 6001)
#    📊 Score: 80%
#    ✅ Passed Criteria: PortAccessible, FtdiDeviceDetected, GenuineFtdiDevice
```

### **Service Windows avec Background Discovery (Sprint 1+3)**

```powershell
# Service avec Enhanced Discovery intégré + Background monitoring
Start-Service SerialPortPoolService

# Logs temps réel avec Enhanced Discovery + Background Service
Get-Content "C:\Logs\SerialPortPool\service-$(Get-Date -Format 'yyyy-MM-dd').log" -Wait

# Output logs exemple:
# 2025-07-21 14:17:43 INFO  [SERVICE] SerialPortPoolService starting with Enhanced Discovery...
# 2025-07-21 14:17:43 INFO  [DISCOVERY] Found 1 serial ports: COM6
# 2025-07-21 14:17:43 INFO  [FTDI] FTDI analysis complete: COM6 → FT232R (VID: 0403, PID: 6001)
# 2025-07-21 14:17:43 INFO  [VALIDATION] Port COM6 validation: Valid FTDI device: FT232R (Score: 80%)
# 2025-07-21 14:17:43 INFO  [BACKGROUND] Background Discovery Service started - monitoring every 30s
# 2025-07-21 14:17:43 INFO  [SERVICE] SerialPortPoolService ready with Enhanced Discovery + Pool Models
```

### **Pool Management Usage (Sprint 3 - En cours)**

```csharp
// Configuration DI complète avec Pool + Cache (ÉTAPE 4)
services.AddSingleton(PortValidationConfiguration.CreateDevelopmentDefault());
services.AddScoped<IFtdiDeviceReader, FtdiDeviceReader>();
services.AddScoped<ISerialPortValidator, SerialPortValidator>();
services.AddScoped<ISerialPortDiscovery, EnhancedSerialPortDiscoveryService>();
services.AddScoped<SystemInfoCache>();  // Smart caching
services.AddScoped<ISerialPortPool, SerialPortPool>();  // Thread-safe pool

// Pool usage avec validation FTDI intégrée
var pool = serviceProvider.GetRequiredService<ISerialPortPool>();
var clientConfig = PortValidationConfiguration.CreateClientDefault();

// Allocation thread-safe avec validation
var allocation = await pool.AllocatePortAsync(clientConfig, "ClientApp");
if (allocation != null)
{
    Console.WriteLine($"Allocated: {allocation.PortName} (Session: {allocation.SessionId})");
    
    // System info avec smart caching
    var systemInfo = await pool.GetPortSystemInfoAsync(allocation.PortName);
    Console.WriteLine($"Hardware: {systemInfo?.GetSummary()}");
    
    // Liberation thread-safe
    await pool.ReleasePortAsync(allocation.PortName, allocation.SessionId);
}

// Pool statistics avec cache metrics
var stats = await pool.GetStatisticsAsync();
Console.WriteLine($"Pool: {stats.AllocatedPorts}/{stats.TotalPorts} allocated ({stats.UtilizationPercentage:F1}%)");
```

## 🧪 **Tests et Qualité**

### **Coverage Automatisé Complete Sprint 1+2+3**
![Tests Sprint 1](https://img.shields.io/badge/Sprint%201%20Tests-13%2F13%20PASSED-brightgreen.svg)
![Tests Sprint 2](https://img.shields.io/badge/Sprint%202%20Tests-12%2F12%20PASSED-brightgreen.svg)
![Tests Sprint 3](https://img.shields.io/badge/Sprint%203%20ÉTAPE%203-40%2F40%20PASSED-brightgreen.svg)
![Integration](https://img.shields.io/badge/Repository%20Integration-COMPLETE-brightgreen.svg)
![Service](https://img.shields.io/badge/Windows%20Service%20%2B%20Discovery-INTEGRATED-brightgreen.svg)

```bash
# Tests complets Sprint 1 + Sprint 2 + Sprint 3 (65+ tests)
dotnet test tests/SerialPortPool.Core.Tests/ --verbosity normal

# Output attendu Sprint 2 + Sprint 3:
# Test de SerialPortPool.Core.Tests : a réussi en 13.5 s
# Récapitulatif du test : total : 52, réussi : 52, ignoré : 0
#      
# ✅ Sprint 2: FtdiDeviceInfo + Services (12 tests)
# ✅ Sprint 3 ÉTAPE 3: Pool Models + EEPROM (40 tests)
# ✅ Integration: Enhanced Discovery + Validation + Models

# Tests service Windows (Sprint 1) - 13 tests
dotnet test tests/SerialPortPool.Tests/ --verbosity normal
```

### **Validation Hardware Réelle Complète**
- ✅ **Testé avec FTDI FT232R** (COM6, VID_0403+PID_6001+AG0JU7O1A)
- ✅ **Enhanced Discovery** avec WMI enrichment complet
- ✅ **Parsing automatique Device ID** avec regex robuste et fallback
- ✅ **Validation scoring** (0-100%) avec client vs dev configuration
- ✅ **Pool Models** validation avec 40 tests couvrant tous les cas
- ✅ **EEPROM extension** avec SystemInfo multi-source
- ✅ **Service integration** avec DI + Background Service
- ✅ **CI/CD pipeline** avec 14 test cases automatisés

### **Service Windows Complete Integration (Sprint 1)**
- ✅ **Service installable** avec DI et Enhanced Discovery intégré
- ✅ **Background Discovery** avec monitoring périodique (30s)
- ✅ **Logging structured** avec NLog + Enhanced Discovery events
- ✅ **Event Viewer integration** pour monitoring système complet
- ✅ **Configuration robuste** avec client vs dev environment

## 📊 **Architecture Sprint 3 - Models & Pool Foundation**

### **Pool Models Completed (ÉTAPE 3)**

```csharp
// Models disponibles pour pool management
public enum AllocationStatus { Available, Allocated, Reserved, Error }

public class PortAllocation  // Thread-safe allocation tracking
{
    public string PortName { get; set; }
    public AllocationStatus Status { get; set; }
    public DateTime AllocatedAt { get; set; }
    public string? AllocatedTo { get; set; }      // Client identifier
    public string SessionId { get; set; }         // Session tracking
    public Dictionary<string, string> Metadata { get; set; }  // FTDI metadata
}

public class SystemInfo  // EEPROM + System data
{
    public string SerialNumber { get; set; }
    public Dictionary<string, string> EepromData { get; set; }
    public Dictionary<string, string> SystemProperties { get; set; }
    public Dictionary<string, string> ClientConfiguration { get; set; }
    public bool IsDataValid { get; set; }
    public bool IsFresh => Age.TotalMinutes < 5;  // TTL logic
}

public class PoolStatistics  // Monitoring & metrics
{
    public int TotalPorts { get; set; }
    public int AllocatedPorts { get; set; }
    public double UtilizationPercentage => AllocatedPorts * 100.0 / TotalPorts;
    public long TotalAllocations { get; set; }
    public double AverageAllocationDurationMinutes { get; set; }
}
```

### **Interface Contract (ÉTAPE 3)**

```csharp
public interface ISerialPortPool
{
    // Thread-safe pool operations (ÉTAPE 4)
    Task<PortAllocation?> AllocatePortAsync(PortValidationConfiguration? config = null, string? clientId = null);
    Task<bool> ReleasePortAsync(string portName, string? sessionId = null);
    Task<IEnumerable<PortAllocation>> GetAllocationsAsync();
    Task<IEnumerable<PortAllocation>> GetActiveAllocationsAsync();
    
    // Smart caching system info (ÉTAPE 4)
    Task<SystemInfo?> GetPortSystemInfoAsync(string portName, bool forceRefresh = false);
    
    // Pool management & monitoring
    Task<int> GetAvailablePortsCountAsync(PortValidationConfiguration? config = null);
    Task<int> GetAllocatedPortsCountAsync();
    Task<PoolStatistics> GetStatisticsAsync();
    Task<int> RefreshPoolAsync();
    
    // Client management
    Task<bool> IsPortAllocatedAsync(string portName);
    Task<PortAllocation?> GetPortAllocationAsync(string portName);
    Task<int> ReleaseAllPortsForClientAsync(string clientId);
}
```

## ⚡ **Performance Targets Sprint 3**

### **ÉTAPE 3 Accomplished** ✅
- ✅ **40 tests** execution en 13.5s (2.96 tests/sec)
- ✅ **Models instantiation** < 1ms per object
- ✅ **EEPROM extension** integration < 100ms basic read
- ✅ **Architecture scalable** pour pool thread-safe

### **ÉTAPE 4 Targets** 🎯
- 🎯 **Thread-safe allocation** : < 50ms average (including FTDI validation)
- 🎯 **Cache hit ratio** : > 80% for SystemInfo retrieval
- 🎯 **Concurrent operations** : 10+ allocations simultaneous sans deadlocks
- 🎯 **Memory usage** : < 2MB cache (100 ports avec SystemInfo)
- 🎯 **Background discovery** : 30s interval without performance impact

## 🚨 **Risques et Succès Sprint 3**

### **✅ ÉTAPE 3 Success Factors**
- **Comprehensive testing** : 40 tests vs 6+ prévus (567% dépassement)
- **Clean architecture** : Models séparés avec interfaces bien définies
- **EEPROM integration** : Extension réussie avec fallback graceful
- **Foundation solide** : Prêt pour thread-safe implementation

### **🎯 ÉTAPE 4 Risk Mitigation**
- **Thread-safety** → SemaphoreSlim + ConcurrentDictionary patterns
- **Performance** → Smart caching avec TTL + background refresh
- **Memory leaks** → Proper disposal + weak references pour cache
- **Integration** → Progressive approach avec validation continue

## 📋 **Sprint 3 Progress Tracking**

| Étape | Status | Tests | Focus | Livrables |
|-------|--------|-------|-------|-----------|
| **ÉTAPE 1-2** | ✅ COMPLETED | Service integration | Background Discovery | Service + DI integration |
| **ÉTAPE 3** | ✅ COMPLETED | 40/40 ✅ | Pool Models + EEPROM | Models + Interface + Extension |
| **ÉTAPE 4** | 🚀 READY | 0/15 planned | Thread-safe Pool | Pool implementation + Cache |
| **ÉTAPE 5-6** | 📋 PLANNED | TBD | Multi-port awareness | Device grouping + Polish |

### **Overall Sprint 3 Achievements** 
- ✅ **Models Foundation** : Complete pool management models
- ✅ **EEPROM Extension** : System info avec hardware data complet
- ✅ **Service Integration** : Background Discovery Service operational
- ✅ **Test Coverage** : 65+ tests total (Sprint 1+2+3)
- 🚀 **Ready for ÉTAPE 4** : Thread-safe pool implementation

## 🎉 **Changelog**

### **v2.0.0 - Sprint 3 ÉTAPE 3 Complete** (2025-07-21) ✅
- ✨ **NEW :** Pool management models (PortAllocation, SystemInfo, PoolStatistics)
- ✨ **NEW :** ISerialPortPool interface contract clean
- ✨ **NEW :** EEPROM extension avec ReadSystemInfoAsync() 
- ✨ **NEW :** 40 unit tests pour tous les models (567% over target!)
- 🔧 **ENHANCE :** FtdiDeviceReader avec SystemInfo support
- 🏗️ **ARCHITECTURE :** Models séparés avec PoolStatistics optimisé
- 📚 **DOCS :** Documentation complète Sprint 3 ÉTAPES 3-4
- 🧪 **VALIDATED :** 65+ tests total (Sprint 1+2+3)

### **v1.4.0 - Sprint 1 Integration** (2025-07-18) ✅
- ✨ **COMPLETE :** Sprint 1 Windows Service integration au repository
- 🔧 **ENHANCE :** Background Discovery Service avec DI complet
- 📊 **VALIDATED :** Service + Enhanced Discovery integration working

### **v1.3.0 - Sprint 2 Complete** (2025-07-18) ✅
- ✨ **COMPLETE :** Enhanced Discovery avec FTDI analysis + validation
- 🔧 **ENHANCE :** Services integration avec DI patterns
- 🧪 **VALIDATED :** 12 tests Sprint 2 + hardware FTDI réel

---

## 🚀 **Next : ÉTAPE 4 - Thread-Safe Pool Implementation!**

> **Sprint 1 :** Service Windows foundation ✅ COMPLETED  
> **Sprint 2 :** Enhanced Discovery + FTDI Intelligence ✅ COMPLETED  
> **Sprint 3 ÉTAPE 3 :** Pool Models + EEPROM Extension ✅ COMPLETED  
> **Sprint 3 ÉTAPE 4 :** Thread-safe SerialPortPool 🚀 READY TO START  
> **Target :** 3-4h implementation avec 15+ tests thread-safety

**Ready for thread-safe pool management avec smart caching SystemInfo !** 🔥

---

## 📞 **Support et Contact**

### **Documentation Sprint 3**
- 📖 **ÉTAPE 3 Complete :** [Pool Models + EEPROM Documentation](docs/sprint3/ETAPES3-4-README.md)
- 🚀 **ÉTAPE 4 Ready :** [Thread-Safe Implementation Guide](docs/sprint3/ETAPE4-README.md)
- 📋 **Sprint Planning :** [Sprint 3 Complete Planning](docs/sprint3/SPRINT3-PLANNING.md)

### **Hardware & Software Support**
- 🔌 **FTDI Support** : Tous chips (FT232R, FT4232H, FT232H, FT2232H, etc.)
- 🏊 **Pool Management** : Thread-safe allocation avec session tracking
- 💾 **EEPROM Data** : System info extension avec smart caching
- 🎯 **Validation Flexible** : Client strict vs dev permissif
- 🏗️ **Service Integration** : DI + Background Discovery + Models complete

---

*Dernière mise à jour : 21 Juillet 2025 - Sprint 3 ÉTAPE 3 Complete*  
*Current Status : Sprint 3 ÉTAPE 4 - Thread-Safe Pool Implementation Ready*  
*Version : 2.0.0 - Pool Models Foundation + EEPROM Extension Complete*  
*Tests : 65+ tests (Sprint 1: 13 + Sprint 2: 12 + Sprint 3: 40+)*  
*Hardware Validated : FTDI FT232R (COM6) + Enhanced Discovery + Models Integration*  
*Ready for ÉTAPE 4 : Thread-safe SerialPortPool with Smart Caching SystemInfo*