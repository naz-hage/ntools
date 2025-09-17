#!/usr/bin/env python3
"""
add-issue.py
Python scaffold to create work items from markdown files (GitHub / Azure DevOps).

This is initial scaffolding: CLI options, help, basic file validation, and dry-run output.

Usage:
  python add-issue.py --file-path example.md --target github --dry-run
"""

import argparse
import sys
from pathlib import Path
from datetime import datetime
import json
import re
import os
import urllib.parse

# requests is optional at runtime; we handle missing dependency with a helpful error
try:
    import requests
    from requests.auth import HTTPBasicAuth
except Exception:
    requests = None

# Tool identity/version - update __version__ when releasing a new tool version
__version__ = '0.1.0'
TOOL_NAME = Path(__file__).stem
TOOL_VERSION = __version__
# When a dry-run prints parsed fields first, set this flag so writers can avoid
# printing the full body/payload again (prevents duplicated output seen by users).
DRY_RUN_PRINTED = False


# Example markdown templates for reference
AZDO_TEMPLATE = '''# PBI-001: Enhanced Markdown Parser for Copilot

## Target: azdo
## Project: YourProject
## Area: YourProject\\YourArea
## Iteration: YourProject\\Sprint 1
## Assignee: 
## Labels: enhancement, high-priority
## Work Item Type: PBI

## Description

Brief description of the problem, opportunity, or goal. State what will change and why.

This PBI will enhance the markdown parser to support better metadata extraction for work item creation, improving the developer experience when creating issues from markdown files.

## Acceptance Criteria
- [ ] List clear, testable requirements for completion
- [ ] Use checkboxes for each criterion
- [ ] Include UI, logic, error handling, and test coverage as needed
'''

GITHUB_TEMPLATE = '''# PBI-001: Enhanced Markdown Parser for Copilot

## Target: github
## Repository: owner/repo-name
## Assignee: 
## Labels: 

## Description

Brief description of the problem, opportunity, or goal. State what will change and why.

This PBI will enhance the markdown parser to support better metadata extraction for work item creation, improving the developer experience when creating issues from markdown files.

## Acceptance Criteria
- [ ] List clear, testable requirements for completion
- [ ] Use checkboxes for each criterion
- [ ] Include UI, logic, error handling, and test coverage as needed
'''


def _collect_area_paths(node, out):
    # node may be a dict with 'path' and nested children keys varying by API shape
    if not isinstance(node, dict):
        return
    p = node.get('path') or node.get('name')
    if p:
        out.add(p)
    # try common child keys
    for key in ('children', 'childNodes', 'nodes', 'value'):
        child = node.get(key)
        if isinstance(child, list):
            for c in child:
                _collect_area_paths(c, out)
        elif isinstance(child, dict):
            _collect_area_paths(child, out)


def get_azdo_area_paths(org: str, project: str, pat: str, depth: int = 5):
    """Return a set of AreaPath strings for the given project by querying classification nodes.
    If the request fails return None.
    """
    if requests is None:
        return None
    url = f'https://dev.azure.com/{org}/{project}/_apis/wit/classificationnodes/areas?$depth={depth}&api-version=7.0'
    try:
        resp = requests.get(url, auth=HTTPBasicAuth('', pat), timeout=15)
        if resp.status_code != 200:
            return None
        j = resp.json()
        paths = set()
        # The API may return a node dict or a dict with 'value' list
        if isinstance(j, dict) and 'value' in j and isinstance(j['value'], list):
            for node in j['value']:
                _collect_area_paths(node, paths)
        else:
            _collect_area_paths(j, paths)
        return paths
    except Exception:
        return None


def _normalize_path(p: str) -> str:
    if not p:
        return ''
    # Normalize by removing leading backslashes and trimming
    return p.lstrip('\\/').strip().lower()


def _find_canonical_from_candidates(candidate: str, candidates_set):
    """Return canonical candidate from set using normalized matching, or None."""
    if not candidate:
        return None
    norm = _normalize_path(candidate)
    for c in candidates_set:
        if _normalize_path(c) == norm:
            return c
    # try prefix match
    for c in sorted(candidates_set):
        if _normalize_path(c).startswith(norm):
            return c
    # try suffix match (match last segments), e.g. provided 'proto\warriors' -> '\proto\area\warriors'
    for c in sorted(candidates_set):
        if _normalize_path(c).endswith(norm):
            return c
    return None


def parse_args():
    # Disable argparse's automatic -h/--help so we can print a custom header
    parser = argparse.ArgumentParser(
        prog='add-issue.py',
        description='Create a work item from a markdown file and publish to GitHub or Azure DevOps (scaffold).',
        formatter_class=argparse.RawDescriptionHelpFormatter,
        add_help=False,
        epilog='''
Examples:
    python add-issue.py --help-template azdo                              # Show Azure DevOps template
    python add-issue.py --help-template github                            # Show GitHub template
    python add-issue.py backlog/pbi-007.md --dry-run                      # Parse and preview (target from file)
    python add-issue.py backlog/pbi-007.md                                # Create work item (all params from file)
    python add-issue.py backlog/pbi-007.md --target azdo                  # Override target
    python add-issue.py --query 137 --target azdo --project MyProject    # Query existing item
'''
    )
    # Add custom help flag so we can print a consistent header before help text
    parser.add_argument('--help', '-h', action='store_true', dest='help_flag', help='Show this help message and exit')
    # Add positional argument for file path (optional) - user is encouraged to use --file-path
    parser.add_argument('file_path_pos', nargs='?', help='Path to markdown file containing the work item')

    # Note: --file-path is intentionally not marked required so that running the script
    # with no arguments can display the help text and a short description/banner
    # without raising an argparse error. The file-path requirement will be enforced
    # after showing help or parsing when appropriate.
    parser.add_argument('--file-path', '--file', '-f', required=False, help='Path to markdown file containing the work item')
    parser.add_argument('--target', choices=['github', 'azdo'], default='github', help='Target platform to create the work item (default: github)')
    parser.add_argument('--dry-run', action='store_true', help='Parse and print the extracted fields but do not create anything')
    parser.add_argument('--query', '-q', help='Query an existing work item by ID and display its details')
    parser.add_argument('--help-template', choices=['azdo', 'github'], help='Show example markdown template for the specified target')
    parser.add_argument('--work-item-type', choices=['PBI', 'Bug', 'Task'], default='PBI', help='Type of work item to create')
    parser.add_argument('--project', help='Azure DevOps project name (for azdo target)')
    parser.add_argument('--assignee', help='Assignee (username)')
    parser.add_argument('--labels', help='Comma-separated labels (GitHub) or tags')
    parser.add_argument('--area', help='Area path override (Azure DevOps)')
    parser.add_argument('--iteration', help='Iteration path override (Azure DevOps)')
    parser.add_argument('--require-criteria', action='store_true', help='Fail if Acceptance Criteria section is missing')
    parser.add_argument('--repo', help='GitHub repo owner/name (owner/repo)')
    parser.add_argument('--config', help='Optional path to a config file for tokens/credentials')
    parser.add_argument('--verbose', action='store_true', help='Show verbose output')
    args = parser.parse_args()
    
    # Handle positional file path
    if args.file_path_pos and not args.file_path:
        args.file_path = args.file_path_pos
    
    return parser, args


def validate_file(path: str) -> Path:
    p = Path(path)
    if not p.exists():
        raise FileNotFoundError(f"File not found: {p}")
    if not p.is_file():
        raise ValueError(f"Not a file: {p}")
    return p


def extract_fields_from_markdown(path: Path):
    """
    Very small conservative parser to extract Title, Description, Acceptance Criteria
    Returns a dict: {title, description, acceptance_criteria:list}
    """
    text = path.read_text(encoding='utf-8')
    lines = text.splitlines()

    title = None
    description_lines = []
    acceptance = []

    # Additional metadata fields we may find in templates
    metadata = {
        'target': None,      # github or azdo (overrides CLI --target)
        'project': None,     # Azure DevOps project
        'area': None,
        'iteration': None,
        'repository': None,  # GitHub repo (owner/repo format)
        'repo': None,        # alias for repository
        'organization': None,
        'assignee': None,
        'labels': None,
        'work_item_type': None,  # PBI, Bug, Task
        'config': None,
    }

    # Find first H1 or H2 near top as title
    for i, line in enumerate(lines[:20]):
        m = re.match(r'^#{1,2}\s+(.*)', line)
        if m:
            title = m.group(1).strip()
            title_line = i
            break

    if title is None:
        # fallback: use filename as title
        title = path.stem
        title_line = -1

    # Find Acceptance Criteria section
    ac_start = None
    for i, line in enumerate(lines):
        if re.match(r'^##\s*Acceptance Criteria', line, re.IGNORECASE):
            ac_start = i
            break

    # Also capture simple metadata headings like "## Target", "## Project", "## Area", etc.
    # We search the whole document for these short headings and capture the first paragraph after each.
    meta_map = {
        'target': r'^##\s*(Target|Platform)',
        'project': r'^##\s*(Project|Azure\s*Project)',
        'area': r'^##\s*Area',
        'iteration': r'^##\s*(Iteration|Iteration\s*Path)',
        'repository': r'^##\s*(Repository|Repo)',
        'repo': r'^##\s*(Repo|Repository|Org/Repo|Repository\s*Name)',
        'organization': r'^##\s*(Organization|Org)',
        'assignee': r'^##\s*Assignee',
        'labels': r'^##\s*Labels?',
        'work_item_type': r'^##\s*(Work\s*Item\s*Type|Type)',
        'config': r'^##\s*Config',
    }

    for i, line in enumerate(lines):
        for key, pattern in meta_map.items():
            if re.match(pattern, line, re.IGNORECASE):
                # For inline values (like "## Target: azdo"), extract directly from the line
                if ':' in line:
                    value = line.split(':', 1)[1].strip()
                    if value:
                        metadata[key] = value
                        break
                
                # Otherwise capture the paragraph following this heading
                j = i + 1
                # skip blank lines
                while j < len(lines) and lines[j].strip() == '':
                    j += 1
                # gather lines until blank or next heading
                buf = []
                while j < len(lines) and not re.match(r'^##\s*', lines[j]):
                    buf.append(lines[j].strip())
                    j += 1
                if buf:
                    metadata[key] = ' '.join(buf).strip()
                break

    # Handle repository field - prefer 'repository' over 'repo', combine with org if needed
    repo = metadata.get('repository') or metadata.get('repo')
    org = metadata.get('organization')
    if org and repo and '/' not in repo:
        metadata['repository'] = f"{org.strip()}/{repo.strip()}"
    elif repo:
        metadata['repository'] = repo

    # Capture description: first paragraph after title, stop at first ## heading
    desc_start = title_line + 1 if title_line >= 0 else 0
    # skip blank lines
    while desc_start < len(lines) and lines[desc_start].strip() == '':
        desc_start += 1
    
    # Look for "## Description" section specifically, or use first paragraph
    desc_section_start = None
    for i, line in enumerate(lines):
        if re.match(r'^##\s*Description', line, re.IGNORECASE):
            desc_section_start = i + 1
            break
    
    if desc_section_start is not None:
        # Extract content from ## Description section until next ## heading
        j = desc_section_start
        while j < len(lines) and lines[j].strip() == '':
            j += 1  # skip blank lines after heading
        while j < len(lines) and not lines[j].startswith('##'):
            if lines[j].strip():  # only add non-empty lines
                description_lines.append(lines[j].strip())
            j += 1
    else:
        # Fallback: use first paragraph after title
        desc_end = desc_start
        while desc_end < len(lines) and lines[desc_end].strip() != '' and not lines[desc_end].startswith('##'):
            description_lines.append(lines[desc_end].strip())
            desc_end += 1

    # Parse acceptance criteria list under the AC heading if present
    if ac_start is not None:
        for j in range(ac_start+1, len(lines)):
            l = lines[j].strip()
            if re.match(r'^##\s+', l):
                break
            if re.match(r'^[-*+]\s+', l) or re.match(r'^\d+\.', l):
                acceptance.append(re.sub(r'^[-*+\d\.\s]+', '', l).strip())

    return {
        'title': title,
        'description': '\n'.join(description_lines).strip(),
    'acceptance_criteria': acceptance,
    'metadata': metadata
    }


def dry_run_print(fields: dict, args=None):
    print('--- Parsed Fields ---')
    print(f"Title: {fields.get('title')}")
    print('')
    print('Description:')
    print(fields.get('description') or '(none)')
    print('')
    print('Acceptance Criteria:')
    ac = fields.get('acceptance_criteria') or []
    if not ac:
        print('  (none)')
    else:
        for i, a in enumerate(ac, 1):
            print(f'  {i}. {a}')
    print('')
    # Print extracted metadata if present. Show both GitHub and AzDo relevant fields so users can see all values.
    meta = fields.get('metadata') or {}

    # Show only parameters relevant to the selected target (default github)
    target = 'github'
    if args is not None:
        target = getattr(args, 'target', target) or target

    # End parsed fields first, then show parameters block (so parameters appear below the 'End' marker)
    print('--- End Parsed Fields ---')

    # Compute effective parameter values (prefer CLI overrides when present)
    if args is not None:
        effective_repo = getattr(args, 'repo', None) or meta.get('repository') or meta.get('repo') or ''
        effective_assignee = getattr(args, 'assignee', None) or meta.get('assignee') or ''
        effective_labels = getattr(args, 'labels', None) or meta.get('labels') or ''
        effective_config = getattr(args, 'config', None) or meta.get('config') or ''
        effective_area = getattr(args, 'area', None) or meta.get('area') or ''
        effective_iteration = getattr(args, 'iteration', None) or meta.get('iteration') or ''
    else:
        effective_repo = meta.get('repository') or meta.get('repo') or ''
        effective_assignee = meta.get('assignee') or ''
        effective_labels = meta.get('labels') or ''
        effective_config = meta.get('config') or ''
        effective_area = meta.get('area') or ''
        effective_iteration = meta.get('iteration') or ''

    # Print a fixed set of parameters so output is predictable
    print('Parameters:')
    if target == 'azdo':
        print(f'  Project: {meta.get("project") or ""}')
        print(f'  Area: {effective_area}')
        print(f'  Iteration: {effective_iteration}')
        print(f'  Assignee: {effective_assignee}')
        print(f'  Labels: {effective_labels}')
        print(f'  Config: {effective_config}')
    else:
        print(f'  Repo (org/repo): {effective_repo}')
        print(f'  Assignee: {effective_assignee}')
        print(f'  Labels: {effective_labels}')
        print(f'  Config: {effective_config}')
    # Mark that we printed the parsed-fields summary so writers can suppress
    # duplicate body/payload output when running in dry-run mode.
    global DRY_RUN_PRINTED
    DRY_RUN_PRINTED = True


def print_header():
    # Standardized header using the tool's name and version
    start_year = 2020
    current_year = datetime.now().year
    years = f"{start_year}-{current_year}" if current_year != start_year else f"{start_year}"
    print(f"*** {TOOL_NAME}, Build automation, naz-ahmad, {years} -  Version: {TOOL_VERSION}")
    print('')


def query_github_issue(issue_id: str, args):
    """Query and display a GitHub issue by ID."""
    print(f"[GitHub Query] Querying issue #{issue_id}...")
    print("GitHub query functionality not yet implemented.")
    # TODO: Implement GitHub API call to get issue details


def query_azdo_workitem(work_item_id: str, args):
    """Query and display an Azure DevOps work item by ID."""
    if requests is None:
        print('ERROR: requests library is required to query Azure DevOps work items. Install with: pip install requests')
        return

    # Get org and project
    org = os.environ.get('AZURE_DEVOPS_ORG')
    project = args.project
    if not org or not project:
        print('ERROR: Azure DevOps organization and project are required. Set AZURE_DEVOPS_ORG env and pass --project.')
        return

    # Read PAT from environment
    pat = os.environ.get('AZURE_DEVOPS_EXT_PAT') or os.environ.get('AZURE_DEVOPS_PAT')
    if not pat:
        print('ERROR: Azure DevOps PAT not found in AZURE_DEVOPS_EXT_PAT (or AZURE_DEVOPS_PAT).')
        return

    try:
        # Query work item
        url = f'https://dev.azure.com/{org}/_apis/wit/workItems/{work_item_id}?api-version=7.0'
        resp = requests.get(url, auth=HTTPBasicAuth('', pat), timeout=15)
        
        if resp.status_code != 200:
            try:
                err = resp.json()
            except Exception:
                err = resp.text
            print(f"ERROR querying work item {work_item_id}: HTTP {resp.status_code} - {err}")
            return

        data = resp.json()
        fields = data.get('fields', {})
        
        # Extract and display key fields
        title = fields.get('System.Title', '(no title)')
        description = fields.get('System.Description', '(no description)')
        work_item_type = fields.get('System.WorkItemType', 'Unknown')
        state = fields.get('System.State', 'Unknown')
        area = fields.get('System.AreaPath', '(no area)')
        iteration = fields.get('System.IterationPath', '(no iteration)')
        assignee = fields.get('System.AssignedTo', {})
        
        print(f"\n--- Work Item {work_item_id} ({work_item_type}) ---")
        print(f"Title: {title}")
        print(f"State: {state}")
        print(f"Area: {area}")
        print(f"Iteration: {iteration}")
        
        # Display assignee if present
        if assignee:
            if isinstance(assignee, dict):
                assignee_name = assignee.get('displayName') or assignee.get('uniqueName') or str(assignee)
            else:
                assignee_name = str(assignee)
            print(f"Assignee: {assignee_name}")
        else:
            print("Assignee: (unassigned)")
        print("\nDescription:")
        if description and description.strip():
            # Strip basic HTML tags for display
            import re
            clean_desc = re.sub(r'<[^>]+>', '', description)
            print(clean_desc.strip())
        else:
            print("(no description)")
        
        # Look for acceptance criteria in various possible fields
        ac_content = None
        ac_field_name = None
        
        # Check for dedicated acceptance criteria field
        for field_name, field_value in fields.items():
            if field_name.lower().find('acceptance') != -1 and field_value:
                ac_content = field_value
                ac_field_name = field_name
                break
        
        # If no dedicated field, check if AC is in description
        if not ac_content and description:
            if 'acceptance criteria' in description.lower() or '<ul>' in description.lower():
                # Extract AC from description if it looks like it contains criteria
                import re
                ul_match = re.search(r'<ul[^>]*>(.*?)</ul>', description, re.DOTALL | re.IGNORECASE)
                if ul_match:
                    ac_content = ul_match.group(1)
                    ac_field_name = "System.Description (extracted)"
        
        print("\nAcceptance Criteria:")
        if ac_content:
            # Replacement for the $SELECTION_PLACEHOLDER$:
            if getattr(args, 'verbose', False):
                print(f"(from {ac_field_name}):")
            # Clean up HTML for display
            import re
            clean_ac = re.sub(r'<li[^>]*>', '- ', ac_content)
            clean_ac = re.sub(r'</li>', '', clean_ac)
            clean_ac = re.sub(r'<[^>]+>', '', clean_ac)
            clean_ac = '\n'.join(line.strip() for line in clean_ac.splitlines() if line.strip())
            print(clean_ac)
        else:
            print("(no acceptance criteria found)")
        
        print(f"\n--- End Work Item {work_item_id} ---")
        
    except Exception as e:
        print(f"ERROR querying work item: {e}")


def main():
    parser, args = parse_args()

    # Print the standard header on every invocation so users always see tool
    # identity and short usage regardless of which sub-path they take.
    print_header()

    # If no args provided, print the help and exit 0
    if len(sys.argv) == 1:
        print("")
        parser.print_help()
        sys.exit(0)

    # Handle template help
    # If the user asked for the script-level help, show help (header already printed above)
    if getattr(args, 'help_flag', False):
        print("")
        parser.print_help()
        sys.exit(0)

    if args.help_template:
        if args.help_template == 'azdo':
            print("")
            print("Azure DevOps markdown template:")
            print(AZDO_TEMPLATE)
        else:
            print("")
            print("GitHub markdown template:")
            print(GITHUB_TEMPLATE)
        return

    # Handle query mode
    if args.query:
        if args.target == 'github':
            query_github_issue(args.query, args)
        else:
            query_azdo_workitem(args.query, args)
        return

    # Enforce required file-path after help display so argparse doesn't auto-error
    if not args.file_path:
        print('ERROR: --file-path is required. See help below:')
        parser.print_help()
        sys.exit(2)

    try:
        p = validate_file(args.file_path)
    except Exception as e:
        print(f"ERROR: {e}")
        sys.exit(1)

    fields = extract_fields_from_markdown(p)

    # Merge CLI overrides into metadata (CLI wins, but prefer markdown for target)
    meta = fields.get('metadata', {})
    
    # Use target from markdown if specified, otherwise use CLI --target
    if meta.get('target'):
        target = meta['target'].lower()
        if target not in ['github', 'azdo']:
            print(f"ERROR: Invalid target '{target}' in markdown. Must be 'github' or 'azdo'.")
            sys.exit(2)
        args.target = target
    
    # Apply CLI overrides
    if args.area:
        meta['area'] = args.area
    if args.iteration:
        meta['iteration'] = args.iteration
    if args.repo:
        meta['repository'] = args.repo
    if args.assignee:
        meta['assignee'] = args.assignee
    if args.labels:
        meta['labels'] = args.labels
    if args.config:
        meta['config'] = args.config
    if args.work_item_type:
        meta['work_item_type'] = args.work_item_type
    if args.project:
        meta['project'] = args.project
    fields['metadata'] = meta

    if args.require_criteria and not fields['acceptance_criteria']:
        print('ERROR: Acceptance Criteria is required but not found.')
        sys.exit(2)

    # Validate required metadata per target before performing actions (and before dry-run output)
    if args.target == 'github':
        repo = fields['metadata'].get('repository')
        if not repo:
            print('ERROR: --repo (or ## Repository in the markdown) is required for GitHub target.')
            sys.exit(2)
    elif args.target == 'azdo':
        project = fields['metadata'].get('project') or args.project
        area = fields['metadata'].get('area')
        iteration = fields['metadata'].get('iteration')
        if not project:
            print('ERROR: --project (or ## Project in the markdown) is required for Azure DevOps target.')
            sys.exit(2)
        if not area or not iteration:
            print('ERROR: ## Area and ## Iteration Path in the markdown are required for Azure DevOps target.')
            sys.exit(2)

    if args.dry_run:
        dry_run_print(fields, args)
        # Also show the platform-specific payload preview by invoking the writer with dry_run=True
        if args.target == 'github':
            create_github_issue(fields, args, dry_run=True)
        else:
            create_azdo_workitem(fields, args, dry_run=True)
        sys.exit(0)
    # Route to appropriate writer (stubs for now) which will respect dry-run
    if args.target == 'github':
        created = create_github_issue(fields, args, dry_run=args.dry_run)
    else:
        created = create_azdo_workitem(fields, args, dry_run=args.dry_run)

    if created:
        print('Operation completed (see messages above).')
    else:
        print('No operation performed.')


def create_github_issue(fields: dict, args, dry_run: bool = False) -> bool:
    """Create a GitHub issue using GitHub CLI (`gh`) or API. Returns True if created or placeholder written."""
    title = fields['title']
    description = fields.get('description') or ''
    ac_list = fields.get('acceptance_criteria') or []
    meta = fields.get('metadata', {})
    
    # Get repository from metadata
    repo = meta.get('repository')
    if not repo:
        print('ERROR: Repository not specified in metadata')
        return False
    
    # Build issue body: Description + Acceptance Criteria (similar to PowerShell script)
    body_lines = []
    
    if description:
        body_lines.append(description)
        body_lines.append('')  # blank line
    
    if ac_list:
        body_lines.append('## Acceptance Criteria')
        for ac in ac_list:
            # Keep the checkboxes in the format they appear in the markdown
            if ac.strip().startswith('[ ]') or ac.strip().startswith('[x]'):
                body_lines.append(f'- {ac.strip()}')
            else:
                body_lines.append(f'- [ ] {ac.strip()}')
    
    body = '\n'.join(body_lines)
    labels = (meta.get('labels') or '').strip()  # handle None case
    assignee = (meta.get('assignee') or '').strip() or None  # handle None case

    if dry_run:
        # If the parsed-fields summary has already been printed, avoid
        # repeating repository/title/labels which are already shown in the
        # Parameters block. Only show a concise command preview.
        if globals().get('DRY_RUN_PRINTED'):
            label_list = [label.strip() for label in labels.split(',') if label.strip()]
            cmd_preview = ['gh', 'issue', 'create', '--title', title, '--body-file', '<tempfile>', '--repo', repo]
            for label in label_list:
                cmd_preview.extend(['--label', label])
            if assignee:
                cmd_preview.extend(['--assignee', assignee])
            print(f"[dry-run] Command to execute: {' '.join(cmd_preview)}")
            return True
        # Otherwise show the full body as before
        print('[dry-run] Would create GitHub issue with:')
        print(f'  Repository: {repo}')
        print(f'  Title: {title}')
        print(f'  Labels: {labels}')
        if assignee:
            print(f'  Assignee: {assignee}')
        print('  Body:')
        print(body)
        return True

    # Try to use GitHub CLI first
    import subprocess
    import tempfile
    import os
    
    try:
        # Create temporary file for body content
        with tempfile.NamedTemporaryFile(mode='w', suffix='.md', delete=False, encoding='utf-8') as f:
            f.write(body)
            temp_body_file = f.name
        
        try:
            # Build gh command
            cmd = ['gh', 'issue', 'create', '--title', title, '--body-file', temp_body_file, '--repo', repo]
            
            # Add labels if specified (split comma-separated and trim)
            if labels:
                label_list = [label.strip() for label in labels.split(',') if label.strip()]
                for label in label_list:
                    cmd.extend(['--label', label])
            
            if assignee:
                cmd.extend(['--assignee', assignee])
            
            if dry_run:
                print(f"[dry-run] Command to execute: {' '.join(cmd)}")
                return True
            
            # Execute the command
            print(f"Creating GitHub issue in {repo}...")
            result = subprocess.run(cmd, capture_output=True, text=True, check=True)
            
            # Parse the issue URL from the output
            issue_url = result.stdout.strip()
            print(f"Created GitHub issue: {issue_url}")
            
            return True
            
        except subprocess.CalledProcessError as e:
            print(f"ERROR: GitHub CLI command failed: {e}")
            print(f"stderr: {e.stderr}")
            return False
        except FileNotFoundError:
            print("ERROR: GitHub CLI (`gh`) not found. Please install it or use --dry-run to see what would be created.")
            return False
        finally:
            # Clean up temp file
            if os.path.exists(temp_body_file):
                os.unlink(temp_body_file)
                
    except Exception as e:
        print(f"ERROR creating GitHub issue: {e}")
        return False


def create_azdo_workitem(fields: dict, args, dry_run: bool = False) -> bool:
    """Stub: Create Azure DevOps work item using az boards or API. Returns True if created or placeholder written."""
    title = fields['title']
    # Separate description and acceptance criteria
    description_only = fields.get('description') or ''
    ac_list = fields.get('acceptance_criteria') or []
    # Determine organization and project early so dry-run can show URL
    org = os.environ.get('AZURE_DEVOPS_ORG') or fields.get('metadata', {}).get('organization')
    project = fields.get('metadata', {}).get('project') or args.project
    work_item_type = fields.get('metadata', {}).get('work_item_type') or args.work_item_type or 'PBI'
    if work_item_type.lower() == 'pbi':
        work_item_type = 'Product Backlog Item'

    if dry_run:
        # If the parsed-fields summary has already been printed, avoid
        # repeating title/other fields; show only the request URL and a
        # short note to consult the Parameters block above.
        if globals().get('DRY_RUN_PRINTED'):
            api_version = '7.0'
            type_for_url = urllib.parse.quote(work_item_type, safe='')
            url = f"https://dev.azure.com/{org}/{project}/_apis/wit/workitems/${type_for_url}?api-version={api_version}"
            print('[dry-run] Azure DevOps payload suppressed; see Parameters above for title/labels.')
            print('\nRequest URL:')
            print(' ', url)
            return True
        # Show the eventual REST payload, URL, and headers so user can inspect before sending
        print('[dry-run] Azure DevOps work item payload preview:')
        print(f'  Title: {title}')
        print('  Body:')
        print(description_only)
        # Build sample payload for preview (without validating area)
        sample_payload = []
        sample_payload.append({"op": "add", "path": "/fields/System.Title", "value": title})
        if description_only:
            html_body = '\n'.join([f'<p>{line}</p>' for line in description_only.splitlines() if line.strip()])
            sample_payload.append({"op": "add", "path": "/fields/System.Description", "value": html_body})
        meta = fields.get('metadata', {})
        if meta.get('area'):
            sample_payload.append({"op": "add", "path": "/fields/System.AreaPath", "value": meta.get('area')})
        if meta.get('iteration'):
            sample_payload.append({"op": "add", "path": "/fields/System.IterationPath", "value": meta.get('iteration')})
        if meta.get('assignee'):
            sample_payload.append({"op": "add", "path": "/fields/System.AssignedTo", "value": meta.get('assignee')})
        if meta.get('labels'):
            sample_payload.append({"op": "add", "path": "/fields/System.Tags", "value": meta.get('labels')})
        # Show acceptance criteria handling: we will attempt to write to a dedicated Acceptance Criteria field if present,
        # otherwise we will append the Acceptance Criteria to the Description (not post as a comment)
        if ac_list:
            ac_preview = '\n'.join([f'- {a}' for a in ac_list])
            print('\nAcceptance Criteria:')
            print(ac_preview)
            print('\nNote: on create we will try to write the Acceptance Criteria into a work item field named similarly to "Acceptance Criteria" if the project defines one;')
            print('otherwise the Acceptance Criteria will be appended to the Description.')
        api_version = '7.0'
        type_for_url = urllib.parse.quote(work_item_type, safe='')
        url = f"https://dev.azure.com/{org}/{project}/_apis/wit/workitems/${type_for_url}?api-version={api_version}"
        headers = {"Content-Type": "application/json-patch+json"}
        print('\nRequest URL:')
        print(' ', url)
        print('\nHeaders:')
        for k, v in headers.items():
            print(' ', k+':', v)
        print('\nJSON-Patch payload:')
        print(json.dumps(sample_payload, indent=2))
        return True

    # Use Azure DevOps REST API
    if requests is None:
        print('ERROR: requests library is required to call Azure DevOps REST API. Install with: pip install requests')
        return False

    # Determine organization and project
    org = os.environ.get('AZURE_DEVOPS_ORG') or fields.get('metadata', {}).get('organization')
    project = fields.get('metadata', {}).get('project') or args.project
    if not org or not project:
        print('ERROR: Azure DevOps organization and project are required. Set AZURE_DEVOPS_ORG env or include ## Organization in markdown, and pass --project or include ## Project in markdown.')
        return False

    # Read PAT from environment (preferred)
    pat = os.environ.get('AZURE_DEVOPS_EXT_PAT') or os.environ.get('AZURE_DEVOPS_PAT')
    if not pat:
        print('ERROR: Azure DevOps PAT not found in AZURE_DEVOPS_EXT_PAT (or AZURE_DEVOPS_PAT).')
        return False

    # Build payload (JSON Patch)
    work_item_type = fields.get('metadata', {}).get('work_item_type') or args.work_item_type or 'PBI'
    # Map generic names to common Azure DevOps work item type names
    if work_item_type.lower() == 'pbi':
        work_item_type = 'Product Backlog Item'

    payload = []
    payload.append({"op": "add", "path": "/fields/System.Title", "value": title})
    # Azure expects HTML for Description; we will minimally wrap paragraphs (description only)
    if description_only:
        # Simple transformation: wrap paragraphs in <p>
        html_body = '\n'.join([f'<p>{line}</p>' for line in description_only.splitlines() if line.strip()])
        payload.append({"op": "add", "path": "/fields/System.Description", "value": html_body})

    meta = fields.get('metadata', {})
    if meta.get('area'):
        # Validate area exists in the project to avoid TF401347 / invalid tree name errors
        area_value = meta.get('area')
        area_paths = get_azdo_area_paths(org, project, pat)
        if area_paths is None:
            # Could not validate; proceed but warn
            print('Warning: unable to validate Area Path (could not fetch project classification nodes); attempting create and letting server respond.')
            payload.append({"op": "add", "path": "/fields/System.AreaPath", "value": area_value})
        else:
                # Try to find a canonical area path from the project's area paths (allow suffix/prefix/case variants)
                canonical = _find_canonical_from_candidates(area_value, area_paths)
                if canonical is None:
                    # Could not find canonical path; warn but proceed using provided value (server may accept it)
                    print("Warning: could not find canonical AreaPath for provided value; proceeding with provided value:", repr(area_value))
                    payload.append({"op": "add", "path": "/fields/System.AreaPath", "value": area_value})
                else:
                    # use canonical path
                    payload.append({"op": "add", "path": "/fields/System.AreaPath", "value": canonical})
    if meta.get('iteration'):
        payload.append({"op": "add", "path": "/fields/System.IterationPath", "value": meta.get('iteration')})
    if meta.get('assignee') and meta.get('assignee').strip():
        # Only add assignee if it has a non-empty value
        # Note: Azure DevOps requires the assignee to be a valid identity in the organization
        payload.append({"op": "add", "path": "/fields/System.AssignedTo", "value": meta.get('assignee')})
    if meta.get('labels'):
        # Azure tags are semicolon separated
        tags = meta.get('labels')
        payload.append({"op": "add", "path": "/fields/System.Tags", "value": tags})

    # Handle Acceptance Criteria: prefer a dedicated work item field if available in the project.
    if ac_list:
        ac_text_md = '\n'.join([f'- {a}' for a in ac_list])
        ac_html = '<ul>' + '\n'.join([f'<li>{a}</li>' for a in ac_list]) + '</ul>'
        # Attempt to discover a field whose name/reference contains 'acceptance'
        try:
            fields_url = f'https://dev.azure.com/{org}/{project}/_apis/wit/fields?api-version=7.0'
            fresp = requests.get(fields_url, auth=HTTPBasicAuth('', pat), timeout=15)
            ac_field_ref = None
            if fresp.status_code == 200:
                fj = fresp.json()
                for fld in fj.get('value', []):
                    name = (fld.get('name') or '').lower()
                    ref = (fld.get('referenceName') or '').lower()
                    if 'acceptance' in name or 'acceptance' in ref:
                        ac_field_ref = fld.get('referenceName')
                        break
        except Exception:
            ac_field_ref = None

        if ac_field_ref:
            # write AC into that field (use HTML if possible)
            payload.append({"op": "add", "path": f"/fields/{ac_field_ref}", "value": ac_html})
        else:
            # Do not append AC to Description or post a comment; only write AC when a dedicated field is present.
            print('Warning: Acceptance Criteria field not found; acceptance criteria will NOT be written to the work item.')

    # Construct URL
    api_version = '7.0'
    type_for_url = urllib.parse.quote(work_item_type, safe='')
    url = f"https://dev.azure.com/{org}/{project}/_apis/wit/workitems/${type_for_url}?api-version={api_version}"

    headers = {"Content-Type": "application/json-patch+json"}

    try:
        resp = requests.post(url, json=payload, headers=headers, auth=HTTPBasicAuth('', pat))
    except Exception as e:
        print(f"ERROR: HTTP request failed: {e}")
        return False

    # Handle response: report creation and where Acceptance Criteria ended up.
    if resp.status_code >= 200 and resp.status_code < 300:
        try:
            data = resp.json()
        except Exception:
            data = None

        wi_id = None
        wi_url = None
        if isinstance(data, dict):
            wi_id = data.get('id')
            wi_url = data.get('url')

        if not wi_url and wi_id:
            wi_url = f"https://dev.azure.com/{org}/{project}/_workitems/edit/{wi_id}"

        print(f"Created Azure DevOps work item: {wi_url or '(no url returned)'} (id: {wi_id})")

        # Acceptance Criteria handling: if we discovered a dedicated field earlier (ac_field_ref)
        # it was written into the payload. Otherwise AC was appended to the Description.
        if ac_list:
            if 'ac_field_ref' in locals() and ac_field_ref:
                print(f"Acceptance Criteria written to field: {ac_field_ref}")
            else:
                print('Acceptance Criteria added to the Description field of the work item.')

        return True
    else:
        try:
            err = resp.json()
        except Exception:
            err = resp.text
        print(f"ERROR creating work item: HTTP {resp.status_code} - {err}")
        return False

if __name__ == '__main__':
    main()
