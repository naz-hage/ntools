"""
GitHub Pull Request platform implementation.

This module provides GitHub-specific pull request operations using
the GitHub CLI (gh) tool for reliable and authenticated API access.
"""

import json
import logging
import re
import subprocess
import sys
from typing import Any, Dict, List, Optional

from ..exceptions import AuthenticationError, PlatformError, ValidationError
from .pr_base import PRPlatform

logger = logging.getLogger(__name__)


class GitHubPullRequestPlatform(PRPlatform):
    """
    GitHub implementation of the PR platform interface.

    Uses GitHub CLI (gh) for all operations to ensure proper authentication
    and reliable API access without dealing with token management directly.
    """

    def __init__(self):
        """Initialize GitHub PR platform."""
        self._validate_gh_cli()

    def _validate_gh_cli(self) -> None:
        """Validate that GitHub CLI is installed and authenticated."""
        try:
            result = subprocess.run(
                ["gh", "--version"],
                capture_output=True,
                text=True,
                encoding="utf-8",
                errors="replace",
                check=True,
            )
            logger.debug(f"GitHub CLI version: {result.stdout.strip()}")
        except (subprocess.CalledProcessError, FileNotFoundError):
            raise PlatformError(
                "GitHub CLI (gh) is not installed or not in PATH. "
                "Install from: https://cli.github.com/"
            )

    def validate_auth(self) -> bool:
        """Validate GitHub CLI authentication."""
        try:
            result = subprocess.run(
                ["gh", "auth", "status"],
                capture_output=True,
                text=True,
                encoding="utf-8",
                errors="replace",
                check=True,
            )
            return "Logged in to github.com" in result.stdout
        except subprocess.CalledProcessError:
            raise AuthenticationError("Not authenticated with GitHub CLI. Run 'gh auth login'")

    def _run_gh_command(self, args: List[str], check: bool = True) -> subprocess.CompletedProcess:
        """
        Run a GitHub CLI command with proper error handling.

        Args:
            args: Command arguments
            check: Whether to raise exception on non-zero exit

        Returns:
            CompletedProcess instance
        """
        try:
            cmd = ["gh"] + args
            logger.debug(f"Running command: {' '.join(cmd)}")

            result = subprocess.run(
                cmd, capture_output=True, text=True, encoding="utf-8", errors="replace", check=check
            )

            if result.stderr and logger.isEnabledFor(logging.DEBUG):
                logger.debug(f"Command stderr: {result.stderr}")

            return result

        except subprocess.CalledProcessError as e:
            logger.error(f"GitHub CLI command failed: {e}")
            logger.error(f"Command: {' '.join(['gh'] + args)}")
            if e.stdout:
                logger.error(f"Stdout: {e.stdout}")
            if e.stderr:
                logger.error(f"Stderr: {e.stderr}")
            raise PlatformError(f"GitHub CLI command failed: {e.stderr.strip()}")

    def create_pull_request(
        self,
        title: str,
        description: str,
        source_branch: Optional[str] = None,
        target_branch: Optional[str] = None,
        work_item_id: Optional[int] = None,
        draft: bool = False,
    ) -> str:
        """Create a pull request on GitHub."""
        # Build command arguments
        args = ["pr", "create"]

        if title:
            args.extend(["--title", title])

        if description:
            args.extend(["--body", description])

        if source_branch:
            args.extend(["--head", source_branch])

        if target_branch:
            args.extend(["--base", target_branch])

        if draft:
            args.append("--draft")

        # GitHub doesn't support work items directly, but we can add to body
        if work_item_id:
            description = f"{description}\n\nRelated Work Item: #{work_item_id}"
            args[args.index("--body") + 1] = description

        # Run the command
        result = self._run_gh_command(args)

        # Extract PR URL from output
        pr_url = result.stdout.strip()
        if not pr_url or not pr_url.startswith("https://"):
            raise PlatformError("Failed to extract PR URL from GitHub CLI output")

        return pr_url

    def get_pull_request(self, pr_number: int) -> Dict[str, Any]:
        """Get details of a GitHub pull request."""
        args = [
            "pr",
            "view",
            str(pr_number),
            "--json",
            "number,title,body,state,author,headRefName,baseRefName,url,createdAt,updatedAt",
        ]

        result = self._run_gh_command(args)
        data = json.loads(result.stdout)

        # Parse issue references from PR body and title
        work_items = []
        body = data.get("body", "")
        title = data.get("title", "")
        text_to_search = f"{title}\n{body}"

        if text_to_search:
            # Remove code blocks (```...``` and `...`) to avoid matching demo/example code
            text_no_code = re.sub(r"```.*?```", "", text_to_search, flags=re.DOTALL)
            text_no_code = re.sub(r"`[^`]*`", "", text_no_code)

            # Look for issue references in the cleaned text
            # Using specific GitHub linking keywords instead of generic #123 pattern
            # to avoid duplicate matching
            issue_patterns = [
                r"closes?\s+#(\d+)",  # closes #123
                r"fixes?\s+#(\d+)",  # fixes #123
                r"resolves?\s+#(\d+)",  # resolves #123
                r"related\s+(?:to\s+)?#(\d+)",  # related to #123
                r"(?:^|\s)#(\d+)",  # #123 at start or after whitespace (standalone references)
            ]

            for pattern in issue_patterns:
                matches = re.findall(pattern, text_no_code, re.IGNORECASE)
                for match in matches:
                    issue_num = int(match)
                    if issue_num not in work_items:
                        work_items.append(issue_num)

        return {
            "number": data.get("number"),
            "title": data.get("title"),
            "description": data.get("body"),
            "status": data.get("state", "").lower(),
            "author": data.get("author", {}).get("login"),
            "source_branch": data.get("headRefName"),
            "target_branch": data.get("baseRefName"),
            "url": data.get("url"),
            "created_at": data.get("createdAt"),
            "updated_at": data.get("updatedAt"),
            "work_items": work_items,  # Issue references for GitHub
        }

    def list_pull_requests(
        self, state: str = "open", author: Optional[str] = None, limit: int = 10
    ) -> List[Dict[str, Any]]:
        """List GitHub pull requests."""
        args = [
            "pr",
            "list",
            "--json",
            "number,title,body,state,author,headRefName,baseRefName,url,createdAt,updatedAt",
        ]

        if state != "all":
            args.extend(["--state", state])

        if author:
            args.extend(["--author", author])

        args.extend(["--limit", str(limit)])

        result = self._run_gh_command(args)
        prs_data = json.loads(result.stdout)

        return [
            {
                "number": pr.get("number"),
                "title": pr.get("title"),
                "description": pr.get("body"),
                "status": pr.get("state", "").lower(),
                "author": pr.get("author", {}).get("login"),
                "source_branch": pr.get("headRefName"),
                "target_branch": pr.get("baseRefName"),
                "url": pr.get("url"),
                "created_at": pr.get("createdAt"),
                "updated_at": pr.get("updatedAt"),
            }
            for pr in prs_data
        ]

    def approve_pull_request(self, pr_number: int) -> bool:
        """Approve a GitHub pull request."""
        try:
            args = ["pr", "review", str(pr_number), "--approve"]
            self._run_gh_command(args)
            return True
        except PlatformError:
            return False

    def update_pull_request(
        self,
        pr_number: int,
        title: Optional[str] = None,
        description: Optional[str] = None,
        status: Optional[str] = None,
    ) -> bool:
        """Update an existing pull request on GitHub."""
        try:
            args = ["pr", "edit", str(pr_number)]

            if title is not None:
                args.extend(["--title", title])

            if description is not None:
                args.extend(["--body", description])

            if status is not None:
                # Map status to GitHub state values
                state_mapping = {"active": "open", "abandoned": "closed", "completed": "closed"}
                if status not in state_mapping:
                    raise ValidationError(
                        f"Invalid status: {status}. Must be one of: active, abandoned, completed"
                    )
                args.extend(["--state", state_mapping[status]])

            if len(args) == 2:  # Only ["pr", "edit", str(pr_number)]
                raise ValidationError(
                    "At least one field (title, description, or status) must be provided for update"
                )

            self._run_gh_command(args)
            return True

        except subprocess.CalledProcessError as e:
            raise PlatformError(
                f"Failed to update pull request: {e.stderr.decode() if e.stderr else str(e)}"
            )
