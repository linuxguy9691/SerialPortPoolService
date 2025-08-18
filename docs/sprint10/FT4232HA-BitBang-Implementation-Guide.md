# FT4232HA Bit-Bang Implementation Guide - Sprint 10

![Hardware](https://img.shields.io/badge/Hardware-FT4232HA%20Mini%20Module-blue.svg)
![Port](https://img.shields.io/badge/Dedicated%20Port-Port%204%20(D)-orange.svg)
![API](https://img.shields.io/badge/API-FTD2XX_NET-green.svg)
![Sprint](https://img.shields.io/badge/Sprint%2010-Real%20GPIO-success.svg)

## 🎯 **Overview - FT4232HA Bit-Bang Capabilities**

Based on **FT4232HA Mini Module Datasheet v1.0** (Document Reference: FT_001519)

### **✅ Native Bit-Bang Support Confirmed**
> *"Two of these have an option to independently configure an MPSSE engine. This allows the FT4232HA to operate as two UART/Bit-Bang ports plus two MPSSE engines used to emulate JTAG, SPI, I2C, **Bit-bang** or other synchronous serial modes."*

**Perfect for Sprint 10:** MPSSE native bit-bang support via FTD2XX_NET API ✅

---

## 🔌 **Port Configuration - Client Approved**

### **Port Allocation Strategy**
```
FT4232HA Mini Module (4-Port Configuration):
├── Port A (AD0-AD7) → RS232 Communication (COM11) ✅
├── Port B (BD0-BD7) → RS232 Communication (COM12) ✅  
├── Port C (CD0-CD7) → RS232 Communication (COM13) ✅
└── Port D (DD0-DD7) → DEDICATED GPIO Bit-Bang 🔌
```

**Client Decision:** Port 4 dedicated to GPIO (willing to "sacrifice" for hardware control) ✅

---

## 📋 **Port D GPIO Pin Mapping (CN3 Connector)**

### **Physical Pin Layout for Bit-Bang Implementation**

| Pin Function | FT4232HA Pin | Connector | Physical Pin | Client Requirement |
|--------------|--------------|-----------|--------------|-------------------|
| **GPIO Bit 0** | DD0 | CN3-17 | Pin 17 | Power On Ready Input |
| **GPIO Bit 1** | DD1 | CN3-16 | Pin 16 | Power Down Heads-Up Input |
| **GPIO Bit 2** | DD2 | CN3-15 | Pin 15 | Critical Fail Signal Output |
| **GPIO Bit 3** | DD3 | CN3-14 | Pin 14 | Workflow Active Output |
| **GPIO Bit 4** | DD4 | CN3-13 | Pin 13 | *Reserved for future* |
| **GPIO Bit 5** | DD5 | CN3-11 | Pin 11 | *Reserved for future* |
| **GPIO Bit 6** | DD6 | CN3-10 | Pin 10 | *Reserved for future* |
| **GPIO Bit 7** | DD7 | CN3-9 | Pin 9 | *Reserved for future* |

### **Power & Ground Pins (Required)**
| Function | Connector | Pin | Description |
|----------|-----------|-----|-------------|
| **VIO** | CN3-12, CN3-22 | Pin 12, 22 | 3.3V GPIO Operating Voltage |
| **GND** | CN3-2, CN3-4 | Pin 2, 4 | Ground Reference |

---

## ⚡ **Electrical Specifications**

### **Power Requirements (Page 5-6)**
- **VIO Voltage:** 3.3V (connected to all VCCIO pins)
- **GPIO Logic Levels:** TTL compatible at 3.3V
- **Operating Temperature:** -40°C to +85°C (automotive grade)
- **USB Power:** Bus-powered (500mA max) or Self-powered

### **Power Configuration for GPIO**
```csharp
// Power setup requirement for reliable GPIO operation
// VIO (CN3 pins 12 & 22) must be connected to 3.3V
// Short R9 to connect VCC3V3 to VIO (as per datasheet)
```

---

## 🔧 **FTD2XX_NET Implementation Strategy**

### **MPSSE Configuration for Port D Bit-Bang**

```csharp
// Sprint 10 Implementation approach
public class FtdiBitBangProtocolProvider : IBitBangProtocolProvider
{
    private FTDI _ftdiDevice = new FTDI();
    private const string DEVICE_SERIAL = "FT9A9OFO"; // Client hardware serial
    
    public async Task InitializeAsync(BitBangConfiguration config)
    {
        // Open Port D (4th port) by serial number + port index
        var status = _ftdiDevice.OpenByDescription("FT4232HA D");
        if (status != FTDI.FT_STATUS.FT_OK)
            throw new FtdiException($"Failed to open Port D: {status}");
        
        // Configure Port D for MPSSE bit-bang mode
        await ConfigureMpsseBitBangAsync();
        
        // Set GPIO direction: DD0,DD1 = inputs, DD2,DD3 = outputs
        await SetGpioDirectionAsync(0b00001100); // Bits 2,3 as outputs
    }
    
    // Client requirement: Power On Ready input monitoring
    public async Task<bool> ReadPowerOnReadyAsync()
    {
        var gpioState = await ReadGpioPortAsync();
        return (gpioState & 0x01) != 0; // Read DD0 (bit 0)
    }
    
    // Client requirement: Power Down Heads-Up input monitoring  
    public async Task<bool> ReadPowerDownHeadsUpAsync()
    {
        var gpioState = await ReadGpioPortAsync();
        return (gpioState & 0x02) != 0; // Read DD1 (bit 1)
    }
    
    // Client requirement: Critical Fail Signal output
    public async Task SetCriticalFailSignalAsync(bool state)
    {
        var currentState = await ReadGpioPortAsync();
        var newState = state ? 
            (currentState | 0x04) :    // Set DD2 (bit 2)
            (currentState & ~0x04);    // Clear DD2 (bit 2)
        await WriteGpioPortAsync(newState);
    }
    
    // Bonus: Workflow Active Signal output
    public async Task SetWorkflowActiveSignalAsync(bool state)
    {
        var currentState = await ReadGpioPortAsync();
        var newState = state ? 
            (currentState | 0x08) :    // Set DD3 (bit 3)
            (currentState & ~0x08);    // Clear DD3 (bit 3)
        await WriteGpioPortAsync(newState);
    }
}
```

---

## 🔬 **EEPROM Configuration (Sprint 8 Bonus)**

### **Default EEPROM Settings (Page 10)**
```
USB Vendor ID (VID): 0403h (FTDI default)
USB Product ID (PID): 6048h (FT4232HA default)
Product Description: "FT4232HA" (default)
```

### **Sprint 8 Dynamic BIB Selection Integration**
```csharp
// EEPROM ProductDescription can be customized via FT_PROG
// For Sprint 8: ProductDescription → BIB_ID mapping
// Example: "client_demo" → Automatic BIB selection
```

---

## 🎯 **Client Requirements Mapping**

### **Sprint 9 Multi-Level + Hardware Hooks Implementation**

| Client Requirement | Hardware Implementation | GPIO Pin | Direction |
|-------------------|------------------------|----------|-----------|
| **Power On Ready** | Monitor hardware ready signal | DD0 (CN3-17) | INPUT |
| **Power Down Heads-Up** | Monitor shutdown request | DD1 (CN3-16) | INPUT |
| **Critical Fail Signal** | Hardware emergency notification | DD2 (CN3-15) | OUTPUT |
| **Workflow Active** | Indicate active workflow status | DD3 (CN3-14) | OUTPUT |

### **Client Workflow Integration**
```csharp
// Sprint 9 + Sprint 10 integration example
public async Task<EnhancedWorkflowResult> ExecuteWorkflowWithHardwareAsync(...)
{
    // Wait for Power On Ready before starting
    while (!await _bitBangProvider.ReadPowerOnReadyAsync())
    {
        await Task.Delay(100);
    }
    
    // Signal workflow is active
    await _bitBangProvider.SetWorkflowActiveSignalAsync(true);
    
    try
    {
        // Execute workflow phases...
        var result = await ExecuteWorkflowPhasesAsync();
        
        // Trigger hardware signal on critical failure
        if (result.Level == ValidationLevel.CRITICAL)
        {
            await _bitBangProvider.SetCriticalFailSignalAsync(true);
        }
        
        return result;
    }
    finally
    {
        // Clear workflow active signal
        await _bitBangProvider.SetWorkflowActiveSignalAsync(false);
    }
}
```

---

## ⚠️ **Implementation Considerations**

### **Hardware Limitations & Guidelines**

1. **Port D Dedication:** 
   - ✅ Port D completely dedicated to GPIO (no RS232 capability when in bit-bang mode)
   - ✅ Ports A, B, C remain fully functional for RS232 communication

2. **MPSSE Mode Switching:**
   - ⚠️ Cannot simultaneously use Port D for UART and bit-bang
   - ✅ Mode is persistent until explicitly changed

3. **GPIO Performance:**
   - ✅ USB 2.0 Hi-Speed (480Mb/s) provides fast GPIO response
   - ✅ Bit-bang rates up to 30Mbps for fast status monitoring

4. **Power Requirements:**
   - ✅ Bus-powered (500mA) sufficient for GPIO operations
   - ✅ VIO must be properly connected to 3.3V for reliable GPIO

### **Error Handling Strategy**
```csharp
// Robust GPIO operations with error recovery
public async Task<bool> SafeReadGpioAsync(int bitIndex, int maxRetries = 3)
{
    for (int retry = 0; retry < maxRetries; retry++)
    {
        try
        {
            var state = await ReadGpioPortAsync();
            return (state & (1 << bitIndex)) != 0;
        }
        catch (FtdiException ex) when (retry < maxRetries - 1)
        {
            _logger.LogWarning($"GPIO read retry {retry + 1}/{maxRetries}: {ex.Message}");
            await Task.Delay(50); // Brief delay before retry
        }
    }
    throw new HardwareException("GPIO read failed after all retries");
}
```

---

## 🚀 **Sprint 10 Implementation Checklist**

### **Phase 1: Basic GPIO Setup (2h)**
- [ ] FTD2XX_NET NuGet package integration
- [ ] Port D identification and opening logic
- [ ] MPSSE bit-bang mode configuration
- [ ] Basic GPIO read/write operations

### **Phase 2: Client Requirements (1h)**
- [ ] Power On Ready input monitoring
- [ ] Power Down Heads-Up input monitoring  
- [ ] Critical Fail Signal output control
- [ ] Workflow Active Signal output control

### **Phase 3: Integration & Testing (1h)**
- [ ] Integration with Sprint 9 workflow orchestrator
- [ ] Hardware event system integration
- [ ] Error handling and recovery logic
- [ ] Performance optimization and caching

### **Expected Sprint 10 Deliverable**
```csharp
// Complete working implementation
var bitBangProvider = new FtdiBitBangProtocolProvider();
await bitBangProvider.InitializeAsync(config);

// Real hardware GPIO control
var powerReady = await bitBangProvider.ReadPowerOnReadyAsync();        // ✅ Real hardware read
await bitBangProvider.SetCriticalFailSignalAsync(true);               // ✅ Real hardware write
```

---

## 📚 **References**

- **Hardware:** FT4232HA Mini Module Datasheet v1.0 (FT_001519)
- **API:** FTD2XX_NET Library Documentation
- **Client Requirements:** Sprint 9 Multi-Level Validation + Hardware Hooks
- **Integration:** Sprint 8 EEPROM + Sprint 9 Orchestrator

---

*FT4232HA Bit-Bang Implementation Guide*  
*Created: August 18, 2025*  
*Target: Sprint 10 Real GPIO Implementation*  
*Hardware: FT4232HA Mini Module with Port D dedicated GPIO*

**🚀 Ready for Real Hardware GPIO Implementation! 🚀**