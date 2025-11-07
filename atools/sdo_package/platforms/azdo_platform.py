"""
Azure DevOps platform implementation for work item management.
"""

import os
from typing import Dict, Any, Optional, List

try:
    from .base import WorkItemPlatform
    from ..client import extract_platform_info_from_git
    from ..exceptions import ConfigurationError
    from ..parsers.metadata_parser import MetadataParser
except ImportError:
    from base import WorkItemPlatform
    from client import extract_platform_info_from_git
    from exceptions import ConfigurationError
    from parsers.metadata_parser import MetadataParser


class AzureDevOpsPlatform(WorkItemPlatform):
    """Azure DevOps implementation of work item platform."""

    def get_config(self) -> Dict[str, str]:
        """Get Azure DevOps configuration by extracting from Git remote."""
        platform_info = extract_platform_info_from_git()
        if platform_info and platform_info.get('platform') == 'azdo':
            git_info = {
                'organization': platform_info['organization'],
                'project': platform_info['project'],
                'repository': platform_info['repository'],
                'remote_url': platform_info['remote_url']
            }
            if self.verbose:
                print("✓ Extracted Azure DevOps information from Git remote:")
                print(f"  Organization: {git_info['organization']}")
                print(f"  Project: {git_info['project']}")
                print(f"  Repository: {git_info['repository']}")
            return git_info
        else:
            raise ConfigurationError(
                "Could not extract Azure DevOps info from Git remote",
                "Please ensure you are in an Azure DevOps Git repository "
                "with properly configured remotes."
            )

    def validate_auth(self) -> bool:
        """Validate Azure DevOps authentication."""
        pat = os.environ.get("AZURE_DEVOPS_PAT") or os.environ.get("AZURE_DEVOPS_EXT_PAT")
        if not pat:
            print("❌ AZURE_DEVOPS_PAT environment variable not set.")
            print("Please set your Azure DevOps Personal Access Token.")
            return False
        return True

    def create_work_item(
        self,
        title: str,
        description: str,
        metadata: Dict[str, Any],
        acceptance_criteria: Optional[List[str]] = None,
        dry_run: bool = False
    ) -> Optional[Dict[str, Any]]:
        """Create an Azure DevOps work item."""
        try:
            import requests
        except ImportError:
            print("❌ requests library not available. Install with: pip install requests")
            return None

        # Normalize work item type early for dry-run display
        work_item_type_raw = metadata.get('work_item_type', 'PBI')
        work_item_type = MetadataParser.normalize_work_item_type(work_item_type_raw, 'azdo')

        if dry_run:
            print('[dry-run] Would create Azure DevOps work item with:')
            print(f'  Title: {title}')
            print(f'  Project: {metadata.get("project", "Not specified")}')
            print(f'  Work Item Type: {work_item_type_raw} (normalized: {work_item_type})')
            if metadata.get('area'):
                print(f'  Area: {metadata.get("area")}')
            if metadata.get('iteration'):
                print(f'  Iteration: {metadata.get("iteration")}')
            if metadata.get('assignee'):
                print(f'  Assignee: {metadata.get("assignee")}')
            print('  Description:')
            print(description)
            if acceptance_criteria:
                print('  Acceptance Criteria:')
                print(f'    {len(acceptance_criteria)} items (will be added via dedicated field or appended to description)')
                if self.verbose:
                    for i, ac in enumerate(acceptance_criteria, 1):
                        print(f'    {i}. {ac}')
            if self.verbose:
                print(f'  Normalized Work Item Type: {work_item_type}')
                print(f'  Organization: {metadata.get("organization", "Not specified")}')
                print(f'  Area Path: {metadata.get("area", f"{metadata.get("project", "Project")}\\Area")}')
                print(f'  Iteration Path: {metadata.get("iteration", f"{metadata.get("project", "Project")}\\Iteration")}')
                print(f'  Acceptance Criteria Count: {len(acceptance_criteria) if acceptance_criteria else 0}')
            return {"dry_run": True, "title": title, "project": metadata.get("project")}

        # Get configuration - try Git remote first, then fall back to metadata
        organization = metadata.get('organization')
        project = metadata.get('project')

        if not organization or not project:
            try:
                config = self.get_config()
                organization = config.get('organization')
                project = config.get('project')
            except ConfigurationError:
                print("❌ Missing organization or project configuration")
                print("   Either configure Git remote for Azure DevOps, or specify 'organization' and 'project' in metadata")
                return None

        if not organization or not project:
            print("❌ Missing organization or project configuration")
            return None

        # Get PAT from environment
        pat = os.environ.get("AZURE_DEVOPS_PAT") or os.environ.get("AZURE_DEVOPS_EXT_PAT")
        if not pat:
            print("❌ AZURE_DEVOPS_PAT environment variable not set")
            return None

        # Encode PAT for Basic authentication
        import base64
        auth_token = base64.b64encode(f":{pat}".encode('utf-8')).decode('utf-8')

        area_path = metadata.get('area', f"{project}\\Area")
        iteration_path = metadata.get('iteration', f"{project}\\Iteration")
        assignee = metadata.get('assignee')

        # Initialize operations list for work item creation
        operations = []

        # Build description with acceptance criteria
        full_description = description
        if acceptance_criteria:
            # Handle Acceptance Criteria: prefer a dedicated work item field if available in the project
            if self.verbose:
                print(f"📋 Processing acceptance criteria for {work_item_type}")
            ac_html = '<ul>' + '\n'.join([f'<li>{ac}</li>' for ac in acceptance_criteria]) + '</ul>'
            
            # Try to discover a field whose name/reference contains 'acceptance'
            ac_field_ref = None
            try:
                fields_url = f"https://dev.azure.com/{organization}/{project}/_apis/wit/fields?api-version=7.1"
                if self.verbose:
                    print(f"🔍 Checking for acceptance criteria fields at: {fields_url}")
                response = requests.get(fields_url, headers={'Authorization': f'Basic {auth_token}'}, timeout=30)
                if response.status_code == 200:
                    fields_data = response.json()
                    if self.verbose:
                        print(f"📊 Found {len(fields_data.get('value', []))} fields")
                    for field in fields_data.get('value', []):
                        field_name = field.get('name', '').lower()
                        if 'acceptance' in field_name:
                            ac_field_ref = field.get('referenceName')
                            if self.verbose:
                                print(f"✅ Found acceptance criteria field: {field.get('name')} -> {ac_field_ref}")
                            break
                    if not ac_field_ref and self.verbose:
                        print("ℹ️  No dedicated acceptance criteria field found")
                else:
                    if self.verbose:
                        print(f"⚠️  Failed to get fields, status: {response.status_code}")
            except Exception as e:
                if self.verbose:
                    print(f"⚠️  Exception checking fields: {e}")

            if ac_field_ref and work_item_type != 'Task':
                # Use dedicated acceptance criteria field (for PBIs, Bugs, etc., but NOT Tasks)
                operations.append({"op": "add", "path": f"/fields/{ac_field_ref}", "value": ac_html})
                if self.verbose:
                    print(f"📝 Using dedicated field {ac_field_ref} for acceptance criteria")
            else:
                # Append to description (for Tasks and other work item types without dedicated field)
                if self.verbose:
                    print(f"📝 Using description approach for {work_item_type}")
                if full_description:
                    full_description += f'\n\n<h3>Acceptance Criteria</h3>\n{ac_html}'
                else:
                    full_description = f'<h3>Acceptance Criteria</h3>\n{ac_html}'

        # Azure DevOps work item creation payload
        # URL encode the work item type for the API call
        import urllib.parse
        encoded_work_item_type = urllib.parse.quote(work_item_type)
        url = f"https://dev.azure.com/{organization}/{project}/_apis/wit/workitems/${encoded_work_item_type}?api-version=7.1"

        headers = {
            'Content-Type': 'application/json-patch+json',
            'Authorization': f'Basic {auth_token}'
        }

        # Add standard work item fields to operations
        operations.extend([
            {
                "op": "add",
                "path": "/fields/System.Title",
                "value": title
            },
            {
                "op": "add",
                "path": "/fields/System.Description",
                "value": full_description
            },
            {
                "op": "add",
                "path": "/fields/System.AreaPath",
                "value": area_path
            },
            {
                "op": "add",
                "path": "/fields/System.IterationPath",
                "value": iteration_path
            }
        ])

        # Add assignee if provided
        if assignee:
            operations.append({
                "op": "add",
                "path": "/fields/System.AssignedTo",
                "value": assignee
            })

        # Add priority if provided
        priority = metadata.get('priority')
        if priority:
            operations.append({
                "op": "add",
                "path": "/fields/Microsoft.VSTS.Common.Priority",
                "value": int(priority)
            })

        # Add tags if provided
        tags = metadata.get('tags') or metadata.get('labels')
        if tags:
            if isinstance(tags, list):
                tags_str = "; ".join(tags)
            else:
                tags_str = str(tags)
            operations.append({
                "op": "add",
                "path": "/fields/System.Tags",
                "value": tags_str
            })

        if self.verbose:
            print(f"📡 Creating {work_item_type} in {organization}/{project}")
            print(f"   URL: {url}")
            print(f"   Operations: {len(operations)}")
            
            # Show equivalent az CLI command (without sensitive auth info)
            print(f"   Equivalent az CLI command:")
            az_cmd = f"az boards work-item create --title \"{title}\" --type \"{work_item_type}\""
            az_cmd += f" --project \"{project}\" --organization \"https://dev.azure.com/{organization}\""
            if area_path:
                az_cmd += f" --area \"{area_path}\""
            if iteration_path:
                az_cmd += f" --iteration \"{iteration_path}\""
            if assignee:
                az_cmd += f" --assigned-to \"{assignee}\""
            if full_description:
                # Escape quotes in description for CLI and truncate if too long
                escaped_desc = full_description.replace('"', '\\"')
                if len(escaped_desc) > 100:
                    escaped_desc = escaped_desc[:97] + "..."
                az_cmd += f" --description \"{escaped_desc}\""
            # Note: Authentication is handled via AZURE_DEVOPS_PAT environment variable or az login
            print(f"   {az_cmd}")
            print(f"   (Note: Requires AZURE_DEVOPS_PAT environment variable or 'az login')")

        try:
            response = requests.post(url, json=operations, headers=headers, timeout=30)

            if response.status_code in [200, 201]:
                work_item = response.json()
                work_item_id = work_item.get('id')
                work_item_url = work_item.get('_links', {}).get('html', {}).get('href')

                print(f"✅ Created {work_item_type} #{work_item_id}: {title}")
                if work_item_url:
                    print(f"   URL: {work_item_url}")

                return {
                    "id": work_item_id,
                    "url": work_item_url,
                    "type": work_item_type,
                    "title": title
                }
            else:
                error_msg = f"Failed to create work item (HTTP {response.status_code})"
                try:
                    error_details = response.json()
                    if 'message' in error_details:
                        error_msg += f": {error_details['message']}"
                    if self.verbose and 'detailedMessage' in error_details:
                        error_msg += f"\nDetails: {error_details['detailedMessage']}"
                except:
                    error_msg += f": {response.text[:200]}"

                print(f"❌ {error_msg}")
                return None

        except requests.exceptions.Timeout:
            print("❌ Request timed out. Azure DevOps may be experiencing issues.")
            return None
        except requests.exceptions.ConnectionError:
            print("❌ Connection error. Check your internet connection.")
            return None
        except Exception as e:
            print(f"❌ Unexpected error: {str(e)}")
            return None
