# SDO Command Mappings

This document provides mappings between SDO commands and their equivalent GitHub CLI (`gh`) and Azure CLI (`az`) commands.

**Quick Access**: Use `sdo map` to view these mappings directly in your terminal:
- `sdo map` - Show mappings for detected platform
- `sdo map gh` - Show GitHub CLI mappings
- `sdo map azdo` - Show Azure CLI mappings
- `sdo map --all` - Show all mappings

## Work Item Commands

### sdo workitem create

| SDO Command | GitHub CLI | Azure CLI |
|-------------|------------|-----------|
| `sdo workitem create --file issue.md` | `gh issue create --title "Title" --body "Description" --label "bug"` | `az boards work-item create --title "Title" --type "Product Backlog Item" --description "Description"` |

### sdo workitem list

| SDO Command | GitHub CLI | Azure CLI |
|-------------|------------|-----------|
| `sdo workitem list` | `gh issue list` | `az boards query --wiql "SELECT [System.Id], [System.Title] FROM WorkItems"` |
| `sdo workitem list --type Task` | `gh issue list --label "task"` | `az boards query --wiql "SELECT [System.Id], [System.Title] FROM WorkItems WHERE [System.WorkItemType] = 'Task'"` |
| `sdo workitem list --state "In Progress"` | `gh issue list --state "open" --label "in-progress"` | `az boards query --wiql "SELECT [System.Id], [System.Title] FROM WorkItems WHERE [System.State] = 'In Progress'"` |
| `sdo workitem list --assigned-to-me` | `gh issue list --assignee "@me"` | `az boards query --wiql "SELECT [System.Id], [System.Title] FROM WorkItems WHERE [System.AssignedTo] = @Me"` |

### sdo workitem show

| SDO Command | GitHub CLI | Azure CLI |
|-------------|------------|-----------|
| `sdo workitem show --id 123` | `gh issue view 123` | `az boards work-item show --id 123` |
| `sdo workitem show --id 123 --comments` | `gh issue view 123 --comments` | `az boards work-item show --id 123` (comments shown separately) |

### sdo workitem update

| SDO Command | GitHub CLI | Azure CLI |
|-------------|------------|-----------|
| `sdo workitem update --id 123 --title "New Title"` | `gh issue edit 123 --title "New Title"` | `az boards work-item update --id 123 --fields "System.Title=New Title"` |
| `sdo workitem update --id 123 --state Done` | `gh issue edit 123 --state "closed"` | `az boards work-item update --id 123 --state "Done"` |
| `sdo workitem update --id 123 --assigned-to "user@domain.com"` | `gh issue edit 123 --assignee "user"` | `az boards work-item update --id 123 --assigned-to "user@domain.com"` |

### sdo workitem comment

| SDO Command | GitHub CLI | Azure CLI |
|-------------|------------|-----------|
| `sdo workitem comment --id 123 --text "Comment text"` | `gh issue comment 123 --body "Comment text"` | `az boards work-item update --id 123 --discussion "Comment text"` |

## Repository Commands

### sdo repo create

| SDO Command | GitHub CLI | Azure CLI |
|-------------|------------|-----------|
| `sdo repo create` | `gh repo create <repo-name>` | `az repos create --name <repo-name> --project <project>` |

### sdo repo show

| SDO Command | GitHub CLI | Azure CLI |
|-------------|------------|-----------|
| `sdo repo show` | `gh repo view` | `az repos show --repository <repo>` |

### sdo repo ls

| SDO Command | GitHub CLI | Azure CLI |
|-------------|------------|-----------|
| `sdo repo ls` | `gh repo list` | `az repos list --project <project>` |

### sdo repo delete

| SDO Command | GitHub CLI | Azure CLI |
|-------------|------------|-----------|
| `sdo repo delete` | `gh repo delete <repo>` | `az repos delete --id <repo-id>` |

## Pull Request Commands

### sdo pr create

| SDO Command | GitHub CLI | Azure CLI |
|-------------|------------|-----------|
| `sdo pr create --file pr.md --work-item 123` | `gh pr create --title "Title" --body "Description"` | `az repos pr create --title "Title" --description "Description" --work-items 123` |

### sdo pr show

| SDO Command | GitHub CLI | Azure CLI |
|-------------|------------|-----------|
| `sdo pr show 123` | `gh pr view 123` | `az repos pr show --id 123` |

### sdo pr status

| SDO Command | GitHub CLI | Azure CLI |
|-------------|------------|-----------|
| `sdo pr status 123` | `gh pr view 123 --json state` | `az repos pr show --id 123 --query "{status:status}"` |

### sdo pr ls

| SDO Command | GitHub CLI | Azure CLI |
|-------------|------------|-----------|
| `sdo pr ls` | `gh pr list` | `az repos pr list` |
| `sdo pr ls --status completed` | `gh pr list --state "closed"` | `az repos pr list --status "completed"` |

### sdo pr update

| SDO Command | GitHub CLI | Azure CLI |
|-------------|------------|-----------|
| `sdo pr update --pr-id 123 --title "New Title"` | `gh pr edit 123 --title "New Title"` | `az repos pr update --id 123 --title "New Title"` |
| `sdo pr update --pr-id 123 --status completed` | `gh pr edit 123 --state "closed"` | `az repos pr update --id 123 --status "completed"` |

## Pipeline Commands

### sdo pipeline create

| SDO Command | GitHub CLI | Azure CLI |
|-------------|------------|-----------|
| `sdo pipeline create` | `gh workflow run <workflow-file>` (creates workflow run) | `az pipelines create --name <pipeline> --yml-path <path>` |

### sdo pipeline show

| SDO Command | GitHub CLI | Azure CLI |
|-------------|------------|-----------|
| `sdo pipeline show` | `gh workflow view <workflow-file>` | `az pipelines show --name <pipeline>` |

### sdo pipeline ls

| SDO Command | GitHub CLI | Azure CLI |
|-------------|------------|-----------|
| `sdo pipeline ls` | `gh workflow list` | `az pipelines list` |

### sdo pipeline run

| SDO Command | GitHub CLI | Azure CLI |
|-------------|------------|-----------|
| `sdo pipeline run --branch main` | `gh workflow run <workflow> --ref main` | `az pipelines run --name <pipeline> --branch main` |

### sdo pipeline status

| SDO Command | GitHub CLI | Azure CLI |
|-------------|------------|-----------|
| `sdo pipeline status` | `gh run list --limit 1` | `az pipelines runs list --pipeline-name <pipeline> --top 1` |
| `sdo pipeline status 12345` | `gh run view 12345` | `az pipelines runs show --id 12345` |

### sdo pipeline logs

| SDO Command | GitHub CLI | Azure CLI |
|-------------|------------|-----------|
| `sdo pipeline logs 12345` | `gh run view 12345 --log` | `az pipelines runs logs --id 12345` |

### sdo pipeline lastbuild

| SDO Command | GitHub CLI | Azure CLI |
|-------------|------------|-----------|
| `sdo pipeline lastbuild` | `gh run list --limit 5` | `az pipelines runs list --pipeline-name <pipeline> --top 5` |

### sdo pipeline update

| SDO Command | GitHub CLI | Azure CLI |
|-------------|------------|-----------|
| `sdo pipeline update` | Manual workflow file update | `az pipelines update --name <pipeline> --yml-path <new-path>` |

### sdo pipeline delete

| SDO Command | GitHub CLI | Azure CLI |
|-------------|------------|-----------|
| `sdo pipeline delete` | `gh workflow disable <workflow>` | `az pipelines delete --name <pipeline>` |
| `sdo pipeline delete my-pipeline` | `gh workflow disable <workflow>` | `az pipelines delete --name my-pipeline` |
| `sdo pipeline delete my-pipeline --force` | `gh workflow disable <workflow>` | `az pipelines delete --name my-pipeline --yes` |

## Notes

- **Platform Detection**: SDO automatically detects whether you're in a GitHub or Azure DevOps repository and uses the appropriate platform
- **Cross-Platform**: SDO provides unified commands across both platforms, while `gh` and `az` are platform-specific
- **Rich Content**: SDO supports markdown files for work items and PRs, providing richer content than direct CLI commands
- **Integration**: SDO commands often include additional features like work item linking and acceptance criteria that aren't available in basic CLI commands
- **Authentication**: SDO uses environment variables (`AZURE_DEVOPS_PAT`, `GITHUB_TOKEN`) while `gh` and `az` use their own authentication methods

## Is it worth the effort to create an sdo tool, and continue to maintain?  or it would better time spent taking advantage of native tools?
This a balanced analysis of whether creating and maintaining SDO was worthwhile compared to relying on native tools like GitHub CLI (`gh`) and Azure CLI (`az`). This is informed by the tool's design goals, the command mappings we created, and the broader DevOps ecosystem. My assessment considers both technical and practical factors.

### SDO's Core Value Proposition
SDO was designed as a **cross-platform, zero-configuration DevOps CLI** to unify operations across Azure DevOps and GitHub. Key strengths include:
- **Unified Interface**: A single tool for work items, repositories, pull requests, and pipelines, regardless of platform. This reduces cognitive load for teams working in hybrid environments (e.g., Azure DevOps for enterprise and GitHub for open-source).
- **Automatic Platform Detection**: Extracts organization/project details from Git remotes, eliminating manual configuration. Users don't need to remember which CLI to use or set environment variables for each platform.
- **Rich Content Handling**: Supports markdown files for creating work items and PRs with formatted descriptions, checklists, and attachments—features not natively streamlined in `gh` or `az`.
- **Consistency and Simplicity**: Commands follow a predictable structure (e.g., `sdo workitem create` vs. platform-specific syntax). It abstracts away API differences, making it easier for non-experts.
- **Specialized Features**: Bulk operations (e.g., via the complementary `wi.csproj` tool), enhanced error handling, and logging tailored for DevOps workflows.

From the command mappings we documented, SDO effectively bridges gaps:
- For GitHub users, it provides Azure DevOps parity without switching tools.
- For Azure DevOps users, it offers GitHub integration with richer content support.
- It excels in scenarios with frequent cross-platform work or complex markdown-based workflows.

### Comparison with Native Tools
Native tools are mature, officially supported, and widely adopted:
- **GitHub CLI (`gh`)**: Excellent for GitHub-specific tasks (issues, PRs, workflows). It's lightweight, has extensive community support, and integrates seamlessly with GitHub's ecosystem (e.g., GitHub Actions).
- **Azure CLI (`az`)**: Comprehensive for Azure DevOps and broader Azure services. It's battle-tested, receives frequent updates, and supports advanced features like work item queries and pipeline management.

**Where SDO Shines**:
- **Hybrid Teams**: If your workflow involves both platforms (e.g., mirroring repos or managing work items across Azure DevOps and GitHub), SDO reduces tool-switching friction. Native tools require separate commands and configurations.
- **User Experience**: For less technical users or teams prioritizing simplicity, SDO's zero-config approach and markdown support can improve productivity. Native CLIs often require more setup (e.g., PATs, organization IDs).
- **Customization**: SDO allows domain-driven extensions (e.g., custom work item templates), which native tools don't easily support without scripting.

**Where Native Tools Excel**:
- **Maturity and Reliability**: `gh` and `az` are backed by their respective platforms, with automatic updates and extensive documentation. SDO requires ongoing maintenance to keep pace with API changes (e.g., GitHub's REST API v4 or Azure DevOps API versions).
- **Feature Depth**: Native tools offer more granular control and integrations. For example, `az boards` has advanced WIQL queries for work items, while `gh` supports GitHub-specific features like draft PRs or repository templates.
- **Ecosystem Integration**: They work natively with CI/CD pipelines, IDEs, and other tools without additional layers.
- **Performance**: Native tools are optimized for their platforms, potentially faster for single-platform operations.

### Maintenance Costs and Effort Assessment
Creating SDO involved significant upfront effort (modular Python architecture, platform abstractions, markdown parsing), but ongoing maintenance is the bigger question:

- **Development Overhead**: SDO requires tracking API changes for two platforms, handling authentication (PATs/tokens), and ensuring cross-platform compatibility. From the project history, updates have included architecture refinements and feature additions, which is time-intensive.
- **Testing and Validation**: The codebase includes unit tests, but integration testing with real APIs (as noted in the guidelines) adds complexity. Native tools handle this internally.
- **User Adoption**: SDO needs documentation (like the mappings we created) and training, whereas `gh` and `az` are industry standards with vast resources.
- **Resource Allocation**: If your team has limited bandwidth, maintaining a custom tool might divert effort from core features. The guidelines emphasize validation workflows (e.g., linting, testing), which take 30-60 seconds per run—multiplied across development cycles.

**Was It Worth It?**
- **Yes, in Specific Contexts**: If SDO serves a niche need (e.g., hybrid DevOps shops, teams with heavy markdown-based workflows, or organizations wanting a unified CLI), the effort has paid off. It provides value by reducing complexity and enabling features not easily replicated with native tools. The domain-driven architecture makes it extensible for future enhancements.
- **Potentially Not, for General Use**: For single-platform teams or those comfortable with native CLIs, SDO might be overkill. The maintenance burden could be better spent on scripting native tools or using existing integrations (e.g., GitHub Actions with Azure DevOps extensions). Native tools evolve rapidly and are "future-proof" without custom maintenance.

### Recommendations
- **Evaluate Your Use Case**: If SDO is actively used and saves time for your team, continue maintaining it with periodic reviews (e.g., quarterly API compatibility checks). Consider open-sourcing it for community contributions to reduce maintenance load.
- **Hybrid Approach**: Use SDO for unified operations and fall back to native tools for platform-specific advanced features. The command mappings we created can help with this.
- **Alternatives to Consider**: 
  - **Scripting**: Combine `gh` and `az` with shell scripts or tools like `jq` for custom workflows.
  - **Third-Party Tools**: Explore tools like `devops-cli` or GitHub's official extensions if they meet your needs.
  - **Deprecation Path**: If adoption is low, consider archiving SDO and migrating users to native tools, documenting the mappings as a transition guide.
- **Metrics for Decision**: Track usage (e.g., command frequency), maintenance time, and user feedback. If SDO isn't providing clear ROI, pivot to native tools.

