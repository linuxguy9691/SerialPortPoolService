// ===================================================================
// SPRINT 9: RS232ProtocolHandler - Multi-Level Validation + Hardware Hooks
// File: SerialPortPool.Core/Protocols/RS232ProtocolHandler.cs
// Purpose: FIXED - Compatible with existing service registration
// ===================================================================

using System.IO.Ports;
using System.Text;
using Microsoft.Extensions.Logging;
using SerialPortPool.Core.Interfaces;
using SerialPortPool.Core.Models;

namespace SerialPortPool.Core.Protocols;

/// <summary>
/// Enhanced RS232 Protocol Handler with Multi-Level Validation + Hardware Integration
/// SPRINT 9: Supports PASS/WARN/FAIL/CRITICAL classification with GPIO hooks
/// FIXED: Named RS232ProtocolHandler for compatibility with existing service registration
/// </summary>
public class RS232ProtocolHandler : IProtocolHandler
{
    private readonly ILogger<RS232ProtocolHandler> _logger;
    private readonly IBitBangProtocolProvider? _bitBangProvider;
    private SerialPort? _serialPort;
    private ProtocolSession? _currentSession;
    private readonly ProtocolStatistics _statistics;
    private bool _disposed;

    // ✨ NEW: Enhanced statistics with validation levels
    private readonly Dictionary<ValidationLevel, int> _validationLevelCounts = new()
    {
        [ValidationLevel.PASS] = 0,
        [ValidationLevel.WARN] = 0,
        [ValidationLevel.FAIL] = 0,
        [ValidationLevel.CRITICAL] = 0
    };

    public RS232ProtocolHandler(ILogger<RS232ProtocolHandler> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _bitBangProvider = null; // Will be injected later if available
        _statistics = new ProtocolStatistics
        {
            StatisticsStartTime = DateTime.UtcNow
        };
        
        _logger.LogInformation("🚀 Enhanced RS232 Handler initialized (Sprint 9)");
    }

    // Constructor with BitBang provider injection (for future use)
    public RS232ProtocolHandler(
        ILogger<RS232ProtocolHandler> logger,
        IBitBangProtocolProvider? bitBangProvider)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _bitBangProvider = bitBangProvider;
        _statistics = new ProtocolStatistics
        {
            StatisticsStartTime = DateTime.UtcNow
        };
        
        _logger.LogInformation("🚀 Enhanced RS232 Handler initialized with hardware support: {HardwareAvailable}", 
            bitBangProvider != null);
    }

    #region IProtocolHandler Properties

    public string ProtocolName => "RS232";
    public string ProtocolVersion => "9.0.0";
    public string SupportedProtocol => "rs232";
    public bool IsSessionActive => _currentSession?.IsActive == true && _serialPort?.IsOpen == true;
    public ProtocolSession? CurrentSession => _currentSession;

    #endregion

    #region Enhanced IProtocolHandler Methods

    /// <summary>
    /// 🚀 SPRINT 9: Enhanced ExecuteCommandAsync with Multi-Level Validation + Hardware Hooks
    /// </summary>
    public async Task<ProtocolResponse> ExecuteCommandAsync(
        ProtocolSession session, 
        ProtocolCommand command, 
        CancellationToken cancellationToken)
    {
        if (!IsSessionActive)
        {
            return ProtocolResponse.FromError("No active session");
        }

        var startTime = DateTime.UtcNow;
        _statistics.TotalCommands++;

        try
        {
            _logger.LogDebug("📤 Executing command with enhanced validation: {Command}", command.Command.Trim());
        
        // Log vers BibUutLogger si disponible
            if (session.Configuration?.BibLogger != null)
            {
                session.Configuration?.BibLogger.LogBibExecution(LogLevel.Information, "📤 TX: '{Command}'", command.Command);
            }
            // Send command
            var dataToSend = command.Data.Length > 0 ? command.Data : Encoding.UTF8.GetBytes(command.Command);
            await Task.Run(() => _serialPort!.Write(dataToSend, 0, dataToSend.Length), cancellationToken);

            // Read response
            var responseData = await ReadResponseAsync(command.Timeout, cancellationToken);
            var responseText = Encoding.UTF8.GetString(responseData).Trim();
            
            _logger.LogDebug("📥 Received response: '{Response}' (Length: {Length})", 
                responseText, responseText.Length);

            //Log vers BibUutLogger si disponible
            if (session.Configuration?.BibLogger != null)
            {
                session.Configuration?.BibLogger.LogBibExecution(LogLevel.Information, "📥 RX: '{Response}'",  responseText);
            }
            // ✨ SPRINT 9: Enhanced multi-level validation
            var enhancedResult = PerformEnhancedValidation(command, responseText);
            
            // 🔌 NEW: Handle hardware integration for critical conditions
            if (enhancedResult.ShouldTriggerCriticalOutput && _bitBangProvider != null)
            {
                await HandleCriticalHardwareSignalAsync(enhancedResult);
            }
            
            var executionTime = DateTime.UtcNow - startTime;
            _currentSession?.UpdateLastActivity();

            // Enhanced logging with validation level
            LogValidationResultWithHardware(enhancedResult, command.Command.Trim(), responseText);
            
            // Update enhanced statistics
            UpdateEnhancedStatistics(enhancedResult.Level, executionTime, true);

            return CreateEnhancedProtocolResponse(command, responseData, enhancedResult, executionTime);
        }
        catch (TimeoutException ex)
        {
            _statistics.TimeoutCommands++;
            UpdateEnhancedStatistics(ValidationLevel.FAIL, DateTime.UtcNow - startTime, false);
            _logger.LogWarning("⏰ Command timeout: {Command} - {Error}", command.Command.Trim(), ex.Message);
            return ProtocolResponse.FromError($"Command timeout: {ex.Message}");
        }
        catch (Exception ex)
        {
            UpdateEnhancedStatistics(ValidationLevel.CRITICAL, DateTime.UtcNow - startTime, false);
            _logger.LogError(ex, "💥 Command execution failed: {Command}", command.Command.Trim());
            return ProtocolResponse.FromError($"Command execution failed: {ex.Message}");
        }
    }

    /// <summary>
    /// 🎯 SPRINT 9: Enhanced validation logic with multi-level support
    /// FIXED: Removed async as no await is used
    /// </summary>
    private EnhancedValidationResult PerformEnhancedValidation(
        ProtocolCommand command, 
        string actualResponse)
    {
        // Check if this is a multi-level command
        if (command is MultiLevelProtocolCommand multiLevelCommand)
        {
            _logger.LogDebug("🎯 Performing multi-level validation with {PatternCount} levels", 
                multiLevelCommand.ValidationPatterns.Count + 1);
            
            var result = multiLevelCommand.ValidateResponseMultiLevel(actualResponse);
            result.ActualResponse = actualResponse;
            
            _logger.LogDebug("✨ Multi-level validation result: {Level} - {Message}", 
                result.Level, result.Message);
            
            return result;
        }
        else
        {
            // Legacy single-level validation with conversion to EnhancedValidationResult
            _logger.LogDebug("📊 Performing legacy validation (converted to enhanced)");
            
            var legacyResult = command.ValidateResponse(actualResponse);
            var enhancedResult = EnhancedValidationResult.FromLegacyResult(legacyResult);
            enhancedResult.ActualResponse = actualResponse;
            
            return enhancedResult;
        }
    }

    /// <summary>
    /// 🔌 SPRINT 9: Hardware integration for critical conditions
    /// </summary>
    private async Task HandleCriticalHardwareSignalAsync(EnhancedValidationResult validationResult)
    {
        try
        {
            if (_bitBangProvider != null && await _bitBangProvider.IsAvailableAsync())
            {
                _logger.LogCritical("🚨🔌 TRIGGERING Hardware Critical Fail Signal: {Message}", 
                    validationResult.Message);
                
                await _bitBangProvider.SetCriticalFailSignalAsync(true);
                
                _logger.LogInformation("🔌✅ Hardware critical signal triggered successfully");
                
                // Add hardware metadata to validation result
                validationResult.ValidationMetadata["HardwareSignalTriggered"] = true;
                validationResult.ValidationMetadata["HardwareSignalTime"] = DateTime.Now.ToString("O");
            }
            else
            {
                _logger.LogWarning("⚠️🔌 Hardware critical signal requested but bit bang provider not available");
                validationResult.ValidationMetadata["HardwareSignalRequested"] = true;
                validationResult.ValidationMetadata["HardwareSignalTriggered"] = false;
                validationResult.ValidationMetadata["HardwareSignalError"] = "BitBang provider not available";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "💥🔌 Failed to trigger hardware critical signal");
            
            // Add error metadata but don't fail the validation
            validationResult.ValidationMetadata["HardwareSignalError"] = ex.Message;
            validationResult.ValidationMetadata["HardwareSignalTriggered"] = false;
        }
    }

    /// <summary>
    /// 📊 SPRINT 9: Enhanced logging with validation levels + hardware status
    /// </summary>
    private void LogValidationResultWithHardware(
        EnhancedValidationResult result, 
        string command, 
        string response)
    {
        var icon = result.Level switch
        {
            ValidationLevel.PASS => "✅",
            ValidationLevel.WARN => "⚠️",
            ValidationLevel.FAIL => "❌",
            ValidationLevel.CRITICAL => "🚨",
            _ => "❓"
        };
        
        var hardwareIcon = result.ShouldTriggerCriticalOutput ? "🔌💥" : "";
        
        // Enhanced logging format
        _logger.Log(
            result.Level.GetLogLevel(),
            "{Icon} {HardwareIcon} {Level}: {Command} → '{Response}' | {Message}",
            icon, hardwareIcon, result.Level, command, response, result.Message);
        
        // Additional details for non-PASS levels
        if (result.Level != ValidationLevel.PASS)
        {
            if (result.CapturedGroups.Any())
            {
                _logger.LogDebug("🎯 Captured groups: {Groups}", 
                    string.Join(", ", result.CapturedGroups.Select(g => $"{g.Key}='{g.Value}'")));
            }
            
            if (!string.IsNullOrEmpty(result.MatchedPattern))
            {
                _logger.LogDebug("🔍 Matched pattern: {Pattern}", result.MatchedPattern);
            }
            
            if (result.ShouldTriggerCriticalOutput)
            {
                _logger.LogInformation("🔌 Hardware critical signal will be triggered");
            }
        }
    }

    /// <summary>
    /// 📈 SPRINT 9: Enhanced statistics with validation level tracking
    /// </summary>
    private void UpdateEnhancedStatistics(ValidationLevel level, TimeSpan duration, bool success)
    {
        // Update validation level counts
        _validationLevelCounts[level]++;
        
        // Update standard statistics
        _statistics.TotalExecutionTime += duration;
        _statistics.LastCommandTime = DateTime.UtcNow;
        
        if (success && (level == ValidationLevel.PASS || level == ValidationLevel.WARN))
        {
            _statistics.SuccessfulCommands++;
        }
        else
        {
            _statistics.FailedCommands++;
        }
    }

    /// <summary>
    /// 🎯 SPRINT 9: Create enhanced protocol response with multi-level metadata
    /// </summary>
    private ProtocolResponse CreateEnhancedProtocolResponse(
        ProtocolCommand command,
        byte[] responseData,
        EnhancedValidationResult validationResult,
        TimeSpan executionTime)
    {
        return new ProtocolResponse
        {
            RequestId = command.CommandId,
            Success = validationResult.ShouldContinueWorkflow,
            Data = responseData,
            ExecutionTime = executionTime,
            CompletedAt = DateTime.UtcNow,
            Metadata = new Dictionary<string, object>
            {
                // ✨ Enhanced validation metadata
                ["ValidationResult"] = validationResult,
                ["ValidationLevel"] = validationResult.Level.ToString(),
                ["ValidationMessage"] = validationResult.Message,
                ["ShouldContinueWorkflow"] = validationResult.ShouldContinueWorkflow,
                ["RequiresImmediateAttention"] = validationResult.RequiresImmediateAttention,
                
                // 🔌 Hardware integration metadata
                ["HardwareTriggered"] = validationResult.ShouldTriggerCriticalOutput,
                ["RequiresHardwareNotification"] = validationResult.RequiresHardwareNotification,
                
                // 🎯 Regex capture groups
                ["CapturedGroups"] = validationResult.CapturedGroups,
                ["MatchedPattern"] = validationResult.MatchedPattern,
                
                // 📊 Validation method information
                ["ValidationMethod"] = validationResult.ValidationMethod.ToString(),
                ["ValidatedAt"] = validationResult.ValidatedAt.ToString("O"),
                
                // 🔧 Command information
                ["CommandType"] = command.GetType().Name,
                ["IsMultiLevel"] = command is MultiLevelProtocolCommand
            }
        };
    }

    /// <summary>
    /// 📋 Get validation level statistics summary
    /// </summary>
    public string GetValidationLevelSummary()
    {
        var total = _validationLevelCounts.Values.Sum();
        if (total == 0) return "No validations performed yet";
        
        var summary = string.Join(", ", _validationLevelCounts
            .Where(kvp => kvp.Value > 0)
            .Select(kvp => $"{kvp.Key.GetIcon()}{kvp.Value}"));
        
        var passRate = total > 0 ? (_validationLevelCounts[ValidationLevel.PASS] * 100.0 / total) : 0;
        var criticalRate = total > 0 ? (_validationLevelCounts[ValidationLevel.CRITICAL] * 100.0 / total) : 0;
        
        return $"{summary} | Pass: {passRate:F1}%, Critical: {criticalRate:F1}%";
    }

    #endregion

    #region Standard IProtocolHandler Implementation (Inherited from Sprint 8)

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
                ["MultiLevelValidation"] = "PASS, WARN, FAIL, CRITICAL",
                ["HardwareIntegration"] = "BitBang Protocol Hooks",
                ["ValidationLevels"] = 4,
                ["HardwareTriggers"] = "Critical Fail Output",
                ["Handshaking"] = "None, XOnXOff, RequestToSend, RequestToSendXOnXOff",
                ["FlowControl"] = "Hardware, Software, None",
                ["TimeoutCustomizable"] = true,
                ["RetryLogic"] = true
            }
        };
    }

    public Task<bool> CanHandleProtocolAsync(string protocolName)
    {
        return Task.FromResult(
            protocolName.Equals("rs232", StringComparison.OrdinalIgnoreCase) ||
            protocolName.Equals("serial", StringComparison.OrdinalIgnoreCase));
    }

    public async Task<ProtocolSession> OpenSessionAsync(string portName, PortConfiguration config, CancellationToken cancellationToken)
    {
        if (IsSessionActive)
        {
            throw new InvalidOperationException("A session is already active. Close the current session first.");
        }

        try
        {
            _logger.LogInformation("🚀 Opening enhanced RS232 session on {PortName}", portName);

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

            // Create enhanced session
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

            _logger.LogInformation("✅ Enhanced RS232 session opened: {SessionId} with hardware support: {HardwareEnabled}", 
                _currentSession.SessionId, _bitBangProvider != null);
            
            return _currentSession;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "💥 Failed to open enhanced RS232 session on {PortName}", portName);
            _serialPort?.Dispose();
            _serialPort = null;
            throw;
        }
    }

    public async Task<ProtocolSession> OpenSessionAsync(ProtocolConfiguration config, CancellationToken cancellationToken)
    {
        // Convert ProtocolConfiguration to PortConfiguration for reuse
        var portConfig = new PortConfiguration
        {
            PortNumber = 1,
            Protocol = config.Protocol,
            Speed = config.BaudRate,
            DataPattern = $"{config.Parity.ToLower()[0]}{config.DataBits}{(config.StopBits == "Two" ? "2" : "1")}"
        };
        
        portConfig.Settings["read_timeout"] = config.ReadTimeout;
        portConfig.Settings["write_timeout"] = config.WriteTimeout;

        return await OpenSessionAsync(config.PortName, portConfig, cancellationToken);
    }

    public async Task CloseSessionAsync(CancellationToken cancellationToken)
    {
        if (_currentSession != null)
        {
            await CloseSessionAsync(_currentSession, cancellationToken);
        }
    }

    public async Task CloseSessionAsync(ProtocolSession session, CancellationToken cancellationToken)
    {
        if (session == null) return;

        try
        {
            _logger.LogInformation("🔒 Closing enhanced RS232 session: {SessionId}", session.SessionId);

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

            _logger.LogInformation("✅ Enhanced RS232 session closed: {SessionId} | Validation Summary: {Summary}", 
                session.SessionId, GetValidationLevelSummary());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "💥 Error closing enhanced RS232 session: {SessionId}", session.SessionId);
            throw;
        }
        finally
        {
            _serialPort?.Dispose();
            _serialPort = null;
        }
    }

    public async Task<ProtocolResponse> SendCommandAsync(ProtocolRequest request, CancellationToken cancellationToken)
    {
        // Convert to ProtocolCommand for enhanced validation
        var command = new ProtocolCommand
        {
            CommandId = request.CommandId,
            Command = request.Command,
            Data = request.Data,
            Parameters = request.Parameters,
            Timeout = request.Timeout,
            ExpectedResponse = null // No validation for direct requests
        };

        return await ExecuteCommandAsync(_currentSession!, command, cancellationToken);
    }

    public async Task<IEnumerable<ProtocolResponse>> SendCommandSequenceAsync(IEnumerable<ProtocolRequest> requests, CancellationToken cancellationToken)
    {
        var responses = new List<ProtocolResponse>();

        foreach (var request in requests)
        {
            if (cancellationToken.IsCancellationRequested) 
                break;

            var response = await SendCommandAsync(request, cancellationToken);
            responses.Add(response);

            // ✨ Enhanced: Stop sequence on FAIL or CRITICAL
            if (response.Metadata.TryGetValue("ValidationResult", out var validationResultObj) &&
                validationResultObj is EnhancedValidationResult validationResult &&
                !validationResult.ShouldContinueWorkflow)
            {
                _logger.LogWarning("🛑 Command sequence stopped due to {Level} validation", validationResult.Level);
                break;
            }
        }

        return responses;
    }

    public async Task<IEnumerable<ProtocolResponse>> ExecuteCommandSequenceAsync(ProtocolSession session, IEnumerable<ProtocolCommand> commands, CancellationToken cancellationToken)
    {
        var responses = new List<ProtocolResponse>();

        foreach (var command in commands)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            var response = await ExecuteCommandAsync(session, command, cancellationToken);
            responses.Add(response);

            // ✨ Enhanced: Stop sequence on validation failure
            if (response.Metadata.TryGetValue("ValidationResult", out var validationResultObj) &&
                validationResultObj is EnhancedValidationResult validationResult &&
                !validationResult.ShouldContinueWorkflow)
            {
                _logger.LogWarning("🛑 Command sequence stopped due to {Level} validation", validationResult.Level);
                break;
            }
        }

        return responses;
    }

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

    public Task<bool> TestConnectivityAsync(ProtocolConfiguration config, CancellationToken cancellationToken)
    {
        return Task.Run(() =>
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

                testPort.Open();
                Thread.Sleep(100);
                testPort.Close();
                return true;
            }
            catch
            {
                return false;
            }
        }, cancellationToken);
    }

    public ProtocolStatistics GetStatistics()
    {
        return _statistics;
    }

    #endregion

    #region Private Helper Methods

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
            _logger.LogError(ex, "💥 Error during Enhanced RS232ProtocolHandler disposal");
        }

        _serialPort?.Dispose();
        _disposed = true;
        
        _logger.LogInformation("🔒 Enhanced RS232ProtocolHandler disposed | Final Summary: {Summary}", 
            GetValidationLevelSummary());
    }

    #endregion
}