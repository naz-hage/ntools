# ntools-scripts Module API

This page lists the exported functions from the `ntools-scripts` PowerShell module in a compact table (Function | Description & common usage). Use this as the canonical reference and link to it from other docs.

| Function | Description & common usage |
|---|---|
| Get-ntoolsScriptsVersion | Returns the module version. Usage: `Get-ntoolsScriptsVersion` |
| Publish-AllProjects | Build and publish non-test projects to an output directory. Usage: `Publish-AllProjects -OutputDir C:\Artifacts -Version 1.0.0 -RepositoryRoot C:\MyRepo` |
| Get-VersionFromJson | Read version fields from a JSON file (ntools.json style). Usage: `Get-VersionFromJson -Path ./dev-setup/ntools.json` |
| Update-DocVersions | Update a markdown table of versions in a file. Usage: `Update-DocVersions -File docs/versions.md -Version 1.2.3` |
| Write-TestResult | Write a standardized test result line. Usage: `Write-TestResult -Name 'smoke' -Passed $true` |
| Test-TargetExists | Check whether an MSBuild target exists in a project/targets file. Usage: `Test-TargetExists -ProjectFile foo.targets -TargetName Publish` |
| Test-TargetDependencies | Validate MSBuild target dependencies. Usage: `Test-TargetDependencies -ProjectFile foo.targets -TargetName Publish` |
| Test-TargetDelegation | Verify MSBuild target delegation patterns used by `nb`. Usage: `Test-TargetDelegation -SolutionDir .` |
| Get-FileHash256 | Compute SHA256 hash of a file. Usage: `Get-FileHash256 -Path C:\Artifacts\nb.exe` |
| Get-FileVersionInfo | Read file version metadata (file version/product version). Usage: `Get-FileVersionInfo -Path C:\Artifacts\nb.exe` |
| Invoke-FastForward | Fast-forward a git ref to a specified commit/branch. Usage: `Invoke-FastForward -Repo . -Remote origin -Branch main` |
| Write-OutputMessage | Consistent formatted output writer (info/warn/error). Usage: `Write-OutputMessage -Level Info -Message 'Starting'` |
| Get-NToolsFileVersion | Helper to get NTools product version from binaries. Usage: `Get-NToolsFileVersion -FilePath C:\Artifacts\nb.exe` |
| Add-DeploymentPathToEnvironment | Add deploy path to PATH for current process/user. Usage: `Add-DeploymentPathToEnvironment -Path C:\My\deploy\bin` |
| Invoke-NToolsDownload | Download NTools release artifacts (zip/nuget). Usage: `Invoke-NToolsDownload -Version 1.2.3 -OutputDir C:\Downloads` |
| Install-NTools | Install NTools packages based on an ntools.json config. Usage: `Install-NTools -NtoolsJsonPath ./dev-setup/ntools.json` |
| Invoke-VerifyArtifacts | Run artifact validation (hashes, versions). Usage: `Invoke-VerifyArtifacts -ArtifactsPath C:\Artifacts\MySolution\Release\1.2.3 -ProductVersion 1.2.3` |
| Set-DevelopmentEnvironment | Set local dev env variables (DevDrive/MainDir). Usage: `Set-DevelopmentEnvironment -DevDrive 'D:' -MainDir 'source'` |
| Test-IsAdministrator | Returns true if running elevated. Usage: `Test-IsAdministrator` |
| Test-MicrosoftPowerShellSecurityModuleLoaded | Check for Microsoft.PowerShell.Security module availability. Usage: `Test-MicrosoftPowerShellSecurityModuleLoaded` |
| Test-CertificateStore | Validate certificate presence in store. Usage: `Test-CertificateStore -Thumbprint <thumbprint>` |
| New-SelfSignedCodeCertificate | Create a self-signed code-signing certificate (dev). Usage: `New-SelfSignedCodeCertificate -Subject 'CN=ntools-dev' -ExportPath ./dev-cert.pfx` |
| Export-CertificateToPfx | Export a certificate object to PFX file. Usage: `Export-CertificateToPfx -Certificate $cert -Password (ConvertTo-SecureString -AsPlainText 'pw' -Force) -Path ./cert.pfx` |
| Export-CertificateToCer | Export certificate to .cer (DER/PEM). Usage: `Export-CertificateToCer -Certificate $cert -Path ./cert.cer` |
| Import-CertificateToRoot | Import a certificate to the LocalMachine\Root store. Usage: `Import-CertificateToRoot -Path ./cert.cer` |
| Import-CertificateToCurrentUser | Import a certificate to CurrentUser store. Usage: `Import-CertificateToCurrentUser -Path ./cert.pfx -Password (ConvertTo-SecureString -AsPlainText 'pw' -Force)` |
| Set-ScriptSignature | Sign a script file with a certificate. Usage: `Set-ScriptSignature -ScriptPath ./scripts/setup/install.ps1 -CertificateThumbprint <thumbprint>` |
| Get-ScriptSignature | Get signature information for a script file. Usage: `Get-ScriptSignature -ScriptPath ./scripts/setup/install.ps1` |
| Set-CodeSigningTrust | Add a certificate to the machine/user trust store for code signing flows. Usage: `Set-CodeSigningTrust -Path ./cert.cer -Scope Machine` |

Notes:
- For runtime discovery: `Import-Module './scripts/module-package/ntools-scripts.psm1' -Force; Get-Command -Module ntools-scripts`.


