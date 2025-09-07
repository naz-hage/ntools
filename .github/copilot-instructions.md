# ntools - Software Tools Collection

ntools is a comprehensive C# .NET solution providing build automation, backup utilities, and development tools. The main component is `nb.exe`, a custom MSBuild-based automation tool for building, testing, and releasing .NET projects.

Always reference these instructions first and fallback to search or bash commands only when you encounter unexpected information that does not match the info here.

## Working Effectively

### Prerequisites and Environment Setup
- **CRITICAL**: This project targets .NET 9.0, but works with .NET 8.0 for partial builds
- **NEVER CANCEL**: All build and test operations - builds may take 10+ minutes, tests may take 5+ minutes
- Git configuration is required before most operations
- Python 3.12+ required for documentation and pre-commit hooks

```bash
# Configure Git (required for most operations)
git config --global user.name "Your Name"
git config --global user.email "your.email@example.com"

# Install required Python tools - takes 2-3 minutes. NEVER CANCEL.
pip install mkdocs pre-commit

# Install pre-commit hooks - takes 10 seconds
pre-commit install
```

### Build System Overview
The project uses a custom build system with these key components:
- **nbuild.targets**: Main MSBuild targets file with custom automation
- **nb.exe**: Primary CLI tool (requires .NET 9.0 to build)
- **NbuildTasks**: Core functionality library (.NET Standard 2.0 - builds with .NET 8.0)
- **Pre-commit hooks**: Automated quality control and version management

### Core Build Commands

**TIMING WARNING**: Set timeout to 60+ minutes for builds, 30+ minutes for tests. NEVER CANCEL.

```bash
# Build the core library (works with .NET 8.0) - takes 5-10 seconds
dotnet build NbuildTasks/NbuildTasks.csproj --configuration Release

# Build individual .NET 9.0 projects (requires .NET 9.0)
# FAILS with .NET 8.0: "The current .NET SDK does not support targeting .NET 9.0"
dotnet build Nbuild/Nbuild.csproj --configuration Release

# Full solution build (requires .NET 9.0) - takes 10+ minutes. NEVER CANCEL.
dotnet build ntools.sln --configuration Release

# Build documentation - takes 1 second
mkdocs build
```

### Testing Infrastructure

**NEVER CANCEL**: Test execution may take 5-15 minutes. Set timeout to 30+ minutes.

```bash
# Test the core library (works with .NET 8.0)
dotnet test NbuildTasks/NbuildTasks.csproj --configuration Release

# Run all tests (requires .NET 9.0)
dotnet test ntools.sln --configuration Release

# Test pre-commit hooks (may take 2-5 minutes on first run)
pre-commit run --all-files
```

### Key Project Structure

**Core Projects (.NET Standard 2.0 - builds with .NET 8.0)**:
- `NbuildTasks/`: Core build automation library

**Main Applications (.NET 9.0 - requires .NET 9.0)**:
- `Nbuild/`: Main nb.exe CLI tool
- `GitHubRelease/`: GitHub release automation
- `nBackup/`: Backup utility
- `wi/`: Work item management
- `lf/`: Line feed utility

**Test Projects (.NET 9.0)**:
- `*Tests/`: Unit test projects for each component

## Complete Validation Workflow

### COMPREHENSIVE VALIDATION SCENARIO
Run this complete validation to ensure all major functionality works:

```bash
# Complete validation workflow - takes 2-3 seconds total
echo "=== COMPREHENSIVE VALIDATION OF NTOOLS ===" 

# 1. Verify Git configuration
git config --get user.name && git config --get user.email
echo "✅ Git configuration: PASSED"

# 2. Build core library (.NET Standard 2.0 - WORKS with .NET 8.0)
time dotnet build NbuildTasks/NbuildTasks.csproj --configuration Release
echo "✅ Core build: PASSED"

# 3. Build documentation
time mkdocs build
echo "✅ Documentation build: PASSED" 

# 4. Verify .NET 9.0 projects fail as expected
dotnet build Nbuild/Nbuild.csproj --configuration Release 2>&1 | grep -q "NETSDK1045"
echo "✅ .NET 9.0 limitation confirmed: PASSED"

# 5. Check outputs exist
ls -la NbuildTasks/bin/Release/NbuildTasks.dll && echo "✅ Build artifacts: PASSED"
ls -la site/index.html && echo "✅ Documentation artifacts: PASSED"

# 6. Verify git status
git status
echo "✅ Git status: PASSED"

echo "=== ALL VALIDATIONS COMPLETE ==="
```

**Expected Results:**
- Core build: ~1.2 seconds 
- Documentation build: ~0.4 seconds
- .NET 9.0 builds fail with NETSDK1045 error (expected)
- Total validation time: ~2-3 seconds

### MANUAL VALIDATION REQUIREMENT
After making any changes, you MUST run these validation scenarios:

1. **Core Library Validation**:
```bash
# Build and verify core functionality - takes 10 seconds
dotnet build NbuildTasks/NbuildTasks.csproj --configuration Release
# Check output exists
ls -la NbuildTasks/bin/Release/
```

2. **Documentation Validation**:
```bash
# Build docs and verify - takes 1 second
mkdocs build
# Verify site directory created
ls -la site/
```

3. **Pre-commit Validation**:
```bash
# KNOWN ISSUE: Pre-commit hooks fail due to dotnet-format version mismatch
# The dotnet-format hook references v5.1.250801 which doesn't exist
# ERROR: "pathspec 'v5.1.250801' did not match any file(s) known to git"

# WORKAROUND: Manual validation instead of pre-commit
git status  # Verify working tree state
```

4. **Git Operations Validation**:
```bash
# Verify git is properly configured
git config --get user.name
git config --get user.email
git status
```

## Common Development Workflows

### Making Code Changes
1. **Always** build NbuildTasks first to verify core changes:
   ```bash
   dotnet build NbuildTasks/NbuildTasks.csproj --configuration Release
   ```

2. **Always** validate working tree state before committing:
   ```bash
   git status  # Check for untracked files and changes
   ```

3. **Always** build documentation if docs changed:
   ```bash
   mkdocs build
   ```

### Working with .NET Version Limitations
- **DO NOT** attempt to build .NET 9.0 projects with .NET 8.0
- **Focus** changes on NbuildTasks (.NET Standard 2.0) when possible
- **Document** any .NET 9.0 requirements clearly in your changes

### Pre-commit Hook System
The project uses comprehensive pre-commit automation:
- **Version management**: Automatically updates documentation from JSON config files
- **Code quality**: C# formatting, PowerShell analysis, JSON/YAML validation
- **Commit message validation**: Conventional commit format required

```bash
# Manual version update (equivalent to pre-commit automation)
# This is typically done automatically by hooks
powershell -c "dev-setup/update-versions.ps1"
```

## Build Targets and Automation

### Available nb.exe Targets (when nb.exe is available)
When the full ntools is installed (requires .NET 9.0), these targets are available:
- `CLEAN`: Clean up project and artifacts
- `SOLUTION`: Build solution in Release mode
- `TEST`: Run all tests with code coverage
- `STAGE`: Create stage build with full validation
- `PROD`: Create production build
- `MKDOCS`: Build documentation
- `UPDATE_DOC_VERSIONS`: Update documentation versions

### MSBuild Integration
The project includes custom MSBuild targets in `nbuild.targets`:
- Integrates with GitHub Actions workflows
- Provides comprehensive build automation
- Supports both Windows and Linux environments

## Troubleshooting

### .NET Version Issues
```bash
# Check available .NET versions
dotnet --list-sdks

# If you see "NETSDK1045" errors, you're trying to build .NET 9.0 with .NET 8.0
# Solution: Focus on .NET Standard 2.0 projects only
```

### Pre-commit Hook Issues
```bash
# CRITICAL: Pre-commit hooks are BROKEN in this environment
# ERROR: dotnet-format repository version v5.1.250801 doesn't exist
# RESULT: All pre-commit commands fail with git checkout error

# DO NOT USE: pre-commit run --all-files
# DO NOT USE: pre-commit run <specific-hook>

# ALTERNATIVE: Manual validation approach
git status                    # Check working tree
git diff                     # Review changes  
mkdocs build                 # Validate docs build
dotnet build NbuildTasks/NbuildTasks.csproj --configuration Release

# If you need to install pre-commit hooks (they will fail at runtime):
pre-commit install  # Installs hooks but they fail when triggered
```

### Build Failures
- **Always check** .NET version compatibility first
- **Never cancel** long-running operations - they may take 10+ minutes
- **Check git configuration** if build fails with git operations

## Key Files Reference

### Configuration Files
- `.pre-commit-config.yaml`: Pre-commit hook configuration
- `mkdocs.yml`: Documentation configuration
- `ntools.sln`: Main solution file
- `nbuild.targets`: Custom build targets

### Development Scripts
- `dev-setup/install.ps1`: Full installation script (Windows)
- `dev-setup/update-versions.ps1`: Version management automation
- `prebuild.bat`: Pre-build setup (Windows-specific)

### Documentation
- `docs/`: Complete documentation source
- `docs/installation.md`: Installation instructions
- `docs/usage.md`: Usage examples
- `docs/ntools/nbuild-targets.md`: Complete target reference

## Environment Variables
When using the full nb.exe tool with GitHub integration:
- `OWNER`: GitHub repository owner
- `API_GITHUB_KEY`: GitHub API token for releases

## Critical Reminders
- **NEVER CANCEL** builds or tests - they may take 10+ minutes
- **ALWAYS** set timeouts to 60+ minutes for builds, 30+ minutes for tests
- **ALWAYS** validate changes with the provided scenarios
- **DO NOT** attempt .NET 9.0 builds with .NET 8.0 SDK
- **ALWAYS** configure git before attempting build operations
- **FOCUS** on .NET Standard 2.0 components when .NET 9.0 unavailable