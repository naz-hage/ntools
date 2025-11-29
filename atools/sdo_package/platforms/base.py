"""
Abstract base class for work item platform implementations.
"""

from abc import ABC, abstractmethod
from typing import Dict, Any, Optional, List


class WorkItemPlatform(ABC):
    """Abstract base class for work item platform implementations."""

    def __init__(self, verbose: bool = False):
        """
        Initialize platform with verbose flag.

        Args:
            verbose: Whether to show verbose output
        """
        self.verbose = verbose

    @abstractmethod
    def get_config(self) -> Dict[str, str]:
        """Get platform-specific configuration."""
        pass

    @abstractmethod
    def validate_auth(self) -> bool:
        """Validate authentication for the platform."""
        pass

    @abstractmethod
    def create_work_item(
        self,
        title: str,
        description: str,
        metadata: Dict[str, Any],
        acceptance_criteria: Optional[List[str]] = None,
        dry_run: bool = False,
    ) -> Optional[Dict[str, Any]]:
        """Create a work item on the platform."""
        pass
