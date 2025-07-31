using SerialPortPool.Core.Models.Configuration;

namespace SerialPortPool.Core.Interfaces;

/// <summary>
/// Generic protocol handler interface for multi-protocol communication
/// Supports RS232, RS485, USB, CAN, I2C, SPI protocols
/// </summary>
public interface IProtocolHandler : IDisposable
{
    /// <summary>
    /// Protocol name supported by this handler
    /// </summary>
    string SupportedProtocol { get; }
    
    /// <summary>
    /// Whether the communication session is currently active
    /// </summary>
    bool IsSessionActive { get; }
    
    /// <summary>
    /// Current session information
    /// </summary>
    CommunicationSession? CurrentSession { get; }
    
    /// <summary>
    /// Open communication session with specified port and configuration
    /// </summary>
    /// <param name="portName">Physical port name (e.g., COM8)</param>
    /// <param name="portConfig">Port configuration with protocol settings</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if session opened successfully</returns>
    Task<bool> OpenSessionAsync(string portName, PortConfiguration portConfig, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Close active communication session
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    Task CloseSessionAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Send command and wait for response
    /// </summary>
    /// <param name="request">Command request with timeout and retry settings</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Protocol response with timing and success information</returns>
    Task<ProtocolResponse> SendCommandAsync(ProtocolRequest request, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Send command sequence (multiple commands)
    /// </summary>
    /// <param name="requests">Sequence of command requests</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Sequence of protocol responses</returns>
    Task<IEnumerable<ProtocolResponse>> SendCommandSequenceAsync(IEnumerable<ProtocolRequest> requests, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Test connection health
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if connection is healthy</returns>
    Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get protocol-specific capabilities and settings
    /// </summary>
    ProtocolCapabilities GetCapabilities();
}

/// <summary>
/// Factory for creating protocol handlers
/// </summary>
public interface IProtocolHandlerFactory
{
    /// <summary>
    /// Create protocol handler for specified protocol
    /// </summary>
    /// <param name="protocolName">Protocol name (rs232, rs485, usb, can, i2c, spi)</param>
    /// <returns>Protocol handler instance</returns>
    IProtocolHandler CreateHandler(string protocolName);
    
    /// <summary>
    /// Get all supported protocols
    /// </summary>
    /// <returns>List of supported protocol names</returns>
    IEnumerable<string> GetSupportedProtocols();
    
    /// <summary>
    /// Check if protocol is supported
    /// </summary>
    /// <param name="protocolName">Protocol name to check</param>
    /// <returns>True if protocol is supported</returns>
    bool IsProtocolSupported(string protocolName);
    
    /// <summary>
    /// Get capabilities for specific protocol
    /// </summary>
    /// <param name="protocolName">Protocol name</param>
    /// <returns>Protocol capabilities</returns>
    ProtocolCapabilities GetProtocolCapabilities(string protocolName);
}

/// <summary>
/// Protocol command request
/// </summary>
public class ProtocolRequest
{
    public string Command { get; set; } = string.Empty;
    public string ExpectedResponse { get; set; } = string.Empty;
    public bool IsRegexPattern { get; set; } = false;
    public int TimeoutMs { get; set; } = 5000;
    public int RetryCount { get; set; } = 1;
    public bool IsCritical { get; set; } = true;
    public Dictionary<string, object> Parameters { get; set; } = new();
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// Create simple command request
    /// </summary>
    public static ProtocolRequest Create(string command, string expectedResponse, int timeoutMs = 5000)
    {
        return new ProtocolRequest
        {
            Command = command,
            ExpectedResponse = expectedResponse,
            TimeoutMs = timeoutMs
        };
    }
    
    /// <summary>
    /// Create request from command definition
    /// </summary>
    public static ProtocolRequest FromDefinition(CommandDefinition definition)
    {
        return new ProtocolRequest
        {
            Command = definition.Command,
            ExpectedResponse = definition.ExpectedResponse,
            IsRegexPattern = definition.IsRegex,
            TimeoutMs = definition.TimeoutMs,
            RetryCount = definition.RetryCount,
            IsCritical = definition.IsCritical,
            Parameters = new Dictionary<string, object>(definition.Parameters),
            Description = definition.Description
        };
    }
}

/// <summary>
/// Protocol command response
/// </summary>
public class ProtocolResponse
{
    public bool Success { get; set; }
    public string ActualResponse { get; set; } = string.Empty;
    public string ExpectedResponse { get; set; } = string.Empty;
    public TimeSpan ResponseTime { get; set; }
    public int AttemptCount { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public Dictionary<string, object> Metadata { get; set; } = new();
    
    /// <summary>
    /// Create successful response
    /// </summary>
    public static ProtocolResponse Success(string actualResponse, string expectedResponse, TimeSpan responseTime, int attemptCount = 1)
    {
        return new ProtocolResponse
        {
            Success = true,
            ActualResponse = actualResponse,
            ExpectedResponse = expectedResponse,
            ResponseTime = responseTime,
            AttemptCount = attemptCount
        };
    }
    
    /// <summary>
    /// Create failed response
    /// </summary>
    public static ProtocolResponse Failure(string errorMessage, string expectedResponse, TimeSpan responseTime, int attemptCount = 1)
    {
        return new ProtocolResponse
        {
            Success = false,
            ActualResponse = string.Empty,
            ExpectedResponse = expectedResponse,
            ResponseTime = responseTime,
            AttemptCount = attemptCount,
            ErrorMessage = errorMessage
        };
    }
}

/// <summary>
/// Communication session information
/// </summary>
public class CommunicationSession
{
    public string SessionId { get; set; } = Guid.NewGuid().ToString("N")[..8];
    public string PortName { get; set; } = string.Empty;
    public string Protocol { get; set; } = string.Empty;
    public PortConfiguration Configuration { get; set; } = new();
    public DateTime StartTime { get; set; } = DateTime.UtcNow;
    public DateTime? EndTime { get; set; }
    public TimeSpan Duration => EndTime?.Subtract(StartTime) ?? DateTime.UtcNow.Subtract(StartTime);
    public int CommandsSent { get; set; }
    public int CommandsSuccessful { get; set; }
    public bool IsActive => EndTime == null;
    
    /// <summary>
    /// Calculate success rate
    /// </summary>
    public double SuccessRate => CommandsSent > 0 ? (double)CommandsSuccessful / CommandsSent * 100 : 0;
}

/// <summary>
/// Protocol capabilities and features
/// </summary>
public class ProtocolCapabilities
{
    public string Protocol { get; set; } = string.Empty;
    public bool SupportsHalfDuplex { get; set; }
    public bool SupportsFullDuplex { get; set; }
    public bool RequiresAddressing { get; set; }
    public bool SupportsMulticast { get; set; }
    public bool SupportsBroadcast { get; set; }
    public int[] SupportedSpeeds { get; set; } = Array.Empty<int>();
    public string[] SupportedDataPatterns { get; set; } = Array.Empty<string>();
    public int MaxPayloadSize { get; set; } = int.MaxValue;
    public int MinResponseTime { get; set; } = 1; // milliseconds
    public int MaxResponseTime { get; set; } = 30000; // milliseconds
    public Dictionary<string, object> SpecificFeatures { get; set; } = new();
}