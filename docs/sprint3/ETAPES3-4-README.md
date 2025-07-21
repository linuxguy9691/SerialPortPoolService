# Sprint 3 - ÉTAPES 3-4 : Pool Management & EEPROM System Info

![Sprint](https://img.shields.io/badge/Sprint%203-IN%20PROGRESS-yellow.svg)
![Etape](https://img.shields.io/badge/ÉTAPE%203--4-Semaine%202-blue.svg)
![Status](https://img.shields.io/badge/Status-READY%20TO%20START-green.svg)

## 🎯 **Vue d'Ensemble**

Cette documentation couvre les **ÉTAPES 3-4** du Sprint 3, focalisées sur la création des **modèles de pool management** et de l'**implémentation thread-safe** avec extension EEPROM.

### **Objectif Global**
- ✅ **ÉTAPE 3** : Foundation models + interfaces + EEPROM extension basique
- ✅ **ÉTAPE 4** : Implémentation complète thread-safe avec smart caching

---

## 🏗️ **ÉTAPE 3 : Pool Models & EEPROM Extension**

**Durée Estimée :** 1h30  
**Complexité :** Simple - Modèles et contracts seulement  
**Prérequis :** Sprint 1 + Sprint 2 (Enhanced Discovery) completed

### **📋 Scope ÉTAPE 3**

#### **1. Modèles de Base (30 min)**
```csharp
// SerialPortPool.Core/Models/AllocationStatus.cs
public enum AllocationStatus 
{
    Available,    // Port libre et prêt à être alloué
    Allocated,    // Port actuellement alloué à un client
    Reserved,     // Port réservé temporairement
    Error         // Port en état d'erreur
}

// SerialPortPool.Core/Models/PortAllocation.cs
public class PortAllocation 
{
    public string PortName { get; set; } = string.Empty;
    public AllocationStatus Status { get; set; }
    public DateTime AllocatedAt { get; set; }
    public DateTime? ReleasedAt { get; set; }
    public string? AllocatedTo { get; set; }    // Client identifier
    public string? SessionId { get; set; }      // Session tracking
    public TimeSpan? AllocationDuration { get; } // Computed property
}

// SerialPortPool.Core/Models/SystemInfo.cs  
public class SystemInfo
{
    public string SerialNumber { get; set; } = string.Empty;
    public string FirmwareVersion { get; set; } = string.Empty;
    public string HardwareRevision { get; set; } = string.Empty;
    public Dictionary<string, string> EepromData { get; set; } = new();
    public Dictionary<string, string> SystemProperties { get; set; } = new();
    public DateTime LastRead { get; set; } = DateTime.Now;
}
```

#### **2. Interface Contract (15 min)**
```csharp
// SerialPortPool.Core/Interfaces/ISerialPortPool.cs
public interface ISerialPortPool
{
    /// <summary>
    /// Allocate a port from the pool with optional validation config
    /// </summary>
    Task<PortAllocation?> AllocatePortAsync(PortValidationConfiguration? config = null);
    
    /// <summary>
    /// Release a port back to the pool
    /// </summary>
    Task<bool> ReleasePortAsync(string portName, string? sessionId = null);
    
    /// <summary>
    /// Get all current port allocations
    /// </summary>
    Task<IEnumerable<PortAllocation>> GetAllocationsAsync();
    
    /// <summary>
    /// Get system information for a specific port
    /// </summary>
    Task<SystemInfo?> GetPortSystemInfoAsync(string portName);
    
    /// <summary>
    /// Get available ports count
    /// </summary>
    Task<int> GetAvailablePortsCountAsync();
}
```

#### **3. EEPROM Extension (30 min)**
```csharp
// Extension de SerialPortPool.Core/Services/FtdiDeviceReader.cs
public async Task<SystemInfo?> ReadSystemInfoAsync(string portName)
{
    // Lecture EEPROM basique + system properties
    // Parsing données hardware spécifiques client
    // Fallback graceful si lecture échoue
}
```

#### **4. Tests Unitaires (15 min)**
```csharp
// tests/SerialPortPool.Core.Tests/Models/PoolModelsTests.cs
// Tests de validation pour tous les nouveaux modèles
// Tests contract interfaces
// Tests EEPROM extension basique
```

### **🎯 Livrables ÉTAPE 3**
- [ ] `AllocationStatus` enum avec valeurs appropriées
- [ ] `PortAllocation` modèle complet avec propriétés calculées  
- [ ] `SystemInfo` modèle avec EEPROM data structure
- [ ] `ISerialPortPool` interface contract clean
- [ ] Extension `ReadSystemInfoAsync()` dans `FtdiDeviceReader`
- [ ] 6+ tests unitaires couvrant les modèles
- [ ] Documentation inline des nouveaux modèles

---

## ⚙️ **ÉTAPE 4 : Pool Implementation avec System Info**

**Durée Estimée :** 3h  
**Complexité :** Moyenne - Thread-safety + performance  
**Dépendances :** ÉTAPE 3 complète

### **📋 Scope ÉTAPE 4**

#### **1. Pool Implementation Thread-Safe (90 min)**
```csharp
// SerialPortPool.Core/Services/SerialPortPool.cs
public class SerialPortPool : ISerialPortPool
{
    private readonly ConcurrentDictionary<string, PortAllocation> _allocations = new();
    private readonly SemaphoreSlim _allocationSemaphore = new(1, 1);
    private readonly ILogger<SerialPortPool> _logger;
    private readonly ISerialPortDiscovery _discovery;
    private readonly ISerialPortValidator _validator;
    
    // Thread-safe allocation logic
    // Proper resource cleanup
    // Error handling & recovery
    // Session management
}
```

#### **2. Smart Caching System (60 min)**
```csharp
// SerialPortPool.Core/Services/SystemInfoCache.cs
public class SystemInfoCache
{
    private readonly ConcurrentDictionary<string, CacheEntry> _cache = new();
    private readonly TimeSpan _defaultTtl = TimeSpan.FromMinutes(5);
    
    public async Task<SystemInfo?> GetSystemInfoAsync(string portName, bool forceRefresh = false)
    {
        // Smart caching avec TTL configurable
        // Background refresh pour ports actifs
        // Memory management et cleanup
    }
    
    private class CacheEntry
    {
        public SystemInfo SystemInfo { get; set; }
        public DateTime ExpiresAt { get; set; }
        public bool IsRefreshing { get; set; }
    }
}
```

#### **3. Integration EEPROM + Pool (30 min)**
```csharp
// Integration SystemInfo dans allocation process
// Validation hardware lors allocation
// Caching intelligent des données EEPROM
// Performance monitoring
```

#### **4. Tests Integration (20 min)**
```csharp
// tests/SerialPortPool.Core.Tests/Services/SerialPortPoolTests.cs
// Tests thread-safety (concurrent allocations)
// Tests caching behavior
// Tests EEPROM integration
// Tests error scenarios & recovery
```

### **🔧 Fonctionnalités Avancées ÉTAPE 4**

#### **Thread-Safety Features**
- ✅ **ConcurrentDictionary** pour allocations thread-safe
- ✅ **SemaphoreSlim** pour allocation atomique  
- ✅ **Proper disposal** des ressources
- ✅ **Deadlock prevention** strategies

#### **Performance Features**  
- ✅ **Smart caching** avec TTL configurable (5min default)
- ✅ **Background refresh** pour ports actifs seulement
- ✅ **Lazy loading** des données EEPROM
- ✅ **Memory pressure management**

#### **Monitoring & Diagnostics**
- ✅ **Structured logging** avec correlation IDs
- ✅ **Performance counters** (allocation time, cache hit rate)
- ✅ **Health checks** pour pool state
- ✅ **Error tracking** et recovery metrics

### **🎯 Livrables ÉTAPE 4**
- [ ] `SerialPortPool` implémentation complète thread-safe
- [ ] `SystemInfoCache` avec smart caching
- [ ] Integration EEPROM dans allocation process
- [ ] 12+ tests couvrant thread-safety et performance
- [ ] Logging structuré avec correlation
- [ ] Documentation performance et threading model

---

## 📊 **Architecture Complète ÉTAPES 3-4**

```
SerialPortPool.Core/
├── Models/
│   ├── AllocationStatus.cs          ← ÉTAPE 3
│   ├── PortAllocation.cs            ← ÉTAPE 3
│   └── SystemInfo.cs                ← ÉTAPE 3
├── Interfaces/
│   └── ISerialPortPool.cs           ← ÉTAPE 3
└── Services/
    ├── SerialPortPool.cs            ← ÉTAPE 4
    ├── SystemInfoCache.cs           ← ÉTAPE 4
    └── FtdiDeviceReader.cs          ← Extension ÉTAPE 3

tests/SerialPortPool.Core.Tests/
├── Models/
│   └── PoolModelsTests.cs           ← ÉTAPE 3
└── Services/
    └── SerialPortPoolTests.cs       ← ÉTAPE 4
```

---

## ⚡ **Performance Targets**

### **ÉTAPE 3** (Foundation)
- ✅ Modèles instantiation < 1ms
- ✅ Interface contract validation
- ✅ EEPROM read basic < 100ms

### **ÉTAPE 4** (Production Ready)
- 🎯 **Allocation** : < 50ms (thread-safe)
- 🎯 **Release** : < 10ms (cleanup included)
- 🎯 **Cache hit** : < 1ms SystemInfo retrieval
- 🎯 **Cache miss** : < 200ms EEPROM + cache update
- 🎯 **Concurrent operations** : 10+ allocations simultaneous
- 🎯 **Memory usage** : < 2MB cache (100 ports)

---

## 🧪 **Testing Strategy**

### **ÉTAPE 3** (Unit Tests)
```csharp
[Fact] public void AllocationStatus_HasCorrectValues()
[Fact] public void PortAllocation_CalculatesDuration()  
[Fact] public void SystemInfo_ParsesEepromData()
[Fact] public void ISerialPortPool_ContractIsValid()
```

### **ÉTAPE 4** (Integration + Performance)
```csharp
[Fact] public async Task Pool_AllocateRelease_IsThreadSafe()
[Fact] public async Task Cache_RespectsTimeToLive()
[Fact] public async Task Pool_HandlesMultipleClients()
[Fact] public async Task SystemInfo_CachesEfficiently()
```

---

## 🚨 **Risques et Mitigation**

### **Risques ÉTAPE 3**
- ❌ **Over-engineering modèles** → Keep simple, extensible
- ❌ **Interface trop complexe** → Start minimal, expand ÉTAPE 4

### **Risques ÉTAPE 4**  
- ❌ **Deadlocks** → SemaphoreSlim + proper disposal patterns
- ❌ **Memory leaks** → Weak references + periodic cleanup  
- ❌ **Cache invalidation** → TTL + manual refresh capabilities
- ❌ **EEPROM read performance** → Smart caching + background refresh

---

## 🎯 **Success Criteria**

### **ÉTAPE 3 Complete** ✅
- [ ] Tous les modèles compilent et passent tests
- [ ] Interface `ISerialPortPool` est claire et extensible
- [ ] Extension EEPROM fonctionne avec hardware réel
- [ ] Code review passé (architecture + naming)

### **ÉTAPE 4 Complete** ✅  
- [ ] Pool allocation/release thread-safe testé
- [ ] Cache performance targets atteints
- [ ] Integration EEPROM + pool functional
- [ ] Logging structuré opérationnel
- [ ] 15+ tests passent (unit + integration)

---

## 🚀 **Préparation ÉTAPE 5-6**

**After ÉTAPES 3-4**, nous aurons :
- ✅ Pool management thread-safe functional
- ✅ EEPROM system info integrated
- ✅ Smart caching pour performance
- ✅ Foundation solide pour multi-port awareness

**Next Sprint 3 focus** : Multi-port device detection + grouping

---

## 📅 **Timeline**

| Étape | Durée | Focus | Livrables |
|-------|--------|-------|-----------|
| **ÉTAPE 3** | 1h30 | Models + Interfaces | 4 modèles + 6 tests |
| **ÉTAPE 4** | 3h | Implementation + Performance | Pool + Cache + 12 tests |
| **Total** | 4h30 | Pool Management Foundation | Thread-safe pool ready |

---

*Document créé : 21 Juillet 2025*  
*Sprint 3 Status : 🎯 ÉTAPES 3-4 READY TO START*  
*Next : Multi-port detection (ÉTAPES 5-6)*