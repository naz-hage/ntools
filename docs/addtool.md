

- When looking for new development tool for your projet, your need the following:
    - Web location to download the tool and the mame of the downloaded file.  This file will be used to install the tool
    - Command and arguments to install and uninstall the tool
    - Location where the tool will be installed
    - Location of the tool File name.  This file name will be used to check if the tool is already installed
    - Version of the tool
    - Name of the tool
    
- To add a new tool to your project which can be installed by `ntools`, you need to define json file.  Below is an example of the json file for 7-zip development tool
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
      "InstallArgs": "/S /D=\u0022$(ProgramFiles)\\7-Zip\u0022",
      "InstallPath": "$(ProgramFiles)\\7-Zip",
      "UninstallCommand": "$(InstallPath)\\Uninstall.exe",
      "UninstallArgs": "/S"
    }
  ]
}
```
- By convention, the json file is named apps.json and is located in the DevSetup folder of your project

- Use Nb.exe to install the tool
```cmd
cd DevSetup
Nb.exe -c install -json apps.json
```

## List of Environment Variables
| Variable Name | Description |
| --- | --- |
| Name | The name of the tool | 
| Version | The version of the tool |
| AppFileName | The file name of the tool.  This file name will be used to check if the tool is already installed |
| WebDownloadFile | The web location to download the tool |
| DownloadedFile | The name of the downloaded file.  This file will be used to install the tool |
| InstallCommand | The command to install the tool |
| InstallArgs | The arguments to install the tool |
| InstallPath | The location where the tool will be installed |
| UninstallCommand | The command to uninstall the tool |
| UninstallArgs | The arguments to uninstall the tool |

