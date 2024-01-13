# Software Tools Collection

This repository contains a collection of software tools designed to automate various tasks on Windows clients.  This tasks can run on local devlopment machines or GitHub Actions.

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
[NbuildTasks](./NbuildTasks/README.md) is a class library that exposes `MSBuild` tasks. It is used by `Nbuild` to perform various tasks during the build of any project.

## Installation
- Download the latest release from the [releases](https://github.com/naz-hage/ntools/releases/) page and extract the zip file to the folder `%ProgramFiles%/nbuild` on your computer.  Add the folder to your `PATH` environment variable.
- Use the command line : `curl -L -o ntools.zip  https://github.com/naz-hage/ntools/releases/download/1.0.42/ntools-1.0.42.zip`

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