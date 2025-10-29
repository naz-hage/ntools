"""
SDO Work Items - Business logic for work item operations.
"""

from typing import Optional, Dict, Any
from .parsers.markdown_parser import MarkdownParser
from .parsers.metadata_parser import MetadataParser
from .platforms.azdo_platform import AzureDevOpsPlatform
from .platforms.github_platform import GitHubPlatform
from .exceptions import ValidationError, ConfigurationError


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