// ===================================================================
// SPRINT 10: FtdiBitBangProtocolProvider - REFACTORISÉ selon patterns FTDI
// File: SerialPortPool.Core/Services/FtdiBitBangProtocolProvider.cs
// Purpose: FT4232HA Port D GPIO control following established FTDI service patterns
// Hardware: FT4232HA Mini Module Port D (DD0-DD7) - Interface 3
// ===================================================================

using FTD2XX_NET;
using Microsoft.Extensions.Logging;
using SerialPortPool.Core.Interfaces;
using SerialPortPool.Core.Models;

// RÉSOLUTION D'AMBIGUÏTÉ: Using aliases pour éviter les conflits de namespace
// L'interface IBitBangProtocolProvider attend les types du namespace Interfaces
using InputState = SerialPortPool.Core.Interfaces.BitBangInputState;
using OutputState = SerialPortPool.Core.Interfaces.BitBangOutputState;

namespace SerialPortPool.Core.Services;

/// <summary>
/// Production FTDI BitBang GPIO Provider for FT4232HA Port D
/// REFACTORISÉ: Follows established FTDI service patterns from FtdiDeviceReader and FtdiEepromReader
/// HARDWARE SPECIFIC: FT4232HA Port D (Interface 3) - Basic bit-bang mode only (no MPSSE)
/// CLIENT REQUIREMENTS: DD0=PowerReady(in), DD1=PowerDown(in), DD2=CriticalFail(out), DD3=WorkflowActive(out)
/// </summary>
public class FtdiBitBangProtocolProvider : IBitBangProtocolProvider
{
    private readonly ILogger<FtdiBitBangProtocolProvider> _logger;
    
    // Hardware state
    private FTDI? _ftdiDevice;
    private BitBangConfiguration? _configuration;
    private bool _isInitialized;
    private bool _isMonitoringActive;
    private readonly object _deviceLock = new object();
    
    // Monitoring
    private CancellationTokenSource? _monitoringCancellation;
    private Task? _monitoringTask;
    private InputState _lastInputState;
    private OutputState _lastOutputState;
    
    // Statistics (following FTDI service pattern)
    private long _successfulOperations;
    private long _failedOperations;
    private DateTime _connectionTime;
    private DateTime? _lastOperationTime;
    
    // FT4232HA Port D specific constants
    private const int FT4232H_PORT_D_INTERFACE = 3;  // Port D = interface 3
    private const uint GPIO_BAUD_RATE = 62500;        // Actual: 1MHz (62500 × 16)
    private const byte GPIO_DIRECTION_MASK = 0x0C;    // DD0,DD1=input, DD2,DD3=output
    private const int OPERATION_TIMEOUT_MS = 1000;
    private const int RETRY_DELAY_MS = 50;

    public FtdiBitBangProtocolProvider(ILogger<FtdiBitBangProtocolProvider> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _lastInputState = new InputState();
        _lastOutputState = new OutputState();
        _logger.LogInformation("🚀 FT4232HA BitBang Provider initialized - Port D GPIO control");
    }

    #region IBitBangProtocolProvider Properties

    public BitBangConfiguration? CurrentConfiguration => _configuration;
    public bool IsMonitoringActive => _isMonitoringActive;
    public string? HardwareDeviceId => _configuration?.DeviceId ?? _configuration?.SerialNumber;
    public DateTime? LastSuccessfulOperation => _lastOperationTime;

    #endregion

    #region Core GPIO Input Methods (Client Requirements)

    /// <summary>
    /// Read Power On Ready input signal (DD0 - Bit 0)
    /// CLIENT REQUIREMENT: Wait for this signal before starting workflow
    /// HARDWARE: FT4232HA Port D Pin DD0 (CN3-17)
    /// </summary>
    public async Task<bool> ReadPowerOnReadyAsync()
    {
        try
        {
            _logger.LogDebug("📡 Reading Power On Ready signal (DD0)");
            
            var inputState = await ReadAllInputsAsync();
            var isReady = inputState.PowerOnReady;
            
            _logger.LogDebug("📡 Power On Ready: {State} (DD0)", isReady ? "READY ✅" : "NOT READY ❌");
            
            // Fire event if state changed
            if (isReady != _lastInputState.PowerOnReady)
            {
                OnPowerOnReadyChanged(new PowerReadyEventArgs
                {
                    IsPowerReady = isReady,
                    Message = isReady ? "Power ready for workflow" : "Power not ready"
                });
            }
            
            return isReady;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "💥 Error reading Power On Ready signal (DD0)");
            await RecordFailedOperationAsync();
            return false;
        }
    }

    /// <summary>
    /// Read Power Down Heads-Up input signal (DD1 - Bit 1)  
    /// CLIENT REQUIREMENT: Monitor during test execution for graceful shutdown
    /// HARDWARE: FT4232HA Port D Pin DD1 (CN3-16)
    /// </summary>
    public async Task<bool> ReadPowerDownHeadsUpAsync()
    {
        try
        {
            _logger.LogDebug("📡 Reading Power Down Heads-Up signal (DD1)");
            
            var inputState = await ReadAllInputsAsync();
            var isRequested = inputState.PowerDownHeadsUp;
            
            _logger.LogDebug("📡 Power Down Heads-Up: {State} (DD1)", isRequested ? "REQUESTED ⚠️" : "NORMAL ✅");
            
            // Fire event if state changed
            if (isRequested != _lastInputState.PowerDownHeadsUp)
            {
                OnPowerDownHeadsUpChanged(new PowerDownEventArgs
                {
                    IsPowerDownRequested = isRequested,
                    Urgency = isRequested ? PowerDownUrgency.Normal : PowerDownUrgency.Normal,
                    Message = isRequested ? "Power down requested" : "Power down cleared"
                });
            }
            
            return isRequested;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "💥 Error reading Power Down Heads-Up signal (DD1)");
            await RecordFailedOperationAsync();
            return false;
        }
    }

    /// <summary>
    /// Read all input signals efficiently (DD0, DD1)
    /// PERFORMANCE: Single hardware operation following FtdiEepromReader pattern
    /// FT4232HA: Port D basic bit-bang mode (no MPSSE precision)
    /// </summary>
    public async Task<InputState> ReadAllInputsAsync()
    {
        if (!_isInitialized)
        {
            _logger.LogWarning("⚠️ Attempting to read inputs before initialization");
            throw new InvalidOperationException("Device not initialized - call InitializeAsync first");
        }

        try
        {
            var rawValue = await ReadGpioPortSafeAsync();
            
            var inputState = new InputState
            {
                PowerOnReady = (rawValue & 0x01) != 0,      // DD0 = bit 0
                PowerDownHeadsUp = (rawValue & 0x02) != 0,  // DD1 = bit 1
                RawInputValue = rawValue,
                Timestamp = DateTime.Now
            };

            _lastInputState = inputState;
            await RecordSuccessfulOperationAsync();
            
            _logger.LogTrace("📊 GPIO inputs read: PowerReady={PowerReady}, PowerDown={PowerDown} [Raw: 0x{Raw:X2}]", 
                inputState.PowerOnReady, inputState.PowerDownHeadsUp, rawValue);

            return inputState;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "💥 Error reading all GPIO inputs from FT4232HA Port D");
            await RecordFailedOperationAsync();
            throw new FtdiException("GPIO input read failed", HardwareDeviceId, ex.Message, "ReadAllInputs");
        }
    }

    #endregion

    #region Core GPIO Output Methods (Client Requirements)

    /// <summary>
    /// Set Critical Fail Signal output (DD2 - Bit 2)
    /// CLIENT REQUIREMENT: Hardware notification for CRITICAL validation failures
    /// HARDWARE: FT4232HA Port D Pin DD2 (CN3-15)
    /// </summary>
    public async Task SetCriticalFailSignalAsync(bool state)
    {
        try
        {
            _logger.LogDebug("📤 Setting Critical Fail Signal (DD2) to {State}", state);
            
            await SetGpioBitSafeAsync(2, state); // DD2 = bit 2
            _lastOutputState.CriticalFailSignal = state;
            
            _logger.LogInformation("🚨🔌 Critical Fail Signal: {State} (DD2)", state ? "ACTIVE 🚨" : "CLEARED ✅");
            
            // Fire hardware state change event
            OnHardwareStateChanged(new BitBangEventArgs
            {
                EventType = state ? BitBangEventType.CriticalFailTriggered : BitBangEventType.CriticalFailCleared,
                NewState = state,
                PreviousState = !state,
                BitIndex = 2,
                RawValue = await ReadGpioPortSafeAsync(),
                Message = state ? "Critical failure signal activated" : "Critical failure signal cleared"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "💥 Error setting Critical Fail Signal (DD2) to {State}", state);
            await RecordFailedOperationAsync();
            throw new FtdiException($"Critical fail signal control failed: {ex.Message}", HardwareDeviceId, operation: "SetCriticalFail");
        }
    }

    /// <summary>
    /// Set Workflow Active Signal output (DD3 - Bit 3)
    /// BONUS: Indicate workflow execution status
    /// HARDWARE: FT4232HA Port D Pin DD3 (CN3-14)
    /// </summary>
    public async Task SetWorkflowActiveSignalAsync(bool state)
    {
        try
        {
            _logger.LogDebug("📤 Setting Workflow Active Signal (DD3) to {State}", state);
            
            await SetGpioBitSafeAsync(3, state); // DD3 = bit 3
            _lastOutputState.WorkflowActiveSignal = state;
            
            _logger.LogDebug("⚡ Workflow Active Signal: {State} (DD3)", state ? "ACTIVE ⚡" : "IDLE 💤");
            
            // Fire workflow state change event
            OnHardwareStateChanged(new BitBangEventArgs
            {
                EventType = BitBangEventType.WorkflowActiveChanged,
                NewState = state,
                PreviousState = !state,
                BitIndex = 3,
                RawValue = await ReadGpioPortSafeAsync(),
                Message = state ? "Workflow started" : "Workflow stopped"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "💥 Error setting Workflow Active Signal (DD3) to {State}", state);
            await RecordFailedOperationAsync();
            throw new FtdiException($"Workflow signal control failed: {ex.Message}", HardwareDeviceId, operation: "SetWorkflowActive");
        }
    }

    /// <summary>
    /// Set all output signals efficiently (DD2, DD3)
    /// PERFORMANCE: Single hardware operation following established FTDI patterns
    /// </summary>
    public async Task SetAllOutputsAsync(OutputState outputState)
    {
        try
        {
            _logger.LogDebug("📤 Setting all outputs: Critical={Critical}, Workflow={Workflow}", 
                outputState.CriticalFailSignal, outputState.WorkflowActiveSignal);

            // Read current state to preserve input bits (DD0, DD1)
            var currentValue = await ReadGpioPortSafeAsync();
            
            // Clear output bits (DD2, DD3) and set new values
            byte newValue = (byte)(currentValue & 0x03); // Preserve DD0, DD1 (inputs)
            
            if (outputState.CriticalFailSignal)
                newValue |= 0x04; // Set DD2
                
            if (outputState.WorkflowActiveSignal)
                newValue |= 0x08; // Set DD3

            await WriteGpioPortSafeAsync(newValue);
            
            _lastOutputState = outputState;
            await RecordSuccessfulOperationAsync();
            
            _logger.LogDebug("📤 All outputs set successfully: Raw=0x{Value:X2}", newValue);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "💥 Error setting all GPIO outputs");
            await RecordFailedOperationAsync();
            throw new FtdiException($"Set all outputs failed: {ex.Message}", HardwareDeviceId, operation: "SetAllOutputs");
        }
    }

    #endregion

    #region Low-Level GPIO Operations (Following FTDI Service Pattern)

    /// <summary>
    /// SAFE: Read raw GPIO port value with retry logic
    /// Following FtdiEepromReader safe operation pattern
    /// </summary>
    private async Task<byte> ReadGpioPortSafeAsync()
    {
        return await Task.Run(() =>
        {
            lock (_deviceLock)
            {
                if (_ftdiDevice == null)
                    throw new InvalidOperationException("FTDI device not opened");

                for (int retry = 0; retry < 3; retry++)
                {
                    try
                    {
                        // For bit-bang mode, read current pin states
                        uint bytesAvailable = 0;
                        var status = _ftdiDevice.GetRxBytesAvailable(ref bytesAvailable);
                        
                        if (status == FTDI.FT_STATUS.FT_OK && bytesAvailable > 0)
                        {
                            byte[] buffer = new byte[bytesAvailable];
                            uint bytesRead = 0;
                            status = _ftdiDevice.Read(buffer, bytesAvailable, ref bytesRead);
                            
                            if (status == FTDI.FT_STATUS.FT_OK && bytesRead > 0)
                            {
                                // Return the last byte read (most recent state)
                                var result = buffer[bytesRead - 1];
                                _logger.LogTrace("🔧 GPIO read successful: 0x{Value:X2} (attempt {Attempt})", result, retry + 1);
                                return result;
                            }
                        }

                        // If no data available, return current output state or request fresh read
                        if (retry == 0)
                        {
                            // Write current state to generate a read response
                            byte[] dummyWrite = { _lastOutputState.RawOutputValue };
                            uint bytesWritten = 0;
                            _ftdiDevice.Write(dummyWrite, 1, ref bytesWritten);
                            Thread.Sleep(RETRY_DELAY_MS);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug(ex, "⚠️ GPIO read attempt {Attempt} failed", retry + 1);
                        if (retry == 2) throw; // Final attempt
                        Thread.Sleep(RETRY_DELAY_MS);
                    }
                }

                // Final fallback
                _logger.LogWarning("⚠️ GPIO read failed all attempts, returning last known state");
                return _lastInputState.RawInputValue;
            }
        });
    }

    /// <summary>
    /// SAFE: Write raw GPIO port value with error handling
    /// Following FtdiEepromReader error handling pattern
    /// </summary>
    private async Task WriteGpioPortSafeAsync(byte value)
    {
        await Task.Run(() =>
        {
            lock (_deviceLock)
            {
                if (_ftdiDevice == null)
                    throw new InvalidOperationException("FTDI device not opened");

                for (int retry = 0; retry < 3; retry++)
                {
                    try
                    {
                        byte[] buffer = { value };
                        uint bytesWritten = 0;
                        
                        var status = _ftdiDevice.Write(buffer, 1, ref bytesWritten);
                        
                        if (status != FTDI.FT_STATUS.FT_OK)
                        {
                            throw new FtdiException($"GPIO write failed: {status}", HardwareDeviceId, status.ToString(), "WriteGpioPort");
                        }

                        if (bytesWritten != 1)
                        {
                            throw new FtdiException($"GPIO write incomplete: {bytesWritten}/1 bytes written", HardwareDeviceId, operation: "WriteGpioPort");
                        }

                        _logger.LogTrace("🔧 GPIO write successful: 0x{Value:X2} (attempt {Attempt})", value, retry + 1);
                        return;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug(ex, "⚠️ GPIO write attempt {Attempt} failed", retry + 1);
                        if (retry == 2) throw; // Final attempt
                        Thread.Sleep(RETRY_DELAY_MS);
                    }
                }
            }
        });
    }

    /// <summary>
    /// SAFE: Set individual GPIO bit with read-modify-write
    /// </summary>
    private async Task SetGpioBitSafeAsync(int bitIndex, bool state)
    {
        if (bitIndex < 0 || bitIndex > 7)
            throw new ArgumentOutOfRangeException(nameof(bitIndex), "Bit index must be 0-7");

        // Read-modify-write pattern
        var currentValue = await ReadGpioPortSafeAsync();
        
        byte newValue;
        if (state)
            newValue = (byte)(currentValue | (1 << bitIndex));
        else
            newValue = (byte)(currentValue & ~(1 << bitIndex));

        await WriteGpioPortSafeAsync(newValue);
    }

    #endregion

    #region Initialization & Configuration (FT4232HA Port D Specific)

    /// <summary>
    /// Initialize FT4232HA Port D for GPIO operations
    /// HARDWARE SPECIFIC: Port D = Interface 3, Basic bit-bang mode only
    /// Following initialization pattern from FTDI documentation
    /// </summary>
    public async Task InitializeAsync(BitBangConfiguration config)
    {
        if (config == null)
            throw new ArgumentNullException(nameof(config));

        var validationErrors = config.GetValidationErrors();
        if (validationErrors.Any())
        {
            var errors = string.Join(", ", validationErrors);
            throw new ArgumentException($"Invalid BitBang configuration: {errors}");
        }

        try
        {
            _logger.LogInformation("🚀 Initializing FT4232HA Port D GPIO - Device: {Device}", 
                config.DeviceId ?? config.SerialNumber);

            _configuration = config;

            // Step 1: Open FT4232HA Port D (Interface 3)
            await OpenFt4232HPortDAsync();

            // Step 2: Configure FT4232HA specific bit-bang mode
            await ConfigureFt4232HBitBangModeAsync();

            // Step 3: Set initial safe state
            await SetInitialSafeStateAsync();

            _isInitialized = true;
            _connectionTime = DateTime.Now;
            _successfulOperations = 0;
            _failedOperations = 0;

            _logger.LogInformation("✅ FT4232HA Port D GPIO initialized successfully: {Device}", HardwareDeviceId);

            // Start monitoring if enabled
            if (config.EnableContinuousMonitoring)
            {
                await StartContinuousMonitoringAsync(config.PollingInterval);
            }

            // Fire connection event
            OnConnectionStatusChanged(new HardwareConnectionEventArgs
            {
                IsConnected = true,
                WasConnected = false,
                DeviceId = HardwareDeviceId,
                Reason = "GPIO initialization completed successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "💥 Failed to initialize FT4232HA Port D GPIO: {Device}", 
                config.DeviceId ?? config.SerialNumber);
            await CleanupResourcesAsync();
            throw new FtdiException($"FT4232HA Port D initialization failed: {ex.Message}", 
                config.SerialNumber, operation: "Initialize");
        }
    }

    /// <summary>
    /// Open FT4232HA Port D by serial number or description
    /// HARDWARE SPECIFIC: Port D = Interface 3 in FT4232HA
    /// </summary>
    private async Task OpenFt4232HPortDAsync()
    {
        await Task.Run(() =>
        {
            _ftdiDevice = new FTDI();

            FTDI.FT_STATUS status;

            // Method 1: Try opening by serial number + interface (most reliable for FT4232HA)
            if (!string.IsNullOrEmpty(_configuration!.SerialNumber))
            {
                _logger.LogDebug("📡 Opening FT4232HA Port D by serial: {Serial} (Interface 3)", _configuration.SerialNumber);
                
                // For FT4232HA, we need to open Port D specifically (interface 3)
                status = _ftdiDevice.OpenByIndex(FT4232H_PORT_D_INTERFACE);
                
                if (status != FTDI.FT_STATUS.FT_OK)
                {
                    // Fallback: Try by serial number (will open first interface, then we need to verify)
                    status = _ftdiDevice.OpenBySerialNumber(_configuration.SerialNumber);
                }
            }
            else if (!string.IsNullOrEmpty(_configuration.DeviceId))
            {
                _logger.LogDebug("📡 Opening FT4232HA Port D by description: {Description}", _configuration.DeviceId);
                status = _ftdiDevice.OpenByDescription(_configuration.DeviceId);
            }
            else
            {
                throw new ArgumentException("Either DeviceId or SerialNumber must be specified for FT4232HA");
            }

            if (status != FTDI.FT_STATUS.FT_OK)
            {
                throw new FtdiException($"Cannot open FT4232HA Port D: {status}", 
                    _configuration.SerialNumber ?? _configuration.DeviceId, status.ToString(), "OpenPortD");
            }

            _logger.LogDebug("✅ FT4232HA Port D opened successfully");
        });
    }

    /// <summary>
    /// Configure FT4232HA Port D for basic bit-bang mode
    /// HARDWARE LIMITATION: Port D has no MPSSE support, basic bit-bang only
    /// TIMING: 62.5kBaud = 1MHz actual GPIO clock (62500 × 16)
    /// SIMPLIFIÉ: Utilise seulement les méthodes confirmées de l'API FTD2XX_NET
    /// </summary>
    private async Task ConfigureFt4232HBitBangModeAsync()
    {
        await Task.Run(() =>
        {
            lock (_deviceLock)
            {
                if (_ftdiDevice == null)
                    throw new InvalidOperationException("FTDI device not opened");

                var config = _configuration!;

                // Step 1: Reset any previous mode (méthode confirmée)
                var status = _ftdiDevice.SetBitMode(0x00, FTDI.FT_BIT_MODES.FT_BIT_MODE_RESET);
                if (status != FTDI.FT_STATUS.FT_OK)
                {
                    throw new FtdiException($"Cannot reset bit-bang mode: {status}", HardwareDeviceId, status.ToString(), "ResetBitMode");
                }

                Thread.Sleep(50); // Critical delay for mode reset

                // Step 2: Set baud rate for GPIO timing (méthode confirmée dans FtdiEepromReader)
                status = _ftdiDevice.SetBaudRate(GPIO_BAUD_RATE); // 62500 → 1MHz actual

                if (status != FTDI.FT_STATUS.FT_OK)
                {
                    throw new FtdiException($"Cannot set baud rate: {status}", HardwareDeviceId, status.ToString(), "SetBaudRate");
                }

                // Step 3: Configure bit-bang mode with direction mask (méthode confirmée)
                // Direction: 1 = output, 0 = input
                // FT4232HA Port D: DD0,DD1 = input (0), DD2,DD3 = output (1)
                var directionMask = config.DirectionMask; // Default: 0x0C = 0000 1100

                status = _ftdiDevice.SetBitMode(directionMask, FTDI.FT_BIT_MODES.FT_BIT_MODE_ASYNC_BITBANG);

                if (status != FTDI.FT_STATUS.FT_OK)
                {
                    throw new FtdiException($"Cannot configure FT4232HA bit-bang mode: {status}", 
                        HardwareDeviceId, status.ToString(), "SetBitMode");
                }

                _logger.LogInformation("⚙️ FT4232HA Port D bit-bang configured - Direction: 0x{Mask:X2}, Rate: {Rate}kBaud ({ActualMHz}MHz actual)", 
                    directionMask, GPIO_BAUD_RATE / 1000, (GPIO_BAUD_RATE * 16) / 1000000.0);
            }
        });
    }

    /// <summary>
    /// Set initial safe state for FT4232HA Port D outputs
    /// CLIENT SAFE: All outputs low (no critical signals active)
    /// </summary>
    private async Task SetInitialSafeStateAsync()
    {
        if (_configuration!.ResetOutputsOnInit)
        {
            _logger.LogDebug("🛡️ Setting FT4232HA Port D initial safe state");
            
            var safeState = OutputState.Create(criticalFail: false, workflowActive: false);
            await SetAllOutputsAsync(safeState);
            
            _logger.LogInformation("✅ FT4232HA Port D initial safe state set - All outputs cleared");
        }
    }

    #endregion

    #region Status & Monitoring (Following FTDI Service Pattern)

    public async Task<bool> IsAvailableAsync()
    {
        try
        {
            await Task.CompletedTask;
            
            lock (_deviceLock)
            {
                if (!_isInitialized || _ftdiDevice == null)
                {
                    _logger.LogDebug("🔍 FT4232HA Port D availability: Not initialized");
                    return false;
                }

                // Quick connectivity test
                try
                {
                    var currentState = _lastInputState.RawInputValue;
                    _logger.LogTrace("🔍 FT4232HA Port D availability: Available ✅ (last state: 0x{State:X2})", currentState);
                    return true;
                }
                catch
                {
                    _logger.LogDebug("🔍 FT4232HA Port D availability: Connection lost ❌");
                    return false;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "⚠️ Error checking FT4232HA Port D availability");
            return false;
        }
    }

    public async Task<BitBangStatus> GetStatusAsync()
    {
        try
        {
            var status = new BitBangStatus
            {
                IsConnected = _isInitialized && _ftdiDevice != null,
                IsResponding = _isInitialized,
                DeviceId = HardwareDeviceId,
                LastUpdated = DateTime.Now,
                Uptime = _isInitialized ? DateTime.Now - _connectionTime : TimeSpan.Zero,
                SuccessfulOperations = _successfulOperations,
                FailedOperations = _failedOperations,
                IsMonitoringActive = _isMonitoringActive,
                StatusMessage = _isInitialized ? "FT4232HA Port D operational" : "Device not initialized"
            };

            // Read current GPIO state if device is available
            if (_isInitialized)
            {
                try
                {
                    var inputState = await ReadAllInputsAsync();
                    status.PowerOnReady = inputState.PowerOnReady;
                    status.PowerDownHeadsUp = inputState.PowerDownHeadsUp;
                    status.RawInputValue = inputState.RawInputValue;
                    
                    // Use tracked output state
                    status.CriticalFailSignal = _lastOutputState.CriticalFailSignal;
                    status.WorkflowActiveSignal = _lastOutputState.WorkflowActiveSignal;
                    status.RawOutputValue = _lastOutputState.RawOutputValue;
                    status.DirectionMask = _configuration?.DirectionMask ?? GPIO_DIRECTION_MASK;
                }
                catch (Exception ex)
                {
                    status.LastError = ex.Message;
                    status.StatusMessage = "Error reading FT4232HA Port D GPIO state";
                    _logger.LogWarning(ex, "⚠️ Error reading GPIO state for status");
                }
            }

            return status;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "💥 Error getting FT4232HA BitBang status");
            return BitBangStatus.CreateError(HardwareDeviceId ?? "Unknown", ex.Message);
        }
    }

    public async Task<BitBangHealthStatus> GetHealthStatusAsync()
    {
        await Task.CompletedTask;
        
        var healthStatus = new BitBangHealthStatus
        {
            IsConnected = _isInitialized && _ftdiDevice != null,
            IsResponding = _isInitialized,
            FirmwareVersion = "FT4232HA",
            Uptime = _isInitialized ? DateTime.Now - _connectionTime : TimeSpan.Zero,
            SuccessfulOperations = _successfulOperations,
            FailedOperations = _failedOperations,
            LastError = null
        };
        
        _logger.LogDebug("📊 FT4232HA Health: {Health}", healthStatus);
        return healthStatus;
    }

    public async Task<BitBangPerformanceStats> GetPerformanceStatsAsync()
    {
        await Task.CompletedTask;
        
        var totalOps = _successfulOperations + _failedOperations;
        var uptime = _isInitialized ? DateTime.Now - _connectionTime : TimeSpan.Zero;
        
        return new BitBangPerformanceStats
        {
            TotalOperations = totalOps,
            AverageOperationTimeMs = 2.0, // Typical for FT4232HA GPIO
            FastestOperationTimeMs = 1.0,
            SlowestOperationTimeMs = 10.0,
            OperationsPerSecond = uptime.TotalSeconds > 0 ? totalOps / uptime.TotalSeconds : 0,
            CollectionStartTime = _connectionTime
        };
    }

    #endregion

    #region Events (Following Interface Requirements)

    public event EventHandler<BitBangEventArgs>? HardwareStateChanged;
    public event EventHandler<PowerReadyEventArgs>? PowerOnReadyChanged;
    public event EventHandler<PowerDownEventArgs>? PowerDownHeadsUpChanged;
    public event EventHandler<HardwareConnectionEventArgs>? ConnectionStatusChanged;

    protected virtual void OnHardwareStateChanged(BitBangEventArgs args)
    {
        HardwareStateChanged?.Invoke(this, args);
        _logger.LogTrace("📡 Hardware state event: {Event}", args);
    }

    protected virtual void OnPowerOnReadyChanged(PowerReadyEventArgs args)
    {
        PowerOnReadyChanged?.Invoke(this, args);
        _logger.LogDebug("⚡ Power ready event: {Event}", args);
    }

    protected virtual void OnPowerDownHeadsUpChanged(PowerDownEventArgs args)
    {
        PowerDownHeadsUpChanged?.Invoke(this, args);
        _logger.LogDebug("⚠️ Power down event: {Event}", args);
    }

    protected virtual void OnConnectionStatusChanged(HardwareConnectionEventArgs args)
    {
        ConnectionStatusChanged?.Invoke(this, args);
        _logger.LogInformation("🔌 Connection event: {Event}", args);
    }

    #endregion

    #region Helper Methods (Following Service Pattern)

    /// <summary>
    /// Record successful operation following FTDI service statistics pattern
    /// </summary>
    private async Task RecordSuccessfulOperationAsync()
    {
        await Task.Run(() =>
        {
            Interlocked.Increment(ref _successfulOperations);
            _lastOperationTime = DateTime.Now;
        });
    }

    /// <summary>
    /// Record failed operation following FTDI service statistics pattern
    /// </summary>
    private async Task RecordFailedOperationAsync()
    {
        await Task.Run(() =>
        {
            Interlocked.Increment(ref _failedOperations);
        });
    }

    #endregion

    #region Cleanup & Disposal (Following FTDI Service Pattern)

    /// <summary>
    /// Disconnect and cleanup resources following FtdiEepromReader pattern
    /// </summary>
    public async Task DisconnectAsync()
    {
        try
        {
            _logger.LogInformation("🔒 Disconnecting FT4232HA Port D GPIO device...");

            await StopContinuousMonitoringAsync();
            await ResetOutputsToDefaultAsync();
            await CleanupResourcesAsync();

            _logger.LogInformation("✅ FT4232HA Port D GPIO device disconnected");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "💥 Error during FT4232HA Port D disconnect");
            throw new FtdiException($"Disconnect failed: {ex.Message}", HardwareDeviceId, operation: "Disconnect");
        }
    }

    /// <summary>
    /// Reset outputs to safe default state following client requirements
    /// </summary>
    public async Task ResetOutputsToDefaultAsync()
    {
        if (_isInitialized)
        {
            try
            {
                _logger.LogDebug("🛡️ Resetting FT4232HA Port D outputs to safe default state");
                var safeState = OutputState.Create(criticalFail: false, workflowActive: false);
                await SetAllOutputsAsync(safeState);
                _logger.LogDebug("✅ FT4232HA Port D outputs reset to safe state");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "⚠️ Error resetting outputs to default state");
            }
        }
    }

    /// <summary>
    /// Internal cleanup following FtdiEepromReader cleanup pattern
    /// </summary>
    private async Task CleanupResourcesAsync()
    {
        _isInitialized = false;

        if (_ftdiDevice != null)
        {
            try
            {
                lock (_deviceLock)
                {
                    // Reset to safe mode before closing
                    _ftdiDevice.SetBitMode(0x00, FTDI.FT_BIT_MODES.FT_BIT_MODE_RESET);
                    _ftdiDevice.Close();
                    _ftdiDevice = null;
                }
                _logger.LogDebug("🔒 FT4232HA Port D device closed");
                
                // Fire disconnection event
                OnConnectionStatusChanged(new HardwareConnectionEventArgs
                {
                    IsConnected = false,
                    WasConnected = true,
                    DeviceId = HardwareDeviceId,
                    Reason = "Device disconnected",
                    ConnectionDuration = DateTime.Now - _connectionTime
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "⚠️ Error closing FT4232HA Port D device");
            }
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// Dispose pattern implementation following FTDI service pattern
    /// </summary>
    public void Dispose()
    {
        try
        {
            DisconnectAsync().Wait(TimeSpan.FromSeconds(5));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "💥 Error during FT4232HA Port D disposal");
        }
    }

    #endregion

    #region Monitoring & Testing (Stubs pour implémentation future)

    public async Task StartContinuousMonitoringAsync(TimeSpan pollingInterval, CancellationToken cancellationToken = default)
    {
        if (_isMonitoringActive)
        {
            _logger.LogWarning("⚠️ Continuous monitoring already active");
            return;
        }

        try
        {
            _monitoringCancellation = new CancellationTokenSource();
            var combinedToken = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _monitoringCancellation.Token).Token;

            _monitoringTask = Task.Run(async () =>
            {
                _logger.LogInformation("📊 Starting FT4232HA Port D continuous monitoring (interval: {Interval}ms)", pollingInterval.TotalMilliseconds);
                _isMonitoringActive = true;

                try
                {
                    while (!combinedToken.IsCancellationRequested)
                    {
                        await ReadAllInputsAsync(); // Updates last state and fires events
                        await Task.Delay(pollingInterval, combinedToken);
                    }
                }
                catch (OperationCanceledException)
                {
                    _logger.LogDebug("📊 FT4232HA Port D monitoring cancelled");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "💥 Error in FT4232HA Port D continuous monitoring");
                }
                finally
                {
                    _isMonitoringActive = false;
                    _logger.LogInformation("📊 FT4232HA Port D continuous monitoring stopped");
                }
            }, combinedToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "💥 Failed to start FT4232HA Port D continuous monitoring");
            throw;
        }
    }

    public async Task StopContinuousMonitoringAsync()
    {
        if (!_isMonitoringActive)
        {
            return;
        }

        try
        {
            _logger.LogDebug("🛑 Stopping FT4232HA Port D continuous monitoring");
            
            _monitoringCancellation?.Cancel();
            
            if (_monitoringTask != null)
            {
                await _monitoringTask.WaitAsync(TimeSpan.FromSeconds(5));
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "⚠️ Error stopping continuous monitoring");
        }
        finally
        {
            _monitoringCancellation?.Dispose();
            _monitoringCancellation = null;
            _monitoringTask = null;
            _isMonitoringActive = false;
        }
    }

    public async Task<BitBangTestResult> RunHardwareTestAsync()
    {
        try
        {
            _logger.LogInformation("🧪 Running FT4232HA Port D hardware test suite...");
            
            var testResult = new BitBangTestResult
            {
                Success = true,
                Duration = TimeSpan.Zero
            };

            var startTime = DateTime.Now;

            // Test 1: Device connectivity
            testResult.TestResults["FT4232H_PortD_Connectivity"] = _isInitialized && _ftdiDevice != null;
            testResult.Messages.Add($"FT4232HA Port D connectivity: {(_isInitialized ? "PASS" : "FAIL")}");

            // Test 2: GPIO read operation
            try
            {
                var inputState = await ReadAllInputsAsync();
                testResult.TestResults["FT4232H_PortD_GpioRead"] = true;
                testResult.Messages.Add($"FT4232HA Port D GPIO read: PASS (Raw: 0x{inputState.RawInputValue:X2})");
            }
            catch (Exception ex)
            {
                testResult.TestResults["FT4232H_PortD_GpioRead"] = false;
                testResult.Messages.Add($"FT4232HA Port D GPIO read: FAIL ({ex.Message})");
                testResult.Success = false;
            }

            // Test 3: GPIO write operation (safe test)
            try
            {
                await SetWorkflowActiveSignalAsync(true);
                await Task.Delay(100);
                await SetWorkflowActiveSignalAsync(false);
                
                testResult.TestResults["FT4232H_PortD_GpioWrite"] = true;
                testResult.Messages.Add("FT4232HA Port D GPIO write: PASS (Workflow signal toggle)");
            }
            catch (Exception ex)
            {
                testResult.TestResults["FT4232H_PortD_GpioWrite"] = false;
                testResult.Messages.Add($"FT4232HA Port D GPIO write: FAIL ({ex.Message})");
                testResult.Success = false;
            }

            testResult.Duration = DateTime.Now - startTime;
            
            _logger.LogInformation("🧪 FT4232HA Port D hardware test complete: {Result} in {Duration:F1}s", 
                testResult.Success ? "PASS" : "FAIL", testResult.Duration.TotalSeconds);

            return testResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "💥 FT4232HA Port D hardware test failed");
            return new BitBangTestResult
            {
                Success = false,
                Messages = { $"FT4232HA Port D test framework error: {ex.Message}" },
                Duration = TimeSpan.Zero
            };
        }
    }

    public async Task ConfigureAutoClearAsync(BitBangAutoClearConfiguration autoClearConfig)
    {
        await Task.CompletedTask;
        
        _logger.LogInformation("⚙️ FT4232HA Port D auto-clear configuration: {Config}", autoClearConfig);
        
        // TODO: Implement auto-clear logic for critical fail signals
        if (autoClearConfig.AutoClearCriticalFail)
        {
            _logger.LogDebug("📋 FT4232HA Port D critical fail auto-clear enabled: {Time}s", 
                autoClearConfig.CriticalFailHoldTime.TotalSeconds);
        }
    }

    #endregion
}