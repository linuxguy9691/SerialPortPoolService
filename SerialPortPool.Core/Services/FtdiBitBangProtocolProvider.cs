// ===================================================================
// SPRINT 10: FtdiBitBangProtocolProvider - Real GPIO Implementation
// File: SerialPortPool.Core/Services/FtdiBitBangProtocolProvider.cs
// Purpose: Real FTDI GPIO control via FTD2XX_NET for client requirements
// Hardware: FT4232HA Port D (DD0-DD7) dedicated GPIO
// ===================================================================

using FTD2XX_NET;
using Microsoft.Extensions.Logging;
using SerialPortPool.Core.Interfaces;
using SerialPortPool.Core.Models;

// Resolve ambiguity by using the types from BitBangConfiguration.cs
using InputState = SerialPortPool.Core.Models.BitBangInputState;
using OutputState = SerialPortPool.Core.Models.BitBangOutputState;

namespace SerialPortPool.Core.Services;

/// <summary>
/// Real FTDI GPIO implementation via FTD2XX_NET for FT4232HA Port D
/// SPRINT 10: Replace stub with actual hardware control
/// CLIENT REQUIREMENTS: DD0=PowerReady(in), DD1=PowerDown(in), DD2=CriticalFail(out), DD3=WorkflowActive(out)
/// 
/// NOTE: Uses BitBangInputState/BitBangOutputState from BitBangConfiguration.cs (Models namespace)
/// </summary>
public class FtdiBitBangProtocolProvider : IBitBangProtocolProvider
{
    private readonly ILogger<FtdiBitBangProtocolProvider> _logger;
    private FTDI? _ftdiDevice;
    private BitBangConfiguration? _configuration;
    private bool _isInitialized;
    private bool _isMonitoringActive;
    private CancellationTokenSource? _monitoringCancellation;
    private Task? _monitoringTask;
    private BitBangStatus _lastStatus;
    private readonly object _deviceLock = new object();

    // Performance tracking
    private long _successfulOperations;
    private long _failedOperations;
    private DateTime _connectionTime;
    private DateTime? _lastOperationTime;

    public FtdiBitBangProtocolProvider(ILogger<FtdiBitBangProtocolProvider> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _lastStatus = new BitBangStatus();
        _logger.LogInformation("🚀 Real FTDI BitBang Provider initialized - Sprint 10");
    }

    #region IBitBangProtocolProvider Properties

    public BitBangConfiguration? CurrentConfiguration => _configuration;
    public bool IsMonitoringActive => _isMonitoringActive;
    public string? HardwareDeviceId => _configuration?.DeviceId ?? _configuration?.SerialNumber;
    public DateTime? LastSuccessfulOperation => _lastOperationTime;

    #endregion

    #region Core GPIO Input Methods (Client Requirements)

    /// <summary>
    /// 📡 Read Power On Ready input signal (DD0 - Bit 0)
    /// CLIENT REQUIREMENT: Wait for this signal before starting workflow
    /// </summary>
    public async Task<bool> ReadPowerOnReadyAsync()
    {
        try
        {
            var inputState = await ReadAllInputsAsync();
            var isReady = inputState.PowerOnReady;
            
            _logger.LogDebug("📡 Power On Ready: {State} (DD0)", isReady ? "READY ✅" : "NOT READY ❌");
            return isReady;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "💥 Error reading Power On Ready signal");
            _failedOperations++;
            return false;
        }
    }

    /// <summary>
    /// 📡 Read Power Down Heads-Up input signal (DD1 - Bit 1)  
    /// CLIENT REQUIREMENT: Monitor during test execution for graceful shutdown
    /// </summary>
    public async Task<bool> ReadPowerDownHeadsUpAsync()
    {
        try
        {
            var inputState = await ReadAllInputsAsync();
            var isRequested = inputState.PowerDownHeadsUp;
            
            _logger.LogDebug("📡 Power Down Heads-Up: {State} (DD1)", isRequested ? "REQUESTED ⚠️" : "NORMAL ✅");
            return isRequested;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "💥 Error reading Power Down Heads-Up signal");
            _failedOperations++;
            return false;
        }
    }

    /// <summary>
    /// 📡 Read all input signals efficiently (DD0, DD1)
    /// PERFORMANCE: Single hardware operation for both inputs
    /// </summary>
    public async Task<InputState> ReadAllInputsAsync()
    {
        if (!_isInitialized)
        {
            _logger.LogWarning("⚠️ Attempting to read inputs before initialization");
            throw new InvalidOperationException("Device not initialized");
        }

        try
        {
            var rawValue = await ReadGpioPortAsync();
            
            var inputState = new InputState
            {
                PowerOnReady = (rawValue & 0x01) != 0,      // DD0 = bit 0
                PowerDownHeadsUp = (rawValue & 0x02) != 0,  // DD1 = bit 1
                RawInputValue = rawValue,
                Timestamp = DateTime.Now
            };

            _logger.LogDebug("📊 All inputs read: {InputState}", inputState);
            _successfulOperations++;
            _lastOperationTime = DateTime.Now;

            return inputState;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "💥 Error reading all GPIO inputs");
            _failedOperations++;
            throw;
        }
    }

    #endregion

    #region Core GPIO Output Methods (Client Requirements)

    /// <summary>
    /// 📤 Set Critical Fail Signal output (DD2 - Bit 2)
    /// CLIENT REQUIREMENT: Hardware notification for CRITICAL validation failures
    /// </summary>
    public async Task SetCriticalFailSignalAsync(bool state)
    {
        try
        {
            await SetGpioBitAsync(2, state); // DD2 = bit 2
            
            _logger.LogInformation("🚨🔌 Critical Fail Signal: {State} (DD2)", state ? "ACTIVE 🚨" : "CLEARED ✅");
            
            // Fire event for critical signal changes
            if (state)
            {
                OnHardwareStateChanged(new BitBangEventArgs
                {
                    EventType = BitBangEventType.CriticalFailTriggered,
                    NewState = true,
                    BitIndex = 2,
                    Message = "Critical failure signal activated"
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "💥 Error setting Critical Fail Signal to {State}", state);
            _failedOperations++;
            throw;
        }
    }

    /// <summary>
    /// 📤 Set Workflow Active Signal output (DD3 - Bit 3)
    /// BONUS: Indicate workflow execution status
    /// </summary>
    public async Task SetWorkflowActiveSignalAsync(bool state)
    {
        try
        {
            await SetGpioBitAsync(3, state); // DD3 = bit 3
            
            _logger.LogDebug("⚡ Workflow Active Signal: {State} (DD3)", state ? "ACTIVE ⚡" : "IDLE 💤");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "💥 Error setting Workflow Active Signal to {State}", state);
            _failedOperations++;
            throw;
        }
    }

    /// <summary>
    /// 📤 Set all output signals efficiently (DD2, DD3)
    /// PERFORMANCE: Single hardware operation for both outputs
    /// </summary>
    public async Task SetAllOutputsAsync(OutputState outputState)
    {
        try
        {
            // Read current state to preserve input bits
            var currentValue = await ReadGpioPortAsync();
            
            // Clear output bits (DD2, DD3) and set new values
            byte newValue = (byte)(currentValue & 0x03); // Keep DD0, DD1 (inputs)
            
            if (outputState.CriticalFailSignal)
                newValue |= 0x04; // Set DD2
                
            if (outputState.WorkflowActiveSignal)
                newValue |= 0x08; // Set DD3

            await WriteGpioPortAsync(newValue);
            
            _logger.LogDebug("📤 All outputs set: {OutputState}", outputState);
            _successfulOperations++;
            _lastOperationTime = DateTime.Now;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "💥 Error setting all GPIO outputs");
            _failedOperations++;
            throw;
        }
    }

    #endregion

    #region Low-Level GPIO Operations

    /// <summary>
    /// 🔧 Read raw GPIO port value (all 8 bits)
    /// </summary>
    private async Task<byte> ReadGpioPortAsync()
    {
        return await Task.Run(() =>
        {
            lock (_deviceLock)
            {
                if (_ftdiDevice == null)
                    throw new InvalidOperationException("FTDI device not opened");

                // For bit-bang mode, we need to read the current pin states
                uint bytesAvailable = 0;
                var status = _ftdiDevice.GetRxBytesAvailable(ref bytesAvailable);
                
                if (status == FTDI.FT_STATUS.FT_OK && bytesAvailable > 0)
                {
                    // Read available data
                    byte[] buffer = new byte[bytesAvailable];
                    uint bytesRead = 0;
                    status = _ftdiDevice.Read(buffer, bytesAvailable, ref bytesRead);
                    
                    if (status == FTDI.FT_STATUS.FT_OK && bytesRead > 0)
                    {
                        // Return the last byte read (most recent state)
                        return buffer[bytesRead - 1];
                    }
                }

                // If no data available, return last known state
                return _lastStatus.RawInputValue;
            }
        });
    }

    /// <summary>
    /// 🔧 Write raw GPIO port value
    /// </summary>
    private async Task WriteGpioPortAsync(byte value)
    {
        await Task.Run(() =>
        {
            lock (_deviceLock)
            {
                if (_ftdiDevice == null)
                    throw new InvalidOperationException("FTDI device not opened");

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

                _logger.LogTrace("🔧 GPIO write: 0x{Value:X2}", value);
            }
        });
    }

    /// <summary>
    /// 🔧 Set individual GPIO bit
    /// </summary>
    private async Task SetGpioBitAsync(int bitIndex, bool state)
    {
        if (bitIndex < 0 || bitIndex > 7)
            throw new ArgumentOutOfRangeException(nameof(bitIndex), "Bit index must be 0-7");

        // Read current state
        var currentValue = await ReadGpioPortAsync();
        
        // Modify the specific bit
        byte newValue;
        if (state)
            newValue = (byte)(currentValue | (1 << bitIndex));
        else
            newValue = (byte)(currentValue & ~(1 << bitIndex));

        // Write back
        await WriteGpioPortAsync(newValue);
    }

    #endregion

    #region Initialization & Configuration

    /// <summary>
    /// 🚀 Initialize GPIO hardware with client configuration
    /// </summary>
    public async Task InitializeAsync(BitBangConfiguration config)
    {
        if (config == null)
            throw new ArgumentNullException(nameof(config));

        if (!config.IsValid)
        {
            var errors = string.Join(", ", config.GetValidationErrors());
            throw new ArgumentException($"Invalid configuration: {errors}");
        }

        try
        {
            _logger.LogInformation("🚀 Initializing FTDI GPIO - Device: {Device}", 
                config.DeviceId ?? config.SerialNumber);

            _configuration = config;

            // Step 1: Open FTDI device
            await OpenFtdiDeviceAsync();

            // Step 2: Configure for bit-bang mode
            await ConfigureBitBangModeAsync();

            // Step 3: Set initial safe state
            await SetInitialStateAsync();

            _isInitialized = true;
            _connectionTime = DateTime.Now;

            _logger.LogInformation("✅ FTDI GPIO initialized successfully: {Device}", HardwareDeviceId);

            // Start monitoring if enabled
            if (config.EnableContinuousMonitoring)
            {
                await StartContinuousMonitoringAsync(config.PollingInterval);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "💥 Failed to initialize FTDI GPIO: {Device}", config.DeviceId ?? config.SerialNumber);
            await CleanupAsync();
            throw;
        }
    }

    /// <summary>
    /// 🔌 Open FTDI device by ID or serial number
    /// </summary>
    private async Task OpenFtdiDeviceAsync()
    {
        await Task.Run(() =>
        {
            _ftdiDevice = new FTDI();

            FTDI.FT_STATUS status;

            // Try opening by serial number first (most reliable)
            if (!string.IsNullOrEmpty(_configuration!.SerialNumber))
            {
                _logger.LogDebug("📡 Opening FTDI device by serial: {Serial}", _configuration.SerialNumber);
                status = _ftdiDevice.OpenBySerialNumber(_configuration.SerialNumber);
            }
            else if (!string.IsNullOrEmpty(_configuration.DeviceId))
            {
                _logger.LogDebug("📡 Opening FTDI device by description: {Description}", _configuration.DeviceId);
                status = _ftdiDevice.OpenByDescription(_configuration.DeviceId);
            }
            else
            {
                throw new ArgumentException("Either DeviceId or SerialNumber must be specified");
            }

            if (status != FTDI.FT_STATUS.FT_OK)
            {
                throw new FtdiException($"Cannot open FTDI device: {status}", 
                    _configuration.SerialNumber ?? _configuration.DeviceId, status.ToString(), "OpenDevice");
            }

            _logger.LogDebug("✅ FTDI device opened successfully");
        });
    }

    /// <summary>
    /// ⚙️ Configure device for bit-bang GPIO mode
    /// </summary>
    private async Task ConfigureBitBangModeAsync()
    {
        await Task.Run(() =>
        {
            lock (_deviceLock)
            {
                if (_ftdiDevice == null)
                    throw new InvalidOperationException("FTDI device not opened");

                // Configure bit-bang mode with direction mask
                // Direction: 1 = output, 0 = input
                // Default: DD0,DD1 = input (0), DD2,DD3 = output (1)
                var directionMask = _configuration!.DirectionMask; // 0x0C = 0000 1100

                var status = _ftdiDevice.SetBitMode(directionMask, FTDI.FT_BIT_MODES.FT_BIT_MODE_ASYNC_BITBANG);

                if (status != FTDI.FT_STATUS.FT_OK)
                {
                    throw new FtdiException($"Cannot configure bit-bang mode: {status}", 
                        HardwareDeviceId, status.ToString(), "SetBitMode");
                }

                _logger.LogInformation("⚙️ Bit-bang mode configured - Direction mask: 0x{Mask:X2}", directionMask);
            }
        });
    }

    /// <summary>
    /// 🛡️ Set initial safe state for outputs
    /// </summary>
    private async Task SetInitialStateAsync()
    {
        if (_configuration!.ResetOutputsOnInit)
        {
            _logger.LogDebug("🛡️ Setting initial safe state for outputs");
            
            var initialState = OutputState.Create(criticalFail: false, workflowActive: false);
            await SetAllOutputsAsync(initialState);
            
            _logger.LogInformation("✅ Initial safe state set - All outputs cleared");
        }
    }

    #endregion

    #region Status & Monitoring (À compléter dans la prochaine étape)

    public async Task<bool> IsAvailableAsync()
    {
        try
        {
            await Task.CompletedTask; // Remove async warning
            
            // Check if device is initialized and accessible
            lock (_deviceLock)
            {
                if (!_isInitialized || _ftdiDevice == null)
                {
                    _logger.LogDebug("🔍 Device availability: Not initialized");
                    return false;
                }

                // TODO: Could add additional connectivity checks here
                // For now, assume device is available if initialized
                _logger.LogDebug("🔍 Device availability: Available ✅");
                return true;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "⚠️ Error checking device availability");
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
                StatusMessage = _isInitialized ? "Device operational" : "Device not initialized"
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
                    
                    // For outputs, we'll track them from our last operations
                    // This is a simplification - in a full implementation we'd read actual state
                    status.CriticalFailSignal = false; // TODO: Track actual output state
                    status.WorkflowActiveSignal = false; // TODO: Track actual output state
                }
                catch (Exception ex)
                {
                    status.LastError = ex.Message;
                    status.StatusMessage = "Error reading GPIO state";
                }
            }

            _lastStatus = status;
            return status;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "💥 Error getting BitBang status");
            return BitBangStatus.CreateError(HardwareDeviceId ?? "Unknown", ex.Message);
        }
    }

    public async Task<BitBangHealthStatus> GetHealthStatusAsync()
    {
        await Task.CompletedTask; // Remove async warning
        
        var healthStatus = new BitBangHealthStatus
        {
            IsConnected = _isInitialized && _ftdiDevice != null,
            IsResponding = _isInitialized,
            FirmwareVersion = "Unknown",
            Uptime = _isInitialized ? DateTime.Now - _connectionTime : TimeSpan.Zero,
            SuccessfulOperations = _successfulOperations,
            FailedOperations = _failedOperations,
            LastError = null // TODO: Track last error
        };
        
        return healthStatus;
    }

    public async Task<BitBangPerformanceStats> GetPerformanceStatsAsync()
    {
        await Task.CompletedTask; // Remove async warning
        
        var totalOps = _successfulOperations + _failedOperations;
        var uptime = _isInitialized ? DateTime.Now - _connectionTime : TimeSpan.Zero;
        
        return new BitBangPerformanceStats
        {
            TotalOperations = totalOps,
            AverageOperationTimeMs = totalOps > 0 ? 1.0 : 0.0, // TODO: Track actual timing
            FastestOperationTimeMs = 0.5, // TODO: Track actual timing
            SlowestOperationTimeMs = 5.0, // TODO: Track actual timing
            OperationsPerSecond = uptime.TotalSeconds > 0 ? totalOps / uptime.TotalSeconds : 0,
            CollectionStartTime = _connectionTime
        };
    }

    #endregion

    #region Events (À compléter)

    public event EventHandler<BitBangEventArgs>? HardwareStateChanged;
    public event EventHandler<PowerReadyEventArgs>? PowerOnReadyChanged;
    public event EventHandler<PowerDownEventArgs>? PowerDownHeadsUpChanged;
    public event EventHandler<HardwareConnectionEventArgs>? ConnectionStatusChanged;

    protected virtual void OnHardwareStateChanged(BitBangEventArgs args)
    {
        HardwareStateChanged?.Invoke(this, args);
    }

    #endregion

    #region Cleanup & Disposal

    /// <summary>
    /// 🧹 Disconnect and cleanup resources
    /// </summary>
    public async Task DisconnectAsync()
    {
        try
        {
            _logger.LogInformation("🔒 Disconnecting FTDI GPIO device...");

            await StopContinuousMonitoringAsync();
            await ResetOutputsToDefaultAsync();
            await CleanupAsync();

            _logger.LogInformation("✅ FTDI GPIO device disconnected");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "💥 Error during FTDI GPIO disconnect");
            throw;
        }
    }

    /// <summary>
    /// 🛡️ Reset outputs to safe default state
    /// </summary>
    public async Task ResetOutputsToDefaultAsync()
    {
        if (_isInitialized)
        {
            _logger.LogDebug("🛡️ Resetting outputs to safe default state");
            var safeState = OutputState.Create(criticalFail: false, workflowActive: false);
            await SetAllOutputsAsync(safeState);
        }
    }

    /// <summary>
    /// 🧹 Internal cleanup
    /// </summary>
    private async Task CleanupAsync()
    {
        _isInitialized = false;

        if (_ftdiDevice != null)
        {
            try
            {
                lock (_deviceLock)
                {
                    _ftdiDevice.Close();
                    _ftdiDevice = null;
                }
                _logger.LogDebug("🔒 FTDI device closed");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "⚠️ Error closing FTDI device");
            }
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// 🗑️ Dispose pattern implementation
    /// </summary>
    public void Dispose()
    {
        try
        {
            DisconnectAsync().Wait(5000);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "💥 Error during disposal");
        }
    }

    #endregion

    #region Monitoring (Stubs pour l'instant)

    public async Task StartContinuousMonitoringAsync(TimeSpan pollingInterval, CancellationToken cancellationToken = default)
    {
        // TODO: Implémenter le monitoring continu
        _logger.LogDebug("📊 Continuous monitoring requested (to be implemented)");
        await Task.CompletedTask;
    }

    public async Task StopContinuousMonitoringAsync()
    {
        // TODO: Implémenter l'arrêt du monitoring
        _logger.LogDebug("🛑 Stopping continuous monitoring");
        await Task.CompletedTask;
    }

    public async Task<BitBangTestResult> RunHardwareTestAsync()
    {
        try
        {
            _logger.LogInformation("🧪 Running hardware test suite...");
            
            var testResult = new BitBangTestResult
            {
                Success = true,
                Duration = TimeSpan.Zero
            };

            var startTime = DateTime.Now;

            // Test 1: Device connectivity
            testResult.TestResults["DeviceConnectivity"] = _isInitialized && _ftdiDevice != null;
            testResult.Messages.Add($"Device connectivity: {(_isInitialized ? "PASS" : "FAIL")}");

            // Test 2: GPIO read operation
            try
            {
                var inputState = await ReadAllInputsAsync();
                testResult.TestResults["GpioRead"] = true;
                testResult.Messages.Add($"GPIO read: PASS (Raw: 0x{inputState.RawInputValue:X2})");
            }
            catch (Exception ex)
            {
                testResult.TestResults["GpioRead"] = false;
                testResult.Messages.Add($"GPIO read: FAIL ({ex.Message})");
                testResult.Success = false;
            }

            // Test 3: GPIO write operation (safe test)
            try
            {
                var originalState = await ReadAllInputsAsync();
                await SetWorkflowActiveSignalAsync(true);
                await Task.Delay(100);
                await SetWorkflowActiveSignalAsync(false);
                
                testResult.TestResults["GpioWrite"] = true;
                testResult.Messages.Add("GPIO write: PASS (Workflow signal toggle)");
            }
            catch (Exception ex)
            {
                testResult.TestResults["GpioWrite"] = false;
                testResult.Messages.Add($"GPIO write: FAIL ({ex.Message})");
                testResult.Success = false;
            }

            testResult.Duration = DateTime.Now - startTime;
            
            _logger.LogInformation("🧪 Hardware test complete: {Result} in {Duration:F1}s", 
                testResult.Success ? "PASS" : "FAIL", testResult.Duration.TotalSeconds);

            return testResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "💥 Hardware test failed");
            return new BitBangTestResult
            {
                Success = false,
                Messages = { $"Test framework error: {ex.Message}" },
                Duration = TimeSpan.Zero
            };
        }
    }

    public async Task ConfigureAutoClearAsync(BitBangAutoClearConfiguration autoClearConfig)
    {
        await Task.CompletedTask; // Remove async warning
        
        _logger.LogInformation("⚙️ Auto-clear configuration received: {Config}", autoClearConfig);
        
        // TODO: Store configuration and implement auto-clear logic
        // For now, just log the configuration
        if (autoClearConfig.AutoClearCriticalFail)
        {
            _logger.LogDebug("📋 Critical fail auto-clear enabled: {Time}s", 
                autoClearConfig.CriticalFailHoldTime.TotalSeconds);
        }
        
        if (autoClearConfig.AutoClearOnDisconnect)
        {
            _logger.LogDebug("📋 Auto-clear on disconnect enabled");
        }
    }

    #endregion
}