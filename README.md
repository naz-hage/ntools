# Software Tools Collection

This repository contains a collection of software tools specifically designed to automate various build and test tasks on Windows clients. Whether you are a developer working on your local machine or using GitHub Actions for continuous integration, these tools will greatly simplify your workflow and enhance your productivity.

With the NTools, you can effortlessly backup your files and folders, build your projects with ease, perform Git operations seamlessly, and leverage powerful MSBuild tasks. The installation process is straightforward, and the tools are highly reliable and efficient, ensuring the safety and integrity of your data.

Take advantage of the NTools' intuitive command-line interface to streamline your development process. From building solutions to running tests, creating staging builds, and exploring available options, the NTools provide a seamless experience for all your software development needs.

Don't settle for mediocre tools when you can have the NTools at your disposal. Try them out today and witness the difference they can make in your development workflow. Enhancements are added weekly! 

Don't hesitate to write an [issue](https://github.com/naz-hage/ntools/issues) if you have any questions or suggestions.

## Table of Contents
1. [Nbackup](#Nbackup)
2. [Nbuild](#Nbuild)
3. [Ngit](#Ngit)
4. [NbuildTasks](#nbuildtasks)
5. [Installation](#Installation)
6. [Usage](#Usage)

## Nbackup
[Nbackup](./Nbackup/README.md) is a tool that leverages `robocopy` to backup a list of files and folders from a source to a destination. It is designed to be reliable and efficient, ensuring that your data is safe.

## Nbuild (Nb)
[Nbuild](./Nbuild/README.md) is a tool that launches MSBuild with a target to build. It simplifies the build process and makes it easier to manage your projects.

## Ngit (Ng)
[Ngit](./Ngit/README.md) is simple wrapper for `Git` tool that perform simple commands such as get tag and set tag.

## NbuildTasks
[NbuildTasks](./NbuildTasks/README.md) is a class library that exposes `MSBuild` tasks. It is used by `Nbuild` to perform various tasks such as web download and tools installation during the build of any project.

## Installation
To get started with the NTools repository, follow these steps:

1. Clone this repository to your local machine.
2. Open a command prompt in administrative mode and navigate to the root folder of the repository.
3. Run the following command to install the tools:

    ```cmd
    install.bat
    ```

   This command will install the Dotnet Core Desktop runtime and download the Ntools from GitHub. The tools will be installed in the `C:\Program Files\Nbuild` folder.

Once the installation is complete, you'll be ready to use the NTools from the command line. Refer to the [Usage](#Usage) section for examples of how to use the tools.

## Usage
Once installation is complete, you can use the NTools from the command line.  Open a Terminal and navigate to your solution folder.

- Examples: 
-   Build a solution:

    ```cmd
    nb.exe solution
    ```
- Clean a solution:

    ```cmd
    nb.exe clean
    ```

- Run tests on a solution:

    ```cmd
    nb.exe test
    ```
- Create a staging build:

    ```cmd
    nb.exe staging
    ```
- Display available options:
    
        ```cmd
        nb.exe -cmd targets
        ```