# NTools.Scripts module - comprehensive version with all functions consolidated

# =============================================================================
# Common Functions (from Common.psm1)
# =============================================================================

function Write-Info {
    param([Parameter(Mandatory = $true)][string]$Message)
    Write-Host "[INFO] $Message" -ForegroundColor Cyan
}

function Write-Success {
    param([Parameter(Mandatory = $true)][string]$Message)
    Write-Host "[SUCCESS] $Message" -ForegroundColor Green
}

function Write-Warning {
    param([Parameter(Mandatory = $true)][string]$Message)
    Write-Host "[WARNING] $Message" -ForegroundColor Yellow
}

function Write-Error {
    param([Parameter(Mandatory = $true)][string]$Message)
    Write-Host "[ERROR] $Message" -ForegroundColor Red
}

# =============================================================================
# Build Functions (from Build.psm1)
# =============================================================================

function Get-ProjectFiles {
    param(
        [Parameter(Mandatory = $false)][string]$SearchPath = $PSScriptRoot,
        [Parameter(Mandatory = $false)][switch]$ExcludeTests,
        [Parameter(Mandatory = $false)][string[]]$IncludePatterns = @("*.csproj"),
        [Parameter(Mandatory = $false)][string[]]$ExcludePatterns = @()
    )
    
    $projects = Get-ChildItem -Path $SearchPath -Filter "*.csproj" -Recurse
    
    if ($ExcludeTests) {
        $projects = $projects | Where-Object { $_.FullName -notmatch '(?i)test' }
    }
    
    if ($ExcludePatterns.Count -gt 0) {
        foreach ($pattern in $ExcludePatterns) {
            $projects = $projects | Where-Object { $_.FullName -notmatch $pattern }
        }
    }
    
    return $projects
}

function Invoke-ProjectPublish {
    param(
        [Parameter(Mandatory = $true)][string]$ProjectPath,
        [Parameter(Mandatory = $true)][string]$OutputPath,
        [Parameter(Mandatory = $false)][string]$Configuration = "Release",
        [Parameter(Mandatory = $false)][string]$ProductVersion = $null,
        [Parameter(Mandatory = $false)][hashtable]$AdditionalProperties = @{}
    )
    
    if (-not (Test-Path $ProjectPath)) {
        throw "Project file not found: $ProjectPath"
    }
    
    # Ensure output directory exists
    if (-not (Test-Path $OutputPath)) {
        New-Item -ItemType Directory -Force -Path $OutputPath | Out-Null
    }
    
    # Build dotnet publish command
    $publishArgs = @("publish", $ProjectPath, "-c", $Configuration, "-o", $OutputPath)
    
    # Add version if specified
    if ($ProductVersion) {
        $publishArgs += "/p:Version=$ProductVersion"
    }
    
    # Add additional properties
    foreach ($property in $AdditionalProperties.GetEnumerator()) {
        $publishArgs += "/p:$($property.Key)=$($property.Value)"
    }
    
    Write-Host "Publishing $ProjectPath to $OutputPath" -ForegroundColor Cyan
    Write-Host "Command: dotnet $($publishArgs -join ' ')" -ForegroundColor Gray
    
    try {
        & dotnet @publishArgs
        if ($LASTEXITCODE -ne 0) {
            throw "dotnet publish failed with exit code $LASTEXITCODE"
        }
        Write-Host "Successfully published $ProjectPath" -ForegroundColor Green
        return $true
    }
    catch {
        Write-Host "Failed to publish $ProjectPath : $($_.Exception.Message)" -ForegroundColor Red
        return $false
    }
}

# =============================================================================
# DevOps Functions (from devops/ folder)
# =============================================================================

function Get-VersionFromJson {
    param([string]$JsonPath)
    
    try {
        $json = Get-Content $JsonPath | ConvertFrom-Json
        $appInfo = $json.NbuildAppList[0]
        return @{
            Name = $appInfo.Name
            Version = $appInfo.Version
            Found = $true
        }
    }
    catch {
        Write-Warning "Failed to parse $($JsonPath): $($_)"
        return @{ Found = $false }
    }
}

function Update-MarkdownTable {
    param(
        [string]$MarkdownPath,
        [hashtable]$VersionMap
    )
    
    $content = Get-Content $MarkdownPath
    $today = Get-Date -Format "dd-MMM-yy"
    
    for ($i = 0; $i -lt $content.Length; $i++) {
        $line = $content[$i]
        
        # Check if this is a table row with a tool
        if ($line -match '^\| \[([^\]]+)\]') {
            $toolName = $matches[1]
            
            # Find matching version in our map
            foreach ($key in $VersionMap.Keys) {
                $versionInfo = $VersionMap[$key]
                if ($versionInfo.Name -eq $toolName) {
                    # Update the version and date in the line
                    $content[$i] = $line -replace '\| [^|]+ \| [^|]+ \|', "| $($versionInfo.Version) | $today |"
                    break
                }
            }
        }
    }
    
    Set-Content $MarkdownPath $content
    Write-Info "Updated markdown table: $MarkdownPath"
}

# =============================================================================
# Testing Functions (from test/ folder)
# =============================================================================

function Write-TestResult {
    param(
        [string]$Test,
        [bool]$Passed,
        [string]$Details = ""
    )
    
    $status = if ($Passed) { "✅ PASS" } else { "❌ FAIL" }
    $message = "$status - $Test"
    if ($Details) {
        $message += ": $Details"
    }
    Write-Host $message
    return $Passed
}

function Test-TargetExists {
    param(
        [string]$TargetName,
        [string]$FilePath
    )
    
    if (-not (Test-Path $FilePath)) {
        return @{ Exists = $false; Details = "File not found: $FilePath" }
    }
    
    $content = Get-Content $FilePath -Raw
    $targetPattern = "Target\s+Name=`"$TargetName`""
    
    if ($content -match $targetPattern) {
        return @{ Exists = $true; Details = "Target found in $FilePath" }
    } else {
        return @{ Exists = $false; Details = "Target not found in $FilePath" }
    }
}

function Test-TargetDependencies {
    param(
        [string]$TargetName,
        [string]$ExpectedDependency,
        [string]$FilePath
    )
    
    if (-not (Test-Path $FilePath)) {
        return @{ Valid = $false; Details = "File not found: $FilePath" }
    }
    
    $content = Get-Content $FilePath -Raw
    $targetPattern = "Target\s+Name=`"$TargetName`"\s+DependsOnTargets=`"([^`"]*)`""
    
    if ($content -match $targetPattern) {
        $dependencies = $matches[1] -split ';'
        if ($dependencies -contains $ExpectedDependency) {
            return @{ Valid = $true; Details = "Dependency '$ExpectedDependency' found" }
        } else {
            return @{ Valid = $false; Details = "Dependency '$ExpectedDependency' not found. Found: $($dependencies -join ', ')" }
        }
    } else {
        return @{ Valid = $false; Details = "Target '$TargetName' not found or has no dependencies" }
    }
}

function Test-TargetDelegation {
    param(
        [string]$SourceTarget,
        [string]$DelegateTarget
    )
    
    $sourceFile = "C:\source\ntools\nbuild.targets"
    $delegateFile = "C:\Program Files\nbuild\common.targets"
    
    # Test 1: Source target exists
    $sourceTest = Test-TargetExists -TargetName $SourceTarget -FilePath $sourceFile
    $result1 = Write-TestResult -Test "Source target '$SourceTarget' exists" -Passed $sourceTest.Exists -Details $sourceTest.Details
    
    # Test 2: Delegate target exists
    $delegateTest = Test-TargetExists -TargetName $DelegateTarget -FilePath $delegateFile
    $result2 = Write-TestResult -Test "Delegate target '$DelegateTarget' exists" -Passed $delegateTest.Exists -Details $delegateTest.Details
    
    # Test 3: Source depends on delegate
    $depTest = Test-TargetDependencies -TargetName $SourceTarget -ExpectedDependency $DelegateTarget -FilePath $sourceFile
    $result3 = Write-TestResult -Test "Source target depends on delegate" -Passed $depTest.Valid -Details $depTest.Details
    
    return ($result1 -and $result2 -and $result3)
}

# =============================================================================
# Utility Functions (from utils/ folder)
# =============================================================================

function Get-FileHash256 {
    param([Parameter(Mandatory = $true)][string]$FilePath)
    
    if (-Not (Test-Path -Path $FilePath)) {
        throw "File '$FilePath' does not exist."
    }
    
    try {
        $fileStream = [System.IO.File]::OpenRead($FilePath)
        $sha256 = [System.Security.Cryptography.SHA256]::Create()
        $hashBytes = $sha256.ComputeHash($fileStream)
        $fileStream.Close()
        
        $hashString = [BitConverter]::ToString($hashBytes) -replace '-', ''
        return $hashString
    } catch {
        throw "An error occurred calculating hash: $_"
    }
}

function Get-FileVersionInfo {
    param([string]$FilePath)
    
    if (Test-Path $FilePath) {
        $fileVersionInfo = Get-Item $FilePath | Select-Object -ExpandProperty VersionInfo
        return @{
            FilePath = $FilePath
            FileVersion = $fileVersionInfo.FileVersion
            ProductVersion = $fileVersionInfo.ProductVersion
            Found = $true
        }
    } else {
        return @{
            FilePath = $FilePath
            Found = $false
            Error = "File not found"
        }
    }
}

function Invoke-FastForward {
    param(
        [string]$BranchName = "main",
        [switch]$Force
    )
    
    Write-Info "Fast-forwarding to latest $BranchName..."
    
    try {
        git fetch origin
        if ($LASTEXITCODE -ne 0) { throw "Git fetch failed" }
        
        $currentBranch = git branch --show-current
        if ($currentBranch -ne $BranchName) {
            git checkout $BranchName
            if ($LASTEXITCODE -ne 0) { throw "Git checkout failed" }
        }
        
        if ($Force) {
            git reset --hard "origin/$BranchName"
        } else {
            git merge --ff-only "origin/$BranchName"
        }
        
        if ($LASTEXITCODE -ne 0) { throw "Git merge/reset failed" }
        
        Write-Success "Successfully fast-forwarded to latest $BranchName"
        return $true
    }
    catch {
        Write-Error "Fast-forward failed: $_"
        return $false
    }
}

# =============================================================================
# Main Module Functions
# =============================================================================

function Get-NtoolsScriptsVersion {
    return "NTools.Scripts version 2.3.0"
}

function Publish-AllProjects {
    param(
        [Parameter(Mandatory=$true)] [string]$OutputDir,
        [Parameter(Mandatory=$true)] [string]$Version,
        [Parameter(Mandatory=$true)] [string]$RepositoryRoot
    )

    # Ensure output directory exists
    if (-not (Test-Path $OutputDir)) {
        New-Item -ItemType Directory -Force -Path $OutputDir | Out-Null
        Write-Info "Created output directory: $OutputDir"
    }

    Write-Info "Starting project publishing process..."
    Write-Info "Output Directory: $OutputDir"
    Write-Info "Product Version: $Version"
    Write-Info "Repository root: $RepositoryRoot"

    # Validate repository root exists
    if (-not (Test-Path $RepositoryRoot)) {
        Write-Error "Repository root path does not exist: $RepositoryRoot"
        return @{ Success = 0; Failures = 1 }
    }
    
    # Get all non-test projects
    $projects = Get-ProjectFiles -SearchPath $RepositoryRoot -ExcludeTests
    Write-Info "Found $($projects.Count) projects to publish"

    $successCount = 0
    $failureCount = 0

    foreach ($project in $projects) {
        $result = Invoke-ProjectPublish -ProjectPath $project.FullName -OutputPath $OutputDir -ProductVersion $Version
        if ($result) {
            $successCount++
        } else {
            $failureCount++
        }
    }

    Write-Info "Publishing completed"
    Write-Success "$successCount projects published successfully"
    
    if ($failureCount -gt 0) {
        Write-Error "$failureCount projects failed to publish"
        throw "$failureCount projects failed to publish"
    } else {
        Write-Success "All projects published successfully to $OutputDir"
    }
    
    return @{ Success = $successCount; Failures = $failureCount }
}

# ============================================================================
# Install and Setup Functions
# ============================================================================

function Write-OutputMessage {
    param(
        [Parameter(Mandatory = $true)]
        [String]$Prefix,
        [Parameter(Mandatory = $true)]
        [String]$Message
    )

    $dateTime = Get-Date -Format "yyyy-MM-dd hh:mm tt"
    
    # append to the log file install.log
    if (!(Test-Path -Path "install.log")) {
        New-Item -ItemType File -Path "install.log" -Force
    }

    if ($Message -eq "EmtpyLine") {
        Add-Content -Path "install.log" -Value ""
        Write-Output ""
    } else {
        Write-Output "$dateTime $Prefix : $Message"
        Write-Output ""
        Add-Content -Path "install.log" -Value "$dateTime | $Prefix | $Message"
    }
}

function Get-NToolsFileVersion {
    param (
        [Parameter(Mandatory=$true)]
        [string]$FilePath
    )   

    $versionInfo = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($FilePath)
    # return the all file version parts joined by a dot
    return ($versionInfo.FileMajorPart, $versionInfo.FileMinorPart, $versionInfo.FileBuildPart, $versionInfo.FilePrivatePart) -join "."
}

function Add-DeploymentPathToEnvironment {
    param (
        [Parameter(Mandatory=$true)]
        [string]$DeploymentPath
    )

    $path = [Environment]::GetEnvironmentVariable("PATH", "Machine")
    if ($path -notlike "*$DeploymentPath*") {
        Write-OutputMessage $MyInvocation.MyCommand.Name "Adding $DeploymentPath to the PATH environment variable."
        [Environment]::SetEnvironmentVariable("PATH", $path + ";$DeploymentPath", "Machine")
    }
    else {
        Write-OutputMessage $MyInvocation.MyCommand.Name "$DeploymentPath already exists in the PATH environment variable."
    }
}

function Invoke-NToolsDownload {
    param (
        [Parameter(Mandatory=$true)]
        [string]$Version,
        [Parameter(Mandatory=$false)]
        [string]$DownloadsDirectory = "c:\NToolsDownloads"
    )
    
    # display parameters
    Write-Host "DownloadNtools - Parameters:"
    Write-Host "Downloading NTools version $Version ..."
    Write-Host "Downloads directory: $DownloadsDirectory"

    # Create the Downloads directory if it doesn't exist
    if (!(Test-Path -Path $DownloadsDirectory)) {
        Write-Host "Creating downloads directory: $DownloadsDirectory ..."
        New-Item -ItemType Directory -Path $DownloadsDirectory | Out-Null
    }

    $url = "https://github.com/naz-hage/ntools/releases/download/$Version/$Version.zip"
    $fileName = "$DownloadsDirectory\$Version.zip"
    
    try {
        Invoke-WebRequest -Uri $url -OutFile $fileName -ErrorAction Stop
    } catch {
        Write-Host "Failed to download NTools version $Version from $url"
        Write-Host "Error: $($_.Exception.Message)"
        return $false
    }

    if (Test-Path $fileName) {
        Write-Host "Downloaded NTools version $Version to $fileName"
        return $true
    } else {
        Write-Host "Failed to download NTools version $Version from $url"
        return $false
    }
}

function Install-NTools {
    param (
        [Parameter(Mandatory=$false, HelpMessage = "The version of NTools to install. If not specified, the version is read from ntools.json.")]
        [string]$Version,
        [Parameter(Mandatory=$false, HelpMessage = "The directory to download the NTools zip file to. Defaults to 'c:\\NToolsDownloads'.")]
        [string]$DownloadsDirectory = "c:\NToolsDownloads",
        [Parameter(Mandatory=$false, HelpMessage = "Path to the ntools.json file. If not specified, looks for ntools.json relative to script location.")]
        [string]$NtoolsJsonPath
    )

    $deploymentPath = $env:ProgramFiles + "\NBuild"

    # display parameters
    Write-Host "InstallNtools - Parameters:"
    Write-Host "Version: $Version"
    Write-Host "Downloads directory: $DownloadsDirectory"
    Write-Host "NTools JSON path: $NtoolsJsonPath"

    # If Version is not specified, read it from ntools.json
    if (-not $Version) {
        # Determine ntools.json path
        if (-not $NtoolsJsonPath) {
            $scriptDir = Split-Path -Parent $PSCommandPath
            $NtoolsJsonPath = "$scriptDir\..\ntools.json"
            Write-Host "No NtoolsJsonPath specified, using default: $NtoolsJsonPath"
        }

        Write-Host "Reading version from $NtoolsJsonPath ..."
        
        if (Test-Path -Path $NtoolsJsonPath) {
            try {
                $NtoolsJson = Get-Content -Path $NtoolsJsonPath -Raw | ConvertFrom-json
                $Version = $NtoolsJson.NbuildAppList[0].Version
                Write-Host "Version read from ntools.json: $Version"
            }
            catch {
                Write-Warning "Failed to read version from ntools.json. Please specify the version manually."
                return $false
            }
        }
        else {
            Write-Warning "ntools.json not found at '$NtoolsJsonPath'. Please specify the version manually or provide a valid NtoolsJsonPath."
            return $false
        }
    }

    # Download the specified version of NTools
    $downloadResult = Invoke-NToolsDownload -Version $Version -DownloadsDirectory $DownloadsDirectory
    if (-not $downloadResult) {
        return $false
    }

    # Check if the downloaded file exists
    $downloadedFile = Join-Path -Path $DownloadsDirectory -ChildPath "$Version.zip"

    if (!(Test-Path -Path $downloadedFile)) {
        Write-Host "Downloaded file not found: $downloadedFile"
        return $false
    }
    
    # Unzip the downloaded file to the deployment path
    try {
        Expand-Archive -Path $downloadedFile -DestinationPath $deploymentPath -Force
        # add deployment path to the PATH environment variable if it doesn't already exist
        Add-DeploymentPathToEnvironment $deploymentPath

        Write-Host "NTools version $Version installed to $deploymentPath"
        
        # indicate success to callers
        return $true
    }
    catch {
        Write-Host "Failed to extract or install NTools: $($_.Exception.Message)"
        return $false
    }
}

Export-ModuleMember -Function Get-NtoolsScriptsVersion, Publish-AllProjects, Get-VersionFromJson, Update-MarkdownTable, Write-TestResult, Test-TargetExists, Test-TargetDependencies, Test-TargetDelegation, Get-FileHash256, Get-FileVersionInfo, Invoke-FastForward, Write-OutputMessage, Get-NToolsFileVersion, Add-DeploymentPathToEnvironment, Invoke-NToolsDownload, Install-NTools
