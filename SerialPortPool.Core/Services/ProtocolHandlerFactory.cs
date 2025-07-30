// SerialPortPool.Core/Services/ProtocolHandlerFactory.cs - Week 2 Final
using Microsoft.Extensions.DependencyInjection;
using SerialPortPool.Core.Interfaces;

namespace SerialPortPool.Core.Services;

/// <summary>
/// Factory for creating protocol handlers
/// Week 2: RS232 implementation, extensible architecture for Sprint 6
/// </summary>
public class ProtocolHandlerFactory : IProtocolHandlerFactory
{
    private readonly IServiceProvider _serviceProvider;

    public ProtocolHandlerFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    /// <summary>
    /// Get protocol handler for specified protocol
    /// Week 2: RS232 only, Sprint 6: 5 additional protocols
    /// </summary>
    public IProtocolHandler GetHandler(string protocol)
    {
        if (string.IsNullOrEmpty(protocol))
            throw new ArgumentException("Protocol cannot be null or empty", nameof(protocol));

        return protocol.ToLowerInvariant() switch
        {
            // Week 2: RS232 Implementation ✅
            "rs232" => _serviceProvider.GetRequiredService<RS232ProtocolHandler>(),
            
            // Sprint 6: Planned Protocol Implementations 🚀
            "rs485" => throw new NotSupportedException("RS485 protocol planned for Sprint 6"),
            "usb" => throw new NotSupportedException("USB protocol planned for Sprint 6"),
            "can" => throw new NotSupportedException("CAN protocol planned for Sprint 6"),
            "i2c" => throw new NotSupportedException("I2C protocol planned for Sprint 6"),
            "spi" => throw new NotSupportedException("SPI protocol planned for Sprint 6"),
            
            // Unknown protocol
            _ => throw new ArgumentException($"Unknown protocol: {protocol}. Supported: rs232 (Week 2), rs485/usb/can/i2c/spi (Sprint 6 planned)")
        };
    }

    /// <summary>
    /// Check if protocol is supported
    /// </summary>
    public bool IsProtocolSupported(string protocol)
    {
        if (string.IsNullOrEmpty(protocol))
            return false;

        return protocol.ToLowerInvariant() switch
        {
            "rs232" => true,  // Week 2: Implemented ✅
            "rs485" => false, // Sprint 6: Planned 🚀
            "usb" => false,   // Sprint 6: Planned 🚀
            "can" => false,   // Sprint 6: Planned 🚀
            "i2c" => false,   // Sprint 6: Planned 🚀
            "spi" => false,   // Sprint 6: Planned 🚀
            _ => false
        };
    }

    /// <summary>
    /// Get list of all supported protocols
    /// </summary>
    public IEnumerable<string> GetSupportedProtocols()
    {
        // Week 2: Only RS232
        yield return "rs232";
        
        // Sprint 6: Will add these protocols
        // yield return "rs485";
        // yield return "usb";
        // yield return "can";
        // yield return "i2c";
        // yield return "spi";
    }
    
    /// <summary>
    /// Get list of planned protocols (for Sprint 6)
    /// </summary>
    public IEnumerable<string> GetPlannedProtocols()
    {
        return new[] { "rs485", "usb", "can", "i2c", "spi" };
    }
}

/// <summary>
/// Extension methods for protocol factory
/// </summary>
public static class ProtocolHandlerFactoryExtensions
{
    /// <summary>
    /// Try to get handler safely without throwing exceptions
    /// </summary>
    public static bool TryGetHandler(this IProtocolHandlerFactory factory, string protocol, out IProtocolHandler? handler)
    {
        try
        {
            handler = factory.GetHandler(protocol);
            return true;
        }
        catch
        {
            handler = null;
            return false;
        }
    }
}