<#
.SYNOPSIS
    Verifies that a repository has been successfully archived by comparing the source and archived repositories.

.DESCRIPTION
    This script compares the source and archived repositories in Azure DevOps to ensure the archive operation was successful.
    It checks for the existence of both repositories, compares branch and tag counts, and optionally verifies the latest commit hashes.
    The script reads configuration from a JSON parameter file (repo-verify.param by default).

.PARAMETER paramFile
    The path to the JSON parameter file containing the configuration.
    If not specified, defaults to "repo-verify.param" in the same directory as the script.

.EXAMPLE
    .\repo-verify.ps1
    Uses the default repo-verify.param file in the script directory.

.EXAMPLE
    .\repo-verify.ps1 -paramFile "C:\config\my-verify-config.param"
    Uses a custom parameter file from a specific location.

.NOTES
    - Requires Azure DevOps Personal Access Token (PAT) to be set in the environment variable 'PAT'
    - Parameter file must be valid JSON with required fields: sourceRepo, sourceOrganization, sourceProject, targetRepo, targetOrganization, targetProject
    - Optional fields: logFile (optional log file path)

.LINK
    https://docs.microsoft.com/en-us/azure/devops/repos/git/
#>

param (
    [string]$paramFile = $null
)

. (Join-Path $PSScriptRoot "import.ps1")

function Get-RepoVerifyParameters {
    param (
        [string]$paramFile = $null
    )
    if (-not $paramFile) {
        $paramFile = Join-Path $PSScriptRoot "repo-verify.param"
    }
    if (-not (Test-Path $paramFile)) {
        throw "Parameter file not found: $paramFile"
    }
    try {
        $paramContent = Get-Content $paramFile -Raw | ConvertFrom-Json
    } catch {
        throw "Failed to parse parameter file '$paramFile': $($_.Exception.Message)"
    }
    $requiredParams = @('sourceRepo', 'sourceOrganization', 'sourceProject', 'targetRepo', 'targetOrganization', 'targetProject')
    foreach ($param in $requiredParams) {
        if (-not $paramContent.$param -or [string]::IsNullOrWhiteSpace($paramContent.$param)) {
            throw "Required parameter '$param' is missing or empty in parameter file"
        }
    }
    $params = @{
        sourceRepo         = $paramContent.sourceRepo
        sourceOrganization = $paramContent.sourceOrganization
        sourceProject      = $paramContent.sourceProject
        targetRepo         = $paramContent.targetRepo
        targetOrganization = $paramContent.targetOrganization
        targetProject      = $paramContent.targetProject
        logFile            = if ($paramContent.logFile -and -not [string]::IsNullOrWhiteSpace($paramContent.logFile)) {
                                if ([System.IO.Path]::IsPathRooted($paramContent.logFile)) {
                                    $paramContent.logFile
                                } else {
                                    Join-Path $PSScriptRoot $paramContent.logFile
                                }
                            } else { $null }
        paramFile          = $paramFile
        pat                = "$env:PAT"
    }
    if (-not $params.pat) {
        throw "Personal Access Token (PAT) is not set. Please set the PAT in the environment variable 'PAT'."
    }
    return $params
}

$params = Get-RepoVerifyParameters -paramFile $paramFile
Start-RepoLog -logFile $params.logFile -paramFile $params.paramFile

Write-RepoLog "Verifying archive for repository: $($params.sourceRepo)" "Green" $params.logFile
Write-RepoLog "  Source: $($params.sourceOrganization)/$($params.sourceProject)" "White" $params.logFile
Write-RepoLog "  Target: $($params.targetOrganization)/$($params.targetProject)" "White" $params.logFile

function Get-GitUrl {
    param(
        [string]$organization,
        [string]$project,
        [string]$repoName,
        [string]$pat
    )
    $orgHost = if ($organization -match '^https?://') { $organization } else { "https://dev.azure.com/$organization" }
    $url = "$orgHost/$project/_git/$repoName"
    if ($pat) {
        # Insert PAT for HTTPS auth (username:pat@...)
        $url = $url -replace '^https://', "https://user:$pat@"
    }
    return $url
}

# Main verification logic switch
function Invoke-GitVerification {
    param(
        [string]$srcUrl,
        [string]$tgtUrl,
        [string]$srcDir,
        [string]$tgtDir,
        [string]$logFile
    )
    Write-RepoLog "Cloning source repo: $srcUrl" "White" $logFile
    git clone --mirror $srcUrl $srcDir 2>&1 | Tee-Object -FilePath $logFile -Append
    if ($LASTEXITCODE -ne 0) { throw "Failed to clone source repo." }

    Write-RepoLog "Cloning target repo: $tgtUrl" "White" $logFile
    git clone --mirror $tgtUrl $tgtDir 2>&1 | Tee-Object -FilePath $logFile -Append
    if ($LASTEXITCODE -ne 0) { throw "Failed to clone target repo." }

    Write-RepoLog "Comparing refs and tip hashes between source and target bare repos..." "White" $logFile
    $srcRefs = git --git-dir=$srcDir for-each-ref --format='%(refname):%(objectname)'
    $tgtRefs = git --git-dir=$tgtDir for-each-ref --format='%(refname):%(objectname)'

    $srcRefTable = @{}
    foreach ($line in $srcRefs) {
        if ($line -match '^(.*?):(.*?)$') { $srcRefTable[$matches[1]] = $matches[2] }
    }
    $tgtRefTable = @{}
    foreach ($line in $tgtRefs) {
        if ($line -match '^(.*?):(.*?)$') { $tgtRefTable[$matches[1]] = $matches[2] }
    }

    $allRefs = ($srcRefTable.Keys + $tgtRefTable.Keys) | Sort-Object -Unique
    $mismatch = $false
    foreach ($ref in $allRefs) {
        $srcHash = $srcRefTable[$ref]
        $tgtHash = $tgtRefTable[$ref]
        if ($srcHash -ne $tgtHash) {
            Write-RepoLog "Ref '$ref' hash mismatch: Source=$srcHash, Target=$tgtHash" "Red" $logFile
            $mismatch = $true
        }
    }

    if ($mismatch) {
        Write-RepoLog "Verification failed: Ref hash mismatches detected." "Red" $logFile
        throw "Verification failed: Ref hash mismatches detected."
    } else {
        Write-RepoLog "Verification successful: All refs and tip hashes match between source and target repositories." "Green" $logFile
    }
}

# REST API verification: compare branch/tag names and tip hashes
function Invoke-RestApiVerification {
    param(
        $params
    )
    function Get-RepoInfo {
        param(
            [string]$organization,
            [string]$project,
            [string]$repositoryName,
            [string]$personalAccessToken
        )
        $base64AuthInfo = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes(":$personalAccessToken"))
        $reposUrl = "$organization/$project/_apis/git/repositories?api-version=7.0"
        $repoInfo = $null
        try {
            $reposResponse = Invoke-RestMethod -Uri $reposUrl -Headers @{ Authorization = "Basic $base64AuthInfo" } -ErrorAction Stop
            $repoInfo = ($reposResponse.value | Where-Object { $_.name -eq $repositoryName }) | Select-Object -First 1
            Write-RepoLog ("DEBUG: RepoInfo for ${repositoryName}: " + ($repoInfo | ConvertTo-Json -Depth 5)) "Gray" $params.logFile
        } catch {
            Write-RepoLog "Failed to fetch repository info: $($_.Exception.Message)" "Red" $params.logFile
        }
        return $repoInfo
    }
    function Get-RepoRefs {
        param(
            [string]$organization,
            [string]$project,
            [string]$repositoryId,
            [string]$personalAccessToken
        )
        $base64AuthInfo = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes(":$personalAccessToken"))
        $refsUrl = "$organization/$project/_apis/git/repositories/$repositoryId/refs?filter=&api-version=7.0"
        try {
            $refsResponse = Invoke-RestMethod -Uri $refsUrl -Headers @{ Authorization = "Basic $base64AuthInfo" } -ErrorAction Stop
            return $refsResponse.value
        } catch {
            Write-RepoLog "Failed to fetch refs: $($_.Exception.Message)" "Red" $params.logFile
            return $null
        }
    }

    $srcRepo = Get-RepoInfo -organization $params.sourceOrganization -project $params.sourceProject -repositoryName $params.sourceRepo -personalAccessToken $params.pat
    $tgtRepo = Get-RepoInfo -organization $params.targetOrganization -project $params.targetProject -repositoryName $params.targetRepo -personalAccessToken $params.pat

    if (-not $srcRepo) { throw "Source repository not found." }
    if (-not $tgtRepo) { throw "Target (archived) repository not found." }

    Write-RepoLog "Both source and target repositories exist." "Green" $params.logFile
    Write-RepoLog "DEBUG: srcRepo.id = $($srcRepo.id), tgtRepo.id = $($tgtRepo.id)" "Gray" $params.logFile
    $srcRefs = Get-RepoRefs -organization $params.sourceOrganization -project $params.sourceProject -repositoryId $srcRepo.id -personalAccessToken $params.pat
    $tgtRefs = Get-RepoRefs -organization $params.targetOrganization -project $params.targetProject -repositoryId $tgtRepo.id -personalAccessToken $params.pat

    if (-not $srcRefs -or -not $tgtRefs) {
        Write-RepoLog "DEBUG: srcRefs = $($srcRefs | ConvertTo-Json -Depth 5)" "Gray" $params.logFile
        Write-RepoLog "DEBUG: tgtRefs = $($tgtRefs | ConvertTo-Json -Depth 5)" "Gray" $params.logFile
        throw "Failed to fetch refs for one or both repositories."
    }

    $srcBranches = $srcRefs | Where-Object { $_.name -like 'refs/heads/*' }
    $tgtBranches = $tgtRefs | Where-Object { $_.name -like 'refs/heads/*' }
    $srcTags = $srcRefs | Where-Object { $_.name -like 'refs/tags/*' }
    $tgtTags = $tgtRefs | Where-Object { $_.name -like 'refs/tags/*' }

    Write-RepoLog "Branch count: Source=$($srcBranches.Count), Target=$($tgtBranches.Count)" "White" $params.logFile
    Write-RepoLog "Tag count:    Source=$($srcTags.Count), Target=$($tgtTags.Count)" "White" $params.logFile

    if ($srcBranches.Count -ne $tgtBranches.Count -or $srcTags.Count -ne $tgtTags.Count) {
        throw "Branch or tag count mismatch between source and target repositories."
    }

    $mismatch = $false
    foreach ($srcBranch in $srcBranches) {
        $branchName = $srcBranch.name.Replace('refs/heads/', '')
        $tgtBranch = $tgtBranches | Where-Object { $_.name -eq $srcBranch.name }
        if (-not $tgtBranch) {
            Write-RepoLog "Branch '$branchName' missing in target repo." "Red" $params.logFile
            $mismatch = $true
            continue
        }
        if ($srcBranch.objectId -ne $tgtBranch.objectId) {
            Write-RepoLog "Branch '$branchName' commit hash mismatch: Source=$($srcBranch.objectId), Target=$($tgtBranch.objectId)" "Red" $params.logFile
            $mismatch = $true
        }
    }
    foreach ($srcTag in $srcTags) {
        $tagName = $srcTag.name.Replace('refs/tags/', '')
        $tgtTag = $tgtTags | Where-Object { $_.name -eq $srcTag.name }
        if (-not $tgtTag) {
            Write-RepoLog "Tag '$tagName' missing in target repo." "Red" $params.logFile
            $mismatch = $true
            continue
        }
        if ($srcTag.objectId -ne $tgtTag.objectId) {
            Write-RepoLog "Tag '$tagName' commit hash mismatch: Source=$($srcTag.objectId), Target=$($tgtTag.objectId)" "Red" $params.logFile
            $mismatch = $true
        }
    }

    if ($mismatch) {
        throw "Verification failed: Branch or tag mismatches detected."
    }

    Write-RepoLog "Verification successful: All branches and tags match between source and target repositories." "Green" $params.logFile
}

function Remove-IfExists {
    param([string]$path)
    if (Test-Path $path) {
        Remove-Item -Recurse -Force $path
    }
}


try {
    $tmpRoot = "c:\git-compare"
    $srcDir = Join-Path $tmpRoot "src"
    $tgtDir = Join-Path $tmpRoot "tgt"
    Remove-IfExists $tmpRoot
    New-Item -ItemType Directory -Path $srcDir | Out-Null
    New-Item -ItemType Directory -Path $tgtDir | Out-Null

    $srcUrl = Get-GitUrl -organization $params.sourceOrganization -project $params.sourceProject -repoName $params.sourceRepo -pat $params.pat
    $tgtUrl = Get-GitUrl -organization $params.targetOrganization -project $params.targetProject -repoName $params.targetRepo -pat $params.pat

    $verificationMethod = if ($params.verificationMethod) { $params.verificationMethod } else { "git" }
    Write-RepoLog "Verification method: $verificationMethod" "White" $params.logFile

    switch ($verificationMethod.ToLower()) {
        "git" {
            Invoke-GitVerification -srcUrl $srcUrl -tgtUrl $tgtUrl -srcDir $srcDir -tgtDir $tgtDir -logFile $params.logFile
        }
        "rest" {
            Invoke-RestApiVerification -params $params
        }
        default {
            throw "Unknown verification method: $verificationMethod. Supported: 'git', 'rest'."
        }
    }

} catch {
    $errorMessage = "An error occurred: $($_.Exception.Message)"
    Write-RepoLog $errorMessage "Red" $params.logFile
    if ($params.logFile) {
        Add-Content -Path $params.logFile -Value "[$((Get-Date -Format 'yyyy-MM-dd HH:mm:ss'))] ERROR: $($_.Exception.Message)" -Encoding UTF8
        Add-Content -Path $params.logFile -Value "[$((Get-Date -Format 'yyyy-MM-dd HH:mm:ss'))] Script execution failed." -Encoding UTF8
    }
    Write-Error $errorMessage
} finally {
    if ($params.logFile) {
        Add-Content -Path $params.logFile -Value "[$((Get-Date -Format 'yyyy-MM-dd HH:mm:ss'))] Script execution completed." -Encoding UTF8
        Add-Content -Path $params.logFile -Value ("=" * 60) -Encoding UTF8
    }
    if ($tmpRoot -and (Test-Path $tmpRoot)) {
        Remove-IfExists $tmpRoot
    }
}
