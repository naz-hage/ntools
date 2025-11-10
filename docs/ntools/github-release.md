nb.exe is also designed to help you create and manage GitHub releases. It also enables you to download release assets, such as files named in the format x.y.z.zip. The tool expects downloaded assets to be in a zip file named ${tag}.zip, where tag must be a valid tag in the repository created by the tool (e.g., ``x.y.z``).  checkout the [versioning](../versioning.md) section for more details.

## Requirements

### Repository Requirements
- The repository must have:
  - A GitHub token to create releases (required for private repos and write operations).
  - A GitHub owner to create releases (can be specified via command line).
  - A Git branch to create releases.
  - At least one Git tag prior to creating releases.

### Authentication Requirements
- **Public Repositories:** No authentication required for read operations
- **Private Repositories:** Authentication required for all operations
- **Write Operations:** Authentication always required (create releases, upload assets)

### GitHub Actions Workflow Example
Here is an example of how to set up authentication in a GitHub Actions workflow file. You can use either a personal access token or GitHub CLI authentication:

**Option 1: Personal Access Token (Recommended for CI/CD)**
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

**Option 2: GitHub CLI Authentication**
```yml
- name: Authenticate with GitHub CLI
  run: |
    gh auth login --with-token <<< ${{ secrets.GITHUB_TOKEN }}
  shell: bash

- name: Build using ntools
  run: |
    & "$env:ProgramFilesPath/nbuild/nb.exe" ${{ env.Build_Type }} -v ${{ env.Enable_Logging }}
  shell: pwsh
  working-directory: ${{ github.workspace }}
  env:
    OWNER: ${{ github.repository_owner }}
```

The above actions build, test, and create releases using the GitHubRelease tool and upload to GitHub.

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

## Repository Visibility-Based Authentication

**New Feature**: NTools now intelligently determines when authentication is required based on repository visibility and operation type.

### How It Works
- **Public Repositories:**
  - Read operations (listing releases, downloading assets) work without authentication
  - Write operations (creating releases, uploading assets) require authentication
- **Private Repositories:**
  - All operations require authentication
- **Unknown Visibility:**
  - The tool attempts to determine repository visibility via unauthenticated API calls
  - If visibility cannot be determined, authentication is required for safety

### Authentication Flow
1. Tool determines if the operation requires authentication based on repository visibility
2. If authentication is required, it tries available authentication methods in order:
   - `API_GITHUB_KEY` environment variable
   - GitHub CLI authentication (`gh auth token`)
   - Windows Credential Manager
3. If no valid authentication is found, clear error messages guide users to set up authentication

### Benefits
- **Simplified Usage:** Public repositories work without any authentication setup
- **Security:** Private repositories still require proper authentication
- **Flexibility:** Multiple authentication methods supported
- **User-Friendly:** Clear error messages when authentication is needed

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

## Manifests and private GitHub release assets

- `nb download` and `nb install` can consume JSON manifests that reference GitHub release assets (for example `WebDownloadFile` entries that point at `https://github.com/OWNER/REPO/releases/download/TAG/asset.zip`).
- For private repositories, unauthenticated requests to the public `releases/download` URL will return 404. `nb` will attempt an authenticated fallback using the GitHub API when a token is available.

How authentication is provided
- **Multiple authentication methods supported** (tried in order of preference):
  1. `API_GITHUB_KEY` environment variable
  2. GitHub CLI authentication (`gh auth token`)
  3. Windows Credential Manager (`GitHubRelease`/`API_GITHUB_KEY`)
- **For private repositories:** Authentication is required for all operations
- **For public repositories:** Authentication is only required for write operations (creating releases)
- **GitHub CLI Setup:**
  ```bash
  # Install GitHub CLI if not already installed
  winget install --id GitHub.cli
  
  # Authenticate with GitHub
  gh auth login
  ```
- **Environment Variable Setup:**
  ```powershell
  $env:API_GITHUB_KEY = 'ghp_XXXX'
  .\Release\nb.exe install --json private-repo.json --verbose
  ```

Behavior
- On download failure (404) for a GitHub release URL, `nb` will parse the owner/repo/tag and call into `GitHubRelease.ReleaseService.DownloadAssetByName(tag, assetName, dest)` which uses the GitHub API to find the asset and download it using the authenticated asset endpoint. This approach supports private repositories when the token has appropriate scopes (typically `repo` and `releases`).

Notes and troubleshooting
- **Authentication Methods:** The tool supports multiple authentication methods. If one method fails, it will try the next available method.
- **Token Scopes:** Ensure your authentication method has appropriate scopes:
  - For private repositories: `repo` scope (includes releases access)
  - For public repositories: `public_repo` scope for write operations
- **GitHub CLI:** If using `gh auth login`, ensure the token has the correct scopes by running `gh auth status` to verify.
- **Environment Variables:** The `API_GITHUB_KEY` takes precedence over other methods when set.
- **For enterprise GitHub installations with custom hosts:** The `GitHubRelease` helpers must be configured to use the appropriate API base URL.
