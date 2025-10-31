"""
Markdown Parser for SDO work item files.
"""

import re
from typing import Dict, List, Any


class MarkdownParser:
    """Parser for markdown files containing work item information."""
    
    def parse_file(self, file_path: str) -> Dict[str, Any]:
        """
        Parse a markdown file and extract work item information.
        
        Args:
            file_path: Path to the markdown file
            
        Returns:
            Dictionary containing:
            - title: Work item title
            - description: Main description
            - metadata: Dictionary of metadata fields
            - acceptance_criteria: List of acceptance criteria
        """
        with open(file_path, 'r', encoding='utf-8') as f:
            content = f.read()
        
        return self.parse_content(content)
    
    def parse_content(self, content: str) -> Dict[str, Any]:
        """
        Parse markdown content and extract work item information.
        
        Args:
            content: Raw markdown content
            
        Returns:
            Dictionary with parsed information
        """
        lines = content.split('\n')
        
        # Initialize result
        result = {
            'title': '',
            'description': '',
            'metadata': {},
            'acceptance_criteria': []
        }
        
        # State tracking
        in_acceptance_criteria = False
        description_lines = []
        
        for line in lines:
            line = line.strip()
            
            # Skip empty lines
            if not line:
                if description_lines and not in_acceptance_criteria:
                    description_lines.append('')
                continue
            
            # Extract title (first # heading)
            if line.startswith('# ') and not result['title']:
                result['title'] = line[2:].strip()
                continue
            
            # Extract metadata (## Key: Value format)
            metadata_match = re.match(r'^##\s*([^:]+):\s*(.*)$', line)
            if metadata_match:
                key = metadata_match.group(1).strip().lower().replace(' ', '_')
                value = metadata_match.group(2).strip()
                result['metadata'][key] = value
                continue
            
            # Check for Acceptance Criteria section
            if line.lower() in ['## acceptance criteria', '## acceptance criteria:', 
                               '### acceptance criteria', '### acceptance criteria:']:
                in_acceptance_criteria = True
                continue
            
            # Extract acceptance criteria
            if in_acceptance_criteria:
                # Check for new section starting
                if line.startswith('#'):
                    in_acceptance_criteria = False
                    description_lines.append(line)
                    continue
                
                # Extract criteria items
                criteria_match = re.match(r'^[-*]\s*\[\s*\]\s*(.+)$', line)
                if criteria_match:
                    result['acceptance_criteria'].append(criteria_match.group(1).strip())
                    continue
                
                # Handle numbered criteria
                numbered_match = re.match(r'^\d+\.\s*(.+)$', line)
                if numbered_match:
                    result['acceptance_criteria'].append(numbered_match.group(1).strip())
                    continue
            
            # Add to description if not in special sections
            if not in_acceptance_criteria and not line.startswith('#'):
                description_lines.append(line)
        
        # Join description lines
        result['description'] = '\n'.join(description_lines).strip()
        
        return result
    
    def validate_structure(self, parsed_content: Dict[str, Any]) -> List[str]:
        """
        Validate the parsed content structure.
        
        Args:
            parsed_content: Result from parse_content()
            
        Returns:
            List of validation error messages (empty if valid)
        """
        errors = []
        
        if not parsed_content.get('title'):
            errors.append("Missing title (no # heading found)")
        
        if not parsed_content.get('metadata'):
            errors.append("No metadata found (no ## Key: Value pairs)")
        
        return errors