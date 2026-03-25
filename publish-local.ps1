


nb git_autotag --buildtype stage
nb publish

# Get git info and extract version tag and project name
$gitInfo = nb.exe git_info | Out-String

# Extract project name
$projectMatch = $gitInfo | Select-String -Pattern 'Project \[([^\]]+)\]'
if ($projectMatch -and $projectMatch.Matches.Groups.Count -gt 1) {
    $project = $projectMatch.Matches.Groups[1].Value
    Write-Host "Extracted project: $project" -ForegroundColor Green
} else {
    Write-Host "Failed to extract project from git_info" -ForegroundColor Red
    exit 1
}

# Extract version tag
$tagMatch = $gitInfo | Select-String -Pattern 'Tag \[([^\]]+)\]'

if ($tagMatch -and $tagMatch.Matches.Groups.Count -gt 1) {
    $version = $tagMatch.Matches.Groups[1].Value
    Write-Host "Extracted version: $version" -ForegroundColor Green
} else {
    Write-Host "Failed to extract version from git_info" -ForegroundColor Red
    exit 1
}
$deploymentFolder = "C:/Program Files/nbuild"
$artifactsFolder = "C:/Artifacts/$project/Release/$version"

Write-Host "Deploying from: $artifactsFolder" -ForegroundColor Cyan
Write-Host "Deploying to: $deploymentFolder" -ForegroundColor Cyan

# Extract the archive
# & "C:\Program Files\7-Zip\7z.exe" x "$artifactsFolder" -o"$deploymentFolder" -y
robocopy.exe $artifactsFolder $deploymentFolder /mir

# robocopy exit codes:
# 0 = No errors
# 1 = Files copied successfully (no errors)
# 2 = Extra files detected (no errors)
# 3 = Files copied + skipped (no errors)
# 4+ = Actual errors
if ($LASTEXITCODE -le 3) {
    Write-Host "Deployment completed successfully" -ForegroundColor Green
} else {
    Write-Host "Deployment failed with exit code $LASTEXITCODE" -ForegroundColor Red
    exit $LASTEXITCODE
}

# display project info
nb git_info