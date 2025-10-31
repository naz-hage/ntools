# sdo - Simple DevOps Operations Tool

This page documents the `sdo` CLI tool. Use this page for detailed prerequisites, installation, usage examples, troubleshooting, and testing.

## Overview

`sdo` (Simple DevOps Operations) is a modern CLI tool that parses backlog-style markdown files and creates GitHub issues or Azure DevOps work items (PBIs, Bugs, or Tasks). It supports dry-run previews and follows a file-first metadata policy.

## Prerequisites

### Required Environment Variables

- `AZURE_DEVOPS_PAT` or `AZURE_DEVOPS_EXT_PAT` - Personal Access Token for Azure DevOps API access
- `GITHUB_TOKEN` - GitHub Personal Access Token (for GitHub operations)

### Python Requirements

- Python 3.8+
- Dependencies listed in `requirements.txt`

### Git Repository Context

The tool automatically detects Azure DevOps organization and project from the current Git remote configuration.

## Installation

### From Source

```bash
cd atools
pip install -r requirements.txt
```

### As Module

```bash
python -m sdo_package.cli --help
```

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

### Examples

#### Dry Run (Preview)

```bash
sdo workitem create --file-path "atools/issue-azdo-example.md" --dry-run
```

#### Create Azure DevOps Work Item

```bash
sdo workitem create --file-path "atools/issue-azdo-example.md"
```

#### Create with Verbose Output

```bash
sdo workitem create --file-path "atools/issue-azdo-example.md" --verbose
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
- **Fields**: Title, Description, Area Path, Iteration Path, Priority, Effort
- **Automatic**: Organization/Project detection from Git remote

### GitHub
- **Issue Types**: Issues
- **Fields**: Title, Description, Labels, Assignees
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

All major functionality is tested with 34 test cases covering:
- Work item creation workflows
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