param (
    [Parameter(Mandatory = $true)]
    [string]$FilePath
)

if (-Not (Test-Path -Path $FilePath)) {
    Write-Error "File '$FilePath' does not exist."
    exit 1
}

try {
    $fileStream = [System.IO.File]::OpenRead($FilePath)
    $sha256 = [System.Security.Cryptography.SHA256]::Create()
    $hashBytes = $sha256.ComputeHash($fileStream)
    $fileStream.Close()

    $hashString = [BitConverter]::ToString($hashBytes) -replace '-', ''
    Write-Output "SHA-256 Hash: $hashString"
} catch {
    Write-Error "An error occurred: $_"
    exit 1
}
