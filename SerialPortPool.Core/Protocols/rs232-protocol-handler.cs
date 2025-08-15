// SerialPortPool.Core/Protocols/RS232ProtocolHandler.cs - COMPLETE IMPLEMENTATION
using System.IO.Ports;
using System.Text;
using Microsoft.Extensions.Logging;
using SerialPortPool.Core.Interfaces;
using SerialPortPool.Core.Models;

namespace SerialPortPool.Core.Protocols;

/// <summary>
/// Complete RS232 Protocol Handler Implementation
/// Handles real serial communication with comprehensive error handling
/// </summary>
public class RS232ProtocolHandler : IProtocolHandler
{
    private readonly ILogger<RS232ProtocolHandler> _logger;
    private SerialPort? _serialPort;
    private ProtocolSession? _currentSession;
    private readonly ProtocolStatistics _statistics;
    private bool _disposed;

    public RS232ProtocolHandler(ILogger<RS232ProtocolHandler> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _statistics = new ProtocolStatistics
        {
            StatisticsStartTime = DateTime.UtcNow
        };
    }

    #region IProtocolHandler Properties

    public string ProtocolName => "RS232";
    public string ProtocolVersion => "1.0.0";
    public string SupportedProtocol => "rs232";
    public bool IsSessionActive => _currentSession?.IsActive == true && _serialPort?.IsOpen == true;
    public ProtocolSession? CurrentSession => _currentSession;

    #endregion

    #region IProtocolHandler Methods

    /// <summary>
    /// Get protocol capabilities
    /// </summary>
    public ProtocolCapabilities GetCapabilities()
    {
        return new ProtocolCapabilities
        {
            ProtocolName = ProtocolName,
            Version = ProtocolVersion,
            SupportsAsyncOperations = true,
            SupportsSequenceCommands = true,
            SupportsBidirectionalCommunication = true,
            DefaultTimeout = TimeSpan.FromSeconds(5),
            SupportedCommands = new List<string> { "SEND", "RECEIVE", "QUERY", "RESET" },
            SupportedBaudRates = new List<int> { 9600, 19200, 38400, 57600, 115200, 230400, 460800, 921600 },
            SupportedSpeeds = new List<int> { 9600, 19200, 38400, 57600, 115200, 230400, 460800, 921600 },
            SupportedDataPatterns = new List<string> { "n81", "n82", "e71", "e72", "o71", "o72" },
            MaxCommandLength = 1024,
            MaxResponseLength = 4096,
            SupportsConcurrentSessions = false,
            MaxConcurrentSessions = 1,
            Features = new Dictionary<string, object>
            {
                ["Handshaking"] = "None, XOnXOff, RequestToSend, RequestToSendXOnXOff",
                ["FlowControl"] = "Hardware, Software, None",
                ["LineBreak"] = "Supported",
                ["TimeoutCustomizable"] = true,
                ["RetryLogic"] = true
            }
        };
    }

    /// <summary>
    /// Check if protocol can be handled
    /// </summary>
    public async Task<bool> CanHandleProtocolAsync(string protocolName)
    {
        await Task.CompletedTask;
        return protocolName.Equals("rs232", StringComparison.OrdinalIgnoreCase) ||
               protocolName.Equals("serial", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Open session with port configuration
    /// </summary>
    public async Task<ProtocolSession> OpenSessionAsync(string portName, PortConfiguration config, CancellationToken cancellationToken)
    {
        if (IsSessionActive)
        {
            throw new InvalidOperationException("A session is already active. Close the current session first.");
        }

        try
        {
            _logger.LogInformation($"Opening RS232 session on {portName}");

            // Create and configure serial port
            _serialPort = new SerialPort(portName)
            {
                BaudRate = config.GetBaudRate(),
                DataBits = config.GetDataBits(),
                Parity = config.GetParity(),
                StopBits = config.GetStopBits(),
                ReadTimeout = config.GetReadTimeout(),
                WriteTimeout = config.GetWriteTimeout(),
                Handshake = Handshake.None
            };

            // Open the port
            await Task.Run(() => _serialPort.Open(), cancellationToken);

            // Create session
            _currentSession = new CommunicationSession
            {
                SessionId = Guid.NewGuid().ToString(),
                PortName = portName,
                ProtocolName = ProtocolName,
                Configuration = new ProtocolConfiguration
                {
                    PortName = portName,
                    Protocol = "rs232",
                    BaudRate = config.GetBaudRate(),
                    DataBits = config.GetDataBits(),
                    Parity = config.GetParity().ToString(),
                    StopBits = config.GetStopBits().ToString(),
                    ReadTimeout = config.GetReadTimeout(),
                    WriteTimeout = config.GetWriteTimeout()
                },
                CreatedAt = DateTime.UtcNow,
                IsActive = true,
                Status = SessionStatus.Active
            };

            _logger.LogInformation($"RS232 session opened successfully: {_currentSession.SessionId}");
            return _currentSession;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to open RS232 session on {portName}");
            _serialPort?.Dispose();
            _serialPort = null;
            throw;
        }
    }

    /// <summary>
    /// Open session with protocol configuration
    /// </summary>
    public async Task<ProtocolSession> OpenSessionAsync(ProtocolConfiguration config, CancellationToken cancellationToken)
    {
        if (IsSessionActive)
        {
            throw new InvalidOperationException("A session is already active. Close the current session first.");
        }

        try
        {
            _logger.LogInformation($"Opening RS232 session on {config.PortName}");

            // Create and configure serial port
            _serialPort = new SerialPort(config.PortName)
            {
                BaudRate = config.BaudRate,
                DataBits = config.DataBits,
                Parity = ParseParity(config.Parity),
                StopBits = ParseStopBits(config.StopBits),
                ReadTimeout = config.ReadTimeout,
                WriteTimeout = config.WriteTimeout,
                Handshake = Handshake.None
            };

            // Open the port
            await Task.Run(() => _serialPort.Open(), cancellationToken);

            // Create session
            _currentSession = new CommunicationSession
            {
                SessionId = Guid.NewGuid().ToString(),
                PortName = config.PortName,
                ProtocolName = ProtocolName,
                Configuration = config,
                CreatedAt = DateTime.UtcNow,
                IsActive = true,
                Status = SessionStatus.Active
            };

            _logger.LogInformation($"RS232 session opened successfully: {_currentSession.SessionId}");
            return _currentSession;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to open RS232 session on {config.PortName}");
            _serialPort?.Dispose();
            _serialPort = null;
            throw;
        }
    }

    /// <summary>
    /// Close current session
    /// </summary>
    public async Task CloseSessionAsync(CancellationToken cancellationToken)
    {
        if (_currentSession != null)
        {
            await CloseSessionAsync(_currentSession, cancellationToken);
        }
    }

    /// <summary>
    /// Close specific session
    /// </summary>
    public async Task CloseSessionAsync(ProtocolSession session, CancellationToken cancellationToken)
    {
        if (session == null) return;

        try
        {
            _logger.LogInformation($"Closing RS232 session: {session.SessionId}");

            if (_serialPort?.IsOpen == true)
            {
                await Task.Run(() => _serialPort.Close(), cancellationToken);
            }

            session.IsActive = false;
            session.Status = SessionStatus.Closed;
            session.UpdateLastActivity();

            if (_currentSession?.SessionId == session.SessionId)
            {
                _currentSession = null;
            }

            _logger.LogInformation($"RS232 session closed: {session.SessionId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error closing RS232 session: {session.SessionId}");
            throw;
        }
        finally
        {
            _serialPort?.Dispose();
            _serialPort = null;
        }
    }

    /// <summary>
    /// Send command and get response
    /// </summary>
    public async Task<ProtocolResponse> SendCommandAsync(ProtocolRequest request, CancellationToken cancellationToken)
    {
        if (!IsSessionActive)
        {
            return ProtocolResponse.FromError("No active session");
        }

        var startTime = DateTime.UtcNow;
        _statistics.TotalCommands++;

        try
        {
            _logger.LogDebug($"Sending RS232 command: {request.Command}");

            // Send data
            var dataToSend = request.Data.Length > 0 ? request.Data : Encoding.UTF8.GetBytes(request.Command);
            await Task.Run(() => _serialPort!.Write(dataToSend, 0, dataToSend.Length), cancellationToken);

            // Read response
            var responseData = await ReadResponseAsync(request.Timeout, cancellationToken);
            
            var executionTime = DateTime.UtcNow - startTime;
            _statistics.SuccessfulCommands++;
            _statistics.TotalExecutionTime += executionTime;
            _statistics.LastCommandTime = DateTime.UtcNow;

            _currentSession?.UpdateLastActivity();

            var response = new ProtocolResponse
            {
                RequestId = request.CommandId,
                Success = true,
                Data = responseData,
                ExecutionTime = executionTime,
                CompletedAt = DateTime.UtcNow
            };

            _logger.LogDebug($"RS232 command completed: {request.Command} → {response.DataAsString.Trim()}");
            return response;
        }
        catch (TimeoutException ex)
        {
            _statistics.TimeoutCommands++;
            _statistics.FailedCommands++;
            _logger.LogWarning(ex, $"RS232 command timeout: {request.Command}");
            return ProtocolResponse.FromError($"Command timeout: {ex.Message}");
        }
        catch (Exception ex)
        {
            _statistics.FailedCommands++;
            _logger.LogError(ex, $"RS232 command failed: {request.Command}");
            return ProtocolResponse.FromError($"Command failed: {ex.Message}");
        }
    }

    // <summary>
/// Execute command with protocol command object - SPRINT 8 ENHANCED with regex validation
/// </summary>
public async Task<ProtocolResponse> ExecuteCommandAsync(ProtocolSession session, ProtocolCommand command, CancellationToken cancellationToken)
{
    var request = new ProtocolRequest
    {
        CommandId = command.CommandId,
        Command = command.Command,
        Data = command.Data.Length > 0 ? command.Data : Encoding.UTF8.GetBytes(command.Command),
        Parameters = command.Parameters,
        Timeout = command.Timeout,
        CreatedAt = DateTime.UtcNow
    };

    var protocolResponse = await SendCommandAsync(request, cancellationToken);
    
    // ✨ SPRINT 8 NEW: Use enhanced validation with regex support
    if (!string.IsNullOrEmpty(command.ExpectedResponse))
    {
        var cleanResponse = protocolResponse.DataAsString.Trim();
        _logger.LogDebug($"🔍 Debug validation - Response: '{cleanResponse}' (Length: {cleanResponse.Length})");
        _logger.LogDebug($"🔍 Debug validation - Bytes: [{string.Join(",", Encoding.UTF8.GetBytes(cleanResponse))}]");
        _logger.LogDebug($"🔍 Debug validation - Expected: '{command.ExpectedResponse}'");
        var validationResult = command.ValidateResponse(cleanResponse);;
        
        if (!validationResult.IsValid)
        {
            _logger.LogWarning("📊 Response validation failed: {ValidationResult}", validationResult.GetSummary());
            
            // Add validation details to response metadata
            protocolResponse.Metadata["ValidationResult"] = validationResult.IsValid.ToString();
            protocolResponse.Metadata["ValidationMessage"] = validationResult.Message;
            protocolResponse.Metadata["ValidationMethod"] = validationResult.ValidationMethod.ToString();
            
            // Add regex capture groups if available
            if (validationResult.CapturedGroups.Any())
            {
                protocolResponse.Metadata["CapturedGroups"] = string.Join(", ", 
                    validationResult.CapturedGroups.Select(g => $"{g.Key}={g.Value}"));
                
                _logger.LogDebug("🎯 Regex groups captured: {Groups}", 
                    string.Join(", ", validationResult.CapturedGroups.Select(g => $"{g.Key}='{g.Value}'")));
            }
        }
        else
        {
            _logger.LogDebug("✅ Response validation passed: {ValidationResult}", validationResult.GetSummary());
            
            // Add successful validation metadata
            protocolResponse.Metadata["ValidationResult"] = "true";
            protocolResponse.Metadata["ValidationMethod"] = validationResult.ValidationMethod.ToString();
            
            // Add regex capture groups for successful regex matches
            if (validationResult.CapturedGroups.Any())
            {
                foreach (var group in validationResult.CapturedGroups)
                {
                    protocolResponse.Metadata[$"Captured_{group.Key}"] = group.Value;
                }
            }
        }
    }

    return protocolResponse;
}

    /// <summary>
    /// Send command sequence
    /// </summary>
    public async Task<IEnumerable<ProtocolResponse>> SendCommandSequenceAsync(IEnumerable<ProtocolRequest> requests, CancellationToken cancellationToken)
    {
        var responses = new List<ProtocolResponse>();

        foreach (var request in requests)
        {
            if (cancellationToken.IsCancellationRequested) 
                break;

            var response = await SendCommandAsync(request, cancellationToken);
            responses.Add(response);

            if (!response.Success) 
                break;
        }

        return responses;
    }

    /// <summary>
    /// Execute command sequence
    /// </summary>
    public async Task<IEnumerable<ProtocolResponse>> ExecuteCommandSequenceAsync(ProtocolSession session, IEnumerable<ProtocolCommand> commands, CancellationToken cancellationToken)
    {
        var requests = commands.Select(cmd => new ProtocolRequest
        {
            CommandId = cmd.CommandId,
            Command = cmd.Command,
            Data = cmd.Data.Length > 0 ? cmd.Data : Encoding.UTF8.GetBytes(cmd.Command),
            Parameters = cmd.Parameters,
            Timeout = cmd.Timeout,
            CreatedAt = DateTime.UtcNow
        });

        return await SendCommandSequenceAsync(requests, cancellationToken);
    }

    /// <summary>
    /// Test connection
    /// </summary>
    public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken)
    {
        if (!IsSessionActive) return false;

        try
        {
            var testRequest = new ProtocolRequest
            {
                Command = "AT",
                Data = Encoding.UTF8.GetBytes("AT\r\n"),
                Timeout = TimeSpan.FromSeconds(2),
                CreatedAt = DateTime.UtcNow
            };

            var response = await SendCommandAsync(testRequest, cancellationToken);
            return response.Success;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Test connectivity with configuration
    /// </summary>
    public async Task<bool> TestConnectivityAsync(ProtocolConfiguration config, CancellationToken cancellationToken)
    {
        try
        {
            using var testPort = new SerialPort(config.PortName)
            {
                BaudRate = config.BaudRate,
                DataBits = config.DataBits,
                Parity = ParseParity(config.Parity),
                StopBits = ParseStopBits(config.StopBits),
                ReadTimeout = 1000,
                WriteTimeout = 1000
            };

            await Task.Run(() =>
            {
                testPort.Open();
                Thread.Sleep(100);
                testPort.Close();
            }, cancellationToken);

            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Get protocol statistics
    /// </summary>
    public ProtocolStatistics GetStatistics()
    {
        return _statistics;
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Read response from serial port
    /// </summary>
    private async Task<byte[]> ReadResponseAsync(TimeSpan timeout, CancellationToken cancellationToken)
    {
        var buffer = new List<byte>();
        var startTime = DateTime.UtcNow;

        return await Task.Run(() =>
        {
            while (DateTime.UtcNow - startTime < timeout && !cancellationToken.IsCancellationRequested)
            {
                try
                {
                    if (_serialPort!.BytesToRead > 0)
                    {
                        int byteRead = _serialPort.ReadByte();
                        if (byteRead != -1)
                        {
                            buffer.Add((byte)byteRead);
                            
                            // Check for line endings to determine end of response
                            if (buffer.Count >= 2)
                            {
                                var lastTwo = buffer.Skip(buffer.Count - 2).ToArray();
                                if (lastTwo[0] == '\r' && lastTwo[1] == '\n')
                                {
                                    break;
                                }
                            }
                        }
                    }
                    else
                    {
                        Thread.Sleep(10); // Small delay to prevent busy waiting
                    }
                }
                catch (TimeoutException)
                {
                    break;
                }
            }

            return buffer.ToArray();
        }, cancellationToken);
    }

    /// <summary>
    /// Parse parity from string
    /// </summary>
    private static Parity ParseParity(string parity)
    {
        return parity.ToUpperInvariant() switch
        {
            "NONE" => Parity.None,
            "EVEN" => Parity.Even,
            "ODD" => Parity.Odd,
            "MARK" => Parity.Mark,
            "SPACE" => Parity.Space,
            _ => Parity.None
        };
    }

    /// <summary>
    /// Parse stop bits from string
    /// </summary>
    private static StopBits ParseStopBits(string stopBits)
    {
        return stopBits.ToUpperInvariant() switch
        {
            "ONE" => StopBits.One,
            "TWO" => StopBits.Two,
            "ONEPOINTFIVE" => StopBits.OnePointFive,
            _ => StopBits.One
        };
    }

    #endregion

    #region IDisposable

    public void Dispose()
    {
        if (_disposed) return;

        try
        {
            CloseSessionAsync(CancellationToken.None).Wait(5000);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during RS232ProtocolHandler disposal");
        }

        _serialPort?.Dispose();
        _disposed = true;
    }

    #endregion
}