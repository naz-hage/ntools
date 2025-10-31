# SDO Architecture Documentation

## Overview

SDO (Simple DevOps Operations Tool) is a command-line interface tool designed to streamline work item creation across multiple DevOps platforms including Azure DevOps and GitHub. The tool follows a modular, extensible architecture that allows for easy addition of new platforms and work item types.

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

```
┌─────────────────────────────────────────────────────────────────┐
│                         SDO CLI Tool                            │
├─────────────────────────────────────────────────────────────────┤
│  sdo.py (Main Entry Point)                                     │
│  ├── Click CLI Framework                                       │
│  ├── Command Registration                                      │
│  └── Global Configuration                                      │
└─────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────┐
│                    CLI Layer (cli.py)                          │
├─────────────────────────────────────────────────────────────────┤
│  ├── add-issue command implementation                          │
│  ├── Command argument parsing and validation                   │
│  ├── Verbose output management                                 │
│  └── Error handling and user feedback                          │
└─────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────┐
│               Business Logic (work_items.py)                   │
├─────────────────────────────────────────────────────────────────┤
│  ├── WorkItemManager: Main orchestration class                 │
│  ├── Content processing and validation                         │
│  ├── Platform selection and routing                            │
│  └── Result aggregation and reporting                          │
└─────────────────────────────────────────────────────────────────┘
                                    │
                    ┌───────────────┼───────────────┐
                    ▼               ▼               ▼
    ┌─────────────────────┐ ┌─────────────────┐ ┌─────────────────┐
    │   Parser Layer      │ │ Platform Layer  │ │  Client Layer   │
    │   (parsers/)        │ │ (platforms/)    │ │   (client.py)   │
    └─────────────────────┘ └─────────────────┘ └─────────────────┘
```

## Component Details

### 1. Main Entry Point (sdo.py)
**Purpose**: Application bootstrap and CLI framework setup
**Responsibilities**:
- Click application initialization
- Global command registration
- Environment setup and configuration
- Version management

**Key Features**:
- Supports `--verbose` flag for detailed output
- Hierarchical command structure
- Extensible command registration

### 2. CLI Layer (cli.py)
**Purpose**: Command implementations and user interaction
**Responsibilities**:
- `add-issue` command implementation
- Input validation and sanitization
- User feedback and progress reporting
- Error presentation to users

**Key Features**:
- Rich command-line experience with Click
- Comprehensive input validation
- Detailed verbose output for debugging
- Graceful error handling with helpful messages

### 3. Business Logic Layer (work_items.py)
**Purpose**: Core application logic and orchestration
**Responsibilities**:
- Content processing workflow coordination
- Platform detection and selection
- Work item creation orchestration
- Result validation and reporting

**Classes**:
- `WorkItemManager`: Main orchestration class
- `WorkItemResult`: Result encapsulation
- Various helper functions for content processing

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
    def create_work_item(self, **kwargs) -> Dict[str, Any]:
        pass
    
    @abstractmethod
    def validate_connection(self) -> bool:
        pass
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

### 6. Client Layer (client.py)
**Purpose**: Low-level API communication

#### AzureDevOpsClient
**Responsibilities**:
- HTTP request management for Azure DevOps API
- Authentication handling
- Request/response logging
- Connection validation

**Key Features**:
- Comprehensive verbose logging
- Secure token handling with redaction
- Retry logic for transient failures
- Detailed error context

## Data Flow

### 1. Command Execution Flow
```
User Command → CLI Parser → WorkItemManager → Platform Selection
     ↓
Content Parsing ← MarkdownParser ← File Input
     ↓
Metadata Extraction ← MetadataParser ← Content Analysis
     ↓
Platform Creation ← PlatformFactory ← Platform Detection
     ↓
Work Item Creation ← Platform Implementation ← API/CLI Calls
     ↓
Result Reporting ← WorkItemResult ← Success/Error Aggregation
```

### 2. Error Handling Flow
```
Exception Occurrence → Platform-Specific Handler → SDOException
     ↓
Business Logic Error Handler → User-Friendly Message
     ↓
CLI Error Presenter → Console Output with Context
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

### 1. **Environment Variables**
- `AZURE_DEVOPS_PAT`: Azure DevOps Personal Access Token
- `GITHUB_TOKEN`: GitHub authentication token (optional if using GitHub CLI)
- `SDO_VERBOSE`: Global verbose mode setting

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
- Bulk work item creation
- Template management system
- Advanced field mapping configurations
- Plugin system for custom platforms

### 2. **Architecture Evolution**
- Microservice decomposition for large-scale deployments
- Event-driven architecture for workflow automation
- Caching layer for improved performance
- Configuration management service

## Dependencies

### 1. **Core Dependencies**
- `click`: CLI framework
- `requests`: HTTP client for API calls
- `typing`: Type hints and annotations

### 2. **Development Dependencies**
- `pytest`: Testing framework
- `black`: Code formatting
- `mypy`: Static type checking
- `coverage`: Test coverage analysis

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

