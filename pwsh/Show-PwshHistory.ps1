# PowerShell History Management Script
# This script displays PowerShell history and provides options to clear it

param(
    [switch]$Clear,
    [switch]$ShowAll,
    [int]$Last = 20,
    [switch]$Help,
    [string]$MatchString
)

function Show-Help {
    Write-Host "PowerShell History Management Script" -ForegroundColor Green
    Write-Host ""
    Write-Host "Usage:" -ForegroundColor Yellow
    Write-Host "  .\Show-PwshHistory.ps1                        # Show last 20 commands"
    Write-Host "  .\Show-PwshHistory.ps1 -Last 50               # Show last 50 commands"
    Write-Host "  .\Show-PwshHistory.ps1 -ShowAll               # Show all history"
    Write-Host "  .\Show-PwshHistory.ps1 -MatchString 'foo'     # Show only lines containing 'foo'"
    Write-Host "  .\Show-PwshHistory.ps1 -Clear                 # Clear PowerShell history"
    Write-Host "  .\Show-PwshHistory.ps1 -Help                  # Show this help"
    Write-Host ""
    Write-Host "Options:" -ForegroundColor Yellow
    Write-Host "  -MatchString   Show only history lines containing the specified string."
    Write-Host "  -ShowAll       Show all available history."
    Write-Host "  -Last <n>      Show the last n commands (default: 20)."
    Write-Host "  -Clear         Clear all history (session and PSReadLine)."
    Write-Host ""
    Write-Host "History file locations:" -ForegroundColor Yellow
    Write-Host "  Current session: Get-History"
    Write-Host "  PSReadLine history: $((Get-PSReadLineOption).HistorySavePath)"
    Write-Host ""
    Write-Host "Note: -MatchString applies to both session and PSReadLine history display." -ForegroundColor Yellow
}

function Show-HistoryInfo {
    Write-Host "PowerShell History Information" -ForegroundColor Green
    Write-Host "=============================" -ForegroundColor Green
    
    # Show current session history count
    $sessionHistory = Get-History
    Write-Host "Current session history count: $($sessionHistory.Count)" -ForegroundColor Cyan
    
    # Show PSReadLine history file info
    $historyPath = (Get-PSReadLineOption).HistorySavePath
    if (Test-Path $historyPath) {
        $historyLines = Get-Content $historyPath | Measure-Object -Line
        Write-Host "PSReadLine history file: $historyPath" -ForegroundColor Cyan
        Write-Host "PSReadLine history count: $($historyLines.Lines)" -ForegroundColor Cyan
        Write-Host "History file size: $([math]::Round((Get-Item $historyPath).Length / 1KB, 2)) KB" -ForegroundColor Cyan
    } else {
        Write-Host "PSReadLine history file not found: $historyPath" -ForegroundColor Yellow
    }
    Write-Host ""
}

function Show-SessionHistory {
    param([int]$Count, [string]$MatchString)
    $title = if ($MatchString) { "Current Session History (matching '$MatchString'):" } else { "Current Session History (last $Count commands):" }
    Write-Host $title -ForegroundColor Green
    Write-Host ("=" * $title.Length) -ForegroundColor Green
    $history = Get-History
    if ($history.Count -eq 0) {
        Write-Host "No history available in current session." -ForegroundColor Yellow
        return
    }
    if ($MatchString) {
        $filtered = $history | Where-Object { $_.CommandLine -match [regex]::Escape($MatchString) }
        if ($filtered.Count -eq 0) {
            Write-Host "No session history lines match '$MatchString'." -ForegroundColor Yellow
            return
        }
        $filtered | ForEach-Object {
            Write-Host "$($_.Id.ToString().PadLeft(4)): $($_.CommandLine)" -ForegroundColor White
        }
    } else {
        $startIndex = [Math]::Max(0, $history.Count - $Count)
        $history[$startIndex..($history.Count - 1)] | ForEach-Object {
            Write-Host "$($_.Id.ToString().PadLeft(4)): $($_.CommandLine)" -ForegroundColor White
        }
    }
    Write-Host ""
}

function Show-PSReadLineHistory {
    param([int]$Count, [bool]$ShowAll, [string]$MatchString)
    $historyPath = (Get-PSReadLineOption).HistorySavePath
    if (-not (Test-Path $historyPath)) {
        Write-Host "PSReadLine history file not found: $historyPath" -ForegroundColor Yellow
        return
    }
    $allLines = Get-Content $historyPath
    if ($allLines.Count -eq 0) {
        Write-Host "No history found in PSReadLine file." -ForegroundColor Yellow
        return
    }
    if ($MatchString) {
        $filtered = $allLines | Where-Object { $_ -match [regex]::Escape($MatchString) }
        Write-Host "PSReadLine History (matching '$MatchString'):" -ForegroundColor Green
        Write-Host ("=" * (28 + $MatchString.Length)) -ForegroundColor Green
        if ($filtered.Count -eq 0) {
            Write-Host "No PSReadLine history lines match '$MatchString'." -ForegroundColor Yellow
            return
        }
        for ($i = 0; $i -lt $filtered.Count; $i++) {
            Write-Host "$($i+1).ToString().PadLeft(4)): $($filtered[$i])" -ForegroundColor White
        }
        Write-Host ""
        return
    }
    Write-Host "PSReadLine History:" -ForegroundColor Green
    Write-Host "==================" -ForegroundColor Green
    if ($ShowAll) {
        $linesToShow = $allLines
        Write-Host "Showing all $($allLines.Count) commands:" -ForegroundColor Cyan
    } else {
        $startIndex = [Math]::Max(0, $allLines.Count - $Count)
        $linesToShow = $allLines[$startIndex..($allLines.Count - 1)]
        Write-Host "Showing last $($linesToShow.Count) commands:" -ForegroundColor Cyan
    }
    for ($i = 0; $i -lt $linesToShow.Count; $i++) {
        $lineNumber = if ($ShowAll) { $i + 1 } else { $allLines.Count - $linesToShow.Count + $i + 1 }
        Write-Host "$($lineNumber.ToString().PadLeft(4)): $($linesToShow[$i])" -ForegroundColor White
    }
    Write-Host ""
}

function Clear-PowerShellHistory {
    Write-Host "Clearing PowerShell History..." -ForegroundColor Yellow
    
    # Clear current session history
    Clear-History
    Write-Host "✓ Current session history cleared" -ForegroundColor Green
    
    # Clear PSReadLine history
    $historyPath = (Get-PSReadLineOption).HistorySavePath
    if (Test-Path $historyPath) {
        try {
            Remove-Item $historyPath -Force
            Write-Host "✓ PSReadLine history file deleted: $historyPath" -ForegroundColor Green
        } catch {
            Write-Host "✗ Failed to delete PSReadLine history file: $($_.Exception.Message)" -ForegroundColor Red
        }
    } else {
        Write-Host "PSReadLine history file not found: $historyPath" -ForegroundColor Yellow
    }
    
    # Clear the in-memory PSReadLine history
    try {
        [Microsoft.PowerShell.PSConsoleReadLine]::ClearHistory()
        Write-Host "✓ PSReadLine in-memory history cleared" -ForegroundColor Green
    } catch {
        Write-Host "Note: Could not clear PSReadLine in-memory history: $($_.Exception.Message)" -ForegroundColor Yellow
    }
    
    Write-Host ""
    Write-Host "PowerShell history has been cleared!" -ForegroundColor Green
    Write-Host "Note: You may need to restart PowerShell for all changes to take effect." -ForegroundColor Cyan
}

# Main script logic
if ($Help) {
    Show-Help
    exit
}

if ($Clear) {
    $confirmation = Read-Host "Are you sure you want to clear all PowerShell history? (y/N)"
    if ($confirmation -eq 'y' -or $confirmation -eq 'Y') {
        Clear-PowerShellHistory
    } else {
        Write-Host "History clearing cancelled." -ForegroundColor Yellow
    }
    exit
}

# Display history information
Show-HistoryInfo


# Show session history
if ($ShowAll) {
    $sessionHistory = Get-History
    if ($sessionHistory.Count -gt 0) {
        Show-SessionHistory -Count $sessionHistory.Count -MatchString $MatchString
    }
} else {
    Show-SessionHistory -Count $Last -MatchString $MatchString
}

# Show PSReadLine history
Show-PSReadLineHistory -Count $Last -ShowAll $ShowAll -MatchString $MatchString

Write-Host "Use -Clear parameter to clear all history, or -Help for more options." -ForegroundColor Cyan
