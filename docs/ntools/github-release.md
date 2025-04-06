# GitHubRelease Documentation

GitHubRelease is a tool that allows you to create and manage GitHub releases from the command line. It simplifies the process of creating and managing releases, making it easier to publish your software updates on GitHub.

## Requirements

### Repository Requirements
- The repository must have:
  - A `.NET` solution file and at least one `.NET` project to build the project.
  - A GitHub token to create releases.
  - A GitHub owner to create releases.
  - A Git branch to create releases.
  - At least one Git tag prior to creating releases.

### Environment Requirements
- **Windows Platforms:**
  - The GitHub API token must be stored in the Windows Credential Manager with:
    - **Target Name:** `GitHubRelease`
    - **Credential Name:** `API_GITHUB_KEY`
- **Non-Windows Platforms:**
  - The following environment variables must be set:
    - **`OWNER`:** The GitHub repository owner's username.
    - **`API_GITHUB_KEY`:** The GitHub API token (personal access token).

### GitHub Actions Workflow Example
Here is an example of how to set up the required environment variables in a GitHub Actions workflow file:

```yml
- name: Build using ntools
  run: |
    & "$env:ProgramFilesPath/nbuild/nb.exe" ${{ env.Build_Type }} -v ${{ env.Enable_Logging }}
  shell: pwsh
  working-directory: ${{ github.workspace }}
  env:
    OWNER: ${{ github.repository_owner }}
    API_GITHUB_KEY: ${{ secrets.API_GITHUB_KEY }}
```

### Branch Checkout Example
Before running the tool, you must checkout a branch. Here is an example of how to checkout a branch in a GitHub Actions workflow file:

```yml
- name: Checkout Repository
  uses: actions/checkout@v4
  with:
    token: ${{ secrets.API_GITHUB_KEY }}
    fetch-depth: 0
    ref: ${{ github.event.pull_request.head.ref }}
    repository: ${{ github.event.pull_request.head.repo.full_name }}
```

## Release Process

### Stage Release
- When `nb stage` runs successfully:
  - The tool creates a stage release tagged with the next stage release number.
  - The release notes include the commits since the last stage or production tag.
  - The API token from the repository secrets is used to create this release.
  - The release package is uploaded to the release.

### Production Release
- When `nb prod` runs successfully:
  - The tool creates a production release tagged with the next production release number.
  - The release notes include the commits since the last production tag.
  - All previous stage releases are deleted.
  - The API token from the repository secrets is used to create this release.
  - The release package is uploaded to the release.

## Create a GitHub Token

Follow the [GitHub documentation](https://docs.github.com/en/github/authenticating-to-github/keeping-your-account-and-data-secure/creating-a-personal-access-token) to create a GitHub token.

### Required Permissions
The access token must have the following permissions:
- **Repository Permissions:**
  - **Contents:** `Read and write`
  - **Metadata:** `Read-only`
  - **Actions:** `Read and write` (if needed)
  - **Packages:** `Read and write` (if needed)
- **Workflow Permissions:**
  - **Workflows:** `Read and write` (if needed)
- **Release Permissions:**
  - **Releases:** `Read and write`

## GitHubRelease Command Line Options

### Usage
```batch
GitHubRelease.exe command [-repo value] [-tag value] [-branch value] [-path value] [-v value]
  - command : Specifies the command to execute.
         create          -> Create a release. Requires repo, tag, branch, and path.
         download        -> Download an asset. Requires repo, tag, and path.
         ----
 (one of create, download, required)
  - repo    : Repository name. (string, default=)
  - tag     : Tag name. (string, default=)
  - branch  : Branch name. (string, default=main)
  - path    : Asset path. Must be an absolute path. (string, default=)
  - v       : Optional parameter which sets the console output verbose level. (true or false, default=False)
```