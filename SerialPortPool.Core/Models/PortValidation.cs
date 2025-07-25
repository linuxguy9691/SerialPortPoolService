namespace SerialPortPool.Core.Models;

/// <summary>
/// Result of port validation for pool eligibility
/// </summary>
public class PortValidationResult
{
    /// <summary>
    /// Whether the port is valid for inclusion in the pool
    /// </summary>
    public bool IsValid { get; set; }
    
    /// <summary>
    /// Human-readable reason for the validation result
    /// </summary>
    public string Reason { get; set; } = string.Empty;
    
    /// <summary>
    /// Detailed explanation of the validation decision
    /// </summary>
    public string Details { get; set; } = string.Empty;
    
    /// <summary>
    /// Validation criteria that failed (if any)
    /// </summary>
    public ValidationCriteria[] FailedCriteria { get; set; } = Array.Empty<ValidationCriteria>();
    
    /// <summary>
    /// Validation criteria that passed
    /// </summary>
    public ValidationCriteria[] PassedCriteria { get; set; } = Array.Empty<ValidationCriteria>();
    
    /// <summary>
    /// Timestamp when validation was performed
    /// </summary>
    public DateTime ValidatedAt { get; set; } = DateTime.Now;
    
    /// <summary>
    /// Overall validation score (0-100, where 100 = perfect match)
    /// </summary>
    public int ValidationScore { get; set; }
    
    /// <summary>
    /// Create a successful validation result
    /// </summary>
    public static PortValidationResult Success(string reason, ValidationCriteria[] passedCriteria)
    {
        return new PortValidationResult
        {
            IsValid = true,
            Reason = reason,
            Details = "All validation criteria met successfully",
            PassedCriteria = passedCriteria,
            FailedCriteria = Array.Empty<ValidationCriteria>(),
            ValidationScore = 100
        };
    }
    
    /// <summary>
    /// Create a failed validation result
    /// </summary>
    public static PortValidationResult Failure(string reason, ValidationCriteria[] failedCriteria, ValidationCriteria[]? passedCriteria = null)
    {
        return new PortValidationResult
        {
            IsValid = false,
            Reason = reason,
            Details = $"Failed criteria: {string.Join(", ", failedCriteria)}",
            FailedCriteria = failedCriteria,
            PassedCriteria = passedCriteria ?? Array.Empty<ValidationCriteria>(),
            ValidationScore = CalculateScore(failedCriteria, passedCriteria ?? Array.Empty<ValidationCriteria>())
        };
    }
    
    private static int CalculateScore(ValidationCriteria[] failed, ValidationCriteria[] passed)
    {
        var total = failed.Length + passed.Length;
        if (total == 0) return 0;
        return (int)Math.Round((double)passed.Length / total * 100);
    }
    
    public override string ToString()
    {
        var status = IsValid ? "✅ VALID" : "❌ INVALID";
        return $"{status}: {Reason} (Score: {ValidationScore}%)";
    }
}

/// <summary>
/// Specific validation criteria for serial port pool eligibility
/// </summary>
public enum ValidationCriteria
{
    /// <summary>
    /// Device is not an FTDI device
    /// </summary>
    NotFtdiDevice,
    
    /// <summary>
    /// FTDI device detected but wrong chip type
    /// </summary>
    WrongFtdiChip,
    
    /// <summary>
    /// FTDI device is not a 4232H chip (client requirement)
    /// </summary>
    Not4232HChip,
    
    /// <summary>
    /// Device has invalid or missing EEPROM data
    /// </summary>
    InvalidEepromData,
    
    /// <summary>
    /// Port is reserved for system use
    /// </summary>
    SystemReservedPort,
    
    /// <summary>
    /// Device is not accessible or in error state
    /// </summary>
    DeviceNotAccessible,
    
    /// <summary>
    /// Unknown or unrecognized device
    /// </summary>
    UnknownDevice,
    
    /// <summary>
    /// Device ID parsing failed
    /// </summary>
    DeviceIdParsingFailed,
    
    /// <summary>
    /// Device is FTDI and recognized
    /// </summary>
    FtdiDeviceDetected,
    
    /// <summary>
    /// Device is a genuine FTDI device (VID_0403)
    /// </summary>
    GenuineFtdiDevice,
    
    /// <summary>
    /// Device is the required 4232H chip
    /// </summary>
    Is4232HChip,
    
    /// <summary>
    /// Device has valid EEPROM data
    /// </summary>
    ValidEepromData,
    
    /// <summary>
    /// Port is accessible and functional
    /// </summary>
    PortAccessible,
    
    /// <summary>
    /// Device meets all client requirements
    /// </summary>
    MeetsClientRequirements
}

/// <summary>
/// Configuration for port validation rules
/// </summary>
public class PortValidationConfiguration
{
    /// <summary>
    /// Whether to require FTDI devices only
    /// </summary>
    public bool RequireFtdiDevice { get; set; } = true;
    
    /// <summary>
    /// Whether to require specifically 4232H chips
    /// </summary>
    public bool Require4232HChip { get; set; } = true;
    
    /// <summary>
    /// Expected manufacturer name in device descriptor
    /// </summary>
    public string ExpectedManufacturer { get; set; } = "FTDI";
    
    /// <summary>
    /// Required EEPROM keys that must be present
    /// </summary>
    public string[] RequiredEepromKeys { get; set; } = { "Manufacturer", "ProductID" };
    
    /// <summary>
    /// Port names to exclude from the pool (system ports)
    /// </summary>
    public string[] ExcludedPortNames { get; set; } = { "COM1", "COM2" };
    
    /// <summary>
    /// Minimum validation score required for pool inclusion (0-100)
    /// </summary>
    public int MinimumValidationScore { get; set; } = 90;
    
    /// <summary>
    /// Whether to perform strict validation (all criteria must pass)
    /// </summary>
    public bool StrictValidation { get; set; } = true;
    
    /// <summary>
    /// Allowed FTDI Product IDs (PIDs) - for client, only 6048 (4232H)
    /// </summary>
    public string[] AllowedFtdiProductIds { get; set; } = { "6048" }; // Only FT4232H
    
    /// <summary>
    /// Create default configuration for client requirements
    /// </summary>
    public static PortValidationConfiguration CreateClientDefault()
    {
        return new PortValidationConfiguration
        {
            RequireFtdiDevice = true,
            Require4232HChip = true,
            ExpectedManufacturer = "FTDI",
            AllowedFtdiProductIds = new[] { "6048" }, // Only FT4232H
            ExcludedPortNames = new[] { "COM1", "COM2" },
            MinimumValidationScore = 100, // Must be perfect match
            StrictValidation = true
        };
    }
    
    /// <summary>
    /// Create permissive configuration for development/testing
    /// </summary>
    public static PortValidationConfiguration CreateDevelopmentDefault()
    {
        return new PortValidationConfiguration
        {
            RequireFtdiDevice = true,
            Require4232HChip = false, // Allow other FTDI chips for testing
            ExpectedManufacturer = "FTDI",
            AllowedFtdiProductIds = new[] { "6001", "6048", "6014" }, // Multiple chips allowed
            ExcludedPortNames = new[] { "COM1" },
            MinimumValidationScore = 70, // More lenient
            StrictValidation = false
        };
    }
}