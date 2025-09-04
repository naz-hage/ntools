#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Generic test for MSBuild target delegation and configuration validation.

.DESCRIPTION
    Validates that MSBuild targets are properly configured with correct dependencies
    and delegation patterns. Can be used to verify target migration from project-specific
    to common targets files.

.PARAMETER SourceTarget
    The delegating target name (e.g., "STAGE")

.PARAMETER DelegateTarget
    The target being delegated to (e.g., "STAGE_NEW")

.PARAMETER SourceFile
    Path to the file containing the source target

.PARAMETER DelegateFile
    Path to the file containing the delegate target

.PARAMETER ExpectedDependencies
    Array of expected dependencies for the delegate target

.EXAMPLE
    .\Test-TargetDelegation.ps1 -SourceTarget "STAGE" -DelegateTarget "STAGE_NEW" -SourceFile "nbuild.targets" -DelegateFile "Nbuild\resources\common.targets" -ExpectedDependencies @("CLEAN", "TAG", "AUTOTAG_STAGE", "TEST", "COVERAGE", "COVERAGE_SUMMARY", "PUBLISH", "SMOKE_TEST", "PACKAGE")
#>

param(
    [Parameter(Mandatory = $true)]
    [string]$SourceTarget,
    
    [Parameter(Mandatory = $true)]
    [string]$DelegateTarget,
    
    [Parameter(Mandatory = $true)]
    [string]$SourceFile,
    
    [Parameter(Mandatory = $true)]
    [string]$DelegateFile,
    
    [Parameter(Mandatory = $true)]
    [string[]]$ExpectedDependencies,
    
    [Parameter(Mandatory = $false)]
    [string]$WorkingDirectory = (Get-Location).Path,
    
    [Parameter(Mandatory = $false)]
    [switch]$VerboseOutput
)

function Write-TestResult {
    param(
        [string]$Test,
        [bool]$Passed,
        [string]$Details = ""
    )
    
    $status = if ($Passed) { "✅ PASS" } else { "❌ FAIL" }
    $message = "$status - $Test"
    if ($Details) {
        $message += ": $Details"
    }
    Write-Host $message
    return $Passed
}

function Test-TargetExists {
    param(
        [string]$TargetName,
        [string]$FilePath
    )
    
    if (-not (Test-Path $FilePath)) {
        return @{ Exists = $false; Details = "File not found: $FilePath" }
    }
    
    $content = Get-Content $FilePath -Raw
    $targetPattern = "Target\s+Name=`"$TargetName`""
    
    if ($content -match $targetPattern) {
        return @{ Exists = $true; Details = "Target found in $FilePath" }
    } else {
        return @{ Exists = $false; Details = "Target not found in $FilePath" }
    }
}

function Test-TargetDependencies {
    param(
        [string]$TargetName,
        [string]$FilePath,
        [string[]]$ExpectedDeps
    )
    
    $content = Get-Content $FilePath -Raw
    $targetPattern = "Target\s+Name=`"$TargetName`"\s+DependsOnTargets=`"([^`"]*)`""
    
    if ($content -match $targetPattern) {
        $actualDeps = $Matches[1] -split '[;\s]+' | Where-Object { $_ -ne '' } | ForEach-Object { $_.Trim() }
        $missing = $ExpectedDeps | Where-Object { $_ -notin $actualDeps }
        $extra = $actualDeps | Where-Object { $_ -notin $ExpectedDeps }
        
        if ($missing.Count -eq 0 -and $extra.Count -eq 0) {
            return @{ 
                Valid = $true
                Details = "All dependencies match: $($actualDeps -join ', ')"
                Actual = $actualDeps
                Missing = @()
                Extra = @()
            }
        } else {
            return @{ 
                Valid = $false
                Details = "Dependency mismatch. Missing: $($missing -join ', '). Extra: $($extra -join ', ')"
                Actual = $actualDeps
                Missing = $missing
                Extra = $extra
            }
        }
    } else {
        return @{ 
            Valid = $false
            Details = "Could not parse dependencies for target $TargetName"
            Actual = @()
            Missing = $ExpectedDeps
            Extra = @()
        }
    }
}

function Test-TargetDelegation {
    param(
        [string]$SourceTargetName,
        [string]$DelegateTargetName,
        [string]$SourceFilePath
    )
    
    $content = Get-Content $SourceFilePath -Raw
    $delegationPattern = "Target\s+Name=`"$SourceTargetName`"\s+DependsOnTargets=`"$DelegateTargetName`""
    
    if ($content -match $delegationPattern) {
        return @{ 
            Valid = $true
            Details = "$SourceTargetName correctly delegates to $DelegateTargetName"
        }
    } else {
        return @{ 
            Valid = $false
            Details = "$SourceTargetName does not delegate to $DelegateTargetName"
        }
    }
}

# Main test execution
Write-Host "=== MSBuild Target Delegation Test ===" -ForegroundColor Cyan
Write-Host "Testing: $SourceTarget -> $DelegateTarget" -ForegroundColor Yellow
Write-Host ""

$testsPassed = 0
$totalTests = 0

# Test 1: Source target exists
$totalTests++
$sourceTest = Test-TargetExists -TargetName $SourceTarget -FilePath (Join-Path $WorkingDirectory $SourceFile)
$passed = Write-TestResult -Test "Source target '$SourceTarget' exists in $SourceFile" -Passed $sourceTest.Exists -Details $sourceTest.Details
if ($passed) { $testsPassed++ }

# Test 2: Delegate target exists
$totalTests++
$delegateTest = Test-TargetExists -TargetName $DelegateTarget -FilePath (Join-Path $WorkingDirectory $DelegateFile)
$passed = Write-TestResult -Test "Delegate target '$DelegateTarget' exists in $DelegateFile" -Passed $delegateTest.Exists -Details $delegateTest.Details
if ($passed) { $testsPassed++ }

# Test 3: Source target delegates to delegate target
$totalTests++
$delegationTest = Test-TargetDelegation -SourceTargetName $SourceTarget -DelegateTargetName $DelegateTarget -SourceFilePath (Join-Path $WorkingDirectory $SourceFile)
$passed = Write-TestResult -Test "Source target delegates correctly" -Passed $delegationTest.Valid -Details $delegationTest.Details
if ($passed) { $testsPassed++ }

# Test 4: Delegate target has expected dependencies
$totalTests++
$dependencyTest = Test-TargetDependencies -TargetName $DelegateTarget -FilePath (Join-Path $WorkingDirectory $DelegateFile) -ExpectedDeps $ExpectedDependencies
$passed = Write-TestResult -Test "Delegate target has correct dependencies" -Passed $dependencyTest.Valid -Details $dependencyTest.Details
if ($passed) { $testsPassed++ }

# Test 5: Targets are available via nb targets command
$totalTests++
try {
    $nbTargets = & nb targets 2>$null | Out-String
    $sourceAvailable = $nbTargets -match $SourceTarget
    $delegateAvailable = $nbTargets -match $DelegateTarget
    
    if ($sourceAvailable -and $delegateAvailable) {
        $passed = Write-TestResult -Test "Both targets available via 'nb targets'" -Passed $true -Details "Both $SourceTarget and $DelegateTarget are listed"
        if ($passed) { $testsPassed++ }
    } else {
        $missing = @()
        if (-not $sourceAvailable) { $missing += $SourceTarget }
        if (-not $delegateAvailable) { $missing += $DelegateTarget }
        Write-TestResult -Test "Both targets available via 'nb targets'" -Passed $false -Details "Missing targets: $($missing -join ', ')"
    }
} catch {
    Write-TestResult -Test "Both targets available via 'nb targets'" -Passed $false -Details "Failed to run 'nb targets': $($_.Exception.Message)"
}

# Summary
Write-Host ""
Write-Host "=== Test Results Summary ===" -ForegroundColor Cyan
Write-Host "Passed: $testsPassed/$totalTests tests" -ForegroundColor $(if ($testsPassed -eq $totalTests) { "Green" } else { "Yellow" })

if ($testsPassed -eq $totalTests) {
    Write-Host "✅ All tests passed! Target delegation is properly configured." -ForegroundColor Green
    exit 0
} else {
    Write-Host "❌ Some tests failed. Please review the target configuration." -ForegroundColor Red
    exit 1
}
