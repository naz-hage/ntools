"""
Pipeline Operations Module for SDO
Contains command handlers for Azure DevOps pipeline and GitHub Actions workflow operations.
"""

import os
import sys
import subprocess
from typing import Optional

# Handle imports for both module and script execution
try:
    from .client import (
        AzureDevOpsClient,
        ensure_config_exists,
        get_personal_access_token,
        extract_platform_info_from_git
    )
except ImportError:
    from client import (
        AzureDevOpsClient,
        ensure_config_exists,
        get_personal_access_token,
        extract_platform_info_from_git
    )

# Constants for error messages
MISSING_PAT_MSG = 'AZURE_DEVOPS_PAT environment variable'
MISSING_CONFIG_MSG = "Please set AZURE_DEVOPS_PAT environment variable."
MISSING_GH_MSG = "GitHub CLI (gh) is required for GitHub operations. Install from https://cli.github.com/"


def get_pipeline_config() -> Optional[dict]:
    """Get pipeline/workflow configuration by extracting from Git remote."""
    # Extract platform information from Git remote
    platform_info = extract_platform_info_from_git()
    if not platform_info:
        print("❌ Could not extract platform info from Git remote.")
        print("Please ensure you're in a Git repository with a supported remote (Azure DevOps or GitHub).")
        return None

    platform = platform_info.get("platform")

    if platform == "azdo":
        print("✓ Detected Azure DevOps repository:")
        print(f"  Organization: {platform_info['organization']}")
        print(f"  Project: {platform_info['project']}")
        print(f"  Repository: {platform_info['repository']}")

        # Set extracted values
        config = platform_info.copy()

        # Set default pipeline values based on repository name
        repo_name = platform_info['repository']
        config['pipelineName'] = repo_name
        config['pipelineYamlPath'] = '.azure-pipelines/azurepipeline.yml'
        config['pipelineFolder'] = '\\'

        print(f"  Pipeline Name: {config['pipelineName']}")
        print(f"  Pipeline YAML Path: {config['pipelineYamlPath']}")
        print(f"  Pipeline Folder: {config['pipelineFolder']}")

        return config

    elif platform == "github":
        print("✓ Detected GitHub repository:")
        print(f"  Owner: {platform_info['owner']}")
        print(f"  Repository: {platform_info['repo']}")

        # Set extracted values
        config = platform_info.copy()

        # Find the default workflow name dynamically
        default_workflow = _find_default_workflow_name()
        if default_workflow:
            config['workflowName'] = default_workflow
        else:
            config['workflowName'] = 'ci'  # Fallback

        config['workflowYamlPath'] = f".github/workflows/{config['workflowName'].lower().replace(' ', '-')}.yml"

        print(f"  Default Workflow: {config['workflowName']}")
        print(f"  Workflow YAML Path: {config['workflowYamlPath']}")

        return config

    else:
        print(f"❌ Unsupported platform: {platform}")
        print("SDO currently supports Azure DevOps and GitHub.")
        return None


def cmd_azdo_pipeline_create(verbose: bool = False) -> int:
    """Handle 'sdo pipeline create' command for Azure DevOps."""
    config = get_pipeline_config()
    if config is None:
        return 1

    # Get personal access token from environment
    pat = get_personal_access_token()

    # Check required parameters
    required = ['organization', 'project', 'pipelineName', 'repository', 'pipelineYamlPath']
    missing = [key for key in required if not config.get(key)]

    if not pat:
        missing.append('AZURE_DEVOPS_PAT environment variable')

    if missing:
        print(f"Missing required configuration: {', '.join(missing)}")
        print("Please set AZURE_DEVOPS_PAT environment variable.")
        return 1

    # Initialize Azure DevOps client
    client = AzureDevOpsClient(
        config['organization'],
        config['project'],
        pat,
        verbose=verbose
    )

    # Check if pipeline already exists
    existing_pipeline = client.get_pipeline(config['pipelineName'])
    if existing_pipeline:
        print(f"Pipeline '{config['pipelineName']}' already exists!")
        print(f"ID: {existing_pipeline['id']}")
        print(f"URL: {existing_pipeline['url']}")
        return 0

    # Create pipeline
    print(f"Creating pipeline '{config['pipelineName']}'...")
    pipeline = client.create_pipeline(
        config['pipelineName'],
        config['repository'],
        config['pipelineYamlPath'],
        config.get('pipelineFolder', '\\')
    )

    if pipeline:
        print("✓ Pipeline created successfully!")
        print(f"ID: {pipeline['id']}")
        print(f"Name: {pipeline['name']}")
        print(f"URL: {pipeline['url']}")
        print(f"Folder: {pipeline['folder']}")
        return 0
    else:
        print("❌ Failed to create pipeline")
        return 1


def cmd_azdo_pipeline_show(verbose: bool = False) -> int:
    """Handle 'sdo pipeline show' command for Azure DevOps."""
    config = get_pipeline_config()
    if config is None:
        return 1

    # Get personal access token from environment
    pat = get_personal_access_token()

    # Check required parameters
    required = ['organization', 'project']
    missing = [key for key in required if not config.get(key)]

    if not pat:
        missing.append('AZURE_DEVOPS_PAT environment variable')

    if missing:
        print(f"Missing required configuration: {', '.join(missing)}")
        print("Please set AZURE_DEVOPS_PAT environment variable.")
        return 1

    # Initialize Azure DevOps client
    client = AzureDevOpsClient(
        config['organization'],
        config['project'],
        pat,
        verbose=verbose
    )

    # Get pipeline name from config (always available from Git extraction)
    pipeline_name = config['pipelineName']

    # Get pipeline information
    pipeline = client.get_pipeline(pipeline_name)

    if pipeline:
        print(f"Pipeline Information:")
        print(f"  ID: {pipeline['id']}")
        print(f"  Name: {pipeline['name']}")
        print(f"  URL: {pipeline['url']}")
        print(f"  Folder: {pipeline.get('folder', 'Unknown')}")
        print(f"  Revision: {pipeline.get('revision', 'Unknown')}")
        return 0
    else:
        print(f"Pipeline '{pipeline_name}' not found.")
        return 1


def cmd_azdo_pipeline_list(verbose: bool = False) -> int:
    """Handle 'sdo pipeline list' command for Azure DevOps."""
    config = get_pipeline_config()
    if config is None:
        return 1

    # Get personal access token from environment
    pat = get_personal_access_token()

    # Check required parameters
    required = ['organization', 'project']
    missing = [key for key in required if not config.get(key)]

    if not pat:
        missing.append('AZURE_DEVOPS_PAT environment variable')

    if missing:
        print(f"Missing required configuration: {', '.join(missing)}")
        print("Please set AZURE_DEVOPS_PAT environment variable.")
        return 1

    # Initialize Azure DevOps client
    client = AzureDevOpsClient(
        config['organization'],
        config['project'],
        pat,
        verbose=verbose
    )

    # List pipelines
    pipelines = client.list_pipelines()

    if pipelines is not None:
        if not pipelines:
            print("No pipelines found in this project.")
            return 0

        print(f"Pipelines in project '{config['project']}' ({len(pipelines)} total):")
        print("-" * 80)
        for pipeline in pipelines:
            print(f"  {pipeline['name']}")
            print(f"    ID: {pipeline['id']}")
            print(f"    URL: {pipeline['url']}")
            print(f"    Folder: {pipeline.get('folder', '\\')}")
            print()
        return 0
    else:
        print("❌ Failed to list pipelines")
        return 1


def cmd_azdo_pipeline_delete(verbose: bool = False) -> int:
    """Handle 'sdo pipeline delete' command for Azure DevOps."""
    # Get configuration (auto-extract from Git if needed)
    config = get_pipeline_config()
    if not config:
        return 1

    # Get personal access token from environment
    pat = get_personal_access_token()

    # Check required parameters
    required = ['organization', 'project']
    missing = [key for key in required if not config.get(key)]

    if not pat:
        missing.append('AZURE_DEVOPS_PAT environment variable')

    if missing:
        print(f"Missing required configuration: {', '.join(missing)}")
        print("Please set AZURE_DEVOPS_PAT environment variable.")
        return 1

    # Get pipeline name from config (always available from Git extraction)
    pipeline_name = config['pipelineName']

    # Confirm deletion
    print(f"⚠️  WARNING: This will permanently delete the pipeline '{pipeline_name}'!")
    print("This action cannot be undone.")
    try:
        confirm = input("Are you sure you want to continue? (yes/no): ").strip().lower()
        if confirm not in ['yes', 'y']:
            print("Pipeline deletion cancelled.")
            return 0
    except KeyboardInterrupt:
        print("\nPipeline deletion cancelled.")
        return 0

    # Initialize Azure DevOps client
    client = AzureDevOpsClient(
        config['organization'],
        config['project'],
        pat,
        verbose=verbose
    )

    # Delete pipeline
    print(f"Deleting pipeline '{pipeline_name}'...")
    if client.delete_pipeline(pipeline_name):
        print("✓ Pipeline deleted successfully!")
        return 0
    else:
        print("❌ Failed to delete pipeline")
        print("Note: Azure DevOps pipelines cannot be deleted via API.")
        return 1


def cmd_azdo_pipeline_run(verbose: bool = False) -> int:
    """Handle 'sdo pipeline run' command for Azure DevOps."""
    # Get configuration (auto-extract from Git if needed)
    config = get_pipeline_config()
    if not config:
        return 1

    # Get personal access token from environment
    pat = get_personal_access_token()

    # Check required parameters
    required = ['organization', 'project']
    missing = [key for key in required if not config.get(key)]

    if not pat:
        missing.append('AZURE_DEVOPS_PAT environment variable')

    if missing:
        print(f"Missing required configuration: {', '.join(missing)}")
        print("Please set AZURE_DEVOPS_PAT environment variable.")
        return 1

    # Get pipeline name from config (always available from Git extraction)
    pipeline_name = config['pipelineName']

    # Get optional parameters
    branch = config.get('branch', 'main')
    parameters = config.get('parameters', {})

    # Initialize Azure DevOps client
    client = AzureDevOpsClient(
        config['organization'],
        config['project'],
        pat
    )

    # Run pipeline
    print(f"Running pipeline '{pipeline_name}' on branch '{branch}'...")
    build = client.run_pipeline(pipeline_name, branch, parameters)

    if build:
        print(f"✓ Pipeline run started successfully! Build ID: {build['id']}")
        print(f"💡 Use 'sdo pipeline status {build['id']}' to check progress.")
        return 0
    else:
        print("❌ Failed to run pipeline")
        return 1


def cmd_azdo_pipeline_status(build_id: int, verbose: bool = False) -> int:
    """Handle 'sdo pipeline status' command for Azure DevOps."""
    # Get configuration (auto-extract from Git if needed)
    config = get_pipeline_config()
    if not config:
        return 1

    # Get personal access token from environment
    pat = get_personal_access_token()

    # Check required parameters
    required = ['organization', 'project']
    missing = [key for key in required if not config.get(key)]

    if not pat:
        missing.append('AZURE_DEVOPS_PAT environment variable')

    if missing:
        print(f"Missing required configuration: {', '.join(missing)}")
        print("Please set AZURE_DEVOPS_PAT environment variable.")
        return 1

    try:
        build_id = int(build_id)
    except ValueError:
        print(f"Invalid build ID: {build_id}. Must be a number.")
        return 1

    # Initialize Azure DevOps client
    client = AzureDevOpsClient(
        config['organization'],
        config['project'],
        pat
    )

    # Get build status
    print(f"Getting status for build ID: {build_id}...")
    build = client.get_build_status(build_id)

    if not build:
        print(f"❌ Build {build_id} not found or access denied")
        return 1

    # Display build information
    print("\n" + "="*60)
    print(f"Build Information - ID: {build['id']}")
    print("="*60)
    print(f"Build Number: {build['buildNumber']}")
    print(f"Status: {build['status']}")
    print(f"Result: {build.get('result', 'N/A')}")

    # Check if build is waiting for approval
    if build['status'] == 'notStarted' and build.get('reason') == 'manual':
        print("⚠️  This build is waiting for manual approval!")
        print("   Go to the build URL to approve or reject it.")
        print("   Use 'sdo pipeline logs <build_id>' to check for any available logs.")
    elif build['status'] == 'notStarted':
        print("⚠️  This build has not started yet.")
        print("   It may be queued or waiting for agent availability.")

    print(f"Pipeline: {build['definition']['name']}")
    print(f"Branch: {build.get('sourceBranch', 'N/A').replace('refs/heads/', '')}")
    print(f"Triggered By: {build['requestedBy']['displayName']}")
    print(f"Start Time: {build.get('startTime', 'N/A')}")
    print(f"Finish Time: {build.get('finishTime', 'N/A')}")
    print(f"URL: {build['url']}")

    # Show build duration if completed
    if build.get('startTime') and build.get('finishTime'):
        from datetime import datetime
        start = datetime.fromisoformat(build['startTime'].replace('Z', '+00:00'))
        finish = datetime.fromisoformat(build['finishTime'].replace('Z', '+00:00'))
        duration = finish - start
        print(f"Duration: {duration}")

    # Get and display timeline information
    print(f"\nJob/Step Details:")
    print("-"*40)
    timeline = client.get_build_timeline(build_id)
    if timeline and 'records' in timeline:
        for record in timeline['records']:
            if record.get('type') == 'Job':
                status = record.get('state', 'Unknown')
                result = record.get('result', '')
                name = record.get('name', 'Unknown')
                print(f"Job: {name} - Status: {status} - Result: {result}")
            elif record.get('type') == 'Task':
                status = record.get('state', 'Unknown')
                result = record.get('result', '')
                name = record.get('name', 'Unknown')
                print(f"  Task: {name} - Status: {status} - Result: {result}")

    print(f"\nTo view detailed logs in Azure DevOps:")
    print(f"https://dev.azure.com/{config['organization']}/{config['project']}/_build/results?buildId={build_id}")

    return 0


def cmd_azdo_pipeline_logs(build_id: int, verbose: bool = False) -> int:
    """Handle 'sdo pipeline logs' command for Azure DevOps."""
    # Get configuration (auto-extract from Git if needed)
    config = get_pipeline_config()
    if not config:
        return 1

    # Get personal access token from environment
    pat = get_personal_access_token()

    # Check required parameters
    required = ['organization', 'project']
    missing = [key for key in required if not config.get(key)]

    if not pat:
        missing.append('AZURE_DEVOPS_PAT environment variable')

    if missing:
        print(f"Missing required configuration: {', '.join(missing)}")
        print("Please set AZURE_DEVOPS_PAT environment variable.")
        return 1

    try:
        build_id = int(build_id)
    except ValueError:
        print(f"Invalid build ID: {build_id}. Must be a number.")
        return 1

    # Initialize Azure DevOps client
    client = AzureDevOpsClient(
        config['organization'],
        config['project'],
        pat
    )

    # Get build logs
    print(f"Getting logs for build ID: {build_id}...")
    logs = client.get_build_logs(build_id)

    if not logs:
        print(f"❌ No logs found for build {build_id}")
        print("The build may not have started yet or logs may not be available.")
        return 1

    # Display logs
    print(f"\n{'='*60}")
    print(f"Build Logs - ID: {build_id}")
    print(f"{'='*60}")

    if 'value' in logs:
        for log_entry in logs['value']:
            log_url = log_entry.get('url')
            if log_url:
                print(f"📄 Log: {log_entry.get('name', 'Unknown')}")
                print(f"   URL: {log_url}")
                print()

    print("💡 To view detailed logs in Azure DevOps:")
    print(f"https://dev.azure.com/{config['organization']}/{config['project']}/_build/results?buildId={build_id}")

    return 0


def cmd_azdo_pipeline_lastbuild(verbose: bool = False) -> int:
    """Handle 'sdo pipeline lastbuild' command for Azure DevOps."""
    # Get configuration (auto-extract from Git if needed)
    config = get_pipeline_config()
    if not config:
        return 1

    # Get personal access token from environment
    pat = get_personal_access_token()

    # Check required parameters
    required = ['organization', 'project']
    missing = [key for key in required if not config.get(key)]

    if not pat:
        missing.append('AZURE_DEVOPS_PAT environment variable')

    if missing:
        print(f"Missing required configuration: {', '.join(missing)}")
        print("Please set AZURE_DEVOPS_PAT environment variable.")
        return 1

    # Get pipeline name from config (always available from Git extraction)
    pipeline_name = config['pipelineName']

    # Initialize Azure DevOps client
    client = AzureDevOpsClient(
        config['organization'],
        config['project'],
        pat
    )

    print(f"Getting last build ID for pipeline: {pipeline_name}...")

    # List builds (most recent first)
    builds = client.list_builds(pipeline_name=pipeline_name, top=1)

    if not builds:
        print("❌ No builds found")
        return 1

    # Get the most recent build
    latest_build = builds[0]
    build_id = latest_build['id']
    status = latest_build.get('status', 'Unknown')
    result = latest_build.get('result', 'Unknown')

    print(f"✅ Last Build ID: {build_id}")
    print(f"   Status: {status}")
    print(f"   Result: {result}")
    print(f"   Build Number: {latest_build.get('buildNumber', 'N/A')}")
    print(f"   Queued: {latest_build.get('queueTime', 'N/A')}")
    print(f"   Started: {latest_build.get('startTime', 'N/A')}")
    print(f"   Finished: {latest_build.get('finishTime', 'N/A')}")

    # Save to config for easy access
    print(f"\n💡 Use 'sdo pipeline status {build_id}' to check progress.")

    return 0


def cmd_azdo_pipeline_update(verbose: bool = False) -> int:
    """Handle 'sdo pipeline update' command for Azure DevOps."""
    # Get configuration (auto-extract from Git if needed)
    config = get_pipeline_config()
    if not config:
        return 1

    # Get personal access token from environment
    pat = get_personal_access_token()

    # Check required parameters for update
    required = ['organization', 'project', 'pipelineName', 'repository', 'pipelineYamlPath']
    missing = [key for key in required if not config.get(key)]

    if not pat:
        missing.append(MISSING_PAT_MSG)

    if missing:
        print(f"Missing required configuration: {', '.join(missing)}")
        print(MISSING_CONFIG_MSG)
        return 1

    # Initialize Azure DevOps client
    client = AzureDevOpsClient(
        config['organization'],
        config['project'],
        pat
    )

    # Check if pipeline exists
    existing_pipeline = client.get_pipeline(config['pipelineName'])
    if not existing_pipeline:
        print(f"Pipeline '{config['pipelineName']}' does not exist!")
        print("Use 'sdo pipeline create' to create a new pipeline.")
        return 1

    # Update pipeline configuration
    print(f"Updating pipeline '{config['pipelineName']}'...")

    # Check if the YAML path has changed
    current_yaml_path = existing_pipeline.get('configuration', {}).get('path', '')
    new_yaml_path = config['pipelineYamlPath']

    if current_yaml_path == new_yaml_path:
        print("✓ Pipeline configuration is already up to date!")
        print(f"Name: {config['pipelineName']}")
        print(f"YAML Path: {current_yaml_path}")
        print(f"Repository: {config['repository']}")
        return 0

    # Azure DevOps doesn't allow updating the YAML path of existing pipelines via REST API
    # The YAML path is set during pipeline creation and cannot be changed
    print(f"⚠️  Pipeline '{config['pipelineName']}' needs to be recreated!")
    print("The YAML path has changed and Azure DevOps doesn't support updating it via API.")
    print()
    print("Follow these steps:")
    print("1. Go to the pipeline URL:")
    print(f"   https://dev.azure.com/{config['organization']}/{config['project']}/_build?definitionId={existing_pipeline['id']}")
    print("2. Click the '...' menu (3 dots) in the top right")
    print("3. Select 'Delete pipeline'")
    print("4. Confirm the deletion")
    print()
    print("After deleting the pipeline, use one of these commands to recreate it:")
    print("   sdo pipeline create")
    print()
    return 0


# GitHub Actions Workflow Functions

def _check_gh_cli() -> bool:
    """Check if GitHub CLI is available."""
    try:
        result = subprocess.run(
            ["gh", "--version"],
            capture_output=True,
            text=True,
            check=True
        )
        return True
    except (subprocess.CalledProcessError, FileNotFoundError):
        return False


def _run_gh_command(cmd: list, verbose: bool = False) -> tuple[int, str, str]:
    """Run a GitHub CLI command and return the result."""
    try:
        if verbose:
            print(f"Running: gh {' '.join(cmd)}")

        result = subprocess.run(
            ["gh"] + cmd,
            capture_output=True,
            text=True,
            check=False
        )

        if verbose:
            if result.stdout:
                print(f"Output: {result.stdout}")
            if result.stderr:
                print(f"Error: {result.stderr}")

        return result.returncode, result.stdout, result.stderr
    except Exception as e:
        return 1, "", str(e)


def _find_default_workflow_name(verbose: bool = False) -> str:
    """Find the most appropriate default workflow name."""
    returncode, stdout, stderr = _run_gh_command(
        ["workflow", "list", "--json", "name,state"],
        verbose
    )
    if returncode != 0:
        return None

    try:
        import json
        workflows = json.loads(stdout)
        if not workflows:
            return None

        # Priority order for default workflow selection
        priorities = [
            lambda w: w.get('name', '').lower() == 'ci',
            lambda w: w.get('name', '').lower() == 'build',
            lambda w: w.get('name', '').lower() == 'test',
            lambda w: 'ci' in w.get('name', '').lower() and 'pages' not in w.get('name', '').lower(),
            lambda w: 'build' in w.get('name', '').lower() and 'pages' not in w.get('name', '').lower(),
            lambda w: 'test' in w.get('name', '').lower(),
            lambda w: 'workflow' in w.get('name', '').lower() and 'pages' not in w.get('name', '').lower(),
            lambda w: w.get('state') == 'active' and 'pages' not in w.get('name', '').lower(),
            lambda w: 'ci' in w.get('name', '').lower(),
            lambda w: 'build' in w.get('name', '').lower(),
            lambda w: w.get('state') == 'active',  # Any active workflow
        ]

        for priority_func in priorities:
            for workflow in workflows:
                if priority_func(workflow):
                    return workflow['name']

        # If no priority match, return the first workflow
        return workflows[0]['name']

    except (json.JSONDecodeError, KeyError, IndexError):
        return None


def cmd_github_workflow_list(verbose: bool = False) -> int:
    """List GitHub Actions workflows."""
    if not _check_gh_cli():
        print(f"❌ {MISSING_GH_MSG}")
        return 1

    print("Fetching GitHub Actions workflows...")
    returncode, stdout, stderr = _run_gh_command(["workflow", "list"], verbose)

    if returncode != 0:
        print(f"❌ Failed to list workflows: {stderr}")
        return 1

    if not stdout.strip():
        print("No workflows found in this repository.")
        return 0

    print("GitHub Actions Workflows:")
    print("-" * 80)
    # Parse the output (gh workflow list returns tab-separated values)
    lines = stdout.strip().split('\n')
    for line in lines:
        if line.strip():
            parts = line.split('\t')
            if len(parts) >= 3:
                name, state, id = parts[0], parts[1], parts[2]
                print(f"  {name}")
                print(f"    State: {state}")
                print(f"    ID: {id}")
                print()

    return 0


def cmd_github_workflow_run(workflow_name: str = None, branch: str = None, verbose: bool = False) -> int:
    """Run a GitHub Actions workflow."""
    if not _check_gh_cli():
        print(f"❌ {MISSING_GH_MSG}")
        return 1

    # If no workflow name provided, try to find a default
    if not workflow_name:
        workflow_name = _find_default_workflow_name(verbose)
        if not workflow_name:
            print("❌ No workflow name provided and could not find a default workflow.")
            print("Please specify a workflow name or ensure you have a workflow file.")
            return 1

    cmd = ["workflow", "run", workflow_name]
    if branch:
        cmd.extend(["--ref", branch])

    print(f"Running GitHub Actions workflow '{workflow_name}'...")
    returncode, stdout, stderr = _run_gh_command(cmd, verbose)

    if returncode != 0:
        print(f"❌ Failed to run workflow: {stderr}")
        return 1

    print("✓ Workflow run initiated successfully!")
    if stdout:
        print(f"Output: {stdout}")

    return 0


def cmd_github_workflow_view(workflow_name: str = None, verbose: bool = False) -> int:
    """View GitHub Actions workflow details."""
    if not _check_gh_cli():
        print(f"❌ {MISSING_GH_MSG}")
        return 1

    if not workflow_name:
        # Try to find the most appropriate workflow
        workflow_name = _find_default_workflow_name(verbose)
        if not workflow_name:
            print("❌ No workflow name provided and could not find a default workflow.")
            print("Please specify a workflow name or ensure you have workflow files.")
            return 1

    print(f"Viewing workflow '{workflow_name}'...")
    returncode, stdout, stderr = _run_gh_command(["workflow", "view", workflow_name], verbose)

    if returncode != 0:
        print(f"❌ Failed to view workflow: {stderr}")
        return 1

    print(stdout)
    return 0


def cmd_github_run_list(verbose: bool = False) -> int:
    """List recent GitHub Actions runs."""
    if not _check_gh_cli():
        print(f"❌ {MISSING_GH_MSG}")
        return 1

    print("Fetching recent GitHub Actions runs...")
    returncode, stdout, stderr = _run_gh_command(["run", "list"], verbose)

    if returncode != 0:
        print(f"❌ Failed to list runs: {stderr}")
        return 1

    if not stdout.strip():
        print("No workflow runs found.")
        return 0

    print("Recent GitHub Actions Runs:")
    print("-" * 80)
    print(stdout)
    return 0


def cmd_github_run_view(run_id: str, verbose: bool = False) -> int:
    """View details of a GitHub Actions run."""
    if not _check_gh_cli():
        print(f"❌ {MISSING_GH_MSG}")
        return 1

    if not run_id:
        print("❌ Run ID is required")
        return 1

    print(f"Viewing run details for ID: {run_id}...")
    returncode, stdout, stderr = _run_gh_command(["run", "view", run_id], verbose)

    if returncode != 0:
        print(f"❌ Failed to view run: {stderr}")
        return 1

    print(stdout)
    return 0


def cmd_github_run_logs(run_id: str, verbose: bool = False) -> int:
    """View logs for a GitHub Actions run."""
    if not _check_gh_cli():
        print(f"❌ {MISSING_GH_MSG}")
        return 1

    if not run_id:
        print("❌ Run ID is required")
        return 1

    print(f"Fetching logs for run ID: {run_id}...")
    returncode, stdout, stderr = _run_gh_command(["run", "view", run_id, "--log"], verbose)

    if returncode != 0:
        print(f"❌ Failed to get logs: {stderr}")
        return 1

    print(stdout)
    return 0


# Unified command handlers that detect platform and call appropriate functions

def cmd_pipeline_create(verbose: bool = False) -> int:
    """Handle 'sdo pipeline create' command."""
    config = get_pipeline_config()
    if config is None:
        return 1

    platform = config.get("platform")

    if platform == "azdo":
        return cmd_azdo_pipeline_create(verbose)
    elif platform == "github":
        print("GitHub Actions workflows are typically created by adding YAML files to .github/workflows/")
        print("SDO does not create workflow files automatically.")
        print("\nTo create a workflow:")
        print("1. Create the directory: mkdir .github/workflows")
        print("2. Add a workflow YAML file (e.g., ci.yml)")
        print("3. Commit and push the changes")
        return 0
    else:
        print(f"❌ Unsupported platform: {platform}")
        return 1


def cmd_pipeline_show(verbose: bool = False) -> int:
    """Handle 'sdo pipeline show' command."""
    config = get_pipeline_config()
    if config is None:
        return 1

    platform = config.get("platform")

    if platform == "azdo":
        return cmd_azdo_pipeline_show(verbose)
    elif platform == "github":
        workflow_name = config.get("workflowName", "ci")
        return cmd_github_workflow_view(workflow_name, verbose)
    else:
        print(f"❌ Unsupported platform: {platform}")
        return 1


def cmd_pipeline_list(verbose: bool = False) -> int:
    """Handle 'sdo pipeline list' command."""
    config = get_pipeline_config()
    if config is None:
        return 1

    platform = config.get("platform")

    if platform == "azdo":
        return cmd_azdo_pipeline_list(verbose)
    elif platform == "github":
        return cmd_github_workflow_list(verbose)
    else:
        print(f"❌ Unsupported platform: {platform}")
        return 1


def cmd_pipeline_delete(verbose: bool = False) -> int:
    """Handle 'sdo pipeline delete' command."""
    config = get_pipeline_config()
    if config is None:
        return 1

    platform = config.get("platform")

    if platform == "azdo":
        return cmd_azdo_pipeline_delete(verbose)
    elif platform == "github":
        print("GitHub Actions workflows are deleted by removing the YAML files from .github/workflows/")
        print("SDO does not delete workflow files automatically.")
        return 0
    else:
        print(f"❌ Unsupported platform: {platform}")
        return 1


def cmd_pipeline_run(verbose: bool = False) -> int:
    """Handle 'sdo pipeline run' command."""
    config = get_pipeline_config()
    if config is None:
        return 1

    platform = config.get("platform")

    if platform == "azdo":
        return cmd_azdo_pipeline_run(verbose)
    elif platform == "github":
        workflow_name = config.get("workflowName")
        branch = config.get("branch", "main")
        return cmd_github_workflow_run(workflow_name, branch, verbose)
    else:
        print(f"❌ Unsupported platform: {platform}")
        return 1


def cmd_pipeline_status(build_id: int, verbose: bool = False) -> int:
    """Handle 'sdo pipeline status' command."""
    config = get_pipeline_config()
    if config is None:
        return 1

    platform = config.get("platform")

    if platform == "azdo":
        return cmd_azdo_pipeline_status(build_id, verbose)
    elif platform == "github":
        return cmd_github_run_view(str(build_id), verbose)
    else:
        print(f"❌ Unsupported platform: {platform}")
        return 1


def cmd_pipeline_logs(build_id: int, verbose: bool = False) -> int:
    """Handle 'sdo pipeline logs' command."""
    config = get_pipeline_config()
    if config is None:
        return 1

    platform = config.get("platform")

    if platform == "azdo":
        return cmd_azdo_pipeline_logs(build_id, verbose)
    elif platform == "github":
        return cmd_github_run_logs(str(build_id), verbose)
    else:
        print(f"❌ Unsupported platform: {platform}")
        return 1


def cmd_pipeline_lastbuild(verbose: bool = False) -> int:
    """Handle 'sdo pipeline lastbuild' command."""
    config = get_pipeline_config()
    if config is None:
        return 1

    platform = config.get("platform")

    if platform == "azdo":
        return cmd_azdo_pipeline_lastbuild(verbose)
    elif platform == "github":
        return cmd_github_run_list(verbose)
    else:
        print(f"❌ Unsupported platform: {platform}")
        return 1


def cmd_pipeline_update(verbose: bool = False) -> int:
    """Handle 'sdo pipeline update' command."""
    config = get_pipeline_config()
    if config is None:
        return 1

    platform = config.get("platform")

    if platform == "azdo":
        return cmd_azdo_pipeline_update(verbose)
    elif platform == "github":
        print("GitHub Actions workflows are updated by modifying the YAML files in .github/workflows/")
        print("SDO does not update workflow files automatically.")
        print("After modifying workflow files, commit and push the changes to trigger updates.")
        return 0
    else:
        print(f"❌ Unsupported platform: {platform}")
        return 1