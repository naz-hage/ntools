
function GetFileVersion {
    param (
        [Parameter(Mandatory=$true)]
        [string]$FilePath
    )   

    $versionInfo = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($FilePath)
    $versionInfo.FileVersion

    # Return the version info object
    #return $versionInfo.FileVersion
}

function Main {
    param (
        [string]$FilePath
    )

    # Call Get-FileVersions function with the specified path
    
    GetFileVersion -FilePath $FilePath
}

Main -FilePath $args[0]


