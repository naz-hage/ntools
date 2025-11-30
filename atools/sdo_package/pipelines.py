"""
Pipeline Operations Module for SDO
Contains command handlers for Azure DevOps pipeline and GitHub Actions workflow operations.
"""

import os
import sys
import subprocess
import requests
import re
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

# Import colorama for cross-platform colored output
try:
    from colorama import init, Fore, Style
    # Initialize colorama for cross-platform support
    init(autoreset=True, strip=False)
    COLORS_SUPPORTED = True
except ImportError:
    # Fallback if colorama is not available
    COLORS_SUPPORTED = False
    class Fore:
        GREEN = ""
        RED = ""
        YELLOW = ""
        CYAN = ""
        WHITE = ""
    class Style:
        RESET_ALL = ""

# Function to apply color only if supported
def colorize(text, color_code):
    """Apply color to text if colors are supported, otherwise return plain text."""
    if COLORS_SUPPORTED:
        return f"{color_code}{text}{Style.RESET_ALL}"
    return text

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
        print("[OK] Detected Azure DevOps repository:")
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
        print("[OK] Detected GitHub repository:")
        print(f"  Owner: {platform_info['owner']}")
        print(f"  Repository: {platform_info['repo']}")

        # Set extracted values
        config = platform_info.copy()

        # Find the default workflow name and path dynamically
        result = _find_default_workflow_name()
        if result is not None:
            workflow_name, workflow_path = result
            config['workflowName'] = workflow_name
            config['workflowYamlPath'] = workflow_path
        else:
            config['workflowName'] = 'ci'  # Fallback
            config['workflowYamlPath'] = '.github/workflows/ci.yml'  # Fallback

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

    # Check if YAML file exists locally before creating pipeline
    yaml_path = config['pipelineYamlPath']
    if not os.path.isfile(yaml_path):
        print(f"❌ Pipeline YAML file not found: {yaml_path}")
        print("Please create the pipeline YAML file before creating the pipeline definition.")
        print()
        print("Example YAML file structure:")
        print(f"  {yaml_path}")
        print("  trigger:")
        print("  - main")
        print("  ")
        print("  pool:")
        print("    vmImage: 'ubuntu-latest'")
        print("  ")
        print("  steps:")
        print("  - script: echo Hello, world!")
        print("    displayName: 'Run a one-line script'")
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
        print(f"URL: https://dev.azure.com/{config['organization']}/{config['project']}/_build?definitionId={existing_pipeline['id']}")
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
        print(f"URL: https://dev.azure.com/{config['organization']}/{config['project']}/_build?definitionId={pipeline['id']}")
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
        print(f"  URL: https://dev.azure.com/{config['organization']}/{config['project']}/_build?definitionId={pipeline['id']}")
        print(f"  Folder: {pipeline.get('folder', 'Unknown')}")
        print(f"  Revision: {pipeline.get('revision', 'Unknown')}")
        return 0
    else:
        print(f"Pipeline '{pipeline_name}' not found.")
        return 1


def cmd_azdo_pipeline_list(config: dict = None, repo_filter: str = None, show_all: bool = False, verbose: bool = False) -> int:
    """Handle 'sdo pipeline list' command for Azure DevOps."""
    if config is None:
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

        # Filter pipelines by repository (use current repo if no filter specified and not show_all)
        current_repo = config.get('repository')
        if show_all:
            target_repo = None  # Don't filter
        else:
            target_repo = repo_filter if repo_filter is not None else current_repo
        
        if target_repo:
            filtered_pipelines = []
            for pipeline in pipelines:
                pipeline_name = pipeline['name']
                
                # Filter by pipeline name prefix (fast, no API calls needed)
                # This assumes pipelines follow naming convention: {repo}-{pipeline-type}
                if pipeline_name.startswith(f"{target_repo}-") or pipeline_name == target_repo:
                    filtered_pipelines.append(pipeline)
            pipelines = filtered_pipelines

        if not pipelines:
            if target_repo:
                print(f"No pipelines found for repository '{target_repo}' in project '{config['project']}'.")
            else:
                print("No pipelines found in this project.")
            return 0

        if target_repo:
            if repo_filter:
                print(f"Pipelines for repository '{target_repo}' in project '{config['project']}' ({len(pipelines)} total):")
            else:
                print(f"Pipelines for current repository '{target_repo}' in project '{config['project']}' ({len(pipelines)} total):")
        else:
            print(f"Pipelines in project '{config['project']}' ({len(pipelines)} total):")
        
        if pipelines:
            print()
            # Display in a more compact format
            for i, pipeline in enumerate(pipelines, 1):
                name = pipeline['name']
                pipeline_id = pipeline['id']
                folder = pipeline.get('folder', '\\')
                full_url = f"https://dev.azure.com/{config['organization']}/{config['project']}/_build?definitionId={pipeline_id}"
                
                # Color the pipeline name based on type
                if '-prebuild' in name or '-precheck' in name:
                    name_colored = colorize(name, Fore.YELLOW)
                elif '-authserver' in name or '-api' in name or '-httpapi' in name:
                    name_colored = colorize(name, Fore.GREEN)
                elif '-web' in name:
                    name_colored = colorize(name, Fore.BLUE)
                elif '-dbmigrator' in name or '-migrator' in name:
                    name_colored = colorize(name, Fore.MAGENTA)
                elif '-data-warehouse' in name or '-image-promotion' in name:
                    name_colored = colorize(name, Fore.CYAN)
                else:
                    name_colored = colorize(name, Fore.WHITE)
                
                # Compact single-line format
                print(f"{i:2d}. {name_colored} (ID: {pipeline_id}, Folder: {folder})")
                print(f"    {full_url}")
                print()
            
            print(colorize("💡 Tip: Use 'sdo pipeline run <name>' to trigger a pipeline", Fore.CYAN))
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


def cmd_azdo_pipeline_run(config: dict = None, pipeline_name: str = None, branch: str = None, verbose: bool = False) -> int:
    """Handle 'sdo pipeline run' command for Azure DevOps."""
    if config is None:
        config = get_pipeline_config()
        if not config:
            return 1

    # If pipeline_name is provided, override the default pipeline name
    if pipeline_name:
        config['pipelineName'] = pipeline_name

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


def check_build_logs_for_errors(client: AzureDevOpsClient, build_id: int, verbose: bool = False) -> list[str]:
    """Check build logs for common error patterns and return a list of error messages."""
    errors = []

    try:
        logs = client.get_build_logs(build_id)
        if not logs or 'value' not in logs:
            return errors

        # Common error patterns to look for
        error_patterns = [
            (r"No hosted parallelism has been purchased or granted", "Parallelism limit exceeded - no hosted agents available"),
            (r"##\[error\]", "Build task error detected"),
            (r"Authentication failed", "Authentication error"),
            (r"Access denied", "Access denied error"),
            (r"not found", "Resource not found error"),
            (r"(?<!TimeoutInMinutes: )\btimeout\b", "Timeout error"),  # Negative lookbehind to avoid matching "TimeoutInMinutes: 60"
            (r"out of memory", "Out of memory error"),
            (r"disk space", "Disk space error"),
            (r"exceeded.*limit", "Resource limit exceeded"),
            (r"queue.*full", "Build queue full"),
            (r"no agents", "No available agents"),
            (r"parallelism.*grant", "Parallelism grant required"),
            (r"free parallelism grant", "Free parallelism grant needed"),
        ]

        for log_entry in logs['value']:
            log_url = log_entry.get('url')
            if log_url:
                try:
                    log_response = requests.get(log_url, headers=client.headers, timeout=10)
                    if log_response.status_code == 200:
                        log_content = log_response.text

                        if verbose:
                            print(f"DEBUG: Checking log from {log_url}")
                            print(f"DEBUG: Log content length: {len(log_content)} characters")
                            # Show first 500 characters for debugging
                            print(f"DEBUG: Log preview: {log_content[:500]}...")

                        # Check for each error pattern
                        for pattern, description in error_patterns:
                            if re.search(pattern, log_content, re.IGNORECASE):
                                if verbose:
                                    print(f"DEBUG: Matched pattern '{pattern}' -> {description}")
                                if description not in errors:  # Avoid duplicates
                                    errors.append(description)
                                break  # Only add one error per log entry

                except (requests.RequestException, Exception) as e:
                    if verbose:
                        print(f"DEBUG: Failed to download log from {log_url}: {e}")
                    # Skip logs that can't be downloaded
                    continue

    except Exception as e:
        if verbose:
            print(f"DEBUG: Exception in check_build_logs_for_errors: {e}")
        # If log checking fails, return empty list
        pass

    return errors


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

    if verbose:
        print(f"DEBUG: Full build response keys: {list(build.keys())}")
        print(f"DEBUG: Build status: {build.get('status')}")
        print(f"DEBUG: Build result: {build.get('result')}")
        print(f"DEBUG: Build queue status: {build.get('queue', {}).get('status') if build.get('queue') else 'N/A'}")
        print(f"DEBUG: Build properties: {build.get('properties', {})}")
        validation_results = build.get('validationResults', [])
        if validation_results:
            print(f"DEBUG: Validation results: {validation_results}")
        else:
            print("DEBUG: No validation results found")

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
    print(f"URL: https://dev.azure.com/{config['organization']}/{config['project']}/_build/results?buildId={build_id}")

    # Get timeline information for failure analysis and job details
    timeline = client.get_build_timeline(build_id)

    # Check if build failed and provide detailed diagnostics
    if build.get('result') == 'failed':
        print("\n🔍 FAILURE ANALYSIS:")
        print("-" * 40)

        # Check for parallelism issues specifically
        parallelism_error = False

        # Check validation results for parallelism errors
        validation_results = build.get('validationResults', [])
        parallelism_validation_errors = []
        for validation in validation_results:
            message = validation.get('message', '').lower()
            if 'parallelism' in message or 'hosted' in message and 'purchased' in message:
                parallelism_validation_errors.append(validation)
                parallelism_error = True

        if parallelism_validation_errors:
            print(f"🚫 PARALLELISM ERROR DETECTED:")
            for validation in parallelism_validation_errors:
                result = validation.get('result', 'Unknown')
                message = validation.get('message', 'No message')
                print(f"  • {result}: {message}")
            print(f"  💡 SOLUTION: Request a free parallelism grant at https://aka.ms/azpipelines-parallelism-request")
            print()

        # Check for validation results (includes parallelism errors)
        if validation_results and not parallelism_error:
            print(f"Found {len(validation_results)} validation error(s):")
            for validation in validation_results:
                result = validation.get('result', 'Unknown')
                message = validation.get('message', 'No message')
                print(f"  • {result}: {message}")
        elif not parallelism_error:
            print("No validation errors found.")

        # Try to get more detailed error information
        issues = client.get_build_issues(build_id)
        if issues and 'value' in issues:
            print(f"Found {len(issues['value'])} issue(s):")
            for issue in issues['value'][:5]:  # Show first 5 issues
                issue_type = issue.get('type', 'Unknown')
                category = issue.get('category', 'Unknown')
                message = issue.get('message', 'No message')
                print(f"  • {issue_type} ({category}): {message}")
        else:
            print("No detailed issues found in API response.")

        # Check build logs for common error patterns
        print("\n🔎 Checking build logs for common errors...")
        log_errors = check_build_logs_for_errors(client, build_id, verbose)
        if log_errors:
            print("Found error(s) in build logs:")
            for error in log_errors[:3]:  # Show first 3 errors
                print(f"  • {error}")
        else:
            print("No common errors detected in logs.")

        # Show timeline errors
        if timeline and 'records' in timeline:
            failed_tasks = [record for record in timeline['records']
                          if record.get('type') == 'Task' and record.get('result') == 'failed']
            if failed_tasks:
                print(f"\nFailed task(s): {len(failed_tasks)}")
                for task in failed_tasks[:3]:  # Show first 3 failed tasks
                    name = task.get('name', 'Unknown')
                    print(f"  • Task: {name}")

        print(f"\n💡 Quick troubleshooting:")
        print(f"   • Check the build logs: sdo pipeline logs {build_id}")
        print(f"   • View in browser: https://dev.azure.com/{config['organization']}/{config['project']}/_build/results?buildId={build_id}")
        print(f"   • Common issues: YAML syntax, missing dependencies, test failures")    # Get and display timeline information
    print(f"\nJob/Step Details:")
    print("-"*40)
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

    # Add failure summary at the end
    if build.get('result') == 'failed':
        print(f"\n" + "="*60)
        print(f"📋 FAILURE SUMMARY - Build {build_id}")
        print(f"="*60)

        total_failures = 0
        failure_sources = []

        # Count validation errors
        validation_results = build.get('validationResults', [])
        if validation_results:
            total_failures += len(validation_results)
            failure_sources.append(f"Validation: {len(validation_results)} error(s)")

        # Count API issues
        issues = client.get_build_issues(build_id)
        if issues and 'value' in issues:
            total_failures += len(issues['value'])
            failure_sources.append(f"API Issues: {len(issues['value'])} issue(s)")

        # Count log errors
        log_errors = check_build_logs_for_errors(client, build_id)
        if log_errors:
            total_failures += len(log_errors)
            failure_sources.append(f"Log Errors: {len(log_errors)} error(s)")

        # Count timeline failures
        timeline = client.get_build_timeline(build_id)
        timeline_failures = 0
        if timeline and 'records' in timeline:
            failed_tasks = [record for record in timeline['records']
                          if record.get('type') == 'Task' and record.get('result') == 'failed']
            timeline_failures = len(failed_tasks)
            if timeline_failures > 0:
                total_failures += timeline_failures
                failure_sources.append(f"Timeline: {timeline_failures} failed task(s)")

        if total_failures > 0:
            print(f"❌ Total Failures Found: {total_failures}")
            print(f"Failure Sources:")
            for source in failure_sources:
                print(f"  • {source}")
        else:
            print(f"❌ Build Failed: No specific errors detected")
            print(f"   This may indicate a system-level failure or parallelism issue")

        print(f"\n🔗 Click here for full details:")
        print(f"https://dev.azure.com/{config['organization']}/{config['project']}/_build/results?buildId={build_id}")
        print(f"="*60)

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
            log_name = log_entry.get('name', 'Unknown')
            if log_url:
                print(f"📄 Log: {log_name}")
                print(f"{'-'*40}")

                # Download and display the full log content
                try:
                    log_response = requests.get(log_url, headers=client.headers, timeout=30)
                    if log_response.status_code == 200:
                        log_content = log_response.text

                        # Display the log content
                        if log_content.strip():
                            # For very large logs, show a reasonable preview with truncation warning
                            lines = log_content.split('\n')
                            max_lines = 500  # Show up to 500 lines per log

                            if len(lines) > max_lines:
                                # Show first 200 lines, then a truncation message, then last 100 lines
                                preview_lines = lines[:200] + [f"\n... ({len(lines) - 300} lines truncated) ...\n"] + lines[-100:]
                                display_content = '\n'.join(preview_lines)
                                print(display_content)
                                print(f"\n⚠️  Log truncated - showing first 200 and last 100 of {len(lines)} total lines")
                                print(f"   Full log available at: https://dev.azure.com/{config['organization']}/{config['project']}/_build/results?buildId={build_id}&view=logs&j={log_entry.get('id', '')}")
                            else:
                                # Show full log content
                                print(log_content)
                        else:
                            print("(Log is empty)")
                    else:
                        print(f"❌ Failed to download log (HTTP {log_response.status_code})")
                        print(f"   URL: https://dev.azure.com/{config['organization']}/{config['project']}/_build/results?buildId={build_id}&view=logs&j={log_entry.get('id', '')}")

                except requests.exceptions.Timeout:
                    print("❌ Log download timed out (30 second limit)")
                    print(f"   URL: https://dev.azure.com/{config['organization']}/{config['project']}/_build/results?buildId={build_id}&view=logs&j={log_entry.get('id', '')}")
                except requests.exceptions.RequestException as e:
                    print(f"❌ Failed to download log: {e}")
                    print(f"   URL: https://dev.azure.com/{config['organization']}/{config['project']}/_build/results?buildId={build_id}&view=logs&j={log_entry.get('id', '')}")
                except Exception as e:
                    print(f"❌ Unexpected error displaying log: {e}")
                    print(f"   URL: https://dev.azure.com/{config['organization']}/{config['project']}/_build/results?buildId={build_id}&view=logs&j={log_entry.get('id', '')}")

                print()  # Add blank line between logs
    else:
        print("No logs found for this build.")

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
    print(f"   URL: https://dev.azure.com/{config['organization']}/{config['project']}/_build/results?buildId={build_id}")

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
            encoding='utf-8',
            errors='replace',
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


def _find_default_workflow_name(verbose: bool = False) -> tuple[str, str] | None:
    """Find the most appropriate default workflow name and return (name, path)."""
    returncode, stdout, stderr = _run_gh_command(
        ["workflow", "list", "--json", "name,path"],
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
            lambda w: w.get('name', '').lower() != 'pages-build-deployment',  # Prefer non-pages workflows
            lambda w: True,  # Any workflow
        ]

        for priority_func in priorities:
            for workflow in workflows:
                if priority_func(workflow):
                    return workflow['name'], workflow['path']

        # If no priority match, return the first workflow
        if workflows:
            workflow = workflows[0]
            return workflow['name'], workflow['path']

    except (json.JSONDecodeError, KeyError, IndexError):
        return None

    return None


def cmd_github_workflow_list(verbose: bool = False) -> int:
    """List GitHub Actions workflows."""
    if not _check_gh_cli():
        print(f"❌ {MISSING_GH_MSG}")
        return 1

    # Get configuration to access owner/repo
    config = get_pipeline_config()
    if config is None:
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
                print(f"    URL: https://github.com/{config['owner']}/{config['repo']}/actions/workflows/{name.replace(' ', '%20')}")
                print()

    return 0


def cmd_github_workflow_run(workflow_name: str = None, branch: str = None, verbose: bool = False) -> int:
    """Run a GitHub Actions workflow."""
    if not _check_gh_cli():
        print(f"❌ {MISSING_GH_MSG}")
        return 1

    # If no workflow name provided, try to find a default
    if not workflow_name:
        result = _find_default_workflow_name(verbose)
        if result is None:
            print("❌ No workflow name provided and could not find a default workflow.")
            print("Please specify a workflow name or ensure you have a workflow file.")
            return 1
        workflow_name, _ = result

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
    
    # Get configuration to show URL
    config = get_pipeline_config()
    if config:
        print(f"💡 View workflow runs at: https://github.com/{config['owner']}/{config['repo']}/actions")
        print(f"💡 Use 'sdo pipeline status' to check progress.")

    return 0


def cmd_github_workflow_view(workflow_name: str = None, verbose: bool = False) -> int:
    """View GitHub Actions workflow details."""
    if not _check_gh_cli():
        print(f"❌ {MISSING_GH_MSG}")
        return 1

    if not workflow_name:
        # Try to find the most appropriate workflow
        result = _find_default_workflow_name(verbose)
        if result is None:
            print("❌ No workflow name provided and could not find a default workflow.")
            print("Please specify a workflow name or ensure you have workflow files.")
            return 1
        workflow_name, _ = result

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

    # Get configuration to access owner/repo
    config = get_pipeline_config()
    if config is None:
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
    # Replace cryptic symbols with clear text for better readability
    readable_output = stdout.replace("âœ“", "PASSED").replace("✓", "PASSED").replace("❌", "FAILED").replace("✗", "FAILED")
    print(readable_output)
    print(f"\n💡 View all runs at: https://github.com/{config['owner']}/{config['repo']}/actions")
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

    # Get detailed job information (suppress raw output in verbose mode)
    returncode, job_stdout, stderr = _run_gh_command(["run", "view", run_id, "--json", "jobs"], False)

    if returncode != 0:
        # Fallback to regular view if JSON fails
        returncode, stdout, stderr = _run_gh_command(["run", "view", run_id], verbose)
        if returncode != 0:
            print(f"❌ Failed to view run: {stderr}")
            return 1

        # Replace cryptic symbols with clear text for better readability
        readable_output = stdout.replace("âœ“", "PASSED").replace("✓", "PASSED").replace("❌", "FAILED").replace("✗", "FAILED")
        print(readable_output)
        return 0

    # Parse job information for clearer display
    try:
        import json
        job_data = json.loads(job_stdout)

        # Get basic run info (suppress raw output in verbose mode)
        returncode, run_stdout, stderr = _run_gh_command(["run", "view", run_id, "--json", "status,conclusion,name,headSha,headBranch,url"], False)
        if returncode == 0:
            try:
                run_data = json.loads(run_stdout)
                status = run_data.get('status', 'unknown')
                conclusion = run_data.get('conclusion', '')
                name = run_data.get('name', 'Unknown')
                branch = run_data.get('headBranch', 'unknown')
                run_url = run_data.get('url', f'https://github.com/naz-hage/ntools/actions/runs/{run_id}')

                print(f"\n* {branch} {name} naz-hage/ntools")
                print(f"Status: {status}" + (f" - {conclusion}" if conclusion else ""))
                print()
            except json.JSONDecodeError:
                pass

        # Display jobs with clear status
        if 'jobs' in job_data and job_data['jobs']:
            print("JOBS")
            print("-" * 6)  # Add separator line below JOBS
            for job in job_data['jobs']:
                job_name = job.get('name', 'Unknown')
                job_status = job.get('status', 'unknown')
                job_conclusion = job.get('conclusion', '')
                duration = job.get('completedAt', '')

                if duration and job.get('startedAt'):
                    # Calculate duration if available
                    try:
                        from datetime import datetime
                        start = datetime.fromisoformat(job['startedAt'].replace('Z', '+00:00'))
                        end = datetime.fromisoformat(duration.replace('Z', '+00:00'))
                        duration_seconds = int((end - start).total_seconds())
                        if duration_seconds < 60:
                            duration_str = f"{duration_seconds}s"
                        elif duration_seconds < 3600:
                            minutes = duration_seconds // 60
                            seconds = duration_seconds % 60
                            duration_str = f"{minutes}m{seconds}s"
                        else:
                            hours = duration_seconds // 3600
                            minutes = (duration_seconds % 3600) // 60
                            duration_str = f"{hours}h{minutes}m"
                    except Exception as e:
                        if verbose:
                            print(f"DEBUG: Duration calculation failed: {e}")
                        duration_str = "completed"
                else:
                    duration_str = "completed"

                # Determine clear status with colors (if supported)
                if job_conclusion == 'success':
                    status_text = colorize("PASSED", Fore.GREEN)
                elif job_conclusion == 'failure':
                    status_text = colorize("FAILED", Fore.RED)
                elif job_status == 'in_progress':
                    status_text = colorize("RUNNING", Fore.YELLOW)
                elif job_status == 'queued':
                    status_text = colorize("QUEUED", Fore.CYAN)
                else:
                    status_text = job_status.upper() if job_status else "UNKNOWN"

                print(f"{status_text} {job_name} in {duration_str} (ID {job.get('databaseId', 'N/A')})")

                # Show job steps in verbose mode
                if verbose and 'steps' in job:
                    for step in job['steps']:
                        step_name = step.get('name', 'Unknown')
                        step_status = step.get('conclusion', step.get('status', 'unknown'))

                        # Determine step status symbol with colors (if supported)
                        if step_status == 'success':
                            step_symbol = colorize("✓", Fore.GREEN)
                        elif step_status == 'failure':
                            step_symbol = colorize("✗", Fore.RED)
                        elif step_status == 'skipped':
                            step_symbol = colorize("○", Fore.YELLOW)
                        elif step_status == 'in_progress':
                            step_symbol = colorize("●", Fore.CYAN)
                        else:
                            step_symbol = colorize("?", Fore.WHITE)

                        print(f"  {step_symbol} {step_name}")

                # No blank line after each job for more compact output
            artifacts = []
            for job in job_data['jobs']:
                if 'steps' in job:
                    for step in job['steps']:
                        if step.get('name', '').lower().startswith('upload') or 'artifact' in step.get('name', '').lower():
                            # This is a rough heuristic - could be improved
                            pass

            # Try to get artifacts - this might not be available in all GitHub CLI versions
            try:
                returncode, artifacts_stdout, stderr = _run_gh_command(["run", "view", run_id, "--json", "artifacts"], False)
                if returncode == 0:
                    try:
                        artifacts_data = json.loads(artifacts_stdout)
                        if artifacts_data.get('artifacts'):
                            print("\nARTIFACTS")
                            for artifact in artifacts_data['artifacts'][:5]:  # Show first 5
                                print(f"{artifact.get('name', 'Unknown')}")
                            if len(artifacts_data['artifacts']) > 5:
                                print(f"... and {len(artifacts_data['artifacts']) - 5} more")
                            print()
                    except json.JSONDecodeError:
                        pass
            except:
                # Artifacts not available in this GitHub CLI version
                pass

            print(f"View this run on GitHub: {run_url}")
            print(f"For more information about a job, try: gh run view --job=<job-id> (see job IDs above)")
            # URL already shown above
        else:
            # Fallback to regular view
            returncode, stdout, stderr = _run_gh_command(["run", "view", run_id], verbose)
            if returncode == 0:
                readable_output = stdout.replace("âœ“", "PASSED").replace("✓", "PASSED").replace("❌", "FAILED").replace("✗", "FAILED")
                print(readable_output)

    except (json.JSONDecodeError, KeyError, Exception) as e:
        if verbose:
            print(f"DEBUG: Failed to parse job data: {e}")
        # Fallback to regular view
        returncode, stdout, stderr = _run_gh_command(["run", "view", run_id], verbose)
        if returncode == 0:
            readable_output = stdout.replace("âœ“", "PASSED").replace("✓", "PASSED").replace("❌", "FAILED").replace("✗", "FAILED")
            print(readable_output)

    return 0


def cmd_github_run_logs(run_id: str, verbose: bool = False) -> int:
    """View logs for a GitHub Actions run."""
    if not _check_gh_cli():
        print(f"❌ {MISSING_GH_MSG}")
        return 1

    if not run_id:
        print("❌ Run ID is required")
        return 1

    # Get configuration to show URL
    config = get_pipeline_config()
    if config is None:
        return 1

    print(f"Fetching logs for run ID: {run_id}...")
    returncode, stdout, stderr = _run_gh_command(["run", "view", run_id, "--log"], verbose)

    if returncode != 0:
        print(f"❌ Failed to get logs: {stderr}")
        return 1

    print(stdout)
    print(f"\n💡 View this run on GitHub: https://github.com/{config['owner']}/{config['repo']}/actions/runs/{run_id}")
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
        # Find the repository root directory
        try:
            import subprocess
            result = subprocess.run(
                ["git", "rev-parse", "--show-toplevel"],
                capture_output=True,
                text=True,
                check=True
            )
            repo_root = result.stdout.strip()
        except (subprocess.CalledProcessError, FileNotFoundError):
            print("❌ Could not determine Git repository root directory.")
            print("Please ensure you're in a Git repository.")
            return 1

        # Check if .github/workflows directory exists and has files relative to repo root
        workflows_dir = os.path.join(repo_root, ".github", "workflows")
        has_workflows = False
        existing_workflows = []

        if os.path.isdir(workflows_dir):
            try:
                workflow_files = [f for f in os.listdir(workflows_dir) if f.endswith(('.yml', '.yaml'))]
                if workflow_files:
                    has_workflows = True
                    existing_workflows = workflow_files
            except OSError:
                pass  # Directory exists but can't read it

        if has_workflows:
            print("GitHub Actions workflows found in your repository:")
            for workflow in existing_workflows[:5]:  # Show first 5
                print(f"  • {workflow}")
            if len(existing_workflows) > 5:
                print(f"  ... and {len(existing_workflows) - 5} more")
            print()
            print("Your repository already has workflow files.")
            print("To create additional workflows, add more YAML files to .github/workflows/")
            print("and commit/push them to trigger GitHub Actions.")
            return 0
        else:
            print("GitHub Actions workflows are created by adding YAML files to .github/workflows/")
            print("SDO does not create workflow files automatically.")
            print()
            print("To create a workflow:")
            print("1. Create the directory: mkdir -p .github/workflows")
            print("2. Add a workflow YAML file (e.g., ci.yml)")
            print("3. Commit and push the changes")
            print()
            print("Example workflow file (.github/workflows/ci.yml):")
            print("  name: CI")
            print("  on:")
            print("    push:")
            print("      branches: [ main ]")
            print("    pull_request:")
            print("      branches: [ main ]")
            print("  jobs:")
            print("    build:")
            print("      runs-on: ubuntu-latest")
            print("      steps:")
            print("      - uses: actions/checkout@v4")
            print("      - name: Run tests")
            print("        run: echo 'Add your test commands here'")
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


def cmd_pipeline_list(repo_filter: str = None, show_all: bool = False, verbose: bool = False) -> int:
    """Handle 'sdo pipeline list' command."""
    config = get_pipeline_config()
    if config is None:
        return 1

    platform = config.get("platform")

    if platform == "azdo":
        return cmd_azdo_pipeline_list(config=config, repo_filter=repo_filter, show_all=show_all, verbose=verbose)
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


def cmd_pipeline_run(pipeline_name: str = None, branch: str = None, verbose: bool = False) -> int:
    """Handle 'sdo pipeline run' command."""
    config = get_pipeline_config()
    if config is None:
        return 1

    platform = config.get("platform")

    if platform == "azdo":
        return cmd_azdo_pipeline_run(config=config, pipeline_name=pipeline_name, branch=branch, verbose=verbose)
    elif platform == "github":
        workflow_name = config.get("workflowName")
        return cmd_github_workflow_run(workflow_name, branch, verbose)
    else:
        print(f"❌ Unsupported platform: {platform}")
        return 1


def cmd_pipeline_status(build_id: int = None, verbose: bool = False) -> int:
    """Handle 'sdo pipeline status' command."""
    config = get_pipeline_config()
    if config is None:
        return 1

    platform = config.get("platform")

    # If no build_id provided, get the latest build
    if build_id is None:
        if platform == "azdo":
            # Get the latest build ID for Azure DevOps
            pat = get_personal_access_token()
            if not pat:
                print("Missing required configuration: AZURE_DEVOPS_PAT environment variable")
                print("Please set AZURE_DEVOPS_PAT environment variable.")
                return 1

            client = AzureDevOpsClient(
                config['organization'],
                config['project'],
                pat
            )

            pipeline_name = config['pipelineName']
            builds = client.list_builds(pipeline_name=pipeline_name, top=1)

            if not builds:
                print("❌ No builds found")
                return 1

            build_id = builds[0]['id']
            print(f"Getting status for latest build (ID: {build_id})...")

        elif platform == "github":
            # For GitHub, get the latest run for the default workflow and show its details
            if not _check_gh_cli():
                print(f"❌ {MISSING_GH_MSG}")
                return 1

            # Get the workflow filename from the path
            workflow_path = config.get('workflowYamlPath', '')
            if workflow_path:
                # Extract filename from path (e.g., ".github/workflows/ntools.yml" -> "ntools.yml")
                workflow_filename = workflow_path.split('/')[-1]
                workflow_filter = workflow_filename
            else:
                # Fallback to workflow name
                workflow_filter = config.get('workflowName', 'ci')

            print(f"Getting status for latest '{workflow_filter}' workflow run...")
            returncode, stdout, stderr = _run_gh_command([
                "run", "list", 
                "--workflow", workflow_filter,
                "--limit", "1", 
                "--json", "databaseId,status,name,conclusion"
            ], False)

            if returncode != 0:
                print(f"❌ Failed to get latest run: {stderr}")
                return 1

            try:
                import json
                runs = json.loads(stdout)
                if not runs:
                    print(f"No workflow runs found for '{workflow_filter}'.")
                    return 0

                latest_run = runs[0]
                run_id = str(latest_run['databaseId'])
                status = latest_run.get('status', 'unknown')
                conclusion = latest_run.get('conclusion', '')
                name = latest_run.get('name', 'Unknown')
                
                print(f"Latest run: {name} (ID: {run_id})")
                print(f"Status: {status}" + (f" - {conclusion}" if conclusion else ""))
                
                return cmd_github_run_view(run_id, verbose)
            except (json.JSONDecodeError, KeyError, IndexError) as e:
                print(f"❌ Failed to parse run data: {e}")
                return 1
        else:
            print(f"❌ Unsupported platform: {platform}")
            return 1

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
        # For GitHub, show details about the most recent run
        if not _check_gh_cli():
            print(f"❌ {MISSING_GH_MSG}")
            return 1

        # Get the workflow filename from the path
        workflow_path = config.get('workflowYamlPath', '')
        if workflow_path:
            # Extract filename from path (e.g., ".github/workflows/ntools.yml" -> "ntools.yml")
            workflow_filename = workflow_path.split('/')[-1]
            workflow_filter = workflow_filename
        else:
            # Fallback to workflow name
            workflow_filter = config.get('workflowName', 'ci')

        print(f"Getting information about the last '{workflow_filter}' workflow run...")
        returncode, stdout, stderr = _run_gh_command([
            "run", "list",
            "--workflow", workflow_filter,
            "--limit", "1",
            "--json", "databaseId,status,conclusion,name,headBranch,headSha,createdAt,updatedAt,event"
        ], False)

        if returncode != 0:
            print(f"❌ Failed to get last run: {stderr}")
            return 1

        try:
            import json
            runs = json.loads(stdout)
            if not runs:
                print(f"No workflow runs found for '{workflow_filter}'.")
                return 0

            latest_run = runs[0]
            run_id = str(latest_run['databaseId'])
            status = latest_run.get('status', 'unknown')
            conclusion = latest_run.get('conclusion', '')
            name = latest_run.get('name', 'Unknown')
            branch = latest_run.get('headBranch', 'unknown')
            sha = latest_run.get('headSha', 'unknown')[:8]  # Short SHA
            created_at = latest_run.get('createdAt', 'unknown')
            updated_at = latest_run.get('updatedAt', 'unknown')
            event = latest_run.get('event', 'unknown')

            print(f"✅ Last Run ID: {run_id}")
            print(f"   Name: {name}")
            print(f"   Status: {status}" + (f" - {conclusion}" if conclusion else ""))
            print(f"   Branch: {branch}")
            print(f"   Commit: {sha}")
            print(f"   Trigger: {event}")
            print(f"   Created: {created_at}")
            print(f"   Updated: {updated_at}")
            print(f"   URL: https://github.com/{config['owner']}/{config['repo']}/actions/runs/{run_id}")

            # Use 'sdo pipeline status <run_id>' to check progress
            print(f"\n💡 Use 'sdo pipeline status {run_id}' to check progress.")

            return 0
        except (json.JSONDecodeError, KeyError, IndexError) as e:
            print(f"❌ Failed to parse run data: {e}")
            return 1
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