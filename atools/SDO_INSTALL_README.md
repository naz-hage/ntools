# SDO Installation Instructions

## Overview

SDO (Simple DevOps) is now installed using an isolated virtual environment to prevent dependency conflicts with other Python tools.

## Installation Steps

### 1. Extract NTools Zip File

Extract the NTools zip file to `C:\Program Files\NBuild` (or your preferred location).

### 2. Run SDO Installation Script

Open a command prompt and navigate to the NTools directory:

```cmd
cd "C:\Program Files\NBuild"
python atools\install_sdo.py
```

**Alternative: If NTools is already in PATH:**

```cmd
python install_sdo.py
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
├── atools\                   # SDO source code
│   ├── sdo_package\
│   └── install_sdo.py
└── [other NTools files...]
```

## Troubleshooting

### Python Not Found
- Ensure Python 3.8+ is installed and in PATH
- Run: `python --version` to verify

### Permission Errors
- Run command prompt as Administrator
- Or install to user directory: `python install_sdo.py --nbuild-path "%USERPROFILE%\NBuild"`

### Installation Fails
- Check internet connection (pip downloads dependencies)
- Try dry-run first: `python install_sdo.py --dry-run`

### SDO Command Not Found
- Ensure `C:\Program Files\NBuild` is in PATH
- Restart command prompt after installation
- Check that `sdo.cmd` exists in NBuild directory

## Benefits of Virtual Environment Approach

- **No Dependency Conflicts**: SDO packages don't interfere with system Python
- **Isolated Environment**: Each tool has its own dependencies
- **Easy Updates**: Delete `venv\` folder to reset SDO installation
- **Professional Distribution**: Follows Python best practices

## Uninstalling SDO

To remove SDO completely:

```cmd
# Remove virtual environment
rmdir /s "C:\Program Files\NBuild\venv"

# Remove launcher script
del "C:\Program Files\NBuild\sdo.cmd"

# Remove source code (optional)
rmdir /s "C:\Program Files\NBuild\atools"
```