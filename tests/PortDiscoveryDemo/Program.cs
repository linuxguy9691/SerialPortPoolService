using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SerialPortPool.Core.Interfaces;
using SerialPortPool.Core.Models;
using SerialPortPool.Core.Services;

namespace PortDiscoveryDemo;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("🔍 Enhanced Serial Port Discovery Demo - ÉTAPE 6");
        Console.WriteLine("=================================================");
        Console.WriteLine("🎯 Features: FTDI Analysis + Validation + Real-time Hardware Analysis");
        Console.WriteLine();
        
        // Setup dependency injection
        var serviceProvider = SetupServices();
        
        try
        {
            // Get services
            var discovery = serviceProvider.GetRequiredService<ISerialPortDiscovery>();
            var ftdiReader = serviceProvider.GetRequiredService<IFtdiDeviceReader>();
            var validator = serviceProvider.GetRequiredService<ISerialPortValidator>();
            
            Console.WriteLine("📡 Scanning for serial ports with FTDI analysis...");
            Console.WriteLine();
            
            // Discover all ports with full analysis
            var ports = await discovery.DiscoverPortsAsync();
            var portList = ports.ToList();
            
            if (portList.Count == 0)
            {
                Console.WriteLine("❌ No serial ports found on this system.");
                Console.WriteLine("💡 This is normal if you don't have USB serial adapters connected.");
            }
            else
            {
                Console.WriteLine($"✅ Found {portList.Count} serial port(s) with comprehensive analysis:");
                Console.WriteLine();
                
                foreach (var port in portList)
                {
                    await DisplayPortAnalysis(port);
                    Console.WriteLine();
                }
                
                // Display summary
                DisplaySummary(portList);
                
                // Test different validation configurations
                await TestValidationConfigurations(portList, validator);
                
                // Test FTDI device enumeration
                await TestFtdiDeviceEnumeration(ftdiReader);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error during discovery: {ex.Message}");
            Console.WriteLine($"📋 Stack trace: {ex.StackTrace}");
        }
        
        Console.WriteLine();
        Console.WriteLine("🎯 Enhanced discovery demo completed!");
        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();
    }
    
    static ServiceProvider SetupServices()
    {
        var services = new ServiceCollection();
        
        // Logging
        services.AddLogging(builder =>
            builder.AddConsole().SetMinimumLevel(LogLevel.Information));
        
        // Configuration
        services.AddSingleton(PortValidationConfiguration.CreateDevelopmentDefault());
        
        // Services
        services.AddScoped<IFtdiDeviceReader, FtdiDeviceReader>();
        services.AddScoped<ISerialPortValidator, SerialPortValidator>();
        services.AddScoped<ISerialPortDiscovery, EnhancedSerialPortDiscoveryService>();
        
        return services.BuildServiceProvider();
    }
    
    static async Task DisplayPortAnalysis(SerialPortInfo port)
    {
        Console.WriteLine($"📍 Port: {port.PortName}");
        Console.WriteLine($"   📝 Name: {port.FriendlyName}");
        Console.WriteLine($"   🚦 Status: {port.Status}");
        Console.WriteLine($"   🔧 Device ID: {(string.IsNullOrEmpty(port.DeviceId) ? "N/A" : port.DeviceId)}");
        Console.WriteLine($"   ⏰ Last Seen: {port.LastSeen:HH:mm:ss}");
        
        // FTDI Analysis
        if (port.IsFtdiDevice && port.FtdiInfo != null)
        {
            var ftdi = port.FtdiInfo;
            Console.WriteLine($"   🏭 FTDI Device: ✅ YES (Chip: {ftdi.ChipType})");
            Console.WriteLine($"   🔍 VID/PID: {ftdi.VendorId}/{ftdi.ProductId}");
            Console.WriteLine($"   🔑 Serial Number: {ftdi.SerialNumber}");
            Console.WriteLine($"   🏢 Manufacturer: {ftdi.Manufacturer}");
            Console.WriteLine($"   📦 Product: {ftdi.ProductDescription}");
            Console.WriteLine($"   🎯 Is 4232H: {(ftdi.Is4232H ? "✅ YES" : "❌ NO")}");
            Console.WriteLine($"   🔐 Genuine FTDI: {(ftdi.IsGenuineFtdi ? "✅ YES" : "❌ NO")}");
            
            if (ftdi.EepromData.Any())
            {
                Console.WriteLine($"   💾 EEPROM Data: {ftdi.EepromData.Count} entries");
                foreach (var kvp in ftdi.EepromData.Take(3))
                {
                    Console.WriteLine($"      - {kvp.Key}: {kvp.Value}");
                }
            }
        }
        else
        {
            Console.WriteLine($"   🏭 FTDI Device: ❌ NO");
        }
        
        // Validation Results
        if (port.ValidationResult != null)
        {
            var result = port.ValidationResult;
            var validIcon = result.IsValid ? "✅" : "❌";
            Console.WriteLine($"   {validIcon} Valid for Pool: {(result.IsValid ? "YES" : "NO")}");
            Console.WriteLine($"   📋 Validation: {result.Reason}");
            Console.WriteLine($"   📊 Score: {result.ValidationScore}%");
            
            if (result.PassedCriteria.Any())
            {
                Console.WriteLine($"   ✅ Passed Criteria: {string.Join(", ", result.PassedCriteria)}");
            }
            
            if (result.FailedCriteria.Any())
            {
                Console.WriteLine($"   ❌ Failed Criteria: {string.Join(", ", result.FailedCriteria)}");
            }
        }
        else
        {
            Console.WriteLine($"   ⚠️ Not validated");
        }
    }
    
    static void DisplaySummary(List<SerialPortInfo> ports)
    {
        Console.WriteLine("📊 SUMMARY");
        Console.WriteLine("==========");
        
        var totalPorts = ports.Count;
        var ftdiPorts = ports.Count(p => p.IsFtdiDevice);
        var validPorts = ports.Count(p => p.IsValidForPool);
        var availablePorts = ports.Count(p => p.Status == PortStatus.Available);
        var ftdi4232H = ports.Count(p => p.IsFtdi4232H);
        
        Console.WriteLine($"📍 Total Ports: {totalPorts}");
        Console.WriteLine($"🏭 FTDI Devices: {ftdiPorts}");
        Console.WriteLine($"🎯 4232H Chips: {ftdi4232H}");
        Console.WriteLine($"🚦 Available: {availablePorts}");
        Console.WriteLine($"✅ Valid for Pool: {validPorts}");
        Console.WriteLine($"📊 Valid Rate: {(totalPorts > 0 ? (validPorts * 100.0 / totalPorts):0):F1}%");
        
        if (ftdiPorts > 0)
        {
            Console.WriteLine();
            Console.WriteLine("🔍 FTDI Chip Distribution:");
            var chipTypes = ports.Where(p => p.FtdiInfo != null)
                                .GroupBy(p => p.FtdiInfo!.ChipType)
                                .OrderBy(g => g.Key);
            
            foreach (var group in chipTypes)
            {
                var isValid = group.Key == "FT4232H" ? "✅" : "❌";
                Console.WriteLine($"   {isValid} {group.Key}: {group.Count()}");
            }
        }
    }
    
    static async Task TestValidationConfigurations(List<SerialPortInfo> ports, ISerialPortValidator validator)
    {
        if (!ports.Any()) return;
        
        Console.WriteLine();
        Console.WriteLine("🧪 VALIDATION CONFIGURATION TESTING");
        Console.WriteLine("===================================");
        
        // Test with client configuration (strict)
        var clientConfig = PortValidationConfiguration.CreateClientDefault();
        Console.WriteLine($"📋 Client Configuration (Strict - Only FT4232H):");
        
        var clientResults = await validator.ValidatePortsAsync(ports, clientConfig);
        foreach (var kvp in clientResults)
        {
            var port = kvp.Key;
            var result = kvp.Value;
            var icon = result.IsValid ? "✅" : "❌";
            Console.WriteLine($"   {icon} {port.PortName}: {result.Reason} ({result.ValidationScore}%)");
        }
        
        // Test with development configuration (permissive)
        var devConfig = PortValidationConfiguration.CreateDevelopmentDefault();
        Console.WriteLine($"📋 Development Configuration (Permissive - Multiple FTDI chips):");
        
        var devResults = await validator.ValidatePortsAsync(ports, devConfig);
        foreach (var kvp in devResults)
        {
            var port = kvp.Key;
            var result = kvp.Value;
            var icon = result.IsValid ? "✅" : "❌";
            Console.WriteLine($"   {icon} {port.PortName}: {result.Reason} ({result.ValidationScore}%)");
        }
    }
    
    static async Task TestFtdiDeviceEnumeration(IFtdiDeviceReader ftdiReader)
    {
        Console.WriteLine();
        Console.WriteLine("🔍 FTDI DEVICE ENUMERATION TEST");
        Console.WriteLine("===============================");
        
        var ftdiDevices = await ftdiReader.GetAllFtdiDevicesAsync();
        var deviceList = ftdiDevices.ToList();
        
        if (deviceList.Count == 0)
        {
            Console.WriteLine("❌ No FTDI devices found in system enumeration.");
        }
        else
        {
            Console.WriteLine($"✅ Found {deviceList.Count} FTDI device(s) in system:");
            
            foreach (var device in deviceList)
            {
                Console.WriteLine($"   🏭 {device.ChipType} (VID: {device.VendorId}, PID: {device.ProductId})");
                Console.WriteLine($"      🔑 Serial: {device.SerialNumber}");
                Console.WriteLine($"      📦 Product: {device.ProductDescription}");
                Console.WriteLine($"      🎯 Valid for Client: {(device.Is4232H ? "✅ YES" : "❌ NO")}");
                Console.WriteLine();
            }
        }
    }
}