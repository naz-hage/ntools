# Version Automation Guide

This document outlines the automation solutions implemented to keep the `docs/ntools/ntools.md` file synchronized with version information from JSON configuration files in the `dev-setup/` directory.

The ntools project maintains tool version information in two places:
1. **JSON Configuration Files** (`dev-setup/*.json`) - Used for automated installation
2. **Documentation Table** (`docs/ntools/ntools.md`) - User-facing version reference

Previously, these had to be updated manually, leading to inconsistencies and outdated documentation.

## Solutions Implemented

### 1. PowerShell Module Integration (v2.3.0+)

- **Module**: `scripts/module-package/ntools-scripts.psm1`
- **Function**: `Get-VersionFromJson`
- **Purpose**: Consolidated version management within the ntools-scripts module
- **Integration**: Available in all build processes and CI/CD pipelines

#### Using the PowerShell Module
```powershell
# Import the module
Import-Module "./scripts/module-package/ntools-scripts.psm1" -Force

# Update documentation with latest versions
# (Handled by MSBuild target)

# Get version from specific JSON file
$version = Get-VersionFromJson -JsonFilePath "./dev-setup/ntools.json"
```

### 2. MSBuild Task Integration

- **File**: `NbuildTasks/UpdateVersionsInDocs.cs`
- **Purpose**: MSBuild task for build-time automation
- **Execution**: Integrated into your existing NBuild workflow

#### Features
- Native C# MSBuild task implementation
- JSON parsing using `System.Text.Json`
- Regex-based markdown table updating
- Comprehensive tool name mapping logic
- Build-time logging and error handling
- Can be part of CI/CD pipeline

#### Usage
```xml
<Target Name="UpdateDocVersions">
  <UpdateVersionsInDocs 
    DevSetupPath="$(MSBuildProjectDirectory)\dev-setup" 
    DocsPath="$(MSBuildProjectDirectory)\docs\ntools\ntools.md" />
</Target>
```

## Benefits

- **Accuracy**: Eliminates manual version updates and associated errors
- **Consistency**: Ensures documentation always reflects current configuration
- **Automation**: Integrates with existing build and CI/CD processes
- **Maintainability**: Single source of truth for version information
- **Reliability**: Automated validation prevents version drift

## Configuration

### JSON Configuration Structure

Version information is stored in JSON files within the `dev-setup/` directory. Each tool is defined with an `NbuildAppList` entry:

```json
{
  "NbuildAppList": [
    {
      "Name": "ntools",
      "Version": "2.3.0",
      "Description": "NBuild Tools Suite"
    },
    {
      "Name": "nbuild", 
      "Version": "1.8.2",
      "Description": "NBuild Build System"
    }
  ]
}
```

**Key Points:**
- Each JSON file can contain multiple tools in the `NbuildAppList` array
- The `Name` field maps to the tool name in documentation tables
- The `Version` field contains the current version number
- The `Description` field provides additional context (optional)

## Troubleshooting

### Common Issues

1. **Versions not updating in documentation**
   - Verify JSON files exist in `dev-setup/` directory
   - Check that `NbuildAppList` entries have correct `Name` and `Version` fields
   - Ensure the markdown table in `docs/ntools/ntools.md` has the correct format

2. **MSBuild task fails**
   - Check build logs for specific error messages
   - Verify file paths are correct relative to the build file
   - Ensure the task assembly is properly referenced

3. **Command line tool not found**
   - Run `nb --help` to verify ntools is installed
   - Check that `nb update_doc_versions` is available
   - Verify PATH includes ntools installation directory

### Debugging Steps
```bash
# Check JSON file structure
Get-Content .\dev-setup\ntools.json | ConvertFrom-Json | Select-Object -ExpandProperty NbuildAppList

# Verify markdown table format
Select-String -Path .\docs\ntools\ntools.md -Pattern "\|.*\|.*\|"

# Test MSBuild task manually
msbuild /t:UpdateDocVersions your-project.proj
```
