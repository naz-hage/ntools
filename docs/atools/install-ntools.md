# install-ntools.py

A cross-platform Python installer that downloads, extracts, and installs NTools release ZIPs along with the SDO (Simple DevOps) tool.

## Overview

This script performs a complete installation of both NTools and SDO:

1. **Downloads** the specified NTools release from GitHub
2. **Extracts** NTools to the deployment directory
3. **Updates** system PATH (on Windows)
4. **Installs** SDO with the same version number
5. **Configures** SDO for immediate use

## Quick Usage

### Dry Run (Recommended First)
```bash
python atools/install-ntools.py --version 1.60.0 --dry-run
```

### Full Installation
```bash
python atools/install-ntools.py --version 1.60.0
```

### Custom Installation Path
```bash
python atools/install-ntools.py --version 1.60.0 --deploy-path "/custom/path"
```

## Installation Process

The installer provides clear visual feedback with banners for each phase:

### Phase 1: NTools Installation
```
==================================================
Installing NTools (Build Tools)...
==================================================
```

- Downloads the release ZIP from GitHub
- Extracts to deployment directory
- Updates system PATH
- Verifies installation

### Phase 2: SDO Installation (Skipped in Dry Run)
```
==================================================
Installing SDO (Simple DevOps) tool...
==================================================
```

- **Note**: This phase is skipped in `--dry-run` mode
- Uninstalls any existing SDO installation
- Installs SDO with matching version
- Updates pyproject.toml version
- Creates launcher scripts
- Tests the installation

### Final Result
```
[SUCCESS] NTools and SDO installation completed successfully!
Both tools are installed in: C:\Program Files\Nbuild
You can now use 'ntools' and 'sdo' commands from any location.
```

## Command Line Options

| Option | Description | Default |
|--------|-------------|---------|
| `--version` | **Required.** Release version to install (e.g., `1.60.0`) | - |
| `--dry-run` | Show what would be done without making changes | `false` |
| `--deploy-path` | Custom installation directory | Platform-specific default |
| `--downloads-dir` | Directory for ZIP downloads | Platform-specific temp directory |
| `--json` | Path to ntools.json config file | `./dev-setup/ntools.json` |
| `--no-path-update` | Skip PATH environment variable updates | `false` |

## Safety Features

- **Version validation**: Ensures release exists before downloading
- **Path safety**: Refuses to overwrite critical system directories
- **Backup protection**: Safe removal of existing installations
- **Network verification**: HEAD requests to verify download URLs
- **Cross-platform**: Works on Windows, macOS, and Linux

## Examples

### Basic Installation
```bash
python atools/install-ntools.py --version 1.60.0
```

### Development Installation
```bash
python atools/install-ntools.py --version 1.60.0 --deploy-path ./local-tools
```

### Offline Verification
```bash
python atools/install-ntools.py --version 1.60.0 --dry-run
```

## Troubleshooting

### PATH Not Updated
If `ntools` command is not found after installation:
- **Windows**: Sign out and sign back in, or restart your command prompt
- **Unix**: Run `source ~/.bashrc` or restart your terminal

### Permission Errors
If installation fails with permission errors:
- **Windows**: Run as Administrator
- **Unix**: Use `sudo` or install to user directory with `--deploy-path`

### Network Issues
If download fails:
- Verify internet connection
- Check if the version exists on GitHub releases
- Use `--dry-run` to verify URLs without downloading

## Related Documentation

- [atools/index.md](../atools/index.md) - Overview of all atools scripts
- [installation.md](../installation.md) - General installation guide
- [setup.md](../setup.md) - Development setup instructions
