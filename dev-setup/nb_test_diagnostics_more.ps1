param([string]$SolutionDir)

if (-not $SolutionDir) { $SolutionDir = (Get-Location).Path }

Write-Host "---- Repo-wide Microsoft.TestPlatform / MSTest DLLs ----"
Get-ChildItem -Path $SolutionDir -Include 'Microsoft.TestPlatform*.dll','Microsoft.VisualStudio.TestPlatform.TestFramework*.dll','MSTest*.dll' -Recurse -ErrorAction SilentlyContinue |
  Select-Object FullName, Length, LastWriteTime |
  Format-Table -AutoSize

Write-Host "---- Debug Microsoft*.dll ----"
if (Test-Path (Join-Path $SolutionDir 'Debug')) {
  Get-ChildItem -Path (Join-Path $SolutionDir 'Debug') -Filter 'Microsoft*.dll' -Recurse -ErrorAction SilentlyContinue |
    Select-Object FullName, Length, LastWriteTime |
    Format-Table -AutoSize
} else {
  Write-Host "No Debug folder at $SolutionDir\Debug"
}

Write-Host "---- dotnet package lists for *Tests.csproj (may be long) ----"
if (Get-Command dotnet -ErrorAction SilentlyContinue) {
  Get-ChildItem -Path $SolutionDir -Filter '*Tests.csproj' -Recurse -ErrorAction SilentlyContinue |
    ForEach-Object {
      Write-Host "---- $($_.FullName) ----"
      dotnet list $_.FullName package --include-transitive
    }
} else {
  Write-Host "dotnet not found on PATH; skipping package lists."
}
