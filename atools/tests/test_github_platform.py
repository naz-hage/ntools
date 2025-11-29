"""
Tests for GitHub platform functionality.
Tests GitHub CLI integration, repository operations, and error handling.
"""

import pytest
from unittest.mock import patch, MagicMock, call
import subprocess
import sys
from pathlib import Path

# Add the atools directory to sys.path
sys.path.insert(0, str(Path(__file__).parent.parent))

from sdo_package.platforms.github_platform import GitHubPlatform
from sdo_package.repositories import GitHubRepositoryPlatform


class TestGitHubPlatformInit:
    """Test GitHub platform initialization."""

    def test_platform_initialization(self):
        """Test basic platform initialization."""
        config = {"platform": "github", "owner": "testuser", "repo": "testrepo"}

        platform = GitHubPlatform(config)

        assert platform.config == config
        assert platform.verbose == False
        assert platform.config["owner"] == "testuser"
        assert platform.config["repo"] == "testrepo"

    def test_platform_initialization_verbose(self):
        """Test platform initialization with verbose mode."""
        platform = GitHubPlatform(verbose=True)

        assert platform.verbose == True


class TestGitHubPlatformAuthentication:
    """Test GitHub platform authentication."""

    def setup_method(self):
        """Set up test fixtures."""
        self.platform = GitHubPlatform()

    @patch("subprocess.run")
    def test_validate_auth_success(self, mock_run):
        """Test successful authentication validation."""
        mock_run.return_value = MagicMock(
            returncode=0, stdout="✓ Logged in to github.com as testuser", stderr=""
        )

        result = self.platform.validate_auth()

        assert result == True
        # The implementation calls subprocess.run with check=False
        mock_run.assert_called_once()
        call_args = mock_run.call_args
        assert call_args[0][0] == ["gh", "auth", "status"]
        assert call_args[1]["capture_output"] == True
        assert call_args[1]["text"] == True

    @patch("subprocess.run")
    def test_validate_auth_failure(self, mock_run):
        """Test authentication validation failure."""
        mock_run.return_value = MagicMock(
            returncode=1, stdout="", stderr="You are not logged in to any GitHub hosts"
        )

        result = self.platform.validate_auth()

        assert result == False

    @patch("subprocess.run")
    def test_validate_auth_gh_not_installed(self, mock_run):
        """Test authentication validation when gh CLI is not installed."""
        mock_run.side_effect = FileNotFoundError("gh: command not found")

        result = self.platform.validate_auth()

        assert result == False

    @patch("subprocess.run")
    def test_validate_auth_unexpected_error(self, mock_run):
        """Test authentication validation with unexpected error."""
        mock_run.side_effect = Exception("Unexpected error")

        result = self.platform.validate_auth()

        assert result == False


class TestGitHubRepositoryPlatformInit:
    """Test GitHub repository platform initialization."""

    def test_platform_initialization(self):
        """Test basic platform initialization."""
        config = {"platform": "github", "owner": "testuser", "repo": "testrepo"}

        platform = GitHubRepositoryPlatform(config)

        assert platform.config == config
        assert platform.verbose == False
        assert platform.config["owner"] == "testuser"
        assert platform.config["repo"] == "testrepo"

    def test_platform_initialization_verbose(self):
        """Test platform initialization with verbose mode."""
        config = {"platform": "github", "owner": "testuser"}
        platform = GitHubRepositoryPlatform(config, verbose=True)

        assert platform.verbose == True


class TestGitHubRepositoryPlatformAuth:
    """Test GitHub repository platform authentication."""

    def setup_method(self):
        """Set up test fixtures."""
        self.config = {"platform": "github", "owner": "testuser"}
        self.platform = GitHubRepositoryPlatform(self.config)

    @patch("subprocess.run")
    def test_validate_auth_success(self, mock_run):
        """Test successful authentication validation."""
        mock_run.return_value = MagicMock(returncode=0, stdout="", stderr="")

        result = self.platform.validate_auth()

        assert result == True
        mock_run.assert_called_once_with(["gh", "auth", "status"], capture_output=True, text=True)

    @patch("subprocess.run")
    def test_validate_auth_failure(self, mock_run):
        """Test authentication validation failure."""
        mock_run.return_value = MagicMock(returncode=1, stdout="", stderr="not authenticated")

        result = self.platform.validate_auth()

        assert result == False

    @patch("subprocess.run", side_effect=FileNotFoundError("gh: command not found"))
    def test_validate_auth_gh_not_installed(self, mock_run):
        """Test authentication validation when gh CLI is not installed."""
        result = self.platform.validate_auth()

        assert result == False

    @patch("subprocess.run", side_effect=Exception("Unexpected error"))
    def test_validate_auth_unexpected_error(self, mock_run):
        """Test authentication validation with unexpected error."""
        result = self.platform.validate_auth()

        assert result == False


class TestGitHubRepositoryPlatformOperations:
    """Test GitHub repository platform operations."""

    def setup_method(self):
        """Set up test fixtures."""
        self.config = {"platform": "github", "owner": "testuser"}
        self.platform = GitHubRepositoryPlatform(self.config)

    @patch("subprocess.run")
    def test_create_repository_success(self, mock_run):
        """Test successful repository creation."""
        mock_run.return_value = MagicMock(returncode=0, stdout="", stderr="")

        result = self.platform.create_repository("new-repo")

        assert result == True
        mock_run.assert_called_once_with(
            [
                "gh",
                "repo",
                "create",
                "testuser/new-repo",
                "--public",
                "--description",
                "Repository new-repo",
            ],
            capture_output=True,
            text=True,
        )

    @patch("subprocess.run")
    def test_create_repository_verbose(self, mock_run):
        """Test repository creation with verbose output."""
        self.platform.verbose = True
        mock_run.return_value = MagicMock(returncode=0, stdout="", stderr="")

        result = self.platform.create_repository("new-repo")

        assert result == True

    @patch("subprocess.run")
    def test_create_repository_failure(self, mock_run):
        """Test repository creation failure."""
        mock_run.return_value = MagicMock(
            returncode=1, stdout="", stderr="Repository already exists"
        )

        result = self.platform.create_repository("existing-repo")

        assert result == False

    @patch("subprocess.run")
    def test_create_repository_subprocess_error(self, mock_run):
        """Test repository creation with subprocess error."""
        mock_run.side_effect = Exception("Subprocess error")

        result = self.platform.create_repository("test-repo")

        assert result == False

    @patch("subprocess.run")
    @patch("json.loads")
    def test_get_repository_success(self, mock_json_loads, mock_run):
        """Test successful repository retrieval."""
        mock_repo_data = {
            "name": "test-repo",
            "description": "Test repository",
            "url": "https://github.com/testuser/test-repo",
            "createdAt": "2023-01-01T00:00:00Z",
            "updatedAt": "2023-01-02T00:00:00Z",
            "defaultBranchRef": {"name": "main"},
            "isPrivate": False,
        }
        mock_run.return_value = MagicMock(returncode=0, stdout='{"name": "test-repo"}', stderr="")
        mock_json_loads.return_value = mock_repo_data

        result = self.platform.get_repository("test-repo")

        assert result is not None
        assert result["name"] == "test-repo"
        assert result["description"] == "Test repository"
        assert result["webUrl"] == "https://github.com/testuser/test-repo"
        assert result["defaultBranch"] == "main"
        assert result["isPrivate"] == False

    @patch("subprocess.run")
    def test_get_repository_not_found(self, mock_run):
        """Test repository retrieval when repository doesn't exist."""
        mock_run.return_value = MagicMock(returncode=1, stdout="", stderr="Not Found")

        result = self.platform.get_repository("nonexistent-repo")

        assert result is None

    @patch("subprocess.run")
    def test_get_repository_command_failure(self, mock_run):
        """Test repository retrieval with command failure."""
        mock_run.return_value = MagicMock(returncode=1, stdout="", stderr="Permission denied")

        result = self.platform.get_repository("private-repo")

        assert result is None

    @patch("subprocess.run")
    @patch("json.loads")
    def test_list_repositories_success(self, mock_json_loads, mock_run):
        """Test successful repository listing."""
        mock_repos_data = [
            {
                "name": "repo1",
                "description": "First repo",
                "url": "https://github.com/testuser/repo1",
                "createdAt": "2023-01-01T00:00:00Z",
                "updatedAt": "2023-01-02T00:00:00Z",
                "defaultBranchRef": {"name": "main"},
                "isPrivate": False,
            },
            {
                "name": "repo2",
                "description": "Second repo",
                "url": "https://github.com/testuser/repo2",
                "createdAt": "2023-01-03T00:00:00Z",
                "updatedAt": "2023-01-04T00:00:00Z",
                "defaultBranchRef": {"name": "develop"},
                "isPrivate": True,
            },
        ]
        mock_run.return_value = MagicMock(
            returncode=0, stdout='[{"name": "repo1"}, {"name": "repo2"}]', stderr=""
        )
        mock_json_loads.return_value = mock_repos_data

        result = self.platform.list_repositories()

        assert result is not None
        assert len(result) == 2
        assert result[0]["name"] == "repo1"
        assert result[0]["isPrivate"] == False
        assert result[1]["name"] == "repo2"
        assert result[1]["isPrivate"] == True

    @patch("subprocess.run")
    def test_list_repositories_failure(self, mock_run):
        """Test repository listing failure."""
        mock_run.return_value = MagicMock(returncode=1, stdout="", stderr="Network error")

        result = self.platform.list_repositories()

        assert result is None

    @patch("subprocess.run")
    def test_delete_repository_success(self, mock_run):
        """Test successful repository deletion."""
        mock_run.return_value = MagicMock(returncode=0, stdout="", stderr="")

        result = self.platform.delete_repository("old-repo")

        assert result == True
        mock_run.assert_called_once_with(
            ["gh", "repo", "delete", "testuser/old-repo", "--yes"], capture_output=True, text=True
        )

    @patch("subprocess.run")
    def test_delete_repository_failure(self, mock_run):
        """Test repository deletion failure."""
        mock_run.return_value = MagicMock(returncode=1, stdout="", stderr="Repository not found")

        result = self.platform.delete_repository("nonexistent-repo")

        assert result == False

    @patch("subprocess.run")
    def test_delete_repository_subprocess_error(self, mock_run):
        """Test repository deletion with subprocess error."""
        mock_run.side_effect = Exception("Subprocess error")

        result = self.platform.delete_repository("test-repo")

        assert result == False
