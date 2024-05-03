Content of DevSetup.ps1:
This script sets up the development environment for your project, installs `ntools` and the necessary development tools, and sets the development environment variables.

```powershell
# DevSetup.ps1
$fileName = Split-Path -Leaf $PSCommandPath
Write-OutputMessage $fileName "Started installation script."

# Download and import the common Install module
$url = "https://raw.githubusercontent.com/naz-hage/ntools/main/install.psm1"
$output = "./install.psm1"
Invoke-WebRequest -Uri $url -OutFile $output

if ($LASTEXITCODE -ne 0) {
    Write-OutputMessage $fileName "Error: downloading install.psm1 failed. Exiting script."
    exit 1
}
Import-Module ./install.psm1 -Force

# Check if the script is running with admin rights
if (-NOT ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator")) {
    Write-OutputMessage $fileName "Error: Please run this script as an administrator."
    exit 1
} else {
    Write-OutputMessage $fileName "Admin rights detected"
}

# Install Ntools
MainInstallApp -command install -json .\app-Ntools.json
if ($LASTEXITCODE -ne 0) {
    Write-OutputMessage $fileName "Error: Installation of app-Ntools.json failed. Exiting script."
    exit 1
}

# Install development tools for the MyProject project
& $global:NbExePath -c install -json .\apps.json
if ($LASTEXITCODE -ne 0) {
    Write-OutputMessage $fileName "Error: Installation of ntools failed. Exiting script."
    exit 1
}

Write-OutputMessage $fileName "Completed installation script."
Write-OutputMessage $fileName "EmtpyLine"
```
