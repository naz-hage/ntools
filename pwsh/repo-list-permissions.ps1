<#
.SYNOPSIS
    Lists permissions for a specific repository in an Azure DevOps project.

.DESCRIPTION
    This script retrieves and displays the permissions for a specified repository in an Azure DevOps project.
    It uses the Azure DevOps REST API to fetch the repository ID, project ID, and associated permissions.
    The script reads configuration from a JSON parameter file (repo-list-permissions.param by default).

.PARAMETER paramFile
    The path to the JSON parameter file containing the configuration.
    If not specified, defaults to "repo-list-permissions.param" in the same directory as the script.

.EXAMPLE
    .\repo-list-permissions.ps1
    
    Uses the default repo-list-permissions.param file in the script directory.

.EXAMPLE
    .\repo-list-permissions.ps1 -paramFile "C:\config\my-list-permissions.param"
    
    Uses a custom parameter file from a specific location.

.NOTES
    - Requires Azure DevOps Personal Access Token (PAT) to be set in the environment variable 'PAT'
    - Parameter file must be valid JSON with required fields: repositoryName, organization, project
    - Optional fields: logFile (optional log file path)
    
    EXECUTION POLICY:
    If you encounter "cannot be loaded" or "not digitally signed" errors, you have several options:
    
    Option 1 - Change execution policy for current user (recommended for development):
    - Execute: Set-ExecutionPolicy -ExecutionPolicy Unrestricted -Scope CurrentUser
    - (No Administrator privileges required when using -Scope CurrentUser)
    
    Option 2 - Run with bypass for single execution:
    - powershell.exe -ExecutionPolicy Bypass -File ".\repo-list-permissions.ps1"
    - powershell.exe -ExecutionPolicy Bypass -File ".\repo-list-permissions.ps1" -paramFile ".\repo-list-permissions.param"
    
    Option 3 - Unblock the file:
    - Right-click the script file > Properties > Check "Unblock" if present > OK
    - Or run: Unblock-File -Path ".\repo-list-permissions.ps1"
    
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
    $paramFile = Join-Path $PSScriptRoot "repo-list-permissions.param"
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
        "Repository List Permissions Script Log"
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
    $checkResponse = CheckRepositoryExists -organization $organization -project $project -repositoryName $repositoryName -personalAccessToken $personalAccessToken
    if (-not $checkResponse) {
        throw "Repository '$repositoryName' was not found in project '$project'."
    }

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

    # URL encode the security token
    $encodedSecurityToken = [System.Web.HttpUtility]::UrlEncode($securityToken)

    # Construct the permissions URL
    $securityNamespaceId = "52d39943-cb85-4d7f-8fa8-c6baac873819"
    $permissionsUrl = "$organization/_apis/accesscontrolentries/$securityNamespaceId?token=$encodedSecurityToken&api-version=7.0"
    Write-LogOutput "DEBUG: Permissions URL: $permissionsUrl" "Cyan"

    # Get permissions for the repository
    $permissionsResponse = Invoke-RestMethod -Uri $permissionsUrl -Headers @{Authorization = "Basic " + [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes(":$personalAccessToken"))} -ErrorAction Stop

    # Output the permissions
    Write-LogOutput "Permissions for repository '$repositoryName':" "Green"
    $json = $permissionsResponse | ConvertTo-Json -Depth 10
    Write-Host $json
    if ($logFile) {
        Add-Content -Path $logFile -Value $json -Encoding UTF8
    }

} catch {
    $errorMessage = "An error occurred: $($_.Exception.Message)"
    Write-LogOutput $errorMessage "Red"
    # In PowerShell 7+, the response content may be disposed and inaccessible.
    # Log only the error message, exception type, and stack trace.
    Write-LogOutput "Exception Type: $($_.Exception.GetType().FullName)" "Red"
    Write-LogOutput "Stack Trace: $($_.Exception.StackTrace)" "Red"
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