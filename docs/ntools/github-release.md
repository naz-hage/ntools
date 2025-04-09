# GitHubRelease Documentation

GitHubRelease is a tool that allows you to create and manage GitHub releases from the command line. It simplifies the process of creating and managing releases, making it easier to publish your software updates on GitHub.

## Requirements

### Repository Requirements
- The repository must have:
  - A GitHub token to create releases.
  - A GitHub owner to create releases.
  - A Git branch to create releases.
  - At least one Git tag prior to creating releases.

### Environment Requirements
- The GitHub API token and repository owner are obtained from environment variables:
  - **`OWNER`:** The GitHub repository owner's username.
  - **`API_GITHUB_KEY`:** The GitHub API token (personal access token).
- **Local development with Windows Platforms:**
  - For additional security, the GitHub API token should be saved in the Windows Credential Manager with:
    - **Target Name:** `GitHubRelease`
    - **Credential Name:** `API_GITHUB_KEY`
- 
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

| **Permission Type** | **Scope**              | **Description**                          |
|----------------------|------------------------|------------------------------------------|
| Repository           | Contents: `Read/Write`| Access repository contents.              |
|                      | Metadata: `Read-only` | Access repository metadata.              |
|                      | Actions: `Read/Write` | Manage GitHub Actions (if needed).       |
|                      | Packages: `Read/Write`| Manage GitHub Packages (if needed).      |
| Workflow             | Workflows: `Read/Write`| Manage workflows (if needed).           |
| Release              | Releases: `Read/Write`| Manage GitHub releases.                  |

## GitHubRelease Command Line Options

### Usage
```batch
GitHubRelease.exe command [-repo value] [-tag value] [-branch value] [-file value] [-path value] [-v value]
  - command : Specifies the command to execute.
         create          -> Create a release. Requires repo, tag, branch and file.
         download        -> Download an asset. Requires repo, tag, and path (optional)
         ----
 (one of create,download, required)
  - repo    : Specifies the repository name. Applicable to all commands. (string, default=)
  - tag     : Specifies the tag name. Aplicable for all commands (string, default=)
  - branch  : Specifies the branch name. Applicable for create command (string, default=main)
  - file    : Specifies the asset file name. Must include full path. Applicable for create command (string, default=)
  - path    : Specifies the asset path. Must be an absolute path. (string, default=)
  - v       : Optional parameter which sets the console output verbose level. (true or false, default=False)
```
### Example: Creating a Release
To create a release for the repository `my-repo` with the tag `1.0.0`, branch `main`, and an asset located at `C:\Releases\1.0.0.zip`, you would use the following command:

```batch
GitHubRelease.exe create -repo my-repo -tag 1.0.0 -branch main -file C:\Releases\1.1.0.zip -v true
```

### Example: Downloading an Asset
To download an asset from the release with the tag `1.0.0` in the repository `my-repo` to the path `C:\Downloads`:

```batch
GitHubRelease.exe download -repo my-repo -tag 1.0.0 -path C:\Downloads -v true
```
An asset named 1.0.0.zip will be downloaded to the specified path if it exists in the release.


-- Current expections:
- The download assets expects to download an asset as a zip file name ${tag}.zip
- tag must be a valid tag in the repository created by the tool. (X.Y.Z)
