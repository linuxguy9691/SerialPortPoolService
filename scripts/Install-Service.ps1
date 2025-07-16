#Requires -RunAsAdministrator

param(
    [Parameter(Mandatory=$false)]
    [string]$ServicePath = ".\src\SerialPortPoolService\bin\Release\net9.0-windows\SerialPortPoolService.exe"
)

Write-Host "Installing SerialPortPoolService..." -ForegroundColor Green

try {
    if (-not (Test-Path $ServicePath)) {
        throw "Service executable not found at: $ServicePath"
    }

    $logDir = "C:\Logs\SerialPortPool"
    if (-not (Test-Path $logDir)) {
        New-Item -ItemType Directory -Path $logDir -Force
        Write-Host "Created log directory: $logDir" -ForegroundColor Yellow
    }

    # Vérifier si le service existe déjà
    if (Get-Service "SerialPortPoolService" -ErrorAction SilentlyContinue) {
        Write-Host "Service already exists. Stopping and removing..." -ForegroundColor Yellow
        Stop-Service "SerialPortPoolService" -Force
        Remove-Service "SerialPortPoolService"
    }

    $fullPath = (Resolve-Path $ServicePath).Path
    New-Service -Name "SerialPortPoolService" `
                -BinaryPathName $fullPath `
                -DisplayName "Serial Port Pool Service" `
                -Description "Manages a pool of serial port interfaces for centralized access" `
                -StartupType Automatic

    Write-Host "Service installed successfully!" -ForegroundColor Green
    Write-Host "Starting service..." -ForegroundColor Yellow
    
    Start-Service -Name "SerialPortPoolService"
    
    $status = Get-Service "SerialPortPoolService"
    Write-Host "Service status: $($status.Status)" -ForegroundColor Green
    
    Write-Host "`nService installation completed!" -ForegroundColor Green
    Write-Host "Check logs at: C:\Logs\SerialPortPool\" -ForegroundColor Cyan
}
catch {
    Write-Error "Failed to install service: $_"
    exit 1
}
