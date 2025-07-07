<#
.SYNOPSIS
    This script imports various PowerShell modules required for DevOps scripts.

.DESCRIPTION
    The script imports the following modules:
    - common.psm1: Contains common utility functions.
    - service-principal.psm1: Contains functions related to service principals.
    - service-connection.psm1: Contains functions related to service connections.
    - fast.psm1: Contains functions related to fast operations.
    - key-vault.psm1: Contains functions related to Azure Key Vault.
    - securestring.psm1: Contains functions related to secure strings.

.PARAMETER moduleName
    The name of the module to be imported.

.EXAMPLE
    .\import-ps-modules.ps1

.NOTES
    Ensure that the module files exist in the specified paths.
#>

# Function to import a module
function ImportPsModule {
    param (
        [string]$moduleName
    )

    $modulePath = Resolve-Path -Path "$PSScriptRoot/$moduleName"
    if (-Not (Test-Path -Path "$modulePath")) {
        HandleError "The module file $moduleName does not exist at path $modulePath"
    }
    Import-Module "$modulePath" -Force -DisableNameChecking
    LogMessage "Successfully imported module $moduleName" -ForegroundColor Green
}

# Import the common module
$modulePath = Resolve-Path -Path "$PSScriptRoot/repos.psm1"
if (-Not (Test-Path -Path "$modulePath")) {
    Write-Error " ==> The module file common.psm1 does not exist at path $modulePath"
    exit 1
}
Import-Module "$modulePath" -Force -DisableNameChecking