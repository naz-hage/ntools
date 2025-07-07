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


# Refactored: Use shared functions from repos.psm1
. (Join-Path $PSScriptRoot "import.ps1")

$params = Get-RepoCheckParameters -paramFile $paramFile
Start-RepoLog -logFile $params.logFile -paramFile $params.paramFile

Write-RepoLog "Parameters in use:" "Green" $params.logFile
Write-RepoLog "  Organization:   $($params.organization)" "White" $params.logFile
Write-RepoLog "  Repository:     $($params.repositoryName)" "White" $params.logFile
if ($params.project) {
    Write-RepoLog "  Specific Project: $($params.project)" "White" $params.logFile
} else {
    Write-RepoLog "  Search Scope:   All projects in organization" "White" $params.logFile
}

try {
    $projectsToSearch = if ($params.project) {
        @($params.project)
    } else {
        Get-AllAzDevOpsProjects -organization $params.organization -pat $params.pat
    }

    Write-RepoLog "Projects to search:" "Green" $params.logFile
    foreach ($proj in $projectsToSearch) {
        Write-RepoLog "  - $proj" "Yellow" $params.logFile
    }
    Write-RepoLog "" "White" $params.logFile

    $foundRepositories = @()
    foreach ($proj in $projectsToSearch) {
        Write-RepoLog "Checking project '$proj'..." "Cyan" $params.logFile
        $repositoryExists = CheckRepositoryExists -organization $params.organization -project $proj -repositoryName $params.repositoryName -personalAccessToken $params.pat
        if ($repositoryExists) {
            $repositoryUrl = "$($params.organization)/$proj/_git/$($params.repositoryName)"
            Write-RepoLog "✓ FOUND: Repository exists in project '$proj'" "Green" $params.logFile
            Write-RepoLog "  URL: $repositoryUrl" "Green" $params.logFile
            $foundRepositories += @{ Project = $proj; Url = $repositoryUrl }
        } else {
            Write-RepoLog "  Not found in project '$proj'" "Gray" $params.logFile
        }
    }

    Write-RepoCheckSummary -repositoryName $params.repositoryName -projects $projectsToSearch -found $foundRepositories -logFile $params.logFile

} catch {
    $errorMessage = "An error occurred: $($_.Exception.Message)"
    Write-RepoLog $errorMessage "Red" $params.logFile
    Write-Error $errorMessage
    if ($params.logFile) {
        Add-Content -Path $params.logFile -Value "[$((Get-Date -Format 'yyyy-MM-dd HH:mm:ss'))] ERROR: $($_.Exception.Message)" -Encoding UTF8
        Add-Content -Path $params.logFile -Value "[$((Get-Date -Format 'yyyy-MM-dd HH:mm:ss'))] Script execution failed." -Encoding UTF8
    }
} finally {
    if ($params.logFile) {
        Add-Content -Path $params.logFile -Value "[$((Get-Date -Format 'yyyy-MM-dd HH:mm:ss'))] Script execution completed." -Encoding UTF8
        Add-Content -Path $params.logFile -Value ("=" * 60) -Encoding UTF8
    }
}

