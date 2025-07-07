# ntools PowerShell Scripts for Azure DevOps Repositories

This folder contains a collection of PowerShell scripts and supporting files for managing Azure DevOps repositories. Each script is designed to automate a specific repository management task, such as archiving, checking existence, deleting, listing permissions, setting read-only access, and listing associated pipelines.

## File Descriptions

### PowerShell Scripts

- **repo-archive.ps1**  
  Archives a repository by moving it from a source Azure DevOps project to a target project. Performs a mirror clone and push, preserving all branches, tags, and history. Reads configuration from `repo-archive.param`.

- **repo-check.ps1**  
  Checks if a specified repository exists in Azure DevOps projects. Can search across all projects or a specific project. Reads configuration from `repo-check.param`.

- **repo-delete.ps1**  
  Deletes a specified repository from an Azure DevOps project, with safety checks and confirmation prompts. Reads configuration from `repo-delete.param`.

- **repo-list-permissions.ps1**  
  Lists permissions for a specific repository in an Azure DevOps project using the REST API. Reads configuration from `repo-list-permissions.param`.

- **repo-read-only.ps1**  
  Sets a repository to read-only for a specific user by denying write permissions. Reads configuration from `repo-read-only.param`.

- **repos-pipelines.ps1**  
  Lists all repositories in a project and their associated YAML pipelines. Reads configuration from `repos-pipelines.param`.

- **repos.psm1**  
  PowerShell module containing shared functions for repository management, such as checking if a repository exists in a project.

### Parameter and Template Files

- **repo-archive.param** / **repo-archive-template.param**  
  JSON parameter file and template for `repo-archive.ps1`.

- **repo-check.param** / **repo-check-template.param**  
  JSON parameter file and template for `repo-check.ps1`.

- **repo-delete.param** / **repo-delete-template.param**  
  JSON parameter file and template for `repo-delete.ps1`.

- **repo-list-permissions.param** / **repo-list-permissions-template.param**  
  JSON parameter file and template for `repo-list-permissions.ps1`.

### Log Files

- **repo-archive.log**  
  Log output from `repo-archive.ps1`.

- **repo-check.log**  
  Log output from `repo-check.ps1`.

- **repo-delete.log**  
  Log output from `repo-delete.ps1`.

- **repo-list-permissions.log**  
  Log output from `repo-list-permissions.ps1`.

### Other Files

- **import.ps1**  
  (Not described in detail; likely used for importing or initializing resources.)

## Usage

Each script can be run directly in PowerShell. Most scripts accept a `-paramFile` argument to specify a custom parameter file. See the comments at the top of each script for usage examples and required parameters.

---

*This README was generated automatically to describe the contents and purpose of each file in this folder.*
