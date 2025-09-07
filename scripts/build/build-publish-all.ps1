# PowerShell script to publish all .NET projects in the repo to a single output directory
param(
    [Parameter(Mandatory=$true)]
    [ValidateNotNullOrEmpty()]
    [string]$PublishDir,
    [Parameter(Mandatory=$true)]
    [ValidateNotNullOrEmpty()]
    [string]$ProductVersion
)

# Import required modules
$scriptDir = Split-Path -Parent $PSCommandPath
Import-Module "$scriptDir\..\modules\Common.psm1" -Force
Import-Module "$scriptDir\..\modules\Build.psm1" -Force

Write-Info "Starting project publishing process..."
Write-Info "Output Directory: $PublishDir"
Write-Info "Product Version: $ProductVersion"

# Ensure output directory exists
if (-not (Test-Path $PublishDir)) {
    New-Item -ItemType Directory -Force -Path $PublishDir | Out-Null
    Write-Info "Created output directory: $PublishDir"
}

# Get all non-test projects
$repositoryRoot = Split-Path -Parent (Split-Path -Parent $scriptDir)
$projects = Get-ProjectFiles -SearchPath $repositoryRoot -ExcludeTests

Write-Info "Found $($projects.Count) projects to publish"

$successCount = 0
$failureCount = 0

foreach ($project in $projects) {
    $result = Invoke-ProjectPublish -ProjectPath $project.FullName -OutputPath $PublishDir -ProductVersion $ProductVersion
    if ($result) {
        $successCount++
    } else {
        $failureCount++
    }
}

Write-Info "Publishing completed"
Write-Success "$successCount projects published successfully"
if ($failureCount -gt 0) {
    Write-Error "$failureCount projects failed to publish"
    exit 1
} else {
    Write-Success "All projects published successfully to $PublishDir"
}
