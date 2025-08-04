// SerialPortPool.Core/Services/BibWorkflowOrchestrator.cs - FIXED SIGNATURES
using Microsoft.Extensions.Logging;
using SerialPortPool.Core.Interfaces;
using SerialPortPool.Core.Models;

namespace SerialPortPool.Core.Services;

/// <summary>
/// BIB workflow orchestrator for complete 3-phase workflows
/// Week 2: Integration of BIB mapping + XML configuration + RS232 protocol
/// Uses ZERO TOUCH composition pattern with existing port reservation
/// </summary>
public class BibWorkflowOrchestrator : IBibWorkflowOrchestrator
{
    private readonly IPortReservationService _reservationService;
    private readonly IBibConfigurationLoader _configLoader;
    private readonly IBibMappingService _bibMapping;
    private readonly IProtocolHandlerFactory _protocolFactory;
    private readonly ILogger<BibWorkflowOrchestrator> _logger;

    public BibWorkflowOrchestrator(
        IPortReservationService reservationService,
        IBibConfigurationLoader configLoader,
        IBibMappingService bibMapping,
        IProtocolHandlerFactory protocolFactory,
        ILogger<BibWorkflowOrchestrator> logger)
    {
        _reservationService = reservationService ?? throw new ArgumentNullException(nameof(reservationService));
        _configLoader = configLoader ?? throw new ArgumentNullException(nameof(configLoader));
        _bibMapping = bibMapping ?? throw new ArgumentNullException(nameof(bibMapping));
        _protocolFactory = protocolFactory ?? throw new ArgumentNullException(nameof(protocolFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Execute complete BIB workflow: BIB.UUT.PORT → Protocol → 3-Phase Commands
    /// Week 2: RS232 protocol with temporary BIB mapping
    /// </summary>
    public async Task<BibWorkflowResult> ExecuteBibWorkflowAsync(
        string bibId,
        string uutId,
        int portNumber,
        string clientId = "BibWorkflow",
        CancellationToken cancellationToken = default)
    {
        var workflowResult = new BibWorkflowResult
        {
            WorkflowId = Guid.NewGuid().ToString(),
            BibId = bibId,
            UutId = uutId,
            PortNumber = portNumber,
            ClientId = clientId,
            StartTime = DateTime.Now
        };

        PortReservation? reservation = null;
        ProtocolSession? session = null;

        try
        {
            _logger.LogInformation($"🚀 Starting BIB workflow: {bibId}.{uutId}.{portNumber} for client {clientId}");

            // Phase 1: Load BIB configuration from XML
            var portConfig = await LoadPortConfigurationAsync(bibId, uutId, portNumber);
            workflowResult.ProtocolName = portConfig.Protocol;
            workflowResult.ConfigurationLoaded = true;

            _logger.LogInformation($"📄 Configuration loaded: {portConfig.Protocol.ToUpper()} @ {portConfig.Speed} ({portConfig.DataPattern})");

            // Phase 2: Find physical port through BIB mapping
            var physicalPort = await FindPhysicalPortAsync(bibId, uutId, portNumber);
            if (string.IsNullOrEmpty(physicalPort))
            {
                return workflowResult.WithError("Physical port mapping not found - check BIB mapping configuration");
            }

            workflowResult.PhysicalPort = physicalPort;
            _logger.LogInformation($"📍 Physical port mapped: {physicalPort}");

            // Phase 3: Reserve port using existing foundation (ZERO TOUCH composition)
            reservation = await ReservePortAsync(physicalPort, clientId);
            if (reservation == null)
            {
                return workflowResult.WithError($"Port reservation failed for {physicalPort}");
            }

            workflowResult.ReservationId = reservation.ReservationId;
            workflowResult.PortReserved = true;
            _logger.LogInformation($"🔒 Port reserved: {physicalPort} (Reservation: {reservation.ReservationId})");

            // Phase 4: Open protocol session
            var protocolHandler = _protocolFactory.GetHandler(portConfig.Protocol);
            var protocolConfig = CreateProtocolConfiguration(physicalPort, portConfig);

            // FIXED: OpenSessionAsync with correct signature
            session = await protocolHandler.OpenSessionAsync(protocolConfig, cancellationToken);
            workflowResult.SessionId = session.SessionId;
            workflowResult.SessionOpened = true;

            _logger.LogInformation($"📡 Protocol session opened: {session.SessionId} ({portConfig.Protocol.ToUpper()})");

            // Phase 5: Execute 3-phase workflow
            await Execute3PhaseWorkflowAsync(protocolHandler, session, portConfig, workflowResult, cancellationToken);

            // Phase 6: Determine overall success
            workflowResult.Success = workflowResult.AllPhasesSuccessful();
            workflowResult.EndTime = DateTime.Now;

            _logger.LogInformation($"✅ BIB workflow completed: {workflowResult.BibId}.{workflowResult.UutId}.{workflowResult.PortNumber} " +
                                 $"(Success: {workflowResult.Success}, Duration: {workflowResult.Duration.TotalSeconds:F1}s)");

            return workflowResult;
        }
        catch (Exception ex)
        {
            workflowResult.EndTime = DateTime.Now;
            var errorMessage = $"BIB workflow failed: {ex.Message}";
            
            _logger.LogError(ex, $"❌ {errorMessage} (Workflow: {workflowResult.WorkflowId})");
            return workflowResult.WithError(errorMessage);
        }
        finally
        {
            // Cleanup resources
            await CleanupWorkflowAsync(session, reservation, workflowResult.WorkflowId);
        }
    }

    /// <summary>
    /// Execute BIB workflow using automatic port discovery
    /// Week 2: Finds best available port for the specified BIB.UUT
    /// </summary>
    public async Task<BibWorkflowResult> ExecuteBibWorkflowAutoPortAsync(
        string bibId,
        string uutId,
        string clientId = "BibWorkflowAuto",
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation($"🔍 Auto-discovering ports for BIB workflow: {bibId}.{uutId}");

            // Find available ports for this UUT
            var availablePorts = await _bibMapping.GetUutPortsAsync(bibId, uutId);
            var mappedPorts = availablePorts.Where(m => !string.IsNullOrEmpty(m.PhysicalPort)).ToList();

            if (!mappedPorts.Any())
            {
                var errorResult = new BibWorkflowResult
                {
                    WorkflowId = Guid.NewGuid().ToString(),
                    BibId = bibId,
                    UutId = uutId,
                    ClientId = clientId,
                    StartTime = DateTime.Now,
                    EndTime = DateTime.Now
                };
                
                return errorResult.WithError($"No mapped ports found for {bibId}.{uutId}");
            }

            // Try ports in order until one succeeds
            foreach (var portMapping in mappedPorts.OrderBy(m => m.PortNumber))
            {
                _logger.LogDebug($"🎯 Attempting workflow with port {portMapping.PortNumber} ({portMapping.PhysicalPort})");
                
                var result = await ExecuteBibWorkflowAsync(bibId, uutId, portMapping.PortNumber, clientId, cancellationToken);
                
                if (result.Success)
                {
                    _logger.LogInformation($"✅ Auto-port workflow succeeded with port {portMapping.PortNumber}");
                    return result;
                }
                
                _logger.LogWarning($"⚠️ Port {portMapping.PortNumber} failed, trying next port...");
                
                // Small delay before trying next port
                await Task.Delay(1000, cancellationToken);
            }

            // All ports failed
            var failedResult = new BibWorkflowResult
            {
                WorkflowId = Guid.NewGuid().ToString(),
                BibId = bibId,
                UutId = uutId,
                ClientId = clientId,
                StartTime = DateTime.Now,
                EndTime = DateTime.Now
            };
            
            return failedResult.WithError($"All {mappedPorts.Count} ports failed for {bibId}.{uutId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"❌ Auto-port workflow failed for {bibId}.{uutId}");
            
            var errorResult = new BibWorkflowResult
            {
                WorkflowId = Guid.NewGuid().ToString(),
                BibId = bibId,
                UutId = uutId,
                ClientId = clientId,
                StartTime = DateTime.Now,
                EndTime = DateTime.Now
            };
            
            return errorResult.WithError($"Auto-port workflow error: {ex.Message}");
        }
    }

    #region Private Helper Methods

    /// <summary>
    /// Load port configuration from BIB XML
    /// </summary>
    private async Task<PortConfiguration> LoadPortConfigurationAsync(string bibId, string uutId, int portNumber)
    {
        var bibConfig = await _configLoader.LoadBibConfigurationAsync(bibId);
        if (bibConfig == null)
        {
            throw new InvalidOperationException($"BIB configuration not found: {bibId}");
        }

        var uutConfig = bibConfig.GetUut(uutId);
        var portConfig = uutConfig.GetPort(portNumber);

        // Week 2: Validate RS232 protocol
        if (!portConfig.Protocol.Equals("rs232", StringComparison.OrdinalIgnoreCase))
        {
            throw new NotSupportedException($"Week 2: Only RS232 protocol supported, found: {portConfig.Protocol}");
        }

        return portConfig;
    }

    /// <summary>
    /// Find physical port through BIB mapping
    /// </summary>
    private async Task<string?> FindPhysicalPortAsync(string bibId, string uutId, int portNumber)
    {
        try
        {
            var uutPorts = await _bibMapping.GetUutPortsAsync(bibId, uutId);
            var targetMapping = uutPorts.FirstOrDefault(m => m.PortNumber == portNumber);
            
            if (targetMapping == null)
            {
                _logger.LogWarning($"⚠️ No BIB mapping found for {bibId}.{uutId}.{portNumber}");
                return null;
            }

            return targetMapping.PhysicalPort;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"❌ Error finding physical port for {bibId}.{uutId}.{portNumber}");
            return null;
        }
    }

    /// <summary>
    /// Reserve port using existing foundation (ZERO TOUCH composition)
    /// </summary>
    private async Task<PortReservation?> ReservePortAsync(string physicalPort, string clientId)
    {
        try
        {
            // Create reservation criteria for specific port
            var criteria = new PortReservationCriteria
            {
                ValidationConfig = PortValidationConfiguration.CreateDevelopmentDefault(),
                DefaultReservationDuration = TimeSpan.FromMinutes(10),
                PreferredDeviceId = physicalPort // Use port name as device preference
            };

            // Use existing reservation service (ZERO TOUCH)
            var reservation = await _reservationService.ReservePortAsync(criteria, clientId);
            
            return reservation;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"❌ Error reserving port {physicalPort}");
            return null;
        }
    }

    /// <summary>
    /// Create protocol configuration from port configuration
    /// </summary>
    private static ProtocolConfiguration CreateProtocolConfiguration(string physicalPort, PortConfiguration portConfig)
    {
        var protocolConfig = new ProtocolConfiguration
        {
            PortName = physicalPort,
            Protocol = portConfig.Protocol,
            Speed = portConfig.Speed,
            DataPattern = portConfig.DataPattern,
            BibId = portConfig.ParentBibId,
            UutId = portConfig.ParentUutId,
            PortNumber = portConfig.PortNumber
        };

        // Copy protocol-specific settings
        foreach (var setting in portConfig.Settings)
        {
            protocolConfig.Settings[setting.Key] = setting.Value;
        }

        return protocolConfig;
    }

    /// <summary>
    /// Execute 3-phase workflow (Start → Test → Stop)
    /// </summary>
    private async Task Execute3PhaseWorkflowAsync(
        IProtocolHandler protocolHandler,
        ProtocolSession session,
        PortConfiguration portConfig,
        BibWorkflowResult workflowResult,
        CancellationToken cancellationToken)
    {
        // Phase 1: Start Commands
        _logger.LogInformation($"🔋 Executing START phase: {portConfig.StartCommands.Commands.Count} command(s)");
        workflowResult.StartResult = await ExecuteCommandSequenceAsync(
            protocolHandler, session, portConfig.StartCommands, "START", cancellationToken);

        if (!workflowResult.StartResult.IsSuccess)
        {
            _logger.LogWarning($"⚠️ START phase failed, stopping workflow");
            return;
        }

        // Phase 2: Test Commands
        _logger.LogInformation($"🧪 Executing TEST phase: {portConfig.TestCommands.Commands.Count} command(s)");
        workflowResult.TestResult = await ExecuteCommandSequenceAsync(
            protocolHandler, session, portConfig.TestCommands, "TEST", cancellationToken);

        if (!workflowResult.TestResult.IsSuccess)
        {
            _logger.LogWarning($"⚠️ TEST phase failed");
        }

        // Phase 3: Stop Commands (always execute for cleanup)
        _logger.LogInformation($"🔌 Executing STOP phase: {portConfig.StopCommands.Commands.Count} command(s)");
        workflowResult.StopResult = await ExecuteCommandSequenceAsync(
            protocolHandler, session, portConfig.StopCommands, "STOP", cancellationToken);

        if (!workflowResult.StopResult.IsSuccess)
        {
            _logger.LogWarning($"⚠️ STOP phase failed - cleanup may be incomplete");
        }
    }

    /// <summary>
    /// Execute a command sequence (Start/Test/Stop)
    /// FIXED: Correct parameter order for ExecuteCommandAsync
    /// </summary>
    private async Task<CommandSequenceResult> ExecuteCommandSequenceAsync(
        IProtocolHandler protocolHandler,
        ProtocolSession session,
        CommandSequence commandSequence,
        string phaseName,
        CancellationToken cancellationToken)
    {
        var sequenceResult = new CommandSequenceResult();
        var sequenceStartTime = DateTime.Now;

        try
        {
            _logger.LogDebug($"📤 {phaseName} phase: executing {commandSequence.Commands.Count} command(s)");

            foreach (var command in commandSequence.Commands)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    _logger.LogWarning($"⚠️ {phaseName} phase cancelled");
                    break;
                }

                // FIXED: Correct parameter order and added missing cancellationToken
                var commandResult = await protocolHandler.ExecuteCommandAsync(session, command, cancellationToken);
                sequenceResult.CommandResults.Add(commandResult);

                _logger.LogDebug($"📥 {phaseName} command result: {commandResult}");

                // Check if we should continue on failure
                if (!commandResult.IsSuccess && !commandSequence.ContinueOnFailure)
                {
                    _logger.LogWarning($"⚠️ {phaseName} phase stopped due to command failure (continue_on_failure=false)");
                    break;
                }

                // Small delay between commands
                await Task.Delay(100, cancellationToken);
            }

            var sequenceDuration = DateTime.Now - sequenceStartTime;
            _logger.LogInformation($"📊 {phaseName} phase completed: {sequenceResult.SuccessfulCommands}/{sequenceResult.TotalCommands} commands succeeded in {sequenceDuration.TotalSeconds:F1}s");

            return sequenceResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"❌ Error during {phaseName} phase execution");
            
            // Create error result
            var errorResult = CommandResult.Failure($"{phaseName}_PHASE_ERROR", ex.Message, DateTime.Now - sequenceStartTime);
            sequenceResult.CommandResults.Add(errorResult);
            
            return sequenceResult;
        }
    }

    /// <summary>
    /// Cleanup workflow resources
    /// FIXED: Correct parameter order for CloseSessionAsync
    /// </summary>
    private async Task CleanupWorkflowAsync(ProtocolSession? session, PortReservation? reservation, string workflowId)
    {
        try
        {
            _logger.LogDebug($"🧹 Cleaning up workflow resources: {workflowId}");

            // Close protocol session
            if (session != null && session.IsActive)
            {
                try
                {
                    var protocolHandler = _protocolFactory.GetHandler(session.ProtocolName);
                    // FIXED: Added CancellationToken.None parameter
                    await protocolHandler.CloseSessionAsync(session, CancellationToken.None);
                    _logger.LogDebug($"📡 Protocol session closed: {session.SessionId}");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, $"⚠️ Error closing protocol session: {session.SessionId}");
                }
            }

            // Release port reservation
            if (reservation != null && !reservation.IsExpired && reservation.IsActive)
            {
                try
                {
                    await _reservationService.ReleaseReservationAsync(reservation.ReservationId, reservation.ClientId);
                    _logger.LogDebug($"🔓 Port reservation released: {reservation.ReservationId}");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, $"⚠️ Error releasing port reservation: {reservation.ReservationId}");
                }
            }

            _logger.LogDebug($"✅ Workflow cleanup completed: {workflowId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"❌ Error during workflow cleanup: {workflowId}");
        }
    }

    #endregion

    #region Statistics and Monitoring

    /// <summary>
    /// Get workflow statistics
    /// </summary>
    public async Task<BibWorkflowStatistics> GetWorkflowStatisticsAsync()
    {
        // This would be implemented with proper workflow tracking in a production system
        await Task.CompletedTask;
        
        return new BibWorkflowStatistics
        {
            TotalWorkflows = 0,
            SuccessfulWorkflows = 0,
            FailedWorkflows = 0,
            AverageWorkflowDuration = TimeSpan.Zero,
            GeneratedAt = DateTime.Now
        };
    }

    #endregion
}