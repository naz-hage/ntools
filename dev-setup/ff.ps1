# This script is used to search for a file in a folder.
# Usage: .\ff.ps1 -fileName "file.txt" -folderPath "C:\Temp" -recursive $true
# The script will search for "file.txt" in "C:\Temp" and all its subfolders.
param (
    [Parameter(Mandatory=$true)]
    [string]$fileName,

    [string]$folderPath = (Get-Location),

    [bool]$recursive = $false
)

if (Test-Path $folderPath) {
    Write-Output " ==> Searching for '$fileName' in '$folderPath' (recursive: $recursive)"
    $files = Get-ChildItem -Path $folderPath -Filter $fileName -File -ErrorAction SilentlyContinue -Recurse:($recursive)

    if ($files) {
        foreach ($file in $files) {
            $fileInfo = Get-Item $file.FullName
            Write-Host " ==> Found: $($file.FullName) (Date: $($fileInfo.LastWriteTime), Size: $($fileInfo.Length) bytes)" -ForegroundColor Green
        }
    } else {
        Write-Host " ==> No files found matching '$fileName' in '$folderPath'" -ForegroundColor Red
    }
} else {
    Write-Host " ==> Folder not found: $folderPath" -ForegroundColor Red
}

# SIG # Begin signature block
# MIIFrgYJKoZIhvcNAQcCoIIFnzCCBZsCAQExDzANBglghkgBZQMEAgEFADB5Bgor
# BgEEAYI3AgEEoGswaTA0BgorBgEEAYI3AgEeMCYCAwEAAAQQH8w7YFlLCE63JNLG
# KX7zUQIBAAIBAAIBAAIBAAIBADAxMA0GCWCGSAFlAwQCAQUABCB4ruQjK8UQBpTx
# omJLhM+vcU9O391qMbUYasoSC2+u7aCCAyQwggMgMIICCKADAgECAhA6olpcH/S2
# vE+o1FQfAltpMA0GCSqGSIb3DQEBCwUAMBoxGDAWBgNVBAMMD250b29scy1uYXot
# aGFnZTAeFw0yNDEwMTAwOTQ2MzdaFw0yNTEwMTAxMDA2MzdaMBoxGDAWBgNVBAMM
# D250b29scy1uYXotaGFnZTCCASIwDQYJKoZIhvcNAQEBBQADggEPADCCAQoCggEB
# AJqqeHmx0/EQeymyA+pmuwaDBej0Fy+oEdfHBcKpX9Mxtn7dP8VEPlojxWNuy/tR
# NcvW3gN4Td6N8OxoG7E/328C3yHfDfwko/6e1pFzojCREtBYievWsS1lByT+AMFq
# NL1m+93f3HePaBh7DIAuIWIg1Zco0djI6hFXqLCxtwMCOrauH/gsFhZOzTZyacHF
# pE1x1tlsHwQfrug/L/xW5uTgZFVNdwIt96QddFWY1gpAeiJyVRlv8tfuJ9B7oe/L
# +LvUOmj5HsWW1GA8QMMUgBozjytvO72jFqoHNQwj8g7NvT4LKNiJzCtpcwEdcwCO
# e4g855L0vowylvu5n98X7ukCAwEAAaNiMGAwDgYDVR0PAQH/BAQDAgeAMBMGA1Ud
# JQQMMAoGCCsGAQUFBwMDMBoGA1UdEQQTMBGCD250b29scy1uYXotaGFnZTAdBgNV
# HQ4EFgQUolZ5trJ3n7IbqP5ruWnvoJ4TqOYwDQYJKoZIhvcNAQELBQADggEBAAqT
# FqTQnU2INVUobxqj2pYScG2DwEsu8Dr5Scodx/UWR5DD043QcVcRYXHK9+SXkwA4
# WliZHo1HfddELhMnGs5Su2wsUpRGRztLQwwmGEvHfvsXXH+o4DKWUVaROEZPfRR3
# o4UrCCalGfB+zuwMY7VdCn51dUXflMmUpw8JsFwWuQCHPjgUafEyMTtPw3G/CYLs
# rWDoM3QLcz64vPDbrvFgBhxE7etKj2+BxSsqeUiLnUDPbBXRmxLLzOvjEx3F7XRc
# xhjJ3fN0bTGVSNkE/yUnWR/179ESVsIBp5pJ9Ep7/So6ML69CirMGDv0IZujJ1nJ
# w5TtJPH5xUPgtBkPW3wxggHgMIIB3AIBATAuMBoxGDAWBgNVBAMMD250b29scy1u
# YXotaGFnZQIQOqJaXB/0trxPqNRUHwJbaTANBglghkgBZQMEAgEFAKCBhDAYBgor
# BgEEAYI3AgEMMQowCKACgAChAoAAMBkGCSqGSIb3DQEJAzEMBgorBgEEAYI3AgEE
# MBwGCisGAQQBgjcCAQsxDjAMBgorBgEEAYI3AgEVMC8GCSqGSIb3DQEJBDEiBCB5
# jTfAYUtLIlbXTtrBlztyb4lNYuNVKZ8Yqy11F1vkdDANBgkqhkiG9w0BAQEFAASC
# AQBfCcITO2qP3BBUbrEgnlAKsZhq9RAK7D7jqG9mDIx2JXLSY2hCKPAgd/xQQJIQ
# U+/Hk2xLKZiF1OgBZGvyE4M2nIXK1G0LRp0OrjpvxNL8UKYdPNx0OaPldcnaeyxT
# TVKsaezAAUM6e2VrGCLk+nwzpa5AypUmtY9yW3LPatfM1N8lEJcGGRIjyiz+zBUf
# Ixdmo6PA3ayQQNTMClRSj19nXoCoSHQO2ZrSbDCnhnTOFs29wTbohwDUuGOlk5Uh
# g63WDmPkbFjZFZZZiBKfgPdoHGBy7GyKFDjLFrXd4kK3qjDL5V1PDFMagdfq9KX/
# M+XRSVKf4KxybH5VVQ/OogtH
# SIG # End signature block
