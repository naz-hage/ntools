# sdo - Simple DevOps Operations Tool (C# Version)

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

`sdo` (Simple DevOps Operations) is a C# CLI tool providing unified operations across Azure DevOps and GitHub. It:
- Automatically detects your platform from Git remote configuration
- Translates work item states and operations between GitHub and Azure DevOps
- Maps `sdo` commands to native platform CLIs for reference and learning
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
- **CLI Mapping** — Shows sdo command equivalents in native CLIs (`gh`, `az`)
- **State Translation** — Automatically maps work item states between platforms
- **Clean Output** — Emoji-enhanced tables and concise error messages

## Main Help

```
$ sdo --help

sdo v1.73.7 - Simple DevOps Operations by naz-hage (2020-2026)

Description:
  Simple DevOps Operations CLI tool for Azure DevOps and GitHub

Usage:
  sdo [command] [options]

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

Show how `sdo` commands map to native platform CLIs (`gh` for GitHub, `az` for Azure DevOps).

**Usage:**
```bash
sdo map                              # Show all mappings
sdo map --platform gh               # GitHub mappings only
sdo map --platform azdo             # Azure DevOps mappings only
sdo map --all                       # Show all mappings for both platforms
sdo map --verbose                   # Show with verbose output
```

**Help:**
```
$ sdo map --help

Description:
  Show command mappings between SDO and native CLI tools

Usage:
  sdo map [options]

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
$ sdo auth --help

Description:
  Verify authentication with GitHub or Azure DevOps

Usage:
  sdo auth [command] [options]

Options:
  --verbose       Enable verbose output
  -?, -h, --help  Show help and usage information

Commands:
  gh    Verify GitHub authentication
  azdo  Verify Azure DevOps authentication
```

**Usage:**
```bash
sdo auth gh                  # Verify GitHub
sdo auth azdo               # Verify Azure DevOps
sdo auth gh --verbose       # Show token source details
sdo auth azdo --verbose     # Show token source details
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
$ sdo wi --help

Description:
  Work item management commands

Usage:
  sdo wi [command] [options]

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
sdo wi list                                    # List active items
sdo wi list --top 20                           # Limit to 20 items
sdo wi list --state Done                       # Show Done items
sdo wi list --state "In Progress" --top 10    # Show In Progress, limit 10
sdo wi list --assigned-to user@example.com    # Filter by assignee
sdo wi list --assigned-to-me                  # Your items
sdo wi list --type PBI                         # Filter by type (Azure DevOps)
sdo wi list --area "Project\Area"             # Filter by area (Azure DevOps)
sdo wi list --iteration "Project\Sprint 1"    # Filter by iteration (Azure DevOps)
sdo wi list --verbose                         # Show API commands
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
sdo wi show --id 243                # Show item details
sdo wi show --id 243 --comments     # Include comments
sdo wi show --id 243 --verbose      # Show API command
```

**Options:**
- `--id <id>` — Work item ID (required)
- `-c, --comments` — Show comments/discussion
- `--verbose` — Show mapping

#### wi create

Create new work item from markdown file.

**Usage:**
```bash
sdo wi create --file-path work-item.md              # Create
sdo wi create -f work-item.md                       # Short option
sdo wi create --file-path work-item.md --dry-run   # Preview
sdo wi create --file-path work-item.md --verbose   # Show mapping
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
sdo wi update --id 243 --state Done                    # Update state
sdo wi update --id 243 --title "New Title"            # Update title
sdo wi update --id 243 --assignee user@example.com    # Change assignee
sdo wi update --id 243 --state Done --title "Title"   # Multiple fields
sdo wi update --id 243 --state done                   # Case-insensitive
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
sdo wi comment --id 243 --message "This is my comment"
sdo wi comment --id 243 --message "Looks good!" --verbose
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
$ sdo pr --help

Description:
  Pull request operations

Usage:
  sdo pr [command] [options]

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
sdo pr create --file pr.md                        # Create from file
sdo pr create -f pr.md                            # Short option
sdo pr create --file pr.md --work-item 243        # Link to work item
sdo pr create --file pr.md --draft                # Create as draft
sdo pr create --file pr.md --dry-run              # Preview
sdo pr create --file pr.md --verbose              # Show mapping
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
sdo pr list                         # List active PRs (default)
sdo pr list --status closed         # List closed PRs
sdo pr list --status merged         # List merged PRs
sdo pr list --top 20               # Limit to 20
sdo pr list --verbose              # Show API commands
```

**Options:**
- `--status <status>` — Filter PRs by status (default: active)
- `--top <top>` — Maximum number of PRs to show (default: 10)
- `--verbose` — Show mapping

#### pr show

Display pull request details.

**Usage:**
```bash
sdo pr show 12                # Show PR #12
sdo pr show 12 --verbose      # Show API command
```

**Options:**
- `<pr-id>` — Pull request number/ID (required)
- `--verbose` — Show mapping

#### pr status

Check pull request status.

**Usage:**
```bash
sdo pr status 12              # Check PR status
sdo pr status 12 --verbose    # Show full details
```

**Options:**
- `<pr-id>` — Pull request number/ID (required)
- `--verbose` — Show mapping

#### pr update

Update pull request properties.

**Usage:**
```bash
sdo pr update --pr-id 12 --title "Updated title"
sdo pr update --pr-id 12 --file updated.md
sdo pr update --pr-id 12 --status closed
sdo pr update --pr-id 12 --title "New title" --status merged
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
$ sdo repo --help

Description:
  Repository management commands

Usage:
  sdo repo [command] [options]

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
sdo repo list                       # List repos
sdo repo list --top 10              # Limit to 10
sdo repo list --verbose             # Show API commands
```

**Options:**
- `--top <top>` — Return top N repositories
- `--verbose` — Show mapping

#### repo show

Display repository information from current Git remote.

**Usage:**
```bash
sdo repo show                       # Show current repo
sdo repo show --verbose             # Show API command
```

**Options:**
- `--verbose` — Show mapping

#### repo create

Create a new repository.

**Usage:**
```bash
sdo repo create myrepo                          # Create repo
sdo repo create myrepo --description "My repo"  # With description
sdo repo create myrepo --private                # Make private
sdo repo create myrepo --private --verbose      # With mapping
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
sdo repo delete                     # Delete (with prompt)
sdo repo delete --force             # Delete (no prompt)
sdo repo delete --force --verbose   # Show mapping
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
$ sdo pipeline --help

Description:
  Pipeline/workflow management commands (create, show, list, run, status, logs, delete, lastbuild, update)

Usage:
  sdo pipeline [command] [options]

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
sdo pipeline list                   # List pipelines
sdo pipeline list --repo myrepo     # Filter by repository
sdo pipeline list --all             # Show all pipelines in project
sdo pipeline list --verbose         # Show API commands
```

**Options:**
- `--repo <repo>` — Filter by repository name
- `--all` — Show all pipelines in the project
- `--verbose` — Show mapping

#### pipeline show

Display pipeline/workflow details.

**Usage:**
```bash
sdo pipeline show 1234              # Show pipeline by ID
sdo pipeline show myworkflow        # Show pipeline by name
sdo pipeline show --verbose         # Show API command
```

**Options:**
- `<pipeline-id-or-name>` — Pipeline ID or name (optional; Azure DevOps only)
- `--verbose` — Show mapping

#### pipeline create

Create a new pipeline/workflow from YAML file.

**Usage:**
```bash
sdo pipeline create .github/workflows/ci.yml
sdo pipeline create .github/workflows/ci.yml --verbose
```

**Options:**
- `<file-path>` — Path to YAML definition file (required)
- `--verbose` — Show mapping

#### pipeline run

Execute/trigger a pipeline.

**Usage:**
```bash
sdo pipeline run myworkflow         # Trigger run
sdo pipeline run myworkflow -b main # Run on specific branch
sdo pipeline run myworkflow --branch develop --verbose
```

**Options:**
- `<pipeline-name>` — Pipeline name (optional, uses current repo if not provided)
- `-b, --branch <branch>` — Branch to run the pipeline on
- `--verbose` — Show mapping

#### pipeline status

Query pipeline build/run status.

**Usage:**
```bash
sdo pipeline status                 # Show latest build status
sdo pipeline status 1234            # Check specific build
sdo pipeline status 1234 --verbose  # Show details
```

**Options:**
- `<build-id>` — Build/run ID (optional, shows latest if not provided)
- `--verbose` — Show mapping

#### pipeline logs

Retrieve pipeline execution logs.

**Usage:**
```bash
sdo pipeline logs                     # Show latest logs
sdo pipeline logs 1234                # Show logs for specific build
sdo pipeline logs --build-id 1234     # Explicit build ID
sdo pipeline logs 1234 --verbose      # Show with details
```

**Options:**
- `<build-id>` — Build/run ID (optional, shows latest if not provided)
- `--build-id <build-id>` — Build/run ID
- `--verbose` — Show mapping and API commands

#### pipeline lastbuild

Show last build/run for pipeline.

**Usage:**
```bash
sdo pipeline lastbuild              # Last build for current repo
sdo pipeline lastbuild myworkflow   # Last build for specific pipeline
sdo pipeline lastbuild myworkflow --verbose  # Show details
```

**Options:**
- `<pipeline-name>` — Pipeline name (optional, uses current repo if not provided)
- `--verbose` — Show mapping

#### pipeline update

Update pipeline configuration.

**Usage:**
```bash
sdo pipeline update --file updated.yml
sdo pipeline update --file updated.yml --pipeline myworkflow
sdo pipeline update --file updated.yml --message "Update CI config" --branch main
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
sdo pipeline delete 1234            # Delete (with prompt)
sdo pipeline delete 1234 --force    # Delete (no prompt)
sdo pipeline delete 1234 --force --verbose
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
$ sdo user --help

Description:
  User management commands

Usage:
  sdo user [command] [options]

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
sdo user list                      # List users
sdo user list --top 50             # Limit to 50
sdo user list --verbose            # Show API commands
```

**Options:**
- `--top <N>` — Maximum results (default: 50)
- `--verbose` — Show mapping

#### user show

Display user details.

**Usage:**
```bash
sdo user show --login naz-hage         # Show GitHub user
sdo user show --login naz-hage --verbose  # Show API command
```

**Options:**
- `--login <login>` — GitHub login or Azure identity (required)
- `--verbose` — Show mapping

#### user search

Search for users.

**Usage:**
```bash
sdo user search --query "naz"              # Search for users
sdo user search --query "naz" --top 20     # Limit to 20
sdo user search --query "naz" --verbose    # Show API command
```

**Options:**
- `--query <term>` — Search term (required)
- `--top <N>` — Maximum results (default: 50)
- `--verbose` — Show mapping

#### user permissions

Show user permissions.

**Usage:**
```bash
sdo user permissions --user naz-hage          # Show permissions
sdo user permissions --user naz-hage --verbose  # Show detailed
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

---

## Advanced Automation Features

Advanced Automation Features introduces comprehensive enterprise capabilities to standardize operations, automate bulk workflows, and ensure reliable testing across GitHub and Azure DevOps platforms.

### Configuration System (YAML-based)

The configuration system allows you to standardize work item queries by storing default filters in a YAML configuration file.

#### Overview

- **Automatic Discovery**: Searches for `sdo-config.yaml` in:
  1. Current working directory
  2. `.temp` subfolder in current directory  
  3. `.temp` subfolder in parent directory
- **Configuration Priority** (highest to lowest):
  1. CLI parameters (e.g., `sdo wi list --state "Done"`)
  2. Config file defaults (from sdo-config.yaml)
  3. Hard-coded defaults in code
- **Optional**: Configuration files are completely optional; all commands work without them

#### Configuration File Format

Create `sdo-config.yaml` in your project:

```yaml
commands:
  wi:
    list:
      area_path: "Project\\Warriors"
      state: "To Do,In Progress"
      top: 9
```

#### Usage Examples

**Without Configuration**:
```bash
sdo wi list --area "Project\Warriors" --state "To Do,In Progress" --top 9
# Returns: 9 work items from Warriors area in To Do or In Progress states
```

**With Configuration File**:
```bash
# Place sdo-config.yaml in .temp\ folder
sdo wi list
# Returns: Same 9 items using defaults from config
# Displays: Config: C:\project\.temp\sdo-config.yaml
```

**Override Configuration Defaults**:
```bash
# CLI parameters override config defaults
sdo wi list --state "Done"
# Returns: All work items in Done state (ignores config state filter)
```

#### Configuration Discovery

The tool automatically searches for configuration files:

```bash
# From C:\project\myrepo
sdo wi list
# Searches for config in this order:
# 1. C:\project\myrepo\sdo-config.yaml
# 2. C:\project\myrepo\.temp\sdo-config.yaml  
# 3. C:\project\.temp\sdo-config.yaml
```

#### Explicit Configuration Path

Specify a custom configuration path:

```bash
sdo wi list --config "C:\my-configs\standard.yaml"
```

#### Supported Configuration Keys

Under `commands.wi.list` in the YAML file:

| Key | Azure DevOps | GitHub | Description |
|-----|--------------|--------|-------------|
| `area_path` | ✓ | - | Area path filter (e.g., "Project\\Area\\SubArea") |
| `state` | ✓ | ✓ | State filter: New, Approved, Committed, Done, To Do, In Progress |
| `type` | ✓ | - | Work item type: PBI, Bug, Task, Spike, Epic |
| `iteration` | ✓ | - | Iteration filter (e.g., "Project\\Sprint 1") |
| `top` | ✓ | ✓ | Maximum items to return (default: 50) |

#### Example Scenarios

**Scenario 1: Team Standardization**

Team commits configuration to project repository:
```yaml
# .github/.temp/sdo-config.yaml
commands:
  wi:
    list:
      area_path: "MyProject\\Backend"
      state: "In Progress"
      top: 20
```

All team members get consistent results without remembering filters:
```bash
git clone https://github.com/myorg/myproject
cd myproject
sdo wi list  # Gets team's standard view automatically
```

**Scenario 2: Sprint Planning**

Create sprint-specific configuration:
```yaml
# .temp/sdo-sprint-5.yaml
commands:
  wi:
    list:
      iteration: "MyProject\\Sprint 5"
      state: "New,Approved,Committed"
```

Use it explicitly:
```bash
sdo wi list --config .temp/sdo-sprint-5.yaml
```

**Scenario 3: Release Validation**

Store release criteria configuration:
```yaml
# .temp/sdo-release.yaml
commands:
  wi:
    list:
      state: "Done"
      area_path: "MyProject\\Release"
      top: 100
```

Verify release readiness:
```bash
sdo wi list --config .temp/sdo-release.yaml
# Shows all completed release items
```

---

### Bulk Operations Processor

Process large batches of work items or pull requests efficiently with built-in error handling and retry logic.

#### Overview

- **Batch Size**: Default 10 operations per batch (configurable)
- **Retry Logic**: 3 retry attempts with exponential backoff (configurable)
- **Error Handling**: Continues processing on failures, provides summary report
- **Thread-Safe**: Supports concurrent operations safely

#### Features

- Graceful failure handling with partial success reporting
- Detailed error summaries showing what succeeded and what failed
- Configurable batch sizes and retry attempts
- Supports both create and update operations

#### Example Usage

When processing large numbers of work items:

```bash
# Process 100 work items in batches of 10
# Automatically retries failed operations up to 3 times
sdo wi bulk-create items.txt --batch-size 10 --max-retries 3
```

#### Use Cases

1. **Migration**: Move items from one platform to another
2. **Bulk Updates**: Update multiple work items simultaneously
3. **Data Import**: Import large CSV files of work items
4. **Batch Operations**: Create multiple related items in one command

---

### Markdown Parser for Content Creation

Create professional work items and pull requests using markdown files with rich formatting support.

#### Overview

- **Markdown Support**: GitHub-flavored markdown (GFM) syntax
- **Metadata**: YAML frontmatter for work item properties
- **Rich Content**: Support for code blocks, acceptance criteria, tables
- **Error Handling**: Detailed error reporting with line numbers
- **Security**: HTML sanitization for safe content

#### Markdown Format

**Basic Format** (required):
```markdown
# Work Item Title

This is the description text. 
Supports multiple paragraphs.

## Acceptance Criteria
- [ ] Criterion 1
- [ ] Criterion 2  
- [x] Criterion 3 (completed)

## Code Examples
```csharp
public void DoSomething()
{
    // Code here
}
```
```

**With YAML Frontmatter** (optional):
```markdown
---
work_item_type: PBI
priority: High
assignee: user@example.com
labels: "feature, backend, auth"
target: "azdo"  # azdo or github
---

# Authentication Service Design

Implement OAuth2 integration for secure user authentication...

## Acceptance Criteria
- [ ] OAuth2 provider integrated
- [ ] Token refresh mechanism implemented
- [ ] Session management in place
```

#### Creating Work Items from Markdown

```bash
# Create from markdown file
sdo wi create --file-path work-item.md

# Preview before creating (dry-run)
sdo wi create --file-path work-item.md --dry-run

# Show mapping to native commands
sdo wi create --file-path work-item.md --verbose
```

#### Supported Metadata

In YAML frontmatter under `---`:

| Field | Type | Example | Platforms |
|-------|------|---------|-----------|
| `work_item_type` | string | PBI, Bug, Task, Feature, Epic | Azure DevOps |
| `priority` | string | High, Medium, Low | Both |
| `assignee` | email | user@example.com | Both |
| `labels` | csv | "backend, auth, security" | GitHub |
| `area_path` | string | Project\\Team\\Component | Azure DevOps |
| `target` | string | azdo, github | Both |

#### Metadata via Headers

Alternative to YAML frontmatter using level-2 headers:

```markdown
# Feature Title

## Type: PBI
## Priority: High
## Assignee: user@example.com

Description here...
```

#### Parsing Details

The parser extracts:

1. **Title**: First H1 header (required)
2. **Frontmatter**: YAML between `---` markers (optional)
3. **Metadata**: Level-2 headers in "Key: Value" format (optional)
4. **Description**: All paragraphs before acceptance criteria
5. **Code Blocks**: All fenced code blocks with language detection
6. **Acceptance Criteria**: Unordered list after "## Acceptance Criteria" header

#### Error Handling

```bash
# Verbose mode shows detailed parsing errors
sdo wi create --file-path work-item.md --verbose

# Errors include:
# - Line number and content
# - Specific issue (e.g., "Title is required", "Malformed header")
# - Warnings for minor issues (unclosed code blocks, etc.)
```

#### Example Scenarios

**Scenario 1: Feature Request**

Create `features/auth-redesign.md`:
```markdown
---
work_item_type: PBI
priority: High
assignee: alice@company.com
labels: "backend, security, authentication"
---

# Redesign Authentication System

Complete overhaul of the authentication layer to support modern OAuth2 and OpenID Connect standards.

## Acceptance Criteria
- [ ] OAuth2 provider integration
- [ ] OpenID Connect support
- [ ] Token refresh mechanism
- [ ] Backward compatibility maintained
- [ ] Documentation updated

## Technical Details
```csharp
public interface IAuthenticationProvider
{
    Task<AuthToken> AuthenticateAsync(credentials);
    Task<bool> RefreshTokenAsync(token);
}
```
```

Then create:
```bash
sdo wi create --file-path features/auth-redesign.md
```

**Scenario 2: Bug Report**

Create `bugs/parser-crash.md`:
```markdown
---
work_item_type: Bug
priority: Critical
target: "github"
---

# Parser Crashes on Malformed Markdown

The markdown parser crashes when encountering headers without proper spacing.

## Acceptance Criteria
- [ ] Malformed header detected and handled gracefully
- [ ] Error message is user-friendly
- [ ] Unit tests added for edge cases

## Reproduction Steps
```markdown
#NoSpace Header
```
```

**Scenario 3: Pull Request**

Create `prs/feature-merge.md`:
```markdown
---
work_item_type: Pull Request
target: "github"
---

# Add User Authentication to Dashboard

This PR implements OAuth2 authentication for dashboard access.

## Changes
- Added authentication middleware
- Integrated identity provider
- Updated login form

## Testing
```bash
npm test
npm run lint
```
```

---

### E2E Testing Infrastructure

Automated testing framework for validating SDO functionality across Azure DevOps and GitHub platforms with color-coded output.

#### Overview

- **Cross-Platform Testing**: Validates both Azure DevOps and GitHub operations
- **Color-Coded Output**: Green for success, red for errors, visible in console
- **Test Discovery**: Reflection-based automatic test discovery
- **Specific Test Execution**: Run individual tests via `--test-case` parameter
- **Logging**: Plain text output to `sdo-e2e-test.log`

#### Test Targets

Available MSBuild targets for E2E testing:

```bash
# Run all Azure DevOps tests
nb RUN_AZDO_WI_ASSIGNED_TO_ME_TEST

# Run all GitHub tests  
nb RUN_GITHUB_WI_ASSIGNED_TO_ME_TEST

# Run Azure DevOps pipeline tests
nb RUN_AZDO_PIPELINE_TEST

# Run GitHub Actions tests
nb RUN_GITHUB_PIPELINE_TEST
```

#### Example Output

```
----------------------------------------------------------
STEP 1: Get unfiltered work item count
----------------------------------------------------------
[INFO] Executing: sdo.exe wi list
[INFO] Unfiltered work items count: 34

----------------------------------------------------------
STEP 2: Execute --assigned-to-me filter
----------------------------------------------------------
[INFO] Executing: sdo.exe wi list --assigned-to-me
[SUCCESS] √ Exit code is 0
[SUCCESS] √ Work items returned: 1
[SUCCESS] √ Filter validation passed: 1 <= 34

----------------------------------------------------------
[SUCCESS] √ Azure DevOps List Assigned To Me Test PASSED
[SUCCESS] √ PASSED: Validate_AzureDevOps_ListAssignedToMe
----------------------------------------------------------
```

#### Running Specific Tests

Run individual test cases:

```bash
# Run a specific test
sdo-e2e-tests.exe --test-case Validate_AzureDevOps_ListAssignedToMe

# Run with verbose output
sdo-e2e-tests.exe --test-case Validate_GitHub_ListAssignedToMe --verbose
```

#### Test Coverage

**Platform Parity Tests**:
- Work item list filtering
- Assigned-to-me logic
- User detection across platforms
- Pipeline creation and execution
- Repository operations

**Reliability Tests**:
- Connection timeout handling
- Authentication failure recovery
- API rate limiting
- Network error resilience

#### Log Files

Test logs are saved to:
- Location: `sdo-e2e-test/bin/Debug/net10.0/sdo-e2e-test.log`
- Format: Plain text with timestamps
- Content: Detailed step-by-step execution trace

#### Integration with CI/CD

These tests can be integrated into your build pipeline:

```bash
# In your GitHub Actions workflow
- name: Run E2E Tests
  run: nb RUN_AZDO_WI_ASSIGNED_TO_ME_TEST

# Or in Azure Pipelines
- task: PowerShell@2
  inputs:
    scriptType: 'inline'
    script: 'nb RUN_GITHUB_WI_ASSIGNED_TO_ME_TEST'
```

---

## Service Enhancements

Advanced Automation Features includes reliability and performance improvements to core services.

### AzureDevOpsClient Enhancements

- **Endpoint Prioritization**: Queries `/connectionData` first (most reliable), falls back to Graph API
- **Improved User Detection**: Better identification of current user
- **Enhanced Error Handling**: Detailed error messages for API failures
- **Structured Logging**: Better diagnostics for troubleshooting

### GitHubClient Enhancements

- **Better Timeout Handling**: Default 30 seconds, configurable per request
- **Improved Error Messages**: Clearer feedback on API errors
- **Enhanced Logging**: Structured logging for debugging
- **Rate Limiting Awareness**: Better handling of GitHub API rate limits

### Command Improvements

**WorkItemCommand**:
- Configuration file loading with error validation
- Comprehensive error handling for list/show/create operations
- Better user feedback and validation messages

**PullRequestCommand**:
- Branch existence validation before creation
- Improved error messages
- Better diagnostic information

---

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
sdo wi update --id 170 --state Done          # Correct
sdo wi update --id 170 --state "In Progress"  # Correct
sdo wi update --id 170 --state done           # Also works (case-insensitive)
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
sdo wi list --assigned-to-me-filter

# ✓ Correct
sdo wi list --assigned-to-me
```

**Forgetting required flags:**
```bash
# ❌ Missing --id for show
sdo wi show

# ✓ Correct
sdo wi show --id 243
```

**Mixing GitHub and Azure DevOps states:**
```bash
# ❌ GitHub state on Azure DevOps repo
git remote set-url origin https://dev.azure.com/org/project/_git/repo
sdo wi update --id 243 --state closed  # Won't work

# ✓ Correct
sdo wi update --id 243 --state Done
```

