# SerialPortPoolService

![Build Status](https://github.com/linuxguy9691/SerialPortPoolService/workflows/Automated%20Tests%20-%20Sprint%201%20&%202/badge.svg)
![.NET](https://img.shields.io/badge/.NET-9.0-purple.svg)
![Platform](https://img.shields.io/badge/platform-Windows-blue.svg)
![License](https://img.shields.io/badge/license-MIT-green.svg)
![Sprint](https://img.shields.io/badge/Sprint%202-COMPLETED-brightgreen.svg)

Un service Windows professionnel pour gérer un pool d'interfaces série de manière centralisée et sécurisée, avec découverte automatique, identification FTDI intelligente et filtrage selon critères hardware spécifiques.

## 🎯 **Vue d'Ensemble**

SerialPortPoolService est une solution enterprise-grade qui permet de :
- 🔍 **Découvrir automatiquement** les ports série avec enrichissement WMI
- 🔌 **Identifier intelligemment** les devices FTDI (VID_0403) avec analyse des chips
- 🎯 **Filtrer selon critères** hardware spécifiques (FTDI 4232H requis pour client)
- 📊 **Validation avancée** avec scoring et critères configurables
- 🌐 **Fournir une API REST** pour l'allocation/libération des ports (Sprint 4+)
- ⚡ **Gérer automatiquement** les reconnexions et la tolérance aux pannes
- 🔐 **Sécuriser l'accès** aux ressources série critiques

## 📋 **Statut du Projet**

### **✅ Sprint 1 - Service Windows de Base** 
**Status :** 🎉 **COMPLETED WITH EXCELLENCE**
- [x] Service Windows installable et gérable
- [x] Logging professionnel (fichiers + Event Viewer)
- [x] Scripts d'installation PowerShell
- [x] Tests automatisés (7/7 tests, 100% coverage)
- [x] Documentation complète

### **✅ Sprint 2 - Découverte et Filtrage FTDI** 
**Status :** 🎉 **COMPLETED WITH EXCELLENCE**

#### **✅ ÉTAPE 1-2 : Foundation + Discovery (COMPLETED)**
- [x] Modèles de base (SerialPortInfo, PortStatus)
- [x] Interface ISerialPortDiscovery + SerialPortDiscoveryService
- [x] Discovery avec System.IO.Ports + WMI enrichment
- [x] Test d'accessibilité ports (Available/Allocated/Error)
- [x] Structure projet SerialPortPool.Core

#### **✅ ÉTAPE 4-5 : FTDI Models & Validation (COMPLETED)**
- [x] FtdiDeviceInfo avec parsing automatique Device ID
- [x] Système validation PortValidationResult avec scoring 0-100%
- [x] Configuration client vs développement (strict vs permissif)
- [x] Interfaces ISerialPortValidator + IFtdiDeviceReader
- [x] SerialPortInfo étendu avec propriétés FTDI intelligentes
- [x] Support complet chips FTDI (FT232R, FT4232H, FT232H, etc.)
- [x] Validation criteria system avec 15+ critères configurables
- [x] Tests hardware réels avec Device ID parsing

#### **✅ ÉTAPE 6 : Service Implementation (COMPLETED)**
- [x] FtdiDeviceReader service implementation complet
- [x] SerialPortValidator service implementation complet
- [x] EnhancedSerialPortDiscoveryService avec intégration complète
- [x] Dependency Injection configuration
- [x] Demo interactive avec analyse FTDI temps réel
- [x] Validation hardware réelle (COM6 - FT232R)

### **🚀 Sprint 3 - Pool Management** 
**Status :** 🔄 **READY TO START**
- [ ] Intégration discovery au service Windows principal
- [ ] Pool management avec allocation/libération
- [ ] Configuration avancée et monitoring
- [ ] State management et persistence

### **🔮 Sprints Futurs**
- [ ] Sprint 4 : API REST et interface web
- [ ] Sprint 5 : Monitoring avancé et métriques  
- [ ] Sprint 6 : Haute disponibilité et clustering

## 🏗️ **Architecture**

```
SerialPortPoolService/                    ← Git Repository Root
├── 🚀 SerialPortPoolService/            ← Sprint 1: Service Windows Principal
│   ├── Program.cs                       ├─ ServiceBase robuste
│   ├── NLog.config                      ├─ Logging professionnel
│   └── scripts/Install-Service.ps1      └─ Installation automatisée
├── 🔍 SerialPortPool.Core/              ← Sprint 2: Discovery + FTDI Engine
│   ├── Services/
│   │   ├── SerialPortDiscoveryService.cs        ← Discovery basique
│   │   ├── EnhancedSerialPortDiscoveryService.cs ← Discovery avec FTDI intégré
│   │   ├── FtdiDeviceReader.cs                  ← Analyse devices FTDI
│   │   └── SerialPortValidator.cs               ← Validation configurable
│   ├── Models/
│   │   ├── SerialPortInfo.cs            ├─ Modèle enrichi FTDI
│   │   ├── FtdiDeviceInfo.cs            ├─ Analyse devices FTDI
│   │   ├── PortValidation.cs            ├─ Système validation
│   │   └── PortStatus.cs                └─ États des ports
│   └── Interfaces/
│       ├── ISerialPortDiscovery.cs      ├─ Discovery interface
│       └── ISerialPortValidator.cs      └─ Validation + FTDI interfaces
├── 🧪 tests/
│   ├── SerialPortPool.Core.Tests/       ├─ 12 unit tests Sprint 2
│   └── PortDiscoveryDemo/              └─ Demo interactive discovery
├── 📊 SerialPortPoolService.sln         ← Solution unifiée
├── 🚀 .github/workflows/                ← CI/CD automation
└── 📚 docs/                            ← Documentation complète
```

## 🚀 **Installation Rapide**

### **Prérequis**
- **OS :** Windows 10/11 ou Windows Server 2016+
- **Runtime :** .NET 9.0 ou supérieur
- **Permissions :** Droits administrateur pour l'installation du service

### **Installation en 3 étapes**

```powershell
# 1. Cloner le repository
git clone https://github.com/[username]/SerialPortPoolService.git
cd SerialPortPoolService

# 2. Compiler toute la solution
dotnet build SerialPortPoolService.sln --configuration Release

# 3. Installer le service (PowerShell Admin requis)
.\SerialPortPoolService\scripts\Install-Service.ps1
```

### **Vérification de l'installation**

```powershell
# Vérifier le statut du service
Get-Service SerialPortPoolService

# Demo discovery avec analyse FTDI complète
dotnet run --project tests\PortDiscoveryDemo\

# Tests complets Sprint 1 + 2
dotnet test tests/SerialPortPool.Core.Tests/ --verbosity normal
```

## 🔧 **Utilisation**

### **Demo Discovery Interactive avec Analyse FTDI Complète**

```bash
# Découvrir et analyser tous les ports série avec validation temps réel
dotnet run --project tests/PortDiscoveryDemo/

# Output exemple avec device FTDI réel (COM6):
# 🔍 Enhanced Serial Port Discovery Demo - ÉTAPE 6
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

### **Analyse Hardware en Code avec Services Intégrés**

```csharp
// Setup Dependency Injection (ÉTAPE 6)
var serviceProvider = SetupServices();
var discovery = serviceProvider.GetRequiredService<ISerialPortDiscovery>();
var validator = serviceProvider.GetRequiredService<ISerialPortValidator>();

// Discovery avec analyse FTDI intégrée
var ports = await discovery.DiscoverPortsAsync();
foreach (var port in ports)
{
    if (port.IsFtdiDevice && port.FtdiInfo != null)
    {
        Console.WriteLine($"FTDI {port.FtdiInfo.ChipType} detected");
        Console.WriteLine($"Valid for pool: {port.IsValidForPool}");
        Console.WriteLine($"Validation: {port.ValidationReason} (Score: {port.ValidationScore}%)");
        
        // Analyse détaillée
        Console.WriteLine($"Genuine FTDI: {port.IsGenuineFtdi}");
        Console.WriteLine($"Client Compatible (4232H): {port.IsFtdi4232H}");
    }
}

// Configuration validation flexible
var clientConfig = PortValidationConfiguration.CreateClientDefault(); 
// Strict: seulement FT4232H (PID 6011)

var devConfig = PortValidationConfiguration.CreateDevelopmentDefault();
// Permissif: FT232R + FT4232H + autres chips
```

### **Services Implementation (ÉTAPE 6)**

```csharp
// Configuration DI complète
services.AddSingleton(PortValidationConfiguration.CreateDevelopmentDefault());
services.AddScoped<IFtdiDeviceReader, FtdiDeviceReader>();
services.AddScoped<ISerialPortValidator, SerialPortValidator>();
services.AddScoped<ISerialPortDiscovery, EnhancedSerialPortDiscoveryService>();

// Utilisation avec validation intégrée
var enhancedDiscovery = new EnhancedSerialPortDiscoveryService(logger, ftdiReader, validator);
var validPorts = await enhancedDiscovery.DiscoverValidPortsAsync(clientConfig);
```

### **Gestion du Service Windows**

```powershell
# Démarrer le service
Start-Service SerialPortPoolService

# Arrêter le service
Stop-Service SerialPortPoolService

# Mode développement interactif
cd SerialPortPoolService\bin\Release\net9.0-windows\
.\SerialPortPoolService.exe
```

## 🧪 **Tests et Qualité**

### **Coverage Automatisé Sprint 1 + 2**
![Tests](https://img.shields.io/badge/Tests%20Sprint%201-7%2F7%20PASSED-brightgreen.svg)
![Tests](https://img.shields.io/badge/Tests%20Sprint%202-12%2F12%20PASSED-brightgreen.svg)
![Coverage](https://img.shields.io/badge/FTDI%20Models-100%25-brightgreen.svg)
![Integration](https://img.shields.io/badge/Service%20Integration-100%25-brightgreen.svg)

```bash
# Tests complets Sprint 1 + 2
dotnet test tests/SerialPortPool.Core.Tests/ --verbosity normal

# Output attendu:
# Test de SerialPortPool.Core.Tests : a réussi (5.4 s)
# Récapitulatif du test : total : 12, échec : 0, réussi : 12, ignoré : 0
#      
# ✅ FtdiDeviceInfo parsing avec Device ID réel
# ✅ FtdiDeviceReader service avec WMI intégration
# ✅ SerialPortValidator avec scoring configurable
# ✅ EnhancedDiscoveryService avec FTDI analysis
# ✅ Validation FT232R vs FT4232H
# ✅ Configuration client vs développement
# ✅ SerialPortInfo propriétés FTDI étendues
```

### **Validation Hardware Réelle Complète**
- ✅ **Testé avec FTDI FT232R** (COM6, VID_0403+PID_6001+AG0JU7O1A)
- ✅ **Parsing automatique Device ID** avec regex robuste
- ✅ **Détection chip type** (FT232R vs FT4232H vs autres)
- ✅ **Validation client** (4232H requis) vs **dev** (permissif)
- ✅ **Scoring système** (0-100%) avec critères détaillés
- ✅ **Service integration** avec DI et async/await
- ✅ **WMI enrichment** avec PnP entity fallback
- ✅ **Multi-configuration** validation testing

## 📊 **Configuration et Exemples**

### **Configuration Client (Production) - ÉTAPE 6**

```csharp
// Configuration stricte pour client final
var clientConfig = PortValidationConfiguration.CreateClientDefault();
// RequireFtdiDevice: true
// Require4232HChip: true  
// AllowedProductIds: ["6011"]     // Seulement FT4232H
// MinimumValidationScore: 100     // Doit être parfait
// StrictValidation: true

// Test avec votre COM6 (FT232R)
var validator = new SerialPortValidator(ftdiReader, logger, clientConfig);
var result = await validator.ValidatePortAsync(portInfo);
// Résultat: ❌ INVALID (Score: 50%) - "requires 4232H chip (found FT232R)"
```

### **Configuration Développement**

```csharp
// Configuration permissive pour développement
var devConfig = PortValidationConfiguration.CreateDevelopmentDefault();
// RequireFtdiDevice: true
// Require4232HChip: false         // Plus permissif
// AllowedProductIds: ["6001", "6011", "6014"]  // Plusieurs chips
// MinimumValidationScore: 70      // Plus tolérant
// StrictValidation: false

// Test avec votre COM6 (FT232R)
var result = await validator.ValidatePortAsync(portInfo, devConfig);
// Résultat: ✅ VALID (Score: 80%) - "Valid FTDI device: FT232R"
```

### **Enhanced Discovery Service (ÉTAPE 6)**

```csharp
// Service intégré avec validation automatique
var discovery = new EnhancedSerialPortDiscoveryService(logger, ftdiReader, validator);

// Discovery complète avec analyse FTDI
var ports = await discovery.DiscoverPortsAsync();
foreach (var port in ports)
{
    // Toutes les propriétés automatiquement remplies :
    Console.WriteLine($"Port: {port.PortName}");
    Console.WriteLine($"FTDI: {port.IsFtdiDevice} ({port.FtdiChipType})");
    Console.WriteLine($"Valid: {port.IsValidForPool} ({port.ValidationScore}%)");
    Console.WriteLine($"Details: {port.ToDetailedString()}");
}

// Discovery filtered (seulement ports valides)
var validPorts = await discovery.DiscoverValidPortsAsync(clientConfig);
```

## 🛠️ **Développement**

### **Stack Technique**
- **Framework :** .NET 9.0 (Windows)
- **Architecture :** Multi-projet avec solution unified + DI
- **Discovery :** System.IO.Ports + WMI (Win32_SerialPort, Win32_PnPEntity)
- **FTDI Analysis :** Device ID parsing + chip identification + WMI enrichment
- **Validation :** Configurable criteria system avec scoring 0-100%
- **Services :** IFtdiDeviceReader, ISerialPortValidator, Enhanced Discovery
- **Logging :** NLog avec rotation automatique + structured logging
- **Tests :** xUnit + validation hardware réelle + service integration
- **CI/CD :** GitHub Actions (14 tests automatisés)

### **Commandes Développeur Essentielles**

```bash
# Setup environnement complet
git clone https://github.com/[username]/SerialPortPoolService.git
cd SerialPortPoolService
dotnet restore

# Build et test solution complète
dotnet build SerialPortPoolService.sln --configuration Debug
dotnet test tests/SerialPortPool.Core.Tests/ --verbosity normal

# Demo avec analyse FTDI temps réel + services intégrés
dotnet run --project tests/PortDiscoveryDemo/

# Build release pour production
dotnet build SerialPortPoolService.sln --configuration Release
```

### **Architecture ÉTAPE 6 - Services Implementation**

```bash
# Nouvaux services implémentés (ÉTAPE 6)
SerialPortPool.Core/Services/
├── FtdiDeviceReader.cs                    # Service FTDI avec WMI
├── SerialPortValidator.cs                 # Validation configurable avec scoring
├── EnhancedSerialPortDiscoveryService.cs  # Discovery intégré avec FTDI+Validation
└── SerialPortDiscoveryService.cs          # Discovery basique (conservé)

# Demo interactive mise à jour
tests/PortDiscoveryDemo/Program.cs
└── Configuration DI complète + analyse temps réel hardware
```

## 📈 **Monitoring et Métriques**

### **Logs Multi-Niveaux**
- **Sprint 1 :** Service Windows lifecycle (start/stop/errors)
- **Sprint 2 :** Discovery détaillé + analyse FTDI + validation scoring
- **ÉTAPE 6 :** Service integration avec structured logging
- **Fichiers :** `C:\Logs\SerialPortPool\service-YYYY-MM-DD.log`
- **Event Viewer :** Application Log > SerialPortPoolService

### **FTDI Analysis Metrics - ÉTAPE 6**
- **Devices détectés :** Total FTDI devices (VID_0403) avec enrichissement WMI
- **Chip identification :** FT232R vs FT4232H vs autres avec parsing automatique
- **Validation scoring :** Distribution des scores 0-100% selon configuration
- **Client compatibility :** Pourcentage devices valides pool (strict vs permissif)
- **Hardware mapping :** Device ID → Chip type → Validation result correlation
- **Performance metrics :** Discovery time, WMI query latency, validation throughput

### **Exemples Logs ÉTAPE 6**

```
2025-07-18 14:17:43 INFO  Found 1 serial ports: COM6
2025-07-18 14:17:43 INFO  FTDI analysis complete: COM6 → FT232R (VID: 0403, PID: 6001)
2025-07-18 14:17:43 INFO  Port COM6 validation passed: Valid FTDI device: FT232R (PID: 6001) (Score: 80%)
2025-07-18 14:17:43 INFO  Discovery complete: 1 ports processed, 1 FTDI devices, 1 valid for pool
2025-07-18 14:17:43 INFO  Valid ports discovery: 1 out of 1 ports are valid for pool
```

## 🤝 **Contribution**

### **Workflow de Contribution Sprint 2 Complété**

1. **Fork** le repository principal
2. **Créer une branche** feature : `git checkout -b feature/sprint3-pool-management`
3. **Développer** avec tests : TDD recommandé, coverage > 80%
4. **Tester localement** : `dotnet test` + validation hardware si disponible
5. **Demo validation** : Tester avec devices FTDI réels
6. **Commit** avec message descriptif : Convention [Conventional Commits](https://www.conventionalcommits.org/)
7. **Push** et créer une **Pull Request**

### **Standards de Qualité ÉTAPE 6**

- ✅ **Tests unitaires** obligatoires (coverage 100% services implementation)
- ✅ **Validation hardware** avec Device IDs réels et services intégrés
- ✅ **Service integration** testing avec DI configuration
- ✅ **Demo interactive** mise à jour pour nouvelles fonctionnalités
- ✅ **Documentation** README + comments code détaillés
- ✅ **CI/CD pipeline** doit passer (GitHub Actions enhanced)

## 🔐 **Sécurité**

### **Considérations Sécurité ÉTAPE 6**
- 🔒 **Device ID parsing** sécurisé avec regex validées + input sanitization
- 🛡️ **Validation stricte** VID_0403 pour genuine FTDI + chip verification
- 📝 **Audit complet** validation decisions + scoring + service interactions
- 🔐 **Configuration isolation** client vs dev environments + validation rules
- 🎯 **Hardware fingerprinting** avec serial numbers + WMI enrichment
- 🔧 **Service boundaries** avec interfaces et dependency injection
- 📊 **Structured logging** pour audit trails et troubleshooting

## 📄 **Licensing**

Ce projet est sous licence [MIT License](LICENSE).

```
MIT License - Copyright (c) 2025 SerialPortPoolService
Utilisation libre pour projets commerciaux et open source.
Includes advanced FTDI device analysis and validation system with service integration.
```

## 🎉 **Changelog**

### **v1.3.0 - Sprint 2 ÉTAPE 6 - SERVICE IMPLEMENTATION** (2025-07-18) ✅
- ✨ **NEW :** FtdiDeviceReader service avec WMI integration complète
- ✨ **NEW :** SerialPortValidator service avec scoring configurable
- ✨ **NEW :** EnhancedSerialPortDiscoveryService avec FTDI+Validation intégrés
- ✨ **NEW :** Dependency Injection configuration complète
- ✨ **NEW :** Demo interactive mise à jour avec services integration
- ✨ **NEW :** Validation hardware temps réel avec COM6 (FT232R)
- ✨ **NEW :** Configuration client vs dev testing intégré
- ✨ **NEW :** Async/await pattern avec error handling complet
- ✨ **NEW :** Structured logging avec niveaux INFO/DEBUG
- ✨ **NEW :** Service boundaries avec interfaces bien définies
- 🔧 **IMPROVE :** SerialPortInfo étendu avec toutes propriétés FTDI
- 🔧 **IMPROVE :** Performance WMI queries avec proper disposal
- 🔧 **IMPROVE :** Architecture prête pour Sprint 3 integration
- 📚 **DOCS :** Documentation service integration complète
- 🧪 **VALIDATED :** 12 tests unitaires + service integration + hardware réel

### **v1.2.0 - Sprint 2 ÉTAPE 5** (2025-07-16) ✅
- ✨ **NEW :** FtdiDeviceInfo avec parsing automatique Device ID
- ✨ **NEW :** Système validation PortValidationResult avec scoring 0-100%
- ✨ **NEW :** Configuration client vs développement (strict vs permissif)
- ✨ **NEW :** Interfaces ISerialPortValidator + IFtdiDeviceReader
- ✨ **NEW :** SerialPortInfo étendu avec propriétés FTDI intelligentes
- ✨ **NEW :** Support complet chips FTDI (FT232R, FT4232H, FT232H, etc.)
- ✨ **NEW :** Validation criteria system avec 15+ critères configurables
- ✨ **NEW :** Tests hardware réels avec Device ID parsing
- 🔧 **IMPROVE :** Architecture prête pour services ÉTAPE 6
- 🔧 **IMPROVE :** Modèles extensibles pour EEPROM et multi-port devices
- 📚 **DOCS :** Documentation complète validation system
- 🧪 **VALIDATED :** 12 tests unitaires avec hardware réel (COM6)

### **v1.1.0 - Sprint 2 ÉTAPE 2** (2025-07-16) ✅
- ✨ **NEW :** SerialPortPool.Core library avec discovery engine
- ✨ **NEW :** Discovery automatique System.IO.Ports + WMI enrichment
- ✨ **NEW :** Identification devices FTDI (VID_0403 detection)
- ✨ **NEW :** Test d'accessibilité ports (Available/Allocated/Error)
- ✨ **NEW :** Demo interactive pour discovery temps réel
- ✨ **NEW :** Solution unified (SerialPortPoolService.sln)
- 🧪 **VALIDATED :** Hardware FTDI réel (COM6 - FT232R)

### **v1.0.0 - Sprint 1** (2025-07-16) ✅
- ✨ **NEW :** Service Windows installable et gérable
- ✨ **NEW :** Logging professionnel avec NLog
- ✨ **NEW :** Scripts PowerShell d'installation/désinstallation
- ✨ **NEW :** Pipeline CI/CD GitHub Actions
- ✨ **NEW :** Tests automatisés (7/7, 100% coverage)

### **v2.0.0 - Sprint 3 - Pool Management** (Planned) 🔄
- ✨ **PLANNED :** Integration discovery au service Windows principal
- ✨ **PLANNED :** Pool allocation/liberation avec state management
- ✨ **PLANNED :** Configuration persistence et monitoring
- ✨ **PLANNED :** Multi-threaded pool operations

---

## 🚀 **Ready for Sprint 3 - Pool Management!**

> **Sprint 1 :** Foundation Windows Service solide ✅  
> **Sprint 2 - ÉTAPE 6 :** Service Implementation avec FTDI Analysis ✅  
> **Sprint 3 :** Pool Management & Windows Service Integration 🔄  
> **Future :** API REST, monitoring avancé, clustering  

**Services FTDI intelligents implémentés et testés avec hardware réel !** 🔌⚡🎉

**Prêt pour l'intégration au service Windows principal et le pool management !**

---

## 📞 **Support et Contact**

### **Documentation ÉTAPE 6**
- 📖 **Service Integration :** [Service Implementation Guide](https://github.com/[username]/SerialPortPoolService/docs/service-integration)
- 📋 **FTDI Analysis :** [FTDI Device Reader Documentation](https://github.com/[username]/SerialPortPoolService/docs/ftdi-analysis)
- 🎯 **Validation System :** [Port Validation Guide](https://github.com/[username]/SerialPortPoolService/docs/validation)
- 🔧 **Configuration :** [Client vs Dev Configuration](https://github.com/[username]/SerialPortPoolService/docs/configuration)

### **Support Hardware**
- 🔌 **FTDI Support :** Tous les chips FTDI (FT232R, FT4232H, FT232H, etc.)
- 📱 **Device ID Parsing :** Automatic VID/PID/Serial extraction avec WMI enrichment
- 🎯 **Client Requirements :** FT4232H validation and filtering
- 🧪 **Dev Testing :** Permissive mode pour développement avec scoring flexible
- 🔧 **Service Integration :** Dependency injection avec async/await patterns

---

*Dernière mise à jour : 18 Juillet 2025 - Post ÉTAPE 6 COMPLETED*  
*Current Sprint : Sprint 3 - Pool Management (Ready to Start)*  
*Version : 1.3.0 - Service Implementation Complete*  
*Hardware Validated : FTDI FT232R (COM6, VID_0403+PID_6001+AG0JU7O1A)*  
*Integration Status : Services fully implemented with DI and real-time hardware analysis*