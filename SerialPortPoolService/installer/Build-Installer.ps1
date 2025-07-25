# Build-Installer-SingleFile.ps1
param(
    [string]$Configuration = "Release"
)

Write-Host "Building SerialPortPool Single-File MSI..." -ForegroundColor Cyan

try {
    # 1. Clean previous builds
    Remove-Item "publish" -Recurse -ErrorAction SilentlyContinue
    Remove-Item "staging" -Recurse -ErrorAction SilentlyContinue
    
    # 2. Publish self-contained single-file
    Write-Host "Publishing self-contained application..." -ForegroundColor Yellow
    dotnet publish SerialPortPoolService.csproj  `
        --configuration $Configuration `
        --runtime win-x64 `
        --self-contained true `
        --output "publish\self-contained" `
        --verbosity minimal
    
    if ($LASTEXITCODE -ne 0) {
        throw "Publish failed"
    }
    
    # 3. Create staging with ONLY necessary files
    Write-Host "Creating staging area..." -ForegroundColor Yellow
    $stagingDir = "staging"
    New-Item $stagingDir -ItemType Directory -Force | Out-Null
    
    # Copy ONLY the main executable and config files
    Copy-Item "publish\self-contained\SerialPortPoolService.exe" $stagingDir
    Copy-Item "publish\self-contained\NLog.config" $stagingDir -ErrorAction SilentlyContinue
    Copy-Item "default-settings.json" $stagingDir -ErrorAction SilentlyContinue
    
    # Configuration files
    #if (Test-Path "Configuration\bib-configurations.json") {
    #    Copy-Item "Configuration\bib-configurations.json" $stagingDir
    #}
    
    Write-Host "Staging complete. Files:" -ForegroundColor Green
    Get-ChildItem $stagingDir | ForEach-Object { 
        Write-Host "  - $($_.Name) ($([math]::Round($_.Length / 1MB, 1)) MB)" -ForegroundColor Gray 
    }
    
    # 4. Build MSI with WiX
    Write-Host "Building MSI..." -ForegroundColor Yellow
    
    $wixSource = "installer\SerialPortPool-Setup.wxs"
    $wixObject = "installer\obj\SerialPortPool-Setup.wixobj"
    $msiOutput = "installer\bin\SerialPortPool-Setup.msi"
    
    New-Item (Split-Path $wixObject -Parent) -ItemType Directory -Force | Out-Null
    New-Item (Split-Path $msiOutput -Parent) -ItemType Directory -Force | Out-Null
    
    # Compile
    & candle.exe $wixSource -out $wixObject -dSourceDir=$stagingDir
    if ($LASTEXITCODE -ne 0) { throw "WiX compilation failed" }
    
    # Link
    & light.exe $wixObject -out $msiOutput -ext WixUIExtension
    if ($LASTEXITCODE -ne 0) { throw "WiX linking failed" }
    
    Write-Host "SUCCESS! MSI created successfully!" -ForegroundColor Green
    
    if (Test-Path $msiOutput) {
        $fileInfo = Get-Item $msiOutput
        Write-Host "MSI: $($fileInfo.Name) ($([math]::Round($fileInfo.Length / 1MB, 1)) MB)" -ForegroundColor Cyan
        Write-Host "Path: $($fileInfo.FullName)" -ForegroundColor Gray
    }
    
} catch {
    Write-Host "ERROR: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
} finally {
    # Cleanup
    Remove-Item "staging" -Recurse -ErrorAction SilentlyContinue
}