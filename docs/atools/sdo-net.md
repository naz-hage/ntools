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

sdo.net v1.72.6 - Simple DevOps Operations by naz-hage (2020-2026)

Description:
  Simple DevOps Operations CLI tool for Azure DevOps and GitHub

Usage:
  sdo.net [command] [options]

Options:
  --verbose       Enable verbose output
  -?, -h, --help  Show help and usage information
  --version       Show version information

Commands:
  map       Show command mappings between sdo.net and native CLI tools
  auth      Verify authentication with GitHub or Azure DevOps
  pipeline  Pipeline/workflow management commands
  pr        Pull request operations
  repo      Repository management commands
  wi        Work item management commands
  user      User management commands for GitHub and Azure DevOps
```

## Command Reference

### map — CLI Mappings

Show how `sdo.net` commands map to native platform CLIs (`gh` for GitHub, `az` for Azure DevOps).

**Description:**
Useful for understanding equivalent commands and learning native CLI syntax.

**Usage:**
```bash
sdo.net map                              # Show all mappings
sdo.net map --platform gh               # GitHub mappings only
sdo.net map --platform azdo --all       # All Azure DevOps mappings
```

**Verbose Mode:**
With `--verbose`, displays exact native API commands for copy/paste.

---

### auth — Verify Authentication

Validate credentials are configured for your platform.

**Usage:**
```bash
sdo.net auth gh                  # Verify GitHub
sdo.net auth azdo               # Verify Azure DevOps
sdo.net auth gh --verbose       # Show token source details
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
  create   Create a new work item from markdown file
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
- `--top <N>` — Maximum results (default: 50)
- `--state <state>` — Filter by state
- `--type <type>` — Filter by type (PBI, Bug, Task, Spike, Epic)
- `--assigned-to <user>` — Filter by assignee
- `--assigned-to-me` — Your items
- `--area <path>` — Filter by area path (Azure DevOps)
- `--iteration <path>` — Filter by iteration (Azure DevOps)
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
- `--comments` — Include comments/discussion
- `-c` — Short alias for `--comments`
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
- `--file-path <path>` — Path to markdown file (required)
- `-f <path>` — Short alias
- `--dry-run` — Preview without creating
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
- `--state <state>` — New state
- `--title <text>` — New title
- `--assignee <user>` — New assignee
- `--description <text>` — New description
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
- `--message <text>` — Comment text (required)
- `--verbose` — Show mapping

---

### pr — Pull Request Operations

List, show, create, and manage pull requests on GitHub and Azure DevOps.

**Subcommands:**
- `list` — List pull requests
- `show` — Display PR details
- `create` — Create pull request
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
  create  Create a new pull request
  list    List pull requests
  show    Display pull request information
  status  Check pull request status
  update  Update pull request properties
```

#### pr list

List pull requests with optional filtering.

**Usage:**
```bash
sdo.net pr list                         # List open PRs
sdo.net pr list --state closed         # List closed PRs
sdo.net pr list --state merged         # List merged PRs
sdo.net pr list --top 20               # Limit to 20
sdo.net pr list --verbose              # Show API commands
```

**Options:**
- `--state <state>` — Filter: `open`, `closed`, `merged`
- `--top <N>` — Maximum results
- `--verbose` — Show mapping

#### pr show

Display pull request details.

**Usage:**
```bash
sdo.net pr show --id 12                # Show PR #12
sdo.net pr show --id 12 --verbose      # Show API command
```

**Options:**
- `--id <id>` — PR ID/number (required)
- `--verbose` — Show mapping

#### pr create

Create a new pull request.

**Usage:**
```bash
sdo.net pr create --title "Fix bug" --description "Details" --source feature-branch --target main
sdo.net pr create --title "Feature" --source dev --target main --draft
```

**Options:**
- `--title <text>` — PR title (required)
- `--description <text>` — PR description
- `--source <branch>` — Source branch
- `--target <branch>` — Target branch
- `--draft` — Create as draft (GitHub)
- `--verbose` — Show mapping

#### pr status

Check pull request status.

**Usage:**
```bash
sdo.net pr status --id 12              # Check PR status
sdo.net pr status --id 12 --verbose    # Show full details
```

**Options:**
- `--id <id>` — PR ID/number (required)
- `--verbose` — Show mapping

#### pr update

Update pull request properties.

**Usage:**
```bash
sdo.net pr update --id 12 --title "Updated title"
sdo.net pr update --id 12 --state closed
```

**Options:**
- `--id <id>` — PR ID/number (required)
- `--title <text>` — New title
- `--description <text>` — New description
- `--state <state>` — New state (`open`, `closed`)
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
  create      Create a new repository
  delete      Delete a repository
  list        List repositories
  show        Display repository information from current Git remote
```

#### repo list

List repositories for your organization.

**Usage:**
```bash
sdo.net repo list                       # List repos
sdo.net repo list --top 10             # Limit to 10
sdo.net repo list --org myorg          # Specific organization
sdo.net repo list --verbose            # Show API commands
```

**Options:**
- `--top <N>` — Maximum results (default: 50)
- `--org <org>` — Organization name
- `--verbose` — Show mapping

#### repo show

Display repository details from current Git remote.

**Usage:**
```bash
sdo.net repo show                      # Show current repo
sdo.net repo show --verbose            # Show API command
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
- `--description <text>` — Repository description
- `--private` — Make repository private
- `--verbose` — Show mapping

#### repo delete

Delete a repository.

**Usage:**
```bash
sdo.net repo delete                    # Delete (with prompt)
sdo.net repo delete --force            # Delete (no prompt)
sdo.net repo delete --force --verbose  # Show mapping
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
- `run` — Trigger pipeline run
- `status` — Check pipeline status
- `logs` — View pipeline logs
- `lastbuild` — Show last build/run
- `update` — Update pipeline
- `delete` — Delete pipeline

**Help:**
```
$ sdo.net pipeline --help

Description:
  Pipeline/workflow management commands

Usage:
  sdo.net pipeline [command] [options]

Commands:
  create     Create a new pipeline/workflow from YAML definition file
  delete     Delete a pipeline/workflow
  lastbuild  Show last build/run for a pipeline
  list       List pipelines/workflows
  logs       Display pipeline/workflow logs
  run        Trigger a pipeline run
  show       Display pipeline/workflow information
  status     Check pipeline run status
  update     Update a pipeline/workflow
```

#### pipeline list

List pipelines/workflows in repository.

**Usage:**
```bash
sdo.net pipeline list                   # List pipelines
sdo.net pipeline list --top 10          # Limit to 10
sdo.net pipeline list --verbose         # Show API commands
```

**Options:**
- `--top <N>` — Maximum results (default: 50)
- `--org <org>` — Organization (Azure DevOps)
- `--project <project>` — Project name (Azure DevOps)
- `--verbose` — Show mapping

#### pipeline show

Display pipeline/workflow details.

**Usage:**
```bash
sdo.net pipeline show --id 1234         # Show pipeline
sdo.net pipeline show --id 1234 --verbose  # Show API command
```

**Options:**
- `--id <id>` — Pipeline or workflow ID (required)
- `--verbose` — Show mapping

#### pipeline create

Create a new pipeline/workflow from YAML file.

**Usage:**
```bash
sdo.net pipeline create --file-path .github/workflows/ci.yml
sdo.net pipeline create .github/workflows/ci.yml --verbose
```

**Options:**
- `<file-path>` — Path to YAML definition file (required)
- `--verbose` — Show mapping

#### pipeline run

Trigger a pipeline run.

**Usage:**
```bash
sdo.net pipeline run --id 1234          # Trigger run
sdo.net pipeline run --id 1234 --verbose  # Show details
```

**Options:**
- `--id <id>` — Pipeline ID (required)
- `--verbose` — Show mapping

#### pipeline status

Check pipeline run status.

**Usage:**
```bash
sdo.net pipeline status --id 1234       # Check status
sdo.net pipeline status --id 1234 --verbose  # Show details
```

**Options:**
- `--id <id>` — Pipeline or run ID (required)
- `--verbose` — Show mapping

#### pipeline logs

View pipeline/workflow logs.

**Usage:**
```bash
sdo.net pipeline logs --id 1234         # Show logs
sdo.net pipeline logs --id 1234 --verbose  # Show logs with API
```

**Options:**
- `--id <id>` — Pipeline or run ID (required)
- `--verbose` — Show mapping and API commands

#### pipeline lastbuild

Show last build/run for pipeline.

**Usage:**
```bash
sdo.net pipeline lastbuild myworkflow       # Show last build
sdo.net pipeline lastbuild myworkflow --verbose  # Show details
```

**Options:**
- `<pipeline-name>` — Pipeline name (optional, uses current repo if not provided)
- `--verbose` — Show mapping

#### pipeline update

Update a pipeline/workflow.

**Usage:**
```bash
sdo.net pipeline update --id 1234 --file-path updated.yml
sdo.net pipeline update --id 1234 --file-path updated.yml --verbose
```

**Options:**
- `--id <id>` — Pipeline ID (required)
- `--file-path <path>` — Updated YAML file (required)
- `--verbose` — Show mapping

#### pipeline delete

Delete a pipeline/workflow.

**Usage:**
```bash
sdo.net pipeline delete --id 1234       # Delete (with prompt)
sdo.net pipeline delete --id 1234 --force  # Delete (no prompt)
```

**Options:**
- `<pipeline-id>` — Pipeline ID (optional, uses current repo if not provided)
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

