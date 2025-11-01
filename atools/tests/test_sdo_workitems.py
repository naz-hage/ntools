"""
Tests for SDO work items business logic.
"""

import pytest
from unittest.mock import patch
import sys
from pathlib import Path

# Add the atools directory to sys.path
sys.path.insert(0, str(Path(__file__).parent.parent))

from sdo_package.work_items import WorkItemManager, WorkItemResult
from sdo_package.exceptions import PlatformError, ParsingError


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
                "pat": "test_pat"
            }
        }

        # Mock platform creation - return dict with id and url
        mock_platform_instance = mock_platform.return_value
        mock_platform_instance.create_work_item.return_value = {
            "id": 123,
            "url": "https://example.com/123"
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
    def test_create_work_item_platform_error(self, mock_platform, mock_markdown_parser,
                                           mock_exists):
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
                "pat": "test_pat"
            }
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
            "metadata": {
                "target": "unknown_platform"  # This defaults to Azure DevOps
            }
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
            success=True,
            work_item_id="123",
            url="https://example.com/123",
            platform="azure_devops"
        )

        assert result.success is True
        assert result.work_item_id == "123"
        assert result.url == "https://example.com/123"
        assert result.platform == "azure_devops"
        assert result.error_message is None

    def test_failed_result_creation(self):
        """Test creating a failed result."""
        result = WorkItemResult(
            success=False,
            error_message="Test error"
        )

        assert result.success is False
        assert result.error_message == "Test error"
        assert result.work_item_id is None
        assert result.url is None
        assert result.platform is None

    def test_result_string_representation(self):
        """Test string representation of results."""
        success_result = WorkItemResult(
            success=True,
            work_item_id="123",
            url="https://example.com/123",
            platform="azure_devops"
        )

        result_str = str(success_result)
        assert "123" in result_str
        assert "success" in result_str.lower()

        failed_result = WorkItemResult(
            success=False,
            error_message="Test error"
        )

        result_str = str(failed_result)
        assert "error" in result_str.lower()
        assert "Test error" in result_str


if __name__ == "__main__":
    pytest.main([__file__, "-v"])
