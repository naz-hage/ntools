# NTools PowerShell Scripts

This directory contains all PowerShell scripts for the NTools project, organized by purpose for better maintainability and reuse.

## Directory Structure

- **`build/`** - Build and publishing related scripts
- **`test/`** - Testing and validation scripts  
- **`devops/`** - DevOps automation and deployment scripts
- **`setup/`** - Installation and environment setup scripts
- **`utils/`** - General utility scripts
- **`modules/`** - Reusable PowerShell modules (.psm1)

## Naming Conventions

- `build-*.ps1` - Build-related scripts
- `test-*.ps1` - Test-related scripts
- `setup-*.ps1` - Setup and installation scripts
- `devops-*.ps1` - DevOps automation scripts
- `util-*.ps1` - Utility scripts
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