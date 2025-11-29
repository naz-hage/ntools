"""
Tests for SDO work items business logic.
"""

import pytest
from unittest.mock import patch, MagicMock
import sys
from pathlib import Path

# Add the atools directory to sys.path
sys.path.insert(0, str(Path(__file__).parent.parent))

from sdo_package.work_items import WorkItemManager, WorkItemResult  # noqa: E402
from sdo_package.exceptions import PlatformError, ParsingError  # noqa: E402


class TestWorkItemManager:
    """Test the WorkItemManager class."""

    def setup_method(self):
        """Set up test fixtures."""
        self.manager = WorkItemManager(verbose=False)

    def test_work_item_manager_init(self):
        """Test WorkItemManager initialization."""
        assert self.manager is not None
        assert hasattr(self.manager, "verbose")
        assert self.manager.verbose is False

        # Test verbose initialization
        verbose_manager = WorkItemManager(verbose=True)
        assert verbose_manager.verbose is True

    @patch("sdo_package.work_items.os.path.exists", return_value=True)
    @patch("sdo_package.parsers.markdown_parser.MarkdownParser")
    @patch("sdo_package.platforms.azdo_platform.AzureDevOpsPlatform")
    def test_create_work_item_success(self, mock_platform, mock_markdown_parser, mock_exists):
        """Test successful work item creation."""
        # Mock parser results - MarkdownParser now returns everything including metadata
        mock_markdown_instance = mock_markdown_parser.return_value
        mock_markdown_instance.parse_file.return_value = {
            "title": "Test Issue",
            "description": "Test description",
            "acceptance_criteria": ["Criteria 1", "Criteria 2"],
            "metadata": {
                "target": "azdo",  # Use 'target' as it comes from MarkdownParser
                "project": "TestProject",
                "organization": "TestOrg",
                "pat": "test_pat",
            },
        }

        # Mock platform creation - return dict with id and url
        mock_platform_instance = mock_platform.return_value
        mock_platform_instance.create_work_item.return_value = {
            "id": 123,
            "url": "https://example.com/123",
        }

        # Test work item creation
        result = self.manager.create_work_item("test.md")

        assert isinstance(result, WorkItemResult)
        assert result.success is True
        assert result.work_item_id == "123"
        assert result.url == "https://example.com/123"

        # Verify method calls
        mock_markdown_instance.parse_file.assert_called_once_with("test.md")
        mock_platform_instance.create_work_item.assert_called_once()

    @patch("sdo_package.work_items.os.path.exists", return_value=True)
    @patch("sdo_package.parsers.markdown_parser.MarkdownParser")
    def test_create_work_item_parsing_error(self, mock_markdown_parser, mock_exists):
        """Test work item creation with parsing error."""
        # Mock parsing error
        mock_markdown_instance = mock_markdown_parser.return_value
        mock_markdown_instance.parse_file.side_effect = ParsingError("Invalid markdown")

        # Test that parsing error is handled
        result = self.manager.create_work_item("test.md")

        assert isinstance(result, WorkItemResult)
        assert result.success is False
        assert "Invalid markdown" in result.error_message

    @patch("sdo_package.work_items.os.path.exists", return_value=True)
    @patch("sdo_package.parsers.markdown_parser.MarkdownParser")
    @patch("sdo_package.platforms.azdo_platform.AzureDevOpsPlatform")
    def test_create_work_item_platform_error(
        self, mock_platform, mock_markdown_parser, mock_exists
    ):
        """Test work item creation with platform error."""
        # Mock successful parsing
        mock_markdown_instance = mock_markdown_parser.return_value
        mock_markdown_instance.parse_file.return_value = {
            "title": "Test Issue",
            "description": "Test description",
            "acceptance_criteria": ["Criteria 1"],
            "metadata": {
                "target": "azdo",
                "project": "TestProject",
                "organization": "TestOrg",
                "pat": "test_pat",
            },
        }

        # Mock platform error
        mock_platform_instance = mock_platform.return_value
        mock_platform_instance.create_work_item.side_effect = PlatformError("API error")

        # Test that platform error is handled
        result = self.manager.create_work_item("test.md")

        assert isinstance(result, WorkItemResult)
        assert result.success is False
        assert "API error" in result.error_message

    def test_create_work_item_invalid_file(self):
        """Test work item creation with invalid file."""
        result = self.manager.create_work_item("nonexistent.md")

        assert isinstance(result, WorkItemResult)
        assert result.success is False
        assert "file" in result.error_message.lower() or "not found" in result.error_message.lower()

    @patch("sdo_package.work_items.os.path.exists", return_value=True)
    @patch("sdo_package.parsers.markdown_parser.MarkdownParser")
    def test_create_work_item_unknown_platform(self, mock_markdown_parser, mock_exists):
        """Test work item creation with unknown platform (defaults to Azure DevOps)."""
        # Mock parsing results - unknown platform defaults to Azure DevOps
        mock_markdown_instance = mock_markdown_parser.return_value
        mock_markdown_instance.parse_file.return_value = {
            "title": "Test Issue",
            "description": "Test description",
            "acceptance_criteria": ["Criteria 1"],
            "metadata": {"target": "unknown_platform"},  # This defaults to Azure DevOps
        }

        # Test that it defaults to Azure DevOps but fails due to missing config
        result = self.manager.create_work_item("test.md")

        assert isinstance(result, WorkItemResult)
        assert result.success is False
        assert "azure devops configuration" in result.error_message.lower()


class TestWorkItemResult:
    """Test the WorkItemResult class."""

    def test_successful_result_creation(self):
        """Test creating a successful result."""
        result = WorkItemResult(
            success=True, work_item_id="123", url="https://example.com/123", platform="azure_devops"
        )

        assert result.success is True
        assert result.work_item_id == "123"
        assert result.url == "https://example.com/123"
        assert result.platform == "azure_devops"
        assert result.error_message is None

    def test_failed_result_creation(self):
        """Test creating a failed result."""
        result = WorkItemResult(success=False, error_message="Test error")

        assert result.success is False
        assert result.error_message == "Test error"
        assert result.work_item_id is None
        assert result.url is None
        assert result.platform is None

    def test_result_string_representation(self):
        """Test string representation of results."""
        success_result = WorkItemResult(
            success=True, work_item_id="123", url="https://example.com/123", platform="azure_devops"
        )

        result_str = str(success_result)
        assert "123" in result_str
        assert "success" in result_str.lower()

        failed_result = WorkItemResult(success=False, error_message="Test error")

        result_str = str(failed_result)
        assert "error" in result_str.lower()
        assert "Test error" in result_str


class TestWorkItemCommands:
    """Test the work item command functions (list, show, update, comment)."""

    @patch("sdo_package.client.extract_platform_info_from_git")
    @patch("sdo_package.client.AzureDevOpsClient")
    @patch.dict("os.environ", {"AZURE_DEVOPS_PAT": "test-pat"})
    def test_cmd_workitem_list_azdo(self, mock_client_class, mock_extract):
        """Test workitem list command for Azure DevOps."""
        # Mock platform detection
        mock_extract.return_value = {
            "platform": "azdo",
            "organization": "test-org",
            "project": "test-project",
            "repository": "test-repo",
        }

        # Mock client and work items
        mock_client = mock_client_class.return_value
        mock_client.list_work_items.return_value = [
            {
                "id": 123,
                "fields": {
                    "System.Title": "Test Item",
                    "System.State": "New",
                    "System.WorkItemType": "Task",
                    "System.AssignedTo": {"displayName": "John Doe"},
                    "System.IterationPath": "Sprint 1",
                },
            },
            {
                "id": 124,
                "fields": {
                    "System.Title": "Another Item",
                    "System.State": "Done",
                    "System.WorkItemType": "Bug",
                    "System.IterationPath": "Sprint 2",
                },
            },
        ]

        # Import and test
        from sdo_package.work_items import cmd_workitem_list

        # Create mock args
        args = MagicMock()
        args.top = 10
        args.work_item_type = None
        args.state = None
        args.assignee = None
        args.verbose = False

        # Should not raise exception
        cmd_workitem_list(args)

        # Verify API calls
        mock_client.list_work_items.assert_called_once()

    @patch("sdo_package.client.extract_platform_info_from_git")
    @patch("sdo_package.work_items.subprocess.run")
    def test_cmd_workitem_list_github(self, mock_subprocess, mock_extract):
        """Test workitem list command for GitHub."""
        # Mock platform detection
        mock_extract.return_value = {
            "platform": "github",
            "owner": "test-owner",
            "repo": "test-repo",
        }

        # Mock GitHub CLI response
        mock_result = MagicMock()
        mock_result.returncode = 0
        mock_result.stdout = (
            '[{"number":1,"title":"Test Issue","state":"OPEN",'
            '"labels":[{"name":"bug"}],"assignees":[{"login":"user1"}]}]'
        )
        mock_subprocess.return_value = mock_result

        # Import and test
        from sdo_package.work_items import cmd_workitem_list

        # Create mock args
        args = MagicMock()
        args.top = 10
        args.work_item_type = None
        args.state = None
        args.assignee = None
        args.verbose = False

        # Should not raise exception
        cmd_workitem_list(args)

        # Verify subprocess call
        assert mock_subprocess.called

    @patch("sdo_package.client.extract_platform_info_from_git")
    @patch("sdo_package.client.AzureDevOpsClient")
    @patch.dict("os.environ", {"AZURE_DEVOPS_PAT": "test-pat"})
    def test_cmd_workitem_show_azdo(self, mock_client_class, mock_extract):
        """Test workitem show command for Azure DevOps."""
        # Mock platform detection
        mock_extract.return_value = {
            "platform": "azdo",
            "organization": "test-org",
            "project": "test-project",
            "repository": "test-repo",
        }

        # Mock client and work item
        mock_client = mock_client_class.return_value
        mock_client.get_work_item.return_value = {
            "id": 123,
            "fields": {
                "System.Title": "Test Work Item",
                "System.State": "In Progress",
                "System.WorkItemType": "Task",
                "System.Description": "Test description",
                "System.AssignedTo": {"displayName": "John Doe"},
                "System.CreatedDate": "2025-01-01T10:00:00Z",
                "System.ChangedDate": "2025-01-02T10:00:00Z",
                "System.IterationPath": "Sprint 1",
                "Microsoft.VSTS.Common.AcceptanceCriteria": "- [ ] Criterion 1",
            },
            "_links": {"html": {"href": "https://example.com/123"}},
        }

        # Import and test
        from sdo_package.work_items import cmd_workitem_show

        # Create mock args
        args = MagicMock()
        args.id = 123
        args.verbose = False

        # Should not raise exception
        cmd_workitem_show(args)

        # Verify API calls
        mock_client.get_work_item.assert_called_once_with(123)

    @patch("sdo_package.client.extract_platform_info_from_git")
    @patch("sdo_package.work_items.subprocess.run")
    def test_cmd_workitem_show_github(self, mock_subprocess, mock_extract):
        """Test workitem show command for GitHub."""
        # Mock platform detection
        mock_extract.return_value = {
            "platform": "github",
            "owner": "test-owner",
            "repo": "test-repo",
        }

        # Mock GitHub CLI response
        mock_result = type(
            "obj",
            (object,),
            {
                "returncode": 0,
                "stdout": '{"number":123,"title":"Test Issue","state":"OPEN",'
                '"body":"Description\\n\\n## Acceptance Criteria\\n- [ ] Item 1",'
                '"createdAt":"2025-01-01T10:00:00Z","updatedAt":"2025-01-02T10:00:00Z",'
                '"labels":[{"name":"bug"}],"assignees":[],"url":"https://github.com/test/123"}',
            },
        )()
        mock_subprocess.return_value = mock_result

        # Import and test
        from sdo_package.work_items import cmd_workitem_show

        # ClickArgs not needed - using MagicMock

        args = MagicMock()

        # Should not raise exception
        cmd_workitem_show(args)

        # Verify subprocess call
        assert mock_subprocess.called

    @patch("sdo_package.client.extract_platform_info_from_git")
    @patch("sdo_package.client.AzureDevOpsClient")
    @patch.dict("os.environ", {"AZURE_DEVOPS_PAT": "fake-pat-token"})
    def test_cmd_workitem_update_azdo(self, mock_client_class, mock_extract):
        """Test workitem update command for Azure DevOps."""
        # Mock platform detection
        mock_extract.return_value = {
            "platform": "azdo",
            "organization": "test-org",
            "project": "test-project",
            "repository": "test-repo",
        }

        # Mock client
        mock_client = mock_client_class.return_value
        mock_client.update_work_item.return_value = {
            "id": 123,
            "fields": {"System.Title": "Updated Title", "System.State": "Done"},
        }

        # Import and test
        from sdo_package.work_items import cmd_workitem_update

        # Create mock args
        args = MagicMock()
        args.id = 123
        args.title = "Updated Title"
        args.description = None
        args.assignee = None
        args.state = "Done"
        args.verbose = False

        # Should not raise exception
        cmd_workitem_update(args)

        # Verify API calls
        mock_client.update_work_item.assert_called_once()

    @patch("sdo_package.client.extract_platform_info_from_git")
    @patch("sdo_package.work_items.subprocess.run")
    def test_cmd_workitem_update_github(self, mock_subprocess, mock_extract):
        """Test workitem update command for GitHub."""
        # Mock platform detection
        mock_extract.return_value = {
            "platform": "github",
            "owner": "test-owner",
            "repo": "test-repo",
        }

        # Mock GitHub CLI response
        mock_result = type(
            "obj",
            (object,),
            {"returncode": 0, "stdout": '{"number":123,"title":"Updated Title","state":"CLOSED"}'},
        )()
        mock_subprocess.return_value = mock_result

        # Import and test
        from sdo_package.work_items import cmd_workitem_update

        # Create mock args
        args = MagicMock()
        args.id = 123
        args.title = "Updated Title"
        args.description = None
        args.assignee = None
        args.state = "closed"
        args.verbose = False

        # Should not raise exception
        cmd_workitem_update(args)

        # Verify subprocess call
        assert mock_subprocess.called

    @patch("sdo_package.client.extract_platform_info_from_git")
    @patch("sdo_package.client.AzureDevOpsClient")
    @patch.dict("os.environ", {"AZURE_DEVOPS_PAT": "fake-pat-token"})
    def test_cmd_workitem_comment_azdo(self, mock_client_class, mock_extract):
        """Test workitem comment command for Azure DevOps."""
        # Mock platform detection
        mock_extract.return_value = {
            "platform": "azdo",
            "organization": "test-org",
            "project": "test-project",
            "repository": "test-repo",
        }

        # Mock client
        mock_client = mock_client_class.return_value
        mock_client.add_work_item_comment.return_value = {"id": 1, "text": "Test comment"}

        # Import and test
        from sdo_package.work_items import cmd_workitem_comment

        # Create mock args
        args = MagicMock()
        args.id = 123
        args.text = "Test comment"
        args.verbose = False

        # Should not raise exception
        cmd_workitem_comment(args)

        # Verify API calls
        mock_client.add_work_item_comment.assert_called_once_with(123, "Test comment")

    @patch("sdo_package.client.extract_platform_info_from_git")
    @patch("sdo_package.work_items.subprocess.run")
    def test_cmd_workitem_comment_github(self, mock_subprocess, mock_extract):
        """Test workitem comment command for GitHub."""
        # Mock platform detection
        mock_extract.return_value = {
            "platform": "github",
            "owner": "test-owner",
            "repo": "test-repo",
        }

        # Mock GitHub CLI response
        mock_result = type(
            "obj",
            (object,),
            {"returncode": 0, "stdout": '{"id":"comment123","body":"Test comment"}'},
        )()
        mock_subprocess.return_value = mock_result

        # Import and test
        from sdo_package.work_items import cmd_workitem_comment

        # Create mock args
        args = MagicMock()
        args.id = 123
        args.text = "Test comment"
        args.verbose = False

        # Should not raise exception
        cmd_workitem_comment(args)

        # Verify subprocess call
        assert mock_subprocess.called

    @patch("sdo_package.client.extract_platform_info_from_git")
    @patch("sdo_package.client.AzureDevOpsClient")
    @patch.dict("os.environ", {"AZURE_DEVOPS_PAT": "fake-pat-token"})
    def test_get_work_item_platform_azdo(self, mock_client_class, mock_extract):
        """Test get_work_item_platform for Azure DevOps."""
        # Mock platform detection
        mock_extract.return_value = {
            "platform": "azdo",
            "organization": "test-org",
            "project": "test-project",
            "repository": "test-repo",
        }

        # Mock client
        mock_client = mock_client_class.return_value

        # Import and test
        from sdo_package.work_items import get_work_item_platform

        platform, config = get_work_item_platform()

        assert platform == "azdo"
        assert config["organization"] == "test-org"
        assert config["project"] == "test-project"

    @patch("sdo_package.client.extract_platform_info_from_git")
    @patch("sdo_package.work_items.subprocess.run")
    def test_get_work_item_platform_github(self, mock_subprocess, mock_extract):
        """Test get_work_item_platform for GitHub."""
        # Mock platform detection
        mock_extract.return_value = {
            "platform": "github",
            "owner": "test-owner",
            "repo": "test-repo",
        }

        # Mock gh CLI check
        mock_result = type("obj", (object,), {"returncode": 0})()
        mock_subprocess.return_value = mock_result

        # Import and test
        from sdo_package.work_items import get_work_item_platform

        platform, config = get_work_item_platform()

        assert platform == "github"
        assert config["owner"] == "test-owner"
        assert config["repo"] == "test-repo"

    @patch("sdo_package.client.extract_platform_info_from_git")
    def test_get_work_item_platform_unsupported(self, mock_extract):
        """Test get_work_item_platform with unsupported platform."""
        # Mock platform detection with unknown platform
        mock_extract.return_value = {"platform": "unknown", "owner": "test-owner"}

        # Import and test
        from sdo_package.work_items import get_work_item_platform
        from sdo_package.exceptions import SDOError

        with pytest.raises(SDOError) as exc_info:
            get_work_item_platform()

        assert "unsupported" in str(exc_info.value).lower()


if __name__ == "__main__":
    pytest.main([__file__, "-v"])
