<#
.SYNOPSIS
    Testing utility functions for NTools PowerShell scripts.

.DESCRIPTION
    This module provides common testing functionality used across NTools test scripts,
    including test result formatting, MSBuild target validation, and test execution helpers.

.FUNCTIONS
    | Function Name               | Description                                                   |
    |-----------------------------|---------------------------------------------------------------|
    | Write-TestResult            | Writes test results with consistent formatting.               |
    | Test-TargetExists           | Tests if an MSBuild target exists in a file.                 |
    | Test-TargetDelegation       | Tests if one target properly delegates to another.            |
    | Test-TargetDependencies     | Tests if a target has expected dependencies.                  |
    | Start-TestSuite             | Initializes a test suite with counters.                       |
    | Complete-TestSuite          | Completes a test suite and reports results.                   |

.EXAMPLE
    Import-Module .\scripts\modules\Testing.psm1
    Start-TestSuite -Name "Target Delegation Tests"
    $result = Test-TargetExists -TargetName "BUILD" -FilePath ".\nbuild.targets"
    Write-TestResult -Test "BUILD target exists" -Passed $result.Exists -Details $result.Details
    Complete-TestSuite

.NOTES
    This module is designed to provide consistent testing patterns across all NTools test scripts.
#>

# Test suite state
$script:CurrentTestSuite = $null
$script:TotalTests = 0
$script:PassedTests = 0
$script:StartTime = $null

function Write-TestResult {
    <#
    .SYNOPSIS
        Writes test results with consistent formatting.
    #>
    param(
        [Parameter(Mandatory = $true)]
        [string]$Test,
        
        [Parameter(Mandatory = $true)]
        [bool]$Passed,
        
        [Parameter(Mandatory = $false)]
        [string]$Details = ""
    )
    
    $status = if ($Passed) { "✅ PASS" } else { "❌ FAIL" }
    $message = "$status - $Test"
    if ($Details) {
        $message += ": $Details"
    }
    
    $color = if ($Passed) { "Green" } else { "Red" }
    Write-Host $message -ForegroundColor $color
    
    $script:TotalTests++
    if ($Passed) {
        $script:PassedTests++
    }
    
    return $Passed
}

function Test-TargetExists {
    <#
    .SYNOPSIS
        Tests if an MSBuild target exists in a file.
    #>
    param(
        [Parameter(Mandatory = $true)]
        [string]$TargetName,
        
        [Parameter(Mandatory = $true)]
        [string]$FilePath
    )
    
    if (-not (Test-Path $FilePath)) {
        return @{ Exists = $false; Details = "File not found: $FilePath" }
    }
    
    try {
        $content = Get-Content $FilePath -Raw -ErrorAction Stop
        $targetPattern = "Target\s+Name=`"$TargetName`""
        
        if ($content -match $targetPattern) {
            return @{ Exists = $true; Details = "Target '$TargetName' found in $FilePath" }
        } else {
            return @{ Exists = $false; Details = "Target '$TargetName' not found in $FilePath" }
        }
    }
    catch {
        return @{ Exists = $false; Details = "Error reading file '$FilePath': $($_.Exception.Message)" }
    }
}

function Test-TargetDelegation {
    <#
    .SYNOPSIS
        Tests if one target properly delegates to another.
    #>
    param(
        [Parameter(Mandatory = $true)]
        [string]$SourceTargetName,
        
        [Parameter(Mandatory = $true)]
        [string]$DelegateTargetName,
        
        [Parameter(Mandatory = $true)]
        [string]$SourceFilePath
    )
    
    if (-not (Test-Path $SourceFilePath)) {
        return @{ Valid = $false; Details = "Source file not found: $SourceFilePath" }
    }
    
    try {
        $content = Get-Content $SourceFilePath -Raw -ErrorAction Stop
        $delegationPattern = "Target\s+Name=`"$SourceTargetName`"\s+DependsOnTargets=`"$DelegateTargetName`""
        
        if ($content -match $delegationPattern) {
            return @{ Valid = $true; Details = "Target '$SourceTargetName' correctly delegates to '$DelegateTargetName'" }
        } else {
            return @{ Valid = $false; Details = "Target '$SourceTargetName' does not delegate to '$DelegateTargetName'" }
        }
    }
    catch {
        return @{ Valid = $false; Details = "Error reading file '$SourceFilePath': $($_.Exception.Message)" }
    }
}

function Test-TargetDependencies {
    <#
    .SYNOPSIS
        Tests if a target has expected dependencies.
    #>
    param(
        [Parameter(Mandatory = $true)]
        [string]$TargetName,
        
        [Parameter(Mandatory = $true)]
        [string]$FilePath,
        
        [Parameter(Mandatory = $true)]
        [string[]]$ExpectedDeps
    )
    
    if (-not (Test-Path $FilePath)) {
        return @{ Valid = $false; Details = "File not found: $FilePath"; Actual = @(); Missing = @(); Extra = @() }
    }
    
    try {
        $content = Get-Content $FilePath -Raw -ErrorAction Stop
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
                $details = @()
                if ($missing.Count -gt 0) { $details += "Missing: $($missing -join ', ')" }
                if ($extra.Count -gt 0) { $details += "Extra: $($extra -join ', ')" }
                
                return @{ 
                    Valid = $false
                    Details = $details -join '; '
                    Actual = $actualDeps
                    Missing = $missing
                    Extra = $extra
                }
            }
        } else {
            return @{ 
                Valid = $false
                Details = "Target '$TargetName' not found or has no dependencies"
                Actual = @()
                Missing = $ExpectedDeps
                Extra = @()
            }
        }
    }
    catch {
        return @{ 
            Valid = $false
            Details = "Error reading file '$FilePath': $($_.Exception.Message)"
            Actual = @()
            Missing = @()
            Extra = @()
        }
    }
}

function Start-TestSuite {
    <#
    .SYNOPSIS
        Initializes a test suite with counters.
    #>
    param(
        [Parameter(Mandatory = $true)]
        [string]$Name
    )
    
    $script:CurrentTestSuite = $Name
    $script:TotalTests = 0
    $script:PassedTests = 0
    $script:StartTime = Get-Date
    
    Write-Host "=== Starting Test Suite: $Name ===" -ForegroundColor Cyan
    Write-Host ""
}

function Complete-TestSuite {
    <#
    .SYNOPSIS
        Completes a test suite and reports results.
    #>
    param(
        [Parameter(Mandatory = $false)]
        [switch]$ExitOnFailure
    )
    
    $endTime = Get-Date
    $elapsed = $endTime - $script:StartTime
    $failedTests = $script:TotalTests - $script:PassedTests
    
    Write-Host ""
    Write-Host "=== Test Suite Complete: $script:CurrentTestSuite ===" -ForegroundColor Cyan
    Write-Host "Total Tests: $script:TotalTests"
    Write-Host "Passed: $script:PassedTests" -ForegroundColor Green
    Write-Host "Failed: $failedTests" -ForegroundColor $(if ($failedTests -eq 0) { "Green" } else { "Red" })
    Write-Host "Elapsed Time: $(Format-ElapsedTime $elapsed)"
    
    $success = $failedTests -eq 0
    $overallStatus = if ($success) { "✅ ALL TESTS PASSED" } else { "❌ SOME TESTS FAILED" }
    $statusColor = if ($success) { "Green" } else { "Red" }
    
    Write-Host ""
    Write-Host $overallStatus -ForegroundColor $statusColor
    
    if ($ExitOnFailure -and -not $success) {
        exit 1
    }
    
    return $success
}

function Format-ElapsedTime {
    <#
    .SYNOPSIS
        Formats elapsed time for display (duplicated from Common.psm1 to avoid dependency).
    #>
    param(
        [Parameter(Mandatory = $true)]
        [TimeSpan]$TimeSpan
    )
    
    if ($TimeSpan.TotalMinutes -ge 1) {
        return "{0:N0} minutes, {1:N0} seconds" -f $TimeSpan.Minutes, $TimeSpan.Seconds
    }
    else {
        return "{0:N2} seconds" -f $TimeSpan.TotalSeconds
    }
}

# Export functions
Export-ModuleMember -Function Write-TestResult, Test-TargetExists, Test-TargetDelegation, Test-TargetDependencies, Start-TestSuite, Complete-TestSuite