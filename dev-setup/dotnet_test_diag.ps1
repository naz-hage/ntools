param(
    [Parameter(Mandatory=$true)][string]$ProjectPath,
    [Parameter(Mandatory=$false)][string]$DiagPath
)

if (-not (Test-Path $ProjectPath)) {
    Write-Error "Project file not found: $ProjectPath"
    exit 2
}

if (-not $DiagPath) {
    $projName = [System.IO.Path]::GetFileNameWithoutExtension($ProjectPath)
    $DiagPath = Join-Path -Path (Join-Path (Split-Path $ProjectPath -Parent) '..\TestResults') -ChildPath "$projName-diag.txt"
}

$DiagPath = Resolve-Path -LiteralPath $DiagPath -ErrorAction SilentlyContinue | ForEach-Object { $_.ProviderPath } 
if (-not $DiagPath) { $DiagPath = $DiagPath }

$diagDir = Split-Path -Parent $DiagPath
if (-not (Test-Path $diagDir)) { New-Item -ItemType Directory -Path $diagDir -Force | Out-Null }

Write-Host "Running dotnet test:`n  Project: $ProjectPath`n  Diag:    $DiagPath"

dotnet test $ProjectPath --diag:$DiagPath -v minimal
$rc = $LASTEXITCODE
if ($rc -ne 0) { Write-Host "dotnet test exited with code $rc"; exit $rc }
Write-Host "dotnet test completed successfully for $ProjectPath"
exit 0
