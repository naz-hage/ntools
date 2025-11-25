"""

Comprehensive tests for SDO CLI command-line interface.

Tests command parsing, validation, error handling, and integration.

"""

import pytest

from unittest.mock import patch, MagicMock

from click.testing import CliRunner

import sys

from pathlib import Path

# Add the atools directory to sys.path

sys.path.insert(0, str(Path(__file__).parent.parent))

from sdo_package import cli

from sdo_package.exceptions import SDOError, ConfigurationError, ValidationError

class TestCLIBasics:

    """Test basic CLI functionality."""

    def setup_method(self):

        """Set up test fixtures."""

        self.runner = CliRunner()

    def test_cli_help_command(self):

        """Test that CLI help command works."""

        result = self.runner.invoke(cli.cli, ["--help"])

        assert result.exit_code == 0

        assert "SDO" in result.output

        assert "Simple DevOps Operations Tool" in result.output

        assert "workitem" in result.output

        assert "repo" in result.output

    def test_cli_version_command(self):
        """Test that CLI version command works."""
        result = self.runner.invoke(cli.cli, ["--version"])

        assert result.exit_code == 0
        assert "SDO" in result.output

    def test_cli_verbose_flag(self):

        """Test that verbose flag is accepted."""

        result = self.runner.invoke(cli.cli, ["--verbose", "--help"])

        assert result.exit_code == 0

    def test_cli_unknown_command(self):

        """Test that unknown commands show appropriate error."""

        result = self.runner.invoke(cli.cli, ["unknown-command"])

        assert result.exit_code == 2  # Click error code for unknown command

        assert "No such command" in result.output

    def test_cli_no_command(self):

        """Test that running CLI without command shows help."""

        result = self.runner.invoke(cli.cli, [])

        assert result.exit_code == 0

        assert "SDO" in result.output

class TestWorkitemCommands:

    """Test workitem command group."""

    def setup_method(self):

        """Set up test fixtures."""

        self.runner = CliRunner()

    def test_workitem_help(self):

        """Test workitem command help."""

        result = self.runner.invoke(cli.cli, ["workitem", "--help"])

        assert result.exit_code == 0

        assert "Work item operations" in result.output

        assert "create" in result.output

    def test_workitem_create_help(self):

        """Test workitem create command help."""

        result = self.runner.invoke(cli.cli, ["workitem", "create", "--help"])

        assert result.exit_code == 0

        assert "Create a work item from markdown file" in result.output

        assert "--file-path" in result.output

        assert "--dry-run" in result.output

    @patch("sdo_package.cli.cmd_workitem_create")

    def test_workitem_create_success(self, mock_cmd):

        """Test successful workitem create command."""

        mock_cmd.return_value = True

        with self.runner.isolated_filesystem():

            # Create a test markdown file

            with open("test.md", "w") as f:

                f.write("# Test Work Item\n\nDescription here")

            result = self.runner.invoke(cli.cli, ["workitem", "create", "--file-path", "test.md"])

            assert result.exit_code == 0

            mock_cmd.assert_called_once()

    @patch("sdo_package.cli.cmd_workitem_create")

    def test_workitem_create_missing_file(self, mock_cmd):

        """Test workitem create with missing file."""

        result = self.runner.invoke(cli.cli, ["workitem", "create", "--file-path", "nonexistent.md"])

        assert result.exit_code == 2  # Click validation error

        assert "does not exist" in result.output.lower()

    @patch("sdo_package.cli.cmd_workitem_create")

    def test_workitem_create_dry_run(self, mock_cmd):

        """Test workitem create with dry-run flag."""

        mock_cmd.return_value = True

        with self.runner.isolated_filesystem():

            with open("test.md", "w") as f:

                f.write("# Test Work Item\n\nDescription here")

            result = self.runner.invoke(cli.cli, ["workitem", "create", "--file-path", "test.md", "--dry-run"])

            assert result.exit_code == 0

            mock_cmd.assert_called_once()

    @patch("sdo_package.cli.cmd_workitem_create")

    def test_workitem_create_business_logic_error(self, mock_cmd):

        """Test workitem create when business logic returns error."""

        mock_cmd.return_value = False

        with self.runner.isolated_filesystem():

            with open("test.md", "w") as f:

                f.write("# Test Work Item\n\nDescription here")

            result = self.runner.invoke(cli.cli, ["workitem", "create", "--file-path", "test.md"])

            assert result.exit_code == 1

    @patch("sdo_package.cli.cmd_workitem_create")

    def test_workitem_create_sdo_error(self, mock_cmd):

        """Test workitem create with SDO error."""

        mock_cmd.side_effect = SDOError("Test error")

        with self.runner.isolated_filesystem():

            with open("test.md", "w") as f:

                f.write("# Test Work Item\n\nDescription here")

            result = self.runner.invoke(cli.cli, ["workitem", "create", "--file-path", "test.md"])

            assert result.exit_code == 1

            assert "❌ Test error" in result.output

    @patch("sdo_package.cli.cmd_workitem_create")

    def test_workitem_create_sdo_error_verbose(self, mock_cmd):

        """Test workitem create with SDO error in verbose mode."""

        mock_cmd.side_effect = SDOError("Test error")

        with self.runner.isolated_filesystem():

            with open("test.md", "w") as f:

                f.write("# Test Work Item\n\nDescription here")

            result = self.runner.invoke(cli.cli, ["--verbose", "workitem", "create", "--file-path", "test.md"])

            assert result.exit_code == 1

            assert "❌ Test error" in result.output

            assert "Error type:" in result.output

class TestRepoCommands:

    """Test repository command group."""

    def setup_method(self):

        """Set up test fixtures."""

        self.runner = CliRunner()

    def test_repo_help(self):

        """Test repo command help."""

        result = self.runner.invoke(cli.cli, ["repo", "--help"])

        assert result.exit_code == 0

        assert "Repository operations" in result.output

        assert "create" in result.output

        assert "show" in result.output

        assert "ls" in result.output

        assert "delete" in result.output

    def test_repo_create_help(self):

        """Test repo create command help."""

        result = self.runner.invoke(cli.cli, ["repo", "create", "--help"])

        assert result.exit_code == 0

        assert "Create a repository in the current project" in result.output

    def test_repo_show_help(self):

        """Test repo show command help."""

        result = self.runner.invoke(cli.cli, ["repo", "show", "--help"])

        assert result.exit_code == 0

        assert "Show information about the current repository" in result.output

    def test_repo_ls_help(self):

        """Test repo ls command help."""

        result = self.runner.invoke(cli.cli, ["repo", "ls", "--help"])

        assert result.exit_code == 0

        assert "List all repositories" in result.output

    def test_repo_delete_help(self):

        """Test repo delete command help."""

        result = self.runner.invoke(cli.cli, ["repo", "delete", "--help"])

        assert result.exit_code == 0

        assert "Delete the current repository" in result.output

        # Remove all broken decorators and code below this line
    @patch("sdo_package.cli.cmd_repo_create")
    def test_repo_create_success(self, mock_cmd):
        """Test successful repo create command."""
        mock_cmd.return_value = 0
        result = self.runner.invoke(cli.cli, ["repo", "create"])
        assert result.exit_code == 0
        mock_cmd.assert_called_once_with(verbose=False)

    @patch("sdo_package.cli.cmd_repo_create")
    def test_repo_create_verbose(self, mock_cmd):
        """Test repo create with verbose flag."""
        mock_cmd.return_value = 0
        result = self.runner.invoke(cli.cli, ["repo", "create", "--verbose"])
        assert result.exit_code == 0
        mock_cmd.assert_called_once_with(verbose=True)

    @patch("sdo_package.cli.cmd_repo_create")
    def test_repo_create_error(self, mock_cmd):
        """Test repo create with business logic error."""
        mock_cmd.return_value = 1
        result = self.runner.invoke(cli.cli, ["repo", "create"])
        assert result.exit_code == 1

    @patch("sdo_package.cli.cmd_repo_show")
    def test_repo_show_success(self, mock_cmd):
        """Test successful repo show command."""
        mock_cmd.return_value = 0
        result = self.runner.invoke(cli.cli, ["repo", "show"])
        assert result.exit_code == 0
        mock_cmd.assert_called_once_with(verbose=False)

    @patch("sdo_package.cli.cmd_repo_list")
    def test_repo_ls_success(self, mock_cmd):
        """Test successful repo ls command."""
        mock_cmd.return_value = 0
        result = self.runner.invoke(cli.cli, ["repo", "ls"])
        assert result.exit_code == 0
        mock_cmd.assert_called_once_with(verbose=False)

    @patch("sdo_package.cli.cmd_repo_delete")
    def test_repo_delete_success(self, mock_cmd):
        """Test successful repo delete command."""
        mock_cmd.return_value = 0
        result = self.runner.invoke(cli.cli, ["repo", "delete"])
        assert result.exit_code == 0
        mock_cmd.assert_called_once_with(verbose=False)

class TestClickArgsAdapter:

    """Test the ClickArgs adapter class."""

    def test_click_args_creation(self):

        """Test ClickArgs adapter creation."""

        mock_ctx = MagicMock()

        mock_ctx.obj = {'verbose': True}

        mock_ctx.params = {'test_param': 'test_value'}

        args = cli.ClickArgs(mock_ctx)

        assert args.verbose == True

        assert args.test_param == 'test_value'

    def test_click_args_missing_attribute(self):

        """Test ClickArgs handles missing attributes."""

        mock_ctx = MagicMock()

        mock_ctx.obj = {}

        mock_ctx.params = {}

        args = cli.ClickArgs(mock_ctx)

        assert args.missing_attr is None

        assert args.verbose == False

class TestMainFunction:

    """Test the main entry point function."""

    @patch("sys.argv", ["sdo", "--version"])

    @patch("sdo_package.cli.cli")

    def test_main_version_handling(self, mock_cli):

        """Test main function handles version option."""

        with patch("sys.exit") as mock_exit:

            cli.main()

            mock_exit.assert_called_once_with(0)

    @patch("sdo_package.cli.cli")

    def test_main_with_args_list(self, mock_cli):

        """Test main function with args as list."""

        cli.main(["--help"])

        mock_cli.assert_called_once_with(["--help"], standalone_mode=False)


    @patch("sdo_package.cli.cli")
    def test_main_without_args(self, mock_cli):
        """Test main function without args."""
        cli.main([])
        mock_cli.assert_called_once_with([], standalone_mode=False)

