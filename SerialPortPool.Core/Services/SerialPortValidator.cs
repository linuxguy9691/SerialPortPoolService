using Microsoft.Extensions.Logging;
using SerialPortPool.Core.Interfaces;
using SerialPortPool.Core.Models;

namespace SerialPortPool.Core.Services;

/// <summary>
/// Service for validating serial ports against pool eligibility criteria
/// </summary>
public class SerialPortValidator : ISerialPortValidator
{
    private readonly IFtdiDeviceReader _ftdiReader;
    private readonly ILogger<SerialPortValidator> _logger;
    private PortValidationConfiguration _configuration;

    public SerialPortValidator(
        IFtdiDeviceReader ftdiReader,
        ILogger<SerialPortValidator> logger,
        PortValidationConfiguration? configuration = null)
    {
        _ftdiReader = ftdiReader ?? throw new ArgumentNullException(nameof(ftdiReader));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration ?? PortValidationConfiguration.CreateClientDefault();
    }

    /// <summary>
    /// Validate a single port for pool eligibility
    /// </summary>
    public async Task<PortValidationResult> ValidatePortAsync(SerialPortInfo portInfo, PortValidationConfiguration? configuration = null)
    {
        var config = configuration ?? _configuration;
        var passedCriteria = new List<ValidationCriteria>();
        var failedCriteria = new List<ValidationCriteria>();

        try
        {
            _logger.LogDebug($"Validating port {portInfo.PortName} against criteria");

            // Check if port is excluded
            if (config.ExcludedPortNames.Contains(portInfo.PortName))
            {
                failedCriteria.Add(ValidationCriteria.SystemReservedPort);
                return PortValidationResult.Failure(
                    $"Port {portInfo.PortName} is excluded from pool",
                    failedCriteria.ToArray());
            }

            // Check port accessibility
            if (portInfo.Status == PortStatus.Error)
            {
                failedCriteria.Add(ValidationCriteria.DeviceNotAccessible);
            }
            else
            {
                passedCriteria.Add(ValidationCriteria.PortAccessible);
            }

            // Get or read FTDI information
            FtdiDeviceInfo? ftdiInfo = portInfo.FtdiInfo;
            if (ftdiInfo == null && !string.IsNullOrEmpty(portInfo.DeviceId))
            {
                ftdiInfo = await _ftdiReader.ReadDeviceInfoFromIdAsync(portInfo.DeviceId);
                if (ftdiInfo != null)
                {
                    portInfo.FtdiInfo = ftdiInfo;
                    portInfo.IsFtdiDevice = true;
                }
            }

            // Validate FTDI requirements
            if (config.RequireFtdiDevice)
            {
                if (ftdiInfo == null || !ftdiInfo.IsGenuineFtdi)
                {
                    failedCriteria.Add(ValidationCriteria.NotFtdiDevice);
                }
                else
                {
                    passedCriteria.Add(ValidationCriteria.FtdiDeviceDetected);
                    passedCriteria.Add(ValidationCriteria.GenuineFtdiDevice);

                    // Validate specific chip requirements
                    if (config.Require4232HChip)
                    {
                        if (!ftdiInfo.Is4232H)
                        {
                            failedCriteria.Add(ValidationCriteria.Not4232HChip);
                        }
                        else
                        {
                            passedCriteria.Add(ValidationCriteria.Is4232HChip);
                        }
                    }

                    // Validate allowed product IDs
                    if (config.AllowedFtdiProductIds.Length > 0)
                    {
                        if (!config.AllowedFtdiProductIds.Contains(ftdiInfo.ProductId))
                        {
                            failedCriteria.Add(ValidationCriteria.WrongFtdiChip);
                        }
                        else
                        {
                            passedCriteria.Add(ValidationCriteria.MeetsClientRequirements);
                        }
                    }

                    // Validate manufacturer
                    if (!string.IsNullOrEmpty(config.ExpectedManufacturer))
                    {
                        if (string.IsNullOrEmpty(ftdiInfo.Manufacturer) || 
                            !ftdiInfo.Manufacturer.Contains(config.ExpectedManufacturer, StringComparison.OrdinalIgnoreCase))
                        {
                            failedCriteria.Add(ValidationCriteria.InvalidEepromData);
                        }
                        else
                        {
                            passedCriteria.Add(ValidationCriteria.ValidEepromData);
                        }
                    }
                }
            }

            // Determine overall result
            var isValid = !failedCriteria.Any() || (!config.StrictValidation && passedCriteria.Count > failedCriteria.Count);
            var score = CalculateValidationScore(passedCriteria, failedCriteria);

            if (!isValid || score < config.MinimumValidationScore)
            {
                var reason = BuildFailureReason(portInfo, ftdiInfo, failedCriteria, config);
                var result = PortValidationResult.Failure(reason, failedCriteria.ToArray(), passedCriteria.ToArray());
                
                _logger.LogInformation($"Port {portInfo.PortName} validation failed: {reason} (Score: {score}%)");
                return result;
            }
            else
            {
                var reason = BuildSuccessReason(portInfo, ftdiInfo);
                var result = PortValidationResult.Success(reason, passedCriteria.ToArray());
                result.ValidationScore = score;
                
                _logger.LogInformation($"Port {portInfo.PortName} validation passed: {reason} (Score: {score}%)");
                return result;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error validating port {portInfo.PortName}");
            return PortValidationResult.Failure(
                $"Validation error: {ex.Message}",
                new[] { ValidationCriteria.UnknownDevice });
        }
    }

    /// <summary>
    /// Quick validation check - returns only boolean result
    /// </summary>
    public async Task<bool> IsValidPortAsync(SerialPortInfo portInfo, PortValidationConfiguration? configuration = null)
    {
        var result = await ValidatePortAsync(portInfo, configuration);
        return result.IsValid;
    }

    /// <summary>
    /// Validate multiple ports and return only the valid ones
    /// </summary>
    public async Task<IEnumerable<SerialPortInfo>> GetValidPortsAsync(IEnumerable<SerialPortInfo> ports, PortValidationConfiguration? configuration = null)
    {
        var validPorts = new List<SerialPortInfo>();
        var config = configuration ?? _configuration;

        foreach (var port in ports)
        {
            var result = await ValidatePortAsync(port, config);
            if (result.IsValid)
            {
                port.ValidationResult = result;
                port.IsValidForPool = true;
                validPorts.Add(port);
            }
        }

        _logger.LogInformation($"Validated {ports.Count()} ports, {validPorts.Count} are valid for pool");
        return validPorts;
    }

    /// <summary>
    /// Validate multiple ports and return detailed results for each
    /// </summary>
    public async Task<IDictionary<SerialPortInfo, PortValidationResult>> ValidatePortsAsync(IEnumerable<SerialPortInfo> ports, PortValidationConfiguration? configuration = null)
    {
        var results = new Dictionary<SerialPortInfo, PortValidationResult>();
        var config = configuration ?? _configuration;

        foreach (var port in ports)
        {
            var result = await ValidatePortAsync(port, config);
            results[port] = result;
            
            // Update port with validation result
            port.ValidationResult = result;
            port.IsValidForPool = result.IsValid;
        }

        return results;
    }

    /// <summary>
    /// Get the current validation configuration
    /// </summary>
    public PortValidationConfiguration GetConfiguration()
    {
        return _configuration;
    }

    /// <summary>
    /// Update the validation configuration
    /// </summary>
    public void SetConfiguration(PortValidationConfiguration configuration)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger.LogInformation($"Validation configuration updated: RequireFtdi={configuration.RequireFtdiDevice}, Require4232H={configuration.Require4232HChip}");
    }

    /// <summary>
    /// Calculate validation score based on passed/failed criteria
    /// </summary>
    private static int CalculateValidationScore(List<ValidationCriteria> passed, List<ValidationCriteria> failed)
    {
        var total = passed.Count + failed.Count;
        if (total == 0) return 0;
        
        return (int)Math.Round((double)passed.Count / total * 100);
    }

    /// <summary>
    /// Build failure reason message
    /// </summary>
    private static string BuildFailureReason(SerialPortInfo portInfo, FtdiDeviceInfo? ftdiInfo, List<ValidationCriteria> failedCriteria, PortValidationConfiguration config)
    {
        if (ftdiInfo == null)
        {
            return config.RequireFtdiDevice ? "Not an FTDI device (FTDI required)" : "Device not recognized";
        }

        if (failedCriteria.Contains(ValidationCriteria.Not4232HChip))
        {
            return $"FTDI device detected but requires 4232H chip (found {ftdiInfo.ChipType})";
        }

        if (failedCriteria.Contains(ValidationCriteria.WrongFtdiChip))
        {
            return $"FTDI chip {ftdiInfo.ChipType} not in allowed list (PID: {ftdiInfo.ProductId})";
        }

        if (failedCriteria.Contains(ValidationCriteria.SystemReservedPort))
        {
            return $"Port {portInfo.PortName} is excluded from pool";
        }

        if (failedCriteria.Contains(ValidationCriteria.DeviceNotAccessible))
        {
            return $"Port {portInfo.PortName} is not accessible (Status: {portInfo.Status})";
        }

        return $"Failed {failedCriteria.Count} validation criteria";
    }

    /// <summary>
    /// Build success reason message
    /// </summary>
    private static string BuildSuccessReason(SerialPortInfo portInfo, FtdiDeviceInfo? ftdiInfo)
    {
        if (ftdiInfo?.Is4232H == true)
        {
            return $"Valid FTDI 4232H device (PID: {ftdiInfo.ProductId})";
        }

        if (ftdiInfo != null)
        {
            return $"Valid FTDI device: {ftdiInfo.ChipType} (PID: {ftdiInfo.ProductId})";
        }

        return $"Port {portInfo.PortName} meets validation criteria";
    }
}