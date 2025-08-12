// ===================================================================
// NEW SPRINT 8: FtdiException.cs - FTDI Exception Handling
// File: SerialPortPool.Core/Models/FtdiException.cs
// Purpose: Custom exception for FTDI EEPROM operations
// ===================================================================

namespace SerialPortPool.Core.Models;

/// <summary>
/// Custom exception for FTDI EEPROM operations
/// SPRINT 8 FEATURE: Provides specific error handling for FTD2XX_NET operations
/// </summary>
public class FtdiException : Exception
{
    /// <summary>
    /// FTDI serial number associated with this error (if available)
    /// </summary>
    public string? SerialNumber { get; }
    
    /// <summary>
    /// FTDI status code from FTD2XX_NET API (if available)
    /// </summary>
    public string? FtdiStatus { get; }
    
    /// <summary>
    /// Operation that was being performed when error occurred
    /// </summary>
    public string? Operation { get; }

    public FtdiException() : base()
    {
    }

    public FtdiException(string message) : base(message)
    {
    }

    public FtdiException(string message, Exception innerException) : base(message, innerException)
    {
    }

    public FtdiException(string message, string? serialNumber, string? ftdiStatus = null, string? operation = null) 
        : base(message)
    {
        SerialNumber = serialNumber;
        FtdiStatus = ftdiStatus;
        Operation = operation;
    }

    public FtdiException(string message, Exception innerException, string? serialNumber, string? ftdiStatus = null, string? operation = null) 
        : base(message, innerException)
    {
        SerialNumber = serialNumber;
        FtdiStatus = ftdiStatus;
        Operation = operation;
    }

    /// <summary>
    /// Get detailed error message including FTDI-specific information
    /// </summary>
    public string GetDetailedMessage()
    {
        var details = new List<string> { Message };
        
        if (!string.IsNullOrEmpty(SerialNumber))
            details.Add($"Serial: {SerialNumber}");
            
        if (!string.IsNullOrEmpty(FtdiStatus))
            details.Add($"Status: {FtdiStatus}");
            
        if (!string.IsNullOrEmpty(Operation))
            details.Add($"Operation: {Operation}");
        
        return string.Join(" | ", details);
    }

    public override string ToString()
    {
        return GetDetailedMessage();
    }
}