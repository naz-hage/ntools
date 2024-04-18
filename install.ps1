[cmdletbinding()]
param(
    [Parameter(Mandatory = $false)]
    [String]
    $DevDrive = "C:",
    [Parameter(Mandatory = $false)]
    [String]
    $MainDir = "source"
)

# Check if admin
if (-NOT ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator")) {
    Write-Output "Please run this script as an administrator."
    exit 1
} else {
    Write-Output "Admin rights detected"
}

# Setup Development environment for Ntools
# save current directory
$currentdir = Get-Location
Set-Location -Path "nbuild\resources"
.\install-app.ps1 install .\app-Dotnet_Runtime.json
.\install-app.ps1 install .\app-Ntools.json
.\install-ntools.ps1 $DevDrive $MainDir

# Restore current directory
Set-Location -Path $currentdir

