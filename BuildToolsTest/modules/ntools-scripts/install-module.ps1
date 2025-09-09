param(
    [string]$BuildTools = "$env:ProgramFiles\nbuild",
    [string]$ModuleName = 'ntools-scripts'
)

$scriptRoot = Split-Path -Parent $PSCommandPath
$src = Join-Path $scriptRoot ''
$dest = Join-Path $BuildTools "modules\$ModuleName"

Write-Host "Installing module from $src to $dest"
if (-not (Test-Path $dest)) { New-Item -ItemType Directory -Force -Path $dest | Out-Null }

# Copy module package files
Get-ChildItem -Path $src -File | ForEach-Object {
    $destFile = Join-Path $dest $_.Name
    if ($_.Extension -eq '.psm1' -or $_.Extension -eq '.ps1' -or $_.Extension -eq '.psd1') {
        # For PowerShell files, preserve encoding by reading/writing as UTF8
        $content = Get-Content -Path $_.FullName -Raw -Encoding UTF8
        Set-Content -Path $destFile -Value $content -Encoding UTF8 -Force
    } else {
        # For other files, use regular copy
        Copy-Item -Path $_.FullName -Destination $destFile -Force
    }
}

# Also copy repo modules (if present) to the module folder to ensure helpers are available
$repoModules = Join-Path (Join-Path $scriptRoot '..') 'modules'
if (Test-Path $repoModules) {
    Write-Host "Copying repo modules from $repoModules to $dest"
    Get-ChildItem -Path $repoModules -Filter '*.psm1' -File | ForEach-Object {
        $destFile = Join-Path $dest $_.Name
        # For PowerShell files, preserve encoding by reading/writing as UTF8
        $content = Get-Content -Path $_.FullName -Raw -Encoding UTF8
        Set-Content -Path $destFile -Value $content -Encoding UTF8 -Force
    }
}

Write-Host "Module installed to $dest"
Write-Host "You can import it with: Import-Module '$dest\$ModuleName.psd1' -Force"

