# SDO Architecture Documentation

## Overview

SDO (Simple DevOps Operations Tool) is a command-line interface tool designed to streamline work item creation and repository management across multiple DevOps platforms including Azure DevOps and GitHub. The tool follows a modular, extensible architecture that allows for easy addition of new platforms and operations.

## File Structure

```
atools/
├── sdo.py                          # Main entry point and CLI framework
├── sdo_package/                    # Main package directory
│   ├── __init__.py                 # Package initialization
│   ├── cli.py                      # CLI command implementations
│   ├── client.py                   # Azure DevOps REST API client
│   ├── exceptions.py               # Custom exception classes
│   ├── pull_requests.py            # Pull request operations (multi-platform)
│   ├── pipelines.py                # Pipeline operations (multi-platform)
│   ├── repositories.py             # Repository operations (multi-platform)
│   ├── version.py                  # Version information
│   ├── work_items.py               # Work item orchestration logic
│   ├── parsers/                    # Content parsing modules
│   │   ├── __init__.py
│   │   ├── markdown_parser.py      # Markdown file parsing
│   │   └── metadata_parser.py      # Metadata extraction and platform detection
│   └── platforms/                  # Platform-specific implementations
│       ├── __init__.py
│       ├── base.py                 # Abstract base classes for platforms
│       ├── pr_base.py              # Abstract base class for PR platforms
│       ├── azdo_platform.py        # Azure DevOps work item operations
│       ├── azdo_pr_platform.py     # Azure DevOps pull request operations
│       ├── azdo_pipeline_platform.py # Azure DevOps pipeline operations
│       ├── github_platform.py      # GitHub work item operations
│       └── github_pr_platform.py   # GitHub pull request operations
├── tests/                          # Test suite
│   ├── run_sdo_tests.py            # Test runner script
│   ├── test_azdo_platform.py       # Azure DevOps platform tests
│   ├── test_client.py              # Client module tests
│   ├── test_cli_comprehensive.py   # Comprehensive CLI tests
│   ├── test_github_platform.py     # GitHub platform tests
│   ├── test_markdown_parser.py     # Markdown parser tests
│   ├── test_pull_requests.py       # Pull request operations tests
│   ├── test_pipelines.py           # Pipeline operations tests
│   ├── test_repositories.py        # Repository operations tests
│   ├── test_sdo.py                 # Main module tests
│   ├── test_sdo_cli.py             # CLI integration tests
│   ├── test_sdo_workitems.py       # Work item tests
│   └── test_sdo_pipelines.py       # Pipeline tests
├── pyproject.toml                  # Python project configuration
├── requirements.txt                # Production dependencies
├── requirements-dev.txt            # Development dependencies
├── issue-azdo-example.md           # Azure DevOps issue template
├── issue-gh-example.md             # GitHub issue template
└── install-sdo.py                  # Installation script
```

## Architecture Principles

### 1. **Separation of Concerns**
- **CLI Layer**: User interaction and command handling
- **Business Logic**: Work item orchestration and validation
- **Platform Layer**: Platform-specific implementations
- **Parser Layer**: Content extraction and metadata processing
- **Client Layer**: API communication and authentication

### 2. **Strategy Pattern**
- Abstract platform interface allows seamless switching between DevOps platforms
- Platform-specific implementations handle unique API requirements
- Consistent interface across all supported platforms

### 3. **Extensibility**
- Plugin-style architecture for adding new platforms
- Modular parser system for different content formats
- Configurable metadata extraction

## System Architecture

SDO uses a **domain-driven architecture** with separate platform abstractions for each business domain. Each domain (work items, repositories, pipelines, pull requests) has its own business logic module and platform interface because the operations are fundamentally different between Azure DevOps and GitHub.

```
┌─────────────────────────────────────────────────────────────────┐
│                         SDO CLI Tool                            │
├─────────────────────────────────────────────────────────────────┤
│  sdo.py (Main Entry Point)                                      │
│  ├── Click CLI Framework                                        │
│  ├── Command Registration                                       │
│  └── Global Configuration                                       │
└─────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────┐
│                    CLI Layer (cli.py)                           │
├─────────────────────────────────────────────────────────────────┤
│  ├── workitem group: create, list, show, update, comment        │
│  ├── repo group: create, show, ls, delete                       │
│  ├── pr group: create, show, status, ls, update                 │
│  ├── pipeline group: create, show, ls, run, status, logs        │
│  ├── Command argument parsing and validation                    │
│  ├── Verbose output management                                  │
│  └── Error handling and user feedback                           │
└─────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────┐
│                 Business Logic Layer                            │
├─────────────────────────────────────────────────────────────────┤
│  ├── work_items.py: Work item orchestration                     │
│  ├── repositories.py: Repository operations                     │
│  ├── pull_requests.py: PR operations                            │
│  └── pipelines.py: Pipeline operations                          │
│                                                                 │
│  Each module:                                                   │
│  ├── Platform detection and selection                           │
│  ├── Content processing workflow coordination                   │
│  ├── Result validation and reporting                            │
│  └── Error handling and user feedback                           │
└─────────────────────────────────────────────────────────────────┘
                                    │
                    ┌───────────────┼───────────────┐
                    │               │               │
                    ▼               ▼               ▼
    ┌─────────────────────┐ ┌─────────────────┐ ┌─────────────────┐
    │   Parser Layer      │ │ Platform Layer  │ │  Client Layer   │
    │   (parsers/)        │ │ (platforms/)    │ │   (client.py)   │
    │                     │ │                 │ │                 │
    │  Content parsing:   │ │ Domain-specific │ │ Low-level API:  │
    │  ├── Markdown files │ │ abstractions:   │ │ ├── HTTP client │
    │  ├── Metadata       │ │ ├── Work items  │ │ ├── Auth        │
    │  └── Validation     │ │ ├── Repos       │ │ ├── Platform    │
    │                     │ │ ├── Pipelines   │ │ │   detection   │
    │                     │ │ └── PRs         │ │ └── Logging     │
    └─────────────────────┘ └─────────────────┘ └─────────────────┘
```

### Architecture Principles

#### 1. **Domain-Driven Design**
Each business domain (work items, repositories, pipelines, PRs) has:
- **Dedicated business logic module** (work_items.py, repositories.py, etc.)
- **Separate platform abstractions** because Azure DevOps and GitHub APIs are fundamentally different
- **Independent evolution** allowing each domain to be enhanced separately

#### 2. **Platform Abstraction Strategy**
```
Business Logic Module → Platform Interface → Platform Implementation → Client Layer

Example for Work Items:
work_items.py → WorkItemPlatform → azdo_platform.py/github_platform.py → client.py

Example for Repositories:
repositories.py → RepositoryPlatform → AzureDevOpsRepoPlatform/GitHubRepoPlatform → client.py
```

#### 3. **Why Separate Platform Interfaces**
Work item operations are too different to share common code:
- **Azure DevOps**: Complex work item types, parent-child relationships, acceptance criteria fields
- **GitHub**: Simple issue model, issue references, labels, CLI-based operations

Attempting consolidation would create more complexity than separation.

## Component Details

### 1. Main Entry Point (sdo.py)
**Purpose**: Simple bootstrap and CLI delegation
**Responsibilities**:
- Import CLI components from sdo_package.cli
- Provide backward compatibility for tests
- Delegate execution to the main CLI function

**Key Features**:
- Minimal bootstrap code
- Exposes CLI app for testing
- Simple delegation pattern

### 2. CLI Layer (cli.py)
**Purpose**: Command implementations and user interaction
**Responsibilities**:
- Hierarchical command structure with groups and subcommands
- Work item operations (`sdo workitem create`)
- Repository operations (`sdo repo create/show/ls/delete`)
- Pull request operations (`sdo pr create/show/status/ls/update`)
- Input validation and sanitization
- User feedback and progress reporting
- Error presentation to users

**Command Structure**:
```
sdo
├── workitem
│   ├── create          # Create work items from markdown files
│   ├── list            # List work items with filtering
│   ├── show            # Show work item details
│   ├── update          # Update work item fields
│   └── comment         # Add comments to work items
├── repo
│   ├── create          # Create repositories
│   ├── show            # Show repository information
│   ├── ls              # List repositories
│   └── delete          # Delete repositories
├── pr
│   ├── create          # Create pull requests from markdown files
│   ├── show            # Show pull request details
│   ├── status          # Check pull request status
│   ├── ls              # List pull requests
│   └── update          # Update pull request
├── pipeline
│   ├── create          # Create pipelines
│   ├── show            # Show pipeline information
│   ├── ls              # List pipelines
│   ├── run             # Run pipelines
│   ├── status          # Check pipeline status
│   ├── logs            # View pipeline logs
│   ├── lastbuild       # Show last build information
│   ├── update          # Update pipeline configuration
│   └── delete          # Delete pipelines
└── add-issue           # Legacy compatibility command
```

**Key Features**:
- Rich command-line experience with Click
- Comprehensive input validation
- Detailed verbose output for debugging
- Graceful error handling with helpful messages
- Global `--verbose` flag support

### 3. Business Logic Layer (work_items.py, repositories.py, pull_requests.py & pipelines.py)
**Purpose**: Core application logic and orchestration
**Responsibilities**:
- Content processing workflow coordination
- Platform detection and selection
- Work item creation orchestration (work_items.py)
- Repository operations orchestration (repositories.py)
- Pull request operations orchestration (pull_requests.py)
- Pipeline operations orchestration (pipelines.py)
- Result validation and reporting

**Key Functions**:
- `cmd_workitem_create/list/show/update/comment()`: Work item operation workflows
- `cmd_repo_create/show/ls/delete()`: Repository operation workflows
- `cmd_pr_create/show/status/ls/update()`: Pull request operation workflows
- `cmd_pipeline_create/show/ls/run/status/logs/lastbuild/update/delete()`: Pipeline operation workflows
- `get_pr_platform()`: Platform factory for pull request operations
- `get_pipeline_platform()`: Platform factory for pipeline operations
- Platform factory functions for creating appropriate platform instances
- Result handling and user feedback

**Architecture Note**: Repository, pull request, and pipeline operations use abstract base classes with platform-specific implementations, providing a consistent interface across Azure DevOps and GitHub.

### 4. Parser Layer (parsers/)
**Purpose**: Content extraction and metadata processing

#### MarkdownParser (markdown_parser.py)
**Responsibilities**:
- Markdown file parsing and content extraction
- Section identification (title, description, acceptance criteria)
- Content normalization and validation

**Key Features**:
- Robust markdown parsing with error handling
- Section-based content extraction
- Support for various markdown formats

#### MetadataParser (metadata_parser.py)
**Responsibilities**:
- Platform detection from file content
- Metadata extraction and validation
- Configuration parsing

**Key Features**:
- Multiple platform detection strategies
- Flexible metadata extraction
- Validation and error reporting

### 5. Platform Layer (platforms/)
**Purpose**: Platform-specific implementations

#### Base Platform (base.py)
**Abstract Interface**:
```python
class WorkItemPlatform(ABC):
    @abstractmethod
    def get_config(self) -> Dict[str, str]:
        """Get platform-specific configuration."""
        
    @abstractmethod
    def validate_auth(self) -> bool:
        """Validate authentication for the platform."""
        
    @abstractmethod
    def create_work_item(
        self,
        title: str,
        description: str,
        metadata: Dict[str, Any],
        acceptance_criteria: Optional[List[str]] = None,
        dry_run: bool = False
    ) -> Optional[Dict[str, Any]]:
        """Create a work item on the platform."""
```

**Repository Platform Interface**:
```python
class RepositoryPlatform(ABC):
    @abstractmethod
    def create_repository(self, name: str, **kwargs) -> Dict[str, Any]:
        """Create a repository."""
        
    @abstractmethod
    def get_repository(self, name: str) -> Optional[Dict[str, Any]]:
        """Get repository information."""
        
    @abstractmethod
    def list_repositories(self) -> List[Dict[str, Any]]:
        """List all repositories."""
        
    @abstractmethod
    def delete_repository(self, name: str) -> bool:
        """Delete a repository."""
```

#### Azure DevOps Platform (azdo_platform.py)
**Responsibilities**:
- Azure DevOps REST API integration
- Work item creation via REST API
- Authentication and authorization
- Azure DevOps-specific field mapping

**Key Features**:
- PAT (Personal Access Token) authentication
- Support for multiple work item types
- Rich error handling for API failures
- Verbose logging for debugging

#### GitHub Platform (github_platform.py)
**Responsibilities**:
- GitHub CLI integration
- Issue creation via GitHub CLI
- Repository context detection
- GitHub-specific field mapping

**Key Features**:
- GitHub CLI command execution
- Automatic repository detection
- Label and milestone support
- Error handling for CLI failures

#### Pull Request Platform (pr_base.py)
**Abstract Interface**:
```python
class PRPlatform(ABC):
    @abstractmethod
    def create_pull_request(
        self,
        title: str,
        description: str,
        source_branch: Optional[str] = None,
        target_branch: Optional[str] = None,
        work_item_id: Optional[int] = None,
        draft: bool = False
    ) -> str:
        """Create a new pull request."""
        
    @abstractmethod
    def get_pull_request(self, pr_number: int) -> Dict[str, Any]:
        """Get details of a specific pull request."""
        
    @abstractmethod
    def list_pull_requests(
        self,
        state: str = "open",
        author: Optional[str] = None,
        limit: int = 10
    ) -> List[Dict[str, Any]]:
        """List pull requests with optional filtering."""
        
    @abstractmethod
    def approve_pull_request(self, pr_number: int) -> bool:
        """Approve a pull request."""
        
    @abstractmethod
    def update_pull_request(
        self,
        pr_number: int,
        title: Optional[str] = None,
        description: Optional[str] = None,
        status: Optional[str] = None
    ) -> bool:
        """Update an existing pull request."""
        
    @abstractmethod
    def validate_auth(self) -> bool:
        """Validate that the platform is properly authenticated."""
```

#### Azure DevOps PR Platform (azdo_pr_platform.py)
**Responsibilities**:
- Azure DevOps Pull Request REST API integration
- PR creation with work item linking
- PR status and review management
- Work item reference validation

**Key Features**:
- Work item linking and validation
- Draft PR support
- Branch auto-detection
- Comprehensive error handling

#### GitHub PR Platform (github_pr_platform.py)
**Responsibilities**:
- GitHub CLI integration for PRs
- PR creation with issue references
- Issue linking extraction from PR description
- PR management via gh CLI

**Key Features**:
- GitHub CLI command execution
- Automatic issue reference detection
- Draft PR support
- Repository context detection

#### Pipeline Platform Interface
**Abstract Interface**:
```python
class PipelinePlatform(ABC):
    @abstractmethod
    def create_pipeline(self, name: str, **kwargs) -> Dict[str, Any]:
        """Create a new pipeline."""
        
    @abstractmethod
    def get_pipeline(self, name: str) -> Optional[Dict[str, Any]]:
        """Get pipeline information."""
        
    @abstractmethod
    def list_pipelines(self, repo_filter: Optional[str] = None) -> List[Dict[str, Any]]:
        """List pipelines with optional repository filtering."""
        
    @abstractmethod
    def run_pipeline(self, name: str, branch: str) -> Dict[str, Any]:
        """Run a pipeline on specified branch."""
        
    @abstractmethod
    def get_pipeline_status(self, build_id: Optional[int] = None) -> Dict[str, Any]:
        """Get pipeline build status."""
        
    @abstractmethod
    def get_pipeline_logs(self, build_id: int) -> str:
        """Get logs for a pipeline build."""
        
    @abstractmethod
    def delete_pipeline(self, name: str) -> bool:
        """Delete a pipeline."""
```

#### Azure DevOps Pipeline Platform (azdo_pipeline_platform.py)
**Responsibilities**:
- Azure DevOps Pipeline REST API integration
- Pipeline creation, execution, and monitoring
- Build status and log retrieval
- Pipeline configuration management

**Key Features**:
- Full pipeline lifecycle management
- Build queue and execution tracking
- Comprehensive logging and status reporting
- Integration with Azure DevOps build system

### 7. Repository Operations (repositories.py)
**Purpose**: Multi-platform repository management
**Responsibilities**:
- Repository creation, listing, and deletion
- Platform-specific repository operations
- Git remote parsing for platform detection
- Unified interface across platforms

**Architecture**:
- `RepositoryPlatform`: Abstract base class for repository operations
- `AzureDevOpsRepositoryPlatform`: Azure DevOps REST API implementation
- `GitHubRepositoryPlatform`: GitHub CLI implementation
- `create_repository_platform()`: Factory function for platform selection

**Key Features**:
- Platform-agnostic repository operations
- Automatic platform detection from Git remotes
- Consistent error handling and logging
- Support for both Azure DevOps and GitHub repositories

### 8. Pipeline Operations (pipelines.py)
**Purpose**: Multi-platform CI/CD pipeline management
**Responsibilities**:
- Pipeline creation, execution, monitoring, and management
- Platform-specific pipeline operations
- Build status tracking and log retrieval
- Unified interface across platforms

**Architecture**:
- `PipelinePlatform`: Abstract base class for pipeline operations
- `AzureDevOpsPipelinePlatform`: Azure DevOps REST API implementation
- `create_pipeline_platform()`: Factory function for platform selection

**Key Features**:
- Platform-agnostic pipeline operations
- Automatic platform detection from Git remotes
- Comprehensive build monitoring and logging
- Support for both Azure DevOps and GitHub Actions pipelines

### 6. Client Layer (client.py)
**Purpose**: Low-level API communication and platform utilities
**Responsibilities**:
- HTTP request management for Azure DevOps API
- Authentication handling for both platforms
- Request/response logging
- Connection validation
- Platform detection from Git remotes
- GitHub CLI command execution
- Cross-platform utility functions

**Key Components**:
- `AzureDevOpsClient`: REST API client for Azure DevOps
- `extract_platform_info_from_git()`: Platform detection logic
- GitHub CLI integration functions
- Authentication validation functions
- Comprehensive verbose logging with secure token redaction

**Key Features**:
- Comprehensive verbose logging
- Secure token handling with redaction
- Retry logic for transient failures
- Platform-agnostic utility functions
- Detailed error context

## Data Flow

### 1. Command Execution Flow
```
User Command → CLI Parser (cli.py) → Business Logic Module
     ↓
Business Logic: Content Parsing (Parser Layer) → Platform Selection → Platform Implementation → Client Layer (API/CLI calls)
     ↓
Result Reporting ← Success/Error Aggregation ← Platform Response
```

### 2. Domain-Specific Flows

#### Work Items Flow:
```
sdo workitem create file.md
    ↓
CLI (cli.py) → work_items.py → MarkdownParser → Platform Detection
    ↓
WorkItemPlatform → azdo_platform.py/github_platform.py → AzureDevOpsClient/GitHub CLI
    ↓
API Response → Result Formatting → User Output
```

#### Repository Flow:
```
sdo repo create
    ↓
CLI (cli.py) → repositories.py → Git Remote Parsing → Platform Detection
    ↓
RepositoryPlatform → AzureDevOpsRepoPlatform/GitHubRepoPlatform → Client Layer
    ↓
API Response → Result Formatting → User Output
```

#### Pipeline Flow:
```
sdo pipeline run --branch main
    ↓
CLI (cli.py) → pipelines.py → Git Remote Parsing → Platform Detection
    ↓
PipelinePlatform → AzureDevOpsPipelinePlatform → AzureDevOpsClient
    ↓
Build Queue Response → Status Monitoring → User Output
```

### 3. Error Handling Flow
```
Exception Occurrence → Platform-Specific Handler → Business Logic Error Handler → CLI Error Presenter
     ↓
User-Friendly Message ← Error Context Aggregation ← Exception Details
```

## Security Considerations

### 1. **Authentication Management**
- PAT tokens stored securely in environment variables
- Token redaction in all logging output
- No credential storage in configuration files

### 2. **Input Validation**
- All user inputs validated before processing
- File path validation to prevent directory traversal
- Content sanitization to prevent injection attacks

### 3. **API Security**
- HTTPS-only communication with external APIs
- Proper error handling to avoid information disclosure
- Request timeout handling to prevent hanging

## Configuration Management

### 2. **Environment Variables**
- `AZURE_DEVOPS_PAT`: Azure DevOps Personal Access Token
- `GITHUB_TOKEN`: GitHub authentication token (optional if using GitHub CLI)

### 2. **File-based Configuration**
- Platform detection from markdown metadata
- Template-based work item creation
- Flexible field mapping configurations

## Testing Strategy

### 1. **Unit Testing**
- Individual component testing with mocking
- Parser validation with various input formats
- Platform implementation testing with mock APIs

### 2. **Integration Testing**
- End-to-end workflow testing
- Real API integration testing (with test accounts)
- Cross-platform compatibility testing

### 3. **Performance Testing**
- Large file processing performance
- API response time testing
- Memory usage optimization

## Extensibility Points

### 1. **Adding New Platforms**
1. Implement `WorkItemPlatform` abstract base class
2. Add platform detection logic to `MetadataParser`
3. Register platform in platform factory
4. Add platform-specific configuration options

### 2. **Adding New Content Parsers**
1. Create parser class with standardized interface
2. Add content type detection logic
3. Register parser in parser factory
4. Add parser-specific configuration options

### 3. **Adding New Work Item Types**
1. Extend platform implementations with new type support
2. Add type-specific field mappings
3. Update validation logic for new types
4. Add type-specific templates

## Performance Characteristics

### 1. **Scalability**
- Designed for single work item operations
- Efficient parsing for files up to 10MB
- Parallel processing capabilities for future enhancement

### 2. **Resource Usage**
- Minimal memory footprint
- Efficient file I/O operations
- Optimized API request patterns

### 3. **Response Times**
- Local file parsing: < 100ms
- API operations: 1-5 seconds (depending on platform)
- Total operation time: typically < 10 seconds

## Future Enhancements

### 1. **Planned Features**
- Bulk work item creation from multiple files
- Template management system for work items
- Advanced field mapping configurations for custom work item types
- Interactive mode for work item creation
- Repository cloning and initialization features
- Branch management operations
- PR review and approval workflows
- PR merge operations with conflict resolution
- Enhanced pipeline monitoring and alerting
- Pipeline template management
- Cross-platform pipeline migration tools

### 2. **Architecture Evolution**
- Microservice decomposition for large-scale deployments
- Event-driven architecture for workflow automation
- Caching layer for improved performance
- Configuration management service
- Plugin system for custom platforms

## Dependencies

### 1. **Core Dependencies**
- `click>=8.0.0`: CLI framework for command-line interfaces
- `requests>=2.25.0`: HTTP client for API calls (Azure DevOps)
- `pyyaml>=6.0.0`: YAML parsing for configuration files

### 2. **Development Dependencies**
- `pytest`: Testing framework
- `black`: Code formatting
- `mypy`: Static type checking
- `coverage`: Test coverage analysis
- `isort`: Import sorting
- `flake8`: Style guide enforcement

## Deployment Considerations

### 1. **Installation**
- Python package installation via pip
- MSBuild integration for .NET projects
- Cross-platform compatibility (Windows, Linux, macOS)

### 2. **Environment Setup**
- Python 3.8+ requirement
- Platform-specific authentication setup
- Development vs. production configuration

This architecture provides a solid foundation for the SDO tool while maintaining flexibility for future enhancements and platform additions.

