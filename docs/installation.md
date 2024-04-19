
## Installation
To get started with the `NTools` repository, follow these steps:

1. Clone this repository to your local machine.
2. Open a PowerShell in administrative mode and navigate to the root folder of the repository.
3. Change the PowerShell execution policy to allow the installation script to run. Run the following command:

```cmd
Set-ExecutionPolicy -ExecutionPolicy Unrestricted -Scope Process
```
    
    This command will allow the installation script to run. Once the installation is complete, the execution policy will revert to its original state.
    - This sets the environment to run the `Install.ps1` script which installs the Development tools and sets up the following environment variables:
        - `$DevDrive` is a string parameter that represents the drive where the development environment will be set up. It is not mandatory , and if it is not provided when the script is run, it will default to `C:`.
        - `$MainDir` is also a string parameter that represents the main directory where the development environment will be set up. It is not mandatory, and if it is not provided when the script is run, it will default to `source`.

4. Run the following command to install the Development tools:

```cmd
cd DevSetup
.\install.ps1
```

   This command will install the Dotnet Core Desktop runtime and download the `NTools` from GitHub. The tools will be installed in the `C:\Program Files\Nbuild` folder.  The `C:\Program Files\Nbuild` will be added to the system path.