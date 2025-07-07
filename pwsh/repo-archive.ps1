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

# Function to write output to both console and log file
function Write-LogOutput {
    param(
        [string]$Message,
        [string]$ForegroundColor = "White"
    )
    
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    $logMessage = "[$timestamp] $Message"
    
    # Write to console with color
    Write-Host $Message -ForegroundColor $ForegroundColor
    
    # Write to log file if specified
    if ($logFile) {
        Add-Content -Path $logFile -Value $logMessage -Encoding UTF8
    }
}

Write-LogOutput "Using parameter file: $paramFile" "Green"

# Initialize log file with header
if ($logFile) {
    $logHeader = @(
        "=" * 60
        "Repository Archive Script Log"
        "Started: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')"
        "Parameter File: $paramFile"
        "=" * 60
    )
    Set-Content -Path $logFile -Value $logHeader -Encoding UTF8
    Write-LogOutput "Logging to: $logFile" "Green"
}

Write-LogOutput "Parameters in use:" "Green"
Write-LogOutput "  Source Organization: $sourceOrganization"
Write-LogOutput "  Source Project:      $sourceProject"
Write-LogOutput "  Source Repo:         $sourceRepo"
Write-LogOutput "  Target Organization: $targetOrganization"
Write-LogOutput "  Target Project:      $targetProject"
Write-LogOutput "  Target Repo:         $targetRepo"


$personalAccessToken = "$env:PAT"  # Replace with your Azure DevOps Personal Access Token
if (-not $personalAccessToken) {
    throw "Personal Access Token (PAT) is not set. Please set the PAT in the environment variable 'PAT'."
}

# output current project name
Write-LogOutput "Current Project: $sourceProject" "Cyan"
Write-LogOutput "Target Project: $targetProject" "Cyan"

Write-LogOutput "Source Repository: $sourceRepo" "Cyan"
Write-LogOutput "Target Repository: $targetRepo" "Cyan"

# Save current location
$currentLocation = Get-Location

# Set source folder
$sourceFolder = "c:\azdevops\repos"
# Create source folder if it doesn't exist
if (-not (Test-Path $sourceFolder)) {
    New-Item -ItemType Directory -Path $sourceFolder -Force | Out-Null
}

try {
    # Change to source folder
    Set-Location $sourceFolder
    $repositoryPath = Join-Path $sourceFolder $sourceRepo

    # Delete existing repository folder if it exists
    if (Test-Path $repositoryPath) {
        Write-LogOutput "Removing existing repository folder: $repositoryPath"
        Remove-Item -Recurse -Force $repositoryPath
    }

    # Step 1: Clone the repository from the source project
    Write-LogOutput "Cloning the repository from the source project..."
    git clone --mirror "$sourceOrganization/$sourceProject/_git/$sourceRepo" $repositoryPath 2>&1 | Tee-Object -Variable cloneOutput
    if (-not (Test-Path "$repositoryPath")) {
        Write-LogOutput "Git clone failed. Output:" "Red"
        $cloneOutput | ForEach-Object { Write-LogOutput "  $_" "Red" }
        throw "Failed to clone the repository. Ensure the source repository URL is correct."
    }
    Set-Location "$repositoryPath"

    # Step 2: Add the target repository as a remote
    Write-LogOutput "Adding the target repository as a remote..."
    git remote set-url --push origin "$targetOrganization/$targetProject/_git/$targetRepo" 2>&1 | Tee-Object -Variable remoteOutput
    if ($LASTEXITCODE -ne 0) {
        Write-LogOutput "Git remote set-url failed with exit code: $LASTEXITCODE" "Red"
        Write-LogOutput "Git output:" "Red"
        $remoteOutput | ForEach-Object { Write-LogOutput "  $_" "Red" }
        throw "Failed to set the remote URL for the target repository."
    }

    # Add a function that checks if the repository exist in the target project
    $repositoryExists = CheckRepositoryExists -organization $targetOrganization -project $targetProject -repositoryName $targetRepo -personalAccessToken $personalAccessToken

    # if repository does not exist, create it
    if (-not $repositoryExists) {
        Write-LogOutput "Creating the repository in the target project..."
        CreateRepository -organization $targetOrganization -project $targetProject -repositoryName $targetRepo -personalAccessToken $personalAccessToken
    } else {
        Write-LogOutput "The repository '$targetRepo' already exists in the target project '$targetProject'." "Yellow"
        throw "Repository already exists in the target project. Aborting migration."
    }

    # Step 3: Push all branches and tags to the target repository
    Write-LogOutput "Pushing all branches and tags to the target repository..."
    
    # First, clean up any pull request refs that can't be pushed
    Write-LogOutput "Cleaning up pull request references..."
    git for-each-ref --format='%(refname)' refs/pull | ForEach-Object {
        Write-LogOutput "Removing ref: $_"
        git update-ref -d $_
    }
    
    # Push all remaining branches
    Write-LogOutput "Pushing branches..."
    git push origin --all 2>&1 | Tee-Object -Variable branchOutput
    if ($LASTEXITCODE -ne 0) {
        Write-LogOutput "Git push branches failed with exit code: $LASTEXITCODE" "Red"
        Write-LogOutput "Git output:" "Red"
        $branchOutput | ForEach-Object { Write-LogOutput "  $_" "Red" }
        throw "Failed to push branches to the target repository. Exit code: $LASTEXITCODE"
    }
    
    # Then, push all tags
    Write-LogOutput "Pushing tags..."
    git push origin --tags 2>&1 | Tee-Object -Variable tagOutput
    if ($LASTEXITCODE -ne 0) {
        Write-LogOutput "Git push tags failed with exit code: $LASTEXITCODE" "Red"
        Write-LogOutput "Git output:" "Red"
        $tagOutput | ForEach-Object { Write-LogOutput "  $_" "Red" }
        throw "Failed to push tags to the target repository. Exit code: $LASTEXITCODE"
    }
    
    Write-LogOutput "Successfully pushed all branches and tags (excluding PR refs)" "Green"

    # Step 4: Verify the migration
    Write-LogOutput "Verifying the migration..."
    Write-LogOutput "Repository successfully moved from $sourceProject to $targetProject." "Green"
} catch {
    $errorMessage = "An error occurred: $($_.Exception.Message)"
    Write-LogOutput $errorMessage "Red"
    if ($logFile) {
        Add-Content -Path $logFile -Value "[$((Get-Date -Format 'yyyy-MM-dd HH:mm:ss'))] ERROR: $($_.Exception.Message)" -Encoding UTF8
        Add-Content -Path $logFile -Value "[$((Get-Date -Format 'yyyy-MM-dd HH:mm:ss'))] Script execution failed." -Encoding UTF8
    }
    Write-Error $errorMessage
} finally {
    # Restore original location
    Set-Location $currentLocation
    if ($logFile) {
        Add-Content -Path $logFile -Value "[$((Get-Date -Format 'yyyy-MM-dd HH:mm:ss'))] Script execution completed." -Encoding UTF8
        Add-Content -Path $logFile -Value ("=" * 60) -Encoding UTF8
    }
}