# Build-Installer-Simple.ps1 - Simplified MSI build script

param(
    [string]$Configuration = "Release",
    [string]$OutputDir = ".\bin\Installer",
    [switch]$Clean
)

$ErrorActionPreference = "Stop"

function Write-Status($Message) { Write-Host "BUILD $Message" -ForegroundColor Cyan }
function Write-Success($Message) { Write-Host "SUCCESS $Message" -ForegroundColor Green }
function Write-Error($Message) { Write-Host "ERROR $Message" -ForegroundColor Red }
function Write-Info($Message) { Write-Host "INFO $Message" -ForegroundColor Yellow }

try {
    Write-Host "SerialPortPool MSI Installer Build Script (Simple)" -ForegroundColor Cyan
    Write-Host "=================================================" -ForegroundColor Cyan
    
    # 1. Add WiX to PATH manually if needed
    $wixPaths = @(
        "${env:ProgramFiles(x86)}\WiX Toolset v3.14\bin",
        "${env:ProgramFiles}\WiX Toolset v3.14\bin",
        "${env:ProgramFiles(x86)}\WiX Toolset v3.11\bin",
        "${env:ProgramFiles}\WiX Toolset v3.11\bin"
    )
    
    $wixFound = $false
    foreach ($path in $wixPaths) {
        if (Test-Path (Join-Path $path "candle.exe")) {
            $env:PATH += ";$path"
            Write-Success "Added WiX to PATH: $path"
            $wixFound = $true
            break
        }
    }
    
    if (-not $wixFound) {
        # Check if already in PATH
        $existing = Get-Command "candle.exe" -ErrorAction SilentlyContinue
        if ($existing) {
            Write-Success "WiX already in PATH: $($existing.Source)"
        } else {
            Write-Error "WiX Toolset not found. Please install it first."
            exit 1
        }
    }
    
    # 2. Find project structure
    Write-Status "Finding project structure..."
    
    $currentDir = $PSScriptRoot
    $parentDir = Split-Path $currentDir -Parent
    
    # Look for project file
    $projectPath = $null
    $possiblePaths = @($parentDir, $currentDir, (Split-Path $parentDir -Parent))
    
    foreach ($path in $possiblePaths) {
        Write-Host "  Checking: $path" -ForegroundColor Gray
        $csproj = Get-ChildItem -Path $path -Filter "*.csproj" -ErrorAction SilentlyContinue | Select-Object -First 1
        $sln = Get-ChildItem -Path $path -Filter "*.sln" -ErrorAction SilentlyContinue | Select-Object -First 1
        
        if ($csproj -or $sln) {
            $projectPath = $path.ToString()  # Ensure it's a string
            Write-Success "Found project at: $projectPath"
            break
        }
    }
    
    if (-not $projectPath) {
        Write-Error "Cannot find .csproj or .sln file in expected locations"
        Write-Info "Searched: $($possiblePaths -join ', ')"
        exit 1
    }
    
    # 3. Clean if requested
    if ($Clean) {
        Write-Status "Cleaning previous builds..."
        if (Test-Path $OutputDir) { Remove-Item $OutputDir -Recurse -Force }
        if (Test-Path "obj") { Remove-Item "obj" -Recurse -Force }
        Write-Success "Clean completed"
    }
    
    # 4. Build the project
    Write-Status "Building project..."
    
    Push-Location $projectPath
    try {
        $buildFile = Get-ChildItem -Filter "*.sln" | Select-Object -First 1
        if (-not $buildFile) {
            $buildFile = Get-ChildItem -Filter "*.csproj" | Select-Object -First 1
        }
        
        & dotnet build $buildFile.Name --configuration $Configuration --verbosity minimal --nologo
        if ($LASTEXITCODE -ne 0) {
            Write-Error "Build failed with exit code: $LASTEXITCODE"
            exit 1
        }
        Write-Success "Build completed"
    }
    finally {
        Pop-Location
    }
    
    # 5. Find built executable
    Write-Status "Locating built executable..."
    
    # Ensure projectPath is a string and build possible paths
    $baseProjectPath = $projectPath.ToString()
    $binPaths = @(
        (Join-Path $baseProjectPath "bin\$Configuration\net9.0-windows"),
        (Join-Path $baseProjectPath "bin\$Configuration\net8.0-windows"), 
        (Join-Path $baseProjectPath "bin\$Configuration\net7.0-windows"),
        (Join-Path $baseProjectPath "bin\$Configuration\net6.0-windows")
    )
    
    $serviceBinPath = $null
    foreach ($binPath in $binPaths) {
        Write-Host "  Checking: $binPath" -ForegroundColor Gray
        if (Test-Path $binPath) {
            $exeFiles = Get-ChildItem -Path $binPath -Filter "*.exe" -ErrorAction SilentlyContinue
            if ($exeFiles) {
                $serviceBinPath = $binPath
                Write-Success "Found executable(s) in: $serviceBinPath"
                Write-Info "  Found .exe files: $($exeFiles.Name -join ', ')"
                break
            }
        }
    }
    
    if (-not $serviceBinPath) {
        Write-Error "Cannot find built executable in any expected location"
        Write-Info "Expected locations checked:"
        foreach ($path in $binPaths) {
            Write-Info "  - $path (Exists: $(Test-Path $path))"
        }
        exit 1
    }
    
    # 6. Create staging area
    Write-Status "Creating staging area..."
    
    $stagingDir = Join-Path $PSScriptRoot "staging"
    if (Test-Path $stagingDir) { Remove-Item $stagingDir -Recurse -Force }
    New-Item $stagingDir -ItemType Directory -Force | Out-Null
    
    # Copy all files from bin directory
    Copy-Item "$serviceBinPath\*" $stagingDir -Recurse -Force
    Write-Success "Files staged successfully"
    
    # 6.5. Copy PowerShell scripts for service installation
    Write-Status "Copying PowerShell installation scripts..."

    $installScriptSource = Join-Path $baseProjectPath "scripts\Install-Service.ps1"
    $uninstallScriptSource = Join-Path $baseProjectPath "Uninstall-Service.ps1"

    if (Test-Path $installScriptSource) {
        Copy-Item $installScriptSource $stagingDir -Force
        Write-Success "Install-Service.ps1 copied to staging"
    } else {
        Write-Error "Install-Service.ps1 not found at: $installScriptSource"
        exit 1
    }

    if (Test-Path $uninstallScriptSource) {
        Copy-Item $uninstallScriptSource $stagingDir -Force
        Write-Success "Uninstall-Service.ps1 copied to staging"
    } else {
        Write-Error "Uninstall-Service.ps1 not found at: $uninstallScriptSource"
        exit 1
    }

    # 7. Create minimal config and docs
    $configDir = Join-Path $stagingDir "Configuration"
    $docsDir = Join-Path $stagingDir "Documentation"
    New-Item $configDir -ItemType Directory -Force | Out-Null
    New-Item $docsDir -ItemType Directory -Force | Out-Null
    
    # Simple config file
    @{ "ServiceSettings" = @{ "LogLevel" = "Information" } } | ConvertTo-Json | Out-File (Join-Path $configDir "settings.json") -Encoding UTF8
    
    # Simple readme
    "SerialPortPool Service Installation Complete" | Out-File (Join-Path $docsDir "README.txt") -Encoding UTF8
    
    # 8. Build MSI
    Write-Status "Building MSI installer..."
    
    New-Item $OutputDir -ItemType Directory -Force | Out-Null
    
    $wixSource = Join-Path $PSScriptRoot "SerialPortPool-Setup.wxs"
    $wixObject = Join-Path $PSScriptRoot "obj\SerialPortPool-Setup.wixobj"
    $msiOutput = Join-Path $OutputDir "SerialPortPool-Setup.msi"
    
    New-Item (Split-Path $wixObject -Parent) -ItemType Directory -Force | Out-Null
    
    # Compile
    #& candle.exe $wixSource -out $wixObject -dSourceDir=$stagingDir -ext WixUtilExtension
    & candle.exe $wixSource -out $wixObject -dSourceDir="$stagingDir" -ext WixUtilExtension
    if ($LASTEXITCODE -ne 0) {
        Write-Error "WiX compilation failed"
        exit 1
    }
    
    # Link
    & light.exe $wixObject -out $msiOutput -ext WixUIExtension -ext WixUtilExtension -spdb
    if ($LASTEXITCODE -ne 0) {
        Write-Error "WiX linking failed"
        exit 1
    }
    
    Write-Success "MSI created successfully!"
    
    # 9. Cleanup and report
    Remove-Item $stagingDir -Recurse -Force -ErrorAction SilentlyContinue
    
    if (Test-Path $msiOutput) {
        $fileInfo = Get-Item $msiOutput
        Write-Success "MSI Package Details:"
        Write-Info "  File: $($fileInfo.Name)"
        Write-Info "  Size: $([math]::Round($fileInfo.Length / 1MB, 2)) MB"
        Write-Info "  Path: $($fileInfo.FullName)"
    }
    
    Write-Host ""
    Write-Success "Build completed successfully!"
    Write-Info "Installer: $msiOutput"
    
} catch {
    Write-Error "Build failed: $($_.Exception.Message)"
    Write-Host "Stack trace:" -ForegroundColor Red
    Write-Host $_.ScriptStackTrace -ForegroundColor Red
    exit 1
}