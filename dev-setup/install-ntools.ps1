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

# Import the install module
Import-Module "$PSScriptRoot\install.psm1" -Force

# If Version is not specified, read it from ntools.json
if (-not $Version) {
    $NtoolsJsonPath = "$PSScriptRoot\ntools.json"

    if (Test-Path -Path $NtoolsJsonPath) {
        try {
            $NtoolsJson = Get-Content -Path $NtoolsJsonPath -Raw | ConvertFrom-Json
            $Version = $NtoolsJson.NbuildAppList[0].Version
            Write-Host "Version read from ntools.json: $Version"
        }
        catch {
            Write-Warning "Failed to read version from ntools.json. Please specify the version manually."
            return
        }
    }
    else {
        Write-Warning "ntools.json not found in the script directory. Please specify the version manually."
        return
    }
}

# Check if the version is empty after attempting to read from ntools.json
if (-not $Version) {
    Write-Warning "No version specified and failed to read from ntools.json. Exiting."
    return
}

# Set the deployment path
$DeploymentPath = Join-Path -Path $env:ProgramFiles -ChildPath "Nbuild"

# Call the InstallNtools function from the install module
InstallNtools -version $Version -downloadsDirectory $DownloadsDirectory

Write-Host "NTools version $Version installed to $DeploymentPath"