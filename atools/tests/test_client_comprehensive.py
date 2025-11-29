"""
Tests for Azure DevOps client functionality.
Tests API calls, authentication, error handling, and core operations.
"""

import pytest
from unittest.mock import patch, MagicMock
import requests
from pathlib import Path
import sys

# Add the atools directory to sys.path
sys.path.insert(0, str(Path(__file__).parent.parent))

from sdo_package.client import AzureDevOpsClient, extract_platform_info_from_git  # noqa: E402
from sdo_package.exceptions import AuthenticationError, NetworkError  # noqa: E402


class TestAzureDevOpsClientInit:
    """Test Azure DevOps client initialization."""

    def test_client_initialization(self):
        """Test basic client initialization."""
        client = AzureDevOpsClient(organization="test-org", project="test-project", pat="test-pat")

        assert client.organization == "test-org"
        assert client.project == "test-project"
        assert client.pat == "test-pat"
        assert client.base_url == "https://dev.azure.com/test-org/test-project"
        assert client.timeout == 30

    def test_client_initialization_with_timeout(self):
        """Test client initialization with custom timeout."""
        client = AzureDevOpsClient(
            organization="test-org", project="test-project", pat="test-pat", timeout=60
        )

        assert client.timeout == 60

    def test_client_initialization_with_verbose(self):
        """Test client initialization with verbose mode."""
        client = AzureDevOpsClient(
            organization="test-org", project="test-project", pat="test-pat", verbose=True
        )

        assert client.verbose is True


class TestAzureDevOpsClientAuthentication:
    """Test Azure DevOps client authentication."""

    def test_auth_header_setup(self):
        """Test authentication header is properly set up."""
        client = AzureDevOpsClient(organization="test-org", project="test-project", pat="test-pat")

        # Check that authorization header is set
        assert "Authorization" in client.headers
        assert client.headers["Authorization"].startswith("Basic ")

    def test_missing_pat_error(self):
        """Test error when PAT is missing."""
        with pytest.raises(AuthenticationError):
            _client = AzureDevOpsClient(organization="test-org", project="test-project", pat=None)  # noqa: F841


class TestAzureDevOpsClientAPICalls:
    """Test Azure DevOps API call functionality."""

    def setup_method(self):
        """Set up test fixtures."""
        self.client = AzureDevOpsClient(
            organization="test-org", project="test-project", pat="test-pat", verbose=True
        )

    @patch("requests.Session.request")
    def test_successful_api_call(self, mock_request):
        """Test successful API call."""
        # Mock the response
        mock_response = MagicMock()
        mock_response.status_code = 200
        mock_response.ok = True
        mock_response.json.return_value = {"test": "data"}
        mock_request.return_value = mock_response

        result = self.client._make_request(
            "GET", "https://dev.azure.com/test-org/test-project/_apis/test", "test operation"
        )

        assert result.status_code == 200
        mock_request.assert_called_once()

    @patch("requests.Session.request")
    def test_api_call_401_error(self, mock_request):
        """Test API call with 401 authentication error."""
        mock_response = MagicMock()
        mock_response.status_code = 401
        mock_response.ok = False
        mock_response.text = "401 Unauthorized"
        mock_request.return_value = mock_response

        with pytest.raises(AuthenticationError):
            self.client._make_request(
                "GET", "https://dev.azure.com/test-org/test-project/_apis/test", "test operation"
            )

    @patch("requests.Session.request")
    def test_api_call_403_error(self, mock_request):
        """Test API call with 403 forbidden error."""
        mock_response = MagicMock()
        mock_response.status_code = 403
        mock_response.ok = False
        mock_response.text = "403 Forbidden"
        mock_request.return_value = mock_response

        with pytest.raises(AuthenticationError):
            self.client._make_request(
                "GET", "https://dev.azure.com/test-org/test-project/_apis/test", "test operation"
            )

    @patch("requests.Session.request")
    def test_api_call_404_error(self, mock_request):
        """Test API call with 404 not found error."""
        mock_response = MagicMock()
        mock_response.status_code = 404
        mock_response.ok = False
        mock_response.text = "404 Not Found"
        mock_request.return_value = mock_response

        with pytest.raises(requests.exceptions.HTTPError):
            self.client._make_request(
                "GET", "https://dev.azure.com/test-org/test-project/_apis/test", "test operation"
            )

    @patch("requests.Session.request")
    def test_api_call_network_error(self, mock_request):
        """Test API call with network error."""
        mock_request.side_effect = requests.exceptions.ConnectionError("Network error")

        with pytest.raises(NetworkError):
            self.client._make_request(
                "GET", "https://dev.azure.com/test-org/test-project/_apis/test", "test operation"
            )

    @patch("requests.Session.request")
    def test_api_call_timeout_error(self, mock_request):
        """Test API call with timeout error."""
        mock_request.side_effect = requests.exceptions.Timeout("Timeout")

        with pytest.raises(NetworkError):
            self.client._make_request(
                "GET", "https://dev.azure.com/test-org/test-project/_apis/test", "test operation"
            )


class TestRepositoryOperations:
    """Test repository-related API operations."""

    def setup_method(self):
        """Set up test fixtures."""
        with patch("sdo_package.client.get_personal_access_token", return_value="test-pat"):
            self.client = AzureDevOpsClient(
                organization="test-org", project="test-project", pat="test-pat"
            )

    @patch("requests.get")
    def test_list_repositories_success(self, mock_get):
        """Test successful repository listing."""
        mock_response = MagicMock()
        mock_response.json.return_value = {
            "value": [
                {
                    "id": "repo1",
                    "name": "Repository 1",
                    "webUrl": "https://dev.azure.com/test-org/test-project/_git/repo1",
                    "defaultBranch": "refs/heads/main",
                },
                {
                    "id": "repo2",
                    "name": "Repository 2",
                    "webUrl": "https://dev.azure.com/test-org/test-project/_git/repo2",
                    "defaultBranch": "refs/heads/develop",
                },
            ]
        }
        mock_response.raise_for_status = MagicMock()
        mock_get.return_value = mock_response

        repos = self.client.list_repositories()

        assert len(repos) == 2
        assert repos[0]["name"] == "Repository 1"
        assert repos[1]["name"] == "Repository 2"
        mock_get.assert_called_once()

    @patch("requests.get")
    def test_list_repositories_empty(self, mock_get):
        """Test listing repositories when none exist."""
        mock_response = MagicMock()
        mock_response.json.return_value = {"value": []}
        mock_response.raise_for_status = MagicMock()
        mock_get.return_value = mock_response

        repos = self.client.list_repositories()

        assert repos == []
        mock_get.assert_called_once()

    @patch("requests.get")
    def test_get_repository_success(self, mock_get):
        """Test successful repository retrieval."""
        mock_response = MagicMock()
        mock_response.json.return_value = {
            "value": [
                {
                    "id": "repo1",
                    "name": "Repository 1",
                    "webUrl": "https://dev.azure.com/test-org/test-project/_git/repo1",
                    "defaultBranch": "refs/heads/main",
                    "size": 1024,
                }
            ]
        }
        mock_response.raise_for_status = MagicMock()
        mock_get.return_value = mock_response

        repo = self.client.get_repository("Repository 1")

        assert repo["name"] == "Repository 1"
        assert repo["id"] == "repo1"
        mock_get.assert_called_once()

    @patch("requests.get")
    def test_get_repository_not_found(self, mock_get):
        """Test repository retrieval when not found."""
        mock_response = MagicMock()
        mock_response.json.return_value = {"value": []}
        mock_response.raise_for_status = MagicMock()
        mock_get.return_value = mock_response

        repo = self.client.get_repository("nonexistent")

        assert repo is None
        mock_get.assert_called_once()

    @patch("requests.post")
    @patch("requests.get")
    def test_create_repository_success(self, mock_get, mock_post):
        """Test successful repository creation."""
        # Mock the GET request for projects
        mock_get_response = MagicMock()
        mock_get_response.json.return_value = {
            "value": [{"id": "project-id", "name": "test-project"}]
        }
        mock_get_response.raise_for_status = MagicMock()
        mock_get.return_value = mock_get_response

        # Mock the POST request for repository creation
        mock_post_response = MagicMock()
        expected_repo_data = {
            "id": "new-repo",
            "name": "New Repository",
            "webUrl": "https://dev.azure.com/test-org/test-project/_git/new-repo",
        }
        mock_post_response.json.return_value = expected_repo_data
        mock_post_response.raise_for_status = MagicMock()
        mock_post.return_value = mock_post_response

        result = self.client.create_repository("new-repo")

        assert result == expected_repo_data
        mock_get.assert_called_once()
        mock_post.assert_called_once()

    @patch("requests.delete")
    @patch("requests.get")
    def test_delete_repository_success(self, mock_get, mock_delete):
        """Test successful repository deletion."""
        # Mock get_repository call
        mock_get_response = MagicMock()
        mock_get_response.json.return_value = {
            "value": [{"id": "repo-to-delete-id", "name": "repo-to-delete"}]
        }
        mock_get_response.raise_for_status = MagicMock()
        mock_get.return_value = mock_get_response

        # Mock delete call
        mock_delete_response = MagicMock()
        mock_delete_response.raise_for_status = MagicMock()
        mock_delete.return_value = mock_delete_response

        result = self.client.delete_repository("repo-to-delete")

        assert result is True
        mock_get.assert_called_once()
        mock_delete.assert_called_once()


class TestPlatformDetection:
    """Test Git remote URL parsing for platform detection."""

    @patch("subprocess.run")
    def test_github_platform_detection(self, mock_run):
        """Test GitHub platform detection."""
        mock_run.return_value = MagicMock(
            returncode=0, stdout="origin\thttps://github.com/testuser/testrepo.git (fetch)\n"
        )

        result = extract_platform_info_from_git()

        assert result is not None
        assert result["platform"] == "github"
        assert result["owner"] == "testuser"
        # The regex captures .git in the repo name
        assert result["repo"] in ["testrepo", "testrepo.git"]

    @patch("subprocess.run")
    def test_azure_devops_https_platform_detection(self, mock_run):
        """Test Azure DevOps HTTPS platform detection."""
        mock_run.return_value = MagicMock(
            returncode=0,
            stdout="origin\thttps://dev.azure.com/testorg/testproject/_git/testrepo (fetch)\n",
        )

        result = extract_platform_info_from_git()

        assert result is not None
        assert result["platform"] == "azdo"
        assert result["organization"] == "testorg"
        assert result["project"] == "testproject"
        assert result["repository"] == "testrepo"

    @patch("subprocess.run")
    def test_azure_devops_ssh_platform_detection(self, mock_run):
        """Test Azure DevOps SSH platform detection."""
        mock_run.return_value = MagicMock(
            returncode=0,
            stdout="origin\tgit@ssh.dev.azure.com:v3/testorg/testproject/testrepo (fetch)\n",
        )

        result = extract_platform_info_from_git()

        assert result is not None
        assert result["platform"] == "azdo"
        assert result["organization"] == "testorg"
        assert result["project"] == "testproject"
        assert result["repository"] == "testrepo"

    @patch("subprocess.run")
    def test_unsupported_platform(self, mock_run):
        """Test unsupported platform detection."""
        mock_run.return_value = MagicMock(
            returncode=0, stdout="origin\thttps://gitlab.com/testuser/testrepo.git (fetch)\n"
        )

        result = extract_platform_info_from_git()

        assert result is None

    @patch("subprocess.run")
    def test_git_command_failure(self, mock_run):
        """Test handling of git command failure."""
        mock_run.return_value = MagicMock(returncode=1)

        result = extract_platform_info_from_git()

        assert result is None

    @patch("subprocess.run")
    def test_no_git_repository(self, mock_run):
        """Test handling when not in a git repository."""
        mock_run.side_effect = Exception("Not a git repository")

        result = extract_platform_info_from_git()

        assert result is None

    @patch("subprocess.run")
    def test_azure_remote_preference(self, mock_run):
        """Test that azure remote is preferred over origin."""
        mock_run.return_value = MagicMock(
            returncode=0,
            stdout="azure\thttps://dev.azure.com/testorg/testproject/_git/testrepo (fetch)\norigin\thttps://github.com/testuser/testrepo.git (fetch)\n",
        )

        result = extract_platform_info_from_git()

        assert result is not None
        assert result["platform"] == "azdo"
