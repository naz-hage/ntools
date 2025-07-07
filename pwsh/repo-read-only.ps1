
<#
.SYNOPSIS
    Sets a repository to read-only for a specific user in an Azure DevOps project.

.DESCRIPTION
    This script sets deny write permissions for a specified user on a repository in an Azure DevOps project.
    It reads configuration from a JSON parameter file (repo-read-only.param by default), logs all actions, and prompts for confirmation before making changes.

.PARAMETER paramFile
    The path to the JSON parameter file containing the configuration.
    If not specified, defaults to "repo-read-only.param" in the same directory as the script.

.EXAMPLE
    .\repo-read-only.ps1
    Uses the default repo-read-only.param file in the script directory.

.EXAMPLE
    .\repo-read-only.ps1 -paramFile "C:\config\my-read-only.param"
    Uses a custom parameter file from a specific location.

.NOTES
    - Requires Azure DevOps Personal Access Token (PAT) to be set in the environment variable 'PAT'
    - Parameter file must be valid JSON with required fields: repositoryName, organization, project, userId
    - Optional fields: logFile (optional log file path)

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
    $paramFile = Join-Path $PSScriptRoot "repo-read-only.param"
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
$requiredParams = @('repositoryName', 'organization', 'project', 'userId')
foreach ($param in $requiredParams) {
    if (-not $paramContent.$param -or [string]::IsNullOrWhiteSpace($paramContent.$param)) {
        throw "Required parameter '$param' is missing or empty in parameter file"
    }
}

# Assign variables from parameter file
$repositoryName = $paramContent.repositoryName
$organization = $paramContent.organization
$project = $paramContent.project
$userId = $paramContent.userId

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
    Write-Host $Message -ForegroundColor $ForegroundColor
    if ($logFile) {
        Add-Content -Path $logFile -Value $logMessage -Encoding UTF8
    }
}

Write-LogOutput "Using parameter file: $paramFile" "Green"

# Initialize log file with header
if ($logFile) {
    $logHeader = @(
        "=" * 60
        "Repository Read-Only Script Log"
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
Write-LogOutput "  UserId:       $userId"

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
        Write-LogOutput "Nothing to update." "Yellow"
        return
    }

    # Display warning and ask for confirmation
    Write-LogOutput ""
    Write-LogOutput "WARNING: You are about to set the following repository to read-only for a user:" "Red"
    Write-LogOutput "  Organization: $organization" "Red"
    Write-LogOutput "  Project:      $project" "Red"
    Write-LogOutput "  Repository:   $repositoryName" "Red"
    Write-LogOutput "  UserId:       $userId" "Red"
    Write-LogOutput ""
    Write-LogOutput "This will DENY write permissions for this user on the repository." "Red"
    Write-LogOutput ""

    $confirmation1 = Read-Host "Type the repository name '$repositoryName' to confirm"
    if ($confirmation1 -ne $repositoryName) {
        Write-LogOutput "Repository name confirmation failed. Operation cancelled." "Yellow"
        return
    }

    $confirmation2 = Read-Host "Are you absolutely sure you want to set this repository to read-only for this user? Type 'READONLY' to confirm"
    if ($confirmation2 -ne "READONLY") {
        Write-LogOutput "Final confirmation failed. Operation cancelled." "Yellow"
        return
    }

    Write-LogOutput "User confirmed read-only update. Proceeding..." "Yellow"

    # Get the repository ID
    $reposUrl = "$organization/$project/_apis/git/repositories?api-version=7.0"
    $reposResponse = Invoke-RestMethod -Uri $reposUrl -Headers @{Authorization = "Basic " + [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes(":$personalAccessToken"))} -ErrorAction Stop
    $repository = $reposResponse.value | Where-Object { $_.name -eq $repositoryName }
    if (-not $repository) {
        throw "Repository '$repositoryName' not found in project '$project'."
    }
    $repositoryId = $repository.id

    # Get the project ID
    $projectsUrl = "$organization/_apis/projects?api-version=7.0"
    $projectsResponse = Invoke-RestMethod -Uri $projectsUrl -Headers @{Authorization = "Basic " + [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes(":$personalAccessToken"))} -ErrorAction Stop
    $projectId = ($projectsResponse.value | Where-Object { $_.name -eq $project }).id
    if (-not $projectId) {
        throw "Project '$project' not found in organization '$organization'."
    }

    # Construct the security token
    $securityToken = "repoV2/$projectId/$repositoryId"
    Write-LogOutput "DEBUG: Security Token: $securityToken" "Cyan"

    # Construct the permissions URL
    $securityNamespaceId = "52d39943-cb85-4d7f-8fa8-c6baac873819"
    $permissionsUrl = "$organization/_apis/securitynamespaces/$securityNamespaceId/permissions/$securityToken?api-version=7.0"
    Write-LogOutput "DEBUG: Permissions URL: $permissionsUrl" "Cyan"

    # Set permissions to deny write access (-16 = Deny Write)
    $body = @{
        permissions = -16
        descriptor = "Microsoft.TeamFoundation.Identity;$userId"
    } | ConvertTo-Json -Depth 10

    $result = Invoke-RestMethod -Uri $permissionsUrl -Method Post -Headers @{Authorization = "Basic " + [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes(":$personalAccessToken"))} -Body $body -ContentType "application/json" -ErrorAction Stop

    Write-LogOutput "Repository '$repositoryName' has been set to read-only for user '$userId'." "Green"
    if ($logFile) {
        Add-Content -Path $logFile -Value ($result | ConvertTo-Json -Depth 10) -Encoding UTF8
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