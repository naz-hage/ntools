# PBI: Git Tools Phase 1 – Infrastructure Automation & Developer Workflow

## 🎯 Title
Phase 1: Automated Package Management, Pre-commit Hooks, and Versioning Infrastructure

## 📋 User Story
**As a** developer on the ntools project
**I want** robust automation for package management, pre-commit hooks, and versioning
**So that** I can maintain code quality, reduce manual work, and streamline development

## 🚀 Implementation Details (This PR)
This PBI covers the first major phase of the Git Repository Management Toolkit, focusing on infrastructure automation, package management, and developer workflow improvements. The following features and files have been implemented:

### 📦 Package Management Automation
- **dev-setup/update-packages.ps1**: Comprehensive NuGet package updater script
  - Installs and uses dotnet-outdated-tool
  - Updates only to stable releases (excludes previews/betas)
  - Supports dry-run, interactive, and strict modes
  - Handles network issues gracefully with --ignore-failed-sources
  - Sets DOTNET_HOST_PATH automatically for credential providers

### 🔧 Pre-commit Hooks & Quality Gates
- **.pre-commit-config.yaml**: Pre-commit hook configuration
- **dev-setup/hooks/**: Custom git hooks for development workflow
  - advanced-pre-commit: Advanced pre-commit validation
  - optimized-pre-commit: Optimized performance pre-commit hook

### 📊 Version Management & Documentation
- **dev-setup/update-versions.ps1**: Automated version updating script
- **NbuildTasks/UpdateVersionsInDocs.cs**: MSBuild task for updating versions in documentation
- **.github/workflows/update-versions.yml**: GitHub Actions workflow for version automation

### 📖 Enhanced Documentation
- **docs/pre-commit-setup.md**: Guide for setting up pre-commit hooks
- **docs/version-automation-guide.md**: Comprehensive version automation documentation
- **docs/nbuild-tasks-integration.md**: Integration guide for MSBuild tasks

### 🛠️ Debug and Utility Scripts
- **debug-resources.cs**: Debug utilities for embedded resources
- **dev-setup/check-all-files.ps1**: (Legacy) File status and size checker
- **dev-setup/restore-stash.ps1**: (Legacy) Stash recovery helper

### 🧪 Testing & Validation
- All scripts tested for dry run, error handling, and edge cases
- Multi-solution and corporate/private NuGet feed support validated
- Documentation includes usage, troubleshooting, and workflow integration

### 🔍 Files Changed / Added
- .github/workflows/update-versions.yml
- .pre-commit-config.yaml
- NbuildTasks/UpdateVersionsInDocs.cs
- debug-resources.cs
- dev-setup/hooks/advanced-pre-commit
- dev-setup/hooks/optimized-pre-commit
- dev-setup/update-packages.ps1
- dev-setup/update-versions.ps1
- docs/nbuild-tasks-integration.md
- docs/pre-commit-setup.md
- docs/version-automation-guide.md

### 📝 Notes
- This PBI implements Phase 1 of the [Epic: Git Repository Management Tools](git-tools-pbi.md)
- Focus is on developer experience, automation, and code quality
- Legacy scripts are retained for reference but superseded by new tools

## ✅ Acceptance Criteria (Phase 1)
- [x] Automated package management for NuGet
- [x] Pre-commit hook setup and documentation
- [x] Version management automation
- [x] GitHub Actions workflow for versioning
- [x] Documentation for all new tools and workflows
- [x] All scripts tested for dry run and error handling

## 📦 Deliverables
- All scripts, tasks, and documentation listed above
- Foundation for future toolkit enhancements (see Epic for full vision)

---

**Epic:** [Git Repository Management Tools](git-tools-pbi.md)
**Priority:** High
**Effort:** 5 Story Points
**Sprint:** Current
