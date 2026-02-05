# Plan to Implement Azure DevOps Release Notes

## Overview
Implement Azure DevOps release notes generation that mirrors the existing GitHub release notes format used in ntools, using the cross-platform release notes generator extension.

## Current Options Analyzed

### 1. Cross-Platform Release Notes Generator (RECOMMENDED)
- **Extension**: `richardfennellBM.BM-VSTS-XplatGenerateReleaseNotes-Task` (V4)
- **Template System**: Handlebars-based (120+ helpers available)
- **Platforms**: Works with Classic Builds, Classic Releases, and YAML Multi-Stage Pipelines
- **Data Sources**: Azure DevOps REST APIs (work items, commits, PRs, tests)
- **Output Formats**: Markdown, HTML, custom formats

### 2. Built-in Azure DevOps Features
- **Release Comparisons**: UI-based work item and commit associations
- **Work Item Queries**: WIQL queries for custom release notes
- **Pipeline Artifacts**: Publish release notes as build artifacts

### 3. YAML Pipeline Integration
- **Task Integration**: Direct pipeline task for automated generation
- **Template Flexibility**: Inline or file-based Handlebars templates
- **Build Comparison**: Automatic comparison with previous successful builds

## Implementation Plan

### Phase 1: Extension Installation & Setup (1-2 days)
1. **Install Extension**: Add `richardfennellBM.BM-VSTS-XplatGenerateReleaseNotes-Task` to Azure DevOps organization
2. **Verify Permissions**: Ensure pipeline has access to work items, builds, and repositories
3. **Test Extension**: Run basic pipeline with default template to verify functionality

### Phase 2: Template Development (2-3 days)
1. **Analyze GitHub Format**: Review existing ntools ReleaseFormatter.cs structure
2. **Create Handlebars Template**: Develop template matching GitHub format:
   - What's Changed section with commit details
   - New Contributors section with PR links
   - Full Changelog section with compare URLs
3. **Test Template**: Validate template renders correctly with sample data
4. **Refine Formatting**: Ensure date formatting and URL generation match GitHub style

### Phase 3: Pipeline Integration (1-2 days)
1. **Update YAML Pipeline**: Add release notes generation task to existing pipelines
2. **Configure Task Parameters**:
   - Output file location
   - Template location (inline or file)
   - Build comparison settings
   - Work item and PR association options
3. **Test Pipeline**: Run full pipeline with release notes generation
4. **Validate Output**: Compare generated notes with GitHub format

### Phase 4: Advanced Features (2-3 days, optional)
1. **Work Item Integration**: Include work item types, states, and relationships
2. **Test Results**: Add test summary and failure information
3. **Custom Helpers**: Develop Handlebars helpers for specific formatting needs
4. **Multi-Environment**: Support different templates for different environments

### Phase 5: Documentation & Training (1 day)
1. **Update Documentation**: Create setup and usage guides
2. **Team Training**: Train team on template customization
3. **Maintenance Guide**: Document template updates and troubleshooting

## Success Criteria
- [ ] Release notes generated automatically in pipelines
- [ ] Format matches existing GitHub release notes structure
- [ ] Includes commits, PRs, work items, and contributor information
- [ ] Template is maintainable and customizable
- [ ] Integration works across different pipeline types

## Risk Mitigation
- **Extension Dependency**: Monitor extension updates and have fallback plan
- **API Changes**: Test thoroughly with Azure DevOps API changes
- **Template Complexity**: Start with simple template, add complexity incrementally
- **Performance Impact**: Monitor pipeline execution time impact

## Timeline
- **Total Duration**: 1-2 weeks (depending on complexity)
- **Parallel Work**: Template development can happen in parallel with extension setup
- **Testing**: Allocate time for thorough testing across different scenarios

## Resources Required
- Azure DevOps administrator access for extension installation
- Pipeline contributor permissions for YAML updates
- Access to test repositories with commits, PRs, and work items
- Handlebars template development knowledge (or learning time)