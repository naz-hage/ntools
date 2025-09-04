# Quick validation function for MSBuild target delegation
function Test-TargetDelegation {
    param(
        [string]$Source = "STAGE",
        [string]$Delegate = "STAGE_NEW", 
        [string[]]$ExpectedDeps = @("CLEAN", "TAG", "AUTOTAG_STAGE", "TEST", "COVERAGE", "COVERAGE_SUMMARY", "PUBLISH", "SMOKE_TEST", "PACKAGE")
    )
    
    $results = @{
        SourceExists = (Get-Content "nbuild.targets" -Raw) -match "Target\s+Name=`"$Source`""
        DelegateExists = (Get-Content "Nbuild\resources\common.targets" -Raw) -match "Target\s+Name=`"$Delegate`""
        DelegationWorks = (Get-Content "nbuild.targets" -Raw) -match "Target\s+Name=`"$Source`"\s+DependsOnTargets=`"$Delegate`""
        TargetsAvailable = (& nb targets 2>$null) -match "$Source|$Delegate"
    }
    
    $allPassed = $results.Values -notcontains $false
    
    Write-Host "=== Target Delegation Test Results ===" -ForegroundColor Cyan
    $results.GetEnumerator() | ForEach-Object { 
        $status = if ($_.Value) { "✅" } else { "❌" }
        Write-Host "$status $($_.Key): $($_.Value)"
    }
    Write-Host "Overall: $(if ($allPassed) { '✅ PASS' } else { '❌ FAIL' })" -ForegroundColor $(if ($allPassed) { "Green" } else { "Red" })
    
    return $allPassed
}

# Usage: Test-TargetDelegation
