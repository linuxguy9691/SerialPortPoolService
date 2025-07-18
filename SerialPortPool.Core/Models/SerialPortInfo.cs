namespace SerialPortPool.Core.Models;

/// <summary>
/// Information about a serial port in the system
/// </summary>
public class SerialPortInfo
{
    /// <summary>
    /// The port name (e.g., "COM3", "COM10")
    /// </summary>
    public string PortName { get; set; } = string.Empty;
    
    /// <summary>
    /// Human-readable friendly name from device manager
    /// </summary>
    public string FriendlyName { get; set; } = string.Empty;
    
    /// <summary>
    /// Current status of the port
    /// </summary>
    public PortStatus Status { get; set; } = PortStatus.Unknown;
    
    /// <summary>
    /// Last time this port was seen/updated
    /// </summary>
    public DateTime LastSeen { get; set; } = DateTime.Now;
    
    /// <summary>
    /// Hardware device identifier
    /// </summary>
    public string DeviceId { get; set; } = string.Empty;
    
    /// <summary>
    /// Whether this port is suitable for the pool (set by validation)
    /// </summary>
    public bool IsValidForPool { get; set; } = false;
    
    // ===== FTDI-SPECIFIC PROPERTIES (Sprint 2) =====
    
    /// <summary>
    /// Whether this device is an FTDI device
    /// </summary>
    public bool IsFtdiDevice { get; set; } = false;
    
    /// <summary>
    /// Detailed FTDI device information (null if not FTDI)
    /// </summary>
    public FtdiDeviceInfo? FtdiInfo { get; set; }
    
    /// <summary>
    /// Validation result for pool eligibility
    /// </summary>
    public PortValidationResult? ValidationResult { get; set; }
    
    // ===== CONVENIENCE PROPERTIES =====
    
    /// <summary>
    /// Whether this is specifically a 4232H chip (client requirement)
    /// </summary>
    public bool IsFtdi4232H => FtdiInfo?.Is4232H ?? false;
    
    /// <summary>
    /// Whether this is a genuine FTDI device (VID_0403)
    /// </summary>
    public bool IsGenuineFtdi => FtdiInfo?.IsGenuineFtdi ?? false;
    
    /// <summary>
    /// FTDI chip type if available (e.g., "FT232R", "FT4232H")
    /// </summary>
    public string FtdiChipType => FtdiInfo?.ChipType ?? "N/A";
    
    /// <summary>
    /// Validation score (0-100) if validation was performed
    /// </summary>
    public int ValidationScore => ValidationResult?.ValidationScore ?? 0;
    
    /// <summary>
    /// Human-readable validation status
    /// </summary>
    public string ValidationStatus
    {
        get
        {
            if (ValidationResult == null) return "Not Validated";
            return ValidationResult.IsValid ? "✅ Valid for Pool" : "❌ Invalid for Pool";
        }
    }
    
    /// <summary>
    /// Detailed validation reason
    /// </summary>
    public string ValidationReason => ValidationResult?.Reason ?? "No validation performed";
    
    public override string ToString()
    {
        var ftdiInfo = IsFtdiDevice ? $" [FTDI: {FtdiChipType}]" : "";
        var validationInfo = ValidationResult != null ? $" ({ValidationStatus})" : "";
        return $"{PortName} - {FriendlyName} ({Status}){ftdiInfo}{validationInfo}";
    }
    
    public override bool Equals(object? obj)
    {
        return obj is SerialPortInfo other && PortName == other.PortName;
    }
    
    public override int GetHashCode()
    {
        return PortName.GetHashCode();
    }
    
    /// <summary>
    /// Create a detailed summary of this port for logging/display
    /// </summary>
    public string ToDetailedString()
    {
        var details = new List<string>
        {
            $"Port: {PortName}",
            $"Name: {FriendlyName}",
            $"Status: {Status}",
            $"Last Seen: {LastSeen:HH:mm:ss}"
        };
        
        if (!string.IsNullOrEmpty(DeviceId))
        {
            details.Add($"Device ID: {DeviceId}");
        }
        
        if (IsFtdiDevice && FtdiInfo != null)
        {
            details.Add($"FTDI Device: YES ({FtdiInfo.ChipType})");
            details.Add($"FTDI VID/PID: {FtdiInfo.VendorId}/{FtdiInfo.ProductId}");
            if (!string.IsNullOrEmpty(FtdiInfo.SerialNumber))
            {
                details.Add($"Serial Number: {FtdiInfo.SerialNumber}");
            }
        }
        else
        {
            details.Add("FTDI Device: NO");
        }
        
        details.Add($"Valid for Pool: {(IsValidForPool ? "YES" : "NO")}");
        
        if (ValidationResult != null)
        {
            details.Add($"Validation: {ValidationResult.Reason} (Score: {ValidationResult.ValidationScore}%)");
        }
        
        return string.Join("\n   ", details);
    }
}