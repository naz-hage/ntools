"""
Azure DevOps platform implementation for work item management.
"""

import os
from typing import Dict, Any, Optional, List

try:
    from .base import WorkItemPlatform
    from ..client import AzureDevOpsClient, extract_azure_devops_info_from_git
    from ..exceptions import ConfigurationError, ValidationError
    from ..parsers.metadata_parser import MetadataParser
except ImportError:
    from base import WorkItemPlatform
    from client import AzureDevOpsClient, extract_azure_devops_info_from_git
    from exceptions import ConfigurationError, ValidationError
    from parsers.metadata_parser import MetadataParser


class AzureDevOpsPlatform(WorkItemPlatform):
    """Azure DevOps implementation of work item platform."""
    
    def get_config(self) -> Dict[str, str]:
        """Get Azure DevOps configuration by extracting from Git remote."""
        git_info = extract_azure_devops_info_from_git()
        if git_info:
            if self.verbose:
                print("✓ Extracted Azure DevOps information from Git remote:")
                print(f"  Organization: {git_info['organization']}")
                print(f"  Project: {git_info['project']}")
                print(f"  Repository: {git_info['repository']}")
            return git_info
        else:
            raise ConfigurationError(
                "Could not extract Azure DevOps info from Git remote",
                "Please ensure you are in an Azure DevOps Git repository with properly configured remotes."
            )
    
    def validate_auth(self) -> bool:
        """Validate Azure DevOps authentication."""
        pat = os.environ.get("AZURE_DEVOPS_PAT") or os.environ.get("AZURE_DEVOPS_EXT_PAT")
        if not pat:
            print("❌ AZURE_DEVOPS_PAT environment variable not set.")
            print("Please set your Azure DevOps Personal Access Token.")
            return False
        return True
    
    def create_work_item(
        self,
        title: str,
        description: str,
        metadata: Dict[str, Any],
        acceptance_criteria: Optional[List[str]] = None,
        dry_run: bool = False
    ) -> Optional[Dict[str, Any]]:
        """Create an Azure DevOps work item."""
        if dry_run:
            print('[dry-run] Would create Azure DevOps work item with:')
            print(f'  Title: {title}')
            print(f'  Project: {metadata.get("project", "Not specified")}')
            print(f'  Work Item Type: {metadata.get("work_item_type", "PBI")}')
            if metadata.get('area'):
                print(f'  Area: {metadata.get("area")}')
            if metadata.get('iteration'):
                print(f'  Iteration: {metadata.get("iteration")}')
            if metadata.get('assignee'):
                print(f'  Assignee: {metadata.get("assignee")}')
            print('  Description:')
            print(description)
            if acceptance_criteria:
                print('  Acceptance Criteria:')
                for ac in acceptance_criteria:
                    # Keep the checkboxes in the format they appear in the markdown
                    if ac.strip().startswith('[ ]') or ac.strip().startswith('[x]'):
                        print(f'  - {ac.strip()}')
                    else:
                        print(f'  - [ ] {ac.strip()}')
            return {"dry_run": True, "title": title, "project": metadata.get("project")}
        
        print("Azure DevOps work item creation not yet implemented")
        return None

