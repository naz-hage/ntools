# Code Coverage in NTools

The NTools build system includes comprehensive code coverage support that can be easily configured and integrated into your build pipeline.

## Overview

Code coverage is automatically collected when running the `TEST` target if the `EnableCodeCoverage` property is set to `true` (which is the default). The system uses XPlat Code Coverage collection and ReportGenerator for creating detailed HTML and text reports.

## Configuration

### Basic Configuration

The following properties control code coverage behavior:

| Property | Default Value | Description |
|----------|---------------|-------------|
| `EnableCodeCoverage` | `true` | Controls whether code coverage is collected during test runs |
| `CoverageAssemblyFilters` | `+*;-*Tests*;-*Test*` | Filters for assemblies to include/exclude from coverage |
| `CoverageClassFilters` | `+*;-*.Tests.*;-*.Test.*` | Filters for classes to include/exclude from coverage |

### Customizing Coverage Filters

You can customize coverage filters by setting properties in your project file:

```xml
<PropertyGroup>
    <EnableCodeCoverage>true</EnableCodeCoverage>
    <CoverageAssemblyFilters>+MyProject*;-*Tests*;-*Mock*</CoverageAssemblyFilters>
    <CoverageClassFilters>+*;-*.Tests.*;-*.Mocks.*</CoverageClassFilters>
</PropertyGroup>
```

### Filter Syntax

Coverage filters use the following syntax:
- `+` includes the pattern
- `-` excludes the pattern
- `*` is a wildcard
- Filters are separated by semicolons (`;`)

## MSBuild Targets

### COVERAGE Target

The `COVERAGE` target generates comprehensive code coverage reports:

```bash
nb COVERAGE
```

This target:
1. Installs ReportGenerator tool if not present
2. Processes coverage files from test results
3. Generates HTML reports in `CoverageReport` folder
4. Creates text summaries for CI/CD integration
5. Copies coverage files for GitHub Actions

### COVERAGE_SUMMARY Target

For a quick coverage overview:

```bash
nb COVERAGE_SUMMARY
```

This displays high-level coverage metrics without generating full reports.

### TEST Target with Coverage

The `TEST` target automatically includes coverage collection when `EnableCodeCoverage` is true:

```bash
nb TEST
```

This runs all tests and collects coverage data in a single step.

## Output Files

After running coverage targets, you'll find:

### HTML Reports
- `CoverageReport/index.html` - Main coverage report
- `CoverageReport/` - Detailed HTML coverage reports

### Text Reports
- Console output with coverage summary
- Coverage files copied to artifacts for CI/CD

### Coverage Data
- Raw coverage files in test results directory
- Processed coverage data for further analysis

## CI/CD Integration

### GitHub Actions

Coverage reports are automatically copied to appropriate locations for GitHub Actions integration. The `COVERAGE` target handles this automatically.

### Disabling Coverage

To disable coverage collection (e.g., for faster builds):

```xml
<PropertyGroup>
    <EnableCodeCoverage>false</EnableCodeCoverage>
</PropertyGroup>
```

Or via command line:

```bash
nb TEST -p:EnableCodeCoverage=false
```

## Best Practices

1. **Filter Appropriately**: Use assembly and class filters to focus on relevant code
2. **CI/CD Integration**: Include coverage in your build pipeline for quality gates
3. **Regular Monitoring**: Review coverage reports regularly to maintain code quality
4. **Test Strategy**: Use coverage data to identify untested code paths

## Troubleshooting

### ReportGenerator Installation
If ReportGenerator installation fails, install manually:
```bash
dotnet tool install -g dotnet-reportgenerator-globaltool
```

### Coverage Collection Issues
1. Ensure tests are running in Release configuration
2. Check that test assemblies are not excluded by filters
3. Verify XPlat Code Coverage collector is available

### Report Generation
1. Check that coverage files exist in test results
2. Verify ReportGenerator tool is installed
3. Ensure proper permissions for output directories

## Examples

### Basic Usage
```bash
# Run tests with coverage
nb TEST

# Generate coverage reports
nb COVERAGE

# Quick coverage summary
nb COVERAGE_SUMMARY
```

### Custom Configuration
```xml
<PropertyGroup>
    <EnableCodeCoverage>true</EnableCodeCoverage>
    <CoverageAssemblyFilters>+MyProject*;+MyLibrary*;-*Tests*</CoverageAssemblyFilters>
    <CoverageClassFilters>+*;-*.Tests.*;-*TestHelpers*</CoverageClassFilters>
</PropertyGroup>
```

### Full Build Pipeline
```bash
# Complete build with tests and coverage
nb STAGE
```

This comprehensive coverage system provides detailed insights into code quality while being flexible enough to adapt to different project needs.
