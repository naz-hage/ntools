## Installation
To get started with the `NTools` repository, follow these steps:

1. Clone this repository to your local machine.
2. Open a command prompt in administrative mode and navigate to the root folder of the repository.
3. Change the PowerShell execution policy to allow the installation script to run. Run the following command:

```cmd
Set-ExecutionPolicy -ExecutionPolicy Unrestricted -Scope Process
```

This command will allow the installation script to run. Once the installation is complete, the execution policy will revert to its original state.

4. Run the following command to install the tools:

```cmd
install.bat
```

This command will install the Dotnet Core Desktop runtime and download the `NTools` from GitHub. The tools will be installed in the `C:\Program Files\Nbuild` folder.
