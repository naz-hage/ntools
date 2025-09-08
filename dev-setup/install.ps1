#
# Backward compatibility wrapper for install.ps1
# This script has been split into multiple scripts in scripts/setup/
# This wrapper maintains compatibility with existing documentation
#

# Show deprecation warning
Write-Warning "DEPRECATION NOTICE: dev-setup/install.ps1 has been moved and split into multiple scripts:"
Write-Warning "  - scripts/setup/setup-install-apps.ps1 (for apps installation)"
Write-Warning "  - scripts/setup/setup-install-ntools.ps1 (for ntools installation)"
Write-Warning "  - scripts/setup/setup-environment.ps1 (for environment setup)"
Write-Warning "Please update your references to use the appropriate new script."
Write-Warning "This wrapper will be removed in a future version."
Write-Host ""

# Get the script root directory
$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Definition
$repoRoot = Split-Path -Parent $scriptRoot

# Path to the new centralized script
$newScriptPath = Join-Path $repoRoot "scripts\setup\setup-install-apps.ps1"

# Verify the new script exists
if (-not (Test-Path $newScriptPath)) {
    Write-Error "The centralized script was not found at: $newScriptPath"
    Write-Error "Please check your ntools installation."
    exit 1
}

# Forward all parameters to the new script
Write-Host "Forwarding to centralized script: $newScriptPath"
& $newScriptPath @args