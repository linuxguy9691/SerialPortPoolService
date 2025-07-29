# Execute-POC.ps1 - Sprint 5 POC Validation Script
# Quick validation before full 4-hour critical test suite

param(
    [switch]$FullValidation = $false,
    [switch]$QuickTest = $false
)

Write-Host ""
Write-Host "🚀 =================================================" -ForegroundColor Green
Write-Host "🚀 SerialPortPool Sprint 5 POC Validation" -ForegroundColor Green
Write-Host "🚀 Strategy: ZERO TOUCH Extension Layer" -ForegroundColor Green
Write-Host "🚀 =================================================" -ForegroundColor Green
Write-Host ""

# Validate environment
Write-Host "🔍 Validating environment..." -ForegroundColor Yellow

if (!(Test-Path "SerialPortPoolService.sln")) {
    Write-Host "❌ ERROR: SerialPortPoolService.sln not found" -ForegroundColor Red
    Write-Host "   Please run this script from the repository root" -ForegroundColor Red
    exit 1
}

if (!(Get-Command "dotnet" -ErrorAction SilentlyContinue)) {
    Write-Host "❌ ERROR: .NET CLI not found" -ForegroundColor Red
    Write-Host "   Please install .NET 9.0 SDK" -ForegroundColor Red
    exit 1
}

Write-Host "✅ Environment validation passed" -ForegroundColor Green

# Build solution
Write-Host ""
Write-Host "🔧 Building solution..." -ForegroundColor Yellow

$buildResult = dotnet build SerialPortPoolService.sln --configuration Release --verbosity quiet
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ ERROR: Solution build failed" -ForegroundColor Red
    Write-Host "   Check build errors and resolve before POC validation" -ForegroundColor Red
    exit 1
}

Write-Host "✅ Solution built successfully" -ForegroundColor Green

if ($QuickTest) {
    # Quick interactive test
    Write-Host ""
    Write-Host "🎯 Running Quick POC Test (Interactive Mode)..." -ForegroundColor Yellow
    Write-Host "   This will test basic DI integration and service resolution" -ForegroundColor Gray
    Write-Host ""
    
    # Run service in interactive mode
    dotnet run --project SerialPortPoolService --configuration Release -- --console
    
    Write-Host ""
    Write-Host "✅ Quick POC Test completed" -ForegroundColor Green
    Write-Host "   Review output above for any errors or issues" -ForegroundColor Gray
    
} elseif ($FullValidation) {
    # Full 4-hour critical validation
    Write-Host ""
    Write-Host "🧪 Running Full POC Validation (4-hour critical tests)..." -ForegroundColor Yellow
    Write-Host "   This will execute all GO/NO-GO critical tests" -ForegroundColor Gray
    Write-Host ""
    
    # Run critical POC tests
    Write-Host "▶️ Executing critical POC tests..." -ForegroundColor Cyan
    $testResult = dotnet test tests/SerialPortPool.Core.Tests/ --filter "CriticalPOCTests" --configuration Release --verbosity normal
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host ""
        Write-Host "✅ =================================================" -ForegroundColor Green
        Write-Host "✅ POC VALIDATION: GO DECISION" -ForegroundColor Green
        Write-Host "✅ =================================================" -ForegroundColor Green
        Write-Host "🎉 All critical tests passed!" -ForegroundColor Green
        Write-Host "🚀 Continue with Sprint 5 implementation" -ForegroundColor Green
        Write-Host ""
        Write-Host "📋 Next Steps:" -ForegroundColor White
        Write-Host "   ✅ Day 2: Begin RS232 protocol implementation" -ForegroundColor Gray
        Write-Host "   ✅ Week 1: Complete XML configuration system" -ForegroundColor Gray
        Write-Host "   ✅ Week 2: Build 3-phase workflow orchestrator" -ForegroundColor Gray
        Write-Host ""
    } else {
        Write-Host ""
        Write-Host "❌ =================================================" -ForegroundColor Red
        Write-Host "❌ POC VALIDATION: NO-GO DECISION" -ForegroundColor Red
        Write-Host "❌ =================================================" -ForegroundColor Red
        Write-Host "💥 Critical tests failed!" -ForegroundColor Red
        Write-Host "🔄 Execute pivot strategy" -ForegroundColor Red
        Write-Host ""
        Write-Host "📋 Pivot Options:" -ForegroundColor White
        Write-Host "   🔄 Option 1: Modify approach (different composition pattern)" -ForegroundColor Gray
        Write-Host "   🔄 Option 2: Direct integration (controlled modifications)" -ForegroundColor Gray
        Write-Host "   🔄 Option 3: Separate service approach" -ForegroundColor Gray
        Write-Host ""
        exit 1
    }
    
} else {
    # Default: Show usage
    Write-Host "📋 POC Validation Options:" -ForegroundColor White
    Write-Host ""
    Write-Host "🎯 Quick Test (Recommend first):" -ForegroundColor Yellow
    Write-Host "   .\Execute-POC.ps1 -QuickTest" -ForegroundColor Gray
    Write-Host "   - Tests basic DI integration" -ForegroundColor Gray
    Write-Host "   - Validates service resolution" -ForegroundColor Gray
    Write-Host "   - Duration: ~5 minutes" -ForegroundColor Gray
    Write-Host ""
    Write-Host "🧪 Full Validation (After quick test passes):" -ForegroundColor Yellow
    Write-Host "   .\Execute-POC.ps1 -FullValidation" -ForegroundColor Gray
    Write-Host "   - Executes all 4 critical GO/NO-GO tests" -ForegroundColor Gray
    Write-Host "   - Maximum duration: 4 hours" -ForegroundColor Gray
    Write-Host "   - Makes final GO/NO-GO decision" -ForegroundColor Gray
    Write-Host ""
    Write-Host "📋 Recommended approach:" -ForegroundColor White
    Write-Host "   1. Run quick test first: .\Execute-POC.ps1 -QuickTest" -ForegroundColor Gray
    Write-Host "   2. If successful, run full validation: .\Execute-POC.ps1 -FullValidation" -ForegroundColor Gray
    Write-Host "   3. Based on results, continue Sprint 5 or pivot" -ForegroundColor Gray
    Write-Host ""
}

Write-Host "🚀 POC validation script completed" -ForegroundColor Green
Write-Host ""

# Additional info
Write-Host "📋 POC Files Created:" -ForegroundColor White
Write-Host "   ✅ IPortReservationService.cs - Interface contract" -ForegroundColor Gray
Write-Host "   ✅ PortReservationCriteria.cs - Criteria model" -ForegroundColor Gray
Write-Host "   ✅ ReservationStatistics.cs - Statistics model" -ForegroundColor Gray
Write-Host "   ✅ CriticalPOCTests.cs - 4 critical GO/NO-GO tests" -ForegroundColor Gray
Write-Host "   ✅ POCValidationRunner.cs - Decision automation" -ForegroundColor Gray
Write-Host "   ✅ Program.cs enhancements - DI integration" -ForegroundColor Gray
Write-Host ""
Write-Host "📋 Foundation Preserved (ZERO TOUCH):" -ForegroundColor White
Write-Host "   ✅ ISerialPortPool.cs - Unchanged" -ForegroundColor Gray
Write-Host "   ✅ PortAllocation.cs - Unchanged" -ForegroundColor Gray
Write-Host "   ✅ SerialPortPool.cs - Unchanged" -ForegroundColor Gray
Write-Host "   ✅ 65+ existing tests - Should all pass" -ForegroundColor Gray
Write-Host ""

<#
.SYNOPSIS
Sprint 5 POC Validation Script

.DESCRIPTION
This script validates the ZERO TOUCH Extension Layer POC for Sprint 5.
It provides two validation modes:
- QuickTest: Basic DI integration and service resolution (~5 minutes)
- FullValidation: Complete 4-hour critical test suite for GO/NO-GO decision

.PARAMETER QuickTest
Run quick validation test (recommended first)

.PARAMETER FullValidation  
Run full 4-hour critical validation for GO/NO-GO decision

.EXAMPLE
.\Execute-POC.ps1 -QuickTest
Run quick POC validation test

.EXAMPLE
.\Execute-POC.ps1 -FullValidation
Run full critical validation suite

.NOTES
Author: Sprint 5 POC Implementation
Date: July 2025
Requires: .NET 9.0 SDK, SerialPortPoolService solution
#>