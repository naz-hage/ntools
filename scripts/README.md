# NTools PowerShell Scripts

This directory contains the consolidated PowerShell module for the NTools project. All scripts have been converted to functions within the NTools.Scripts module for better maintainability and reuse.

## Directory Structure

- **`module-package/`** - **NTools.Scripts PowerShell Module** - Consolidated module containing all functions
- **`setup/`** - Entry point scripts for installation (uses module functions internally)
- **`build/`**, **`devops/`**, **`test/`** - Legacy individual scripts (deprecated, use module functions instead)

## NTools.Scripts Module (Primary Interface)

The **`module-package/`** folder contains the consolidated NTools.Scripts PowerShell module (v2.3.0) that includes functions converted from all original script categories:

### Available Functions by Category:

#### Build Functions
- `Invoke-ArtifactVerification` - Comprehensive artifact verification (was `build-verify-artifacts.ps1`)
- `Publish-AllProjects` - Build and publish all projects with deterministic repository path
- `Get-ProjectFiles` - Get project files with filtering
- `Invoke-ProjectPublish` - Publish individual projects

#### DevOps Functions  
- `Get-AgentIPAddress` - Get public IP for Azure DevOps agents (was `devops-get-ip.ps1`)
- `Install-PreCommitHooks` - Install git pre-commit hooks (was `devops-precommit-hooks.ps1`)
- `Add-WAFRule` - Add Azure WAF rules (was `devops-waf-add-rule.ps1`)
- `Remove-WAFRule` - Remove Azure WAF rules (was `devops-waf-delete-rule.ps1`)
- `Get-VersionFromJson` - Extract version information from JSON files
- `Update-MarkdownTable` - Update version tables in markdown documentation

#### Setup Functions
- `Set-DevelopmentEnvironment` - Set up development environment (was `setup-environment.ps1`)
- `Install-DevelopmentApps` - Install development applications (was `setup-install-apps.ps1`)
- `Set-CodeSigningTrust` - Configure code signing trust (was `setup-signing-trust.ps1`)
- `Set-CodeSigning` - Configure code signing (was `setup-signing.ps1`)
- `Install-NTools` - Install NTools from releases with configurable ntools.json path
- `Install-NToolsScriptsModule` - Install this module (was `install-module.ps1`)

#### Test Functions
- `Invoke-CodeCoverage` - Run tests with code coverage (was `test-coverage.ps1`)
- `Test-MSBuildDelegation` - Test MSBuild target delegation (was `test-delegation.ps1`)
- `Test-QuickTargets` - Quick target validation (was `test-target-quick.ps1`)
- `Test-NToolsScriptsModule` - Test module functionality (was `test-module.ps1`)
- `Write-TestResult` - Write formatted test results
- `Test-TargetExists` - Check if MSBuild targets exist
- `Test-TargetDependencies` - Validate target dependencies
- `Test-TargetDelegation` - Test target delegation patterns

#### Utility Functions
- `Get-FileHash256` - Calculate SHA256 hash of files
- `Get-FileVersionInfo` - Get file version information
- `Invoke-FastForward` - Git fast-forward operations
- `Write-OutputMessage` - Standardized output messaging
- `Get-NToolsFileVersion` - Get NTools file version information
- `Add-DeploymentPathToEnvironment` - Add paths to PATH environment variable
- `Invoke-NToolsDownload` - Download NTools packages

#### Common Functions
- `Write-Info`, `Write-Success`, `Write-Warning`, `Write-Error` - Standardized logging functions
- `Get-NtoolsScriptsVersion` - Get module version information

### Usage Examples:

```powershell
# Import the module
Import-Module "./scripts/module-package/NTools.Scripts.psm1" -Force

# Get module information
Get-NtoolsScriptsVersion
Get-Command -Module NTools.Scripts | Select-Object Name

# Use module functions
Install-NTools -NtoolsJsonPath "./dev-setup/ntools.json"
Publish-AllProjects -OutputDir ".\artifacts" -Version "1.0.0" -RepositoryRoot (Get-Location)
Invoke-CodeCoverage
Test-MSBuildDelegation
```

### MSBuild Integration:
The module is automatically integrated with the build system:
- `INSTALL_NTOOLS_SCRIPTS` - Install the module during build
- `PUBLISH` - Uses `Publish-AllProjects` function with deterministic repository path

### GitHub Actions Integration:
```yaml
- name: Install ntools using NTools.Scripts module
  run: |
    Import-Module "./scripts/module-package/NTools.Scripts.psm1" -Force
    Install-NTools -NtoolsJsonPath "./dev-setup/ntools.json"
```

## Entry Point Scripts (setup/ folder)

The `setup/` folder contains entry point scripts that use the module functions internally:

- `setup-install-ntools.ps1` - Entry point for NTools installation (calls `Install-NTools`)

These scripts provide backward compatibility while internally using the consolidated module.

## Legacy Scripts (Deprecated)

Individual scripts in `build/`, `devops/`, and `test/` folders are maintained for reference but are deprecated:

### Migration Path:
```powershell
# Old way
./scripts/build/build-verify-artifacts.ps1

# New way
Import-Module "./scripts/module-package/NTools.Scripts.psm1" -Force
Invoke-ArtifactVerification

# Old way  
./scripts/devops/devops-get-ip.ps1

# New way
Import-Module "./scripts/module-package/NTools.Scripts.psm1" -Force
Get-AgentIPAddress
```

## Key Improvements in v2.3.0

### 1. Deterministic Repository Path Detection
The `Publish-AllProjects` function now requires explicit `RepositoryRoot` parameter:
```powershell
# Before (unreliable heuristic detection)
Publish-AllProjects -OutputDir $output -Version $version

# After (deterministic)
Publish-AllProjects -OutputDir $output -Version $version -RepositoryRoot $repoRoot
```

### 2. Configurable ntools.json Path
```powershell
# Use specific configuration file
Install-NTools -NtoolsJsonPath "./dev-setup/ntools.json"
```

### 3. Unified Error Handling
All functions use consistent logging:
```powershell
Write-Info "Starting operation..."
Write-Success "Operation completed successfully"
Write-Warning "Potential issue detected"
Write-Error "Operation failed"
```

## Documentation

For complete module documentation, see [NTools.Scripts Module Documentation](../docs/ntools/ntools-scripts-module.md).

## Best Practices

1. **Use the module functions instead of individual scripts**
2. **Always import with -Force during development** to reload changes
3. **Use explicit parameters** like `-RepositoryRoot` for deterministic behavior
4. **Check module version** with `Get-NtoolsScriptsVersion` for troubleshooting