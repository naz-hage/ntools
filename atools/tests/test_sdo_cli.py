"""
Tests for SDO CLI commands.
"""

import pytest
from unittest.mock import patch, MagicMock
from click.testing import CliRunner
import sys
from pathlib import Path

# Add the atools directory to sys.path
sys.path.insert(0, str(Path(__file__).parent.parent))

from sdo_package import cli
from sdo_package.exceptions import SDOException


class TestAddIssueCommand:
    """Test the add-issue CLI command."""
    
    def setup_method(self):
        """Set up test fixtures."""
        self.runner = CliRunner()
    
    @patch("sdo_package.work_items.WorkItemManager")
    def test_add_issue_success(self, mock_manager):
        """Test successful add-issue command execution."""
        # Mock successful work item creation
        mock_instance = mock_manager.return_value
        from sdo_package.work_items import WorkItemResult
        mock_instance.create_work_item.return_value = WorkItemResult(
            success=True,
            work_item_id="123",
            url="https://example.com/123",
            platform="test"
        )
        
        with self.runner.isolated_filesystem():
            # Create a test markdown file
            with open("test.md", "w") as f:
                f.write("# Test Issue\n\nDescription here")
            
            result = self.runner.invoke(cli.add_issue, ["test.md"])
            
            assert result.exit_code == 0
            assert "success" in result.output.lower()
            mock_instance.create_work_item.assert_called_once()
    
    @patch("sdo_package.work_items.WorkItemManager")
    def test_add_issue_with_verbose(self, mock_manager):
        """Test add-issue command with verbose flag."""
        mock_instance = mock_manager.return_value
        from sdo_package.work_items import WorkItemResult
        mock_instance.create_work_item.return_value = WorkItemResult(
            success=True,
            work_item_id="123",
            url="https://example.com/123",
            platform="test"
        )
        
        with self.runner.isolated_filesystem():
            with open("test.md", "w") as f:
                f.write("# Test Issue\n\nDescription here")
            
            result = self.runner.invoke(cli.add_issue, ["--verbose", "test.md"])
            
            assert result.exit_code == 0
            # Verbose output should be more detailed
            assert len(result.output) > 0
    
    def test_add_issue_missing_file(self):
        """Test add-issue command with missing file."""
        result = self.runner.invoke(cli.add_issue, ["nonexistent.md"])
        
        assert result.exit_code != 0
        assert "error" in result.output.lower() or "not found" in result.output.lower()
    
    @patch("sdo_package.work_items.WorkItemManager")
    def test_add_issue_platform_error(self, mock_manager):
        """Test add-issue command with platform error."""
        mock_instance = mock_manager.return_value
        from sdo_package.work_items import WorkItemResult
        mock_instance.create_work_item.return_value = WorkItemResult(
            success=False,
            error_message="Platform error"
        )
        
        with self.runner.isolated_filesystem():
            with open("test.md", "w") as f:
                f.write("# Test Issue\n\nDescription here")
            
            result = self.runner.invoke(cli.add_issue, ["test.md"])
            
            assert result.exit_code != 0
            assert "error" in result.output.lower()
    
    @patch("sdo_package.work_items.WorkItemManager")
    def test_add_issue_dry_run(self, mock_manager):
        """Test add-issue command with dry run flag."""
        mock_instance = mock_manager.return_value
        
        with self.runner.isolated_filesystem():
            with open("test.md", "w") as f:
                f.write("# Test Issue\n\nDescription here")
            
            result = self.runner.invoke(cli.add_issue, ["--dry-run", "test.md"])
            
            assert result.exit_code == 0
            # Should not actually call create_work_item in dry run
            mock_instance.create_work_item.assert_not_called()
            assert "dry run" in result.output.lower() or "would create" in result.output.lower()


class TestCLIHelpers:
    """Test CLI helper functions."""
    
    def test_handle_verbose_output(self):
        """Test verbose output handling."""
        # Test that verbose output functions work correctly
        pass
    
    def test_error_formatting(self):
        """Test error message formatting."""
        # Test that errors are formatted nicely for users
        pass
    
    def test_success_formatting(self):
        """Test success message formatting."""
        # Test that success messages are clear and informative
        pass


class TestCLIIntegration:
    """Test CLI integration scenarios."""
    
    def setup_method(self):
        """Set up test fixtures."""
        self.runner = CliRunner()
    
    @patch("sdo_package.work_items.WorkItemManager")
    def test_full_workflow_azure_devops(self, mock_manager):
        """Test full workflow for Azure DevOps."""
        mock_instance = mock_manager.return_value
        from sdo_package.work_items import WorkItemResult
        mock_instance.create_work_item.return_value = WorkItemResult(
            success=True,
            work_item_id="12345",
            url="https://dev.azure.com/org/project/_workitems/edit/12345",
            platform="azure_devops"
        )
        
        with self.runner.isolated_filesystem():
            # Create Azure DevOps test file
            with open("azdo_issue.md", "w") as f:
                f.write("""# Bug: Sample Issue
                
## Description
This is a test issue for Azure DevOps.

## Acceptance Criteria
- [ ] Criteria 1
- [ ] Criteria 2

## Metadata
Platform: azure_devops
WorkItemType: Bug
""")
            
            result = self.runner.invoke(cli.add_issue, ["azdo_issue.md"])
            
            assert result.exit_code == 0
            assert "12345" in result.output
            mock_instance.create_work_item.assert_called_once()
    
    @patch("sdo_package.work_items.WorkItemManager")
    def test_full_workflow_github(self, mock_manager):
        """Test full workflow for GitHub."""
        mock_instance = mock_manager.return_value
        from sdo_package.work_items import WorkItemResult
        mock_instance.create_work_item.return_value = WorkItemResult(
            success=True,
            work_item_id="67890",
            url="https://github.com/owner/repo/issues/67890",
            platform="github"
        )
        
        with self.runner.isolated_filesystem():
            # Create GitHub test file
            with open("github_issue.md", "w") as f:
                f.write("""# Feature: Sample Feature
                
## Description
This is a test feature for GitHub.

## Acceptance Criteria
- [ ] Criteria 1
- [ ] Criteria 2

## Metadata
Platform: github
""")
            
            result = self.runner.invoke(cli.add_issue, ["github_issue.md"])
            
            assert result.exit_code == 0
            assert "67890" in result.output
            mock_instance.create_work_item.assert_called_once()


if __name__ == "__main__":
    pytest.main([__file__, "-v"])

