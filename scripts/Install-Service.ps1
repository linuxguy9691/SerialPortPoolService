#Requires -RunAsAdministrator

param(
    [Parameter(Mandatory=$false)]
    [string]$ServicePath = ".\bin\Release\net6.0\SerialPortPoolService.exe"
)

Write-Host "Installing SerialPortPoolService..." -ForegroundColor Green

try {
    if (-not (Test-Path $ServicePath)) {
        throw "Service executable not found at: $ServicePath"
    }

    $logDir = "C:\Logs\SerialPortPool"
    if (-not (Test-Path $logDir)) {
        New-Item -ItemType Directory -Path $logDir -Force
        Write-Host "Created log directory: $logDir"
    }

    $fullPath = (Resolve-Path $ServicePath).Path
    New-Service -Name "SerialPortPoolService" `
                -BinaryPathName $fullPath `
                -DisplayName "Serial Port Pool Service" `
                -Description "Manages a pool of serial port interfaces" `
                -StartupType Automatic

    Write-Host "Service installed successfully!" -ForegroundColor Green
    Start-Service -Name "SerialPortPoolService"
    Write-Host "Service started!" -ForegroundColor Green
}
catch {
    Write-Error "Failed to install service: $_"
    exit 1
}
