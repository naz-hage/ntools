# PowerShell script to publish all .NET projects in the repo to a single output directory
param(
    [Parameter(Mandatory=$true)]
    [ValidateNotNullOrEmpty()]
    [string]$PublishDir 
)
New-Item -ItemType Directory -Force -Path $PublishDir | Out-Null
# Exclude test projects by filtering out paths that contain 'test' (case-insensitive)
Get-ChildItem -Path $PSScriptRoot -Filter *.csproj -Recurse |
    Where-Object { $_.FullName -notmatch '(?i)test' } |
    ForEach-Object {
        Write-Host "Publishing $($_.FullName) to $PublishDir"
        dotnet publish $_.FullName -c Release -o $PublishDir
    }
Write-Host "All non-test projects published to $PublishDir"
