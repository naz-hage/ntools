"""
SDO CLI - Click-based command line interface.
"""

import click
import sys
from typing import Optional

from .exceptions import SDOError, ConfigurationError, ValidationError
from .work_items import cmd_workitem_create


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
    # Ensure object exists for storing global options
    ctx.ensure_object(dict)
    ctx.obj['verbose'] = verbose
    
    if version:
        from . import __version__, __author__
        click.echo(f"*** sdo, Simple DevOps Operations Tool, {__author__}, 2024 - Version: {__version__} ***")
        sys.exit(0)


@cli.group()
@click.pass_context
def workitem(ctx):
    """Work item operations."""
    pass


@workitem.command()
@click.option('--file-path', '-f', type=click.Path(exists=True, readable=True), 
              required=True, help='Path to markdown file containing work item details')
@click.option('--dry-run', is_flag=True, help='Parse and preview work item creation without creating it')
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
    
    # Handle the case where args is a list vs being called by Click
    if isinstance(args, list):
        cli(args, standalone_mode=False)
    else:
        cli()


if __name__ == '__main__':
    main()