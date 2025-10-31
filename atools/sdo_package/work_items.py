"""
SDO Work Items - Business logic for work item operations.
"""

import os
from typing import Optional, Dict, Any
from .exceptions import ValidationError, ConfigurationError, PlatformError, ParsingError


class WorkItemResult:
    """Result of work item creation operation."""

    def __init__(self, success: bool, work_item_id: str = None, url: str = None,
                 platform: str = None, error_message: str = None):
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
                "acceptance_criteria": parsed_result["acceptance_criteria"]
            }
            metadata = parsed_result["metadata"]

            # Determine platform and create work item
            detected_platform = MetadataParser.detect_platform(metadata)

            # Map detected platform names to work_items expected names
            if detected_platform == 'azdo':
                platform = 'azure_devops'
            elif detected_platform == 'github':
                platform = 'github'
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

    def _create_azdo_work_item(self, content: Dict[str, Any],
                              metadata: Dict[str, Any]) -> WorkItemResult:
        """Create Azure DevOps work item."""
        try:
            # Lazy import (available in Phase 4)
            from .platforms.azdo_platform import AzureDevOpsPlatform

            # Extract required parameters
            organization = metadata.get("organization", "")
            project = metadata.get("project", "")
            pat = metadata.get("pat", "")

            if not all([organization, project, pat]):
                return WorkItemResult(False,
                                    error_message="Missing Azure DevOps configuration "
                                                 "(organization, project, pat)")

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
                work_item_type=work_item_type,
                metadata=metadata
            )

            if result and result.get("id"):
                return WorkItemResult(
                    success=True,
                    work_item_id=str(result["id"]),
                    url=result.get("url", ""),
                    platform="azure_devops"
                )
            else:
                return WorkItemResult(False, error_message="Azure DevOps work item creation failed")

        except Exception as e:
            return WorkItemResult(False, error_message=f"Azure DevOps error: {str(e)}")

    def _create_github_work_item(self, content: Dict[str, Any],
                                metadata: Dict[str, Any]) -> WorkItemResult:
        """Create GitHub work item."""
        try:
            # Lazy import (available in Phase 4)
            from .platforms.github_platform import GitHubPlatform

            # Extract required parameters
            owner = metadata.get("owner", "")
            repo = metadata.get("repo", metadata.get("repository", ""))

            if not all([owner, repo]):
                return WorkItemResult(False,
                                    error_message="Missing GitHub configuration (owner, repo)")

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
                metadata=metadata
            )

            if result and result.get("id"):
                return WorkItemResult(
                    success=True,
                    work_item_id=str(result["id"]),
                    url=result.get("url", ""),
                    platform="github"
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
        platform_name = MetadataParser.detect_platform(content['metadata'])
        print(f"✓ Using platform: {platform_name}")

        # Create platform instance
        if platform_name == 'azdo':
            platform = AzureDevOpsPlatform(verbose=args.verbose)
        elif platform_name == 'github':
            platform = GitHubPlatform(verbose=args.verbose)
        else:
            raise ConfigurationError(f"Unsupported platform: {platform_name}")

        # Validate authentication
        if not platform.validate_auth():
            return None

        # Create the work item
        result = platform.create_work_item(
            title=content['title'],
            description=content['description'],
            metadata=content['metadata'],
            acceptance_criteria=content['acceptance_criteria'],
            dry_run=args.dry_run
        )

        return result

    except FileNotFoundError:
        raise ValidationError(f"File not found: {args.file_path}")
    except Exception as e:
        if isinstance(e, (ValidationError, ConfigurationError)):
            raise
        else:
            raise ValidationError(f"Failed to create work item: {str(e)}")


def main():
    """For testing purposes."""
    print("SDO Work Items module loaded successfully")


if __name__ == '__main__':
    main()
