"""
Azure DevOps API client for work item operations.
"""

import subprocess
import re
from typing import Dict, Any, Optional


def extract_platform_info_from_git() -> Optional[Dict[str, str]]:
    """
    Extract platform info from git remote URLs.
    
    Supports GitHub and Azure DevOps repositories.
    
    Returns None if:
    - Not a git repository
    - No supported platform remotes found
    - Git command fails
    
    Returns dict with platform-specific info for supported platforms.
    """
    try:
        # Run git remote -v to get remote information
        result = subprocess.run(
            ["git", "remote", "-v"], 
            capture_output=True, 
            text=True, 
            check=False
        )
        
        if result.returncode != 0:
            # Not a git repository or other git error
            return None
            
        # Parse the output to find supported platform remotes
        lines = result.stdout.strip().split('\n')
        for line in lines:
            if not line.strip():
                continue
                
            # Parse: remote_name\turl (fetch) or remote_name\turl (push)
            parts = line.split('\t')
            if len(parts) >= 2:
                remote_url = parts[1].split()[0]  # Remove (fetch)/(push) suffix
                remote_url = remote_url.strip()
                
                # Check for GitHub URLs
                github_match = re.match(r'https://github\.com/([^/]+)/([^/\s]+)', remote_url)
                if github_match:
                    owner, repo = github_match.groups()
                    return {
                        'platform': 'github',
                        'owner': owner,
                        'repo': repo,
                        'remote_url': remote_url
                    }
                
                # Check for Azure DevOps URLs
                azure_patterns = [
                    r"https://(?:[^@]+@)?dev\.azure\.com/([^/]+)/([^/]+)/_git/([^/\s]+)",
                    r"git@ssh\.dev\.azure\.com:v3/([^/]+)/([^/]+)/([^/\s]+)"
                ]
                
                for pattern in azure_patterns:
                    match = re.match(pattern, remote_url)
                    if match:
                        org, project, repo = match.groups()
                        return {
                            'platform': 'azdo',
                            'organization': org,
                            'project': project, 
                            'repository': repo,
                            'remote_url': remote_url
                        }
        
        # No supported platform remotes found
        return None
        
    except (subprocess.SubprocessError, OSError):
        # git command not found or other system error
        return None


class AzureDevOpsClient:
    """Azure DevOps REST API client."""
    
    def __init__(self, organization: str, project: str, pat: str, verbose: bool = False):
        self.organization = organization
        self.project = project
        self.pat = pat
        self.verbose = verbose
        self.base_url = f"https://dev.azure.com/{organization}/{project}/_apis"
    
    def create_work_item(self, **kwargs) -> Optional[Dict[str, Any]]:
        """Create a work item - placeholder implementation."""
        if self.verbose:
            print("Azure DevOps API client - create_work_item called")
        return None

