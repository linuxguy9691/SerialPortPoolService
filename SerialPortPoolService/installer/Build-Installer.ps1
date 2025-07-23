# SerialPortPoolService/installer/Build-Installer.ps1
# Professional MSI build script for SerialPortPool MVP

param(
    [Parameter(Mandatory=$false)]
    [string]$Configuration = "Release",
    
    [Parameter(Mandatory=$false)]
    [string]$Platform = "x64",
    
    [Parameter(Mandatory=$false)]
    [string]$OutputDir = ".\bin\Installer",
    
    [Parameter(Mandatory=$false)]
    [switch]$Clean,
    
    [Parameter(Mandatory=$false)]
    [switch]$Verbose
)

# Colors for output
$ErrorColor = "Red"
$SuccessColor = "Green"
$InfoColor = "Cyan"
$WarningColor = "Yellow"

function Write-Status {
    param([string]$Message, [string]$Color = "White")
    Write-Host "🔧 $Message" -ForegroundColor $Color
}

function Write-Success {
    param([string]$Message)
    Write-Host "✅ $Message" -ForegroundColor $SuccessColor
}

function Write-Error {
    param([string]$Message)
    Write-Host "❌ $Message" -ForegroundColor $ErrorColor
}

function Write-Info {
    param([string]$Message)
    Write-Host "ℹ️  $Message" -ForegroundColor $InfoColor
}

try {
    Write-Host "🚀 SerialPortPool MSI Installer Build Script" -ForegroundColor $InfoColor
    Write-Host "=" * 50 -ForegroundColor $InfoColor
    
    # Check prerequisites
    Write-Status "Checking prerequisites..."
    
    # Check if WiX is installed
    $wixPath = Get-Command "candle.exe" -ErrorAction SilentlyContinue
    if (-not $wixPath) {
        # Try common WiX installation paths
        $commonPaths = @(
            "${env:ProgramFiles(x86)}\WiX Toolset v3.11\bin\candle.exe",
            "${env:ProgramFiles}\WiX Toolset v3.11\bin\candle.exe",
            "${env:USERPROFILE}\.dotnet\tools\wix.exe"
        )
        
        $wixFound = $false
        foreach ($path in $commonPaths) {
            if (Test-Path $path) {
                $env:PATH += ";$(Split-Path $path -Parent)"
                $wixFound = $true
                Write-Success "Found WiX at: $path"
                break
            }
        }
        
        if (-not $wixFound) {
            Write-Error "WiX Toolset not found. Please install WiX Toolset v3.11 or later."
            Write-Info "Download from: https://wixtoolset.org/releases/"
            exit 1
        }
    } else {
        Write-Success "WiX Toolset found: $($wixPath.Source)"
    }
    
    # Validate project structure
    Write-Status "Validating project structure..."
    
    $projectRoot = Split-Path -Parent $PSScriptRoot
    $serviceProjectPath = Join-Path $projectRoot "SerialPortPoolService"
    $serviceBinPath = Join-Path $serviceProjectPath "bin\$Configuration\net9.0-windows"
    
    if (-not (Test-Path $serviceProjectPath)) {
        Write-Error "Service project not found at: $serviceProjectPath"
        exit 1
    }
    
    Write-Success "Project structure validated"
    
    # Clean previous builds if requested
    if ($Clean) {
        Write-Status "Cleaning previous builds..."
        if (Test-Path $OutputDir) {
            Remove-Item $OutputDir -Recurse -Force
        }
        if (Test-Path "obj") {
            Remove-Item "obj" -Recurse -Force
        }
        Write-Success "Clean completed"
    }
    
    # Build the main service first
    Write-Status "Building SerialPortPool Service..."
    
    Push-Location $projectRoot
    try {
        $buildArgs = @(
            "build"
            "SerialPortPoolService.sln"
            "--configuration", $Configuration
            "--verbosity", $(if ($Verbose) { "normal" } else { "minimal" })
            "--nologo"
        )
        
        & dotnet @buildArgs
        if ($LASTEXITCODE -ne 0) {
            Write-Error "Service build failed with exit code: $LASTEXITCODE"
            exit 1
        }
        
        Write-Success "Service build completed"
    }
    finally {
        Pop-Location
    }
    
    # Verify service executable exists
    $serviceExePath = Join-Path $serviceBinPath "SerialPortPoolService.exe"
    if (-not (Test-Path $serviceExePath)) {
        Write-Error "Service executable not found at: $serviceExePath"
        Write-Info "Expected after building with configuration: $Configuration"
        exit 1
    }
    
    Write-Success "Service executable verified: $serviceExePath"
    
    # Create staging directory
    Write-Status "Preparing installer staging area..."
    
    $stagingDir = Join-Path $PSScriptRoot "staging"
    if (Test-Path $stagingDir) {
        Remove-Item $stagingDir -Recurse -Force
    }
    New-Item $stagingDir -ItemType Directory -Force | Out-Null
    
    # Copy service files
    $serviceFiles = @(
        "SerialPortPoolService.exe",
        "SerialPortPool.Core.dll",
        "NLog.dll",
        "NLog.Extensions.Logging.dll",
        "NLog.config",
        "Microsoft.Extensions.*.dll",
        "System.IO.Ports.dll",
        "System.Management.dll"
    )
    
    foreach ($pattern in $serviceFiles) {
        $files = Get-ChildItem $serviceBinPath -Filter $pattern -ErrorAction SilentlyContinue
        foreach ($file in $files) {
            Copy-Item $file.FullName $stagingDir -Force
            Write-Verbose "Copied: $($file.Name)"
        }
    }
    
    # Create configuration directory
    $configStagingDir = Join-Path $stagingDir "Configuration"
    New-Item $configStagingDir -ItemType Directory -Force | Out-Null
    
    # Create default configuration files
    Write-Status "Creating default configuration files..."
    
    # Default BIB configuration
    $defaultBibConfig = @{
        "Configurations" = @{
            "BIB_001" = @{
                "BibId" = "BIB_001"
                "DutId" = "DUT_05"
                "Description" = "Standard Test Configuration for BIB 001"
                "PortSettings" = @{
                    "BaudRate" = 115200
                    "Parity" = "None"
                    "DataBits" = 8
                    "StopBits" = "One"
                    "ReadTimeout" = 2000
                    "WriteTimeout" = 2000
                }
                "PowerOnCommands" = @(
                    @{
                        "Command" = "ATZ`r`n"
                        "ExpectedResponse" = "OK"
                        "TimeoutMs" = 3000
                        "RetryCount" = 3
                        "Description" = "Reset device"
                    }
                )
                "TestCommands" = @(
                    @{
                        "Command" = "AT+STATUS`r`n"
                        "ExpectedResponse" = "STATUS_OK"
                        "TimeoutMs" = 2000
                        "RetryCount" = 3
                        "Description" = "Check device status"
                    }
                )
                "PowerOffCommands" = @(
                    @{
                        "Command" = "AT+SHUTDOWN`r`n"
                        "ExpectedResponse" = "SHUTDOWN_OK"
                        "TimeoutMs" = 5000
                        "RetryCount" = 1
                        "Description" = "Graceful shutdown"
                    }
                )
            }
        }
        "DefaultSettings" = @{
            "BaudRate" = 115200
            "Parity" = "None"
            "DataBits" = 8
            "StopBits" = "One"
            "ReadTimeout" = 2000
            "WriteTimeout" = 2000
        }
    }
    
    $defaultBibConfig | ConvertTo-Json -Depth 10 | Out-File (Join-Path $configStagingDir "bib-configurations.json") -Encoding UTF8
    
    # Default settings
    $defaultSettings = @{
        "ServiceSettings" = @{
            "DiscoveryInterval" = 30
            "LogLevel" = "Information"
            "EnableBackgroundDiscovery" = $true
        }
        "ClientSettings" = @{
            "DefaultTimeout" = 2000
            "MaxRetries" = 3
            "EnableValidation" = $true
        }
    }
    
    $defaultSettings | ConvertTo-Json -Depth 5 | Out-File (Join-Path $configStagingDir "default-settings.json") -Encoding UTF8
    
    # Create documentation directory
    $docsStagingDir = Join-Path $stagingDir "Documentation"
    New-Item $docsStagingDir -ItemType Directory -Force | Out-Null
    
    # Create basic documentation
    $readmeContent = @"
SerialPortPool MVP - Industrial Communication Solution
===================================================

Version: 1.0.0
Installation Date: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')

QUICK START:
-----------
1. The SerialPortPool service has been installed and started automatically
2. Configure your BIB devices in: C:\Program Files\SerialPortPool\Configuration\bib-configurations.json
3. Run the demo: Start Menu > SerialPortPool > SerialPortPool Demo
4. View logs: Start Menu > SerialPortPool > View Logs

CONFIGURATION:
-------------
- Service Configuration: [Install Dir]\Configuration\
- Log Files: C:\Logs\SerialPortPool\
- Service Name: SerialPortPoolService

SUPPORT:
--------
For technical support and documentation, see the Documentation folder.

UNINSTALL:
----------
Use Start Menu > SerialPortPool > Uninstall SerialPortPool
Or use Windows "Add or Remove Programs"
"@
    
    $readmeContent | Out-File (Join-Path $docsStagingDir "README-MVP.txt") -Encoding UTF8
    
    Write-Success "Configuration and documentation files created"
    
    # Build WiX installer
    Write-Status "Building WiX installer..."
    
    # Create output directory
    New-Item $OutputDir -ItemType Directory -Force | Out-Null
    
    # WiX Compiler (candle)
    $wixSourceFile = Join-Path $PSScriptRoot "SerialPortPool-Setup.wxs"
    $wixObjectFile = Join-Path $PSScriptRoot "obj\SerialPortPool-Setup.wixobj"
    
    New-Item (Split-Path $wixObjectFile -Parent) -ItemType Directory -Force | Out-Null
    
    $candleArgs = @(
        $wixSourceFile
        "-out", $wixObjectFile
        "-dSourceDir=$stagingDir"
        "-ext", "WixUtilExtension"
    )
    
    if ($Verbose) {
        $candleArgs += "-v"
    }
    
    Write-Info "Running candle.exe..."
    & candle.exe @candleArgs
    if ($LASTEXITCODE -ne 0) {
        Write-Error "WiX compilation failed with exit code: $LASTEXITCODE"
        exit 1
    }
    
    Write-Success "WiX compilation completed"
    
    # WiX Linker (light)
    $msiOutputFile = Join-Path $OutputDir "SerialPortPool-Setup.msi"
    
    $lightArgs = @(
        $wixObjectFile
        "-out", $msiOutputFile
        "-ext", "WixUIExtension"
        "-ext", "WixUtilExtension"
        "-spdb"  # Suppress PDB file creation
    )
    
    if ($Verbose) {
        $lightArgs += "-v"
    }
    
    Write-Info "Running light.exe..."
    & light.exe @lightArgs
    if ($LASTEXITCODE -ne 0) {
        Write-Error "WiX linking failed with exit code: $LASTEXITCODE"
        exit 1
    }
    
    Write-Success "MSI installer created successfully!"
    
    # Verify output
    if (Test-Path $msiOutputFile) {
        $fileInfo = Get-Item $msiOutputFile
        Write-Success "MSI Package Details:"
        Write-Info "  File: $($fileInfo.Name)"
        Write-Info "  Size: $([math]::Round($fileInfo.Length / 1MB, 2)) MB"
        Write-Info "  Path: $($fileInfo.FullName)"
        Write-Info "  Created: $($fileInfo.CreationTime)"
    }
    
    # Cleanup staging
    Write-Status "Cleaning up staging area..."
    Remove-Item $stagingDir -Recurse -Force -ErrorAction SilentlyContinue
    
    Write-Host ""
    Write-Success "🎉 MSI Installer build completed successfully!"
    Write-Info "📦 Installer: $msiOutputFile"
    Write-Info "🚀 Ready for client deployment!"
    
} catch {
    Write-Error "Build failed: $($_.Exception.Message)"
    Write-Host "Stack trace:" -ForegroundColor Yellow
    Write-Host $_.ScriptStackTrace -ForegroundColor Yellow
    exit 1
}