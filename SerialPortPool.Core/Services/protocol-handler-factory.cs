using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SerialPortPool.Core.Interfaces;
using SerialPortPool.Core.Protocols;

namespace SerialPortPool.Core.Services;

/// <summary>
/// Protocol Handler Factory - Creates appropriate protocol handlers
/// Supports RS232, RS485, USB, CAN, I2C, SPI protocols with extensible architecture
/// </summary>
public class ProtocolHandlerFactory : IProtocolHandlerFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ProtocolHandlerFactory> _logger;
    
    private readonly Dictionary<string, Type> _protocolHandlers = new(StringComparer.OrdinalIgnoreCase)
    {
        ["rs232"] = typeof(RS232ProtocolHandler),
        // Future Sprint 6 protocols:
        // ["rs485"] = typeof(RS485ProtocolHandler),
        // ["usb"] = typeof(USBProtocolHandler),
        // ["can"] = typeof(CANProtocolHandler),
        // ["i2c"] = typeof(I2CProtocolHandler),
        // ["spi"] = typeof(SPIProtocolHandler)
    };

    public ProtocolHandlerFactory(IServiceProvider serviceProvider, ILogger<ProtocolHandlerFactory> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <summary>
    /// Create protocol handler for specified protocol
    /// </summary>
    public IProtocolHandler CreateHandler(string protocolName)
    {
        if (string.IsNullOrWhiteSpace(protocolName))
        {
            throw new ArgumentException("Protocol name cannot be empty", nameof(protocolName));
        }

        var normalizedProtocol = protocolName.Trim().ToLowerInvariant();

        if (!_protocolHandlers.TryGetValue(normalizedProtocol, out var handlerType))
        {
            var supportedProtocols = string.Join(", ", GetSupportedProtocols());
            throw new NotSupportedException($"Protocol '{protocolName}' is not supported. Supported protocols: {supportedProtocols}");
        }

        try
        {
            _logger.LogDebug("Creating protocol handler for: {Protocol}", protocolName);

            // Create handler instance using DI container
            var handler = (IProtocolHandler)_serviceProvider.GetRequiredService(handlerType);
            
            _logger.LogInformation("Protocol handler created successfully: {Protocol} → {HandlerType}", 
                protocolName, handlerType.Name);

            return handler;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create protocol handler for: {Protocol}", protocolName);
            throw new InvalidOperationException($"Failed to create protocol handler for '{protocolName}': {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Get all supported protocols
    /// </summary>
    public IEnumerable<string> GetSupportedProtocols()
    {
        return _protocolHandlers.Keys.OrderBy(k => k);
    }

    /// <summary>
    /// Check if protocol is supported
    /// </summary>
    public bool IsProtocolSupported(string protocolName)
    {
        if (string.IsNullOrWhiteSpace(protocolName))
            return false;

        return _protocolHandlers.ContainsKey(protocolName.Trim().ToLowerInvariant());
    }

    /// <summary>
    /// Get capabilities for specific protocol
    /// </summary>
    public ProtocolCapabilities GetProtocolCapabilities(string protocolName)
    {
        if (!IsProtocolSupported(protocolName))
        {
            throw new NotSupportedException($"Protocol '{protocolName}' is not supported");
        }

        // Create temporary handler to get capabilities
        using var handler = CreateHandler(protocolName);
        return handler.GetCapabilities();
    }

    /// <summary>
    /// Register new protocol handler type (for future extensibility)
    /// </summary>
    public void RegisterProtocolHandler<T>(string protocolName) where T : class, IProtocolHandler
    {
        if (string.IsNullOrWhiteSpace(protocolName))
        {
            throw new ArgumentException("Protocol name cannot be empty", nameof(protocolName));
        }

        var normalizedProtocol = protocolName.Trim().ToLowerInvariant();
        
        _protocolHandlers[normalizedProtocol] = typeof(T);
        
        _logger.LogInformation("Registered new protocol handler: {Protocol} → {HandlerType}", 
            protocolName, typeof(T).Name);
    }

    /// <summary>
    /// Get detailed protocol information
    /// </summary>
    public ProtocolInfo GetProtocolInfo(string protocolName)
    {
        if (!IsProtocolSupported(protocolName))
        {
            throw new NotSupportedException($"Protocol '{protocolName}' is not supported");
        }

        var capabilities = GetProtocolCapabilities(protocolName);
        var handlerType = _protocolHandlers[protocolName.ToLowerInvariant()];

        return new ProtocolInfo
        {
            Name = protocolName.ToUpperInvariant(),
            HandlerType = handlerType.Name,
            Capabilities = capabilities,
            IsImplemented = true,
            Description = GetProtocolDescription(protocolName)
        };
    }

    /// <summary>
    /// Get all protocol information
    /// </summary>
    public IEnumerable<ProtocolInfo> GetAllProtocolInfo()
    {
        return GetSupportedProtocols().Select(GetProtocolInfo);
    }

    /// <summary>
    /// Get protocol description
    /// </summary>
    private string GetProtocolDescription(string protocolName)
    {
        return protocolName.ToLowerInvariant() switch
        {
            "rs232" => "RS232 serial communication - Standard point-to-point serial interface",
            "rs485" => "RS485 serial communication - Multi-point serial interface with differential signaling",
            "usb" => "USB communication - Universal Serial Bus virtual serial interface",
            "can" => "CAN Bus communication - Controller Area Network for automotive and industrial",
            "i2c" => "I2C communication - Inter-Integrated Circuit bus for short-distance device communication",
            "spi" => "SPI communication - Serial Peripheral Interface for high-speed device communication",
            _ => $"Protocol: {protocolName.ToUpperInvariant()}"
        };
    }
}

/// <summary>
/// Protocol information details
/// </summary>
public class ProtocolInfo
{
    public string Name { get; set; } = string.Empty;
    public string HandlerType { get; set; } = string.Empty;
    public ProtocolCapabilities Capabilities { get; set; } = new();
    public bool IsImplemented { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}