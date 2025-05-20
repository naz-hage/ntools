Nb.exe is also designed to help you create and manage GitHub releases. It also enables you to download release assets, such as files named in the format x.y.z.zip. The tool expects downloaded assets to be in a zip file named ${tag}.zip, where tag must be a valid tag in the repository created by the tool (e.g., ``x.y.z``).  checkout the [tagging](../versioning.md) section for more details.

## Requirements

### Repository Requirements
- The repository must have:
  - A GitHub token to create releases.
  - A GitHub owner to create releases.
  - A Git branch to create releases.
  - At least one Git tag prior to creating releases.

### Environment Requirements
- The GitHub API token (Required) and repository owner (Optional) are obtained from environment variables:
  - **`OWNER`:** The GitHub repository owner's username.
    - The owner is optional and can be specified in the command line with `-repo` option. Checkout usage below.
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
The above action builds, test, and creates a release using the GitHubRelease tool and upload to GitHub.

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
See [nb.exe](../ntools/nbuild.md) for the command line options.
