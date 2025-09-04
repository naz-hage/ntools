param(
    [Parameter(Mandatory = $true)]
    [string]$ArtifactsPath,
    
    [Parameter(Mandatory = $true)]
    [string]$ProductVersion
)

# Initialize counters
$SuccessCount = 0
$WarningCount = 0
$ErrorCount = 0

# Helper functions for consistent output formatting
function Write-Info {
    param([string]$Message)
    Write-Host "[INFO] $Message" -ForegroundColor Cyan
}

function Write-Success {
    param([string]$Message)
    Write-Host "[SUCCESS] $Message" -ForegroundColor Green
    $script:SuccessCount++
}

function Write-Warning {
    param([string]$Message)
    Write-Host "[WARNING] $Message" -ForegroundColor Yellow
    $script:WarningCount++
}

function Write-Error {
    param([string]$Message)
    Write-Host "[ERROR] $Message" -ForegroundColor Red
    $script:ErrorCount++
}

Write-Host "================================================================" -ForegroundColor Magenta
Write-Host "                 NTOOLS ARTIFACT VERIFICATION" -ForegroundColor Magenta
Write-Host "================================================================" -ForegroundColor Magenta
Write-Info "Starting comprehensive artifact verification..."
Write-Info "Artifacts Path: $ArtifactsPath"
Write-Info "Product Version: $ProductVersion"

# Check if artifacts directory exists
if (-not (Test-Path $ArtifactsPath)) {
    Write-Error "Artifacts directory not found: $ArtifactsPath"
    exit 1
}

Write-Info "Artifacts directory found: $ArtifactsPath"

# Define expected executables and their descriptions
$ExpectedExecutables = @{
    "nb.exe" = "Build automation CLI"
    "lf.exe" = "List files utility"
    "nBackup.exe" = "Backup utility"
    "wi.exe" = "Work item utility"
    "Nbuild.exe" = "Build engine"
}

# Define expected libraries
$ExpectedLibraries = @{
    "nb.dll" = "Build automation library"
    "lf.dll" = "List files library"
    "nBackup.dll" = "Backup library"
    "wi.dll" = "Work item library"
    "Nbuild.dll" = "Build engine library"
    "NbuildTasks.dll" = "Build tasks library"
    "GitHubRelease.dll" = "GitHub release library"
    "ApiVersions.dll" = "API versions library"
}

# Define expected configuration files
$ExpectedConfigs = @{
    "nb.runtimeconfig.json" = "nb runtime configuration"
    "lf.runtimeconfig.json" = "lf runtime configuration"
    "nBackup.runtimeconfig.json" = "nBackup runtime configuration"
    "wi.runtimeconfig.json" = "wi runtime configuration"
    "Nbuild.runtimeconfig.json" = "Nbuild runtime configuration"
    "backup.json" = "Backup configuration"
    "ntools.json" = "ntools configuration"
}

# Define expected target files
$ExpectedTargets = @{
    "common.targets" = "Common MSBuild targets"
    "nbuild.targets" = "Nbuild MSBuild targets"
    "dotnet.targets" = "Dotnet MSBuild targets"
    "git.targets" = "Git MSBuild targets"
    "nuget.targets" = "NuGet MSBuild targets"
    "apps-versions.targets" = "Application versions targets"
}

Write-Info "Verifying executables..."
foreach ($exe in $ExpectedExecutables.Keys) {
    $exePath = Join-Path $ArtifactsPath $exe
    if (Test-Path $exePath) {
        Write-Success "Found executable: $exe ($($ExpectedExecutables[$exe]))"
        
        # Try to get version info
        try {
            $versionInfo = Get-ItemProperty $exePath | Select-Object VersionInfo
            if ($versionInfo.VersionInfo.ProductVersion) {
                Write-Info "  Version: $($versionInfo.VersionInfo.ProductVersion)"
            }
        }
        catch {
            Write-Warning "  Could not retrieve version info for $exe"
        }
    }
    else {
        Write-Error "Missing executable: $exe"
    }
}

Write-Info "Verifying libraries..."
foreach ($lib in $ExpectedLibraries.Keys) {
    $libPath = Join-Path $ArtifactsPath $lib
    if (Test-Path $libPath) {
        Write-Success "Found library: $lib ($($ExpectedLibraries[$lib]))"
    }
    else {
        Write-Error "Missing library: $lib"
    }
}

Write-Info "Verifying configuration files..."
foreach ($config in $ExpectedConfigs.Keys) {
    $configPath = Join-Path $ArtifactsPath $config
    if (Test-Path $configPath) {
        Write-Success "Found config: $config ($($ExpectedConfigs[$config]))"
    }
    else {
        Write-Warning "Missing config: $config"
    }
}

Write-Info "Verifying MSBuild target files..."
foreach ($target in $ExpectedTargets.Keys) {
    $targetPath = Join-Path $ArtifactsPath $target
    if (Test-Path $targetPath) {
        Write-Success "Found target file: $target ($($ExpectedTargets[$target]))"
    }
    else {
        Write-Warning "Missing target file: $target"
    }
}

Write-Info "Checking for unwanted test artifacts..."
$testPatterns = @("*Test*.dll", "*Test*.exe", "*test*.dll", "*test*.exe")
$foundTestArtifacts = $false

foreach ($pattern in $testPatterns) {
    $testFiles = Get-ChildItem -Path $ArtifactsPath -Filter $pattern -ErrorAction SilentlyContinue
    foreach ($testFile in $testFiles) {
        Write-Warning "Found test artifact (should not be in release): $($testFile.Name)"
        $foundTestArtifacts = $true
    }
}

if (-not $foundTestArtifacts) {
    Write-Success "No unwanted test artifacts found"
}

Write-Info "Verifying basic executable functionality..."
$nbExe = Join-Path $ArtifactsPath "nb.exe"
if (Test-Path $nbExe) {
    try {
        $result = & $nbExe --version 2>&1
        if ($LASTEXITCODE -eq 0) {
            Write-Success "nb.exe executes successfully"
            Write-Info "  Output: $result"
        }
        else {
            Write-Warning "nb.exe returned non-zero exit code: $LASTEXITCODE"
        }
    }
    catch {
        Write-Warning "Failed to execute nb.exe: $($_.Exception.Message)"
    }
}

$lfExe = Join-Path $ArtifactsPath "lf.exe"
if (Test-Path $lfExe) {
    try {
        $result = & $lfExe --help 2>&1
        if ($LASTEXITCODE -eq 0) {
            Write-Success "lf.exe executes successfully"
        }
        else {
            Write-Warning "lf.exe returned non-zero exit code: $LASTEXITCODE"
        }
    }
    catch {
        Write-Warning "Failed to execute lf.exe: $($_.Exception.Message)"
    }
}

Write-Info "Verifying folder structure..."
$folderCount = (Get-ChildItem -Path $ArtifactsPath -Directory).Count
$fileCount = (Get-ChildItem -Path $ArtifactsPath -File).Count

Write-Info "Found $folderCount subdirectories and $fileCount files"

if ($fileCount -gt 10) {
    Write-Success "Artifact count looks reasonable ($fileCount files)"
}
else {
    Write-Warning "Low artifact count - expected more files ($fileCount files)"
}

Write-Host "================================================================" -ForegroundColor Magenta
Write-Host "                    VERIFICATION SUMMARY" -ForegroundColor Magenta
Write-Host "================================================================" -ForegroundColor Magenta
Write-Host "Artifacts Path: $ArtifactsPath" -ForegroundColor White
Write-Host "[SUCCESS] Count: $SuccessCount" -ForegroundColor Green
Write-Host "[WARNING] Count: $WarningCount" -ForegroundColor Yellow
Write-Host "[ERROR] Count: $ErrorCount" -ForegroundColor Red

if ($ErrorCount -eq 0) {
    Write-Host "[SUCCESS] Artifact verification completed successfully!" -ForegroundColor Green
    exit 0
} else {
    Write-Host "[FAILED] Artifact verification failed with $ErrorCount error(s)" -ForegroundColor Red
    exit 1
}
