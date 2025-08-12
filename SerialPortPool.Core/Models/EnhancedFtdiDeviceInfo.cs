// ===================================================================
// FIXED: EnhancedFtdiDeviceInfo.cs - Complete Implementation
// File: SerialPortPool.Core/Models/EnhancedFtdiDeviceInfo.cs
// Purpose: Enhanced FTDI device info combining WMI + EEPROM data
// ===================================================================

namespace SerialPortPool.Core.Models;

/// <summary>
/// Enhanced FTDI device info combining WMI + EEPROM data
/// SPRINT 8 FEATURE: ADDITIVE approach - extends existing FtdiDeviceInfo
/// BACKWARD COMPATIBLE: All existing FtdiDeviceInfo properties preserved
/// ZERO TOUCH: Existing code continues to work unchanged
/// </summary>
public class EnhancedFtdiDeviceInfo : FtdiDeviceInfo
{
    // ✨ EEPROM enhancements (additive to existing WMI data)
    /// <summary>
    /// Raw EEPROM data read via FTD2XX_NET
    /// Contains ProductDescription and other EEPROM-exclusive fields
    /// </summary>
    public FtdiEepromData? EepromDataObject { get; set; }
    
    /// <summary>
    /// Whether valid EEPROM data was successfully read
    /// </summary>
    public bool HasEepromData { get; set; }
    
    // ✨ EEPROM-exclusive fields (not available via WMI)
    /// <summary>
    /// Maximum power consumption from EEPROM
    /// </summary>
    public int MaxPower { get; set; }
    
    /// <summary>
    /// Self-powered capability from EEPROM
    /// </summary>
    public bool SelfPowered { get; set; }
    
    /// <summary>
    /// Remote wakeup capability from EEPROM
    /// </summary>
    public bool RemoteWakeup { get; set; }
    
    // ✨ Enhanced capabilities and smart properties
    /// <summary>
    /// Device supports custom ProductDescription in EEPROM
    /// CRITICAL for dynamic BIB selection feature
    /// </summary>
    public bool SupportsCustomProductDescription => HasEepromData && 
        !string.IsNullOrEmpty(EepromDataObject?.ProductDescription) &&
        EepromDataObject.ProductDescription != ProductDescription;
    
    /// <summary>
    /// BEST ProductDescription: EEPROM first, fallback to WMI
    /// This is the field used for dynamic BIB mapping
    /// </summary>
    public string EffectiveProductDescription => 
        (HasEepromData && !string.IsNullOrEmpty(EepromDataObject?.ProductDescription)) 
            ? EepromDataObject.ProductDescription 
            : ProductDescription;
    
    /// <summary>
    /// BEST Manufacturer: EEPROM first, fallback to WMI
    /// </summary>
    public string EffectiveManufacturer => 
        (HasEepromData && !string.IsNullOrEmpty(EepromDataObject?.Manufacturer)) 
            ? EepromDataObject.Manufacturer 
            : Manufacturer;
    
    /// <summary>
    /// Enhanced device capabilities summary
    /// </summary>
    public string EnhancedCapabilities
    {
        get
        {
            var capabilities = new List<string>();
            
            if (Is4232H) capabilities.Add("4232H");
            if (IsMultiPortDevice) capabilities.Add($"{ExpectedPortCount}-Port");
            if (IsHighSpeedCapable) capabilities.Add("HighSpeed");
            if (HasEepromData) capabilities.Add("EEPROM");
            if (SupportsCustomProductDescription) capabilities.Add("CustomProduct");
            if (SelfPowered) capabilities.Add("SelfPowered");
            if (RemoteWakeup) capabilities.Add("RemoteWakeup");
            
            return string.Join(", ", capabilities);
        }
    }
    
    /// <summary>
    /// Data source summary (WMI, EEPROM, or both)
    /// </summary>
    public string DataSources
    {
        get
        {
            var sources = new List<string> { "WMI" };
            if (HasEepromData) sources.Add("EEPROM");
            return string.Join(" + ", sources);
        }
    }
    
    /// <summary>
    /// Create enhanced device info combining WMI + EEPROM data
    /// ZERO TOUCH: Preserves all existing WMI functionality
    /// </summary>
    /// <param name="wmiData">Existing WMI-based device info</param>
    /// <param name="eepromData">New EEPROM data (optional)</param>
    /// <returns>Enhanced device info with best of both sources</returns>
    public static EnhancedFtdiDeviceInfo CombineWithWmiData(FtdiDeviceInfo wmiData, FtdiEepromData? eepromData = null)
    {
        var enhanced = new EnhancedFtdiDeviceInfo();
        
        // ✅ PRESERVE: Copy all existing WMI properties (ZERO TOUCH)
        enhanced.VendorId = wmiData.VendorId;
        enhanced.ProductId = wmiData.ProductId;
        enhanced.ChipType = wmiData.ChipType;
        enhanced.SerialNumber = wmiData.SerialNumber;
        enhanced.RawDeviceId = wmiData.RawDeviceId;
        enhanced.Manufacturer = wmiData.Manufacturer;
        enhanced.ProductDescription = wmiData.ProductDescription;
        enhanced.EepromData = new Dictionary<string, string>(wmiData.EepromData);
        
        // ✨ ENHANCE: Add EEPROM data if available
        if (eepromData?.IsValid == true)
        {
            enhanced.EepromDataObject = eepromData;
            enhanced.HasEepromData = true;
            
            // Override with better EEPROM data where available
            if (!string.IsNullOrEmpty(eepromData.Manufacturer))
                enhanced.Manufacturer = eepromData.Manufacturer;
            
            if (!string.IsNullOrEmpty(eepromData.ProductDescription))
                enhanced.ProductDescription = eepromData.ProductDescription;
            
            // Set EEPROM-exclusive fields
            enhanced.MaxPower = eepromData.MaxPower;
            enhanced.SelfPowered = eepromData.SelfPowered;
            enhanced.RemoteWakeup = eepromData.RemoteWakeup;
            
            // Merge additional EEPROM fields into the existing EepromData dictionary
            foreach (var field in eepromData.AdditionalFields)
            {
                enhanced.EepromData[field.Key] = field.Value;
            }
            
            // Add key EEPROM fields to the dictionary for compatibility
            enhanced.EepromData["EepromProductDescription"] = eepromData.ProductDescription;
            enhanced.EepromData["EepromManufacturer"] = eepromData.Manufacturer;
            enhanced.EepromData["MaxPower"] = eepromData.MaxPower.ToString();
            enhanced.EepromData["SelfPowered"] = eepromData.SelfPowered.ToString();
        }
        
        return enhanced;
    }
    
    /// <summary>
    /// Create enhanced device info from EEPROM data only
    /// Used when WMI data is not available
    /// </summary>
    /// <param name="eepromData">EEPROM data</param>
    /// <returns>Enhanced device info based on EEPROM</returns>
    public static EnhancedFtdiDeviceInfo FromEepromData(FtdiEepromData eepromData)
    {
        var enhanced = new EnhancedFtdiDeviceInfo
        {
            SerialNumber = eepromData.SerialNumber,
            VendorId = eepromData.VendorId,
            ProductId = eepromData.ProductId,
            Manufacturer = eepromData.Manufacturer,
            ProductDescription = eepromData.ProductDescription,
            EepromDataObject = eepromData,
            HasEepromData = true,
            MaxPower = eepromData.MaxPower,
            SelfPowered = eepromData.SelfPowered,
            RemoteWakeup = eepromData.RemoteWakeup
        };
        
        // Determine chip type from Product ID
        enhanced.ChipType = eepromData.ProductId.ToUpper() switch
        {
            "6001" => "FT232R",
            "6011" => "FT4232H", 
            "6048" => "FT4232HL",
            "6014" => "FT232H",
            "6010" => "FT2232H",
            _ => $"Unknown FTDI Chip (PID: {eepromData.ProductId})"
        };
        
        return enhanced;
    }
    
    /// <summary>
    /// Get enhanced device description for logging/display
    /// </summary>
    public override string GetDetailedDescription()
    {
        var baseDesc = base.GetDetailedDescription();
        var dataSource = HasEepromData ? " (WMI + EEPROM)" : " (WMI only)";
        var productDesc = SupportsCustomProductDescription ? 
            $" [Custom: '{EffectiveProductDescription}']" : "";
        
        return baseDesc + dataSource + productDesc;
    }
    
    /// <summary>
    /// Get EEPROM-enhanced summary
    /// </summary>
    public string GetEnhancedSummary()
    {
        var status = Is4232H ? "✅ VALID (4232H/4232HL)" : "❌ INVALID (Not 4232H/HL)";
        var eepromInfo = HasEepromData ? " + EEPROM" : "";
        var customProduct = SupportsCustomProductDescription ? 
            $" ['{EffectiveProductDescription}']" : "";
        
        return $"FTDI {ChipType} (VID: {VendorId}, PID: {ProductId}){eepromInfo} - {status}{customProduct}";
    }
    
    public override string ToString()
    {
        return GetEnhancedSummary();
    }
}