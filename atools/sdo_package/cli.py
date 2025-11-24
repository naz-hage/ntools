"""
SDO CLI - Click-based command line interface.
"""

import click
import os
import sys
sys.path.insert(0, os.path.dirname(os.path.dirname(__file__)))

from .exceptions import SDOError, ConfigurationError, ValidationError
from .work_items import cmd_workitem_create
from .repositories import (
    cmd_repo_create,
    cmd_repo_show,
    cmd_repo_list,
    cmd_repo_delete
)
from .version import __version__


# Define the CLI docstring with version
CLI_DOCSTRING = f"""SDO {__version__} - Simple DevOps Operations Tool

A modern CLI tool for Azure DevOps and GitHub operations.

PLATFORM SUPPORT:
- Azure DevOps: Work items (PBIs, Tasks, Bugs) and repository management
- GitHub: Work items (Issues, Pull Requests) and repository management

Environment Variables:
    AZURE_DEVOPS_PAT    - Personal Access Token for Azure DevOps (required for Azure DevOps operations)

Requirements:
    GitHub CLI (gh)     - Required for GitHub operations (install from https://cli.github.com/)
"""


class ClickArgs:
    """Adapter to convert Click context to arguments object for compatibility."""

    def __init__(self, ctx: click.Context):
        self.ctx = ctx
        # Store global options
        self.verbose = ctx.obj.get('verbose', False) if ctx.obj else False

    def __getattr__(self, name):
        """Allow access to click parameters as attributes."""
        if hasattr(self.ctx, 'params'):
            return self.ctx.params.get(name)
        return None


@click.group(help=CLI_DOCSTRING)
@click.option('--verbose', '-v', is_flag=True, help='Show detailed API error information')
@click.version_option(version=__version__, prog_name='SDO')
@click.pass_context
def cli(ctx, verbose):
    # Store global options in context
    ctx.ensure_object(dict)
    ctx.obj['verbose'] = verbose


@cli.group()
@click.pass_context
def workitem(ctx):
    """Work item operations."""
    pass


@workitem.command()
@click.option('--file-path', '-f', type=click.Path(exists=True, readable=True),
              required=True,
              help='Path to markdown file containing work item details')
@click.option('--dry-run', is_flag=True,
              help='Parse and preview work item creation without creating it')
@click.option('--verbose', '-v', is_flag=True,
              help='Show detailed API information and responses')
@click.pass_context
def create(ctx, file_path, dry_run, verbose):
    """Create a work item from markdown file.

    Examples:
        sdo workitem create --file-path workitem.md
        sdo workitem create -f workitem.md --dry-run
        sdo workitem create -f workitem.md --verbose
    """
    try:
        # Convert click context to args object for compatibility
        args = ClickArgs(ctx)
        args.file_path = file_path
        args.dry_run = dry_run
        args.verbose = verbose

        # Call the business logic
        result = cmd_workitem_create(args)

        if not result and not dry_run:
            sys.exit(1)

    except (SDOError, ConfigurationError, ValidationError) as e:
        click.echo(f"❌ {str(e)}", err=True)
        if ctx.obj.get('verbose'):
            click.echo(f"   Error type: {type(e).__name__}", err=True)
            if hasattr(e, 'details'):
                click.echo(f"   Details: {e.details}", err=True)
        sys.exit(1)
    except Exception as e:
        click.echo(f"❌ Unexpected error: {str(e)}", err=True)
        if ctx.obj.get('verbose'):
            import traceback
            click.echo(f"   Full traceback:\n{traceback.format_exc()}", err=True)
        sys.exit(1)


@cli.group()
@click.pass_context
def repo(ctx):
    """Repository operations."""
    pass


@repo.command()
@click.option('--verbose', '-v', is_flag=True,
              help='Show detailed API information and responses')
@click.pass_context
def create(ctx, verbose):
    """Create a repository in the current project.

    The repository name is extracted from the current Git remote.
    If the repository already exists, no action is taken.

    Examples:
        sdo repo create
        sdo repo create --verbose
    """
    try:
        # Call the business logic
        result = cmd_repo_create(verbose=verbose)

        if result != 0:
            sys.exit(result)

    except (SDOError, ConfigurationError, ValidationError) as e:
        click.echo(f"❌ {str(e)}", err=True)
        if ctx.obj.get('verbose'):
            click.echo(f"   Error type: {type(e).__name__}", err=True)
            if hasattr(e, 'details'):
                click.echo(f"   Details: {e.details}", err=True)
        sys.exit(1)
    except Exception as e:
        click.echo(f"❌ Unexpected error: {str(e)}", err=True)
        if ctx.obj.get('verbose'):
            import traceback
            click.echo(f"   Full traceback:\n{traceback.format_exc()}", err=True)
        sys.exit(1)


@repo.command()
@click.option('--verbose', '-v', is_flag=True,
              help='Show detailed API information and responses')
@click.pass_context
def show(ctx, verbose):
    """Show information about the current repository.

    The repository name is extracted from the current Git remote.

    Examples:
        sdo repo show
        sdo repo show --verbose
    """
    try:
        # Call the business logic
        result = cmd_repo_show(verbose=verbose)

        if result != 0:
            sys.exit(result)

    except (SDOError, ConfigurationError, ValidationError) as e:
        click.echo(f"❌ {str(e)}", err=True)
        if ctx.obj.get('verbose'):
            click.echo(f"   Error type: {type(e).__name__}", err=True)
            if hasattr(e, 'details'):
                click.echo(f"   Details: {e.details}", err=True)
        sys.exit(1)
    except Exception as e:
        click.echo(f"❌ Unexpected error: {str(e)}", err=True)
        if ctx.obj.get('verbose'):
            import traceback
            click.echo(f"   Full traceback:\n{traceback.format_exc()}", err=True)
        sys.exit(1)


@repo.command()
@click.option('--verbose', '-v', is_flag=True,
              help='Show detailed API information and responses')
@click.pass_context
def ls(ctx, verbose):
    """List all repositories in the current project.

    Examples:
        sdo repo ls
        sdo repo ls --verbose
    """
    try:
        # Call the business logic
        result = cmd_repo_list(verbose=verbose)

        if result != 0:
            sys.exit(result)

    except (SDOError, ConfigurationError, ValidationError) as e:
        click.echo(f"❌ {str(e)}", err=True)
        if ctx.obj.get('verbose'):
            click.echo(f"   Error type: {type(e).__name__}", err=True)
            if hasattr(e, 'details'):
                click.echo(f"   Details: {e.details}", err=True)
        sys.exit(1)
    except Exception as e:
        click.echo(f"❌ Unexpected error: {str(e)}", err=True)
        if ctx.obj.get('verbose'):
            import traceback
            click.echo(f"   Full traceback:\n{traceback.format_exc()}", err=True)
        sys.exit(1)


@repo.command()
@click.option('--verbose', '-v', is_flag=True,
              help='Show detailed API information and responses')
@click.pass_context
def delete(ctx, verbose):
    """Delete the current repository.

    ⚠️  WARNING: This action cannot be undone!

    The repository name is extracted from the current Git remote.
    You will be prompted to confirm before deletion.

    Examples:
        sdo repo delete
        sdo repo delete --verbose
    """
    try:
        # Call the business logic
        result = cmd_repo_delete(verbose=verbose)

        if result != 0:
            sys.exit(result)

    except (SDOError, ConfigurationError, ValidationError) as e:
        click.echo(f"❌ {str(e)}", err=True)
        if ctx.obj.get('verbose'):
            click.echo(f"   Error type: {type(e).__name__}", err=True)
            if hasattr(e, 'details'):
                click.echo(f"   Details: {e.details}", err=True)
        sys.exit(1)
    except Exception as e:
        click.echo(f"❌ Unexpected error: {str(e)}", err=True)
        if ctx.obj.get('verbose'):
            import traceback
            click.echo(f"   Full traceback:\n{traceback.format_exc()}", err=True)
        sys.exit(1)


def main(args=None):
    """Main entry point for the CLI."""
    if args is None:
        args = sys.argv[1:]

    # Handle version option early
    if '--version' in args or '-v' in args:
        click.echo(f"SDO version {__version__}")
        sys.exit(0)

    try:
        # Handle the case where args is a list vs being called by Click
        if isinstance(args, list):
            cli(args, standalone_mode=False)
        else:
            cli()
    except click.exceptions.UsageError as e:
        click.echo(f"❌ {str(e)}", err=True)
        click.echo()
        click.echo("Run 'sdo --help' for available commands.", err=True)
        sys.exit(1)
    except click.exceptions.ClickException as e:
        click.echo(f"❌ {str(e)}", err=True)
        sys.exit(1)
    except Exception as e:
        click.echo(f"❌ Unexpected error: {str(e)}", err=True)
        sys.exit(1)


# Set the CLI app name for tests
cli.name = "sdo"


# Add compatibility function for tests that expect add_issue
@click.command()
@click.argument('file_path', type=click.Path(exists=True))
@click.option('--verbose', is_flag=True, help='Show verbose output')
@click.option('--dry-run', is_flag=True, help='Dry run mode')
def add_issue(file_path, verbose, dry_run):
    """Create an issue from markdown file - compatibility function for tests."""
    from .work_items import WorkItemManager

    if dry_run:
        click.echo("[dry-run] Would create work item from file: {}".format(file_path))
        click.echo("[dry-run] Work item creation suppressed")
        return

    manager = WorkItemManager(verbose=verbose)
    result = manager.create_work_item(file_path)

    if result.success:
        click.echo(f"Success: Created work item {result.work_item_id}")
        if result.url:
            click.echo(f"URL: {result.url}")
    else:
        click.echo(f"Error: {result.error_message}")
        raise click.ClickException(result.error_message)


if __name__ == '__main__':
    main()
