# Dummy UUT Simulator - Sprint 5 Demo Support

![Python](https://img.shields.io/badge/Python-3.7%2B-blue.svg)
![Serial](https://img.shields.io/badge/pyserial-3.5%2B-green.svg)
![Demo](https://img.shields.io/badge/Sprint%205-Demo%20Ready-brightgreen.svg)

## 🎯 **Overview**

Le **Dummy UUT Simulator** est un simulateur Python qui émule un device de test (UUT - Unit Under Test) pour démontrer les capacités de communication RS232 du SerialPortPool sans nécessiter de hardware réel.

### **🚀 Pourquoi ce Dummy UUT ?**
- ✅ **Tests fiables** - Réponses predictibles et controlées
- ✅ **Setup rapide** - Aucun hardware physique requis
- ✅ **Demo portable** - Fonctionne sur n'importe quel système
- ✅ **Configuration flexible** - Commandes et réponses customisables
- ✅ **Debugging facile** - Logs détaillés de toutes les communications

---

## 📋 **Quick Start - 3 Minutes Setup**

### **1. Installation Dependencies**
```bash
# Clone du repository (si pas déjà fait)
cd SerialPortPoolService/tests/DummyUUT/

# Installation des dépendances Python
pip install -r requirements.txt
```

### **2. Démarrage Rapide**
```bash
# Démarrage avec configuration par défaut (COM5, 115200 baud)
python dummy_uut.py

# Ou avec port spécifique pour démo
python dummy_uut.py --port COM8 --baud 115200
```

### **3. Vérification**
```bash
# Vous devriez voir:
🔌 Dummy UUT started on COM8 @ 115200 baud
📋 Device State: ONLINE
🎯 Supported Commands: ['ATZ', 'INIT_RS232', 'AT+STATUS', ...]
📡 Ready for SerialPortPool testing...
```

---

## 🔧 **Configuration & Usage**

### **Options de Ligne de Commande**
```bash
# Options disponibles
python dummy_uut.py --help

# Exemples d'usage
python dummy_uut.py --port COM8                    # Port spécifique
python dummy_uut.py --port COM8 --baud 9600        # Port + baud rate
python dummy_uut.py --port COM8 --duration 300     # Arrêt auto après 5 min
```

### **Ports Recommandés par Plateforme**
```bash
# Windows
python dummy_uut.py --port COM8    # Généralement libre
python dummy_uut.py --port COM9    # Alternative
python dummy_uut.py --port COM10   # Alternative

# Linux
python dummy_uut.py --port /dev/ttyUSB0
python dummy_uut.py --port /dev/ttyACM0

# macOS
python dummy_uut.py --port /dev/cu.usbserial-*
```

---

## 📡 **Commandes Supportées**

### **Commandes de Base (Sprint 5)**
| Commande | Réponse | Usage |
|----------|---------|-------|
| `ATZ\r\n` | `OK\r\n` | Reset/initialization standard |
| `INIT_RS232\r\n` | `READY\r\n` | Initialization phase demo |
| `AT+STATUS\r\n` | `STATUS_OK\r\n` | Status check |
| `RUN_TEST_1\r\n` | `PASS\r\n` | Test execution demo |
| `TEST\r\n` | `PASS\r\n` | Simple test command |
| `AT+QUIT\r\n` | `GOODBYE\r\n` | Graceful shutdown |
| `STOP_RS232\r\n` | `BYE\r\n` | Stop sequence demo |
| `EXIT\r\n` | `BYE\r\n` | Exit command |

### **Gestion des Erreurs**
- **Commande inconnue** → `ERROR: Unknown command\r\n`
- **Timeout** → Pas de réponse (simulation realistic)
- **Port occupé** → Message d'erreur au démarrage

---

## 🎬 **Integration avec Demo Application**

### **Configuration XML Demo**
```xml
<!-- tests/RS232Demo/Configuration/demo-config.xml -->
<root>
  <bib id="bib_demo">
    <uut id="uut_python_simulator">
      <port number="1">
        <protocol>rs232</protocol>
        <speed>115200</speed>
        <data_pattern>n81</data_pattern>
        <start>
          <command>INIT_RS232\r\n</command>
          <expected_response>READY</expected_response>
          <timeout_ms>3000</timeout_ms>
        </start>
        <test>
          <command>RUN_TEST_1\r\n</command>
          <expected_response>PASS</expected_response>
          <timeout_ms>5000</timeout_ms>
        </test>
        <stop>
          <command>STOP_RS232\r\n</command>
          <expected_response>BYE</expected_response>
          <timeout_ms>2000</timeout_ms>
        </stop>
      </port>
    </uut>
  </bib>
</root>
```

### **Workflow Demo Complet**
```bash
# Terminal 1: Démarrer dummy UUT
python dummy_uut.py --port COM8

# Terminal 2: Démarrer demo application
cd tests/RS232Demo/
dotnet run

# Résultat attendu: Workflow 3-phases complet
```

---

## 🔍 **Troubleshooting**

### **Problèmes Courants**

#### **❌ "Port COM8 not available"**
```bash
# Solution 1: Vérifier ports disponibles
python -c "import serial.tools.list_ports; print([p.device for p in serial.tools.list_ports.comports()])"

# Solution 2: Utiliser port différent
python dummy_uut.py --port COM9

# Solution 3: Créer port virtuel (Windows)
# Installer com0com: https://sourceforge.net/projects/com0com/
```

#### **❌ "Permission denied" (Linux/macOS)**
```bash
# Ajouter user au groupe dialout
sudo usermod -a -G dialout $USER
# Puis redémarrer session

# Ou utiliser sudo (temporaire)
sudo python dummy_uut.py --port /dev/ttyUSB0
```

#### **❌ "ModuleNotFoundError: No module named 'serial'"**
```bash
# Installer pyserial
pip install pyserial

# Ou avec requirements.txt
pip install -r requirements.txt
```

### **Validation du Setup**
```bash
# Test rapide de fonctionnement
python -c "
import serial
import serial.tools.list_ports
print('✅ pyserial installed successfully')
print('📡 Available ports:', [p.device for p in serial.tools.list_ports.comports()])
"
```

---

## 🚀 **Advanced Usage**

### **Commandes Custom**
```python
# Ajouter commandes personnalisées via code
dummy = DummyUUT("COM8")
dummy.add_command("CUSTOM_CMD\r\n", "CUSTOM_RESPONSE\r\n")
dummy.start()
```

### **Configuration pour Tests Spécifiques**
```bash
# Test de timeout (ne répond pas à certaines commandes)
python dummy_uut.py --port COM8 --simulate-timeout

# Test de latence (réponses lentes)
python dummy_uut.py --port COM8 --response-delay 500

# Mode verbose pour debugging
python dummy_uut.py --port COM8 --verbose
```

### **Integration avec Tests Automatisés**
```python
# Démarrage programmatique pour tests
import subprocess
import time

# Démarrer dummy UUT
process = subprocess.Popen(["python", "dummy_uut.py", "--port", "COM8"])
time.sleep(2)  # Attendre démarrage

# Exécuter tests
# ... vos tests ici ...

# Arrêter dummy UUT
process.terminate()
```

---

## 📊 **Monitoring & Logs**

### **Output Logs Standard**
```bash
2025-07-30 14:30:15 - INFO - 🔌 Dummy UUT started on COM8 @ 115200 baud
2025-07-30 14:30:15 - INFO - 📋 Device State: ONLINE
2025-07-30 14:30:22 - INFO - 📥 RX: 'INIT_RS232'
2025-07-30 14:30:22 - INFO - 📤 TX: 'READY'
2025-07-30 14:30:25 - INFO - 📥 RX: 'RUN_TEST_1'
2025-07-30 14:30:25 - INFO - 📤 TX: 'PASS'
```

### **Statistiques de Session**
```python
# Métriques disponibles via API
status = dummy.get_status()
print(f"Commands received: {status['commands_received']}")
print(f"Responses sent: {status['responses_sent']}")
print(f"Uptime: {status['uptime_seconds']}s")
```

---

## 🎯 **Sprint 5 Demo Integration**

### **Scénario Demo Standard**
1. **🤖 Démarrer Dummy UUT** sur COM8
2. **🎬 Lancer Demo Application** 
3. **📋 Sélectionner "Python Simulator Demo"**
4. **🚀 Exécuter workflow BIB_DEMO → UUT_PYTHON_SIMULATOR**
5. **