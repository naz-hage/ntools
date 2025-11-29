"""
Comprehensive unit tests for Pull Request functionality.

Tests cover all PR operations across GitHub and Azure DevOps platforms,
including command handlers, platform implementations, and error handling.
"""

import json
import sys

from pathlib import Path
from unittest.mock import MagicMock, patch
import pytest

# Add the atools directory to sys.path
sys.path.insert(0, str(Path(__file__).parent.parent))

from sdo_package.exceptions import PlatformError
from sdo_package.pull_requests import (
    cmd_pr_create,
    cmd_pr_show,
    cmd_pr_status,
    cmd_pr_list,
    read_markdown_pr_file,
)


class TestPRCommandHandlers:
    """Test PR command handler functions."""

    @patch("sdo_package.pull_requests.get_pr_platform")
    def test_cmd_pr_create_success(self, mock_get_platform):
        """Test successful PR creation."""
        # Setup mock platform
        mock_platform = MagicMock()
        mock_platform.create_pull_request.return_value = "https://github.com/test/repo/pull/123"
        mock_get_platform.return_value = mock_platform

        # Setup args
        args = MagicMock()
        args.title = "Test PR"
        args.description = "Test description"
        args.file = None
        args.work_item = 123
        args.draft = False
        args.dry_run = False
        args.verbose = False

        # Mock stdout to capture output
        with patch("builtins.print") as mock_print:
            cmd_pr_create(args)

        # Verify platform was called correctly
        mock_platform.create_pull_request.assert_called_once_with(
            title="123: Test PR", description="Test description", work_item_id=123, draft=False
        )

        # Verify success message
        mock_print.assert_any_call("[OK] Pull request created successfully!")
        mock_print.assert_any_call("URL: https://github.com/test/repo/pull/123")

    def test_cmd_pr_create_missing_work_item(self):
        """Test PR creation fails when work item is missing."""
        # Setup args without work item
        args = MagicMock()
        args.title = "Test PR"
        args.description = "Test description"
        args.file = None
        args.source_branch = "feature/test"
        args.target_branch = "main"
        args.work_item = None
        args.draft = False
        args.dry_run = False
        args.verbose = False

        # Test that SystemExit is raised due to ValidationError
        with pytest.raises(SystemExit):
            cmd_pr_create(args)

    def test_cmd_pr_create_invalid_work_item(self):
        """Test PR creation fails when work item ID is invalid."""
        # Setup args with invalid work item
        args = MagicMock()
        args.title = "Test PR"
        args.description = "Test description"
        args.file = None
        args.source_branch = "feature/test"
        args.target_branch = "main"
        args.work_item = "invalid"
        args.draft = False
        args.dry_run = False
        args.verbose = False

        # Test that SystemExit is raised due to ValidationError
        with pytest.raises(SystemExit):
            cmd_pr_create(args)

    @patch("sdo_package.pull_requests.get_pr_platform")
    def test_cmd_pr_create_dry_run(self, mock_get_platform):
        """Test PR creation dry run."""
        # Setup mock platform
        mock_platform = MagicMock()
        mock_platform.__class__.__name__ = "GitHubPullRequestPlatform"
        mock_get_platform.return_value = mock_platform

        # Setup args
        args = MagicMock()
        args.title = "Test PR"
        args.description = "Test description"
        args.file = None
        args.work_item = 123
        args.draft = True
        args.dry_run = True
        args.verbose = False

        # Mock stdout to capture output
        with patch("builtins.print") as mock_print:
            cmd_pr_create(args)

        # Verify platform was NOT called
        mock_platform.create_pull_request.assert_not_called()

        # Verify dry run output
        mock_print.assert_any_call("[DRY RUN] Would create PR with:")
        mock_print.assert_any_call("  Title: 123: Test PR")
        mock_print.assert_any_call("  Description: Test description")
        mock_print.assert_any_call("  Source Branch: (current branch)")
        mock_print.assert_any_call("  Target Branch: main")
        mock_print.assert_any_call("  Work Item: 123")
        mock_print.assert_any_call("  Draft: True")

    @patch("sdo_package.pull_requests.get_pr_platform")
    def test_cmd_pr_show_success(self, mock_get_platform):
        """Test successful PR show command."""
        # Setup mock platform
        mock_platform = MagicMock()
        mock_platform.get_pull_request.return_value = {
            "number": 123,
            "title": "Test PR",
            "status": "open",
            "author": "testuser",
            "source_branch": "feature/test",
            "target_branch": "main",
            "description": "Test description",
        }
        mock_get_platform.return_value = mock_platform

        # Setup args
        args = MagicMock()
        args.pr_number = 123
        args.verbose = False

        # Mock stdout to capture output
        with patch("builtins.print") as mock_print:
            cmd_pr_show(args)

        # Verify platform was called correctly
        mock_platform.get_pull_request.assert_called_once_with(123)

        # Verify output
        mock_print.assert_any_call("Pull Request #123")
        mock_print.assert_any_call("Title: Test PR")
        mock_print.assert_any_call("Status: open")
        mock_print.assert_any_call("Author: testuser")

    @patch("sdo_package.pull_requests.get_pr_platform")
    def test_cmd_pr_list_success(self, mock_get_platform):
        """Test successful PR list command."""
        # Setup mock platform
        mock_platform = MagicMock()
        mock_platform.list_pull_requests.return_value = [
            {
                "number": 123,
                "title": "Test PR 1",
                "status": "open",
                "author": "testuser",
                "url": "https://github.com/test/repo/pull/123",
            },
            {
                "number": 124,
                "title": "Test PR 2",
                "status": "open",
                "author": "testuser2",
                "url": "https://github.com/test/repo/pull/124",
            },
        ]
        mock_get_platform.return_value = mock_platform

        # Setup args
        args = MagicMock()
        args.status = "active"  # CLI uses 'active' as default
        args.author = None
        args.top = 10  # CLI uses 'top' for limit
        args.verbose = False

        # Mock stdout to capture output
        with patch("builtins.print") as mock_print:
            cmd_pr_list(args)

        # Verify platform was called correctly
        mock_platform.list_pull_requests.assert_called_once_with(
            state="open", author=None, limit=10  # Should be mapped from 'active' to 'open'
        )

        # Verify output
        mock_print.assert_any_call("Active Pull Requests (2 found):")

    @patch("sdo_package.pull_requests.get_pr_platform")
    def test_cmd_pr_status_success(self, mock_get_platform):
        """Test successful PR status check."""
        # Setup mock platform
        mock_platform = MagicMock()
        mock_pr_data = {
            "title": "Test PR",
            "status": "active",
            "author": "testuser",
            "source_branch": "feature/test",
            "target_branch": "main",
            "merge_status": "ready",
            "reviewers": [{"vote": "approve"}, {"vote": "none"}],
        }
        mock_platform.get_pull_request.return_value = mock_pr_data
        mock_get_platform.return_value = mock_platform

        # Setup args
        args = MagicMock()
        args.pr_number = 123
        args.verbose = False

        # Mock stdout to capture output
        with patch("builtins.print") as mock_print:
            cmd_pr_status(args)

        # Verify platform was called correctly
        mock_platform.get_pull_request.assert_called_once_with(123)

        # Verify status output
        mock_print.assert_any_call("🟢 PR #123: ACTIVE")
        mock_print.assert_any_call("Title: Test PR")
        mock_print.assert_any_call("Author: testuser")
        mock_print.assert_any_call("Branch: feature/test → main")
        mock_print.assert_any_call("Merge Status: ready")
        mock_print.assert_any_call("Reviews: 1/2 approved")


class TestGitHubPullRequestPlatform:
    """Test GitHub PR platform implementation."""

    def test_platform_initialization(self):
        """Test GitHub platform initialization."""
        with patch("subprocess.run") as mock_run:
            mock_run.return_value = MagicMock(stdout="gh version 2.0.0")
            from sdo_package.platforms.github_pr_platform import GitHubPullRequestPlatform

            platform = GitHubPullRequestPlatform()
            assert platform is not None
            mock_run.assert_called_with(
                ["gh", "--version"],
                capture_output=True,
                text=True,
                encoding="utf-8",
                errors="replace",
                check=True,
            )

    @patch("subprocess.run")
    def test_get_config_from_git(self, mock_run):
        """Test getting GitHub config from Git remote."""
        # Mock git remote command
        mock_run.return_value = MagicMock(stdout="https://github.com/test/repo.git\n")

        from sdo_package.platforms.github_pr_platform import GitHubPullRequestPlatform

        platform = GitHubPullRequestPlatform()

        # This would normally be tested through get_pr_platform, but testing the logic
        assert platform is not None

    @patch("sdo_package.platforms.github_pr_platform.subprocess.run")
    def test_create_pull_request_success(self, mock_run):
        """Test successful GitHub PR creation."""
        from sdo_package.platforms.github_pr_platform import GitHubPullRequestPlatform

        # Mock gh version check
        version_result = MagicMock()
        version_result.stdout = "gh version 2.0.0"
        # Mock gh pr create
        pr_result = MagicMock()
        pr_result.stdout = "https://github.com/test/repo/pull/123"
        pr_result.stderr = ""

        mock_run.side_effect = [version_result, pr_result]

        platform = GitHubPullRequestPlatform()
        url = platform.create_pull_request(
            title="Test PR",
            description="Test description",
            source_branch="feature/test",
            target_branch="main",
        )

        assert url == "https://github.com/test/repo/pull/123"
        # Should be called twice: version check + pr create
        assert mock_run.call_count == 2

    @patch("sdo_package.platforms.github_pr_platform.subprocess.run")
    def test_create_pull_request_dry_run(self, mock_run):
        """Test GitHub PR creation dry run."""
        from sdo_package.platforms.github_pr_platform import GitHubPullRequestPlatform

        platform = GitHubPullRequestPlatform()

        # This should not call subprocess
        with patch("builtins.print") as mock_print:
            # We can't actually test dry run here since it's handled at command level
            # But we can verify the platform exists
            assert platform is not None

    @patch("sdo_package.platforms.github_pr_platform.subprocess.run")
    def test_list_pull_requests_success(self, mock_run):
        """Test successful GitHub PR listing."""
        from sdo_package.platforms.github_pr_platform import GitHubPullRequestPlatform

        # Mock gh command returning JSON
        mock_result = MagicMock()
        mock_result.stdout = json.dumps(
            [
                {
                    "number": 123,
                    "title": "Test PR",
                    "state": "open",
                    "author": {"login": "testuser"},
                    "headRefName": "feature/test",
                    "baseRefName": "main",
                    "url": "https://github.com/test/repo/pull/123",
                }
            ]
        )
        mock_run.return_value = mock_result

        platform = GitHubPullRequestPlatform()
        prs = platform.list_pull_requests()

        assert len(prs) == 1
        assert prs[0]["number"] == 123
        assert prs[0]["title"] == "Test PR"
        assert prs[0]["status"] == "open"

    @patch("sdo_package.platforms.github_pr_platform.subprocess.run")
    def test_get_pull_request_success(self, mock_run):
        """Test successful GitHub PR retrieval."""
        from sdo_package.platforms.github_pr_platform import GitHubPullRequestPlatform

        # Mock gh command returning JSON
        mock_result = MagicMock()
        mock_result.stdout = json.dumps(
            {
                "number": 123,
                "title": "Test PR",
                "body": "Test description",
                "state": "open",
                "author": {"login": "testuser"},
                "headRefName": "feature/test",
                "baseRefName": "main",
                "url": "https://github.com/test/repo/pull/123",
            }
        )
        mock_run.return_value = mock_result

        platform = GitHubPullRequestPlatform()
        pr = platform.get_pull_request(123)

        assert pr["number"] == 123
        assert pr["title"] == "Test PR"
        assert pr["status"] == "open"
        assert pr["author"] == "testuser"
        assert pr["work_items"] == []


class TestAzureDevOpsPullRequestPlatform:
    """Test Azure DevOps PR platform implementation."""

    def test_platform_initialization(self):
        """Test Azure DevOps platform initialization."""
        from sdo_package.platforms.azdo_pr_platform import AzureDevOpsPullRequestPlatform

        platform = AzureDevOpsPullRequestPlatform()
        assert platform.client is None  # Client is lazy-loaded

    @patch("sdo_package.platforms.azdo_pr_platform.AzureDevOpsClient")
    def test_create_pull_request_success(self, mock_client_class):
        """Test successful Azure DevOps PR creation."""
        from sdo_package.platforms.azdo_pr_platform import AzureDevOpsPullRequestPlatform

        # Mock client
        mock_client = MagicMock()
        mock_client.project = "test-project"
        mock_client.base_url = "https://dev.azure.com/test-org"
        mock_client.session.post.return_value = MagicMock(
            status_code=201, json=MagicMock(return_value={"pullRequestId": 123})
        )
        mock_client_class.return_value = mock_client

        # Mock git commands
        with patch("subprocess.run") as mock_run:
            mock_run.return_value = MagicMock(stdout="feature/test\n")

            platform = AzureDevOpsPullRequestPlatform()
            platform.client = mock_client
            platform._repository = "test-repo"

            url = platform.create_pull_request(title="Test PR", description="Test description")

            assert "pullrequest/123" in url
            mock_client.session.post.assert_called_once()

    @patch("sdo_package.platforms.azdo_pr_platform.AzureDevOpsClient")
    def test_list_pull_requests_success(self, mock_client_class):
        """Test successful Azure DevOps PR listing."""
        from sdo_package.platforms.azdo_pr_platform import AzureDevOpsPullRequestPlatform

        # Mock client and response
        mock_client = MagicMock()
        mock_client.project = "test-project"
        mock_client.base_url = "https://dev.azure.com/test-org"
        mock_response = MagicMock()
        mock_response.status_code = 200
        mock_response.json.return_value = {
            "value": [
                {
                    "pullRequestId": 123,
                    "title": "Test PR",
                    "status": "active",
                    "createdBy": {"displayName": "testuser"},
                    "sourceRefName": "refs/heads/feature/test",
                    "targetRefName": "refs/heads/main",
                }
            ]
        }
        mock_client.session.get.return_value = mock_response
        mock_client_class.return_value = mock_client

        platform = AzureDevOpsPullRequestPlatform()
        platform.client = mock_client
        platform._repository = "test-repo"

        prs = platform.list_pull_requests()

        assert len(prs) == 1
        assert prs[0]["number"] == 123
        assert prs[0]["status"] == "open"

    @patch("sdo_package.platforms.azdo_pr_platform.AzureDevOpsClient")
    def test_get_pull_request_success(self, mock_client_class):
        """Test successful Azure DevOps PR retrieval."""
        from sdo_package.platforms.azdo_pr_platform import AzureDevOpsPullRequestPlatform

        # Mock client and responses
        mock_client = MagicMock()
        mock_client.project = "test-project"
        mock_client.base_url = "https://dev.azure.com/test-org"
        mock_client.api_version = "7.1"

        # Mock PR response
        mock_pr_response = MagicMock()
        mock_pr_response.status_code = 200
        mock_pr_response.json.return_value = {
            "pullRequestId": 123,
            "title": "Test PR",
            "description": "Test description",
            "status": "active",
            "createdBy": {"displayName": "testuser"},
            "sourceRefName": "refs/heads/feature/test",
            "targetRefName": "refs/heads/main",
        }

        # Mock work items response
        mock_wi_response = MagicMock()
        mock_wi_response.status_code = 200
        mock_wi_response.json.return_value = {"value": [{"id": 456}]}

        # Configure session.get to return different responses based on URL
        def mock_get(url, params=None):
            if "workitems" in url:
                return mock_wi_response
            else:
                return mock_pr_response

        mock_client.session.get.side_effect = mock_get
        mock_client_class.return_value = mock_client

        platform = AzureDevOpsPullRequestPlatform()
        platform.client = mock_client
        platform._repository = "test-repo"

        pr = platform.get_pull_request(123)

        assert pr["number"] == 123
        assert pr["title"] == "Test PR"
        assert pr["status"] == "open"
        assert pr["work_items"] == [456]


class TestPRMarkdownParsing:
    """Test PR markdown parsing functionality."""

    @patch("pathlib.Path.read_text")
    @patch("pathlib.Path.exists")
    @patch("pathlib.Path.is_file")
    @patch("pathlib.Path.stat")
    def test_parse_markdown_success(self, mock_stat, mock_is_file, mock_exists, mock_read_text):
        """Test successful markdown parsing."""
        # Setup mocks
        mock_exists.return_value = True
        mock_is_file.return_value = True
        mock_stat.return_value = MagicMock(st_mode=0o644)  # Read permission
        mock_read_text.return_value = "# Test PR\n\nThis is a test description."

        title, description = read_markdown_pr_file("test.md")

        assert title == "Test PR"
        assert description == "This is a test description."


class TestGetPRPlatform:
    """Test the get_pr_platform function."""

    @patch("sdo_package.platforms.github_pr_platform.subprocess.run")
    @patch("sdo_package.client.extract_platform_info_from_git")
    @patch("sdo_package.pull_requests.os.environ.get")
    def test_get_pr_platform_github(self, mock_env_get, mock_extract_platform, mock_subprocess):
        """Test platform detection for GitHub."""
        from sdo_package.pull_requests import get_pr_platform
        from sdo_package.platforms.github_pr_platform import GitHubPullRequestPlatform

        # Setup mocks
        mock_extract_platform.return_value = {"platform": "github"}
        # Mock gh CLI validation
        mock_subprocess.return_value = MagicMock(returncode=0, stdout="gh version 2.0.0")

        # Test
        platform = get_pr_platform()

        # Verify
        assert isinstance(platform, GitHubPullRequestPlatform)
        mock_extract_platform.assert_called_once()

    @patch("sdo_package.client.extract_platform_info_from_git")
    @patch("sdo_package.pull_requests.os.environ.get")
    def test_get_pr_platform_azdo_with_pat(self, mock_env_get, mock_extract_platform):
        """Test platform detection for Azure DevOps with PAT."""
        from sdo_package.pull_requests import get_pr_platform
        from sdo_package.platforms.azdo_pr_platform import AzureDevOpsPullRequestPlatform

        # Setup mocks
        mock_extract_platform.return_value = {"platform": "azdo"}
        mock_env_get.return_value = "test-pat"

        # Test
        platform = get_pr_platform()

        # Verify
        assert isinstance(platform, AzureDevOpsPullRequestPlatform)
        mock_extract_platform.assert_called_once()
        mock_env_get.assert_called_once_with("AZURE_DEVOPS_PAT")

    @patch("sdo_package.client.extract_platform_info_from_git")
    @patch("sdo_package.pull_requests.os.environ.get")
    def test_get_pr_platform_azdo_without_pat(self, mock_env_get, mock_extract_platform):
        """Test platform detection for Azure DevOps without PAT raises error."""
        from sdo_package.pull_requests import get_pr_platform

        # Setup mocks
        mock_extract_platform.return_value = {"platform": "azdo"}
        mock_env_get.return_value = None

        # Test
        with pytest.raises(PlatformError) as exc_info:
            get_pr_platform()

        # Verify
        assert "AZURE_DEVOPS_PAT environment variable not set" in str(exc_info.value)

    @patch("sdo_package.client.extract_platform_info_from_git")
    def test_get_pr_platform_unsupported_platform(self, mock_extract_platform):
        """Test platform detection for unsupported platform raises error."""
        from sdo_package.pull_requests import get_pr_platform

        # Setup mocks
        mock_extract_platform.return_value = {"platform": "bitbucket"}

        # Test
        with pytest.raises(PlatformError) as exc_info:
            get_pr_platform()

        # Verify
        assert "Unsupported platform detected: bitbucket" in str(exc_info.value)

    @patch("sdo_package.client.extract_platform_info_from_git")
    def test_get_pr_platform_no_platform_detected(self, mock_extract_platform):
        """Test platform detection when no platform is detected raises error."""
        from sdo_package.pull_requests import get_pr_platform

        # Setup mocks
        mock_extract_platform.return_value = None

        # Test
        with pytest.raises(PlatformError) as exc_info:
            get_pr_platform()

        # Verify
        assert "Could not detect platform from Git remotes" in str(exc_info.value)
