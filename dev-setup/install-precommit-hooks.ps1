# PowerShell script to install pre-commit hooks for ntools
# Usage: Run this script from the repo root or dev-setup folder

Write-Host "Installing pre-commit hooks using pre-commit framework..." -ForegroundColor Cyan

# Ensure pre-commit is installed
if (-not (Get-Command pre-commit -ErrorAction SilentlyContinue)) {
    Write-Host "pre-commit not found. Installing via pip..." -ForegroundColor Yellow
    pip install pre-commit
}

# Install hooks
pre-commit install
pre-commit install --hook-type commit-msg

Write-Host "Pre-commit hooks installed successfully!" -ForegroundColor Green
