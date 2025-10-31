"""
Azure DevOps API client for work item operations.
"""

import base64
import json
import logging
import subprocess
import re
import requests
import time
from typing import Dict, Any, Optional, List

# Set up logging
logger = logging.getLogger(__name__)


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
    """Azure DevOps REST API client with comprehensive work item support."""

    def __init__(self, organization: str, project: str, pat: str, verbose: bool = False,
                 timeout: int = 30, max_retries: int = 3):
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
            raise ValueError("Organization is required")
        if not project:
            raise ValueError("Project is required")
        if not pat:
            raise ValueError("Personal Access Token is required")

        self.organization = organization
        self.project = project
        self.pat = pat
        self.verbose = verbose
        self.timeout = timeout
        self.max_retries = max_retries
        self.base_url = f"https://dev.azure.com/{organization}/{project}/_apis"
        self.api_version = "7.1"

        # Setup authentication
        auth_string = base64.b64encode(f":{self.pat}".encode()).decode()
        self.headers = {"Authorization": f"Basic {auth_string}", "Content-Type": "application/json"}

        # Setup session with retry logic
        self.session = requests.Session()
        self.session.headers.update(self.headers)

        logger.info(f"Initialized Azure DevOps client for {organization}/{project}")

    def _make_request(self, method: str, url: str, operation: str,
                      data: Dict = None, params: Dict = None,
                      retry_count: int = 0) -> requests.Response:
        """Make HTTP request with retry logic and error handling."""
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

        except requests.exceptions.ConnectionError as e:
            logger.error(f"Connection error during {operation}: {e}")
            raise ConnectionError(f"Failed to connect to Azure DevOps during {operation}")
        except requests.exceptions.Timeout as e:
            logger.error(f"Timeout during {operation}: {e}")
            if retry_count < self.max_retries:
                # Exponential backoff
                wait_time = 2**retry_count
                logger.info(f"Retrying in {wait_time} seconds... "
                           f"(attempt {retry_count + 1}/{self.max_retries})")
                if self.verbose:
                    print(f"⏳ Request timed out. Retrying in {wait_time} seconds...")
                time.sleep(wait_time)
                return self._make_request(method, url, operation, data, params, retry_count + 1)
            else:
                raise TimeoutError(f"Request timed out during {operation}")
        except requests.exceptions.RequestException as e:
            logger.error(f"Request exception during {operation}: {e}")
            raise ConnectionError(f"Network error during {operation}")

    def _handle_http_error(self, response: requests.Response, operation: str) -> None:
        """Handle HTTP error responses with detailed messages."""
        status_code = response.status_code

        # Try to extract error message from response
        try:
            error_data = response.json()
            error_message = error_data.get("message", response.text)
        except (ValueError, json.JSONDecodeError):
            error_message = response.text

        if self.verbose:
            logger.error(f"HTTP {status_code} during {operation}")
            logger.error(f"Response: {error_message}")

        # Raise appropriate exception based on status code
        if status_code == 401:
            raise PermissionError(f"Authentication failed during {operation}")
        else:
            raise Exception(f"API error during {operation}: HTTP {status_code}")

    def create_work_item(self, work_item_type: str, title: str, description: str = "",
                        assigned_to: Optional[str] = None, area_path: Optional[str] = None,
                        iteration_path: Optional[str] = None, parent_id: Optional[int] = None,
                        tags: Optional[str] = None, acceptance_criteria: Optional[List[str]] = None,
                        **kwargs) -> Optional[Dict[str, Any]]:
        """Create a new work item with comprehensive field support."""
        url = (f"https://dev.azure.com/{self.organization}/{self.project}/"
               f"_apis/wit/workitems/${work_item_type}?api-version={self.api_version}")

        # Build JSON Patch document for work item creation
        patch_document = [{"op": "add", "path": "/fields/System.Title", "value": title}]

        if description:
            patch_document.append({"op": "add", "path": "/fields/System.Description",
                                   "value": description})

        if assigned_to:
            patch_document.append({"op": "add", "path": "/fields/System.AssignedTo",
                                   "value": assigned_to})

        if area_path:
            patch_document.append({"op": "add", "path": "/fields/System.AreaPath",
                                   "value": area_path})

        if iteration_path:
            patch_document.append({"op": "add", "path": "/fields/System.IterationPath",
                                   "value": iteration_path})

        if tags:
            patch_document.append({"op": "add", "path": "/fields/System.Tags", "value": tags})

        if parent_id:
            patch_document.append({
                "op": "add",
                "path": "/relations/-",
                "value": {
                    "rel": "System.LinkTypes.Hierarchy-Reverse",
                    "url": (f"https://dev.azure.com/{self.organization}/{self.project}/"
                           f"_apis/wit/workitems/{parent_id}"),
                },
            })

        # Handle Acceptance Criteria
        if acceptance_criteria:
            ac_html = '<ul>' + '\n'.join([f'<li>{ac}</li>' for ac in acceptance_criteria]) + '</ul>'

            # Try to find a dedicated acceptance criteria field
            ac_field_ref = None
            try:
                fields_url = (f"https://dev.azure.com/{self.organization}/{self.project}/"
                             f"_apis/wit/fields?api-version={self.api_version}")
                response = requests.get(fields_url, headers=self.headers)
                if response.status_code == 200:
                    fields_data = response.json()
                    for field in fields_data.get('value', []):
                        field_name = field.get('name', '').lower()
                        if 'acceptance' in field_name:
                            ac_field_ref = field.get('referenceName')
                            break
            except Exception:
                pass  # Continue without dedicated field

            if ac_field_ref and work_item_type != 'Task':
                # Use dedicated acceptance criteria field
                patch_document.append({"op": "add", "path": f"/fields/{ac_field_ref}",
                                       "value": ac_html})
            else:
                # Append to description
                if description:
                    description += f'\n\n<h3>Acceptance Criteria</h3>\n{ac_html}'
                else:
                    description = f'<h3>Acceptance Criteria</h3>\n{ac_html}'
                # Update the description in the patch document
                desc_patch = next((p for p in patch_document
                                  if p.get('path') == '/fields/System.Description'), None)
                if desc_patch:
                    desc_patch['value'] = description
                else:
                    patch_document.append({"op": "add", "path": "/fields/System.Description",
                                           "value": description})

        # JSON Patch requires specific content type
        headers = self.headers.copy()
        headers["Content-Type"] = "application/json-patch+json"

        try:
            if self.verbose:
                print(f"Creating {work_item_type}: {title}")

            response = requests.post(url, headers=headers, json=patch_document)
            response.raise_for_status()

            work_item = response.json()
            if self.verbose:
                print(f"Work item created successfully: #{work_item.get('id')}")

            return work_item
        except requests.RequestException as e:
            if self.verbose:
                print(f"Failed to create {work_item_type}: {e}")
            return None
