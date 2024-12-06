- GitHubRelease is a tool that allows you to create and manage GitHub releases from the command line. It simplifies the process of creating and managing releases, making it easier to publish your software updates on GitHub.
  - The repository must have a .net solution file and one .net project to build the project.
  - The repository must have
    - GitHub token to create releases.
    - GitHub owner to create releases.
    - a git branch to create releases.
    - a git at least one tag prior to create releases.
  - A Repository secret [token](#create-a-github-token) named `API_GITHUB_KEY` must be added to the GitHub repository Secrets and variables
  - Here an example how to add env in the GitHub actions workflow file 
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
  - The checkout a branch must be specified before running the the tool. Here an example how to checkout a branch in the GitHub actions workflow file

```yml
- name: Checkout Repository
  uses: actions/checkout@v4
  with:
    token: ${{ secrets.API_GITHUB_KEY }}
    fetch-depth: 0
    ref: ${{ github.event.pull_request.head.ref }}
    repository: ${{ github.event.pull_request.head.repo.full_name }}
```

- When `nb stage` runs successfully, the tool creates a stage release. This release is tagged with the next tag release number, and the release notes include the commits since the last stage or prod tag. The API token from the repository secrets is used to create this release.  The release package is uploaded to the release. The release is also tagged with the next stage release number.

- When `nb prod` runs successfully, the tool creates a production release. This release is also tagged with the next prod release, and the release notes include the commits since the last production tag. All previous stage releases are deleted. The API token from the repository secrets is used to create this release. The release package is uploaded to the release. The release is also tagged with the next prod release number. All previous stage releases are deleted.

# Create a GitHub token

- Follow the How to create a GitHub token [link](https://docs.github.com/en/github/authenticating-to-github/keeping-your-account-and-data-secure/creating-a-personal-access-token)
- The access token must have the following permissions:
     - Under **Repository permissions**, set the following:
        - **Contents**: `Read and write`
        - **Metadata**: `Read-only`
        - **Actions**: `Read and write` (if needed)
        - **Packages**: `Read and write` (if needed)
      - Under **Workflow permissions**, set the following:
        - **Workflows**: `Read and write` (if needed)
      - Under **Release permissions**, set the following:
        - **Releases**: `Read and write`
  
# GitHubRelease.exe command line options:
 ```-c create -r <repo name> -t <tag Version> -b $(GitBranch) -p <Release Package>```
 
   - `-c` create : create a release
  - `-r` <repo name> : repository name
  - `-t` <tag Version> : tag version
  - `-b` <git Branch> : git branch
  - `-p` <Release Package> : release package