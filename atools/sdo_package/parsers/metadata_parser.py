"""
Metadata Parser for SDO platform detection and validation.
"""

import re
from typing import Dict, Any, List, Optional


class MetadataParser:
    """Parser for work item metadata and platform detection."""

    @staticmethod
    def detect_platform(metadata: Dict[str, Any]) -> str:
        """
        Detect target platform from metadata.

        Args:
            metadata: Dictionary of metadata fields

        Returns:
            Platform name ('azdo' or 'github')
        """
        # Check explicit platform specification (both 'platform' and 'target' keys)
        platform = metadata.get("platform", metadata.get("target", "")).lower()
        if platform in ["azdo", "azure", "azure-devops", "azure_devops"]:
            return "azdo"
        elif platform in ["github", "gh"]:
            return "github"

        # Check for platform-specific indicators
        repo = metadata.get("repo") or metadata.get("repository")
        if repo and "/" in str(repo):
            return "github"  # Format like "owner/repo"

        if metadata.get("organization") or metadata.get("project"):
            return "azdo"

        # Default fallback
        return "azdo"

    @staticmethod
    def normalize_work_item_type(work_item_type: Optional[str], platform: str) -> str:
        """
        Normalize work item type for the target platform.

        Args:
            work_item_type: Raw work item type from metadata
            platform: Target platform ('azdo' or 'github')

        Returns:
            Normalized work item type
        """
        if not work_item_type:
            return "Task" if platform == "azdo" else "issue"

        wit = work_item_type.lower().strip()

        if platform == "azdo":
            # Azure DevOps work item types
            type_mapping = {
                "pbi": "Product Backlog Item",
                "product backlog item": "Product Backlog Item",
                "backlog item": "Product Backlog Item",
                "user story": "User Story",
                "story": "User Story",
                "task": "Task",
                "bug": "Bug",
                "issue": "Issue",
                "epic": "Epic",
                "feature": "Feature",
            }
            return type_mapping.get(wit, "Task")

        else:  # GitHub
            # GitHub only has issues, but we can use labels
            return "issue"

    @staticmethod
    def parse_labels(labels_str: Optional[str]) -> List[str]:
        """
        Parse labels string into list.

        Args:
            labels_str: Comma-separated labels string

        Returns:
            List of label strings
        """
        if not labels_str:
            return []

        return [label.strip() for label in labels_str.split(",") if label.strip()]

    @staticmethod
    def parse_parent_id(parent_str: Optional[str]) -> Optional[int]:
        """
        Parse parent ID from string.

        Args:
            parent_str: Parent ID string

        Returns:
            Parent ID as integer or None
        """
        if not parent_str:
            return None

        # Handle formats like "#123", "123", "PBI-123"
        match = re.search(r"\d+", str(parent_str))
        if match:
            try:
                return int(match.group())
            except ValueError:
                # Invalid number format, return None
                pass

        return None

    @staticmethod
    def validate_metadata(metadata: Dict[str, Any], platform: str) -> List[str]:
        """
        Validate metadata for platform-specific requirements.

        Args:
            metadata: Metadata dictionary
            platform: Target platform

        Returns:
            List of validation error messages
        """
        errors = []

        if platform == "github":
            if not metadata.get("repo") and not metadata.get("repository"):
                errors.append("GitHub platform requires 'repo' or 'repository' field")

        elif platform == "azdo":
            # Azure DevOps validation can be more flexible as it can detect from git remote
            pass

        return errors
