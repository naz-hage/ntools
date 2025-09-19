# ntools-scripts PowerShell Module

This page documents the `ntools-scripts` PowerShell module and serves as the canonical API reference for the module's exported functions and usage.

**Location**: `scripts/module-package/ntools-scripts.psm1`

## Quick start

Follow these minimal steps to start using the module locally or in CI.

```powershell
# Import for local development
Import-Module "./scripts/module-package/ntools-scripts.psm1" -Force

# Check the module version
Get-NtoolsScriptsVersion

# Install NTools using a local configuration file
Install-NTools -NtoolsJsonPath "./dev-setup/ntools.json"
```

## Table of contents

- Quick start
- Overview
- Import the Module
- Usage examples
- Module information
- Architecture & migration
- Module development
- Troubleshooting
- Canonical API reference


## Overview
Module files:
```
scripts/
## Available functions (summary)

The module exports a broad set of functions covering build, CI/devops, setup/install, testing, utilities, and code-signing helpers. For the complete, authoritative list of exported function names and the exact API surface, see the dedicated API reference:

- docs/ntools/ntools-scripts-module-api.md

If you need to discover functions at runtime, import the module and run:

```powershell
Get-Command -Module ntools-scripts | Sort-Object Name
```
```powershell
# Test the module
Import-Module "./scripts/module-package/ntools-scripts.psm1" -Force
Test-NToolsScriptsModule
```
## Manual Installation
```powershell
# Install module manually for development
Install-NToolsScriptsModule -InstallPath "$env:ProgramFiles\WindowsPowerShell\Modules\ntools-scripts" -Force
```


## Module Information

**Version**: 2.3.0  
**Location**: `scripts/module-package/ntools-scripts.psm1`  
**Installation**: Automatically installed via MSBuild targets and GitHub Actions

## Architecture

The module consolidates functionality from the previous script structure:

## Available Functions

The module exports 36 functions organized by category:

### Build Functions
-- `Publish-AllProjects` - Build and publish all non-test projects with deterministic repository path
-- `Invoke-VerifyArtifacts` - Comprehensive artifact verification
- `Get-ProjectFiles` - Get project files with filtering
- `Invoke-ProjectPublish` - Publish individual projects

### DevOps Functions  
- `Get-AgentPublicIp` - Get public IP for Azure DevOps agents and set pipeline variable
- `Add-WafAllowRule` - Add Azure WAF allow rule for an IP
- `Remove-WafCustomRule` - Remove Azure WAF custom rule
- `Get-VersionFromJson` - Extract version information from JSON files
- `Update-DocVersions` - Update version tables in markdown documentation

### Setup Functions
- `Set-DevelopmentEnvironment` - Set up development environment
- `Install-DevelopmentApps` - Install development applications
- `Install-NTools` - Install NTools from releases (with configurable ntools.json path)
- `Install-NToolsScriptsModule` - Install this module

### Test Functions
- `Invoke-CodeCoverage` - Run tests with code coverage
- `Test-MSBuildDelegation` - Test MSBuild target delegation (**Note**: Now automatically integrated into `nb smoke_test` target)
- `Test-QuickTargets` - Quick target validation
- `Test-NToolsScriptsModule` - Test module functionality
- `Write-TestResult` - Write formatted test results
- `Test-TargetExists` - Check if MSBuild targets exist
- `Test-TargetDependencies` - Validate target dependencies
- `Test-TargetDelegation` - Test target delegation patterns (**Note**: Core function used by `nb smoke_test` target)

### Utility Functions
- `Get-FileHash256` - Calculate SHA256 hash of files
- `Get-FileVersionInfo` - Get file version information
- `Invoke-FastForward` - Git fast-forward operations
- `Write-OutputMessage` - Standardized output messaging
- `Get-NToolsFileVersion` - Get NTools file version information
- `Add-DeploymentPathToEnvironment` - Add paths to PATH environment variable
- `Invoke-NToolsDownload` - Download NTools packages

### Common Functions
- `Write-Info` - Write informational messages
- `Write-Success` - Write success messages
- `Write-Warning` - Write warning messages
- `Write-Error` - Write error messages
- `Get-NtoolsScriptsVersion` - Get module version information

## Canonical module API (single source of truth)

The complete, authoritative list of exported functions is available in the separate API reference:

- docs/ntools/ntools-scripts-module-api.md

For quick discovery at runtime, import the module and list commands:

```powershell
Import-Module "./scripts/module-package/ntools-scripts.psm1" -Force
Get-Command -Module ntools-scripts | Select-Object Name
```


## Usage Examples
For import instructions, see the "Import the Module" section near the top of this page.

### Get Module Information
```powershell
# Get version
Get-NtoolsScriptsVersion

# List all available functions
Get-Command -Module ntools-scripts | Select-Object Name | Format-Table -AutoSize
```

### Install NTools with Custom Configuration
```powershell
# Install using specific ntools.json file
Install-NTools -NtoolsJsonPath "./dev-setup/ntools.json"

# Install specific version
Install-NTools -Version "1.29.7" -DownloadsDirectory "C:\MyDownloads"
```

### Publish Projects with Deterministic Path
```powershell
# Publish all projects with explicit repository root
Publish-AllProjects -OutputDir "C:\Artifacts" -Version "1.0.0" -RepositoryRoot "C:\MyRepo"
```

### Run Tests and Coverage
```powershell
# Run code coverage
Invoke-CodeCoverage

# Test MSBuild delegation (also available via 'nb smoke_test')
Test-MSBuildDelegation

# Or use the comprehensive smoke test target
# nb smoke_test  # (from command line - includes both artifact validation AND target delegation)
```

### Integration with Build System
- **Usage**: `PUBLISH` target uses `Publish-AllProjects` function with deterministic repository path

### Artifact Verification (MSBuild)
The module exposes `Invoke-VerifyArtifacts` which is also wired into MSBuild via the `VERIFY_ARTIFACTS` target in `nbuild.targets`.

PowerShell example (local):
```powershell
Import-Module "./scripts/module-package/ntools-scripts.psm1" -Force
Invoke-VerifyArtifacts -ArtifactsPath "C:\Artifacts\MySolution\Release\1.2.3" -ProductVersion "1.2.3"
```

MSBuild / nb CLI example:
```bash
# Run the MSBuild target from the repo root (nb delegates to MSBuild)
nb verify_artifacts /p:ArtifactsFolder="C:\Artifacts\MySolution\Release\1.2.3" /p:ProductVersion="1.2.3"
```

### MSBuild Integration
- **Usage**: `PUBLISH` target uses `Publish-AllProjects` function with deterministic repository path
- **Smoke Testing**: `SMOKE_TEST` target uses `Test-TargetDelegation` function for build system validation
- **Location**: Module installed to `$env:ProgramFiles\nbuild\modules\ntools-scripts\`

### SMOKE_TEST Target Integration
The comprehensive `SMOKE_TEST` target combines artifact validation with PowerShell module functions:

```bash
# Comprehensive smoke test (recommended)
nb smoke_test
```

This target performs:
1. **Artifact Validation**: Tests 4+ executables (nb.exe, lf.exe, nBackup.exe, wi.exe)
2. **Build System Validation**: Uses `Test-TargetDelegation` function to verify MSBuild target relationships
3. **Consolidated Results**: Single pass/fail result for all validation checks

**Migration Note**: The deprecated `TEST_TARGET_DELEGATION` target functionality is now integrated into `SMOKE_TEST`.

### GitHub Actions Integration
```yaml
- name: Install ntools using ntools-scripts module
  run: |
    Import-Module "./scripts/module-package/ntools-scripts.psm1" -Force
    Install-NTools -NtoolsJsonPath "./dev-setup/ntools.json"
```

## Module Development

### Adding New Functions
1. Add function to appropriate section in `ntools-scripts.psm1`
2. Add function name to `Export-ModuleMember` line
3. Update `FunctionsToExport` in `ntools-scripts.psd1`
4. Increment module version
5. Update documentation

### Testing
```powershell
# Test the module
Import-Module "./scripts/module-package/ntools-scripts.psm1" -Force
Test-NToolsScriptsModule
```

### Installation
```powershell
# Install module manually for development
Install-NToolsScriptsModule -InstallPath "$env:ProgramFiles\WindowsPowerShell\Modules\ntools-scripts" -Force
```

## Best Practices

1. **Always specify -Force when importing** to reload changes during development
2. **Use explicit parameters** like `-RepositoryRoot` for deterministic behavior
3. **Import the module** before calling any functions
4. **Check module version** with `Get-NtoolsScriptsVersion` for troubleshooting
5. **Use the utility functions** like `Write-Info`, `Write-Success` for consistent output

## Troubleshooting

### Module Not Found
```powershell
# Verify module location
Test-Path "./scripts/module-package/ntools-scripts.psm1"

# Check if installed
Test-Path "$env:ProgramFiles\nbuild\modules\ntools-scripts\ntools-scripts.psm1"
```

### Version Issues
```powershell
# Check module version
Get-NtoolsScriptsVersion

# Check installed version vs source
Import-Module "./scripts/module-package/ntools-scripts.psm1" -Force
Get-NtoolsScriptsVersion
```

### Function Not Available
```powershell
# List all available functions
Get-Command -Module ntools-scripts

# Check if module is properly imported
Get-Module ntools-scripts
```
