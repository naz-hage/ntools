<#
.SYNOPSIS
    Checks if a repository exists across Azure DevOps projects.

.DESCRIPTION
    This script checks if a specified repository exists in Azure DevOps projects.
    It can search across all projects in an organization or a specific project.
    The script reads configuration from a JSON parameter file (repo-check.param by default).

.PARAMETER paramFile
    The path to the JSON parameter file containing the configuration.
    If not specified, defaults to "repo-check.param" in the same directory as the script.

.EXAMPLE
    .\repo-check.ps1
    
    Uses the default repo-check.param file in the script directory.

.EXAMPLE
    .\repo-check.ps1 -paramFile "C:\config\my-check-config.param"
    
    Uses a custom parameter file from a specific location.

.NOTES
    - Requires Azure DevOps Personal Access Token (PAT) to be set in the environment variable 'PAT'
    - Parameter file must be valid JSON with required fields: repositoryName, organization
    - Optional fields: project (if not specified, searches all projects), logFile (optional log file path)
    
    EXECUTION POLICY:
    If you encounter "cannot be loaded" or "not digitally signed" errors, you have several options:
    
    Option 1 - Change execution policy for current user (recommended for development):
    - Execute: Set-ExecutionPolicy -ExecutionPolicy Unrestricted -Scope CurrentUser
    - (No Administrator privileges required when using -Scope CurrentUser)
    
    Option 2 - Run with bypass for single execution:
    - powershell.exe -ExecutionPolicy Bypass -File ".\repo-check.ps1"
    - powershell.exe -ExecutionPolicy Bypass -File ".\repo-check.ps1" -paramFile ".\repo-check.param"
    
    Option 3 - Unblock the file:
    - Right-click the script file > Properties > Check "Unblock" if present > OK
    - Or run: Unblock-File -Path ".\repo-check.ps1"
    
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
    $paramFile = Join-Path $PSScriptRoot "repo-check.param"
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
$requiredParams = @('repositoryName', 'organization')
foreach ($param in $requiredParams) {
    if (-not $paramContent.$param -or [string]::IsNullOrWhiteSpace($paramContent.$param)) {
        throw "Required parameter '$param' is missing or empty in parameter file"
    }
}

# Assign variables from parameter file
$repositoryName = $paramContent.repositoryName
$organization = $paramContent.organization
$project = if ($paramContent.project -and -not [string]::IsNullOrWhiteSpace($paramContent.project)) {
    $paramContent.project
} else {
    $null
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
        "Repository Check Script Log"
        "Started: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')"
        "Parameter File: $paramFile"
        "=" * 60
    )
    Set-Content -Path $logFile -Value $logHeader -Encoding UTF8
    Write-LogOutput "Logging to: $logFile" "Green"
}

Write-LogOutput "Parameters in use:" "Green"
Write-LogOutput "  Organization:   $organization"
Write-LogOutput "  Repository:     $repositoryName"
if ($project) {
    Write-LogOutput "  Specific Project: $project"
} else {
    Write-LogOutput "  Search Scope:   All projects in organization"
}

# Variables
$personalAccessToken = "$env:PAT"
if (-not $personalAccessToken) {
    throw "Personal Access Token (PAT) is not set. Please set the PAT in the environment variable 'PAT'."
}

try {
    # Determine which projects to search
    $projectsToSearch = @()
    
    if ($project) {
        # Search specific project only
        $projectsToSearch = @($project)
        Write-LogOutput "Searching in specific project: $project" "Cyan"
    } else {
        # Search all projects in organization
        Write-LogOutput "Fetching all projects in organization..." "Cyan"
        
        $headers = @{ Authorization = "Basic " + [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes(":$personalAccessToken")) }
        $projectsUrl = "$organization/_apis/projects?api-version=7.1"

        try {
            $response = Invoke-RestMethod -Uri $projectsUrl -Headers $headers -Method Get
            if ($response.value) {
                foreach ($proj in $response.value) {
                    $projectsToSearch += $proj.name
                }
                Write-LogOutput "Found $($projectsToSearch.Count) projects in organization" "Green"
            } else {
                Write-LogOutput "No projects found in the organization." "Yellow"
                return
            }
        } catch {
            Write-LogOutput "Failed to fetch projects from the organization: $($_.Exception.Message)" "Red"
            throw "Failed to fetch projects from the organization: $_"
        }
    }

    # Display projects to search
    Write-LogOutput "Projects to search:" "Green"
    foreach ($proj in $projectsToSearch) {
        Write-LogOutput "  - $proj" "Yellow"
    }
    Write-LogOutput ""

    # Search for repository in projects
    $foundRepositories = @()
    
    foreach ($proj in $projectsToSearch) {
        Write-LogOutput "Checking project '$proj'..." "Cyan"
        
        $repositoryExists = CheckRepositoryExists -organization $organization -project $proj -repositoryName $repositoryName -personalAccessToken $personalAccessToken

        if ($repositoryExists) {
            $repositoryUrl = "$organization/$proj/_git/$repositoryName"
            Write-LogOutput "✓ FOUND: Repository exists in project '$proj'" "Green"
            Write-LogOutput "  URL: $repositoryUrl" "Green"
            $foundRepositories += @{
                Project = $proj
                Url = $repositoryUrl
            }
        } else {
            Write-LogOutput "  Not found in project '$proj'" "Gray"
        }
    }

    # Summary
    Write-LogOutput ""
    Write-LogOutput "=== SEARCH SUMMARY ===" "Cyan"
    Write-LogOutput "Repository Name: $repositoryName" "Cyan"
    Write-LogOutput "Projects Searched: $($projectsToSearch.Count)" "Cyan"
    Write-LogOutput "Repositories Found: $($foundRepositories.Count)" "Cyan"

    if ($foundRepositories.Count -gt 0) {
        Write-LogOutput ""
        Write-LogOutput "Repository found in the following locations:" "Green"
        foreach ($repo in $foundRepositories) {
            Write-LogOutput "  Project: $($repo.Project)" "Green"
            Write-LogOutput "  URL:     $($repo.Url)" "Green"
            Write-LogOutput ""
        }
    } else {
        Write-LogOutput ""
        Write-LogOutput "Repository '$repositoryName' was not found in any searched projects." "Yellow"
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

