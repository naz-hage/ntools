#region Shared parameter and logging functions for repo-check.ps1 refactor

function Get-RepoCheckParameters {
    param (
        [string]$paramFile = $null
    )
    # Determine parameter file path
    if (-not $paramFile) {
        $paramFile = Join-Path $PSScriptRoot "repo-check.param"
    }
    if (-not (Test-Path $paramFile)) {
        throw "Parameter file not found: $paramFile"
    }
    try {
        $paramContent = Get-Content $paramFile -Raw | ConvertFrom-Json
    } catch {
        throw "Failed to parse parameter file '$paramFile': $($_.Exception.Message)"
    }
    $requiredParams = @('repositoryName', 'organization')
    foreach ($param in $requiredParams) {
        if (-not $paramContent.$param -or [string]::IsNullOrWhiteSpace($paramContent.$param)) {
            throw "Required parameter '$param' is missing or empty in parameter file"
        }
    }
    $params = @{
        repositoryName = $paramContent.repositoryName
        organization   = $paramContent.organization
        project        = if ($paramContent.project -and -not [string]::IsNullOrWhiteSpace($paramContent.project)) { $paramContent.project } else { $null }
        logFile        = if ($paramContent.logFile -and -not [string]::IsNullOrWhiteSpace($paramContent.logFile)) {
                            if ([System.IO.Path]::IsPathRooted($paramContent.logFile)) {
                                $paramContent.logFile
                            } else {
                                Join-Path $PSScriptRoot $paramContent.logFile
                            }
                        } else { $null }
        paramFile      = $paramFile
        pat            = "$env:PAT"
    }
    if (-not $params.pat) {
        throw "Personal Access Token (PAT) is not set. Please set the PAT in the environment variable 'PAT'."
    }
    return $params
}

function Start-RepoLog {
    param(
        [string]$logFile,
        [string]$paramFile
    )
    if ($logFile) {
        $logHeader = @(
            "=" * 60
            "Repository Check Script Log"
            "Started: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')"
            "Parameter File: $paramFile"
            "=" * 60
        )
        Set-Content -Path $logFile -Value $logHeader -Encoding UTF8
        Write-Host "Logging to: $logFile" -ForegroundColor Green
    }
}

function Write-RepoLog {
    param(
        [string]$Message,
        [string]$ForegroundColor = "White",
        [string]$logFile = $null
    )
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    $logMessage = "[$timestamp] $Message"
    Write-Host $Message -ForegroundColor $ForegroundColor
    if ($logFile) {
        Add-Content -Path $logFile -Value $logMessage -Encoding UTF8
    }
}

function Get-AllAzDevOpsProjects {
    param(
        [string]$organization,
        [string]$pat
    )
    $headers = @{ Authorization = "Basic " + [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes(":$pat")) }
    $projectsUrl = "$organization/_apis/projects?api-version=7.1"
    $projectsToSearch = @()
    try {
        $response = Invoke-RestMethod -Uri $projectsUrl -Headers $headers -Method Get
        if ($response.value) {
            foreach ($proj in $response.value) {
                $projectsToSearch += $proj.name
            }
        }
    } catch {
        throw "Failed to fetch projects from the organization: $($_.Exception.Message)"
    }
    return $projectsToSearch
}

function Write-RepoCheckSummary {
    param(
        [string]$repositoryName,
        [array]$projects,
        [array]$found,
        [string]$logFile = $null
    )
    Write-RepoLog "" "White" $logFile
    Write-RepoLog "=== SEARCH SUMMARY ===" "Cyan" $logFile
    Write-RepoLog "Repository Name: $repositoryName" "Cyan" $logFile
    Write-RepoLog "Projects Searched: $($projects.Count)" "Cyan" $logFile
    Write-RepoLog "Repositories Found: $($found.Count)" "Cyan" $logFile
    if ($found.Count -gt 0) {
        Write-RepoLog "" "White" $logFile
        Write-RepoLog "Repository found in the following locations:" "Green" $logFile
        foreach ($repo in $found) {
            Write-RepoLog "  Project: $($repo.Project)" "Green" $logFile
            Write-RepoLog "  URL:     $($repo.Url)" "Green" $logFile
            Write-RepoLog "" "White" $logFile
        }
    } else {
        Write-RepoLog "" "White" $logFile
        Write-RepoLog "Repository '$repositoryName' was not found in any searched projects." "Yellow" $logFile
    }
}
#endregion
# Function to check if the repository exists in the target project
function CheckRepositoryExists {
    param (
        [string]$organization,
        [string]$project,
        [string]$repositoryName,
        [string]$personalAccessToken
    )

    # Base64 encode the PAT for authentication
    $base64AuthInfo = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes(":$personalAccessToken"))

    try {
        # Get the list of repositories in the target project
        $reposUrl = "$organization/$project/_apis/git/repositories?api-version=7.0"
        $reposResponse = Invoke-RestMethod -Uri $reposUrl -Headers @{ Authorization = "Basic $base64AuthInfo" } -ErrorAction Stop

        # Check if the repository exists
        $repository = $reposResponse.value | Where-Object { $_.name -eq $repositoryName }
        if ($repository) {
            Write-Host "Repository '$repositoryName' already exists in project '$project'." -ForegroundColor Yellow
            return $true
        } else {
            Write-Host "Repository '$repositoryName' does not exist in project '$project'." -ForegroundColor Red
            return $false
        }
    } catch {
        Write-Error "Failed to check if the repository exists: $($_.Exception.Message)"
        return $false
    }
}

# Function to create a repository in Azure DevOps
function CreateRepository {
    param (
        [string]$organization,
        [string]$project,
        [string]$repositoryName,
        [string]$personalAccessToken
    )

    # Base64 encode the PAT for authentication
    $base64AuthInfo = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes(":$personalAccessToken"))

    try {
        # Construct the API URL
        $createRepoUrl = "$organization/$project/_apis/git/repositories?api-version=7.0"

        # Define the request body
        $body = @{
            name = $repositoryName
        } | ConvertTo-Json -Depth 10

        # Make the API call to create the repository
        $response = Invoke-RestMethod -Uri $createRepoUrl -Method Post -Headers @{ Authorization = "Basic $base64AuthInfo" } -Body $body -ContentType "application/json" -ErrorAction Stop

        # Add a delay after repository creation
        Write-Host "Waiting for the repository to be fully available..."
        Start-Sleep -Seconds 10

        # check if the repository was created successfully
        $checkResponse = CheckRepositoryExists -organization $organization -project $project -repositoryName $repositoryName -personalAccessToken $personalAccessToken
        if (-not $checkResponse) {
            throw "Repository '$repositoryName' was not created successfully."
        }

        Write-Host "Repository '$repositoryName' successfully created in project '$project'." -ForegroundColor Green
        return $response
    } catch {
        Write-Error "Failed to create the repository: $($_.Exception.Message)"
        Write-Host "DEBUG: Full Error Details: $($_ | ConvertTo-Json -Depth 10)" -ForegroundColor Red
        return $null
    }
}

# Function to delete a repository in Azure DevOps
function DeleteRepository {
    param (
        [string]$organization,
        [string]$project,
        [string]$repositoryName,
        [string]$personalAccessToken
    )

    # Base64 encode the PAT for authentication
    $base64AuthInfo = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes(":$personalAccessToken"))

    try {
        # Get the list of repositories to find the repository ID
        $reposUrl = "$organization/$project/_apis/git/repositories?api-version=7.0"
        Write-Host "DEBUG: Repos URL: $reposUrl" -ForegroundColor Cyan
        $reposResponse = Invoke-RestMethod -Uri $reposUrl -Headers @{ Authorization = "Basic $base64AuthInfo" } -ErrorAction Stop

        # Find the repository by name
        $repository = $reposResponse.value | Where-Object { $_.name -eq $repositoryName }
        if (-not $repository) {
            Write-Error "Repository '$repositoryName' does not exist in project '$project'."
            return $false
        }

        $repositoryId = $repository.id

        # Output the repository ID for confirmation
        Write-Host "Repository ID for '$repositoryName': $repositoryId" -ForegroundColor Cyan

        # Construct the API URL for deletion
        # https://dev.azure.com/{organization}/{project}/_apis/git/repositories/{repositoryId}?api-version=7.0
        
        # Correctly construct the API URL for deletion You can either remove the braces entirely 
        # or use subexpression $() if you need complex expressions.

        $deleteRepoUrl = "$organization/$project/_apis/git/repositories/$($repositoryId)?api-version=7.0"
        Write-Host "DEBUG: Delete Repo URL: $deleteRepoUrl" -ForegroundColor Cyan

        # Ask for confirmation before proceeding
        $confirmation = Read-Host "Are you sure you want to delete the repository '$repositoryName' in project '$project'? Type 'yes' to confirm"
        if ($confirmation -ne "yes") {
            Write-Host "Deletion canceled by the user." -ForegroundColor Yellow
            return $false
        }

        # Make the API call to delete the repository
        $response = Invoke-RestMethod -Uri $deleteRepoUrl -Method Delete -Headers @{
            Authorization = "Basic $base64AuthInfo"
            Accept = "application/json; api-version=7.0"
        } -ErrorAction Stop

        Write-Host "Repository '$repositoryName' successfully deleted from project '$project'." -ForegroundColor Green
        return $true
    } catch {
        Write-Error "Failed to delete the repository: $($_.Exception.Message)"
        Write-Host "DEBUG: Full Error Details: $($_ | ConvertTo-Json -Depth 20)" -ForegroundColor Red
        return $false
    }
}

Export-ModuleMember -Function *