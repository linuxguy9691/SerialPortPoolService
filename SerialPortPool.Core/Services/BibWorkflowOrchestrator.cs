// ===================================================================
// SPRINT 8 ENHANCED: BibWorkflowOrchestrator with Dynamic Port Discovery
// File: SerialPortPool.Core/Services/BibWorkflowOrchestrator.cs
// ENHANCEMENT: Uses DynamicPortMappingService instead of static mapping
// ===================================================================

using Microsoft.Extensions.Logging;
using SerialPortPool.Core.Interfaces;
using SerialPortPool.Core.Models;

namespace SerialPortPool.Core.Services;

/// <summary>
/// SPRINT 8 ENHANCED: BIB workflow orchestrator with dynamic port discovery
/// ZERO TOUCH: Maintains existing interface, adds dynamic capabilities
/// SMART: Automatically discovers and maps ports using EEPROM data
/// </summary>
public class BibWorkflowOrchestrator : IBibWorkflowOrchestrator
{
    private readonly IPortReservationService _reservationService;
    private readonly IBibConfigurationLoader _configLoader;
    //Needed only before EEPROM mapping implemented
    //private readonly IBibMappingService _bibMapping; // ← Keep for backwards compatibility
    private readonly IDynamicPortMappingService _dynamicPortMapping; // ← NEW Sprint 8
    private readonly IProtocolHandlerFactory _protocolFactory;
    private readonly ILogger<BibWorkflowOrchestrator> _logger;

    public BibWorkflowOrchestrator(
        IPortReservationService reservationService,
        IBibConfigurationLoader configLoader,
        //Needed only before EEPROM mapping implemented
        //IBibMappingService bibMapping, // ← Keep existing
        IDynamicPortMappingService dynamicPortMapping, // ← NEW Sprint 8
        IProtocolHandlerFactory protocolFactory,
        ILogger<BibWorkflowOrchestrator> logger)
    {
        _reservationService = reservationService ?? throw new ArgumentNullException(nameof(reservationService));
        _configLoader = configLoader ?? throw new ArgumentNullException(nameof(configLoader));
        //
        //_bibMapping = bibMapping ?? throw new ArgumentNullException(nameof(bibMapping));
        _dynamicPortMapping = dynamicPortMapping ?? throw new ArgumentNullException(nameof(dynamicPortMapping)); // ← NEW
        _protocolFactory = protocolFactory ?? throw new ArgumentNullException(nameof(protocolFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Execute complete BIB workflow: BIB.UUT.PORT → Protocol → 3-Phase Commands
    /// SPRINT 8 ENHANCED: Now uses dynamic port discovery
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
            _logger.LogInformation($"🚀 SPRINT 8: Starting DYNAMIC BIB workflow: {bibId}.{uutId}.{portNumber} for client {clientId}");

            // Phase 1: Load BIB configuration from XML
            var portConfig = await LoadPortConfigurationAsync(bibId, uutId, portNumber);
            workflowResult.ProtocolName = portConfig.Protocol;
            workflowResult.ConfigurationLoaded = true;

            _logger.LogInformation($"📄 Configuration loaded: {portConfig.Protocol.ToUpper()} @ {portConfig.Speed} ({portConfig.DataPattern})");

            // Phase 2: ✨ SPRINT 8 ENHANCEMENT - Dynamic port discovery
            var physicalPort = await FindPhysicalPortDynamicAsync(bibId, uutId, portNumber);
            if (string.IsNullOrEmpty(physicalPort))
            {
                return workflowResult.WithError("SPRINT 8: Dynamic port discovery failed - no suitable ports found");
            }

            workflowResult.PhysicalPort = physicalPort;
            _logger.LogInformation($"🎯 SPRINT 8: Dynamic port discovered: {physicalPort} (auto-mapped from hardware)");

            // Phase 3: Reserve port using existing foundation (ZERO TOUCH composition)
            reservation = await ReservePortAsync(physicalPort, clientId);
            if (reservation == null)
            {
                return workflowResult.WithError($"Port reservation failed for dynamically discovered port {physicalPort}");
            }

            workflowResult.ReservationId = reservation.ReservationId;
            workflowResult.PortReserved = true;
            _logger.LogInformation($"🔒 Port reserved: {physicalPort} (Reservation: {reservation.ReservationId})");

            // Phase 4: Open protocol session
            var protocolHandler = _protocolFactory.GetHandler(portConfig.Protocol);
            var protocolConfig = CreateProtocolConfiguration(physicalPort, portConfig);

            session = await protocolHandler.OpenSessionAsync(protocolConfig, cancellationToken);
            workflowResult.SessionId = session.SessionId;
            workflowResult.SessionOpened = true;

            _logger.LogInformation($"📡 Protocol session opened: {session.SessionId} ({portConfig.Protocol.ToUpper()})");

            // Phase 5: Execute 3-phase workflow
            await Execute3PhaseWorkflowAsync(protocolHandler, session, portConfig, workflowResult, cancellationToken);

            // Phase 6: Determine overall success
            workflowResult.Success = workflowResult.AllPhasesSuccessful();
            workflowResult.EndTime = DateTime.Now;

            _logger.LogInformation($"✅ SPRINT 8: Dynamic BIB workflow completed: {workflowResult.BibId}.{workflowResult.UutId}.{workflowResult.PortNumber} " +
                                 $"(Success: {workflowResult.Success}, Port: {physicalPort}, Duration: {workflowResult.Duration.TotalSeconds:F1}s)");

            return workflowResult;
        }
        catch (Exception ex)
        {
            workflowResult.EndTime = DateTime.Now;
            var errorMessage = $"SPRINT 8: Dynamic BIB workflow failed: {ex.Message}";
            
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
    /// SPRINT 8 ENHANCED: Now uses dynamic port mapping service
    /// </summary>
    public async Task<BibWorkflowResult> ExecuteBibWorkflowAutoPortAsync(
        string bibId,
        string uutId,
        string clientId = "BibWorkflowAuto",
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation($"🔍 SPRINT 8: Auto-discovering ports for DYNAMIC BIB workflow: {bibId}.{uutId}");

            // ✨ SPRINT 8: Use dynamic port mapping service
            var availableMappings = await _dynamicPortMapping.GetAllPortMappingsAsync();
            var relevantMappings = availableMappings.Where(m => 
                m.BibId.Equals(bibId, StringComparison.OrdinalIgnoreCase) && 
                m.UutId.Equals(uutId, StringComparison.OrdinalIgnoreCase)).ToList();

            //Not need after EEPROM mapping implemented
            /*if (!relevantMappings.Any())
            {
                _logger.LogWarning($"⚠️ SPRINT 8: No dynamic mappings found for {bibId}.{uutId}");
                
                // Fallback to legacy system
                _logger.LogInformation($"🔄 Falling back to legacy BIB mapping for {bibId}.{uutId}");
                // Use legacy BIB mapping service (if available)
                //var legacyPorts = await _bibMapping.GetUutPortsAsync(bibId, uutId);
                var mappedPorts = legacyPorts.Where(m => !string.IsNullOrEmpty(m.PhysicalPort)).ToList();

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
                    
                    return errorResult.WithError($"SPRINT 8: No ports found for {bibId}.{uutId} (both dynamic and legacy mapping failed)");
                }

                // Use legacy ports
                foreach (var portMapping in mappedPorts.OrderBy(m => m.PortNumber))
                {
                    _logger.LogDebug($"🎯 Attempting legacy workflow with port {portMapping.PortNumber} ({portMapping.PhysicalPort})");
                    
                    var result = await ExecuteBibWorkflowAsync(bibId, uutId, portMapping.PortNumber, clientId, cancellationToken);
                    
                    if (result.Success)
                    {
                        _logger.LogInformation($"✅ Legacy auto-port workflow succeeded with port {portMapping.PortNumber}");
                        return result;
                    }
                    
                    await Task.Delay(1000, cancellationToken);
                }

                var fallbackResult = new BibWorkflowResult
                {
                    WorkflowId = Guid.NewGuid().ToString(),
                    BibId = bibId,
                    UutId = uutId,
                    ClientId = clientId,
                    StartTime = DateTime.Now,
                    EndTime = DateTime.Now
                };
                
                return fallbackResult.WithError($"All legacy ports failed for {bibId}.{uutId}");
            }*/

            // ✨ SPRINT 8: Use dynamic mappings
            _logger.LogInformation($"🎯 SPRINT 8: Found {relevantMappings.Count} dynamic port mappings for {bibId}.{uutId}");

            foreach (var mapping in relevantMappings.OrderBy(m => m.UutPortNumber))
            {
                _logger.LogDebug($"🔌 SPRINT 8: Attempting dynamic workflow - UUT Port {mapping.UutPortNumber} → {mapping.PhysicalPort}");
                
                var result = await ExecuteBibWorkflowAsync(bibId, uutId, mapping.UutPortNumber, clientId, cancellationToken);
                
                if (result.Success)
                {
                    _logger.LogInformation($"✅ SPRINT 8: Dynamic auto-port workflow succeeded with UUT Port {mapping.UutPortNumber} → {mapping.PhysicalPort}");
                    return result;
                }
                
                _logger.LogWarning($"⚠️ Dynamic port {mapping.UutPortNumber} failed, trying next port...");
                await Task.Delay(1000, cancellationToken);
            }

            // All dynamic ports failed
            var failedResult = new BibWorkflowResult
            {
                WorkflowId = Guid.NewGuid().ToString(),
                BibId = bibId,
                UutId = uutId,
                ClientId = clientId,
                StartTime = DateTime.Now,
                EndTime = DateTime.Now
            };
            
            return failedResult.WithError($"SPRINT 8: All {relevantMappings.Count} dynamic ports failed for {bibId}.{uutId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"❌ SPRINT 8: Dynamic auto-port workflow failed for {bibId}.{uutId}");
            
            var errorResult = new BibWorkflowResult
            {
                WorkflowId = Guid.NewGuid().ToString(),
                BibId = bibId,
                UutId = uutId,
                ClientId = clientId,
                StartTime = DateTime.Now,
                EndTime = DateTime.Now
            };
            
            return errorResult.WithError($"SPRINT 8: Dynamic auto-port workflow error: {ex.Message}");
        }
    }

    #region SPRINT 8 Enhanced Helper Methods

    /// <summary>
    /// ✨ SPRINT 8: Find physical port using dynamic discovery with intelligent fallback
    /// STRATEGY: Try dynamic discovery first, fallback to legacy mapping
    /// </summary>
    private async Task<string?> FindPhysicalPortDynamicAsync(string bibId, string uutId, int portNumber)
{
    try
    {
        _logger.LogDebug($"🔍 SPRINT 8: Dynamic port discovery for {bibId}.{uutId}.{portNumber}");

        // SEULEMENT le dynamic mapping - PAS de fallback
        var dynamicPort = await _dynamicPortMapping.GetDynamicPortForUutPortAsync(bibId, uutId, portNumber);
        if (!string.IsNullOrEmpty(dynamicPort))
        {
            _logger.LogInformation($"🎯 SPRINT 8: Dynamic mapping SUCCESS - {bibId}.{uutId}.{portNumber} → {dynamicPort}");
            return dynamicPort;
        }

        _logger.LogError($"❌ SPRINT 8: Dynamic mapping failed for {bibId}.{uutId}.{portNumber}");
        return null; // ← Pas de fallback, FORCE le debug du dynamic mapping
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, $"❌ SPRINT 8: Error during dynamic port discovery for {bibId}.{uutId}.{portNumber}");
        return null;
    }
}

    /// <summary>
    /// Legacy port mapping (preserved for backwards compatibility)
    /// </summary>
    /// Not need after EEPROM mapping implemented
    /*private async Task<string?> FindPhysicalPortLegacyAsync(string bibId, string uutId, int portNumber)
    {
        try
        {
            var uutPorts = await _bibMapping.GetUutPortsAsync(bibId, uutId);
            var targetMapping = uutPorts.FirstOrDefault(m => m.PortNumber == portNumber);
            
            if (targetMapping == null)
            {
                _logger.LogWarning($"⚠️ No legacy BIB mapping found for {bibId}.{uutId}.{portNumber}");
                return null;
            }

            return targetMapping.PhysicalPort;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"❌ Error in legacy port mapping for {bibId}.{uutId}.{portNumber}");
            return null;
        }
    }*/

    #endregion

    #region Existing Helper Methods (Preserved from original implementation)

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
    /// </summary>
    /// <summary>
/// Execute a command sequence (Start/Test/Stop) - ENHANCED Sprint 9
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
        _logger.LogInformation($"📤 {phaseName} phase: executing {commandSequence.Commands.Count} command(s) " +
                             $"(continue_on_failure: {commandSequence.ContinueOnFailure})");

        foreach (var command in commandSequence.Commands)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning($"⚠️ {phaseName} phase cancelled");
                break;
            }

            _logger.LogDebug($"📤 {phaseName}: Executing command '{command.Command.Trim()}'");

            // Execute command via protocol handler
            var protocolResponse = await protocolHandler.ExecuteCommandAsync(session, command, cancellationToken);
            
            // Convert ProtocolResponse to CommandResult
            var commandResult = ConvertToCommandResult(command, protocolResponse);
            sequenceResult.CommandResults.Add(commandResult);

            // 🚀 SPRINT 9: Enhanced continue_on_failure logic
            var shouldContinue = DetermineIfShouldContinue(
                commandResult, 
                protocolResponse, 
                commandSequence, 
                command, 
                phaseName);

            _logger.LogInformation($"📥 {phaseName} command result: {commandResult} | Continue: {shouldContinue}");

            if (!shouldContinue)
            {
                _logger.LogWarning($"🛑 {phaseName} phase stopped due to validation result");
                break;
            }

            // Small delay between commands
            await Task.Delay(100, cancellationToken);
        }

        var sequenceDuration = DateTime.Now - sequenceStartTime;
        _logger.LogInformation($"📊 {phaseName} phase completed: {sequenceResult.SuccessfulCommands}/{sequenceResult.TotalCommands} " +
                             $"commands succeeded in {sequenceDuration.TotalSeconds:F1}s");

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
    /// Convert ProtocolResponse to CommandResult
    /// </summary>
    private static CommandResult ConvertToCommandResult(ProtocolCommand originalCommand, ProtocolResponse protocolResponse)
    {
        return new CommandResult
        {
            Command = originalCommand.Command,
            Response = protocolResponse.DataAsString,
            IsSuccess = protocolResponse.Success,
            ErrorMessage = protocolResponse.Success ? null : protocolResponse.ErrorMessage,
            Duration = protocolResponse.ExecutionTime,
            StartTime = protocolResponse.CompletedAt - protocolResponse.ExecutionTime,
            EndTime = protocolResponse.CompletedAt,
            ProtocolName = "RS232",
            SessionId = "session-id",
            Metadata = new Dictionary<string, object>(protocolResponse.Metadata)
        };
    }

    /// <summary>
    /// Cleanup workflow resources
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

    #region Interface Implementation (Statistics and Validation)

    /// <summary>
    /// Validate if workflow can be executed for specified parameters
    /// </summary>
    public async Task<bool> ValidateWorkflowAsync(string bibId, string uutId, int portNumber)
    {
        try
        {
            _logger.LogDebug($"🔍 Validating workflow: {bibId}.{uutId}.{portNumber}");

            // Check if BIB configuration exists and is valid
            var bibConfig = await _configLoader.LoadBibConfigurationAsync(bibId);
            if (bibConfig == null)
            {
                _logger.LogWarning($"⚠️ BIB configuration not found: {bibId}");
                return false;
            }

            // Check if UUT exists in BIB
            try
            {
                var uutConfig = bibConfig.GetUut(uutId);
                
                // Check if port exists in UUT
                var portConfig = uutConfig.GetPort(portNumber);
                
                // ✨ SPRINT 8: Check if dynamic or legacy port mapping exists
                var physicalPort = await FindPhysicalPortDynamicAsync(bibId, uutId, portNumber);
                if (string.IsNullOrEmpty(physicalPort))
                {
                    _logger.LogWarning($"⚠️ No port mapping (dynamic or legacy) for {bibId}.{uutId}.{portNumber}");
                    return false;
                }

                // Check if protocol is supported
                if (!_protocolFactory.IsProtocolSupported(portConfig.Protocol))
                {
                    _logger.LogWarning($"⚠️ Protocol not supported: {portConfig.Protocol}");
                    return false;
                }

                _logger.LogDebug($"✅ SPRINT 8: Workflow validation passed: {bibId}.{uutId}.{portNumber} → {physicalPort}");
                return true;
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning($"⚠️ Workflow validation failed: {ex.Message}");
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"❌ Error during workflow validation: {bibId}.{uutId}.{portNumber}");
            return false;
        }
    }

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

    /// <summary>
/// 🎯 SPRINT 9: Intelligent logic to determine if workflow should continue
/// </summary>
private bool DetermineIfShouldContinue(
    CommandResult commandResult,
    ProtocolResponse protocolResponse,
    CommandSequence commandSequence,
    ProtocolCommand command,
    string phaseName)
{
    try
    {
        // 🚀 SPRINT 9: Check if we have enhanced validation result
        if (protocolResponse.Metadata.TryGetValue("ValidationResult", out var validationObj) &&
            validationObj is EnhancedValidationResult enhancedResult)
        {
            _logger.LogDebug($"🎯 {phaseName}: Enhanced validation detected - Level: {enhancedResult.Level}");
            
            return ProcessEnhancedValidationResult(enhancedResult, commandSequence, command, phaseName);
        }
        
        // 🔄 LEGACY: Fall back to basic success/failure logic
        _logger.LogDebug($"📊 {phaseName}: Legacy validation mode - Success: {commandResult.IsSuccess}");
        
        return ProcessLegacyValidationResult(commandResult, commandSequence, phaseName);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, $"❌ Error determining continue logic for {phaseName}");
        return commandResult.IsSuccess || commandSequence.ContinueOnFailure;
    }
}

/// <summary>
/// 🚀 SPRINT 9: Process enhanced validation result with multi-level logic
/// </summary>
private bool ProcessEnhancedValidationResult(
    EnhancedValidationResult enhancedResult,
    CommandSequence commandSequence,
    ProtocolCommand command,
    string phaseName)
{
    var level = enhancedResult.Level;
    var sequenceSetting = commandSequence.ContinueOnFailure;
    
    // 🚨 CRITICAL: Always stop
    if (level == ValidationLevel.CRITICAL)
    {
        _logger.LogCritical($"🚨 {phaseName}: CRITICAL validation - EMERGENCY STOP");
        return false;
    }
    
    // ❌ FAIL: Check continue_on_failure setting
    if (level == ValidationLevel.FAIL)
    {
        _logger.LogWarning($"❌ {phaseName}: FAIL validation - continue_on_failure: {sequenceSetting}");
        return sequenceSetting;
    }
    
    // ⚠️ WARN: Continue with alert
    if (level == ValidationLevel.WARN)
    {
        _logger.LogWarning($"⚠️ {phaseName}: WARN validation - CONTINUING with alert");
        return true;
    }
    
    // ✅ PASS: Always continue
    return true;
}

/// <summary>
/// 🔄 LEGACY: Process basic validation result (backward compatibility)
/// </summary>
private bool ProcessLegacyValidationResult(
    CommandResult commandResult,
    CommandSequence commandSequence,
    string phaseName)
{
    if (commandResult.IsSuccess)
    {
        return true;
    }
    
    // Command failed - check continue_on_failure
    if (commandSequence.ContinueOnFailure)
    {
        _logger.LogWarning($"⚠️ {phaseName}: Command failed but continue_on_failure=true - continuing");
        return true;
    }
    else
    {
        _logger.LogWarning($"🛑 {phaseName}: Command failed and continue_on_failure=false - stopping");
        return false;
    }
}

// ===================================================================
// SPRINT 10: Multi-UUT Wrapper Methods - Option 1 (Simple Sequential)
// File: SerialPortPool.Core/Services/BibWorkflowOrchestrator.cs
// Purpose: Simple wrapper methods reusing 100% existing proven code
// Effort: 45 minutes | Risk: MINIMAL | Value: IMMEDIATE
// ===================================================================

// 🚀 ADD THESE METHODS TO EXISTING BibWorkflowOrchestrator CLASS

#region SPRINT 10: Multi-UUT Wrapper Methods (Option 1)

/// <summary>
/// 🆕 SPRINT 10: Execute workflow for ALL ports in a specific UUT
/// OPTION 1: Simple sequential execution reusing proven single-port method
/// </summary>
public async Task<List<BibWorkflowResult>> ExecuteBibWorkflowAllPortsAsync(
    string bibId,
    string uutId,
    string clientId = "MultiPortWorkflow",
    CancellationToken cancellationToken = default)
{
    var results = new List<BibWorkflowResult>();
    
    try
    {
        _logger.LogInformation($"🚀 SPRINT 10: Starting Multi-Port workflow: {bibId}.{uutId} (ALL PORTS)");

        // Load BIB configuration to get port list
        var bibConfig = await _configLoader.LoadBibConfigurationAsync(bibId);
        if (bibConfig == null)
        {
            var errorResult = CreateErrorResult(bibId, uutId, 0, clientId, "BIB configuration not found");
            results.Add(errorResult);
            return results;
        }

        var uut = bibConfig.GetUut(uutId);
        var totalPorts = uut.Ports.Count;
        
        _logger.LogInformation($"📊 Multi-Port execution: {totalPorts} ports discovered in {bibId}.{uutId}");

        // 🔄 OPTION 1: Sequential execution reusing existing method
        for (int i = 0; i < uut.Ports.Count; i++)
        {
            var port = uut.Ports[i];
            var portNumber = port.PortNumber;
            
            if (cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning($"⚠️ Multi-Port workflow cancelled at port {i + 1}/{totalPorts}");
                break;
            }

            _logger.LogInformation($"🔌 Executing port {i + 1}/{totalPorts}: {bibId}.{uutId}.{portNumber}");

            // ✅ REUSE: 100% existing proven method
            var portResult = await ExecuteBibWorkflowAsync(bibId, uutId, portNumber, clientId, cancellationToken);
            results.Add(portResult);

            // Log intermediate result
            var status = portResult.Success ? "✅ SUCCESS" : "❌ FAILED";
            _logger.LogInformation($"{status} Port {portNumber}: {portResult.GetSummary()}");

            // Small delay between ports for hardware stability
            if (i < uut.Ports.Count - 1) // Don't delay after last port
            {
                await Task.Delay(500, cancellationToken);
            }
        }

        // 📊 Final summary
        var successCount = results.Count(r => r.Success);
        var totalTime = results.Sum(r => r.Duration.TotalSeconds);
        
        _logger.LogInformation($"🎯 Multi-Port workflow completed: {successCount}/{results.Count} ports successful in {totalTime:F1}s");

        return results;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, $"💥 Multi-Port workflow failed for {bibId}.{uutId}");
        
        var errorResult = CreateErrorResult(bibId, uutId, 0, clientId, $"Multi-Port workflow error: {ex.Message}");
        results.Add(errorResult);
        return results;
    }
}

/// <summary>
/// 🆕 SPRINT 10: Execute workflow for ALL UUTs in a BIB
/// OPTION 1: Simple sequential execution reusing proven methods
/// </summary>
public async Task<List<BibWorkflowResult>> ExecuteBibWorkflowAllUutsAsync(
    string bibId,
    string clientId = "MultiUutWorkflow", 
    CancellationToken cancellationToken = default)
{
    var results = new List<BibWorkflowResult>();

    try
    {
        _logger.LogInformation($"🚀 SPRINT 10: Starting Multi-UUT workflow: {bibId} (ALL UUTS)");

        // Load BIB configuration to get UUT list
        var bibConfig = await _configLoader.LoadBibConfigurationAsync(bibId);
        if (bibConfig == null)
        {
            var errorResult = CreateErrorResult(bibId, "unknown", 0, clientId, "BIB configuration not found");
            results.Add(errorResult);
            return results;
        }

        var totalUuts = bibConfig.Uuts.Count;
        _logger.LogInformation($"📊 Multi-UUT execution: {totalUuts} UUTs discovered in {bibId}");

        // 🔄 OPTION 1: Sequential execution per UUT
        for (int i = 0; i < bibConfig.Uuts.Count; i++)
        {
            var uut = bibConfig.Uuts[i];
            
            if (cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning($"⚠️ Multi-UUT workflow cancelled at UUT {i + 1}/{totalUuts}");
                break;
            }

            _logger.LogInformation($"🏭 Executing UUT {i + 1}/{totalUuts}: {bibId}.{uut.UutId} ({uut.Ports.Count} ports)");

            // ✅ REUSE: Use the Multi-Port method we just created
            var uutResults = await ExecuteBibWorkflowAllPortsAsync(bibId, uut.UutId, clientId, cancellationToken);
            results.AddRange(uutResults);

            // UUT-level summary
            var uutSuccessCount = uutResults.Count(r => r.Success);
            var uutStatus = uutSuccessCount == uutResults.Count ? "✅ SUCCESS" : "⚠️ PARTIAL";
            
            _logger.LogInformation($"{uutStatus} UUT {uut.UutId}: {uutSuccessCount}/{uutResults.Count} ports successful");

            // Delay between UUTs for system stability
            if (i < bibConfig.Uuts.Count - 1)
            {
                await Task.Delay(1000, cancellationToken);
            }
        }

        // 📊 Final BIB-level summary
        var totalSuccessCount = results.Count(r => r.Success);
        var totalExecutionTime = results.Sum(r => r.Duration.TotalSeconds);
        
        _logger.LogInformation($"🎯 Multi-UUT workflow completed: {totalSuccessCount}/{results.Count} total ports successful in {totalExecutionTime:F1}s");

        return results;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, $"💥 Multi-UUT workflow failed for {bibId}");
        
        var errorResult = CreateErrorResult(bibId, "unknown", 0, clientId, $"Multi-UUT workflow error: {ex.Message}");
        results.Add(errorResult);
        return results;
    }
}

/// <summary>
/// 🆕 SPRINT 10: Execute COMPLETE BIB workflow (all UUTs, all ports)
/// OPTION 1: Convenience method with enhanced summary reporting
/// </summary>
public async Task<AggregatedWorkflowResult> ExecuteBibWorkflowCompleteAsync(
    string bibId,
    string clientId = "CompleteBibWorkflow",
    CancellationToken cancellationToken = default)
{
    var startTime = DateTime.Now;
    
    try
    {
        _logger.LogInformation($"🚀 SPRINT 10: Starting COMPLETE BIB workflow: {bibId}");

        // ✅ REUSE: Use Multi-UUT method
        var allResults = await ExecuteBibWorkflowAllUutsAsync(bibId, clientId, cancellationToken);
        
        // 📊 Create aggregated summary
        var aggregated = new AggregatedWorkflowResult
        {
            BibId = bibId,
            TotalWorkflows = allResults.Count,
            SuccessfulWorkflows = allResults.Count(r => r.Success),
            FailedWorkflows = allResults.Count(r => !r.Success),
            TotalExecutionTime = DateTime.Now - startTime,
            Results = allResults,
            GeneratedAt = DateTime.Now
        };

        // Enhanced statistics
        aggregated.UniqueUuts = allResults.Select(r => r.UutId).Distinct().Count();
        aggregated.AverageWorkflowDuration = allResults.Any() ? 
            TimeSpan.FromTicks((long)allResults.Average(r => r.Duration.Ticks)) : TimeSpan.Zero;

        _logger.LogInformation($"🎉 COMPLETE BIB workflow finished: {aggregated.GetDetailedSummary()}");

        return aggregated;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, $"💥 Complete BIB workflow failed for {bibId}");
        
        return new AggregatedWorkflowResult
        {
            BibId = bibId,
            TotalExecutionTime = DateTime.Now - startTime,
            ErrorMessage = $"Complete workflow error: {ex.Message}",
            GeneratedAt = DateTime.Now
        };
    }
}

/// <summary>
/// 🆕 SPRINT 10: Execute workflow with SUMMARY reporting focus
/// OPTION 1: Enhanced logging and reporting for management/monitoring
/// </summary>
public async Task<AggregatedWorkflowResult> ExecuteBibWorkflowWithSummaryAsync(
    string bibId,
    bool includeDetailedLogs = true,
    string clientId = "SummaryWorkflow",
    CancellationToken cancellationToken = default)
{
    if (includeDetailedLogs)
    {
        _logger.LogInformation($"📋 SUMMARY WORKFLOW: Enhanced logging enabled for {bibId}");
    }

    // ✅ REUSE: Use Complete method
    var result = await ExecuteBibWorkflowCompleteAsync(bibId, clientId, cancellationToken);

    if (includeDetailedLogs)
    {
        // 📊 Log detailed breakdown
        _logger.LogInformation($"📊 ═══ SUMMARY REPORT FOR {bibId} ═══");
        _logger.LogInformation($"📊 Total Workflows: {result.TotalWorkflows}");
        _logger.LogInformation($"📊 Success Rate: {result.SuccessRate:F1}%");
        _logger.LogInformation($"📊 Execution Time: {result.TotalExecutionTime.TotalMinutes:F1} minutes");
        _logger.LogInformation($"📊 Average per Workflow: {result.AverageWorkflowDuration.TotalSeconds:F1} seconds");
        
        // Group by UUT for detailed breakdown
        var uutGroups = result.Results.GroupBy(r => r.UutId).ToList();
        foreach (var uutGroup in uutGroups)
        {
            var uutSuccess = uutGroup.Count(r => r.Success);
            var uutTotal = uutGroup.Count();
            _logger.LogInformation($"📊   UUT {uutGroup.Key}: {uutSuccess}/{uutTotal} successful");
        }
        
        _logger.LogInformation($"📊 ═══ END SUMMARY REPORT ═══");
    }

    return result;
}

#region Helper Methods

/// <summary>
/// Create error result for failed workflows
/// </summary>
private BibWorkflowResult CreateErrorResult(string bibId, string uutId, int portNumber, string clientId, string errorMessage)
{
    return new BibWorkflowResult
    {
        WorkflowId = Guid.NewGuid().ToString(),
        BibId = bibId,
        UutId = uutId,
        PortNumber = portNumber,
        ClientId = clientId,
        StartTime = DateTime.Now,
        EndTime = DateTime.Now,
        Success = false,
        ErrorMessage = errorMessage
    };
}

#endregion

#endregion
}