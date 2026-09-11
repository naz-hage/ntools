# ntools-scripts PowerShell Module

This page documents how to install, use, develop, and troubleshoot the `ntools-scripts` PowerShell module. The [module API reference](ntools-scripts-module-api.md) is the canonical list of exported functions.

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

- [Quick start](#quick-start)
- [Overview](#overview)
- [Installation](#installation)
- [Module information](#module-information)
- [Usage examples](#usage-examples)
- [Module development](#module-development)
- [Troubleshooting](#troubleshooting)
- [Module API reference](ntools-scripts-module-api.md)

## Overview

The module consolidates build, CI, setup, testing, utility, and release functions from the previous script structure. Only the functions listed in `scripts/module-package/ntools-scripts.psd1` are exported.

Discover the exported commands at runtime:

```powershell
Import-Module "./scripts/module-package/ntools-scripts.psm1" -Force
Get-Command -Module ntools-scripts | Sort-Object Name
```

## Installation

Install the module manually for local development:

```powershell
Install-NToolsScriptsModule -InstallPath "$env:ProgramFiles\WindowsPowerShell\Modules\ntools-scripts" -Force
```


## Module Information

**Version**: 2.3.0  
**Location**: `scripts/module-package/ntools-scripts.psm1`  
**Installation**: Automatically installed via MSBuild targets and GitHub Actions

## Architecture

The module consolidates functionality from the previous script structure. The manifest at `scripts/module-package/ntools-scripts.psd1` controls the public API; see the [module API reference](ntools-scripts-module-api.md) for the exported commands and examples.

## Usage Examples
Import the module before calling its exported functions:

```powershell
Import-Module "./scripts/module-package/ntools-scripts.psm1" -Force
```

### Get Module Information
```powershell
# Get version
Get-NtoolsScriptsVersion

# List all available functions
Get-Command -Module ntools-scripts | Select-Object Name | Format-Table -AutoSize
```

### Install NTools with Custom Configuration
```powershell
# Install using specific ntools.json file (PowerShell wrapper is deprecated)
# Prefer the cross-platform Python script for CI and cross-platform installs:
# python atools/install-ntools.py --version 1.32.0 --json dev-setup/ntools.json
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

# Test MSBuild delegation (also available via 'sdo smoke_test')
Test-MSBuildDelegation

# Or use the comprehensive smoke test target
# sdo smoke_test  # (from command line - includes both artifact validation AND target delegation)
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

MSBuild / sdo CLI example:
```bash
# Run the MSBuild target from the repo root (sdo delegates to MSBuild)
sdo verify_artifacts /p:ArtifactsFolder="C:\Artifacts\MySolution\Release\1.2.3" /p:ProductVersion="1.2.3"
```

### MSBuild Integration
- **Usage**: `PUBLISH` target uses `Publish-AllProjects` function with deterministic repository path
- **Smoke Testing**: `SMOKE_TEST` target uses `Test-TargetDelegation` function for build system validation
- **Location**: Module installed to `$env:ProgramFiles\nbuild\modules\ntools-scripts\`

### SMOKE_TEST Target Integration
The comprehensive `SMOKE_TEST` target combines artifact validation with PowerShell module functions:

```bash
# Comprehensive smoke test (recommended)
sdo smoke_test
```

This target performs:
1. **Artifact Validation**: Tests the packaged `sdo.exe` executable
2. **Build System Validation**: Uses `Test-TargetDelegation` function to verify MSBuild target relationships
3. **Consolidated Results**: Single pass/fail result for all validation checks

**Migration Note**: The deprecated `TEST_TARGET_DELEGATION` target functionality is now integrated into `SMOKE_TEST`.

### GitHub Actions Integration
```yaml
- name: Install ntools using ntools-scripts module
  run: |
    Import-Module "./scripts/module-package/ntools-scripts.psm1" -Force
  python atools/install-ntools.py --version 1.32.0 --json dev-setup/ntools.json --downloads-dir ${{ runner.temp }} --dry-run
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
