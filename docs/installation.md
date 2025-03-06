
To get started with `ntools`, you need to install the latest version of [64-bit Git for Windows](https://git-scm.com/download/win) on your machine, then follow these steps:

- Open a PowerShell in administrative mode.  Assume c:\source as directory `%MainDirectory%` which will be used through this document.
- Clone this repository to your local machine from the `%MainDirectory%` folder.
```powershell
cd c:\source
git clone https://github.com/naz-hage/ntools
```
- Change the PowerShell execution policy to allow the installation script to run. Run the following command:

```powershell
Set-ExecutionPolicy -ExecutionPolicy Unrestricted -Scope Process
```
    
This command will allow the installation script to run. Once the installation is complete, the execution policy will revert to its original state.


- Run the following command to install the ntools:

```powershell
cd c:\source\ntools\dev-setup
.\install.ps1
```
- This command will install the Dotnet Core Desktop runtime and download the `ntools` from GitHub, installs the ntools package in the `%ProgramFiles%\Nbuild` folder, sets up the nTools development environment, adds the `%ProgramFiles%\Nbuild` will be added to the system path.  

After the installation is complete, check out the [nbuild.targets](./ntools/nbuild-targets.md) for more all the available targets, and navigate to [Usage](usage.md) to learn how to execute a build target.

ntools is now installed on your machine, and you can start using it to learn how to build and run [additional targets](usage.md). If you have any questions or encounter any issues during the installation process, please don't hesitate to write an an [issue](https://github.com/naz-hage/NTools/issues). We're here to help!