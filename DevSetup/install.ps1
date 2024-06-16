# Import the module
#########################
Import-Module ./install.psm1 -Force

$fileName = Split-Path -Leaf $PSCommandPath

Write-OutputMessage $fileName "Started installation script."

# Check if admin
#########################
if (-NOT ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator")) {
    Write-OutputMessage $fileName "Error: Please run this script as an administrator."
    exit 1
} else {
    Write-OutputMessage $fileName "Admin rights detected"
}

# Install Dotnet_Runtime
#########################
if (MainInstallApp -command install -json .\app-Dotnet_Runtime.json) {
    Write-OutputMessage $fileName "Installation of app-Dotnet_Runtime succeeded."
} else {
    Write-OutputMessage $fileName "Error: Installation of app-Dotnet_Runtime.json failed. Exiting script."
    exit 1
}

# install Ntools
#########################
if (MainInstallApp -command install -json .\app-Ntools.json) {
    Write-OutputMessage $fileName "Installation of app-Ntools succeeded."
} else {
    Write-OutputMessage $fileName "Error: Installation of app-Ntools.json failed. Exiting script."
    exit 1
}

#install Development tools for Ntools
#########################
MainInstallNtools 
#.\install-ntools.ps1 $DevDrive $MainDir
if ($LASTEXITCODE -ne 0) {
    Write-OutputMessage $fileName "Error: Installation of ntools failed. Exiting script."
    exit 1
}

Write-OutputMessage $fileName "Completed installation script."
Write-OutputMessage $fileName "EmtpyLine"
