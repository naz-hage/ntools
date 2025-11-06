"""
Tests for Azure DevOps platform work item creation.
"""

import pytest
from unittest.mock import patch, Mock
import os
import base64
import sys
from pathlib import Path

# Add the atools directory to sys.path
sys.path.insert(0, str(Path(__file__).parent.parent))

from sdo_package.platforms.azdo_platform import AzureDevOpsPlatform


class TestAzureDevOpsPlatform:
    """Test the AzureDevOpsPlatform class."""

    def setup_method(self):
        """Set up test fixtures."""
        self.platform = AzureDevOpsPlatform(verbose=False)

    def test_platform_init(self):
        """Test AzureDevOpsPlatform initialization."""
        assert self.platform is not None
        assert hasattr(self.platform, "verbose")
        assert self.platform.verbose is False

        # Test verbose initialization
        verbose_platform = AzureDevOpsPlatform(verbose=True)
        assert verbose_platform.verbose is True

    def test_create_work_item_dry_run_pbi(self):
        """Test dry run work item creation for PBI."""
        metadata = {
            "work_item_type": "PBI",
            "project": "TestProject",
            "organization": "TestOrg",
            "area": "TestProject\\Area1",
            "iteration": "TestProject\\Sprint 1",
            "assignee": "test@example.com",
            "priority": "2",
            "tags": ["test", "automation"]
        }

        result = self.platform.create_work_item(
            title="Test PBI",
            description="This is a test PBI.",
            metadata=metadata,
            acceptance_criteria=["PBI criteria 1", "PBI criteria 2"],
            dry_run=True
        )

        assert result is not None
        assert result["dry_run"] is True
        assert result["title"] == "Test PBI"
        assert result["project"] == "TestProject"

    def test_create_work_item_dry_run_task(self):
        """Test dry run work item creation for Task."""
        metadata = {
            "work_item_type": "Task",
            "project": "TestProject",
            "organization": "TestOrg",
            "area": "TestProject\\Tasks",
            "iteration": "TestProject\\Sprint 1",
            "assignee": "developer@example.com",
            "priority": "3",
            "tags": ["development", "backend"]
        }

        result = self.platform.create_work_item(
            title="Implement API Endpoint",
            description="Create REST API endpoint for user management.",
            metadata=metadata,
            acceptance_criteria=["API returns 200 on success", "Proper error handling"],
            dry_run=True
        )

        assert result is not None
        assert result["dry_run"] is True
        assert result["title"] == "Implement API Endpoint"
        assert result["project"] == "TestProject"

    def test_create_work_item_dry_run_bug(self):
        """Test dry run work item creation for Bug."""
        metadata = {
            "work_item_type": "Bug",
            "project": "TestProject",
            "organization": "TestOrg",
            "area": "TestProject\\Bugs",
            "iteration": "TestProject\\Sprint 1",
            "assignee": "qa@example.com",
            "priority": "1",
            "tags": ["bug", "critical"]
        }

        result = self.platform.create_work_item(
            title="Login Button Not Working",
            description="Users cannot click the login button on mobile devices.",
            metadata=metadata,
            acceptance_criteria=["Button is clickable on mobile", "Login flow works correctly"],
            dry_run=True
        )

        assert result is not None
        assert result["dry_run"] is True
        assert result["title"] == "Login Button Not Working"
        assert result["project"] == "TestProject"

    @patch.dict(os.environ, {'AZURE_DEVOPS_PAT': 'test-pat-123'})
    @patch('requests.post')
    def test_create_work_item_success_pbi(self, mock_post):
        """Test successful PBI creation with mocked API."""
        # Mock successful API response
        mock_response = Mock()
        mock_response.status_code = 201
        mock_response.json.return_value = {
            'id': 12345,
            '_links': {
                'html': {
                    'href': 'https://dev.azure.com/testorg/testproject/_workitems/edit/12345'
                }
            }
        }
        mock_post.return_value = mock_response

        metadata = {
            "work_item_type": "PBI",
            "project": "TestProject",
            "organization": "TestOrg"
        }

        result = self.platform.create_work_item(
            title="Test PBI",
            description="Test description",
            metadata=metadata,
            dry_run=False
        )

        assert result is not None
        assert result["id"] == 12345
        assert "testorg/testproject" in result["url"]
        assert result["type"] == "PBI"
        assert result["title"] == "Test PBI"

        # Verify API call
        mock_post.assert_called_once()
        call_args = mock_post.call_args
        url = call_args[0][0]
        operations = call_args[1]['json']
        headers = call_args[1]['headers']

        # Check URL contains correct work item type
        assert "$PBI" in url
        assert "TestOrg" in url  # Uses capitalized version from metadata
        assert "TestProject" in url

        # Check operations include required fields
        title_op = next((op for op in operations if op['path'] == '/fields/System.Title'), None)
        assert title_op is not None
        assert title_op['value'] == "Test PBI"

        # Check authentication header
        assert 'Authorization' in headers
        assert headers['Authorization'].startswith('Basic ')

    @patch.dict(os.environ, {'AZURE_DEVOPS_PAT': 'test-pat-123'})
    @patch('requests.post')
    def test_create_work_item_success_task(self, mock_post):
        """Test successful Task creation with mocked API."""
        # Mock successful API response
        mock_response = Mock()
        mock_response.status_code = 201
        mock_response.json.return_value = {
            'id': 23456,
            '_links': {
                'html': {
                    'href': 'https://dev.azure.com/testorg/testproject/_workitems/edit/23456'
                }
            }
        }
        mock_post.return_value = mock_response

        metadata = {
            "work_item_type": "Task",
            "project": "TestProject",
            "organization": "TestOrg",
            "assignee": "dev@example.com",
            "priority": "2"
        }

        result = self.platform.create_work_item(
            title="Implement Feature X",
            description="Task description",
            metadata=metadata,
            dry_run=False
        )

        assert result is not None
        assert result["id"] == 23456
        assert result["type"] == "Task"

        # Verify API call
        mock_post.assert_called_once()
        call_args = mock_post.call_args
        url = call_args[0][0]
        operations = call_args[1]['json']

        # Check URL contains correct work item type
        assert "$Task" in url

        # Check assignee and priority operations
        assignee_op = next((op for op in operations if op['path'] == '/fields/System.AssignedTo'), None)
        assert assignee_op is not None
        assert assignee_op['value'] == "dev@example.com"

        priority_op = next((op for op in operations if op['path'] == '/fields/Microsoft.VSTS.Common.Priority'), None)
        assert priority_op is not None
        assert priority_op['value'] == 2

    @patch.dict(os.environ, {'AZURE_DEVOPS_PAT': 'test-pat-123'})
    @patch('requests.post')
    def test_create_work_item_api_error(self, mock_post):
        """Test work item creation with API error."""
        # Mock error response
        mock_response = Mock()
        mock_response.status_code = 400
        mock_response.json.return_value = {
            'message': 'Invalid work item type'
        }
        mock_post.return_value = mock_response

        metadata = {
            "work_item_type": "InvalidType",
            "project": "TestProject",
            "organization": "TestOrg"
        }

        result = self.platform.create_work_item(
            title="Test Item",
            description="Test description",
            metadata=metadata,
            dry_run=False
        )

        assert result is None

    def test_create_work_item_no_pat(self):
        """Test work item creation without PAT."""
        # Ensure no PAT is set
        with patch.dict(os.environ, {}, clear=True):
            metadata = {
                "work_item_type": "PBI",
                "project": "TestProject",
                "organization": "TestOrg"
            }

            result = self.platform.create_work_item(
                title="Test PBI",
                description="Test description",
                metadata=metadata,
                dry_run=False
            )

            assert result is None

    def test_create_work_item_missing_config(self):
        """Test work item creation with missing configuration."""
        with patch.dict(os.environ, {'AZURE_DEVOPS_PAT': 'test-pat-123'}):
            # No organization/project in metadata and no Git config
            metadata = {
                "work_item_type": "PBI"
            }

            result = self.platform.create_work_item(
                title="Test PBI",
                description="Test description",
                metadata=metadata,
                dry_run=False
            )

            assert result is None

    @patch.dict(os.environ, {'AZURE_DEVOPS_PAT': 'test-pat-123'})
    @patch('requests.post')
    def test_create_work_item_with_acceptance_criteria(self, mock_post):
        """Test work item creation with acceptance criteria."""
        # Mock successful API response
        mock_response = Mock()
        mock_response.status_code = 201
        mock_response.json.return_value = {
            'id': 34567,
            '_links': {
                'html': {
                    'href': 'https://dev.azure.com/testorg/testproject/_workitems/edit/34567'
                }
            }
        }
        mock_post.return_value = mock_response

        metadata = {
            "work_item_type": "PBI",
            "project": "TestProject",
            "organization": "TestOrg"
        }

        acceptance_criteria = [
            "User can log in successfully",
            "Password validation works",
            "Error messages are clear"
        ]

        result = self.platform.create_work_item(
            title="Implement Login Feature",
            description="Create user login functionality.",
            metadata=metadata,
            acceptance_criteria=acceptance_criteria,
            dry_run=False
        )

        assert result is not None

        # Verify API call includes acceptance criteria in description
        mock_post.assert_called_once()
        call_args = mock_post.call_args
        operations = call_args[1]['json']

        desc_op = next((op for op in operations if op['path'] == '/fields/System.Description'), None)
        assert desc_op is not None
        description = desc_op['value']

        # Check that acceptance criteria are included
        assert "## Acceptance Criteria" in description
        for criteria in acceptance_criteria:
            assert criteria in description


if __name__ == "__main__":
    pytest.main([__file__, "-v"])