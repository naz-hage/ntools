<#
.SYNOPSIS
    Installs NTools.
.DESCRIPTION
    This script downloads, unzips, and installs NTools to the specified deployment path.
    It also adds the deployment path to the PATH environment variable.
    If the version is not specified, it reads the version from ntools.json file in the same directory as this script.
.PARAMETER Version
    The version of NTools to install. If not specified, the version is read from ntools.json.
.PARAMETER DownloadsDirectory
    The directory to download the NTools zip file to. Defaults to "c:\NToolsDownloads".
.EXAMPLE
    .\install-ntools.ps1 -Version "1.2.3" -DownloadsDirectory "C:\Downloads"
    .\install-ntools.ps1 -DownloadsDirectory "C:\Downloads" # Reads version from ntools.json
.NOTES
    Requires administrative privileges.
#>

[CmdletBinding()]
param (
    [Parameter(Mandatory = $false, HelpMessage = "The version of NTools to install. If not specified, the version is read from ntools.json.")]
    [string]$Version,

    [Parameter(Mandatory = $false, HelpMessage = "The directory to download the NTools zip file to. Defaults to 'c:\\NToolsDownloads'.")]
    [string]$DownloadsDirectory = "c:\NToolsDownloads"
)

# display $PSScriptRoot
Write-Host "PSScriptRoot: $PSScriptRoot"

# Import the consolidated ntools-scripts module instead of Install.psm1
$scriptDir = Split-Path -Parent $PSCommandPath
# Prefer the new manifest name, fall back to the legacy manifest for compatibility
$moduleManifest = "$scriptDir\..\module-package\ntools-scripts.psd1"

if (Test-Path $moduleManifest) {
    Import-Module $moduleManifest -Force
} else {
    # Try to import from installed location; prefer new module folder name first
    $installedModule = "$env:ProgramFiles\nbuild\modules\ntools-scripts\ntools-scripts.psd1"
    if (Test-Path $installedModule) {
        Import-Module $installedModule -Force
    } else {
        Write-Error "ntools-scripts module not found. Please install it first using: scripts\module-package\install-module.ps1"
        exit 1
    }
}

$fileName = Split-Path -Leaf $PSCommandPath

# Check if admin
#########################
if (-NOT ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator")) {
    Write-OutputMessage $fileName "Error: Please run this script as an administrator."
    exit 1
} else {
    Write-OutputMessage $fileName "Admin rights detected"
}

# Call the Install-NTools function from the consolidated module
$result = Install-NTools -version $Version -downloadsDirectory $DownloadsDirectory
if (-not $result) {
    Write-OutputMessage $fileName "Failed to install NTools. Please check the logs for more details."
    exit 1
}
