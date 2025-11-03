"""
SDO CLI - Click-based command line interface.
"""

import click
import sys

from .exceptions import SDOError, ConfigurationError, ValidationError
from .work_items import cmd_workitem_create
from .version import __version__


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


@click.group()
@click.option('--verbose', '-v', is_flag=True, help='Show detailed API error information')
@click.option('--version', is_flag=True, help='Show version information')
@click.pass_context
def cli(ctx, verbose, version):
    """SDO - Simple DevOps Operations Tool

    A modern CLI tool for Azure DevOps operations.

    Environment Variables:
        AZURE_DEVOPS_PAT    - Personal Access Token (required)
        AZURE_DEVOPS_EXT_PAT - Alternative PAT variable
    """
    # Handle version option
    if version:
        click.echo(f"SDO version {__version__}")
        sys.exit(0)

    # Store global options in context
    ctx.ensure_object(dict)
    ctx.obj['verbose'] = verbose

    if version:
        from . import __version__, __author__
        click.echo(f"*** sdo, Simple DevOps Operations Tool, {__author__}, 2024 - "
                   f"Version: {__version__} ***")
        sys.exit(0)


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
@click.pass_context
def create(ctx, file_path, dry_run):
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
