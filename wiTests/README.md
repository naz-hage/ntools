# wiTests Project

This project contains comprehensive unit tests for the `wi` (Azure DevOps Work Item CLI) tool. The tests are designed to mock Azure DevOps API interactions rather than making actual API calls, ensuring fast and reliable test execution.

## Test Coverage

### 1. AzureDevOpsWorkItemHelperTests.cs
Tests for the main Azure DevOps work item helper class:

- **Constructor validation**: Ensures PAT environment variable is properly validated
- **HttpClient creation**: Verifies correct authorization header setup
- **Interface implementation**: Confirms the class implements `IAzureDevOpsWorkItemHelper`

### 2. ProgramIntegrationTests.cs  
Tests for command-line argument parsing and validation:

- **Services file option**: Tests required services file parameter parsing
- **Parent ID option**: Tests integer validation for parent work item ID
- **Child task option**: Tests optional PBI ID parameter
- **Short aliases**: Verifies `-s`, `-p`, `-c` aliases work correctly
- **Combined options**: Tests all options working together

### 3. ServicesFileTests.cs
Tests for services file handling and processing:

- **File reading**: Tests successful reading of services files
- **Error handling**: Tests file not found, locked files, access denied scenarios
- **Empty lines**: Tests handling of empty lines and whitespace
- **Special characters**: Tests services with various naming conventions
- **File filtering**: Tests skipping empty/whitespace entries

### 4. HttpClientExtensionsTests.cs
Tests for the HTTP PATCH extension method:

- **PATCH request creation**: Verifies correct HTTP method usage
- **Content handling**: Tests both null and valid content scenarios
- **JSON content**: Tests proper content type handling

## Design Principles

### No Real API Calls
All tests use mocking or custom test handlers to avoid:
- Dependency on external Azure DevOps services
- Need for valid PAT tokens during testing
- Network-related test failures
- Slow test execution

### Comprehensive Coverage
Tests cover:
- ✅ Happy path scenarios
- ✅ Error conditions and edge cases
- ✅ Input validation
- ✅ Environment variable handling
- ✅ Command-line parsing
- ✅ File I/O operations

### Test Isolation
Each test:
- Sets up its own environment variables
- Creates temporary files as needed
- Cleans up after execution
- Runs independently of other tests

## Running Tests

```bash
# Run all tests
dotnet test wiTests

# Run with verbose output
dotnet test wiTests --logger console

# Run specific test class
dotnet test wiTests --filter "ClassName=AzureDevOpsWorkItemHelperTests"
```

## Test Dependencies

- **MSTest**: Test framework
- **System.CommandLine**: For command-line parsing tests
- **No Moq**: Simplified approach using custom test handlers instead of complex mocking

## Benefits

1. **Fast Execution**: No network calls mean tests run quickly
2. **Reliable**: Tests don't depend on external services
3. **Comprehensive**: Cover both success and failure scenarios  
4. **Maintainable**: Simple test structure without complex mocking
5. **CI/CD Friendly**: Can run in any environment without configuration
