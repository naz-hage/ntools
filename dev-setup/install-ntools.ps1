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

# Call the InstallNtools function from the install module
$result = InstallNtools -version $Version -downloadsDirectory $DownloadsDirectory
if (-not $result) {
    Write-Host "Failed to install NTools. Please check the logs for more details." -ForegroundColor Red
    exit 1
}
