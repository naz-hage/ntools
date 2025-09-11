# ntools-scripts module - comprehensive version with all functions consolidated

#region Common (from Common.psm1)
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

#endregion

#region Build (from Build.psm1)
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

#region DevOps (from devops/ folder)
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

function Update-DocVersions {
    param(
        [string]$DevSetupPath,
        [hashtable]$DocsPath
    )
    
    $content = Get-Content $DevSetupPath
    $today = Get-Date -Format "dd-MMM-yy"
    
    for ($i = 0; $i -lt $content.Length; $i++) {
        $line = $content[$i]
        
        # Check if this is a table row with a tool
        if ($line -match '^\| \[([^\]]+)\]') {
            $toolName = $matches[1]
            
            # Find matching version in our map
            foreach ($key in $DocsPath.Keys) {
                $versionInfo = $DocsPath[$key]
                if ($versionInfo.Name -eq $toolName) {
                    # Update the version and date in the line
                    $content[$i] = $line -replace '\| [^|]+ \| [^|]+ \|', "| $($versionInfo.Version) | $today |"
                    break
                }
            }
        }
    }
    
    Set-Content $DevSetupPath $content
    Write-Info "Updated markdown table: $DevSetupPath"
}

# DevOps: get public IP and set pipeline variable
function Get-AgentPublicIp {
    param(
        [int]$MaxAttempts = 5
    )

    $count = 0
    $agentIp = $null

    while ($count -lt $MaxAttempts -and $null -eq $agentIp) {
        $count++
        try {
            $agentIp = (Invoke-RestMethod http://ipinfo.io/json).ip
        } catch {
            Write-Host "Failed to retrieve IP address. Retrying $count ..."
            Start-Sleep -Seconds 5
        }
    }

    if ($null -eq $agentIp) {
        throw "IP Not Found"
    }

    Write-Output "IP Address: $agentIp"
    # For Azure DevOps logging command to set a pipeline variable
    Write-Host "##vso[task.setvariable variable=agentIp]$agentIp"
    return $agentIp
}

# DevOps: add a WAF custom rule allowing the agent IP
function Add-WafAllowRule {
    [CmdletBinding()]
    param (
        [Parameter(Mandatory=$true)] [string]$ResourceGroupName,
        [Parameter(Mandatory=$true)] [string]$WafPolicyName,
        [Parameter(Mandatory=$true)] [string]$CustomRuleName,
        [Parameter(Mandatory=$true)] [string]$AgentIp
    )

    Write-Host "Adding WAF allow rule '$CustomRuleName' for $AgentIp to policy $WafPolicyName in $ResourceGroupName"

    try {
        $azArgs = @(
            'network','front-door','waf-policy','rule','create',
            '--action','Allow',
            '--name',$CustomRuleName,
            '--policy-name',$WafPolicyName,
            '--resource-group',$ResourceGroupName,
            '--priority','20',
            '--rule-type','MatchRule',
            '--defer'
        )
        & az @azArgs
        if ($LASTEXITCODE -eq 0) { Write-Host "Custom rule '$CustomRuleName' added successfully." }
        else { throw "Failed to add custom rule '$CustomRuleName'." }

        $azArgs = @(
            'network','front-door','waf-policy','rule','match-condition','add',
            '--match-variable','SocketAddr',
            '--operator','IPMatch',
            '--values',"$AgentIp",
            '--negate','false',
            '--name',$CustomRuleName,
            '--resource-group',$ResourceGroupName,
            '--policy-name',$WafPolicyName
        )
        & az @azArgs
        if ($LASTEXITCODE -eq 0) { Write-Host "Match condition added to custom rule '$CustomRuleName' successfully." }
        else { throw "Failed to add match condition to custom rule '$CustomRuleName'." }
    }
    catch {
        Write-Error "An error occurred: $_"
        throw $_
    }
}

# DevOps: delete a WAF custom rule
function Remove-WafCustomRule {
    [CmdletBinding()]
    param (
        [Parameter(Mandatory=$true)] [string]$ResourceGroupName,
        [Parameter(Mandatory=$true)] [string]$WafPolicyName,
        [Parameter(Mandatory=$true)] [string]$CustomRuleName
    )

    Write-Host "Removing WAF custom rule '$CustomRuleName' from policy $WafPolicyName in $ResourceGroupName"

    try {
    $azArgs = @('network','front-door','waf-policy','rule','delete', '--name',$CustomRuleName, '--policy-name',$WafPolicyName, '--resource-group',$ResourceGroupName)
    & az @azArgs
    if ($LASTEXITCODE -eq 0) { Write-Host "Custom rule '$CustomRuleName' deleted successfully." }
    else { throw "Failed to delete custom rule '$CustomRuleName'." }
    }
    catch {
        Write-Error "An error occurred while deleting the custom rule: $_"
        throw $_
    }
}

#endregion

#region Testing (from test/ folder)
# =============================================================================
# Testing Functions (from test/ folder)
# =============================================================================

function Write-TestResult {
    param(
        [string]$Test,
        [bool]$Passed,
        [string]$Details = ""
    )
    
    $status = if ($Passed) { " heck" } else { " ail" }
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

#region Utility (from utils/ folder)
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


#endregion

#region Main
# =============================================================================
# Main Module Functions
# =============================================================================

function Get-ntoolsScriptsVersion {
    return "ntools-scripts version 2.3.0"
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


#endregion

#region InstallAndSetup
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

# Development environment setup (top-level)
function Set-DevelopmentEnvironment {
    param(
        [Parameter(Mandatory=$false)][string]$DevDrive = "c:",
        [Parameter(Mandatory=$false)][string]$MainDir = "source"
    )

    # Log start
    Write-OutputMessage $MyInvocation.MyCommand.Name "Starting Set-DevelopmentEnvironment."

    # Ensure caller is running as Administrator
    if (-NOT ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator")) {
        Write-OutputMessage $MyInvocation.MyCommand.Name "Error: Please run this cmdlet as an administrator." -ForegroundColor Red
        throw "Administrator rights required to set development environment variables."
    }

    # Set the environment variables for the current user
    [System.Environment]::SetEnvironmentVariable("devDrive", $DevDrive, [System.EnvironmentVariableTarget]::User)
    [System.Environment]::SetEnvironmentVariable("mainDir", $MainDir, [System.EnvironmentVariableTarget]::User)

    # Read and report the environment variables
    $newDevDrive = [System.Environment]::GetEnvironmentVariable("devDrive", [System.EnvironmentVariableTarget]::User)
    $newMainDir = [System.Environment]::GetEnvironmentVariable("mainDir", [System.EnvironmentVariableTarget]::User)

    Write-OutputMessage $MyInvocation.MyCommand.Name "DevDrive set to '$newDevDrive' and MainDir set to '$newMainDir' successfully."
    return $true
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
        [Parameter(Mandatory = $true)]
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

## Export will be declared at the end of the file after all functions are defined
# Export-ModuleMember -Function ... (deferred)

#endregion

#region ArtifactVerification
# =============================================================================
# Artifact verification (migrated from scripts/build/build-verify-artifacts.ps1)
# =============================================================================
function Invoke-VerifyArtifacts {
    param(
        [Parameter(Mandatory = $true)][string]$ArtifactsPath,
        [Parameter(Mandatory = $true)][string]$ProductVersion
    )

    # counters
    $SuccessCount = 0
    $WarningCount = 0
    $ErrorCount = 0

    Write-Host "================================================================" -ForegroundColor Magenta
    Write-Host "                 NTOOLS ARTIFACT VERIFICATION" -ForegroundColor Magenta
    Write-Host "================================================================" -ForegroundColor Magenta
    Write-Info "Starting comprehensive artifact verification..."
    Write-Info "Artifacts Path: $ArtifactsPath"
    Write-Info "Product Version: $ProductVersion"

    if (-not (Test-Path $ArtifactsPath)) {
        Write-Error "Artifacts directory not found: $ArtifactsPath"
        $ErrorCount++
        return @{ Success = $SuccessCount; Warning = $WarningCount; Error = $ErrorCount }
    }


    Write-Info "Artifacts directory found: $ArtifactsPath"

    $ExpectedExecutables = @{
        "nb.exe" = "Build automation CLI"
        "lf.exe" = "List files utility"
        "nBackup.exe" = "Backup utility"
        "wi.exe" = "Work item utility"
    }

    $ExpectedLibraries = @{
        "nb.dll" = "Build automation library"
        "lf.dll" = "List files library"
        "nBackup.dll" = "Backup library"
        "wi.dll" = "Work item library"
        "NbuildTasks.dll" = "Build tasks library"
        "GitHubRelease.dll" = "GitHub release library"
        "ApiVersions.dll" = "API versions library"
    }

    $ExpectedConfigs = @{
        "nb.runtimeconfig.json" = "nb runtime configuration"
        "lf.runtimeconfig.json" = "lf runtime configuration"
        "nBackup.runtimeconfig.json" = "nBackup runtime configuration"
        "wi.runtimeconfig.json" = "wi runtime configuration"
        "Nbuild.runtimeconfig.json" = "Nbuild runtime configuration"
        "backup.json" = "Backup configuration"
        "ntools.json" = "ntools configuration"
    }

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
            $SuccessCount++
            try {
                $versionInfo = Get-ItemProperty $exePath | Select-Object VersionInfo
                if ($versionInfo.VersionInfo.ProductVersion) { Write-Info "  Version: $($versionInfo.VersionInfo.ProductVersion)" }
            } catch { Write-Warning "  Could not retrieve version info for $exe"; $WarningCount++ }
        } else {
            Write-Error "Missing executable: $exe"
            $ErrorCount++
        }
    }

    Write-Info "Verifying libraries..."
    foreach ($lib in $ExpectedLibraries.Keys) {
        $libPath = Join-Path $ArtifactsPath $lib
        if (Test-Path $libPath) {
            Write-Success "Found library: $lib ($($ExpectedLibraries[$lib]))"
            $SuccessCount++
        } else {
            Write-Error "Missing library: $lib"
            $ErrorCount++
        }
    }

    Write-Info "Verifying configuration files..."
    foreach ($config in $ExpectedConfigs.Keys) {
        $configPath = Join-Path $ArtifactsPath $config
        if (Test-Path $configPath) {
            Write-Success "Found config: $config ($($ExpectedConfigs[$config]))"
            $SuccessCount++
        } else {
            Write-Warning "Missing config: $config"
            $WarningCount++
        }
    }

    Write-Info "Verifying MSBuild target files..."
    foreach ($target in $ExpectedTargets.Keys) {
        $targetPath = Join-Path $ArtifactsPath $target
        if (Test-Path $targetPath) {
            Write-Success "Found target file: $target ($($ExpectedTargets[$target]))"
            $SuccessCount++
        } else {
            Write-Warning "Missing target file: $target"
            $WarningCount++
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
            $WarningCount++
        }
    }
    if (-not $foundTestArtifacts) { Write-Success "No unwanted test artifacts found"; $SuccessCount++ }

    Write-Info "Verifying basic executable functionality..."
    $nbExe = Join-Path $ArtifactsPath "nb.exe"
    if (Test-Path $nbExe) {
        try {
            $result = & $nbExe --version 2>&1
            if ($LASTEXITCODE -eq 0) { Write-Success "nb.exe executes successfully"; Write-Info "  Output: $result"; $SuccessCount++ } else { Write-Warning "nb.exe returned non-zero exit code: $LASTEXITCODE"; $WarningCount++ }
        } catch { Write-Warning "Failed to execute nb.exe: $($_.Exception.Message)"; $WarningCount++ }
    }

    $lfExe = Join-Path $ArtifactsPath "lf.exe"
    if (Test-Path $lfExe) {
        try {
            $result = & $lfExe --help 2>&1
            if ($LASTEXITCODE -eq 0) { Write-Success "lf.exe executes successfully"; $SuccessCount++ } else { Write-Warning "lf.exe returned non-zero exit code: $LASTEXITCODE"; $WarningCount++ }
        } catch { Write-Warning "Failed to execute lf.exe: $($_.Exception.Message)"; $WarningCount++ }
    }

    Write-Info "Verifying folder structure..."
    $folderCount = (Get-ChildItem -Path $ArtifactsPath -Directory -ErrorAction SilentlyContinue).Count
    $fileCount = (Get-ChildItem -Path $ArtifactsPath -File -ErrorAction SilentlyContinue).Count
    Write-Info "Found $folderCount subdirectories and $fileCount files"
    if ($fileCount -gt 10) { Write-Success "Artifact count looks reasonable ($fileCount files)"; $SuccessCount++ } else { Write-Warning "Low artifact count - expected more files ($fileCount files)"; $WarningCount++ }

    Write-Host "================================================================" -ForegroundColor Magenta
    Write-Host "                    VERIFICATION SUMMARY" -ForegroundColor Magenta
    Write-Host "================================================================" -ForegroundColor Magenta
    Write-Host "Artifacts Path: $ArtifactsPath" -ForegroundColor White
    Write-Host "[SUCCESS] Count: $SuccessCount" -ForegroundColor Green
    Write-Host "[WARNING] Count: $WarningCount" -ForegroundColor Yellow
    Write-Host "[ERROR] Count: $ErrorCount" -ForegroundColor Red

    return @{ Success = $SuccessCount; Warning = $WarningCount; Error = $ErrorCount }
}

#endregion

#region CodeSigning (merged from scripts/module-package/sign-trust.psm1)
# =============================================================================
# Code signing / trust functions (merged from scripts/module-package/sign-trust.psm1)
# =============================================================================

function Test-IsAdministrator {
    return ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

function Test-MicrosoftPowerShellSecurityModuleLoaded {
    try {
        Import-Module Microsoft.PowerShell.Security -ErrorAction Stop
        return $true
    } catch {
        Write-Error "Failed to import Microsoft.PowerShell.Security module: $($_.Exception.Message)"
        return $false
    }
}

function Test-CertificateStore {
    try {
        if (-not (Get-PSDrive -PSProvider Certificate -ErrorAction SilentlyContinue)) {
            Write-Error "Certificate PSProvider not available on this system."
            return $false
        }
        return $true
    } catch {
        Write-Error "Failed to access the Certificate PSProvider: $($_.Exception.Message)"
        return $false
    }
}

function New-SelfSignedCodeCertificate {
    param(
        [Parameter(Mandatory=$true)][string]$DnsName,
        [Parameter(Mandatory=$false)][string]$CertSubject = '',
        [Parameter(Mandatory=$false)][string]$CertStoreLocation = "Cert:\\CurrentUser\\My",
        [Parameter(Mandatory=$false)][int]$NotAfterYears = 5
    )

    try {
        if ([string]::IsNullOrWhiteSpace($CertSubject)) {
            $cert = New-SelfSignedCertificate -DnsName $DnsName -Type CodeSigningCert -CertStoreLocation $CertStoreLocation -NotAfter (Get-Date).AddYears($NotAfterYears)
        } else {
            # include subject if provided for compatibility with legacy scripts
            $cert = New-SelfSignedCertificate -DnsName $DnsName -Subject $CertSubject -Type CodeSigningCert -CertStoreLocation $CertStoreLocation -NotAfter (Get-Date).AddYears($NotAfterYears)
        }
        return $cert
    } catch {
        throw "Failed to create self-signed certificate: $($_.Exception.Message)"
    }
}

function Export-CertificateToPfx {
    param(
        [Parameter(Mandatory=$true)][System.Security.Cryptography.X509Certificates.X509Certificate2]$Certificate,
        [Parameter(Mandatory=$true)][string]$FilePath,
        [Parameter(Mandatory=$true)][System.Security.SecureString]$Password
    )

    try {
        Export-PfxCertificate -Cert $Certificate -FilePath $FilePath -Password $Password -Force
        return $FilePath
    } catch {
        throw "Failed to export certificate to PFX: $($_.Exception.Message)"
    }
}

function Export-CertificateToCer {
    param(
        [Parameter(Mandatory=$true)][System.Security.Cryptography.X509Certificates.X509Certificate2]$Certificate,
        [Parameter(Mandatory=$true)][string]$FilePath
    )

    try {
        Export-Certificate -Cert $Certificate -FilePath $FilePath -Force | Out-Null
        return $FilePath
    } catch {
        throw "Failed to export certificate to CER: $($_.Exception.Message)"
    }
}

function Import-CertificateToRoot {
    param(
        [Parameter(Mandatory=$true)][string]$CerFilePath
    )

    if (-not (Test-IsAdministrator)) {
        throw "Importing to LocalMachine Trusted Root requires Administrator rights."
    }

    try {
        Import-Certificate -FilePath $CerFilePath -CertStoreLocation "Cert:\\LocalMachine\\Root" | Out-Null
        return $true
    } catch {
        throw "Failed to import certificate to Trusted Root: $($_.Exception.Message)"
    }
}

function Import-CertificateToCurrentUser {
    param(
        [Parameter(Mandatory=$true)][string]$PfxFilePath,
        [Parameter(Mandatory=$true)][System.Security.SecureString]$Password
    )
    try {
        $cert = Import-PfxCertificate -FilePath $PfxFilePath -CertStoreLocation "Cert:\\CurrentUser\\My" -Password $Password -Exportable
        return $cert
    } catch {
        throw "Failed to import PFX to CurrentUser store: $($_.Exception.Message)"
    }
}

function Set-ScriptSignature {
    param(
        [Parameter(Mandatory=$true)][string]$ScriptPath,
        [Parameter(Mandatory=$true)][System.Security.Cryptography.X509Certificates.X509Certificate2]$Certificate
    )

    if (-not (Test-Path $ScriptPath)) {
        throw "Script path not found: $ScriptPath"
    }

    try {
        $sig = Set-AuthenticodeSignature -FilePath $ScriptPath -Certificate $Certificate
        return $sig
    } catch {
    throw ("Failed to sign script {0}: {1}" -f $ScriptPath, $_.Exception.Message)
    }
}

function Get-ScriptSignature {
    param(
        [Parameter(Mandatory=$true)][string]$ScriptPath
    )
    try {
        $signature = Get-AuthenticodeSignature -FilePath $ScriptPath
        return $signature
    } catch {
    throw ("Failed to verify signature for {0}: {1}" -f $ScriptPath, $_.Exception.Message)
    }
}

function Set-CodeSigningTrust {
    <#
    Creates a code-signing certificate, exports it, optionally trusts it at the machine level,
    imports to CurrentUser, signs the supplied scripts and verifies signatures.

    Parameters:
      -DnsName (string) : DNS name used for the self-signed cert (common name)
      -Location (string) : Directory to write .pfx/.cer files (will be created)
      -CertSubject (string) : Certificate subject (optional; default CN=<DnsName>)
      -CertPasswordPlain (string) : Plain text password for the PFX (optional; generated if empty)
      -ScriptPaths (string[]) : Scripts to sign (optional)
      -TrustMachine (switch) : If set, import CER into LocalMachine\Root (requires Admin)
      -NotAfterYears (int) : Certificate validity in years (default 5)
      -Force (switch) : Overwrite existing files if present
    #>
    param(
        [Parameter(Mandatory=$true)][string]$DnsName,
        [Parameter(Mandatory=$false)][string]$Location = "$(Join-Path -Path (Get-Location) -ChildPath 'certs')",
    [Parameter(Mandatory=$false)][string]$CertSubject = '',
    [Parameter(Mandatory=$false)][System.Security.SecureString]$CertPasswordPlain = $null,
        [Parameter(Mandatory=$false)][string[]]$ScriptPaths = @(),
        [Parameter(Mandatory=$false)][switch]$TrustMachine,
        [Parameter(Mandatory=$false)][int]$NotAfterYears = 5,
        [Parameter(Mandatory=$false)][switch]$Force
    )

    if (-not (Test-MicrosoftPowerShellSecurityModuleLoaded)) { throw "Required PowerShell Security module unavailable" }
    if (-not (Test-CertificateStore)) { throw "Certificate store unavailable" }

    if (-not $CertSubject) { $CertSubject = "CN=$DnsName" }

    # ensure location
    if (-not (Test-Path $Location)) { New-Item -ItemType Directory -Force -Path $Location | Out-Null }

    # password: prefer env:VTAPIKEY if present, otherwise use supplied SecureString or generate one
    if (($null -eq $CertPasswordPlain) -and -not [string]::IsNullOrWhiteSpace($env:VTAPIKEY)) {
        $CertPasswordPlain = ConvertTo-SecureString -String $env:VTAPIKEY -Force -AsPlainText
        Write-Host "Using VTAPIKEY environment variable as PFX password (converted to SecureString)."
    }

    if ($null -eq $CertPasswordPlain) {
        # generate a random password if not supplied
        $bytes = New-Object byte[] 16; (New-Object System.Security.Cryptography.RNGCryptoServiceProvider).GetBytes($bytes)
        $plain = [Convert]::ToBase64String($bytes)
        $CertPasswordPlain = ConvertTo-SecureString -String $plain -Force -AsPlainText
        Write-Host "Generated random password for PFX (kept only in memory)."
    }

    $securePassword = $CertPasswordPlain

    # create cert (pass CertSubject for compatibility)
    $cert = New-SelfSignedCodeCertificate -DnsName $DnsName -CertSubject $CertSubject -CertStoreLocation "Cert:\\CurrentUser\\My" -NotAfterYears $NotAfterYears

    # prepare paths
    $pfxPath = Join-Path $Location "$DnsName.pfx"
    $cerPath = Join-Path $Location "$DnsName.cer"

    if ((Test-Path $pfxPath -PathType Leaf -ErrorAction SilentlyContinue -ErrorVariable ev) -and (-not $Force)) {
        throw "PFX file already exists at $pfxPath. Use -Force to overwrite."
    }

    # export
    Export-CertificateToPfx -Certificate $cert -FilePath $pfxPath -Password $securePassword | Out-Null
    Export-CertificateToCer -Certificate $cert -FilePath $cerPath | Out-Null

    # optionally trust in LocalMachine - requires admin
    if ($TrustMachine) {
        if (-not (Test-IsAdministrator)) { throw "TrustMachine requested but caller is not Administrator." }
        Import-CertificateToRoot -CerFilePath $cerPath | Out-Null
        Write-Host "Imported $cerPath to LocalMachine\Root"
    }

    # import to current user
    $imported = Import-CertificateToCurrentUser -PfxFilePath $pfxPath -Password $securePassword

    # if no ScriptPaths were provided, default to legacy ff.ps1 location for compatibility
    if (($null -eq $ScriptPaths) -or ($ScriptPaths.Count -eq 0)) {
        $legacy = 'C:\\source\\ntools\\dev-setup\\ff.ps1'
        if (Test-Path $legacy) { $ScriptPaths = @($legacy) }
    }

    # sign provided scripts
    $signed = @()
    foreach ($script in $ScriptPaths) {
        try {
            $full = (Resolve-Path -Path $script).Path
            $sig = Set-ScriptSignature -ScriptPath $full -Certificate $imported
            $signed += @{ Script = $full; Status = $sig.Status; Signature = $sig }
            Write-Host "Signed script: $full (Status: $($sig.Status))"
        } catch {
            Write-Warning ("Failed to sign {0}: {1}" -f $script, $_.Exception.Message)
            $signed += @{ Script = $script; Status = 'Error'; Error = $_.Exception.Message }
        }
    }

    return @{ Certificate = $cert; Pfx = $pfxPath; Cer = $cerPath; Imported = $imported; Signed = $signed }
}

#endregion

#region Exports
# Final export of public functions (including merged signing functions)
Export-ModuleMember -Function Get-ntoolsScriptsVersion, Publish-AllProjects, Get-VersionFromJson, Update-DocVersions, Write-TestResult, Test-TargetExists, Test-TargetDependencies, Test-TargetDelegation, Get-FileHash256, Get-FileVersionInfo, Invoke-FastForward, Write-OutputMessage, Get-NToolsFileVersion, Add-DeploymentPathToEnvironment, Invoke-NToolsDownload, Install-NTools, Invoke-VerifyArtifacts, Set-DevelopmentEnvironment, Get-AgentPublicIp, Add-WafAllowRule, Remove-WafCustomRule, Test-IsAdministrator, Test-MicrosoftPowerShellSecurityModuleLoaded, Test-CertificateStore, New-SelfSignedCodeCertificate, Export-CertificateToPfx, Export-CertificateToCer, Import-CertificateToRoot, Import-CertificateToCurrentUser, Set-ScriptSignature, Get-ScriptSignature, Set-CodeSigningTrust

#endregion

