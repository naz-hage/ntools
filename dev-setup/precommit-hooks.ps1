# PowerShell script to install or uninstall pre-commit hooks for ntools
# Usage: Run this script from the repo root or dev-setup folder
#        .\precommit-hook.ps1 [-Action install|uninstall]

param(
    [ValidateSet("install", "uninstall")]
    [string]$Action = "install"
)

Write-Host "Pre-commit hook action: $Action" -ForegroundColor Cyan

# Ensure pre-commit is installed and on PATH, or use full path if needed
$preCommitCmd = Get-Command pre-commit -ErrorAction SilentlyContinue
if (-not $preCommitCmd) {
    if ($Action -eq "install") {
        Write-Host "pre-commit not found on PATH. Installing via pip..." -ForegroundColor Yellow
        pip install --user pre-commit
        $userScripts = Join-Path $env:USERPROFILE "AppData\Roaming\Python\Python313\Scripts"
        $preCommitExe = Join-Path $userScripts "pre-commit.exe"
        if (Test-Path $preCommitExe) {
            Write-Host "pre-commit installed at $preCommitExe. Running using full path..." -ForegroundColor Yellow
            if ($Action -eq "install") {
                & $preCommitExe install
                & $preCommitExe install --hook-type commit-msg
                Write-Host "Pre-commit hooks installed successfully!" -ForegroundColor Green
            }
        } else {
            Write-Host "ERROR: pre-commit not found after installation. Please add $userScripts to your PATH or restart your terminal." -ForegroundColor Red
            exit 1
        }
    } else {
        Write-Host "pre-commit is not installed. Nothing to uninstall." -ForegroundColor Yellow
    }
} else {
    if ($Action -eq "install") {
        & pre-commit install
        & pre-commit install --hook-type commit-msg
        Write-Host "Pre-commit hooks installed successfully!" -ForegroundColor Green
    } elseif ($Action -eq "uninstall") {
        & pre-commit uninstall
        Write-Host "Pre-commit hooks uninstalled successfully!" -ForegroundColor Green
    }
}
