# NBuild Tasks Integration Guide

This document explains how the new MSBuild tasks have been integrated into the ntools project for automating documentation version updates setup.

## New MSBuild Tasks Added

### 1. UpdateVersionsInDocs Task

**Location**: `NbuildTasks/UpdateVersionsInDocs.cs`  
**Purpose**: Automatically synchronizes version information from JSON configuration files to the documentation table.

#### Features:
- Reads all JSON files in the `dev-setup/` directory
- Extracts version information from `NbuildAppList[0].Version`
- Updates corresponding entries in the `docs/ntools/ntools.md` table
- Handles tool name mapping between JSON and documentation names
- Updates "Last Checked on" dates automatically
- Provides detailed logging during execution

#### Parameters:
- `DevSetupPath` (Required): Path to the directory containing JSON configuration files
- `DocsPath` (Required): Path to the markdown documentation file to update

Two new targets have been added to `nbuild.targets`:

#### UPDATE_DOC_VERSIONS Target

```xml
<Target Name="UPDATE_DOC_VERSIONS">
    <UpdateVersionsInDocs 
        DevSetupPath="$(SolutionDir)\dev-setup" 
        DocsPath="$(SolutionDir)\docs\ntools\ntools.md" />
    <Message Text="Documentation versions updated successfully." />
    <Message Text="==> DONE"/>
</Target>
```

## Tool Name Mapping

The `UpdateVersionsInDocs` task includes intelligent name mapping between JSON configuration names and documentation display names:

| Documentation Name | JSON Configuration Name |
|-------------------|------------------------|
| Node.js | Node.js |
| PowerShell | Powershell |
| Git for Windows | Git for Windows |
| Visual Studio Code | Visual Studio Code |
| NuGet | Nuget |
| Terraform Lint | terraform lint |
| kubernetes | kubectl |
| Azure CLI | AzureCLI |
| MongoDB Community Server | MongoDB |
| (and others...) | |

## Benefits

### 1. **Build Integration**
- Native MSBuild tasks run as part of your build process
- Leverages existing build infrastructure
- Provides consistent execution environment

### 2. **Performance**
- Compiled C# code executes faster than scripts
- Integrated with MSBuild's dependency tracking
- Efficient JSON parsing with System.Text.Json

### 3. **Error Handling**
- Comprehensive error reporting through MSBuild logging
- Graceful handling of missing files or invalid JSON
- Clear success/failure indicators

### 4. **Maintainability**
- Centralized logic in the NbuildTasks project
- Follows established MSBuild patterns
- Easy to extend with additional functionality

## Troubleshooting

### Common Issues

1. **Assembly not found errors**:
   - Ensure the solution is built in Release mode first
   - Verify NbuildTasks.dll exists in the Release folder

2. **Path resolution issues**:
   - Check that SolutionDir property is correctly set
   - Verify relative paths resolve correctly from build context

3. **JSON parsing errors**:
   - Ensure all JSON files in dev-setup are valid
   - Check that JSON files follow the expected NbuildAppList structure

### Debug Information

Enable detailed logging by adding to your build command:
```bash
dotnet build nbuild.targets -target:UPDATE_DOC_VERSIONS -verbosity:detailed
```

This will show:
- Which JSON files are being processed
- Which tool versions are found
- Which documentation entries are updated
- Any warnings or errors encountered

## Future Enhancements

Potential improvements for the tasks:
- **Validation**: Verify that all tools in documentation have corresponding JSON files
- **Reporting**: Generate summary reports of version changes
- **Configuration**: Support for custom mapping files
- **Integration**: Automatic changelog generation based on version changes

---

*These MSBuild tasks provide a robust, integrated solution for maintaining documentation consistency within the ntools build system.*
