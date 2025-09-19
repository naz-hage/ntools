<#
.SYNOPSIS
    This module contains functions to install and manage development tools.
.DESCRIPTION
    This module provides functions to prepare the downloads directory, get application information,
    check if an application is installed, install applications, and manage environment variables.
    It also includes functions to handle .NET Core installation and version checking.
    The module is designed to be used in a PowerShell environment and requires administrative privileges
    for certain operations.
    The module also includes functions to write output messages to the console and log files.
    The module is intended to be used as part of a larger script or automation process.

.FUNCTIONS
    | Function Name               | Description                                                   |
    |-----------------------------|---------------------------------------------------------------|
    | PrepareDownloadsDirectory   | Prepares the downloads directory for storing installation files. |
    | GetAppInfo                  | Retrieves application information from a JSON file.           |
    | CheckIfAppInstalled         | Checks if an application is installed and its version.        |
    | Install                     | Installs an application using the provided JSON configuration.|
    | MainInstallApp              | Main function to install an application.                      |
    | CheckIfDotnetInstalled      | Checks if .NET Core is installed and its version.             |
    | InstallDotNetCore           | Installs .NET Core if not already installed.                  |
    | InstallNtools               | Installs NTools by downloading and unzipping the specified version. |
    | DownloadNtools              | Downloads the specified version of NTools from GitHub.        |
    | SetDevEnvironmentVariables  | Sets development environment variables.                       |
    | Write-OutputMessage         | Writes output messages to the console and log files.          |
    | GetFileVersion              | Retrieves the file version of a specified file.               |
    | EnsureMinikubeIsRunning       | Checks if Minikube is running and starts it if not.           |

.EXAMPLE
    Import-Module ./install.psm1
    PrepareDownloadsDirectory -directory "C:\NToolsDownloads"
    $appInfo = GetAppInfo --json "C:\NToolsDownloads\ntools.json"
    $isInstalled = CheckIfAppInstalled --json "C:\NToolsDownloads\ntools.json"
    Install --json "C:\NToolsDownloads\ntools.json"
    SetDevEnvironmentVariables -devDrive "D:" -mainDir "C:\MainDir"
    InstallDotNetCore -dotnetVersion "3.1.0"

    MainInstallApp -command install --json "C:\NToolsDownloads\ntools.json"
    
    SetDevEnvironmentVariables -devDrive "D:" -mainDir "C:\MainDir"
    Write-OutputMessage -Prefix "Info" -Message "Installation completed successfully."
    GetFileVersion -FilePath "C:\Program Files\NBuild\nb.exe"

.NOTES
    
#>

# DEPRECATION NOTICE
#########################
Write-Warning "DEPRECATION NOTICE: dev-setup/install.psm1 has been moved to scripts/modules/Install.psm1"
Write-Warning "Please update your module imports to use: Import-Module ./scripts/modules/Install.psm1"
Write-Warning "This module will be removed in a future version."

# local variables
$downloadsDirectory = "c:\NToolsDownloads"
$deploymentPath = $env:ProgramFiles + "\NBuild"
$nbToolsPath = "$deploymentPath\ntools.json"

# global variables
$global:NbExePath = "$deploymentPath\nb.exe"

function PrepareDownloadsDirectory {
    param (
        [Parameter(Mandatory=$true)]
        [string]$directory)

    # Create the Downloads directory if it doesn't exist
    if (!(Test-Path -Path $directory)) {
        New-Item -ItemType Directory -Path $directory | Out-Null
    }

    # Grant Administrators full control of the Downloads directory
    icacls.exe $directory /grant 'Administrators:(OI)(CI)F' /inheritance:r
}

function GetAppInfo {
    param (
        [Parameter(Mandatory=$true)]
        [string]$jsonFile
    )
    # read file git.json and convert to json object
    $json = Get-Content -Path $jsonFile -Raw
    
    # $config = $json | ConvertFrom-Json
    # Retrieve elements using dot notation

    $config = $json | ConvertFrom-Json | Select-Object -ExpandProperty NbuildAppList | Select-Object -First 1

    $appInfo = @{
        Name = $config.Name
        Version = $config.Version
        AppFileName = $config.AppFileName
        WebDownloadFile = $config.WebDownloadFile
        DownloadedFile = $config.DownloadedFile
        InstallCommand = $config.InstallCommand
        InstallArgs = $config.InstallArgs
        InstallPath = $config.InstallPath
        UninstallCommand = $config.UninstallCommand
        UninstallArgs = $config.UninstallArgs            
    }

    # Update the InstallPath and AppFileName with the actual path

    $appInfo.InstallPath = $appInfo.InstallPath -replace '\$\(ProgramFiles\)', $env:ProgramFiles
    $appInfo.AppFileName = $appInfo.AppFileName -replace '\$\(InstallPath\)', $appInfo.InstallPath
    $appInfo.AppFileName = $appInfo.AppFileName -replace '\$\(ProgramFiles\)', $env:ProgramFiles

    $appInfo.InstallCommand = $appInfo.InstallCommand -replace '\$\(Version\)', $appInfo.Version
    
    $appInfo.InstallArgs = $appInfo.InstallArgs -replace '\$\(InstallPath\)', $appInfo.InstallPath
    $appInfo.InstallArgs = $appInfo.InstallArgs -replace '\$\(Version\)', $appInfo.Version

    $appInfo.UninstallArgs = $appInfo.UninstallArgs -replace '\$\(InstallPath\)', $appInfo.InstallPath

    $appInfo.DownloadedFile = $appInfo.DownloadedFile -replace '\$\(Version\)', $appInfo.Version

    $appInfo.WebDownloadFile = $appInfo.WebDownloadFile -replace '\$\(Version\)', $appInfo.Version
    $appInfo.InstallCommand = $appInfo.InstallCommand -replace '\$\(DownloadedFile\)', $appInfo.DownloadedFile
    $appInfo.InstallArgs = $appInfo.InstallArgs -replace '\$\(DownloadedFile\)', $appInfo.DownloadedFile

    return $appInfo
}
function CheckIfAppInstalled {
    param (
        [Parameter(Mandatory=$true)]
        [string]$json
    )
   
    $appInfo = GetAppInfo $json
    #$appInfo.InstallPath = $appInfo.InstallPath -replace '\$\(ProgramFiles\)', $env:ProgramFiles
    #$appInfo.AppFileName = $appInfo.AppFileName -replace '\$\(InstallPath\)', $appInfo.InstallPath
    #$appInfo.AppFileName = $appInfo.AppFileName -replace '\$\(ProgramFiles\)', $env:ProgramFiles

    Write-Host "Checking if $($appInfo.Name) is installed at $($appInfo.AppFileName)..."

    # Check if the file exists using Test-Path
    if (Test-Path -Path $appInfo.AppFileName) {
        $currentVersion = GetFileVersion -FilePath $appInfo.AppFileName
        Write-Host "$($appInfo.Name) version $currentVersion is installed."

        # Compare the versions
        if ($currentVersion -eq $appInfo.Version) {
            Write-Host "$($appInfo.Name) version $currentVersion is up to date."
            return $true
        } else {
            Write-Host "$($appInfo.Name) version $currentVersion is not up to date. Expected version: $($appInfo.Version)."
            return $false
        }
    } else {
        Write-Host "$($appInfo.Name) is not installed."
        return $false
    }
}

function Install {
    param (
        [Parameter(Mandatory=$true)]
        [string]$json
    )

    # Get the app info
    $appInfo = GetAppInfo $json

    # Check if the app is already installed
    if (CheckIfAppInstalled $json) {
        Write-Host "$($appInfo.Name) is already installed and up to date."
        return $true
    }

    # Prepare the downloads directory
    Write-Host "Preparing downloads directory $downloadsDirectory..."
    PrepareDownloadsDirectory $downloadsDirectory

    # Download the app
    Write-Host "Downloading $($appInfo.Name) version $($appInfo.Version)..."
    $downloadedFile = "$downloadsDirectory\$($appInfo.DownloadedFile)"
    Write-Host "Downloading from $($appInfo.WebDownloadFile) to $downloadedFile..."
    Invoke-WebRequest -Uri $appInfo.WebDownloadFile -OutFile $downloadedFile

    # Check if the download was successful
    if (!(Test-Path -Path $downloadedFile)) {
        Write-Host "Error: Failed to download $($appInfo.Name)."
        return $false
    }

    Write-Host "Downloaded $($appInfo.Name) to $downloadedFile."

    # Create the installation directory if it doesn't exist
    $installDir = Split-Path -Path $appInfo.InstallPath -Parent
    if (!(Test-Path -Path $installDir)) {
        Write-Host "Creating installation directory $installDir..."
        New-Item -ItemType Directory -Path $installDir -Force | Out-Null
    }

    # Install the app based on the install command
    $installCommand = $appInfo.InstallCommand
    $installArgs = $appInfo.InstallArgs
    Write-Host "Installing $($appInfo.Name) using command: $installCommand $installArgs"
    & $installCommand $installArgs

    # Check if the installation was successful
    if (CheckIfAppInstalled $json) {
        Write-Host "$($appInfo.Name) installed successfully."
        return $true
    } else {
        Write-Host "Error: Failed to install $($appInfo.Name)."
        return $false
    }
}

function MainInstallApp {
    param (
        [Parameter(Mandatory=$true)]
        [string]$command,
        [Parameter(Mandatory=$true)]
        [string]$json
    )

    if ($command -eq "install") {
        return Install $json
    } else {
        Write-Host "Error: Invalid command '$command'. Valid commands are: install."
        return $false
    }
}

function CheckIfDotnetInstalled {
    param (
        [Parameter(Mandatory=$true)]
        [string]$version
    )

    # Check if the dotnet command is available
    try {
        $dotnetOutput = dotnet --list-runtimes
        if ($dotnetOutput -match $version) {
            Write-Host ".NET Core $version is installed."
            return $true
        } else {
            Write-Host ".NET Core $version is not installed."
            return $false
        }
    } catch {
        Write-Host ".NET Core is not installed."
        return $false
    }
}

function InstallDotNetCore {
    param (
        [Parameter(Mandatory=$true)]
        [string]$dotnetVersion
    )

    if (CheckIfDotnetInstalled $dotnetVersion) {
        Write-Host ".NET Core $dotnetVersion is already installed."
        return $true
    }

    Write-Host "Installing .NET Core $dotnetVersion..."

    # Define the installation script URL
    $installScript = "https://dot.net/v1/dotnet-install.ps1"

    # Download and execute the installation script
    try {
        Invoke-WebRequest -Uri $installScript -OutFile "dotnet-install.ps1"
        .\dotnet-install.ps1 -Version $dotnetVersion
        Remove-Item "dotnet-install.ps1"
    } catch {
        Write-Host "Error: Failed to install .NET Core $dotnetVersion."
        return $false
    }

    # Verify the installation
    if (CheckIfDotnetInstalled $dotnetVersion) {
        Write-Host ".NET Core $dotnetVersion installed successfully."
        return $true
    } else {
        Write-Host "Error: Failed to install .NET Core $dotnetVersion."
        return $false
    }
}

function AddDeploymentPathToEnvironment {
    param (
        [Parameter(Mandatory=$true)]
        [string]$deploymentPath
    )

    # Get the current PATH environment variable
    $currentPath = [System.Environment]::GetEnvironmentVariable("PATH", [System.EnvironmentVariableTarget]::Machine)

    # Check if the deployment path is already in the PATH
    if ($currentPath -split ';' -contains $deploymentPath) {
        Write-Host "Deployment path $deploymentPath is already in the PATH environment variable."
    } else {
        # Add the deployment path to the PATH environment variable
        $newPath = "$currentPath;$deploymentPath"
        [System.Environment]::SetEnvironmentVariable("PATH", $newPath, [System.EnvironmentVariableTarget]::Machine)
        Write-Host "Added deployment path $deploymentPath to the PATH environment variable."
    }
}

function InstallNtools {
    param (
        [Parameter(Mandatory=$false)]
        [string]$version,
        [Parameter(Mandatory=$false)]
        [string]$downloadsDirectory = "c:\NToolsDownloads"
    )

    # If version is not specified, read it from ntools.json
    if (-not $version) {
        $ntoolsJsonPath = Join-Path $PSScriptRoot "ntools.json"
        if (Test-Path $ntoolsJsonPath) {
            $ntoolsJson = Get-Content $ntoolsJsonPath | ConvertFrom-Json
            $version = $ntoolsJson.NbuildAppList[0].Version
            Write-Host "Version not specified. Using version $version from ntools.json."
        } else {
            throw "Version not specified and ntools.json not found."
        }
    }

    $deploymentPath = $env:ProgramFiles + "\NBuild"

    # Check if NTools is already installed
    $nbExePath = "$deploymentPath\nb.exe"
    if (Test-Path $nbExePath) {
        $currentVersion = GetFileVersion -FilePath $nbExePath
        if ($currentVersion -eq $version) {
            Write-Host "NTools version $currentVersion is already installed and up to date."
            return $true
        } else {
            Write-Host "NTools version $currentVersion is installed but not up to date. Expected version: $version."
        }
    }

    # Download NTools
    DownloadNtools -version $version -downloadsDirectory $downloadsDirectory

    # Check if the download was successful
    $downloadedFile = "$downloadsDirectory\$version.zip"
    if (!(Test-Path $downloadedFile)) {
        Write-Host "Error: Failed to download NTools version $version."
        return $false
    }

    # Create the deployment directory if it doesn't exist
    if (!(Test-Path $deploymentPath)) {
        Write-Host "Creating deployment directory $deploymentPath..."
        New-Item -ItemType Directory -Path $deploymentPath -Force | Out-Null
    }

    # Unzip the downloaded file to the deployment path
    try {
        Expand-Archive -Path $downloadedFile -DestinationPath $deploymentPath -Force -ErrorAction Stop
    } catch {
        throw "Failed to extract $downloadedFile : $($_.Exception.Message)"
    }
    # add deployment path to the PATH environment variable if it doesn't already exist
    AddDeploymentPathToEnvironment $deploymentPath

    Write-Host "NTools version $Version installed to $deploymentPath"
    # indicate success to callers
    return $true
}

function DownloadNtools {
    param (
        [Parameter(Mandatory=$true)]
        [string]$version,
        [Parameter(Mandatory=$false)]
        [string]$downloadsDirectory = "c:\NToolsDownloads"
    )
    
    # display parameters
    Write-Host "DownloadNtools - Parameters:"
    Write-Host "Downloading NTools version $version ..."
    Write-Host "Downloads directory: $downloadsDirectory"

    # Create the Downloads directory if it doesn't exist
    if (!(Test-Path -Path $downloadsDirectory)) {
        Write-Host "Creating downloads directory: $downloadsDirectory ..."
        New-Item -ItemType Directory -Path $downloadsDirectory | Out-Null
    }

    $url = "https://github.com/naz-hage/ntools/releases/download/$version/$version.zip"
    $fileName = "$downloadsDirectory\$version.zip"
    
    try {
            Invoke-WebRequest -Uri $url -OutFile $fileName -UseBasicParsing -ErrorAction Stop
    } catch {
            $msg = "Failed to download NTools version $version from $url : $($_.Exception.Message)"
            throw $msg
    }

    if (Test-Path $fileName) {
        Write-Host "Downloaded NTools version $version to $fileName"
    } else {
        Write-Host "Failed to download NTools version $version from $url"
    }
}

function SetDevEnvironmentVariables {
    param (
        [Parameter(Mandatory=$true)]
        [string]$devDrive,
        [Parameter(Mandatory=$true)]
        [string]$mainDir)

    # Set the environment variables for the current user
    [System.Environment]::SetEnvironmentVariable("devDrive", $devDrive, [System.EnvironmentVariableTarget]::User)
    [System.Environment]::SetEnvironmentVariable("mainDir", $mainDir, [System.EnvironmentVariableTarget]::User)

    # Read and display the environment variables
    $newDevDrive = [System.Environment]::GetEnvironmentVariable("devDrive", [System.EnvironmentVariableTarget]::User)
    $newMainDir = [System.Environment]::GetEnvironmentVariable("mainDir", [System.EnvironmentVariableTarget]::User)
    
    Write-OutputMessage $MyInvocation.MyCommand.Name "DevDrive set to '$newDevDrive' and MainDir set to '$newMainDir' successfully."
}

# Simple function to write output to the console with a new line
function Write-OutputMessage {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Prefix,
        [Parameter(Mandatory = $true)]
        [string]$Message,
        [Parameter(Mandatory = $false)]
        [System.ConsoleColor]$ForegroundColor = [System.ConsoleColor]::White,
        [Parameter(Mandatory = $false)]
        [switch]$NoNewline
    )

    $dateTime = Get-Date -Format "yyyy-MM-dd hh:mm tt"
    $formattedMessage = "[$Prefix] $Message"

    # ensure log file exists
    if (!(Test-Path -Path "install.log")) {
        # create empty log file
        New-Item -Path "install.log" -ItemType File -Force | Out-Null
    }

    if ($Message -eq "EmtpyLine") {
        Add-Content -Path "install.log" -Value ""
        if ($NoNewline) { Write-Host "" -NoNewline -ForegroundColor $ForegroundColor } else { Write-Host "" -ForegroundColor $ForegroundColor }
    } else {
        if ($NoNewline) {
            Write-Host $formattedMessage -ForegroundColor $ForegroundColor -NoNewline
        } else {
            Write-Host $formattedMessage -ForegroundColor $ForegroundColor
        }
        Write-Output ""
        Add-Content -Path "install.log" -Value "$dateTime | $Prefix | $Message"
    }
}

function GetFileVersion {
    param (
        [Parameter(Mandatory=$true)]
        [string]$FilePath
    )   
    try {
        $versionInfo = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($FilePath)
        return $versionInfo.FileVersion
    } catch {
        throw "Failed to get file version for $FilePath : $($_.Exception.Message)"
    }
}

<#
    Function: EnsureMinikubeIsRunning
    Description: Checks if Minikube is running and starts it if not.
    Troubleshooting: https://minikube.sigs.k8s.io/docs/drivers/docker/#Standard%20Docker
#>

function EnsureMinikubeIsRunning {
    # Check if Minikube is running
    Write-Host "Checking if Minikube is running..."
    $minikubeStatus = sudo minikube status | Select-String "host: Running"

    if ($minikubeStatus) {
        Write-Host "Minikube is already running."
    } else {
        Write-Host "Starting Minikube..."
        sudo minikube start --driver=docker
    }
}

Export-ModuleMember -Function *