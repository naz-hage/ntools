Research Azure DevOps release notes generation capabilities

Read [](file:///c%3A/source/ntools)

Read [](file:///c%3A/source/ntools/GitHubRelease)

Read [](file:///c%3A/source/ntools/GitHubRelease/ReleaseService.cs#1-1), lines 1 to 50

Searched for text `GenerateReleaseNotes` (`**/*.cs`), no results

Searched for text `release notes` (`**/*.cs`), 11 results

Read [](file:///c%3A/source/ntools/GitHubRelease/ReleaseFormatter.cs)

Read [](file:///c%3A/source/ntools/GitHubRelease/CommitService.cs#100-100), lines 100 to 150

Using "Fetch Web Page"

Fetched https://learn.microsoft.com/en-us/azure/devops/release-notes/

Using "Fetch Web Page"

Fetched https://learn.microsoft.com/en-us/rest/api/azure/devops/release/releases

Using "Fetch Web Page"

Fetched https://learn.microsoft.com/en-us/azure/devops/pipelines/release/notes

Using "Fetch Web Page"

Fetched https://learn.microsoft.com/en-us/azure/devops/pipelines/release/

Using "Fetch Web Page"

Fetched https://learn.microsoft.com/en-us/azure/devops/repos/git/release-notes

Using "Fetch Web Page"

Fetched https://learn.microsoft.com/en-us/azure/devops/cli/release

Using "Fetch Web Page"

Fetched https://learn.microsoft.com/en-us/azure/devops/release-notes/

Using "Fetch Web Page"

Fetched https://learn.microsoft.com/en-us/azure/devops/cli/

Using "Fetch Web Page"

Fetched https://learn.microsoft.com/en-us/rest/api/azure/devops/wit/work-items

Searching codebase for "Azure DevOps release notes generation"

Searched for text `release notes`, 20 results

Using "Fetch Web Page"

Fetched https://learn.microsoft.com/en-us/rest/api/azure/devops/wit/wiql

Using "Fetch Web Page"

Fetched https://marketplace.visualstudio.com/search?term=release notes&target=AzureDevOps&category=All categories&sortBy=Relevance

Using "Fetch Web Page"

Fetched https://learn.microsoft.com/en-us/azure/devops/pipelines/tasks/utility/generate-release-notes

Using "Fetch Web Page"

Fetched https://learn.microsoft.com/en-us/azure/devops/pipelines/tasks/

Using "Fetch Web Page"

Fetched https://marketplace.visualstudio.com/search?term=release notes&target=AzureDevOps&category=All categories&sortBy=Relevance

Using "Fetch Web Page"

Fetched https://learn.microsoft.com/en-us/azure/devops/pipelines/tasks/utility/query-work-items

Using "Fetch Web Page"

Fetched https://learn.microsoft.com/en-us/azure/devops/pipelines/tasks/utility/query-work-items

Read [](file:///c%3A/source/ntools)

Read [](file:///c%3A/source/ntools/GitHubRelease)

Read [](file:///c%3A/source/ntools/GitHubRelease/ReleaseService.cs#1-1), lines 1 to 50

Read [](file:///c%3A/source/ntools/GitHubRelease/ReleaseFormatter.cs)

Read [](file:///c%3A/source/ntools/GitHubRelease/CommitService.cs#1-1), lines 1 to 100

Using "Fetch Web Page"

Fetched 4 resources

Summarized conversation history

Using "Fetch Web Page"

Fetched 4 resources

Searched for text `ReleaseService` (`**/*.cs`), 20 results

Read [](file:///c%3A/source/ntools/GitHubRelease/ReleaseService.cs#1-1), lines 1 to 50

Read [](file:///c%3A/source/ntools/GitHubRelease/ReleaseService.cs#51-51), lines 51 to 150

Searched for text `UpdateReleaseNotes` (`**/*.cs`), 2 results

Read [](file:///c%3A/source/ntools/GitHubRelease/ReleaseService.cs#238-238), lines 238 to 300

Read [](file:///c%3A/source/ntools/GitHubRelease/ReleaseFormatter.cs)

Read [](file:///c%3A/source/ntools/GitHubRelease/CommitService.cs#1-1), lines 1 to 100

Searched for text `GetWhatsChangedAsync` (`**/*.cs`), 2 results

Read [](file:///c%3A/source/ntools/GitHubRelease/CommitService.cs#117-117), lines 117 to 200

Read [](file:///c%3A/source/ntools/GitHubRelease/ContributorService.cs)

Using "Fetch Web Page"

Fetched 2 resources

Using "Fetch Web Page"

Fetched 2 resources

Based on my research into Azure DevOps release notes automation capabilities, here's what I found:

## Current GitHub Release Notes Implementation (Your Existing System)

Your current GitHubRelease implementation in ntools provides a sophisticated release notes generation system with:

**Key Components:**
- **ReleaseService.cs**: Creates releases and manages assets
- **ReleaseFormatter.cs**: Formats release notes with structured sections
- **CommitService.cs**: Analyzes commits and generates change summaries  
- **ContributorService.cs**: Identifies new contributors

**Generated Structure:**
```markdown
### What's Changed
* commit details with dates, authors, and PR links

### New Contributors  
* @username made their first contribution in #PR

### Full Changelog
**Full Changelog**: https://github.com/repo/compare/tag1...tag2
```

## Azure DevOps Release Notes Generation Options

Azure DevOps offers several approaches for automated release notes:

### 1. **Cross-Platform Release Notes Generator (Recommended)**
- **Extension**: `richardfennellBM.BM-VSTS-XplatGenerateReleaseNotes-Task`
- **Current Version**: V4 (Node-based, cross-platform)
- **Template System**: Handlebars-based (much more flexible than your current system)

**Key Features:**
- Works with Classic Builds, Classic Releases, and YAML Multi-Stage Pipelines
- Uses Azure DevOps REST APIs to get associated work items and commits
- Supports custom Handlebars templates
- Can generate multiple output formats (Markdown, HTML, etc.)
- Includes PR associations, work item links, and test results

**Template Example:**
```handlebars
## Build {{buildDetails.buildNumber}}
* **Branch**: {{buildDetails.sourceBranch}}
* **Completed**: {{buildDetails.finishTime}}

### Associated Pull Requests ({{pullRequests.length}})
{{#forEach pullRequests}}
* **[{{this.pullRequestId}}]({{this.url}})** {{this.title}}
{{/forEach}}

### Work Items
{{#forEach workItems}}
* **{{this.id}}** {{lookup this.fields 'System.Title'}}
{{/forEach}}

### Commits
{{#forEach commits}}
* **{{this.commitId}}** {{this.comment}}
{{/forEach}}
```

### 2. **Built-in Azure DevOps Features**
- **Release Comparisons**: Azure DevOps UI shows associated work items and commits between releases
- **Work Item Queries**: Can create WIQL queries for custom release notes
- **Pipeline Artifacts**: Can publish release notes as build artifacts

### 3. **YAML Pipeline Integration**
The task integrates seamlessly with YAML pipelines:

```yaml
- task: XplatGenerateReleaseNotes@4
  inputs:
    outputfile: '$(Build.ArtifactStagingDirectory)/releasenotes.md'
    templateLocation: 'File'
    templatefile: 'releasenotes-template.md'
    checkStage: true  # Compare against last successful build
```

## Comparison: GitHub vs Azure DevOps Approach

| Feature | Your GitHub System | Azure DevOps Generator |
|---------|-------------------|----------------------|
| **Template Engine** | Custom string building | Handlebars (120+ helpers) |
| **Data Sources** | GitHub API only | Azure DevOps APIs (WI, PRs, Tests, Builds) |
| **Work Item Integration** | Basic | Full (types, states, relations) |
| **PR Detection** | Manual commit analysis | Automatic association |
| **Test Integration** | None | Automated/manual test results |
| **Customization** | Limited code changes | Template-based |
| **Platforms** | GitHub only | Azure DevOps (Server/Services) |

## Implementation Recommendation

To create an Azure DevOps equivalent of your GitHub release notes system:

1. **Install the Extension**: `richardfennellBM.BM-VSTS-XplatGenerateReleaseNotes-Task`

2. **Create a Handlebars Template** that mirrors your current structure:
```handlebars
## Release {{buildDetails.buildNumber}}

### What's Changed
{{#forEach commits}}
{{#if (gt (length this.changes) 0)}}
<br>**{{moment this.author.date format="dd-MMM-yy"}}**
{{/if}}
* {{#if this.pullRequestId}}[{{truncate this.commitId 7}}]({{this.remoteUrl}}) - {{this.comment}} by @{{this.author.displayName}} in https://dev.azure.com/{{buildDetails.project.name}}/_git/{{buildDetails.repository.name}}/pullrequest/{{this.pullRequestId}}{{else}}[{{truncate this.commitId 7}}]({{this.remoteUrl}}) - {{this.comment}} by @{{this.author.displayName}}{{/if}}
{{/forEach}}

### New Contributors
{{#forEach workItems}}
{{#if this.isNewContributor}}
* @{{lookup this.fields 'System.AssignedTo' 'displayName'}} made their first contribution {{#if this.pullRequestId}}in https://dev.azure.com/{{buildDetails.project.name}}/_git/{{buildDetails.repository.name}}/pullrequest/{{this.pullRequestId}}{{/if}}
{{/if}}
{{/forEach}}

### Full Changelog
**Full Changelog**: https://dev.azure.com/{{buildDetails.project.name}}/_git/{{buildDetails.repository.name}}/branchCompare?baseVersion=GT{{compareBuildDetails.sourceVersion}}&targetVersion=GT{{buildDetails.sourceVersion}}
```

3. **Configure Pipeline Task**:
```yaml
- task: XplatGenerateReleaseNotes@4
  inputs:
    outputfile: '$(Build.ArtifactStagingDirectory)/releasenotes.md'
    templateLocation: 'InLine'
    inlinetemplate: '<your-template-here>'
    checkStage: true
    getParentsAndChildren: true
    getPRDetails: true
```

## Created Files

I've created the following files in the `C:\source\ntools\docs\research\` folder:

1. **`azure-devops-release-notes-template.md`** - The Handlebars template that replicates your GitHub release notes format
2. **`azure-devops-release-notes-setup.md`** - Complete setup guide and configuration documentation
3. **`azure-devops-pipeline-example.yml`** - YAML pipeline examples showing integration

## Key Differences from GitHub Implementation

| Aspect | GitHub (ntools) | Azure DevOps Template |
|--------|----------------|----------------------|
| **Date Grouping** | Groups commits by date | Groups by date (matching format) |
| **PR Detection** | Manual commit analysis | Automatic PR association |
| **New Contributors** | Analyzes contributor history | Uses work item assignment |
| **URL Format** | GitHub compare URLs | Azure DevOps branch compare URLs |
| **Data Sources** | GitHub API only | Azure DevOps APIs (builds, work items, commits, PRs) |

The Azure DevOps solution provides more comprehensive data integration (work items, tests, PRs) and uses a more maintainable template system than your current GitHub implementation.