- After installing `NTools`, two environment variables, `DEVDRIVE` and `MAINDIR`, are created.
- When you create a new project, for example `MyProject`, clone the project into the `%DEVDRIVE%\%MAINDIR%` directory. Your file structure should look like this:
```cmd
c:\source\MyProject
```
- In your project, create a `DevSetup` folder and add the `apps.json` file to it.
- In the `DevSetup`folder, create a `DevSetup.ps1` file and add the following PowerShell script:

```powershell
# The [cmdletbinding()] attribute is used to make the script function like a cmdlet
# it is a lightweight command used in the PowerShell environment. This attribute allows the script to use cmdlet 
# features such as common parameters (like -Verbose, -Debug, etc.) and the ability to be used in pipelines.
[cmdletbinding()]
param(
    [Parameter(Mandatory = $false)]
    [String]
    $DevDrive = "c:",

    [Parameter(Mandatory = $false)]
    [String]
    $MainDir = "source"
)

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

# Set the development environment variables
SetDevEnvironmentVariables -devDrive $DevDrive -mainDir $MainDir

Write-OutputMessage $fileName "Completed installation script."
Write-OutputMessage $fileName "EmtpyLine"
```

This script sets up the development environment for your project, installs `NTools` and the necessary development tools, and sets the development environment variables.