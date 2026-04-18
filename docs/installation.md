
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

### Option 2: Cross-Platform Installation with install-ntools.py

For a minimal, cross-platform setup, use the install-ntools.py script:

```bash
# Dry run (recommended first)
python atools/install-ntools.py --version 1.74.0 --dry-run

# Full installation
python atools/install-ntools.py --version 1.74.0
```

#### What install-ntools.py Does

This Python script performs a complete NTools installation:

1. **Downloads** the specified NTools release from GitHub
2. **Extracts** NTools to the deployment directory
3. **Updates** system PATH (on Windows)
4. **Verifies** the installation

#### Command Line Options

| Option | Description | Default |
|--------|-------------|---------|
| `--version` | **Required.** Release version to install (e.g., `1.74.0`) | - |
| `--dry-run` | Show what would be done without making changes | `false` |
| `--deploy-path` | Custom installation directory | Platform-specific default |
| `--downloads-dir` | Directory for ZIP downloads | Platform-specific temp directory |
| `--json` | Path to ntools.json config file | `./dev-setup/ntools.json` |
| `--no-path-update` | Skip PATH environment variable updates | `false` |

#### Installation Examples

**Basic Installation:**
```bash
python atools/install-ntools.py --version 1.74.0
```

**Development Installation with Custom Path:**
```bash
python atools/install-ntools.py --version 1.74.0 --deploy-path ./local-tools
```

**Offline Verification:**
```bash
python atools/install-ntools.py --version 1.74.0 --dry-run
```

#### Installation Process Output

The installer provides clear visual feedback:

```
==================================================
Installing NTools (Build Tools)...
==================================================

[SUCCESS] NTools installation completed successfully!
NTools is installed in: C:\Program Files\Nbuild
You can now use 'ntools' commands from any location.
```

#### Safety Features

- **Version validation**: Ensures release exists before downloading
- **Path safety**: Refuses to overwrite critical system directories
- **Backup protection**: Safe removal of existing installations
- **Network verification**: HEAD requests to verify download URLs
- **Cross-platform**: Works on Windows, macOS, and Linux

#### Troubleshooting

**PATH Not Updated**
- **Windows**: Sign out and sign back in, or restart your command prompt
- **Unix**: Run `source ~/.bashrc` or restart your terminal

**Permission Errors**
- **Windows**: Run as Administrator
- **Unix**: Use `sudo` or install to user directory with `--deploy-path`

**Network Issues**
- Verify internet connection
- Check if the version exists on GitHub releases
- Use `--dry-run` to verify URLs without downloading

## Post-Installation

After the installation is complete, check out the [nbuild.targets](./ntools/nbuild-targets.md) for all available targets, and navigate to [Usage](usage.md) to learn how to execute a build target.

**Note:** For DevOps operations across Azure DevOps and GitHub, use the sdo (sdo.exe) tool which is included with ntools. See the [List of Tools](index.md) documentation for sdo usage and examples.

ntools is now installed on your machine, and you can start using it to learn how to build and run [additional targets](usage.md). If you have any questions or encounter any issues during the installation process, please don't hesitate to create an [issue](https://github.com/naz-hage/NTools/issues). We're here to help!