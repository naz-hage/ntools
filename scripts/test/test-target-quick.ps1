#!/usr/bin/env pwsh
# Quick validation function for MSBuild target delegation
param(
    [string]$Source = "STAGE",
    [string]$Delegate = "STAGE_NEW", 
    [string[]]$ExpectedDeps = @("CLEAN", "TAG", "AUTOTAG_STAGE", "TEST", "COVERAGE", "COVERAGE_SUMMARY", "PUBLISH", "SMOKE_TEST", "PACKAGE")
)

# Import required modules
$scriptDir = Split-Path -Parent $PSCommandPath
Import-Module "$scriptDir\..\modules\Testing.psm1" -Force

# Start test suite
Start-TestSuite -Name "Quick Target Delegation Validation"

# Test 1: Source target exists
$sourceTest = Test-TargetExists -TargetName $Source -FilePath "nbuild.targets"
Write-TestResult -Test "Source target '$Source' exists" -Passed $sourceTest.Exists -Details $sourceTest.Details

# Test 2: Delegate target exists
$delegateTest = Test-TargetExists -TargetName $Delegate -FilePath "Nbuild\resources\common.targets"
Write-TestResult -Test "Delegate target '$Delegate' exists" -Passed $delegateTest.Exists -Details $delegateTest.Details

# Test 3: Delegation works
$delegationTest = Test-TargetDelegation -SourceTargetName $Source -DelegateTargetName $Delegate -SourceFilePath "nbuild.targets"
Write-TestResult -Test "Target delegation works" -Passed $delegationTest.Valid -Details $delegationTest.Details

# Test 4: Targets available via nb command
try {
    $nbTargets = & nb targets 2>$null | Out-String
    $targetsAvailable = ($nbTargets -match $Source) -and ($nbTargets -match $Delegate)
    $details = if ($targetsAvailable) { "Both targets found in nb targets output" } else { "One or both targets missing from nb targets output" }
    Write-TestResult -Test "Targets available via 'nb targets'" -Passed $targetsAvailable -Details $details
} catch {
    Write-TestResult -Test "Targets available via 'nb targets'" -Passed $false -Details "Error running 'nb targets': $($_.Exception.Message)"
}

# Complete test suite
$success = Complete-TestSuite -ExitOnFailure
exit $(if ($success) { 0 } else { 1 })
