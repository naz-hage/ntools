# Software Tools Collection

A powerful command-line utility for .NET developers, providing build automation, testing, Git operations, and GitHub release management.

## Key Features

- **Build & Test**: Comprehensive build system with .NET SDK integration and extensive testing support
- **Git Operations**: Complete Git workflow automation including tagging, branching, and repository cloning
- **GitHub Integration**: Automated release creation and asset management
- **Global Options**: `--dry-run` and `--verbose` options available across all commands
- **Modular Architecture**: Clean separation of concerns with service-based design
- **Extensive Testing**: Comprehensive unit test suite with targeted execution options

## Recent Updates

- **.NET SDK Integration**: Updated nb.exe to use .NET SDK instead of direct MSBuild dependency with automatic dotnet.exe detection
- **System.CommandLine 2.0.1**: Upgraded to stable CLI framework with global option support
- **Enhanced Testing**: Added granular unit testing targets for efficient test execution
- **Git Clone Support**: New command for repository cloning operations
- **Improved Documentation**: Updated CLI reference and build target documentation

- Checkout the [documentation](https://naz-hage.github.io/ntools/) for more information.

- The [installation](https://naz-hage.github.io/ntools/installation/) process is straightforward, and the tools are highly reliable and efficient, ensuring the safety and integrity of your data.

- Take advantage of the `ntools`' [intuitive command-line interface](https://naz-hage.github.io/ntools/usage/) to streamline your development process. From building solutions to running tests, creating stage builds, and exploring available options, the `ntools` provide a seamless experience for all your software development needs.

- Don't hesitate to write an [issue](https://github.com/naz-hage/NTools/issues) if you have any questions or suggestions.

- GitHubRelease is a tool that allows you to create and manage GitHub releases from the command line. It simplifies the process of creating and managing releases, making it easier to publish your software updates on GitHub.
    - **Authentication Methods** (choose one):
        - Set `API_GITHUB_KEY` environment variable with your GitHub personal access token
        - Use GitHub CLI: Run `gh auth login` to authenticate
        - Windows Credential Manager (for additional security)
    - **Smart Authentication**: Public repositories work without authentication for read operations, private repositories require authentication
    - Must add a Repository secret token named `API_GITHUB_KEY` to the GitHub repository Secrets and variables (for CI/CD)
    - Must add a Repository secret owner named `OWNER` to the GitHub repository Secrets and variables (optional)
    - Example GitHub Actions workflow:
    ```yml
    - name: Build using ntools
      run: |
        & "$env:ProgramFilesPath/nbuild/nb.exe" ${{ env.Build_Type }} -v ${{ env.Enable_Logging }}
      shell: pwsh
      working-directory: ${{ github.workspace }}
      env:
        OWNER: ${{ secrets.OWNER }}
        API_GITHUB_KEY: ${{ secrets.API_GITHUB_KEY }}
    ```

When `nb stage` runs successfully, the tool creates a stage release. This release is tagged with the next tag release number, and the release notes include the commits since the last stage or prod tag. The API token from the repository secrets is used to create this release.  The release package is uploaded to the release. The release is also tagged with the next stage release number.

When `nb prod` runs successfully, the tool creates a production release. This release is also tagged with the next prod release, and the release notes include the commits since the last production tag. All previous stage releases are deleted. The API token from the repository secrets is used to create this release. The release package is uploaded to the release. The release is also tagged with the next prod release number. All previous stage releases are deleted.
