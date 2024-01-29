# .\InstallNbuild.ps1

function InstallNBuildTools {
    # download the NBuildTools zip file 
    param (
        [Parameter(Mandatory=$true)]
        [string]$ntoolsversion
    )

    # check if NBuildTools are installed
    $nbuildInstalled = CheckIfNbuildInstalled $ntoolsversion
    if ($nbuildInstalled) {
        Write-Host "NBuildTools version: $ntoolsversion or higher are already installed."
        return
    }
    else
    {
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
}

function CheckIfNbuildInstalled {
    param (
        [Parameter(Mandatory=$true)]
        [string]$ntoolsversion
    )

    # check if nbuildtasks.dll exists in the deployment path
    $deploymentPath = $env:ProgramFiles + "\NBuild"
    $nbuildTasksPath = "$deploymentPath\nbuildtasks.dll"

    # check version of nbuildtasks.dll
    $nbuildTasksVersion =& .\file-version.ps1 $nbuildTasksPath
    write-host "Installed NBuild Version: $nbuildTasksVersion"

    if ($null -eq $nbuildTasksVersion) {
        write-host "NBuildTools: $ntoolsversion are not installed."
        return $false
    }
    else
    {
        write-host "NBuildTools: $ntoolsversion are installed."
        return $true
    }

}

function Main {

    $version = "1.1.0"
    # Install NBuildTools version 1.1.0
    InstallNBuildTools $version

    # set DevDrive and MainDir environment variables
    $devDrive = "C:"
    $mainDir = "source"
    setx DevDrive $devDrive
    setx MainDir $mainDir
}

# Run the Main function
Main


