# DEPRECATION NOTICE
#########################
Write-Warning "DEPRECATION NOTICE: dev-setup/install.ps1 has been moved and split into multiple scripts in scripts/setup/"
Write-Warning "  - Application installation is now handled by the Install module (see scripts/module-package/ntools-scripts.psm1 -> Install-NTools / Install-DevelopmentApps)"
Write-Warning "Please update your references to use the canonical module functions (Import-Module ./scripts/module-package/ntools-scripts.psm1)."
Write-Warning "This script will be removed in a future version."
Write-Host ""

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

# Install dotnet-runtime
#########################
if (MainInstallApp -command install -json .\dotnet-runtime.json) {
    Write-OutputMessage $fileName "Installation of dotnet-runtime succeeded."
} else {
    Write-OutputMessage $fileName "Error: Installation of dotnet-runtime.json failed. Exiting script."
    exit 1
}

# Call the InstallNtools function from the install module
$result = InstallNtools
if (-not $result) {
    Write-OutputMessage $fileName "Failed to install NTools. Please check the logs for more details." -ForegroundColor Red
    exit 1
}

# install apps
#########################
if (MainInstallApp -command install -json .\apps.json) {
    Write-OutputMessage $fileName "Installation of ntools succeeded."
} else {
    Write-OutputMessage $fileName "Error: Installation of ntools.json failed. Exiting script."
    exit 1
}


Write-OutputMessage $fileName "Completed installation script."
Write-OutputMessage $fileName "EmtpyLine"