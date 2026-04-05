# sdo.net - Simple DevOps Operations Tool (C# Version)

This page documents the `sdo.net` CLI tool - the C# migration of Simple DevOps Operations. Use this page for detailed usage examples, prerequisites, and command reference.

## Overview

`sdo.net` (Simple DevOps Operations) is a C# implementation providing unified operations for work items across Azure DevOps and GitHub platforms. It automatically detects the target platform from Git remote configuration.

### Key Features

- **Work Item Management**:
  - `wi` commands for listing, showing, creating, updating and commenting on work items (GitHub issues/Azure DevOps work items) with cross-platform state translation

- **Pull Request (PR) Commands**:
  - `pr` commands for listing, showing, creating and updating pull requests (merge/approve planned as advanced ops)

- **Pipeline / Workflow Commands**:
  - `pipeline` commands to list, show, create, run, check status, view logs and manage pipeline definitions

- **Repository Commands**:
  - `repo` commands to list, show and manage repositories with consistent cross-platform output

- **User & Permissions Commands**:
  - `user` subcommands: `list`, `show`, `search`, `permissions` — useful for audits and parity checks

- **Map / Native CLI Mappings**:
  - `map` shows how SDO commands map to native platform CLIs (`gh`, `az`), and with `--verbose` prints the exact native API commands (copy/paste ready)

- **Platform Auto-Detection**: Extracts org/project from Git remotes and selects GitHub or Azure DevOps automatically

- **State Management & Translation**:
  - Canonical work item states: `New`, `Approved`, `Committed`, `Done`, `To Do`, `In Progress` with automatic translation to GitHub `open|closed` where appropriate

- **Performance & API Improvements**:
  - Pagination and batch API optimizations for GitHub and Azure DevOps (reduced O(n) calls)

- **Testing & CI Targets**:
  - Comprehensive unit test suites (work item, repo, PR tests) and specific `UNIT_TEST_*` targets for CI and local verification

- **UX & Output**:
  - Clean, emoji-enhanced table output and concise error messages; verbose mode exposes request/response and mapping information for debugging


### Main Help

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
  pipeline  Pipeline/workflow management commands (create, show, list, run, status, logs, delete, lastbuild, update)
  pr        Pull request operations
  repo      Repository management commands
  wi        Work item management commands
  user      User management commands for GitHub and Azure DevOps
```

## Help Output

View the available commands and options:

 
### wi Command Help

```
$ sdo.net wi --help

Simple DevOps Operations (sdo.net) - C# Migration
============================================
Description:
  Work item management commands

Usage:
  sdo.net wi [command] [options]

Options:
  -?, -h, --help  Show help and usage information

Commands:
  comment  Add comment to a work item
  create   Create a new work item from markdown file
  list     List work items with optional filtering
  show     Display detailed work item information
  update   Update work item properties
```

### Show Subcommand Help

```
$ sdo.net wi show --help

Simple DevOps Operations (sdo.net) - C# Migration
============================================
Description:
  Display detailed work item information

Usage:
  sdo.net wi show [options]

Options:
  --id <id>       Work item ID (required)
  -c, --comments  Show comments/discussion
  --verbose       Enable verbose output
  -?, -h, --help  Show help and usage information
```

### List Subcommand Help

```
$ sdo.net wi list --help

Simple DevOps Operations (sdo.net) - C# Migration
============================================
Description:
  List work items with optional filtering

Usage:
  sdo.net wi list [options]

Options:
  --type <type>                Filter by work item type (PBI, Bug, Task, Spike,
                               Epic)
  --state <state>              Filter by state (New, Approved, Committed, Done,
                               To Do, In Progress)
  --assigned-to <assigned-to>  Filter by assigned user (email or display name)
  --assigned-to-me             Filter by work items assigned to current user
  --top <top>                  Maximum number of items to return (default: 50)
  --verbose                    Enable verbose output
  -?, -h, --help               Show help and usage information
```

## Prerequisites

### Authentication

#### GitHub
- GitHub CLI (`gh`) must be installed and authenticated: `gh auth login`
- Alternatively, set `GITHUB_TOKEN` environment variable
- Or store credentials in Windows Credential Manager under `gh:github.com:` target

#### Azure DevOps
- Set `AZURE_DEVOPS_PAT` environment variable
- Or store credentials in Windows Credential Manager under `GitAzureDevOps` target

### Git Repository Context

The tool automatically detects the platform from your current Git remote:
- **GitHub**: Detected from remotes like `https://github.com/owner/repo`
- **Azure DevOps**: Detected from remotes like `https://dev.azure.com/org/project/_git/repo`

## Installation

Place `sdo.net` in your PATH or run directly from:
```
C:\source\ntools\sdo.net\bin\Release\sdo.net
```

Build from source:
```bash
cd C:\source\ntools
nb build
```

## Usage

### Authentication Verification

Verify your authentication is configured correctly:

```bash
sdo.net auth gh              # Verify GitHub authentication
sdo.net auth devops          # Verify Azure DevOps authentication
```

Success output:
```
√ GitHub authentication successful
```

### Work Item Management

#### List Work Items

List work items from the current repository (defaults to GitHub). By default, shows all work items **EXCEPT closed (GitHub) or done/closed (Azure DevOps)** to display active work-in-progress items.

```bash
# List active work items (excludes closed/done by default)
sdo.net wi list

# List specific number of items
sdo.net wi list --top 20

# Show only done items
sdo.net wi list --state Done

# Show items in a specific state
sdo.net wi list --state "In Progress"

# Show all items including done/closed
sdo.net wi list --state closed

# Verbose output with debugging information
sdo.net wi list --verbose
```

**Default Behavior:**
- **GitHub**: Shows all issues EXCEPT `closed`
- **Azure DevOps**: Shows all work items EXCEPT `done` or `closed`

**Valid States:**
- Azure DevOps: `New`, `Approved`, `Committed`, `Done`, `To Do`, `In Progress`
- GitHub: `open`, `closed` (automatically translated from Azure DevOps states)

**Output Format:**

```
📋 Issues (18 found):
------------------------------------------------------------------------------------------------------------------------
#      Title                                    State      Labels                         Assignee
------------------------------------------------------------------------------------------------------------------------
#246   Phase 6: Deployment & Transition (CI,... OPEN       backlog, sdo.net-migration         Unassigned
#245   Phase 5: Testing & Validation (Unit, ... OPEN       backlog, testing               Unassigned
#243   Phase 3: Command Implementation (Migr... OPEN       enhancement, backlog           Unassigned
...
------------------------------------------------------------------------------------------------------------------------

📊 Total: 18 issue(s)
```

**Options:**

- `--top <N>`: Maximum number of items to return (default: 50)
- `--state <state>`: Filter by specific state (e.g., "open", "closed", "In Progress", "Done"). Omit to show all EXCEPT closed/done
- `--type <type>`: Filter by type (PBI, Bug, Task, etc.) - planned for Phase 3.2
- `--assigned-to <user>`: Filter by assignee email or name - planned for Phase 3.2
- `--assigned-to-me`: Filter items assigned to current user - planned for Phase 3.2
- `--verbose`: Show detailed diagnostic information

#### Create Work Item

Create new work items from markdown files:

```bash
# Create from markdown file
sdo.net wi create --file-path work-item.md

# Create with dry-run (preview without creating)
sdo.net wi create --file-path work-item.md --dry-run

# Verbose output with JSON payload
sdo.net wi create --file-path work-item.md --verbose
```

**Markdown Format:**
```markdown
# Work Item Title

This is the work item description.

## Acceptance Criteria
- Criterion 1
- Criterion 2
- Criterion 3
```

#### Update Work Item

Update work item properties:

```bash
# Update state
sdo.net wi update --id 243 --state Done

# Update title
sdo.net wi update --id 243 --title "Updated Title"

# Update state with success message showing new state
sdo.net wi update --id 166 --state done
# Output: √ Work item 166 updated successfully to state: Done

# Update with verbose output
sdo.net wi update --id 243 --state "In Progress" --verbose
```

**Valid States:**
- `New`, `Approved`, `Committed`, `Done`, `To Do`, `In Progress` (case-insensitive)
- GitHub only supports: `open`, `closed`

#### Add Comment

Add comments to work items:

```bash
# Add comment to issue
sdo.net wi comment --id 243 --message "This is my comment"

# Verbose output
sdo.net wi comment --id 243 --message "Great work!" --verbose
```

#### Show Work Item Details

Display full details of a specific work item:

```bash
# Show GitHub issue #243
sdo.net wi show --id 243

# Show with verbose output
sdo.net wi show --id 243 --verbose

# Show with comments (when available)
sdo.net wi show --id 243 --comments
```

**Output Format:**

```
Issue #243
======================================================================
Title:       Phase 3: Command Implementation (Migrate CLI Commands to C#)
State:       open
Created:     2026-03-01T22:48:29.0000000Z
Updated:     2026-03-01T22:48:29.0000000Z

Description:
Phase 3 focuses on migrating the Python sdo.net command implementations to the new C# 
`sdo.net` project. This covers pipeline, PR, and user commands and ensures behavior parity with the Python tool.

- [ ] Implement pipeline commands: `create`, `show`, `list`, `update`, `run`, `status`, `logs`, `lastbuild`, `delete`
- [ ] Implement PR commands: `merge`, `approve`
- [ ] Implement user commands: `show`, `list`, `search`, `permissions`
- [ ] Add command-specific unit tests and integration tests
- [ ] Ensure command-line option parity with Python implementation

URL: https://github.com/naz-hage/ntools/issues/243
```

**Options:**

- `--id <number>`: Work item ID (required)
- `--comments`: Include comments/discussion items
- `--verbose`: Show detailed diagnostic information

## Command Reference

This section documents the primary commands introduced in Phase 1 (foundation), Phase 2 (service/auth), and Phase 4 (advanced features). Commands are read-only where applicable and follow the same patterns used across the CLI.

### map — Show native CLI mappings
Description: show how sdo.net commands map to native platform CLIs (`gh` for GitHub, `az` / `az repos` for Azure DevOps).

Options:
- `--platform <gh|azdo>`: restrict to a single platform mapping
- `--all` : show all mappings for both platforms

Examples:
```
sdo.net map
sdo.net map --platform gh
sdo.net map --platform azdo --all
```

Behavior: with `--verbose` many commands print the equivalent native command (e.g. `gh api repos/owner/repo/collaborators?per_page=100`) in yellow for easy copy/paste.

### auth — Verify authentication
Description: validate available credentials for the detected platform.

Options:
- `gh` : verify GitHub auth (checks `GITHUB_TOKEN`, `gh auth`, Windows credential store)
- `azdo` : verify Azure DevOps PAT (`AZURE_DEVOPS_PAT`)
- `--verbose` : show token detection details and any scope information

Examples:
```
sdo.net auth gh
sdo.net auth azdo --verbose
```

### wi — Work item (issue) commands
Description: CRUD and comment operations for GitHub issues and Azure DevOps work items. Options are translated between platforms.

Common Options:
- `--id <number>` : identifier for show/update/comment
- `--top <n>` : limit number of results for `list` (default: 50)
- `--state <state>` : filter or set state (Azure states: `New, Approved, Committed, Done, To Do, In Progress`; GitHub: `open|closed`)
- `--assigned-to <user>` : filter by assignee (planned)
- `--file-path <path>` : markdown file for `create`
- `--comments` : include comments in `show`
- `--verbose` : show mapping and extra diagnostics

Examples:
```
sdo.net wi list
sdo.net wi list --top 20 --state "In Progress"
sdo.net wi show --id 243 --comments
sdo.net wi create --file-path ./work-item.md --verbose
sdo.net wi update --id 243 --state Done
sdo.net wi comment --id 243 --message "Looks good to me"
```

Notes:
- `create` accepts a markdown document with Title, Description and Acceptance Criteria (see examples earlier in this doc).
- `update --state` performs platform-aware translation (Azure DevOps -> GitHub open/closed mapping where appropriate).

### repo — Repository commands
Description: list and inspect repositories for the authenticated user or detected organization.

Options:
- `--top <n>` : limit results
- `--org <organization>` : specify organization (Azure/GitHub)
- `--verbose` : show mapping and API request details

Examples:
```
sdo.net repo list --top 5
sdo.net repo show --name naz-hage/ntools --verbose
```

### pr — Pull request commands
Description: list, show and operate on pull requests. Merge/approve operations are considered advanced and require write permissions.

Options:
- `--id <id>` : pull request id/number
- `--state <open|closed|merged>` : filter PR list
- `--merge-method <merge|squash|rebase>` : merge strategy (GitHub)
- `--verbose` : show mapping and API details

Examples:
```
sdo.net pr list --state open
sdo.net pr show --id 12
sdo.net pr merge --id 12 --merge-method squash --verbose
```

### pipeline — Pipeline / workflow commands
Description: manage CI pipelines / workflows. Read-only operations are safe for E2E parity checks; trigger/update operations are advanced.

Options:
- `--id <id>` : pipeline or run id
- `--top <n>` : limit list results
- `--org`, `--project` : Azure DevOps scoping
- `--verbose` : show equivalent `gh/az` commands and API payloads

Examples:
```
sdo.net pipeline list
sdo.net pipeline status --id 1234
sdo.net pipeline logs --id 1234 --verbose
```

### user — User and permissions commands
Description: list/search users and show permission summaries for a user or identity. Useful for audits and E2E parity checks.

Options:
- `--login <login>` : GitHub login or Azure identity shorthand
- `--user <login|descriptor>` : user identifier for permissions queries
- `--query <term>` : search term for `search`
- `--top <n>` : limit results
- `--verbose` : show mapping and the exact API calls performed

Examples:
```
sdo.net user list --top 50
sdo.net user show --login naz-hage --verbose
sdo.net user search --query "naz" --top 20
sdo.net user permissions --user naz-hage --verbose
```

Notes:
- Many Phase 4 commands are implemented to match Python behavior and are included where ready; others are planned or gated behind integration tests.
- Use `--verbose` on most commands to display an equivalent native CLI mapping (e.g., `gh` or `az`), request/response dumps, and extra diagnostics.

## Platform Detection

The tool automatically detects which platform to use based on your Git remote:

```bash
# List remotes
git remote -v

# Output shows platform detection
# origin  https://github.com/naz-hage/ntools (fetch) -> Uses GitHub
# origin  https://dev.azure.com/org/project/_git/repo (fetch) -> Uses Azure DevOps
```

To override platform detection (planned):
- `--github`: Force GitHub operations
- `--devops`: Force Azure DevOps operations

## Troubleshooting

### "No issues found"

**Cause**: Authentication failed or no open issues in repository

**Solution**:
1. Verify authentication: `sdo.net auth gh`
2. Check repository has issues: `gh issue list --repo owner/repo`
3. Try with `--state closed` to verify API is working
4. Use `--verbose` flag to see detailed error messages

### "Could not determine GitHub repository from Git remote"

**Cause**: Git remote configuration is missing or incorrectly formatted

**Solution**:
1. Check remotes: `git remote -v`
2. Ensure remotes are properly configured:
   ```bash
   git remote add origin https://github.com/owner/repo
   # or for Azure DevOps
   git remote add origin https://dev.azure.com/org/project/_git/repo
   ```

### Authentication Errors

**GitHub**:
```bash
# Verify gh CLI is installed and authenticated
gh auth status

# Re-authenticate if needed
gh auth login

# Set token via environment variable
$env:GITHUB_TOKEN = "your_token"
```

**Azure DevOps**:
```bash
# Set personal access token
$env:AZURE_DEVOPS_PAT = "your_token"
```

### State Validation Errors

**Update Fails with State Not Supported**:

```
✗ Failed to update work item 158
  Supported states: New, Approved, Committed, Done, To Do, In Progress
```

**Cause**: The state provided is not valid for your Azure DevOps project configuration

**Solution**:
1. Use one of the supported states: `New`, `Approved`, `Committed`, `Done`, `To Do`, `In Progress`
2. Check your project workflow: Some Azure DevOps projects may have different state configurations
3. Verify the exact state name: Use `sdo.net wi list --state Done` (capital D) instead of lowercase

**Examples**:
```bash
# Correct - uses valid state with exact casing
sdo.net wi update --id 170 --state Done

# Correct - alternative form
sdo.net wi update --id 170 --state "In Progress"

# Also works - case-insensitive input
sdo.net wi update --id 170 --state done
```

## Testing


### Manual Testing

Test with real repositories:

```bash
# Test with ntools repository
cd C:\source\ntools
sdo.net wi list --top 5
sdo.net wi show --id 243
sdo.net wi create --file-path test.md --dry-run
sdo.net wi update --id 243 --state Done

# Test with different repository
cd Path\To\Your\Repo
sdo.net wi list
sdo.net wi show --id <issue_number>
```


## Development

### Building from Source

```bash
cd C:\source\ntools
nb build                                    # Build solution
nb test                                     # Run all tests
nb UNIT_TEST_WORKITEM_COMMAND              # Run wi command tests
nb UNIT_TEST_WORKITEM_STATE_TRANSLATOR     # Run state translator tests
```

