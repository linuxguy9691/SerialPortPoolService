// SerialPortPool.Core/Models/ProtocolConfiguration.cs - VERSION COMPLÈTE CORRIGÉE
using System;
using System.Collections.Generic;
using SerialPortPool.Core.Interfaces;

namespace SerialPortPool.Core.Models
{
    /// <summary>
    /// Configuration for protocol communication
    /// Enhanced for Sprint 5 with complete RS232 support
    /// </summary>
    public class ProtocolConfiguration
    {
        /// <summary>
        /// Unique configuration identifier
        /// </summary>
        public string ConfigurationId { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Port name for communication
        /// </summary>
        public string PortName { get; set; } = string.Empty;

        /// <summary>
        /// Protocol name (e.g., "rs232", "rs485", "usb")
        /// </summary>
        public string Protocol { get; set; } = string.Empty;

        /// <summary>
        /// Communication speed (baud rate for serial protocols)
        /// </summary>
        public int Speed { get; set; } = 115200;

        /// <summary>
        /// Data pattern (e.g., "n81" = no parity, 8 data bits, 1 stop bit)
        /// </summary>
        public string DataPattern { get; set; } = "n81";

        /// <summary>
        /// Protocol-specific settings
        /// </summary>
        public Dictionary<string, object> Settings { get; set; } = new();

        /// <summary>
        /// Configuration metadata
        /// </summary>
        public Dictionary<string, string> Metadata { get; set; } = new();

        /// <summary>
        /// BIB identifier (for tracking)
        /// </summary>
        public string BibId { get; set; } = string.Empty;

        /// <summary>
        /// UUT identifier (for tracking)
        /// </summary>
        public string UutId { get; set; } = string.Empty;

        /// <summary>
        /// Port number within UUT
        /// </summary>
        public int PortNumber { get; set; }

        /// <summary>
        /// Configuration creation timestamp
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        #region RS232 Specific Properties - FIXED FOR RS232ProtocolHandler

        /// <summary>
        /// Get baud rate for RS232 protocol
        /// </summary>
        public int BaudRate 
        { 
            get => Speed; 
            set => Speed = value; 
        }

        /// <summary>
        /// Data bits for RS232 (5, 6, 7, 8)
        /// </summary>
        public int DataBits { get; set; } = 8;

        /// <summary>
        /// Parity setting for RS232
        /// </summary>
        public string Parity { get; set; } = "None";

        /// <summary>
        /// Stop bits setting for RS232  
        /// </summary>
        public string StopBits { get; set; } = "One";

        /// <summary>
        /// Read timeout for RS232 protocol
        /// </summary>
        public int ReadTimeout { get; set; } = 2000;

        /// <summary>
        /// Write timeout for RS232 protocol
        /// </summary>
        public int WriteTimeout { get; set; } = 2000;

        /// <summary>
        /// General timeout as TimeSpan (AJOUTÉ pour compatibility)
        /// </summary>
        public TimeSpan Timeout 
        { 
            get => TimeSpan.FromMilliseconds(ReadTimeout); 
            set => ReadTimeout = (int)value.TotalMilliseconds; 
        }

        #endregion

        #region RS232 Helper Methods

        /// <summary>
        /// Get baud rate for RS232 protocol
        /// </summary>
        public int GetBaudRate() => Speed;

        /// <summary>
        /// Parse data pattern and get parity setting
        /// </summary>
        public System.IO.Ports.Parity GetParity()
        {
            // Use Parity property if set, otherwise parse DataPattern
            if (!string.IsNullOrEmpty(Parity) && Parity != "None")
            {
                return Parity.ToUpperInvariant() switch
                {
                    "NONE" => System.IO.Ports.Parity.None,
                    "EVEN" => System.IO.Ports.Parity.Even,
                    "ODD" => System.IO.Ports.Parity.Odd,
                    "MARK" => System.IO.Ports.Parity.Mark,
                    "SPACE" => System.IO.Ports.Parity.Space,
                    _ => System.IO.Ports.Parity.None
                };
            }

            // Fallback to DataPattern parsing (existing logic)
            var pattern = DataPattern.ToLowerInvariant();
            if (pattern.Length < 1) return System.IO.Ports.Parity.None;

            return pattern[0] switch
            {
                'n' => System.IO.Ports.Parity.None,
                'e' => System.IO.Ports.Parity.Even,
                'o' => System.IO.Ports.Parity.Odd,
                'm' => System.IO.Ports.Parity.Mark,
                's' => System.IO.Ports.Parity.Space,
                _ => System.IO.Ports.Parity.None
            };
        }

        /// <summary>
        /// Parse data pattern and get data bits count
        /// </summary>
        public int GetDataBits()
        {
            // Use DataBits property if set, otherwise parse DataPattern
            if (DataBits != 8) return DataBits;

            // Fallback to DataPattern parsing (existing logic)
            var pattern = DataPattern.ToLowerInvariant();
            if (pattern.Length < 2) return 8;

            return pattern[1] switch
            {
                '5' => 5,
                '6' => 6,
                '7' => 7,
                '8' => 8,
                _ => 8
            };
        }

        /// <summary>
        /// Parse data pattern and get stop bits setting
        /// </summary>
        public System.IO.Ports.StopBits GetStopBits()
        {
            // Use StopBits property if set, otherwise parse DataPattern
            if (!string.IsNullOrEmpty(StopBits) && StopBits != "One")
            {
                return StopBits.ToUpperInvariant() switch
                {
                    "ONE" => System.IO.Ports.StopBits.One,
                    "TWO" => System.IO.Ports.StopBits.Two,
                    "ONEPOINTFIVE" => System.IO.Ports.StopBits.OnePointFive,
                    _ => System.IO.Ports.StopBits.One
                };
            }

            // Fallback to DataPattern parsing (existing logic)
            var pattern = DataPattern.ToLowerInvariant();
            if (pattern.Length < 3) return System.IO.Ports.StopBits.One;

            return pattern[2] switch
            {
                '1' => System.IO.Ports.StopBits.One,
                '2' => System.IO.Ports.StopBits.Two,
                '5' => System.IO.Ports.StopBits.OnePointFive,
                _ => System.IO.Ports.StopBits.One
            };
        }

        /// <summary>
        /// Get read timeout for RS232 protocol
        /// </summary>
        public int GetReadTimeout()
        {
            // Use ReadTimeout property if set, otherwise check Settings
            if (ReadTimeout != 2000) return ReadTimeout;

            return Settings.TryGetValue("read_timeout", out var timeout) && timeout is int value
                ? value
                : 2000;
        }

        /// <summary>
        /// Get write timeout for RS232 protocol
        /// </summary>
        public int GetWriteTimeout()
        {
            // Use WriteTimeout property if set, otherwise check Settings
            if (WriteTimeout != 2000) return WriteTimeout;

            return Settings.TryGetValue("write_timeout", out var timeout) && timeout is int value
                ? value
                : 2000;
        }

        /// <summary>
        /// BIB/UUT specific logger for structured logging
        /// </summary>
        public IBibUutLogger? BibLogger { get; set; }
        #endregion

        /// <summary>
        /// Create a default RS232 configuration
        /// </summary>
        public static ProtocolConfiguration CreateDefault(string portName)
        {
            return new ProtocolConfiguration
            {
                PortName = portName,
                Protocol = "rs232",
                Speed = 115200,
                DataPattern = "n81",
                BaudRate = 115200,
                DataBits = 8,
                Parity = "None",
                StopBits = "One",
                ReadTimeout = 2000,
                WriteTimeout = 2000
            };
        }

        /// <summary>
        /// Validate this configuration
        /// </summary>
        public bool IsValid()
        {
            return !string.IsNullOrEmpty(PortName) &&
                   !string.IsNullOrEmpty(Protocol) &&
                   Speed > 0 &&
                   ReadTimeout > 0 &&
                   WriteTimeout > 0;
        }

        public override string ToString()
        {
            return $"{Protocol.ToUpper()}: {PortName} @ {Speed} ({DataPattern})";
        }
    }
}