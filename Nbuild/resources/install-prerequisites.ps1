
# To get PS Execution Policy to allow this script to run, run the following command in an elevated PS window:
# Set-ExecutionPolicy -ExecutionPolicy Unrestricted -Scope LocalMachine
# to run this script, run the following command in an elevated PS window:
# .\InstallPrerequisites.ps1

# This script will download and install the Prerequisites tools for a dev environment

function Test-Administratorpriviledges {
    if (-not ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator))
    {
        Write-Host "Administrator priviledges are required to install Build Tools."
        return
    }
}

function CreateDownloadDirectory {
    # Create the directory if it doesn't exist
    if (!(Test-Path -Path "C:\NToolsDownloads")) {
        New-Item -ItemType Directory -Path "C:\NToolsDownloads"
    }
}

function Main {
    # Check if the script is running as an administrator. If not, exit.
    Test-Administratorpriviledges

    # Install 7zip version 23.01
    .\install-7zip.ps1 23.01

    # Install MSBuildTools version 17    
    .\install-vs-build-tools 17
   
   # Install NTools version 1.1.0
   .\install-ntools.ps1 1.1.0
}

# Run the Main function
Main


    

