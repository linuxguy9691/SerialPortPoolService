SerialPortPool Service - MVP Installation
=========================================

Thank you for installing SerialPortPool Service!

INSTALLATION COMPLETED
=====================
- Windows Service: SerialPortPoolService
- Status: Ready to start
- Configuration: C:\Program Files\SerialPortPool\Configuration\
- Logs: C:\Logs\SerialPortPool\

QUICK START
===========
1. Connect your FTDI device (FT232R, FT4232H recommended)
2. Start the service: Services.msc -> SerialPortPoolService -> Start
3. Check logs for device discovery confirmation
4. Configure your applications to use the pool

FEATURES
========
- Thread-safe serial port pool management
- FTDI device discovery and validation
- Multi-port device awareness
- Smart caching with TTL
- Background discovery service
- Enhanced logging with NLog

CONFIGURATION
=============
Service settings: default-settings.json
Validation rules: Client vs Development modes
Port discovery: Automatic FTDI device detection

SUPPORT
=======
Documentation: Check the Documentation folder
Logs: C:\Logs\SerialPortPool\service-YYYY-MM-DD.log
Service Management: Windows Services (services.msc)

NEXT STEPS
==========
For advanced configuration and usage examples,
refer to the complete documentation package.

Version: 1.0.0
Build: Sprint 3 Foundation + MVP Features
Platform: Windows 10/11, Windows Server 2016+