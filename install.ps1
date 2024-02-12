# Set variables
$DotnetVersion = "8.0.1"
$NtoolsVersion = "1.2.57"
$DevDrive = "c:"
$MainDir = "source"

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
.\install-ntools.ps1 $DotnetVersion $NtoolsVersion $DevDrive $MainDir

# Restore current directory
Set-Location -Path $currentdir

