#
# Backward compatibility wrapper for install-ntools.ps1
# This script has been moved to scripts/setup/setup-install-ntools.ps1
# This wrapper maintains compatibility with existing workflows and documentation
#

# Show deprecation warning
Write-Warning "DEPRECATION NOTICE: dev-setup/install-ntools.ps1 has been moved to scripts/setup/setup-install-ntools.ps1"
Write-Warning "Please update your references to use the new centralized location."
Write-Warning "This wrapper will be removed in a future version."
Write-Host ""

# Get the script root directory
$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Definition
$repoRoot = Split-Path -Parent $scriptRoot

# Path to the new centralized script
$newScriptPath = Join-Path $repoRoot "scripts\setup\setup-install-ntools.ps1"

# Verify the new script exists
if (-not (Test-Path $newScriptPath)) {
    Write-Error "The centralized script was not found at: $newScriptPath"
    Write-Error "Please check your ntools installation."
    exit 1
}

# Forward all parameters to the new script
Write-Host "Forwarding to centralized script: $newScriptPath"
& $newScriptPath @args