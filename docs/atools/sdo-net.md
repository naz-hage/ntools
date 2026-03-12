# sdo.exe - Simple DevOps Operations Tool (C# Version)

This page documents the `sdo.exe` CLI tool - the C# migration of Simple DevOps Operations. Use this page for detailed usage examples, prerequisites, and command reference.

## Overview

`sdo.exe` (Simple DevOps Operations) is a C# implementation providing unified operations for work items across Azure DevOps and GitHub platforms. It automatically detects the target platform from Git remote configuration.

### Key Features (Phase 3.1 Complete)

- **Work Item Management**:
  - `workitem show`: Display detailed work item information (GitHub issues or Azure DevOps work items)
  - `workitem list`: List work items with filtering and pagination
- **Multi-Platform Support**: Seamless operations across Azure DevOps and GitHub
- **Automatic Platform Detection**: Detects platform from Git remote configuration
- **Clean Output Formatting**: Native table display with emoji headers and proper column alignment

### Planned Features (Phase 3.2+)

- `workitem create`: Create new work items from structured input
- `workitem update`: Update work item properties
- `workitem comment`: Add comments/discussion to work items
- Additional commands: `pipeline`, `pr` (pull request), `repo` (repository) management

## Help Output

View the available commands and options:

### Main Help

```
$ sdo --help

Simple DevOps Operations (SDO) - C# Migration
============================================
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
  workitem  Work item management commands
```

### Workitem Command Help

```
$ sdo workitem --help

Simple DevOps Operations (SDO) - C# Migration
============================================
Description:
  Work item management commands

Usage:
  sdo workitem [command] [options]

Options:
  -?, -h, --help  Show help and usage information

Commands:
  show  Display detailed work item information
  list  List work items with optional filtering
```

### Show Subcommand Help

```
$ sdo workitem show --help

Simple DevOps Operations (SDO) - C# Migration
============================================
Description:
  Display detailed work item information

Usage:
  sdo workitem show [options]

Options:
  --id <id>       Work item ID (required)
  -c, --comments  Show comments/discussion
  --verbose       Enable verbose output
  -?, -h, --help  Show help and usage information
```

### List Subcommand Help

```
$ sdo workitem list --help

Simple DevOps Operations (SDO) - C# Migration
============================================
Description:
  List work items with optional filtering

Usage:
  sdo workitem list [options]

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

Place `sdo.exe` in your PATH or run directly from:
```
C:\source\ntools\Sdo\bin\Release\sdo.exe
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
sdo auth gh              # Verify GitHub authentication
sdo auth devops          # Verify Azure DevOps authentication
```

Success output:
```
√ GitHub authentication successful
```

### Work Item Management

#### List Work Items

List work items from the current repository (defaults to GitHub). By default, shows all work items **EXCEPT closed (GitHub) or done (Azure DevOps)** to display active work-in-progress items.

```bash
# List active work items (excludes closed/done by default)
sdo workitem list

# List specific number of items
sdo workitem list --top 20

# Show only closed issues
sdo workitem list --state closed

# Show items in a specific state
sdo workitem list --state "In Progress"

# Verbose output with debugging information
sdo workitem list --verbose
```

**Default Behavior:**
- **GitHub**: Shows all issues EXCEPT `closed`
- **Azure DevOps**: Shows all work items EXCEPT `done`

**Output Format:**

```
📋 Issues (18 found):
------------------------------------------------------------------------------------------------------------------------
#      Title                                    State      Labels                         Assignee
------------------------------------------------------------------------------------------------------------------------
#246   Phase 6: Deployment & Transition (CI,... OPEN       backlog, sdo-migration         Unassigned
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

#### Show Work Item Details

Display full details of a specific work item:

```bash
# Show GitHub issue #243
sdo workitem show --id 243

# Show with verbose output
sdo workitem show --id 243 --verbose

# Show with comments (when available)
sdo workitem show --id 243 --comments
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
Phase 3 focuses on migrating the Python SDO command implementations to the new C# 
`Sdo` project. This covers pipeline, PR, and user commands and ensures behavior parity with the Python tool.

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

### Planned Commands

#### Create Work Item (Phase 3.2)

```bash
sdo workitem create --title "New Feature" --type PBI --description "Description"
```

#### Update Work Item (Phase 3.2)

```bash
sdo workitem update --id 243 --title "Updated Title" --state closed --assigned-to user@example.com
```

#### Add Comment (Phase 3.2)

```bash
sdo workitem comment --id 243 --text "This is my comment"
```

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
1. Verify authentication: `sdo auth gh`
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

## Testing

### Unit Tests

All commands have comprehensive unit test coverage:

```bash
# Run all workitem command tests
cd C:\source\ntools
nb UNIT_TEST_WORKITEM_COMMAND

# Output shows
Passed!  - Failed: 0, Passed: 33, Skipped: 10, Total: 43
```

### Manual Testing

Test with real repositories:

```bash
# Test with ntools repository
cd C:\source\ntools
sdo workitem list --top 5
sdo workitem show --id 243

# Test with different repository
cd Path\To\Your\Repo
sdo workitem list
sdo workitem show --id <issue_number>
```

## Phase Status

### Phase 3.1 - ✅ Work Item Commands (show, list)

- ✅ Implement `workitem show` subcommand
- ✅ Implement `workitem list` subcommand  
- ✅ Support GitHub issues with full metadata
- ✅ Proper date deserialization and formatting
- ✅ Labels and assignee display
- ✅ 33 unit tests passing
- ✅ Real data verified with GitHub API
- ✅ Clean output formatting matching Python SDO

### Phase 3.2 - Pending (update, comment)

- ⏳ Implement `workitem update` subcommand
- ⏳ Implement `workitem comment` subcommand
- ⏳ Add filtering options (--type, --assigned-to, --assigned-to-me)
- ⏳ 10 placeholder tests awaiting implementation

### Phase 3.3-3.5 - Planned

- Repository commands (`repo create`, `repo list`, `repo show`, `repo delete`)
- Pull request commands (`pr merge`, `pr approve`, `pr list`, `pr show`)
- Pipeline commands (`pipeline create`, `pipeline run`, `pipeline status`, etc.)
- User/Permission commands (`user show`, `user list`, `user search`, `permissions`)

## Implementation Notes

**Phase 3.1 Enhancements (March 11, 2026)**:
- ✅ **GitHub API Pagination**: Full pagination implemented to fetch all issues across multiple pages
- ✅ **State Filtering**: Default behavior excludes closed items; `--state closed` retrieves closed issues  
- ✅ **Result Limiting**: `--top` parameter correctly limits results after filtering
- ✅ All 33 unit tests passing with pagination
- ✅ Verified with real data: `workitem list` now returns all 18 open issues (previously limited to 14)

**Known Limitations**:
- Azure DevOps: Same pagination logic applied; tested with state filtering
- Update/Comment operations: Unit tests written (Phase 3.2), awaiting implementation

## Development

### Building from Source

```bash
cd C:\source\ntools
nb build                    # Build solution
nb test                     # Run all tests
nb UNIT_TEST_WORKITEM_COMMAND  # Run workitem tests only
```

### Source Code

- **Main Command**: [Sdo/Commands/WorkItemCommand.cs](../../Sdo/Commands/WorkItemCommand.cs)
- **GitHub Client**: [Sdo/Services/GitHubClient.cs](../../Sdo/Services/GitHubClient.cs)
- **Tests**: [SdoTests/WorkItemCommandTests.cs](../../SdoTests/WorkItemCommandTests.cs)

### Architecture

The C# implementation follows the `System.CommandLine` framework for CLI parsing and includes:

- Automatic platform detection via `PlatformDetector`
- Async API calls via `GitHubClient` and `AzureDevOpsClient`
- Comprehensive error handling with try-catch and logging
- Clean separation of concerns: Commands → Services → API Clients

## Related Documentation

- [sdo Installation Guide](sdo-installation.md) - Python version setup
- [sdo-migration-plan.md](sdo-migration-plan.md) - Full migration roadmap
- [NBuild System](../nbuild/README.md) - Build automation tool

## See Also

- GitHub: https://github.com/naz-hage/ntools
- Azure DevOps: https://dev.azure.com/naz-hage/ntools
- Issue #243: Phase 3 Command Implementation
