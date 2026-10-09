# sdo-scripts Module API

This is the canonical reference for functions exported by the `sdo-scripts` PowerShell module. The list is defined by `FunctionsToExport` in `scripts/module-package/sdo-scripts.psd1`.

| Function | Description and common usage |
|---|---|
| `Publish-AllProjects` | Build and publish non-test projects. `Publish-AllProjects -OutputDir C:\Artifacts -Version 1.0.0 -RepositoryRoot C:\MyRepo` |
| `Get-SdoScriptsVersion` | Return the module version. `Get-SdoScriptsVersion` |
| `Set-DevelopmentEnvironment` | Set local development environment variables. `Set-DevelopmentEnvironment -DevDrive D: -MainDir source` |
| `Get-VersionFromYaml` | Read version fields from an `sdo.yaml` file. `Get-VersionFromYaml -YamlPath ./dev-setup/sdo.yaml` |
| `Write-TestResult` | Write a standardized test result. `Write-TestResult -Name smoke -Passed $true` |
| `Test-TargetExists` | Check whether an MSBuild target exists. `Test-TargetExists -ProjectFile foo.targets -TargetName Publish` |
| `Test-TargetDependencies` | Validate MSBuild target dependencies. `Test-TargetDependencies -ProjectFile foo.targets -TargetName Publish` |
| `Test-TargetDelegation` | Verify target delegation used by `sdo`. `Test-TargetDelegation -SolutionDir .` |
| `Get-FileHash256` | Compute a SHA256 file hash. `Get-FileHash256 -Path C:\Artifacts\sdo.exe` |
| `Get-FileVersionInfo` | Read file and product version metadata. `Get-FileVersionInfo -Path C:\Artifacts\sdo.exe` |
| `Invoke-FastForward` | Fast-forward a Git ref. `Invoke-FastForward -Repo . -Remote origin -Branch main` |
| `Write-OutputMessage` | Write consistently formatted output. `Write-OutputMessage -Level Info -Message Starting` |
| `Get-SdoFileVersion` | Read the SDO product version from a binary. `Get-SdoFileVersion -FilePath C:\Artifacts\sdo.exe` |
| `Add-DeploymentPathToEnvironment` | Add a deployment path to `PATH`. `Add-DeploymentPathToEnvironment -Path C:\My\deploy\bin` |
| `Invoke-SdoDownload` | Download SDO release artifacts. `Invoke-SdoDownload -Version 1.2.3 -DownloadsDirectory C:\Downloads` |
| `Install-Sdo` | Install an SDO version from a release. `Install-Sdo -Version 1.74.0` |

For runtime discovery:

```powershell
Import-Module './scripts/module-package/sdo-scripts.psm1' -Force
Get-Command -Module sdo-scripts
```
