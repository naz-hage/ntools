# PowerShell script for artifact verification
# Verifies that expected artifacts exist in the correct folder structure

param(
    [Parameter(Mandatory=$true)]
    [ValidateNotNullOrEmpty()]
    [string]$ArtifactsPath,
    
    [Parameter(Mandatory=$false)]
    [string]$ProductVersion = "",
    
    [Parameter(Mandatory=$false)]
    [switch]$Verbose
)

function Write-Info {
    param([string]$Message)
    Write-Host "ℹ️  $Message" -ForegroundColor Cyan
}

function Write-Success {
    param([string]$Message)
    Write-Host "✅ $Message" -ForegroundColor Green
}

function Write-Warning {
    param([string]$Message)
    Write-Host "⚠️  $Message" -ForegroundColor Yellow
}

function Write-Error {
    param([string]$Message)
    Write-Host "❌ $Message" -ForegroundColor Red
}

# Expected artifacts mapping: source pattern -> expected destination
$ExpectedArtifacts = @{
    # Main executables
    "nb.exe" = @{
        "Path" = "nb.exe"
        "Required" = $true
        "Description" = "Main nbuild executable"
    }
    "lf.exe" = @{
        "Path" = "lf.exe"
        "Required" = $true
        "Description" = "List files executable"
    }
    "wi.exe" = @{
        "Path" = "wi.exe"
        "Required" = $true
        "Description" = "Work items executable"
    }
    "nBackup.exe" = @{
        "Path" = "nBackup.exe"
        "Required" = $true
        "Description" = "Backup utility executable"
    }
    "GitHubRelease.exe" = @{
        "Path" = "GitHubRelease.exe"
        "Required" = $true
        "Description" = "GitHub release utility executable"
    }
    "ApiVersions.exe" = @{
        "Path" = "ApiVersions.exe"
        "Required" = $true
        "Description" = "API versions utility executable"
    }
    
    # Supporting libraries
    "NbuildTasks.dll" = @{
        "Path" = "NbuildTasks.dll"
        "Required" = $true
        "Description" = "NBuild tasks library"
    }
    "nBackup.dll" = @{
        "Path" = "nBackup.dll"
        "Required" = $true
        "Description" = "Backup utility library"
    }
    
    # Configuration files
    "common.targets" = @{
        "Path" = "common.targets"
        "Required" = $true
        "Description" = "Common MSBuild targets"
    }
    "ntools.json" = @{
        "Path" = "ntools.json"
        "Required" = $false
        "Description" = "NTools configuration"
    }
    
    # Support scripts
    "publish-all-projects.ps1" = @{
        "Path" = "publish-all-projects.ps1"
        "Required" = $false
        "Description" = "Publishing script"
    }
}

# Expected folder structure
$ExpectedFolders = @(
    "cs",
    "de", 
    "es",
    "fr",
    "it",
    "ja",
    "ko",
    "pl",
    "pt-BR",
    "ru",
    "tr",
    "zh-Hans",
    "zh-Hant",
    "runtimes\win\lib\net9.0"
)

Write-Info "Starting artifact verification for: $ArtifactsPath"
if ($ProductVersion) {
    Write-Info "Product Version: $ProductVersion"
}

# Check if artifacts directory exists
if (-not (Test-Path $ArtifactsPath)) {
    Write-Error "Artifacts directory not found: $ArtifactsPath"
    exit 1
}

$ErrorCount = 0
$WarningCount = 0
$SuccessCount = 0

Write-Info "Verifying folder structure..."

# Verify expected folders exist
foreach ($folder in $ExpectedFolders) {
    $folderPath = Join-Path $ArtifactsPath $folder
    if (Test-Path $folderPath) {
        Write-Success "Folder exists: $folder"
        $SuccessCount++
    } else {
        Write-Warning "Optional folder missing: $folder"
        $WarningCount++
    }
}

Write-Info "Verifying artifact files..."

# Verify expected artifacts exist
foreach ($artifactName in $ExpectedArtifacts.Keys) {
    $artifact = $ExpectedArtifacts[$artifactName]
    $artifactPath = Join-Path $ArtifactsPath $artifact.Path
    
    if (Test-Path $artifactPath) {
        Write-Success "Found: $artifactName - $($artifact.Description)"
        $SuccessCount++
        
        # Additional validation for executables
        if ($artifactPath.EndsWith(".exe")) {
            try {
                $fileInfo = Get-Item $artifactPath
                if ($Verbose) {
                    Write-Info "  Size: $($fileInfo.Length) bytes"
                    Write-Info "  Modified: $($fileInfo.LastWriteTime)"
                }
                
                # Try to get file version
                $versionInfo = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($artifactPath)
                if ($versionInfo.FileVersion) {
                    Write-Info "  Version: $($versionInfo.FileVersion)"
                }
            }
            catch {
                Write-Warning "  Could not get file details: $($_.Exception.Message)"
            }
        }
    } else {
        if ($artifact.Required) {
            Write-Error "Missing required artifact: $artifactName - $($artifact.Description)"
            $ErrorCount++
        } else {
            Write-Warning "Missing optional artifact: $artifactName - $($artifact.Description)"
            $WarningCount++
        }
    }
}

# Verify no test artifacts in the output
Write-Info "Checking for unwanted test artifacts..."
$testPatterns = @("*test*", "*Test*", "*.pdb")
foreach ($pattern in $testPatterns) {
    $testFiles = Get-ChildItem -Path $ArtifactsPath -Filter $pattern -Recurse -ErrorAction SilentlyContinue
    foreach ($testFile in $testFiles) {
        # Allow some exceptions
        if ($testFile.Name -eq "testhost.exe" -or $testFile.Name -eq "testhost.dll") {
            continue  # These are legitimate test infrastructure files
        }
        Write-Warning "Found test artifact: $($testFile.FullName.Replace($ArtifactsPath, ''))"
        $WarningCount++
    }
}

# Display summary
Write-Host ""
Write-Host "════════════════════════════════════════════════════════════════" -ForegroundColor Magenta
Write-Host "                    ARTIFACT VERIFICATION SUMMARY" -ForegroundColor Magenta
Write-Host "════════════════════════════════════════════════════════════════" -ForegroundColor Magenta
Write-Host "Artifacts Path: $ArtifactsPath" -ForegroundColor White
Write-Host "✅ Success Count: $SuccessCount" -ForegroundColor Green
Write-Host "⚠️  Warning Count: $WarningCount" -ForegroundColor Yellow
Write-Host "❌ Error Count: $ErrorCount" -ForegroundColor Red

if ($ErrorCount -eq 0) {
    Write-Host "🎉 Artifact verification completed successfully!" -ForegroundColor Green
    exit 0
} else {
    Write-Host "💥 Artifact verification failed with $ErrorCount error(s)" -ForegroundColor Red
    exit 1
}
