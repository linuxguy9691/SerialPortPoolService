# FTDI API Structure and Compilation Fixes

The compilation errors in your C# code stem from using outdated property names and methods that have been removed or changed in current versions of the FTD2XX_NET library. **The key issue is that VendorId and ProductId are not direct properties**, and device information requires method calls rather than property access.

## Current API structure and breaking changes

The **FTD2XX_NET library has undergone significant changes** between versions, removing direct property access for device identifiers. The current approach requires using `GetDeviceID()` method and extracting vendor/product IDs through bit manipulation, while manufacturer information comes from EEPROM structure reading rather than a direct method call.

**Current stable versions** include FTD2XX.Net v1.2.1 (newer package supporting .NET Standard 2.0+) and FTD2XX_NET v1.0.14 (legacy .NET Framework 3.5+). The newer package provides better cross-platform compatibility but maintains the same core API structure that differs from older versions you may be referencing.

The transition from property-based to method-based access reflects FTDI's focus on more explicit error handling and better control over device communication timing.

## Correct device information methods

Instead of direct properties, the FTDI class provides specific methods for retrieving device information:

```csharp
// Correct method signatures for device information
uint deviceID;
ftdi.GetDeviceID(out deviceID);      // Gets combined VID/PID as uint32

string description;
ftdi.GetDescription(out description);

string serialNumber;
ftdi.GetSerialNumber(out serialNumber);

// Extract VID/PID from combined device ID
ushort vendorId = (ushort)(deviceID >> 16);    // Upper 16 bits = VID
ushort productId = (ushort)(deviceID & 0xFFFF); // Lower 16 bits = PID
```

The **GetManufacturer() method does not exist** in current versions. Instead, manufacturer information must be retrieved from EEPROM structures using device-specific read methods.

## EEPROM structure properties

The EEPROM structures contain the properties you're looking for, but they must be populated using specific read methods:

**FT232R_EEPROM_STRUCTURE** contains VendorId, ProductId, Manufacturer, and Description as direct properties:
```csharp
var eepromData = new FTDI.FT232R_EEPROM_STRUCTURE();
ftdi.ReadFT232REEPROM(eepromData);
string manufacturer = eepromData.Manufacturer;
string productDescription = eepromData.Description;
ushort vendorId = eepromData.VendorId;
ushort productId = eepromData.ProductId;
```

**FT232H_EEPROM_STRUCTURE and FT4232H_EEPROM_STRUCTURE** follow identical patterns with the same core properties (VendorId, ProductId, Manufacturer, Description) plus additional device-specific configuration options like drive current settings and CBUS pin configurations.

All EEPROM structures inherit from `FT_EEPROM_DATA` base class and include standard properties for **MaxPower, SelfPowered, RemoteWakeup, and SerNumEnable**.

## Working device enumeration example

The most reliable approach for gathering device information combines device list enumeration with individual device queries:

```csharp
using FTD2XX_NET;

FTDI ftdi = new FTDI();
uint deviceCount = 0;

// Get number of connected FTDI devices
ftdi.GetNumberOfDevices(ref deviceCount);

if (deviceCount > 0)
{
    // Retrieve device information for all connected devices
    var deviceList = new FTDI.FT_DEVICE_INFO_NODE[deviceCount];
    ftdi.GetDeviceList(deviceList);
    
    for (int i = 0; i < deviceCount; i++)
    {
        var device = deviceList[i];
        uint combinedID = device.ID;
        
        // Extract VID/PID using bit operations
        ushort vendorId = (ushort)(combinedID >> 16);
        ushort productId = (ushort)(combinedID & 0xFFFF);
        
        Console.WriteLine($"Device {i}:");
        Console.WriteLine($"  VID: 0x{vendorId:X4}");
        Console.WriteLine($"  PID: 0x{productId:X4}");
        Console.WriteLine($"  Serial: {device.SerialNumber}");
        Console.WriteLine($"  Description: {device.Description}");
    }
}
```

This approach provides immediate access to basic device information without requiring individual device opening.

## Best practices for current API

**Always use method calls rather than property access** for device information. The current API emphasizes explicit method calls with output parameters to ensure proper error handling through FT_STATUS return values.

**Check device type before reading EEPROM** structures. Different FTDI chips require specific EEPROM read methods (ReadFT232REEPROM, ReadFT232HEEPROM, ReadFT4232HEEPROM) that correspond to their structure types.

**Handle device enumeration efficiently** by using `GetDeviceList()` for basic information rather than opening each device individually. This reduces overhead and prevents conflicts with other applications.

For **manufacturer information specifically**, you must read the appropriate EEPROM structure based on device type rather than calling a non-existent GetManufacturer method.

## Conclusion

The compilation errors indicate usage of removed API elements from older FTD2XX_NET versions. **Replace direct property access with method calls**, extract VendorId/ProductId from the combined Device ID using bit manipulation, and retrieve manufacturer information from EEPROM structures rather than direct method calls. These changes align with the current API's emphasis on explicit error handling and device-type-specific information access.