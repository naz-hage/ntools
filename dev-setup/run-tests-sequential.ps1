param()

# Stop lingering testhost/dotnet processes that can lock test framework DLLs
Get-Process -Name testhost -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
Get-Process -Name dotnet -ErrorAction SilentlyContinue | Where-Object { $_.Id -ne $PID } | Stop-Process -Force -ErrorAction SilentlyContinue

# Remove local copies of TestPlatform DLLs that frequently get locked
# Candidate debug directories to clean (solution Debug and root C:\Debug where builds are emitted)
$candidateDebugDirs = @()
$solutionDebug = Join-Path -Path $PSScriptRoot -ChildPath "..\Debug" | Resolve-Path -ErrorAction SilentlyContinue
if ($solutionDebug) { $candidateDebugDirs += $solutionDebug.Path }
# Also attempt to clean the machine root Debug folder which this repo sometimes targets
if (Test-Path 'C:\Debug') { $candidateDebugDirs += 'C:\Debug' }

foreach ($debugDirPath in $candidateDebugDirs) {
    try {
        Write-Host "Cleaning TestPlatform DLLs in: $debugDirPath"
        Get-ChildItem -Path $debugDirPath -Filter "Microsoft.VisualStudio.TestPlatform.*.dll" -File -ErrorAction SilentlyContinue | Remove-Item -Force -ErrorAction SilentlyContinue
        Get-ChildItem -Path $debugDirPath -Filter "Microsoft.VisualStudio.TestPlatform.TestFramework*.dll" -File -ErrorAction SilentlyContinue | Remove-Item -Force -ErrorAction SilentlyContinue
        Get-ChildItem -Path $debugDirPath -Filter "MSTest*.dll" -File -ErrorAction SilentlyContinue | Remove-Item -Force -ErrorAction SilentlyContinue
    } catch {
        Write-Host "$($debugDirPath): $($_)" -ForegroundColor Yellow
    }
}

# Ensure OWNER is set for the run (some tests require it)
if (-not $env:OWNER) { $env:OWNER = 'owner' }

Write-Host "Running test projects sequentially (OWNER=$env:OWNER)..."

$failed = @()

# Find test projects under the solution and run them one-by-one
# Exclude projects that live under bin/ or obj/ (these include cloned sample repos or build artifacts)
Get-ChildItem -Path (Resolve-Path "$PSScriptRoot\..") -Recurse -Filter "*Tests.csproj" -ErrorAction SilentlyContinue |
    Where-Object { ($_.FullName -notmatch '\\bin\\') -and ($_.FullName -notmatch '\\obj\\') } |
    ForEach-Object {
        $proj = $_.FullName
        Write-Host "---- dotnet test $proj ----"
        dotnet test $proj -v minimal -c Debug
        if ($LASTEXITCODE -ne 0) {
            $failed += $proj
        }
    }

if ($failed.Count -gt 0) {
    Write-Host "Some test projects failed:" -ForegroundColor Red
    $failed | ForEach-Object { Write-Host " - $_" }
    exit 1
}

Write-Host "All test projects passed." -ForegroundColor Green
exit 0
