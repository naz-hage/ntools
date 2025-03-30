<#
.SYNOPSIS
    Installs NTools.
.DESCRIPTION
    This script downloads, unzips, and installs NTools to the specified deployment path.
    It also adds the deployment path to the PATH environment variable.
.PARAMETER Version
    The version of NTools to install.
.PARAMETER DownloadsDirectory
    The directory to download the NTools zip file to. Defaults to "c:\NToolsDownloads".
.EXAMPLE
    .\install-ntools.ps1 -Version "1.2.3" -DownloadsDirectory "C:\Downloads"
.NOTES
    Requires administrative privileges.
#>

[CmdletBinding()]
param (
    [Parameter(Mandatory = $true, HelpMessage = "The version of NTools to install.")]
    [string]$Version,

    [Parameter(Mandatory = $false, HelpMessage = "The directory to download the NTools zip file to. Defaults to 'c:\\NToolsDownloads'.")]
    [string]$DownloadsDirectory = "c:\NToolsDownloads"
)

# Import the install module
Import-Module .\install.psm1

# Set the deployment path
$DeploymentPath = Join-Path -Path $env:ProgramFiles -ChildPath "NBuild"

# Call the InstallNtools function from the install module
InstallNtools -version $Version -downloadsDirectory $DownloadsDirectory

Write-Host "NTools version $Version installed to $DeploymentPath"