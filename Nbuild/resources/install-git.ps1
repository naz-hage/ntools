# .\InstallPoershell.ps1

function GetAppInfo {
    param (
        [Parameter(Mandatory=$true)]
        [string]$jsonFile
    )
    # read file git.json and convert to json object
    $json = Get-Content -Path $jsonFile -Raw
    
    # $config = $json | ConvertFrom-Json
    # Retrieve elements using dot notation

    $config = $json | ConvertFrom-Json
        
        $appInfo = @{
            Name = $config.Name
            Version = $config.Version
            AppFileName = $config.AppFileName
            WebDownloadFile = $config.WebDownloadFile
            DownloadedFile = $config.DownloadedFile
            InstallCommand = $config.InstallCommand
            InstallArgs = $config.InstallArgs
            InstallPath = $config.InstallPath
        }
        
    return $appInfo
}

function CheckIfAppInstalled {
    param (
        [Parameter(Mandatory=$true)]
        [string]$json
    )
   
    $config = GetAppInfo $json 
    # check if app is installed
    $appFileName = $config.InstallPath + "\\" + $config.AppFileName
    # replace $(ProgramFiles) with environment variable
    $appFileName = $appFileName -replace '\$\(ProgramFiles\)', $env:ProgramFiles
     if (-not (Test-Path -Path $appFileName)) {
        Write-Host "$($config.Name) file: $appFileName is not found."
         return $false
     }
     else
     {
        # check if the version is correct
        $installedVersion = & .\file-version.ps1 $appFileName
        $targetVersion = $config.Version
        Write-Host "$config.Name  version: $config.Version is found."
        if ($installedVersion -ge $targetVersion) {
            return $false
        }
        else
        {
            return $true
        }
     }
}

function  Install {
    param (
        [Parameter(Mandatory=$true)]
        [string]$json
    )
     # Retrieve elements using dot notation
     $config = GetAppInfo $json

    # download the Git file
    $output = "C:\NToolsDownloads\" + $config.DownloadedFile
    # replace $(Version) with the version number
    $output = $output -replace '\$\(Version\)', $config.Version
    $webUri = $config.WebDownloadFile -replace '\$\(Version\)', $config.Version
    Invoke-WebRequest -Uri $webUri -OutFile $output

    # install the Git file
    $installCommand = $config.InstallCommand -replace '\$\(Version\)', $config.Version
    write-host "Installing $config.Name version: $config.Version ..."
    write-host "Install command: $installCommand"
    $installArgs=$config.InstallArgs
    write-host "Install arguments: $installArgs"
    $workingFolder = "C:\NToolsDownloads"
    $process = Start-Process -FilePath $installCommand -ArgumentList $installArgs -WorkingDirectory $workingFolder -PassThru
    $process.WaitForExit()

    # check if Git is installed
    $installed = CheckIfAppInstalled $json
    if ($installed) {
        Write-Host "Git version: $installed is installed successfully."
        return
    }
    else
    {
        Write-Host "Git version: $installed is not installed."
    }
}

function Main {

    # Retrieve elements using dot notation
    # $gitConfig = GetAppInfo $json
    # $version = $gitConfig.Version
    # $name = $gitConfig.Name
    # $appFileName = $gitConfig.AppFileName
    # $webDownloadFile = $gitConfig.WebDownloadFile
    # $downloadedFile = $gitConfig.DownloadedFile
    # $installCommand = $gitConfig.InstallCommand
    # $installArgs = $gitConfig.InstallArgs
    # $installPath = $gitConfig.InstallPath
    

    # Retrieve elements using square bracket notation
    #$gitConfig = GetAppInfo $json
    #$version = $gitConfig["Version"]
    #$webDownloadFile = $gitConfig["WebDownloadFile"]

    # check if Git is installed
    $installed = CheckIfAppInstalled "git.json"
    if ($installed) {
        Write-Host "Git version: $version is already installed."
    }
    else {
        Install $Json
    }
}

# Run the Main function
Main