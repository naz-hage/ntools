#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Restores git stash content by removing empty placeholder files and applying stash.

.DESCRIPTION
    This script removes all the empty placeholder files that are blocking the git stash
    from being applied, then applies the stash to restore the actual content.
#>

Write-Host "🔄 Git Stash Recovery Script" -ForegroundColor Cyan
Write-Host "============================" -ForegroundColor Cyan

# List of empty files to remove (based on git status --porcelain)
$emptyFiles = @(
    ".github/workflows/update-versions.yml",
    ".pre-commit-config.yaml", 
    "NbuildTasks/UpdateVersionsInDocs.cs",
    "debug-resources.cs",
    "dev-setup/update-versions.ps1",
    "docs/nbuild-tasks-integration.md",
    "docs/pre-commit-setup.md", 
    "docs/version-automation-guide.md"
)

# Directories that might be empty
$emptyDirs = @(
    "dev-setup/hooks",
    ".github/workflows",
    ".github"
)

Write-Host "`n🗑️ Removing empty placeholder files..." -ForegroundColor Yellow

foreach ($file in $emptyFiles) {
    if (Test-Path $file) {
        $fileInfo = Get-Item $file
        if ($fileInfo.Length -eq 0) {
            Write-Host "   Removing empty file: $file" -ForegroundColor Red
            Remove-Item $file -Force
        } else {
            Write-Host "   Skipping non-empty file: $file ($($fileInfo.Length) bytes)" -ForegroundColor Green
        }
    } else {
        Write-Host "   File not found: $file" -ForegroundColor Gray
    }
}

Write-Host "`n🗂️ Removing empty directories..." -ForegroundColor Yellow

foreach ($dir in $emptyDirs) {
    if (Test-Path $dir) {
        $dirInfo = Get-ChildItem $dir -Force
        if ($dirInfo.Count -eq 0) {
            Write-Host "   Removing empty directory: $dir" -ForegroundColor Red
            Remove-Item $dir -Force -Recurse
        } else {
            Write-Host "   Skipping non-empty directory: $dir ($($dirInfo.Count) items)" -ForegroundColor Green
        }
    } else {
        Write-Host "   Directory not found: $dir" -ForegroundColor Gray
    }
}

Write-Host "`n📦 Applying git stash..." -ForegroundColor Yellow

try {
    # Check if there are any stashes
    $stashes = git stash list
    if (-not $stashes) {
        Write-Host "❌ No stashes found!" -ForegroundColor Red
        exit 1
    }
    
    Write-Host "   Found stashes:" -ForegroundColor Green
    $stashes | ForEach-Object { Write-Host "   $_" -ForegroundColor Cyan }
    
    # Apply the most recent stash
    Write-Host "`n   Applying stash@{0}..." -ForegroundColor Cyan
    git stash apply stash@{0}
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ Stash applied successfully!" -ForegroundColor Green
    } else {
        Write-Host "❌ Failed to apply stash" -ForegroundColor Red
        exit 1
    }
    
} catch {
    Write-Host "❌ Error applying stash: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

Write-Host "`n🔍 Checking git status after stash apply..." -ForegroundColor Yellow
git status --porcelain

Write-Host "`n📊 File sizes after restore:" -ForegroundColor Yellow
foreach ($file in $emptyFiles) {
    if (Test-Path $file) {
        $fileInfo = Get-Item $file
        Write-Host "   $file`: $($fileInfo.Length) bytes" -ForegroundColor Green
    }
}

Write-Host "`n🎉 Stash recovery completed!" -ForegroundColor Green
Write-Host "💡 You can now review the restored files and commit them if everything looks good." -ForegroundColor Cyan
