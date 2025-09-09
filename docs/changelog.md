## [Latest Release](https://github.com/naz-hage/ntools/releases)

## Version 1.26.x - MSBuild Target System Enhancements

#### Code Coverage System
- **Comprehensive Coverage**: New `COVERAGE` and `COVERAGE_SUMMARY` targets with ReportGenerator integration
- **Conditional Collection**: Configurable via `EnableCodeCoverage` property (default: true)
- **Advanced Filtering**: Assembly and class-level filters for precise coverage control
- **HTML Reports**: Detailed HTML coverage reports generated automatically

#### Enhanced Build Pipeline
- **Improved TEST Target**: Conditional code coverage collection based on configuration
- **Comprehensive Smoke Test**: Enhanced `SMOKE_TEST` target validates both published artifacts (4+ executables) AND build system integrity (target delegation)
- **Target Consolidation**: Removed `TEST_TARGET_DELEGATION` - functionality now integrated into `SMOKE_TEST`
-- **PowerShell-based Verification**: REMOVED `SMOKE_TEST_PWSH` — functionality consolidated into `SMOKE_TEST` (see `ntools/nbuild-targets.md`)
- **Streamlined Publishing**: Enhanced `PUBLISH` target with better error handling

#### User-Level PATH Management
- **Improved Security**: No longer requires elevated privileges for installation
- **Test Mode Protection**: Prevents PATH contamination during testing scenarios