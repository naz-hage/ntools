"""
GitHub platform implementation for work item creation.
"""

import subprocess
import json
from typing import Dict, Any, Optional

from .base import WorkItemPlatform
from ..exceptions import PlatformError, ConfigurationError


class GitHubPlatform(WorkItemPlatform):
    """GitHub platform implementation using GitHub CLI."""
    
    def __init__(self, owner: str, repo: str, verbose: bool = False):
        self.owner = owner
        self.repo = repo
        self.verbose = verbose
    
    def auto_detect_repo_info(self) -> Dict[str, str]:
        """Auto-detect repository information from git remote."""
        # Placeholder for git remote detection
        if self.verbose:
            print("Attempting to auto-detect GitHub repository info...")
        return {}
    
    def validate_auth(self) -> bool:
        """Validate GitHub CLI authentication."""
        try:
            result = subprocess.run(["gh", "auth", "status"], 
                                  capture_output=True, text=True, check=False)
            if result.returncode == 0:
                if self.verbose:
                    print("✓ GitHub CLI authenticated")
                return True
            else:
                print("❌ GitHub CLI not authenticated.")
                print("Please run gh auth login to authenticate.")
                return False
        except FileNotFoundError:
            print("❌ GitHub CLI (gh) not found.")
            print("Please install GitHub CLI: https://cli.github.com/")
            return False
    
    def create_work_item(
        self, title: str, description: str, 
        acceptance_criteria: Optional[list] = None,
        metadata: Optional[Dict[str, Any]] = None,
        **kwargs
    ) -> Dict[str, Any]:
        """Create a GitHub issue using GitHub CLI."""
        
        if not self.validate_auth():
            raise PlatformError("GitHub CLI authentication failed")
        
        # Extract repository info from metadata if provided
        repo = metadata.get("repo") or metadata.get("repository") if metadata else None
        owner = metadata.get("owner") if metadata else self.owner
        repo_name = repo or self.repo
        
        # Build issue body
        body_parts = [description]
        
        if acceptance_criteria:
            body_parts.append("## Acceptance Criteria")
            for criterion in acceptance_criteria:
                body_parts.append(f"- [ ] {criterion}")
        
        body = "\n\n".join(body_parts)
        
        try:
            # Create issue using GitHub CLI
            cmd = [
                "gh", "issue", "create",
                "--title", title,
                "--body", body,
                "--repo", f"{owner}/{repo_name}"
            ]
            
            if self.verbose:
                print(f"Creating GitHub issue: {title}")
                print(f"Repository: {owner}/{repo_name}")
            
            result = subprocess.run(cmd, capture_output=True, text=True, check=True)
            
            # Parse the result to get issue URL
            issue_url = result.stdout.strip()
            issue_id = issue_url.split("/")[-1]
            
            if self.verbose:
                print(f"✓ GitHub issue created successfully")
                print(f"  ID: {issue_id}")
                print(f"  URL: {issue_url}")
            
            return {
                "id": issue_id,
                "url": issue_url,
                "status": "success",
                "platform": "github"
            }
            
        except subprocess.CalledProcessError as e:
            error_msg = f"GitHub CLI error: {e.stderr}"
            if self.verbose:
                print(f"❌ {error_msg}")
            raise PlatformError(error_msg)
        except Exception as e:
            error_msg = f"Unexpected error creating GitHub issue: {str(e)}"
            if self.verbose:
                print(f"❌ {error_msg}")
            raise PlatformError(error_msg)