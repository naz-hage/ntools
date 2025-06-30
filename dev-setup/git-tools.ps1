#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Quick Git repository management shortcuts.

.DESCRIPTION
    Provides easy-to-remember commands for common Git repository management tasks.
    This is a simplified interface to the full git-repo-tools.ps1 script.

.PARAMETER Command
    The command to run:
    - status: Show comprehensive repository status
    - stash: Restore from most recent stash
    - clean: Clean up empty files (dry run)
    - analyze: Deep repository analysis
    - help: Show this help

.EXAMPLE
    .\git-tools.ps1 status
    Shows comprehensive repository status

.EXAMPLE
    .\git-tools.ps1 stash
    Restores from the most recent stash

.EXAMPLE
    .\git-tools.ps1 clean
    Shows what empty files would be cleaned up
#>

param(
    [Parameter(Position = 0)]
    [ValidateSet("status", "stash", "clean", "analyze", "help")]
    [string]$Command = "help"
)

$ToolsScript = Join-Path $PSScriptRoot "git-repo-tools.ps1"

if (-not (Test-Path $ToolsScript)) {
    Write-Host "❌ git-repo-tools.ps1 not found in the same directory!" -ForegroundColor Red
    exit 1
}

switch ($Command) {
    "status" {
        Write-Host "🔍 Analyzing repository status..." -ForegroundColor Cyan
        & $ToolsScript -Action Check
    }
    
    "stash" {
        Write-Host "📦 Restoring from stash..." -ForegroundColor Cyan
        & $ToolsScript -Action RestoreStash
    }
    
    "clean" {
        Write-Host "🧹 Checking for cleanup opportunities..." -ForegroundColor Cyan
        & $ToolsScript -Action Cleanup -DryRun
    }
    
    "analyze" {
        Write-Host "🔬 Performing deep analysis..." -ForegroundColor Cyan
        & $ToolsScript -Action Analyze
    }
    
    "help" {
        Write-Host @"
🛠️ Git Repository Tools - Quick Commands

Usage: .\git-tools.ps1 <command>

Commands:
  status   - Show comprehensive repository status (files, sizes, git info)
  stash    - Restore from the most recent git stash
  clean    - Show what empty files/directories would be cleaned up
  analyze  - Perform deep repository analysis with insights
  help     - Show this help message

Examples:
  .\git-tools.ps1 status
  .\git-tools.ps1 stash
  .\git-tools.ps1 clean

For advanced options, use git-repo-tools.ps1 directly:
  .\git-repo-tools.ps1 -Action Check -ExcludePatterns @("*.log")
  .\git-repo-tools.ps1 -Action RestoreStash -StashIndex 1 -DryRun
  .\git-repo-tools.ps1 -Action Cleanup -DryRun:$false

"@ -ForegroundColor Yellow
    }
}
