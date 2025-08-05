// ===================================================================
// File: SerialPortPool.Core/Interfaces/IProtocolHandlerExtensions.cs
// ===================================================================

using SerialPortPool.Core.Models;

namespace SerialPortPool.Core.Interfaces;

/// <summary>
/// Extension methods for simplified protocol handler usage in Sprint 6
/// </summary>
public static class ProtocolHandlerExtensions
{
    /// <summary>
    /// 🔥 Sprint 6: Simplified OpenSessionAsync for line 3
    /// var session = await protocolHandler.OpenSessionAsync(portName);
    /// </summary>
    public static async Task<ProtocolSession> OpenSessionAsync(
        this IProtocolHandler handler,
        string portName,
        CancellationToken cancellationToken = default)
    {
        // Create default RS232 configuration
        var defaultConfig = new PortConfiguration
        {
            PortNumber = 1,
            Protocol = "rs232",
            Speed = 115200,
            DataPattern = "n81"
        };
        
        defaultConfig.Settings["read_timeout"] = 3000;
        defaultConfig.Settings["write_timeout"] = 3000;

        return await handler.OpenSessionAsync(portName, defaultConfig, cancellationToken);
    }

    /// <summary>
    /// 🔥 Sprint 6: Simplified ExecuteCommandAsync for line 4
    /// var result = await protocolHandler.ExecuteCommandAsync(command);
    /// </summary>
    public static async Task<ProtocolResponse> ExecuteCommandAsync(
        this IProtocolHandler handler,
        string command,
        string? expectedResponse = null,
        int timeoutMs = 3000,
        CancellationToken cancellationToken = default)
    {
        var request = new ProtocolRequest
        {
            Command = command,
            Data = System.Text.Encoding.UTF8.GetBytes(command),
            Timeout = TimeSpan.FromMilliseconds(timeoutMs)
        };

        return await handler.SendCommandAsync(request, cancellationToken);
    }
}