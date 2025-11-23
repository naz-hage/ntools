"""
Repository Operations Module
Contains command handlers for repository operations across multiple platforms.

ARCHITECTURAL DECISION: CONSOLIDATED REPOSITORY IMPLEMENTATIONS

WHY CONSOLIDATED IN ONE FILE?
Repository operations are simpler and more similar across platforms:

SIMILARITIES ACROSS PLATFORMS:
- Basic CRUD operations: create, read, list, delete
- Common repository metadata: name, description, URL, visibility
- Similar authentication patterns (tokens)
- Standard repository concepts

MINIMAL PLATFORM DIFFERENCES:
- Azure DevOps: REST API calls with PAT authentication
- GitHub: CLI commands with gh authentication
- Both return similar repository data structures

WHY NOT SEPARATE FILES LIKE WORK ITEMS?
Repository operations are fundamentally similar. The main differences are:
- API mechanism (REST vs CLI)
- Authentication method (PAT vs gh token)
- Minor data format variations

Consolidation reduces code duplication while maintaining platform-specific logic
in separate classes within the same module.

ARCHITECTURE OVERVIEW:
- RepositoryPlatform: Abstract base class defining common interface for all platforms
- AzureDevOpsRepositoryPlatform: Azure DevOps-specific implementation using REST APIs
- GitHubRepositoryPlatform: GitHub-specific implementation using GitHub CLI

COMMON CODE:
- RepositoryPlatform abstract base class (platform-agnostic interface)
- create_repository_platform() factory function (platform detection and instantiation)
- Command handlers in repositories.py (CLI integration)

PLATFORM-SPECIFIC CODE:
- Authentication methods (PAT for Azure DevOps, gh CLI for GitHub)
- API calls (REST APIs for Azure DevOps, CLI commands for GitHub)
- Data format transformations (Azure DevOps JSON vs GitHub CLI JSON)
"""

import os
from abc import ABC, abstractmethod
from typing import Dict, Any, Optional, List

# Import from package
from .client import AzureDevOpsClient, ConfigManager, ensure_config_exists, get_personal_access_token, extract_platform_info_from_git

# Constants for error messages
MISSING_PAT_MSG = 'AZURE_DEVOPS_PAT environment variable'
MISSING_CONFIG_MSG = "Please set AZURE_DEVOPS_PAT environment variable."


class RepositoryPlatform(ABC):
    """
    Abstract base class for repository operations across different platforms.

    COMMON INTERFACE: All platforms must implement these methods
    - validate_auth(): Platform-specific authentication validation
    - create_repository(): Create new repository
    - get_repository(): Get repository information
    - list_repositories(): List all repositories
    - delete_repository(): Delete repository
    """

    def __init__(self, config: Dict[str, str], verbose: bool = False):
        self.config = config
        self.verbose = verbose

    @abstractmethod
    def validate_auth(self) -> bool:
        """Validate authentication for the platform."""
        pass

    @abstractmethod
    def create_repository(self, name: str) -> bool:
        """Create a new repository."""
        pass

    @abstractmethod
    def get_repository(self, name: str) -> Optional[Dict[str, Any]]:
        """Get repository information."""
        pass

    @abstractmethod
    def list_repositories(self) -> Optional[List[Dict[str, Any]]]:
        """List all repositories."""
        pass

    @abstractmethod
    def delete_repository(self, name: str) -> bool:
        """Delete a repository."""
        pass


class AzureDevOpsRepositoryPlatform(RepositoryPlatform):
    """
    Azure DevOps implementation of repository operations.

    AZURE DEVOPS SPECIFIC:
    - Uses Personal Access Tokens (PAT) for authentication
    - Makes direct REST API calls to Azure DevOps services
    - Requires organization, project, and repository context
    - Returns data in Azure DevOps API format
    """

    def validate_auth(self) -> bool:
        """Validate Azure DevOps authentication."""
        pat = get_personal_access_token()
        if not pat:
            print("❌ AZURE_DEVOPS_PAT environment variable not set")
            print("Please set your Azure DevOps Personal Access Token:")
            print("export AZURE_DEVOPS_PAT='your-token-here'")
            return False
        return True

    def create_repository(self, name: str) -> bool:
        """Create a new repository in Azure DevOps."""
        try:
            pat = get_personal_access_token()
            client = AzureDevOpsClient(
                self.config['organization'],
                self.config['project'],
                pat,
                verbose=self.verbose
            )
            return client.create_repository(name)
        except Exception as e:
            print(f"❌ Error creating repository: {e}")
            return False

    def get_repository(self, name: str) -> Optional[Dict[str, Any]]:
        """Get repository information from Azure DevOps."""
        try:
            pat = get_personal_access_token()
            client = AzureDevOpsClient(
                self.config['organization'],
                self.config['project'],
                pat,
                verbose=self.verbose
            )
            return client.get_repository(name)
        except Exception as e:
            print(f"❌ Error getting repository info: {e}")
            return None

    def list_repositories(self) -> Optional[List[Dict[str, Any]]]:
        """List all repositories in the Azure DevOps project."""
        try:
            pat = get_personal_access_token()
            client = AzureDevOpsClient(
                self.config['organization'],
                self.config['project'],
                pat,
                verbose=self.verbose
            )
            return client.list_repositories()
        except Exception as e:
            print(f"❌ Error listing repositories: {e}")
            return None

    def delete_repository(self, name: str) -> bool:
        """Delete a repository from Azure DevOps."""
        try:
            pat = get_personal_access_token()
            client = AzureDevOpsClient(
                self.config['organization'],
                self.config['project'],
                pat,
                verbose=self.verbose
            )
            return client.delete_repository(name)
        except Exception as e:
            print(f"❌ Error deleting repository: {e}")
            return False


class GitHubRepositoryPlatform(RepositoryPlatform):
    """
    GitHub implementation of repository operations.

    GITHUB SPECIFIC:
    - Uses GitHub CLI (gh) for authentication and operations
    - Executes CLI commands instead of direct API calls
    - Requires owner (user/org) and repository context
    - Returns data in GitHub CLI JSON format (transformed to match common interface)
    """

    def validate_auth(self) -> bool:
        """Validate GitHub authentication."""
        try:
            import subprocess
            result = subprocess.run(
                ["gh", "auth", "status"],
                capture_output=True,
                text=True
            )
            if result.returncode == 0:
                return True
            else:
                print("❌ GitHub CLI not authenticated")
                print("Please run: gh auth login")
                return False
        except FileNotFoundError:
            print("❌ GitHub CLI not installed")
            print("Please install GitHub CLI: https://cli.github.com/")
            return False
        except Exception as e:
            print(f"❌ Error validating GitHub auth: {e}")
            return False

    def create_repository(self, name: str) -> bool:
        """Create a new repository in GitHub."""
        try:
            import subprocess

            # Use gh repo create command
            cmd = [
                "gh", "repo", "create",
                f"{self.config['owner']}/{name}",
                "--public",  # Default to public, could be made configurable
                "--description", f"Repository {name}"
            ]

            if self.verbose:
                print(f"Running: {' '.join(cmd)}")

            result = subprocess.run(cmd, capture_output=True, text=True)

            if result.returncode == 0:
                print(f"✓ Created repository '{self.config['owner']}/{name}' successfully")
                return True
            else:
                print(f"❌ Failed to create repository '{name}': {result.stderr.strip()}")
                return False

        except Exception as e:
            print(f"❌ Error creating repository: {e}")
            return False

    def get_repository(self, name: str) -> Optional[Dict[str, Any]]:
        """Get repository information from GitHub."""
        try:
            import subprocess
            import json

            # Use gh repo view command
            cmd = [
                "gh", "repo", "view",
                f"{self.config['owner']}/{name}",
                "--json", "name,description,url,createdAt,updatedAt,defaultBranchRef,isPrivate"
            ]

            if self.verbose:
                print(f"Running: {' '.join(cmd)}")

            result = subprocess.run(cmd, capture_output=True, text=True)

            if result.returncode == 0:
                repo_data = json.loads(result.stdout)
                return {
                    'name': repo_data['name'],
                    'description': repo_data.get('description', ''),
                    'webUrl': repo_data['url'],
                    'defaultBranch': repo_data.get('defaultBranchRef', {}).get('name', 'main'),
                    'isPrivate': repo_data.get('isPrivate', False),
                    'createdDate': repo_data.get('createdAt'),
                    'lastUpdated': repo_data.get('updatedAt')
                }
            else:
                if "Not Found" in result.stderr or "404" in result.stderr:
                    print(f"❌ Repository '{self.config['owner']}/{name}' not found")
                else:
                    print(f"❌ Failed to get repository info: {result.stderr.strip()}")
                return None

        except Exception as e:
            print(f"❌ Error getting repository info: {e}")
            return None

    def list_repositories(self) -> Optional[List[Dict[str, Any]]]:
        """List all repositories for the GitHub user/organization."""
        try:
            import subprocess
            import json

            # Use gh repo list command
            cmd = [
                "gh", "repo", "list",
                self.config['owner'],
                "--json", "name,description,url,createdAt,updatedAt,defaultBranchRef,isPrivate"
            ]

            if self.verbose:
                print(f"Running: {' '.join(cmd)}")

            result = subprocess.run(cmd, capture_output=True, text=True)

            if result.returncode == 0:
                repos_data = json.loads(result.stdout)
                repos = []
                for repo_data in repos_data:
                    repos.append({
                        'name': repo_data['name'],
                        'description': repo_data.get('description', ''),
                        'webUrl': repo_data['url'],
                        'defaultBranch': repo_data.get('defaultBranchRef', {}).get('name', 'main'),
                        'isPrivate': repo_data.get('isPrivate', False),
                        'createdDate': repo_data.get('createdAt'),
                        'lastUpdated': repo_data.get('updatedAt')
                    })
                return repos
            else:
                print(f"❌ Failed to list repositories: {result.stderr.strip()}")
                return None

        except Exception as e:
            print(f"❌ Error listing repositories: {e}")
            return None

    def delete_repository(self, name: str) -> bool:
        """Delete a repository from GitHub."""
        try:
            import subprocess

            # Use gh repo delete command
            cmd = [
                "gh", "repo", "delete",
                f"{self.config['owner']}/{name}",
                "--yes"  # Skip confirmation prompt
            ]

            if self.verbose:
                print(f"Running: {' '.join(cmd)}")

            result = subprocess.run(cmd, capture_output=True, text=True)

            if result.returncode == 0:
                print(f"✓ Deleted repository '{self.config['owner']}/{name}' successfully")
                return True
            else:
                print(f"❌ Failed to delete repository '{name}': {result.stderr.strip()}")
                return False

        except Exception as e:
            print(f"❌ Error deleting repository: {e}")
            return False


def get_repo_config() -> Optional[Dict[str, str]]:
    """Get repository configuration from Git remote information."""
    return extract_platform_info_from_git()


def create_repository_platform(config: Dict[str, str], verbose: bool = False) -> Optional[RepositoryPlatform]:
    """
    Create the appropriate repository platform instance based on configuration.

    PLATFORM DETECTION AND INSTANTIATION:
    - Reads 'platform' key from config (set by extract_platform_info_from_git())
    - 'azdo' -> AzureDevOpsRepositoryPlatform
    - 'github' -> GitHubRepositoryPlatform
    - Returns None for unsupported platforms

    COMMON ENTRY POINT: This function provides unified access to platform-specific implementations
    """
    platform = config.get('platform')
    if platform == 'azdo':
        return AzureDevOpsRepositoryPlatform(config, verbose)
    elif platform == 'github':
        return GitHubRepositoryPlatform(config, verbose)
    else:
        print(f"❌ Unsupported platform: {platform}")
        return None


def cmd_repo_create(verbose=False):
    """Handle 'sdo repo create' command."""
    config = get_repo_config()
    if config is None:
        return 1

    # Create platform instance
    platform = create_repository_platform(config, verbose)
    if platform is None:
        return 1

    # Validate authentication
    if not platform.validate_auth():
        return 1

    # Get repository name from config (GitHub uses 'repo', Azure DevOps uses 'repository')
    repo_name = config.get('repository') or config.get('repo')
    if not repo_name:
        print("❌ Could not determine repository name from Git remote")
        return 1

    # Check if repository already exists
    existing_repo = platform.get_repository(repo_name)
    if existing_repo:
        print(f"✓ Repository '{repo_name}' already exists")
        return 0

    # Create the repository
    if platform.create_repository(repo_name):
        return 0
    else:
        return 1


def cmd_repo_show(verbose=False):
    """Handle 'sdo repo show' command."""
    config = get_repo_config()
    if config is None:
        return 1

    # Create platform instance
    platform = create_repository_platform(config, verbose)
    if platform is None:
        return 1

    # Validate authentication
    if not platform.validate_auth():
        return 1

    # Get repository name from config (GitHub uses 'repo', Azure DevOps uses 'repository')
    repo_name = config.get('repository') or config.get('repo')
    if not repo_name:
        print("❌ Could not determine repository name from Git remote")
        return 1

    # Get repository information
    repo = platform.get_repository(repo_name)

    if repo:
        print("Repository Information:")
        print(f"  Name: {repo['name']}")
        print(f"  URL: {repo['webUrl']}")
        if 'defaultBranch' in repo:
            print(f"  Default Branch: {repo['defaultBranch']}")
        if 'isPrivate' in repo:
            print(f"  Private: {repo['isPrivate']}")
        if 'description' in repo and repo['description']:
            print(f"  Description: {repo['description']}")
        return 0
    else:
        print(f"❌ Repository '{repo_name}' not found")
        return 1


def cmd_repo_list(verbose=False):
    """Handle 'sdo repo list' command."""
    config = get_repo_config()
    if config is None:
        return 1

    # Create platform instance
    platform = create_repository_platform(config, verbose)
    if platform is None:
        return 1

    # Validate authentication
    if not platform.validate_auth():
        return 1

    # List repositories
    repos = platform.list_repositories()

    if repos is not None:
        if not repos:
            print("No repositories found.")
            return 0

        platform_name = config.get('platform', 'unknown').upper()
        location = ""
        if config.get('platform') == 'azdo':
            location = f"project '{config.get('project', 'unknown')}'"
        elif config.get('platform') == 'github':
            location = f"account '{config.get('owner', 'unknown')}'"

        print(f"Repositories in {platform_name} {location} ({len(repos)} total):")
        print("-" * 80)
        for repo in repos:
            print(f"  {repo['name']}")
            if 'webUrl' in repo:
                print(f"    URL: {repo['webUrl']}")
            if 'defaultBranch' in repo:
                print(f"    Default Branch: {repo['defaultBranch']}")
            if 'isPrivate' in repo:
                privacy = "Private" if repo['isPrivate'] else "Public"
                print(f"    Visibility: {privacy}")
            print()
        return 0
    else:
        print("❌ Failed to list repositories")
        return 1


def cmd_repo_delete(verbose=False):
    """Handle 'sdo repo delete' command."""
    config = get_repo_config()
    if config is None:
        return 1

    # Create platform instance
    platform = create_repository_platform(config, verbose)
    if platform is None:
        return 1

    # Validate authentication
    if not platform.validate_auth():
        return 1

    # Get repository name from config (always available from Git extraction)
    repo_name = config['repository']

    # Confirm deletion
    platform_name = config.get('platform', 'unknown').upper()
    print(f"⚠️  WARNING: This will permanently delete the repository '{repo_name}' from {platform_name}!")
    print("This action cannot be undone.")
    try:
        confirm = input("Are you sure you want to continue? (yes/no): ").strip().lower()
        if confirm not in ['yes', 'y']:
            print("Repository deletion cancelled.")
            return 0
    except KeyboardInterrupt:
        print("\nRepository deletion cancelled.")
        return 0

    # Delete repository
    print(f"Deleting repository '{repo_name}'...")
    success = platform.delete_repository(repo_name)

    if success:
        print("✅ Repository deleted successfully!")
        return 0
    else:
        print("❌ Failed to delete repository")
        return 1