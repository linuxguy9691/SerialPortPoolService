@echo off
REM SerialPortPoolService/installer/build-installer.bat
REM Quick build script for SerialPortPool MSI installer

echo 🚀 SerialPortPool MSI Installer - Quick Build
echo =============================================

REM Check if PowerShell is available
powershell -Command "Get-Command PowerShell" >nul 2>&1
if errorlevel 1 (
    echo ❌ PowerShell not found. Please install PowerShell.
    pause
    exit /b 1
)

echo ℹ️  Starting PowerShell build script...
echo.

REM Run the PowerShell build script with parameters
powershell -ExecutionPolicy Bypass -File "%~dp0Build-Installer.ps1" %*

if errorlevel 1 (
    echo.
    echo ❌ Build failed! Check the output above for details.
    pause
    exit /b 1
)

echo.
echo ✅ Build completed successfully!
echo 📦 MSI installer is ready for deployment.
echo.
pause

REM ============================================================================
REM Additional license.rtf file content for WiX installer
REM Save this as SerialPortPoolService/installer/license.rtf
REM ============================================================================

REM license.rtf content (RTF format for WiX):
REM {\rtf1\ansi\deff0 {\fonttbl {\f0 Times New Roman;}}
REM \f0\fs20
REM SerialPortPool Service License Agreement\par
REM \par
REM Copyright (c) 2025 SerialPortPool Solutions\par
REM \par
REM Permission is hereby granted to use this software for industrial communication purposes.\par
REM \par
REM THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,\par
REM INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A\par
REM PARTICULAR PURPOSE AND NONINFRINGEMENT.\par
REM \par
REM By installing this software, you agree to these terms.\par
REM }

REM ============================================================================
REM Installation Instructions (README for the installer)
REM ============================================================================

REM PREREQUISITES:
REM 1. WiX Toolset v3.11 or later
REM    Download from: https://wixtoolset.org/releases/
REM 
REM 2. .NET 9.0 SDK (for building the service)
REM    Download from: https://dotnet.microsoft.com/download
REM 
REM 3. Windows 10/11 or Windows Server 2016+
REM 
REM BUILD INSTRUCTIONS:
REM 1. Open command prompt as Administrator
REM 2. Navigate to SerialPortPoolService/installer/
REM 3. Run: build-installer.bat
REM 4. Find the MSI in: bin\Installer\SerialPortPool-Setup.msi
REM 
REM DEPLOYMENT:
REM 1. Copy SerialPortPool-Setup.msi to target machine
REM 2. Run as Administrator: msiexec /i SerialPortPool-Setup.msi
REM 3. Or double-click the MSI and follow the wizard
REM 
REM UNINSTALL:
REM 1. Windows Settings > Apps > SerialPortPool Service > Uninstall
REM 2. Or: msiexec /x {ProductCode}
REM 3. Or: Start Menu > SerialPortPool > Uninstall SerialPortPool

echo.
echo 📋 Build Instructions:
echo    1. Install WiX Toolset v3.11+
echo    2. Run this script as Administrator
echo    3. MSI will be created in bin\Installer\
echo.
echo 🎯 Ready for Sprint 4 MVP deployment!

REM End of batch file
