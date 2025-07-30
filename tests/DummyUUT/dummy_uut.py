#!/usr/bin/env python3
"""
Dummy UUT Simulator - Sprint 5 Demo
Simple RS232 device simulator for SerialPortPool testing
"""

import serial
import time
import threading
import logging
from datetime import datetime

class DummyUUT:
    """Simulates a UUT device responding to RS232 commands"""
    
    def __init__(self, port_name, baud_rate=115200):
        self.port_name = port_name
        self.baud_rate = baud_rate
        self.serial_port = None
        self.running = False
        self.device_state = "OFFLINE"
        
        # Configure logging
        logging.basicConfig(
            level=logging.INFO,
            format='%(asctime)s - %(levelname)s - %(message)s'
        )
        self.logger = logging.getLogger(__name__)
        
        # Command responses mapping
        self.command_responses = {
            "ATZ\r\n": "OK\r\n",
            "ATZ": "OK\r\n",
            "INIT_RS232\r\n": "READY\r\n", 
            "INIT_RS232": "READY\r\n",
            "AT+STATUS\r\n": "STATUS_OK\r\n",
            "AT+STATUS": "STATUS_OK\r\n",
            "RUN_TEST_1\r\n": "PASS\r\n",
            "RUN_TEST_1": "PASS\r\n",
            "TEST\r\n": "PASS\r\n",
            "TEST": "PASS\r\n",
            "AT+QUIT\r\n": "GOODBYE\r\n",
            "AT+QUIT": "GOODBYE\r\n",
            "STOP_RS232\r\n": "BYE\r\n",
            "STOP_RS232": "BYE\r\n",
            "EXIT\r\n": "BYE\r\n",
            "EXIT": "BYE\r\n",
            "AT+SHUTDOWN\r\n": "SHUTDOWN_OK\r\n",
            "AT+SHUTDOWN": "SHUTDOWN_OK\r\n"
        }
    
    def start(self):
        """Start the dummy UUT simulator"""
        try:
            self.serial_port = serial.Serial(
                port=self.port_name,
                baudrate=self.baud_rate,
                parity=serial.PARITY_NONE,
                stopbits=serial.STOPBITS_ONE,
                bytesize=serial.EIGHTBITS,
                timeout=1
            )
            
            self.running = True
            self.device_state = "ONLINE"
            
            self.logger.info(f"🔌 Dummy UUT started on {self.port_name} @ {self.baud_rate} baud")
            self.logger.info(f"📋 Device State: {self.device_state}")
            self.logger.info(f"🎯 Supported Commands: {list(self.command_responses.keys())}")
            
            # Start command processing thread
            self.command_thread = threading.Thread(target=self._process_commands)
            self.command_thread.daemon = True
            self.command_thread.start()
            
            return True
            
        except serial.SerialException as e:
            self.logger.error(f"❌ Failed to open {self.port_name}: {e}")
            return False
    
    def stop(self):
        """Stop the dummy UUT simulator"""
        self.running = False
        self.device_state = "OFFLINE"
        
        if self.serial_port and self.serial_port.is_open:
            self.serial_port.close()
            self.logger.info(f"🔌 Dummy UUT stopped on {self.port_name}")
    
    def _process_commands(self):
        """Main command processing loop"""
        while self.running:
            try:
                if self.serial_port.in_waiting > 0:
                    # Read incoming command
                    command = self.serial_port.read_until(b'\n').decode('utf-8')
                    command = command.strip()
                    
                    if command:
                        self.logger.info(f"📥 RX: '{command}'")
                        
                        # Find response
                        response = self._get_response(command)
                        
                        # Simulate processing delay
                        time.sleep(0.1)
                        
                        # Send response
                        if response:
                            self.serial_port.write(response.encode('utf-8'))
                            self.logger.info(f"📤 TX: '{response.strip()}'")
                        else:
                            error_response = "ERROR: Unknown command\r\n"
                            self.serial_port.write(error_response.encode('utf-8'))
                            self.logger.warning(f"📤 TX: '{error_response.strip()}'")
                
                time.sleep(0.01)  # Small delay to prevent CPU spinning
                
            except Exception as e:
                self.logger.error(f"❌ Command processing error: {e}")
                time.sleep(0.1)
    
    def _get_response(self, command):
        """Get appropriate response for command"""
        # Try exact match first
        if command in self.command_responses:
            return self.command_responses[command]
        
        # Try with \r\n appended
        command_with_crlf = command + "\r\n"
        if command_with_crlf in self.command_responses:
            return self.command_responses[command_with_crlf]
        
        # Try without \r\n
        command_clean = command.replace("\r", "").replace("\n", "")
        if command_clean in self.command_responses:
            return self.command_responses[command_clean]
        
        return None
    
    def add_command(self, command, response):
        """Add custom command/response pair"""
        self.command_responses[command] = response
        self.logger.info(f"➕ Added command: '{command}' → '{response.strip()}'")
    
    def get_status(self):
        """Get current device status"""
        return {
            "port": self.port_name,
            "baud_rate": self.baud_rate,
            "state": self.device_state,
            "running": self.running,
            "commands_supported": len(self.command_responses)
        }


def main():
    """Main entry point for standalone execution"""
    import argparse
    
    parser = argparse.ArgumentParser(description="Dummy UUT Simulator for SerialPortPool Demo")
    parser.add_argument("--port", default="COM5", help="Serial port to use (default: COM5)")
    parser.add_argument("--baud", type=int, default=115200, help="Baud rate (default: 115200)")
    parser.add_argument("--duration", type=int, default=0, help="Run duration in seconds (0 = forever)")
    
    args = parser.parse_args()
    
    # Create and start dummy UUT
    dummy = DummyUUT(args.port, args.baud)
    
    print("🏭 SerialPortPool Dummy UUT Simulator")
    print("=" * 50)
    print(f"📍 Port: {args.port}")
    print(f"⚡ Baud Rate: {args.baud}")
    print(f"⏱️ Duration: {'Forever' if args.duration == 0 else f'{args.duration}s'}")
    print()
    
    if dummy.start():
        print("✅ Dummy UUT started successfully!")
        print("🎯 Ready for SerialPortPool testing...")
        print()
        print("📋 Supported Commands:")
        for cmd, resp in dummy.command_responses.items():
            print(f"   '{cmd.strip()}' → '{resp.strip()}'")
        print()
        print("Press Ctrl+C to stop...")
        
        try:
            if args.duration > 0:
                time.sleep(args.duration)
            else:
                while True:
                    time.sleep(1)
        except KeyboardInterrupt:
            print("\n🛑 Stopping dummy UUT...")
            dummy.stop()
            print("✅ Dummy UUT stopped.")
    else:
        print("❌ Failed to start dummy UUT!")
        return 1
    
    return 0


if __name__ == "__main__":
    exit(main())


# Usage Examples:
# 
# 1. Basic usage (default COM5, 115200 baud):
#    python dummy_uut.py
#
# 2. Custom port and baud rate:
#    python dummy_uut.py --port COM8 --baud 9600
#
# 3. Run for specific duration:
#    python dummy_uut.py --port COM8 --duration 300  # 5 minutes
#
# 4. For demo with SerialPortPool:
#    python dummy_uut.py --port COM8
#    # Then configure XML to use COM8 in your BIB configuration