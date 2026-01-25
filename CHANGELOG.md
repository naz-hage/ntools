# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Changed
- **.NET 10.0 Upgrade**: Upgraded all 11 projects from .NET 9.0/.NET Standard 2.0 to .NET 10.0
  - Enables C# 14.0 syntax support
  - Access to latest .NET runtime features and performance improvements
  - Updated System.Text.Json (now included in .NET 10.0 runtime)
  - All 141 tests passing on .NET 10.0

### Added
- **Global Options**: Added `--dry-run` and `--verbose` as global options available to all commands
- **Git Clone Command**: New `git_clone` command for cloning Git repositories
- **Unit Testing Infrastructure**: Comprehensive unit testing targets for granular test execution
  - `UNIT_TEST_CLI_VALIDATION`: CLI validation tests
  - `UNIT_TEST_BUILD_STARTER`: BuildStarter tests
  - `UNIT_TEST_CLI`: CLI tests
  - `UNIT_TEST_COMMAND`: Command tests
  - `UNIT_TEST_NB_COMMAND`: NbCommand tests
  - `UNIT_TEST_GIT_CLONE_COMMAND`: GitCloneCommand tests
  - `UNIT_TEST_NTOOLS_JSON`: NtoolsJson tests
  - `UNIT_TEST_PATH_MANAGER`: PathManager tests (long-running, excluded from ALL)
  - `UNIT_TEST_RELEASE_SERVICE_FACTORY`: ReleaseServiceFactory tests
  - `UNIT_TEST_RESOURCE_HELPER`: ResourceHelper tests
  - `UNIT_TEST_ALL`: Run all unit tests except PathManager
- **Test Coverage**: Added comprehensive unit tests for GitCloneCommand with 7 test methods
- **Documentation**: Updated CLI documentation to reflect global options and new testing targets

### Changed
- **Build System**: Updated `nb.exe` to use .NET SDK instead of direct MSBuild dependency
- **System.CommandLine Upgrade**: Upgraded from System.CommandLine 2.0.0-beta4 to stable 2.0.1
- **CLI Architecture**: Refactored command registration to support global options properly
- **Test Framework**: Enhanced testing infrastructure with Moq for service mocking
- **Build System**: Improved MSBuild targets for targeted unit test execution

### Fixed
- **.NET SDK Detection**: Added automatic detection of dotnet.exe with fallback locations and clear error messages when not found
- **Global Option Access**: Fixed issues with accessing global `--dry-run` and `--verbose` options in subcommands
- **CLI API Compatibility**: Resolved breaking changes between System.CommandLine beta and stable versions
- **Command Registration**: Updated all commands to properly inherit global options

### Technical Details
- **Dependencies**: Added Moq package for unit testing
- **Assembly Visibility**: Added `InternalsVisibleTo` for test assembly access to internal classes
- **Test Isolation**: Implemented proper test isolation with mocked services
- **Build Integration**: Integrated unit testing targets with existing MSBuild infrastructure</content>
<parameter name="filePath">c:\source\ntools\CHANGELOG.md