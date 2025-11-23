# sdo - Simple DevOps Operations Tool

This page documents the `sdo` CLI tool. Use this page for detailed prerequisites, installation, usage examples, troubleshooting, and testing.

## Overview

`sdo` (Simple DevOps Operations) is a modern CLI tool that provides unified operations for both work items and repositories across Azure DevOps and GitHub platforms. It parses backlog-style markdown files to create work items and offers comprehensive repository management capabilities.

### Key Features

- **Work Item Creation**: Parse markdown files and create GitHub issues or Azure DevOps work items (PBIs, Bugs, Tasks)
- **Repository Management**: Create, list, show, and delete repositories on both platforms
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

##### Dry Run (Preview)

```bash
sdo workitem create --file-path "atools/issue-azdo-example.md" --dry-run
```

##### Create Azure DevOps Work Item

```bash
sdo workitem create --file-path "atools/issue-azdo-example.md"
```

##### Create with Verbose Output

```bash
sdo workitem create --file-path "atools/issue-azdo-example.md" --verbose
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

## Markdown File Format

SDO expects markdown files with specific metadata headers. See the example files:

- `issue-azdo-example.md` - Azure DevOps work item format
- `issue-gh-example.md` - GitHub issue format

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
- **Authentication**: Personal Access Token (PAT)
- **API**: REST API integration
- **Automatic**: Organization/Project detection from Git remote

### GitHub
- **Work Item Types**: Issues
- **Work Item Fields**: Title, Description, Labels, Assignees
- **Repository Operations**: Create, List, Show, Delete repositories
- **Authentication**: GitHub CLI (`gh`) or Personal Access Token
- **API**: GitHub CLI integration for repositories, REST API for work items
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

All major functionality is tested with 161 test cases covering:
- Work item creation workflows
- Repository management operations
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
- **`platforms/`** - Platform-specific implementations (GitHub, Azure DevOps)
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