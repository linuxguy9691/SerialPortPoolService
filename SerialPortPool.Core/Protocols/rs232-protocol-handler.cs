using System.Diagnostics;
using System.IO.Ports;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using SerialPortPool.Core.Interfaces;
using SerialPortPool.Core.Models.Configuration;

namespace SerialPortPool.Core.Protocols;

/// <summary>
/// RS232 Protocol Handler - Real serial communication implementation
/// Supports configurable baud rates, data patterns, timeouts, and retries
/// </summary>
public class RS232ProtocolHandler : IProtocolHandler
{
    private readonly ILogger<RS232ProtocolHandler> _logger;
    private SerialPort? _serialPort;
    private CommunicationSession? _currentSession;
    private bool _disposed = false;
    
    public string SupportedProtocol => "rs232";
    public bool IsSessionActive => _currentSession?.IsActive == true && _serialPort?.IsOpen == true;
    public CommunicationSession? CurrentSession => _currentSession;

    public RS232ProtocolHandler(ILogger<RS232ProtocolHandler> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Open RS232 communication session
    /// </summary>
    public async Task<bool> OpenSessionAsync(string portName, PortConfiguration portConfig, CancellationToken cancellationToken = default)
    {
        try
        {
            if (IsSessionActive)
            {
                await CloseSessionAsync(cancellationToken);
            }

            _logger.LogInformation("Opening RS232 session on {Port} with config: {Speed} baud, {DataPattern}", 
                portName, portConfig.Speed, portConfig.DataPattern);

            // Parse data pattern (e.g., "n81" = None parity, 8 data bits, 1 stop bit)
            var (parity, dataBits, stopBits) = ParseDataPattern(portConfig.DataPattern);

            // Create and configure serial port
            _serialPort = new SerialPort(portName)
            {
                BaudRate = portConfig.Speed,
                DataBits = dataBits,
                Parity = parity,
                StopBits = stopBits,
                Handshake = Handshake.None,
                ReadTimeout = 5000,
                WriteTimeout = 5000,
                NewLine = "\r\n",
                Encoding = Encoding.ASCII
            };

            // Apply protocol-specific settings
            ApplyProtocolSettings(portConfig);

            // Open the port
            _serialPort.Open();

            // Create session
            _currentSession = new CommunicationSession
            {
                PortName = portName,
                Protocol = SupportedProtocol,
                Configuration = portConfig,
                StartTime = DateTime.UtcNow
            };

            // Test basic connectivity
            await Task.Delay(100, cancellationToken); // Let port stabilize

            _logger.LogInformation("RS232 session opened successfully: {SessionId} on {Port}", 
                _currentSession.SessionId, portName);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open RS232 session on {Port}", portName);
            
            // Cleanup on failure
            _serialPort?.Dispose();
            _serialPort = null;
            _currentSession = null;
            
            return false;
        }
    }

    /// <summary>
    /// Close RS232 communication session
    /// </summary>
    public async Task CloseSessionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (_currentSession != null)
            {
                _currentSession.EndTime = DateTime.UtcNow;
                
                _logger.LogInformation("Closing RS232 session: {SessionId}, Duration: {Duration:F2}s, Success Rate: {SuccessRate:F1}%",
                    _currentSession.SessionId,
                    _currentSession.Duration.TotalSeconds,
                    _currentSession.SuccessRate);
            }

            if (_serialPort?.IsOpen == true)
            {
                _serialPort.Close();
            }

            _serialPort?.Dispose();
            _serialPort = null;

            await Task.CompletedTask; // For consistency with async interface
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error during RS232 session cleanup");
        }
    }

    /// <summary>
    /// Send command and wait for response
    /// </summary>
    public async Task<ProtocolResponse> SendCommandAsync(ProtocolRequest request, CancellationToken cancellationToken = default)
    {
        if (!IsSessionActive || _serialPort == null || _currentSession == null)
        {
            return ProtocolResponse.Failure("No active RS232 session", request.ExpectedResponse, TimeSpan.Zero);
        }

        var stopwatch = Stopwatch.StartNew();
        var attemptCount = 0;
        var maxAttempts = Math.Max(1, request.RetryCount + 1);

        _logger.LogDebug("Sending RS232 command: '{Command}', Expected: '{Expected}', Timeout: {Timeout}ms", 
            request.Command, request.ExpectedResponse, request.TimeoutMs);

        for (attemptCount = 1; attemptCount <= maxAttempts; attemptCount++)
        {
            try
            {
                // Clear any pending data
                _serialPort.DiscardInBuffer();
                _serialPort.DiscardOutBuffer();

                // Send command
                _serialPort.WriteLine(request.Command);
                _currentSession.CommandsSent++;

                _logger.LogTrace("RS232 TX (attempt {Attempt}): '{Command}'", attemptCount, request.Command);

                // Wait for response with timeout
                var response = await ReadResponseAsync(request.TimeoutMs, cancellationToken);

                _logger.LogTrace("RS232 RX (attempt {Attempt}): '{Response}'", attemptCount, response);

                // Validate response
                if (ValidateResponse(response, request.ExpectedResponse, request.IsRegexPattern))
                {
                    stopwatch.Stop();
                    _currentSession.CommandsSuccessful++;
                    
                    _logger.LogDebug("RS232 command successful: '{Command}' → '{Response}' ({Time}ms)", 
                        request.Command, response, stopwatch.ElapsedMilliseconds);

                    return ProtocolResponse.Success(response, request.ExpectedResponse, stopwatch.Elapsed, attemptCount);
                }

                // Response validation failed
                if (attemptCount < maxAttempts)
                {
                    _logger.LogWarning("RS232 response mismatch (attempt {Attempt}/{Max}): Expected '{Expected}', Got '{Actual}'", 
                        attemptCount, maxAttempts, request.ExpectedResponse, response);
                    
                    await Task.Delay(100, cancellationToken); // Brief delay before retry
                }
                else
                {
                    stopwatch.Stop();
                    var errorMsg = $"Response validation failed after {maxAttempts} attempts. Expected: '{request.ExpectedResponse}', Got: '{response}'";
                    
                    _logger.LogError("RS232 command failed: {Error}", errorMsg);
                    return ProtocolResponse.Failure(errorMsg, request.ExpectedResponse, stopwatch.Elapsed, attemptCount);
                }
            }
            catch (TimeoutException)
            {
                if (attemptCount < maxAttempts)
                {
                    _logger.LogWarning("RS232 timeout (attempt {Attempt}/{Max}) for command: '{Command}'", 
                        attemptCount, maxAttempts, request.Command);
                    
                    await Task.Delay(200, cancellationToken); // Longer delay after timeout
                }
                else
                {
                    stopwatch.Stop();
                    var errorMsg = $"Command timeout after {maxAttempts} attempts";
                    
                    _logger.LogError("RS232 command timed out: '{Command}' (timeout: {Timeout}ms)", 
                        request.Command, request.TimeoutMs);
                    
                    return ProtocolResponse.Failure(errorMsg, request.ExpectedResponse, stopwatch.Elapsed, attemptCount);
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                var errorMsg = $"Communication error: {ex.Message}";
                
                _logger.LogError(ex, "RS232 communication error for command: '{Command}'", request.Command);
                return ProtocolResponse.Failure(errorMsg, request.ExpectedResponse, stopwatch.Elapsed, attemptCount);
            }
        }

        // Should not reach here
        stopwatch.Stop();
        return ProtocolResponse.Failure("Unexpected retry loop exit", request.ExpectedResponse, stopwatch.Elapsed, attemptCount);
    }

    /// <summary>
    /// Send command sequence
    /// </summary>
    public async Task<IEnumerable<ProtocolResponse>> SendCommandSequenceAsync(IEnumerable<ProtocolRequest> requests, CancellationToken cancellationToken = default)
    {
        var responses = new List<ProtocolResponse>();

        foreach (var request in requests)
        {
            var response = await SendCommandAsync(request, cancellationToken);
            responses.Add(response);

            // Stop on critical command failure if specified
            if (!response.Success && request.IsCritical)
            {
                _logger.LogWarning("Critical command failed, stopping sequence: '{Command}'", request.Command);
                break;
            }

            // Brief delay between commands
            if (responses.Count < requests.Count())
            {
                await Task.Delay(50, cancellationToken);
            }
        }

        return responses;
    }

    /// <summary>
    /// Test connection health
    /// </summary>
    public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        if (!IsSessionActive || _serialPort == null)
        {
            return false;
        }

        try
        {
            // Simple connectivity test - send a basic command or just check port status
            return _serialPort.IsOpen && _serialPort.BytesToRead >= 0;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "RS232 connection test failed");
            return false;
        }
    }

    /// <summary>
    /// Get RS232 capabilities
    /// </summary>
    public ProtocolCapabilities GetCapabilities()
    {
        return new ProtocolCapabilities
        {
            Protocol = SupportedProtocol,
            SupportsHalfDuplex = true,
            SupportsFullDuplex = true,
            RequiresAddressing = false,
            SupportsMulticast = false,
            SupportsBroadcast = false,
            SupportedSpeeds = new[] { 1200, 2400, 4800, 9600, 19200, 38400, 57600, 115200 },
            SupportedDataPatterns = new[] { "n81", "n82", "e71", "e81", "o71", "o81" },
            MaxPayloadSize = 8192, // Reasonable limit for RS232
            MinResponseTime = 10,
            MaxResponseTime = 30000,
            SpecificFeatures = new Dictionary<string, object>
            {
                ["flow_control"] = new[] { "none", "rts_cts", "xon_xoff" },
                ["encoding"] = new[] { "ascii", "utf8" },
                ["line_endings"] = new[] { "\r\n", "\n", "\r" }
            }
        };
    }

    /// <summary>
    /// Read response from serial port with timeout
    /// </summary>
    private async Task<string> ReadResponseAsync(int timeoutMs, CancellationToken cancellationToken)
    {
        if (_serialPort == null)
        {
            throw new InvalidOperationException("Serial port not initialized");
        }

        var originalTimeout = _serialPort.ReadTimeout;
        _serialPort.ReadTimeout = timeoutMs;

        try
        {
            // Read until we get a line or timeout
            var response = await Task.Run(() =>
            {
                try
                {
                    return _serialPort.ReadLine();
                }
                catch (TimeoutException)
                {
                    // Try to read any available data
                    var available = _serialPort.BytesToRead;
                    if (available > 0)
                    {
                        var buffer = new byte[available];
                        _serialPort.Read(buffer, 0, available);
                        return Encoding.ASCII.GetString(buffer).Trim();
                    }
                    throw;
                }
            }, cancellationToken);

            return response.Trim();
        }
        finally
        {
            _serialPort.ReadTimeout = originalTimeout;
        }
    }

    /// <summary>
    /// Validate response against expected pattern
    /// </summary>
    private bool ValidateResponse(string actualResponse, string expectedResponse, bool isRegexPattern)
    {
        if (string.IsNullOrEmpty(expectedResponse))
        {
            return true; // No validation required
        }

        if (isRegexPattern)
        {
            try
            {
                return Regex.IsMatch(actualResponse, expectedResponse, RegexOptions.IgnoreCase);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("Invalid regex pattern '{Pattern}': {Error}", expectedResponse, ex.Message);
                return false;
            }
        }
        else
        {
            return actualResponse.Equals(expectedResponse, StringComparison.OrdinalIgnoreCase);
        }
    }

    /// <summary>
    /// Parse data pattern string (e.g., "n81")
    /// </summary>
    private (Parity parity, int dataBits, StopBits stopBits) ParseDataPattern(string pattern)
    {
        if (string.IsNullOrEmpty(pattern) || pattern.Length != 3)
        {
            _logger.LogWarning("Invalid data pattern '{Pattern}', using default n81", pattern);
            return (Parity.None, 8, StopBits.One);
        }

        // Parse parity (first character)
        var parity = pattern[0] switch
        {
            'n' or 'N' => Parity.None,
            'e' or 'E' => Parity.Even,
            'o' or 'O' => Parity.Odd,
            'm' or 'M' => Parity.Mark,
            's' or 'S' => Parity.Space,
            _ => Parity.None
        };

        // Parse data bits (second character)
        var dataBits = pattern[1] switch
        {
            '5' => 5,
            '6' => 6,
            '7' => 7,
            '8' => 8,
            _ => 8
        };

        // Parse stop bits (third character)
        var stopBits = pattern[2] switch
        {
            '1' => StopBits.One,
            '2' => StopBits.Two,
            _ => StopBits.One
        };

        return (parity, dataBits, stopBits);
    }

    /// <summary>
    /// Apply protocol-specific settings to serial port
    /// </summary>
    private void ApplyProtocolSettings(PortConfiguration portConfig)
    {
        if (_serialPort == null) return;

        // Apply any RS232-specific settings from protocol settings
        if (portConfig.GetProtocolSetting<string>("flow_control") is string flowControl)
        {
            _serialPort.Handshake = flowControl.ToLowerInvariant() switch
            {
                "rts_cts" => Handshake.RequestToSend,
                "xon_xoff" => Handshake.XOnXOff,
                "none" => Handshake.None,
                _ => Handshake.None
            };
        }

        if (portConfig.GetProtocolSetting<string>("encoding") is string encoding)
        {
            _serialPort.Encoding = encoding.ToLowerInvariant() switch
            {
                "utf8" => Encoding.UTF8,
                "ascii" => Encoding.ASCII,
                _ => Encoding.ASCII
            };
        }

        if (portConfig.GetProtocolSetting<string>("line_ending") is string lineEnding)
        {
            _serialPort.NewLine = lineEnding;
        }
    }

    /// <summary>
    /// Dispose resources
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            CloseSessionAsync().GetAwaiter().GetResult();
            _disposed = true;
        }
    }
}