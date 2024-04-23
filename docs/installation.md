
### Prerequisites
- Install the latest version of [64-bit Git for Windows](https://git-scm.com/download/win) on your machine.

To get started with the `NTools` repository, follow these steps:

- Open a PowerShell in administrative mode.  By convention, two environment variables, `%DevDrive%='c:'` and `%MainDir%='source'` will be used through this document 
- Clone this repository to your local machine from the `%MainDir%` folder.
```cmd
cd c:\source
git clone https://github.com/naz-hage/NTools
```
- Change the PowerShell execution policy to allow the installation script to run. Run the following command:

```cmd
Set-ExecutionPolicy -ExecutionPolicy Unrestricted -Scope Process
```
    
This command will allow the installation script to run. Once the installation is complete, the execution policy will revert to its original state.


- Run the following command to install the Development tools:

```cmd
cd ntools\DevSetup
.\install.ps1
```
- This command will install the Dotnet Core Desktop runtime and download the `NTools` from GitHub, installs the Ntools in the `%ProgramFiles%\Nbuild` folder, and sets up the nTools development environment.  The `%ProgramFiles%\Nbuild` will be added to the system path.  
- The `Install.ps1` script also sets up the following environment variables:
    - `%DevDrive%` is a string parameter that represents the drive where the development environment will be set up. It is not mandatory , and if it is not provided when the script is run, it will default to `C:`.
    - `%MainDir%` is also a string parameter that represents the main directory where the development environment will be set up. It is not mandatory, and if it is not provided when the script is run, it will default to `source`.

- If you prefer to use a different drive or directory, you can specify the `%DevDrive%` and `%MainDir%` parameters when running the script. For example, to install the development tools on the `D:` drive in the `Development` directory, run the following command:

```cmd
.\install.ps1 -DevDrive D: -MainDir Development
```

- After the installation is complete, check out the [nbuild.targets](./ntools/nbuild-targets.md) for more all the available targets, and navigate to [Usage](usage.md) to learn how to execute a build target.
