"""
Tests for SDO pipeline functionality.

Tests pipeline configuration, GitHub CLI integration, workflow operations,
and unified command handlers for both Azure DevOps and GitHub platforms.
"""

import json
import subprocess
from unittest.mock import patch, MagicMock
import sys
from pathlib import Path

# Add the atools directory to sys.path
sys.path.insert(0, str(Path(__file__).parent.parent))

from sdo_package.pipelines import (
    get_pipeline_config,
    _check_gh_cli,
    _run_gh_command,
    _find_default_workflow_name,
    cmd_github_workflow_list,
    cmd_github_workflow_run,
    cmd_github_workflow_view,
    cmd_github_run_list,
    cmd_github_run_view,
    cmd_github_run_logs,
    cmd_pipeline_create,
    cmd_pipeline_show,
    cmd_pipeline_list,
    cmd_pipeline_delete,
    cmd_pipeline_run,
    cmd_pipeline_status,
    cmd_pipeline_logs,
    cmd_pipeline_lastbuild,
    cmd_pipeline_update,
    cmd_azdo_pipeline_delete,
)


class TestPipelineConfig:
    """Test pipeline configuration functions."""

    @patch('sdo_package.pipelines.extract_platform_info_from_git')
    def test_get_pipeline_config_github_success(self, mock_extract):
        """Test successful pipeline config extraction for GitHub."""
        mock_extract.return_value = {
            'platform': 'github',
            'owner': 'testuser',
            'repo': 'testrepo',
            'branch': 'main'
        }

        config = get_pipeline_config()

        assert config is not None
        assert config['platform'] == 'github'
        assert config['owner'] == 'testuser'
        assert config['repo'] == 'testrepo'
        assert config['branch'] == 'main'

    @patch('sdo_package.pipelines.extract_platform_info_from_git')
    def test_get_pipeline_config_azdo_success(self, mock_extract):
        """Test successful pipeline config extraction for Azure DevOps."""
        mock_extract.return_value = {
            'platform': 'azdo',
            'organization': 'testorg',
            'project': 'testproject',
            'repository': 'testrepo',
            'branch': 'main',
            'pipelineName': 'testrepo',
            'pipelineYamlPath': '.azure-pipelines/azure-pipelines.yml'
        }

        config = get_pipeline_config()

        assert config is not None
        assert config['platform'] == 'azdo'
        assert config['organization'] == 'testorg'
        assert config['project'] == 'testproject'
        assert config['pipelineName'] == 'testrepo'

    @patch('sdo_package.pipelines.extract_platform_info_from_git')
    def test_get_pipeline_config_failure(self, mock_extract):
        """Test pipeline config extraction failure."""
        mock_extract.return_value = None

        config = get_pipeline_config()

        assert config is None


class TestGitHubCLIUtils:
    """Test GitHub CLI utility functions."""

    @patch('subprocess.run')
    def test_check_gh_cli_success(self, mock_run):
        """Test successful GitHub CLI check."""
        mock_run.return_value = MagicMock(returncode=0, stdout='gh version 2.40.0')

        result = _check_gh_cli()

        assert result is True
        mock_run.assert_called_once_with(
            ['gh', '--version'],
            capture_output=True,
            text=True,
            check=True
        )

    @patch('subprocess.run')
    def test_check_gh_cli_failure(self, mock_run):
        """Test GitHub CLI check failure."""
        mock_run.side_effect = subprocess.CalledProcessError(1, 'gh')

        result = _check_gh_cli()

        assert result is False

    @patch('subprocess.run')
    def test_check_gh_cli_not_found(self, mock_run):
        """Test GitHub CLI not found."""
        mock_run.side_effect = FileNotFoundError()

        result = _check_gh_cli()

        assert result is False

    @patch('subprocess.run')
    def test_run_gh_command_success(self, mock_run):
        """Test successful GitHub CLI command execution."""
        mock_run.return_value = MagicMock(
            returncode=0,
            stdout='output',
            stderr='error'
        )

        returncode, stdout, stderr = _run_gh_command(['workflow', 'list'])

        assert returncode == 0
        assert stdout == 'output'
        assert stderr == 'error'
        mock_run.assert_called_once_with(
            ['gh', 'workflow', 'list'],
            capture_output=True,
            text=True,
            encoding='utf-8',
            errors='replace',
            check=False
        )

    @patch('subprocess.run')
    def test_run_gh_command_verbose(self, mock_run):
        """Test GitHub CLI command execution with verbose output."""
        mock_run.return_value = MagicMock(
            returncode=0,
            stdout='output',
            stderr='error'
        )

        returncode, stdout, stderr = _run_gh_command(['workflow', 'list'], verbose=True)

        assert returncode == 0
        assert stdout == 'output'
        assert stderr == 'error'

    def test_run_gh_command_exception(self):
        """Test GitHub CLI command execution with exception."""
        returncode, stdout, stderr = _run_gh_command(['invalid', 'command'])

        assert returncode == 1
        assert stdout == ''
        assert 'unknown command' in stderr


class TestWorkflowNameDetection:
    """Test default workflow name detection."""

    @patch('sdo_package.pipelines._run_gh_command')
    def test_find_default_workflow_name_ci_exact(self, mock_run):
        """Test finding workflow with exact 'ci' name."""
        mock_run.return_value = (0, '[{"name": "ci", "state": "active", "path": ".github/workflows/ci.yml"}]', '')

        result = _find_default_workflow_name()

        assert result == ('ci', '.github/workflows/ci.yml')

    @patch('sdo_package.pipelines._run_gh_command')
    def test_find_default_workflow_name_contains_ci(self, mock_run):
        """Test finding workflow containing 'ci'."""
        workflows = [
            {"name": "pages-build-deployment", "state": "active", "path": ".github/workflows/pages-build-deployment.yml"},
            {"name": "ntools Workflow", "state": "active", "path": ".github/workflows/ntools.yml"}
        ]
        mock_run.return_value = (0, json.dumps(workflows), '')

        result = _find_default_workflow_name()

        assert result == ('ntools Workflow', '.github/workflows/ntools.yml')

    @patch('sdo_package.pipelines._run_gh_command')
    def test_find_default_workflow_name_workflow_priority(self, mock_run):
        """Test workflow name priority selection."""
        workflows = [
            {"name": "pages-build-deployment", "state": "active", "path": ".github/workflows/pages-build-deployment.yml"},
            {"name": "my-workflow", "state": "active", "path": ".github/workflows/my-workflow.yml"}
        ]
        mock_run.return_value = (0, json.dumps(workflows), '')

        result = _find_default_workflow_name()

        assert result == ('my-workflow', '.github/workflows/my-workflow.yml')

    @patch('sdo_package.pipelines._run_gh_command')
    def test_find_default_workflow_name_fallback(self, mock_run):
        """Test fallback to first workflow when no priority matches."""
        workflows = [
            {"name": "random-workflow", "state": "active", "path": ".github/workflows/random-workflow.yml"},
            {"name": "another-workflow", "state": "active", "path": ".github/workflows/another-workflow.yml"}
        ]
        mock_run.return_value = (0, json.dumps(workflows), '')

        result = _find_default_workflow_name()

        assert result == ('random-workflow', '.github/workflows/random-workflow.yml')

    @patch('sdo_package.pipelines._run_gh_command')
    def test_find_default_workflow_name_command_failure(self, mock_run):
        """Test workflow name detection when command fails."""
        mock_run.return_value = (1, '', 'error')

        result = _find_default_workflow_name()

        assert result is None

    @patch('sdo_package.pipelines._run_gh_command')
    def test_find_default_workflow_name_json_error(self, mock_run):
        """Test workflow name detection with invalid JSON."""
        mock_run.return_value = (0, 'invalid json', '')

        result = _find_default_workflow_name()

        assert result is None


class TestGitHubWorkflowCommands:
    """Test GitHub workflow command functions."""

    @patch('sdo_package.pipelines._check_gh_cli')
    @patch('sdo_package.pipelines._run_gh_command')
    def test_cmd_github_workflow_list_success(self, mock_run, mock_check):
        """Test successful workflow list command."""
        mock_check.return_value = True
        mock_run.return_value = (0, 'workflow1\tactive\t123\nworkflow2\tactive\t456', '')

        result = cmd_github_workflow_list()

        assert result == 0

    @patch('sdo_package.pipelines._check_gh_cli')
    def test_cmd_github_workflow_list_no_cli(self, mock_check):
        """Test workflow list command when GitHub CLI is not available."""
        mock_check.return_value = False

        result = cmd_github_workflow_list()

        assert result == 1

    @patch('sdo_package.pipelines._check_gh_cli')
    @patch('sdo_package.pipelines._run_gh_command')
    def test_cmd_github_workflow_run_success(self, mock_run, mock_check):
        """Test successful workflow run command."""
        mock_check.return_value = True
        mock_run.return_value = (0, 'Workflow run initiated', '')

        result = cmd_github_workflow_run('test-workflow')

        assert result == 0

    @patch('sdo_package.pipelines._check_gh_cli')
    @patch('sdo_package.pipelines._run_gh_command')
    @patch('sdo_package.pipelines._find_default_workflow_name')
    def test_cmd_github_workflow_run_auto_detect(self, mock_find, mock_run, mock_check):
        """Test workflow run command with auto-detection."""
        mock_check.return_value = True
        mock_find.return_value = ('auto-detected-workflow', '.github/workflows/auto-detected-workflow.yml')
        mock_run.return_value = (0, 'Workflow run initiated', '')

        result = cmd_github_workflow_run()

        assert result == 0
        mock_run.assert_called_with(['workflow', 'run', 'auto-detected-workflow'], False)

    @patch('sdo_package.pipelines._check_gh_cli')
    @patch('sdo_package.pipelines._run_gh_command')
    def test_cmd_github_workflow_view_success(self, mock_run, mock_check):
        """Test successful workflow view command."""
        mock_check.return_value = True
        mock_run.return_value = (0, 'Workflow details...', '')

        result = cmd_github_workflow_view('test-workflow')

        assert result == 0

    @patch('sdo_package.pipelines._check_gh_cli')
    @patch('sdo_package.pipelines._run_gh_command')
    @patch('sdo_package.pipelines._find_default_workflow_name')
    def test_cmd_github_workflow_view_auto_detect(self, mock_find, mock_run, mock_check):
        """Test workflow view command with auto-detection."""
        mock_check.return_value = True
        mock_find.return_value = ('auto-detected-workflow', '.github/workflows/auto-detected-workflow.yml')
        mock_run.return_value = (0, 'Workflow details...', '')

        result = cmd_github_workflow_view()

        assert result == 0
        mock_run.assert_called_with(['workflow', 'view', 'auto-detected-workflow'], False)


class TestAzureDevOpsPipelineDelete:
    """Test Azure DevOps pipeline delete functionality."""

    @patch('sdo_package.pipelines.get_pipeline_config')
    @patch('sdo_package.pipelines.get_personal_access_token')
    @patch('sdo_package.pipelines.AzureDevOpsClient')
    def test_cmd_azdo_pipeline_delete_with_pipeline_name(self, mock_client_class, mock_pat, mock_config):
        """Test Azure DevOps pipeline delete with specific pipeline name."""
        mock_config.return_value = {
            'organization': 'testorg',
            'project': 'testproject',
            'pipelineName': 'auto-detected-pipeline'
        }
        mock_pat.return_value = 'test-pat'
        mock_client = MagicMock()
        mock_client_class.return_value = mock_client
        mock_client.delete_pipeline.return_value = True

        result = cmd_azdo_pipeline_delete(pipeline_name='my-custom-pipeline', force=True)

        assert result == 0
        mock_client.delete_pipeline.assert_called_once_with('my-custom-pipeline')

    @patch('sdo_package.pipelines.get_pipeline_config')
    @patch('sdo_package.pipelines.get_personal_access_token')
    @patch('sdo_package.pipelines.AzureDevOpsClient')
    def test_cmd_azdo_pipeline_delete_auto_detect_name(self, mock_client_class, mock_pat, mock_config):
        """Test Azure DevOps pipeline delete with auto-detected pipeline name."""
        mock_config.return_value = {
            'organization': 'testorg',
            'project': 'testproject',
            'pipelineName': 'auto-detected-pipeline'
        }
        mock_pat.return_value = 'test-pat'
        mock_client = MagicMock()
        mock_client_class.return_value = mock_client
        mock_client.delete_pipeline.return_value = True

        result = cmd_azdo_pipeline_delete(force=True)  # No pipeline_name provided

        assert result == 0
        mock_client.delete_pipeline.assert_called_once_with('auto-detected-pipeline')

    @patch('sdo_package.pipelines.get_pipeline_config')
    @patch('sdo_package.pipelines.get_personal_access_token')
    @patch('builtins.input')
    @patch('sdo_package.pipelines.AzureDevOpsClient')
    def test_cmd_azdo_pipeline_delete_with_confirmation_yes(self, mock_client_class, mock_input, mock_pat, mock_config):
        """Test Azure DevOps pipeline delete with user confirmation (yes)."""
        mock_config.return_value = {
            'organization': 'testorg',
            'project': 'testproject',
            'pipelineName': 'test-pipeline'
        }
        mock_pat.return_value = 'test-pat'
        mock_input.return_value = 'yes'
        mock_client = MagicMock()
        mock_client_class.return_value = mock_client
        mock_client.delete_pipeline.return_value = True

        result = cmd_azdo_pipeline_delete(force=False)  # Confirmation required

        assert result == 0
        mock_client.delete_pipeline.assert_called_once_with('test-pipeline')
        mock_input.assert_called_once_with("Are you sure you want to continue? (yes/no): ")

    @patch('sdo_package.pipelines.get_pipeline_config')
    @patch('sdo_package.pipelines.get_personal_access_token')
    @patch('builtins.input')
    @patch('sdo_package.pipelines.AzureDevOpsClient')
    def test_cmd_azdo_pipeline_delete_with_confirmation_no(self, mock_client_class, mock_input, mock_pat, mock_config):
        """Test Azure DevOps pipeline delete with user confirmation (no)."""
        mock_config.return_value = {
            'organization': 'testorg',
            'project': 'testproject',
            'pipelineName': 'test-pipeline'
        }
        mock_pat.return_value = 'test-pat'
        mock_input.return_value = 'no'
        mock_client = MagicMock()
        mock_client_class.return_value = mock_client

        result = cmd_azdo_pipeline_delete(force=False)  # Confirmation required

        assert result == 0
        mock_client.delete_pipeline.assert_not_called()
        mock_input.assert_called_once_with("Are you sure you want to continue? (yes/no): ")

    @patch('sdo_package.pipelines.get_pipeline_config')
    @patch('sdo_package.pipelines.get_personal_access_token')
    @patch('builtins.input')
    @patch('sdo_package.pipelines.AzureDevOpsClient')
    def test_cmd_azdo_pipeline_delete_keyboard_interrupt(self, mock_client_class, mock_input, mock_pat, mock_config):
        """Test Azure DevOps pipeline delete with keyboard interrupt during confirmation."""
        mock_config.return_value = {
            'organization': 'testorg',
            'project': 'testproject',
            'pipelineName': 'test-pipeline'
        }
        mock_pat.return_value = 'test-pat'
        mock_input.side_effect = KeyboardInterrupt()
        mock_client = MagicMock()
        mock_client_class.return_value = mock_client

        result = cmd_azdo_pipeline_delete(force=False)  # Confirmation required

        assert result == 0
        mock_client.delete_pipeline.assert_not_called()

    @patch('sdo_package.pipelines.get_pipeline_config')
    @patch('sdo_package.pipelines.get_personal_access_token')
    @patch('sdo_package.pipelines.AzureDevOpsClient')
    def test_cmd_azdo_pipeline_delete_success(self, mock_client_class, mock_pat, mock_config):
        """Test successful Azure DevOps pipeline delete."""
        mock_config.return_value = {
            'organization': 'testorg',
            'project': 'testproject',
            'pipelineName': 'test-pipeline'
        }
        mock_pat.return_value = 'test-pat'
        mock_client = MagicMock()
        mock_client_class.return_value = mock_client
        mock_client.delete_pipeline.return_value = True

        result = cmd_azdo_pipeline_delete(force=True)

        assert result == 0
        mock_client.delete_pipeline.assert_called_once_with('test-pipeline')

    @patch('sdo_package.pipelines.get_pipeline_config')
    @patch('sdo_package.pipelines.get_personal_access_token')
    @patch('sdo_package.pipelines.AzureDevOpsClient')
    def test_cmd_azdo_pipeline_delete_failure(self, mock_client_class, mock_pat, mock_config):
        """Test failed Azure DevOps pipeline delete."""
        mock_config.return_value = {
            'organization': 'testorg',
            'project': 'testproject',
            'pipelineName': 'test-pipeline'
        }
        mock_pat.return_value = 'test-pat'
        mock_client = MagicMock()
        mock_client_class.return_value = mock_client
        mock_client.delete_pipeline.return_value = False

        result = cmd_azdo_pipeline_delete(force=True)

        assert result == 1
        mock_client.delete_pipeline.assert_called_once_with('test-pipeline')

    @patch('sdo_package.pipelines.get_pipeline_config')
    def test_cmd_azdo_pipeline_delete_config_failure(self, mock_config):
        """Test Azure DevOps pipeline delete with config failure."""
        mock_config.return_value = None

        result = cmd_azdo_pipeline_delete()

        assert result == 1

    @patch('sdo_package.pipelines.get_pipeline_config')
    @patch('sdo_package.pipelines.get_personal_access_token')
    def test_cmd_azdo_pipeline_delete_missing_pat(self, mock_pat, mock_config):
        """Test Azure DevOps pipeline delete with missing PAT."""
        mock_config.return_value = {
            'organization': 'testorg',
            'project': 'testproject',
            'pipelineName': 'test-pipeline'
        }
        mock_pat.return_value = None

        result = cmd_azdo_pipeline_delete()

        assert result == 1

    @patch('sdo_package.pipelines.get_pipeline_config')
    def test_cmd_pipeline_create_github(self, mock_config):
        """Test pipeline create command for GitHub."""
        mock_config.return_value = {'platform': 'github'}

        result = cmd_pipeline_create()

        assert result == 0

    @patch('sdo_package.pipelines.get_pipeline_config')
    def test_cmd_pipeline_create_azdo(self, mock_config):
        """Test pipeline create command for Azure DevOps."""
        mock_config.return_value = {'platform': 'azdo'}

        # This would normally call cmd_azdo_pipeline_create
        # For testing, we just check it doesn't crash
        result = cmd_pipeline_create()

        assert isinstance(result, int)

    @patch('sdo_package.pipelines.get_pipeline_config')
    def test_cmd_pipeline_create_invalid_platform(self, mock_config):
        """Test pipeline create command with invalid platform."""
        mock_config.return_value = {'platform': 'invalid'}

        result = cmd_pipeline_create()

        assert result == 1

    @patch('sdo_package.pipelines.get_pipeline_config')
    def test_cmd_pipeline_show_github(self, mock_config):
        """Test pipeline show command for GitHub."""
        mock_config.return_value = {
            'platform': 'github',
            'workflowName': 'test-workflow'
        }

        with patch('sdo_package.pipelines.cmd_github_workflow_view') as mock_cmd:
            mock_cmd.return_value = 0
            result = cmd_pipeline_show()
            assert result == 0

    @patch('sdo_package.pipelines.get_pipeline_config')
    def test_cmd_pipeline_show_azdo(self, mock_config):
        """Test pipeline show command for Azure DevOps."""
        mock_config.return_value = {'platform': 'azdo'}

        # This would normally call cmd_azdo_pipeline_show
        result = cmd_pipeline_show()

        assert isinstance(result, int)

    @patch('sdo_package.pipelines.get_pipeline_config')
    def test_cmd_pipeline_list_github(self, mock_config):
        """Test pipeline list command for GitHub."""
        mock_config.return_value = {'platform': 'github'}

        with patch('sdo_package.pipelines.cmd_github_workflow_list') as mock_cmd:
            mock_cmd.return_value = 0
            result = cmd_pipeline_list()
            assert result == 0

    @patch('sdo_package.pipelines.get_pipeline_config')
    def test_cmd_pipeline_run_github(self, mock_config):
        """Test pipeline run command for GitHub."""
        mock_config.return_value = {
            'platform': 'github',
            'workflowName': 'test-workflow',
            'branch': 'main'
        }

        with patch('sdo_package.pipelines.cmd_github_workflow_run') as mock_cmd:
            mock_cmd.return_value = 0
            result = cmd_pipeline_run('main')  # Add the required branch argument
            assert result == 0

    @patch('sdo_package.pipelines.get_pipeline_config')
    def test_cmd_pipeline_status_github(self, mock_config):
        """Test pipeline status command for GitHub."""
        mock_config.return_value = {'platform': 'github'}

        with patch('sdo_package.pipelines.cmd_github_run_view') as mock_cmd:
            mock_cmd.return_value = 0
            result = cmd_pipeline_status(123)
            assert result == 0

    @patch('sdo_package.pipelines.get_pipeline_config')
    def test_cmd_pipeline_status_github_no_build_id(self, mock_config):
        """Test pipeline status command for GitHub with no build_id (shows latest run details for default workflow)."""
        mock_config.return_value = {
            'platform': 'github',
            'workflowYamlPath': '.github/workflows/ntools.yml',
            'workflowName': 'ntools Workflow'
        }

        with patch('sdo_package.pipelines._check_gh_cli', return_value=True), \
             patch('sdo_package.pipelines._run_gh_command') as mock_run_cmd, \
             patch('sdo_package.pipelines.cmd_github_run_view') as mock_view:

            # Mock the run list command to return a latest run for the specific workflow
            mock_run_cmd.return_value = (0, '[{"databaseId": 12345, "status": "completed", "name": "ntools Workflow", "conclusion": "success"}]', '')
            mock_view.return_value = 0

            result = cmd_pipeline_status()

            assert result == 0
            mock_run_cmd.assert_called_with([
                "run", "list", 
                "--workflow", "ntools.yml",
                "--limit", "1", 
                "--json", "databaseId,status,name,conclusion"
            ], False)
            mock_view.assert_called_with("12345", False)

    @patch('sdo_package.pipelines.get_pipeline_config')
    def test_cmd_pipeline_logs_github(self, mock_config):
        """Test pipeline logs command for GitHub."""
        mock_config.return_value = {'platform': 'github'}

        with patch('sdo_package.pipelines.cmd_github_run_logs') as mock_cmd:
            mock_cmd.return_value = 0
            result = cmd_pipeline_logs(123)
            assert result == 0

    @patch('sdo_package.pipelines.get_pipeline_config')
    def test_cmd_pipeline_lastbuild_github(self, mock_config):
        """Test pipeline lastbuild command for GitHub."""
        mock_config.return_value = {
            'platform': 'github',
            'workflowYamlPath': '.github/workflows/ci.yml',
            'workflowName': 'ci',
            'owner': 'test-owner',
            'repo': 'test-repo'
        }

        with patch('sdo_package.pipelines._check_gh_cli', return_value=True), \
             patch('sdo_package.pipelines._run_gh_command') as mock_run_cmd:

            # Mock the run list command to return a latest run
            mock_run_cmd.return_value = (0, '[{"databaseId": 12345, "status": "completed", "conclusion": "success", "name": "CI Workflow", "headBranch": "main", "headSha": "abc123", "createdAt": "2023-01-01T00:00:00Z", "updatedAt": "2023-01-01T00:05:00Z", "event": "push"}]', '')

            result = cmd_pipeline_lastbuild()

            assert result == 0
            # Verify the correct command was called
            mock_run_cmd.assert_called_with([
                "run", "list",
                "--workflow", "ci.yml",
                "--limit", "1",
                "--json", "databaseId,status,conclusion,name,headBranch,headSha,createdAt,updatedAt,event"
            ], False)

    @patch('sdo_package.pipelines.get_pipeline_config')
    def test_cmd_pipeline_delete_github(self, mock_config):
        """Test pipeline delete command for GitHub."""
        mock_config.return_value = {'platform': 'github'}

        result = cmd_pipeline_delete()

        assert result == 0

    @patch('sdo_package.pipelines.get_pipeline_config')
    def test_cmd_pipeline_delete_azdo_with_pipeline_name(self, mock_config):
        """Test pipeline delete command for Azure DevOps with specific pipeline name."""
        mock_config.return_value = {'platform': 'azdo'}

        with patch('sdo_package.pipelines.cmd_azdo_pipeline_delete') as mock_cmd:
            mock_cmd.return_value = 0
            result = cmd_pipeline_delete(pipeline_name='my-pipeline')
            assert result == 0
            mock_cmd.assert_called_once_with(pipeline_name='my-pipeline', force=False, verbose=False)

    @patch('sdo_package.pipelines.get_pipeline_config')
    def test_cmd_pipeline_delete_azdo_with_force(self, mock_config):
        """Test pipeline delete command for Azure DevOps with force flag."""
        mock_config.return_value = {'platform': 'azdo'}

        with patch('sdo_package.pipelines.cmd_azdo_pipeline_delete') as mock_cmd:
            mock_cmd.return_value = 0
            result = cmd_pipeline_delete(pipeline_name='my-pipeline', force=True)
            assert result == 0
            mock_cmd.assert_called_once_with(pipeline_name='my-pipeline', force=True, verbose=False)

    @patch('sdo_package.pipelines.get_pipeline_config')
    def test_cmd_pipeline_delete_azdo_with_verbose(self, mock_config):
        """Test pipeline delete command for Azure DevOps with verbose flag."""
        mock_config.return_value = {'platform': 'azdo'}

        with patch('sdo_package.pipelines.cmd_azdo_pipeline_delete') as mock_cmd:
            mock_cmd.return_value = 0
            result = cmd_pipeline_delete(pipeline_name='my-pipeline', force=False, verbose=True)
            assert result == 0
            mock_cmd.assert_called_once_with(pipeline_name='my-pipeline', force=False, verbose=True)

    @patch('sdo_package.pipelines.get_pipeline_config')
    def test_cmd_pipeline_delete_azdo_auto_detect(self, mock_config):
        """Test pipeline delete command for Azure DevOps with auto-detected pipeline name."""
        mock_config.return_value = {'platform': 'azdo'}

        with patch('sdo_package.pipelines.cmd_azdo_pipeline_delete') as mock_cmd:
            mock_cmd.return_value = 0
            result = cmd_pipeline_delete()  # No pipeline_name provided
            assert result == 0
            mock_cmd.assert_called_once_with(pipeline_name=None, force=False, verbose=False)

    @patch('sdo_package.pipelines.get_pipeline_config')
    def test_cmd_pipeline_update_github(self, mock_config):
        """Test pipeline update command for GitHub."""
        mock_config.return_value = {'platform': 'github'}

        result = cmd_pipeline_update()

        assert result == 0