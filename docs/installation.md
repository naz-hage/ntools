
To get started with `sdo`, you need to install the latest version of [64-bit Git for Windows](https://git-scm.com/download/win) and the [.NET SDK](https://dotnet.microsoft.com/download) on your machine, then follow these steps:

- Open a PowerShell in administrative mode.  Assume c:\source as directory `%MainDirectory%` which will be used through this document.
- Clone this repository to your local machine from the `%MainDirectory%` folder.
```powershell
cd c:\source
git clone https://github.com/naz-hage/sdo
```

## Installation Options

### Option 1: Full Development Environment Setup (Recommended for Contributors)

This installs the complete development environment including .NET runtime, SDO, and development tools:

```powershell
cd ./sdo
# Change PowerShell execution policy (one-time setup)
Set-ExecutionPolicy -ExecutionPolicy Unrestricted -Scope Process

# Import the installation module
Import-Module ./dev-setup/Install.psm1 -Force

# Install the requested SDO version
InstallNtools -version "1.74.0"
```

The version can be supplied explicitly, or omitted to read the default version from `dev-setup/ntools.json`:


## Post-Installation

After the installation is complete, check out the [sdo.targets](./sdo-targets.md) for all available targets, and navigate to [Usage](usage.md) to learn how to execute a build target.

**Note:** For DevOps operations across Azure DevOps and GitHub, use the SDO
(sdo.exe) tool. See the [SDO documentation](index.md) for usage and examples.

SDO is now installed on your machine, and you can use it to build and run
[additional targets](usage.md). If you have any questions or encounter any
issues during the installation process, please create an
[issue](https://github.com/naz-hage/sdo/issues).