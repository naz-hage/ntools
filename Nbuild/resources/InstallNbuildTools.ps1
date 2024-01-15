
# To get PS Execution Policy to allow this script to run, run the following command in an elevated PS window:
# Set-ExecutionPolicy -ExecutionPolicy Unrestricted -Scope LocalMachine
# to run this script, run the following command in an elevated PS window:
# .\InstallMsbuild.ps1

# This script will download and install the latest version of the MSBuild tools
# It will also install the .NET 4.7.2 SDK if it is not already installed. Then is will install the NBuildTools

function Test-Administratorpriviledges {
    if (-not ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator))
    {
        Write-Host "Administrator priviledges are required to install Build Tools."
        return
    }
}

function InstallMsBuildTools {
    param (
        [Parameter(Mandatory=$true)]
        [string]$version
    )

    # Create the download directory if it doesn't exist
    CreateDownloadDirectory

    # Define the URL and the output file
    $url = "https://aka.ms/vs/$version/release/vs_BuildTools.exe"
    $output = "C:\NToolsDownloads\BuildTools_Full.exe"

    # Download the Build Tools file
    Invoke-WebRequest -Uri $url -OutFile $output

    # Install the MS Build tools
    Start-Process -FilePath $output -ArgumentList "--add",
        "Microsoft.VisualStudio.Workload.MSBuildTools",
        "Microsoft.NetCore.Component.SDK",
        "Microsoft.VisualStudio.Workload.ManagedDesktop",
        "Microsoft.VisualStudio.Workload.NetWeb",
        "--quiet" -Wait

    # check if installation was successful
    $msBuildToolsInstalled = CheckIfMsBuildToolsInstalled
    if ($msBuildToolsInstalled) {
        Write-Host "MS Build Tools installed successfully."
    }
    else
    {
        Write-Host "MS Build Tools installation failed."
    }
}

function CheckIfMsBuildToolsInstalled {
    # Check if the build tools are installed
    $msbuildPath = "C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\amd64\MSBuild.exe"
  
    if (-not (Test-Path -Path $msbuildPath)) {
        write-host "msbuildPath file does not exist."
        return $false
    }

    $msbuildVersion = .\GetFileVersion.ps1 $msbuildPath
    write-host "MSBuild Version: $msbuildVersion"

    if ($null -eq $msbuildVersion) {
        write-host "MS Build Tools are not installed."
        return $false
    }
    else
    {
        write-host "MS Build Tools are installed."
        return $true
    }

}

function CreateDownloadDirectory {
    # Create the directory if it doesn't exist
    if (!(Test-Path -Path "C:\NToolsDownloads")) {
        New-Item -ItemType Directory -Path "C:\NToolsDownloads"
    }
}

function InstallNBuildTools {
    # check if $msbuildPath file exists
    if (-not (Test-Path -Path $msbuildPath)) {
        Write-Host "msbuildPath file does not exist."
        return $false
    }

    # download the NBuildTools zip file 
    if (!(Test-Path -Path "C:\NToolsDownloads\NBuildTools.zip")) {
        Write-Host "NBuildTools zip file does not exist."
        return $false
    }
    else {
        # continue with installation
    }
    if (-not (Test-Path -Path $msbuildPath)) {
        Write-Host "msbuildPath file does not exist."
        return $false
    }

    $msbuildVersion = .\GetFileVersion.ps1 $msbuildPath

    write-host "MSBuild Version: $msbuildVersion"

    if ($null -eq $msbuildVersion) {
        Write-Host "Nbuild Tools are not installed."
        return $false
    }
    else
    {
        Write-Host "Nbuild Tools are installed."
        return $true
    }
}

function CreateDownloadDirectory {
    # Create the directory if it doesn't exist
    if (!(Test-Path -Path "C:\NToolsDownloads")) {
        New-Item -ItemType Directory -Path "C:\NToolsDownloads"
    }
}

function InstallNBuildTools {
    # download the NBuildTools zip file 
    param (
        [Parameter(Mandatory=$true)]
        [string]$ntoolsversion
    )
    $url= "https://github.com/naz-hage/ntools/releases/download/$ntoolsversion/$ntoolsversion.zip"
        $output = "C:\NToolsDownloads\NBuildTools.zip"
        Invoke-WebRequest -Uri $url -OutFile $output
   
        $deploymentPath = $env:ProgramFiles + "\NBuild"
        
        # unzip the NBuildTools zip file
        Expand-Archive -Path $output -DestinationPath $deploymentPath

        # check if nbuildtasks.dll exists in the deployment path
        $nbuildTasksPath = "$deploymentPath\nbuildtasks.dll"
        if (Test-Path -Path $nbuildTasksPath) {
            Write-Host "nbuildtasks.dll exists."

            # add deployment path to the PATH environment variable if it doesn't already exist
            $path = [Environment]::GetEnvironmentVariable("PATH", "Machine")
            if ($path -notlike "*$deploymentPath*") {
                Write-Host "Adding $deploymentPath to the PATH environment variable."
                [Environment]::SetEnvironmentVariable("PATH", $path + ";$deploymentPath", "Machine")
            }
            else
            {
                Write-Host "$deploymentPath already exists in the PATH environment variable."
            }
        }
        else
        {
            Write-Host "nbuildtasks.dll does not exist."
        }
    }

    function Main {
        # Check if the script is running as an administrator. If not, exit.
        Test-Administratorpriviledges

       $msBuildToolsInstalled = CheckIfMsBuildToolsInstalled
       if ($msBuildToolsInstalled) {
       Write-Host "MS Build Tools is already installed."
   }
   else
   {
        Write-Host "Installing MS BuildTools version 17 (Visual Studio 2022)"
        InstallMsBuildTools 17
   }

   # Install NBuildTools version 1.1.0
   #InstallNBuildTools 1.1.0

   # set DevDrive and MainDir environment variables
    $devDrive = "C:"
    $mainDir = "src"
    setx DevDrive $devDrive
    setx MainDir $mainDir  
}

# Run the Main function
Main


    

