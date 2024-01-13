
# To get PS Execution Policy to allow this script to run, run the following command in an elevated PS window:
# Set-ExecutionPolicy -ExecutionPolicy Unrestricted -Scope LocalMachine
# to run this script, run the following command in an elevated PS window:
# .\InstallMsbuild.ps1

# This script will download and install the latest version of the MSBuild tools
# It will also install the .NET 4.7.2 SDK if it is not already installed

function Test-Administratorpriviledges {
    if (-not ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator))
    {
        Write-Output "Administrator priviledges are required to install Build Tools."
        return
    }
}

function InstallMsBuildTools {

    # Create the download directory if it doesn't exist
    CreateDownloadDirectory

    # Define the URL and the output file
    $url = "https://aka.ms/vs/17/release/vs_BuildTools.exe"
    $output = "C:\NToolsDownloads\BuildTools_Full.exe"

    # Download the Build Tools file
    Invoke-WebRequest -Uri $url -OutFile $output

    # Install the build tools
    Start-Process -FilePath $output -ArgumentList "--add","Microsoft.VisualStudio.Workload.MSBuildTools","--quiet" -Wait

    # check if installation was successful byn
    $buildToolsInstalled = CheckIfBuildToolsInstalled
    if ($buildToolsInstalled) {
        Write-Output "Build Tools installed successfully."
    }
    else
    {
        Write-Output "Build Tools installation failed."
    }
}

function CheckIfBuildToolsInstalled {
    # Check if the build tools are installed
    $msbuildPath = "C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\amd64\MSBuild.exe"
    $msbuildVersion = .\GetFileVersion.ps1 $msbuildPath

    write-host "MSBuild Version: $msbuildVersion"

    if ($null -eq $msbuildVersion) {
        Write-Output "Build Tools are not installed."
        return $false
    }
    else
    {
        Write-Output "Build Tools are installed."
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
    $ntoolsversion = "1.1.0"
    $url= "https://github.com/naz-hage/ntools/releases/download/$ntoolsversion/$ntoolsversion.zip"
        $output = "C:\NToolsDownloads\NBuildTools.zip"
        Invoke-WebRequest -Uri $url -OutFile $output
   
        $deploymentPath = $env:ProgramFiles + "\NBuild"
        
        # unzip the NBuildTools zip file
        Expand-Archive -Path $output -DestinationPath $deploymentPath

        # check if nbuildtasks.dll exists in the deployment path
        $nbuildTasksPath = "$deploymentPath\nbuildtasks.dll"
        if (Test-Path -Path $nbuildTasksPath) {
            Write-Output "nbuildtasks.dll exists."

            # add deployment path to the PATH environment variable if it doesn't already exist
            $path = [Environment]::GetEnvironmentVariable("PATH", "Machine")
            if ($path -notlike "*$deploymentPath*") {
                Write-Output "Adding $deploymentPath to the PATH environment variable."
                [Environment]::SetEnvironmentVariable("PATH", $path + ";$deploymentPath", "Machine")
            }
            else
            {
                Write-Output "$deploymentPath already exists in the PATH environment variable."
            }
        }
        else
        {
            Write-Output "nbuildtasks.dll does not exist."
        }
    }

    function Main {
        Test-Administratorpriviledges

        # Get file version of the MSBuild.exe file
        # "c:\Program Files\Microsoft Visual Studio\2022\Preview\Msbuild\Current\Bin\amd64\MSBuild.exe"
        # "C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\amd64\MSBuild.exe"
       # check if installation was successful byn
       $buildToolsInstalled = CheckIfBuildToolsInstalled
       if ($buildToolsInstalled) {
       Write-Output "Build Tools is already installed."
   }
   else
   {
        InstallMsBuildTools
   }

   # Install NBuildTools
   InstallNBuildTools
}

Main


    

