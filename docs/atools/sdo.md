# sdo - Simple DevOps Operations Tool

This page documents the `sdo` CLI tool. Use this page for detailed prerequisites, installation, usage examples, troubleshooting, and testing.

## Overview

`sdo` (Simple DevOps Operations) is a modern CLI tool that provides unified operations for both work items and repositories across Azure DevOps and GitHub platforms. It parses backlog-style markdown files to create work items and offers comprehensive repository management capabilities.

### Key Features

- **Work Item Creation**: Parse markdown files and create GitHub issues or Azure DevOps work items (PBIs, Bugs, Tasks)
- **Repository Management**: Create, list, show, and delete repositories on both platforms
- **Pull Request Management**: Create, list, show, and update pull requests with work item linking
- **Multi-Platform Support**: Seamless operations across Azure DevOps and GitHub
- **Dry-Run Previews**: Preview operations before execution
- **Automatic Platform Detection**: Detects platform from Git remote configuration

## Prerequisites

### Required Environment Variables

- `AZURE_DEVOPS_PAT` - Personal Access Token for Azure DevOps API access
- `GITHUB_TOKEN` - GitHub Personal Access Token (for GitHub operations)

### Python Requirements

- Python 3.8+
- Dependencies listed in `requirements.txt`

### Git Repository Context

The tool automatically detects Azure DevOps organization and project from the current Git remote configuration.

## Installation

For detailed installation instructions, see [SDO Installation](sdo-installation.md).

### Quick Installation (Virtual Environment)

```bash
# Navigate to NTools directory
cd "C:\Program Files\NBuild"

# Run the installer
python atools\install-sdo.py
```

### Prerequisites

- Python 3.8+
- Administrative privileges (recommended)

## Usage

### Basic Command Structure

```bash
sdo [OPTIONS] COMMAND [ARGS]...
```

### Global Options

- `-v, --verbose` - Show detailed API error information
- `--version` - Show version information

### Work Item Operations

#### Create Work Item

```bash
sdo workitem create --file-path FILE_PATH [OPTIONS]
```

**Options:**
- `-f, --file-path PATH` - Path to markdown file (required)
- `--dry-run` - Parse and preview without creating

#### List Work Items

```bash
sdo workitem list [OPTIONS]
```

Lists work items with optional filtering. Works with both Azure DevOps and GitHub.

**Options:**
- `--type TYPE` - Filter by work item type (PBI, Bug, Task, Spike, Epic)
- `--state STATE` - Filter by state (New, Approved, Committed, Done, To Do, In Progress)
- `--assigned-to EMAIL` - Filter by assigned user (email or display name)
- `--assigned-to-me` - Filter by work items assigned to current user
- `--top INTEGER` - Maximum number of items to return (default: 50)
- `-v, --verbose` - Show detailed API information and URLs

#### Show Work Item

```bash
sdo workitem show --id ID [OPTIONS]
```

Shows detailed work item information including acceptance criteria.

**Options:**
- `--id INTEGER` - Work item ID (required)
- `-c, --comments` - Show comments/discussion
- `-v, --verbose` - Show full API response

#### Update Work Item

```bash
sdo workitem update --id ID [OPTIONS]
```

Updates work item fields.

**Options:**
- `--id INTEGER` - Work item ID (required)
- `--title TEXT` - Update title
- `--description TEXT` - Update description
- `--assigned-to EMAIL` - Update assigned user (email or display name)
- `--state STATE` - Update state (New, Approved, Committed, Done, To Do, In Progress)
- `-v, --verbose` - Show full API response

#### Add Comment to Work Item

```bash
sdo workitem comment --id ID --text TEXT [OPTIONS]
```

Adds a comment to a work item or issue.

**Options:**
- `--id INTEGER` - Work item ID (required)
- `--text TEXT` - Comment text (required)
- `-v, --verbose` - Show full API response

### Repository Operations

#### Create Repository

```bash
sdo repo create [OPTIONS]
```

Creates a new repository in the current project/organization.

**Options:**
- `-v, --verbose` - Show detailed API information

#### List Repositories

```bash
sdo repo ls [OPTIONS]
```

Lists all repositories in the current project/organization.

**Options:**
- `-v, --verbose` - Show detailed API information

#### Show Repository

```bash
sdo repo show [OPTIONS]
```

Shows detailed information about the current repository.

**Options:**
- `-v, --verbose` - Show detailed API information

#### Delete Repository

```bash
sdo repo delete [OPTIONS]
```

Deletes the current repository (with confirmation prompt).

**Options:**
- `-v, --verbose` - Show detailed API information

### Examples

#### Work Item Operations

##### Create Work Item

###### Dry Run (Preview)

```bash
sdo workitem create --file-path "atools/issue-azdo-example.md" --dry-run
```

###### Create Azure DevOps Work Item

```bash
sdo workitem create --file-path "atools/issue-azdo-example.md"
```

###### Create with Verbose Output

```bash
sdo workitem create --file-path "atools/issue-azdo-example.md" --verbose
```

##### List Work Items

```bash
# List all work items
sdo workitem list

# List only PBIs in "New" state
sdo workitem list --type PBI --state New

# List work items assigned to you
sdo workitem list --assigned-to-me

# List tasks in progress with verbose output
sdo workitem list --type Task --state "In Progress" --verbose

# List top 10 work items
sdo workitem list --top 10
```

##### Show Work Item

```bash
# Show work item details
sdo workitem show --id 123

# Show work item with comments
sdo workitem show --id 123 --comments

# Show with verbose API response
sdo workitem show --id 123 --verbose
```

##### Update Work Item

```bash
# Update work item state
sdo workitem update --id 123 --state "In Progress"

# Update title and description
sdo workitem update --id 123 --title "New Title" --description "Updated description"

# Assign work item to user
sdo workitem update --id 123 --assigned-to "user@example.com"

# Update multiple fields
sdo workitem update --id 123 --state Done --assigned-to "user@example.com" --verbose
```

##### Add Comment

```bash
# Add a comment to work item
sdo workitem comment --id 123 --text "This is a test comment"

# Add comment with verbose output
sdo workitem comment --id 123 --text "Completed testing" --verbose
```

#### Repository Operations

##### Create Repository

```bash
sdo repo create
sdo repo create --verbose
```

##### List Repositories

```bash
sdo repo ls
sdo repo list  # Alternative command
```

##### Show Repository Details

```bash
sdo repo show
sdo repo show --verbose
```

##### Delete Repository

```bash
sdo repo delete  # Will prompt for confirmation
```

### Pull Request Operations

#### Create Pull Request

```bash
sdo pr create [OPTIONS]
```

Creates a new pull request from a markdown file.

**Options:**
- `-f, --file PATH` - Path to markdown file containing PR details (required)
- `--work-item INTEGER` - Work item ID to link to the pull request (required)
- `--draft` - Create as draft pull request
- `--dry-run` - Parse and preview PR creation without creating it
- `-v, --verbose` - Show detailed API information and responses

**Examples:**
```bash
# Create PR with dry-run preview
sdo pr create -f pr-description.md --work-item 123 --dry-run

# Create draft PR
sdo pr create -f pr-description.md --work-item 123 --draft

# Create PR with verbose output
sdo pr create -f pr-description.md --work-item 123 --verbose
```

#### List Pull Requests

```bash
sdo pr ls [OPTIONS]
```

Lists pull requests in the current repository.

**Options:**
- `--status STATUS` - Filter by status (active, completed, abandoned) [default: active]
- `--top INTEGER` - Maximum number of PRs to show [default: 10]
- `-v, --verbose` - Show detailed API information

**Examples:**
```bash
# List active PRs
sdo pr ls

# List completed PRs
sdo pr ls --status completed

# List with custom limit
sdo pr ls --top 20

# List with verbose output
sdo pr ls --verbose
```

#### Show Pull Request Details

```bash
sdo pr show PR_NUMBER [OPTIONS]
```

Shows detailed information about a specific pull request.

**Options:**
- `-v, --verbose` - Show detailed API information

**Examples:**
```bash
# Show PR details
sdo pr show 123

# Show PR with verbose API details
sdo pr show 123 --verbose
```

#### Check Pull Request Status

```bash
sdo pr status PR_NUMBER [OPTIONS]
```

Shows the current status of a specific pull request including CI/CD check results.

Displays:
- PR status (open, closed, merged, etc.)
- Title, author, and branch information
- CI/CD checks status (GitHub and Azure DevOps)
- Check results with pass/fail indicators and URLs

**Options:**
- `-v, --verbose` - Show detailed API information

**Examples:**
```bash
# Check PR status with CI/CD checks
sdo pr status 123

# Check PR status with verbose details
sdo pr status 123 --verbose
```

#### Update Pull Request

```bash
sdo pr update [OPTIONS]
```

Updates an existing pull request.

**Options:**
- `--pr-id INTEGER` - Pull request ID to update (required)
- `-f, --file PATH` - Path to markdown file with updated PR details
- `-t, --title TEXT` - New title for the PR
- `--status STATUS` - New status (active, abandoned, completed)
- `-v, --verbose` - Show detailed API information

**Examples:**
```bash
# Update PR title
sdo pr update --pr-id 123 --title "Updated PR Title"

# Update PR from markdown file
sdo pr update --pr-id 123 --file updated-pr.md

# Update PR status to completed
sdo pr update --pr-id 123 --status completed
```

### Examples

#### Pull Request Operations

##### Create Pull Request from Markdown

```bash
sdo pr create -f pr-description.md --work-item 208
```

##### Preview PR Creation

```bash
sdo pr create -f pr-description.md --work-item 208 --dry-run
```

##### List Recent Pull Requests

```bash
sdo pr ls --top 5
```

##### Show Pull Request Details

```bash
sdo pr show 211
```

##### Check Pull Request Status

```bash
sdo pr status 211
```

**Output includes CI/CD checks:**
```
✅ PR #211: MERGED
Title: [208] Implement Comprehensive Pull Request Management for SDO CLI
Author: naz-hage
Branch: gh-208 → main

CI/CD Checks:
Python Tests    pass    27s     https://github.com/naz-hage/ntools/actions/runs/...
Publish Docs    pass    13s     https://github.com/naz-hage/ntools/actions/runs/...
Running a workflow (Release)    pass    3m24s   https://github.com/naz-hage/ntools/actions/runs/...
```

##### Update Pull Request

```bash
sdo pr update --pr-id 211 --title "Updated: Implement PR Management"
```

## Markdown File Format

SDO expects markdown files with specific metadata headers. See the example files:

**Azure DevOps Examples:**
- [issue-azdo-bug-example.md](issue-azdo-bug-example.md) - Bug work item format
- [issue-azdo-task-example.md](issue-azdo-task-example.md) - Task work item format
- [issue-azdo-pbi-example.md](issue-azdo-pbi-example.md) - Product Backlog Item format
- [issue-azdo-epic-example.md](issue-azdo-epic-example.md) - Epic work item format

**GitHub Examples:**
- [issue-gh-example.md](issue-gh-example.md) - GitHub issue format

### Required Metadata

```markdown
# Issue Title

## Target: azdo|github
## Type: PBI|Bug|Task|Issue
## Project: ProjectName (for Azure DevOps)

Description content here...
```

## Supported Platforms

### Azure DevOps
- **Work Item Types**: Product Backlog Item (PBI), Bug, Task
- **Work Item Fields**: Title, Description, Area Path, Iteration Path, Priority, Effort
- **Repository Operations**: Create, List, Show, Delete repositories
- **Pull Request Operations**: Create, List, Show, Update pull requests with work item linking
- **Authentication**: Personal Access Token (PAT)
- **API**: REST API integration
- **Automatic**: Organization/Project detection from Git remote

### GitHub
- **Work Item Types**: Issues
- **Work Item Fields**: Title, Description, Labels, Assignees
- **Repository Operations**: Create, List, Show, Delete repositories
- **Pull Request Operations**: Create, List, Show, Update pull requests with issue references
- **Authentication**: GitHub CLI (`gh`) or Personal Access Token
- **API**: GitHub CLI integration for repositories, REST API for work items and PRs
- **Automatic**: Repository detection from Git remote

## Error Handling

SDO provides clear error messages for common issues:

- **Authentication**: Missing or invalid PAT tokens
- **File Access**: Markdown file not found or unreadable
- **API Errors**: Azure DevOps/GitHub API failures
- **Validation**: Missing required metadata fields

Use `--verbose` flag for detailed error information including API responses.

## Testing

### Run All Tests

```bash
cd atools
python -m pytest tests/
```

### Test Categories

- **Unit Tests**: Core functionality validation
- **Integration Tests**: API interaction testing
- **CLI Tests**: Command-line interface validation
- **Error Path Tests**: Failure scenario handling

### Test Coverage

All major functionality is tested with comprehensive test cases covering:
- Work item creation workflows
- Repository management operations
- Pull request management operations (create, list, show, update)
- Multi-platform API integrations
- Error handling and validation
- CLI command interfaces
- Markdown parsing and metadata extraction
- Platform-specific operations
- Error handling scenarios
- CLI argument parsing

## Troubleshooting

### Common Issues

#### Authentication Failures

**Error:** `Authentication failed. Check your AZURE_DEVOPS_PAT token.`

**Solution:**
```bash
# Set PAT token
export AZURE_DEVOPS_PAT="your-token-here"

# Verify token has required permissions
# Azure DevOps: Work Items (Read/Write), Project (Read)
# GitHub: Issues (Read/Write), Repository (Read)
```

#### Git Remote Configuration

**Error:** `Unable to detect Azure DevOps organization from Git remote`

**Solution:**
```bash
# Check current remote
git remote -v

# Should show Azure DevOps URL like:
# origin  https://dev.azure.com/organization/project/_git/repo (fetch)
```

#### File Format Issues

**Error:** `Missing required metadata: target`

**Solution:** Ensure markdown file includes required headers:
```markdown
## Target: azdo
## Type: PBI
```

### Debug Mode

Enable verbose output for detailed troubleshooting:

```bash
sdo --verbose workitem create --file-path file.md
```

This shows:
- API request/response details
- File parsing results
- Authentication status
- Full error stack traces

## Architecture

SDO is built with a modular architecture:

- **`cli.py`** - Click-based command interface
- **`client.py`** - Azure DevOps REST API client
- **`work_items.py`** - Business logic for work item operations
- **`pull_requests.py`** - Business logic for pull request operations
- **`platforms/`** - Platform-specific implementations (GitHub, Azure DevOps)
  - `github_pr_platform.py` - GitHub pull request operations
  - `azdo_pr_platform.py` - Azure DevOps pull request operations
- **`parsers/`** - Markdown parsing and metadata extraction

## Development

### Code Quality

SDO maintains high code quality standards:
- **Linting**: flake8 with comprehensive rules
- **Testing**: 34 test cases with high coverage
- **Type Hints**: Full type annotation support
- **Documentation**: Comprehensive docstrings

### Contributing

1. Follow existing code patterns
2. Add tests for new functionality
3. Update documentation
4. Run full test suite before submitting