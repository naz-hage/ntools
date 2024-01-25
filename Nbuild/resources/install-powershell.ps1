# .\InstallPoershell.ps1

function CheckIfPowerShellInstalled {
    param (
        [Parameter(Mandatory=$true)]
        [string]$powershellVersion
    )
    
    $installedVersion = $PSVersionTable.PSVersion.ToString()
    Write-Host "Installed PowerShell version: $installedVersion"
    # check if the installed version is greater than or equal to the required version
    if ([version]$installedVersion -ge [version]$powershellVersion) {
        return $true
    }
    else
    {
        return $false
    }   
}


function  InstallPowerShell {
    param (
        [Parameter(Mandatory=$true)]
        [string]$powershellVersion
    )
    # "https://github.com/PowerShell/PowerShell/releases/download/v7.4.1/PowerShell-7.4.1-win-x64.msi"
    # "https://github.com/PowerShell/PowerShell/releases/download/7.4.1/PowerShell-7.4.1-win-x64.msi"
    # Install PowerShell 7.4.1
    $powershellFile = "PowerShell-$powershellVersion-win-x64.msi"
    $powershellUrl = "https://github.com/PowerShell/PowerShell/releases/download/v$powershellVersion/$powershellFile"

    # download the PowerShell file
    $powershellOutput = "C:\NToolsDownloads\" + $powershellFile
    Invoke-WebRequest -Uri $powershellUrl -OutFile $powershellOutput

    # install the PowerShell file
    Start-Process -FilePath msiexec.exe -ArgumentList "/i $powershellOutput /quiet" -Wait

    # check if PowerShell is installed
    $powershellInstalled = CheckIfPowerShellInstalled $powershellVersion
    if ($powershellInstalled) {
        Write-Host "PowerShell version: $powershellVersion is already installed."
        return
    }
    else
    {
        Write-Host "PowerShell version: $powershellVersion is not installed."
    }
}

function Main {
    
    $version = "7.4.1"
    $powershellInstalled = CheckIfPowerShellInstalled $version
    if ($powershellInstalled) {
        Write-Host "PowerShell version: $version is already installed."
    }
    else
    {
        Write-Host "Installing PowerShell version: $version ..."
        InstallPowerShell $version
    }
}

# Run the Main function
Main