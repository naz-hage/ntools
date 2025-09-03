# Migration Guide - MSBuild Target System Enhancements

This guide helps you understand and migrate to the enhanced MSBuild target system introduced in version 1.26.x.

## Overview

The latest version introduces significant enhancements to the build system while maintaining full backward compatibility. Existing projects will continue to work without any changes, but you can opt into new features for improved functionality.

## What's Changed

### 1. Code Coverage System

#### Before
- Basic test execution without coverage
- Manual coverage collection if needed
- Limited reporting options

#### After
- Automatic code coverage collection during tests
- Comprehensive HTML and text reports
- Configurable assembly and class filters
- CI/CD integration support

### 2. PATH Management

#### Before
- Required administrator privileges for installation
- System-wide PATH modifications
- Potential security concerns

#### After
- User-level PATH modifications only
- No administrator privileges required
- Enhanced security and isolation
- Test mode protection

### 3. Build Pipeline

#### Before
- Basic target execution
- Limited artifact verification
- Manual validation processes

#### After
- Enhanced target delegation system
- Automated artifact verification
- Comprehensive smoke testing
- Better error handling and diagnostics

## Migration Steps

### For Existing Projects

**Good News: No action required!** Your existing projects will continue to work exactly as before. The enhancements are backward compatible.

### To Enable New Features

#### 1. Code Coverage

To take advantage of the new code coverage system:

```xml
<!-- Add to your project file or nbuild.targets -->
<PropertyGroup>
    <EnableCodeCoverage>true</EnableCodeCoverage> <!-- Default: true -->
</PropertyGroup>
```

#### 2. Custom Coverage Filters

Customize what gets included in coverage reports:

```xml
<PropertyGroup>
    <CoverageAssemblyFilters>+MyProject*;+MyLibrary*;-*Tests*</CoverageAssemblyFilters>
    <CoverageClassFilters>+*;-*.Tests.*;-*.Mocks.*</CoverageClassFilters>
</PropertyGroup>
```

#### 3. Artifact Verification

Add smoke testing to your build pipeline:

```bash
# Basic artifact verification
nb SMOKE_TEST

# PowerShell-based detailed verification
nb SMOKE_TEST_PWSH
```

## New MSBuild Targets

The following new targets are available:

| Target | Description | Usage |
|--------|-------------|-------|
| `COVERAGE` | Generate comprehensive coverage reports | `nb COVERAGE` |
| `COVERAGE_SUMMARY` | Display coverage summary | `nb COVERAGE_SUMMARY` |
| `SMOKE_TEST` | Basic artifact verification | `nb SMOKE_TEST` |
| `SMOKE_TEST_PWSH` | PowerShell-based verification | `nb SMOKE_TEST_PWSH` |

## Enhanced Existing Targets

### TEST Target

The TEST target now includes conditional code coverage:

```bash
# Run tests with coverage (default)
nb TEST

# Run tests without coverage
nb TEST -p:EnableCodeCoverage=false
```

### STAGE Target

The STAGE target now includes the complete enhanced pipeline:
- Clean → Build → Test with Coverage → Generate Reports → Publish → Verify → Package

## Configuration Reference

### Code Coverage Properties

| Property | Default | Description |
|----------|---------|-------------|
| `EnableCodeCoverage` | `true` | Enable/disable coverage collection |
| `CoverageAssemblyFilters` | `+*;-*Tests*;-*Test*` | Assembly inclusion/exclusion patterns |
| `CoverageClassFilters` | `+*;-*.Tests.*;-*.Test.*` | Class inclusion/exclusion patterns |

### Filter Syntax

- `+pattern` - Include assemblies/classes matching pattern
- `-pattern` - Exclude assemblies/classes matching pattern
- `*` - Wildcard character
- `;` - Separator between filters

## Common Scenarios

### Scenario 1: Disable Coverage for Performance

For faster builds during development:

```xml
<PropertyGroup Condition="'$(Configuration)' == 'Debug'">
    <EnableCodeCoverage>false</EnableCodeCoverage>
</PropertyGroup>
```

### Scenario 2: Custom Coverage for Microservices

For projects with multiple services:

```xml
<PropertyGroup>
    <CoverageAssemblyFilters>+$(SolutionName)*;-*Tests*;-*TestHelpers*</CoverageAssemblyFilters>
    <CoverageClassFilters>+*;-*.Tests.*;-*.Mocks.*;-*TestData*</CoverageClassFilters>
</PropertyGroup>
```

### Scenario 3: CI/CD Integration

For GitHub Actions workflows:

```yaml
- name: Run Tests with Coverage
  run: nb TEST

- name: Generate Coverage Reports
  run: nb COVERAGE

- name: Verify Artifacts
  run: nb SMOKE_TEST
```

## Troubleshooting

### Coverage Collection Issues

1. **No coverage data collected**
   - Check that `EnableCodeCoverage` is true
   - Verify test assemblies aren't excluded by filters
   - Ensure XPlat Code Coverage is available

2. **ReportGenerator installation fails**
   - Install manually: `dotnet tool install -g dotnet-reportgenerator-globaltool`
   - Check network connectivity and permissions

3. **PATH issues after upgrade**
   - Restart your terminal/IDE
   - Check user PATH environment variable
   - Re-run installation if needed

### Target Execution Issues

1. **Target not found errors**
   - Verify you're using the latest version
   - Check that common.targets is properly imported
   - Ensure build tools are up to date

2. **Permission errors**
   - No longer should occur with user-level PATH
   - If still occurring, check file permissions in user directories

## Best Practices

### 1. Gradual Adoption
- Start with default settings
- Gradually customize filters based on your needs
- Monitor coverage reports to ensure quality

### 2. CI/CD Integration
- Include coverage generation in your build pipeline
- Set up quality gates based on coverage thresholds
- Archive coverage reports as build artifacts

### 3. Team Collaboration
- Document your coverage configuration
- Share filter patterns across team members
- Review coverage reports during code reviews

## Getting Help

If you encounter issues during migration:

1. Check the [troubleshooting section](#troubleshooting)
2. Review the [changelog](changelog.md) for detailed changes
3. Create an [issue](https://github.com/naz-hage/NTools/issues) with specific details

## Summary

The enhanced MSBuild target system provides:
- ✅ Full backward compatibility
- ✅ Enhanced code coverage capabilities
- ✅ Improved security with user-level installation
- ✅ Better build pipeline with verification
- ✅ Comprehensive documentation and tooling

Your existing workflows continue to work while new capabilities are available when you need them.
