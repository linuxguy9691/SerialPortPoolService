# Configuration XML - Sprint 9 Documentation Complète

## 🚀 Aperçu - Sprint 9 Enhanced

Cette documentation décrit la syntaxe XML complète pour Sprint 9 avec :
- ✅ **Multi-Level Validation** (PASS/WARN/FAIL/CRITICAL)
- 🔌 **Bit Bang Protocol Hooks** (Hardware Integration)
- 🎯 **Enhanced Workflow Control** (continue_on_failure granulaire)
- 🔧 **Backward Compatibility** (Sprint 7-8 configs still work)

## 📋 Structure Hiérarchique Sprint 9

```
ROOT
├── BIB (Board Interface Box)
│   ├── METADATA (Enhanced Sprint 9)
│   ├── HARDWARE_CONFIG (🆕 NEW - Bit Bang Protocol)
│   │   └── BIT_BANG_PROTOCOL (GPIO Configuration)
│   └── UUT (Unit Under Test)
│       ├── METADATA (Enhanced)
│       └── PORT (Communication Port)
│           ├── PROTOCOL (rs232, rs485, etc.)
│           ├── WORKFLOW_CONTROL (🆕 NEW - Enhanced Control)
│           ├── START (Phase d'initialisation)
│           ├── TEST (Phase de test avec multi-level)
│           └── STOP (Phase d'arrêt)
```

## 🔧 Configuration Complète Sprint 9

### Structure XML Complète avec Toutes les Fonctionnalités

```xml
<?xml version="1.0" encoding="UTF-8"?>
<root>
  <bib id="sprint9_demo" description="Sprint 9 Complete Demo - Multi-Level + Hardware">
    <!-- 📊 ENHANCED METADATA (Sprint 9) -->
    <metadata>
      <version>9.0.0</version>
      <sprint>9</sprint>
      <client>PRODUCTION_CLIENT_V9</client>
      <location>Montreal, QC</location>
      <validation_system>multi_level</validation_system>
      <hardware_integration>bit_bang_protocol</hardware_integration>
      <features>4_level_classification,hardware_hooks,enhanced_workflow_control</features>
    </metadata>
    
    <!-- 🔌 NEW: HARDWARE CONFIGURATION (Bit Bang Protocol) -->
    <hardware_config>
      <bit_bang_protocol enabled="true">
        <!-- Device identification -->
        <device_id>FT4232H_A</device_id>
        <serial_number>FT9A9OFOA</serial_number>
        
        <!-- 📡 INPUT BITS (Client Requirements) -->
        <input_bits>
          <power_on_ready bit="0" active_low="false" />
          <power_down_heads_up bit="1" active_low="false" />
        </input_bits>
        
        <!-- 📡 OUTPUT BITS (Client Requirements) -->
        <output_bits>
          <critical_fail_signal bit="2" active_low="false" />
          <workflow_active bit="3" active_low="false" />
        </output_bits>
        
        <!-- 🔧 TIMING CONFIGURATION -->
        <timing>
          <polling_interval_ms>100</polling_interval_ms>
          <signal_hold_time_ms>1000</signal_hold_time_ms>
          <auto_clear_signals>true</auto_clear_signals>
        </timing>
      </bit_bang_protocol>
    </hardware_config>
    
    <uut id="production_uut" description="Sprint 9 Production UUT - Multi-Level + Hardware">
      <!-- 📊 ENHANCED UUT METADATA -->
      <metadata>
        <hardware_revision>Rev.9.0</hardware_revision>
        <firmware_version>v9.1.0</firmware_version>
        <validation_levels>4</validation_levels>
        <hardware_hooks>enabled</hardware_hooks>
      </metadata>
      
      <port number="1">
        <protocol>rs232</protocol>
        <speed>115200</speed>
        <data_pattern>n81</data_pattern>
        <read_timeout>3000</read_timeout>
        <write_timeout>3000</write_timeout>
        <handshake>None</handshake>
        <rts_enable>false</rts_enable>
        <dtr_enable>true</dtr_enable>
        
        <!-- 🆕 NEW: ENHANCED WORKFLOW CONTROL (Sprint 9) -->
        <workflow_control>
          <!-- Hardware integration control -->
          <wait_for_power_on_ready>true</wait_for_power_on_ready>
          <monitor_power_down_heads_up>true</monitor_power_down_heads_up>
          <signal_critical_fail>true</signal_critical_fail>
          <signal_workflow_active>true</signal_workflow_active>
          
          <!-- Multi-level validation control -->
          <continue_on_warn>true</continue_on_warn>
          <continue_on_fail>false</continue_on_fail>
          <continue_on_critical>false</continue_on_critical>
          <emergency_stop_on_critical>true</emergency_stop_on_critical>
          
          <!-- Timing control -->
          <power_on_ready_timeout_ms>30000</power_on_ready_timeout_ms>
          <power_down_grace_period_ms>5000</power_down_grace_period_ms>
        </workflow_control>
        
        <!-- ✅ START PHASE: Enhanced with Hardware Awareness -->
        <start continue_on_failure="false">
          <command>INIT_SYSTEM\r\n</command>
          
          <!-- Primary success pattern (PASS level) -->
          <expected_response regex="true">^SYSTEM:(READY|INITIALIZED|OK)(\r\n)?$</expected_response>
          
          <!-- 🎯 MULTI-LEVEL VALIDATION (Sprint 9) -->
          <validation_levels>
            <warn regex="true">^SYSTEM:(SLOW_START|TEMP_HIGH|LOW_BATTERY)(\r\n)?$</warn>
            <fail regex="true">^SYSTEM:(ERROR|FAIL|TIMEOUT)(\r\n)?$</fail>
            <critical trigger_hardware="true" stop_workflow="true" regex="true">^SYSTEM:(CRITICAL|EMERGENCY|OVERVOLTAGE)(\r\n)?$</critical>
          </validation_levels>
          
          <timeout_ms>3000</timeout_ms>
          <retry_count>1</retry_count>
        </start>
        
        <!-- 🧪 TEST PHASE: Multi-Level + Power Down Monitoring -->
        <test continue_on_failure="true">
          <command>RUN_TESTS\r\n</command>
          
          <!-- Primary success pattern -->
          <expected_response regex="true">^TESTS:(PASS|SUCCESS|ALL_OK)(\r\n)?$</expected_response>
          
          <!-- 🎯 ADVANCED MULTI-LEVEL PATTERNS -->
          <validation_levels>
            <!-- ⚠️ WARN: Continue workflow with alert -->
            <warn continue_on_failure="true" regex="true">^TESTS:(MINOR_ISSUES|WARNINGS|PARTIAL_PASS)(\r\n)?$</warn>
            
            <!-- ❌ FAIL: Continue if continue_on_failure="true" -->
            <fail continue_on_failure="true" regex="true">^TESTS:(FAIL|MAJOR_ERRORS|INCOMPLETE)(\r\n)?$</fail>
            
            <!-- 🚨 CRITICAL: Emergency stop + hardware signal -->
            <critical 
              trigger_hardware="true" 
              stop_workflow="true" 
              continue_on_failure="false" 
              regex="true">^TESTS:(CRITICAL_FAIL|SAFETY_VIOLATION|HARDWARE_DAMAGE)(\r\n)?$</critical>
          </validation_levels>
          
          <timeout_ms>10000</timeout_ms>
          <retry_count>0</retry_count>
        </test>
        
        <!-- 🔌 STOP PHASE: Enhanced Cleanup with Hardware -->
        <stop continue_on_failure="true">
          <command>SHUTDOWN\r\n</command>
          
          <!-- Primary success pattern -->
          <expected_response regex="true">^SHUTDOWN:(OK|COMPLETE|BYE|GOODBYE)(\r\n)?$</expected_response>
          
          <!-- 🎯 SHUTDOWN VALIDATION LEVELS -->
          <validation_levels>
            <warn regex="true">^SHUTDOWN:(SLOW|FORCED|WITH_WARNINGS)(\r\n)?$</warn>
            <fail continue_on_failure="true" regex="true">^SHUTDOWN:(ERROR|TIMEOUT|INCOMPLETE)(\r\n)?$</fail>
            <critical trigger_hardware="true" regex="true">^SHUTDOWN:(EMERGENCY|UNSAFE|HARDWARE_STUCK)(\r\n)?$</critical>
          </validation_levels>
          
          <timeout_ms>5000</timeout_ms>
          <retry_count>2</retry_count>
        </stop>
      </port>
    </uut>
  </bib>
</root>
```

## ✨ Sprint 9 - Nouvelles Fonctionnalités

### 🎯 Multi-Level Validation System

#### **Syntaxe de Base**
```xml
<validation_levels>
  <warn regex="true" options="IgnoreCase">PATTERN_WARN</warn>
  <fail regex="true" continue_on_failure="true">PATTERN_FAIL</fail>
  <critical trigger_hardware="true" stop_workflow="true" regex="true">PATTERN_CRITICAL</critical>
</validation_levels>
```

#### **Attributs par Niveau**

| Niveau | Attributs Disponibles | Description |
|--------|-----------------------|-------------|
| **warn** | `regex`, `options`, `continue_on_failure` | Continue automatiquement, log alert |
| **fail** | `regex`, `options`, `continue_on_failure` | Stop workflow (sauf si continue_on_failure="true") |
| **critical** | `regex`, `options`, `trigger_hardware`, `stop_workflow` | Emergency stop + hardware signal |

### 🔌 Bit Bang Protocol Configuration

#### **Hardware Configuration Complete**
```xml
<hardware_config>
  <bit_bang_protocol enabled="true">
    <!-- Device identification -->
    <device_id>FT4232H_A</device_id>
    <serial_number>FT9A9OFOA</serial_number>
    
    <!-- INPUT BITS: Hardware state monitoring -->
    <input_bits>
      <power_on_ready bit="0" active_low="false" debounce_ms="50" />
      <power_down_heads_up bit="1" active_low="false" debounce_ms="100" />
      <emergency_stop bit="2" active_low="true" debounce_ms="10" />
    </input_bits>
    
    <!-- OUTPUT BITS: Hardware control signals -->
    <output_bits>
      <critical_fail_signal bit="3" active_low="false" pulse_width_ms="1000" />
      <workflow_active bit="4" active_low="false" />
      <test_in_progress bit="5" active_low="false" />
    </output_bits>
    
    <!-- TIMING: Hardware polling and signal control -->
    <timing>
      <polling_interval_ms>100</polling_interval_ms>
      <signal_hold_time_ms>1000</signal_hold_time_ms>
      <auto_clear_signals>true</auto_clear_signals>
      <max_signal_duration_ms>5000</max_signal_duration_ms>
    </timing>
  </bit_bang_protocol>
</hardware_config>
```

### 🔧 Enhanced Workflow Control

#### **Global Workflow Control**
```xml
<workflow_control>
  <!-- Hardware integration -->
  <wait_for_power_on_ready>true</wait_for_power_on_ready>
  <monitor_power_down_heads_up>true</monitor_power_down_heads_up>
  <signal_critical_fail>true</signal_critical_fail>
  
  <!-- Multi-level behavior -->
  <continue_on_warn>true</continue_on_warn>
  <continue_on_fail>false</continue_on_fail>
  <continue_on_critical>false</continue_on_critical>
  <emergency_stop_on_critical>true</emergency_stop_on_critical>
  
  <!-- Timeouts -->
  <power_on_ready_timeout_ms>30000</power_on_ready_timeout_ms>
  <power_down_grace_period_ms>5000</power_down_grace_period_ms>
  <critical_signal_timeout_ms>2000</critical_signal_timeout_ms>
</workflow_control>
```

#### **Per-Command Control**
```xml
<test continue_on_failure="true" timeout_behavior="graceful">
  <command>TEST_COMMAND</command>
  
  <!-- Override global settings per level -->
  <validation_levels>
    <warn continue_on_failure="true">WARN_PATTERN</warn>
    <fail continue_on_failure="true">FAIL_PATTERN</fail>  <!-- Override global -->
    <critical continue_on_failure="false">CRITICAL_PATTERN</critical>  <!-- Safety override -->
  </validation_levels>
</test>
```

## 📊 Exemples Pratiques Sprint 9

### **Exemple 1: Production Test avec Hardware Complet**
```xml
<?xml version="1.0" encoding="UTF-8"?>
<root>
  <bib id="production_test" description="Production Test with Full Hardware Integration">
    <hardware_config>
      <bit_bang_protocol enabled="true">
        <device_id>FT4232H_PROD</device_id>
        <input_bits>
          <power_on_ready bit="0" />
          <emergency_stop bit="1" active_low="true" />
        </input_bits>
        <output_bits>
          <critical_fail_signal bit="2" />
          <test_in_progress bit="3" />
        </output_bits>
      </bit_bang_protocol>
    </hardware_config>
    
    <uut id="production_board">
      <port number="1">
        <protocol>rs232</protocol>
        <speed>115200</speed>
        
        <workflow_control>
          <wait_for_power_on_ready>true</wait_for_power_on_ready>
          <continue_on_fail>false</continue_on_fail>
        </workflow_control>
        
        <test continue_on_failure="false">
          <command>PRODUCTION_TEST\r\n</command>
          <expected_response regex="true">^TEST:PASS(\r\n)?$</expected_response>
          
          <validation_levels>
            <warn regex="true">^TEST:(PASS_WITH_WARNINGS|MARGINAL)(\r\n)?$</warn>
            <fail regex="true">^TEST:(FAIL|ERROR)(\r\n)?$</fail>
            <critical trigger_hardware="true" regex="true">^TEST:(CRITICAL_FAIL|SAFETY_FAIL)(\r\n)?$</critical>
          </validation_levels>
        </test>
      </port>
    </uut>
  </bib>
</root>
```

### **Exemple 2: Debug/Development Mode**
```xml
<test continue_on_failure="true">  <!-- Continue pour voir tous les résultats -->
  <command>DEBUG_ALL_TESTS\r\n</command>
  <expected_response regex="true">ALL_TESTS_COMPLETE</expected_response>
  
  <validation_levels>
    <!-- En mode debug, continue même sur FAIL pour collecter plus d'info -->
    <warn continue_on_failure="true" regex="true">DEBUG:WARN.*</warn>
    <fail continue_on_failure="true" regex="true">DEBUG:FAIL.*</fail>
    <critical continue_on_failure="false" trigger_hardware="false" regex="true">DEBUG:CRITICAL.*</critical>
  </validation_levels>
</test>
```

### **Exemple 3: Client Demo (Votre Configuration)**
```xml
<?xml version="1.0" encoding="UTF-8"?>
<root>
  <bib id="client_demo" description="Client Demo - Sprint 9 Enhanced">
    <metadata>
      <version>9.0.0</version>
      <client>ENHANCED_PRODUCTION_CLIENT</client>
    </metadata>
    
    <uut id="production_uut">
      <port number="1">
        <protocol>rs232</protocol>
        <speed>115200</speed>
        <data_pattern>n81</data_pattern>
        
        <start>
          <command>INIT_RS232</command>
          <expected_response>READY</expected_response>
          <timeout_ms>3000</timeout_ms>
        </start>
        
        <test continue_on_failure="true">  <!-- ✅ Solution à votre problème -->
          <command>TEST</command>
          <expected_response>PASS</expected_response>
          
          <!-- ✨ Multi-level demo patterns -->
          <validation_levels>
            <warn regex="true">^PASS(\r\n)?$</warn>
            <fail regex="true">^PASS(\r\n)?$</fail>
            <critical trigger_hardware="true" regex="true">^CRITICAL.*(\r\n)?$</critical>
          </validation_levels>
          
          <timeout_ms>5000</timeout_ms>
        </test>
        
        <stop>
          <command>AT+QUIT</command>
          <expected_response>GOODBYE</expected_response>  <!-- ✅ Corrigé -->
          <timeout_ms>2000</timeout_ms>
        </stop>
      </port>
    </uut>
  </bib>
</root>
```

## 🎯 Migration Guide - Sprint 8 → Sprint 9

### **Configuration Sprint 8 (Ancien)**
```xml
<test>
  <command>TEST</command>
  <expected_response regex="true">^(PASS|OK)$</expected_response>
</test>
```

### **Configuration Sprint 9 (Enhanced)**
```xml
<test continue_on_failure="true">
  <command>TEST</command>
  <expected_response regex="true">^PASS$</expected_response>  <!-- PASS level -->
  
  <!-- ✨ NEW: Multi-level patterns -->
  <validation_levels>
    <warn regex="true">^(OK|MARGINAL)$</warn>
    <fail regex="true">^FAIL$</fail>
    <critical trigger_hardware="true" regex="true">^CRITICAL$</critical>
  </validation_levels>
</test>
```

## 🔄 Backward Compatibility

### **Sprint 7-8 Configs Still Work**
```xml
<!-- ✅ This still works in Sprint 9 -->
<test>
  <command>SIMPLE_TEST</command>
  <expected_response>PASS</expected_response>  <!-- Simple string match -->
</test>

<!-- ✅ This also still works -->
<test>
  <command>REGEX_TEST</command>
  <expected_response regex="true">^PASS.*$</expected_response>  <!-- Sprint 8 regex -->
</test>
```

### **Enhanced with Sprint 9 Features**
```xml
<!-- 🚀 Enhanced with multi-level (optional) -->
<test continue_on_failure="true">
  <command>ENHANCED_TEST</command>
  <expected_response>PASS</expected_response>  <!-- Primary PASS -->
  
  <!-- Optional: Add multi-level validation -->
  <validation_levels>
    <warn regex="true">WARN.*</warn>
    <fail regex="true">FAIL.*</fail>
  </validation_levels>
</test>
```

## 🚨 Best Practices Sprint 9

### **1. Sécurité CRITICAL Level**
```xml
<!-- ✅ TOUJOURS: CRITICAL = Emergency Stop -->
<critical trigger_hardware="true" stop_workflow="true" continue_on_failure="false">
  CRITICAL_PATTERN
</critical>

<!-- ❌ JAMAIS: Continue sur CRITICAL -->
<critical continue_on_failure="true">CRITICAL_PATTERN</critical>
```

### **2. Hardware Integration**
```xml
<!-- ✅ BONNES PRATIQUES -->
<bit_bang_protocol enabled="true">
  <timing>
    <polling_interval_ms>100</polling_interval_ms>  <!-- Pas trop fréquent -->
    <auto_clear_signals>true</auto_clear_signals>   <!-- Nettoyage automatique -->
  </timing>
</bit_bang_protocol>
```

### **3. Multi-Level Patterns**
```xml
<!-- ✅ SPÉCIFIQUE et ORDONNÉ -->
<validation_levels>
  <warn regex="true">^STATUS:(WARN|MARGINAL)(\r\n)?$</warn>        <!-- Spécifique -->
  <fail regex="true">^STATUS:(FAIL|ERROR)(\r\n)?$</fail>          <!-- Spécifique -->
  <critical regex="true">^STATUS:(CRITICAL|EMERGENCY)(\r\n)?$</critical> <!-- Spécifique -->
</validation_levels>

<!-- ❌ TROP PERMISSIF -->
<validation_levels>
  <warn regex="true">.*WARN.*</warn>     <!-- Trop large -->
  <fail regex="true">.*</fail>           <!-- Matche tout -->
</validation_levels>
```

## 🔧 Troubleshooting

### **Problèmes Courants et Solutions**

1. **continue_on_failure n'est pas respecté**
   ```xml
   <!-- Solution: Vérifier la position de l'attribut -->
   <test continue_on_failure="true">  <!-- ✅ Bon -->
   ```

2. **Hardware trigger ne fonctionne pas**
   ```xml
   <!-- Solution: Vérifier hardware_config ET trigger_hardware -->
   <hardware_config>
     <bit_bang_protocol enabled="true">...</bit_bang_protocol>
   </hardware_config>
   
   <critical trigger_hardware="true">PATTERN</critical>
   ```

3. **Multi-level patterns se chevauchent**
   ```xml
   <!-- Solution: Patterns spécifiques et mutuellement exclusifs -->
   <validation_levels>
     <warn regex="true">^TEST:WARN(\r\n)?$</warn>     <!-- Spécifique -->
     <fail regex="true">^TEST:FAIL(\r\n)?$</fail>     <!-- Spécifique -->
     <critical regex="true">^TEST:CRITICAL(\r\n)?$</critical> <!-- Spécifique -->
   </validation_levels>
   ```

---

**Sprint 9 XML Configuration = Multi-Level Intelligence + Hardware Integration! 🚀**

*Documentation complète et à jour - Sprint 9*  
*Includes: Multi-Level Validation + Bit Bang Protocol Hooks + Enhanced Workflow Control*