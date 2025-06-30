#!/usr/bin/env pwsh

<#
.SYNOPSIS
    A comprehensive Git repository management tool for file status analysis and stash recovery.

.DESCRIPTION
    This script provides multiple utilities for managing Git repositories:
    - Comprehensive file status analysis (tracked, untracked, modified, empty files)
    - Smart stash recovery with conflict resolution
    - File size analysis and health checks
    - Git history and branch analysis
    - Cleanup utilities for empty/orphaned files

.PARAMETER Action
    The action to perform. Valid values: Check, RestoreStash, Cleanup, Analyze
    - Check: Analyze all files and show comprehensive status
    - RestoreStash: Intelligently restore from git stash with conflict resolution
    - Cleanup: Remove empty files and directories
    - Analyze: Deep analysis of repository health and history

.PARAMETER StashIndex
    Specific stash index to restore (default: 0 for most recent)

.PARAMETER IncludeIgnored
    Include ignored files in analysis

.PARAMETER DryRun
    Show what would be done without making changes

.PARAMETER ExcludePatterns
    Array of glob patterns to exclude from analysis (e.g., "*.log", "node_modules/*")

.PARAMETER TargetDirectory
    Target directory to analyze (default: current directory)

.EXAMPLE
    .\git-repo-tools.ps1 -Action Check
    Performs comprehensive file status analysis

.EXAMPLE
    .\git-repo-tools.ps1 -Action RestoreStash -StashIndex 1 -DryRun
    Shows what would happen when restoring stash@{1}

.EXAMPLE
    .\git-repo-tools.ps1 -Action Cleanup -DryRun -Verbose
    Shows empty files that would be cleaned up

.EXAMPLE
    .\git-repo-tools.ps1 -Action Analyze -IncludeIgnored
    Deep repository analysis including ignored files

.EXAMPLE
    .\git-repo-tools.ps1 -Action Check -ExcludePatterns @("*.log", "node_modules/*", "bin/*")
    Check status excluding specific patterns
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateSet("Check", "RestoreStash", "Cleanup", "Analyze")]
    [string]$Action,
    
    [int]$StashIndex = 0,
    
    [switch]$IncludeIgnored,
    
    [switch]$DryRun,
    
    [string[]]$ExcludePatterns = @(),
    
    [string]$TargetDirectory = "."
)

# Set location to target directory
Set-Location $TargetDirectory

# Utility Functions
function Write-Header {
    param([string]$Title, [string]$Color = "Cyan")
    Write-Host "`n$Title" -ForegroundColor $Color
    Write-Host ("=" * $Title.Length) -ForegroundColor $Color
}

function Write-Section {
    param([string]$Title, [string]$Color = "Yellow")
    Write-Host "`n$Title" -ForegroundColor $Color
    Write-Host ("-" * $Title.Length) -ForegroundColor $Color
}

function Get-FileInfo {
    param(
        [string]$FilePath,
        [string[]]$ExcludePatterns = @()
    )
    
    # Check if file matches exclude patterns
    foreach ($pattern in $ExcludePatterns) {
        if ($FilePath -like $pattern) {
            return $null
        }
    }
    
    if (Test-Path $FilePath) {
        $fileInfo = Get-Item $FilePath
        $size = $fileInfo.Length
        $status = if ($size -eq 0) { "EMPTY" } else { "HAS CONTENT" }
        $statusColor = if ($size -eq 0) { "Red" } else { "Green" }
        
        return @{
            Path = $FilePath
            Size = $size
            Status = $status
            Color = $statusColor
            LastModified = $fileInfo.LastWriteTime
            Exists = $true
        }
    } else {
        return @{
            Path = $FilePath
            Size = 0
            Status = "NOT FOUND"
            Color = "Yellow"
            LastModified = $null
            Exists = $false
        }
    }
}

function Get-GitStatus {
    param([switch]$IncludeIgnored)
    
    $gitStatusArgs = @("status", "--porcelain")
    if ($IncludeIgnored) {
        $gitStatusArgs += "--ignored"
    }
    
    $gitStatus = & git @gitStatusArgs
    $result = @{
        Untracked = @()
        Modified = @()
        Staged = @()
        Ignored = @()
    }
    
    if ($gitStatus) {
        foreach ($line in $gitStatus) {
            $status = $line.Substring(0, 2)
            $file = $line.Substring(3)
            
            switch -Regex ($status) {
                "^\?\?" { $result.Untracked += $file }
                "^!!" { $result.Ignored += $file }
                "^[MARC]." { $result.Staged += $file }
                "^.[MD]" { $result.Modified += $file }
                default { $result.Modified += $file }
            }
        }
    }
    
    return $result
}

function Show-FileAnalysis {
    param(
        [hashtable]$GitStatus,
        [string[]]$ExcludePatterns,
        [switch]$IncludeIgnored
    )
    
    Write-Header "📋 Comprehensive Repository Analysis"
    
    # Analyze each category
    $categories = @(
        @{ Name = "🆕 Untracked Files"; Files = $GitStatus.Untracked; Color = "Magenta" },
        @{ Name = "📝 Modified Files"; Files = $GitStatus.Modified; Color = "Blue" },
        @{ Name = "📦 Staged Files"; Files = $GitStatus.Staged; Color = "Green" }
    )
    
    if ($IncludeIgnored) {
        $categories += @{ Name = "🚫 Ignored Files"; Files = $GitStatus.Ignored; Color = "Gray" }
    }
    
    $allStats = @{
        TotalFiles = 0
        EmptyFiles = 0
        ContentFiles = 0
        MissingFiles = 0
        TotalSize = 0
    }
    
    foreach ($category in $categories) {
        if ($category.Files.Count -gt 0) {
            Write-Section $category.Name $category.Color
            
            foreach ($file in $category.Files) {
                $info = Get-FileInfo -FilePath $file -ExcludePatterns $ExcludePatterns
                if ($null -eq $info) { continue }
                
                $sizeText = if ($info.Exists) { "$($info.Size) bytes" } else { "N/A" }
                $modifiedText = if ($info.LastModified) { $info.LastModified.ToString("yyyy-MM-dd HH:mm") } else { "N/A" }
                
                Write-Host "   $($info.Path.PadRight(45)) | $($sizeText.PadRight(12)) | $($modifiedText.PadRight(16)) | " -NoNewline
                Write-Host $info.Status -ForegroundColor $info.Color
                
                # Update statistics
                $allStats.TotalFiles++
                if (-not $info.Exists) {
                    $allStats.MissingFiles++
                } elseif ($info.Size -eq 0) {
                    $allStats.EmptyFiles++
                } else {
                    $allStats.ContentFiles++
                    $allStats.TotalSize += $info.Size
                }
            }
        }
    }
    
    # Show summary
    Write-Section "📊 Repository Summary"
    Write-Host "   Total files analyzed: " -NoNewline; Write-Host $allStats.TotalFiles -ForegroundColor White
    Write-Host "   Files with content: " -NoNewline; Write-Host $allStats.ContentFiles -ForegroundColor Green
    Write-Host "   Empty files: " -NoNewline; Write-Host $allStats.EmptyFiles -ForegroundColor Red
    Write-Host "   Missing files: " -NoNewline; Write-Host $allStats.MissingFiles -ForegroundColor Yellow
    Write-Host "   Total size: " -NoNewline; Write-Host "$([math]::Round($allStats.TotalSize / 1KB, 2)) KB" -ForegroundColor Cyan
}

function Show-GitInfo {
    Write-Section "🔍 Git Repository Information"
    
    try {
        $branch = git branch --show-current
        $remoteUrl = git config --get remote.origin.url
        $lastCommit = git log -1 --pretty=format:"%h - %s (%cr) <%an>"
        $stashCount = (git stash list | Measure-Object).Count
        
        Write-Host "   Current branch: " -NoNewline; Write-Host $branch -ForegroundColor Green
        Write-Host "   Remote URL: " -NoNewline; Write-Host $remoteUrl -ForegroundColor Cyan
        Write-Host "   Last commit: " -NoNewline; Write-Host $lastCommit -ForegroundColor Yellow
        Write-Host "   Stash count: " -NoNewline; Write-Host $stashCount -ForegroundColor Magenta
        
        # Check if we're ahead/behind remote
        try {
            $ahead = git rev-list --count '@{u}..HEAD' 2>$null
            $behind = git rev-list --count 'HEAD..@{u}' 2>$null
            
            if ($ahead -gt 0 -or $behind -gt 0) {
                Write-Host "   Sync status: " -NoNewline
                if ($ahead -gt 0) { Write-Host "$ahead ahead " -NoNewline -ForegroundColor Green }
                if ($behind -gt 0) { Write-Host "$behind behind" -NoNewline -ForegroundColor Red }
                Write-Host ""
            }
        } catch {
            # Remote tracking not set up or other error
        }
    } catch {
        Write-Host "   Could not retrieve Git information" -ForegroundColor Red
    }
}

function Invoke-StashRestore {
    param(
        [int]$StashIndex,
        [switch]$DryRun
    )
    
    Write-Header "📦 Smart Stash Recovery"
    
    # Check if stashes exist
    $stashes = git stash list
    if (-not $stashes) {
        Write-Host "❌ No stashes found!" -ForegroundColor Red
        return
    }
    
    if ($StashIndex -ge $stashes.Count) {
        Write-Host "❌ Stash index $StashIndex not found. Available: 0-$($stashes.Count - 1)" -ForegroundColor Red
        return
    }
    
    Write-Host "Available stashes:" -ForegroundColor Green
    for ($i = 0; $i -lt $stashes.Count; $i++) {
        $marker = if ($i -eq $StashIndex) { ">>> " } else { "    " }
        Write-Host "$marker$($stashes[$i])" -ForegroundColor $(if ($i -eq $StashIndex) { "Yellow" } else { "Gray" })
    }
    
    if ($DryRun) {
        Write-Host "`n🔍 DRY RUN: Would restore stash@{$StashIndex}" -ForegroundColor Magenta
        
        # Show what files would be affected
        $stashFiles = git stash show --name-only stash@{$StashIndex}
        Write-Host "`nFiles that would be restored:" -ForegroundColor Cyan
        foreach ($file in $stashFiles) {
            $info = Get-FileInfo -FilePath $file
            $status = if ($info.Exists) { "UPDATE" } else { "CREATE" }
            Write-Host "   $status`: $file" -ForegroundColor $(if ($status -eq "CREATE") { "Green" } else { "Yellow" })
        }
        return
    }
    
    # Check for conflicts
    $gitStatus = Get-GitStatus
    if ($gitStatus.Modified.Count -gt 0 -or $gitStatus.Staged.Count -gt 0) {
        Write-Host "⚠️ Warning: You have uncommitted changes that might conflict:" -ForegroundColor Yellow
        $gitStatus.Modified + $gitStatus.Staged | ForEach-Object { Write-Host "   $_" -ForegroundColor Red }
        
        $response = Read-Host "Continue anyway? (y/N)"
        if ($response -ne "y" -and $response -ne "Y") {
            Write-Host "Operation cancelled." -ForegroundColor Yellow
            return
        }
    }
    
    # Apply stash
    Write-Host "`nApplying stash@{$StashIndex}..." -ForegroundColor Cyan
    git stash apply stash@{$StashIndex}
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ Stash applied successfully!" -ForegroundColor Green
    } else {
        Write-Host "❌ Stash application failed with conflicts" -ForegroundColor Red
        Write-Host "💡 Use 'git status' to see conflicts and resolve them manually" -ForegroundColor Cyan
    }
}

function Invoke-Cleanup {
    param([switch]$DryRun)
    
    Write-Header "🧹 Repository Cleanup"
    
    $gitStatus = Get-GitStatus
    $emptyFiles = @()
    $emptyDirs = @()
    
    # Find empty files
    foreach ($file in ($gitStatus.Untracked + $gitStatus.Modified)) {
        $info = Get-FileInfo -FilePath $file
        if ($info.Exists -and $info.Size -eq 0) {
            $emptyFiles += $file
        }
    }
    
    # Find empty directories
    Get-ChildItem -Directory -Recurse | ForEach-Object {
        $dirContents = Get-ChildItem $_.FullName -Force
        if ($dirContents.Count -eq 0) {
            $emptyDirs += $_.FullName
        }
    }
    
    if ($emptyFiles.Count -eq 0 -and $emptyDirs.Count -eq 0) {
        Write-Host "✅ No empty files or directories found!" -ForegroundColor Green
        return
    }
    
    if ($DryRun) {
        Write-Host "🔍 DRY RUN: Would clean up the following:" -ForegroundColor Magenta
    }
    
    if ($emptyFiles.Count -gt 0) {
        Write-Section "Empty Files"
        foreach ($file in $emptyFiles) {
            if ($DryRun) {
                Write-Host "   Would remove: $file" -ForegroundColor Red
            } else {
                Write-Host "   Removing: $file" -ForegroundColor Red
                Remove-Item $file -Force
            }
        }
    }
    
    if ($emptyDirs.Count -gt 0) {
        Write-Section "Empty Directories"
        foreach ($dir in $emptyDirs) {
            if ($DryRun) {
                Write-Host "   Would remove: $dir" -ForegroundColor Red
            } else {
                Write-Host "   Removing: $dir" -ForegroundColor Red
                Remove-Item $dir -Force -Recurse
            }
        }
    }
    
    if (-not $DryRun) {
        Write-Host "`n✅ Cleanup completed!" -ForegroundColor Green
    }
}

function Invoke-DeepAnalysis {
    param([switch]$IncludeIgnored)
    
    Write-Header "🔬 Deep Repository Analysis"
    
    # Basic info
    Show-GitInfo
    
    # File analysis
    $gitStatus = Get-GitStatus -IncludeIgnored:$IncludeIgnored
    Show-FileAnalysis -GitStatus $gitStatus -ExcludePatterns $ExcludePatterns -IncludeIgnored:$IncludeIgnored
    
    # Additional insights
    Write-Section "📈 Repository Insights"
    
    try {
        $totalCommits = git rev-list --count HEAD
        $contributors = git shortlog -sn | Measure-Object
        $largestFiles = Get-ChildItem -Recurse -File | Sort-Object Length -Descending | Select-Object -First 5
        
        Write-Host "   Total commits: " -NoNewline; Write-Host $totalCommits -ForegroundColor Cyan
        Write-Host "   Contributors: " -NoNewline; Write-Host $contributors.Count -ForegroundColor Cyan
        
        Write-Host "`n   Largest files:" -ForegroundColor Yellow
        foreach ($file in $largestFiles) {
            $sizeKB = [math]::Round($file.Length / 1KB, 2)
            Write-Host "     $($file.Name) - $sizeKB KB" -ForegroundColor Gray
        }
        
    } catch {
        Write-Host "   Could not retrieve additional insights" -ForegroundColor Red
    }
}

# Main execution
try {
    # Verify we're in a Git repository
    if (-not (Test-Path ".git")) {
        Write-Host "❌ Not a Git repository!" -ForegroundColor Red
        exit 1
    }
    
    switch ($Action) {
        "Check" {
            $gitStatus = Get-GitStatus -IncludeIgnored:$IncludeIgnored
            Show-FileAnalysis -GitStatus $gitStatus -ExcludePatterns $ExcludePatterns -IncludeIgnored:$IncludeIgnored
            Show-GitInfo
        }
        
        "RestoreStash" {
            Invoke-StashRestore -StashIndex $StashIndex -DryRun:$DryRun
        }
        
        "Cleanup" {
            Invoke-Cleanup -DryRun:$DryRun
        }
        
        "Analyze" {
            Invoke-DeepAnalysis -IncludeIgnored:$IncludeIgnored
        }
    }
    
    Write-Host "`n🎉 Operation completed!" -ForegroundColor Green
    
} catch {
    Write-Host "`n❌ Error: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}
