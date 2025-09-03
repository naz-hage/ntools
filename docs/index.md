## Software Tools Collection

This repository contains a collection of software tools specifically designed to automate various build and test tasks on Windows clients. Whether you are a developer working on your local machine or using GitHub Actions for continuous integration, these tools will simplify your workflow and enhance your productivity.

The latest version includes enhanced MSBuild target system with comprehensive code coverage support, user-level PATH management, and robust artifact verification capabilities.

---
- Getting started
    - [Install ntools](installation.md) - Enhanced with user-level installation
    - [Practice using ntools](usage.md) - Updated with new features
    - [Setup your project](setup.md)
- Build System Features
    - [Code Coverage](ntools/code-coverage.md) - NEW: Comprehensive coverage reporting
    - [MSBuild Targets](ntools/nbuild-targets.md) - Enhanced target system
- List of tools
    - [Nbuild (nb.exe)](ntools/nbuild.md) - Enhanced build system
    - [File and Folder Listing Utility (lf.exe)](ntools/lf.md)
    - [Nbackup (nbackup.exe)](ntools/nbackup.md)
    - [Azure DevOps Work Item CLI Utility (wi.exe)](ntools/wi.md)
    - [Github Release](ntools/github-release.md)

## Recent Enhancements

### Build System Improvements
- **Code Coverage**: Comprehensive coverage reporting with ReportGenerator
- **User-Level Installation**: No administrator privileges required
- **Artifact Verification**: Automated smoke testing and verification
- **Enhanced Pipeline**: Improved build pipeline with better error handling

### Security & Usability
- **User PATH Management**: Safer user-level environment modifications
- **Test Mode Protection**: Prevents environment contamination during testing
- **Backward Compatibility**: All existing projects continue to work unchanged

Don't hesitate to write an [issue](https://github.com/naz-hage/NTools/issues) if you have any questions or suggestions.