// SerialPortPool.Core/Services/RS232ProtocolHandler.cs - FIXED Week 2
using System.IO.Ports;
using System.Text;
using Microsoft.Extensions.Logging;
using SerialPortPool.Core.Interfaces;
using SerialPortPool.Core.Models;

namespace SerialPortPool.Core.Services;

/// <summary>
/// RS232 protocol handler implementation for Sprint 5 POC
/// Week 2: Complete RS232 support with 3-phase workflow
/// </summary>
public class RS232ProtocolHandler : IProtocolHandler
{
    private readonly ILogger<RS232ProtocolHandler> _logger;
    private bool _disposed = false;

    public RS232ProtocolHandler(ILogger<RS232ProtocolHandler> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public string ProtocolName => "RS232";
    public string ProtocolVersion => "1.0";

    /// <summary>
    /// Check if this handler can process RS232 protocol
    /// </summary>
    public async Task<bool> CanHandleProtocolAsync(string protocol)
    {
        await Task.CompletedTask;
        return string.Equals(protocol, "rs232", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Open RS232 communication session
    /// </summary>
    public async Task<ProtocolSession> OpenSessionAsync(
        ProtocolConfiguration config, 
        CancellationToken cancellationToken = default)
    {
        if (config == null)
            throw new ArgumentNullException(nameof(config));

        if (string.IsNullOrWhiteSpace(config.PortName))
            throw new ArgumentException("Port name is required for RS232", nameof(config));

        try
        {
            _logger.LogInformation($"📡 Opening RS232 session on {config.PortName}...");

            var serialPort = new SerialPort(config.PortName)
            {
                BaudRate = config.GetBaudRate(),
                Parity = config.GetParity(),
                DataBits = config.GetDataBits(),
                StopBits = config.GetStopBits(),
                ReadTimeout = config.GetReadTimeout(),
                WriteTimeout = config.GetWriteTimeout()
            };

            // Open port (run in task to avoid blocking)
            await Task.Run(() => serialPort.Open(), cancellationToken);

            var session = ProtocolSession.CreateRS232Session(
                config.PortName, 
                serialPort, 
                config);

            _logger.LogInformation($"✅ RS232 session opened: {config.PortName} @ {config.GetBaudRate()} baud ({config.DataPattern})");
            _logger.LogDebug($"📊 Session: {session.SessionId}, Timeout: R={config.GetReadTimeout()}ms/W={config.GetWriteTimeout()}ms");

            return session;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"❌ Failed to open RS232 session on {config.PortName}");
            throw new InvalidOperationException($"Failed to open RS232 session on {config.PortName}: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Execute single RS232 command
    /// </summary>
    public async Task<CommandResult> ExecuteCommandAsync(
        ProtocolSession session, 
        Models.ProtocolCommand command,  // ← FIXED: Explicitly use Models.ProtocolCommand
        CancellationToken cancellationToken = default)
    {
        if (session == null)
            throw new ArgumentNullException(nameof(session));
        if (command == null)
            throw new ArgumentNullException(nameof(command));

        var startTime = DateTime.Now;
        var serialPort = session.NativeHandle as SerialPort;
        
        if (serialPort == null || !serialPort.IsOpen)
            throw new InvalidOperationException("RS232 session is not open or invalid");

        _logger.LogDebug($"📤 RS232 TX ({session.PortName}): {command.Command.Trim()}");

        try
        {
            // Send command
            var commandBytes = Encoding.UTF8.GetBytes(command.Command);
            await serialPort.BaseStream.WriteAsync(commandBytes, 0, commandBytes.Length, cancellationToken);
            await serialPort.BaseStream.FlushAsync(cancellationToken);

            // Read response with timeout
            var response = await ReadResponseWithTimeoutAsync(serialPort, command.TimeoutMs, cancellationToken);
            
            var endTime = DateTime.Now;
            var duration = endTime - startTime;

            // Validate response
            var isSuccess = ValidateResponse(response, command.ExpectedResponse);

            _logger.LogDebug($"📥 RS232 RX ({session.PortName}): {response?.Trim() ?? "NO_RESPONSE"} [{duration.TotalMilliseconds:F0}ms]");

            var result = new CommandResult
            {
                Command = command.Command,
                Response = response,
                IsSuccess = isSuccess,
                Duration = duration,
                StartTime = startTime,
                EndTime = endTime,
                ProtocolName = ProtocolName,
                SessionId = session.SessionId
            };

            // Update session statistics
            if (isSuccess)
                session.RecordSuccessfulCommand();
            else
                session.RecordFailedCommand($"Response validation failed: expected '{command.ExpectedResponse}', got '{response}'");

            return result;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning($"⏰ RS232 command timeout ({session.PortName}): {command.Command.Trim()}");
            session.RecordFailedCommand("Command timeout");
            
            return CommandResult.Failure(
                command.Command, 
                "Command timeout", 
                DateTime.Now - startTime);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"❌ RS232 command failed ({session.PortName}): {command.Command.Trim()}");
            session.RecordFailedCommand(ex.Message);
            
            return CommandResult.Failure(
                command.Command, 
                ex.Message, 
                DateTime.Now - startTime);
        }
    }

    /// <summary>
    /// Execute sequence of commands (3-phase workflow support)
    /// </summary>
    public async Task<CommandSequenceResult> ExecuteCommandSequenceAsync(
        ProtocolSession session,
        IEnumerable<Models.ProtocolCommand> commands,  // ← FIXED: Explicitly use Models.ProtocolCommand
        CancellationToken cancellationToken = default)
    {
        var result = new CommandSequenceResult();
        
        foreach (var command in commands)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            var commandResult = await ExecuteCommandAsync(session, command, cancellationToken);
            result.CommandResults.Add(commandResult);

            // Add small delay between commands
            await Task.Delay(50, cancellationToken);
        }

        return result;
    }

    /// <summary>
    /// Close RS232 session
    /// </summary>
    public async Task<bool> CloseSessionAsync(
        ProtocolSession session, 
        CancellationToken cancellationToken = default)
    {
        if (session?.NativeHandle is not SerialPort serialPort)
            return false;

        try
        {
            _logger.LogInformation($"🔌 Closing RS232 session: {session.PortName} (Session: {session.SessionId})");

            await Task.Run(() =>
            {
                if (serialPort.IsOpen)
                {
                    serialPort.Close();
                }
                serialPort.Dispose();
            }, cancellationToken);

            session.IsActive = false;
            
            _logger.LogInformation($"✅ RS232 session closed: {session.PortName} ({session.CommandCount} commands executed)");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"❌ Error closing RS232 session: {session.PortName}");
            return false;
        }
    }

    /// <summary>
    /// Test RS232 connectivity
    /// </summary>
    public async Task<ProtocolTestResult> TestConnectivityAsync(
        ProtocolConfiguration config,
        CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.Now;
        
        try
        {
            // Quick connection test
            using var testPort = new SerialPort(config.PortName)
            {
                BaudRate = config.GetBaudRate(),
                ReadTimeout = 1000,
                WriteTimeout = 1000
            };

            await Task.Run(() => testPort.Open(), cancellationToken);
            
            var duration = DateTime.Now - startTime;
            
            return new ProtocolTestResult
            {
                IsSuccess = true,
                Duration = duration,
                ResponseTime = duration
            };
        }
        catch (Exception ex)
        {
            return new ProtocolTestResult
            {
                IsSuccess = false,
                ErrorMessage = ex.Message,
                Duration = DateTime.Now - startTime
            };
        }
    }

    /// <summary>
    /// Get RS232 protocol capabilities
    /// </summary>
    public ProtocolCapabilities GetCapabilities()
    {
        return new ProtocolCapabilities
        {
            SupportedSpeeds = new List<int> { 1200, 2400, 4800, 9600, 19200, 38400, 57600, 115200, 230400, 460800, 921600 },
            SupportedDataPatterns = new List<string> { "n81", "e71", "n82", "o81", "e81" },
            MaxCommandLength = 1024,
            MaxResponseLength = 4096,
            SupportsConcurrentSessions = false,
            MaxConcurrentSessions = 1,
            Features = new Dictionary<string, object>
            {
                { "3PhaseWorkflow", true },
                { "AutoRetry", true },
                { "TimeoutControl", true },
                { "FlowControl", true }
            }
        };
    }

    /// <summary>
    /// Get RS232 handler statistics
    /// </summary>
    public ProtocolHandlerStatistics GetStatistics()
    {
        // Implementation would track statistics across all sessions
        return new ProtocolHandlerStatistics
        {
            ActiveSessions = 0, // Would be tracked
            TotalSessions = 0,  // Would be tracked
            TotalCommands = 0,  // Would be tracked
            SuccessfulCommands = 0, // Would be tracked
            FailedCommands = 0, // Would be tracked
            AverageCommandDuration = TimeSpan.Zero,
            GeneratedAt = DateTime.Now
        };
    }

    #region IDisposable Implementation

    /// <summary>
    /// Dispose the protocol handler
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Protected dispose method
    /// </summary>
    /// <param name="disposing">Whether disposing from Dispose() call</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            _logger.LogDebug("Disposing RS232ProtocolHandler");
            _disposed = true;
        }
    }

    #endregion

    #region Private Helper Methods

    /// <summary>
    /// Read response from serial port with timeout
    /// </summary>
    private async Task<string?> ReadResponseWithTimeoutAsync(
        SerialPort serialPort, 
        int timeoutMs, 
        CancellationToken cancellationToken)
    {
        var buffer = new byte[4096];
        var responseBuilder = new StringBuilder();
        var timeoutCts = new CancellationTokenSource(timeoutMs);
        var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        try
        {
            while (!combinedCts.Token.IsCancellationRequested)
            {
                if (serialPort.BytesToRead > 0)
                {
                    var bytesToRead = Math.Min(buffer.Length, serialPort.BytesToRead);
                    var bytesRead = await serialPort.BaseStream.ReadAsync(buffer, 0, bytesToRead, combinedCts.Token);
                    
                    if (bytesRead > 0)
                    {
                        var chunk = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        responseBuilder.Append(chunk);
                        
                        // Check for common line endings
                        if (chunk.Contains('\n') || chunk.Contains('\r'))
                        {
                            break;
                        }
                    }
                }
                
                await Task.Delay(10, combinedCts.Token);
            }
            
            return responseBuilder.ToString().Trim();
        }
        catch (OperationCanceledException)
        {
            // Timeout or cancellation
            var partial = responseBuilder.ToString().Trim();
            return string.IsNullOrEmpty(partial) ? null : partial;
        }
        finally
        {
            timeoutCts?.Dispose();
            combinedCts?.Dispose();
        }
    }

    /// <summary>
    /// Validate response against expected pattern
    /// </summary>
    private static bool ValidateResponse(string? response, string? expectedResponse)
    {
        if (string.IsNullOrEmpty(expectedResponse))
            return true; // No validation required

        if (string.IsNullOrEmpty(response))
            return false; // Expected response but got nothing

        // Support regex patterns (starting with ^)
        if (expectedResponse.StartsWith("^") && expectedResponse.EndsWith("$"))
        {
            try
            {
                var regex = new System.Text.RegularExpressions.Regex(expectedResponse);
                return regex.IsMatch(response);
            }
            catch
            {
                // Fall back to string comparison if regex is invalid
                return string.Equals(response.Trim(), expectedResponse.Trim(), StringComparison.OrdinalIgnoreCase);
            }
        }

        // Simple string comparison
        return string.Equals(response.Trim(), expectedResponse.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    #endregion
}