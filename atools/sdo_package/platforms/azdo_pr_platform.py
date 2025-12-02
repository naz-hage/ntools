"""
Azure DevOps Pull Request platform implementation.

This module provides Azure DevOps-specific pull request operations using
the Azure DevOps REST API with proper authentication and error handling.
"""

import logging
from typing import Any, Dict, List, Optional

import requests

from ..client import AzureDevOpsClient, extract_platform_info_from_git, get_personal_access_token
from ..exceptions import AuthenticationError, PlatformError, ValidationError
from .pr_base import PRPlatform

logger = logging.getLogger(__name__)


class AzureDevOpsPullRequestPlatform(PRPlatform):
    """
    Azure DevOps implementation of the PR platform interface.

    Uses Azure DevOps REST API through the existing AzureDevOpsClient
    for consistent authentication and API access patterns.
    """

    def __init__(self):
        """Initialize Azure DevOps PR platform."""
        self.client = None
        self._project = None
        self._repository = None
        self._organization = None

    def validate_auth(self) -> bool:
        """Validate Azure DevOps authentication."""
        try:
            # Ensure client is initialized before validation
            self._ensure_client()

            # Try to get user profile to validate auth
            self.client.get_user_profile()
            return True
        except Exception as e:
            logger.error(f"Azure DevOps authentication validation failed: {e}")
            raise AuthenticationError(f"Azure DevOps authentication failed: {e}")

    def _ensure_client(self) -> None:
        """Ensure Azure DevOps client is initialized."""
        if not self.client:
            # Extract platform information from Git remote
            platform_info = extract_platform_info_from_git()
            if not platform_info or platform_info.get("platform") != "azdo":
                raise PlatformError("Not in an Azure DevOps repository")

            organization = platform_info.get("organization")
            project = platform_info.get("project")
            repository = platform_info.get("repository")
            pat = get_personal_access_token()

            if not organization or not project or not pat:
                raise AuthenticationError(
                    "Missing Azure DevOps configuration (organization, project, or PAT)"
                )

            self.client = AzureDevOpsClient(organization, project, pat)
            self._organization = organization
            self._project = project
            self._repository = repository

    def _get_repository_name(self) -> str:
        """Get the repository name from Git remote."""
        try:
            import subprocess

            result = subprocess.run(
                ["git", "remote", "get-url", "origin"], capture_output=True, text=True, check=True
            )

            url = result.stdout.strip()
            # Extract repo name from Azure DevOps URL
            # Format: https://dev.azure.com/{org}/{project}/_git/{repo}
            if "_git/" in url:
                repo_part = url.split("_git/")[-1]
                return repo_part.rstrip(".git")
            else:
                # Fallback: try to get from git config
                result = subprocess.run(
                    ["git", "config", "--get", "remote.origin.url"],
                    capture_output=True,
                    text=True,
                    check=True,
                )
                url = result.stdout.strip()
                if "_git/" in url:
                    repo_part = url.split("_git/")[-1]
                    return repo_part.rstrip(".git")

            raise PlatformError("Could not determine repository name from Git remote")

        except subprocess.CalledProcessError as e:
            raise PlatformError(f"Failed to get repository name from Git: {e}")

    def create_pull_request(
        self,
        title: str,
        description: str,
        source_branch: Optional[str] = None,
        target_branch: Optional[str] = None,
        work_item_id: Optional[int] = None,
        draft: bool = False,
    ) -> str:
        """Create a pull request in Azure DevOps."""
        self._ensure_client()

        # Get current branch if not specified
        if not source_branch:
            try:
                import subprocess

                result = subprocess.run(
                    ["git", "branch", "--show-current"], capture_output=True, text=True, check=True
                )
                source_branch = result.stdout.strip()
            except subprocess.CalledProcessError:
                raise PlatformError(
                    "Could not determine current branch and no source branch specified"
                )

        # Default target branch
        if not target_branch:
            target_branch = "main"

        # Prepare PR data
        pr_data = {
            "sourceRefName": f"refs/heads/{source_branch}",
            "targetRefName": f"refs/heads/{target_branch}",
            "title": title,
            "description": description,
            "isDraft": draft,
        }

        # Add work item if specified
        if work_item_id:
            pr_data["workItemRefs"] = [{"id": str(work_item_id)}]

        try:
            url = f"{self.client.base_url}/_apis/git/repositories/{self._repository}/pullRequests"
            params = {"api-version": self.client.api_version}
            response = self.client.session.post(
                url, params=params, json=pr_data, headers={"Content-Type": "application/json"}
            )

            if response.status_code not in (200, 201):
                if self.client.verbose:
                    logger.error(f"Failed to create PR: {response.status_code} - {response.text}")
                raise PlatformError(f"Failed to create pull request: {response.text}")

            pr_result = response.json()
            pr_id = pr_result.get("pullRequestId")

            # Construct PR URL
            pr_url = f"{self.client.base_url}/_git/{self._repository}/pullrequest/{pr_id}"
            return pr_url

        except requests.RequestException as e:
            logger.error(f"Network error creating PR: {e}")
            raise PlatformError(f"Network error creating pull request: {e}")

    def update_pull_request(
        self,
        pr_number: int,
        title: Optional[str] = None,
        description: Optional[str] = None,
        status: Optional[str] = None,
    ) -> bool:
        """Update an existing pull request in Azure DevOps."""
        self._ensure_client()

        # Prepare update data
        update_data = {}

        if title is not None:
            update_data["title"] = title

        if description is not None:
            update_data["description"] = description

        if status is not None:
            # Map status to Azure DevOps status values
            status_mapping = {
                "active": "active",
                "abandoned": "abandoned",
                "completed": "completed",
            }
            if status not in status_mapping:
                raise ValidationError(
                    f"Invalid status: {status}. Must be one of: active, abandoned, completed"
                )
            update_data["status"] = status_mapping[status]

        if not update_data:
            raise ValidationError(
                "At least one field (title, description, or status) must be provided for update"
            )

        try:
            url = f"{self.client.base_url}/_apis/git/repositories/{self._repository}/pullRequests/{pr_number}"
            params = {"api-version": self.client.api_version}
            response = self.client.session.patch(
                url, params=params, json=update_data, headers={"Content-Type": "application/json"}
            )

            if response.status_code not in (200, 201):
                if self.client.verbose:
                    logger.error(f"Failed to update PR: {response.status_code} - {response.text}")
                raise PlatformError(f"Failed to update pull request: {response.text}")

            return True

        except requests.RequestException as e:
            if self.client.verbose:
                logger.error(f"Network error updating PR: {e}")
            raise PlatformError(f"Network error updating pull request: {e}")

    def get_pull_request(self, pr_number: int) -> Dict[str, Any]:
        """Get details of an Azure DevOps pull request."""
        self._ensure_client()

        try:
            url = f"{self.client.base_url}/_apis/git/repositories/{self._repository}/pullRequests/{pr_number}"
            params = {"api-version": self.client.api_version, "$expand": "workItems"}

            response = self.client.session.get(url, params=params)

            if response.status_code == 404:
                raise PlatformError(f"Pull request #{pr_number} not found")
            elif response.status_code != 200:
                logger.error(f"Failed to get PR: {response.status_code} - {response.text}")
                raise PlatformError(f"Failed to get pull request: {response.text}")

            pr_data = response.json()

            # Get work items linked to this PR using separate endpoint
            work_items = []
            try:
                workitems_url = f"{self.client.base_url}/_apis/git/repositories/{self._repository}/pullRequests/{pr_number}/workitems"
                workitems_params = {"api-version": self.client.api_version}
                workitems_response = self.client.session.get(workitems_url, params=workitems_params)

                if workitems_response.status_code == 200:
                    workitems_data = workitems_response.json()
                    if workitems_data.get("value"):
                        for wi in workitems_data["value"]:
                            if wi.get("id"):
                                work_items.append(wi["id"])
                else:
                    logger.warning(
                        f"Failed to get work items for PR {pr_number}: {workitems_response.status_code} - {workitems_response.text}"
                    )
            except Exception as e:
                logger.warning(f"Failed to get work items for PR {pr_number}: {e}")

            return {
                "number": pr_data.get("pullRequestId"),
                "title": pr_data.get("title"),
                "description": pr_data.get("description"),
                "status": self._map_pr_status(pr_data.get("status")),
                "author": pr_data.get("createdBy", {}).get("displayName"),
                "source_branch": pr_data.get("sourceRefName", "").replace("refs/heads/", ""),
                "target_branch": pr_data.get("targetRefName", "").replace("refs/heads/", ""),
                "url": f"{self.client.base_url}/_git/{self._repository}/pullrequest/{pr_number}",
                "created_at": pr_data.get("creationDate"),
                "updated_at": pr_data.get(
                    "creationDate"
                ),  # Azure DevOps doesn't have updatedAt in basic response
                "work_items": work_items,
            }

        except requests.RequestException as e:
            logger.error(f"Network error getting PR: {e}")
            raise PlatformError(f"Network error getting pull request: {e}")

    def list_pull_requests(
        self, state: str = "open", author: Optional[str] = None, limit: int = 10
    ) -> List[Dict[str, Any]]:
        """List Azure DevOps pull requests."""
        self._ensure_client()

        try:
            url = f"{self.client.base_url}/_apis/git/repositories/{self._repository}/pullRequests"
            params = {"api-version": self.client.api_version, "$top": limit}

            # Map state parameter
            if state == "open":
                params["status"] = "active"
            elif state == "closed":
                params["status"] = "completed"
            # For "all", don't filter by status

            if author:
                params["creatorId"] = author  # This might need adjustment based on Azure DevOps API

            response = self.client.session.get(url, params=params)

            if response.status_code != 200:
                logger.error(f"Failed to list PRs: {response.status_code} - {response.text}")
                raise PlatformError(f"Failed to list pull requests: {response.text}")

            prs_data = response.json().get("value", [])

            return [
                {
                    "number": pr.get("pullRequestId"),
                    "title": pr.get("title"),
                    "description": pr.get("description"),
                    "status": self._map_pr_status(pr.get("status")),
                    "author": pr.get("createdBy", {}).get("displayName"),
                    "source_branch": pr.get("sourceRefName", "").replace("refs/heads/", ""),
                    "target_branch": pr.get("targetRefName", "").replace("refs/heads/", ""),
                    "url": f"{self.client.base_url}/_git/{self._repository}/pullrequest/{pr.get('pullRequestId')}",
                    "created_at": pr.get("creationDate"),
                    "updated_at": pr.get("creationDate"),
                }
                for pr in prs_data
            ]

        except requests.RequestException as e:
            logger.error(f"Network error listing PRs: {e}")
            raise PlatformError(f"Network error listing pull requests: {e}")

    def approve_pull_request(self, pr_number: int) -> bool:
        """Approve an Azure DevOps pull request."""
        self._ensure_client()

        try:
            # Use "me" as reviewer ID (Azure DevOps resolves this to current user)
            reviewer_id = "me"
            url = f"{self.client.base_url}/_apis/git/repositories/{self._repository}/pullRequests/{pr_number}/reviewers/{reviewer_id}"
            params = {"api-version": self.client.api_version}
            vote_data = {"vote": 10}  # 10 = approved

            response = self.client.session.put(
                url, params=params, json=vote_data, headers={"Content-Type": "application/json"}
            )

            if response.status_code in (200, 201):
                return True
            else:
                logger.warning(f"Failed to approve PR: {response.status_code} - {response.text}")
                return False

        except Exception as e:
            logger.error(f"Error approving PR: {e}")
            return False

    def _map_pr_status(self, azdo_status: str) -> str:
        """Map Azure DevOps PR status to standard status."""
        status_map = {"active": "open", "completed": "closed", "abandoned": "closed"}
        return status_map.get(azdo_status.lower(), "unknown")
