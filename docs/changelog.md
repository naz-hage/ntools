## [Latest Release](https://github.com/naz-hage/ntools/releases)

## Version 1.26.x - MSBuild Target System Enhancements

#### Code Coverage System
- **Comprehensive Coverage**: New `COVERAGE` and `COVERAGE_SUMMARY` targets with ReportGenerator integration
- **Conditional Collection**: Configurable via `EnableCodeCoverage` property (default: true)
- **Advanced Filtering**: Assembly and class-level filters for precise coverage control
- **HTML Reports**: Detailed HTML coverage reports generated automatically

#### Enhanced Build Pipeline
- **Improved TEST Target**: Conditional code coverage collection based on configuration
- **Artifact Verification**: New `SMOKE_TEST` and `SMOKE_TEST_PWSH` targets for build validation
- **Streamlined Publishing**: Enhanced `PUBLISH` target with better error handling

#### User-Level PATH Management
- **Improved Security**: No longer requires elevated privileges for installation
- **Test Mode Protection**: Prevents PATH contamination during testing scenarios