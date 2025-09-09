@{
    RootModule        = 'ntools-scripts.psm1'
    ModuleVersion     = '2.3.0'
    GUID              = 'b3b7d6a8-0000-4000-8000-000000000001'
    Author            = 'n-tools'
    CompanyName       = 'n-tools'
    PowerShellVersion = '5.1'
    Copyright         = '(c) n-tools'
    Description       = 'Comprehensive PowerShell module for NTools (ntools-scripts) with consolidated functions from build, devops, test, utility, and install scripts'
    FileList          = @(
        'ntools-scripts.psm1'
    )
    FunctionsToExport = @(
        'Publish-AllProjects',
        'Get-ntoolsScriptsVersion',
        'Get-VersionFromJson',
        'Update-MarkdownTable',
        'Write-TestResult',
        'Test-TargetExists',
        'Test-TargetDependencies',
        'Test-TargetDelegation',
        'Get-FileHash256',
        'Get-FileVersionInfo',
        'Invoke-FastForward',
        'Write-OutputMessage',
        'Get-NToolsFileVersion',
        'Add-DeploymentPathToEnvironment',
        'Invoke-NToolsDownload',
        'Install-NTools'
    )
}
+

