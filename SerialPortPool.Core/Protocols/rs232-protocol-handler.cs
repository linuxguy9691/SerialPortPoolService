// ===================================================================
// RS232 PROTOCOL HANDLER - IMPLÉMENTATION COMPLÈTE
// Fichier: SerialPortPool.Core/Protocols/rs232-protocol-handler.cs
// REMPLACEZ LE CONTENU EXISTANT PAR CECI
// ===================================================================

using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SerialPortPool.Core.Interfaces;
using SerialPortPool.Core.Models;

namespace SerialPortPool.Core.Protocols
{
    /// <summary>
    /// Gestionnaire de protocole RS232 complet
    /// </summary>
    public class RS232ProtocolHandler : IProtocolHandler
    {
        private readonly ILogger<RS232ProtocolHandler> _logger;
        private CommunicationSession? _currentSession;
        private SerialPort? _serialPort;
        private readonly ProtocolStatistics _statistics;
        private bool _disposed;

        public RS232ProtocolHandler(ILogger<RS232ProtocolHandler> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _statistics = new ProtocolStatistics();
        }

        #region IProtocolHandler Properties

        public string ProtocolName => "RS232";
        public string ProtocolVersion => "1.0.0";
        public string SupportedProtocol => "RS232";
        public bool IsSessionActive => _currentSession?.IsActive == true;
        public ProtocolSession? CurrentSession => _currentSession;

        #endregion

        #region IProtocolHandler Methods

        public ProtocolCapabilities GetCapabilities()
        {
            return new ProtocolCapabilities
            {
                ProtocolName = ProtocolName,
                Version = ProtocolVersion,
                SupportsAsyncOperations = true,
                SupportsSequenceCommands = true,
                SupportsBidirectionalCommunication = true,
                MaxConcurrentSessions = 1,
                DefaultTimeout = TimeSpan.FromSeconds(5),
                SupportedCommands = new List<string> { "SEND", "RECEIVE", "QUERY", "RESET" },
                SupportedBaudRates = new List<int> { 9600, 19200, 38400, 57600, 115200 }
            };
        }

        public async Task<bool> CanHandleProtocolAsync(string protocolName)
        {
            await Task.CompletedTask;
            return string.Equals(protocolName, "RS232", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(protocolName, "Serial", StringComparison.OrdinalIgnoreCase);
        }

        public async Task<ProtocolSession> OpenSessionAsync(string portName, PortConfiguration config, CancellationToken cancellationToken)
        {
            var protocolConfig = new ProtocolConfiguration
            {
                PortName = portName,
                BaudRate = config.BaudRate,
                DataBits = config.DataBits,
                Parity = config.Parity,
                StopBits = config.StopBits,
                Timeout = config.ReadTimeout
            };

            return await OpenSessionAsync(protocolConfig, cancellationToken);
        }

        public async Task<ProtocolSession> OpenSessionAsync(ProtocolConfiguration config, CancellationToken cancellationToken)
        {
            if (_currentSession?.IsActive == true)
            {
                throw new InvalidOperationException("Une session est déjà active");
            }

            try
            {
                _logger.LogInformation("Ouverture session RS232 sur {PortName}", config.PortName);

                _serialPort = new SerialPort(config.PortName)
                {
                    BaudRate = config.BaudRate,
                    DataBits = config.DataBits,
                    Parity = ParseParity(config.Parity),
                    StopBits = ParseStopBits(config.StopBits),
                    ReadTimeout = (int)config.Timeout.TotalMilliseconds,
                    WriteTimeout = (int)config.Timeout.TotalMilliseconds
                };

                await Task.Run(() => _serialPort.Open(), cancellationToken);

                _currentSession = new CommunicationSession
                {
                    SessionId = Guid.NewGuid().ToString(),
                    PortName = config.PortName,
                    Configuration = config,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true,
                    Status = "Connected"
                };

                return _currentSession;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur ouverture session RS232");
                throw;
            }
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
            if (session == null || !session.IsActive) return;

            try
            {
                if (_serialPort?.IsOpen == true)
                {
                    await Task.Run(() => _serialPort.Close(), cancellationToken);
                }

                session.IsActive = false;
                _logger.LogInformation("Session RS232 fermée");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur fermeture session RS232");
                throw;
            }
        }

        public async Task<ProtocolResponse> SendCommandAsync(ProtocolRequest request, CancellationToken cancellationToken)
        {
            if (!IsSessionActive || _serialPort?.IsOpen != true)
            {
                return ProtocolResponse.FromError("Aucune session active");
            }

            var startTime = DateTime.UtcNow;
            _statistics.TotalCommands++;

            try
            {
                await Task.Run(() => _serialPort!.Write(request.Data, 0, request.Data.Length), cancellationToken);

                var responseData = Array.Empty<byte>();
                if (_serialPort.BytesToRead > 0)
                {
                    var buffer = new byte[1024];
                    var bytesRead = await Task.Run(() => _serialPort.Read(buffer, 0, buffer.Length), cancellationToken);
                    responseData = buffer.Take(bytesRead).ToArray();
                }

                var executionTime = DateTime.UtcNow - startTime;
                _statistics.SuccessfulCommands++;
                _statistics.TotalExecutionTime += executionTime;

                return new ProtocolResponse
                {
                    RequestId = request.CommandId,
                    Success = true,
                    Data = responseData,
                    ExecutionTime = executionTime
                };
            }
            catch (Exception ex)
            {
                _statistics.FailedCommands++;
                _logger.LogError(ex, "Erreur commande RS232");
                return ProtocolResponse.FromError($"Erreur: {ex.Message}");
            }
        }

        public async Task<ProtocolResponse> ExecuteCommandAsync(ProtocolSession session, ProtocolCommand command, CancellationToken cancellationToken)
        {
            var request = new ProtocolRequest
            {
                Command = command.Name,
                Data = command.Data,
                Parameters = command.Parameters,
                Timeout = command.Timeout
            };

            return await SendCommandAsync(request, cancellationToken);
        }

        public async Task<IEnumerable<ProtocolResponse>> SendCommandSequenceAsync(IEnumerable<ProtocolRequest> requests, CancellationToken cancellationToken)
        {
            var responses = new List<ProtocolResponse>();

            foreach (var request in requests)
            {
                if (cancellationToken.IsCancellationRequested) break;

                var response = await SendCommandAsync(request, cancellationToken);
                responses.Add(response);

                if (!response.Success) break;
            }

            return responses;
        }

        public async Task<IEnumerable<ProtocolResponse>> ExecuteCommandSequenceAsync(ProtocolSession session, IEnumerable<ProtocolCommand> commands, CancellationToken cancellationToken)
        {
            var requests = commands.Select(cmd => new ProtocolRequest
            {
                Command = cmd.Name,
                Data = cmd.Data,
                Parameters = cmd.Parameters,
                Timeout = cmd.Timeout
            });

            return await SendCommandSequenceAsync(requests, cancellationToken);
        }

        public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken)
        {
            if (!IsSessionActive || _serialPort?.IsOpen != true) return false;

            try
            {
                var testRequest = new ProtocolRequest
                {
                    Command = "TEST",
                    Data = Encoding.UTF8.GetBytes("AT\r\n"),
                    Timeout = TimeSpan.FromSeconds(2)
                };

                var response = await SendCommandAsync(testRequest, cancellationToken);
                return response.Success;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> TestConnectivityAsync(ProtocolConfiguration config, CancellationToken cancellationToken)
        {
            try
            {
                using var testPort = new SerialPort(config.PortName)
                {
                    BaudRate = config.BaudRate,
                    DataBits = config.DataBits,
                    Parity = ParseParity(config.Parity),
                    StopBits = ParseStopBits(config.StopBits)
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

        public ProtocolStatistics GetStatistics()
        {
            return _statistics;
        }

        #endregion

        #region Helper Methods

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
                _serialPort?.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur disposition RS232ProtocolHandler");
            }

            _disposed = true;
        }

        #endregion
    }
}