<#
.SYNOPSIS
    Deletes a repository from an Azure DevOps project after confirmation.

.DESCRIPTION
    This script deletes a specified repository from an Azure DevOps project.
    It includes safety checks and requires explicit confirmation before proceeding with the deletion.
    The script reads configuration from a JSON parameter file (repo-delete.param by default).

.PARAMETER paramFile
    The path to the JSON parameter file containing the configuration.
    If not specified, defaults to "repo-delete.param" in the same directory as the script.

.EXAMPLE
    .\repo-delete.ps1
    
    Uses the default repo-delete.param file in the script directory.

.EXAMPLE
    .\repo-delete.ps1 -paramFile "C:\config\my-delete-config.param"
    
    Uses a custom parameter file from a specific location.

.NOTES
    - Requires Azure DevOps Personal Access Token (PAT) to be set in the environment variable 'PAT'
    - Parameter file must be valid JSON with required fields: repositoryName, organization, project
    - Optional fields: logFile (optional log file path)
    - THIS OPERATION IS IRREVERSIBLE - deleted repositories cannot be recovered
    
    EXECUTION POLICY:
    If you encounter "cannot be loaded" or "not digitally signed" errors, you have several options:
    
    Option 1 - Change execution policy for current user (recommended for development):
    - Execute: Set-ExecutionPolicy -ExecutionPolicy Unrestricted -Scope CurrentUser
    - (No Administrator privileges required when using -Scope CurrentUser)
    
    Option 2 - Run with bypass for single execution:
    - powershell.exe -ExecutionPolicy Bypass -File ".\repo-delete.ps1"
    - powershell.exe -ExecutionPolicy Bypass -File ".\repo-delete.ps1" -paramFile ".\repo-delete.param"
    
    Option 3 - Unblock the file:
    - Right-click the script file > Properties > Check "Unblock" if present > OK
    - Or run: Unblock-File -Path ".\repo-delete.ps1"
    
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
    $paramFile = Join-Path $PSScriptRoot "repo-delete.param"
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
$requiredParams = @('repositoryName', 'organization', 'project')
foreach ($param in $requiredParams) {
    if (-not $paramContent.$param -or [string]::IsNullOrWhiteSpace($paramContent.$param)) {
        throw "Required parameter '$param' is missing or empty in parameter file"
    }
}

# Assign variables from parameter file
$repositoryName = $paramContent.repositoryName
$organization = $paramContent.organization
$project = $paramContent.project

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
        "Repository Delete Script Log"
        "Started: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')"
        "Parameter File: $paramFile"
        "=" * 60
    )
    Set-Content -Path $logFile -Value $logHeader -Encoding UTF8
    Write-LogOutput "Logging to: $logFile" "Green"
}

Write-LogOutput "Parameters in use:" "Green"
Write-LogOutput "  Organization: $organization"
Write-LogOutput "  Project:      $project"
Write-LogOutput "  Repository:   $repositoryName"

$personalAccessToken = "$env:PAT"
if (-not $personalAccessToken) {
    throw "Personal Access Token (PAT) is not set. Please set the PAT in the environment variable 'PAT'."
}

try {
    # Check if the repository exists
    Write-LogOutput "Checking if repository exists..." "Cyan"
    $repositoryExists = CheckRepositoryExists -organization $organization -project $project -repositoryName $repositoryName -personalAccessToken $personalAccessToken

    if (-not $repositoryExists) {
        Write-LogOutput "Repository '$repositoryName' does not exist in project '$project'." "Yellow"
        Write-LogOutput "Nothing to delete." "Yellow"
        return
    }

    # Display warning and ask for confirmation
    Write-LogOutput "" 
    Write-LogOutput "WARNING: You are about to delete the following repository:" "Red"
    Write-LogOutput "  Organization: $organization" "Red"
    Write-LogOutput "  Project:      $project" "Red"
    Write-LogOutput "  Repository:   $repositoryName" "Red"
    Write-LogOutput ""
    Write-LogOutput "THIS ACTION IS IRREVERSIBLE!" "Red"
    Write-LogOutput "All code, history, branches, and associated data will be permanently lost." "Red"
    Write-LogOutput ""

    # Multiple confirmation prompts for safety
    $confirmation1 = Read-Host "Type the repository name '$repositoryName' to confirm"
    if ($confirmation1 -ne $repositoryName) {
        Write-LogOutput "Repository name confirmation failed. Operation cancelled." "Yellow"
        return
    }

    $confirmation2 = Read-Host "Are you absolutely sure you want to delete this repository? Type 'DELETE' to confirm"
    if ($confirmation2 -ne "DELETE") {
        Write-LogOutput "Final confirmation failed. Operation cancelled." "Yellow"
        return
    }

    Write-LogOutput "User confirmed deletion. Proceeding..." "Yellow"

    # Perform the deletion
    Write-LogOutput "Deleting repository '$repositoryName'..." "Red"
    $deleteResult = DeleteRepository -organization $organization -project $project -repositoryName $repositoryName -personalAccessToken $personalAccessToken

    if ($deleteResult) {
        Write-LogOutput "Repository '$repositoryName' has been successfully deleted." "Green"
    } else {
        Write-LogOutput "Failed to delete repository '$repositoryName'." "Red"
        throw "Repository deletion failed"
    }

} catch {
    $errorMessage = "An error occurred: $($_.Exception.Message)"
    Write-LogOutput $errorMessage "Red"
    if ($logFile) {
        Add-Content -Path $logFile -Value "[$((Get-Date -Format 'yyyy-MM-dd HH:mm:ss'))] ERROR: $($_.Exception.Message)" -Encoding UTF8
        Add-Content -Path $logFile -Value "[$((Get-Date -Format 'yyyy-MM-dd HH:mm:ss'))] Script execution failed." -Encoding UTF8
    }
    Write-Error $errorMessage
} finally {
    if ($logFile) {
        Add-Content -Path $logFile -Value "[$((Get-Date -Format 'yyyy-MM-dd HH:mm:ss'))] Script execution completed." -Encoding UTF8
        Add-Content -Path $logFile -Value ("=" * 60) -Encoding UTF8
    }
}
