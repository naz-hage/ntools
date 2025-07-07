
<#
.SYNOPSIS
    Lists all repositories in an Azure DevOps project and their associated YAML pipelines.

.DESCRIPTION
    This script retrieves all repositories in a specified Azure DevOps project and lists all YAML pipelines associated with each repository.
    It reads configuration from a JSON parameter file (repos-pipelines.param by default), logs all actions, and provides robust error handling.

.PARAMETER paramFile
    The path to the JSON parameter file containing the configuration.
    If not specified, defaults to "repos-pipelines.param" in the same directory as the script.

.EXAMPLE
    .\repos-pipelines.ps1
    Uses the default repos-pipelines.param file in the script directory.

.EXAMPLE
    .\repos-pipelines.ps1 -paramFile "C:\config\my-pipelines.param"
    Uses a custom parameter file from a specific location.

.NOTES
    - Requires Azure DevOps Personal Access Token (PAT) to be set in the environment variable 'PAT'
    - Parameter file must be valid JSON with required fields: organization, project
    - Optional fields: logFile (optional log file path)

.LINK
    https://docs.microsoft.com/en-us/azure/devops/pipelines/
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
    $paramFile = Join-Path $PSScriptRoot "repos-pipelines.param"
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
$requiredParams = @('organization', 'project')
foreach ($param in $requiredParams) {
    if (-not $paramContent.$param -or [string]::IsNullOrWhiteSpace($paramContent.$param)) {
        throw "Required parameter '$param' is missing or empty in parameter file"
    }
}

# Assign variables from parameter file
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
        "Repository Pipelines Script Log"
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

$personalAccessToken = "$env:PAT"
if (-not $personalAccessToken) {
    throw "Personal Access Token (PAT) is not set. Please set the PAT in the environment variable 'PAT'."
}

try {
    # Get all repositories in the project
    $reposUrl = "$organization/$project/_apis/git/repositories?api-version=7.0"
    $reposResponse = Invoke-RestMethod -Uri $reposUrl -Headers @{Authorization = "Basic " + [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes(":$personalAccessToken"))} -ErrorAction Stop

    foreach ($repo in $reposResponse.value) {
        Write-LogOutput "Repository: $($repo.name)" "Cyan"
        $repoId = $repo.id
        try {
            $pipelinesUrl = "$organization/$project/_apis/pipelines?api-version=7.0"
            $pipelinesResponse = Invoke-RestMethod -Uri $pipelinesUrl -Headers @{Authorization = "Basic " + [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes(":$personalAccessToken"))} -ErrorAction Stop
            foreach ($pipeline in $pipelinesResponse.value) {
                $pipelineDetailsUrl = "$organization/$project/_apis/pipelines/$($pipeline.id)?api-version=7.0"
                $pipelineDetails = Invoke-RestMethod -Uri $pipelineDetailsUrl -Headers @{Authorization = "Basic " + [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes(":$personalAccessToken"))} -ErrorAction Stop
                Write-LogOutput "DEBUG: Detailed Pipeline Metadata: $($pipelineDetails | ConvertTo-Json -Depth 10)" "Gray"
                if ($pipelineDetails.repository -and $pipelineDetails.repository.id -eq $repoId) {
                    Write-LogOutput "  Pipeline: $($pipelineDetails.name)" "Green"
                    Write-LogOutput "  YAML Path: $($pipelineDetails.configuration.path)" "Green"
                } else {
                    Write-LogOutput "  Pipeline '$($pipelineDetails.name)' is not associated with repository '$($repo.name)'." "Yellow"
                }
            }
        } catch {
            $errMsg = "Failed to retrieve pipelines for repository '$($repo.name)'. Error: $($_.Exception.Message)"
            Write-LogOutput $errMsg "Red"
        }
    }
} catch {
    $errMsg = "Failed to retrieve repositories. Error: $($_.Exception.Message)"
    Write-LogOutput $errMsg "Red"
    throw $errMsg
} finally {
    if ($logFile) {
        Add-Content -Path $logFile -Value "[$((Get-Date -Format 'yyyy-MM-dd HH:mm:ss'))] Script execution completed." -Encoding UTF8
        Add-Content -Path $logFile -Value ("=" * 60) -Encoding UTF8
    }
}