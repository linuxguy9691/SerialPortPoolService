<!-- ===================================================================
     SPRINT 11 DEMO CONFIGURATION FILES
     ===================================================================

     File 1: Configuration/examples/bib_client_demo1.xml
     ================================================================= -->

<?xml version="1.0" encoding="UTF-8"?>
<bib id="client_demo1" description="Sprint 11 Demo - Individual BIB File #1">
  <metadata>
    <board_type>production</board_type>
    <revision>v11.0</revision>
    <client>CLIENT_DEMO1_SPRINT11</client>
    <features>multi_file_isolation,individual_config</features>
    <file_type>individual</file_type>
    <demo_line>1</demo_line>
  </metadata>
  
  <uut id="demo1_production_uut" description="Demo Line 1 - Production UUT with Multi-Level Validation">
    <port number="1">
      <protocol>rs232</protocol>
      <speed>115200</speed>
      <data_pattern>n81</data_pattern>
      <auto_discover>true</auto_discover>
      
      <!-- ✅ START sequence - Demo Line 1 -->
      <start>
        <command>ATZ</command>
        <expected_response>OK</expected_response>
        <timeout_ms>3000</timeout_ms>
      </start>

      <start>
        <command>INIT_DEMO1</command>
        <expected_response>READY_DEMO1</expected_response>
        <timeout_ms>3000</timeout_ms>
      </start>
      
      <!-- ✨ TEST sequence - Demo Line 1 specific -->
      <test continue_on_failure="true">
        <command>TEST_DEMO1</command>
        <expected_response>PASS_DEMO1</expected_response>
        <timeout_ms>5000</timeout_ms>
      </test>
      
      <!-- 🎯 Multi-level validation example -->
      <test continue_on_failure="true">
        <command>TEST_DEMO1</command>
        <expected_response>NEVER_MATCH_THIS</expected_response>
        
        <validation_levels>
          <warn regex="true">^PASS_DEMO1(\r\n)?$</warn>
          <fail regex="true">^(ERROR_DEMO1|FAIL_DEMO1)(\r\n)?$</fail>
          <critical regex="true">^(CRITICAL_DEMO1|EMERGENCY_DEMO1)(\r\n)?$</critical>
        </validation_levels>
        
        <timeout_ms>5000</timeout_ms>
      </test>
      
      <!-- ✅ STOP sequence - Demo Line 1 -->
      <stop>
        <command>AT+QUIT_DEMO1</command>
        <expected_response>GOODBYE_DEMO1</expected_response>
        <timeout_ms>2000</timeout_ms>
      </stop>

      <stop>
        <command>EXIT_DEMO1</command>
        <expected_response>BYE_DEMO1</expected_response>
        <timeout_ms>2000</timeout_ms>
      </stop>
    </port>
  </uut>
</bib>

<!-- ===================================================================
     File 2: Configuration/examples/bib_client_demo2.xml
     ================================================================= -->

<?xml version="1.0" encoding="UTF-8"?>
<bib id="client_demo2" description="Sprint 11 Demo - Individual BIB File #2">
  <metadata>
    <board_type>production</board_type>
    <revision>v11.0</revision>
    <client>CLIENT_DEMO2_SPRINT11</client>
    <features>multi_file_isolation,individual_config</features>
    <file_type>individual</file_type>
    <demo_line>2</demo_line>
  </metadata>
  
  <uut id="demo2_production_uut" description="Demo Line 2 - Production UUT with Different Configuration">
    <port number="1">
      <protocol>rs232</protocol>
      <speed>115200</speed>
      <data_pattern>n81</data_pattern>
      <auto_discover>true</auto_discover>
      
      <!-- ✅ START sequence - Demo Line 2 -->
      <start>
        <command>ATZ</command>
        <expected_response>OK</expected_response>
        <timeout_ms>3000</timeout_ms>
      </start>

      <start>
        <command>INIT_DEMO2</command>
        <expected_response>READY_DEMO2</expected_response>
        <timeout_ms>3000</timeout_ms>
      </start>
      
      <!-- ✨ TEST sequence - Demo Line 2 specific -->
      <test continue_on_failure="true">
        <command>TEST_DEMO2</command>
        <expected_response>PASS_DEMO2</expected_response>
        <timeout_ms>5000</timeout_ms>
      </test>
      
      <!-- 🎯 Different multi-level validation for demo2 -->
      <test continue_on_failure="true">
        <command>TEST_DEMO2</command>
        <expected_response>NEVER_MATCH_THIS</expected_response>
        
        <validation_levels>
          <warn regex="true">^(SLOW_DEMO2|WARNING_DEMO2)(\r\n)?$</warn>
          <fail regex="true">^PASS_DEMO2(\r\n)?$</fail>
          <critical regex="true" trigger_hardware="true">^(CRITICAL_DEMO2|EMERGENCY_DEMO2)(\r\n)?$</critical>
        </validation_levels>
        
        <timeout_ms>5000</timeout_ms>
      </test>
      
      <!-- ✅ STOP sequence - Demo Line 2 -->
      <stop>
        <command>AT+QUIT_DEMO2</command>
        <expected_response>GOODBYE_DEMO2</expected_response>
        <timeout_ms>2000</timeout_ms>
      </stop>

      <stop>
        <command>EXIT_DEMO2</command>
        <expected_response>BYE_DEMO2</expected_response>
        <timeout_ms>2000</timeout_ms>
      </stop>
    </port>
  </uut>
</bib>

<!-- ===================================================================
     File 3: Configuration/examples/README-Examples.md
     ================================================================= -->

# 🚀 Sprint 11 - Configuration Examples

## 📋 Individual BIB File Examples

This directory contains example individual BIB configuration files demonstrating the Sprint 11 multi-file system.

### **Files in this directory:**
- `bib_client_demo1.xml` - Demo Line 1 configuration
- `bib_client_demo2.xml` - Demo Line 2 configuration

### **Key Differences from Legacy Format:**

#### **Legacy Format (client-demo.xml):**
```xml
<root>
  <bib id="client_demo" description="...">
    <!-- BIB content -->
  </bib>
</root>
```

#### **Individual Format (Sprint 11):**
```xml
<bib id="client_demo1" description="...">
  <!-- Same BIB content, no root wrapper -->
</bib>
```

### **Testing the Examples:**

#### **1. Discover Example BIBs**
```bash
SerialPortPoolService.exe --config-dir "Configuration/examples/" --discover-bibs
```

#### **2. Execute Specific Demo BIB**
```bash
SerialPortPoolService.exe --bib-ids "client_demo1" --config-dir "Configuration/examples/"
```

#### **3. Execute Both Demo BIBs**
```bash
SerialPortPoolService.exe --bib-ids "client_demo1,client_demo2" --config-dir "Configuration/examples/"
```

#### **4. Execute All Discovered BIBs**
```bash
SerialPortPoolService.exe --all-bibs --config-dir "Configuration/examples/"
```

### **Benefits Demonstrated:**

✅ **Complete Isolation** - Each demo line has independent configuration
✅ **Error Containment** - Syntax error in demo1 doesn't affect demo2
✅ **Safe Addition** - Can add demo3 without risk to existing demos
✅ **Hot Reload** - Modify demo1 config while demo2 continues running

### **File Naming Convention:**
- `bib_` prefix is required for auto-discovery
- BIB ID in filename should match `id` attribute in XML
- `.xml` extension required

### **Production Deployment:**
1. Copy example files to main `Configuration/` directory
2. Rename BIB IDs to match your production lines
3. Update commands and responses for your hardware
4. Test in development environment first