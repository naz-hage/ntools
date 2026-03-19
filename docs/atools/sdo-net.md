# sdo.exe - Simple DevOps Operations Tool (C# Version)

This page documents the `sdo.exe` CLI tool - the C# migration of Simple DevOps Operations. Use this page for detailed usage examples, prerequisites, and command reference.

## Overview

`sdo.exe` (Simple DevOps Operations) is a C# implementation providing unified operations for work items across Azure DevOps and GitHub platforms. It automatically detects the target platform from Git remote configuration.

### Key Features (Phase 3.1 Complete)

- **Work Item Management**:
  - `wi show`: Display detailed work item information (GitHub issues or Azure DevOps work items)
  - `wi list`: List work items with filtering and pagination (excludes done/closed by default)
  - `wi create`: Create new work items from markdown files with title, description, and acceptance criteria
  - `wi update`: Update work item properties (title, state, description, assignee) with platform-aware state translation
  - `wi comment`: Add comments/discussion to work items
- **Multi-Platform Support**: Seamless operations across Azure DevOps and GitHub
- **Automatic Platform Detection**: Detects platform from Git remote configuration
- **State Management**: Canonical work item states (New, Approved, Committed, Done, To Do, In Progress) with automatic platform translation
- **Clean Output Formatting**: Native table display with emoji headers and proper column alignment
- **Comprehensive Error Handling**: Platform-specific error messages with state guidance

### Planned Features (Phase 3.2+)

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
  wi  Work item management commands
```

### wi Command Help

```
$ sdo wi --help

Simple DevOps Operations (SDO) - C# Migration
============================================
Description:
  Work item management commands

Usage:
  sdo wi [command] [options]

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
$ sdo wi show --help

Simple DevOps Operations (SDO) - C# Migration
============================================
Description:
  Display detailed work item information

Usage:
  sdo wi show [options]

Options:
  --id <id>       Work item ID (required)
  -c, --comments  Show comments/discussion
  --verbose       Enable verbose output
  -?, -h, --help  Show help and usage information
```

### List Subcommand Help

```
$ sdo wi list --help

Simple DevOps Operations (SDO) - C# Migration
============================================
Description:
  List work items with optional filtering

Usage:
  sdo wi list [options]

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

List work items from the current repository (defaults to GitHub). By default, shows all work items **EXCEPT closed (GitHub) or done/closed (Azure DevOps)** to display active work-in-progress items.

```bash
# List active work items (excludes closed/done by default)
sdo wi list

# List specific number of items
sdo wi list --top 20

# Show only done items
sdo wi list --state Done

# Show items in a specific state
sdo wi list --state "In Progress"

# Show all items including done/closed
sdo wi list --state closed

# Verbose output with debugging information
sdo wi list --verbose
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

#### Create Work Item

Create new work items from markdown files:

```bash
# Create from markdown file
sdo wi create --file-path work-item.md

# Create with dry-run (preview without creating)
sdo wi create --file-path work-item.md --dry-run

# Verbose output with JSON payload
sdo wi create --file-path work-item.md --verbose
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
sdo wi update --id 243 --state Done

# Update title
sdo wi update --id 243 --title "Updated Title"

# Update state with success message showing new state
sdo wi update --id 166 --state done
# Output: √ Work item 166 updated successfully to state: Done

# Update with verbose output
sdo wi update --id 243 --state "In Progress" --verbose
```

**Valid States:**
- `New`, `Approved`, `Committed`, `Done`, `To Do`, `In Progress` (case-insensitive)
- GitHub only supports: `open`, `closed`

#### Add Comment

Add comments to work items:

```bash
# Add comment to issue
sdo wi comment --id 243 --message "This is my comment"

# Verbose output
sdo wi comment --id 243 --message "Great work!" --verbose
```

#### Show Work Item Details

Display full details of a specific work item:

```bash
# Show GitHub issue #243
sdo wi show --id 243

# Show with verbose output
sdo wi show --id 243 --verbose

# Show with comments (when available)
sdo wi show --id 243 --comments
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
3. Verify the exact state name: Use `sdo wi list --state Done` (capital D) instead of lowercase

**Examples**:
```bash
# Correct - uses valid state with exact casing
sdo wi update --id 170 --state Done

# Correct - alternative form
sdo wi update --id 170 --state "In Progress"

# Also works - case-insensitive input
sdo wi update --id 170 --state done
```

## Testing

### Unit Tests

All commands have comprehensive unit test coverage:

```bash
# Run all wi command tests
cd C:\source\ntools
nb UNIT_TEST_WORKITEM_COMMAND

# Output shows tests for create, update, comment, list, show
Passed!  - Failed: 0, Passed: 78, Skipped: 0, Total: 78
```

### State Translator Tests

Test state handling and translation:

```bash
# Run state translator tests
cd C:\source\ntools
nb UNIT_TEST_WORKITEM_STATE_TRANSLATOR

# Output shows 48 tests for parse, GitHub translation, Azure DevOps translation
Passed!  - Failed: 0, Passed: 48, Skipped: 0, Total: 48
```

### Manual Testing

Test with real repositories:

```bash
# Test with ntools repository
cd C:\source\ntools
sdo wi list --top 5
sdo wi show --id 243
sdo wi create --file-path test.md --dry-run
sdo wi update --id 243 --state Done

# Test with different repository
cd Path\To\Your\Repo
sdo wi list
sdo wi show --id <issue_number>
```

## Phase Status

### Phase 3.1 - ✅ Work Item Commands (COMPLETE)

#### Show & List
- ✅ Implement `wi show` subcommand - Display detailed work item information
- ✅ Implement `wi list` subcommand - List work items with filtering
- ✅ Default filtering (excludes done/closed to show active work)
- ✅ Support GitHub issues with full metadata
- ✅ Proper date deserialization and formatting
- ✅ Labels and assignee display
- ✅ 33 unit tests passing
- ✅ Real data verified with GitHub API
- ✅ Clean output formatting matching Python SDO

#### Create, Update, Comment
- ✅ Implement `wi create` subcommand - Create work items from markdown files
- ✅ Implement `wi update` subcommand - Update work item properties with state translation
- ✅ Implement `wi comment` subcommand - Add comments to work items
- ✅ Markdown parsing (title, description, acceptance criteria)
- ✅ JSON-patch API integration for Azure DevOps
- ✅ Platform-aware state translation (New/Approved/Committed/Done/To Do/In Progress)
- ✅ Dry-run support for preview before creation
- ✅ Verbose logging and success messages with state acknowledgement
- ✅ Comprehensive error handling with state guidance
- ✅ 48 unit tests for WorkItemStateTranslator
- ✅ GitHub and Azure DevOps API integration

### Phase 3.2 - Pending

- ⏳ Add filtering options (--type, --assigned-to, --assigned-to-me)
- ⏳ Enhanced query capabilities

### Phase 3.3-3.5 - Planned

- Repository commands (`repo create`, `repo list`, `repo show`, `repo delete`)
- Pull request commands (`pr merge`, `pr approve`, `pr list`, `pr show`)
- Pipeline commands (`pipeline create`, `pipeline run`, `pipeline status`, etc.)
- User/Permission commands (`user show`, `user list`, `user search`, `permissions`)

## Implementation Notes

**Phase 3.1 Show & List (March 11, 2026)**:
- ✅ **GitHub API Pagination**: Full pagination implemented to fetch all issues across multiple pages
- ✅ **State Filtering**: Default behavior excludes closed items; `--state closed` retrieves closed issues  
- ✅ **Result Limiting**: `--top` parameter correctly limits results after filtering
- ✅ All 33 unit tests passing with pagination
- ✅ Verified with real data: `wi list` returns all open issues

**Phase 3.1 State Management (March 18, 2026)**:
- ✅ **Canonical State Definitions**: 6 states from Python SDO (New, Approved, Committed, Done, To Do, In Progress)
- ✅ **State Translation**: Automatic conversion between Azure DevOps and GitHub states
- ✅ **WorkItemStateTranslator**: Centralized state handling with 48 unit tests
- ✅ **Error Handling**: Platform-specific error messages with supported states guidance
- ✅ **Success Messages**: Update commands show the state being changed to
- ✅ **Create from Markdown**: Support for file-driven creation with acceptance criteria
- ✅ **Dry-run Support**: Preview work items before creation
- ✅ **Comment Support**: Add discussion to work items on both platforms

**Platform State Handling**:
- **Azure DevOps**: Uses exact state names with proper casing ("To Do", "In Progress")
- **GitHub**: Translates states to open/closed binary model
- **Default List Behavior**: Excludes done/closed items to show active work
- **With --state**: Allows viewing items in any state including done/closed

## Development

### Building from Source

```bash
cd C:\source\ntools
nb build                                    # Build solution
nb test                                     # Run all tests
nb UNIT_TEST_WORKITEM_COMMAND              # Run wi command tests
nb UNIT_TEST_WORKITEM_STATE_TRANSLATOR     # Run state translator tests
```

### Source Code

- **Main Command**: [Sdo/Commands/WorkItemCommand.cs](../../Sdo/Commands/WorkItemCommand.cs)
- **GitHub Client**: [Sdo/Services/GitHubClient.cs](../../Sdo/Services/GitHubClient.cs)
- **Azure DevOps Client**: [Sdo/Services/AzureDevOpsClient.cs](../../Sdo/Services/AzureDevOpsClient.cs)
- **State Management**: [Sdo/Models/WorkItemState.cs](../../Sdo/Models/WorkItemState.cs)
- **Command Tests**: [SdoTests/WorkItemCommandTests.cs](../../SdoTests/WorkItemCommandTests.cs)
- **State Translator Tests**: [SdoTests/WorkItemStateTranslatorTests.cs](../../SdoTests/WorkItemStateTranslatorTests.cs)

### Architecture

The C# implementation follows the `System.CommandLine` framework for CLI parsing and includes:

- Automatic platform detection via `PlatformDetector`
- Async API calls via `GitHubClient` and `AzureDevOpsClient`
- Centralized state management via `WorkItemStateTranslator` with 6 canonical states
- Platform-aware state translation (Azure DevOps ↔ GitHub)
- Comprehensive error handling with platform-specific messages and state guidance
- Clean separation of concerns: Commands → Services → API Clients
- Markdown parsing for structured work item creation
- JSON-patch operations for Azure DevOps updates

## Related Documentation

- [sdo Installation Guide](sdo-installation.md) - Python version setup
- [sdo-migration-plan.md](sdo-migration-plan.md) - Full migration roadmap
- [NBuild System](../nbuild/README.md) - Build automation tool

## See Also

- GitHub: https://github.com/naz-hage/ntools
- Azure DevOps: https://dev.azure.com/naz-hage/ntools
- Issue #243: Phase 3 Command Implementation
