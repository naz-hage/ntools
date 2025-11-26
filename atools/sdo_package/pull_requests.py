"""
Pull Request management commands for SDO CLI tool.

This module provides comprehensive pull request operations across
GitHub and Azure DevOps platforms, including create, list, show,
approve, and merge functionality.
"""

import argparse
import json
import logging
import sys
from pathlib import Path
from typing import Any, Dict, List, Optional, Tuple

from .exceptions import (
    AuthenticationError,
    FileOperationError,
    PlatformError,
    ValidationError,
)
from .platforms.pr_base import PRPlatform

logger = logging.getLogger(__name__)


def get_pr_platform() -> PRPlatform:
    """
    Detect and return the appropriate PR platform based on Git remote configuration.

    Returns:
        PRPlatform: Configured platform instance for PR operations

    Raises:
        PlatformError: If no supported platform is detected
    """
    # Import here to avoid circular imports
    from .platforms.github_pr_platform import GitHubPullRequestPlatform
    from .platforms.azdo_pr_platform import AzureDevOpsPullRequestPlatform

    # Check for GitHub remote
    try:
        import subprocess
        result = subprocess.run(
            ["git", "remote", "-v"],
            capture_output=True,
            text=True,
            check=True,
            cwd="."
        )

        remotes = result.stdout.lower()
        if "github.com" in remotes:
            return GitHubPullRequestPlatform()
        elif any(service in remotes for service in ["dev.azure.com", "visualstudio.com"]):
            return AzureDevOpsPullRequestPlatform()
        else:
            raise PlatformError(
                "No supported Git hosting platform detected. "
                "Supported platforms: GitHub, Azure DevOps"
            )
    except subprocess.CalledProcessError as e:
        raise PlatformError(f"Failed to detect Git remote: {e}")
    except FileNotFoundError:
        raise PlatformError("Git is not available or not in a Git repository")


def read_markdown_pr_file(file_path: str) -> Tuple[str, str]:
    """
    Read and parse a markdown file for PR title and description.

    Args:
        file_path: Path to the markdown file

    Returns:
        Tuple of (title, description)

    Raises:
        FileOperationError: If file cannot be read or parsed
    """
    path = Path(file_path).resolve()

    if not path.exists():
        raise FileOperationError(f"PR description file not found: {file_path}")

    if not path.is_file():
        raise FileOperationError(f"Path is not a file: {file_path}")

    try:
        content = path.read_text(encoding='utf-8')

        # Parse title from first heading
        lines = content.split('\n')
        title = ""
        description_lines = []

        for i, line in enumerate(lines):
            line = line.strip()
            if line.startswith('# '):
                title = line[2:].strip()
                description_lines = lines[i + 1:]
                break
            elif line:  # First non-empty line that's not a heading
                description_lines = lines[i:]
                break

        if not title:
            # Use first line as title if no heading found
            title = lines[0].strip() if lines else ""

        # Extract description (skip empty lines at start)
        description = '\n'.join(description_lines).strip()
        while description.startswith('\n'):
            description = description[1:]
        while description.endswith('\n'):
            description = description[:-1]

        if not title:
            raise ValidationError("No title found in markdown file")

        return title, description

    except (OSError, UnicodeDecodeError) as e:
        raise FileOperationError(f"Failed to read markdown file: {e}")


def cmd_pr_create(args: argparse.Namespace) -> None:
    """
    Create a new pull request.

    Args:
        args: Parsed command line arguments
    """
    try:
        platform = get_pr_platform()

        # Get title and description
        if hasattr(args, 'file') and args.file:
            title, description = read_markdown_pr_file(args.file)
        elif hasattr(args, 'title') and args.title:
            title = args.title
            description = getattr(args, 'description', '') or ''
        else:
            raise ValidationError("Either --file or --title must be provided")

        # Get other parameters
        source_branch = getattr(args, 'source_branch', None)
        target_branch = getattr(args, 'target_branch', None)
        work_item_id = getattr(args, 'work_item', None)
        draft = getattr(args, 'draft', False)
        dry_run = getattr(args, 'dry_run', False)

        # Validate required work item
        if not work_item_id:
            raise ValidationError("Work item ID is required when creating a pull request")
        
        # Ensure work item ID is valid
        try:
            work_item_id = int(work_item_id)
            if work_item_id <= 0:
                raise ValueError()
        except (ValueError, TypeError):
            raise ValidationError(f"Invalid work item ID: {work_item_id}. Must be a positive integer.")

        if dry_run:
            print("[DRY RUN] Would create PR with:")
            print(f"  Title: {title}")
            print(f"  Description: {description[:100]}{'...' if len(description) > 100 else ''}")
            if source_branch:
                print(f"  Source Branch: {source_branch}")
            if target_branch:
                print(f"  Target Branch: {target_branch}")
            if work_item_id:
                print(f"  Work Item: {work_item_id}")
            print(f"  Draft: {draft}")
            print(f"  Platform: {platform.__class__.__name__}")
            return

        # Create the PR
        pr_url = platform.create_pull_request(
            title=title,
            description=description,
            source_branch=source_branch,
            target_branch=target_branch,
            work_item_id=work_item_id,
            draft=draft
        )

        print(f"[OK] Pull request created successfully!")
        print(f"URL: {pr_url}")

        if getattr(args, 'verbose', False):
            print(f"Platform: {platform.__class__.__name__}")

    except (PlatformError, AuthenticationError, ValidationError, FileOperationError) as e:
        logger.error(f"Failed to create PR: {e}")
        print(f"[ERROR] {e}")
        sys.exit(1)
    except Exception as e:
        logger.error(f"Unexpected error creating PR: {e}")
        print(f"[ERROR] An unexpected error occurred: {e}")
        sys.exit(1)


def cmd_pr_show(args: argparse.Namespace) -> None:
    """
    Show details of a pull request.

    Args:
        args: Parsed command line arguments
    """
    try:
        platform = get_pr_platform()

        pr_details = platform.get_pull_request(args.pr_number)

        print(f"Pull Request #{args.pr_number}")
        print("=" * 50)
        print(f"Title: {pr_details.get('title', 'N/A')}")
        print(f"Status: {pr_details.get('status', 'N/A')}")
        print(f"Author: {pr_details.get('author', 'N/A')}")
        print(f"Source: {pr_details.get('source_branch', 'N/A')}")
        print(f"Target: {pr_details.get('target_branch', 'N/A')}")

        # Display work items if present
        work_items = pr_details.get('work_items', [])
        if work_items:
            print(f"Work Items: {', '.join(map(str, work_items))}")
        else:
            print("Work Items: None")

        if pr_details.get('description'):
            print(f"\nDescription:\n{pr_details['description']}")

        if getattr(args, 'verbose', False):
            print(f"\nFull Details:")
            print(json.dumps(pr_details, indent=2, default=str))

    except (PlatformError, AuthenticationError) as e:
        logger.error(f"Failed to show PR: {e}")
        print(f"[ERROR] {e}")
        sys.exit(1)
    except Exception as e:
        logger.error(f"Unexpected error showing PR: {e}")
        print(f"[ERROR] An unexpected error occurred: {e}")
        sys.exit(1)


def cmd_pr_status(args: argparse.Namespace) -> None:
    """
    Show status of a pull request.

    Args:
        args: Parsed command line arguments
    """
    try:
        platform = get_pr_platform()

        pr_details = platform.get_pull_request(args.pr_number)

        status = pr_details.get('status', 'Unknown')
        status_icon = {
            'active': '🟢',
            'completed': '✅',
            'abandoned': '❌',
            'draft': '📝'
        }.get(status.lower(), '❓')

        print(f"{status_icon} PR #{args.pr_number}: {status.upper()}")
        print(f"Title: {pr_details.get('title', 'N/A')}")
        print(f"Author: {pr_details.get('author', 'N/A')}")
        print(f"Branch: {pr_details.get('source_branch', 'N/A')} → {pr_details.get('target_branch', 'N/A')}")

        # Show additional status info if available
        if pr_details.get('merge_status'):
            print(f"Merge Status: {pr_details['merge_status']}")
        if pr_details.get('reviewers'):
            reviewers = pr_details['reviewers']
            approved = sum(1 for r in reviewers if r.get('vote') == 'approve')
            print(f"Reviews: {approved}/{len(reviewers)} approved")

        if getattr(args, 'verbose', False):
            print(f"\nFull Status Details:")
            print(json.dumps(pr_details, indent=2, default=str))

    except (PlatformError, AuthenticationError) as e:
        logger.error(f"Failed to get PR status: {e}")
        print(f"[ERROR] {e}")
        sys.exit(1)
    except Exception as e:
        logger.error(f"Unexpected error getting PR status: {e}")
        print(f"[ERROR] An unexpected error occurred: {e}")
        sys.exit(1)


def cmd_pr_list(args: argparse.Namespace) -> None:
    """
    List pull requests.

    Args:
        args: Parsed command line arguments
    """
    try:
        platform = get_pr_platform()

        # Get filter parameters
        state = getattr(args, 'status', 'active')
        author = getattr(args, 'author', None)
        limit = getattr(args, 'top', 10)

        # Map CLI state values to platform values
        state_mapping = {
            'active': 'open',
            'completed': 'closed',
            'abandoned': 'closed'  # Azure DevOps uses 'completed' for closed PRs
        }
        platform_state = state_mapping.get(state, state)

        prs = platform.list_pull_requests(
            state=platform_state,
            author=author,
            limit=limit
        )

        if not prs:
            print(f"No {state} pull requests found.")
            return

        print(f"{state.title()} Pull Requests ({len(prs)} found):")
        print("-" * 70)

        for pr in prs:
            status_icon = "🟢" if pr.get('status') == 'open' else "🔴"
            print(f"{status_icon} #{pr.get('number', 'N/A'):3} | {pr.get('title', 'N/A')[:50]}")
            print(f"    Author: {pr.get('author', 'N/A')} | Status: {pr.get('status', 'N/A')}")
            if pr.get('url'):
                print(f"    URL: {pr.get('url')}")
            print()

        if getattr(args, 'verbose', False):
            print("Full API Response:")
            print(json.dumps(prs, indent=2, default=str))

    except (PlatformError, AuthenticationError) as e:
        logger.error(f"Failed to list PRs: {e}")
        print(f"[ERROR] {e}")
        sys.exit(1)
    except Exception as e:
        logger.error(f"Unexpected error listing PRs: {e}")
        print(f"[ERROR] An unexpected error occurred: {e}")
        sys.exit(1)


def cmd_pr_approve(args: argparse.Namespace) -> None:
    """
    Approve a pull request.

    Args:
        args: Parsed command line arguments
    """
    try:
        platform = get_pr_platform()

        success = platform.approve_pull_request(args.pr_number)

        if success:
            print(f"[OK] Pull request #{args.pr_number} approved successfully!")
        else:
            print(f"[WARNING] Pull request #{args.pr_number} could not be approved.")
            print("This may be because you are the author, or you don't have permission.")

    except (PlatformError, AuthenticationError) as e:
        logger.error(f"Failed to approve PR: {e}")
        print(f"[ERROR] {e}")
        sys.exit(1)
    except Exception as e:
        logger.error(f"Unexpected error approving PR: {e}")
        print(f"[ERROR] An unexpected error occurred: {e}")
        sys.exit(1)


def cmd_pr_update(args: argparse.Namespace) -> None:
    """
    Update a pull request.

    Args:
        args: Parsed command line arguments
    """
    # Placeholder implementation - PR update not yet implemented
    print("[INFO] PR update functionality is not yet implemented.")
    print("This feature will be added in a future release.")


def cmd_pr_merge(args: argparse.Namespace) -> None:
    """
    Merge a pull request.

    Args:
        args: Parsed command line arguments
    """
    # Placeholder implementation - PR merge not yet implemented
    print("[INFO] PR merge functionality is not yet implemented.")
    print("This feature will be added in a future release.")