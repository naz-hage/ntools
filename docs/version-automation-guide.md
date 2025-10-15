# Version Automation Guide

This document outlines the automation solutions implemented to keep the `docs/ntools/ntools.md` file synchronized with version information from JSON configuration files in the `dev-setup/` directory.

The ntools project maintains tool version information in two places:
1. **JSON Configuration Files** (`dev-setup/*.json`) - Used for automated installation
2. **Documentation Table** (`docs/ntools/ntools.md`) - User-facing version reference

Previously, these had to be updated manually, leading to inconsistencies and outdated documentation.

## Solutions Implemented

---

## 1. PowerShell Module Integration (v2.3.0+)

- **Module**: `scripts/module-package/ntools-scripts.psm1`
- **Function**: `Get-VersionFromJson`
- **Purpose**: Consolidated version management within the ntools-scripts module
- **Integration**: Available in all build processes and CI/CD pipelines

### Using the Module Approach
```powershell
# Import the module
Import-Module "./scripts/module-package/ntools-scripts.psm1" -Force

# Update documentation with latest versions
# (Handled by MSBuild target)

# Get version from specific JSON file  
$version = Get-VersionFromJson -JsonFilePath "./dev-setup/ntools.json"
```

---

Tool versions in documentation are updated using the MSBuild task (`UpdateVersionsInDocs`) via the `nb update_doc_versions` command. This extracts all tool/version pairs from every `NbuildAppList` entry in every `*.json` file in `dev-setup` and updates the documentation table accordingly. See the documentation in `ntools.md` for details.

### NBuild Task Integration

- **File**: `NbuildTasks/UpdateVersionsInDocs.cs`
- **Purpose**: MSBuild task for build-time automation
- **Execution**: Integrated into your existing NBuild workflow

### Features
- Native C# MSBuild task implementation
- JSON parsing using `System.Text.Json`
- Regex-based markdown table updating
- Comprehensive tool name mapping logic
- Build-time logging and error handling
- Can be part of CI/CD pipeline

### Usage
```xml
<Target Name="UpdateDocVersions">
  <UpdateVersionsInDocs 
    DevSetupPath="$(MSBuildProjectDirectory)\dev-setup" 
    DocsPath="$(MSBuildProjectDirectory)\docs\ntools\ntools.md" />
</Target>
```
