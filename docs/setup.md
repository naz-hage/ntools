# Setting Up Your Project with dev-setup

The dev-setup folder is a critical part of your project setup. It contains scripts and configuration files to automate the installation of development tools and the setup of your development environment.

## PowerShell Module Integration

**New in v2.3.0**: NTools now includes a consolidated PowerShell module (`ntools-scripts`) that replaces individual scripts with a unified, function-based approach. This module is automatically integrated with the setup process.

### Using ntools-scripts Module
```powershell
# Import the module
Import-Module "./scripts/module-package/ntools-scripts.psm1" -Force

# Install NTools using the module (recommended approach)
Install-NTools -NtoolsJsonPath "./dev-setup/ntools.json"

# Set up development environment
Set-DevelopmentEnvironment
Install-DevelopmentApps
```

For complete module documentation, see [ntools-scripts Module](ntools/ntools-scripts-module.md).

## Overview of dev-setup Folder

The dev-setup folder typically includes the following files:

- **`ntools.json`**  
    - Contains installation information for ntools. This file is required to install ntools before other development tools.
- **`apps.json`**  
    - Lists the development tools required for your project, including their installation and uninstallation details.
- **Legacy scripts** (deprecated in favor of ntools-scripts module)
  - Individual PowerShell scripts for specific tasks

---

## File Details

### 1. ntools.json
This file provides the installation details for ntools, which is required to manage other tools in the project.

**Example:**
```json
{
  "Version": "1.2.0",
  "NbuildAppList": [
    {
      "Name": "Ntools",
      "Version": "1.8.0",
      "AppFileName": "$(InstallPath)\\nb.exe",
      "WebDownloadFile": "https://github.com/naz-hage/ntools/releases/download/$(Version)/$(Version).zip",
      "DownloadedFile": "$(Version).zip",
      "InstallCommand": "powershell.exe",
      "InstallArgs": "-Command Expand-Archive -Path $(Version).zip -DestinationPath '$(InstallPath)' -Force",
      "InstallPath": "$(ProgramFiles)\\Nbuild",
      "UninstallCommand": "powershell.exe",
      "UninstallArgs": "-Command Remove-Item -Path '$(InstallPath)' -Recurse -Force",
      "StoredHash": "XXX",
      "AddToPath": true
    }
  ]
}
```

---

### 2. apps.json
This file lists all the development tools required for the project. Each tool is defined with its name, version, installation details, and uninstallation details.

**Example:**
```json
{
  "Version": "1.2.0",
  "NbuildAppList": [
    {
      "Name": "7-zip",
      "Version": "23.01",
      "AppFileName": "$(InstallPath)\\7z.exe",
      "WebDownloadFile": "https://www.7-zip.org/a/7z2301-x64.exe",
      "DownloadedFile": "7zip.exe",
      "InstallCommand": "$(DownloadedFile)",
      "InstallArgs": "/S /D=\"$(ProgramFiles)\\7-Zip\"",
      "InstallPath": "$(ProgramFiles)\\7-Zip",
      "UninstallCommand": "$(InstallPath)\\Uninstall.exe",
      "UninstallArgs": "/S"
    }
  ]
}
```

**Key Elements in apps.json:**

| Element Name       | Description                                                                 |
|--------------------|-----------------------------------------------------------------------------|
| `Name`            | The name of the tool.                                                      |
| `Version`         | The version of the tool.                                                   |
| `AppFileName`     | The file name of the tool, used to check if it is already installed.        |
| `WebDownloadFile` | The URL to download the tool.                                               |
| `DownloadedFile`  | The name of the downloaded file, used for installation.                    |
| `InstallCommand`  | The command to install the tool.                                            |
| `InstallArgs`     | The arguments for the installation command.                                 |
| `InstallPath`     | The location where the tool will be installed.                              |
| `UninstallCommand`| The command to uninstall the tool.                                          |
| `UninstallArgs`   | The arguments for the uninstallation command.                               |
| `StoredHash`      | (Optional) SHA256 hash of the file for verification.                        |
| `AddToPath`       | (Optional) Whether to add the tool's path to the system PATH environment variable. |

---

### 3. `dev-setup.ps1`
This PowerShell script automates the installation of tools and sets up the development environment.

**Key Responsibilities:**

- Installs ntools using ntools.json.
- Installs other tools listed in apps.json.
- Verifies administrative privileges before proceeding.

**Example:**
```powershell
# Import the install module
$url = "https://raw.githubusercontent.com/naz-hage/ntools/main/dev-setup/install.psm1"
$output = "./install.psm1"
Invoke-WebRequest -Uri $url -OutFile $output
Import-Module ./install.psm1 -Force

# Install Ntools
MainInstallApp -command install -json .\ntools.json
if ($LASTEXITCODE -ne 0) {
    Write-OutputMessage "Error: Installation of ntools failed. Exiting script."
    exit 1
}

# Install other tools
& $global:NbExePath install -json .\apps.json
if ($LASTEXITCODE -ne 0) {
    Write-OutputMessage "Error: Installation of other tools failed. Exiting script."
    exit 1
}

Write-OutputMessage "Completed installation script."
```

---

## Folder Structure

Your project folder should look like this:
```plaintext
%MainDirectory%\
├── MyProject\
│   ├── dev-setup\
│   │   ├── ntools.json
│   │   ├── apps.json
│   │   ├── dev-setup.ps1
│   ├── ... other project and test files
│   └── nbuild.targets  (this file is required in the solution folder)
```

---

## Adding a New Development Tool

To add a new tool to your project:
1. Identify the tool's:
   - Download URL and file name.
   - Installation and uninstallation commands and arguments.
   - Installation path.
   - File name for version checks.
   - Version and name.
2. Add the tool's details to apps.json.

**Example for a new tool:**
```json
{
  "Name": "Docker",
  "Version": "4.38.0.0",
  "AppFileName": "$(InstallPath)\\Docker Desktop.exe",
  "WebDownloadFile": "https://desktop.docker.com/win/main/amd64/Docker%20Desktop%20Installer.exe",
  "DownloadedFile": "Docker Desktop Installer.exe",
  "InstallCommand": "$(DownloadedFile)",
  "InstallArgs": "install --quiet",
  "InstallPath": "$(ProgramFiles)\\Docker\\Docker",
  "UninstallCommand": "powershell.exe",
  "UninstallArgs": "-Command \"Remove-Item -Path '$(InstallPath)' -Recurse -Force\"",
  "AddToPath": true
}
```
