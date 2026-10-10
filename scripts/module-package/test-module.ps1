param(
    [string]$BuildTools = "$env:ProgramFiles\sdo"
)


Write-Host "Testing installed sdo-scripts module in: $BuildTools"


$modulePsm = Join-Path $BuildTools 'sdo-scripts.psm1'

if (Test-Path $modulePsm) {
    Write-Host "Importing module from: $modulePsm"
    Import-Module $modulePsm -Force -ErrorAction Stop
} else {
    Write-Error "sdo-scripts module not found at: $modulePsm"
    exit 2
}

# Define expected functions from the module manifest
$expectedFunctions = @(
    'Publish-AllProjects',
    'Get-SdoScriptsVersion',
    'Get-VersionFromYaml',
    'Write-TestResult',
    'Test-TargetExists',
    'Test-TargetDependencies',
    'Test-TargetDelegation',
    'Get-FileHash256',
    'Get-FileVersionInfo',
    'Invoke-FastForward',
    'Write-OutputMessage',
    'Get-SdoFileVersion',
    'Add-DeploymentPathToEnvironment',
    'Invoke-SdoDownload',
    'Install-Sdo'
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
    $v = Get-SdoScriptsVersion
    if (-not $v) { Write-Error "No version reported"; exit 2 }
    Write-Host "sdo-scripts version: $v" -ForegroundColor Cyan
    
    if ($allFunctionsFound) {
        Write-Host "✓ All $($expectedFunctions.Count) expected functions are available!" -ForegroundColor Green
        exit 0
    }
} catch {
    Write-Error "Get-SdoScriptsVersion failed: $_"
    exit 3
}
