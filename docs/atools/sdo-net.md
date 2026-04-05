# sdo.net - Simple DevOps Operations Tool (C# Version)

Unified CLI for managing work items, pull requests, pipelines, and repositories across **GitHub** and **Azure DevOps**.

## Table of Contents

- [Overview](#overview)
- [Prerequisites](#prerequisites)
- [Key Features](#key-features)
- [Main Help](#main-help)
- [Command Reference](#command-reference)
  - [map — CLI Mappings](#map--cli-mappings)
  - [auth — Verify Authentication](#auth--verify-authentication)
  - [wi — Work Item Management](#wi--work-item-management)
  - [pr — Pull Request Operations](#pr--pull-request-operations)
  - [pipeline — Pipeline/Workflow Management](#pipeline--pipelineworkflow-management)
  - [repo — Repository Management](#repo--repository-management)
  - [user — User Management](#user--user-management)
- [Troubleshooting](#troubleshooting)

## Overview

`sdo.net` (Simple DevOps Operations) is a C# CLI tool providing unified operations across Azure DevOps and GitHub. It:
- Automatically detects your platform from Git remote configuration
- Translates work item states and operations between GitHub and Azure DevOps
- Maps `sdo.net` commands to native platform CLIs for reference and learning
- Provides consistent cross-platform tooling for teams

## Prerequisites

### Authentication

**GitHub**:
- Install GitHub CLI: `gh auth login`
- Or set `GITHUB_TOKEN` environment variable
- Or store credentials in Windows Credential Manager under `gh:github.com:`

**Azure DevOps**:
- Set `AZURE_DEVOPS_PAT` environment variable (Personal Access Token)
- Or store credentials in Windows Credential Manager under `GitAzureDevOps`
- PAT must have scopes: Work Item Read, Code Read

### Git Repository Context

The tool auto-detects your platform from Git remote:
- **GitHub**: `https://github.com/owner/repo`
- **Azure DevOps**: `https://dev.azure.com/org/project/_git/repo`

Verify your remote:
```bash
git remote -v
# origin  https://github.com/naz-hage/ntools (fetch)  -> Uses GitHub
# origin  https://dev.azure.com/org/project/_git/repo (fetch)  -> Uses Azure DevOps
```

## Key Features

- **Work Item Management** — Create, list, show, update work items with cross-platform state translation
- **Pull Requests** — List, show, create pull requests
- **Pipelines/Workflows** — Create, list, show, run, view logs for GitHub Actions and Azure Pipelines
- **Repositories** — List and inspect repositories
- **Users & Permissions** — List users, check permissions, search
- **Platform Auto-Detection** — Automatically detects GitHub or Azure DevOps from Git remote
- **CLI Mapping** — Shows sdo.net command equivalents in native CLIs (`gh`, `az`)
- **State Translation** — Automatically maps work item states between platforms
- **Clean Output** — Emoji-enhanced tables and concise error messages

## Main Help

```
$ sdo.net --help

sdo.net v1.73.7 - Simple DevOps Operations by naz-hage (2020-2026)

Description:
  Simple DevOps Operations CLI tool for Azure DevOps and GitHub

Usage:
  sdo.net [command] [options]

Options:
  --verbose       Enable verbose output
  -?, -h, --help  Show help and usage information
  --version       Show version information

Commands:
  map       Show command mappings between SDO and native CLI tools
  auth      Verify authentication with GitHub or Azure DevOps
  pipeline  Pipeline/workflow management commands (create, show, list, run, status, logs, delete, lastbuild, update)
  pr        Pull request operations
  repo      Repository management commands
  wi        Work item management commands
  user      User management commands for GitHub and Azure DevOps
```

## Command Reference

### map — CLI Mappings

Show how `sdo.net` commands map to native platform CLIs (`gh` for GitHub, `az` for Azure DevOps).

**Usage:**
```bash
sdo.net map                              # Show all mappings
sdo.net map --platform gh               # GitHub mappings only
sdo.net map --platform azdo             # Azure DevOps mappings only
sdo.net map --all                       # Show all mappings for both platforms
sdo.net map --verbose                   # Show with verbose output
```

**Help:**
```
$ sdo.net map --help

Description:
  Show command mappings between SDO and native CLI tools

Usage:
  sdo.net map [options]

Options:
  --platform <platform>  Platform to show mappings for (gh=github, azdo=azure-devops, leave empty for auto-detect)
  --all                  Show all mappings for both platforms
  -?, -h, --help         Show help and usage information
```

With `--verbose`, displays exact native API commands for copy/paste.

---

### auth — Verify Authentication

Validate credentials are configured for your platform.

**Subcommands:**
- `gh` — Verify GitHub authentication
- `azdo` — Verify Azure DevOps authentication

**Help:**
```
$ sdo.net auth --help

Description:
  Verify authentication with GitHub or Azure DevOps

Usage:
  sdo.net auth [command] [options]

Options:
  --verbose       Enable verbose output
  -?, -h, --help  Show help and usage information

Commands:
  gh    Verify GitHub authentication
  azdo  Verify Azure DevOps authentication
```

**Usage:**
```bash
sdo.net auth gh                  # Verify GitHub
sdo.net auth azdo               # Verify Azure DevOps
sdo.net auth gh --verbose       # Show token source details
sdo.net auth azdo --verbose     # Show token source details
```

**Success Output:**
```
✓ GitHub authentication successful
```

---

### wi — Work Item Management

Create, list, show, update work items and add comments. Supports GitHub issues and Azure DevOps work items with automatic state translation.

**Subcommands:**
- `list` — List work items with filtering
- `show` — Display work item details
- `create` — Create new work item from markdown file
- `update` — Update work item properties
- `comment` — Add comment to work item

**Help:**
```
$ sdo.net wi --help

Description:
  Work item management commands

Usage:
  sdo.net wi [command] [options]

Commands:
  comment  Add comment to a work item
  create   Create a new work item
  list     List work items with optional filtering
  show     Display detailed work item information
  update   Update work item properties
```

#### wi list

List work items with optional filtering. By default, shows **active** items (excludes closed/done).

**Usage:**
```bash
sdo.net wi list                                    # List active items
sdo.net wi list --top 20                           # Limit to 20 items
sdo.net wi list --state Done                       # Show Done items
sdo.net wi list --state "In Progress" --top 10    # Show In Progress, limit 10
sdo.net wi list --assigned-to user@example.com    # Filter by assignee
sdo.net wi list --assigned-to-me                  # Your items
sdo.net wi list --type PBI                         # Filter by type (Azure DevOps)
sdo.net wi list --area "Project\Area"             # Filter by area (Azure DevOps)
sdo.net wi list --iteration "Project\Sprint 1"    # Filter by iteration (Azure DevOps)
sdo.net wi list --verbose                         # Show API commands
```

**Valid States:**
- Azure DevOps: `New`, `Approved`, `Committed`, `Done`, `To Do`, `In Progress` (case-insensitive)
- GitHub: `open`, `closed` (auto-translated)

**Options:**
- `--type <type>` — Filter by work item type (PBI, Bug, Task, Spike, Epic)
- `--state <state>` — Filter by state (New, Approved, Committed, Done, To Do, In Progress)
- `--assigned-to <assigned-to>` — Filter by assigned user (email or display name)
- `--assigned-to-me` — Filter by work items assigned to current user
- `--area <area>` — Filter by area path (Azure DevOps only). Example: 'Project\Area\SubArea'
- `--iteration <iteration>` — Filter by iteration (Azure DevOps only). Example: 'Project\Sprint 1'
- `--top <top>` — Maximum number of items to return (default: 50)
- `--verbose` — Show mapping and diagnostics

**Output Example:**
```
gh Issues (18 found):
---
#      Title                                State      Labels                Assignee
---
#246   Phase 6: Deployment & Transition    OPEN       backlog, sdo-net      Unassigned
#245   Phase 5: Testing & Validation       OPEN       backlog, testing      Unassigned
---
Summary:
  OPEN: 18
```

#### wi show

Display full details of a work item.

**Usage:**
```bash
sdo.net wi show --id 243                # Show item details
sdo.net wi show --id 243 --comments     # Include comments
sdo.net wi show --id 243 --verbose      # Show API command
```

**Options:**
- `--id <id>` — Work item ID (required)
- `-c, --comments` — Show comments/discussion
- `--verbose` — Show mapping

#### wi create

Create new work item from markdown file.

**Usage:**
```bash
sdo.net wi create --file-path work-item.md              # Create
sdo.net wi create -f work-item.md                       # Short option
sdo.net wi create --file-path work-item.md --dry-run   # Preview
sdo.net wi create --file-path work-item.md --verbose   # Show mapping
```

**Options:**
- `-f, --file-path <file-path>` — Path to markdown file containing work item details (required)
- `--dry-run` — Parse and preview work item creation without creating it
- `--verbose` — Show mapping

**Markdown Format:**
```markdown
# Work Item Title

This is the description.

## Acceptance Criteria
- Criterion 1
- Criterion 2
- Criterion 3
```

Optional YAML metadata:
```markdown
---
work_item_type: PBI
assignee: user@example.com
labels: "feature, backlog"
target: "github"  # or "azdo"
---

# Title
...
```

#### wi update

Update work item properties.

**Usage:**
```bash
sdo.net wi update --id 243 --state Done                    # Update state
sdo.net wi update --id 243 --title "New Title"            # Update title
sdo.net wi update --id 243 --assignee user@example.com    # Change assignee
sdo.net wi update --id 243 --state Done --title "Title"   # Multiple fields
sdo.net wi update --id 243 --state done                   # Case-insensitive
```

**Options:**
- `--id <id>` — Work item ID (required)
- `--title <title>` — Update work item title
- `--state <state>` — Update work item state
- `--assignee <assignee>` — Update assigned user
- `--description <description>` — Update work item description
- `--verbose` — Show mapping

**Valid States** (case-insensitive):
- `New`, `Approved`, `Committed`, `Done`, `To Do`, `In Progress`

#### wi comment

Add comment to work item.

**Usage:**
```bash
sdo.net wi comment --id 243 --message "This is my comment"
sdo.net wi comment --id 243 --message "Looks good!" --verbose
```

**Options:**
- `--id <id>` — Work item ID (required)
- `--message <message>` — Comment message (required)
- `--verbose` — Show mapping

---

### pr — Pull Request Operations

List, show, create, and manage pull requests on GitHub and Azure DevOps. `pr create` uses markdown files similar to `wi create`.

**Subcommands:**
- `list` — List pull requests
- `show` — Display PR details
- `create` — Create pull request from markdown file
- `status` — Check PR status
- `update` — Update PR properties

**Help:**
```
$ sdo.net pr --help

Description:
  Pull request operations

Usage:
  sdo.net pr [command] [options]

Commands:
  create          Create a pull request from markdown file
  list            List pull requests in the current repository
  show <pr-id>    Show detailed information about a pull request
  status <pr-id>  Show status of a pull request
  update          Update an existing pull request
```

#### pr create

Create a new pull request from markdown file.

**Usage:**
```bash
sdo.net pr create --file pr.md                        # Create from file
sdo.net pr create -f pr.md                            # Short option
sdo.net pr create --file pr.md --work-item 243        # Link to work item
sdo.net pr create --file pr.md --draft                # Create as draft
sdo.net pr create --file pr.md --dry-run              # Preview
sdo.net pr create --file pr.md --verbose              # Show mapping
```

**Options:**
- `-f, --file <file>` — Path to markdown file (required)
- `--work-item <work-item>` — Work item ID to link to PR
- `--draft` — Create as draft pull request
- `--dry-run` — Preview without creating
- `--verbose` — Show mapping

#### pr list

List pull requests with optional filtering.

**Usage:**
```bash
sdo.net pr list                         # List active PRs (default)
sdo.net pr list --status closed         # List closed PRs
sdo.net pr list --status merged         # List merged PRs
sdo.net pr list --top 20               # Limit to 20
sdo.net pr list --verbose              # Show API commands
```

**Options:**
- `--status <status>` — Filter PRs by status (default: active)
- `--top <top>` — Maximum number of PRs to show (default: 10)
- `--verbose` — Show mapping

#### pr show

Display pull request details.

**Usage:**
```bash
sdo.net pr show 12                # Show PR #12
sdo.net pr show 12 --verbose      # Show API command
```

**Options:**
- `<pr-id>` — Pull request number/ID (required)
- `--verbose` — Show mapping

#### pr status

Check pull request status.

**Usage:**
```bash
sdo.net pr status 12              # Check PR status
sdo.net pr status 12 --verbose    # Show full details
```

**Options:**
- `<pr-id>` — Pull request number/ID (required)
- `--verbose` — Show mapping

#### pr update

Update pull request properties.

**Usage:**
```bash
sdo.net pr update --pr-id 12 --title "Updated title"
sdo.net pr update --pr-id 12 --file updated.md
sdo.net pr update --pr-id 12 --status closed
sdo.net pr update --pr-id 12 --title "New title" --status merged
```

**Options:**
- `--pr-id <pr-id>` — PR ID to update (required)
- `-f, --file <file>` — Markdown file with updated details
- `-t, --title <title>` — New title
- `--status <status>` — New status
- `--verbose` — Show mapping

---

### repo — Repository Management

Create, delete, list, and inspect repositories for your organization.

**Subcommands:**
- `list` — List repositories
- `show` — Display repo details
- `create` — Create new repository
- `delete` — Delete repository

**Help:**
```
$ sdo.net repo --help

Description:
  Repository management commands

Usage:
  sdo.net repo [command] [options]

Commands:
  create <name>  Create a new repository
  delete         Delete a repository
  list           List repositories
  show           Display repository information from current Git remote
```

#### repo list

List repositories for your organization.

**Usage:**
```bash
sdo.net repo list                       # List repos
sdo.net repo list --top 10              # Limit to 10
sdo.net repo list --verbose             # Show API commands
```

**Options:**
- `--top <top>` — Return top N repositories
- `--verbose` — Show mapping

#### repo show

Display repository information from current Git remote.

**Usage:**
```bash
sdo.net repo show                       # Show current repo
sdo.net repo show --verbose             # Show API command
```

**Options:**
- `--verbose` — Show mapping

#### repo create

Create a new repository.

**Usage:**
```bash
sdo.net repo create myrepo                          # Create repo
sdo.net repo create myrepo --description "My repo"  # With description
sdo.net repo create myrepo --private                # Make private
sdo.net repo create myrepo --private --verbose      # With mapping
```

**Options:**
- `<name>` — Repository name (required)
- `--description <description>` — Repository description
- `--private` — Make repository private
- `--verbose` — Show mapping

#### repo delete

Delete a repository.

**Usage:**
```bash
sdo.net repo delete                     # Delete (with prompt)
sdo.net repo delete --force             # Delete (no prompt)
sdo.net repo delete --force --verbose   # Show mapping
```

**Options:**
- `--force` — Skip confirmation prompt
- `--verbose` — Show mapping

---

### pipeline — Pipeline/Workflow Management

Manage GitHub Actions workflows and Azure Pipelines. **Read-only operations** are safe; **write operations** require permissions.

**Subcommands:**
- `list` — List pipelines/workflows
- `show` — Display pipeline details
- `create` — Create pipeline from YAML file
- `run` — Execute/trigger a pipeline
- `status` — Query pipeline build/run status
- `logs` — Retrieve pipeline execution logs
- `lastbuild` — Show last build/run
- `update` — Update pipeline configuration
- `delete` — Delete pipeline/workflow

**Help:**
```
$ sdo.net pipeline --help

Description:
  Pipeline/workflow management commands (create, show, list, run, status, logs, delete, lastbuild, update)

Usage:
  sdo.net pipeline [command] [options]

Commands:
  create <file-path>          Create a new pipeline/workflow from YAML definition file
  delete <pipeline-id>        Delete a pipeline/workflow
  lastbuild <pipeline-name>   Show last build/run for a pipeline
  list                        List pipelines/workflows
  logs <build-id>             Retrieve pipeline execution logs
  run <pipeline-name>         Execute/trigger a pipeline
  show <pipeline-id-or-name>  Display pipeline/workflow details
  status <build-id>           Query pipeline build/run status
  update                      Update pipeline configuration
```

#### pipeline list

List pipelines/workflows in repository.

**Usage:**
```bash
sdo.net pipeline list                   # List pipelines
sdo.net pipeline list --repo myrepo     # Filter by repository
sdo.net pipeline list --all             # Show all pipelines in project
sdo.net pipeline list --verbose         # Show API commands
```

**Options:**
- `--repo <repo>` — Filter by repository name
- `--all` — Show all pipelines in the project
- `--verbose` — Show mapping

#### pipeline show

Display pipeline/workflow details.

**Usage:**
```bash
sdo.net pipeline show 1234              # Show pipeline by ID
sdo.net pipeline show myworkflow        # Show pipeline by name
sdo.net pipeline show --verbose         # Show API command
```

**Options:**
- `<pipeline-id-or-name>` — Pipeline ID or name (optional; Azure DevOps only)
- `--verbose` — Show mapping

#### pipeline create

Create a new pipeline/workflow from YAML file.

**Usage:**
```bash
sdo.net pipeline create .github/workflows/ci.yml
sdo.net pipeline create .github/workflows/ci.yml --verbose
```

**Options:**
- `<file-path>` — Path to YAML definition file (required)
- `--verbose` — Show mapping

#### pipeline run

Execute/trigger a pipeline.

**Usage:**
```bash
sdo.net pipeline run myworkflow         # Trigger run
sdo.net pipeline run myworkflow -b main # Run on specific branch
sdo.net pipeline run myworkflow --branch develop --verbose
```

**Options:**
- `<pipeline-name>` — Pipeline name (optional, uses current repo if not provided)
- `-b, --branch <branch>` — Branch to run the pipeline on
- `--verbose` — Show mapping

#### pipeline status

Query pipeline build/run status.

**Usage:**
```bash
sdo.net pipeline status                 # Show latest build status
sdo.net pipeline status 1234            # Check specific build
sdo.net pipeline status 1234 --verbose  # Show details
```

**Options:**
- `<build-id>` — Build/run ID (optional, shows latest if not provided)
- `--verbose` — Show mapping

#### pipeline logs

Retrieve pipeline execution logs.

**Usage:**
```bash
sdo.net pipeline logs                     # Show latest logs
sdo.net pipeline logs 1234                # Show logs for specific build
sdo.net pipeline logs --build-id 1234     # Explicit build ID
sdo.net pipeline logs 1234 --verbose      # Show with details
```

**Options:**
- `<build-id>` — Build/run ID (optional, shows latest if not provided)
- `--build-id <build-id>` — Build/run ID
- `--verbose` — Show mapping and API commands

#### pipeline lastbuild

Show last build/run for pipeline.

**Usage:**
```bash
sdo.net pipeline lastbuild              # Last build for current repo
sdo.net pipeline lastbuild myworkflow   # Last build for specific pipeline
sdo.net pipeline lastbuild myworkflow --verbose  # Show details
```

**Options:**
- `<pipeline-name>` — Pipeline name (optional, uses current repo if not provided)
- `--verbose` — Show mapping

#### pipeline update

Update pipeline configuration.

**Usage:**
```bash
sdo.net pipeline update --file updated.yml
sdo.net pipeline update --file updated.yml --pipeline myworkflow
sdo.net pipeline update --file updated.yml --message "Update CI config" --branch main
```

**Options:**
- `--file <file>` — Path to pipeline/workflow YAML file to update (required)
- `--pipeline <pipeline>` — Pipeline/workflow ID or name (optional)
- `--message <message>` — Commit message to use when updating
- `--branch <branch>` — Branch to push the update to
- `--verbose` — Show mapping

#### pipeline delete

Delete a pipeline/workflow.

**Usage:**
```bash
sdo.net pipeline delete 1234            # Delete (with prompt)
sdo.net pipeline delete 1234 --force    # Delete (no prompt)
sdo.net pipeline delete 1234 --force --verbose
```

**Options:**
- `<pipeline-id>` — Pipeline/workflow ID or name (optional, uses current repo if not provided)
- `--force` — Skip confirmation prompt
- `--verbose` — Show mapping

---

### user — User Management

List users, search, and check permissions.

**Subcommands:**
- `list` — List users
- `show` — Display user details
- `search` — Search users
- `permissions` — Show user permissions

**Help:**
```
$ sdo.net user --help

Description:
  User management commands

Usage:
  sdo.net user [command] [options]

Commands:
  list         List users
  permissions  Show user permissions
  search       Search for users
  show         Display user information
```

#### user list

List users in organization.

**Usage:**
```bash
sdo.net user list                      # List users
sdo.net user list --top 50             # Limit to 50
sdo.net user list --verbose            # Show API commands
```

**Options:**
- `--top <N>` — Maximum results (default: 50)
- `--verbose` — Show mapping

#### user show

Display user details.

**Usage:**
```bash
sdo.net user show --login naz-hage         # Show GitHub user
sdo.net user show --login naz-hage --verbose  # Show API command
```

**Options:**
- `--login <login>` — GitHub login or Azure identity (required)
- `--verbose` — Show mapping

#### user search

Search for users.

**Usage:**
```bash
sdo.net user search --query "naz"              # Search for users
sdo.net user search --query "naz" --top 20     # Limit to 20
sdo.net user search --query "naz" --verbose    # Show API command
```

**Options:**
- `--query <term>` — Search term (required)
- `--top <N>` — Maximum results (default: 50)
- `--verbose` — Show mapping

#### user permissions

Show user permissions.

**Usage:**
```bash
sdo.net user permissions --user naz-hage          # Show permissions
sdo.net user permissions --user naz-hage --verbose  # Show detailed
```

**Options:**
- `--user <id>` — User identifier (required)
- `--verbose` — Show detailed mapping and API calls

---

## Troubleshooting

### Authentication Errors

**Error:** `No authentication token found. Run 'sdo auth' to setup authentication.`

**Causes:**
- GitHub: `GITHUB_TOKEN` not set, `gh` not authenticated, or no Windows Credential Manager entry
- Azure DevOps: `AZURE_DEVOPS_PAT` not set or credentials missing

**Solution:**

GitHub:
```bash
# Verify GitHub CLI is installed and authenticated
gh auth status

# Re-authenticate if needed
gh auth login

# Or set token explicitly
$env:GITHUB_TOKEN = "your_github_token"
```

Azure DevOps:
```bash
# Set personal access token
$env:AZURE_DEVOPS_PAT = "your_azure_pat"
```

### Platform Detection Errors

**Error:** `Could not determine GitHub repository from Git remote`

**Causes:**
- Git remote not configured
- Remote URL doesn't match GitHub or Azure DevOps patterns

**Solution:**
```bash
# Check remote configuration
git remote -v

# Add missing remote
git remote add origin https://github.com/owner/repo
# or
git remote add origin https://dev.azure.com/org/project/_git/repo
```

### State Validation Errors

**Error:** `Failed to update work item. Supported states: New, Approved, Committed, Done, To Do, In Progress`

**Causes:**
- Invalid state name (typo or casing)
- Project has custom state configuration

**Solution:**
```bash
# Use valid state with exact casing (or lowercase)
sdo.net wi update --id 170 --state Done          # Correct
sdo.net wi update --id 170 --state "In Progress"  # Correct
sdo.net wi update --id 170 --state done           # Also works (case-insensitive)
```

Valid states:
- Azure DevOps: `New`, `Approved`, `Committed`, `Done`, `To Do`, `In Progress`
- GitHub: `open`, `closed` (auto-translated)

### Azure DevOps Permissions Errors

**Error:** `Unauthorized. Insufficient permissions.`

**Causes:**
- PAT token doesn't have required scopes
- Work items feature disabled in project

**Solution:**

Ensure PAT has these scopes:
- Work Item > Read
- Code > Read

For write operations:
- Work Item > Write
- Code > Write

```bash
# Create new PAT with required scopes
# https://dev.azure.com/org/_usersSettings/tokens
```

### Common Option Mistakes

**Using invalid option names:**
```bash
# ❌ Wrong
sdo.net wi list --assigned-to-me-filter

# ✓ Correct
sdo.net wi list --assigned-to-me
```

**Forgetting required flags:**
```bash
# ❌ Missing --id for show
sdo.net wi show

# ✓ Correct
sdo.net wi show --id 243
```

**Mixing GitHub and Azure DevOps states:**
```bash
# ❌ GitHub state on Azure DevOps repo
git remote set-url origin https://dev.azure.com/org/project/_git/repo
sdo.net wi update --id 243 --state closed  # Won't work

# ✓ Correct
sdo.net wi update --id 243 --state Done
```

