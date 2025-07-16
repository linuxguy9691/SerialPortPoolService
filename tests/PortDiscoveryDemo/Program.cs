using Microsoft.Extensions.Logging;
using SerialPortPool.Core.Services;

namespace PortDiscoveryDemo;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("🔍 Serial Port Discovery Demo - ÉTAPE 2");
        Console.WriteLine("=========================================");
        
        // Create a simple console logger
        using var loggerFactory = LoggerFactory.Create(builder =>
            builder.AddConsole().SetMinimumLevel(LogLevel.Information));
        
        var logger = loggerFactory.CreateLogger<SerialPortDiscoveryService>();
        
        // Create discovery service
        var discoveryService = new SerialPortDiscoveryService(logger);
        
        try
        {
            Console.WriteLine("📡 Scanning for serial ports...");
            Console.WriteLine();
            
            // Discover all ports
            var ports = await discoveryService.DiscoverPortsAsync();
            var portList = ports.ToList();
            
            if (portList.Count == 0)
            {
                Console.WriteLine("❌ No serial ports found on this system.");
                Console.WriteLine("💡 This is normal if you don't have USB serial adapters connected.");
            }
            else
            {
                Console.WriteLine($"✅ Found {portList.Count} serial port(s):");
                Console.WriteLine();
                
                foreach (var port in portList)
                {
                    Console.WriteLine($"📍 Port: {port.PortName}");
                    Console.WriteLine($"   📝 Name: {port.FriendlyName}");
                    Console.WriteLine($"   🚦 Status: {port.Status}");
                    Console.WriteLine($"   🔧 Device ID: {(string.IsNullOrEmpty(port.DeviceId) ? "N/A" : port.DeviceId)}");
                    Console.WriteLine($"   ⏰ Last Seen: {port.LastSeen:HH:mm:ss}");
                    Console.WriteLine($"   ✅ Valid for Pool: {port.IsValidForPool}");
                    Console.WriteLine();
                }
            }
            
            // Test getting detailed info for a specific port
            if (portList.Count > 0)
            {
                var firstPort = portList.First();
                Console.WriteLine($"🔎 Getting detailed info for {firstPort.PortName}...");
                
                var detailedInfo = await discoveryService.GetPortInfoAsync(firstPort.PortName);
                if (detailedInfo != null)
                {
                    Console.WriteLine($"✅ Detailed scan completed for {detailedInfo.PortName}");
                    Console.WriteLine($"   Full info: {detailedInfo}");
                }
            }
            
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error during discovery: {ex.Message}");
            Console.WriteLine($"📋 Stack trace: {ex.StackTrace}");
        }
        
        Console.WriteLine();
        Console.WriteLine("🎯 Discovery demo completed!");
        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();
    }
}