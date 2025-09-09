# NTools.Scripts PowerShell Module

The NTools.Scripts module is a comprehensive PowerShell module that consolidates all NTools PowerShell functionality into a single, reusable module. This module replaces the previous collection of individual scripts with a structured, function-based approach.

## Overview

**Version**: 2.3.0  
**Location**: `scripts/module-package/NTools.Scripts.psm1`  
**Installation**: Automatically installed via MSBuild targets and GitHub Actions

## Architecture

The module consolidates functionality from the previous script structure:

### Before (Individual Scripts)
```
scripts/
├── build/build-verify-artifacts.ps1
├── devops/devops-get-ip.ps1
├── devops/devops-precommit-hooks.ps1
├── setup/setup-environment.ps1
├── setup/setup-install-apps.ps1
├── test/test-coverage.ps1
└── ... (20+ individual scripts)
```

### After (Consolidated Module)
```
scripts/
├── module-package/
│   ├── NTools.Scripts.psm1     # Main module with all functions
│   ├── NTools.Scripts.psd1     # Module manifest
│   └── install-module.ps1      # Installation script
├── build/                      # Legacy scripts (deprecated)
├── devops/                     # Legacy scripts (deprecated)
├── setup/                      # Entry point scripts
└── test/                       # Legacy scripts (deprecated)
```

## Available Functions

The module exports 36 functions organized by category:

### Build Functions
- `Publish-AllProjects` - Build and publish all non-test projects with deterministic repository path
- `Invoke-ArtifactVerification` - Comprehensive artifact verification
- `Get-ProjectFiles` - Get project files with filtering
- `Invoke-ProjectPublish` - Publish individual projects

### DevOps Functions  
- `Get-AgentIPAddress` - Get public IP for Azure DevOps agents
- `Install-PreCommitHooks` - Install git pre-commit hooks
- `Add-WAFRule` - Add Azure WAF rules
- `Remove-WAFRule` - Remove Azure WAF rules
- `Get-VersionFromJson` - Extract version information from JSON files
- `Update-MarkdownTable` - Update version tables in markdown documentation

### Setup Functions
- `Set-DevelopmentEnvironment` - Set up development environment
- `Install-DevelopmentApps` - Install development applications
- `Set-CodeSigningTrust` - Configure code signing trust
- `Set-CodeSigning` - Configure code signing
- `Install-NTools` - Install NTools from releases (with configurable ntools.json path)
- `Install-NToolsScriptsModule` - Install this module

### Test Functions
- `Invoke-CodeCoverage` - Run tests with code coverage
- `Test-MSBuildDelegation` - Test MSBuild target delegation
- `Test-QuickTargets` - Quick target validation
- `Test-NToolsScriptsModule` - Test module functionality
- `Write-TestResult` - Write formatted test results
- `Test-TargetExists` - Check if MSBuild targets exist
- `Test-TargetDependencies` - Validate target dependencies
- `Test-TargetDelegation` - Test target delegation patterns

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

## Usage Examples

### Import the Module
```powershell
# Import from local development
Import-Module "./scripts/module-package/NTools.Scripts.psm1" -Force

# Import from installed location (after installation)
Import-Module "$env:ProgramFiles\nbuild\modules\NTools.Scripts\NTools.Scripts.psm1" -Force
```

### Get Module Information
```powershell
# Get version
Get-NtoolsScriptsVersion

# List all available functions
Get-Command -Module NTools.Scripts | Select-Object Name | Format-Table -AutoSize
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

# Test MSBuild delegation
Test-MSBuildDelegation
```

## Integration with Build System

The module is automatically integrated with the NTools build system:

### MSBuild Integration
- **Installation**: `nbuild.targets` automatically installs the module during `INSTALL_NTOOLS_SCRIPTS` target
- **Usage**: `PUBLISH` target uses `Publish-AllProjects` function with deterministic repository path
- **Location**: Module installed to `$env:ProgramFiles\nbuild\modules\NTools.Scripts\`

### GitHub Actions Integration
```yaml
- name: Install ntools using NTools.Scripts module
  run: |
    Import-Module "./scripts/module-package/NTools.Scripts.psm1" -Force
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
./scripts/build/build-verify-artifacts.ps1

# New way
Import-Module "./scripts/module-package/NTools.Scripts.psm1" -Force
Invoke-ArtifactVerification
```

### For CI/CD
```yaml
# Old way
run: ./scripts/setup/setup-install-ntools.ps1

# New way  
run: |
  Import-Module "./scripts/module-package/NTools.Scripts.psm1" -Force
  Install-NTools -NtoolsJsonPath "./dev-setup/ntools.json"
```

### For MSBuild
```xml
<!-- Old way -->
<Exec Command='pwsh -File "$(SolutionDir)\scripts\build\publish-all-projects.ps1"' />

<!-- New way (automatically used) -->
<Exec Command='pwsh -Command "Import-Module NTools.Scripts; Publish-AllProjects -RepositoryRoot $(SolutionDir)"' />
```

## Module Development

### Adding New Functions
1. Add function to appropriate section in `NTools.Scripts.psm1`
2. Add function name to `Export-ModuleMember` line
3. Update `FunctionsToExport` in `NTools.Scripts.psd1`
4. Increment module version
5. Update documentation

### Testing
```powershell
# Test the module
Import-Module "./scripts/module-package/NTools.Scripts.psm1" -Force
Test-NToolsScriptsModule
```

### Installation
```powershell
# Install module manually for development
Install-NToolsScriptsModule -InstallPath "$env:ProgramFiles\WindowsPowerShell\Modules\NTools.Scripts" -Force
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
Test-Path "./scripts/module-package/NTools.Scripts.psm1"

# Check if installed
Test-Path "$env:ProgramFiles\nbuild\modules\NTools.Scripts\NTools.Scripts.psm1"
```

### Version Issues
```powershell
# Check module version
Get-NtoolsScriptsVersion

# Check installed version vs source
Import-Module "./scripts/module-package/NTools.Scripts.psm1" -Force
Get-NtoolsScriptsVersion
```

### Function Not Available
```powershell
# List all available functions
Get-Command -Module NTools.Scripts

# Check if module is properly imported
Get-Module NTools.Scripts
```
