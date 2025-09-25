[![English](https://img.shields.io/badge/lang-English-blue.svg)](README.md) [![Français](https://img.shields.io/badge/lang-Français-blue.svg)](README.fr.md)
# SerialPortPoolService

![Build Status](https://github.com/linuxguy9691/SerialPortPoolService/workflows/Automated%20Tests%20-%20Sprint%2014/badge.svg)
![.NET](https://img.shields.io/badge/.NET-9.0-purple.svg)
![Platform](https://img.shields.io/badge/platform-Windows-blue.svg)
![License](https://img.shields.io/badge/license-MIT-green.svg)
![Sprint](https://img.shields.io/badge/Sprint%2014-TERMINÉ-brightgreen.svg)
![Architecture](https://img.shields.io/badge/Architecture-PRODUCTION%20READY-gold.svg)
![Hardware](https://img.shields.io/badge/Hardware-FT4232HA%20VALIDÉ-gold.svg)

Un service Windows professionnel pour gérer un pool d'interfaces série de manière centralisée et sécurisée, avec **mode production BitBang**, **configuration hot reload**, **orchestration Multi-BIB**, et **logging robuste** pour environnements d'automatisation de tests de production.

## 🎯 **Vue d'Ensemble**

SerialPortPoolService est une solution production-ready qui permet de :
- 🏭 **Mode Production BitBang** - Simulation START→TEST(loop)→STOP par UUT_ID avec déclencheurs hardware
- 🔄 **Configuration Hot Reload** - Ajout de fichiers BIB à chaud sans redémarrage de service
- 🎯 **Orchestration Multi-BIB** - Exécution parallèle de multiples configurations Board Interface Box
- 📊 **Logging Robuste** - Validation fail-fast avec logging structuré par BIB/UUT
- 🔌 **Intégration Hardware Réelle** - Support FT4232HA avec mapping dynamique EEPROM
- 🏗️ **Service Windows Enterprise** - Déploiement production avec logging NLog complet

## 📋 **Statut du Projet - SPRINT 14 PRODUCTION READY ✅**

### **✅ Sprint 1-11 - Foundation Enterprise** 
**Status :** 🎉 **TERMINÉ ET VALIDÉ**
- [x] Service Windows production avec logging NLog complet
- [x] Pool thread-safe avec découverte FTDI intelligente
- [x] Communication RS232 production-ready
- [x] Configuration XML multi-fichier avec isolation complète des BIBs
- [x] Validation 4-niveaux (PASS/WARN/FAIL/CRITICAL)
- [x] Intégration EEPROM pour sélection BIB automatique
- [x] Hot reload de configuration avec backup/rollback

### **🔥 Sprint 14 - Mode Production BitBang + Architecture Cleanup** 
**Status :** 🎉 **TERMINÉ - PRODUCTION SIMULATION FONCTIONNELLE**

#### **✅ Mode Production BitBang Implémenté**
- [x] **MultiBibWorkflowService** - Moteur production per UUT_ID avec contrôle indépendant
- [x] **BitBangProductionService** - Simulation déclencheurs START/STOP avec timing XML configurable
- [x] **Pattern Production** - START-once → TEST(loop continu) → STOP-once par UUT_ID
- [x] **Exécution Parallèle** - Multiples BIBs opèrent simultanément de façon indépendante
- [x] **Sessions Persistantes** - Connexions maintenues pendant loops TEST continues

#### **✅ Configuration Hot Reload Restaurée**
- [x] **Consolidation Sprint 11++** - Service `HotReloadConfigurationService` mature réactivé
- [x] **Ajout BIB Runtime** - Nouveaux fichiers BIB détectés et exécutés automatiquement
- [x] **Zero Downtime** - Hot reload fonctionne pendant exécution production
- [x] **Architecture Cleanup** - Services obsolètes Sprint 13 supprimés, architecture unifiée

#### **✅ Logging Infrastructure Robuste**
- [x] **Validation Startup Complète** - Vérification NLog.config et permissions avec fail-fast
- [x] **Diagnostics Transparents** - Messages clairs avec chemins exacts et raisons d'échec
- [x] **Fallback Gracieux** - BibUutLogger avec fallback vers logs principaux si problème
- [x] **Monitoring Runtime** - Détection problèmes disque/permissions avec alertes périodiques

### **📊 Validation Hardware Production**
- ✅ **FT4232HL Réel** - Communication série validée sur COM11-COM14
- ✅ **EEPROM Reading** - Détection automatique BIB via ProductDescription
- ✅ **Port Mapping Dynamique** - Association client_demo → COM11 avec réservation
- ✅ **Simulation BitBang** - Déclencheurs START/STOP configurables via XML
- ✅ **Multi-BIB Parallèle** - 2 BIBs exécutés simultanément sans conflit

---

## 🚀 **Installation Rapide & Utilisation**

### **Prérequis**
- **OS :** Windows 10/11 ou Windows Server 2019+
- **Runtime :** .NET 9.0
- **Permissions :** Droits administrateur pour installation service
- **Hardware :** Device FTDI FT4232HA/HL recommandé

### **Installation**

```powershell
# 1. Cloner et compiler
git clone https://github.com/votre-repo/SerialPortPoolService.git
cd SerialPortPoolService
dotnet build --configuration Release

# 2. Tester en mode interactif
cd SerialPortPoolService/bin/Release/net9.0-windows
.\SerialPortPoolService.exe --config-dir Configuration --discover-bibs --mode production
```

### **Mode Production (Nouveau Sprint 14)**

```powershell
# Mode production avec découverte automatique de BIBs
.\SerialPortPoolService.exe --config-dir Configuration --discover-bibs --mode production

# Sortie attendue:
# 🏭 Production mode detected - using BIB file discovery only
# 📊 BIB client_demo: 1 UUTs detected for production
# 🔄 Starting continuous TEST loop for client_demo.production_uut
# ✅ TEST cycle #1 result: True
```

### **Configuration Hot Reload**

```powershell
# Pendant que le service tourne, ajouter un nouveau fichier BIB:
copy bib_nouveau_test.xml Configuration\

# Logs automatiques:
# 🆕 New configuration file detected: bib_nouveau_test.xml (BIB: nouveau_test)
# 🏭 Starting production workflow for changed BIB: nouveau_test
# ✅ Hot-changed BIB task started: nouveau_test
```

### **Installation comme Service Windows**

```powershell
# Créer et démarrer le service
sc create SerialPortPoolService binPath="C:\Path\To\SerialPortPoolService.exe --mode production --config-dir Configuration"
sc start SerialPortPoolService

# Monitorer via Event Viewer ou logs
Get-EventLog -LogName Application -Source SerialPortPoolService -Newest 10
type C:\Logs\SerialPortPool\service-*.log
```

---

## 🏗️ **Architecture Production - Sprint 14**

### **Structure Projet**
```
SerialPortPoolService/                          ← Service Production
├── Services/
│   ├── ✅ MultiBibWorkflowService.cs          # Mode production per UUT_ID
│   └── ✅ BitBangProductionService.cs         # Simulation déclencheurs hardware
├── Configuration/
│   ├── ✅ bib_client_demo.xml                 # Configuration BIB exemple
│   └── ✅ bib_client_demo_2.xml               # Configuration BIB additionnelle
├── ✅ Program.cs                              # Validation logging robuste
└── ✅ NLog.config                             # Configuration logging production
```

### **Services Core**
```
SerialPortPool.Core/
├── Services/
│   ├── Configuration/
│   │   └── ✅ HotReloadConfigurationService.cs # Hot reload mature (Sprint 11++)
│   ├── ✅ BibWorkflowOrchestrator.cs          # Orchestration améliorée
│   ├── ✅ XmlBibConfigurationLoader.cs        # Chargement multi-fichier
│   ├── ✅ DynamicPortMappingService.cs        # Mapping EEPROM automatique
│   └── ✅ BibUutLogger.cs                     # Logging structuré par BIB/UUT
└── Models/
    ├── ✅ BibConfiguration.cs                 # Configuration BIB avec simulation
    ├── ✅ HardwareSimulationConfig.cs         # Configuration simulation hardware
    └── ✅ MultiBibWorkflowResult.cs           # Résultats agrégés Multi-BIB
```

---

## 🔧 **Configuration XML - Mode Production**

### **Fichier BIB Individuel avec Simulation BitBang**

```xml
<?xml version="1.0" encoding="UTF-8"?>
<bib id="client_demo" description="Client Demo with BitBang Simulation">
  <!-- Configuration simulation hardware -->
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

### **Options Ligne de Commande**

```bash
SerialPortPoolService.exe [OPTIONS]

Options Production:
  --mode production                 Mode production BitBang (recommandé)
  --config-dir <répertoire>         Répertoire fichiers BIB (défaut: Configuration/)
  --discover-bibs                   Découverte automatique fichiers bib_*.xml

Options Legacy:
  --xml-config <fichier>            Fichier XML unique (mode legacy)
  --bib-ids <liste>                 BIB IDs spécifiques (séparés par espaces)
  --mode continuous                 Mode continu legacy
  --interval <minutes>              Intervalle entre cycles

Options Diagnostic:
  --detailed-logs                   Logging détaillé
  --help                           Afficher aide
```

---

## 📊 **Logging et Monitoring**

### **Structure Logs Production**

```
C:\Logs\SerialPortPool\
├── service-2025-09-25.log                    # Logs service principal (NLog)
├── BIB_client_demo\                          # Logs structurés par BIB
│   ├── 2025-09-25\
│   │   ├── production_uut_port1_1735.log     # Logs exécution spécifique
│   │   └── daily_summary_2025-09-25.log      # Résumé journalier
│   └── latest\
│       └── production_uut_current.log        # Marqueur exécution courante
└── BIB_autre_bib\
    └── ...                                   # Structure similaire par BIB
```

### **Validation Logging Robuste**

Au démarrage, le service valide automatiquement:
```
🔧 Validating logging configuration...
📁 Checking NLog.config at: [chemin exact]
✅ NLog.config file found and readable
📁 Checking log directory: C:\Logs\SerialPortPool
✅ Log directory writable
✅ OPTIMAL: File logging + Console logging active
```

Si problème de logging détecté:
```
❌ CRITICAL FAILURE: NO LOGGING AVAILABLE
🛑 SERVICE CANNOT START
💥 ISSUES:
   • NLog.config file not found at: [chemin]
   • Cannot write to log directory: Access denied
```

---

## 🧪 **Tests et Validation**

### **Validation Hardware Sprint 14**

```powershell
# Test complet mode production avec hardware réel
.\SerialPortPoolService.exe --config-dir Configuration --discover-bibs --mode production

# Résultats attendus:
# - Détection FTDI: 5 ports (COM6, COM11-COM14)
# - EEPROM reading: client_demo A/B/C/D détectés
# - Mapping dynamique: client_demo → COM11
# - Production workflow: START → TEST(loop) → STOP
# - Hot reload: Nouveaux fichiers BIB détectés automatiquement
```

### **Tests Intégration Validés**

- ✅ **Multi-BIB Parallèle** - 2 BIBs simultanés sans conflit de ressources
- ✅ **Hot Reload Runtime** - Ajout fichier BIB pendant exécution production
- ✅ **Communication Hardware** - RS232 réel sur FT4232HL validé
- ✅ **Logging Robuste** - Fail-fast startup + fallback gracieux
- ✅ **Simulation BitBang** - Timing configurable XML avec loops continus
- ✅ **Port Mapping EEPROM** - Détection automatique BIB_ID via ProductDescription

### **Limitations Identifiées**

- **BIB Mapping** - Requiert EEPROM programmée avec ProductDescription correspondante
- **Mode Production** - Optimisé pour simulation, hardware GPIO physique nécessite développement additionnel
- **CLI Parsing** - Virgules dans `--bib-ids` nécessitent utilisation d'espaces
- **Concurrent Limits** - Maximum simultané UUTs non établi (nécessite tests charge)

---

## 🎯 **Statut Production Readiness**

### **✅ Fonctionnalités Validées Production**
- **Service Windows** - Installation, démarrage, monitoring via Event Viewer
- **Configuration Multi-Fichier** - Isolation BIB complète avec hot reload
- **Communication Série** - RS232 validé avec hardware FT4232HL
- **Mode Production BitBang** - Pattern START→TEST(loop)→STOP fonctionnel
- **Logging Enterprise** - NLog + BibUutLogger structuré + fail-fast validation
- **Dynamic Port Mapping** - EEPROM-based BIB detection automatique

### **⚠️ Nécessite Validation Additionnelle**
- **Stabilité Long Terme** - Loops TEST continus sur heures/jours
- **Gestion Erreurs** - Recovery gracieux sur pannes communication
- **Performance Charge** - Limites UUTs simultanés et usage ressources
- **Hardware GPIO Physique** - Intégration déclencheurs hardware réels

### **🔮 Développement Futur**
- **GPIO Hardware Réel** - Intégration déclencheurs physiques BitBang
- **API REST** - Endpoints HTTP pour monitoring et contrôle externe
- **Dashboard Web** - Interface monitoring temps réel
- **Analytics Avancées** - Métriques performance et alerting

---

## 📞 **Support et Documentation**

### **Documentation Technique**
- 📖 **Guide Installation**: Instructions complètes service Windows
- 🔧 **Configuration XML**: Référence complète syntaxe BIB
- 📊 **Architecture**: Sprint 14 conclusion avec détails techniques
- 🧪 **Tests**: Procédures validation hardware et intégration

### **Support Hardware**
- 🔌 **FTDI Supporté**: FT232R, FT4232H/HL, FT232H avec EEPROM reading
- 📡 **Communication**: RS232 production-ready avec gestion timeout
- 🎯 **Port Discovery**: WMI + EEPROM intelligent avec cache TTL
- 🔬 **BIB Detection**: ProductDescription automatique vers BIB_ID

### **Support Déploiement**
- 🏗️ **Service Windows**: Installation/désinstallation automatisée
- 📋 **Configuration**: Multi-fichier avec backup/rollback automatique
- 📊 **Monitoring**: NLog + Event Viewer + logs structurés
- 🔄 **Hot Reload**: Configuration runtime sans redémarrage service

---

## 🚀 **Évolution Projet - Sprint 15+**

### **Prochaines Priorités**
- **Tests Stabilité** - Validation long terme et gestion erreurs
- **Performance Optimization** - Limites charge et monitoring ressources
- **Documentation Utilisateur** - Guides opérationnels et troubleshooting
- **Hardware GPIO** - Intégration déclencheurs physiques production

### **Vision Long Terme**
SerialPortPoolService évolue vers une **plateforme automation test enterprise** avec:
- Orchestration Multi-BIB parallèle avancée
- Intégration hardware GPIO physique complète  
- API REST et dashboard monitoring temps réel
- Analytics prédictives et alerting intelligent

---

## 🎉 **Conclusion Sprint 14**

Le **Sprint 14** représente une **étape majeure** vers la production-ready avec:

**🏭 Mode Production BitBang** - Pattern production START→TEST(loop)→STOP implémenté et validé  
**🔄 Hot Reload Restauré** - Configuration runtime sans interruption fonctionnelle  
**📊 Logging Robuste** - Fail-fast validation avec monitoring transparent  
**🏗️ Architecture Mature** - Consolidation services avec cleanup complet  

Le système **SerialPortPoolService** est maintenant **prêt pour déploiement production** avec simulation BitBang complète et infrastructure logging enterprise-grade. Les fonctionnalités avancées (GPIO hardware physique, API REST) représentent les prochaines évolutions naturelles de cette foundation solide.

---

*Dernière mise à jour : Septembre 2025 - Sprint 14 Production BitBang + Hot Reload*  
*Statut Actuel : Production Ready avec Mode BitBang + Configuration Hot Reload*  
*Version : 14.0.0 - Service Production avec Simulation Hardware + Logging Robuste*  
*Hardware Validé : FTDI FT4232HL avec communication RS232 + EEPROM reading*  
*Prêt pour : Déploiement production + validation stabilité long terme*