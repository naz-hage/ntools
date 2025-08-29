# Clear PowerShell History Script
# This script clears both session history and PSReadLine history

param(
    [switch]$Force,
    [switch]$Help,
    [string]$MatchString
)

function Show-Help {
    Write-Host "Clear PowerShell History Script" -ForegroundColor Green
    Write-Host ""
    Write-Host "Usage:" -ForegroundColor Yellow
    Write-Host "  .\Clear-PwshHistory.ps1                 # Interactive clear with confirmation"
    Write-Host "  .\Clear-PwshHistory.ps1 -Force          # Clear all history without confirmation"
    Write-Host "  .\Clear-PwshHistory.ps1 -MatchString 'foo' # Remove only lines containing 'foo' from history"
    Write-Host "  .\Clear-PwshHistory.ps1 -Help           # Show this help"
    Write-Host ""
    Write-Host "This script can:" -ForegroundColor Yellow
    Write-Host "  - Clear all current PowerShell session history (in-memory)"
    Write-Host "  - Delete or filter the PSReadLine history file (persistent history)"
    Write-Host "  - Clear or filter PSReadLine in-memory history (for current session)"
    Write-Host ""
    Write-Host "Notes:" -ForegroundColor Yellow
    Write-Host "  - When using -MatchString, only lines containing the specified string are removed."
    Write-Host "  - After clearing, history is not accessible to PowerShell or normal file browsing."
    Write-Host "  - Deleted or overwritten files may be recoverable with forensic tools until disk space is overwritten."
    Write-Host "  - For most users, history is effectively gone after running this script."
}

function Clear-AllHistory {
    $historyPath = (Get-PSReadLineOption).HistorySavePath
    if ($MatchString) {
        Write-Host "Removing lines containing '$MatchString' from history..." -ForegroundColor Yellow
        # Remove from session history
        $sessionHistory = Get-History | Where-Object { $_.CommandLine -notmatch [regex]::Escape($MatchString) }
        Clear-History
        foreach ($item in $sessionHistory) {
            Add-History -InputObject $item
        }
        Write-Host "✓ Session history lines containing '$MatchString' removed" -ForegroundColor Green
        # Remove from PSReadLine file
        if (Test-Path $historyPath) {
            $allLines = Get-Content $historyPath
            $filtered = $allLines | Where-Object { $_ -notmatch [regex]::Escape($MatchString) }
            $filtered | Set-Content $historyPath
            Write-Host "✓ PSReadLine history lines containing '$MatchString' removed from file: $historyPath" -ForegroundColor Green
        } else {
            Write-Host "! PSReadLine history file not found: $historyPath" -ForegroundColor Yellow
        }
        # Remove from PSReadLine in-memory history (best effort)
        try {
            [Microsoft.PowerShell.PSConsoleReadLine]::ClearHistory()
            foreach ($line in $filtered) {
                [Microsoft.PowerShell.PSConsoleReadLine]::AddToHistory($line)
            }
            Write-Host "✓ PSReadLine in-memory history updated" -ForegroundColor Green
        } catch {
            Write-Host "! Could not update PSReadLine in-memory history: $($_.Exception.Message)" -ForegroundColor Yellow
        }
        Write-Host ""
        Write-Host "PowerShell history lines containing '$MatchString' have been removed!" -ForegroundColor Green
        Write-Host "Note: You may need to restart PowerShell for all changes to take effect." -ForegroundColor Cyan
        return
    }
    Write-Host "Clearing PowerShell History..." -ForegroundColor Yellow
    Write-Host ""
    # Clear current session history
    Write-Host "Clearing current session history..." -ForegroundColor Cyan
    Clear-History
    Write-Host "✓ Current session history cleared" -ForegroundColor Green
    # Clear PSReadLine history file
    Write-Host "Clearing PSReadLine history file..." -ForegroundColor Cyan
    if (Test-Path $historyPath) {
        try {
            Remove-Item $historyPath -Force
            Write-Host "✓ PSReadLine history file deleted: $historyPath" -ForegroundColor Green
        } catch {
            Write-Host "✗ Failed to delete PSReadLine history file: $($_.Exception.Message)" -ForegroundColor Red
        }
    } else {
        Write-Host "! PSReadLine history file not found: $historyPath" -ForegroundColor Yellow
    }
    # Clear PSReadLine in-memory history
    Write-Host "Clearing PSReadLine in-memory history..." -ForegroundColor Cyan
    try {
        [Microsoft.PowerShell.PSConsoleReadLine]::ClearHistory()
        Write-Host "✓ PSReadLine in-memory history cleared" -ForegroundColor Green
    } catch {
        Write-Host "! Could not clear PSReadLine in-memory history: $($_.Exception.Message)" -ForegroundColor Yellow
    }
    Write-Host ""
    Write-Host "PowerShell history has been cleared successfully!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Recommendations:" -ForegroundColor Cyan
    Write-Host "- Restart PowerShell for complete effect"
    Write-Host "- Press Ctrl+L to clear the screen"
    Write-Host "- Use 'Get-History' to verify session history is empty"
}

# Main script logic
if ($Help) {
    Show-Help
    exit
}

if ($Force -or $MatchString) {
    Clear-AllHistory
} else {
    Write-Host "PowerShell History Clear Utility" -ForegroundColor Green
    Write-Host "================================" -ForegroundColor Green
    Write-Host ""
    # Show current history status
    $sessionCount = (Get-History).Count
    $historyPath = (Get-PSReadLineOption).HistorySavePath
    $fileExists = Test-Path $historyPath
    $fileLineCount = if ($fileExists) { (Get-Content $historyPath | Measure-Object -Line).Lines } else { 0 }
    Write-Host "Current status:" -ForegroundColor Yellow
    Write-Host "  Session history: $sessionCount commands"
    $fileStatus = if ($fileExists) { "$fileLineCount commands" } else { "Not found" }
    Write-Host "  PSReadLine file: $fileStatus"
    Write-Host "  File location: $historyPath"
    Write-Host ""
    $confirmation = Read-Host "Are you sure you want to clear ALL PowerShell history? (y/N)"
    if ($confirmation -eq 'y' -or $confirmation -eq 'Y') {
        Clear-AllHistory
    } else {
        Write-Host "History clearing cancelled." -ForegroundColor Yellow
        Write-Host "Use 'Get-Help .\Clear-PwshHistory.ps1' for more options." -ForegroundColor Cyan
    }
}
