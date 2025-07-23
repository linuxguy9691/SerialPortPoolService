@echo off
REM SerialPortPoolService/installer/build-installer-clean.bat
REM Quick build script for SerialPortPool MSI installer

echo SerialPortPool MSI Installer - Quick Build
echo =============================================

REM Check if PowerShell is available
powershell -Command "Get-Command PowerShell" >nul 2>&1
if errorlevel 1 (
    echo ERROR: PowerShell not found. Please install PowerShell.
    pause
    exit /b 1
)

echo INFO: Starting PowerShell build script...
echo.

REM Run the PowerShell build script with parameters
powershell -ExecutionPolicy Bypass -File "%~dp0Build-Installer-Clean.ps1" %*

if errorlevel 1 (
    echo.
    echo ERROR: Build failed! Check the output above for details.
    pause
    exit /b 1
)

echo.
echo SUCCESS: Build completed successfully!
echo INFO: MSI installer is ready for deployment.
echo.
pause