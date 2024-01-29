# .\InstallNbuild.ps1

$downloadsDirectory = "c:\NToolsDownloads"

function PrepareDownloadsDirectory {

    # Create the Downloads directory if it doesn't exist
    if (!(Test-Path -Path $downloadsDirectory)) {
        New-Item -ItemType Directory -Path $downloadsDirectory | Out-Null
    }

    # Grant Administrators full control of the Downloads directory
    icacls.exe $downloadsDirectory /grant 'Administrators:(OI)(CI)F' /inheritance:r
    
}
function InstallNtools {
    # download the Ntools zip file 
    param (
        [Parameter(Mandatory=$true)]
        [string]$ntoolsversion
    )

    # check if Ntools are installed
    $nbuildInstalled = CheckIfNbuildInstalled $ntoolsversion
    if ($nbuildInstalled) {
        Write-Host "Ntools version: $ntoolsversion or higher are already installed."
        return
    }
    else
    {
        $url= "https://github.com/naz-hage/ntools/releases/download/$ntoolsversion/$ntoolsversion.zip"
        $output = "C:\NToolsDownloads\Ntools.zip"
        Invoke-WebRequest -Uri $url -OutFile $output
   
        $deploymentPath = $env:ProgramFiles + "\NBuild"
        
        # unzip the Ntools zip file
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
        write-host "Ntools: $ntoolsversion are not installed."
        return $false
    }
    else
    {
        return $true
    }
}

function CheckIfDotnetInstalled {
    param (
        [Parameter(Mandatory=$true)]
        [string]$dotnetVersion
    )

    # check if nbuildtasks.dll exists in the deployment path
    $InstalledDotnetVersion = (Get-Command dotnet -ErrorAction SilentlyContinue).Version
    Write-Host ".NET Core Version: $InstalledDotnetVersion is installed."

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

function Main {
    param (
        [Parameter(Mandatory=$true)]
        [string]$dotnetVersion,
        [Parameter(Mandatory=$true)]
        [string]$nToolsVersion,
        [Parameter(Mandatory=$true)]
        [string]$devDrive,
        [Parameter(Mandatory=$true)]
        [string]$mainDir)

    # prepare the downloads directory
    PrepareDownloadsDirectory $downloadsDirectory

    # Update the .NET Core version
    InstallDotNetCore $dotnetVersion

    Write-Host "Ntools Version: $nToolsVersion"
    Write-Host "devDrive: $devDrive"
    Write-Host "mainDir: $mainDir"
    
    # set DevDrive and MainDir environment variables
    setx DevDrive $devDrive
    setx MainDir $mainDir

    # Install Ntools
    InstallNtools $dotnetVersion

    $nbExePath = "$env:ProgramFiles\Nbuild\nb.exe"
    & $nbExePath -c install -json ntools.json 
}

# Call the Main function with the provided or default values
Main -dotnetVersion $args[0] -nToolsVersion $args[1] -devDrive $args[2] -mainDir $args[3]

