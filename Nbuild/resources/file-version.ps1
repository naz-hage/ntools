
function GetFileVersion {
    param (
        [Parameter(Mandatory=$true)]
        [string]$FilePath
    )   

    $versionInfo = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($FilePath)
    # return the all file version parts joined by a dot
    return ($versionInfo.FileMajorPart, $versionInfo.FileMinorPart, $versionInfo.FileBuildPart, $versionInfo.FilePrivatePart) -join "."
}


function Main {
    param (
        [string]$FilePath
    )

    # Call GetFileVersion function with the specified path
    return GetFileVersion -FilePath $FilePath
}
    
# Call Main function with the first argument
Main -FilePath $args[0]


