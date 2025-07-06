# Documentation Version Automation Guide

This document outlines the automation solutions implemented to keep the `docs/ntools/ntools.md` file synchronized with version information from JSON configuration files in the `dev-setup/` directory.

## Problem Statement

The ntools project maintains tool version information in two places:
1. **JSON Configuration Files** (`dev-setup/*.json`) - Used for automated installation
2. **Documentation Table** (`docs/ntools/ntools.md`) - User-facing version reference

Previously, these had to be updated manually, leading to inconsistencies and outdated documentation.

## Solutions Implemented

We've implemented **4 complementary automation approaches** that can be used individually or together:

---

## 1. 🔧 PowerShell Script Automation

### What Was Added
- **File**: `dev-setup/update-versions.ps1`
- **Purpose**: Standalone script to sync versions between JSON files and documentation
- **Execution**: Manual or triggered by other automation

### Features
- Reads all JSON files in `dev-setup/` directory
- Extracts `Name` and `Version` from `NbuildAppList[0]`
- Updates corresponding entries in `ntools.md` table
- Handles tool name mapping (e.g., "PowerShell" ↔ "Powershell")
- Updates "Last Checked on" dates automatically
- Provides colored console output for better UX

### Usage
```powershell
# Basic usage
cd dev-setup
.\update-versions.ps1

# With custom paths
.\update-versions.ps1 -DevSetupPath "C:\source\ntools\dev-setup" -DocsPath "C:\source\ntools\docs\ntools\ntools.md"
```

### Benefits
- ✅ **Immediate execution** - Run anytime manually
- ✅ **Portable** - Works on any Windows machine with PowerShell
- ✅ **Standalone** - No dependencies on build system
- ✅ **Flexible** - Can be called from other scripts

---

## 2. 🏗️ NBuild Task Integration

### What Was Added
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

### Benefits
- ✅ **Build integration** - Automatic during build process
- ✅ **Native performance** - C# execution speed
- ✅ **MSBuild compatibility** - Works with existing build system
- ✅ **CI/CD ready** - Integrates with automated builds

---


## Implementation Strategy

### Recommended Approach
1. **Start with PowerShell script** - Immediate utility and testing
2. **Integrate with NBuild** - Automate in build process
3. **Enable GitHub Actions** - Complete automation for team

### Complementary Usage
- **PowerShell Script**: Manual updates and testing
- **NBuild Task**: Build-time verification
- **GitHub Actions**: Team collaboration and CI/CD

## Tool Name Mapping

The automation handles differences between JSON configuration names and documentation display names:

| Documentation Name | JSON Configuration Name |
|-------------------|------------------------|
| Node.js | Node.js |
| PowerShell | Powershell |
| Python | Python |
| Git for Windows | Git for Windows |
| Visual Studio Code | Visual Studio Code |
| NuGet | Nuget |
| Terraform | Terraform |
| Terraform Lint | terraform lint |
| kubernetes | kubectl |
| minikube | minikube |
| Azure CLI | AzureCLI |
| MongoDB Community Server | MongoDB |
| pnpm | pnpm |
| Ntools | Ntools |

## Benefits Summary

### Before Automation
- ❌ Manual synchronization required
- ❌ Prone to human error
- ❌ Inconsistent version information
- ❌ Time-consuming maintenance
- ❌ Often forgotten during updates

### After Automation
- ✅ **Consistency**: Documentation always matches configuration
- ✅ **Accuracy**: Eliminates manual transcription errors
- ✅ **Efficiency**: Saves developer time
- ✅ **Reliability**: Automated processes don't forget
- ✅ **Scalability**: Handles growing number of tools easily
- ✅ **Auditability**: Git history tracks all changes
- ✅ **Team Collaboration**: Works for all team members
- ✅ **CI/CD Integration**: Fits into automated pipelines

## Maintenance

### Regular Tasks
- Monitor automation execution logs
- Update tool name mappings when new tools are added
- Test automation after major Git or build system changes

### Troubleshooting
```powershell
# Test PowerShell script manually
cd dev-setup
.\update-versions.ps1 -Verbose


# Check GitHub Actions logs
# Visit: https://github.com/your-repo/actions

```

## Future Enhancements

Potential improvements to consider:
- **Semantic version validation**: Ensure versions follow semver
- **Changelog generation**: Auto-update changelogs with version changes
- **Notification system**: Alert team of version updates
- **Dependency checking**: Verify tool compatibility
- **Release automation**: Tag releases when versions change

---

*This automation system ensures that the ntools documentation remains accurate and up-to-date with minimal manual intervention, improving both developer productivity and end-user experience.*
