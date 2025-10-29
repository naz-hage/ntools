"""
SDO Work Items - Business logic for work item operations.
"""

from typing import Optional, Dict, Any, List
from .parsers.markdown_parser import MarkdownParser
from .parsers.metadata_parser import MetadataParser
from .platforms.azdo_platform import AzureDevOpsPlatform
from .platforms.github_platform import GitHubPlatform
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
            # Parse the markdown file
            parser = MarkdownParser()
            content = parser.parse_file(file_path)
            
            # Parse metadata
            metadata_parser = MetadataParser()
            metadata = metadata_parser.parse(content)
            
            # Determine platform and create work item
            platform = metadata.get("platform", "").lower()
            
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
    
    def _create_azdo_work_item(self, content: Dict[str, Any], metadata: Dict[str, Any]) -> WorkItemResult:
        """Create Azure DevOps work item."""
        # Implementation placeholder
        return WorkItemResult(False, error_message="Azure DevOps creation not implemented")
    
    def _create_github_work_item(self, content: Dict[str, Any], metadata: Dict[str, Any]) -> WorkItemResult:
        """Create GitHub work item."""
        # Implementation placeholder
        return WorkItemResult(False, error_message="GitHub creation not implemented")


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
    import sys
    print("SDO Work Items module loaded successfully")
    

if __name__ == '__main__':
    main()