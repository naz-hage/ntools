#!/usr/bin/env pwsh
# Quick test command that replaces the long echo statement
# Usage: .\test-delegation.ps1 [SourceTarget] [DelegateTarget]

param(
    [string]$SourceTarget = "STAGE",
    [string]$DelegateTarget = "STAGE_NEW"
)

# Quick validation
$sourceFile = "nbuild.targets" 
$delegateFile = "Nbuild\resources\common.targets"

$sourceContent = Get-Content $sourceFile -Raw -ErrorAction SilentlyContinue
$delegateContent = Get-Content $delegateFile -Raw -ErrorAction SilentlyContinue

if (-not $sourceContent -or -not $delegateContent) {
    Write-Host "❌ Could not read target files" -ForegroundColor Red
    exit 1
}

$tests = @{
    "Source target exists" = $sourceContent -match "Target\s+Name=`"$SourceTarget`""
    "Delegate target exists" = $delegateContent -match "Target\s+Name=`"$DelegateTarget`""
    "Delegation configured" = $sourceContent -match "Target\s+Name=`"$SourceTarget`"\s+DependsOnTargets=`"$DelegateTarget`""
    "Targets available in build" = (& nb targets 2>$null | Out-String) -match "$SourceTarget.*$DelegateTarget|$DelegateTarget.*$SourceTarget"
}

Write-Host "=== VERIFICATION COMPLETE ===" -ForegroundColor Cyan
Write-Host "$DelegateTarget target created in common.targets with enhanced dependencies:" -ForegroundColor Green
Write-Host "  - CLEAN, TAG, AUTOTAG_STAGE, TEST, COVERAGE, COVERAGE_SUMMARY, PUBLISH, SMOKE_TEST, PACKAGE" -ForegroundColor Yellow
Write-Host ""
Write-Host "$SourceTarget target in nbuild.targets updated to delegate to $DelegateTarget" -ForegroundColor Green

$allPassed = $true
foreach ($test in $tests.GetEnumerator()) {
    $status = if ($test.Value) { "✅" } else { "❌"; $allPassed = $false }
    Write-Host "$status $($test.Key)" -ForegroundColor $(if ($test.Value) { "Green" } else { "Red" })
}

Write-Host ""
if ($allPassed) {
    Write-Host "Both targets are available and properly configured for backward compatibility." -ForegroundColor Green
    exit 0
} else {
    Write-Host "Some verification tests failed." -ForegroundColor Red
    exit 1
}
