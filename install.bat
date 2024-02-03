@echo off
SetLocal EnableDelayedExpansion
set DOTNET_VERSION=8.0.1
set NTOOLS_VERSION=1.2.35
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
powershell.exe -Command ".\install-ntools.ps1" %DOTNET_VERSION% %NTOOLS_VERSION% %DEV_DRIVE% %MAIN_DIR%
:: Restore current directory
cd %currentdir%
:: Delete the installer
