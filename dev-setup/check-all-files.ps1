#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Checks the status and size of all Git-tracked and untracked files in the repository.

.DESCRIPTION
    This script provides a comprehensive overview of all files in the repository,
    showing their Git status, file size, and whether they have content or are empty.
#>

Write-Host "📋 Comprehensive File Status Check" -ForegroundColor Cyan
Write-Host "=================================" -ForegroundColor Cyan

# Get all files from git status
Write-Host "`n🔍 Checking Git Status..." -ForegroundColor Yellow
$gitStatus = git status --porcelain
$untrackedFiles = @()
$modifiedFiles = @()

if ($gitStatus) {
    foreach ($line in $gitStatus) {
        $status = $line.Substring(0, 2)
        $file = $line.Substring(3)
        
        if ($status.StartsWith("??")) {
            $untrackedFiles += $file
        } else {
            $modifiedFiles += $file
        }
    }
}

Write-Host "`n📊 File Analysis Results:" -ForegroundColor Green
Write-Host "========================" -ForegroundColor Green

# Function to get file info
function Get-FileInfo {
    param($filePath)
    
    if (Test-Path $filePath) {
        $fileInfo = Get-Item $filePath
        $size = $fileInfo.Length
        $status = if ($size -eq 0) { "EMPTY" } else { "HAS CONTENT" }
        $statusColor = if ($size -eq 0) { "Red" } else { "Green" }
        
        return @{
            Path = $filePath
            Size = $size
            Status = $status
            Color = $statusColor
            Exists = $true
        }
    } else {
        return @{
            Path = $filePath
            Size = 0
            Status = "NOT FOUND"
            Color = "Yellow"
            Exists = $false
        }
    }
}

# Check untracked files
if ($untrackedFiles.Count -gt 0) {
    Write-Host "`n🆕 Untracked Files:" -ForegroundColor Magenta
    Write-Host "==================" -ForegroundColor Magenta
    
    foreach ($file in $untrackedFiles) {
        $info = Get-FileInfo $file
        $sizeText = if ($info.Exists) { "$($info.Size) bytes" } else { "N/A" }
        Write-Host "   $($info.Path.PadRight(50)) | $($sizeText.PadRight(12)) | " -NoNewline
        Write-Host $info.Status -ForegroundColor $info.Color
    }
}

# Check modified files
if ($modifiedFiles.Count -gt 0) {
    Write-Host "`n📝 Modified Files:" -ForegroundColor Blue
    Write-Host "=================" -ForegroundColor Blue
    
    foreach ($file in $modifiedFiles) {
        $info = Get-FileInfo $file
        $sizeText = if ($info.Exists) { "$($info.Size) bytes" } else { "N/A" }
        Write-Host "   $($info.Path.PadRight(50)) | $($sizeText.PadRight(12)) | " -NoNewline
        Write-Host $info.Status -ForegroundColor $info.Color
    }
}

# Check specific files we've been working with
$specificFiles = @(
    "dev-setup/update-packages.ps1",
    "dev-setup/update-versions.ps1",
    ".pre-commit-config.yaml",
    "NbuildTasks/UpdateVersionsInDocs.cs",
    ".github/workflows/update-versions.yml",
    "debug-resources.cs",
    "docs/pre-commit-setup.md",
    "docs/version-automation-guide.md",
    "docs/nbuild-tasks-integration.md"
)

Write-Host "`n🎯 Key Files Status:" -ForegroundColor Cyan
Write-Host "===================" -ForegroundColor Cyan

foreach ($file in $specificFiles) {
    $info = Get-FileInfo $file
    $sizeText = if ($info.Exists) { "$($info.Size) bytes" } else { "N/A" }
    Write-Host "   $($info.Path.PadRight(50)) | $($sizeText.PadRight(12)) | " -NoNewline
    Write-Host $info.Status -ForegroundColor $info.Color
}

# Summary
Write-Host "`n📈 Summary:" -ForegroundColor White
Write-Host "===========" -ForegroundColor White

$allFiles = $untrackedFiles + $modifiedFiles + $specificFiles | Sort-Object | Get-Unique
$emptyCount = 0
$contentCount = 0
$notFoundCount = 0

foreach ($file in $allFiles) {
    $info = Get-FileInfo $file
    if (-not $info.Exists) {
        $notFoundCount++
    } elseif ($info.Size -eq 0) {
        $emptyCount++
    } else {
        $contentCount++
    }
}

Write-Host "   Files with content: " -NoNewline
Write-Host $contentCount -ForegroundColor Green
Write-Host "   Empty files: " -NoNewline  
Write-Host $emptyCount -ForegroundColor Red
Write-Host "   Files not found: " -NoNewline
Write-Host $notFoundCount -ForegroundColor Yellow

# Check if we still have stashes
Write-Host "`n💾 Git Stash Status:" -ForegroundColor Magenta
Write-Host "===================" -ForegroundColor Magenta

$stashList = git stash list
if ($stashList) {
    Write-Host "   Available stashes:" -ForegroundColor Green
    foreach ($stash in $stashList) {
        Write-Host "   $stash" -ForegroundColor Yellow
    }
} else {
    Write-Host "   No stashes found" -ForegroundColor Yellow
}

Write-Host "`n🎉 Check completed!" -ForegroundColor Green
