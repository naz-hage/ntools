
To get started with `ntools`, you need to install the latest version of [64-bit Git for Windows](https://git-scm.com/download/win) on your machine, then follow these steps:

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

# Run full development setup
.\dev-setup\install.ps1
```

**Or using the ntools-scripts module:**

```powershell
cd ./ntools
Import-Module "./scripts/module-package/ntools-scripts.psm1" -Force
Install-NTools -NtoolsJsonPath "./dev-setup/ntools.json"
Install-DevelopmentApps -JsonPath "./dev-setup/apps.json"
```

### Option 2: NTools Only Installation (Cross-Platform)

For users who just want to install NTools without the full development environment:

```powershell
cd ./ntools
python atools/install-ntools.py --version 1.32.0
```

**Custom installation path:**

```powershell
python atools/install-ntools.py --version 1.32.0 --deploy-path "C:\MyTools\Nbuild"
```

## What Each Option Installs

| Component | Development Setup | NTools Only |
|-----------|------------------|-------------|
| .NET Runtime | ✅ | ❌ |
| NTools Core | ✅ | ✅ |
| Development Apps | ✅ | ❌ |
| Cross-Platform | ⚠️ (Windows only) | ✅ |
| Admin Required | ✅ | ❌ (unless system paths) |

## Post-Installation

After the installation is complete, check out the [nbuild.targets](./ntools/nbuild-targets.md) for more all the available targets, and navigate to [Usage](usage.md) to learn how to execute a build target.

ntools is now installed on your machine, and you can start using it to learn how to build and run [additional targets](usage.md). If you have any questions or encounter any issues during the installation process, please don't hesitate to write an an [issue](https://github.com/naz-hage/NTools/issues). We're here to help!

ntools is now installed on your machine, and you can start using it to learn how to build and run [additional targets](usage.md). If you have any questions or encounter any issues during the installation process, please don't hesitate to write an an [issue](https://github.com/naz-hage/NTools/issues). We're here to help!