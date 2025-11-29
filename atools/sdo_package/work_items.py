"""
SDO Work Items - Business logic for work item operations.

ARCHITECTURAL DECISION: SEPARATE PLATFORM FILES

WHY SEPARATE PLATFORM FILES FOR WORK ITEMS?
Work items have significant platform differences that require separate implementations:

AZURE DEVOPS WORK ITEMS:
- Complex work item types: PBIs, Tasks, Bugs, Epics with different fields
- Parent-child relationships and linking
- Area/Iteration paths, priorities, tags
- Acceptance criteria as separate field
- REST API with complex payload structures

GITHUB WORK ITEMS:
- Simple issue/PR model with standard fields
- Issue references instead of formal relationships
- Labels, assignees, milestones
- Body content with markdown formatting
- CLI-based operations

WHY NOT CONSOLIDATED LIKE REPOSITORIES?
Work item operations are too different to share common code effectively.
Each platform needs completely different field mappings, relationship handling,
and API interactions. Attempting consolidation would create more complexity
than separation.

PLATFORM SUPPORT FOR WORK ITEMS:

AZURE DEVOPS WORK ITEMS:
- Product Backlog Items (PBIs)
- Tasks
- Bugs
- Epics
- Uses Azure DevOps REST APIs
- Supports parent-child relationships and acceptance criteria

GITHUB WORK ITEMS:
- Issues
- Uses GitHub CLI (gh) commands
- Supports issue templates and GitHub-specific metadata

COMMON WORK ITEM FEATURES:
- Markdown parsing for work item content
- Metadata extraction (title, description, acceptance criteria)
- Platform-agnostic result handling
- Error handling and validation

PLATFORM DIVERGENCE:
- Work item types: Azure DevOps has structured types, GitHub uses issues/PRs
- Relationships: Azure DevOps supports parent-child links, GitHub uses issue references
- APIs: REST APIs (Azure DevOps) vs CLI commands (GitHub)
"""

import os
import subprocess
from typing import Optional, Dict, Any
from .exceptions import ValidationError, ConfigurationError, PlatformError, ParsingError


def get_work_item_platform():
    """
    Detect and return the appropriate work item platform based on Git remote.

    Returns:
        Tuple of (platform_name, platform_config): Platform name ('github' or 'azdo') and configuration dict

    Raises:
        PlatformError: If no supported platform is detected
    """
    from .client import extract_platform_info_from_git

    # Use the same platform detection logic as repositories.py
    config = extract_platform_info_from_git()
    if not config:
        raise PlatformError(
            "Could not detect platform from Git remotes. "
            "Ensure you're in a Git repository with a remote configured for GitHub or Azure DevOps."
        )

    platform = config.get("platform")
    if platform not in ["github", "azdo"]:
        raise PlatformError(
            f"Unsupported platform detected: {platform}. "
            "Supported platforms: GitHub, Azure DevOps"
        )

    # For Azure DevOps, check for required PAT
    if platform == "azdo":
        pat = os.environ.get("AZURE_DEVOPS_PAT")
        if not pat:
            raise PlatformError(
                "AZURE_DEVOPS_PAT environment variable not set. "
                "Azure DevOps operations require authentication. Please set your Personal Access Token: "
                "export AZURE_DEVOPS_PAT='your-token-here'"
            )

    return platform, config


class WorkItemResult:
    """Result of work item creation operation."""

    def __init__(
        self,
        success: bool,
        work_item_id: str = None,
        url: str = None,
        platform: str = None,
        error_message: str = None,
    ):
        self.success = success
        self.work_item_id = work_item_id
        self.url = url
        self.platform = platform
        self.error_message = error_message

    def __str__(self):
        if self.success:
            return f"Success: Work item {self.work_item_id} created at {self.url}"
        else:
            return f"Error: {self.error_message}"


class WorkItemManager:
    """Manager for work item operations."""

    def __init__(self, verbose: bool = False):
        self.verbose = verbose

    def create_work_item(self, file_path: str) -> WorkItemResult:
        """Create a work item from markdown file."""
        try:
            # Check if file exists first
            if not os.path.exists(file_path):
                return WorkItemResult(False, error_message=f"File not found: {file_path}")

            # Lazy imports (available in later phases)
            from .parsers.markdown_parser import MarkdownParser
            from .parsers.metadata_parser import MetadataParser

            # Parse the markdown file
            parser = MarkdownParser()
            parsed_result = parser.parse_file(file_path)

            # Extract content and metadata from parsed result
            content = {
                "title": parsed_result["title"],
                "description": parsed_result["description"],
                "acceptance_criteria": parsed_result["acceptance_criteria"],
                "repro_steps": parsed_result.get("repro_steps", ""),
            }
            if self.verbose:
                print("DEBUG: repro_steps in content:", repr(content.get("repro_steps", "")))
            metadata = parsed_result["metadata"]

            # Determine platform and create work item
            detected_platform = MetadataParser.detect_platform(metadata)

            # Map detected platform names to work_items expected names
            if detected_platform == "azdo":
                platform = "azure_devops"
            elif detected_platform == "github":
                platform = "github"
            else:
                platform = detected_platform

            if platform == "azure_devops":
                return self._create_azdo_work_item(content, metadata)
            elif platform == "github":
                return self._create_github_work_item(content, metadata)
            else:
                return WorkItemResult(False, error_message=f"Unknown platform: {platform}")

        except ParsingError as e:
            return WorkItemResult(False, error_message=f"Parsing error: {str(e)}")
        except PlatformError as e:
            return WorkItemResult(False, error_message=f"Platform error: {str(e)}")
        except Exception as e:
            return WorkItemResult(False, error_message=f"Unexpected error: {str(e)}")

    def _create_azdo_work_item(
        self, content: Dict[str, Any], metadata: Dict[str, Any]
    ) -> WorkItemResult:
        """Create Azure DevOps work item."""
        try:
            # Lazy import (available in Phase 4)
            from .platforms.azdo_platform import AzureDevOpsPlatform

            # Extract required parameters
            organization = metadata.get("organization", "")
            project = metadata.get("project", "")
            pat = metadata.get("pat", "")

            if not all([organization, project, pat]):
                return WorkItemResult(
                    False,
                    error_message="Missing Azure DevOps configuration "
                    "(organization, project, pat)",
                )

            # Create platform instance and work item
            platform = AzureDevOpsPlatform(organization, project, pat, verbose=self.verbose)

            # Extract content for work item creation
            title = content.get("title", "")
            description = content.get("description", "")
            acceptance_criteria = content.get("acceptance_criteria", [])
            work_item_type = metadata.get("work_item_type", "Task")

            result = platform.create_work_item(
                title=title,
                description=description,
                acceptance_criteria=acceptance_criteria,
                repro_steps=content.get("repro_steps", ""),
                metadata=metadata,
            )

            if result and result.get("id"):
                return WorkItemResult(
                    success=True,
                    work_item_id=str(result["id"]),
                    url=result.get("url", ""),
                    platform="azure_devops",
                )
            else:
                return WorkItemResult(False, error_message="Azure DevOps work item creation failed")

        except Exception as e:
            return WorkItemResult(False, error_message=f"Azure DevOps error: {str(e)}")

    def _create_github_work_item(
        self, content: Dict[str, Any], metadata: Dict[str, Any]
    ) -> WorkItemResult:
        """Create GitHub work item."""
        try:
            # Lazy import (available in Phase 4)
            from .platforms.github_platform import GitHubPlatform

            # Extract required parameters
            owner = metadata.get("owner", "")
            repo = metadata.get("repo", metadata.get("repository", ""))

            if not all([owner, repo]):
                return WorkItemResult(
                    False, error_message="Missing GitHub configuration (owner, repo)"
                )

            # Create platform instance and work item
            platform = GitHubPlatform(owner, repo, verbose=self.verbose)

            # Extract content for work item creation
            title = content.get("title", "")
            description = content.get("description", "")
            acceptance_criteria = content.get("acceptance_criteria", [])

            result = platform.create_work_item(
                title=title,
                description=description,
                acceptance_criteria=acceptance_criteria,
                metadata=metadata,
            )

            if result and result.get("id"):
                return WorkItemResult(
                    success=True,
                    work_item_id=str(result["id"]),
                    url=result.get("url", ""),
                    platform="github",
                )
            else:
                return WorkItemResult(False, error_message="GitHub work item creation failed")

        except Exception as e:
            return WorkItemResult(False, error_message=f"GitHub error: {str(e)}")


def cmd_workitem_create(args) -> Optional[Dict[str, Any]]:
    """
    Create a work item from markdown file.

    Args:
        args: Object containing command arguments with attributes:
            - file_path: Path to markdown file
            - dry_run: Whether to run in dry-run mode
            - verbose: Whether to show verbose output

    Returns:
        Dictionary containing creation result or None if failed
    """
    try:
        # Lazy imports (available in later phases)
        from .parsers.markdown_parser import MarkdownParser
        from .parsers.metadata_parser import MetadataParser
        from .platforms.azdo_platform import AzureDevOpsPlatform
        from .platforms.github_platform import GitHubPlatform

        # Parse the markdown file
        parser = MarkdownParser()
        content = parser.parse_file(args.file_path)

        # Detect platform from metadata
        platform_name = MetadataParser.detect_platform(content["metadata"])
        print(f"✓ Using platform: {platform_name}")

        # Create platform instance
        if platform_name == "azdo":
            platform = AzureDevOpsPlatform(verbose=args.verbose)
        elif platform_name == "github":
            platform = GitHubPlatform(verbose=args.verbose)
        else:
            raise ConfigurationError(f"Unsupported platform: {platform_name}")

        # Validate authentication
        if not platform.validate_auth():
            return None

        # Convert acceptance criteria to strings for platform compatibility
        acceptance_criteria_strings = []
        for ac in content["acceptance_criteria"]:
            if isinstance(ac, dict) and "text" in ac:
                # Format with checkbox based on completion status
                checkbox = "[x]" if ac.get("completed", False) else "[ ]"
                acceptance_criteria_strings.append(f"{checkbox} {ac['text']}")
            else:
                # Fallback for string format
                acceptance_criteria_strings.append(str(ac))

        # Create the work item
        result = platform.create_work_item(
            title=content["title"],
            description=content["description"],
            metadata=content["metadata"],
            acceptance_criteria=acceptance_criteria_strings,
            repro_steps=content.get("repro_steps", ""),
            dry_run=args.dry_run,
        )

        return result

    except FileNotFoundError:
        raise ValidationError(f"File not found: {args.file_path}")
    except Exception as e:
        if isinstance(e, (ValidationError, ConfigurationError)):
            raise
        else:
            raise ValidationError(f"Failed to create work item: {str(e)}")


def cmd_workitem_list(args):
    """Handle 'sdo workitem list' command."""
    try:
        platform, config = get_work_item_platform()
    except PlatformError as e:
        print(f"❌ {str(e)}")
        return 1

    if platform == "azdo":
        return _cmd_workitem_list_azdo(args, config)
    elif platform == "github":
        return _cmd_workitem_list_github(args, config)
    else:
        print(f"❌ Unsupported platform: {platform}")
        return 1


def _cmd_workitem_list_azdo(args, config):
    """Handle Azure DevOps work item list."""
    from .client import AzureDevOpsClient, get_personal_access_token

    # Get PAT
    pat = get_personal_access_token()
    if not pat:
        print("❌ AZURE_DEVOPS_PAT environment variable not set.")
        return 1

    # Initialize Azure DevOps client
    client = AzureDevOpsClient(
        config["organization"], config["project"], pat, verbose=getattr(args, "verbose", False)
    )

    # Check if --assigned-to-me flag is set
    assigned_to = getattr(args, "assigned_to", None)
    assigned_to_me = getattr(args, "assigned_to_me", False)

    if assigned_to_me:
        # Get current user email
        current_user = client.get_current_user()
        if not current_user:
            print("❌ Could not determine current user. Please ensure you are authenticated.")
            return 1
        assigned_to = current_user
        if getattr(args, "verbose", False):
            print(f"👤 Current user: {current_user}")

    # List work items using client API
    work_items = client.list_work_items(
        work_item_type=getattr(args, "type", None),
        state=getattr(args, "state", None),
        assigned_to=assigned_to,
        area_path=None,  # Could read from project config if needed
        top=getattr(args, "top", 50),
    )

    if work_items is None:
        print("❌ Failed to list work items")
        return 1

    if not work_items:
        filter_desc = []
        if getattr(args, "type", None):
            filter_desc.append(f"type: {args.type}")
        if getattr(args, "state", None):
            filter_desc.append(f"state: {args.state}")
        if assigned_to_me:
            filter_desc.append("assigned to you")
        elif getattr(args, "assigned_to", None):
            filter_desc.append(f"assigned to: {args.assigned_to}")

        criteria = " and ".join(filter_desc) if filter_desc else "criteria"
        print(f"ℹ️  No work items found matching the {criteria}.")
        return 0

    # Display results in table format
    if work_items:
        print(f"📋 Work Items ({len(work_items)} found):")
        print("-" * 140)
        print(
            f"{'ID':<6} {'Type':<20} {'Title':<35} {'State':<12} {'Sprint':<20} {'Assigned To':<15}"
        )
        print("-" * 140)

        for item in work_items:
            fields = item.get("fields", {})
            item_id = str(item.get("id", "N/A"))
            work_item_type = fields.get("System.WorkItemType", "N/A")
            title = fields.get("System.Title", "N/A")
            state = fields.get("System.State", "N/A")
            assigned_to_name = (
                fields.get("System.AssignedTo", {}).get("displayName", "Unassigned")
                if fields.get("System.AssignedTo")
                else "Unassigned"
            )

            # Extract sprint from iteration path
            iteration_path = fields.get("System.IterationPath", "")
            sprint = iteration_path.split("\\")[-1] if "\\" in iteration_path else iteration_path

            # Truncate long fields for table formatting
            title = title[:32] + "..." if len(title) > 35 else title
            sprint = sprint[:17] + "..." if len(sprint) > 20 else sprint
            assigned_to_name = (
                assigned_to_name[:12] + "..." if len(assigned_to_name) > 15 else assigned_to_name
            )

            print(
                f"{item_id:<6} {work_item_type:<20} {title:<35} {state:<12} {sprint:<20} {assigned_to_name:<15}"
            )

        print("-" * 140)

        # Summary by type
        from collections import Counter

        type_counts = Counter(
            item.get("fields", {}).get("System.WorkItemType", "Unknown") for item in work_items
        )
        print("\n📊 Summary:")
        for work_type, count in sorted(type_counts.items()):
            print(f"  {work_type}: {count}")

        if getattr(args, "verbose", False):
            print("\nDetailed URLs:")
            for item in work_items:
                item_id = item.get("id", "N/A")
                url = item.get("_links", {}).get("html", {}).get("href", "N/A")
                print(f"  #{item_id}: {url}")

    return 0


def _cmd_workitem_list_github(args, config):
    """Handle GitHub issues list."""
    # Build gh issue list command
    cmd = ["gh", "issue", "list", "--repo", f"{config['owner']}/{config['repo']}"]

    # Add filters
    if getattr(args, "state", None):
        # Map Azure DevOps states to GitHub states
        state_map = {
            "New": "open",
            "To Do": "open",
            "In Progress": "open",
            "Done": "closed",
            "Closed": "closed",
        }
        gh_state = state_map.get(args.state, args.state.lower())
        cmd.extend(["--state", gh_state])

    if getattr(args, "assigned_to", None):
        cmd.extend(["--assignee", args.assigned_to])
    elif getattr(args, "assigned_to_me", False):
        cmd.extend(["--assignee", "@me"])

    # Add labels filter if type specified
    if getattr(args, "type", None):
        cmd.extend(["--label", args.type.lower()])

    # Add limit
    top = getattr(args, "top", 50)
    cmd.extend(["--limit", str(top)])

    # Add JSON output for parsing
    cmd.append("--json")
    cmd.append("number,title,state,labels,assignees,createdAt,updatedAt")

    try:
        result = subprocess.run(cmd, capture_output=True, text=True, check=True)
        import json

        issues = json.loads(result.stdout)

        if not issues:
            print("ℹ️  No issues found matching the criteria.")
            return 0

        # Display results in table format
        print(f"📋 Issues ({len(issues)} found):")
        print("-" * 120)
        print(f"{'#':<8} {'Title':<50} {'State':<10} {'Labels':<30} {'Assignee':<20}")
        print("-" * 120)

        for issue in issues:
            number = f"#{issue['number']}"
            title = issue["title"][:47] + "..." if len(issue["title"]) > 50 else issue["title"]
            state = issue["state"]
            labels = ", ".join([l["name"] for l in issue["labels"][:2]]) if issue["labels"] else ""
            labels = labels[:27] + "..." if len(labels) > 30 else labels
            assignee = issue["assignees"][0]["login"] if issue["assignees"] else "Unassigned"
            assignee = assignee[:17] + "..." if len(assignee) > 20 else assignee

            print(f"{number:<8} {title:<50} {state:<10} {labels:<30} {assignee:<20}")

        print("-" * 120)
        print(f"\n📊 Total: {len(issues)} issue(s)")

        if getattr(args, "verbose", False):
            print("\nDetailed URLs:")
            for issue in issues:
                print(
                    f"  #{issue['number']}: https://github.com/{config['owner']}/{config['repo']}/issues/{issue['number']}"
                )

        return 0

    except subprocess.CalledProcessError as e:
        print(f"❌ Failed to list GitHub issues: {e.stderr}")
        return 1
    except FileNotFoundError:
        print("❌ GitHub CLI (gh) not found. Please install from https://cli.github.com/")
        return 1
    except Exception as e:
        print(f"❌ Error listing issues: {e}")
        if getattr(args, "verbose", False):
            import traceback

            traceback.print_exc()
        return 1


def cmd_workitem_show(args):
    """Handle 'sdo workitem show' command."""
    try:
        platform, config = get_work_item_platform()
    except PlatformError as e:
        print(f"❌ {str(e)}")
        return 1

    if platform == "azdo":
        return _cmd_workitem_show_azdo(args, config)
    elif platform == "github":
        return _cmd_workitem_show_github(args, config)
    else:
        print(f"❌ Unsupported platform: {platform}")
        return 1


def _cmd_workitem_show_azdo(args, config):
    """Handle Azure DevOps work item show."""
    from .client import AzureDevOpsClient, get_personal_access_token

    # Get PAT
    pat = get_personal_access_token()
    if not pat:
        print("❌ AZURE_DEVOPS_PAT environment variable not set.")
        return 1

    # Initialize Azure DevOps client
    client = AzureDevOpsClient(
        config["organization"], config["project"], pat, verbose=getattr(args, "verbose", False)
    )

    # Get work item details using client API
    result = client.get_work_item(args.id)

    if result:
        fields = result.get("fields", {})
        print(f"Work Item #{result['id']}")
        print(f"{'='*70}")
        print(f"Type:        {fields.get('System.WorkItemType', 'N/A')}")
        print(f"Title:       {fields.get('System.Title', 'N/A')}")
        print(f"State:       {fields.get('System.State', 'N/A')}")
        print(
            f"Assigned To: {fields.get('System.AssignedTo', {}).get('displayName', 'Unassigned')}"
        )
        print(f"Created:     {fields.get('System.CreatedDate', 'N/A')}")
        print(f"Changed:     {fields.get('System.ChangedDate', 'N/A')}")

        if fields.get("System.Description"):
            print(f"\nDescription:")
            print(f"{fields['System.Description']}")

        # Show iteration path (sprint)
        iteration_path = fields.get("System.IterationPath")
        if iteration_path:
            print(f"\nIteration:   {iteration_path}")

        # Show Acceptance Criteria if present
        ac_field = None
        for field_name, field_value in fields.items():
            if field_name.lower().find("acceptance") != -1 and field_value:
                ac_field = field_name
                break

        # If no dedicated field, try to extract from description
        ac_content = None
        if ac_field:
            ac_content = fields[ac_field]
        else:
            # Try to extract from description
            description = fields.get("System.Description", "")
            if description and "<h3>Acceptance Criteria</h3>" in description:
                # Extract content after the Acceptance Criteria header
                ac_start = description.find("<h3>Acceptance Criteria</h3>")
                if ac_start != -1:
                    ac_part = description[ac_start + len("<h3>Acceptance Criteria</h3>") :]
                    # Find the next header or end of description
                    next_header = ac_part.find("<h3>")
                    if next_header != -1:
                        ac_content = ac_part[:next_header].strip()
                    else:
                        ac_content = ac_part.strip()

        if ac_content:
            print(f"\nAcceptance Criteria:")
            # Clean up HTML for display
            import re

            clean_ac = re.sub(r"<li[^>]*>", "- ", ac_content)
            clean_ac = re.sub(r"</li>", "", clean_ac)
            clean_ac = re.sub(r"<[^>]+>", "", clean_ac)
            clean_ac = "\n".join(line.strip() for line in clean_ac.splitlines() if line.strip())
            print(clean_ac)

        # Show comments if requested
        if getattr(args, "comments", False):
            comments_result = client.get_work_item_comments(args.id)
            if comments_result and comments_result.get("comments"):
                print(f"\n{'='*70}")
                print(f"💬 Comments ({len(comments_result['comments'])}):")
                print(f"{'='*70}")
                for comment in comments_result["comments"]:
                    author = comment.get("createdBy", {}).get("displayName", "Unknown")
                    created = comment.get("createdDate", "N/A")
                    text = comment.get("text", "")
                    print(f"\n{author} • {created}")
                    print(f"{'-'*70}")
                    print(text)

        # Show URL
        url = result.get("_links", {}).get("html", {}).get("href")
        if url:
            print(f"\nURL: {url}")

        return 0
    else:
        print(f"❌ Work item #{args.id} not found or not accessible")
        return 1


def _cmd_workitem_show_github(args, config):
    """Handle GitHub issue show."""
    # Use gh issue view command
    cmd = ["gh", "issue", "view", str(args.id), "--repo", f"{config['owner']}/{config['repo']}"]
    cmd.extend(
        ["--json", "number,title,state,body,labels,assignees,createdAt,updatedAt,url,comments"]
    )

    try:
        result = subprocess.run(
            cmd, capture_output=True, text=True, check=True, encoding="utf-8", errors="replace"
        )
        import json

        issue = json.loads(result.stdout)

        print(f"Issue #{issue['number']}")
        print(f"{'='*70}")
        print(f"Title:       {issue['title']}")
        print(f"State:       {issue['state']}")
        assignees = (
            ", ".join([a["login"] for a in issue["assignees"]])
            if issue["assignees"]
            else "Unassigned"
        )
        print(f"Assigned To: {assignees}")
        print(f"Created:     {issue['createdAt']}")
        print(f"Updated:     {issue['updatedAt']}")

        if issue["labels"]:
            labels = ", ".join([l["name"] for l in issue["labels"]])
            print(f"Labels:      {labels}")

        if issue.get("body"):
            print(f"\nDescription:")
            print(f"{issue['body']}")

        # Show comments if requested
        if getattr(args, "comments", False) and issue.get("comments"):
            print(f"\n{'='*70}")
            print(f"💬 Comments ({len(issue['comments'])}):")
            print(f"{'='*70}")
            for comment in issue["comments"]:
                author = comment.get("author", {}).get("login", "Unknown")
                created = comment.get("createdAt", "N/A")
                body = comment.get("body", "")
                print(f"\n{author} • {created}")
                print(f"{'-'*70}")
                print(body)

        print(f"\nURL: {issue['url']}")

        return 0

    except subprocess.CalledProcessError as e:
        print(f"❌ Issue #{args.id} not found or not accessible")
        if getattr(args, "verbose", False):
            print(f"Error: {e.stderr}")
        return 1
    except FileNotFoundError:
        print("❌ GitHub CLI (gh) not found. Please install from https://cli.github.com/")
        return 1
    except Exception as e:
        print(f"❌ Error showing issue: {e}")
        if getattr(args, "verbose", False):
            import traceback

            traceback.print_exc()
        return 1


def cmd_workitem_update(args):
    """Handle 'sdo workitem update' command."""
    try:
        platform, config = get_work_item_platform()
    except PlatformError as e:
        print(f"❌ {str(e)}")
        return 1

    if platform == "azdo":
        return _cmd_workitem_update_azdo(args, config)
    elif platform == "github":
        return _cmd_workitem_update_github(args, config)
    else:
        print(f"❌ Unsupported platform: {platform}")
        return 1


def _cmd_workitem_update_azdo(args, config):
    """Handle Azure DevOps work item update."""
    from .client import AzureDevOpsClient, get_personal_access_token

    # Get PAT
    pat = get_personal_access_token()
    if not pat:
        print("❌ AZURE_DEVOPS_PAT environment variable not set.")
        return 1

    # Initialize Azure DevOps client
    client = AzureDevOpsClient(
        config["organization"], config["project"], pat, verbose=getattr(args, "verbose", False)
    )

    # Update work item using client API
    result = client.update_work_item(
        work_item_id=args.id,
        title=getattr(args, "title", None),
        description=getattr(args, "description", None),
        assigned_to=getattr(args, "assigned_to", None),
        state=getattr(args, "state", None),
    )

    if result:
        print(f"✅ Work item #{args.id} updated successfully")
        print(f"   Title: {result['fields'].get('System.Title', 'N/A')}")
        print(f"   State: {result['fields'].get('System.State', 'N/A')}")
        if getattr(args, "verbose", False):
            print(f"   URL: {result.get('_links', {}).get('html', {}).get('href', 'N/A')}")
        return 0
    else:
        print(f"❌ Failed to update work item #{args.id}")
        return 1


def _cmd_workitem_update_github(args, config):
    """Handle GitHub issue update."""
    # Build gh issue edit command
    cmd = ["gh", "issue", "edit", str(args.id), "--repo", f"{config['owner']}/{config['repo']}"]

    if getattr(args, "title", None):
        cmd.extend(["--title", args.title])

    if getattr(args, "description", None):
        cmd.extend(["--body", args.description])

    # Note: GitHub uses assignee (singular) for setting one assignee
    if getattr(args, "assigned_to", None):
        cmd.extend(["--add-assignee", args.assigned_to])

    # Map state to GitHub state (open/closed)
    if getattr(args, "state", None):
        state_map = {
            "New": "open",
            "To Do": "open",
            "In Progress": "open",
            "Done": "closed",
            "Closed": "closed",
        }
        gh_state = state_map.get(args.state, args.state.lower())
        if gh_state == "closed":
            cmd.append("--closed")
        elif gh_state == "open":
            cmd.append("--reopen")

    try:
        result = subprocess.run(cmd, capture_output=True, text=True, check=True)
        print(f"✅ Issue #{args.id} updated successfully")
        if getattr(args, "verbose", False):
            print(f"   URL: https://github.com/{config['owner']}/{config['repo']}/issues/{args.id}")
        return 0

    except subprocess.CalledProcessError as e:
        print(f"❌ Failed to update issue #{args.id}")
        if getattr(args, "verbose", False):
            print(f"Error: {e.stderr}")
        return 1
    except FileNotFoundError:
        print("❌ GitHub CLI (gh) not found. Please install from https://cli.github.com/")
        return 1
    except Exception as e:
        print(f"❌ Error updating issue: {e}")
        if getattr(args, "verbose", False):
            import traceback

            traceback.print_exc()
        return 1


def cmd_workitem_comment(args):
    """Handle 'sdo workitem comment' command."""
    try:
        platform, config = get_work_item_platform()
    except PlatformError as e:
        print(f"❌ {str(e)}")
        return 1

    if platform == "azdo":
        return _cmd_workitem_comment_azdo(args, config)
    elif platform == "github":
        return _cmd_workitem_comment_github(args, config)
    else:
        print(f"❌ Unsupported platform: {platform}")
        return 1


def _cmd_workitem_comment_azdo(args, config):
    """Handle Azure DevOps work item comment."""
    from .client import AzureDevOpsClient, get_personal_access_token

    # Get PAT
    pat = get_personal_access_token()
    if not pat:
        print("❌ AZURE_DEVOPS_PAT environment variable not set.")
        return 1

    # Initialize Azure DevOps client
    client = AzureDevOpsClient(
        config["organization"], config["project"], pat, verbose=getattr(args, "verbose", False)
    )

    # Add comment to work item using client API
    result = client.add_work_item_comment(args.id, args.text)

    if result:
        print(f"✅ Comment added to work item #{args.id}")
        return 0
    else:
        print(f"❌ Failed to add comment to work item #{args.id}")
        return 1


def _cmd_workitem_comment_github(args, config):
    """Handle GitHub issue comment."""
    # Use gh issue comment command
    cmd = [
        "gh",
        "issue",
        "comment",
        str(args.id),
        "--repo",
        f"{config['owner']}/{config['repo']}",
        "--body",
        args.text,
    ]

    try:
        result = subprocess.run(cmd, capture_output=True, text=True, check=True)
        print(f"✅ Comment added to issue #{args.id}")
        return 0

    except subprocess.CalledProcessError as e:
        print(f"❌ Failed to add comment to issue #{args.id}")
        if getattr(args, "verbose", False):
            print(f"Error: {e.stderr}")
        return 1
    except FileNotFoundError:
        print("❌ GitHub CLI (gh) not found. Please install from https://cli.github.com/")
        return 1
    except Exception as e:
        print(f"❌ Error adding comment: {e}")
        if getattr(args, "verbose", False):
            import traceback

            traceback.print_exc()
        return 1


def main():
    """For testing purposes."""
    print("SDO Work Items module loaded successfully")


if __name__ == "__main__":
    main()
