<#
.SYNOPSIS
    Full workflow: archive, check, verify, and delete Azure DevOps repos per a param file.
.DESCRIPTION
    Reads a param file describing a sequence of steps (archive, check, verify, delete) and executes them in order.
    Each step can specify its own parameters or reference shared/global parameters.
    Logs all actions and results for auditability and troubleshooting.
.PARAMETER paramFile
    Path to the JSON parameter file describing the workflow steps and parameters.
.EXAMPLE
    .\repo-archive-check-verify-delete.ps1 -paramFile .\workflow.param
.NOTES
    - Requires supporting scripts: repo-archive.ps1, repo-check.ps1, repo-verify.ps1, repo-delete.ps1 in the same directory.
    - Each step in the param file must have a 'type' (archive, check, verify, delete) and required parameters.
#>

param(
    [string]$paramFile = $null
)

function Write-WorkflowLog {
    param(
        [string]$message
    )
    if ($logFile) {
        $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
        Add-Content -Path $logFile -Value "[$timestamp] [WORKFLOW] $message"
    }
}


# Helper: create a temp param file for each step (renamed to avoid session conflicts)
function New-WorkflowTempFile {
    param(
        [Parameter(Mandatory=$true)][string]$content
    )
    $tmp = [System.IO.Path]::GetTempFileName()
    Set-Content -Path $tmp -Value $content -Encoding UTF8
    return $tmp
}

# Robustly merge global and step params, overlaying step values (non-null) over global
function Merge-Params {
    param($global, $step)
    # Convert to hashtable if needed
    function To-Hashtable($obj) {
        if ($obj -is [hashtable]) { return $obj }
        $ht = @{}
        if ($obj -is [System.Management.Automation.PSCustomObject]) {
            foreach ($p in $obj.PSObject.Properties) { $ht[$p.Name] = $p.Value }
        }
        return $ht
    }
    $g = To-Hashtable $global
    $s = To-Hashtable $step
    $merged = @{}
    foreach ($k in $g.Keys) { $merged[$k] = $g[$k] }
    foreach ($k in $s.Keys) {
        if ($k -eq 'type') { continue } # never merge type
        if ($null -ne $s[$k]) {
            $merged[$k] = $s[$k]
        }
    }
    # Add PAT from environment if not already present
    if (-not $merged.ContainsKey('pat')) {
        $merged['pat'] = $env:PAT
    }
    return $merged
}

l
# Helper to merge hashtables (flatten array of single-key hashtables)
function Merge-HashtableArray {
    param($array)
    $merged = @{}
    foreach ($h in $array) {
        foreach ($k in $h.Keys) { $merged[$k] = $h[$k] }
    }
    return $merged
}

# Main workflow logic starts here




$scriptRoot = $PSScriptRoot

if (-not $paramFile) {
    $paramFile = Join-Path $scriptRoot "workflow.param"
}
if (-not (Test-Path $paramFile)) {
    throw "Parameter file not found: $paramFile"
}

$workflowContent = Get-Content $paramFile -Raw
# Expand environment variables like ${env:PAT}
$workflowContent = [regex]::Replace($workflowContent, '\$\{env:(\w+)\}', { param($match) [Environment]::GetEnvironmentVariable($match.Groups[1].Value) })
$workflow = $workflowContent | ConvertFrom-Json
if (-not $workflow.steps -or $workflow.steps.Count -eq 0) {
    throw "No steps defined in workflow param file."
}

$globalParams = $workflow.globalParams


foreach ($step in $workflow.steps) {
    # For 'delete' step, map targetRepo to repositoryName if present
    if ($type -eq 'delete' -and $cleanParams.ContainsKey('targetRepo')) {
        $cleanParams['repositoryName'] = $cleanParams['targetRepo']
    }
    Write-WorkflowLog "Starting step: $type"
    $type = $step.type.ToLower()
    $params = Merge-Params $globalParams $step
    # Remove nulls and 'type' from params
    $cleanParams = @{}
    foreach ($key in $params.Keys) {
        if ($key -ne 'type' -and $null -ne $params[$key]) {
            $cleanParams[$key] = $params[$key]
        }
    }
    # For 'check' step, map sourceRepo to repositoryName and sourceOrganization to organization if present
    if ($type -eq 'check') {
        if ($cleanParams.ContainsKey('sourceRepo')) {
            $cleanParams['repositoryName'] = $cleanParams['sourceRepo']
        }
        if ($cleanParams.ContainsKey('sourceOrganization')) {
            $cleanParams['organization'] = $cleanParams['sourceOrganization']
        }
    }
    # Mask PAT in debug output
    $debugParams = $cleanParams.Clone()
    if ($debugParams.ContainsKey('pat')) { $debugParams['pat'] = '[REDACTED]' }
    Write-Host "DEBUG: Clean params for $type`: $($debugParams | ConvertTo-Json -Compress)" -ForegroundColor Gray
    if ($cleanParams.Count -eq 0) {
        Write-Error "No valid parameters found for step '$type'. Skipping step."
        Write-WorkflowLog "Skipped step: $type (no valid parameters)"
        continue
    }
    Write-Host "=== Executing step: $type ===" -ForegroundColor Cyan
    Write-WorkflowLog "Executing step: $type with params: $($debugParams | ConvertTo-Json -Compress)"
    $tmpParamFile = New-WorkflowTempFile -content (ConvertTo-Json $cleanParams -Depth 10)
    switch ($type) {
        'archive' {
            & (Join-Path $scriptRoot 'repo-archive.ps1') -paramFile $tmpParamFile
        }
        'check' {
            & (Join-Path $scriptRoot 'repo-check.ps1') -paramFile $tmpParamFile
        }
        'verify' {
            & (Join-Path $scriptRoot 'repo-verify.ps1') -paramFile $tmpParamFile
        }
        'delete' {
            & (Join-Path $scriptRoot 'repo-delete.ps1') -paramFile $tmpParamFile
        }
        default {
            throw "Unknown step type: $type"
        }
    }
    Write-WorkflowLog "Completed step: $type"
}



Write-Host "Workflow completed." -ForegroundColor Green
Write-WorkflowLog "Workflow completed."
