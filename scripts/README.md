# NTools PowerShell Scripts

This directory contains the consolidated PowerShell module for the NTools project. All scripts have been converted to functions within the ntools-scripts module for better maintainability and reuse.

## Directory Structure

-- **`module-package/`** - **ntools-scripts PowerShell Module** - Consolidated module containing all functions
- **`setup/`** - Entry point scripts for installation (uses module functions internally)
- **`build/`**, **`devops/`**, **`test/`** - Legacy individual scripts (deprecated, use module functions instead)

## ntools-scripts Module (Primary Interface)

The **`module-package/`** folder contains the consolidated ntools-scripts PowerShell module (v2.3.0) that includes functions converted from all original script categories:

### Module API
The canonical API documentation for the `ntools-scripts` module lives in the docs site:

- `docs/ntools/ntools-scripts-module.md` — Canonical list of exported functions and descriptions.

Refer to that page for the authoritative function list and usage examples. Keep this README
focused on getting started and examples.

### Usage Examples:

```powershell
# Import the module
Import-Module "./scripts/module-package/ntools-scripts.psm1" -Force

# Get module information
Get-NtoolsScriptsVersion
Get-Command -Module ntools-scripts | Select-Object Name

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
-- name: Install ntools using ntools-scripts module
  run: |
  Import-Module "./scripts/module-package/ntools-scripts.psm1" -Force
    Install-NTools -NtoolsJsonPath "./dev-setup/ntools.json"
```

## Entry Point Scripts (setup/ folder)

The `setup/` folder previously contained thin wrapper entry point scripts. Installation functionality has been moved into the `ntools-scripts` module; call `Install-NTools` directly or use the module's installer targets.

Example:

```powershell
Import-Module "./scripts/module-package/ntools-scripts.psm1" -Force
Install-NTools -NtoolsJsonPath "./dev-setup/ntools.json"
```

## Legacy Scripts (Deprecated)

Individual scripts in `build/`, `devops/`, and `test/` folders are maintained for reference but are deprecated:

## Migration Path

```powershell
# Old way
./scripts/build/build-verify-artifacts.ps1

# New way (module-based)
Import-Module "./scripts/module-package/ntools-scripts.psm1" -Force
Invoke-VerifyArtifacts -ArtifactsPath "C:\Artifacts\MySolution\Release\1.2.3" -ProductVersion "1.2.3"

# Legacy devops scripts (removed)
# The per-script devops wrappers were removed during consolidation. Use the canonical
# `ntools-scripts` module instead. Example usage:

Import-Module "./scripts/module-package/ntools-scripts.psm1" -Force

# Get public IP for pipeline usage and set pipeline variable
Get-AgentPublicIp

# Add/remove Azure WAF rules (requires Azure CLI authentication)
Add-WafAllowRule -ResourceGroupName 'rg' -WafPolicyName 'policy' -CustomRuleName 'allow-agent' -AgentIp '1.2.3.4'
Remove-WafCustomRule -ResourceGroupName 'rg' -WafPolicyName 'policy' -CustomRuleName 'allow-agent'

# NOTE: Pre-commit integration and helpers were deprecated and removed from the
# canonical module and repository docs. Historical notes exist in the repository
# history if needed.
```
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

For complete module documentation, see [ntools-scripts Module Documentation](../docs/ntools/ntools-scripts-module.md).

## Best Practices

1. **Use the module functions instead of individual scripts**
2. **Always import with -Force during development** to reload changes
3. **Use explicit parameters** like `-RepositoryRoot` for deterministic behavior
4. **Check module version** with `Get-NtoolsScriptsVersion` for troubleshooting