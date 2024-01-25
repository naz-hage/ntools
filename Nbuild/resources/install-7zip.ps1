
# To get PS Execution Policy to allow this script to run, run the following command in an elevated PS window:
# Set-ExecutionPolicy -ExecutionPolicy Unrestricted -Scope LocalMachine
# to run this script, run the following command in an elevated PS window:
# .\Install7zip.ps1

# This script will download and install the latest version of the 7zip

function Test-Administratorpriviledges {
    if (-not ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator))
    {
        Write-Host "Administrator priviledges are required to install Build Tools."
        return
    }
}
function CheckIf7zipInstalled {
    param (
        [Parameter(Mandatory=$true)]
        [string]$version
    )
    
    # check if 7zip is installed
    $sevenZipPath = "c:\Program Files\7-Zip\7z.exe"
     if (-not (Test-Path -Path $sevenZipPath)) {
         Write-Host "7zip file: $sevenZipPath is not found."
         return $false
     }
     else
     {
        # check if the version is correct
        $sevenZipVersion = .\GetFileVersion.ps1 $sevenZipPath
        Write-Host "7zip version: $sevenZipVersion is found."
        if ($sevenZipVersion -ne $version) {
            return $false
        }
        else
        {
            return $true
        }
     }
}

function Install7zip {
    param (
        [Parameter(Mandatory=$true)]
        [string]$version
    )

    
    # check if 7zip is installed successfully
    $sevenZipInstalled = CheckIf7zipInstalled $version
    if ($sevenZipInstalled) {
        Write-Host "7zip installed successfully."
    }
    else
    {
        # remove the `.` from the version number
        $version = $version.Replace(".", "")
        
        # download the 7zip zip file https://www.7-zip.org/a/7z2301-x64.exe
        $url = "https://www.7-zip.org/a/7z$version-x64.exe"
        $output = "C:\NToolsDownloads\7zip.exe"
        Invoke-WebRequest -Uri $url -OutFile $output

        # check if the file is downloaded successfully
        if (-not (Test-Path -Path $output)) {
            Write-Host "7zip download failed."
            return
        }
        else
        {
            Write-Host "7zip downloaded successfully."
            #display the file version
            $sevenZipVersion = .\GetFileVersion.ps1 $output
            Write-Host "Downloaded 7zip version: $sevenZipVersion"
        }   

        # install 7zip
        Write-Host "Installing 7zip..."
        Start-Process -FilePath $output -ArgumentList '/S', '/D="c:\Program Files\7-Zip"' -Wait
    }
}

function Main {
    # Check if the script is running as an administrator. If not, exit.
    Test-Administratorpriviledges

    $version = "23.01"
    # Install 7zip version 23.01
    Install7zip $version

    # check if 7zip is installed successfully
    $sevenZipInstalled = CheckIf7zipInstalled $version
    if ($sevenZipInstalled) {
        Write-Host "7zip version: $version installed successfully."
        return $true
    }
    else
    {
        Write-Host "7zip installation failed."
    }
}

# Run the Main function
Main


    

