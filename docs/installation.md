
To get started with `ntools`, you need to install the latest version of [64-bit Git for Windows](https://git-scm.com/download/win) and the [.NET SDK](https://dotnet.microsoft.com/download) on your machine, then follow these steps:

- Open a PowerShell in administrative mode.  Assume c:\source as directory `%MainDirectory%` which will be used through this document.
- Clone this repository to your local machine from the `%MainDirectory%` folder.
```powershell
cd c:\source
git clone https://github.com/naz-hage/ntools
```

## Installation Options

### Option 1: Full Development Environment Setup (Recommended for Contributors)

This installs the complete development environment including .NET runtime, NTools, and development tools:

```powershell
cd ./ntools
# Change PowerShell execution policy (one-time setup)
Set-ExecutionPolicy -ExecutionPolicy Unrestricted -Scope Process

# Import the installation module
Import-Module ./dev-setup/Install.psm1 -Force

# Install the requested NTools version
InstallNtools -version "1.74.0"
```

The version can be supplied explicitly, or omitted to read the default version from `dev-setup/ntools.json`:


## Post-Installation

After the installation is complete, check out the [nbuild.targets](./nbuild-targets.md) for all available targets, and navigate to [Usage](usage.md) to learn how to execute a build target.

**Note:** For DevOps operations across Azure DevOps and GitHub, use the sdo (sdo.exe) tool which is included with ntools. See the [List of Tools](index.md) documentation for sdo usage and examples.

ntools is now installed on your machine, and you can start using it to learn how to build and run [additional targets](usage.md). If you have any questions or encounter any issues during the installation process, please don't hesitate to create an [issue](https://github.com/naz-hage/NTools/issues). We're here to help!