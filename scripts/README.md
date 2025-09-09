# NTools PowerShell Scripts

This directory contains PowerShell scripts for the NTools project, organized by purpose for better maintainability and reuse.

## Directory Structure

- **`build/`** - Build and publishing related scripts
- **`test/`** - Testing and validation scripts  
- **`devops/`** - DevOps automation and deployment scripts
- **`setup/`** - Installation and environment setup scripts
- **`module-package/`** - **NTools.Scripts PowerShell Module** - Consolidated module containing all reusable functions

## NTools.Scripts Module (Recommended)

The **`module-package/`** folder contains the consolidated NTools.Scripts PowerShell module (v2.0.0) that includes functions from all categories:

### Available Functions:
- **Build Functions**: `Publish-AllProjects`
- **DevOps Functions**: `Get-VersionFromJson`, `Update-MarkdownTable`
- **Testing Functions**: `Write-TestResult`, `Test-TargetExists`, `Test-TargetDependencies`, `Test-TargetDelegation`
- **Utility Functions**: `Get-FileHash256`, `Get-FileVersionInfo`, `Invoke-FastForward`
- **Module Functions**: `Get-NtoolsScriptsVersion`

### Usage:
```powershell
# Install the module
.\scripts\module-package\install-module.ps1

# Import and use
Import-Module 'C:\Program Files\nbuild\modules\NTools.Scripts\NTools.Scripts.psd1' -Force
Publish-AllProjects -OutputDir ".\artifacts" -Version "1.0.0"
```

### MSBuild Integration:
- `INSTALL_NTOOLS_SCRIPTS` - Install the module
- `TEST_NTOOLS_SCRIPTS` - Test module installation
- `UNINSTALL_NTOOLS_SCRIPTS` - Remove the module

## Standalone Scripts

Individual scripts are maintained for specific use cases:

### Build Scripts
- `build-verify-artifacts.ps1` - Comprehensive artifact verification

### DevOps Scripts  
- `devops-get-ip.ps1` - Get public IP for Azure DevOps agents
- `devops-precommit-hooks.ps1` - Install/uninstall git pre-commit hooks
- `devops-waf-add-rule.ps1` - Add Azure WAF rules
- `devops-waf-delete-rule.ps1` - Remove Azure WAF rules

### Setup Scripts
- `setup-environment.ps1` - Development environment setup
- `setup-install-apps.ps1` - Install required applications
- `setup-install-ntools.ps1` - Install ntools from releases
- `setup-signing.ps1` - Configure code signing
- `setup-signing-trust.ps1` - Trust code signing certificates

### Test Scripts
- `test-coverage.ps1` - Code coverage analysis
- `test-delegation.ps1` - Quick MSBuild target delegation test
- `test-target-quick.ps1` - Quick target validation

## Naming Conventions

- `build-*.ps1` - Build-related scripts
- `test-*.ps1` - Test-related scripts
- `setup-*.ps1` - Setup and installation scripts
- `devops-*.ps1` - DevOps automation scripts

## Migration Notes

The following functionality has been **consolidated into the NTools.Scripts module**:
- All functions from `scripts/modules/` (Common.psm1, Build.psm1, etc.) - **REMOVED**
- Build publishing: `build-publish-all.ps1` → `Publish-AllProjects` function - **REMOVED**
- Utility scripts: `util-*.ps1` → Utility functions in module - **REMOVED**
- Target delegation testing: `test-target-delegation.ps1` → `Test-Target*` functions - **REMOVED**
- Version management: `devops-update-versions.ps1` → `Get-VersionFromJson`, `Update-MarkdownTable` - **REMOVED**
- `*.psm1` - PowerShell modules

## Script Descriptions

### Build Scripts
- `build-publish-all.ps1` - Publishes all .NET projects to a single output directory
- `build-verify-artifacts.ps1` - Validates build artifacts after compilation

### Test Scripts  
- `test-delegation.ps1` - Quick MSBuild target delegation validation
- `test-target-delegation.ps1` - Comprehensive target delegation testing
- `test-target-quick.ps1` - Quick target validation utility
- `test-coverage.ps1` - Runs tests with code coverage collection

### DevOps Scripts
- `devops-get-ip.ps1` - Retrieves public IP address for pipeline usage
- `devops-update-versions.ps1` - Synchronizes versions between JSON files and documentation
- `devops-precommit-hooks.ps1` - Sets up Git pre-commit hooks
- `devops-waf-rules.ps1` - Manages WAF front door rules

### Setup Scripts
- `setup-install-ntools.ps1` - Installs NTools from GitHub releases
- `setup-install-apps.ps1` - Installs development applications from JSON configurations
- `setup-environment.ps1` - Sets up development environment variables
- `setup-signing.ps1` - Configures code signing certificates

### Utility Scripts
- `util-calc-hash.ps1` - Calculates file hashes
- `util-file-operations.ps1` - Common file operations
- `util-fast-forward.ps1` - Git fast-forward utility

### Modules
- `Common.psm1` - Common utility functions used across scripts
- `Install.psm1` - Installation and setup functions
- `Build.psm1` - Build and publishing functions
- `Testing.psm1` - Testing and validation functions

## Usage Examples

```powershell
# Build all projects
.\scripts\build\build-publish-all.ps1 -PublishDir "C:\output" -ProductVersion "1.0.0"

# Run quick target test
.\scripts\test\test-target-quick.ps1

# Install NTools
.\scripts\setup\setup-install-ntools.ps1 -Version "v1.2.3"

# Get public IP for DevOps pipeline
.\scripts\devops\devops-get-ip.ps1
```

## Module Usage

```powershell
# Import common functions
Import-Module .\scripts\modules\Common.psm1

# Import installation functions  
Import-Module .\scripts\modules\Install.psm1
```

## Migration Notes

This consolidation reorganizes scripts from their previous locations:
- Root directory scripts moved to appropriate subdirectories
- `dev-setup/` scripts moved to `setup/` and other appropriate directories
- `pwsh/` scripts moved to `test/`
- `devops-scripts/` content moved to `devops/`
- Common functionality extracted into reusable modules