
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
        $installedVersion = MainFileVersion -FilePath $appInfo.AppFileName
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

function  Install {
    param (
        [Parameter(Mandatory=$true)]
        [string]$json
    )
     # Retrieve elements using dot notation
     $appInfo = GetAppInfo $json

    # download the Git file
    $output = $downloadsDirectory + "\\" + $appInfo.DownloadedFile
    
    # replace $(Version) with the version number
    $output = $output -replace '\$\(Version\)', $appInfo.Version
    $webUri = $appInfo.WebDownloadFile -replace '\$\(Version\)', $appInfo.Version
    Write-Host "Downloading $($webUri) to : $($output) ..."
    Invoke-WebRequest -Uri $webUri -OutFile $output


    # install the Git file
    $installCommand = $appInfo.InstallCommand -replace '\$\(Version\)', $appInfo.Version
    write-host "Installing $($appInfo.Name) version: $($appInfo.Version) ..."
    write-host "Install command: $installCommand"
    $installArgs=$appInfo.InstallArgs
    write-host "Install arguments: $installArgs"
    $workingFolder = "C:\NToolsDownloads"
    $process = Start-Process -FilePath $installCommand -ArgumentList $installArgs -WorkingDirectory $workingFolder -PassThru
    $process.WaitForExit()

    # check if Git is installed
    $installed = CheckIfAppInstalled $json
    if ($installed) {
        Write-Host "App $($appInfo.Name) version: $($appInfo.Version) is installed successfully."
        return
    }
    else
    {
        Write-Host "App $($appInfo.Name) version: $($appInfo.Version) is not installed."
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

$downloadsDirectory = "c:\NToolsDownloads"

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

function MainInstallNtools {
    # prepare the downloads directory
    PrepareDownloadsDirectory $downloadsDirectory

    # add deployment path to the PATH environment variable if it doesn't already exist
    $deploymentPath = $env:ProgramFiles + "\NBuild"
    $path = [Environment]::GetEnvironmentVariable("PATH", "Machine")
    if ($path -notlike "*$deploymentPath*") {
        Write-OutputMessage $MyInvocation.MyCommand.Name "Adding $deploymentPath to the PATH environment variable."
        [Environment]::SetEnvironmentVariable("PATH", $path + ";$deploymentPath", "Machine")
    }
    else
    {
        Write-OutputMessage $MyInvocation.MyCommand.Name "$deploymentPath already exists in the PATH environment variable."
    }

    $nbExePath = "$deploymentPath\nb.exe"
    $nbToolsPath = "$deploymentPath\ntools.json"
    & $nbExePath -c install -json $nbToolsPath

    Write-OutputMessage $MyInvocation.MyCommand.Name "Completed successfully."
}

function SetDevEnvironmentVariables {
    param (
        [Parameter(Mandatory=$true)]
        [string]$devDrive,
        [Parameter(Mandatory=$true)]
        [string]$mainDir)

    Write-Host "devDrive: $devDrive"
    Write-Host "mainDir: $mainDir"
    
    # set DevDrive and MainDir environment variables
    setx DevDrive $devDrive
    setx MainDir $mainDir

    Write-OutputMessage $MyInvocation.MyCommand.Name "DevDrive and MainDir environment variables set successfully."
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


function MainFileVersion {
    param (
        [string]$FilePath
    )

    # Call GetFileVersion function with the specified path
    return GetFileVersion -FilePath $FilePath
}

Export-ModuleMember -Function PrepareDownloadsDirectory, GetAppInfo, CheckIfAppInstalled, Install, MainInstallApp, CheckIfDotnetInstalled, InstallDotNetCore, MainInstallNtools, SetDevEnvironmentVariables, Write-OutputMessage, GetFileVersion, MainFileVersion