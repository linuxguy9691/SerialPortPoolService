// SerialPortPool.Core/Interfaces/IProtocolHandlerFactory.cs - NEW Week 2
namespace SerialPortPool.Core.Interfaces;

/// <summary>
/// Factory interface for creating protocol handlers
/// Week 2: RS232 focus, extensible for Sprint 6 (5 more protocols)
/// </summary>
public interface IProtocolHandlerFactory
{
    /// <summary>
    /// Get protocol handler for specified protocol
    /// </summary>
    /// <param name="protocol">Protocol name (e.g., "rs232", "rs485", "usb", "can", "i2c", "spi")</param>
    /// <returns>Protocol handler instance</returns>
    /// <exception cref="ArgumentException">Unknown protocol</exception>
    /// <exception cref="NotSupportedException">Protocol not yet implemented</exception>
    IProtocolHandler GetHandler(string protocol);

    /// <summary>
    /// Check if protocol is supported
    /// </summary>
    /// <param name="protocol">Protocol name to check</param>
    /// <returns>True if protocol is supported</returns>
    bool IsProtocolSupported(string protocol);

    /// <summary>
    /// Get list of all supported protocols
    /// </summary>
    /// <returns>List of supported protocol names</returns>
    IEnumerable<string> GetSupportedProtocols();
}