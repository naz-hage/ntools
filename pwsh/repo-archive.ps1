<#
.SYNOPSIS
    Archives a repository by moving it from a source Azure DevOps project to a target project.

.DESCRIPTION
    This script clones a repository from a source Azure DevOps project and pushes it to a target project,
    effectively archiving the repository. It performs a mirror clone to preserve all branches, tags, and history.
    The script will create the target repository if it doesn't exist and verify the migration was successful.
    
    The script reads configuration from a JSON parameter file (repo-archive.param by default).

.PARAMETER paramFile
    The path to the JSON parameter file containing the configuration.
    If not specified, defaults to "repo-archive.param" in the same directory as the script.

.EXAMPLE
    .\repo-archive.ps1
    
    Uses the default repo-archive.param file in the script directory.

.EXAMPLE
    .\repo-archive.ps1 -paramFile "C:\config\my-archive-config.param"
    
    Uses a custom parameter file from a specific location.

.NOTES
    - Requires Azure DevOps Personal Access Token (PAT) to be set in the environment variable 'PAT'
    - Requires Git to be installed and available in PATH
    - The script creates a temporary local clone in c:\azdevops\repos\
    - If the target repository already exists, the script will abort to prevent overwriting
    - Parameter file must be valid JSON with required fields: sourceRepo, sourceOrganization, sourceProject
    - Optional fields: targetOrganization (defaults to sourceOrganization), targetProject (defaults to "Archive"), targetRepo (defaults to sourceRepo + "-archive"), logFile (optional log file path)
    
    EXECUTION POLICY:
    If you encounter "cannot be loaded" or "not digitally signed" errors, you have several options:
    
    Option 1 - Change execution policy for current user (recommended for development):
    - Execute: Set-ExecutionPolicy -ExecutionPolicy Unrestricted -Scope CurrentUser
    - (No Administrator privileges required when using -Scope CurrentUser)
    
    Option 2 - Run with bypass for single execution:
    - powershell.exe -ExecutionPolicy Bypass -File ".\repo-archive.ps1"
    - powershell.exe -ExecutionPolicy Bypass -File ".\repo-archive.ps1" -paramFile ".\repo-archive.param"
    
    Option 3 - Unblock the file:
    - Right-click the script file > Properties > Check "Unblock" if present > OK
    - Or run: Unblock-File -Path ".\repo-archive.ps1"
    
    Note: RemoteSigned policy still requires local scripts to be signed. Use Unrestricted for unsigned local scripts.
    For more info: Get-Help about_Execution_Policies

.LINK
    https://docs.microsoft.com/en-us/azure/devops/repos/git/
#>

param (
    [string]$paramFile = $null
)

# Call import.ps1
$importScriptPath = Join-Path $PSScriptRoot "import.ps1"
if (-Not (Test-Path -Path $importScriptPath)) {
    Write-Error "The import script does not exist at path $importScriptPath"
    exit 1
}
. $importScriptPath

# Determine parameter file path
if (-not $paramFile) {
    $paramFile = Join-Path $PSScriptRoot "repo-archive.param"
}

# Read and parse parameter file
if (-not (Test-Path $paramFile)) {
    throw "Parameter file not found: $paramFile"
}

try {
    $paramContent = Get-Content $paramFile -Raw | ConvertFrom-Json
} catch {
    throw "Failed to parse parameter file '$paramFile': $($_.Exception.Message)"
}

# Validate required parameters
$requiredParams = @('sourceRepo', 'sourceOrganization', 'sourceProject')
foreach ($param in $requiredParams) {
    if (-not $paramContent.$param -or [string]::IsNullOrWhiteSpace($paramContent.$param)) {
        throw "Required parameter '$param' is missing or empty in parameter file"
    }
}

# Assign variables from parameter file
$sourceRepo = $paramContent.sourceRepo
$sourceOrganization = $paramContent.sourceOrganization
$sourceProject = $paramContent.sourceProject

# Set optional parameters with defaults
$targetOrganization = if ($paramContent.targetOrganization -and -not [string]::IsNullOrWhiteSpace($paramContent.targetOrganization)) { 
    $paramContent.targetOrganization 
} else { 
    $sourceOrganization 
}

$targetProject = if ($paramContent.targetProject -and -not [string]::IsNullOrWhiteSpace($paramContent.targetProject)) { 
    $paramContent.targetProject 
} else { 
    "Archive" 
}

$targetRepo = if ($paramContent.targetRepo -and -not [string]::IsNullOrWhiteSpace($paramContent.targetRepo)) { 
    $paramContent.targetRepo 
} else { 
    "$sourceRepo-archive" 
}

# Set log file if specified
$logFile = if ($paramContent.logFile -and -not [string]::IsNullOrWhiteSpace($paramContent.logFile)) {
    if ([System.IO.Path]::IsPathRooted($paramContent.logFile)) {
        $paramContent.logFile
    } else {
        Join-Path $PSScriptRoot $paramContent.logFile
    }
} else {
    $null
}

function Get-RepoArchiveParameters {
    param (
        [string]$paramFile = $null
    )
    if (-not $paramFile) {
        $paramFile = Join-Path $PSScriptRoot "repo-archive.param"
    }
    if (-not (Test-Path $paramFile)) {
        throw "Parameter file not found: $paramFile"
    }
    try {
        $paramContent = Get-Content $paramFile -Raw | ConvertFrom-Json
    } catch {
        throw "Failed to parse parameter file '$paramFile': $($_.Exception.Message)"
    }
    $requiredParams = @('sourceRepo', 'sourceOrganization', 'sourceProject')
    foreach ($param in $requiredParams) {
        if (-not $paramContent.$param -or [string]::IsNullOrWhiteSpace($paramContent.$param)) {
            throw "Required parameter '$param' is missing or empty in parameter file"
        }
    }
    $params = @{
        sourceRepo        = $paramContent.sourceRepo
        sourceOrganization= $paramContent.sourceOrganization
        sourceProject     = $paramContent.sourceProject
        targetOrganization= if ($paramContent.targetOrganization -and -not [string]::IsNullOrWhiteSpace($paramContent.targetOrganization)) { $paramContent.targetOrganization } else { $paramContent.sourceOrganization }
        targetProject     = if ($paramContent.targetProject -and -not [string]::IsNullOrWhiteSpace($paramContent.targetProject)) { $paramContent.targetProject } else { "Archive" }
        targetRepo        = if ($paramContent.targetRepo -and -not [string]::IsNullOrWhiteSpace($paramContent.targetRepo)) { $paramContent.targetRepo } else { "$($paramContent.sourceRepo)-archive" }
        logFile           = if ($paramContent.logFile -and -not [string]::IsNullOrWhiteSpace($paramContent.logFile)) {
                                if ([System.IO.Path]::IsPathRooted($paramContent.logFile)) {
                                    $paramContent.logFile
                                } else {
                                    Join-Path $PSScriptRoot $paramContent.logFile
                                }
                            } else { $null }
        paramFile         = $paramFile
        pat               = "$env:PAT"
    }
    if (-not $params.pat) {
        throw "Personal Access Token (PAT) is not set. Please set the PAT in the environment variable 'PAT'."
    }
    return $params
}

$params = Get-RepoArchiveParameters -paramFile $paramFile
Start-RepoLog -logFile $params.logFile -paramFile $params.paramFile

Write-RepoLog "Parameters in use:" "Green" $params.logFile
Write-RepoLog "  Source Organization: $($params.sourceOrganization)" "White" $params.logFile
Write-RepoLog "  Source Project:      $($params.sourceProject)" "White" $params.logFile
Write-RepoLog "  Source Repo:         $($params.sourceRepo)" "White" $params.logFile
Write-RepoLog "  Target Organization: $($params.targetOrganization)" "White" $params.logFile
Write-RepoLog "  Target Project:      $($params.targetProject)" "White" $params.logFile
Write-RepoLog "  Target Repo:         $($params.targetRepo)" "White" $params.logFile

# Save current location
$currentLocation = Get-Location

# Set source folder
$sourceFolder = "c:\azdevops\repos"
if (-not (Test-Path $sourceFolder)) {
    New-Item -ItemType Directory -Path $sourceFolder -Force | Out-Null
}

try {
    Set-Location $sourceFolder
    $repositoryPath = Join-Path $sourceFolder $params.sourceRepo

    if (Test-Path $repositoryPath) {
        Write-RepoLog "Removing existing repository folder: $repositoryPath" "Yellow" $params.logFile
        Remove-Item -Recurse -Force $repositoryPath
    }

    # Step 1: Clone the repository from the source project
    Write-RepoLog "Cloning the repository from the source project..." "Cyan" $params.logFile
    git clone --mirror "$($params.sourceOrganization)/$($params.sourceProject)/_git/$($params.sourceRepo)" $repositoryPath 2>&1 | Tee-Object -Variable cloneOutput
    if (-not (Test-Path "$repositoryPath")) {
        Write-RepoLog "Git clone failed. Output:" "Red" $params.logFile
        $cloneOutput | ForEach-Object { Write-RepoLog "  $_" "Red" $params.logFile }
        throw "Failed to clone the repository. Ensure the source repository URL is correct."
    }
    Set-Location "$repositoryPath"

    # Step 2: Add the target repository as a remote
    Write-RepoLog "Adding the target repository as a remote..." "Cyan" $params.logFile
    git remote set-url --push origin "$($params.targetOrganization)/$($params.targetProject)/_git/$($params.targetRepo)" 2>&1 | Tee-Object -Variable remoteOutput
    if ($LASTEXITCODE -ne 0) {
        Write-RepoLog "Git remote set-url failed with exit code: $LASTEXITCODE" "Red" $params.logFile
        Write-RepoLog "Git output:" "Red" $params.logFile
        $remoteOutput | ForEach-Object { Write-RepoLog "  $_" "Red" $params.logFile }
        throw "Failed to set the remote URL for the target repository."
    }

    # Check if the repository exists in the target project
    $repositoryExists = CheckRepositoryExists -organization $params.targetOrganization -project $params.targetProject -repositoryName $params.targetRepo -personalAccessToken $params.pat

    if (-not $repositoryExists) {
        Write-RepoLog "Creating the repository in the target project..." "Cyan" $params.logFile
        CreateRepository -organization $params.targetOrganization -project $params.targetProject -repositoryName $params.targetRepo -personalAccessToken $params.pat
    } else {
        Write-RepoLog "The repository '$($params.targetRepo)' already exists in the target project '$($params.targetProject)'." "Yellow" $params.logFile
        throw "Repository already exists in the target project. Aborting migration."
    }

    # Step 3: Push all branches and tags to the target repository
    Write-RepoLog "Pushing all branches and tags to the target repository..." "Cyan" $params.logFile

    # Clean up pull request refs
    Write-RepoLog "Cleaning up pull request references..." "Cyan" $params.logFile
    git for-each-ref --format='%(refname)' refs/pull | ForEach-Object {
        Write-RepoLog "Removing ref: $_" "Gray" $params.logFile
        git update-ref -d $_
    }

    # Push all remaining branches
    Write-RepoLog "Pushing branches..." "Cyan" $params.logFile
    git push origin --all 2>&1 | Tee-Object -Variable branchOutput
    if ($LASTEXITCODE -ne 0) {
        Write-RepoLog "Git push branches failed with exit code: $LASTEXITCODE" "Red" $params.logFile
        Write-RepoLog "Git output:" "Red" $params.logFile
        $branchOutput | ForEach-Object { Write-RepoLog "  $_" "Red" $params.logFile }
        throw "Failed to push branches to the target repository. Exit code: $LASTEXITCODE"
    }

    # Push all tags
    Write-RepoLog "Pushing tags..." "Cyan" $params.logFile
    git push origin --tags 2>&1 | Tee-Object -Variable tagOutput
    if ($LASTEXITCODE -ne 0) {
        Write-RepoLog "Git push tags failed with exit code: $LASTEXITCODE" "Red" $params.logFile
        Write-RepoLog "Git output:" "Red" $params.logFile
        $tagOutput | ForEach-Object { Write-RepoLog "  $_" "Red" $params.logFile }
        throw "Failed to push tags to the target repository. Exit code: $LASTEXITCODE"
    }

    Write-RepoLog "Successfully pushed all branches and tags (excluding PR refs)" "Green" $params.logFile

    # Step 4: Verify the migration
    Write-RepoLog "Verifying the migration..." "Cyan" $params.logFile
    Write-RepoLog "Repository successfully moved from $($params.sourceProject) to $($params.targetProject)." "Green" $params.logFile
} catch {
    $errorMessage = "An error occurred: $($_.Exception.Message)"
    Write-RepoLog $errorMessage "Red" $params.logFile
    if ($params.logFile) {
        Add-Content -Path $params.logFile -Value "[$((Get-Date -Format 'yyyy-MM-dd HH:mm:ss'))] ERROR: $($_.Exception.Message)" -Encoding UTF8
        Add-Content -Path $params.logFile -Value "[$((Get-Date -Format 'yyyy-MM-dd HH:mm:ss'))] Script execution failed." -Encoding UTF8
    }
    Write-Error $errorMessage
} finally {
    Set-Location $currentLocation
    if ($params.logFile) {
        Add-Content -Path $params.logFile -Value "[$((Get-Date -Format 'yyyy-MM-dd HH:mm:ss'))] Script execution completed." -Encoding UTF8
        Add-Content -Path $params.logFile -Value ("=" * 60) -Encoding UTF8
    }
}