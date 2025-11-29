"""
SDO CLI - Click-based command line interface.
"""

import click
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.dirname(__file__)))

from .exceptions import SDOError, ConfigurationError, ValidationError  # noqa: E402
from .work_items import (  # noqa: E402
    cmd_workitem_create,
    cmd_workitem_list,
    cmd_workitem_show,
    cmd_workitem_update,
    cmd_workitem_comment,
)
from .repositories import (  # noqa: E402
    cmd_repo_create,
    cmd_repo_show,
    cmd_repo_list,
    cmd_repo_delete,
)
from .pull_requests import (  # noqa: E402
    cmd_pr_create,
    cmd_pr_show,
    cmd_pr_list,
    cmd_pr_update,
)
from .version import __version__  # noqa: E402


# Define the CLI docstring with version
CLI_DOCSTRING = f"""SDO {__version__} - Simple DevOps Operations Tool

A modern CLI tool for Azure DevOps and GitHub operations.

PLATFORM SUPPORT:
- Azure DevOps: Work items (PBIs, Tasks, Bugs, Epics), repositories, and pull requests
- GitHub: Work items (Issues), repositories, and pull requests

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
        self.verbose = ctx.obj.get("verbose", False) if ctx.obj else False

    def __getattr__(self, name):
        """Allow access to click parameters as attributes."""
        if hasattr(self.ctx, "params"):
            return self.ctx.params.get(name)
        return None


@click.group(help=CLI_DOCSTRING)
@click.option("--verbose", "-v", is_flag=True, help="Show detailed API error information")
@click.version_option(version=__version__, prog_name="SDO")
@click.pass_context
def cli(ctx, verbose):
    # Store global options in context
    ctx.ensure_object(dict)
    ctx.obj["verbose"] = verbose


@cli.group()
@click.pass_context
def workitem(ctx):
    """Work item operations."""
    pass


@workitem.command()
@click.option(
    "--file-path",
    "-f",
    type=click.Path(exists=True, readable=True),
    required=True,
    help="Path to markdown file containing work item details",
)
@click.option(
    "--dry-run", is_flag=True, help="Parse and preview work item creation without creating it"
)
@click.option("--verbose", "-v", is_flag=True, help="Show detailed API information and responses")
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
        if ctx.obj.get("verbose"):
            click.echo(f"   Error type: {type(e).__name__}", err=True)
            if hasattr(e, "details"):
                click.echo(f"   Details: {e.details}", err=True)
        sys.exit(1)
    except Exception as e:
        click.echo(f"❌ Unexpected error: {str(e)}", err=True)
        if ctx.obj.get("verbose"):
            import traceback

            click.echo(f"   Full traceback:\n{traceback.format_exc()}", err=True)
        sys.exit(1)


@workitem.command()
@click.option(
    "--type",
    type=click.Choice(["PBI", "Bug", "Task", "Spike", "Epic"], case_sensitive=False),
    help="Filter by work item type",
)
@click.option(
    "--state",
    type=click.Choice(
        ["New", "Approved", "Committed", "Done", "To Do", "In Progress"], case_sensitive=False
    ),
    help="Filter by state",
)
@click.option("--assigned-to", help="Filter by assigned user (email or display name)")
@click.option(
    "--assigned-to-me", is_flag=True, help="Filter by work items assigned to current user"
)
@click.option("--top", type=int, default=50, help="Maximum number of items to return (default: 50)")
@click.option("--verbose", "-v", is_flag=True, help="Show detailed API information and URLs")
@click.pass_context
def list(ctx, type, state, assigned_to, assigned_to_me, top, verbose):
    """List work items with optional filtering.

    Examples:
        sdo workitem list
        sdo workitem list --type Task --state "In Progress"
        sdo workitem list --assigned-to-me
        sdo workitem list --type PBI --state New --top 10
    """
    try:
        # Convert click context to args object for compatibility
        args = ClickArgs(ctx)
        args.type = type
        args.state = state
        args.assigned_to = assigned_to
        args.assigned_to_me = assigned_to_me
        args.top = top
        args.verbose = verbose

        # Call the business logic
        result = cmd_workitem_list(args)
        if result != 0:
            sys.exit(result)

    except (SDOError, ConfigurationError, ValidationError) as e:
        click.echo(f"❌ {str(e)}", err=True)
        if ctx.obj.get("verbose"):
            click.echo(f"   Error type: {type(e).__name__}", err=True)
        sys.exit(1)
    except Exception as e:
        click.echo(f"❌ Unexpected error: {str(e)}", err=True)
        if ctx.obj.get("verbose"):
            import traceback

            click.echo(f"   Full traceback:\n{traceback.format_exc()}", err=True)
        sys.exit(1)


@workitem.command()
@click.option("--id", required=True, type=int, help="Work item ID")
@click.option("--comments", "-c", is_flag=True, help="Show comments/discussion")
@click.option("--verbose", "-v", is_flag=True, help="Show full API response")
@click.pass_context
def show(ctx, id, comments, verbose):
    """Show detailed work item information.

    Examples:
        sdo workitem show --id 123
        sdo workitem show --id 123 --comments
        sdo workitem show --id 123 --verbose
    """
    try:
        # Convert click context to args object for compatibility
        args = ClickArgs(ctx)
        args.id = id
        args.comments = comments
        args.verbose = verbose

        # Call the business logic
        result = cmd_workitem_show(args)
        if result != 0:
            sys.exit(result)

    except (SDOError, ConfigurationError, ValidationError) as e:
        click.echo(f"❌ {str(e)}", err=True)
        if ctx.obj.get("verbose"):
            click.echo(f"   Error type: {type(e).__name__}", err=True)
        sys.exit(1)
    except Exception as e:
        click.echo(f"❌ Unexpected error: {str(e)}", err=True)
        if ctx.obj.get("verbose"):
            import traceback

            click.echo(f"   Full traceback:\n{traceback.format_exc()}", err=True)
        sys.exit(1)


@workitem.command()
@click.option("--id", required=True, type=int, help="Work item ID")
@click.option("--title", help="Update title")
@click.option("--description", help="Update description")
@click.option("--assigned-to", help="Update assigned user (email or display name)")
@click.option(
    "--state",
    type=click.Choice(
        ["New", "Approved", "Committed", "Done", "To Do", "In Progress"], case_sensitive=False
    ),
    help="Update state",
)
@click.option("--verbose", "-v", is_flag=True, help="Show full API response")
@click.pass_context
def update(ctx, id, title, description, assigned_to, state, verbose):
    """Update work item fields.

    Examples:
        sdo workitem update --id 123 --title "New Title" --state Done
        sdo workitem update --id 123 --assigned-to "user@company.com"
        sdo workitem update --id 123 --description "Updated description" --state "In Progress"
    """
    try:
        # Convert click context to args object for compatibility
        args = ClickArgs(ctx)
        args.id = id
        args.title = title
        args.description = description
        args.assigned_to = assigned_to
        args.state = state
        args.verbose = verbose

        # Call the business logic
        result = cmd_workitem_update(args)
        if result != 0:
            sys.exit(result)

    except (SDOError, ConfigurationError, ValidationError) as e:
        click.echo(f"❌ {str(e)}", err=True)
        if ctx.obj.get("verbose"):
            click.echo(f"   Error type: {type(e).__name__}", err=True)
        sys.exit(1)
    except Exception as e:
        click.echo(f"❌ Unexpected error: {str(e)}", err=True)
        if ctx.obj.get("verbose"):
            import traceback

            click.echo(f"   Full traceback:\n{traceback.format_exc()}", err=True)
        sys.exit(1)


@workitem.command()
@click.option("--id", required=True, type=int, help="Work item ID")
@click.option("--text", required=True, help="Comment text")
@click.option("--verbose", "-v", is_flag=True, help="Show full API response")
@click.pass_context
def comment(ctx, id, text, verbose):
    """Add comment to work item.

    Examples:
        sdo workitem comment --id 123 --text "Fixed the issue"
        sdo workitem comment --id 123 --text "Work completed successfully" --verbose
    """
    try:
        # Convert click context to args object for compatibility
        args = ClickArgs(ctx)
        args.id = id
        args.text = text
        args.verbose = verbose

        # Call the business logic
        result = cmd_workitem_comment(args)
        if result != 0:
            sys.exit(result)

    except (SDOError, ConfigurationError, ValidationError) as e:
        click.echo(f"❌ {str(e)}", err=True)
        if ctx.obj.get("verbose"):
            click.echo(f"   Error type: {type(e).__name__}", err=True)
        sys.exit(1)
    except Exception as e:
        click.echo(f"❌ Unexpected error: {str(e)}", err=True)
        if ctx.obj.get("verbose"):
            import traceback

            click.echo(f"   Full traceback:\n{traceback.format_exc()}", err=True)
        sys.exit(1)


@cli.group()
@click.pass_context
def repo(ctx):
    """Repository operations."""
    pass


@repo.command()
@click.option("--verbose", "-v", is_flag=True, help="Show detailed API information and responses")
@click.pass_context
def create(ctx, verbose):  # noqa: F811
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
        if ctx.obj.get("verbose"):
            click.echo(f"   Error type: {type(e).__name__}", err=True)
            if hasattr(e, "details"):
                click.echo(f"   Details: {e.details}", err=True)
        sys.exit(1)
    except Exception as e:
        click.echo(f"❌ Unexpected error: {str(e)}", err=True)
        if ctx.obj.get("verbose"):
            import traceback

            click.echo(f"   Full traceback:\n{traceback.format_exc()}", err=True)
        sys.exit(1)


@repo.command()
@click.option("--verbose", "-v", is_flag=True, help="Show detailed API information and responses")
@click.pass_context
def show(ctx, verbose):  # noqa: F811
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
        if ctx.obj.get("verbose"):
            click.echo(f"   Error type: {type(e).__name__}", err=True)
            if hasattr(e, "details"):
                click.echo(f"   Details: {e.details}", err=True)
        sys.exit(1)
    except Exception as e:
        click.echo(f"❌ Unexpected error: {str(e)}", err=True)
        if ctx.obj.get("verbose"):
            import traceback

            click.echo(f"   Full traceback:\n{traceback.format_exc()}", err=True)
        sys.exit(1)


@repo.command()
@click.option("--verbose", "-v", is_flag=True, help="Show detailed API information and responses")
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
        if ctx.obj.get("verbose"):
            click.echo(f"   Error type: {type(e).__name__}", err=True)
            if hasattr(e, "details"):
                click.echo(f"   Details: {e.details}", err=True)
        sys.exit(1)
    except Exception as e:
        click.echo(f"❌ Unexpected error: {str(e)}", err=True)
        if ctx.obj.get("verbose"):
            import traceback

            click.echo(f"   Full traceback:\n{traceback.format_exc()}", err=True)
        sys.exit(1)


@repo.command()
@click.option("--verbose", "-v", is_flag=True, help="Show detailed API information and responses")
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
        if ctx.obj.get("verbose"):
            click.echo(f"   Error type: {type(e).__name__}", err=True)
            if hasattr(e, "details"):
                click.echo(f"   Details: {e.details}", err=True)
        sys.exit(1)
    except Exception as e:
        click.echo(f"❌ Unexpected error: {str(e)}", err=True)
        if ctx.obj.get("verbose"):
            import traceback

            click.echo(f"   Full traceback:\n{traceback.format_exc()}", err=True)
        sys.exit(1)


@cli.group()
@click.pass_context
def pr(ctx):
    """Pull request operations."""


@pr.command()
@click.option(
    "--file",
    "-f",
    type=click.Path(exists=True, readable=True),
    required=True,
    help="Path to markdown file containing PR details",
)
@click.option(
    "--work-item", required=True, type=int, help="Work item ID to link to the pull request"
)
@click.option("--draft", is_flag=True, help="Create as draft pull request")
@click.option("--dry-run", is_flag=True, help="Parse and preview PR creation without creating it")
@click.option("--verbose", "-v", is_flag=True, help="Show detailed API information and responses")
@click.pass_context
def create(ctx, file, work_item, draft, dry_run, verbose):  # noqa: F811
    """Create a pull request from markdown file.

    The markdown file should start with a title (# Title) followed by description.

    Examples:
        sdo pr create --file pr.md --work-item 123
        sdo pr create -f pr.md --work-item 123 --draft
        sdo pr create -f pr.md --work-item 123 --dry-run --verbose
    """
    try:
        # Convert click context to args object for compatibility
        args = ClickArgs(ctx)
        args.file = file
        args.work_item = work_item
        args.draft = draft
        args.dry_run = dry_run
        args.verbose = verbose

        # Call the business logic
        cmd_pr_create(args)

    except (SDOError, ConfigurationError, ValidationError) as e:
        click.echo(f"❌ {str(e)}", err=True)
        if ctx.obj.get("verbose"):
            click.echo(f"   Error type: {type(e).__name__}", err=True)
            if hasattr(e, "details"):
                click.echo(f"   Details: {e.details}", err=True)
        sys.exit(1)
    except Exception as e:
        click.echo(f"❌ Unexpected error: {str(e)}", err=True)
        if ctx.obj.get("verbose"):
            import traceback

            click.echo(f"   Full traceback:\n{traceback.format_exc()}", err=True)
        sys.exit(1)


@pr.command()
@click.argument("pr_id", type=int)
@click.option("--verbose", "-v", is_flag=True, help="Show detailed API information and responses")
@click.pass_context
def show(ctx, pr_id, verbose):  # noqa: F811
    """Show detailed information about a pull request.

    Examples:
        sdo pr show 123
        sdo pr show 123 --verbose
    """
    try:
        # Convert click context to args object for compatibility
        args = ClickArgs(ctx)
        args.pr_number = pr_id
        args.verbose = verbose

        # Call the business logic
        cmd_pr_show(args)

    except (SDOError, ConfigurationError, ValidationError) as e:
        click.echo(f"❌ {str(e)}", err=True)
        if ctx.obj.get("verbose"):
            click.echo(f"   Error type: {type(e).__name__}", err=True)
            if hasattr(e, "details"):
                click.echo(f"   Details: {e.details}", err=True)
        sys.exit(1)
    except Exception as e:
        click.echo(f"❌ Unexpected error: {str(e)}", err=True)
        if ctx.obj.get("verbose"):
            import traceback

            click.echo(f"   Full traceback:\n{traceback.format_exc()}", err=True)
        sys.exit(1)


@pr.command()
@click.argument("pr_number", type=int)
@click.option("--verbose", "-v", is_flag=True, help="Show detailed API information and responses")
@click.pass_context
def status(ctx, pr_number, verbose):
    """Show status of a pull request.

    Examples:
        sdo pr status 123
        sdo pr status 123 --verbose
    """
    from .pull_requests import cmd_pr_status

    args = ClickArgs(ctx)
    args.pr_number = pr_number
    args.verbose = verbose

    cmd_pr_status(args)


@pr.command()
@click.option(
    "--status",
    default="active",
    type=click.Choice(["active", "completed", "abandoned"]),
    help="Filter PRs by status (default: active)",
)
@click.option("--top", default=10, type=int, help="Maximum number of PRs to show (default: 10)")
@click.option("--verbose", "-v", is_flag=True, help="Show detailed API information and responses")
@click.pass_context
def ls(ctx, status, top, verbose):  # noqa: F811
    """List pull requests in the current repository.

    Examples:
        sdo pr ls
        sdo pr ls --status completed --top 20
        sdo pr ls --verbose
    """
    try:
        # Convert click context to args object for compatibility
        args = ClickArgs(ctx)
        args.status = status
        args.top = top
        args.verbose = verbose

        # Call the business logic
        cmd_pr_list(args)

    except (SDOError, ConfigurationError, ValidationError) as e:
        click.echo(f"❌ {str(e)}", err=True)
        if ctx.obj.get("verbose"):
            click.echo(f"   Error type: {type(e).__name__}", err=True)
            if hasattr(e, "details"):
                click.echo(f"   Details: {e.details}", err=True)
        sys.exit(1)
    except Exception as e:
        click.echo(f"❌ Unexpected error: {str(e)}", err=True)
        if ctx.obj.get("verbose"):
            import traceback

            click.echo(f"   Full traceback:\n{traceback.format_exc()}", err=True)
        sys.exit(1)


@pr.command()
@click.option("--pr-id", required=True, type=int, help="Pull request ID to update")
@click.option(
    "--file",
    "-f",
    type=click.Path(exists=True, readable=True),
    help="Path to markdown file with updated PR details",
)
@click.option("--title", "-t", help="New title for the pull request")
@click.option(
    "--status",
    type=click.Choice(["active", "abandoned", "completed"]),
    help="New status for the pull request",
)
@click.option("--verbose", "-v", is_flag=True, help="Show detailed API information and responses")
@click.pass_context
def update(ctx, pr_id, file, title, status, verbose):  # noqa: F811
    """Update an existing pull request.

    Examples:
        sdo pr update --pr-id 123 --title "New Title"
        sdo pr update --pr-id 123 --file updated-pr.md
        sdo pr update --pr-id 123 --status completed
    """
    try:
        # Convert click context to args object for compatibility
        args = ClickArgs(ctx)
        args.pr_id = pr_id
        args.file = file
        args.title = title
        args.status = status
        args.verbose = verbose

        # Call the business logic
        cmd_pr_update(args)

    except (SDOError, ConfigurationError, ValidationError) as e:
        click.echo(f"❌ {str(e)}", err=True)
        if ctx.obj.get("verbose"):
            click.echo(f"   Error type: {type(e).__name__}", err=True)
            if hasattr(e, "details"):
                click.echo(f"   Details: {e.details}", err=True)
        sys.exit(1)
    except Exception as e:
        click.echo(f"❌ Unexpected error: {str(e)}", err=True)
        if ctx.obj.get("verbose"):
            import traceback

            click.echo(f"   Full traceback:\n{traceback.format_exc()}", err=True)
        sys.exit(1)


def main(args=None):
    """Main entry point for the CLI."""
    import builtins  # Import builtins to access the original list type

    if args is None:
        args = sys.argv[1:]

    # Handle version option early
    if "--version" in args or "-v" in args:
        click.echo(f"SDO version {__version__}")
        sys.exit(0)

    try:
        # Handle the case where args is a list vs being called by Click
        # Use builtins.list to avoid conflict with our list() function
        if isinstance(args, builtins.list):
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
        import traceback

        click.echo(f"❌ Unexpected error: {str(e)}", err=True)
        if "--verbose" in args or "-v" in args or "VERBOSE" in os.environ:
            traceback.print_exc()
        sys.exit(1)


# Set the CLI app name for tests
cli.name = "sdo"


# Add compatibility function for tests that expect add_issue
@click.command()
@click.argument("file_path", type=click.Path(exists=True))
@click.option("--verbose", is_flag=True, help="Show verbose output")
@click.option("--dry-run", is_flag=True, help="Dry run mode")
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


if __name__ == "__main__":
    main()
