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
    | MainInstallNtools           | Main function to install NTools.                              |
    | SetDevEnvironmentVariables  | Sets development environment variables.                       |
    | Write-OutputMessage         | Writes output messages to the console and log files.          |
    | GetFileVersion              | Retrieves the file version of a specified file.               |
    | EnsureMinikubeRunning       | Checks if Minikube is running and starts it if not.           |

.EXAMPLE
    Import-Module ./install.psm1
    PrepareDownloadsDirectory -directory "C:\NToolsDownloads"
    $appInfo = GetAppInfo -json "C:\NToolsDownloads\ntools.json"
    $isInstalled = CheckIfAppInstalled -json "C:\NToolsDownloads\ntools.json"
    Install -json "C:\NToolsDownloads\ntools.json"
    SetDevEnvironmentVariables -devDrive "D:" -mainDir "C:\MainDir"
    InstallDotNetCore -dotnetVersion "3.1.0"

    MainInstallApp -command install -json "C:\NToolsDownloads\ntools.json"
    MainInstallNtools
    SetDevEnvironmentVariables -devDrive "D:" -mainDir "C:\MainDir"
    Write-OutputMessage -Prefix "Info" -Message "Installation completed successfully."
    GetFileVersion -FilePath "C:\Program Files\NBuild\nb.exe"

.NOTES

    adding the deployment path to the PATH environment variable
    $path = [Environment]::GetEnvironmentVariable("PATH", "Machine")
    if ($path -notlike "*$deploymentPath*") {
        Write-OutputMessage $MyInvocation.MyCommand.Name "Adding $deploymentPath to the PATH environment variable."
        [Environment]::SetEnvironmentVariable("PATH", $path + ";$deploymentPath", "Machine")
    }
    else {
        Write-OutputMessage $MyInvocation.MyCommand.Name "$deploymentPath already exists in the PATH environment variable."
    }

#>
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
    #$appFileName = $appInfo.AppFileName -replace '\$\(InstallPath\)', $appInfo.InstallPath
    #$appFileName = $appFileName -replace '\$\(ProgramFiles\)', $env:ProgramFiles
    # check if app is installed
     if (-not (Test-Path -Path $appInfo.AppFileName)) {
        Write-Host "$($appInfo.Name) file: $($appInfo.AppFileName) is not found."
         return $false
     }
     else
     {
        # check if the version is correct
        #$installedVersion = & .\file-version.ps1 $appInfo.AppFileName
        $installedVersion = GetFileVersion -FilePath $appInfo.AppFileName
        $targetVersion = $appInfo.Version
        Write-Host "$($appInfo.Name)  version: $($appInfo.Version) is found."

        # check if the installed version is greater than or equal to the target version
        if ([version]$installedVersion -ge [version]$targetVersion) {
            return $true
        }
        else
        {
            return $false
        }
     }
}

function Install {
    param (
        [Parameter(Mandatory=$true)]
        [string]$json
    )
    # Retrieve elements using dot notation
    $appInfo = GetAppInfo $json

    # download the App
    $output = $downloadsDirectory + "\\" + $appInfo.DownloadedFile
    # replace $(Version) with the version number
    $output = $output -replace '\$\(Version\)', $appInfo.Version
    $webUri = $appInfo.WebDownloadFile -replace '\$\(Version\)', $appInfo.Version
    Write-Host "Downloading $($webUri) to : $($output) ..."
    Invoke-WebRequest -Uri $webUri -OutFile $output

    #Install the App
    $installCommand = $appInfo.InstallCommand -replace '\$\(Version\)', $appInfo.Version
    Write-Host "Installing $($appInfo.Name) version: $($appInfo.Version) ..."
    Write-Host "Install command: $installCommand"
    $installArgs = $appInfo.InstallArgs
    Write-Host "Install arguments: $installArgs"
    Write-Host "App file Name: $($appInfo.AppFileName) ..."

    $timeout = New-TimeSpan -Minutes 10
    $sw = [Diagnostics.Stopwatch]::StartNew()

    Start-Process -FilePath $installCommand -ArgumentList $installArgs -WorkingDirectory $downloadsDirectory -Wait

    # Wait for App to be installed or timeout
    while (!(Test-Path $appInfo.AppFileName) -and $sw.Elapsed -lt $timeout) {
        Start-Sleep -Seconds 5
    }

    if ($sw.Elapsed -ge $timeout) {
        Write-Output "Installation timed out."
        return $false
    } else {
        Write-Output "Installation completed."
    }

    $installed = CheckIfAppInstalled $json
    if ($installed) {
        Write-Host "App $($appInfo.Name) version: $($appInfo.Version) is installed successfully."
        return $true
    } else {
        Write-Host "App $($appInfo.Name) version: $($appInfo.Version) is not installed."
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

    PrepareDownloadsDirectory $downloadsDirectory

    $app = GetAppInfo $json
    # check if Git is installed
    $installed = CheckIfAppInstalled $json
    if ($installed) {
        Write-Host "App: $($app.Name) version: $($app.Version) or greater is already installed."
    }
    else {
        Install $json
    }
    Write-OutputMessage $MyInvocation.MyCommand.Name "Installation of $($app.Name) completed successfully."
}

function CheckIfDotnetInstalled {
    param (
        [Parameter(Mandatory=$true)]
        [string]$dotnetVersion
    )

    # check if nbuildtasks.dll exists in the deployment path
    $InstalledDotnetVersion = (Get-Command dotnet -ErrorAction SilentlyContinue).Version
    Write-Host ".NET Core Version: $InstalledDotnetVersion is installed."

    if ([string]::IsNullOrEmpty($InstalledDotnetVersion)) {
        return $false
    }
    if ([version]$InstalledDotnetVersion -ge [version]$dotnetVersion) {
        return $true
    }
    else
    {
        return $false
    }
}

function InstallDotNetCore {
    param(
        [Parameter(Mandatory=$true)]
        [string]$dotnetVersion)
   
    # Check if .NET Core is installed
    if (CheckIfDotnetInstalled $dotnetVersion) {
        return
    }   
    else
    {
        $dotnetInstallerUrl = "https://download.visualstudio.microsoft.com/download/pr/f18288f6-1732-415b-b577-7fb46510479a/a98239f751a7aed31bc4aa12f348a9bf/windowsdesktop-runtime-%dotnetVersion%-win-x64.exe"

        # Path where the installer will be downloaded
        $installerPath = Join-Path -Path $downloadsDirectory -ChildPath "dotnet_installer.exe"

        # Download the installer if it doesn't already exist
        if (!(Test-Path -Path $installerPath)) {
            Invoke-WebRequest -Uri $dotnetInstallerUrl -OutFile $installerPath
        }
    
        # Run the installer
        Start-Process -FilePath $installerPath -ArgumentList "/quiet", "/norestart" -NoNewWindow -Wait
    }   
}

    function AddDeploymentPathToEnvironment {
        param (
            [Parameter(Mandatory=$true)]
            [string]$deploymentPath
        )

        $path = [Environment]::GetEnvironmentVariable("PATH", "Machine")
        if ($path -notlike "*$deploymentPath*") {
            Write-OutputMessage $MyInvocation.MyCommand.Name "Adding $deploymentPath to the PATH environment variable."
            [Environment]::SetEnvironmentVariable("PATH", $path + ";$deploymentPath", "Machine")
        }
        else {
            Write-OutputMessage $MyInvocation.MyCommand.Name "$deploymentPath already exists in the PATH environment variable."
        }
    }


function MainInstallNtools {
    # prepare the downloads directory
    PrepareDownloadsDirectory $downloadsDirectory

    # add deployment path to the PATH environment variable if it doesn't already exist
    AddDeploymentPathToEnvironment $deploymentPath

    & $global:NbExePath -c install -json $nbToolsPath

    Write-OutputMessage $MyInvocation.MyCommand.Name "Completed successfully."
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
        [String]
        $Prefix,
        [Parameter(Mandatory = $true)]
        [String]
        $Message
    )

    $dateTime = Get-Date -Format "yyyy-MM-dd hh:mm tt"

    
    
    # append to the log file install.log
    if (!(Test-Path -Path "install.log")) {
        New-Item -ItemType File -Path "install.log" -Force
    }

    if ($Message -eq "EmtpyLine") {
        Add-Content -Path "install.log" -Value ""
        Write-Output ""
    } else {
        Write-Output "$dateTime $Prefix : $Message"
        Write-Output ""
        Add-Content -Path "install.log" -Value "$dateTime | $Prefix | $Message"
    }
}

function GetFileVersion {
    param (
        [Parameter(Mandatory=$true)]
        [string]$FilePath
    )   

    $versionInfo = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($FilePath)
    # return the all file version parts joined by a dot
    return ($versionInfo.FileMajorPart, $versionInfo.FileMinorPart, $versionInfo.FileBuildPart, $versionInfo.FilePrivatePart) -join "."
}

    # Call GetFileVersion function with the specified path
    return GetFileVersion -FilePath $FilePath
}


<#
    Function: EnsureMinikubeRunning
    Description: Checks if Minikube is running and starts it if not.
#>

function EnsureMinikubeRunning {
    # Check if Minikube is running
    $minikubeStatus = sudo minikube status | Select-String "host: Running"

    if ($minikubeStatus) {
        Write-Host "Minikube is already running."
    } else {
        Write-Host "Starting Minikube..."
        sudo minikube start --driver=hyperv --cpus=2 --memory=6144 --disk-size=20g
    }
}

Export-ModuleMember -Function *