param(
    [string]$BuildTools = "$env:ProgramFiles\nbuild"
)


Write-Host "Testing installed ntools-scripts module in: $BuildTools"


# Look for the installed module in a few common locations. Historically the module
# may be placed at the build root or under modules\ntools-scripts\ when installed
# using the repository helper script.
$candidates = @(
    Join-Path $BuildTools 'ntools-scripts.psm1',
    Join-Path $BuildTools 'modules\ntools-scripts\ntools-scripts.psm1',
    Join-Path $BuildTools 'modules\ntools-scripts\ntools-scripts.psd1'
)

$modulePsm = $null
foreach ($cand in $candidates) {
    if (Test-Path $cand) { $modulePsm = $cand; break }
}

if ($modulePsm) {
    Write-Host "Importing module from: $modulePsm"
    Import-Module $modulePsm -Force -ErrorAction Stop
} else {
    Write-Error "ntools-scripts module not found in any of the expected locations:`n  $($candidates -join "`n  ")"
    exit 2
}

# Define expected functions from the module manifest
$expectedFunctions = @(
    'Publish-AllProjects',
    'Get-NtoolsScriptsVersion',
    'Get-VersionFromJson',
    'Update-DocVersions',
    'Write-TestResult',
    'Test-TargetExists',
    'Test-TargetDependencies',
    'Test-TargetDelegation',
    'Get-FileHash256',
    'Get-FileVersionInfo',
    'Invoke-FastForward',
    'Write-OutputMessage',
    'Get-NToolsFileVersion',
    'Add-DeploymentPathToEnvironment',
    'Invoke-NToolsDownload',
    'Install-NTools'
)

Write-Host "Checking for exported functions..."

$allFunctionsFound = $true
$missingFunctions = @()

foreach ($functionName in $expectedFunctions) {
    if (Get-Command $functionName -ErrorAction SilentlyContinue) {
        Write-Host "✓ Found function: $functionName" -ForegroundColor Green
    } else {
        Write-Host "✗ Missing function: $functionName" -ForegroundColor Red
        $missingFunctions += $functionName
        $allFunctionsFound = $false
    }
}

if ($missingFunctions.Count -gt 0) {
    Write-Error "Missing functions: $($missingFunctions -join ', ')"
    exit 1
}

try {
    $v = Get-NtoolsScriptsVersion
    if (-not $v) { Write-Error "No version reported"; exit 2 }
    Write-Host "ntools-scripts version: $v" -ForegroundColor Cyan
    
    if ($allFunctionsFound) {
        Write-Host "✓ All $($expectedFunctions.Count) expected functions are available!" -ForegroundColor Green
        exit 0
    }
} catch {
    Write-Error "Get-NtoolsScriptsVersion failed: $_"
    exit 3
}
