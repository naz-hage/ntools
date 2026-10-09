@{
    RootModule        = 'sdo-scripts.psm1'
    ModuleVersion     = '3.0.0'
    GUID              = 'b3b7d6a8-0000-4000-8000-000000000001'
    Author            = 'naz-hage'
    CompanyName       = 'naz-hage'
    PowerShellVersion = '5.1'
    Copyright         = '(c) naz-hage'
    Description       = 'Comprehensive PowerShell module for SDO (sdo-scripts) with consolidated functions from build, devops, test, utility, and install scripts'
    FileList          = @(
        'sdo-scripts.psm1'
    )
    FunctionsToExport = @(
        'Publish-AllProjects',
        'Get-SdoScriptsVersion',
        'Set-DevelopmentEnvironment',
        'Get-VersionFromYaml',
        'Write-TestResult',
        'Test-TargetExists',
        'Test-TargetDependencies',
        'Test-TargetDelegation',
        'Get-FileHash256',
        'Get-FileVersionInfo',
        'Invoke-FastForward',
        'Write-OutputMessage',
        'Get-SdoFileVersion',
        'Add-DeploymentPathToEnvironment',
        'Invoke-SdoDownload',
        'Install-Sdo'
    )
}
