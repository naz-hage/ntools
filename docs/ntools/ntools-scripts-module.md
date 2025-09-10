# ntools-scripts PowerShell Module

This page documents the `ntools-scripts` PowerShell module and serves as the canonical API reference for the module's exported functions and usage.

**Location**: `scripts/module-package/ntools-scripts.psm1`

## Overview
Module files:
```
scripts/
├─ module-package/
│  ├─ ntools-scripts.psm1     # Main module with all functions
│  ├─ ntools-scripts.psd1     # Module manifest
│  └─ install-module.ps1      # Installation helper
├─ devops/
└─ setup/
```
Refer to the "Canonical module API" section below for the authoritative list of exported functions and descriptions.
### Import the Module
```
# Import from local development
Import-Module "./scripts/module-package/ntools-scripts.psm1" -Force

# Import from installed location (after installation)
Import-Module "$env:ProgramFiles\nbuild\modules\ntools-scripts\ntools-scripts.psm1" -Force
```
### Integration with Build System
-- **Installation**: `nbuild.targets` automatically installs the module during `INSTALL_NTOOLS_SCRIPTS` target
-- **Usage**: `PUBLISH` target uses `Publish-AllProjects` function with deterministic repository path
### GitHub Actions Integration
- name: Install ntools using ntools-scripts module
  run: |
    Import-Module "./scripts/module-package/ntools-scripts.psm1" -Force
    Install-NTools -NtoolsJsonPath "./dev-setup/ntools.json"
# Migration from Legacy Scripts
# New way
run: |
  Import-Module "./scripts/module-package/ntools-scripts.psm1" -Force
  Install-NTools -NtoolsJsonPath "./dev-setup/ntools.json"
<!-- New way (automatically used) -->
<Exec Command='pwsh -Command "Import-Module ntools-scripts; Publish-AllProjects -RepositoryRoot $(SolutionDir)"' />
### Module Development

### Adding New Functions
1. Add function to appropriate section in `ntools-scripts.psm1`
2. Add function name to `Export-ModuleMember` line
3. Update `FunctionsToExport` in `ntools-scripts.psd1`
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
## Troubleshooting

### Module Not Found
```powershell
# Verify module location
Test-Path "./scripts/module-package/ntools-scripts.psm1"

# Check if installed
Test-Path "$env:ProgramFiles\nbuild\modules\ntools-scripts\ntools-scripts.psm1"
```
### Function Not Available
```powershell
# List all available functions
Get-Command -Module ntools-scripts

# Check if module is properly imported
Get-Module ntools-scripts
```
# ntools-scripts PowerShell Module

The ntools-scripts module is a comprehensive PowerShell module that consolidates all NTools PowerShell functionality into a single, reusable module. This module replaces the previous collection of individual scripts with a structured, function-based approach.

## Overview

**Version**: 2.3.0  
**Location**: `scripts/module-package/ntools-scripts.psm1`  
**Installation**: Automatically installed via MSBuild targets and GitHub Actions

## Architecture

The module consolidates functionality from the previous script structure:

### Before (Individual Scripts)
```
scripts/
├── build/build-verify-artifacts.ps1   # deprecated - functionality moved to Invoke-VerifyArtifacts
├── devops/ (legacy scripts migrated into module)

Use the `Set-DevelopmentEnvironment` function in the `ntools-scripts` module instead. It provides the same behavior (sets user `devDrive` and `mainDir` environment variables) and is callable directly from PowerShell or via MSBuild using the `SETUP_ENVIRONMENT` target in `nbuild.targets`.

Example (PowerShell):

```powershell
Import-Module "./scripts/module-package/ntools-scripts.psm1" -Force
Set-DevelopmentEnvironment -DevDrive 'D:' -MainDir 'source'
```

MSBuild (from repo root):

```powershell
msbuild /t:SETUP_ENVIRONMENT /p:DevDrive=D: /p:MainDir=source
```
├── test/  # Legacy test scripts were consolidated into the `ntools-scripts` module
└── ... (legacy scripts removed; use module functions)
```

### After (Consolidated Module)
```
scripts/
├── module-package/
│   ├── ntools-scripts.psm1     # Main module with all functions
│   ├── ntools-scripts.psd1     # Module manifest
│   └── install-module.ps1      # Installation script
├── build/                      # Legacy scripts (deprecated)
├── devops/                     # Legacy scripts (deprecated)
├── setup/                      # Entry point scripts
└── test/                       # Legacy scripts (deprecated)
```

## Available Functions

The module exports 36 functions organized by category:

### Build Functions
-- `Publish-AllProjects` - Build and publish all non-test projects with deterministic repository path
-- `Invoke-VerifyArtifacts` - Comprehensive artifact verification
- `Get-ProjectFiles` - Get project files with filtering
- `Invoke-ProjectPublish` - Publish individual projects

### DevOps Functions  
- `Get-AgentPublicIp` - Get public IP for Azure DevOps agents and set pipeline variable
- `Set-PreCommitHooks` - Install/uninstall git pre-commit hooks
- `Add-WafAllowRule` - Add Azure WAF allow rule for an IP
- `Remove-WafCustomRule` - Remove Azure WAF custom rule
- `Get-VersionFromJson` - Extract version information from JSON files
- `Update-MarkdownTable` - Update version tables in markdown documentation

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

The following section is the canonical, authoritative list of functions exported by the
`ntools-scripts` module. Documentation elsewhere should reference this page for the
module API rather than duplicating the function list.

Exported functions (exactly as exported by `Export-ModuleMember` in `ntools-scripts.psm1`):

- `Get-ntoolsScriptsVersion`
- `Publish-AllProjects`
- `Get-VersionFromJson`
- `Update-MarkdownTable`
- `Write-TestResult`
- `Test-TargetExists`
- `Test-TargetDependencies`
- `Test-TargetDelegation`
- `Get-FileHash256`
- `Get-FileVersionInfo`
- `Invoke-FastForward`
- `Write-OutputMessage`
- `Get-NToolsFileVersion`
- `Add-DeploymentPathToEnvironment`
- `Invoke-NToolsDownload`
- `Install-NTools`
- `Invoke-VerifyArtifacts`
- `Set-DevelopmentEnvironment`
- `Test-IsAdministrator`
- `Test-MicrosoftPowerShellSecurityModuleLoaded`
- `Test-CertificateStore`
- `New-SelfSignedCodeCertificate`
- `Export-CertificateToPfx`
- `Export-CertificateToCer`
- `Import-CertificateToRoot`
- `Import-CertificateToCurrentUser`
- `Set-ScriptSignature`
- `Get-ScriptSignature`
- `Set-CodeSigningTrust`

Notes:
- This page is the single authoritative reference for the `ntools-scripts` module API.
- If other docs (tutorials, README files, or CI snippets) need to show usage examples,
  they should link to this page and include only short examples — keep API surface here.


## Usage Examples

### Import the Module
```powershell
# Import from local development
Import-Module "./scripts/module-package/ntools-scripts.psm1" -Force

# Import from installed location (after installation)
Import-Module "$env:ProgramFiles\nbuild\modules\ntools-scripts\ntools-scripts.psm1" -Force
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
- **Installation**: `nbuild.targets` automatically installs the module during `INSTALL_NTOOLS_SCRIPTS` target
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
- **Installation**: `nbuild.targets` automatically installs the module during `INSTALL_NTOOLS_SCRIPTS` target
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

## Key Improvements

### 1. Deterministic Repository Path Detection
The `Publish-AllProjects` function now requires an explicit `RepositoryRoot` parameter, eliminating unreliable heuristic detection:

```powershell
# Before (unreliable)
Publish-AllProjects -OutputDir $output -Version $version

# After (deterministic)
Publish-AllProjects -OutputDir $output -Version $version -RepositoryRoot $repoRoot
```

### 2. Configurable ntools.json Path
The `Install-NTools` function accepts a custom path to ntools.json:

```powershell
# Use specific configuration file
Install-NTools -NtoolsJsonPath "./dev-setup/ntools.json"
```

### 3. Unified Error Handling
All functions use consistent error handling and logging:

```powershell
Write-Info "Starting operation..."
Write-Success "Operation completed successfully"
Write-Warning "Potential issue detected"
Write-Error "Operation failed"
```

### 4. Backward Compatibility
- Legacy script entry points in `scripts/setup/` still work
- Module functions can be called directly
- All original functionality preserved

## Migration from Legacy Scripts

### For Developers
```powershell
# Old way
./scripts/build/build-verify-artifacts.ps1  # deprecated - use Invoke-VerifyArtifacts

# New way (module-based)
Import-Module "./scripts/module-package/ntools-scripts.psm1" -Force
Invoke-VerifyArtifacts -ArtifactsPath "C:\Artifacts\MySolution\Release\1.2.3" -ProductVersion "1.2.3"
```

### For CI/CD
```yaml
run: |
  Import-Module "./scripts/module-package/ntools-scripts.psm1" -Force
  Install-NTools -NtoolsJsonPath "./dev-setup/ntools.json"
```
### For MSBuild
```xml
<!-- Old way -->
<Exec Command='pwsh -File "$(SolutionDir)\scripts\build\publish-all-projects.ps1"' />

<!-- New way (automatically used) -->
<Exec Command='pwsh -Command "Import-Module ntools-scripts; Publish-AllProjects -RepositoryRoot $(SolutionDir)"' />
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
