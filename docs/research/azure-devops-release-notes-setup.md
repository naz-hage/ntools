# Azure DevOps Release Notes Template

This template replicates the release notes format used by the GitHub Release system in ntools.

## Template Features

- **What's Changed**: Lists commits with dates, authors, and PR links (matching GitHub format)
- **New Contributors**: Identifies first-time contributors with PR links
- **Full Changelog**: Provides Azure DevOps compare URL between builds

## Azure DevOps Pipeline Configuration

Add this task to your YAML pipeline:

```yaml
- task: XplatGenerateReleaseNotes@4
  inputs:
    outputfile: '$(Build.ArtifactStagingDirectory)/releasenotes.md'
    templateLocation: 'File'
    templatefile: 'azure-devops-release-notes-template.md'
    checkStage: true  # Compare against last successful build
    getParentsAndChildren: true
    getPRDetails: true
    getAllParents: false
    sortCS: true
    sortWi: true
    showOnlyPrimary: false
    checkForManuallyLinkedWI: true
    searchCrossProjectForPRs: false
    emptySetText: 'No changes found.'
    replaceFile: true
    appendToFile: false
    stopOnRedeploy: true
    considerPartiallySuccessfulReleases: true
    getIndirectPullRequests: false
    stopOnError: false
    getTestedBy: true
    recursivelyCheckConsumedArtifacts: false
```

## Template Objects Used

- `buildDetails`: Current build information (number, repository, project)
- `compareBuildDetails`: Previous build for comparison
- `commits`: Array of commits with author, date, message, PR associations
- `workItems`: Array of associated work items
- `pullRequests`: Array of associated pull requests

## Handlebars Helpers Used

- `forEach`: Loop through arrays
- `if`: Conditional rendering
- `lookup`: Access nested object properties
- `truncate`: Shorten commit IDs
- `moment`: Format dates (requires moment helper)
- `length`: Check array length
- `gt`: Greater than comparison

## Output Format

The template generates markdown that matches the GitHub release notes structure:

```markdown
## Release 20231215.1

### What's Changed
<br>**15-Dec-23**
* [a1b2c3d](url) - Fixed bug in authentication by @developer in https://dev.azure.com/project/_git/repo/pullrequest/123

### New Contributors
* @newdeveloper made their first contribution in https://dev.azure.com/project/_git/repo/pullrequest/456

### Full Changelog
**Full Changelog**: https://dev.azure.com/project/_git/repo/branchCompare?baseVersion=GTabc123&targetVersion=GTdef456
```

## Prerequisites

1. Install the extension: `richardfennellBM.BM-VSTS-XplatGenerateReleaseNotes-Task`
2. Ensure pipeline has permissions to read work items and commits
3. Configure OAuth scope to allow cross-project access if needed

## Customization

Modify the template to adjust:
- Date formatting in the `moment` helper
- Commit ID truncation length
- URL patterns for your organization
- Additional sections or filtering logic