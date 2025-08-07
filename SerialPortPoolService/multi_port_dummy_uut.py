#!/usr/bin/env python3
"""
Multi-Port Dummy UUT - Sprint 6 Real Test
Simulates multiple UUTs on different COM ports simultaneously

Usage:
  python multi_port_dummy_uut.py --main COM6 --ports COM11,COM12,COM13

Based on detected FTDI hardware:
  - COM6: FT232R (Main UUT)  
  - COM11,COM12,COM13: FT4232HL (Ports 1,2,3)
"""

import serial
import threading
import time
import argparse
import sys
from datetime import datetime


class MultiPortDummyUUT:
    def __init__(self, main_port, secondary_ports, baudrate=9600):
        self.main_port = main_port
        self.secondary_ports = secondary_ports
        self.baudrate = baudrate
        self.running = False
        self.threads = []
        self.serial_connections = {}
        
    def log(self, port, message):
        """Thread-safe logging with timestamp"""
        timestamp = datetime.now().strftime("%H:%M:%S.%f")[:-3]
        print(f"[{timestamp}] {port}: {message}")
        
    def create_main_uut_handler(self, port):
        """Handler for main UUT on FT232R (COM6)"""
        def handler():
            try:
                self.log(port, f"🔌 Opening main UUT on {port} @ {self.baudrate} baud")
                ser = serial.Serial(port, self.baudrate, timeout=1)
                self.serial_connections[port] = ser
                self.log(port, "✅ Main UUT ready - FT232R")
                
                while self.running:
                    try:
                        if ser.in_waiting > 0:
                            command = ser.readline().decode('utf-8').strip()
                            self.log(port, f"📥 RX: {command}")
                            
                            # Main UUT Command Responses
                            if command == "MAIN_POWER_ON":
                                response = "MAIN_POWER:ON\r\n"
                            elif command == "MAIN_INIT":
                                response = "MAIN_INIT:OK\r\n"
                            elif command == "MAIN_TEST_STATUS":
                                response = "MAIN_STATUS:READY\r\n"
                            elif command == "MAIN_SELF_TEST":
                                time.sleep(0.5)  # Simulate processing time
                                response = "MAIN_SELF_TEST:PASS\r\n"
                            elif command == "MAIN_SHUTDOWN":
                                response = "MAIN_SHUTDOWN:OK\r\n"
                            elif command.startswith("MAIN_"):
                                response = f"MAIN_UNKNOWN_CMD:{command}\r\n"
                            else:
                                response = f"MAIN_ERROR:INVALID_COMMAND\r\n"
                                
                            ser.write(response.encode('utf-8'))
                            self.log(port, f"📤 TX: {response.strip()}")
                            
                        time.sleep(0.01)  # Small delay to prevent busy waiting
                        
                    except serial.SerialTimeoutException:
                        continue
                    except Exception as e:
                        self.log(port, f"❌ Main UUT error: {e}")
                        break
                        
            except Exception as e:
                self.log(port, f"❌ Failed to open main UUT: {e}")
            finally:
                if port in self.serial_connections:
                    self.serial_connections[port].close()
                self.log(port, "🔒 Main UUT closed")
                
        return handler
        
    def create_port_handler(self, port, port_num):
        """Handler for secondary ports on FT4232HL (COM11,12,13)"""
        def handler():
            try:
                self.log(port, f"🔌 Opening PORT{port_num} on {port} @ {self.baudrate} baud")
                ser = serial.Serial(port, self.baudrate, timeout=1)
                self.serial_connections[port] = ser
                self.log(port, f"✅ PORT{port_num} ready - FT4232HL")
                
                while self.running:
                    try:
                        if ser.in_waiting > 0:
                            command = ser.readline().decode('utf-8').strip()
                            self.log(port, f"📥 RX: {command}")
                            
                            # Port-specific responses
                            if command == f"PORT{port_num}_ENABLE":
                                response = f"PORT{port_num}:ENABLED\r\n"
                            elif command == f"PORT{port_num}_TEST":
                                time.sleep(0.3)  # Simulate test time
                                response = f"PORT{port_num}:TEST_OK\r\n"
                            elif command == f"PORT{port_num}_DATA_CHECK":
                                response = f"PORT{port_num}:DATA_VALID\r\n"
                            elif command == f"PORT{port_num}_DISABLE":
                                response = f"PORT{port_num}:DISABLED\r\n"
                            elif command.startswith(f"PORT{port_num}_"):
                                response = f"PORT{port_num}_UNKNOWN_CMD:{command}\r\n"
                            else:
                                response = f"PORT{port_num}_ERROR:INVALID_COMMAND\r\n"
                                
                            ser.write(response.encode('utf-8'))
                            self.log(port, f"📤 TX: {response.strip()}")
                            
                        time.sleep(0.01)
                        
                    except serial.SerialTimeoutException:
                        continue
                    except Exception as e:
                        self.log(port, f"❌ PORT{port_num} error: {e}")
                        break
                        
            except Exception as e:
                self.log(port, f"❌ Failed to open PORT{port_num}: {e}")
            finally:
                if port in self.serial_connections:
                    self.serial_connections[port].close()
                self.log(port, f"🔒 PORT{port_num} closed")
                
        return handler
    
    def start(self):
        """Start all UUT simulators"""
        print("🤖 Multi-Port Dummy UUT - Sprint 6 Real Test")
        print("=" * 60)
        print(f"📡 Main UUT: {self.main_port} (FT232R)")
        print(f"📡 Secondary Ports: {', '.join(self.secondary_ports)} (FT4232HL)")
        print(f"📊 Baudrate: {self.baudrate}")
        print("=" * 60)
        print()
        
        self.running = True
        
        # Start main UUT thread
        main_thread = threading.Thread(
            target=self.create_main_uut_handler(self.main_port),
            name=f"MainUUT-{self.main_port}"
        )
        main_thread.daemon = True
        main_thread.start()
        self.threads.append(main_thread)
        
        # Start secondary port threads
        for i, port in enumerate(self.secondary_ports, 1):
            port_thread = threading.Thread(
                target=self.create_port_handler(port, i),
                name=f"Port{i}-{port}"
            )
            port_thread.daemon = True
            port_thread.start()
            self.threads.append(port_thread)
            
        print(f"🚀 All {len(self.threads)} UUT simulators started!")
        print("⚡ Ready for commands...")
        print("💡 Press Ctrl+C to stop all simulators")
        print()
        
        try:
            # Keep main thread alive
            while self.running:
                time.sleep(1)
                
        except KeyboardInterrupt:
            print("\n🛑 Shutdown signal received...")
            self.stop()
            
    def stop(self):
        """Stop all UUT simulators"""
        print("🔒 Stopping all UUT simulators...")
        self.running = False
        
        # Close all serial connections
        for port, ser in self.serial_connections.items():
            try:
                ser.close()
                print(f"🔒 Closed {port}")
            except:
                pass
                
        # Wait for threads to finish
        for thread in self.threads:
            thread.join(timeout=1)
            
        print("✅ All UUT simulators stopped successfully")


def main():
    parser = argparse.ArgumentParser(description='Multi-Port Dummy UUT for Sprint 6 Real Test')
    parser.add_argument('--main', default='COM6', 
                       help='Main UUT port (FT232R) - default: COM6')
    parser.add_argument('--ports', default='COM11,COM12,COM13', 
                       help='Secondary ports (FT4232HL) - default: COM11,COM12,COM13')
    parser.add_argument('--baudrate', type=int, default=9600, 
                       help='Baudrate for all ports - default: 9600')
    
    args = parser.parse_args()
    
    # Parse secondary ports
    secondary_ports = [port.strip() for port in args.ports.split(',')]
    
    # Validate port count
    if len(secondary_ports) != 3:
        print("❌ Error: Exactly 3 secondary ports required (for ports 1,2,3)")
        print("   Example: --ports COM11,COM12,COM13")
        sys.exit(1)
    
    # Create and start multi-port UUT
    uut = MultiPortDummyUUT(args.main, secondary_ports, args.baudrate)
    uut.start()


if __name__ == "__main__":
    main()