"""
GitHub platform implementation for work item management.
"""

import subprocess
import tempfile
import os
from typing import Dict, Any, Optional, List

try:
    from .base import WorkItemPlatform
    from ..client import extract_platform_info_from_git
    from ..exceptions import ConfigurationError
except ImportError:
    from base import WorkItemPlatform
    from client import extract_platform_info_from_git
    from exceptions import ConfigurationError


class GitHubPlatform(WorkItemPlatform):
    """GitHub implementation of work item platform."""

    def get_config(self) -> Dict[str, str]:
        """Get GitHub configuration by extracting from Git remote."""
        platform_info = extract_platform_info_from_git()
        if platform_info and platform_info.get('platform') == 'github':
            config = {
                'owner': platform_info['owner'],
                'repo': platform_info['repo'],
                'remote_url': platform_info['remote_url']
            }
            if self.verbose:
                print("✓ Extracted GitHub information from Git remote:")
                print(f"  Owner: {config['owner']}")
                print(f"  Repository: {config['repo']}")
            return config
        else:
            raise ConfigurationError(
                "Could not extract GitHub info from Git remote",
                "Please ensure you are in a GitHub Git repository with properly configured remotes."
            )

    def validate_auth(self) -> bool:
        """Validate GitHub CLI authentication."""
        try:
            result = subprocess.run(["gh", "auth", "status"],
                                  capture_output=True, text=True, check=False)
            if result.returncode == 0:
                if self.verbose:
                    print("✓ GitHub CLI authenticated")
                return True
            else:
                print("❌ GitHub CLI not authenticated.")
                print("Please run gh auth login to authenticate.")
                return False
        except FileNotFoundError:
            print("❌ GitHub CLI (gh) not found.")
            print("Please install GitHub CLI: https://cli.github.com/")
            return False

    def create_work_item(
        self,
        title: str,
        description: str,
        metadata: Dict[str, Any],
        acceptance_criteria: Optional[List[str]] = None,
        dry_run: bool = False
    ) -> Optional[Dict[str, Any]]:
        """Create a GitHub issue."""
        if not self.validate_auth():
            return None

        repo = metadata.get("repo") or metadata.get("repository")
        if not repo:
            print("❌ Repository not specified. Please add Repository: owner/repo "
                  "to your markdown file.")
            return None

        # Build issue body: Description + Acceptance Criteria
        body_lines = []

        if description:
            body_lines.append(description)
            body_lines.append('')  # blank line

        if acceptance_criteria:
            body_lines.append('## Acceptance Criteria')
            for ac in acceptance_criteria:
                # Keep the checkboxes in the format they appear in the markdown
                if ac.strip().startswith('[ ]') or ac.strip().startswith('[x]'):
                    body_lines.append(f'- {ac.strip()}')
                else:
                    body_lines.append(f'- [ ] {ac.strip()}')

        body = '\n'.join(body_lines)
        labels = (metadata.get('labels') or '').strip()  # handle None case
        assignee = (metadata.get('assignee') or '').strip() or None  # handle None case

        if dry_run:
            print('[dry-run] Would create GitHub issue with:')
            print(f'  Repository: {repo}')
            print(f'  Title: {title}')
            print(f'  Labels: {labels}')
            if assignee:
                print(f'  Assignee: {assignee}')
            print('  Body:')
            print(body)
            return {"dry_run": True, "repo": repo, "title": title}

        try:
            # Create temporary file for body content
            with tempfile.NamedTemporaryFile(mode='w', suffix='.md', delete=False,
                                           encoding='utf-8') as f:
                f.write(body)
                temp_body_file = f.name

            try:
                # Build gh command
                cmd = ['gh', 'issue', 'create', '--title', title, '--body-file',
                       temp_body_file, '--repo', repo]

                # Add labels if specified (split comma-separated and trim)
                if labels:
                    label_list = [label.strip() for label in labels.split(',')
                                 if label.strip()]
                    for label in label_list:
                        cmd.extend(['--label', label])

                if assignee:
                    cmd.extend(['--assignee', assignee])

                # Execute the command
                if self.verbose:
                    print(f"Creating GitHub issue in {repo}...")
                    # Show equivalent gh CLI command
                    print(f"   Equivalent gh CLI command:")
                    print(f"   {' '.join(cmd)}")
                result = subprocess.run(cmd, capture_output=True, text=True, check=True)

                # Parse the issue URL from the output
                issue_url = result.stdout.strip()
                print(f"✓ Created GitHub issue: {issue_url}")

                return {
                    "url": issue_url,
                    "repo": repo,
                    "title": title,
                    "labels": labels,
                    "assignee": assignee
                }

            except subprocess.CalledProcessError as e:
                print(f"❌ GitHub CLI command failed: {e}")
                if e.stderr:
                    print(f"stderr: {e.stderr}")
                return None
            except FileNotFoundError:
                print("❌ GitHub CLI (`gh`) not found. Please install it from "
                      "https://cli.github.com/")
                return None
            finally:
                # Clean up temp file
                if os.path.exists(temp_body_file):
                    os.unlink(temp_body_file)

        except Exception as e:
            print(f"❌ Error creating GitHub issue: {e}")
            return None
