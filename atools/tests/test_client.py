"""
Tests for Azure DevOps client functionality.
"""

from unittest.mock import patch, MagicMock
from pathlib import Path
import sys

# Add the atools directory to sys.path
sys.path.insert(0, str(Path(__file__).parent.parent))

from sdo_package.client import extract_platform_info_from_git


class TestExtractPlatformInfoFromGit:
    """Test the extract_platform_info_from_git function."""

    @patch("subprocess.run")
    def test_github_repository(self, mock_run):
        """Test GitHub repository extraction."""
        # Mock git remote output for GitHub
        mock_result = MagicMock()
        mock_result.returncode = 0
        mock_result.stdout = (
            "origin\thttps://github.com/naz-hage/ntools (fetch)\n"
            "origin\thttps://github.com/naz-hage/ntools (push)\n"
        )
        mock_result.stderr = ""
        mock_run.return_value = mock_result

        result = extract_platform_info_from_git()
        expected = {
            "platform": "github",
            "owner": "naz-hage",
            "repo": "ntools",
            "remote_url": "https://github.com/naz-hage/ntools",
        }
        assert result == expected

    @patch("subprocess.run")
    def test_non_git_directory(self, mock_run):
        """Test that non-git directories return None."""
        # Mock git command failure
        mock_result = MagicMock()
        mock_result.returncode = 128  # Git error code
        mock_result.stdout = ""
        mock_result.stderr = "fatal: not a git repository"
        mock_run.return_value = mock_result

        result = extract_platform_info_from_git()
        assert result is None

    @patch("subprocess.run")
    def test_azure_devops_https_url(self, mock_run):
        """Test Azure DevOps HTTPS URL extraction."""
        # Mock git remote output for Azure DevOps HTTPS
        mock_result = MagicMock()
        mock_result.returncode = 0
        mock_result.stdout = "origin\thttps://dev.azure.com/myorg/myproject/_git/myrepo (fetch)\n"
        mock_result.stderr = ""
        mock_run.return_value = mock_result

        result = extract_platform_info_from_git()
        expected = {
            "platform": "azdo",
            "organization": "myorg",
            "project": "myproject",
            "repository": "myrepo",
            "remote_url": "https://dev.azure.com/myorg/myproject/_git/myrepo",
        }
        assert result == expected

    @patch("subprocess.run")
    def test_azure_devops_ssh_url(self, mock_run):
        """Test Azure DevOps SSH URL extraction."""
        # Mock git remote output for Azure DevOps SSH
        mock_result = MagicMock()
        mock_result.returncode = 0
        mock_result.stdout = "origin\tgit@ssh.dev.azure.com:v3/myorg/myproject/myrepo (fetch)\n"
        mock_result.stderr = ""
        mock_run.return_value = mock_result

        result = extract_platform_info_from_git()
        expected = {
            "platform": "azdo",
            "organization": "myorg",
            "project": "myproject",
            "repository": "myrepo",
            "remote_url": "git@ssh.dev.azure.com:v3/myorg/myproject/myrepo",
        }
        assert result == expected

    @patch("subprocess.run")
    def test_multiple_remotes_github_first(self, mock_run):
        """Test multiple remotes with GitHub first."""
        # Mock git remote output with multiple remotes, GitHub first
        mock_result = MagicMock()
        mock_result.returncode = 0
        mock_result.stdout = """origin\thttps://github.com/user/repo (fetch)
upstream\thttps://dev.azure.com/myorg/myproject/_git/myrepo (fetch)
"""
        mock_result.stderr = ""
        mock_run.return_value = mock_result

        result = extract_platform_info_from_git()
        expected = {
            "platform": "github",
            "owner": "user",
            "repo": "repo",
            "remote_url": "https://github.com/user/repo",
        }
        assert result == expected

    @patch("subprocess.run")
    def test_unsupported_platform(self, mock_run):
        """Test repository with unsupported platform remotes."""
        # Mock git remote output with GitLab
        mock_result = MagicMock()
        mock_result.returncode = 0
        mock_result.stdout = "origin\thttps://gitlab.com/user/repo (fetch)\n"
        mock_result.stderr = ""
        mock_run.return_value = mock_result

        result = extract_platform_info_from_git()
        assert result is None

    @patch("subprocess.run")
    def test_git_command_exception(self, mock_run):
        """Test handling of subprocess exceptions."""
        # Mock subprocess.run raising an exception
        mock_run.side_effect = OSError("Command not found")

        result = extract_platform_info_from_git()
        assert result is None
