#Requires -RunAsAdministrator

Write-Host "Uninstalling SerialPortPoolService..." -ForegroundColor Yellow

try {
    # Check if service exists
    $service = Get-Service "SerialPortPoolService" -ErrorAction SilentlyContinue
    
    if ($service) {
        Write-Host "Service found. Stopping and removing..." -ForegroundColor Yellow
        
        # Stop service if running
        if ($service.Status -eq 'Running') {
            Stop-Service "SerialPortPoolService" -Force -ErrorAction SilentlyContinue
            Write-Host "Service stopped." -ForegroundColor Green
        }
        
        # Remove service
        sc.exe delete "SerialPortPoolService"
        Write-Host "Service removed successfully!" -ForegroundColor Green
    } else {
        Write-Host "Service not found - nothing to uninstall." -ForegroundColor Yellow
    }
    
    Write-Host "Service uninstallation completed!" -ForegroundColor Green
}
catch {
    Write-Host "Warning during service uninstall: $_" -ForegroundColor Yellow
    # Don't fail the uninstall process for service removal issues
}