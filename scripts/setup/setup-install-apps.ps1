# Import the module
#########################
$scriptDir = Split-Path -Parent $PSCommandPath
Import-Module "$scriptDir\..\modules\Install.psm1" -Force

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

# Get path to dev-setup directory (relative to scripts/setup)
$devSetupPath = Join-Path (Split-Path -Parent (Split-Path -Parent $PSCommandPath)) "dev-setup"

# Install dotnet-runtime
#########################
$dotnetRuntimeJson = Join-Path $devSetupPath "dotnet-runtime.json"
if (MainInstallApp -command install -json $dotnetRuntimeJson) {
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
$appsJson = Join-Path $devSetupPath "apps.json"
if (MainInstallApp -command install -json $appsJson) {
    Write-OutputMessage $fileName "Installation of ntools succeeded."
} else {
    Write-OutputMessage $fileName "Error: Installation of ntools.json failed. Exiting script."
    exit 1
}


Write-OutputMessage $fileName "Completed installation script."
Write-OutputMessage $fileName "EmtpyLine"
