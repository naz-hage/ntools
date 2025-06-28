#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Updates NuGet packages in the ntools solution using dotnet-outdated-tool.

.DESCRIPTION
    This script installs the dotnet-outdated-tool (if not already installed) and 
    uses it to update all outdated NuGet packages in the solution to their latest stable versions.
    
    The script automatically:
    - Excludes pre-release packages (only stable versions)
    - Sets DOTNET_HOST_PATH environment variable if needed
    - Ignores failed NuGet sources to handle network issues gracefully

.PARAMETER solutionFile
    Path to the solution file to update. If not specified, searches for *.sln in current directory.

.PARAMETER dryRun
    If specified, only shows what packages would be updated without actually updating them.

.PARAMETER interactive
    If specified, prompts for confirmation before updating each package.

.PARAMETER strictMode
    If specified, does not ignore failed NuGet sources. Use this if you want to see all connection errors.

.EXAMPLE
    .\update-packages.ps1
    Updates all packages in the solution found in current directory.

.EXAMPLE
    .\update-packages.ps1 -solutionFile "C:\source\ntools\ntools.sln"
    Updates packages in the specified solution file.

.EXAMPLE
    .\update-packages.ps1 -dryRun
    Shows what packages would be updated without making changes.

.EXAMPLE
    .\update-packages.ps1 -interactive
    Prompts for confirmation before updating each package.

.EXAMPLE
    .\update-packages.ps1 -solutionFile "MyProject.sln" -dryRun -interactive
    Uses specific solution file with dry run and interactive mode.

.EXAMPLE
    .\update-packages.ps1 -strictMode
    Updates packages without ignoring NuGet source connection errors.
#>

param(
    [string]$solutionFile,
    [switch]$dryRun,
    [switch]$interactive,
    [switch]$strictMode
)

Write-Host "🔄 NuGet Package Updater for ntools" -ForegroundColor Cyan
Write-Host "=================================" -ForegroundColor Cyan

# Determine solution file to use
$currentDir = Get-Location

if ($solutionFile) {
    # Use provided solution file path
    if ([System.IO.Path]::IsPathRooted($solutionFile)) {
        $solutionPath = $solutionFile
    } else {
        $solutionPath = Join-Path $currentDir $solutionFile
    }
    
    if (-not (Test-Path $solutionPath)) {
        Write-Host "❌ Error: Specified solution file not found: $solutionPath" -ForegroundColor Red
        exit 1
    }
} else {
    # Search for .sln files in current directory
    $solutionFiles = Get-ChildItem -Path $currentDir -Filter "*.sln"
    
    if ($solutionFiles.Count -eq 0) {
        Write-Host "❌ Error: No solution file found in current directory: $currentDir" -ForegroundColor Red
        Write-Host "💡 Tip: Specify a solution file with -solutionFile parameter" -ForegroundColor Yellow
        exit 1
    } elseif ($solutionFiles.Count -eq 1) {
        $solutionPath = $solutionFiles[0].FullName
        Write-Host "🔍 Found solution file: $($solutionFiles[0].Name)" -ForegroundColor Green
    } else {
        Write-Host "❌ Error: Multiple solution files found in current directory:" -ForegroundColor Red
        $solutionFiles | ForEach-Object { Write-Host "   - $($_.Name)" -ForegroundColor Yellow }
        Write-Host "💡 Tip: Specify which solution file to use with -solutionFile parameter" -ForegroundColor Yellow
        exit 1
    }
}

Write-Host "📁 Working directory: $currentDir" -ForegroundColor Green
Write-Host "📄 Solution file: $solutionPath" -ForegroundColor Green

# Install dotnet-outdated-tool if not already installed
Write-Host "`n🔧 Checking for dotnet-outdated-tool..." -ForegroundColor Yellow

try {
    $toolCheck = dotnet tool list --global | Select-String "dotnet-outdated-tool"
    
    if ($toolCheck) {
        Write-Host "✅ dotnet-outdated-tool is already installed" -ForegroundColor Green
    } else {
        Write-Host "📦 Installing dotnet-outdated-tool..." -ForegroundColor Yellow
        dotnet tool install --global dotnet-outdated-tool
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "✅ dotnet-outdated-tool installed successfully" -ForegroundColor Green
        } else {
            Write-Host "❌ Failed to install dotnet-outdated-tool" -ForegroundColor Red
            exit 1
        }
    }
} catch {
    Write-Host "❌ Error checking/installing dotnet-outdated-tool: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Change to solution directory
$solutionDir = Split-Path $solutionPath -Parent
Set-Location $solutionDir
Write-Host "`n📂 Changed to solution directory: $(Get-Location)" -ForegroundColor Green

# Set DOTNET_HOST_PATH environment variable if not already set
if (-not $env:DOTNET_HOST_PATH) {
    try {
        $dotnetPath = (Get-Command dotnet).Source
        $env:DOTNET_HOST_PATH = $dotnetPath
        Write-Host "🔧 Set DOTNET_HOST_PATH to: $dotnetPath" -ForegroundColor Green
    } catch {
        Write-Host "⚠️ Warning: Could not set DOTNET_HOST_PATH automatically" -ForegroundColor Yellow
    }
}

# Run dotnet outdated command
Write-Host "`n🔍 Checking for outdated packages..." -ForegroundColor Yellow

# Determine command line flags
$commonFlags = "--pre-release Never"
if (-not $strictMode) {
    $commonFlags += " --ignore-failed-sources"
    Write-Host "💡 Using relaxed mode: NuGet source connection errors will be treated as warnings" -ForegroundColor Cyan
} else {
    Write-Host "⚠️ Using strict mode: All NuGet source connection errors will be shown" -ForegroundColor Yellow
}

try {
    if ($dryRun) {
        Write-Host "🔍 DRY RUN: Showing outdated packages (no changes will be made)" -ForegroundColor Magenta
        Invoke-Expression "dotnet outdated $commonFlags"
    } elseif ($interactive) {
        Write-Host "🤔 interactive MODE: You will be prompted for each update" -ForegroundColor Magenta
        Invoke-Expression "dotnet outdated --upgrade Prompt $commonFlags"
    } else {
        Write-Host "🚀 UPDATING: All packages will be updated to latest stable versions" -ForegroundColor Magenta
        Invoke-Expression "dotnet outdated --upgrade $commonFlags"
    }
    
    if ($LASTEXITCODE -eq 0) {
        if (-not $dryRun) {
            Write-Host "`n✅ Package update completed successfully!" -ForegroundColor Green
            Write-Host "💡 Consider running 'dotnet build' and 'dotnet test' to verify everything still works." -ForegroundColor Cyan
        } else {
            Write-Host "`n✅ Package analysis completed successfully!" -ForegroundColor Green
        }
    } else {
        Write-Host "`n⚠️ Package operation completed with warnings/errors" -ForegroundColor Yellow
        Write-Host "💡 Note: 'Errors occurred while analyzing dependencies' messages are often due to:" -ForegroundColor Cyan
        Write-Host "   - Temporary network connectivity issues with NuGet servers" -ForegroundColor Cyan
        Write-Host "   - Private/corporate NuGet feeds that require authentication" -ForegroundColor Cyan
        Write-Host "   - NuGet feeds that are temporarily unavailable" -ForegroundColor Cyan
        Write-Host "   These don't prevent package updates from working correctly." -ForegroundColor Cyan
    }
} catch {
    Write-Host "`n❌ Error running dotnet outdated: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
} finally {
    # Return to original directory
    Set-Location $currentDir
}

Write-Host "`n🎉 Script completed!" -ForegroundColor Green
