#
# Backward compatibility wrapper for install.psm1
# This module has been moved to scripts/modules/Install.psm1
# This wrapper maintains compatibility with existing imports
#

# Show deprecation warning
Write-Warning "DEPRECATION NOTICE: dev-setup/install.psm1 has been moved to scripts/modules/Install.psm1"
Write-Warning "Please update your module imports to use: Import-Module ./scripts/modules/Install.psm1"
Write-Warning "This wrapper will be removed in a future version."

# Get the script root directory
$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Definition
$repoRoot = Split-Path -Parent $scriptRoot

# Path to the new centralized module
$newModulePath = Join-Path $repoRoot "scripts\modules\Install.psm1"

# Verify the new module exists
if (-not (Test-Path $newModulePath)) {
    Write-Error "The centralized module was not found at: $newModulePath"
    Write-Error "Please check your ntools installation."
    return
}

# Import and re-export all functions from the new module
Write-Host "Forwarding to centralized module: $newModulePath"
$module = Import-Module $newModulePath -PassThru -Force

# Re-export all functions from the centralized module
if ($module) {
    $module.ExportedFunctions.Keys | ForEach-Object {
        Export-ModuleMember -Function $_
    }
}