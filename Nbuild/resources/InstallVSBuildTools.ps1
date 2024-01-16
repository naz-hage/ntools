
function InstallMsBuildTools {
    param (
        [Parameter(Mandatory=$true)]
        [string]$version
    )

    # Create the download directory if it doesn't exist
    CreateDownloadDirectory

    $msBuildToolsInstalled = CheckIfMsBuildToolsInstalled
    if ($msBuildToolsInstalled) {
    Write-Host "MS Build Tools is already installed."
   }
   else
   {
        Write-Host "Installing MS BuildTools version: $version (Visual Studio 2022)"
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

function Main {
    # Check if the script is running as an administrator. If not, exit.
    Test-Administratorpriviledges

    $version = "17"
    # Install MSBuildTools version 17
    InstallMsBuildTools $version

    # check if MSBuildTools is installed successfully
    $msBuildToolsInstalled = CheckIfMsBuildToolsInstalled
    if ($msBuildToolsInstalled) {
        Write-Host "MSBuildTools version: $version installed successfully."
        return $true
    }
    else
    {
        Write-Host "MSBuildTools installation failed."
    }

}

# Run the Main function
Main


