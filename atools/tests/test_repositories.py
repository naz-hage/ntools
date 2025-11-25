"""
Unit tests for repositories module.
"""

import unittest
from unittest.mock import patch, MagicMock
from sdo_package.repositories import (
    get_repo_config,
    cmd_repo_create,
    cmd_repo_show,
    cmd_repo_list,
    cmd_repo_delete
)


class TestRepoConfig(unittest.TestCase):
    """Test repository configuration functions."""

    @patch('sdo_package.repositories.extract_platform_info_from_git')
    def test_get_repo_config_success(self, mock_extract):
        """Test successful configuration extraction."""
        mock_extract.return_value = {
            'platform': 'azdo',
            'organization': 'test-org',
            'project': 'test-project',
            'repository': 'test-repo'
        }

        config = get_repo_config()
        self.assertIsNotNone(config)
        self.assertEqual(config['platform'], 'azdo')
        self.assertEqual(config['organization'], 'test-org')
        self.assertEqual(config['project'], 'test-project')
        self.assertEqual(config['repository'], 'test-repo')

    @patch('sdo_package.repositories.extract_platform_info_from_git')
    def test_get_repo_config_failure(self, mock_extract):
        """Test configuration extraction failure."""
        mock_extract.return_value = None

        config = get_repo_config()
        self.assertIsNone(config)


class TestRepositoryCommands(unittest.TestCase):
    """Test repository command handlers."""

    def setUp(self):
        """Set up test fixtures."""
        self.mock_client = MagicMock()
        self.mock_config = {
            'platform': 'azdo',
            'organization': 'test-org',
            'project': 'test-project',
            'repository': 'test-repo'
        }

    @patch('sdo_package.repositories.get_repo_config')
    @patch('sdo_package.repositories.get_personal_access_token')
    @patch('sdo_package.repositories.AzureDevOpsClient')
    def test_cmd_repo_create_missing_config(self, mock_client_class, mock_pat, mock_config):
        """Test repo create with missing configuration."""
        mock_config.return_value = None

        result = cmd_repo_create(verbose=False)

        self.assertEqual(result, 1)
        mock_client_class.assert_not_called()

    @patch('sdo_package.repositories.get_repo_config')
    @patch('sdo_package.repositories.get_personal_access_token')
    @patch('sdo_package.repositories.AzureDevOpsClient')
    def test_cmd_repo_create_missing_pat(self, mock_client_class, mock_pat, mock_config):
        """Test repo create with missing PAT."""
        mock_config.return_value = self.mock_config
        mock_pat.return_value = None

        result = cmd_repo_create(verbose=False)

        self.assertEqual(result, 1)
        mock_client_class.assert_not_called()

    @patch('sdo_package.repositories.get_repo_config')
    @patch('sdo_package.repositories.create_repository_platform')
    def test_cmd_repo_create_already_exists(self, mock_create_platform, mock_config):
        """Test repo create when repository already exists."""
        mock_config.return_value = self.mock_config

        mock_platform = MagicMock()
        mock_platform.validate_auth.return_value = True
        mock_platform.get_repository.return_value = {
            'id': '123',
            'name': 'test-repo',
            'webUrl': 'https://dev.azure.com/test/test-repo'
        }
        mock_create_platform.return_value = mock_platform

        result = cmd_repo_create(verbose=False)

        self.assertEqual(result, 0)
        mock_platform.create_repository.assert_not_called()

    @patch('sdo_package.repositories.get_repo_config')
    @patch('sdo_package.repositories.create_repository_platform')
    def test_cmd_repo_create_success(self, mock_create_platform, mock_config):
        """Test successful repo create command."""
        mock_config.return_value = self.mock_config

        mock_platform = MagicMock()
        mock_platform.validate_auth.return_value = True
        mock_platform.get_repository.return_value = None
        mock_platform.create_repository.return_value = True
        mock_create_platform.return_value = mock_platform

        result = cmd_repo_create(verbose=False)

        self.assertEqual(result, 0)
        mock_platform.create_repository.assert_called_once_with('test-repo')

    @patch('sdo_package.repositories.get_repo_config')
    @patch('sdo_package.repositories.create_repository_platform')
    def test_cmd_repo_show_success(self, mock_create_platform, mock_config):
        """Test successful repo show command."""
        mock_config.return_value = self.mock_config

        mock_platform = MagicMock()
        mock_platform.validate_auth.return_value = True
        mock_platform.get_repository.return_value = {
            'id': '123',
            'name': 'test-repo',
            'webUrl': 'https://dev.azure.com/test/test-repo',
            'remoteUrl': 'https://dev.azure.com/test/_git/test-repo',
            'defaultBranch': 'refs/heads/main',
            'size': 1024
        }
        mock_create_platform.return_value = mock_platform

        result = cmd_repo_show(verbose=False)

        self.assertEqual(result, 0)
        mock_platform.get_repository.assert_called_once_with('test-repo')

    @patch('sdo_package.repositories.get_repo_config')
    @patch('sdo_package.repositories.get_personal_access_token')
    @patch('sdo_package.repositories.AzureDevOpsClient')
    def test_cmd_repo_show_not_found(self, mock_client_class, mock_pat, mock_config):
        """Test repo show when repository not found."""
        mock_config.return_value = self.mock_config
        mock_pat.return_value = 'fake-pat'
        
        mock_client_instance = MagicMock()
        mock_client_instance.get_repository.return_value = None
        mock_client_class.return_value = mock_client_instance

        result = cmd_repo_show(verbose=False)

        self.assertEqual(result, 1)

    @patch('sdo_package.repositories.get_repo_config')
    @patch('sdo_package.repositories.create_repository_platform')
    def test_cmd_repo_list_success(self, mock_create_platform, mock_config):
        """Test successful repo list command."""
        mock_config.return_value = self.mock_config

        mock_platform = MagicMock()
        mock_platform.validate_auth.return_value = True
        mock_platform.list_repositories.return_value = [
            {
                'id': '123',
                'name': 'test-repo-1',
                'webUrl': 'https://dev.azure.com/test/test-repo-1',
                'defaultBranch': 'refs/heads/main'
            },
            {
                'id': '124',
                'name': 'test-repo-2',
                'webUrl': 'https://dev.azure.com/test/test-repo-2',
                'defaultBranch': 'refs/heads/main'
            }
        ]
        mock_create_platform.return_value = mock_platform

        result = cmd_repo_list(verbose=False)

        self.assertEqual(result, 0)
        mock_platform.list_repositories.assert_called_once()

    @patch('sdo_package.repositories.get_repo_config')
    @patch('sdo_package.repositories.create_repository_platform')
    def test_cmd_repo_list_empty(self, mock_create_platform, mock_config):
        """Test repo list with no repositories."""
        mock_config.return_value = self.mock_config

        mock_platform = MagicMock()
        mock_platform.validate_auth.return_value = True
        mock_platform.list_repositories.return_value = []
        mock_create_platform.return_value = mock_platform

        result = cmd_repo_list(verbose=False)

        self.assertEqual(result, 0)
        mock_platform.list_repositories.assert_called_once()

    @patch('sdo_package.repositories.get_repo_config')
    @patch('sdo_package.repositories.create_repository_platform')
    @patch('builtins.input', return_value='no')
    def test_cmd_repo_delete_cancelled(self, mock_input, mock_create_platform, mock_config):
        """Test repo delete command cancelled by user."""
        mock_config.return_value = self.mock_config

        mock_platform = MagicMock()
        mock_create_platform.return_value = mock_platform

        result = cmd_repo_delete(verbose=False)

        self.assertEqual(result, 0)
        mock_platform.delete_repository.assert_not_called()

    @patch('sdo_package.repositories.get_repo_config')
    @patch('sdo_package.repositories.create_repository_platform')
    @patch('builtins.input', return_value='yes')
    def test_cmd_repo_delete_success(self, mock_input, mock_create_platform, mock_config):
        """Test successful repo delete command."""
        mock_config.return_value = self.mock_config

        mock_platform = MagicMock()
        mock_platform.validate_auth.return_value = True
        mock_platform.delete_repository.return_value = True
        mock_create_platform.return_value = mock_platform

        result = cmd_repo_delete(verbose=False)

        self.assertEqual(result, 0)
        mock_platform.delete_repository.assert_called_once_with('test-repo')

    @patch('sdo_package.repositories.get_repo_config')
    @patch('sdo_package.repositories.create_repository_platform')
    @patch('builtins.input', return_value='yes')
    def test_cmd_repo_delete_failure(self, mock_input, mock_create_platform, mock_config):
        """Test repo delete command failure."""
        mock_config.return_value = self.mock_config

        mock_platform = MagicMock()
        mock_platform.validate_auth.return_value = True
        mock_platform.delete_repository.return_value = False
        mock_create_platform.return_value = mock_platform

        result = cmd_repo_delete(verbose=False)

        self.assertEqual(result, 1)


if __name__ == '__main__':
    unittest.main()
