# Sprint 3 Planning - Service Integration & Pool Management

![Sprint](https://img.shields.io/badge/Sprint%203-IN%20PROGRESS-yellow.svg)
![Target](https://img.shields.io/badge/Target-3%20weeks-blue.svg)
![Risk](https://img.shields.io/badge/Main%20Risk-Service%20Integration-red.svg)

## 🎯 **Objectif Sprint 3**

Intégrer le moteur Enhanced Discovery (Sprint 2) au service Windows (Sprint 1) et implémenter un pool management basique avec allocation/libération des ports FTDI.

## 📋 **Scope Sprint 3**

### **✅ Inclus dans Sprint 3**
- 🔧 **Service Integration :** Enhanced Discovery → Service Windows avec DI complet
- 🏊 **Pool Management Basique :** Allocation/libération thread-safe des ports
- 🔍 **Multi-Port Awareness :** Détecter qu'un device 4232H = plusieurs ports
- ⚙️ **Configuration Management :** Client vs dev settings dans le service
- 📊 **EEPROM System Info :** Extension des données système hardware

### **❌ Repoussé au Sprint 4**
- 🔧 Bit bang port exclusion (complexe)
- 🌐 API REST endpoints  
- 📈 Monitoring avancé et métriques
- 🏗️ High availability features

---

## 🔄 **Découpage en Étapes**

### **🔹 ÉTAPE 1-2: Service Integration Foundation (Semaine 1)**
**Risque Principal :** Intégration sans casser le service Windows existant

#### **ÉTAPE 1: Service DI Integration**
**Objectif :** Intégrer Enhanced Discovery au service Windows avec Dependency Injection

**Tasks :**
- [ ] Modifier `SerialPortPoolService/Program.cs` pour DI complet
- [ ] Ajouter configuration client vs dev selon environnement
- [ ] Setup services : `EnhancedSerialPortDiscoveryService`, `FtdiDeviceReader`, `SerialPortValidator`
- [ ] Tests : Service démarre correctement avec discovery FTDI
- [ ] Validation : Pas de régression sur le service existant

**Livrables :**
- Service Windows avec DI functional
- Configuration client/dev intégrée
- Tests d'intégration service + discovery

#### **ÉTAPE 2: Background Discovery Service**  
**Objectif :** Discovery périodique en arrière-plan

**Tasks :**
- [ ] Créer `PortDiscoveryBackgroundService : BackgroundService`
- [ ] Discovery périodique (ex: toutes les 30 secondes)
- [ ] Logging des changements de ports (connexion/déconnexion)
- [ ] Gestion proper des erreurs et cancellation
- [ ] Tests : Background service lifecycle correct

**Livrables :**
- Background service stable
- Discovery continue sans impact performance
- Error handling et recovery robuste

### **🔹 ÉTAPE 3-4: Pool Management + EEPROM System Info (Semaine 2)**

#### **ÉTAPE 3: Pool Models & EEPROM Extension**
**Objectif :** Modèles de base pour pool + extension EEPROM système

**Tasks :**
- [ ] Interface `ISerialPortPool` avec méthodes allocation/libération
- [ ] Modèle `PortAllocation` (port, timestamp, allocation status)
- [ ] Enum `AllocationStatus` (Available, Allocated, Reserved, Error)
- [ ] Extension EEPROM : `ReadSystemInfoAsync()` pour données client
- [ ] Modèle `SystemInfo` pour données hardware
- [ ] Tests unitaires de tous les modèles

**Livrables :**
- Interfaces pool définies
- Modèles d'allocation complets
- EEPROM system info functional

#### **ÉTAPE 4: Pool Implementation avec System Info**
**Objectif :** Pool functional avec allocation/libération thread-safe

**Tasks :**
- [ ] Classe `SerialPortPool : ISerialPortPool`
- [ ] Méthodes `AllocatePortAsync()` et `ReleasePortAsync()`
- [ ] Thread-safety avec locks appropriés
- [ ] Validation FTDI avant allocation
- [ ] Integration system info dans allocation
- [ ] Tests : Allocation/libération fonctionne correctement

**Livrables :**
- Pool basique functional
- Allocation thread-safe
- System info intégré

### **🔹 ÉTAPE 5-6: Multi-Port Awareness (Semaine 3)**

#### **ÉTAPE 5: Multi-Port Detection avec System Info**
**Objectif :** Comprendre qu'un device 4232H = groupe de ports

**Tasks :**
- [ ] Étendre `FtdiDeviceReader` pour grouper ports par device
- [ ] Nouvelle méthode `GetPortGroupsAsync()`
- [ ] Identifier ports du même device physique 4232H
- [ ] System info au niveau device (partagé sur tous ports)
- [ ] Tests : Détecter groupes de ports correctly

**Livrables :**
- Multi-port detection functional
- Grouping par device physique
- System info cohérent par device

#### **ÉTAPE 6: Pool Avancé Multi-Port + Polish**
**Objectif :** Pool intelligent avec awareness multi-port

**Tasks :**
- [ ] Pool comprend les groupes de ports
- [ ] Allocation préférentielle sur devices avec ports libres  
- [ ] Métadonnées device dans allocation
- [ ] Logging enrichi avec hardware info
- [ ] Tests end-to-end complets
- [ ] Documentation utilisateur

**Livrables :**
- Pool intelligent multi-port
- Documentation complète
- Tests end-to-end validés

---

## 🧪 **Tests Sprint 3**

### **Test Cases Prévus :**
- **TC-021-025:** Service integration (5 tests)
- **TC-026-030:** EEPROM system info (5 tests)  
- **TC-031-035:** Pool allocation basique (5 tests)
- **TC-036-040:** Multi-port detection (5 tests)
- **TC-041-045:** End-to-end scenarios (5 tests)

**Total :** 25 nouveaux tests

### **Hardware Testing :**
- COM6 (FT232R) pour validation dev config
- Simulation multi-port pour tests 4232H
- Service integration tests réels

---

## ⚡ **Risques et Mitigation**

### **🚨 Risque #1 : Service Integration Breaking Changes**
**Mitigation :**
- Approche incrémentale avec rollback possible
- Tests d'intégration intensive à chaque step
- Validation que le service existant continue de fonctionner

### **🚨 Risque #2 : Background Service Performance**
**Mitigation :**
- Discovery throttling (30s intervals)
- Proper cancellation et resource cleanup
- Memory leak monitoring

### **🚨 Risque #3 : Thread Safety Pool**
**Mitigation :**
- Simple locking strategy d'abord
- Extensive concurrent testing
- Clear allocation/deallocation patterns

---

## 📊 **Success Criteria**

### **Must Have :**
✅ Service Windows intègre Enhanced Discovery sans régression  
✅ Pool allocation/libération functional et thread-safe  
✅ Multi-port device detection working  
✅ EEPROM system info reading extended  
✅ 25 nouveaux tests passent  

### **Nice to Have :**
🎯 Performance monitoring basique  
🎯 Configuration hot-reload  
🎯 Advanced error recovery  

---

## 🚀 **Sprint 4 Preview**

**Focus Sprint 4 :**
- Bit bang port exclusion
- API REST endpoints
- Monitoring & métriques avancées
- High availability features

---

## 📅 **Timeline**

**Semaine 1 :** ÉTAPE 1-2 (Service Integration)  
**Semaine 2 :** ÉTAPE 3-4 (Pool + EEPROM)  
**Semaine 3 :** ÉTAPE 5-6 (Multi-Port + Polish)  

**Milestone Reviews :** Fin de chaque semaine  
**Sprint Review :** Fin semaine 3  

---

*Document créé : 18 Juillet 2025*  
*Sprint 3 Status : 🔄 READY TO START*