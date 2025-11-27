"""
Base platform class for Pull Request operations.

This module defines the abstract base class that all PR platform
implementations must inherit from, ensuring consistent interfaces
across different Git hosting platforms.
"""

from abc import ABC, abstractmethod
from typing import Any, Dict, List, Optional


class PRPlatform(ABC):
    """
    Abstract base class for pull request platform implementations.

    This class defines the interface that all platform-specific PR
    implementations must follow, ensuring consistent behavior across
    GitHub, Azure DevOps, and other Git hosting platforms.
    """

    @abstractmethod
    def create_pull_request(
        self,
        title: str,
        description: str,
        source_branch: Optional[str] = None,
        target_branch: Optional[str] = None,
        work_item_id: Optional[int] = None,
        draft: bool = False
    ) -> str:
        """
        Create a new pull request.

        Args:
            title: PR title
            description: PR description/body
            source_branch: Source branch (optional, auto-detect if not provided)
            target_branch: Target branch (optional, defaults to main/master)
            work_item_id: Associated work item ID (optional)
            draft: Whether to create as draft (optional)

        Returns:
            str: URL of the created pull request

        Raises:
            AuthenticationError: If authentication fails
            PlatformError: If platform operation fails
        """
        pass

    @abstractmethod
    def get_pull_request(self, pr_number: int) -> Dict[str, Any]:
        """
        Get details of a specific pull request.

        Args:
            pr_number: Pull request number/ID

        Returns:
            Dict containing PR details with keys:
            - number: PR number
            - title: PR title
            - description: PR description
            - status: PR status (open/closed/merged)
            - author: PR author
            - source_branch: Source branch
            - target_branch: Target branch
            - url: PR URL
            - created_at: Creation timestamp
            - updated_at: Last update timestamp

        Raises:
            AuthenticationError: If authentication fails
            PlatformError: If platform operation fails
        """
        pass

    @abstractmethod
    def list_pull_requests(
        self,
        state: str = "open",
        author: Optional[str] = None,
        limit: int = 10
    ) -> List[Dict[str, Any]]:
        """
        List pull requests with optional filtering.

        Args:
            state: PR state filter ("open", "closed", "all")
            author: Filter by author (optional)
            limit: Maximum number of PRs to return

        Returns:
            List of PR dictionaries with same structure as get_pull_request

        Raises:
            AuthenticationError: If authentication fails
            PlatformError: If platform operation fails
        """
        pass

    @abstractmethod
    def approve_pull_request(self, pr_number: int) -> bool:
        """
        Approve a pull request.

        Args:
            pr_number: Pull request number/ID

        Returns:
            bool: True if approval was successful, False otherwise

        Raises:
            AuthenticationError: If authentication fails
            PlatformError: If platform operation fails
        """
        pass

    @abstractmethod
    def update_pull_request(
        self,
        pr_number: int,
        title: Optional[str] = None,
        description: Optional[str] = None,
        status: Optional[str] = None
    ) -> bool:
        """
        Update an existing pull request.

        Args:
            pr_number: Pull request number/ID to update
            title: New title (optional)
            description: New description (optional)
            status: New status - 'active', 'abandoned', 'completed' (optional)

        Returns:
            bool: True if update was successful

        Raises:
            AuthenticationError: If authentication fails
            PlatformError: If platform operation fails
            ValidationError: If parameters are invalid
        """
        pass

    @abstractmethod
    def validate_auth(self) -> bool:
        """
        Validate that the platform is properly authenticated.

        Returns:
            bool: True if authentication is valid

        Raises:
            AuthenticationError: If authentication is invalid
        """
        pass