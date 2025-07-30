// SerialPortPool.Core/Interfaces/IProtocolHandler.cs - CLEANED Sprint 5
using SerialPortPool.Core.Models;

namespace SerialPortPool.Core.Interfaces;

/// <summary>
/// Protocol handler interface for multi-protocol communication system
/// Sprint 5: RS232 implementation
/// Sprint 6: RS485, USB, CAN, I2C, SPI expansion
/// </summary>
public interface IProtocolHandler : IDisposable
{
    /// <summary>
    /// Protocol identifier (e.g., "rs232", "rs485", "usb", "can", "i2c", "spi")
    /// </summary>
    string ProtocolName { get; }

    /// <summary>
    /// Protocol version for compatibility tracking
    /// </summary>
    string ProtocolVersion { get; }

    /// <summary>
    /// Check if this handler can process the specified protocol
    /// </summary>
    /// <param name="protocol">Protocol name to check</param>
    /// <returns>True if handler supports the protocol</returns>
    Task<bool> CanHandleProtocolAsync(string protocol);

    /// <summary>
    /// Open a communication session with the specified configuration
    /// </summary>
    /// <param name="config">Protocol-specific configuration</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Active protocol session</returns>
    Task<ProtocolSession> OpenSessionAsync(
        ProtocolConfiguration config, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Execute a single command in the protocol session
    /// </summary>
    /// <param name="session">Active protocol session</param>
    /// <param name="command">Command to execute</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Command execution result</returns>
    Task<CommandResult> ExecuteCommandAsync(
        ProtocolSession session, 
        ProtocolCommand command,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Execute a sequence of commands (3-phase workflow support)
    /// </summary>
    /// <param name="session">Active protocol session</param>
    /// <param name="commands">Command sequence to execute</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Sequence execution results</returns>
    Task<CommandSequenceResult> ExecuteCommandSequenceAsync(
        ProtocolSession session,
        IEnumerable<ProtocolCommand> commands,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Close and cleanup the protocol session
    /// </summary>
    /// <param name="session">Session to close</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if closed successfully</returns>
    Task<bool> CloseSessionAsync(
        ProtocolSession session, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Test protocol connectivity without opening full session
    /// </summary>
    /// <param name="config">Protocol configuration to test</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Connectivity test result</returns>
    Task<ProtocolTestResult> TestConnectivityAsync(
        ProtocolConfiguration config,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get protocol-specific capabilities and limitations
    /// </summary>
    /// <returns>Protocol capabilities information</returns>
    ProtocolCapabilities GetCapabilities();

    /// <summary>
    /// Get current protocol statistics (session count, error rates, etc.)
    /// </summary>
    /// <returns>Protocol handler statistics</returns>
    ProtocolHandlerStatistics GetStatistics();
}