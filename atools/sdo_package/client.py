"""
Client Module - Platform-Agnostic Operations

ARCHITECTURE OVERVIEW:
This module provides platform-agnostic operations with platform-specific implementations.

AZURE DEVOPS SUPPORT:
- AzureDevOpsClient: Direct REST API client for Azure DevOps services
- Uses Personal Access Tokens (PAT) for authentication
- Makes HTTP requests to Azure DevOps REST APIs
- Handles Azure DevOps-specific data formats and error responses

GITHUB SUPPORT:
- GitHub operations handled through GitHubRepositoryPlatform in repositories.py
- Uses GitHub CLI (gh) for authentication and operations
- Executes CLI commands instead of direct API calls
- Transforms GitHub CLI JSON output to match common interfaces

COMMON CODE:
- extract_platform_info_from_git(): Platform detection from Git remotes
- Configuration management and validation
- Error handling and logging patterns

PLATFORM DIVERGENCE:
- Authentication: PAT (Azure DevOps) vs GitHub CLI tokens (GitHub)
- API Access: Direct HTTP (Azure DevOps) vs CLI commands (GitHub)
- Data Formats: Azure DevOps JSON vs GitHub CLI JSON
"""

import base64
import json
import logging
import os
import requests
import time
from pathlib import Path
from typing import Dict, Any, Optional, List
import yaml

# Handle imports for both module and script execution
try:
    from .exceptions import (
        AzureDevOpsAPIError,
        AuthenticationError,
        NetworkError,
        ConfigurationError,
        ValidationError,
    )
except ImportError:
    from exceptions import (
        AzureDevOpsAPIError,
        AuthenticationError,
        NetworkError,
        ConfigurationError,
        ValidationError,
    )

# Set up logging
logger = logging.getLogger(__name__)


class AzureDevOpsClient:
    """Client for Azure DevOps REST API operations."""

    def __init__(
        self,
        organization: str,
        project: str,
        pat: str,
        verbose: bool = False,
        timeout: int = 30,
        max_retries: int = 3,
    ):
        """Initialize Azure DevOps client.

        Args:
            organization: Azure DevOps organization name
            project: Project name
            pat: Personal Access Token for authentication
            verbose: Whether to show detailed API information
            timeout: Request timeout in seconds
            max_retries: Maximum number of retry attempts for failed requests
        """
        # Validate required parameters
        if not organization:
            raise ConfigurationError("Organization is required", missing_config=["organization"])
        if not project:
            raise ConfigurationError("Project is required", missing_config=["project"])
        if not pat:
            raise AuthenticationError("Personal Access Token is required")

        self.organization = organization
        self.project = project
        self.pat = pat
        self.verbose = verbose
        self.timeout = timeout
        self.max_retries = max_retries
        self.base_url = f"https://dev.azure.com/{organization}/{project}"
        self.api_version = "7.1"

        # Setup authentication
        auth_string = base64.b64encode(f":{self.pat}".encode()).decode()
        self.headers = {"Authorization": f"Basic {auth_string}", "Content-Type": "application/json"}

        # Setup session with retry logic
        self.session = requests.Session()
        self.session.headers.update(self.headers)

        logger.info(f"Initialized Azure DevOps client for {organization}/{project}")

    def _make_request(
        self,
        method: str,
        url: str,
        operation: str,
        data: Dict = None,
        params: Dict = None,
        retry_count: int = 0,
    ) -> requests.Response:
        """Make HTTP request with retry logic and error handling.

        Args:
            method: HTTP method (GET, POST, PUT, DELETE, PATCH)
            url: Full URL for the request
            operation: Description of the operation for error messages
            data: Request body data
            params: Query parameters
            retry_count: Current retry attempt number

        Returns:
            Response object

        Raises:
            AzureDevOpsAPIError: If API request fails
            NetworkError: If network connectivity fails
            AuthenticationError: If authentication fails
        """
        try:
            if self.verbose:
                logger.debug(f"{method} {url}")
                if data:
                    logger.debug(f"Request body: {json.dumps(data, indent=2)}")

            response = self.session.request(
                method=method, url=url, json=data, params=params, timeout=self.timeout
            )

            if self.verbose:
                logger.debug(f"Response status: {response.status_code}")
                logger.debug(f"Response body: {response.text[:500]}")

            # Handle HTTP errors
            if not response.ok:
                self._handle_http_error(response, operation)

            return response

        except requests.exceptions.HTTPError:
            # Re-raise HTTPError from _handle_http_error (404 not found)
            raise
        except requests.exceptions.ConnectionError as e:
            logger.error(f"Connection error during {operation}: {e}")
            raise NetworkError(
                f"Failed to connect to Azure DevOps during {operation}",
                details=(
                    "Cannot establish connection to Azure DevOps.\n\n"
                    "Please check:\n"
                    "1. Internet connectivity\n"
                    "2. Azure DevOps URL is correct\n"
                    "3. Corporate proxy settings\n"
                    "4. VPN connection if required\n\n"
                    f"URL: {url}"
                ),
            )
        except requests.exceptions.Timeout as e:
            logger.error(f"Timeout during {operation}: {e}")
            if retry_count < self.max_retries:
                # Exponential backoff
                wait_time = 2**retry_count
                logger.info(
                    f"Retrying in {wait_time} seconds... (attempt {retry_count + 1}/{self.max_retries})"
                )
                if self.verbose:
                    print(f"⏳ Request timed out. Retrying in {wait_time} seconds...")
                time.sleep(wait_time)
                return self._make_request(method, url, operation, data, params, retry_count + 1)
            else:
                raise NetworkError(
                    f"Request timed out during {operation}",
                    details=(
                        f"The request took longer than {self.timeout} seconds.\n\n"
                        "Possible causes:\n"
                        "1. Slow network connection\n"
                        "2. Azure DevOps service issues\n"
                        "3. Large response payload\n\n"
                        "Try increasing timeout or check Azure DevOps status:\n"
                        "https://status.dev.azure.com"
                    ),
                )
        except requests.exceptions.RequestException as e:
            logger.error(f"Request exception during {operation}: {e}")
            raise NetworkError(
                f"Network error during {operation}",
                details=f"Request failed: {type(e).__name__}: {e}",
            )

    def _handle_http_error(self, response: requests.Response, operation: str) -> None:
        """Handle HTTP error responses with detailed messages.

        Args:
            response: Response object with error status code
            operation: Description of the operation

        Raises:
            AuthenticationError: For 401 status
            AzureDevOpsAPIError: For other error statuses
        """
        status_code = response.status_code
        url = response.url

        # Try to extract error message from response
        try:
            error_data = response.json()
            error_message = error_data.get("message", response.text)
        except (ValueError, json.JSONDecodeError):
            error_message = response.text

        if self.verbose:
            logger.error(f"HTTP {status_code} during {operation}")
            logger.error(f"URL: {url}")
            logger.error(f"Response: {error_message}")

        # Raise appropriate exception based on status code
        if status_code == 401 or status_code == 403:
            raise AuthenticationError(f"Authentication failed during {operation}")
        elif status_code == 404:
            # Raise HTTPError for 404 not found
            raise requests.exceptions.HTTPError(f"404 Not Found during {operation}")
        else:
            raise AzureDevOpsAPIError(
                f"API error during {operation}",
                status_code=status_code,
                response_body=error_message,
            )

    def _handle_api_error(self, e: requests.RequestException, operation: str) -> None:
        """Handle API errors with optional verbose output.

        DEPRECATED: Use _make_request() instead which has built-in error handling.
        """
        if self.verbose:
            print(f"Error during {operation}:")
            if hasattr(e, "response") and e.response is not None:
                print(f"  Status Code: {e.response.status_code}")
                print(f"  URL: {e.response.url}")
                try:
                    error_content = e.response.json()
                    print("  Response Body:")
                    print(json.dumps(error_content, indent=2))
                except (ValueError, json.JSONDecodeError):
                    print(f"  Response Body: {e.response.text}")
            else:
                print(f"  Details: {e}")
        else:
            print(f"Error {operation}: {e}")

    def get_repository(self, repo_name: str) -> Optional[Dict[str, Any]]:
        """Get repository information by name."""
        url = f"{self.base_url}/_apis/git/repositories?api-version={self.api_version}"

        try:
            response = requests.get(url, headers=self.headers)
            response.raise_for_status()

            repos = response.json().get("value", [])
            for repo in repos:
                if repo["name"] == repo_name:
                    return repo

            return None
        except requests.RequestException as e:
            self._handle_api_error(e, "fetching repository")
            return None

    def list_repositories(self) -> Optional[list]:
        """List all repositories in the project."""
        url = f"{self.base_url}/_apis/git/repositories?api-version={self.api_version}"

        try:
            response = requests.get(url, headers=self.headers)
            response.raise_for_status()

            return response.json().get("value", [])
        except requests.RequestException as e:
            self._handle_api_error(e, "fetching repositories")
            return None

    def delete_repository(self, repo_name: str) -> bool:
        """Delete a repository by name."""
        # First get the repository to find its ID
        repo = self.get_repository(repo_name)
        if not repo:
            print(f"Repository '{repo_name}' not found.")
            return False

        repo_id = repo["id"]
        url = f"{self.base_url}/_apis/git/repositories/{repo_id}?api-version={self.api_version}"

        try:
            response = requests.delete(url, headers=self.headers)
            response.raise_for_status()
            return True
        except requests.RequestException as e:
            self._handle_api_error(e, "deleting repository")
            return False

    def get_pipeline(self, pipeline_name: str) -> Optional[Dict[str, Any]]:
        """Get pipeline information by name."""
        url = f"{self.base_url}/_apis/pipelines?api-version={self.api_version}"

        try:
            response = requests.get(url, headers=self.headers)
            response.raise_for_status()

            pipelines = response.json().get("value", [])
            for pipeline in pipelines:
                if pipeline["name"] == pipeline_name:
                    return pipeline

            return None
        except requests.RequestException as e:
            self._handle_api_error(e, "fetching pipeline")
            return None

    def list_pipelines(self) -> Optional[list]:
        """List all pipelines in the project."""
        url = f"{self.base_url}/_apis/pipelines?api-version={self.api_version}"

        try:
            response = requests.get(url, headers=self.headers)
            response.raise_for_status()

            return response.json().get("value", [])
        except requests.RequestException as e:
            self._handle_api_error(e, "fetching pipelines")
            return None

    def create_pipeline(
        self, pipeline_name: str, repository_name: str, yaml_path: str, folder: str = "\\"
    ) -> Optional[Dict[str, Any]]:
        """Create a new pipeline."""
        # First get repository ID
        repo = self.get_repository(repository_name)
        if not repo:
            print(f"Repository '{repository_name}' not found.")
            return None

        repo_id = repo["id"]
        url = f"{self.base_url}/_apis/pipelines?api-version={self.api_version}"

        data = {
            "name": pipeline_name,
            "configuration": {
                "type": "yaml",
                "path": yaml_path,
                "repository": {"id": repo_id, "type": "azureReposGit"},
            },
            "folder": folder,
        }

        try:
            response = requests.post(url, headers=self.headers, json=data)
            response.raise_for_status()
            return response.json()
        except requests.RequestException as e:
            self._handle_api_error(e, "creating pipeline")
            return None

    def update_pipeline(
        self, pipeline_name: str, repository_name: str, yaml_path: str, folder: str = "\\"
    ) -> Optional[Dict[str, Any]]:
        """Update an existing pipeline's configuration."""
        # First get pipeline and repository info
        pipeline = self.get_pipeline(pipeline_name)
        if not pipeline:
            print(f"Pipeline '{pipeline_name}' not found.")
            return None

        repo = self.get_repository(repository_name)
        if not repo:
            print(f"Repository '{repository_name}' not found.")
            return None

        pipeline_id = pipeline["id"]
        repo_id = repo["id"]
        url = f"{self.base_url}/_apis/pipelines/{pipeline_id}?api-version={self.api_version}"

        data = {
            "configuration": {
                "type": "yaml",
                "path": yaml_path,
                "repository": {"id": repo_id, "type": "azureReposGit"},
            },
            "folder": folder,
        }

        try:
            response = requests.put(url, headers=self.headers, json=data)
            response.raise_for_status()
            return response.json()
        except requests.RequestException as e:
            self._handle_api_error(e, "updating pipeline")
            return None

    def delete_pipeline(self, pipeline_name: str) -> bool:
        """Delete a pipeline by name."""
        # First get the pipeline to find its ID
        pipeline = self.get_pipeline(pipeline_name)
        if not pipeline:
            print(f"Pipeline '{pipeline_name}' not found.")
            return False

        pipeline_id = pipeline["id"]
        url = f"{self.base_url}/_apis/pipelines/{pipeline_id}?api-version={self.api_version}"

        try:
            response = requests.delete(url, headers=self.headers)
            if response.status_code == 405:
                print("❌ Pipeline deletion is not supported via Azure DevOps REST API.")
                print("Pipelines can only be deleted through the Azure DevOps web interface.")
                print(
                    f"Pipeline URL: https://dev.azure.com/{self.organization}/{self.project}/_build?definitionId={pipeline_id}"
                )
                return False
            response.raise_for_status()
            return True
        except requests.RequestException as e:
            self._handle_api_error(e, "deleting pipeline")
            return False

    def run_pipeline(
        self, pipeline_name: str, branch: str = "main", parameters: Optional[Dict[str, Any]] = None
    ) -> Optional[Dict[str, Any]]:
        """Run/queue a pipeline by name."""
        # First get the pipeline to find its ID
        pipeline = self.get_pipeline(pipeline_name)
        if not pipeline:
            print(f"Pipeline '{pipeline_name}' not found.")
            return None

        pipeline_id = pipeline["id"]
        url = f"{self.base_url}/_apis/build/builds?api-version={self.api_version}"

        # Prepare the build request
        build_data = {"definition": {"id": pipeline_id}, "sourceBranch": f"refs/heads/{branch}"}

        # Only add parameters if they exist
        if parameters:
            build_data["parameters"] = json.dumps(parameters)

        try:
            response = requests.post(url, headers=self.headers, json=build_data)
            if response.status_code == 400:
                try:
                    error_details = response.json()
                    if (
                        "message" in error_details
                        and "validation" in error_details.get("message", "").lower()
                    ):
                        print("❌ Pipeline validation failed!")
                        print(
                            "The pipeline contains references to files that don't exist in the repository."
                        )
                        print(
                            "Please check that all template files and resources referenced in the pipeline YAML exist."
                        )
                        if (
                            "customProperties" in error_details
                            and "ValidationResults" in error_details["customProperties"]
                        ):
                            for validation in error_details["customProperties"][
                                "ValidationResults"
                            ]:
                                if validation.get("result") == "error":
                                    print(
                                        f"  - {validation.get('message', 'Unknown validation error')}"
                                    )
                    else:
                        print(f"API Error: {error_details.get('message', 'Unknown error')}")
                except Exception:
                    print(f"API Error Details: {response.text}")
                return None
            response.raise_for_status()
            build = response.json()

            print("✓ Pipeline run queued successfully!")
            print(f"Build ID: {build['id']}")
            print(f"Build Number: {build['buildNumber']}")
            print(f"Status: {build['status']}")
            print(f"URL: {build['url']}")
            return build
        except requests.RequestException as e:
            self._handle_api_error(e, "running pipeline")
            return None

    def create_repository(self, repo_name: str) -> Optional[Dict[str, Any]]:
        """Create a new repository."""
        url = f"{self.base_url}/_apis/git/repositories?api-version={self.api_version}"

        # First get project ID
        project_url = f"https://dev.azure.com/{self.organization}/_apis/projects?api-version={self.api_version}"
        try:
            response = requests.get(project_url, headers=self.headers)
            response.raise_for_status()

            projects = response.json().get("value", [])
            project = next((p for p in projects if p["name"] == self.project), None)

            if not project:
                print(f"Project '{self.project}' not found in organization '{self.organization}'")
                return None

            project_id = project["id"]

            # Create repository
            data = {"name": repo_name, "project": {"id": project_id}}

            response = requests.post(url, headers=self.headers, json=data)
            response.raise_for_status()

            return response.json()

        except requests.RequestException as e:
            self._handle_api_error(e, "creating repository")
            return None

    def get_build_status(self, build_id: int) -> Optional[Dict[str, Any]]:
        """Get the status and details of a specific build."""
        url = f"{self.base_url}/_apis/build/builds/{build_id}?api-version={self.api_version}"

        try:
            response = requests.get(url, headers=self.headers)
            response.raise_for_status()
            return response.json()
        except requests.RequestException as e:
            self._handle_api_error(e, "getting build status")
            return None

    def get_build_logs(self, build_id: int) -> Optional[Dict[str, Any]]:
        """Get the logs for a specific build."""
        url = f"{self.base_url}/_apis/build/builds/{build_id}/logs?api-version={self.api_version}"

        try:
            response = requests.get(url, headers=self.headers)
            response.raise_for_status()
            return response.json()
        except requests.RequestException as e:
            self._handle_api_error(e, "getting build logs")
            return None

    def get_build_timeline(self, build_id: int) -> Optional[Dict[str, Any]]:
        """Get the timeline (job/step details) for a specific build."""
        url = (
            f"{self.base_url}/_apis/build/builds/{build_id}/timeline?api-version={self.api_version}"
        )

        try:
            response = requests.get(url, headers=self.headers)
            response.raise_for_status()
            return response.json()
        except requests.RequestException as e:
            self._handle_api_error(e, "getting build timeline")
            return None

    def list_builds(self, pipeline_name: Optional[str] = None, top: int = 10) -> Optional[list]:
        """List builds, optionally filtered by pipeline name."""
        url = f"{self.base_url}/_apis/build/builds?api-version={self.api_version}&$top={top}"

        if pipeline_name:
            # First get the pipeline to find its ID
            pipeline = self.get_pipeline(pipeline_name)
            if pipeline:
                url += f"&definitions={pipeline['id']}"

        try:
            response = requests.get(url, headers=self.headers)
            response.raise_for_status()
            return response.json().get("value", [])
        except requests.RequestException as e:
            self._handle_api_error(e, "listing builds")
            return None

    def get_pull_request(self, repository_name: str, pr_id: int) -> Optional[Dict[str, Any]]:
        """Get a specific pull request by ID."""
        url = f"{self.base_url}/_apis/git/repositories/{repository_name}/pullRequests/{pr_id}?api-version={self.api_version}"

        try:
            response = requests.get(url, headers=self.headers)
            response.raise_for_status()
            return response.json()
        except requests.RequestException as e:
            self._handle_api_error(e, "getting pull request")
            return None

    def list_pull_requests(
        self, repository_name: str, status: str = "active", top: int = 10
    ) -> Optional[list]:
        """List pull requests for a repository."""
        url = f"{self.base_url}/_apis/git/repositories/{repository_name}/pullRequests?api-version={self.api_version}&$top={top}&status={status}"

        try:
            response = requests.get(url, headers=self.headers)
            response.raise_for_status()
            return response.json().get("value", [])
        except requests.RequestException as e:
            self._handle_api_error(e, "listing pull requests")
            return None

    def create_pull_request(
        self,
        repository_name: str,
        title: str,
        description: str = "",
        source_branch: str = "",
        target_branch: str = "main",
        is_draft: bool = False,
    ) -> Optional[Dict[str, Any]]:
        """Create a new pull request.

        Args:
            repository_name: Name of the repository
            title: PR title
            description: PR description
            source_branch: Source branch name
            target_branch: Target branch name (default: "main")
            is_draft: Create as draft PR (default: False)

        Returns:
            Dict with PR information

        Raises:
            ValidationError: If required parameters are invalid
            AzureDevOpsAPIError: If API request fails
        """
        # Validate inputs
        if not repository_name:
            raise ValidationError("Repository name is required", field="repository_name")
        if not title or not title.strip():
            raise ValidationError("PR title is required and cannot be empty", field="title")
        if not source_branch or not source_branch.strip():
            raise ValidationError("Source branch is required", field="source_branch")
        if not target_branch or not target_branch.strip():
            raise ValidationError("Target branch is required", field="target_branch")

        logger.info(f"Creating PR: {title} ({source_branch} -> {target_branch})")

        # Get repository info to ensure it exists and get the ID
        repo_info = self.get_repository(repository_name)
        if not repo_info:
            raise ValidationError(
                f"Repository '{repository_name}' not found",
                field="repository_name",
                details=(
                    f"Cannot find repository: {repository_name}\n\n"
                    f"Please verify:\n"
                    f"1. Repository name is spelled correctly\n"
                    f"2. You have access to the repository\n"
                    f"3. Repository exists in project: {self.project}"
                ),
            )

        repository_id = repo_info["id"]
        url = f"{self.base_url}/_apis/git/repositories/{repository_id}/pullRequests?api-version={self.api_version}"

        # Ensure branch refs are properly formatted
        if not source_branch.startswith("refs/heads/"):
            source_branch = f"refs/heads/{source_branch}"
        if not target_branch.startswith("refs/heads/"):
            target_branch = f"refs/heads/{target_branch}"

        data = {
            "sourceRefName": source_branch,
            "targetRefName": target_branch,
            "title": title,
            "description": description,
            "isDraft": is_draft,
        }

        try:
            response = self._make_request("POST", url, "creating pull request", data=data)
            pr_data = response.json()
            logger.info(f"PR created successfully: #{pr_data.get('pullRequestId')}")
            return pr_data
        except AzureDevOpsAPIError as e:
            # Add more specific context for PR creation errors
            if e.status_code == 409:
                raise ValidationError(
                    "Pull request already exists or branches are identical",
                    field="pull_request",
                    details=(
                        "Cannot create pull request. Possible reasons:\n"
                        "1. A PR already exists for these branches\n"
                        "2. Source and target branches are identical\n"
                        "3. No changes between source and target branches\n\n"
                        "Check existing PRs or verify branch differences."
                    ),
                )
            elif e.status_code == 400:
                raise ValidationError(
                    "Invalid pull request parameters",
                    field="pull_request",
                    details=(
                        f"Azure DevOps rejected the PR creation request.\n\n"
                        f"Response: {e.response_body}\n\n"
                        "Common issues:\n"
                        "1. Invalid branch names\n"
                        "2. Source branch doesn't exist\n"
                        "3. Target branch doesn't exist or is protected"
                    ),
                )
            # Re-raise other API errors
            raise

    def update_pull_request(
        self, repository_name: str, pr_id: int, updates: Dict[str, Any]
    ) -> Optional[Dict[str, Any]]:
        """Update an existing pull request."""
        url = f"{self.base_url}/_apis/git/repositories/{repository_name}/pullRequests/{pr_id}?api-version={self.api_version}"

        try:
            response = requests.patch(url, headers=self.headers, json=updates)
            response.raise_for_status()
            return response.json()
        except requests.RequestException as e:
            self._handle_api_error(e, "updating pull request")
            return None

    def merge_pull_request(
        self,
        repository_name: str,
        pr_id: int,
        merge_strategy: str = "merge",
        delete_source_branch: bool = False,
    ) -> bool:
        """Merge a pull request."""
        url = f"{self.base_url}/_apis/git/repositories/{repository_name}/pullRequests/{pr_id}?api-version={self.api_version}"

        # Map strategy names to Azure DevOps values
        strategy_map = {
            "merge": "Merge (no fast-forward)",
            "squash": "Squash",
            "rebase": "Rebase",
            "fast-forward": "Merge (fast-forward)",
        }

        completion_options = {
            "mergeStrategy": strategy_map.get(merge_strategy, "Merge (no fast-forward)"),
            "deleteSourceBranch": delete_source_branch,
            "squashMerge": (merge_strategy == "squash"),
        }

        data = {"status": "completed", "completionOptions": completion_options}

        try:
            response = requests.patch(url, headers=self.headers, json=data)
            response.raise_for_status()
            return True
        except requests.RequestException as e:
            self._handle_api_error(e, "merging pull request")
            return False

    def approve_pull_request(self, repository_name: str, pr_id: int, vote: int = 10) -> bool:
        """Approve or vote on a pull request. Vote values: 10=approve, -10=reject, -5=wait, 0=no vote."""
        # Try different approaches to get the current user identity

        # First, try using "me" as the reviewer ID
        reviewer_id = "me"

        url = f"{self.base_url}/_apis/git/repositories/{repository_name}/pullRequests/{pr_id}/reviewers/{reviewer_id}?api-version={self.api_version}"

        vote_data = {"vote": vote}

        try:
            response = requests.put(url, headers=self.headers, json=vote_data)
            response.raise_for_status()
            return True
        except requests.RequestException as e:
            self._handle_api_error(e, "with 'me' reviewer ID")

            # Try to get user from connection data
            try:
                url = f"{self.base_url}/_apis/connectionData?api-version={self.api_version}"
                response = requests.get(url, headers=self.headers)
                response.raise_for_status()
                connection_data = response.json()
                authenticated_user = connection_data.get("authenticatedUser", {})
                reviewer_id = authenticated_user.get("id")

                if reviewer_id:
                    url = f"{self.base_url}/_apis/git/repositories/{repository_name}/pullRequests/{pr_id}/reviewers/{reviewer_id}?api-version={self.api_version}"
                    response = requests.put(url, headers=self.headers, json=vote_data)
                    response.raise_for_status()
                    return True
            except requests.RequestException as e2:
                print(f"Error with connection data approach: {e2}")

            # Final fallback: try organization name
            reviewer_id = self.organization
            url = f"{self.base_url}/_apis/git/repositories/{repository_name}/pullRequests/{pr_id}/reviewers/{reviewer_id}?api-version={self.api_version}"

            try:
                response = requests.put(url, headers=self.headers, json=vote_data)
                response.raise_for_status()
                return True
            except requests.RequestException as e3:
                print(f"Error approving pull request: {e3}")
                print(f"Tried reviewer IDs: 'me', connection data user ID, and '{reviewer_id}'")
                print(
                    "Note: Make sure you have permission to review this PR and that your PAT has the correct scopes."
                )
                return False

    def get_pull_request_merge_status(
        self, repository_name: str, pr_id: int
    ) -> Optional[Dict[str, Any]]:
        """Get pull request merge status including conflict information."""
        # Use more expand options when verbose is enabled
        if self.verbose:
            expand = "mergeStatus,threads,commits,workItems"
        else:
            expand = "mergeStatus"

        url = f"{self.base_url}/_apis/git/repositories/{repository_name}/pullRequests/{pr_id}?api-version={self.api_version}&$expand={expand}"

        try:
            response = requests.get(url, headers=self.headers)
            response.raise_for_status()
            return response.json()
        except requests.RequestException as e:
            self._handle_api_error(e, "getting pull request merge status")
            return None

    def get_work_item(self, work_item_id: int) -> Optional[Dict[str, Any]]:
        """Get work item by ID to validate it exists and is accessible.

        Args:
            work_item_id: Work item ID to retrieve

        Returns:
            Dict with work item information or None if not found/accessible
        """
        # Work items use a different base URL
        url = f"https://dev.azure.com/{self.organization}/{self.project}/_apis/wit/workitems/{work_item_id}?api-version={self.api_version}"

        try:
            response = requests.get(url, headers=self.headers)
            response.raise_for_status()
            return response.json()
        except requests.RequestException as e:
            if hasattr(e, "response") and e.response and e.response.status_code == 404:
                # Work item not found - return None silently for validation purposes
                return None
            # For other errors, still handle them but don't print unless verbose
            if self.verbose:
                self._handle_api_error(e, f"getting work item #{work_item_id}")
            return None

    def create_work_item(
        self,
        work_item_type: str,
        title: str,
        description: str = "",
        assigned_to: Optional[str] = None,
        area_path: Optional[str] = None,
        iteration_path: Optional[str] = None,
        parent_id: Optional[int] = None,
        tags: Optional[str] = None,
        acceptance_criteria: Optional[List[str]] = None,
    ) -> Optional[Dict[str, Any]]:
        """Create a new work item.

        Args:
            work_item_type: Type of work item (PBI, Bug, Task, Spike, etc.)
            title: Work item title
            description: Work item description
            assigned_to: Email or display name of assigned user
            area_path: Area path (e.g., "Project\\Area")
            iteration_path: Iteration path (e.g., "Project\\Sprint 1")
            parent_id: ID of parent work item (for tasks)
            tags: Comma-separated tags
            acceptance_criteria: List of acceptance criteria items

        Returns:
            Dict with created work item information or None on failure
        """
        url = f"https://dev.azure.com/{self.organization}/{self.project}/_apis/wit/workitems/${work_item_type}?api-version={self.api_version}"

        # Build JSON Patch document for work item creation
        patch_document = [{"op": "add", "path": "/fields/System.Title", "value": title}]

        if description:
            patch_document.append(
                {"op": "add", "path": "/fields/System.Description", "value": description}
            )

        if assigned_to:
            patch_document.append(
                {"op": "add", "path": "/fields/System.AssignedTo", "value": assigned_to}
            )

        if area_path:
            patch_document.append(
                {"op": "add", "path": "/fields/System.AreaPath", "value": area_path}
            )

        if iteration_path:
            patch_document.append(
                {"op": "add", "path": "/fields/System.IterationPath", "value": iteration_path}
            )

        if tags:
            patch_document.append({"op": "add", "path": "/fields/System.Tags", "value": tags})

        if parent_id:
            patch_document.append(
                {
                    "op": "add",
                    "path": "/relations/-",
                    "value": {
                        "rel": "System.LinkTypes.Hierarchy-Reverse",
                        "url": f"https://dev.azure.com/{self.organization}/{self.project}/_apis/wit/workitems/{parent_id}",
                    },
                }
            )

        # Handle Acceptance Criteria: prefer a dedicated work item field if available in the project
        logging.debug("Client received acceptance_criteria: {acceptance_criteria}")
        if acceptance_criteria:
            logging.debug("Processing acceptance criteria for work_item_type: {work_item_type}")
            ac_html = "<ul>" + "\n".join([f"<li>{ac}</li>" for ac in acceptance_criteria]) + "</ul>"

            # Try to discover a field whose name/reference contains 'acceptance'
            ac_field_ref = None
            try:
                fields_url = f"https://dev.azure.com/{self.organization}/{self.project}/_apis/wit/fields?api-version={self.api_version}"
                logging.debug("Checking for AC fields at: {fields_url}")
                response = requests.get(fields_url, headers=self.headers)
                if response.status_code == 200:
                    fields_data = response.json()
                    logging.debug("Found {len(fields_data.get('value', []))} fields")
                    for field in fields_data.get("value", []):
                        field_name = field.get("name", "").lower()
                        if "acceptance" in field_name:
                            ac_field_ref = field.get("referenceName")
                            logging.debug("Found AC field: {field.get('name')} -> {ac_field_ref}")
                            break
                    if not ac_field_ref:
                        logging.debug("No acceptance criteria field found")
                else:
                    logging.debug("Failed to get fields, status: {response.status_code}")
            except Exception as e:
                logging.debug("Exception checking fields: {e}")
                pass  # Continue without dedicated field

            if ac_field_ref and work_item_type != "Task":
                # Use dedicated acceptance criteria field (for PBIs, Bugs, etc., but NOT Tasks)
                patch_document.append(
                    {"op": "add", "path": f"/fields/{ac_field_ref}", "value": ac_html}
                )
            else:
                # Append to description (for Tasks and other work item types without dedicated field)
                logging.debug("Using description approach for {work_item_type}")
                if description:
                    description += f"\n\n<h3>Acceptance Criteria</h3>\n{ac_html}"
                else:
                    description = f"<h3>Acceptance Criteria</h3>\n{ac_html}"
                # Update the description in the patch document
                desc_patch = next(
                    (p for p in patch_document if p.get("path") == "/fields/System.Description"),
                    None,
                )
                logging.debug("Found desc_patch: {desc_patch is not None}")
                if desc_patch:
                    desc_patch["value"] = description
                    logging.debug("Updated existing desc_patch")
                else:
                    patch_document.append(
                        {"op": "add", "path": "/fields/System.Description", "value": description}
                    )
                    logging.debug("Added new desc_patch")

        # JSON Patch requires specific content type
        headers = self.headers.copy()
        headers["Content-Type"] = "application/json-patch+json"

        try:
            if self.verbose:
                print(f"Creating {work_item_type}: {title}")
                print(f"URL: {url}")
                print(f"Patch Document: {patch_document}")

            response = requests.post(url, headers=headers, json=patch_document)
            response.raise_for_status()

            work_item = response.json()
            if self.verbose:
                print(f"Work item created successfully: #{work_item.get('id')}")

            return work_item
        except requests.RequestException as e:
            self._handle_api_error(e, f"creating {work_item_type}")
            return None

    def update_work_item(
        self,
        work_item_id: int,
        title: Optional[str] = None,
        description: Optional[str] = None,
        assigned_to: Optional[str] = None,
        state: Optional[str] = None,
        area_path: Optional[str] = None,
        iteration_path: Optional[str] = None,
        tags: Optional[str] = None,
    ) -> Optional[Dict[str, Any]]:
        """Update an existing work item.

        Args:
            work_item_id: ID of work item to update
            title: New title
            description: New description
            assigned_to: New assignee (email or display name)
            state: New state (To Do, In Progress, Done, etc.)
            area_path: New area path
            iteration_path: New iteration path
            tags: New tags (comma-separated)

        Returns:
            Dict with updated work item information or None on failure
        """
        url = f"https://dev.azure.com/{self.organization}/{self.project}/_apis/wit/workitems/{work_item_id}?api-version={self.api_version}"

        # Build JSON Patch document for updates
        patch_document = []

        if title is not None:
            patch_document.append({"op": "replace", "path": "/fields/System.Title", "value": title})

        if description is not None:
            patch_document.append(
                {"op": "replace", "path": "/fields/System.Description", "value": description}
            )

        if assigned_to is not None:
            patch_document.append(
                {"op": "replace", "path": "/fields/System.AssignedTo", "value": assigned_to}
            )

        if state is not None:
            patch_document.append({"op": "replace", "path": "/fields/System.State", "value": state})

        if area_path is not None:
            patch_document.append(
                {"op": "replace", "path": "/fields/System.AreaPath", "value": area_path}
            )

        if iteration_path is not None:
            patch_document.append(
                {"op": "replace", "path": "/fields/System.IterationPath", "value": iteration_path}
            )

        if tags is not None:
            patch_document.append({"op": "replace", "path": "/fields/System.Tags", "value": tags})

        if not patch_document:
            if self.verbose:
                print("No updates provided")
            return None

        # JSON Patch requires specific content type
        headers = self.headers.copy()
        headers["Content-Type"] = "application/json-patch+json"

        try:
            if self.verbose:
                print(f"Updating work item #{work_item_id}")
                print(f"Patch Document: {patch_document}")

            response = requests.patch(url, headers=headers, json=patch_document)
            response.raise_for_status()

            work_item = response.json()
            if self.verbose:
                print(f"Work item #{work_item_id} updated successfully")

            return work_item
        except requests.RequestException as e:
            self._handle_api_error(e, f"updating work item #{work_item_id}")
            return None

    def add_work_item_comment(
        self, work_item_id: int, comment_text: str
    ) -> Optional[Dict[str, Any]]:
        """Add a comment to a work item.

        Args:
            work_item_id: ID of work item
            comment_text: Comment text to add

        Returns:
            Dict with comment information or None on failure
        """
        # Comments API requires preview version
        url = f"https://dev.azure.com/{self.organization}/{self.project}/_apis/wit/workitems/{work_item_id}/comments?api-version={self.api_version}-preview"

        payload = {"text": comment_text}

        try:
            if self.verbose:
                print(f"Adding comment to work item #{work_item_id}")

            response = requests.post(url, headers=self.headers, json=payload)
            response.raise_for_status()

            comment = response.json()
            if self.verbose:
                print(f"Comment added successfully to work item #{work_item_id}")

            return comment
        except requests.RequestException as e:
            self._handle_api_error(e, f"adding comment to work item #{work_item_id}")
            return None

    def get_work_item_comments(self, work_item_id: int) -> Optional[Dict[str, Any]]:
        """Get all comments for a work item.

        Args:
            work_item_id: ID of work item

        Returns:
            Dict with comments list or None on failure
        """
        # Comments API requires preview version
        url = f"https://dev.azure.com/{self.organization}/{self.project}/_apis/wit/workitems/{work_item_id}/comments?api-version={self.api_version}-preview"

        try:
            if self.verbose:
                print(f"Fetching comments for work item #{work_item_id}")

            response = requests.get(url, headers=self.headers)
            response.raise_for_status()

            comments_data = response.json()
            if self.verbose:
                comment_count = len(comments_data.get("comments", []))
                print(f"Found {comment_count} comments for work item #{work_item_id}")

            return comments_data
        except requests.RequestException as e:
            self._handle_api_error(e, f"getting comments for work item #{work_item_id}")
            return None

    def query_work_items(self, wiql: str) -> Optional[Dict[str, Any]]:
        """Execute a WIQL (Work Item Query Language) query.

        Args:
            wiql: WIQL query string (e.g., "SELECT [System.Id] FROM WorkItems WHERE [System.WorkItemType] = 'Task'")

        Returns:
            Dict with query results or None on failure
        """
        url = f"https://dev.azure.com/{self.organization}/{self.project}/_apis/wit/wiql?api-version={self.api_version}"

        payload = {"query": wiql}

        try:
            if self.verbose:
                print(f"Executing WIQL query: {wiql}")

            response = requests.post(url, headers=self.headers, json=payload)
            response.raise_for_status()

            result = response.json()
            if self.verbose:
                work_item_count = len(result.get("workItems", []))
                print(f"Query returned {work_item_count} work items")

            return result
        except requests.RequestException as e:
            self._handle_api_error(e, "executing WIQL query")
            return None

    def _map_work_item_type(self, work_item_type: str) -> str:
        """Map short work item type names to full Azure DevOps names.

        Args:
            work_item_type: Short name like 'PBI', 'Bug', etc.

        Returns:
            Full work item type name used in Azure DevOps
        """
        type_mapping = {
            "PBI": "Product Backlog Item",
            "Bug": "Bug",
            "Task": "Task",
            "Spike": "Spike",
            "Epic": "Epic",
            "Feature": "Feature",
            "User Story": "User Story",
            "Issue": "Issue",
            "Test Case": "Test Case",
            "Test Plan": "Test Plan",
            "Test Suite": "Test Suite",
        }
        return type_mapping.get(work_item_type, work_item_type)

    def list_work_items(
        self,
        work_item_type: Optional[str] = None,
        state: Optional[str] = None,
        assigned_to: Optional[str] = None,
        area_path: Optional[str] = None,
        top: int = 50,
    ) -> Optional[list]:
        """List work items with optional filtering.

        Args:
            work_item_type: Filter by work item type (PBI, Bug, Task, etc.)
            state: Filter by state (To Do, In Progress, Done, etc.)
            assigned_to: Filter by assigned user
            area_path: Filter by area path (team name)
            top: Maximum number of items to return

        Returns:
            List of work items or None on failure
        """
        # Build WIQL query
        conditions = []

        if work_item_type:
            # Map short names to full names
            full_type = self._map_work_item_type(work_item_type)
            conditions.append(f"[System.WorkItemType] = '{full_type}'")

        if state:
            conditions.append(f"[System.State] = '{state}'")

        if assigned_to:
            conditions.append(f"[System.AssignedTo] = '{assigned_to}'")

        if area_path:
            conditions.append(f"[System.AreaPath] = '{area_path}'")

        where_clause = " AND ".join(conditions) if conditions else ""
        if where_clause:
            wiql = f"SELECT [System.Id], [System.Title], [System.State], [System.WorkItemType], [System.AreaPath] FROM WorkItems WHERE {where_clause} ORDER BY [System.ChangedDate] DESC"
        else:
            wiql = f"SELECT [System.Id], [System.Title], [System.State], [System.WorkItemType], [System.AreaPath] FROM WorkItems ORDER BY [System.ChangedDate] DESC"

        result = self.query_work_items(wiql)
        if not result:
            return None

        # Get work item IDs
        work_item_refs = result.get("workItems", [])
        if not work_item_refs:
            return []

        # Limit to top N
        work_item_ids = [item["id"] for item in work_item_refs[:top]]

        # Batch get work items details
        url = f"https://dev.azure.com/{self.organization}/{self.project}/_apis/wit/workitems?ids={','.join(map(str, work_item_ids))}&api-version={self.api_version}"

        try:
            response = requests.get(url, headers=self.headers)
            response.raise_for_status()

            batch_result = response.json()
            return batch_result.get("value", [])
        except requests.RequestException as e:
            self._handle_api_error(e, "getting work items batch")
            return None

    def get_current_user(self) -> Optional[str]:
        """Get the currently authenticated user's email address.

        Returns:
            User email address or None on failure
        """
        # Use the Azure DevOps Profile API to get current user
        url = f"https://app.vssps.visualstudio.com/_apis/profile/profiles/me?api-version={self.api_version}"

        try:
            response = requests.get(url, headers=self.headers)
            response.raise_for_status()

            profile = response.json()
            # Prefer emailAddress which is present for AAD/Work accounts; fallback to publicAlias (display name)
            email = (
                profile.get("emailAddress")
                or profile.get("publicAlias")
                or profile.get("displayName")
            )

            if email and self.verbose:
                print(f"Current user (Profile API): {email}")

            if email:
                return email
        except requests.RequestException:
            # Try other approaches below
            if self.verbose:
                print("Profile API lookup failed; trying connection data...")

        # Fallback 1: Try connectionData to get authenticatedUser uniqueName or id
        try:
            conn_url = f"https://dev.azure.com/{self.organization}/_apis/connectionData?api-version={self.api_version}"
            resp = requests.get(conn_url, headers=self.headers)
            resp.raise_for_status()
            connection_data = resp.json()
            auth_user = connection_data.get("authenticatedUser", {})
            # Try uniqueName or descriptor
            unique_name = (
                auth_user.get("uniqueName")
                or auth_user.get("displayName")
                or auth_user.get("properties", {}).get("mail")
            )
            if unique_name:
                if self.verbose:
                    print(f"Current user (connectionData): {unique_name}")
                return unique_name
        except requests.RequestException:
            if self.verbose:
                print("Connection data lookup failed; trying Azure CLI...")

        # Fallback 2: try to extract from Azure CLI if available
        try:
            import subprocess

            result = subprocess.run(
                ["az", "account", "show", "--query", "user.name", "-o", "tsv"],
                capture_output=True,
                text=True,
                check=True,
            )
            email = result.stdout.strip()
            if email and self.verbose:
                print(f"Current user (Azure CLI): {email}")
            return email if email else None
        except Exception:
            if self.verbose:
                print("Could not determine current user")
            return None

    def get_child_work_items(self, parent_id: int) -> Optional[List[Dict[str, Any]]]:
        """Get all child work items for a given parent work item.

        Args:
            parent_id: ID of the parent work item

        Returns:
            List of child work items or None on failure
        """
        # Query for child work items using parent relationship
        wiql = f"""
        SELECT [System.Id], [System.Title], [System.State], [System.WorkItemType]
        FROM WorkItemLinks
        WHERE (Source.[System.Id] = {parent_id})
        AND ([System.Links.LinkType] = 'System.LinkTypes.Hierarchy-Forward')
        MODE (MustContain)
        """

        result = self.query_work_items(wiql)
        if not result:
            return None

        # Get work item relations
        relations = result.get("workItemRelations", [])
        if not relations:
            return []

        # Extract target work item IDs (skip the first one which is the source)
        child_ids = [rel["target"]["id"] for rel in relations if rel.get("target")]

        if not child_ids:
            return []

        # Batch get work item details
        ids_str = ",".join(map(str, child_ids))
        url = f"https://dev.azure.com/{self.organization}/{self.project}/_apis/wit/workitems?ids={ids_str}&api-version={self.api_version}"

        try:
            response = requests.get(url, headers=self.headers)
            response.raise_for_status()

            batch_result = response.json()
            children = batch_result.get("value", [])

            if self.verbose:
                print(f"Found {len(children)} child work items for #{parent_id}")

            return children
        except requests.RequestException as e:
            self._handle_api_error(e, f"getting child work items for #{parent_id}")
            return None

    def update_work_item_iteration(
        self, work_item_id: int, iteration_path: str
    ) -> Optional[Dict[str, Any]]:
        """Update a work item's iteration path (sprint).

        Args:
            work_item_id: ID of work item to update
            iteration_path: New iteration path (e.g., "ProjectName\\Sprint 03")

        Returns:
            Dict with updated work item information or None on failure
        """
        return self.update_work_item(work_item_id=work_item_id, iteration_path=iteration_path)

    def link_work_item_to_pr(self, repository_name: str, pr_id: int, work_item_id: int) -> bool:
        """Link a work item to a pull request.

        Args:
            repository_name: Name of the repository
            pr_id: Pull request ID
            work_item_id: Work item ID to link

        Returns:
            True if successful, False otherwise
        """
        # First validate the work item exists
        work_item = self.get_work_item(work_item_id)
        if not work_item:
            print(f"Cannot link work item #{work_item_id}: work item not found or not accessible")
            return False

        # Azure DevOps REST API doesn't support linking work items to PRs directly
        # We need to use the Azure CLI command or subprocess
        try:
            import subprocess
            import platform

            # On Windows, we need to use shell=True or call az.cmd explicitly
            # to ensure the command is found in PATH
            is_windows = platform.system() == "Windows"

            # Use Azure CLI to link the work item to the PR
            cmd = [
                "az",
                "repos",
                "pr",
                "work-item",
                "add",
                "--id",
                str(pr_id),
                "--work-items",
                str(work_item_id),
            ]

            subprocess.run(
                cmd,
                capture_output=True,
                text=True,
                check=True,
                shell=is_windows,  # Use shell on Windows to find az.cmd
            )

            if self.verbose:
                print(f"Successfully linked work item #{work_item_id} to PR #{pr_id}")
            return True

        except subprocess.CalledProcessError as e:
            if self.verbose:
                print(f"Error linking work item #{work_item_id} to PR #{pr_id}:")
                print(f"  Exit code: {e.returncode}")
                if e.stderr:
                    print(f"  Error: {e.stderr}")
            else:
                print(f"Failed to link work item #{work_item_id} to PR #{pr_id}")
            return False
        except FileNotFoundError:
            print("Azure CLI (az) is not installed or not in PATH")
            print("Work item linking requires Azure CLI to be installed")
            print("Install from: https://learn.microsoft.com/cli/azure/install-azure-cli")
            return False
        except Exception as e:
            if self.verbose:
                print(f"Unexpected error linking work item: {e}")
            return False


class ConfigManager:
    """Manages YAML configuration files."""

    def __init__(self, config_path: str = "input.yaml", verbose: bool = False):
        self.config_path = Path(config_path)
        self.verbose = verbose

    def load_config(self) -> Dict[str, Any]:
        """Load configuration from YAML file."""
        if not self.config_path.exists():
            return {}

        try:
            with open(self.config_path, "r", encoding="utf-8") as f:
                return yaml.safe_load(f) or {}
        except Exception as e:
            if self.verbose:
                print(f"Error loading config: {e}")
            else:
                print("Error loading config")
            return {}

    def save_config(self, config: Dict[str, Any]) -> bool:
        """Save configuration to YAML file."""
        try:
            with open(self.config_path, "w", encoding="utf-8") as f:
                yaml.dump(config, f, default_flow_style=False, sort_keys=False)
            return True
        except Exception as e:
            if self.verbose:
                print(f"Error saving config: {e}")
            else:
                print("Error saving config")
            return False

    def update_config(self, updates: Dict[str, Any]) -> bool:
        """Update existing configuration with new values."""
        config = self.load_config()
        config.update(updates)
        return self.save_config(config)


def get_personal_access_token() -> Optional[str]:
    """Get personal access token from environment variable AZURE_DEVOPS_PAT."""
    return os.environ.get("AZURE_DEVOPS_PAT")


def extract_azure_devops_info_from_git() -> Optional[Dict[str, str]]:
    """Extract Azure DevOps organization, project, and repository from Git remote URL."""
    try:
        import subprocess

        # Get the remote URL - prefer 'azure' remote, fallback to 'origin'
        remote_name = "azure"
        result = subprocess.run(
            ["git", "remote", "get-url", "azure"], capture_output=True, text=True, cwd=os.getcwd()
        )

        # If azure remote doesn't exist, try origin
        if result.returncode != 0:
            remote_name = "origin"
            result = subprocess.run(
                ["git", "remote", "get-url", "origin"],
                capture_output=True,
                text=True,
                cwd=os.getcwd(),
            )

        if result.returncode != 0:
            return None

        remote_url = result.stdout.strip()

        # Parse Azure DevOps URL patterns
        # Pattern 1: https://dev.azure.com/{org}/{project}/_git/{repo}
        # Pattern 2: https://{org}@dev.azure.com/{org}/{project}/_git/{repo}
        # Pattern 3: git@ssh.dev.azure.com:v3/{org}/{project}/{repo}

        import re

        # Pattern for HTTPS URLs
        https_pattern = r"https://(?:[^@]+@)?dev\.azure\.com/([^/]+)/([^/]+)/_git/([^/\s]+)"
        match = re.search(https_pattern, remote_url)

        if match:
            org, project, repo = match.groups()
            return {"organization": org, "project": project, "repository": repo}

        # Pattern for SSH URLs
        ssh_pattern = r"git@ssh\.dev\.azure\.com:v3/([^/]+)/([^/]+)/([^/\s]+)"
        match = re.search(ssh_pattern, remote_url)

        if match:
            org, project, repo = match.groups()
            return {"organization": org, "project": project, "repository": repo}

        return None

    except Exception:
        return None


def extract_platform_info_from_git() -> Optional[Dict[str, str]]:
    """Extract platform information from Git remote URL.

    Detects the platform (GitHub, Azure DevOps, etc.) and returns
    platform-specific configuration information.

    Returns:
        Dictionary with platform information, or None if not a supported platform
        or not in a git repository.
    """
    try:
        import subprocess
        import re

        # Get the remote information - prefer 'azure' remote, fallback to 'origin'
        remote_name = "azure"
        result = subprocess.run(
            ["git", "remote", "-v"], capture_output=True, text=True, cwd=os.getcwd()
        )

        if result.returncode != 0:
            return None

        remote_output = result.stdout.strip()

        # Parse the remote output to find the fetch URL for the preferred remote
        remote_url = None
        lines = remote_output.split("\n")
        for line in lines:
            if line.startswith(f"{remote_name}\t") and "(fetch)" in line:
                # Extract URL from "remote_name<TAB>url (fetch)" format
                parts = line.split("\t")
                if len(parts) >= 2:
                    remote_url = parts[1].split(" ")[0]  # Remove (fetch) part
                    break

        # If azure remote not found, try origin
        if not remote_url:
            remote_name = "origin"
            for line in lines:
                if line.startswith(f"{remote_name}\t") and "(fetch)" in line:
                    parts = line.split("\t")
                    if len(parts) >= 2:
                        remote_url = parts[1].split(" ")[0]  # Remove (fetch) part
                        break

        if not remote_url:
            return None

        # Check for GitHub URLs
        github_pattern = r"https://github\.com/([^/]+)/([^/\s]+)"
        match = re.search(github_pattern, remote_url)
        if match:
            owner, repo = match.groups()
            return {"platform": "github", "owner": owner, "repo": repo, "remote_url": remote_url}

        # Check for Azure DevOps HTTPS URLs
        azdo_https_pattern = r"https://(?:[^@]+@)?dev\.azure\.com/([^/]+)/([^/]+)/_git/([^/\s]+)"
        match = re.search(azdo_https_pattern, remote_url)
        if match:
            org, project, repo = match.groups()
            return {
                "platform": "azdo",
                "organization": org,
                "project": project,
                "repository": repo,
                "remote_url": remote_url,
            }

        # Check for Azure DevOps SSH URLs
        azdo_ssh_pattern = r"git@ssh\.dev\.azure\.com:v3/([^/]+)/([^/]+)/([^/\s]+)"
        match = re.search(azdo_ssh_pattern, remote_url)
        if match:
            org, project, repo = match.groups()
            return {
                "platform": "azdo",
                "organization": org,
                "project": project,
                "repository": repo,
                "remote_url": remote_url,
            }

        # Unsupported platform
        return None

    except Exception:
        return None


def ensure_config_exists(config_filename: str = "input.yaml", command_type: str = "general"):
    """Ensure config file exists, create sample if it doesn't."""
    config_path = config_filename
    if not Path(config_path).exists():
        print(f"{config_path} not found. Creating sample configuration...")
        sample_config = create_default_config(command_type)
        try:
            with open(config_path, "w", encoding="utf-8") as f:
                f.write(sample_config)
            print(f"✓ Created sample {config_path}")
            print("Please edit it with your actual values before running commands.")
            return True
        except Exception as e:
            print(f"❌ Failed to create sample configuration: {e}")
            return False
    return True


def create_default_config(command_type: str = "general") -> str:
    """Create default configuration YAML content based on command type."""
    if command_type == "repo":
        return """# Azure DevOps Repository Configuration
# This file contains the essential parameters needed for Azure DevOps repository operations
#
# IMPORTANT: Update the personalAccessToken value with your actual PAT before running scripts

# Azure DevOps Organization Settings
organization: "your-organization"
project: "your-project"

# Repository Settings
repository: "your-repository-name"

# Authentication (REQUIRED - Replace YOUR_PAT_HERE with your actual PAT)
personalAccessToken: "YOUR_PAT_HERE"

# API Settings
apiVersion: "7.1"
"""
    elif command_type == "pipeline":
        return """# Azure DevOps Pipeline Configuration
# This file contains the essential parameters needed for Azure DevOps pipeline operations
#
# IMPORTANT: Update the personalAccessToken value with your actual PAT before running scripts

# Azure DevOps Organization Settings
organization: "your-organization"
project: "your-project"

# Pipeline Settings
pipelineName: "your-pipeline-name"
repository: "your-repository-name"
pipelineYamlPath: ".azure-pipelines/your-pipeline.yml"

# Authentication (REQUIRED - Replace YOUR_PAT_HERE with your actual PAT)
personalAccessToken: "YOUR_PAT_HERE"

# Optional Settings
apiVersion: "7.1"
pipelineFolder: "/"

# Pipeline Run Settings (optional)
branch: "main"  # Branch to run the pipeline on
# parameters:    # Uncomment and add parameters if needed
#   param1: "value1"
#   param2: "value2"
"""
    else:
        # Default/general config
        return """# Azure DevOps Configuration
# This file contains the essential parameters needed for Azure DevOps operations
#
# IMPORTANT: Update the personalAccessToken value with your actual PAT before running scripts

# Azure DevOps Organization Settings
organization: "your-organization"
project: "your-project"

# Authentication (REQUIRED - Replace YOUR_PAT_HERE with your actual PAT)
personalAccessToken: "YOUR_PAT_HERE"

# API Settings
apiVersion: "7.1"
"""
