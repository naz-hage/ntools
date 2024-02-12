@echo off
SetLocal EnableDelayedExpansion
set DEV_DRIVE=c:
set MAIN_DIR=source
:: Check if admin
net session >nul 2>&1
if %errorLevel% == 0 (
    echo Admin rights detected
) else (
    echo Please run this script as an administrator.
    exit /b 1
)
:: Install latest Ntools from github
:: save current directory
set currentdir=%cd%
cd nbuild\resources
powershell.exe -Command ".\install-app.ps1 install .\app-Dotnet_Runtime.json
powershell.exe -Command ".\install-app.ps1 install .\app-Ntools.json
powershell.exe -Command ".\install-ntools.ps1" %DEV_DRIVE% %MAIN_DIR%
:: Restore current directory
cd %currentdir%
:: Delete the installer
