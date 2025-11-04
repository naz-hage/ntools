# SDO Installation Instructions

## Overview

SDO (Simple DevOps) is installed using an isolated virtual environment to prevent dependency conflicts with other Python tools. This approach keeps the NTools distribution smaller by shipping only source code rather than pre-compiled executables.

## Prerequisites

Before installing SDO, you must have NTools installed. See the main [Installation](../installation.md) documentation for NTools setup instructions.

## Installation Steps

### 1. Ensure NTools is Installed

Follow the [NTools Installation](../installation.md) guide to extract the NTools zip file to your preferred location (e.g., `C:\Program Files\NBuild`).

### 2. Run SDO Installation Script

Open a command prompt and navigate to the NTools directory:

```cmd
cd "C:\Program Files\NBuild"
python install-sdo.py
```

**Alternative: If NTools is already in PATH:**

```cmd
python install-sdo.py
```

### 3. Verify Installation

Test that SDO is working:

```cmd
sdo --version
sdo --help
```

## What the Installation Does

1. **Creates Virtual Environment**: `C:\Program Files\NBuild\venv\`
2. **Installs SDO**: Installs SDO and dependencies in isolation
3. **Creates Launcher**: `sdo.cmd` script that activates venv and runs SDO
4. **Maintains PATH**: Since NBuild is in PATH, `sdo` works globally

## Directory Structure After Installation

```
C:\Program Files\NBuild\
├── nb.exe                    # NTools executable
├── sdo.cmd                   # SDO launcher script (created by installer)
├── venv\                     # Virtual environment
│   ├── Scripts\             # Python executables
│   ├── Lib\site-packages\   # Isolated SDO dependencies
│   └── pyvenv.cfg
├── install-sdo.py           # SDO installation script
├── pyproject.toml           # SDO package configuration
├── sdo_package\             # SDO source code
└── [other NTools files...]
```

## Benefits of Virtual Environment Approach

- **Smaller Distribution**: NTools zip contains only source code, not compiled executables
- **No Dependency Conflicts**: SDO packages don't interfere with system Python
- **Isolated Environment**: Each tool has its own dependencies
- **Easy Updates**: Delete `venv\` folder to reset SDO installation
- **Flexible Deployment**: Works across different Python environments
- **Professional Distribution**: Follows Python best practices

## Troubleshooting

### Python Not Found
- Ensure Python 3.8+ is installed and in PATH
- Run: `python --version` to verify

### Permission Errors
- Run command prompt as Administrator
- Or install to user directory: `python install-sdo.py --nbuild-path "%USERPROFILE%\NBuild"`

### Installation Fails
- Check internet connection (pip downloads dependencies)
- Try dry-run first: `python install-sdo.py --dry-run`

### SDO Command Not Found
- Ensure `C:\Program Files\NBuild` is in PATH
- Restart command prompt after installation
- Check that `sdo.cmd` exists in NBuild directory

## Uninstalling SDO

To remove SDO completely:

```cmd
# Remove virtual environment
rd /s "C:\Program Files\NBuild\venv"

# Remove launcher script
del "C:\Program Files\NBuild\sdo.cmd"

# Remove source files (optional)
rd /s "C:\Program Files\NBuild\sdo_package"
del "C:\Program Files\NBuild\install-sdo.py"
del "C:\Program Files\NBuild\pyproject.toml"
```