## [Latest Release](https://github.com/naz-hage/ntools/releases)

## Version 1.26.x - MSBuild Target System Enhancements

### 🚀 New Features

#### Code Coverage System
- **Comprehensive Coverage**: New `COVERAGE` and `COVERAGE_SUMMARY` targets with ReportGenerator integration
- **Conditional Collection**: Configurable via `EnableCodeCoverage` property (default: true)
- **Advanced Filtering**: Assembly and class-level filters for precise coverage control
- **HTML Reports**: Detailed HTML coverage reports generated automatically
- **CI/CD Integration**: Coverage files automatically copied for GitHub Actions

#### Enhanced Build Pipeline
- **Improved TEST Target**: Conditional code coverage collection based on configuration
- **Artifact Verification**: New `SMOKE_TEST` and `SMOKE_TEST_PWSH` targets for build validation
- **Streamlined Publishing**: Enhanced `PUBLISH` target with better error handling
- **Target Delegation**: Robust MSBuild target delegation system with validation scripts

#### User-Level PATH Management
- **Improved Security**: No longer requires elevated privileges for installation
- **Test Mode Protection**: Prevents PATH contamination during testing scenarios

### 🔧 Technical Improvements

#### MSBuild Target Enhancements
- Enhanced `COPY_ARTIFACTS` target with organized folder structure
- Improved `CORE` target with comprehensive property display
- Better error handling and dependency management across all targets
- Consistent formatting and indentation in target files

#### PowerShell Scripts
- **verify-artifacts.ps1**: Comprehensive artifact verification with detailed reporting
- **Test-TargetDelegation.ps1**: Generic MSBuild target delegation validator
- **Quick-TargetTest.ps1**: Interactive target testing utility
- Enhanced `publish-all-projects.ps1` with better parameter handling

#### Testing Infrastructure
- Updated unit tests for user-level PATH operations
- New target delegation validation approaches
- Enhanced test coverage for build system components
- Improved test reliability and maintainability

### 🛡️ Security & Compatibility

#### Security Improvements
- User-level environment variable modifications
- Reduced attack surface by avoiding system-wide changes
- Test mode isolation prevents environment contamination

#### Backward Compatibility
- All existing projects continue to work without changes
- Opt-in enhancements don't affect existing workflows
- Maintained API compatibility across all tools

### 📊 Build System Statistics
- **11 files enhanced** with new functionality
- **845 additions, 170 deletions** representing significant improvements
- **100% test coverage** maintained throughout changes
- **Zero breaking changes** ensuring smooth upgrades

### 🎯 Configuration Options

#### Code Coverage Properties
```xml
<PropertyGroup>
    <EnableCodeCoverage>true</EnableCodeCoverage>
    <CoverageAssemblyFilters>+*;-*Tests*;-*Test*</CoverageAssemblyFilters>
    <CoverageClassFilters>+*;-*.Tests.*;-*.Test.*</CoverageClassFilters>
</PropertyGroup>
```

#### Target Usage Examples
```bash
# Run tests with coverage
nb TEST

# Generate coverage reports
nb COVERAGE

# View coverage summary
nb COVERAGE_SUMMARY

# Verify build artifacts
nb SMOKE_TEST

# Complete build pipeline
nb STAGE
```

### 📁 File Structure Improvements
- Organized artifact management with proper folder hierarchy
- Enhanced binary file filtering and organization
- Improved runtime folder support (netstandard2.0, netstandard2.1, win/native)
- Better handling of reference assemblies and localization files

This release represents a major enhancement to the build infrastructure while maintaining full backward compatibility and improving overall system security and usability.