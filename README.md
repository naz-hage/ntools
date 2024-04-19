# Software Tools Collection

This repository contains a collection of software tools specifically designed to automate various build and test tasks on Windows clients. Whether you are a developer working on your local machine or using GitHub Actions for continuous integration, these tools will greatly simplify your workflow and enhance your productivity.

With the `NTools`, you can effortlessly:
- Backup your files and folders
- Build your projects with ease
- Perform Git operations seamlessly
- Leverage powerful MSBuild tasks

The installation process is straightforward, and the tools are highly reliable and efficient, ensuring the safety and integrity of your data.

Take advantage of the `NTools`' intuitive command-line interface to streamline your development process. From building solutions to running tests, creating staging builds, and exploring available options, the `NTools` provide a seamless experience for all your software development needs.

Don't settle for mediocre tools when you can have the `NTools` at your disposal. Try them out today and witness the difference they can make in your development workflow. Enhancements are added weekly! 

Don't hesitate to write an [issue](https://github.com/naz-hage/`NTools`/issues) if you have any questions or suggestions.

## Ntools

1. Nbackup
2. [Nbuild](https://naz-hage.github.io/ntools/ntools/nbuild/)
3. NbuildTasks
4. Ngit

Check out the documentation at: https://naz-hage.github.io/ntools/

## Usage (examples)

Open a Terminal and navigate to your solution folder.

-   Build a solution:

```cmd
Nb.exe solution
```
- Clean a solution:

```cmd
Nb.exe clean
```

- Run tests on a solution:

```cmd
Nb.exe test
```
- Create a staging build:

```cmd
Nb.exe staging
```
- Display available options:
    
```cmd
Nb.exe -c targets
```
