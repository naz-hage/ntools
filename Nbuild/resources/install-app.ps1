# .\Install-app.ps1

$downloadsDirectory = "c:\NToolsDownloads"

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
        $installedVersion = & .\file-version.ps1 $appInfo.AppFileName
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

function Main {
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
}

# Run the Main function
Main -command $args[0] -json $args[1]