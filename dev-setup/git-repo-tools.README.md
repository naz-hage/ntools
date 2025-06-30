# Git Repository Tools

A comprehensive PowerShell toolkit for Git repository management, file analysis, and stash recovery.

## 🚀 Quick Start

```powershell
# Simple status check
.\git-tools.ps1 status

# Restore from stash
.\git-tools.ps1 stash

# Check for cleanup opportunities
.\git-tools.ps1 clean

# Deep repository analysis
.\git-tools.ps1 analyze
```

## 📁 Files Overview

### Core Tools
- **`git-repo-tools.ps1`** - Main comprehensive tool with full functionality
- **`git-tools.ps1`** - Simple wrapper with easy-to-remember commands  
- **`git-repo-tools.config.psm1`** - Configuration module for customization

### Legacy Tools (kept for reference)
- **`check-all-files.ps1`** - Original file analysis tool
- **`restore-stash.ps1`** - Original stash recovery tool

## 🛠️ Detailed Usage

### Main Tool (`git-repo-tools.ps1`)

#### File Analysis
```powershell
# Basic analysis
.\git-repo-tools.ps1 -Action Check

# Include ignored files
.\git-repo-tools.ps1 -Action Check -IncludeIgnored

# Exclude specific patterns
.\git-repo-tools.ps1 -Action Check -ExcludePatterns @("*.log", "node_modules/*")
```

#### Stash Management
```powershell
# Restore most recent stash
.\git-repo-tools.ps1 -Action RestoreStash

# Restore specific stash (dry run)
.\git-repo-tools.ps1 -Action RestoreStash -StashIndex 2 -DryRun

# Restore with verbose output
.\git-repo-tools.ps1 -Action RestoreStash -Verbose
```

#### Repository Cleanup
```powershell
# See what would be cleaned (dry run)
.\git-repo-tools.ps1 -Action Cleanup -DryRun

# Actually perform cleanup
.\git-repo-tools.ps1 -Action Cleanup

# Cleanup with verbose output
.\git-repo-tools.ps1 -Action Cleanup -Verbose
```

#### Deep Analysis
```powershell
# Comprehensive repository analysis
.\git-repo-tools.ps1 -Action Analyze

# Include ignored files in analysis
.\git-repo-tools.ps1 -Action Analyze -IncludeIgnored
```

## ⚙️ Configuration

### Repository Type Detection

The tools automatically detect your repository type based on files present:

- **.NET**: `*.sln`, `*.csproj` files
- **Node.js**: `package.json`, `node_modules/`
- **Python**: `requirements.txt`, `setup.py`, `pyproject.toml`
- **Docker**: `Dockerfile`, `docker-compose.yml`
- **Go**: `go.mod`, `go.sum`
- **Rust**: `Cargo.toml`, `Cargo.lock`
- **Java**: `pom.xml`, `build.gradle`, `*.java`

### Customizing Exclude Patterns

Edit `git-repo-tools.config.psm1` to customize exclude patterns for your repository types:

```powershell
$RepoConfigs = @{
    "dotnet" = @{
        ExcludePatterns = @(
            "bin/*", "obj/*", "Debug/*", "Release/*",
            "*.user", "*.suo", ".vs/*", "TestResults/*"
        )
    }
    
    "nodejs" = @{
        ExcludePatterns = @(
            "node_modules/*", "dist/*", "build/*",
            "*.log", ".npm/*", "coverage/*"
        )
    }
}
```

### Global Settings

Modify global settings in the configuration:

```powershell
$GlobalConfig = @{
    DefaultExcludePatterns = @("*.log", "*.tmp", "node_modules/*")
    LargeFileThreshold = 10MB
    MaxFilesToShow = 100
    ShowTimestamps = $true
    UseColors = $true
}
```

## 🎯 Use Cases

### 1. **Daily Development Workflow**
```powershell
# Check repository status before starting work
.\git-tools.ps1 status

# Clean up before committing
.\git-tools.ps1 clean
```

### 2. **Debugging Empty Files Issue** 
```powershell
# Identify empty placeholder files
.\git-repo-tools.ps1 -Action Check

# Clean them up safely
.\git-repo-tools.ps1 -Action Cleanup -DryRun
.\git-repo-tools.ps1 -Action Cleanup
```

### 3. **Stash Recovery After Conflicts**
```powershell
# See what's in stash before applying
.\git-repo-tools.ps1 -Action RestoreStash -DryRun

# Apply specific stash carefully
.\git-repo-tools.ps1 -Action RestoreStash -StashIndex 1
```

### 4. **Repository Health Check**
```powershell
# Full repository analysis
.\git-repo-tools.ps1 -Action Analyze

# Include all files (even ignored ones)
.\git-repo-tools.ps1 -Action Analyze -IncludeIgnored
```

### 5. **Large Repository Management**
```powershell
# Exclude build artifacts and focus on source
.\git-repo-tools.ps1 -Action Check -ExcludePatterns @("bin/*", "obj/*", "node_modules/*", "dist/*")

# Target specific directory
.\git-repo-tools.ps1 -Action Check -TargetDirectory "src/"
```

## 🔧 Advanced Features

### Smart Stash Recovery
- Detects conflicts before applying stash
- Shows what files will be affected
- Provides interactive confirmation for safety
- Supports dry-run mode to preview changes

### Intelligent File Analysis
- Categorizes files by Git status (untracked, modified, staged, ignored)
- Shows file sizes and last modified dates
- Identifies empty files and missing references
- Calculates repository statistics

### Repository Insights
- Git branch and remote information
- Commit history and contributor stats
- Largest files identification
- Sync status with remote repository

## 📦 Integration Options

### Adding to PATH
Add the tools directory to your PATH for global access:

```powershell
# Add to PowerShell profile
$env:PATH += ";C:\path\to\ntools\dev-setup"
```

### Git Aliases
Create Git aliases for common operations:

```bash
git config --global alias.check "!pwsh -c './git-tools.ps1 status'"
git config --global alias.clean-empty "!pwsh -c './git-tools.ps1 clean'"
git config --global alias.stash-restore "!pwsh -c './git-tools.ps1 stash'"
```

### CI/CD Integration
Use in build pipelines to validate repository state:

```yaml
- name: Check Repository Health
  run: |
    cd ${{ github.workspace }}
    pwsh ./dev-setup/git-repo-tools.ps1 -Action Analyze
```

## 🤝 Contributing

To extend these tools:

1. **Add new repository types** in `git-repo-tools.config.psm1`
2. **Add new actions** in the main `git-repo-tools.ps1` script
3. **Customize analysis logic** for specific file types or patterns
4. **Add new cleanup strategies** for different development workflows

## 📋 Examples Output

### Status Check Output
```
📋 Comprehensive Repository Analysis
=================================

🆕 Untracked Files
==================
   dev-setup/git-repo-tools.ps1          | 15234 bytes   | 2025-06-28 06:30 | HAS CONTENT
   docs/new-feature.md                   | 0 bytes       | 2025-06-28 06:25 | EMPTY

📝 Modified Files  
=================
   src/main.cs                           | 2048 bytes    | 2025-06-28 06:20 | HAS CONTENT

📊 Repository Summary
====================
   Total files analyzed: 15
   Files with content: 13
   Empty files: 2
   Missing files: 0
   Total size: 156.7 KB
```

### Stash Recovery Output
```
📦 Smart Stash Recovery
======================

Available stashes:
>>> stash@{0}: On main: WIP: Add new feature
    stash@{1}: On feature-branch: Debug fixes

Files that would be restored:
   CREATE: src/new-feature.cs
   UPDATE: src/main.cs
   CREATE: tests/new-feature-tests.cs

✅ Stash applied successfully!
```

These tools provide a powerful, customizable foundation for Git repository management that can be adapted to any development workflow!
