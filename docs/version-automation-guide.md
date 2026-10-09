# Version Automation Guide

This document outlines the automation solutions implemented to keep the SDO developer tools documentation synchronized with version information from YAML configuration files in the `dev-setup/` directory.

The SDO project maintains tool version information in two places:
1. **YAML Configuration Files** (`dev-setup/*.yaml`) - Used for automated installation
2. **Documentation Table** (`docs/sdo.md`) - User-facing version reference

Previously, these had to be updated manually, leading to inconsistencies and outdated documentation.

## Solutions Implemented

---

## 1. PowerShell Module Integration (v3.0.0+)

- **Module**: `scripts/module-package/sdo-scripts.psm1`
- **Function**: `Get-VersionFromYaml`
- **Purpose**: Consolidated version management within the SDO scripts module
- **Integration**: Available in all build processes and CI/CD pipelines

### Using the Module Approach
```powershell
# Import the module
Import-Module "./scripts/module-package/sdo-scripts.psm1" -Force

# Update documentation with latest versions
# (Handled by MSBuild target)

# Get version from the SDO YAML manifest
$version = Get-VersionFromYaml -YamlPath "./dev-setup/sdo.yaml"
```

---

Tool versions in documentation are updated using the MSBuild task (`UpdateVersionsInDocs`) via the `sdo update_doc_versions` command. This extracts all tool/version pairs from the `NbuildAppList` entries in `dev-setup/sdo.yaml` and `dev-setup/apps.yaml`, then updates the documentation table accordingly. See the documentation in `sdo.md` for details.

### NBuild Task Integration

- **File**: `NbuildTasks/UpdateVersionsInDocs.cs`
- **Purpose**: MSBuild task for build-time automation
- **Execution**: Integrated into your existing NBuild workflow

### Features
- Native C# MSBuild task implementation
- YAML parsing using `YamlDotNet`
- Regex-based markdown table updating
- Comprehensive tool name mapping logic
- Build-time logging and error handling
- Can be part of CI/CD pipeline

### Usage
```xml
<Target Name="UpdateDocVersions">
  <UpdateVersionsInDocs 
    DevSetupPath="$(MSBuildProjectDirectory)\dev-setup" 
    DocsPath="$(MSBuildProjectDirectory)\docs\sdo.md" />
</Target>
```
