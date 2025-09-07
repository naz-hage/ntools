param (
    [string]$filePath
)

if (Test-Path $filePath) {
    $fileVersionInfo = Get-Item $filePath | Select-Object -ExpandProperty VersionInfo
    Write-Output "File: $filePath"
    Write-Output "File Version: $($fileVersionInfo.FileVersion)"
    Write-Output "Product Version: $($fileVersionInfo.ProductVersion)"
} else {
    Write-Output "File not found: $filePath"
}